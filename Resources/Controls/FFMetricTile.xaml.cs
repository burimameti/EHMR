using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Drawing;
using System.Windows.Input;
using Color = Microsoft.Maui.Graphics.Color;

namespace EHMR.Resources.Controls;

public enum MetricTileDisplayMode
{
    Navigation,
    Metric
}

public enum MetricTileVariant
{
    Neutral,
    Primary,
    Info,
    Success,
    Warning,
    Danger
}


public partial class FFMetricTile : ContentView
{

    private sealed record MetricTilePalette(
        Color Accent,
        Color Background,
        Color Border,
        Color IconBackground,
        Color Title,
        Color Value,
        Color Subtitle);


    public FFMetricTile()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            UpdateVisualState();
            ApplyResponsiveLayout();
        };
        SizeChanged += (_, _) => ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        var width = Width;
        if (width <= 0)
            return;

        var scale = width >= 260 ? 1d
            : width >= 225 ? 0.94d
            : width >= 200 ? 0.88d
            : width >= 175 ? 0.82d
            : 0.76d;

        if (FindElement<Border>("MetricContainer") is Border container)
            container.Padding = new Thickness(14 * scale, 10 * scale);

        if (FindElement<Grid>("MetricGrid") is Grid metricGrid)
            metricGrid.RowSpacing = 8 * scale;

        if (FindElement<Grid>("MetricHeaderGrid") is Grid headerGrid)
            headerGrid.ColumnSpacing = 4 * scale;

        if (FindElement<Border>("MetricIcon") is Border icon)
        {
            icon.WidthRequest = 32 * scale;
            icon.HeightRequest = 32 * scale;
            icon.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 12 * scale
            };
        }

        if (FindElement<Label>("MetricIconLabel") is Label iconLabel)
            iconLabel.FontSize = 12 * scale;

        if (FindElement<Label>("MetricTitle") is Label title)
            title.FontSize = Math.Max(8, 9 * scale);

        if (FindElement<Label>("MetricValue") is Label value)
            value.FontSize = Math.Max(16, 22 * scale);

        if (FindElement<Border>("MoreBorder") is Border moreBorder)
        {
            moreBorder.Padding = new Thickness(10 * scale, 5 * scale);
            moreBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 12 * scale
            };
        }

        if (FindElement<HorizontalStackLayout>("MoreLayout") is HorizontalStackLayout moreLayout)
            moreLayout.Spacing = 5 * scale;

        if (FindElement<Label>("MoreLabel") is Label moreLabel)
            moreLabel.FontSize = Math.Max(9, 11 * scale);

        if (FindElement<Label>("MoreArrow") is Label moreArrow)
            moreArrow.FontSize = Math.Max(10, 12 * scale);
    }

    private T? FindElement<T>(string automationId) where T : VisualElement
    {
        return FindElementRecursive<T>(this, automationId);
    }

    private static T? FindElementRecursive<T>(Element element, string automationId)
        where T : VisualElement
    {
        if (element is T visual && visual.AutomationId == automationId)
            return visual;

        if (element is IElementController controller)
        {
            foreach (var child in controller.LogicalChildren)
            {
                var found = FindElementRecursive<T>(child, automationId);
                if (found != null)
                    return found;
            }
        }

        return null;
    }



    #region Bindable Properties


    public static readonly BindableProperty DisplayModeProperty =
        BindableProperty.Create(
            nameof(DisplayMode),
            typeof(MetricTileDisplayMode),
            typeof(FFMetricTile),
            MetricTileDisplayMode.Metric,
            propertyChanged: OnVisualStateChanged);


    public MetricTileDisplayMode DisplayMode
    {
        get => (MetricTileDisplayMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }



    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(
            nameof(Variant),
            typeof(MetricTileVariant),
            typeof(FFMetricTile),
            MetricTileVariant.Primary,
            propertyChanged: OnVisualStateChanged);



    public MetricTileVariant Variant
    {
        get => (MetricTileVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
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



    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(FFMetricTile),
            string.Empty);


    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }



    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(
            nameof(Subtitle),
            typeof(string),
            typeof(FFMetricTile),
            string.Empty);


    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }




    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(
            nameof(Value),
            typeof(string),
            typeof(FFMetricTile),
            "0");


    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }




    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(
            nameof(Icon),
            typeof(string),
            typeof(FFMetricTile),
            string.Empty);


    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }




    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(FFMetricTile));


    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }




    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(FFMetricTile));


    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }




    #region Visual Colors


    public static readonly BindableProperty TileBackgroundProperty =
        BindableProperty.Create(
            nameof(TileBackground),
            typeof(Color),
            typeof(FFMetricTile),
            Colors.White);


    public Color TileBackground
    {
        get => (Color)GetValue(TileBackgroundProperty);
        set => SetValue(TileBackgroundProperty, value);
    }




    public static readonly BindableProperty TileBorderProperty =
        BindableProperty.Create(
            nameof(TileBorder),
            typeof(Color),
            typeof(FFMetricTile),
            Colors.Transparent);


    public Color TileBorder
    {
        get => (Color)GetValue(TileBorderProperty);
        set => SetValue(TileBorderProperty, value);
    }





    public static readonly BindableProperty IconBackgroundProperty =
        BindableProperty.Create(
            nameof(IconBackground),
            typeof(Color),
            typeof(FFMetricTile),
            Colors.Transparent);



    public Color IconBackground
    {
        get => (Color)GetValue(IconBackgroundProperty);
        set => SetValue(IconBackgroundProperty, value);
    }





    public static readonly BindableProperty TitleColorProperty =
        BindableProperty.Create(
            nameof(TitleColor),
            typeof(Color),
            typeof(FFMetricTile),
            Colors.Black);


    public Color TitleColor
    {
        get => (Color)GetValue(TitleColorProperty);
        set => SetValue(TitleColorProperty, value);
    }




    public static readonly BindableProperty ValueColorProperty =
        BindableProperty.Create(
            nameof(ValueColor),
            typeof(Color),
            typeof(FFMetricTile),
            Colors.Black);



    public Color ValueColor
    {
        get => (Color)GetValue(ValueColorProperty);
        set => SetValue(ValueColorProperty, value);
    }




    public static readonly BindableProperty SubtitleColorProperty =
        BindableProperty.Create(
            nameof(SubtitleColor),
            typeof(Color),
            typeof(FFMetricTile),
            Colors.Gray);



    public Color SubtitleColor
    {
        get => (Color)GetValue(SubtitleColorProperty);
        set => SetValue(SubtitleColorProperty, value);
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


    #endregion




    public static readonly BindableProperty ShowIconProperty =
        BindableProperty.Create(
            nameof(ShowIcon),
            typeof(bool),
            typeof(FFMetricTile),
            true);


    public bool ShowIcon
    {
        get => (bool)GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }




    public static readonly BindableProperty ShowSubtitleProperty =
        BindableProperty.Create(
            nameof(ShowSubtitle),
            typeof(bool),
            typeof(FFMetricTile),
            true);


    public bool ShowSubtitle
    {
        get => (bool)GetValue(ShowSubtitleProperty);
        set => SetValue(ShowSubtitleProperty, value);
    }




    public static readonly BindableProperty ShowAccentBarProperty =
        BindableProperty.Create(
            nameof(ShowAccentBar),
            typeof(bool),
            typeof(FFMetricTile),
            true);



    public bool ShowAccentBar
    {
        get => (bool)GetValue(ShowAccentBarProperty);
        set => SetValue(ShowAccentBarProperty, value);
    }



    #endregion




    #region Visual State


    private static void OnVisualStateChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        ((FFMetricTile)bindable)
            .UpdateVisualState();
    }



    private void UpdateVisualState()
    {
        var palette = ResolvePalette(Variant);


        if(DisplayMode==MetricTileDisplayMode.Navigation)
        {
            ApplyNavigationPalette(palette);
        }
        else
        {
            ApplyMetricPalette(palette);
        }
    }





    private void ApplyNavigationPalette(MetricTilePalette p)
    {
        AccentColor=
            IsSelected
            ? p.Accent
            : Colors.Transparent;


        TitleColor=
            IsSelected
            ? p.Accent
            : Colors.Gray;


        TileBackground=
            Colors.Transparent;


        TileBorder=
            Colors.Transparent;
    }





    private void ApplyMetricPalette(MetricTilePalette p)
    {
        AccentColor=p.Accent;

        TileBackground=p.Background;

        TileBorder=p.Border;

        IconBackground=p.IconBackground;

        TitleColor=p.Title;

        ValueColor=p.Value;

        SubtitleColor=p.Subtitle;
    }



    #endregion




    #region Palette


    private static MetricTilePalette ResolvePalette(
        MetricTileVariant variant)
    {

        return variant switch
        {

            MetricTileVariant.Primary =>
                new(
                    Color.FromArgb("#2563EB"),
                    Color.FromArgb("#EFF6FF"),
                    Color.FromArgb("#BFDBFE"),
                    Color.FromArgb("#DBEAFE"),
                    Color.FromArgb("#475569"),
                    Color.FromArgb("#0F172A"),
                    Color.FromArgb("#64748B")
                ),



            MetricTileVariant.Info =>
                new(
                    Color.FromArgb("#0284C7"),
                    Color.FromArgb("#F0F9FF"),
                    Color.FromArgb("#BAE6FD"),
                    Color.FromArgb("#E0F2FE"),
                    Color.FromArgb("#475569"),
                    Color.FromArgb("#082F49"),
                    Color.FromArgb("#64748B")
                ),




            MetricTileVariant.Success =>
                new(
                    Color.FromArgb("#16A34A"),
                    Color.FromArgb("#F0FDF4"),
                    Color.FromArgb("#BBF7D0"),
                    Color.FromArgb("#DCFCE7"),
                    Color.FromArgb("#475569"),
                    Color.FromArgb("#14532D"),
                    Color.FromArgb("#64748B")
                ),





            MetricTileVariant.Warning =>
                new(
                    Color.FromArgb("#F59E0B"),
                    Color.FromArgb("#FFFBEB"),
                    Color.FromArgb("#FDE68A"),
                    Color.FromArgb("#FEF3C7"),
                    Color.FromArgb("#475569"),
                    Color.FromArgb("#78350F"),
                    Color.FromArgb("#64748B")
                ),





            MetricTileVariant.Danger =>
                new(
                    Color.FromArgb("#DC2626"),
                    Color.FromArgb("#FEF2F2"),
                    Color.FromArgb("#FECACA"),
                    Color.FromArgb("#FEE2E2"),
                    Color.FromArgb("#475569"),
                    Color.FromArgb("#7F1D1D"),
                    Color.FromArgb("#64748B")
                ),





            _ =>
                new(
                    Color.FromArgb("#64748B"),
                    Color.FromArgb("#F8FAFC"),
                    Color.FromArgb("#E2E8F0"),
                    Color.FromArgb("#F1F5F9"),
                    Color.FromArgb("#475569"),
                    Color.FromArgb("#0F172A"),
                    Color.FromArgb("#64748B")
                )
        };
    }


    #endregion

}