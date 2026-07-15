using EHMR.Resources.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Converters
{
    public class MetricTileVariantToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is not MetricTileVariant variant)
                return Colors.Gray;

            var key = variant switch
            {
                MetricTileVariant.Success => "SparkAccentGreen",
                MetricTileVariant.Warning => "SparkAccentAmber",
                MetricTileVariant.Danger => "SparkAccentRed",
                MetricTileVariant.Info => "SparkAccentTeal",
                _ => "SparkTextMuted"
            };

            if(Application.Current?.Resources.TryGetValue(key, out var resource)==true&&resource is Color color)
                return color;

            // Fallback if the resource key doesn't exist yet in the Spark palette
            return variant switch
            {
                MetricTileVariant.Success => Colors.LimeGreen,
                MetricTileVariant.Warning => Colors.Orange,
                MetricTileVariant.Danger => Colors.Crimson,
                MetricTileVariant.Info => Colors.DodgerBlue,
                _ => Colors.Gray
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
