using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using V_Task.Models;

namespace V_Task.Services;

/// <summary>
/// GPU monitoring service using PerformanceCounter for Task Manager-like accuracy
/// Uses "GPU Engine" category for usage and "GPU Adapter Memory" for VRAM
/// </summary>
public class GpuMonitorService : IDisposable
{
    // GPU identification
    private string? _discreteGpuLuid;
    private string? _discreteGpuName;
    
    // Performance counters for real-time monitoring
    private List<PerformanceCounter>? _gpu3DCounters;
    private PerformanceCounter? _gpuMemoryUsedCounter;
    private PerformanceCounter? _gpuMemoryTotalCounter;
    
    private bool _countersInitialized;
    private double _lastUsage;
    private double _lastMemoryUsedGB;
    private double _lastMemoryTotalGB;
    
    // Cached static info
    public string? GpuName { get; private set; }
    public string? DriverVersion { get; private set; }
    public double TotalMemoryGB { get; private set; }
    
    public void Initialize()
    {
        if (_countersInitialized) return;
        
        try
        {
            // First, load static GPU info from WMI
            LoadStaticInfo();
            
            // Then initialize performance counters
            InitializePerformanceCounters();
            
            _countersInitialized = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing GPU monitor: {ex.Message}");
        }
    }
    
    private void LoadStaticInfo()
    {
        try
        {
            using var gpuSearcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController");
            foreach (ManagementObject mo in gpuSearcher.Get())
            {
                var gpuName = mo["Name"];
                var driverVersion = mo["DriverVersion"];
                var adapterRam = mo["AdapterRAM"];
                
                if (gpuName != null)
                {
                    string name = gpuName.ToString() ?? "";
                    
                    // Skip integrated Intel GPUs (UHD, HD Graphics, Iris)
                    // But include Intel Arc which is discrete
                    bool isIntelIntegrated = name.Contains("Intel") && 
                        (name.Contains("UHD") || name.Contains("HD Graphics") || name.Contains("Iris")) &&
                        !name.Contains("Arc");
                    bool isBasicDriver = name.Contains("Microsoft") || name.Contains("Basic");
                    
                    // Prefer discrete GPU
                    if (!isIntelIntegrated && !isBasicDriver)
                    {
                        GpuName = name;
                        _discreteGpuName = name;
                        
                        if (driverVersion != null)
                            DriverVersion = driverVersion.ToString();
                        
                        // Get VRAM - AdapterRAM is 32-bit limited, so we need workarounds
                        TotalMemoryGB = GetGpuMemorySize(name, adapterRam);
                        break;
                    }
                    else if (string.IsNullOrEmpty(GpuName))
                    {
                        // Use integrated as fallback
                        GpuName = name;
                        if (driverVersion != null)
                            DriverVersion = driverVersion.ToString();
                        TotalMemoryGB = GetGpuMemorySize(name, adapterRam);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading GPU static info: {ex.Message}");
        }
    }
    
    private double GetGpuMemorySize(string gpuName, object? adapterRam)
    {
        // Try to get from performance counters first (most accurate for >4GB)
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, DedicatedLimit FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPULocalAdapterMemory");
            
            double maxDedicatedLimit = 0;
            foreach (ManagementObject mo in searcher.Get())
            {
                var dedicatedLimit = mo["DedicatedLimit"];
                if (dedicatedLimit != null)
                {
                    double limitBytes = Convert.ToDouble(dedicatedLimit);
                    if (limitBytes > maxDedicatedLimit)
                        maxDedicatedLimit = limitBytes;
                }
            }
            
            if (maxDedicatedLimit > 0)
            {
                _lastMemoryTotalGB = maxDedicatedLimit / (1024.0 * 1024.0 * 1024.0);
                return _lastMemoryTotalGB;
            }
        }
        catch { }
        
        // Fallback to AdapterRAM with known GPU memory table
        if (adapterRam != null)
        {
            try
            {
                uint ramBytesUint = Convert.ToUInt32(adapterRam);
                double ramGB = ramBytesUint / (1024.0 * 1024.0 * 1024.0);
                
                // AdapterRAM overflows for >4GB, use known values
                if (ramGB < 4.0)
                {
                    ramGB = GetKnownGpuMemory(gpuName, ramGB);
                }
                
                _lastMemoryTotalGB = ramGB;
                return ramGB;
            }
            catch { }
        }
        
        return 0;
    }
    
    private double GetKnownGpuMemory(string gpuName, double detectedGB)
    {
        // Known GPU memory sizes based on model names
        if (gpuName.Contains("Arc"))
        {
            if (gpuName.Contains("B580")) return 12;
            if (gpuName.Contains("A770")) return 16;
            if (gpuName.Contains("A750")) return 8;
            if (gpuName.Contains("A580")) return 8;
            if (gpuName.Contains("A380")) return 6;
            if (gpuName.Contains("A310")) return 4;
            return 8;
        }
        
        // NVIDIA RTX 40 series
        if (gpuName.Contains("4090")) return 24;
        if (gpuName.Contains("4080")) return 16;
        if (gpuName.Contains("4070 Ti Super")) return 16;
        if (gpuName.Contains("4070 Ti")) return 12;
        if (gpuName.Contains("4070 Super")) return 12;
        if (gpuName.Contains("4070")) return 12;
        if (gpuName.Contains("4060 Ti") && gpuName.Contains("16")) return 16;
        if (gpuName.Contains("4060 Ti")) return 8;
        if (gpuName.Contains("4060")) return 8;
        
        // NVIDIA RTX 30 series
        if (gpuName.Contains("3090")) return 24;
        if (gpuName.Contains("3080 Ti")) return 12;
        if (gpuName.Contains("3080") && gpuName.Contains("12")) return 12;
        if (gpuName.Contains("3080")) return 10;
        if (gpuName.Contains("3070 Ti")) return 8;
        if (gpuName.Contains("3070")) return 8;
        if (gpuName.Contains("3060 Ti")) return 8;
        if (gpuName.Contains("3060")) return 12;
        
        // AMD RX 7000 series
        if (gpuName.Contains("7900 XTX")) return 24;
        if (gpuName.Contains("7900 XT")) return 20;
        if (gpuName.Contains("7900 GRE")) return 16;
        if (gpuName.Contains("7800 XT")) return 16;
        if (gpuName.Contains("7700 XT")) return 12;
        if (gpuName.Contains("7600")) return 8;
        
        // AMD RX 6000 series
        if (gpuName.Contains("6900")) return 16;
        if (gpuName.Contains("6800")) return 16;
        if (gpuName.Contains("6700")) return 12;
        if (gpuName.Contains("6600")) return 8;
        
        // If unknown, try to calculate from overflow
        if (detectedGB < 1.0) return 8;
        if (detectedGB < 3.0) return 6;
        
        return Math.Max(4, detectedGB);
    }
    
    private void InitializePerformanceCounters()
    {
        try
        {
            _gpu3DCounters = new List<PerformanceCounter>();
            
            // Get all GPU Engine instances
            var category = new PerformanceCounterCategory("GPU Engine");
            string[] instanceNames = category.GetInstanceNames();
            
            // Group by LUID to identify different GPUs
            var gpuGroups = instanceNames
                .Where(name => name.Contains("engtype_3D"))
                .Select(name => 
                {
                    var match = Regex.Match(name, @"luid_(0x[0-9A-Fa-f]+_0x[0-9A-Fa-f]+)");
                    return new { Name = name, Luid = match.Success ? match.Groups[1].Value : "" };
                })
                .Where(x => !string.IsNullOrEmpty(x.Luid))
                .GroupBy(x => x.Luid)
                .ToList();
            
            // Find discrete GPU (usually has more 3D engine instances or matches our GPU name)
            var discreteGpuGroup = gpuGroups.OrderByDescending(g => g.Count()).FirstOrDefault();
            
            if (discreteGpuGroup != null)
            {
                _discreteGpuLuid = discreteGpuGroup.Key;
                
                // Create counters for ALL 3D engines of this GPU
                // Task Manager sums them up
                foreach (var instance in discreteGpuGroup)
                {
                    try
                    {
                        var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance.Name);
                        counter.NextValue(); // Initialize
                        _gpu3DCounters.Add(counter);
                    }
                    catch { }
                }
            }
            
            // Initialize GPU memory counter
            InitializeMemoryCounters();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing GPU performance counters: {ex.Message}");
        }
    }
    
    private void InitializeMemoryCounters()
    {
        try
        {
            var memCategory = new PerformanceCounterCategory("GPU Adapter Memory");
            string[] memInstances = memCategory.GetInstanceNames();
            
            // Find memory instance for our discrete GPU
            string? memInstance = null;
            if (!string.IsNullOrEmpty(_discreteGpuLuid))
            {
                memInstance = memInstances.FirstOrDefault(name => name.Contains(_discreteGpuLuid));
            }
            
            if (string.IsNullOrEmpty(memInstance) && memInstances.Length > 0)
            {
                // Fallback: find instance with highest dedicated memory
                double maxDedicated = 0;
                foreach (var instance in memInstances)
                {
                    try
                    {
                        using var tempCounter = new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", instance);
                        double val = tempCounter.NextValue();
                        if (val > maxDedicated)
                        {
                            maxDedicated = val;
                            memInstance = instance;
                        }
                    }
                    catch { }
                }
            }
            
            if (!string.IsNullOrEmpty(memInstance))
            {
                _gpuMemoryUsedCounter = new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", memInstance);
                _gpuMemoryUsedCounter.NextValue();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing GPU memory counters: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get current GPU metrics
    /// </summary>
    public GpuMetrics GetMetrics()
    {
        var metrics = new GpuMetrics();
        
        // Get GPU usage by summing all 3D engine usages (like Task Manager)
        if (_gpu3DCounters != null && _gpu3DCounters.Count > 0)
        {
            try
            {
                double totalUsage = 0;
                foreach (var counter in _gpu3DCounters)
                {
                    try
                    {
                        float value = counter.NextValue();
                        totalUsage += value;
                    }
                    catch { }
                }
                
                // Task Manager shows the sum of all 3D engines, capped at 100%
                _lastUsage = Math.Min(100, totalUsage);
            }
            catch { }
        }
        metrics.Usage = _lastUsage;
        
        // Get GPU memory usage
        if (_gpuMemoryUsedCounter != null)
        {
            try
            {
                float memBytes = _gpuMemoryUsedCounter.NextValue();
                _lastMemoryUsedGB = memBytes / (1024.0 * 1024.0 * 1024.0);
            }
            catch { }
        }
        metrics.MemoryUsedGB = _lastMemoryUsedGB;
        
        // Calculate memory percentage
        if (_lastMemoryTotalGB > 0)
        {
            metrics.MemoryUsagePercent = (_lastMemoryUsedGB / _lastMemoryTotalGB) * 100;
        }
        else if (TotalMemoryGB > 0)
        {
            metrics.MemoryUsagePercent = (_lastMemoryUsedGB / TotalMemoryGB) * 100;
        }
        
        return metrics;
    }
    
    /// <summary>
    /// Get GPU metrics asynchronously (for slower WMI calls if needed)
    /// </summary>
    public async Task<GpuMetrics> GetMetricsAsync()
    {
        return await Task.Run(() => GetMetrics());
    }
    
    public void Dispose()
    {
        if (_gpu3DCounters != null)
        {
            foreach (var counter in _gpu3DCounters)
            {
                counter?.Dispose();
            }
            _gpu3DCounters.Clear();
        }
        
        _gpuMemoryUsedCounter?.Dispose();
        _gpuMemoryTotalCounter?.Dispose();
    }
}
