namespace EHMR.Resources.Controls;

public partial class FFStatusChip : ContentView
{
    public FFStatusChip()
    {
        InitializeComponent();
    }

    // ================= TEXT =================
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string),
            typeof(FFStatusChip), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // ================= TYPE =================
    public static readonly BindableProperty TypeProperty =
        BindableProperty.Create(nameof(Type), typeof(string),
            typeof(FFStatusChip), string.Empty,
            propertyChanged: OnTypeChanged);

    public string Type
    {
        get => (string)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    // ================= COLORS =================
    public static readonly BindableProperty BackgroundColorProperty =
        BindableProperty.Create(nameof(BackgroundColor), typeof(Color),
            typeof(FFStatusChip), Colors.LightGray);

    public Color BackgroundColor
    {
        get => (Color)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color),
            typeof(FFStatusChip), Colors.Black);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    // ================= THEME ENGINE =================
    private static void OnTypeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var chip = (FFStatusChip)bindable;

        var type = newValue?.ToString()?.ToLower();

        // If user manually overrides → respect it
        if(chip.BackgroundColor!=Colors.LightGray)
            return;

        switch(type)
        {
            case "success":
                chip.Apply("#DCFCE7", "#166534");
                break;

            case "warning":
                chip.Apply("#FEF3C7", "#92400E");
                break;

            case "danger":
                chip.Apply("#FEE2E2", "#991B1B");
                break;

            case "info":
                chip.Apply("#E0F2FE", "#075985");
                break;

            default:
                chip.Apply("#F3F4F6", "#374151");
                break;
        }
    }

    private void Apply(string bgHex, string fgHex)
    {
        BackgroundColor=Color.FromArgb(bgHex);
        TextColor=Color.FromArgb(fgHex);
    }
}