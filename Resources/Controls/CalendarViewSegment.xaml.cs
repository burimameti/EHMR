using System.Windows.Input;

namespace EHMR.Resources.Controls;

/// <summary>
/// Еден сегмент од преклопувачот на приказ (Ден / Недела / Месец).
/// Активниот сегмент е бел со сенка врз сива патека, како во дизајнот.
/// </summary>
public partial class CalendarViewSegment : ContentView
{
    public CalendarViewSegment()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(CalendarViewSegment), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty IsActiveProperty =
        BindableProperty.Create(
            nameof(IsActive),
            typeof(bool),
            typeof(CalendarViewSegment),
            false,
            propertyChanged: OnIsActiveChanged);

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private static void OnIsActiveChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CalendarViewSegment)bindable).OnPropertyChanged(nameof(LabelColor));

    /// <summary>Активниот сегмент е потемен, неактивните посветли.</summary>
    public Color LabelColor =>
        IsActive
            ? Color.FromArgb("#0F172A")
            : Color.FromArgb("#64748B");

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(CalendarViewSegment));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(CalendarViewSegment));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}
