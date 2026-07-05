using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace EHMR.Converters;

public class BrushToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is SolidColorBrush solidBrush)
        {
            return solidBrush.Color;
        }

        if(value is Color color)
        {
            return color;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is Color color)
        {
            return new SolidColorBrush(color);
        }

        return value;
    }
}