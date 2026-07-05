using EHMR.Domain.Entities;
using System.Globalization;

namespace EHMR.Converters
{
    public class AppointmentStatusDisplayConverter : IValueConverter, IMarkupExtension
    {
        private static readonly Dictionary<AppointmentStatus, string> _labels = new()
    {
        { AppointmentStatus.Scheduled,   "Закажан"    },
        { AppointmentStatus.CheckedIn,   "Пријавен"   },
        { AppointmentStatus.InProgress,  "Во тек"     },
        { AppointmentStatus.Completed,   "Завршен"    },
        { AppointmentStatus.Cancelled,   "Откажан"    },
        { AppointmentStatus.Missed,      "Пропуштен"  },
        { AppointmentStatus.ReScheduled, "Преместен"  },
    };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is AppointmentStatus status&&_labels.TryGetValue(status, out var label))
                return label;

            // Fallback: return the raw enum string so nothing shows blank
            return value?.ToString()??string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        // IMarkupExtension so you can use {converters:StatusDisplayConverter} in XAML
        public IValueConverter ProvideValue(IServiceProvider serviceProvider) => this;
        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => this;
    }
}