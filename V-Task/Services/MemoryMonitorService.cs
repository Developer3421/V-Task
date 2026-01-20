using System;
using System.Runtime.InteropServices;
using V_Task.Models;

namespace V_Task.Services;

/// <summary>
/// Memory monitoring service using native Windows API for speed and accuracy
/// </summary>
public class MemoryMonitorService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
    
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
    
    // Cached static info
    public string? MemorySpeed { get; private set; }
    public string? MemoryType { get; private set; }
    public string? MemorySlots { get; private set; }
    
    public void Initialize()
    {
        LoadStaticInfo();
    }
    
    private void LoadStaticInfo()
    {
        try
        {
            using var memorySearcher = new System.Management.ManagementObjectSearcher("SELECT ConfiguredClockSpeed, Speed, SMBIOSMemoryType FROM Win32_PhysicalMemory");
            
            int usedSlots = 0;
            uint maxSpeed = 0;
            string memType = "DDR4";
            
            foreach (System.Management.ManagementObject mo in memorySearcher.Get())
            {
                usedSlots++;
                var speed = mo["ConfiguredClockSpeed"] ?? mo["Speed"];
                if (speed != null && Convert.ToUInt32(speed) > maxSpeed)
                    maxSpeed = Convert.ToUInt32(speed);
                
                var memoryType = mo["SMBIOSMemoryType"];
                if (memoryType != null)
                {
                    memType = Convert.ToInt32(memoryType) switch
                    {
                        20 => "DDR",
                        21 => "DDR2",
                        24 => "DDR3",
                        26 => "DDR4",
                        34 => "DDR5",
                        _ => "DDR4"
                    };
                }
            }
            
            // Get total memory slots
            int totalSlots = 0;
            using var slotSearcher = new System.Management.ManagementObjectSearcher("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray");
            foreach (System.Management.ManagementObject mo in slotSearcher.Get())
            {
                var slots = mo["MemoryDevices"];
                if (slots != null)
                    totalSlots = Convert.ToInt32(slots);
            }
            
            MemorySpeed = maxSpeed > 0 ? $"{maxSpeed} MHz" : "N/A";
            MemoryType = memType;
            MemorySlots = $"{usedSlots} / {totalSlots}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading memory static info: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get current memory metrics using native API (very fast)
    /// </summary>
    public MemoryMetrics GetMetrics()
    {
        var metrics = new MemoryMetrics();
        
        var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref memStatus))
        {
            // Physical memory
            metrics.TotalGB = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
            metrics.AvailableGB = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
            metrics.UsedGB = metrics.TotalGB - metrics.AvailableGB;
            metrics.UsagePercent = memStatus.dwMemoryLoad;
            
            // Virtual memory (Page File)
            metrics.VirtualTotalGB = memStatus.ullTotalPageFile / (1024.0 * 1024.0 * 1024.0);
            metrics.VirtualAvailableGB = memStatus.ullAvailPageFile / (1024.0 * 1024.0 * 1024.0);
            metrics.VirtualUsedGB = metrics.VirtualTotalGB - metrics.VirtualAvailableGB;
            metrics.VirtualUsagePercent = metrics.VirtualTotalGB > 0 
                ? (metrics.VirtualUsedGB / metrics.VirtualTotalGB) * 100 
                : 0;
        }
        
        return metrics;
    }
}
