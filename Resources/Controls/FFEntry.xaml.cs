using Microsoft.Maui.Controls;

namespace EHMR.Resources.Controls;

public partial class FFEntry : ContentView
{
    public FFEntry()
    {
        InitializeComponent();
    }

    // ═══════════════════════════════════════════════════════════ //
    // BINDABLE PROPERTIES                                         //
    // ═══════════════════════════════════════════════════════════ //

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(FFEntry), string.Empty);

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FFEntry), string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FFEntry), string.Empty);

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(FFEntry), string.Empty,
            propertyChanged: (b, _, _) => ((FFEntry)b).OnPropertyChanged(nameof(HasIcon)));

    public static readonly BindableProperty KeyboardProperty =
        BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(FFEntry), Keyboard.Default);

    public static readonly BindableProperty ReturnTypeProperty =
        BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(FFEntry), ReturnType.Default);

    public static readonly BindableProperty IsPasswordProperty =
        BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(FFEntry), false);

    public static readonly BindableProperty MaxLengthProperty =
        BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(FFEntry), int.MaxValue);

    public static readonly BindableProperty ErrorProperty =
        BindableProperty.Create(nameof(Error), typeof(string), typeof(FFEntry), string.Empty,
            propertyChanged: (b, _, _) => ((FFEntry)b).OnPropertyChanged(nameof(HasError)));

    public static readonly BindableProperty RightContentProperty =
        BindableProperty.Create(nameof(RightContent), typeof(View), typeof(FFEntry), null,
            propertyChanged: (b, _, _) => ((FFEntry)b).OnPropertyChanged(nameof(HasRightContent)));

    public static readonly BindableProperty IsPointerOverProperty =
        BindableProperty.Create(nameof(IsPointerOver), typeof(bool), typeof(FFEntry), false);

    public static readonly BindableProperty IsInputFocusedProperty =
        BindableProperty.Create(nameof(IsInputFocused), typeof(bool), typeof(FFEntry), false);

    // ═══════════════════════════════════════════════════════════ //
    // CLR PROPERTIES                                              //
    // ═══════════════════════════════════════════════════════════ //

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Keyboard Keyboard
    {
        get => (Keyboard)GetValue(KeyboardProperty);
        set => SetValue(KeyboardProperty, value);
    }

    public ReturnType ReturnType
    {
        get => (ReturnType)GetValue(ReturnTypeProperty);
        set => SetValue(ReturnTypeProperty, value);
    }

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public string Error
    {
        get => (string)GetValue(ErrorProperty);
        set => SetValue(ErrorProperty, value);
    }

    public View RightContent
    {
        get => (View)GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public bool IsPointerOver
    {
        get => (bool)GetValue(IsPointerOverProperty);
        private set => SetValue(IsPointerOverProperty, value);
    }

    public bool IsInputFocused
    {
        get => (bool)GetValue(IsInputFocusedProperty);
        private set => SetValue(IsInputFocusedProperty, value);
    }

    // ═══════════════════════════════════════════════════════════ //
    // COMPUTED                                                    //
    // ═══════════════════════════════════════════════════════════ //

    public bool HasLabel => !string.IsNullOrEmpty(Label);
    public bool HasIcon => !string.IsNullOrEmpty(Icon);
    public bool HasError => !string.IsNullOrEmpty(Error);
    public bool HasRightContent => RightContent is not null;

    // ═══════════════════════════════════════════════════════════ //
    // EVENTS                                                      //
    // ═══════════════════════════════════════════════════════════ //

    private void OnEntryFocused(object sender, FocusEventArgs e)
    {
        IsInputFocused=true;
        // Clear error when user starts typing
        if(HasError) Error=string.Empty;
    }

    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        IsInputFocused=false;
    }

    private void OnPointerEntered(object sender, PointerEventArgs e) => IsPointerOver=true;

    private void OnPointerExited(object sender, PointerEventArgs e) => IsPointerOver=false;

    // ═══════════════════════════════════════════════════════════ //
    // PUBLIC API                                                  //
    // ═══════════════════════════════════════════════════════════ //

    /// <summary>Programmatically focus the inner entry.</summary>
    public new void Focus() => PART_Entry.Focus();

    /// <summary>Set an error message — triggers red border state.</summary>
    public void SetError(string message) => Error=message;

    /// <summary>Clear error state.</summary>
    public void ClearError() => Error=string.Empty;
}