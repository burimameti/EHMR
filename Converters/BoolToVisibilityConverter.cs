using System.Globalization;
namespace EHMR.Converters;
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true;
}