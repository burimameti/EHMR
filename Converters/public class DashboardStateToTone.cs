using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Converters
{
    public class DashboardStateToTone : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            var state = value?.ToString()?.ToLower();

            return state switch
            {
                "active" => "#16A34A",
                "completed" => "#2563EB",
                "pending" => "#F59E0B",
                "failed" => "#DC2626",
                "cancelled" => "#6B7280",
                _ => "#64748B"
            };
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
