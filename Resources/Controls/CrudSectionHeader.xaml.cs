namespace EHMR.Resources.Controls;

public partial class CrudSectionHeader : ContentView
{
    public CrudSectionHeader() => InitializeComponent();

    public static readonly BindableProperty TitleProperty=BindableProperty.Create(
        nameof(Title), typeof(string), typeof(CrudSectionHeader), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty TextColorProperty=BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(CrudSectionHeader), Colors.Black);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty LineColorProperty=BindableProperty.Create(
        nameof(LineColor), typeof(Color), typeof(CrudSectionHeader), Colors.Black);

    public Color LineColor
    {
        get => (Color)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }
}