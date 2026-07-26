using System.Globalization;

namespace EHMR.Converters;




public class BoolToColorConverter : IValueConverter
{
    /// <summary>Color used when the bound value is true. Default matches TodayBadgeBg.</summary>
    public Color TrueColor { get; set; } = Color.FromArgb("#111827");

    /// <summary>Color used when the bound value is false. Default is transparent.</summary>
    public Color FalseColor { get; set; } = Colors.Transparent;

    /// <summary>
    /// Pass ConverterParameter="invert" in XAML to flip the pairing — used for the date-badge
    /// number text, which needs White when today (badge is filled dark) and TitleColor otherwise
    /// (badge is transparent), i.e. the opposite pairing from the badge's own background.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isTrue = value is bool b&&b;
        bool invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);

        if(invert)
            return isTrue ? Colors.White : Color.FromArgb("#111827");

        return isTrue ? TrueColor : FalseColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
public class TimeToYConverter : IValueConverter
{
    /// <summary>Pixel height representing one hour on the grid. Must match the value
    /// used for HourLabelStyle rows in MainPage.xaml (default 64).</summary>
    public double HourHeight { get; set; } = 64;

    /// <summary>Hour at which the grid starts (e.g. 8 => grid begins at 08:00).</summary>
    public double GridStartHour { get; set; } = 8;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double totalHours = value switch
        {
            DateTime dt => dt.Hour+dt.Minute/60.0,
            TimeSpan ts => ts.TotalHours,
            _ => GridStartHour
        };

        double y = (totalHours-GridStartHour)*HourHeight;
        return Math.Max(0, y);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts an appointment duration (in minutes, or an End-Start TimeSpan)
/// into a pixel height for the block, matching TimeToYConverter's HourHeight.
/// </summary>
public class DurationToHeightConverter : IValueConverter
{
    public double HourHeight { get; set; } = 64;
    public double MinimumHeight { get; set; } = 28;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double minutes = value switch
        {
            TimeSpan ts => ts.TotalMinutes,
            double d => d,
            int i => i,
            _ => 45
        };

        double height = (minutes/60.0)*HourHeight;
        return Math.Max(MinimumHeight, height);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns true when the bound date is "today" — use to trigger DateBadgeTodayStyle
/// and the dark-filled circle on the active day column.
/// </summary>
public class IsTodayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is DateTime dt)
            return dt.Date==DateTime.Today;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}