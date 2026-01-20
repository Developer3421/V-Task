using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using V_Task.Models;
using V_Task.Services;

namespace V_Task;

/// <summary>
/// Converts a localization key to localized string
/// </summary>
public class LocalizedStringConverter : IValueConverter
{
    public static readonly LocalizedStringConverter Instance = new();
    private static LocalizationService Localization => LocalizationService.Instance;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string key)
        {
            return Localization[key];
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts IsIntegrated boolean to GPU type text (localized)
/// </summary>
public class GpuTypeConverter : IValueConverter
{
    public static readonly GpuTypeConverter Instance = new();
    private static LocalizationService Localization => LocalizationService.Instance;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isIntegrated)
        {
            return isIntegrated ? Localization["GpuIntegrated"] : Localization["GpuDiscrete"];
        }
        return Localization["GpuOther"];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts GpuAdapterInfo to GPU type text (handles mock GPUs properly, localized)
/// </summary>
public class GpuCardTypeConverter : IValueConverter
{
    public static readonly GpuCardTypeConverter Instance = new();
    private static LocalizationService Localization => LocalizationService.Instance;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GpuAdapterInfo gpu)
        {
            if (gpu.IsMock)
                return Localization["GpuOther"];
            return gpu.IsIntegrated ? Localization["GpuIntegrated"] : Localization["GpuDiscrete"];
        }
        
        // Fallback for bool binding
        if (value is bool isIntegrated)
        {
            return isIntegrated ? Localization["GpuIntegrated"] : Localization["GpuDiscrete"];
        }
        
        return Localization["GpuOther"];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts IsActive boolean to localized status text
/// </summary>
public class GpuStatusTextConverter : IValueConverter
{
    public static readonly GpuStatusTextConverter Instance = new();
    private static LocalizationService Localization => LocalizationService.Instance;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? Localization["GpuActive"] : Localization["GpuInactive"];
        }
        return Localization["GpuInactive"];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts IsActive boolean to status color brush
/// </summary>
public class StatusColorConverter : IValueConverter
{
    public static readonly StatusColorConverter Instance = new();

    // Orange for active, gray for inactive
    private static readonly SolidColorBrush ActiveBrush = new(Color.Parse("#FF9500"));
    private static readonly SolidColorBrush InactiveBrush = new(Color.Parse("#8E8E93"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? ActiveBrush : InactiveBrush;
        }
        return InactiveBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
