using Microsoft.Maui.Controls;

namespace EHMR.Resources.Controls.Cells;

public class HeaderCellView : ContentView
{
    public HeaderCellView()
    {
        var label = new Label
        {
            FontSize=11,
            FontAttributes=FontAttributes.Bold,
            CharacterSpacing=0.8,
            VerticalOptions=LayoutOptions.Center
        };

        label.SetBinding(Label.TextProperty, new Binding(nameof(Text), source: this));
        label.SetBinding(Label.TextColorProperty, new Binding(nameof(TextColor), source: this));
        label.SetBinding(Label.HorizontalTextAlignmentProperty, new Binding(nameof(Alignment), source: this));

        Content=label;
    }

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(HeaderCellView));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(HeaderCellView), Colors.Gray);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty AlignmentProperty =
        BindableProperty.Create(nameof(Alignment), typeof(TextAlignment), typeof(HeaderCellView), TextAlignment.Start);

    public TextAlignment Alignment
    {
        get => (TextAlignment)GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }
}