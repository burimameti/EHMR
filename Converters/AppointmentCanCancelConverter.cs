using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Converters
{
    public sealed class AppointmentCanCancelConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if(value is not Appointment appointment)
                return false;

            if(appointment.ScheduledStart.Date<DateTime.Today)
                return false;

            return appointment.Status==AppointmentStatus.Scheduled;
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
            => throw new NotSupportedException();
    }
}