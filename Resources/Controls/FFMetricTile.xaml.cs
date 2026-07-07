using System.Windows.Input;
namespace EHMR.Resources.Controls; 
public enum MetricTileVariant
    {
        Neutral,
        Info,
        Success,
        Warning,
        Danger
}


public partial class FFMetricTile : ContentView
{
    public FFMetricTile()
    {
        InitializeComponent();
    }
    public static readonly BindableProperty IconProperty =
    BindableProperty.Create(nameof(Icon), typeof(string), typeof(FFMetricTile), "");
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    public static readonly BindableProperty IsSelectedProperty =
        BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(FFMetricTile),
            false,
            propertyChanged: OnVisualStateChanged);

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(
            nameof(Variant),
            typeof(MetricTileVariant),
            typeof(FFMetricTile),
            MetricTileVariant.Neutral,
            propertyChanged: OnVisualStateChanged);

    public MetricTileVariant Variant
    {
        get => (MetricTileVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(FFMetricTile), "");
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(FFMetricTile), "0");
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(FFMetricTile));
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(FFMetricTile));
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly BindableProperty TitleColorProperty =
        BindableProperty.Create(
            nameof(TitleColor),
            typeof(Color),
            typeof(FFMetricTile),
            Color.FromArgb("#8D98A5"));
    public Color TitleColor
    {
        get => (Color)GetValue(TitleColorProperty);
        set => SetValue(TitleColorProperty, value);
    }

    public static readonly BindableProperty AccentColorProperty =
        BindableProperty.Create(
            nameof(AccentColor),
            typeof(Color),
            typeof(FFMetricTile),
            Colors.Transparent);
    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    // Neutral color inactive tiles fall back to — unchanged from the original default.
    private static readonly Color InactiveColor = Color.FromArgb("#8D98A5");

    private static Color ResolveAccentColor(MetricTileVariant variant) => variant switch
    {
        MetricTileVariant.Info => Color.FromArgb("#4CB7E8"),
        MetricTileVariant.Success => Color.FromArgb("#22C55E"),
        MetricTileVariant.Warning => Color.FromArgb("#F59E0B"),
        MetricTileVariant.Danger => Color.FromArgb("#EF4444"),
        _ => Color.FromArgb("#4CB7E8") // Neutral keeps the original selected-blue for tab-strip usage
    };

    /// <summary>
    /// Single source of truth for TitleColor/AccentColor, now driven by both
    /// IsSelected (tab-strip usage — only the active tab lights up) and Variant
    /// (KPI usage — health/status color regardless of selection).
    /// </summary>
    private static void OnVisualStateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tile = (FFMetricTile)bindable;

        if(tile.IsSelected)
        {
            var color = ResolveAccentColor(tile.Variant);
            tile.TitleColor=color;
            tile.AccentColor=color;
        }
        else
        {
            tile.TitleColor=InactiveColor;
            tile.AccentColor=Colors.Transparent;
        }
    }
}