namespace V_Task.Models;

public class ProcessInfo
{
    public int Id { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string CpuUsage { get; set; } = string.Empty;
    public string MemoryMB { get; set; } = string.Empty;
    public string Threads { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
