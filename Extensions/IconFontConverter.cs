using EHMR.Domain.Entities.Rbac;
using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace EHMR.Extensions
{

    public class FontFamilyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is IconFontType fontType)
            {
                // Leverages your existing static FontResolver logic
                return FontResolver.GetFontFamily(fontType);
            }

            // Fallback default font if value is null or unexpected
            return "FASolid";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("FontFamilyConverter can only be used for OneWay binding.");
        }
    }

}