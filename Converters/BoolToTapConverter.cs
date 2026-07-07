using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Converters
{
    public class BoolToTabBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value
                ? Color.FromArgb("#68D1DE")
                : Color.FromArgb("#90A1AD");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
    }
}
