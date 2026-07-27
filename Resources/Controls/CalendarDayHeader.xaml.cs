using System.Windows.Input;
using EHMR.ViewModels.Calendar;

namespace EHMR.Resources.Controls;

/// <summary>
/// Заглавје за еден ден во неделниот приказ на календарот.
/// Се поставува директно во колона од гридот, за да се совпадне со
/// вертикалните линии на редовите под него.
/// </summary>
public partial class CalendarDayHeader : ContentView
{
    public CalendarDayHeader()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty DayProperty =
        BindableProperty.Create(
            nameof(Day),
            typeof(CalendarDayDto),
            typeof(CalendarDayHeader),
            propertyChanged: OnDayChanged);

    public CalendarDayDto? Day
    {
        get => (CalendarDayDto?)GetValue(DayProperty);
        set => SetValue(DayProperty, value);
    }

    private static void OnDayChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CalendarDayHeader)bindable).OnPropertyChanged(nameof(HasDay));

    /// <summary>
    /// Без ден нема што да се црта. Кога Day е null внатрешните врзувања паѓаат,
    /// а IsVisible се враќа на true — така сината значка светеше во празна ќелија.
    /// </summary>
    public bool HasDay => Day is not null;

    public static readonly BindableProperty SelectCommandProperty =
        BindableProperty.Create(
            nameof(SelectCommand),
            typeof(ICommand),
            typeof(CalendarDayHeader));

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }
}
