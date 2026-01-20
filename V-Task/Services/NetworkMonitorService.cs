using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using V_Task.Models;

namespace V_Task.Services;

/// <summary>
/// Network monitoring service
/// </summary>
public class NetworkMonitorService
{
    private long _prevBytesReceivedWifi;
    private long _prevBytesSentWifi;
    private long _prevBytesReceivedEthernet;
    private long _prevBytesSentEthernet;
    private DateTime _lastUpdate = DateTime.MinValue;
    
    /// <summary>
    /// Get current network metrics
    /// </summary>
    public NetworkMetrics GetMetrics()
    {
        var metrics = new NetworkMetrics();
        
        try
        {
            var allInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up 
                    && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();
            
            var wifiInterface = allInterfaces.FirstOrDefault(n => 
                n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
            var ethernetInterface = allInterfaces.FirstOrDefault(n => 
                n.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                n.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet);
            
            var now = DateTime.Now;
            var elapsed = (now - _lastUpdate).TotalSeconds;
            bool canCalculateSpeed = _lastUpdate != DateTime.MinValue && elapsed > 0.1;
            
            // Process WiFi
            if (wifiInterface != null)
            {
                var stats = wifiInterface.GetIPStatistics();
                long bytesReceived = stats.BytesReceived;
                long bytesSent = stats.BytesSent;
                
                metrics.WifiConnected = true;
                metrics.WifiName = wifiInterface.Name;
                metrics.TotalBytesReceived += bytesReceived;
                metrics.TotalBytesSent += bytesSent;
                
                if (canCalculateSpeed && _prevBytesReceivedWifi > 0)
                {
                    metrics.WifiDownSpeed = Math.Max(0, (bytesReceived - _prevBytesReceivedWifi) / elapsed);
                    metrics.WifiUpSpeed = Math.Max(0, (bytesSent - _prevBytesSentWifi) / elapsed);
                    metrics.DownloadSpeed += metrics.WifiDownSpeed;
                    metrics.UploadSpeed += metrics.WifiUpSpeed;
                }
                
                _prevBytesReceivedWifi = bytesReceived;
                _prevBytesSentWifi = bytesSent;
            }
            else
            {
                metrics.WifiConnected = false;
                _prevBytesReceivedWifi = 0;
                _prevBytesSentWifi = 0;
            }
            
            // Process Ethernet
            if (ethernetInterface != null)
            {
                var stats = ethernetInterface.GetIPStatistics();
                long bytesReceived = stats.BytesReceived;
                long bytesSent = stats.BytesSent;
                
                metrics.EthernetConnected = true;
                metrics.EthernetName = ethernetInterface.Name;
                metrics.TotalBytesReceived += bytesReceived;
                metrics.TotalBytesSent += bytesSent;
                
                if (canCalculateSpeed && _prevBytesReceivedEthernet > 0)
                {
                    metrics.EthernetDownSpeed = Math.Max(0, (bytesReceived - _prevBytesReceivedEthernet) / elapsed);
                    metrics.EthernetUpSpeed = Math.Max(0, (bytesSent - _prevBytesSentEthernet) / elapsed);
                    metrics.DownloadSpeed += metrics.EthernetDownSpeed;
                    metrics.UploadSpeed += metrics.EthernetUpSpeed;
                }
                
                _prevBytesReceivedEthernet = bytesReceived;
                _prevBytesSentEthernet = bytesSent;
            }
            else
            {
                metrics.EthernetConnected = false;
                _prevBytesReceivedEthernet = 0;
                _prevBytesSentEthernet = 0;
            }
            
            _lastUpdate = now;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting network metrics: {ex.Message}");
        }
        
        return metrics;
    }
    
    /// <summary>
    /// Format bytes per second to human readable string
    /// </summary>
    public static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond < 0) bytesPerSecond = 0;
        
        if (bytesPerSecond >= 1024 * 1024 * 1024)
            return $"{bytesPerSecond / (1024 * 1024 * 1024):F1} GB/s";
        if (bytesPerSecond >= 1024 * 1024)
            return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
        if (bytesPerSecond >= 1024)
            return $"{bytesPerSecond / 1024:F1} KB/s";
        return $"{bytesPerSecond:F0} B/s";
    }
    
    /// <summary>
    /// Format bytes to human readable string
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F1} {sizes[order]}";
    }
}
