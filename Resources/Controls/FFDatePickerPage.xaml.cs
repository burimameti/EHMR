using System.ComponentModel;

namespace EHMR.Resources.Controls;

public partial class FFDatePicker : ContentView
{
    public static readonly BindableProperty ResponsiveScaleProperty =
        BindableProperty.Create(nameof(ResponsiveScale), typeof(double), typeof(FFDatePicker), 1d,
            propertyChanged: (b, _, n) => ((FFDatePicker)b).ApplyResponsiveScale((double)n));

    public double ResponsiveScale
    {
        get => (double)GetValue(ResponsiveScaleProperty);
        set => SetValue(ResponsiveScaleProperty, value);
    }

    private void ApplyResponsiveScale(double scale)
    {
        scale=Math.Clamp(scale, 0.72, 1.0);
        PickerBorder.HeightRequest=48*scale;
        DisplayLabel.FontSize=14*scale;
        InnerDatePicker.FontSize=14*scale;
    }

    public FFDatePicker()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    // =====================================================
    // BINDABLE PROPERTIES
    // =====================================================
    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(FFDatePicker), string.Empty, propertyChanged: OnLabelChanged);

    public static readonly BindableProperty HasLabelProperty =
        BindableProperty.Create(nameof(HasLabel), typeof(bool), typeof(FFDatePicker), true);

    public static readonly BindableProperty SelectedDateProperty =
        BindableProperty.Create(nameof(SelectedDate), typeof(DateTime), typeof(FFDatePicker), DateTime.Today, BindingMode.TwoWay, propertyChanged: OnDateChanged);

    public static readonly BindableProperty IsOptionalFilterProperty =
        BindableProperty.Create(nameof(IsOptionalFilter), typeof(bool), typeof(FFDatePicker), false, propertyChanged: OnFilterModeChanged);

    public static readonly BindableProperty IsFilterActiveProperty =
        BindableProperty.Create(nameof(IsFilterActive), typeof(bool), typeof(FFDatePicker), false, BindingMode.TwoWay, propertyChanged: OnFilterModeChanged);

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool HasLabel
    {
        get => (bool)GetValue(HasLabelProperty);
        set => SetValue(HasLabelProperty,value);
    }

    public DateTime SelectedDate
    {
        get => (DateTime)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public bool IsOptionalFilter
    {
        get => (bool)GetValue(IsOptionalFilterProperty);
        set => SetValue(IsOptionalFilterProperty, value);
    }

    public bool IsPickerEnabled => !IsOptionalFilter||IsFilterActive;

    public bool IsFilterActive
    {
        get => (bool)GetValue(IsFilterActiveProperty);
        set => SetValue(IsFilterActiveProperty, value);
    }

    private static void OnFilterModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control=(FFDatePicker)bindable;
        control.OnPropertyChanged(nameof(IsPickerEnabled));
    }

    // =====================================================
    // COMPONENT INTERACTION LOGIC
    // =====================================================
    private static void OnLabelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (FFDatePicker)bindable;
        control.HasLabel=!string.IsNullOrWhiteSpace(newValue?.ToString());
    }

    private static void OnDateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (FFDatePicker)bindable;
        control.UpdateDisplay();
    }

    private void OnBorderTapped(object sender, TappedEventArgs e)
    {
        if(IsPickerEnabled)
        {
            InnerDatePicker.Focus();
        }
    }

    private void InnerDatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        DisplayLabel.Text=SelectedDate.ToString("dd.MM.yyyy");
    }
}