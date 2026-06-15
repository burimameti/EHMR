using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace EHMR.Extensions;

public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is bool booleanValue)
        {
            return !booleanValue;
        }
        return true; // Default fallback
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is bool booleanValue)
        {
            return !booleanValue;
        }
        return true;
    }
}