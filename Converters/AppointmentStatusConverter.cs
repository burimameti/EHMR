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
    // <summary>
    /// Converts between AppointmentStatus enum and its string representation,
    /// so a Picker whose ItemsSource is List&lt;string&gt; (VM.StatusOptions)
    /// can two-way bind SelectedItem to Appointment.Status directly.
    /// If you already have a generic enum/string converter, reuse that instead.
    /// </summary>
    public class AppointmentStatusConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value?.ToString()??string.Empty;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is string s&&Enum.TryParse<AppointmentStatus>(s, out var status)
                ? status
                : AppointmentStatus.Scheduled;
    }

    /// <summary>
    /// Extracts up to 2 initials from a full name for the avatar badge.
    /// Skip if you already have one from the Patient List / Dashboard work.
    /// </summary>
    public class AppointmentsInitialsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is not string name||string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
                _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Maps AppointmentStatus to a status-pill color. Adjust the hex values to
    /// match your existing MetricTileVariant (Info/Success/Warning/Danger) palette.
    /// </summary>
    public class AppointmentStatusToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var color = value switch
            {
                AppointmentStatus.Scheduled => "#3B82F6",   // Info blue
                AppointmentStatus.InProgress => "#8B5CF6",   // Violet
                AppointmentStatus.Completed => "#10B981",   // Success green
                AppointmentStatus.Cancelled => "#EF4444",   // Danger red
                _ => "#94A3B8"
            };

            return Color.FromArgb(color);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Turns an int count (e.g. Collection.Count) into an inverted bool, so
    /// "no items" placeholder labels can bind IsVisible="{Binding X.Count, ...}".
    /// Also handles plain bool for the IsNewAppointment inversion case.
    /// </summary>
    public class AppointmentInvertedBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value switch
            {
                int count => count==0,
                bool b => !b,
                null => true,
                _ => false
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

}
