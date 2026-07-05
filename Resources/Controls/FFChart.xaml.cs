using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Controls;
public class ChartPoint
{
    public string Label { get; set; } = "";
    public double Value
    {
        get; set;
    }
}

public partial class FFChart : ContentView
{
    public static readonly BindableProperty ValuesProperty =
        BindableProperty.Create(
            nameof(Values),
            typeof(IEnumerable<ChartPoint>),
            typeof(FFChart),
            null,
            propertyChanged: OnValuesChanged);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(FFChart),
            string.Empty,
            propertyChanged: OnTitleChanged);

    public IEnumerable<ChartPoint>? Values
    {
        get => (IEnumerable<ChartPoint>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public FFChart()
    {
        InitializeComponent();

        ChartView.Drawable=new ChartDrawable(this);
    }

    private static void OnValuesChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is FFChart chart)
            chart.ChartView.Invalidate();
    }

    private static void OnTitleChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is FFChart chart)
            chart.TitleLabel.Text=newValue?.ToString()??"";
    }

    private class ChartDrawable : IDrawable
    {
        private readonly FFChart _owner;

        public ChartDrawable(FFChart owner)
        {
            _owner=owner;
        }

        public void Draw(ICanvas canvas, RectF rect)
        {
            var points = _owner.Values?.ToList();

            if(points==null||points.Count<2)
                return;

            double max = points.Max(x => x.Value);

            if(max<=0)
                max=1;

            float left = 16;
            float right = 16;
            float top = 16;
            float bottom = 28;

            float width = rect.Width-left-right;
            float height = rect.Height-top-bottom;

            float step = width/(points.Count-1);

            canvas.StrokeColor=Colors.DodgerBlue;
            canvas.StrokeSize=3;

            PathF path = new();

            for(int i = 0; i<points.Count; i++)
            {
                float x = left+i*step;

                float y = top+height-
                    (float)(points[i].Value/max*height);

                if(i==0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }

            canvas.DrawPath(path);

            canvas.FillColor=Colors.DodgerBlue;

            for(int i = 0; i<points.Count; i++)
            {
                float x = left+i*step;

                float y = top+height-
                    (float)(points[i].Value/max*height);

                canvas.FillCircle(x, y, 4);

                canvas.FontSize=10;
                canvas.FontColor=Colors.Gray;

                canvas.DrawString(
                    points[i].Label,
                    x-15,
                    rect.Height-bottom+6,
                    30,
                    16,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Top);
            }
        }
    }
}