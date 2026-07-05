using EHMR.Domain.Search;
using EHMR.Services;
using EHMR.ViewModels;

using System.Globalization;

namespace EHMR.Converters;
public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is SearchEntityType.Doctor ? "Доктор" : "Пациент";

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}