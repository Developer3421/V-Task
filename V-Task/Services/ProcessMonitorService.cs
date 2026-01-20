using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using V_Task.Models;

namespace V_Task.Services;

/// <summary>
/// Process monitoring service
/// </summary>
public class ProcessMonitorService
{
    /// <summary>
    /// Get list of running processes sorted by memory usage
    /// </summary>
    public async Task<List<ProcessInfo>> GetProcessesAsync(int maxCount = 50)
    {
        return await Task.Run(() =>
        {
            var result = new List<ProcessInfo>();
            
            try
            {
                var processes = Process.GetProcesses();
                var sorted = processes
                    .Select(p =>
                    {
                        try
                        {
                            return new
                            {
                                Process = p,
                                Memory = p.WorkingSet64
                            };
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(x => x != null)
                    .OrderByDescending(x => x!.Memory)
                    .Take(maxCount)
                    .ToList();

                foreach (var item in sorted)
                {
                    if (item == null) continue;
                    try
                    {
                        result.Add(new ProcessInfo
                        {
                            Id = item.Process.Id,
                            ProcessName = item.Process.ProcessName,
                            CpuUsage = "—",
                            MemoryMB = $"{item.Memory / (1024.0 * 1024.0):F1}",
                            Threads = item.Process.Threads.Count.ToString(),
                            Status = item.Process.Responding ? "Running" : "Not Responding"
                        });
                    }
                    catch { }
                }

                // Dispose all processes
                foreach (var p in processes)
                {
                    try { p.Dispose(); } catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting processes: {ex.Message}");
            }
            
            return result;
        });
    }
    
    /// <summary>
    /// Kill a process by ID
    /// </summary>
    public bool KillProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            process.Kill();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error killing process: {ex.Message}");
            return false;
        }
    }
}
