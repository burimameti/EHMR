using System.Windows.Input;
namespace EHMR.Resources.Controls;
public partial class FFMetricTile : ContentView
{
    public FFMetricTile()
    {
        InitializeComponent();
    }
    public static readonly BindableProperty IsSelectedProperty =
    BindableProperty.Create(
        nameof(IsSelected),
        typeof(bool),
        typeof(FFMetricTile),
        false,
        propertyChanged: OnSelectedChanged);

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
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

    private static void OnSelectedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tile = (FFMetricTile)bindable;

        if((bool)newValue)
        {
            tile.TitleColor=Color.FromArgb("#4CB7E8");
            tile.AccentColor=Color.FromArgb("#4CB7E8");
        }
        else
        {
            tile.TitleColor=Color.FromArgb("#8D98A5");
            tile.AccentColor=Colors.Transparent;
        }
    }
}