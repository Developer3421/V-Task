using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using V_Task.Services;

namespace V_Task;

public partial class MainWindow : Window
{
    // Services
    // Dynamic CPU/GPU services removed (CPU/GPU pages are static-only now)
    private readonly MemoryMonitorService _memoryService = new();
    private readonly DiskMonitorService _diskService = new();
    private readonly NetworkMonitorService _networkService = new();
    private readonly HardwareMonitorService _hardwareService = new();
    private readonly GpuMonitorService _gpuService = new();
    private readonly LocalizationService _localization = LocalizationService.Instance;

    // UI Data
    private DispatcherTimer? _updateTimer;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this; // Set DataContext for bindings
        InitializeMonitoring();
        SetupEventHandlers();
        UpdateLocalizedStrings();
    }

    private void InitializeMonitoring()
    {
        try
        {
            // Initialize services in background
            Task.Run(() =>
            {
                _memoryService.Initialize();
                _hardwareService.Initialize();
                _gpuService.Initialize();
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
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        // Keep a lightweight update loop
        _hardwareService.Update();

        // GPU counters are cheap to read; keep it updated
        // (Static info is loaded once inside Initialize)
        // _gpuService.GetMetrics() is called in UpdateGPUInfo()

        // Update UI on main thread
        UpdateAllData();
    }

    private void UpdateAllData()
    {
        try
        {
            UpdateDashboard();
            UpdateMemoryInfo();
            UpdateGPUInfo();
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
        // Static-only CPU info (no dynamic usage/frequency)
        if (DashCpuName != null)
        {
            string name = _hardwareService.CpuName ?? "Unknown CPU";
            DashCpuName.Text = name;
        }

        if (DashCpuCores != null)
        {
            int physCores = _hardwareService.PhysicalCores;
            int logCores = _hardwareService.LogicalCores;

            if (physCores <= 0) physCores = logCores;
            if (logCores <= 0) logCores = Environment.ProcessorCount;

            DashCpuCores.Text = $"{physCores} / {logCores}";
        }
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
                DashNetWifiName.Text = _localization["WiFiNotConnected"];
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
                DashNetEthName.Text = _localization["EthNotConnected"];
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
            DashNetName.Text = activeNames.Count > 0 ? string.Join(" + ", activeNames) : _localization["NoConnection"];
    }

    private void UpdateSystemInfo()
    {
        if (DashSystemUptime != null)
        {
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            DashSystemUptime.Text = $"Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }
        
        if (DashSystemOS != null)
            DashSystemOS.Text = GetWindowsVersionString();
    }
    
    private static string GetWindowsVersionString()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT Caption, BuildNumber FROM Win32_OperatingSystem");
            
            foreach (var obj in searcher.Get())
            {
                string caption = obj["Caption"]?.ToString() ?? "";
                string buildNumber = obj["BuildNumber"]?.ToString() ?? "";
                
                // Detect Windows 11 (build >= 22000)
                string osName;
                if (int.TryParse(buildNumber, out int build))
                {
                    if (build >= 22000)
                        osName = "Windows 11";
                    else if (build >= 10240)
                        osName = "Windows 10";
                    else
                        osName = caption;
                }
                else
                {
                    osName = caption;
                }
                
                // Get display version from registry (e.g., 24H2, 25H2)
                string displayVersion = GetWindowsDisplayVersion();
                
                if (!string.IsNullOrEmpty(displayVersion))
                    return $"{osName} {displayVersion} (Build {buildNumber})";
                else
                    return $"{osName} (Build {buildNumber})";
            }
        }
        catch
        {
            // Fallback
        }
        
        return Environment.OSVersion.ToString();
    }
    
    private static string GetWindowsDisplayVersion()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            
            if (key != null)
            {
                // Try DisplayVersion first (Windows 10 20H2+ and Windows 11)
                var displayVersion = key.GetValue("DisplayVersion") as string;
                if (!string.IsNullOrEmpty(displayVersion))
                    return displayVersion;
                
                // Fallback to ReleaseId for older versions
                var releaseId = key.GetValue("ReleaseId") as string;
                if (!string.IsNullOrEmpty(releaseId))
                    return releaseId;
            }
        }
        catch
        {
            // Ignore registry errors
        }
        
        return string.Empty;
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
            MemSpeed.Text = $"{_localization["Frequency"]} {_memoryService.MemorySpeed ?? _localization["NA"]}";
        if (MemType != null)
            MemType.Text = $"{_localization["Type"]} {_memoryService.MemoryType ?? _localization["NA"]}";
        if (MemSlots != null)
            MemSlots.Text = $"{_localization["Slots"]} {_memoryService.MemorySlots ?? _localization["NA"]}";
    }

    #endregion

    #region GPU Panel

    private void UpdateGPUInfo()
    {
        // Multi-adapter support via GpuMonitorService
        var adapters = _gpuService.GetAdapters();

        // Bind cards
        if (GpuCards != null)
            GpuCards.ItemsSource = adapters;

        // Pick a primary adapter for header/details: prefer discrete real GPU, then anything.
        var primary = adapters.FirstOrDefault(a => !a.IsIntegrated && !a.IsMock) ?? adapters.FirstOrDefault();

        // Driver info
        if (GpuDriver != null)
        {
            var drv = primary?.DriverVersion;
            if (string.IsNullOrWhiteSpace(drv))
                drv = string.IsNullOrWhiteSpace(_gpuService.DriverVersion) ? _localization["NA"] : _gpuService.DriverVersion;

            GpuDriver.Text = $"{_localization["Driver"]} {drv}";
        }

        if (GpuBusInterface != null)
            GpuBusInterface.Text = $"{_localization["Interface"]} PCIe";
    }

    #endregion


    #region Event Handlers

    private void TabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        // Update button states
        if (TabDashboard != null) TabDashboard.Classes.Remove("active");
        if (TabMemory != null) TabMemory.Classes.Remove("active");
        if (TabGPU != null) TabGPU.Classes.Remove("active");
        
        button.Classes.Add("active");

        // Show corresponding panel
        if (PanelDashboard != null) PanelDashboard.IsVisible = false;
        if (PanelMemory != null) PanelMemory.IsVisible = false;
        if (PanelGPU != null) PanelGPU.IsVisible = false;

        if (button.Name == "TabDashboard" && PanelDashboard != null)
            PanelDashboard.IsVisible = true;
        else if (button.Name == "TabMemory" && PanelMemory != null)
            PanelMemory.IsVisible = true;
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
        _hardwareService.Dispose();
        _gpuService.Dispose();
        Close();
    }

    private void RefreshDashboard_Click(object sender, RoutedEventArgs e)
    {
        UpdateAllData();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.SetLanguageChangedCallback(UpdateLocalizedStrings);
        settingsWindow.ShowDialog(this);
    }

    private void AgreementButton_Click(object sender, RoutedEventArgs e)
    {
        var agreementWindow = new UserAgreementWindow();
        agreementWindow.ShowDialog(this);
    }

    #endregion

    #region Localization

    private void UpdateLocalizedStrings()
    {
        // Window title
        if (WindowTitle != null)
            WindowTitle.Text = _localization["AppTitle"];

        Title = _localization["AppTitle"];

        // Tab buttons
        if (TabDashboard != null)
            TabDashboard.Content = _localization["TabDashboard"];
        if (TabMemory != null)
            TabMemory.Content = _localization["TabMemory"];
        if (TabGPU != null)
            TabGPU.Content = _localization["TabGPU"];

        // Dashboard - CPU Card
        if (DashCpuLabel != null)
            DashCpuLabel.Text = _localization["CPU"];
        if (DashCpuCoresLabel != null)
            DashCpuCoresLabel.Text = _localization["CoresPhysLog"];

        // Dashboard - RAM Card
        if (DashRamLabel != null)
            DashRamLabel.Text = _localization["Memory"];
        if (DashRamUsedLabel != null)
            DashRamUsedLabel.Text = _localization["Used"];
        if (DashRamAvailLabel != null)
            DashRamAvailLabel.Text = _localization["Available"];

        // Dashboard - Disk Card
        if (DashDiskLabel != null)
            DashDiskLabel.Text = _localization["Disk"];
        if (DashDiskUsedLabel != null)
            DashDiskUsedLabel.Text = _localization["Used"];
        if (DashDiskTotalLabel != null)
            DashDiskTotalLabel.Text = _localization["Total"];

        // Dashboard - Network Card
        if (DashNetLabel != null)
            DashNetLabel.Text = _localization["Network"];
        if (DashNetDownLabel != null)
            DashNetDownLabel.Text = _localization["Download"];
        if (DashNetUpLabel != null)
            DashNetUpLabel.Text = _localization["Upload"];
        if (DashNetReceivedLabel != null)
            DashNetReceivedLabel.Text = _localization["Received"];
        if (DashNetSentLabel != null)
            DashNetSentLabel.Text = _localization["Sent"];

        // Dashboard - System Card
        if (DashSystemLabel != null)
            DashSystemLabel.Text = _localization["System"];
        if (DashUpdateTimeLabel != null)
            DashUpdateTimeLabel.Text = _localization["UpdateTime"];
        if (DashUpdateTime != null)
            DashUpdateTime.Text = $"1 {_localization["Second"]}";
        if (BtnRefreshDash != null)
            BtnRefreshDash.Content = _localization["Refresh"];

        // Memory Panel
        if (MemDetailsLabel != null)
            MemDetailsLabel.Text = _localization["MemoryDetails"];
        if (MemRamLabel != null)
            MemRamLabel.Text = _localization["RAM"];
        if (MemTotalLabel != null)
            MemTotalLabel.Text = _localization["Total"];
        if (MemUsedLabel != null)
            MemUsedLabel.Text = _localization["Used"];
        if (MemAvailLabel != null)
            MemAvailLabel.Text = _localization["Available"];
        if (MemUsageLabel != null)
            MemUsageLabel.Text = _localization["Usage"];

        // Memory Panel - Swap
        if (MemSwapLabel != null)
            MemSwapLabel.Text = _localization["SwapFile"];
        if (MemSwapTotalLabel != null)
            MemSwapTotalLabel.Text = _localization["Total"];
        if (MemSwapUsedLabel != null)
            MemSwapUsedLabel.Text = _localization["Used"];
        if (MemSwapAvailLabel != null)
            MemSwapAvailLabel.Text = _localization["Available"];
        if (MemSwapUsageLabel != null)
            MemSwapUsageLabel.Text = _localization["Usage"];

        // Memory Panel - Additional Info
        if (MemAdditionalInfoLabel != null)
            MemAdditionalInfoLabel.Text = _localization["AdditionalInfo"];

        // GPU Panel
        if (GpuDetailsLabel != null)
            GpuDetailsLabel.Text = _localization["GPUDetails"];
        if (GpuTechDataLabel != null)
            GpuTechDataLabel.Text = _localization["TechData"];
        if (GpuBusInterface != null)
            GpuBusInterface.Text = $"{_localization["Interface"]} PCIe";
        
        // Refresh GPU cards to update localized converters
        if (GpuCards != null)
        {
            var adapters = _gpuService.GetAdapters();
            GpuCards.ItemsSource = null;
            GpuCards.ItemsSource = adapters;
        }
        
        // Also update other dynamic values in Memory panel
        UpdateMemoryInfo();
    }

    #endregion
}
