using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using LibreHardwareMonitor.Hardware;

namespace V_Task.Services;

/// <summary>
/// Hardware monitoring service using LibreHardwareMonitor
/// Provides static CPU and GPU info and GPU memory usage
/// </summary>
public class HardwareMonitorService : IDisposable
{
    private Computer? _computer;
    private bool _initialized;
    
    // Cached hardware references
    private IHardware? _cpu;
    private IHardware? _gpu;
    
    // CPU static info
    public string? CpuName { get; private set; }
    public int PhysicalCores { get; private set; }
    public int LogicalCores { get; private set; }
    
    // GPU static info and memory usage
    public string? GpuName { get; private set; }
    public double GpuMemoryUsage { get; private set; }
    public double GpuMemoryUsedMB { get; private set; }
    public double GpuMemoryTotalMB { get; private set; }
    
    public void Initialize()
    {
        if (_initialized) return;
        
        try
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = false,
                IsMotherboardEnabled = false,
                IsStorageEnabled = false,
                IsNetworkEnabled = false,
                IsControllerEnabled = false
            };
            
            _computer.Open();
            
            // Find CPU and GPU hardware
            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    _cpu = hardware;
                    CpuName = hardware.Name;
                    CountCpuCores();
                }
                else if (IsDiscreteGpu(hardware))
                {
                    _gpu = hardware;
                    GpuName = hardware.Name;
                }
            }
            
            // If no discrete GPU found, use any available GPU
            if (_gpu == null)
            {
                _gpu = _computer.Hardware.FirstOrDefault(h => 
                    h.HardwareType == HardwareType.GpuNvidia ||
                    h.HardwareType == HardwareType.GpuAmd ||
                    h.HardwareType == HardwareType.GpuIntel);
                    
                if (_gpu != null)
                    GpuName = _gpu.Name;
            }
            
            // Get GPU total memory (static)
            UpdateGpuTotalMemory();
            
            _initialized = true;
            
            // Initial update for GPU memory usage
            Update();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing LibreHardwareMonitor: {ex.Message}");
        }
    }
    
    private bool IsDiscreteGpu(IHardware hardware)
    {
        // Nvidia and AMD are always discrete
        if (hardware.HardwareType == HardwareType.GpuNvidia ||
            hardware.HardwareType == HardwareType.GpuAmd)
            return true;
            
        // Intel Arc is discrete
        if (hardware.HardwareType == HardwareType.GpuIntel &&
            hardware.Name != null &&
            hardware.Name.Contains("Arc", StringComparison.OrdinalIgnoreCase))
            return true;
            
        return false;
    }
    
    private void CountCpuCores()
    {
        // LibreHardwareMonitor sensors are not a reliable source for physical core count
        // (often represent logical cores). Prefer WMI.
        try
        {
            using var cpuSearcher = new ManagementObjectSearcher(
                "SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");

            foreach (ManagementObject mo in cpuSearcher.Get())
            {
                var cores = mo["NumberOfCores"];
                if (cores != null)
                    PhysicalCores = Convert.ToInt32(cores);

                var logical = mo["NumberOfLogicalProcessors"];
                if (logical != null)
                    LogicalCores = Convert.ToInt32(logical);

                break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting CPU core count via WMI: {ex.Message}");
        }

        // Fallbacks (avoid guessing /2)
        if (LogicalCores <= 0)
            LogicalCores = Environment.ProcessorCount;

        if (PhysicalCores <= 0)
            PhysicalCores = LogicalCores;
    }
    
    private void UpdateGpuTotalMemory()
    {
        if (_gpu == null) return;
        
        _gpu.Update();
        
        foreach (var subHardware in _gpu.SubHardware)
        {
            subHardware.Update();
        }
        
        var allSensors = _gpu.Sensors.ToList();
        foreach (var subHardware in _gpu.SubHardware)
        {
            allSensors.AddRange(subHardware.Sensors);
        }
        
        foreach (var sensor in allSensors)
        {
            string sensorName = sensor.Name ?? "";
            float value = sensor.Value ?? 0;
            
            if (sensor.SensorType == SensorType.SmallData)
            {
                if (sensorName.Contains("Memory Total", StringComparison.OrdinalIgnoreCase) ||
                    sensorName.Equals("D3D Dedicated Memory Total", StringComparison.OrdinalIgnoreCase) ||
                    sensorName.Contains("VRAM Total", StringComparison.OrdinalIgnoreCase))
                {
                    if (value > GpuMemoryTotalMB)
                        GpuMemoryTotalMB = value;
                }
            }
            else if (sensor.SensorType == SensorType.Data)
            {
                if (sensorName.Contains("Memory Total", StringComparison.OrdinalIgnoreCase))
                {
                    if (value * 1024 > GpuMemoryTotalMB)
                        GpuMemoryTotalMB = value * 1024;
                }
            }
        }
    }
    
    /// <summary>
    /// Update GPU memory usage metrics
    /// </summary>
    public void Update()
    {
        if (!_initialized || _computer == null) return;
        
        try
        {
            UpdateGpuMemoryUsage();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating hardware metrics: {ex.Message}");
        }
    }
    
    private void UpdateGpuMemoryUsage()
    {
        if (_gpu == null) return;
        
        _gpu.Update();
        
        foreach (var subHardware in _gpu.SubHardware)
        {
            subHardware.Update();
        }
        
        double memUsage = 0;
        double memUsedMB = 0;
        
        var allSensors = _gpu.Sensors.ToList();
        foreach (var subHardware in _gpu.SubHardware)
        {
            allSensors.AddRange(subHardware.Sensors);
        }
        
        foreach (var sensor in allSensors)
        {
            string sensorName = sensor.Name ?? "";
            float value = sensor.Value ?? 0;
            
            switch (sensor.SensorType)
            {
                case SensorType.Load:
                    // Memory usage percentage
                    if ((sensorName.Contains("Memory", StringComparison.OrdinalIgnoreCase) ||
                         sensorName.Contains("VRAM", StringComparison.OrdinalIgnoreCase)) &&
                        !sensorName.Contains("Controller", StringComparison.OrdinalIgnoreCase) &&
                        !sensorName.Contains("Clock", StringComparison.OrdinalIgnoreCase))
                    {
                        if (value > memUsage)
                            memUsage = value;
                    }
                    break;
                    
                case SensorType.SmallData:
                    // Memory in MB
                    if (sensorName.Contains("Memory Used", StringComparison.OrdinalIgnoreCase) ||
                        sensorName.Contains("GPU Memory Used", StringComparison.OrdinalIgnoreCase) ||
                        sensorName.Equals("D3D Dedicated Memory Used", StringComparison.OrdinalIgnoreCase) ||
                        sensorName.Contains("VRAM Used", StringComparison.OrdinalIgnoreCase))
                    {
                        if (value > memUsedMB)
                            memUsedMB = value;
                    }
                    break;
                    
                case SensorType.Data:
                    // Memory in GB (convert to MB)
                    if (sensorName.Contains("Memory Used", StringComparison.OrdinalIgnoreCase))
                    {
                        if (value * 1024 > memUsedMB)
                            memUsedMB = value * 1024;
                    }
                    break;
            }
        }
        
        GpuMemoryUsage = memUsage;
        GpuMemoryUsedMB = memUsedMB;
        
        // Calculate memory usage percentage if not available
        if (GpuMemoryUsage <= 0 && GpuMemoryTotalMB > 0 && GpuMemoryUsedMB > 0)
        {
            GpuMemoryUsage = (GpuMemoryUsedMB / GpuMemoryTotalMB) * 100;
        }
    }
    
    /// <summary>
    /// Get debug info about all sensors (for troubleshooting)
    /// </summary>
    public List<string> GetAllSensorInfo()
    {
        var info = new List<string>();
        
        if (_computer == null) return info;
        
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            info.Add($"=== {hardware.Name} ({hardware.HardwareType}) ===");
            
            foreach (var sensor in hardware.Sensors)
            {
                info.Add($"  [{sensor.SensorType}] {sensor.Name}: {sensor.Value}");
            }
            
            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Update();
                info.Add($"  --- {subHardware.Name} ---");
                
                foreach (var sensor in subHardware.Sensors)
                {
                    info.Add($"    [{sensor.SensorType}] {sensor.Name}: {sensor.Value}");
                }
            }
        }
        
        return info;
    }
    
    public void Dispose()
    {
        _computer?.Close();
        _computer = null;
        _initialized = false;
    }
}
