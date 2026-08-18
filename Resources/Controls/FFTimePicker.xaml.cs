using System.ComponentModel;

namespace EHMR.Resources.Controls;

public partial class FFTimePicker : ContentView
{
    public FFTimePicker()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(FFTimePicker), string.Empty, propertyChanged: OnLabelChanged);

    public static readonly BindableProperty HasLabelProperty =
        BindableProperty.Create(nameof(HasLabel), typeof(bool), typeof(FFTimePicker), false);

    public static readonly BindableProperty SelectedTimeProperty =
        BindableProperty.Create(nameof(SelectedTime), typeof(TimeSpan), typeof(FFTimePicker), DateTime.Now.TimeOfDay, BindingMode.TwoWay, propertyChanged: OnTimeChanged);

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool HasLabel
    {
        get => (bool)GetValue(HasLabelProperty);
        set => SetValue(HasLabelProperty, value);
    }

    public TimeSpan SelectedTime
    {
        get => (TimeSpan)GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    private static void OnLabelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((FFTimePicker)bindable).HasLabel=!string.IsNullOrWhiteSpace(newValue?.ToString());
    }

    private static void OnTimeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((FFTimePicker)bindable).UpdateDisplay();
    }

    private void OnBorderTapped(object? sender, TappedEventArgs e) => InnerTimePicker.Focus();

    private void InnerTimePicker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName==TimePicker.TimeProperty.PropertyName)
            UpdateDisplay();
    }

    private void UpdateDisplay() => DisplayLabel.Text=SelectedTime.ToString(@"hh\:mm");
}