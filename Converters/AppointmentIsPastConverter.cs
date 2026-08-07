using EHMR.Domain.Entities;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Converters
{
    public sealed class AppointmentIsPastConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if(value is not Appointment appointment)
                return false;

            return appointment.ScheduledStart.Date<DateTime.Today;
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
            => throw new NotSupportedException();
    }
}