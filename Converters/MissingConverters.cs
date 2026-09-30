using System.Globalization;
using EHMR.Domain.Entities;

namespace EHMR.Converters;

// =============================================================================
//  Конвертори што се повикуваа од XAML но никогаш не постоеја како класи.
//  Нерешен StaticResource фрла XamlParseException штом страницата се вчита,
//  па секоја од страниците подолу пукаше при отворање.
// =============================================================================

/// <summary>
/// Позадина на значка за активен/неактивен запис.
/// EncounterDetailPage, PatientDetailFormPage.
/// </summary>
public class BoolToActiveBadgeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Color.FromArgb("#16A34A")   // зелена — активен
            : Color.FromArgb("#94A3B8");  // сива — неактивен

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Текст на истата значка.
/// EncounterDetailPage, PatientDetailFormPage.
/// </summary>
public class BoolToActiveLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Активен" : "Неактивен";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Точно кога Guid-от не е празен — значи записот е зачуван.
/// Се користи за криење на „Измени" кај нов запис (PrescriptionDetailFormPage).
/// </summary>
public class GuidToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Guid id&&id!=Guid.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Статус на термин на македонски. AppointmentDetailPage.
/// </summary>
public class AppointmentStatusToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            AppointmentStatus.Scheduled => "Закажан",
    
            AppointmentStatus.InProgress => "Во тек",
            AppointmentStatus.Completed => "Завршен",
            AppointmentStatus.Cancelled => "Откажан",
           // AppointmentStatus.Missed => "Пропуштен",
           // AppointmentStatus.ReScheduled => "Презакажан",
            _ => string.Empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Позадина на чипчето за статус на доктор. DoctorsDetailPage.
/// Парот DoctorStatusToTextColorConverter веќе постои и е регистриран.
/// </summary>
public class DoctorStatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString();

        return text switch
        {
            "Active" or "Активен" => Color.FromArgb("#DCFCE7"),
            "Inactive" or "Неактивен" => Color.FromArgb("#F1F5F9"),
            "Suspended" or "Суспендиран" => Color.FromArgb("#FEF3C7"),
            _ => Color.FromArgb("#F1F5F9")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
