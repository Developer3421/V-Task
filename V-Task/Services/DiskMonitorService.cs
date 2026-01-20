using System;
using System.IO;
using V_Task.Models;

namespace V_Task.Services;

/// <summary>
/// Disk monitoring service
/// </summary>
public class DiskMonitorService
{
    /// <summary>
    /// Get metrics for a specific drive
    /// </summary>
    public DiskMetrics GetMetrics(string driveLetter = "C")
    {
        var metrics = new DiskMetrics();
        
        try
        {
            var driveInfo = new DriveInfo(driveLetter);
            
            if (driveInfo.IsReady)
            {
                metrics.DriveName = $"{driveLetter}:\\";
                metrics.DriveFormat = driveInfo.DriveFormat;
                metrics.TotalGB = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);
                metrics.UsedGB = (driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / (1024.0 * 1024.0 * 1024.0);
                metrics.UsagePercent = (metrics.UsedGB / metrics.TotalGB) * 100;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting disk metrics: {ex.Message}");
        }
        
        return metrics;
    }
}
