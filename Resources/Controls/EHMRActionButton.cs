using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EHMR.Resources.Controls
{
    public class EHMRActionButton : Border
    {
        public EHMRActionButton(string glyph, ICommand command, object parameter)
        {
            var border = new Border{
            WidthRequest=30,
            HeightRequest=30,
            Padding=new Thickness(8, 4),

            StrokeThickness=0,
            BackgroundColor=Color.FromArgb("#2A2A2A"),
            
            StrokeShape=new RoundRectangle
            {
                CornerRadius=14
            } };
            if(glyph.Contains("👁"))
            {
            glyph="👁 Преглед";
            }else
            {
                glyph=glyph+" Промени";
            }
            border.Content=new Label
            {
                Text=glyph,
                FontSize=14,
                TextColor=Colors.White,
                HorizontalTextAlignment=TextAlignment.Center,
                VerticalTextAlignment=TextAlignment.Center
            };

            border.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command=command,
                CommandParameter=parameter
            });
        }
    }
}
