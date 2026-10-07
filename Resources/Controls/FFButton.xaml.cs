using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System.Windows.Input;

namespace EHMR.Resources.Controls;

public partial class FFButton : ContentView
{
    private double _lastWidth = -1;

    public FFButton()
    {
        InitializeComponent();
        ApplyKind();

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => ApplyHover(true);
        pointer.PointerExited += (_, _) => ApplyHover(false);
        Container.GestureRecognizers.Add(pointer);
    }

    private void ApplyHover(bool isHover)
    {
        if (!IsEnabled || IsLoading)
            return;

        if (isHover)
        {
            if(string.Equals(Text, "Откажи", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(Text, "Cancel", StringComparison.OrdinalIgnoreCase))
            {
                BackgroundColorEx=Color.FromArgb("#991B1B");
                return;
            }

            if(IsBlue)
            {
                BackgroundColorEx=Color.FromArgb("#258F84");
                return;
            }

            BackgroundColorEx = ButtonKind switch
            {
                FFButtonKind.Primary => Color.FromArgb("#258F84"),
                FFButtonKind.Secondary => Color.FromArgb("#1E293B"),
                FFButtonKind.Green => Color.FromArgb("#258F84"),
                FFButtonKind.Danger => Color.FromArgb("#991B1B"),
                FFButtonKind.Ghost => Color.FromArgb("#E2E8F0"),
                _ => BackgroundColorEx
            };
        }
        else
        {
            ApplyKind();
        }
    }

    private void OnButtonSizeChanged(object? sender, EventArgs e)
    {
        var width = Width;
        if (width <= 0 || Math.Abs(width - _lastWidth) < 2)
            return;

        _lastWidth = width;

        // Keep the normal desktop appearance at comfortable widths,
        // but reduce internal geometry when the button is constrained.
        var scale = width >= 180 ? 1d
            : width >= 145 ? 0.92d
            : width >= 115 ? 0.84d
            : 0.76d;

        var horizontalPadding = Math.Max(8, 20 * scale);
        Container.Padding = new Thickness(horizontalPadding, 0);
        Container.MinimumHeightRequest = Math.Max(32, HeightRequestEx * scale);
        Container.StrokeShape = new RoundRectangle
        {
            CornerRadius = CornerRadius
        };

        if (width < 145)
        {
            FontSizeEx = Math.Max(11, 13 * scale);
        }
    }

    // ═══════════════════════════════════════════════════════════ //
    // BINDABLE PROPERTIES                                         //
    // ═══════════════════════════════════════════════════════════ //

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FFButton), string.Empty, propertyChanged: (b, _, _) => ((FFButton)b).ApplyKind());

    public static readonly BindableProperty IsBlueProperty =
        BindableProperty.Create(nameof(IsBlue), typeof(bool), typeof(FFButton), false,
            propertyChanged: (b, _, _) => ((FFButton)b).ApplyKind());

    public static readonly BindableProperty ButtonKindProperty =
        BindableProperty.Create(nameof(ButtonKind), typeof(FFButtonKind), typeof(FFButton), FFButtonKind.Primary,
            propertyChanged: (b, _, _) => ((FFButton)b).ApplyKind());

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(FFButton), null);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(FFButton), null);

    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(FFButton), false);

    public static readonly BindableProperty HeightRequestExProperty =
        BindableProperty.Create(nameof(HeightRequestEx), typeof(double), typeof(FFButton), 40d,
            propertyChanged: (b, _, v) => ((FFButton)b).Container.MinimumHeightRequest=(double)v);

    public static readonly BindableProperty WidthRequestExProperty =
        BindableProperty.Create(nameof(WidthRequestEx), typeof(double), typeof(FFButton), -1d,
            propertyChanged: (b, _, v) => ((FFButton)b).Container.MinimumWidthRequest=(double)v);

    public static readonly BindableProperty ContentPaddingProperty =
        BindableProperty.Create(nameof(ContentPadding), typeof(Thickness), typeof(FFButton), new Thickness(20, 0),
            propertyChanged: (b, _, v) => ((FFButton)b).Container.Padding=(Thickness)v);

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(float), typeof(FFButton), 10f);

    public static readonly BindableProperty FontSizeExProperty =
        BindableProperty.Create(nameof(FontSizeEx), typeof(double), typeof(FFButton), 13d);

    public static readonly BindableProperty FontFamilyProperty =
        BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(FFButton), null);

    public static readonly BindableProperty FontAttributesExProperty =
        BindableProperty.Create(nameof(FontAttributesEx), typeof(FontAttributes), typeof(FFButton), FontAttributes.Bold);

    public static readonly BindableProperty HorizontalOptionsExProperty =
        BindableProperty.Create(nameof(HorizontalOptionsEx), typeof(LayoutOptions), typeof(FFButton), LayoutOptions.Center,
            propertyChanged: (b, _, v) => ((FFButton)b).Container.HorizontalOptions=(LayoutOptions)v);

    // Computed color props — driven by ApplyKind()
    public static readonly BindableProperty BackgroundColorExProperty =
        BindableProperty.Create(nameof(BackgroundColorEx), typeof(Color), typeof(FFButton), Colors.Transparent);

    public static readonly BindableProperty TextColorExProperty =
        BindableProperty.Create(nameof(TextColorEx), typeof(Color), typeof(FFButton), Colors.White);

    public static readonly BindableProperty BorderColorProperty =
        BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(FFButton), Colors.Transparent);

    public static readonly BindableProperty BorderThicknessProperty =
        BindableProperty.Create(nameof(BorderThickness), typeof(double), typeof(FFButton), 0d);

    // ═══════════════════════════════════════════════════════════ //
    // CLR PROPERTIES                                              //
    // ═══════════════════════════════════════════════════════════ //

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsBlue
    {
        get => (bool)GetValue(IsBlueProperty);
        set => SetValue(IsBlueProperty, value);
    }

    public FFButtonKind ButtonKind
    {
        get => (FFButtonKind)GetValue(ButtonKindProperty);
        set => SetValue(ButtonKindProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public double HeightRequestEx
    {
        get => (double)GetValue(HeightRequestExProperty);
        set => SetValue(HeightRequestExProperty, value);
    }

    public double WidthRequestEx
    {
        get => (double)GetValue(WidthRequestExProperty);
        set => SetValue(WidthRequestExProperty, value);
    }

    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public float CornerRadius
    {
        get => (float)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double FontSizeEx
    {
        get => (double)GetValue(FontSizeExProperty);
        set => SetValue(FontSizeExProperty, value);
    }

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public FontAttributes FontAttributesEx
    {
        get => (FontAttributes)GetValue(FontAttributesExProperty);
        set => SetValue(FontAttributesExProperty, value);
    }

    public LayoutOptions HorizontalOptionsEx
    {
        get => (LayoutOptions)GetValue(HorizontalOptionsExProperty);
        set => SetValue(HorizontalOptionsExProperty, value);
    }

    public Color BackgroundColorEx
    {
        get => (Color)GetValue(BackgroundColorExProperty);
        set => SetValue(BackgroundColorExProperty, value);
    }

    public Color TextColorEx
    {
        get => (Color)GetValue(TextColorExProperty);
        set => SetValue(TextColorExProperty, value);
    }

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    // ═══════════════════════════════════════════════════════════ //
    // KIND → VISUAL TOKENS                                        //
    // ═══════════════════════════════════════════════════════════ //

    private void ApplyKind()
    {
        if(string.Equals(Text, "Откажи", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(Text, "Cancel", StringComparison.OrdinalIgnoreCase))
        {
            BackgroundColorEx=Color.FromArgb("#B42318");
            TextColorEx=Colors.White;
            BorderColor=Color.FromArgb("#B42318");
            BorderThickness=0;
            return;
        }

        if(IsBlue)
        {
            BackgroundColorEx=Color.FromArgb("#32B9AA");
            TextColorEx=Colors.White;
            BorderColor=Color.FromArgb("#2563EB");
            BorderThickness=0;
            return;
        }

        switch(ButtonKind)
        {
            // Main action: Save, Create, Confirm
            case FFButtonKind.Primary:
                BackgroundColorEx=Color.FromArgb("#32B9AA");
                TextColorEx=Colors.White;
                BorderColor=Color.FromArgb("#DC2626");
                BorderThickness=0;
                break;

            // Supporting action: Edit, Preview, Back
            case FFButtonKind.Secondary:
                BackgroundColorEx=Color.FromArgb("#64748B");
                TextColorEx=Colors.White;
                BorderColor=Color.FromArgb("#B91C1C");
                BorderThickness=0;
                break;

            // Positive clinical action: Complete, Approve
            case FFButtonKind.Green:
                BackgroundColorEx=Color.FromArgb("#32B9AA");
                TextColorEx=Colors.White;
                BorderColor=Color.FromArgb("#15803D");
                BorderThickness=2;
                break;

            // Destructive action: Delete, Cancel therapy
            case FFButtonKind.Danger:
                BackgroundColorEx=Color.FromArgb("#B42318");
                TextColorEx=Colors.White;
                BorderColor=Color.FromArgb("#B42318");
                BorderThickness=2;
                break;

            // Quiet action: Close, Clear filters
            case FFButtonKind.Ghost:
                BackgroundColorEx=Color.FromArgb("#F1F5F9");
                TextColorEx=Color.FromArgb("#475569");
                BorderColor=Color.FromArgb("#CBD5E1");
                BorderThickness=1;
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════ //
    // TAP HANDLER                                                 //
    // ═══════════════════════════════════════════════════════════ //

    private static Color ResolveColorResource(string key, string fallback)
    {
        if(Application.Current?.Resources.TryGetValue(key, out var value)==true&&value is Color color)
            return color;
        return Color.FromArgb(fallback);
    }

    private async void OnTapped(object sender, TappedEventArgs e)
    {
        if(IsLoading) return;
        if(!IsEnabled) return;

        // Press animation
        await Container.ScaleTo(0.96, 80, Easing.CubicOut);
        await Container.ScaleTo(1.00, 80, Easing.CubicIn);

        // Execute command
        if(Command?.CanExecute(CommandParameter)==true)
            Command.Execute(CommandParameter);

        Clicked?.Invoke(this, EventArgs.Empty);
    }

    // ═══════════════════════════════════════════════════════════ //
    // PUBLIC EVENTS                                               //
    // ═══════════════════════════════════════════════════════════ //

    public event EventHandler? Clicked;
}

// ═══════════════════════════════════════════════════════════ //
// ENUM                                                        //
// ═══════════════════════════════════════════════════════════ //

public enum FFButtonKind
{
    Primary,
    Secondary,
    Danger,
    Ghost,
    Green
}