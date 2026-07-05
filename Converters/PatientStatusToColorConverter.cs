using EHMR.Domain.Entities;
using EHMR.ViewModels.Patients.Extensions;
using System;
using System.Globalization;

namespace EHMR.Converters
{

    /// <summary>
    /// Текст боја за статус chip - темна/наситена нијанса за секој статус.
    /// </summary>
    public class PatientStatusToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is not PatientStatus status)
                return Colors.Black;

            return status switch
            {
                PatientStatus.Active => Color.FromArgb("#16A34A"),
                PatientStatus.Inactive => Color.FromArgb("#64748B"),
                PatientStatus.UnderObservation => Color.FromArgb("#D97706"),
                PatientStatus.Discharged => Color.FromArgb("#B91C1C"),
                PatientStatus.Recovered => Color.FromArgb("#2563EB"),
                _ => Colors.Black
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Позадина (tint) на chip-от - светла верзија на соодветната текст боја,
    /// за секој од 5 статуси (порано само Active/останато беше разликувано).
    /// </summary>
    public class PatientStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is not PatientStatus status)
                return Color.FromArgb("#F1F5F9");

            return status switch
            {
                PatientStatus.Active => Color.FromArgb("#DCFCE7"),            // светло зелено
                PatientStatus.Inactive => Color.FromArgb("#F1F5F9"),          // светло сиво
                PatientStatus.UnderObservation => Color.FromArgb("#FEF3C7"), // светло жолто/кафено
                PatientStatus.Discharged => Color.FromArgb("#FEE2E2"),       // светло црвено
                PatientStatus.Recovered => Color.FromArgb("#DBEAFE"),        // светло сино
                _ => Color.FromArgb("#F1F5F9")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class GenderToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is null)
                return string.Empty;

            var strValue = value.ToString();
            if(strValue is null)
                return string.Empty;

            return strValue switch
            {
                "Male" => "Машко",
                "Female" => "Женско",
                _ => strValue
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Македонски приказ-текст за статус (за StatusChip.Text наместо raw enum.ToString()).
    /// </summary>
    public class PatientStatusToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is not PatientStatus status)
                return string.Empty;

            return status switch
            {
                PatientStatus.Active => "Активен",
                PatientStatus.Inactive => "Неактивен",
                PatientStatus.UnderObservation => "Под набљудување",
                PatientStatus.Discharged => "Отпуштен",
                PatientStatus.Recovered => "Закрепнат",
                _ => status.ToString()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public sealed class GenderDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is Gender g ? g.ToDisplay() : value?.ToString()??"";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    public sealed class StatusDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is PatientStatus s ? s.ToDisplay() : value?.ToString()??"";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}