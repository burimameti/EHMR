using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Maui.Controls;
using EHMR.Domain.Entities;

namespace EHMR.Resources.Controls;

public partial class FFPicker : ContentView
{
    public FFPicker()
    {
        InitializeComponent();

        InnerPicker.SelectedIndexChanged += (_, _) =>
        {
            SetValue(SelectedIndexProperty, InnerPicker.SelectedIndex);
            SetValue(SelectedItemProperty, InnerPicker.SelectedItem);
        };

        SizeChanged += OnPickerSizeChanged;
        ApplyResponsiveLayout();
    }

    private void OnPickerSizeChanged(object? sender, EventArgs e)
        => ApplyResponsiveLayout();

    private void ApplyResponsiveLayout()
    {
        // Desktop-first sizing. Keep the normal 1920px layout unchanged,
        // then progressively tighten the control when the available width
        // becomes constrained instead of changing the surrounding layout.
        var width = Width;

        if (width <= 0)
            return;

        if (width < 220)
        {
            PickerBorder.HeightRequest = 38;
            InnerPicker.FontSize = 11.5;
            InnerPicker.Margin = new Thickness(7, 0, 1, 0);
            ArrowButton.WidthRequest = 28;
            ArrowButton.Margin = new Thickness(2, 4, 4, 4);
            ArrowIcon.FontSize = 8;
            FieldLabel.FontSize = 10;
        }
        else if (width < 300)
        {
            PickerBorder.HeightRequest = 41;
            InnerPicker.FontSize = 12;
            InnerPicker.Margin = new Thickness(8, 0, 1, 0);
            ArrowButton.WidthRequest = 31;
            ArrowButton.Margin = new Thickness(2, 5, 5, 5);
            ArrowIcon.FontSize = 8.5;
            FieldLabel.FontSize = 10.5;
        }
        else
        {
            PickerBorder.HeightRequest = 44;
            InnerPicker.FontSize = 13;
            InnerPicker.Margin = new Thickness(12, 0, 2, 0);
            ArrowButton.WidthRequest = 34;
            ArrowButton.Margin = new Thickness(2, 5, 5, 5);
            ArrowIcon.FontSize = 9.5;
            FieldLabel.FontSize = 11;
        }
    }

    private void OnArrowTapped(object? sender, TappedEventArgs e)
    {
#if WINDOWS
        if (InnerPicker.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ComboBox comboBox)
        {
            comboBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            comboBox.IsDropDownOpen = true;
            return;
        }
#endif
        InnerPicker.Focus();
    }

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(FFPicker), string.Empty);

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FFPicker), string.Empty);

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IList),
            typeof(FFPicker),
            null,
            propertyChanged: (b, _, n) =>
            {
                if (b is FFPicker picker)
                {
                    picker.InnerPicker.ItemsSource = n as IList;
                    picker.ApplyItemDisplayBinding();
                    picker.SyncSelection();
                }
            });

    public IList ItemsSource
    {
        get => (IList)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty ItemDisplayBindingProperty =
        BindableProperty.Create(
            nameof(ItemDisplayBinding),
            typeof(BindingBase),
            typeof(FFPicker),
            null,
            propertyChanged: (b, _, n) =>
            {
                if (b is FFPicker picker)
                    picker.InnerPicker.ItemDisplayBinding = n as BindingBase;
            });

    private void ApplyItemDisplayBinding()
    {
        // If the source contains enum values and the caller did not provide an
        // explicit display binding, show the human-readable value instead of
        // the CLR enum name (for example "Daily" or "EHMR.Domain.Entities.X").
        if (ItemDisplayBinding is not null)
            return;

        if (ItemsSource is not IList items || items.Count == 0)
            return;

        var first = items.Cast<object?>().FirstOrDefault(x => x is not null);
        if (first is not Enum)
            return;

        InnerPicker.ItemDisplayBinding = new Binding(".", converter: EnumPickerDisplayConverter.Instance);
    }

    public BindingBase ItemDisplayBinding
    {
        get => (BindingBase)GetValue(ItemDisplayBindingProperty);
        set => SetValue(ItemDisplayBindingProperty, value);
    }

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(
            nameof(SelectedItem),
            typeof(object),
            typeof(FFPicker),
            null,
            BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                if (b is FFPicker picker && !Equals(picker.InnerPicker.SelectedItem, n))
                    picker.InnerPicker.SelectedItem = n;
            });

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    private void SyncSelection()
    {
        var selected=GetValue(SelectedItemProperty);
        if(selected is not null && ItemsSource is IList items && items.Contains(selected))
        {
            if(!Equals(InnerPicker.SelectedItem, selected))
                InnerPicker.SelectedItem=selected;
            return;
        }

        if(selected is null && InnerPicker.SelectedIndex!=-1)
            InnerPicker.SelectedIndex=-1;
    }

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(
            nameof(SelectedIndex),
            typeof(int),
            typeof(FFPicker),
            -1,
            BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                if (b is FFPicker picker &&
                    picker.InnerPicker.SelectedIndex != (int)n)
                {
                    picker.InnerPicker.SelectedIndex = (int)n;
                }
            });

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }
}

internal sealed class EnumPickerDisplayConverter : IValueConverter
{
    public static EnumPickerDisplayConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not Enum enumValue)
            return value?.ToString() ?? string.Empty;

        // Keep picker display independent from enum CLR names. These are the
        // Macedonian data-entry labels already used throughout EHMR.
        return enumValue switch
        {
            DosesFrequency.Daily => "Дневно",
            DosesFrequency.TwiceDaily => "Двапати",
            DosesFrequency.ThreeTimesDaily => "Трипати",
            DosesFrequency.EveryOtherDay => "Секој втор ден",
            DosesFrequency.EveryThreeDays => "Секој трет ден",
            DosesFrequency.Weekly => "Неделно",
            DosesFrequency.Monthly => "Месечно",
            DosesFrequency.Other => "Друго",
            PatientStatus.Active => "Активен",
            PatientStatus.Inactive => "Неактивен",
            PatientStatus.Discharged => "Отпуштен",
            PatientStatus.Deceased => "Починат",
            PatientStatus.Chronic => "Хроничен",
            PatientStatus.Recovered => "Оздравен",
            PatientStatus.UnderObservation => "Под опсервација",
            TherapyStatus.Planned => "Планирана",
            TherapyStatus.Active => "Активна",
            TherapyStatus.Scheduled => "Закажана",
            TherapyStatus.Completed => "Завршена",
            TherapyStatus.Suspended => "Суспендирана",
            TherapyStatus.Canceled => "Откажана",
            TherapyStatus.Missed => "Пропуштена",
            _ => GetEnumDescription(enumValue)
        };
    }

    private static string GetEnumDescription(Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var description = member?.GetCustomAttribute<DescriptionAttribute>()?.Description;
        return string.IsNullOrWhiteSpace(description) ? value.ToString() : description;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => Binding.DoNothing;
}
