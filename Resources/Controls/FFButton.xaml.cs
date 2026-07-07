using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace EHMR.Resources.Controls;

public partial class FFButton : ContentView
{
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

    public static readonly BindableProperty ContentPaddingProperty =
        BindableProperty.Create(nameof(ContentPadding), typeof(Thickness), typeof(FFButton), new Thickness(20, 0),
            propertyChanged: (b, _, v) => ((FFButton)b).Container.Padding=(Thickness)v);

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(float), typeof(FFButton), 10f);

    public static readonly BindableProperty FontSizeExProperty =
        BindableProperty.Create(nameof(FontSizeEx), typeof(double), typeof(FFButton), 13d);

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
            case FFButtonKind.Primary:
                BackgroundColorEx=Color.FromArgb("#67D0DD");
                TextColorEx=Colors.White;
                BorderColor=Colors.Transparent;
                BorderThickness=0;
                break;

            case FFButtonKind.Secondary:
                BackgroundColorEx=Color.FromArgb("#90A1AD");
                TextColorEx=Color.FromArgb("#334155");
                BorderColor=Color.FromArgb("#E2E8F0");
                BorderThickness=1;
                break;
            case FFButtonKind.Green:
                BackgroundColorEx=Color.FromArgb("#DAF6BA");
                TextColorEx=Color.FromArgb("#334155");
                BorderColor=Color.FromArgb("#E2E8F0");
                BorderThickness=1;
                break;
            case FFButtonKind.Danger:
                BackgroundColorEx=Color.FromArgb("#EF4444");
                TextColorEx=Colors.White;
                BorderColor=Colors.Transparent;
                BorderThickness=0;
                break;

            case FFButtonKind.Ghost:
                BackgroundColorEx=Colors.Transparent;
                TextColorEx=Color.FromArgb("#B4CAD9");
                BorderColor=Color.FromArgb("#E2E8F0");
                BorderThickness=1;
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════ //
    // TAP HANDLER                                                 //
    // ═══════════════════════════════════════════════════════════ //

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
    Ghost,Green
}