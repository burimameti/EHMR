using EHMR.Domain.Entities;
using EHMR.ViewModels.Constants;
using System.Globalization;
using IValueConverter = Microsoft.Maui.Controls.IValueConverter;

namespace EHMR.Converters;
public class EncounterStatusOption
{
    public EncounterStatus Value
    {
        get; set;
    }
    public string Label { get; set; } = string.Empty;
}



public static class EncounterStatusLocalization
{

    public static string ToMk(EncounterStatus status) => status switch
    {
        EncounterStatus.Scheduled => "Закажан",
        EncounterStatus.InProgress => "Во тек",
        EncounterStatus.Completed => "Завршен",
        EncounterStatus.Cancelled => "Откажан",
        EncounterStatus.NoShow => "Не се пријавил",
        _ => status.ToString()
    };
}
public abstract class EncounterStatusConverterBase<T> : IValueConverter
{
    public abstract T DefaultValue
    {
        get;
    }
    public abstract T Map(string status);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString();
        if(string.IsNullOrWhiteSpace(status))
            return DefaultValue;

        return Map(status);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class EncounterStatusToTextConverter : EncounterStatusConverterBase<string>
{
    public override string DefaultValue => "";

    public override string Map(string status)
    {
        if(status=="All"||status=="Сите") return "Сите";

        return EncounterStatusSchema.Display.TryGetValue(status, out var v)
            ? v
            : status;
    }
}

public class EncounterStatusToColorConverter : EncounterStatusConverterBase<Color>
{
    public override Color DefaultValue => Color.FromArgb("#64748B"); // Slate 500

    public override Color Map(string status)
    {
        if(status=="All"||status=="Сите") return Color.FromArgb("#64748B"); // Default gray color for 'All'

        return Color.FromArgb(
            EncounterStatusSchema.Color.TryGetValue(status, out var v)
                ? v
                : "#64748B");
    }
}

public class EncounterStatusToBgConverter : EncounterStatusConverterBase<Color>
{
    public override Color DefaultValue => Color.FromArgb("#E2E8F0"); // Slate 200

    public override Color Map(string status)
    {
        if(status=="All"||status=="Сите") return Color.FromArgb("#F1F5F9"); // Clean light background for 'All'

        return Color.FromArgb(
            EncounterStatusSchema.Background.TryGetValue(status, out var v)
                ? v
                : "#E2E8F0");
    }
}