namespace V_Task.Models;

/// <summary>
/// Real-time GPU metrics (for a single adapter)
/// </summary>
public class GpuMetrics
{
    public double Usage { get; set; }
    public double MemoryUsedGB { get; set; }
    public double MemoryUsagePercent { get; set; }

}

/// <summary>
/// One GPU adapter (integrated or discrete) with static + live metrics.
/// </summary>
public class GpuAdapterInfo
{
    public string Id { get; set; } = string.Empty; // e.g. LUID or a synthetic id
    public string Name { get; set; } = "GPU";
    public bool IsIntegrated { get; set; }
    public bool IsMock { get; set; }
    public bool IsActive { get; set; } = true; // Real GPUs are active, mock GPUs are not

    public string? DriverVersion { get; set; }

    // VRAM (Dedicated) usage in GB
    public double TotalMemoryGB { get; set; }
    public double UsedMemoryGB { get; set; }
    public double UsagePercent { get; set; }
    
    // For localized status/card type, use converters in XAML bindings
    // StatusText and CardTypeText are now provided via converters
}

/// <summary>
/// Real-time CPU metrics
/// </summary>
public class CpuMetrics
{
    public double TotalUsage { get; set; }
    public double FrequencyGHz { get; set; }
    public float[]? CoreUsages { get; set; }
}

/// <summary>
/// Real-time memory metrics
/// </summary>
public class MemoryMetrics
{
    public double TotalGB { get; set; }
    public double UsedGB { get; set; }
    public double AvailableGB { get; set; }
    public double UsagePercent { get; set; }
    
    // Virtual memory
    public double VirtualTotalGB { get; set; }
    public double VirtualUsedGB { get; set; }
    public double VirtualAvailableGB { get; set; }
    public double VirtualUsagePercent { get; set; }
}

/// <summary>
/// Network metrics
/// </summary>
public class NetworkMetrics
{
    public double DownloadSpeed { get; set; }
    public double UploadSpeed { get; set; }
    public long TotalBytesReceived { get; set; }
    public long TotalBytesSent { get; set; }
    
    public string? WifiName { get; set; }
    public double WifiDownSpeed { get; set; }
    public double WifiUpSpeed { get; set; }
    public bool WifiConnected { get; set; }
    
    public string? EthernetName { get; set; }
    public double EthernetDownSpeed { get; set; }
    public double EthernetUpSpeed { get; set; }
    public bool EthernetConnected { get; set; }
}

/// <summary>
/// Disk metrics
/// </summary>
public class DiskMetrics
{
    public string? DriveName { get; set; }
    public string? DriveFormat { get; set; }
    public double TotalGB { get; set; }
    public double UsedGB { get; set; }
    public double UsagePercent { get; set; }
}
