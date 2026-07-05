using System;
using System.Globalization;

namespace EHMR.Converters;

public class DateTimeToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value==null)
            return "-";

        var format = parameter as string??"dd MMM yyyy";

        if(value is DateTime dt)
            return dt.ToString(format, culture);

        if(value is DateTimeOffset dto)
            return dto.DateTime.ToString(format, culture);

        return "-";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}