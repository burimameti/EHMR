using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Converters
{
    public class EncounterToLayoutBoundsConverter : IValueConverter
    {
        /// <summary>Pixel height representing one hour on the grid. Must match the row
        /// height used for HourSlots in MainPage.xaml (default 64).</summary>
        public double HourHeight { get; set; } = 64;

        /// <summary>Hour at which the grid starts (e.g. 8 => grid begins at 08:00).</summary>
        public double GridStartHour { get; set; } = 8;

        public double MinimumHeight { get; set; } = 28;

        /// <summary>Column width for a day (must match the WidthRequest on each day Grid
        /// in MainPage.xaml, currently 150 minus a small side inset).</summary>
        public double ColumnWidth { get; set; } = 142;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is null)
                return new Rect(0, 0, ColumnWidth, MinimumHeight);

            var type = value.GetType();
            var startProp = type.GetProperty("StartTime");
            // Replace this line:
            // var start = startProp?.GetValue(value) as DateTime ?? DateTime.Today;

            // With the following to fix CS0077:
            var start = startProp?.GetValue(value) is DateTime dt ? dt : DateTime.Today;

            double durationMinutes;
            var endProp = type.GetProperty("EndTime");
            if(endProp?.GetValue(value) is DateTime end)
            {
                durationMinutes=(end-start).TotalMinutes;
            }
            else
            {
                var durationProp = type.GetProperty("DurationMinutes");
                // Replace this line:
                // durationMinutes=durationProp?.GetValue(value) as double??45;

                // With the following to fix CS0077:
                durationMinutes=durationProp?.GetValue(value) is double d ? d : 45;
                // Replace the following lines in EncounterToLayoutBoundsConverter.Convert:

                // durationMinutes=durationProp?.GetValue(value) as double??45;
                // durationMinutes=durationProp?.GetValue(value) as double??45;

                // With this single line:
                //durationMinutes=durationProp?.GetValue(value) is double d ? d : 45;
               // durationMinutes=durationProp?.GetValue(value) as double??45;
            }

            double totalHours = start.Hour+start.Minute/60.0;
            double y = Math.Max(0, (totalHours-GridStartHour)*HourHeight);
            double height = Math.Max(MinimumHeight, (durationMinutes/60.0)*HourHeight);

            return new Rect(0, y, ColumnWidth, height);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
    public class KeyToColorConverter : IValueConverter
    {
        static readonly Dictionary<string, string> ResourceMap = new()
        {
            ["Pink"]="Encounter{0}",
            ["Green"]="Encounter{0}",
            ["Blue"]="Encounter{0}",
            ["Orange"]="Encounter{0}",
            ["Gray"]="Encounter{0}",
            ["Teal"]="Avatar{0}",
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is not string key||string.IsNullOrWhiteSpace(key))
                return Colors.Transparent;

            // "PinkText" -> base key "Pink", suffix "Text"
            bool isText = key.EndsWith("Text", StringComparison.Ordinal);
            var baseKey = isText ? key[..^4] : key;

            if(!ResourceMap.TryGetValue(baseKey, out var template))
                return Colors.Transparent;

            var resourceKey = template.Contains("Avatar")
                ? string.Format(template, baseKey)
                : string.Format(template, baseKey)+(isText ? "Text" : "");

            if(Application.Current?.Resources.TryGetValue(resourceKey, out var resource)==true
                &&resource is Color color)
            {
                return color;
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class HourToRowIndexConverter : IValueConverter
    {
        // почетен час на мрежата (09:00)
        public int StartHour { get; set; } = 9;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is DateTime dt)
                return Math.Max(0, dt.Hour-StartHour);
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class BoolToPillBgConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b&&b) ? Colors.White : Colors.Transparent;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class DoubleToTopMarginConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var top = value is double d ? d : 0;
            return new Thickness(4, top, 4, 0);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

}
