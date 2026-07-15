using System.Globalization;

namespace EHMR.Converters;

// NOTE: These assume Doctor.Status is a string ("Active" / "Inactive" / "OnLeave" / "Suspended").
// If Status is actually an enum, swap the switch pattern below to match on that enum instead —
// the converters otherwise plug straight into the theme's badge tokens (FFBadgeSuccess, etc.)
// so the pill re-colors automatically when the grid's Variant/theme changes.

public class DoctorStatusToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = BadgeKeyFor(value as string);
        return Application.Current?.Resources[key]??new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static string BadgeKeyFor(string? status) => status?.ToLowerInvariant() switch
    {
        "active" or "активен" => "FFBadgeSuccess",
        "onleave" or "на отсуство" => "FFBadgeWarning",
        "suspended" or "inactive" or "неактивен" => "FFBadgeDanger",
        _ => "FFBadgeInfo"
    };
}

public class DoctorStatusToTextColorConverter : IValueConverter
{
    // Pairs with DoctorStatusToBackgroundConverter — darker tone of the same hue for readable text on the pill.
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as string)?.ToLowerInvariant() switch
        {
            "active" or "активен" => Color.FromArgb("#15803D"),
            "onleave" or "на отсуство" => Color.FromArgb("#B45309"),
            "suspended" or "inactive" or "неактивен" => Color.FromArgb("#B91C1C"),
            _ => Color.FromArgb("#1D4ED8")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class DoctorStatusToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as string)?.ToLowerInvariant() switch
        {
            "active" or "активен" => "Активен",
            "onleave" or "на отсуство" => "На отсуство",
            "suspended" or "inactive" or "неактивен" => "Неактивен",
            _ => value?.ToString()??"-"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}