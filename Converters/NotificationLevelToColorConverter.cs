using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EHMR.ViewModels.DashboardViewModel;

namespace EHMR.Converters
{
    public class NotificationLevelToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value switch
            {
                NotificationLevel.Error => (Color)Application.Current!.Resources["SparkBadgeDangerText"],
                NotificationLevel.Warning => (Color)Application.Current!.Resources["SparkBadgeWarningText"],
                NotificationLevel.Info => (Color)Application.Current!.Resources["SparkAccentTeal"],
                _ => (Color)Application.Current!.Resources["SparkTextMuted"]
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
