using EHMR.Domain.Entities;
using System.Globalization;

namespace EHMR.Converters;

// ─────────────────────────────────────────────────────────────
// Macedonian label
// ─────────────────────────────────────────────────────────────
public class PatientStatusDisplayConverter : IValueConverter, IMarkupExtension
{
    private static readonly Dictionary<PatientStatus, string> _labels = new()
    {
        { PatientStatus.Active,    "Активен"   },
        { PatientStatus.Inactive,  "Неактивен" },
        { PatientStatus.Deceased,  "Починат"   },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is PatientStatus s&&_labels.TryGetValue(s, out var label)) return label;
        return value?.ToString()??string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public IValueConverter ProvideValue(IServiceProvider sp) => this;
    object IMarkupExtension.ProvideValue(IServiceProvider sp) => this;
}

// ─────────────────────────────────────────────────────────────
// Pill background color
// ─────────────────────────────────────────────────────────────
public class PatientStatusToPillBgConverter : IValueConverter, IMarkupExtension
{
    private static readonly Dictionary<PatientStatus, Color> _colors = new()
    {
        { PatientStatus.Active,   Color.FromArgb("#F0FDF4") }, // emerald tint
        { PatientStatus.Inactive, Color.FromArgb("#F1F5F9") }, // slate tint
        { PatientStatus.Deceased, Color.FromArgb("#FFF1F2") }, // rose tint
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is PatientStatus s&&_colors.TryGetValue(s, out var c)) return c;
        return Color.FromArgb("#F1F5F9");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public IValueConverter ProvideValue(IServiceProvider sp) => this;
    object IMarkupExtension.ProvideValue(IServiceProvider sp) => this;
}

// ─────────────────────────────────────────────────────────────
// Pill foreground color (text + dot + stroke)
// ─────────────────────────────────────────────────────────────
public class PatientStatusToPillFgConverter : IValueConverter, IMarkupExtension
{
    private static readonly Dictionary<PatientStatus, Color> _colors = new()
    {
        { PatientStatus.Active,   Color.FromArgb("#15803D") }, // emerald
        { PatientStatus.Inactive, Color.FromArgb("#475569") }, // slate
        { PatientStatus.Deceased, Color.FromArgb("#BE123C") }, // rose
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is PatientStatus s&&_colors.TryGetValue(s, out var c)) return c;
        return Color.FromArgb("#64748B");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public IValueConverter ProvideValue(IServiceProvider sp) => this;
    object IMarkupExtension.ProvideValue(IServiceProvider sp) => this;
}