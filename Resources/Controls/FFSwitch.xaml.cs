namespace EHMR.Resources.Controls;

public partial class FFSwitch : ContentView
{
    public FFSwitch()
    {
        InitializeComponent();
    }

    // ================= LABEL =================
    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(FFSwitch));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    // ================= TOGGLED =================
    public static readonly BindableProperty IsToggledProperty =
        BindableProperty.Create(nameof(IsToggled), typeof(bool), typeof(FFSwitch),
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: OnToggledChanged);

    public bool IsToggled
    {
        get => (bool)GetValue(IsToggledProperty);
        set => SetValue(IsToggledProperty, value);
    }

    // ================= ENABLED =================
    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.Create(nameof(IsEnabled), typeof(bool), typeof(FFSwitch), true);

    public new bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    // ================= STATUS TEXT =================
    public static readonly BindableProperty StatusTextProperty =
        BindableProperty.Create(nameof(StatusText), typeof(string), typeof(FFSwitch));

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    // ================= LOGIC =================
    private static void OnToggledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var c = (FFSwitch)bindable;

        bool value = (bool)newValue;

        c.StatusText=value ? "Enabled" : "Disabled";
    }
}