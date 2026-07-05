using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Globalization;
namespace EHMR.Converters;
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var app = Microsoft.Maui.Controls.Application.Current;
        string status = value?.ToString() ?? "";
        string key = status switch {
            "Active"    or "Completed" => "StatusActive",
            "Inactive"  or "Cancelled" or "Missed" => "StatusInactive",
            "Pending"   => "StatusPending",
            "Scheduled" => "StatusScheduled",
            _             => "TextMuted"
        };
        if (app?.Resources.TryGetValue(key, out var color) == true && color is Color c) return c;
        return Colors.Gray;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}