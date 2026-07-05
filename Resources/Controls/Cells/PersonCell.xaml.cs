using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Controls.Cells;

public class PersonCellView : ContentView
{
    public PersonCellView()
    {
        var avatar = new Border
        {
            WidthRequest=34,
            HeightRequest=34,
            StrokeThickness=0,
            StrokeShape=new RoundRectangle { CornerRadius=17 },
            BackgroundColor=Color.FromArgb("#2FB6DE"),
            Content=new Label
            {
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center,
                TextColor=Colors.White,
                FontAttributes=FontAttributes.Bold
            }
        };

        avatar.SetBinding(Label.TextProperty, new Binding(nameof(Initials), source: this));

        var name = new Label
        {
            FontAttributes=FontAttributes.Bold,
            FontSize=13
        };
        name.SetBinding(Label.TextProperty, new Binding(nameof(Name), source: this));

        var subtitle = new Label
        {
            FontSize=11,
            Opacity=0.7
        };
        subtitle.SetBinding(Label.TextProperty, new Binding(nameof(Subtitle), source: this));

        var stack = new VerticalStackLayout
        {
            Spacing=2,
            Children={ name, subtitle }
        };

        var grid = new Grid
        {
            ColumnDefinitions=
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing=10
        };

        grid.Add(avatar);
        Grid.SetColumn(avatar, 0);

        grid.Add(stack);
        Grid.SetColumn(stack, 1);

        Content=grid;
    }

    public static readonly BindableProperty NameProperty =
        BindableProperty.Create(nameof(Name), typeof(string), typeof(PersonCellView));

    public string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(PersonCellView));

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly BindableProperty InitialsProperty =
        BindableProperty.Create(nameof(Initials), typeof(string), typeof(PersonCellView));

    public string Initials
    {
        get => (string)GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }
}