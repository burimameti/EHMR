using System.Windows.Input;

namespace EHMR.Resources.Controls;

/// <summary>Тонот на значката за промена во KPI картичката.</summary>
public enum KpiDeltaTone
{
    Neutral,
    Positive,
    Negative
}

/// <summary>
/// KPI картичка над календарот: наслов, период, голем број и значка за промена.
/// </summary>
public partial class CalendarKpiCard : ContentView
{
    public CalendarKpiCard()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(CalendarKpiCard), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty PeriodTextProperty =
        BindableProperty.Create(nameof(PeriodText), typeof(string), typeof(CalendarKpiCard), "Последни 7 дена");

    public string PeriodText
    {
        get => (string)GetValue(PeriodTextProperty);
        set => SetValue(PeriodTextProperty, value);
    }

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(CalendarKpiCard), string.Empty);

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly BindableProperty ComparisonTextProperty =
        BindableProperty.Create(nameof(ComparisonText), typeof(string), typeof(CalendarKpiCard), "од минатата недела");

    public string ComparisonText
    {
        get => (string)GetValue(ComparisonTextProperty);
        set => SetValue(ComparisonTextProperty, value);
    }

    public static readonly BindableProperty DeltaTextProperty =
        BindableProperty.Create(
            nameof(DeltaText),
            typeof(string),
            typeof(CalendarKpiCard),
            string.Empty,
            propertyChanged: OnDeltaChanged);

    public string DeltaText
    {
        get => (string)GetValue(DeltaTextProperty);
        set => SetValue(DeltaTextProperty, value);
    }

    public static readonly BindableProperty DeltaToneProperty =
        BindableProperty.Create(
            nameof(DeltaTone),
            typeof(KpiDeltaTone),
            typeof(CalendarKpiCard),
            KpiDeltaTone.Neutral,
            propertyChanged: OnDeltaChanged);

    public KpiDeltaTone DeltaTone
    {
        get => (KpiDeltaTone)GetValue(DeltaToneProperty);
        set => SetValue(DeltaToneProperty, value);
    }

    private static void OnDeltaChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var card = (CalendarKpiCard)bindable;
        card.OnPropertyChanged(nameof(HasDelta));
        card.OnPropertyChanged(nameof(DeltaBackground));
        card.OnPropertyChanged(nameof(DeltaForeground));
    }

    public bool HasDelta => !string.IsNullOrWhiteSpace(DeltaText);

    public Color DeltaBackground => DeltaTone switch
    {
        KpiDeltaTone.Positive => Color.FromArgb("#DCFCE7"),
        KpiDeltaTone.Negative => Color.FromArgb("#FCE7F3"),
        _ => Color.FromArgb("#F1F5F9")
    };

    public Color DeltaForeground => DeltaTone switch
    {
        KpiDeltaTone.Positive => Color.FromArgb("#15803D"),
        KpiDeltaTone.Negative => Color.FromArgb("#BE185D"),
        _ => Color.FromArgb("#475569")
    };

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(CalendarKpiCard));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(CalendarKpiCard));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}
