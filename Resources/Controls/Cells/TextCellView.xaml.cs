using Microsoft.Maui.Controls;

namespace EHMR.Resources.Controls.Cells;
public static class TextCellRenderer
{
    public static View Create(
        string text,
        double fontSize,
        Color color,
        FontAttributes fontAttributes)
    {
        return new Label
        {
            Text=text,
            FontSize=fontSize,
            TextColor=color,
            FontAttributes=fontAttributes,
            VerticalOptions=LayoutOptions.Center,
            LineBreakMode=LineBreakMode.TailTruncation
        };
    }
}
public class TextCellView : ContentView
{
    public TextCellView()
    {
        var label = new Label
        {
            VerticalOptions=LayoutOptions.Center,
            LineBreakMode=LineBreakMode.TailTruncation
        };

        label.SetBinding(Label.TextProperty, new Binding(nameof(Text), source: this));
        label.SetBinding(Label.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        label.SetBinding(Label.TextColorProperty, new Binding(nameof(TextColor), source: this));
        label.SetBinding(Label.FontAttributesProperty, new Binding(nameof(FontAttributes), source: this));

        Content=label;
    }

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TextCellView), "");

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(TextCellView), 13d);

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(TextCellView), Colors.Black);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty FontAttributesProperty =
        BindableProperty.Create(nameof(FontAttributes), typeof(FontAttributes), typeof(TextCellView), FontAttributes.None);

    public FontAttributes FontAttributes
    {
        get => (FontAttributes)GetValue(FontAttributesProperty);
        set => SetValue(FontAttributesProperty, value);
    }
}