using EHMR.Domain.Entities;
using EHMR.Domain.Search;
using EHMR.Services;
using EHMR.ViewModels;
using EHMR.ViewModels.Appointments;
using System.Globalization;

namespace EHMR.Converters;

// ─────────────────────────────────────────────────────────────────────────────
//  BACKGROUND converter
//  Scheduled / CheckedIn / InProgress  → teal-ish active pill
//  Completed                           → muted green/slate (done, not urgent)
//  Cancelled / NoShow                  → dark red/charcoal (dead state)
// ─────────────────────────────────────────────────────────────────────────────
public class EncounterStatusChipBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = ToStatus(value);
        return status switch
        {
            // Active / in-flight → vivid teal
            EncounterStatus.Scheduled => Color.FromArgb("#99D9EA"),   // teal-600
           // EncounterStatus.CheckedIn => Color.FromArgb("#0284C7"),   // sky-600
            EncounterStatus.InProgress => Color.FromArgb("#2563EB"),   // blue-600

            // Completed → calm slate-green
            EncounterStatus.Completed => Color.FromArgb("#475569"),   // slate-600

            // Terminal / bad → dark charcoal-red
            EncounterStatus.Cancelled => Color.FromArgb("#7F1D1D"),   // red-900
           // EncounterStatus.NoShow => Color.FromArgb("#4B1C1C"),   // deeper red-900

            _ => Color.FromArgb("#64748B")    // slate-500 fallback
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static EncounterStatus? ToStatus(object? value) => value switch
    {
        EncounterStatus e => e,
        string s when Enum.TryParse<EncounterStatus>(s, out var p) => p,
        _ => null
    };
}

// ─────────────────────────────────────────────────────────────────────────────
//  TEXT COLOR converter
//  Always white — the backgrounds above are all dark enough.
//  Kept as a separate converter so you can swap it independently.
// ─────────────────────────────────────────────────────────────────────────────
public class EncounterStatusChipTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Colors.White;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ─────────────────────────────────────────────────────────────────────────────
//  DISPLAY LABEL converter  (enum → Macedonian string)
// ─────────────────────────────────────────────────────────────────────────────
public class EncounterStatusChipLabelConverter : IValueConverter
{
    private static readonly Dictionary<EncounterStatus, string> Labels = new()
    {
        [EncounterStatus.Scheduled]="Закажан",
       // [EncounterStatus.CheckedIn]="Пријавен",
        [EncounterStatus.InProgress]="Во тек",
        [EncounterStatus.Completed]="Завршен",
        [EncounterStatus.Cancelled]="Откажан",
       // [EncounterStatus.NoShow]="Не дојде",
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is EncounterStatus s&&Labels.TryGetValue(s, out var label))
            return label;
        if(value is string str&&Enum.TryParse<EncounterStatus>(str, out var parsed)
            &&Labels.TryGetValue(parsed, out var l))
            return l;
        return value?.ToString()??"—";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
/// <summary>
/// Border.Stroke color for one suggestion row: highlighted (parameter color,
/// typically the app's Primary resource) when this row is the one currently
/// tracked by SelectedSuggestion - whether that got there via mouse click or
/// arrow-key navigation - otherwise transparent.
/// Compares by Id+Type rather than reference equality, since the same logical
/// suggestion can come back as a different DTO instance across searches.
/// </summary>
public sealed class SuggestionSelectionStrokeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if(values.Length>=2
           &&values[0] is SearchSuggestionDto current
           &&values[1] is SearchSuggestionDto selected
           &&current.Id==selected.Id
           &&current.Type==selected.Type)
        {
            return parameter as Color??Colors.DodgerBlue;
        }

        return Colors.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Bolds the part of a suggestion's display text that matches the current
/// search query. Reuses AppointmentListViewModel.BuildHighlighted so the
/// matching logic lives in exactly one place.
/// </summary>
public sealed class HighlightMatchConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var formatted = new FormattedString();
        var text = values.Length>0 ? values[0] as string??"" : "";
        var query = values.Length>1 ? values[1] as string??"" : "";

        foreach(var span in AppointmentListViewModel.BuildHighlighted(text, query))
        {
            formatted.Spans.Add(new Span
            {
                Text=span.Text,
                FontAttributes=span.IsHighlighted ? FontAttributes.Bold : FontAttributes.None
            });
        }

        return formatted;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>FontAwesome glyph for a suggestion's entity type.</summary>
public sealed class SearchEntityTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is SearchEntityType.Doctor ? "\uf0f8" : "\uf007";

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Macedonian caption shown under a suggestion's name.</summary>
public sealed class SearchEntityTypeToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is SearchEntityType.Doctor ? "Реуматолог" : "Пациент";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}