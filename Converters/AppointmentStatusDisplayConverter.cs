using EHMR.Domain.Entities;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EHMR.Converters
{
    /// <summary>
    /// Single source of truth for AppointmentStatus <-> Macedonian label.
    /// Used as an IValueConverter for display (status chip, history rows) AND
    /// for the edit-mode status picker's two-way SelectedItem binding (via
    /// ConvertBack), and via its static members from AppointmentListViewModel's
    /// grid badges and AppointmentDetailViewModel.StatusOptions - so every
    /// place a status is shown or edited draws from exactly one mapping.
    /// </summary>
    public class AppointmentStatusDisplayConverter : IValueConverter, IMarkupExtension
    {
        private static readonly Dictionary<AppointmentStatus, string> _labels = new()
        {
            { AppointmentStatus.Scheduled,   "Закажан"   },
            { AppointmentStatus.InProgress,  "Во тек"    },
            { AppointmentStatus.Completed,   "Завршен"   },
            { AppointmentStatus.Cancelled,   "Откажан"   },
            { AppointmentStatus.Missed,      "Пропуштен" },
        };

        // Reverse lookup built once from _labels, so the two directions can
        // never drift apart the way two hand-written dictionaries could.
        private static readonly Dictionary<string, AppointmentStatus> _statusesByLabel =
            _labels.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        /// <summary>Enum -> Macedonian label. Used directly by view models (grid badges, StatusOptions).</summary>
        public static string Label(AppointmentStatus status) =>
            _labels.TryGetValue(status, out var label) ? label : status.ToString();

        /// <summary>Macedonian label -> enum, for parsing a picker selection back into AppointmentStatus.</summary>
        public static AppointmentStatus? FromLabel(string? label) =>
            label is not null&&_statusesByLabel.TryGetValue(label, out var status) ? status : null;

        /// <summary>All statuses as Macedonian labels, in declared order - for picker ItemsSource.</summary>
        public static IReadOnlyList<string> AllLabels { get; } = _labels.Values.ToList();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is AppointmentStatus status&&_labels.TryGetValue(status, out var label))
                return label;
            // Fallback: return the raw enum string so nothing shows blank
            return value?.ToString()??string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is string label)
            {
                var match = FromLabel(label);
                if(match is not null) return match.Value;
            }

            // Unrecognized/cleared selection - leave the bound Appointment.Status
            // untouched rather than silently defaulting it to some status.
            return BindableProperty.UnsetValue;
        }

        // IMarkupExtension so you can use {converters:AppointmentStatusDisplayConverter} in XAML
        public IValueConverter ProvideValue(IServiceProvider serviceProvider) => this;
        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => this;
    }
}