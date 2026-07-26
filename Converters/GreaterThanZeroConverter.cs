// File: Converters/CalendarConverters.cs
using System.Globalization;
using Microsoft.Maui.Controls;

namespace EHMR.Converters;

/// <summary>
/// Returns true when an integer value is greater than zero.
/// Used to show/hide event-count badges on calendar day cells.
/// </summary>
public class GreaterThanZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is int intVal) return intVal>0;
        if(value is double dblVal) return dblVal>0;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts a 0.0–1.0 adherence double into a pixel width for the
/// progress bar inside the dark adherence card on the right panel.
/// The bar container is 220 px wide (adjust MaxWidth to match your layout).
/// </summary>
public class ProgressToWidthConverter : IValueConverter
{
    public double MaxWidth { get; set; } = 220;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is double ratio)
            return Math.Clamp(ratio, 0, 1)*MaxWidth;
        return 0d;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Inverts a boolean — used to hide the day timeline header "close" zone
/// when the day view is not active without adding extra VM properties.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b&&!b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b&&!b;
}