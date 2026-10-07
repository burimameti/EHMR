using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Controls.Cells;

public class BadgeCellView : ContentView
{
    public BadgeCellView()
    {
        var label = new Label
        {
            HorizontalOptions=LayoutOptions.Center,
            VerticalOptions=LayoutOptions.Center,
            FontSize=11,
            FontAttributes=FontAttributes.Bold
        };

        var border = new Border
        {
            Padding=new Thickness(10, 4),
            StrokeThickness=0,
            Content=label
        };

        border.SetBinding(Border.BackgroundColorProperty, new Binding(nameof(BackgroundColor), source: this));
        label.SetBinding(Label.TextProperty, new Binding(nameof(Text), source: this));
        label.SetBinding(Label.TextColorProperty, new Binding(nameof(TextColor), source: this));

        border.StrokeShape=new Rectangle();

        Content=border;
    }

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(BadgeCellView));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty BackgroundColorProperty =
        BindableProperty.Create(nameof(BackgroundColor), typeof(Color), typeof(BadgeCellView), Colors.Gray);

    public Color BackgroundColor
    {
        get => (Color)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(BadgeCellView), Colors.White);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }
}