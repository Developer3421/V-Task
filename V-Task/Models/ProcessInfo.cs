
namespace V_Task.Models;

public class ProcessInfo
{
    public int Id { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string CpuUsage { get; set; } = string.Empty;
    public string MemoryMB { get; set; } = string.Empty;
    public string Threads { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    
    // Additional info (available without admin rights)
    public string UserName { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string MainWindowTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long WorkingSetBytes { get; set; }
    public long PrivateMemoryBytes { get; set; }
    public int HandleCount { get; set; }
    public int BasePriority { get; set; }
    public bool HasWindow { get; set; }
}
