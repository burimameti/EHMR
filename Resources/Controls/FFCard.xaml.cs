using Microsoft.Maui.Controls.Shapes;

namespace EHMR.Resources.Controls;

public partial class FFCard : Border
{
    public FFCard()
    {
        InitializeComponent();
        ApplyStyle();
    }

    // ================= VARIANT =================
    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(
            nameof(Variant),
            typeof(FFCardVariant),
            typeof(FFCard),
            FFCardVariant.Default,
            propertyChanged: OnAppearanceChanged);

    public FFCardVariant Variant
    {
        get => (FFCardVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    // ================= PADDING =================
    public static readonly BindableProperty CardPaddingProperty =
        BindableProperty.Create(
            nameof(CardPadding),
            typeof(Thickness),
            typeof(FFCard),
            new Thickness(16),
            propertyChanged: OnAppearanceChanged);

    public Thickness CardPadding
    {
        get => (Thickness)GetValue(CardPaddingProperty);
        set => SetValue(CardPaddingProperty, value);
    }

    // ================= CORNER =================
    public static readonly BindableProperty CardCornerRadiusProperty =
        BindableProperty.Create(
            nameof(CardCornerRadius),
            typeof(float),
            typeof(FFCard),
            16f,
            propertyChanged: OnAppearanceChanged);

    public float CardCornerRadius
    {
        get => (float)GetValue(CardCornerRadiusProperty);
        set => SetValue(CardCornerRadiusProperty, value);
    }

    // ================= SHADOW =================
    public static readonly BindableProperty HasShadowProperty =
        BindableProperty.Create(
            nameof(HasShadow),
            typeof(bool),
            typeof(FFCard),
            true,
            propertyChanged: OnAppearanceChanged);

    public bool HasShadow
    {
        get => (bool)GetValue(HasShadowProperty);
        set => SetValue(HasShadowProperty, value);
    }

    // ================= PROPERTY CHANGE =================
    private static void OnAppearanceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if(bindable is FFCard card)
            card.ApplyStyle();
    }

    // ================= CORE STYLE ENGINE =================
    private void ApplyStyle()
    {
        Padding=CardPadding;
        Margin=new Thickness(0, 6);

        StrokeShape=new RoundRectangle
        {
            CornerRadius=new CornerRadius(CardCornerRadius)
        };

        ApplyVariantStyle();

        if(!HasShadow)
            Shadow=null;
    }

    // ================= VARIANT SWITCH =================
    private void ApplyVariantStyle()
    {
        switch(Variant)
        {
            case FFCardVariant.Elevated:
                ApplyElevated();
                break;

            case FFCardVariant.Flat:
                ApplyFlat();
                break;

            case FFCardVariant.Outlined:
                ApplyOutlined();
                break;

            case FFCardVariant.Soft:
                ApplySoft();
                break;

            default:
                ApplyDefault();
                break;
        }
    }

    // ================= VARIANTS =================
    private void ApplyDefault()
    {
        StrokeThickness=1;
        Background=null;
        SetDynamicResource(BackgroundProperty, "SparkContentBackground");
        SetDynamicResource(StrokeProperty, "SparkCardBorder");

        Shadow=CreateShadow(0.06f, 0, 4, 16);
    }

    private void ApplyElevated()
    {
        StrokeThickness=0;
        SetDynamicResource(BackgroundProperty, "SparkContentBackground");
        SetDynamicResource(StrokeProperty, "Transparent");

        Shadow=CreateShadow(0.08f, 0, 8, 22);
    }

    private void ApplyFlat()
    {
        StrokeThickness=0;
        SetDynamicResource(BackgroundProperty, "SparkContentBackground");
        SetDynamicResource(StrokeProperty, "Transparent");

        Shadow=null;
    }

    private void ApplyOutlined()
    {
        StrokeThickness=1;
        SetDynamicResource(BackgroundProperty, "SparkContentBackground");
        SetDynamicResource(StrokeProperty, "SparkCardBorder");

        Shadow=null;
    }

    private void ApplySoft()
    {
        StrokeThickness=1;
        SetDynamicResource(BackgroundProperty, "SparkBackgroundAlt");
        SetDynamicResource(StrokeProperty, "SparkCardBorder");

        Shadow=CreateShadow(0.04f, 0, 2, 10);
    }

    // ================= SHADOW FACTORY =================
    private static Shadow CreateShadow(float opacity, double x, double y, float radius)
    {
        return new Shadow
        {
            Brush=Colors.Black,
            Opacity=opacity,
            Offset=new Point(x, y),
            Radius=radius
        };
    }
}