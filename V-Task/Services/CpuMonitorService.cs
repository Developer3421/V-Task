using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using V_Task.Models;

namespace V_Task.Services;

/// <summary>
/// CPU monitoring service using PerformanceCounter for Task Manager-like accuracy
/// Uses "% Processor Utility" counter which accounts for Turbo Boost
/// </summary>
public class CpuMonitorService : IDisposable
{
    private PerformanceCounter? _totalCpuCounter;
    private PerformanceCounter[]? _coreCounters;
    private PerformanceCounter? _frequencyCounter;
    private float _lastTotalUsage;
    private float[]? _lastCoreUsages;
    private double _lastFrequency;
    private bool _initialized;
    
    // Cached static info
    public string? CpuName { get; private set; }
    public string? BaseFrequency { get; private set; }
    public int PhysicalCores { get; private set; }
    public int LogicalCores { get; private set; }
    
    public void Initialize()
    {
        if (_initialized) return;
        
        try
        {
            // Load static CPU info first
            LoadStaticInfo();
            
            int coreCount = Environment.ProcessorCount;
            _lastCoreUsages = new float[coreCount];
            
            // Initialize total CPU counter - "% Processor Utility" is what Task Manager uses
            // It accounts for Turbo Boost frequency scaling
            try
            {
                _totalCpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                _totalCpuCounter.NextValue(); // First call always returns 0
            }
            catch
            {
                // Fallback to older counter (less accurate but works everywhere)
                try
                {
                    _totalCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _totalCpuCounter.NextValue();
                }
                catch { }
            }
            
            // Initialize per-core counters
            _coreCounters = new PerformanceCounter[coreCount];
            for (int i = 0; i < coreCount; i++)
            {
                try
                {
                    // Try modern counter first
                    _coreCounters[i] = new PerformanceCounter("Processor Information", "% Processor Utility", $"0,{i}");
                    _coreCounters[i].NextValue();
                }
                catch
                {
                    try
                    {
                        // Fallback to old counter
                        _coreCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                        _coreCounters[i].NextValue();
                    }
                    catch { }
                }
            }
            
            // Frequency counter - shows current vs base frequency ratio
            try
            {
                _frequencyCounter = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
                _frequencyCounter.NextValue();
            }
            catch { }
            
            _initialized = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing CPU monitor: {ex.Message}");
        }
    }
    
    private void LoadStaticInfo()
    {
        try
        {
            using var cpuSearcher = new ManagementObjectSearcher("SELECT Name, MaxClockSpeed, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
            foreach (ManagementObject mo in cpuSearcher.Get())
            {
                var cpuName = mo["Name"];
                if (cpuName != null)
                    CpuName = cpuName.ToString()?.Trim();
                
                var maxClockSpeed = mo["MaxClockSpeed"];
                if (maxClockSpeed != null)
                    BaseFrequency = $"{Convert.ToDouble(maxClockSpeed) / 1000:F2} GHz";
                
                var cores = mo["NumberOfCores"];
                if (cores != null)
                    PhysicalCores = Convert.ToInt32(cores);
                
                var logicalProcessors = mo["NumberOfLogicalProcessors"];
                if (logicalProcessors != null)
                    LogicalCores = Convert.ToInt32(logicalProcessors);
                
                break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading CPU static info: {ex.Message}");
            PhysicalCores = Environment.ProcessorCount / 2;
            LogicalCores = Environment.ProcessorCount;
        }
    }
    
    /// <summary>
    /// Get current CPU metrics
    /// </summary>
    public CpuMetrics GetMetrics()
    {
        var metrics = new CpuMetrics();
        
        // Get total usage
        try
        {
            if (_totalCpuCounter != null)
            {
                float value = _totalCpuCounter.NextValue();
                _lastTotalUsage = Math.Max(0, Math.Min(100, value));
            }
        }
        catch { }
        metrics.TotalUsage = _lastTotalUsage;
        
        // Get per-core usage
        if (_coreCounters != null && _lastCoreUsages != null)
        {
            for (int i = 0; i < _coreCounters.Length; i++)
            {
                try
                {
                    if (_coreCounters[i] != null)
                    {
                        float value = _coreCounters[i].NextValue();
                        _lastCoreUsages[i] = Math.Max(0, Math.Min(100, value));
                    }
                }
                catch { }
            }
        }
        metrics.CoreUsages = _lastCoreUsages;
        
        // Get current frequency
        try
        {
            if (_frequencyCounter != null && !string.IsNullOrEmpty(BaseFrequency))
            {
                float perfPercent = _frequencyCounter.NextValue();
                var freqStr = BaseFrequency.Replace(" GHz", "").Replace(",", ".");
                if (double.TryParse(freqStr, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out double baseFreqGHz))
                {
                    // Current frequency = base * performance ratio
                    _lastFrequency = baseFreqGHz * (perfPercent / 100.0);
                }
            }
        }
        catch { }
        
        // Fallback: get from WMI if performance counter failed
        if (_lastFrequency <= 0)
        {
            try
            {
                using var cpuSearcher = new ManagementObjectSearcher("SELECT CurrentClockSpeed FROM Win32_Processor");
                foreach (ManagementObject mo in cpuSearcher.Get())
                {
                    var currentSpeed = mo["CurrentClockSpeed"];
                    if (currentSpeed != null)
                    {
                        _lastFrequency = Convert.ToDouble(currentSpeed) / 1000.0;
                        break;
                    }
                }
            }
            catch { }
        }
        metrics.FrequencyGHz = _lastFrequency;
        
        return metrics;
    }
    
    public void Dispose()
    {
        _totalCpuCounter?.Dispose();
        _frequencyCounter?.Dispose();
        
        if (_coreCounters != null)
        {
            foreach (var counter in _coreCounters)
            {
                counter?.Dispose();
            }
        }
    }
}
