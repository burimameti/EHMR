using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace EHMR.Resources.Controls;

public partial class FFButton : ContentView
{
    private Color? _hoverRestoreBackground;
    private Color? _hoverRestoreText;

    public FFButton()
    {
        InitializeComponent();
        ApplyKind();
    }

    // ═══════════════════════════════════════════════════════════ //
    // BINDABLE PROPERTIES                                         //
    // ═══════════════════════════════════════════════════════════ //

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FFButton), string.Empty);

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
        BindableProperty.Create(nameof(HeightRequestEx), typeof(double), typeof(FFButton), 42d,
            propertyChanged: (b, _, v) => ((FFButton)b).Container.MinimumHeightRequest=(double)v);

    public static readonly BindableProperty WidthRequestExProperty =
        BindableProperty.Create(nameof(WidthRequestEx), typeof(double), typeof(FFButton), 180d,
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
        switch(ButtonKind)
        {
            // Main action: Save, Create, Confirm
            case FFButtonKind.Primary:
                BackgroundColorEx=ResolveColorResource("SidebarActiveBg", "#4DD9C7");
                TextColorEx=Colors.White;
                BorderColor=ResolveColorResource("SidebarActiveBg", "#4DD9C7");
                BorderThickness=0;
                break;

            // Supporting action: Edit, Preview, Back
            case FFButtonKind.Secondary:
                BackgroundColorEx=Color.FromArgb("#4A5863");
                TextColorEx=Colors.White;
                BorderColor=Color.FromArgb("#5A5863");
                BorderThickness=0;
                break;

            // Positive clinical action: Complete, Approve
            case FFButtonKind.Green:
                BackgroundColorEx=Color.FromArgb("#15803D");
                TextColorEx=Colors.White;
                BorderColor=Color.FromArgb("#15803D");
                BorderThickness=0;
                break;

            // Destructive action: Delete, Cancel therapy
            case FFButtonKind.Danger:
                BackgroundColorEx=Color.FromArgb("#B42318");
                TextColorEx=Colors.White;
                BorderColor=Color.FromArgb("#B42318");
                BorderThickness=0;
                break;

            // Quiet action: Close, Clear filters
            case FFButtonKind.Ghost:
                BackgroundColorEx=Colors.Transparent;
                TextColorEx=Color.FromArgb("#1F2933");
                BorderColor=Color.FromArgb("#64748B");
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

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if(!IsEnabled)
            return;

        _hoverRestoreBackground=BackgroundColorEx;
        _hoverRestoreText=TextColorEx;

        // Consistent Spark hover: dark sidebar/nav tone.
        BackgroundColorEx=Color.FromArgb("#293441");
        TextColorEx=Colors.White;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if(_hoverRestoreBackground is not null)
            BackgroundColorEx=_hoverRestoreBackground;

        if(_hoverRestoreText is not null)
            TextColorEx=_hoverRestoreText;

        _hoverRestoreBackground=null;
        _hoverRestoreText=null;
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