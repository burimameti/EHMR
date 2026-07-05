using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace EHMR.Resources.Controls.Cells;

public class ActionCellView : ContentView
{
    public ActionCellView()
    {
        var stack = new HorizontalStackLayout
        {
            Spacing=6,
            HorizontalOptions=LayoutOptions.Center,
            VerticalOptions=LayoutOptions.Center
        };

        stack.SetBinding(BindableLayout.ItemsSourceProperty, new Binding(nameof(Actions), source: this));

        BindableLayout.SetItemTemplate(stack, new DataTemplate(() =>
        {
            var border = new Border
            {
                WidthRequest=34,
                HeightRequest=34,
                StrokeThickness=0,
                StrokeShape=new RoundRectangle { CornerRadius=17 }
            };

            var label = new Label
            {
                FontFamily="FASolid",
                FontSize=13,
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center
            };

            label.SetBinding(Label.TextProperty, "Glyph");

            border.SetBinding(Border.BackgroundColorProperty, "BackgroundColor");
            label.SetBinding(Label.TextColorProperty, "ForegroundColor");

            border.Content=label;

            var tap = new TapGestureRecognizer();
            tap.SetBinding(TapGestureRecognizer.CommandProperty, "Command");
            tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, "CommandParameter");

            border.GestureRecognizers.Add(tap);

            return border;
        }));

        Content=stack;
    }

    public static readonly BindableProperty ActionsProperty =
        BindableProperty.Create(nameof(Actions), typeof(object), typeof(ActionCellView));

    public object Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }
}