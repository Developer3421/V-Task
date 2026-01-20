using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using V_Task.Models;

namespace V_Task.Services;

/// <summary>
/// GPU monitoring service using WMI for GPU detection and PerformanceCounter for metrics
/// </summary>
public class GpuMonitorService : IDisposable
{
    // GPU info from WMI (real physical GPUs)
    private sealed class GpuInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? DriverVersion { get; set; }
        public double TotalMemoryGB { get; set; }
        public bool IsIntegrated { get; set; }
        public bool IsMock { get; set; }
        
        // Live metrics
        public double LastUsage { get; set; }
        public double LastMemoryUsedGB { get; set; }
    }
    
    // All detected GPUs (from WMI + mock if needed)
    private readonly List<GpuInfo> _gpus = new();
    
    // Performance counters for metrics (aggregated for the primary GPU)
    private readonly List<PerformanceCounter> _gpu3DCounters = new();
    
    // Per-adapter memory counters: key = adapter instance name or LUID, value = counter
    private readonly Dictionary<string, PerformanceCounter> _memoryCountersByAdapter = new();

    private bool _initialized;

    // Cached static info (primary GPU for backward compat)
    public string? GpuName { get; private set; }
    public string? DriverVersion { get; private set; }
    public double TotalMemoryGB { get; private set; }

    public void Initialize()
    {
        if (_initialized) return;

        try
        {
            LoadGpusFromWmi();
            InitializePerformanceCounters();
            
            // If no real GPUs found, add a mock one
            if (_gpus.Count == 0)
            {
                _gpus.Add(new GpuInfo
                {
                    Id = "mock_gpu",
                    Name = "Інша відеокарта",
                    IsIntegrated = false,
                    IsMock = true,
                    DriverVersion = "Н/Д",
                    TotalMemoryGB = 0
                });
            }
            
            // Set primary GPU info for backward compat (prefer discrete)
            var primary = _gpus.FirstOrDefault(g => !g.IsIntegrated && !g.IsMock) ?? _gpus.FirstOrDefault();
            if (primary != null)
            {
                GpuName = primary.Name;
                DriverVersion = primary.DriverVersion;
                TotalMemoryGB = primary.TotalMemoryGB;
            }

            _initialized = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing GPU monitor: {ex.Message}");
        }
    }

    private void LoadGpusFromWmi()
    {
        try
        {
            using var gpuSearcher = new ManagementObjectSearcher(
                "SELECT Name, DriverVersion, AdapterRAM, PNPDeviceID FROM Win32_VideoController");

            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            int gpuIndex = 0;
            foreach (ManagementObject mo in gpuSearcher.Get())
            {
                var gpuName = mo["Name"]?.ToString();
                if (string.IsNullOrWhiteSpace(gpuName))
                    continue;

                // Skip Microsoft Basic Display Adapter and similar virtual adapters
                bool isBasicDriver = gpuName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                     gpuName.Contains("Basic", StringComparison.OrdinalIgnoreCase) ||
                                     gpuName.Contains("Remote", StringComparison.OrdinalIgnoreCase) ||
                                     gpuName.Contains("Virtual", StringComparison.OrdinalIgnoreCase);

                if (isBasicDriver)
                    continue;
                
                // Skip duplicates by name
                if (seenNames.Contains(gpuName))
                    continue;
                
                seenNames.Add(gpuName);

                var driverVersion = mo["DriverVersion"]?.ToString();
                var adapterRam = mo["AdapterRAM"];
                var pnpDeviceId = mo["PNPDeviceID"]?.ToString() ?? $"gpu_{gpuIndex}";

                // Determine if integrated GPU
                bool isIntegrated = IsIntegratedGpu(gpuName);

                var gpu = new GpuInfo
                {
                    Id = pnpDeviceId,
                    Name = gpuName,
                    DriverVersion = driverVersion,
                    TotalMemoryGB = GetGpuMemorySize(gpuName, adapterRam),
                    IsIntegrated = isIntegrated,
                    IsMock = false
                };
                
                _gpus.Add(gpu);
                gpuIndex++;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading GPU info from WMI: {ex.Message}");
        }
    }
    
    private static bool IsIntegratedGpu(string gpuName)
    {
        // Intel integrated GPUs
        if (gpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            // Intel Arc is discrete
            if (gpuName.Contains("Arc", StringComparison.OrdinalIgnoreCase))
                return false;
            
            // Intel UHD, HD Graphics, Iris are integrated
            if (gpuName.Contains("UHD", StringComparison.OrdinalIgnoreCase) ||
                gpuName.Contains("HD Graphics", StringComparison.OrdinalIgnoreCase) ||
                gpuName.Contains("Iris", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        // AMD integrated GPUs
        if (gpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            gpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
        {
            // RX series is discrete
            if (gpuName.Contains("RX", StringComparison.OrdinalIgnoreCase))
                return false;
            
            // Radeon Graphics (without RX) or Vega integrated
            if (gpuName.Contains("Radeon Graphics", StringComparison.OrdinalIgnoreCase) ||
                (gpuName.Contains("Vega", StringComparison.OrdinalIgnoreCase) && 
                 !gpuName.Contains("RX", StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        
        // NVIDIA is always discrete (no integrated GPUs)
        // GeForce, RTX, GTX, Quadro - all discrete
        
        return false;
    }

    private void InitializePerformanceCounters()
    {
        try
        {
            // Get all 3D engine counters (aggregate for overall GPU usage)
            var category = new PerformanceCounterCategory("GPU Engine");
            string[] instanceNames = category.GetInstanceNames();

            foreach (var name in instanceNames)
            {
                if (name.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", name);
                        counter.NextValue();
                        _gpu3DCounters.Add(counter);
                    }
                    catch { }
                }
            }

            // Memory counter (get first available)
            try
            {
                var memCategory = new PerformanceCounterCategory("GPU Adapter Memory");
                string[] memInstances = memCategory.GetInstanceNames();
                foreach (var instanceName in memInstances)
                {
                    try
                    {
                        var counter = new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", instanceName);
                        counter.NextValue();
                        // Store by instance name (contains LUID)
                        _memoryCountersByAdapter[instanceName] = counter;
                    }
                    catch { }
                }
            }
            catch { }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing GPU performance counters: {ex.Message}");
        }
    }
    
    private double GetGpuMemorySize(string gpuName, object? adapterRam)
    {
        // Try to get from performance counters first (most accurate for >4GB)
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DedicatedLimit FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPULocalAdapterMemory");

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
                return maxDedicatedLimit / (1024.0 * 1024.0 * 1024.0);
            }
        }
        catch { }

        if (adapterRam != null)
        {
            try
            {
                uint ramBytesUint = Convert.ToUInt32(adapterRam);
                double ramGB = ramBytesUint / (1024.0 * 1024.0 * 1024.0);

                // AdapterRAM overflows for >4GB, use known values
                if (ramGB < 4.0)
                    ramGB = GetKnownGpuMemory(gpuName, ramGB);

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

    private void UpdateMetrics()
    {
        // Update GPU usage from performance counters
        double totalUsage = 0;
        foreach (var counter in _gpu3DCounters)
        {
            try { totalUsage += counter.NextValue(); } catch { }
        }
        totalUsage = Math.Min(100, totalUsage);

        // Collect memory usage from all counters - sum of all dedicated memory
        double totalMemoryUsedGB = 0;
        foreach (var kvp in _memoryCountersByAdapter)
        {
            try
            {
                float memBytes = kvp.Value.NextValue();
                totalMemoryUsedGB += memBytes / (1024.0 * 1024.0 * 1024.0);
            }
            catch { }
        }

        // Distribute memory to GPUs proportionally based on their total VRAM
        // Primary GPU (non-integrated, non-mock) gets the dedicated usage
        var primary = _gpus.FirstOrDefault(g => !g.IsIntegrated && !g.IsMock);
        var integrated = _gpus.FirstOrDefault(g => g.IsIntegrated && !g.IsMock);
        
        if (primary != null)
        {
            // Discrete GPU typically uses most of the dedicated memory
            primary.LastUsage = totalUsage;
            primary.LastMemoryUsedGB = totalMemoryUsedGB;
        }
        else if (_gpus.Count > 0)
        {
            // If only integrated GPU available
            var firstGpu = _gpus.FirstOrDefault(g => !g.IsMock) ?? _gpus.FirstOrDefault();
            if (firstGpu != null)
            {
                firstGpu.LastUsage = totalUsage;
                firstGpu.LastMemoryUsedGB = totalMemoryUsedGB;
            }
        }
        
        // Integrated GPU memory is typically shared with system RAM and not reported via these counters
        if (integrated != null)
        {
            integrated.LastUsage = 0;
            integrated.LastMemoryUsedGB = 0;
        }
    }

    /// <summary>
    /// Get current GPU metrics (primary adapter) for backward-compat UI.
    /// </summary>
    public GpuMetrics GetMetrics()
    {
        UpdateMetrics();
        
        var primary = _gpus.FirstOrDefault(g => !g.IsIntegrated && !g.IsMock) ?? _gpus.FirstOrDefault();

        return new GpuMetrics
        {
            Usage = primary?.LastUsage ?? 0,
            MemoryUsedGB = primary?.LastMemoryUsedGB ?? 0,
            MemoryUsagePercent = primary != null && primary.TotalMemoryGB > 0 
                ? Math.Clamp((primary.LastMemoryUsedGB / primary.TotalMemoryGB) * 100.0, 0, 100) 
                : 0
        };
    }

    /// <summary>
    /// All detected GPU adapters (discrete + integrated/mock)
    /// </summary>
    public IReadOnlyList<GpuAdapterInfo> GetAdapters()
    {
        UpdateMetrics();
        
        var result = new List<GpuAdapterInfo>();

        foreach (var gpu in _gpus)
        {
            double percent = gpu.TotalMemoryGB > 0 
                ? Math.Clamp((gpu.LastMemoryUsedGB / gpu.TotalMemoryGB) * 100.0, 0, 100) 
                : 0;

            result.Add(new GpuAdapterInfo
            {
                Id = gpu.Id,
                Name = gpu.Name,
                IsIntegrated = gpu.IsIntegrated,
                IsMock = gpu.IsMock,
                IsActive = !gpu.IsMock, // Real GPUs are active, mock GPUs are not
                DriverVersion = gpu.DriverVersion,
                TotalMemoryGB = gpu.TotalMemoryGB,
                UsedMemoryGB = gpu.LastMemoryUsedGB,
                UsagePercent = percent
            });
        }

        // Sort: Real discrete GPUs first, then real integrated, then mock GPUs
        return result
            .OrderBy(a => a.IsMock)          // Real GPUs first (IsMock=false)
            .ThenBy(a => a.IsIntegrated)     // Discrete first (IsIntegrated=false)
            .ThenBy(a => a.Name)
            .ToList();
    }

    public async Task<GpuMetrics> GetMetricsAsync() => await Task.Run(GetMetrics);

    public void Dispose()
    {
        foreach (var counter in _gpu3DCounters)
            counter.Dispose();
        _gpu3DCounters.Clear();
        
        foreach (var kvp in _memoryCountersByAdapter)
            kvp.Value.Dispose();
        _memoryCountersByAdapter.Clear();
    }
}
