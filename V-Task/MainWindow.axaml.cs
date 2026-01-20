using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using V_Task.Models;
using V_Task.Services;

namespace V_Task;

public partial class MainWindow : Window
{
    // Services
    private readonly CpuMonitorService _cpuService = new();
    private readonly GpuMonitorService _gpuService = new();
    private readonly MemoryMonitorService _memoryService = new();
    private readonly DiskMonitorService _diskService = new();
    private readonly NetworkMonitorService _networkService = new();
    private readonly ProcessMonitorService _processService = new();
    
    // UI Data
    private DispatcherTimer? _updateTimer;
    private readonly ObservableCollection<ProcessInfo> _processes = new();
    private readonly ObservableCollection<CpuCoreInfo> _cpuCores = new();
    
    // Cached metrics
    private GpuMetrics _lastGpuMetrics = new();

    public MainWindow()
    {
        InitializeComponent();
        InitializeMonitoring();
        SetupEventHandlers();
    }

    private void InitializeMonitoring()
    {
        try
        {
            // Initialize all services in background
            Task.Run(() =>
            {
                _cpuService.Initialize();
                _gpuService.Initialize();
                _memoryService.Initialize();
            }).Wait(3000);

            // Setup timer for updates (1 second interval)
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            // Initial update
            Dispatcher.UIThread.Post(() => UpdateAllData(), DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing monitoring: {ex.Message}");
        }
    }

    private void SetupEventHandlers()
    {
        if (ProcessDataGrid != null)
            ProcessDataGrid.ItemsSource = _processes;
        
        if (CpuCoresList != null)
            CpuCoresList.ItemsSource = _cpuCores;
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        // Update GPU data in background
        _ = UpdateGpuDataAsync();
        
        // Update UI on main thread
        UpdateAllData();
    }

    private void UpdateAllData()
    {
        try
        {
            UpdateDashboard();
            UpdateCPUInfo();
            UpdateMemoryInfo();
            UpdateGPUInfo();
            
            if (PanelProcesses != null && PanelProcesses.IsVisible)
                UpdateProcessList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating data: {ex.Message}");
        }
    }

    #region Dashboard Updates

    private void UpdateDashboard()
    {
        try
        {
            UpdateCpuDashboard();
            UpdateRamDashboard();
            UpdateDiskDashboard();
            UpdateNetworkDashboard();
            UpdateSystemInfo();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating dashboard: {ex.Message}");
        }
    }

    private void UpdateCpuDashboard()
    {
        var metrics = _cpuService.GetMetrics();
        
        if (DashCpuName != null && !string.IsNullOrEmpty(_cpuService.CpuName))
            DashCpuName.Text = _cpuService.CpuName;
        
        if (DashCpuUsage != null)
            DashCpuUsage.Text = $"{metrics.TotalUsage:F1}%";
        if (DashCpuProgress != null)
            DashCpuProgress.Value = metrics.TotalUsage;
        if (DashCpuFreq != null)
            DashCpuFreq.Text = _cpuService.BaseFrequency ?? "N/A";
        if (DashCpuCores != null)
            DashCpuCores.Text = $"{_cpuService.PhysicalCores} / {_cpuService.LogicalCores}";
    }

    private void UpdateRamDashboard()
    {
        var metrics = _memoryService.GetMetrics();
        
        if (DashRamTotal != null)
            DashRamTotal.Text = $"{metrics.TotalGB:F1} GB";
        if (DashRamUsed != null)
            DashRamUsed.Text = $"{metrics.UsedGB:F1} GB";
        if (DashRamAvail != null)
            DashRamAvail.Text = $"{metrics.AvailableGB:F1} GB";
        if (DashRamUsage != null)
            DashRamUsage.Text = $"{metrics.UsagePercent:F1}%";
        if (DashRamProgress != null)
            DashRamProgress.Value = metrics.UsagePercent;
    }

    private void UpdateDiskDashboard()
    {
        var metrics = _diskService.GetMetrics("C");
        
        if (DashDiskName != null)
            DashDiskName.Text = $"{metrics.DriveName} ({metrics.DriveFormat})";
        if (DashDiskUsage != null)
            DashDiskUsage.Text = $"{metrics.UsagePercent:F1}%";
        if (DashDiskProgress != null)
            DashDiskProgress.Value = metrics.UsagePercent;
        if (DashDiskUsed != null)
            DashDiskUsed.Text = $"{metrics.UsedGB:F1} GB";
        if (DashDiskTotal != null)
            DashDiskTotal.Text = $"{metrics.TotalGB:F1} GB";
    }

    private void UpdateNetworkDashboard()
    {
        var metrics = _networkService.GetMetrics();
        
        // WiFi
        if (metrics.WifiConnected)
        {
            if (DashNetWifiName != null)
                DashNetWifiName.Text = $"📶 {metrics.WifiName}";
            if (DashNetWifiDown != null)
                DashNetWifiDown.Text = NetworkMonitorService.FormatSpeed(metrics.WifiDownSpeed);
            if (DashNetWifiUp != null)
                DashNetWifiUp.Text = NetworkMonitorService.FormatSpeed(metrics.WifiUpSpeed);
        }
        else
        {
            if (DashNetWifiName != null)
                DashNetWifiName.Text = "📶 Wi-Fi: Не підключено";
            if (DashNetWifiDown != null)
                DashNetWifiDown.Text = "—";
            if (DashNetWifiUp != null)
                DashNetWifiUp.Text = "—";
        }
        
        // Ethernet
        if (metrics.EthernetConnected)
        {
            if (DashNetEthName != null)
                DashNetEthName.Text = $"🔌 {metrics.EthernetName}";
            if (DashNetEthDown != null)
                DashNetEthDown.Text = NetworkMonitorService.FormatSpeed(metrics.EthernetDownSpeed);
            if (DashNetEthUp != null)
                DashNetEthUp.Text = NetworkMonitorService.FormatSpeed(metrics.EthernetUpSpeed);
        }
        else
        {
            if (DashNetEthName != null)
                DashNetEthName.Text = "🔌 Ethernet: Не підключено";
            if (DashNetEthDown != null)
                DashNetEthDown.Text = "—";
            if (DashNetEthUp != null)
                DashNetEthUp.Text = "—";
        }
        
        // Totals
        if (DashNetDown != null)
            DashNetDown.Text = NetworkMonitorService.FormatSpeed(metrics.DownloadSpeed);
        if (DashNetUp != null)
            DashNetUp.Text = NetworkMonitorService.FormatSpeed(metrics.UploadSpeed);
        if (DashNetTotalDown != null)
            DashNetTotalDown.Text = NetworkMonitorService.FormatBytes(metrics.TotalBytesReceived);
        if (DashNetTotalUp != null)
            DashNetTotalUp.Text = NetworkMonitorService.FormatBytes(metrics.TotalBytesSent);
        
        // Network name
        var activeNames = new System.Collections.Generic.List<string>();
        if (metrics.WifiConnected) activeNames.Add("Wi-Fi");
        if (metrics.EthernetConnected) activeNames.Add("Ethernet");
        if (DashNetName != null)
            DashNetName.Text = activeNames.Count > 0 ? string.Join(" + ", activeNames) : "Немає підключення";
    }

    private void UpdateSystemInfo()
    {
        if (DashSystemUptime != null)
        {
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            DashSystemUptime.Text = $"Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }
        
        if (DashSystemOS != null)
            DashSystemOS.Text = Environment.OSVersion.ToString();
    }

    #endregion

    #region CPU Panel

    private void UpdateCPUInfo()
    {
        var metrics = _cpuService.GetMetrics();
        
        if (CpuName != null)
            CpuName.Text = _cpuService.CpuName ?? "Unknown CPU";
        
        if (CpuTotalUsage != null)
            CpuTotalUsage.Text = $"{metrics.TotalUsage:F1}%";
        
        if (CpuFrequency != null)
        {
            if (metrics.FrequencyGHz > 0)
                CpuFrequency.Text = $"{metrics.FrequencyGHz:F2} GHz";
            else
                CpuFrequency.Text = _cpuService.BaseFrequency ?? "N/A";
        }
        
        if (CpuCoresThreads != null)
        {
            if (_cpuService.PhysicalCores > 0 && _cpuService.LogicalCores > 0)
                CpuCoresThreads.Text = $"{_cpuService.PhysicalCores} / {_cpuService.LogicalCores}";
            else
                CpuCoresThreads.Text = $"{Environment.ProcessorCount / 2} / {Environment.ProcessorCount}";
        }
        
        // Update per-core usage
        int coreCount = _cpuService.LogicalCores > 0 ? _cpuService.LogicalCores : Environment.ProcessorCount;
        
        if (_cpuCores.Count != coreCount)
        {
            _cpuCores.Clear();
            for (int i = 0; i < coreCount; i++)
            {
                float coreUsage = (metrics.CoreUsages != null && i < metrics.CoreUsages.Length) 
                    ? metrics.CoreUsages[i] : (float)metrics.TotalUsage;
                _cpuCores.Add(new CpuCoreInfo
                {
                    CoreName = $"Ядро #{i + 1}",
                    Usage = coreUsage,
                    UsageText = $"{coreUsage:F1}%"
                });
            }
        }
        else
        {
            for (int i = 0; i < _cpuCores.Count; i++)
            {
                float coreUsage = (metrics.CoreUsages != null && i < metrics.CoreUsages.Length) 
                    ? metrics.CoreUsages[i] : (float)metrics.TotalUsage;
                _cpuCores[i].Usage = coreUsage;
                _cpuCores[i].UsageText = $"{coreUsage:F1}%";
            }
        }
    }

    #endregion

    #region Memory Panel

    private void UpdateMemoryInfo()
    {
        var metrics = _memoryService.GetMetrics();
        
        // Physical memory
        if (MemTotalRam != null)
            MemTotalRam.Text = $"{metrics.TotalGB:F1} GB";
        if (MemUsedRam != null)
            MemUsedRam.Text = $"{metrics.UsedGB:F1} GB";
        if (MemAvailRam != null)
            MemAvailRam.Text = $"{metrics.AvailableGB:F1} GB";
        if (MemUsageRam != null)
            MemUsageRam.Text = $"{metrics.UsagePercent:F1}%";
        if (MemRamProgress != null)
            MemRamProgress.Value = metrics.UsagePercent;
        
        // Virtual memory
        if (MemTotalSwap != null)
            MemTotalSwap.Text = $"{metrics.VirtualTotalGB:F1} GB";
        if (MemUsedSwap != null)
            MemUsedSwap.Text = $"{metrics.VirtualUsedGB:F1} GB";
        if (MemAvailSwap != null)
            MemAvailSwap.Text = $"{metrics.VirtualAvailableGB:F1} GB";
        if (MemUsageSwap != null)
            MemUsageSwap.Text = $"{metrics.VirtualUsagePercent:F1}%";
        if (MemSwapProgress != null)
            MemSwapProgress.Value = metrics.VirtualUsagePercent;
        
        // Static info
        if (MemSpeed != null)
            MemSpeed.Text = $"Частота: {_memoryService.MemorySpeed ?? "N/A"}";
        if (MemType != null)
            MemType.Text = $"Тип: {_memoryService.MemoryType ?? "N/A"}";
        if (MemSlots != null)
            MemSlots.Text = $"Слоти: {_memoryService.MemorySlots ?? "N/A"}";
        
        if (MemCached != null)
            MemCached.Text = "Кешовано: —";
        if (MemCommitted != null)
            MemCommitted.Text = "Виділено: —";
        if (MemPaged != null)
            MemPaged.Text = "Сторінкова: —";
    }

    #endregion

    #region GPU Panel

    private async Task UpdateGpuDataAsync()
    {
        try
        {
            _lastGpuMetrics = await _gpuService.GetMetricsAsync();
        }
        catch { }
    }

    private void UpdateGPUInfo()
    {
        // Static info
        if (GpuName != null)
            GpuName.Text = _gpuService.GpuName ?? "GPU не знайдено";
        
        if (GpuMemoryTotal != null)
            GpuMemoryTotal.Text = _gpuService.TotalMemoryGB > 0 
                ? $"{_gpuService.TotalMemoryGB:F0} GB" 
                : "N/A";
        
        if (GpuDriver != null)
            GpuDriver.Text = $"Драйвер: {_gpuService.DriverVersion ?? "N/A"}";
        if (GpuBusInterface != null)
            GpuBusInterface.Text = "Інтерфейс: PCIe";
        
        // GPU clocks (currently not available via PerformanceCounter)
        if (GpuCoreClock != null)
            GpuCoreClock.Text = _lastGpuMetrics.CoreClockMHz > 0 
                ? $"{_lastGpuMetrics.CoreClockMHz:F0} MHz" 
                : "—";
        if (GpuMemoryClock != null)
            GpuMemoryClock.Text = _lastGpuMetrics.MemoryClockMHz > 0 
                ? $"{_lastGpuMetrics.MemoryClockMHz:F0} MHz" 
                : "—";
        
        // GPU usage
        if (GpuCoreUsage != null)
            GpuCoreUsage.Text = $"{_lastGpuMetrics.Usage:F0}%";
        if (GpuCoreProgress != null)
            GpuCoreProgress.Value = Math.Min(100, _lastGpuMetrics.Usage);
        
        // GPU memory
        if (GpuMemoryUsed != null)
            GpuMemoryUsed.Text = _lastGpuMetrics.MemoryUsedGB > 0.01 
                ? $"{_lastGpuMetrics.MemoryUsedGB:F2} GB" 
                : "0 GB";
        
        if (GpuMemoryUsage != null)
            GpuMemoryUsage.Text = $"{_lastGpuMetrics.MemoryUsagePercent:F0}%";
        if (GpuMemoryProgress != null)
            GpuMemoryProgress.Value = Math.Min(100, _lastGpuMetrics.MemoryUsagePercent);
    }

    #endregion

    #region Processes Panel

    private async void UpdateProcessList()
    {
        try
        {
            var processData = await _processService.GetProcessesAsync();
            
            _processes.Clear();
            foreach (var p in processData)
            {
                _processes.Add(p);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating process list: {ex.Message}");
        }
    }

    #endregion

    #region Event Handlers

    private void TabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        // Update button states
        if (TabDashboard != null) TabDashboard.Classes.Remove("active");
        if (TabCPU != null) TabCPU.Classes.Remove("active");
        if (TabMemory != null) TabMemory.Classes.Remove("active");
        if (TabProcesses != null) TabProcesses.Classes.Remove("active");
        if (TabGPU != null) TabGPU.Classes.Remove("active");
        
        button.Classes.Add("active");

        // Show corresponding panel
        if (PanelDashboard != null) PanelDashboard.IsVisible = false;
        if (PanelCPU != null) PanelCPU.IsVisible = false;
        if (PanelMemory != null) PanelMemory.IsVisible = false;
        if (PanelProcesses != null) PanelProcesses.IsVisible = false;
        if (PanelGPU != null) PanelGPU.IsVisible = false;

        if (button.Name == "TabDashboard" && PanelDashboard != null)
            PanelDashboard.IsVisible = true;
        else if (button.Name == "TabCPU" && PanelCPU != null)
            PanelCPU.IsVisible = true;
        else if (button.Name == "TabMemory" && PanelMemory != null)
            PanelMemory.IsVisible = true;
        else if (button.Name == "TabProcesses" && PanelProcesses != null)
        {
            PanelProcesses.IsVisible = true;
            UpdateProcessList();
        }
        else if (button.Name == "TabGPU" && PanelGPU != null)
            PanelGPU.IsVisible = true;
    }

    private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _updateTimer?.Stop();
        _cpuService.Dispose();
        _gpuService.Dispose();
        Close();
    }

    private void RefreshDashboard_Click(object sender, RoutedEventArgs e)
    {
        UpdateAllData();
    }

    private void RefreshProcesses_Click(object sender, RoutedEventArgs e)
    {
        UpdateProcessList();
    }

    private void KillProcess_Click(object sender, RoutedEventArgs e)
    {
        if (ProcessDataGrid?.SelectedItem is ProcessInfo processInfo)
        {
            if (_processService.KillProcess(processInfo.Id))
            {
                UpdateProcessList();
            }
        }
    }

    #endregion
}
