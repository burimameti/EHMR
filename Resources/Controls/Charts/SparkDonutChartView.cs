using System.Collections.ObjectModel;
using System.Collections.Specialized;
using EHMR.Backups.Models;
using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Controls.Charts;

/// <summary>
/// Simple ring/donut chart. Bind <see cref="Segments"/> to an
/// ObservableCollection&lt;DonutSegment&gt; and <see cref="CenterText"/> to the
/// text shown in the middle (e.g. "83%"). Shows <see cref="EmptyText"/> when
/// there's no data or all values are zero.
/// </summary>
public partial class SparkDonutChartView : ContentView
{
    readonly GraphicsView _graphicsView;
    readonly Label _centerLabel;
    readonly Label _emptyLabel;
    readonly DonutDrawable _drawable;

    public static readonly BindableProperty SegmentsProperty = BindableProperty.Create(
        nameof(Segments),
        typeof(ObservableCollection<DonutSegment>),
        typeof(SparkDonutChartView),
        propertyChanged: OnSegmentsChanged);

    public ObservableCollection<DonutSegment> Segments
    {
        get => (ObservableCollection<DonutSegment>)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public static readonly BindableProperty CenterTextProperty = BindableProperty.Create(
        nameof(CenterText),
        typeof(string),
        typeof(SparkDonutChartView),
        string.Empty,
        propertyChanged: (b, _, n) => ((SparkDonutChartView)b)._centerLabel.Text=(string)n);

    public string CenterText
    {
        get => (string)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    public static readonly BindableProperty EmptyTextProperty = BindableProperty.Create(
        nameof(EmptyText),
        typeof(string),
        typeof(SparkDonutChartView),
        "No data",
        propertyChanged: (b, _, _) => ((SparkDonutChartView)b).Refresh());

    public string EmptyText
    {
        get => (string)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    public static readonly BindableProperty StrokeWidthProperty = BindableProperty.Create(
        nameof(StrokeWidth),
        typeof(float),
        typeof(SparkDonutChartView),
        18f,
        propertyChanged: (b, _, _) => ((SparkDonutChartView)b).Refresh());

    public float StrokeWidth
    {
        get => (float)GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public SparkDonutChartView()
    {
        _drawable=new DonutDrawable(this);
        _graphicsView=new GraphicsView { Drawable=_drawable };

        _centerLabel=new Label
        {
            HorizontalOptions=LayoutOptions.Center,
            VerticalOptions=LayoutOptions.Center,
            FontAttributes=FontAttributes.Bold,
            FontSize=16,
            TextColor=Color.FromArgb("#1F2A37")
        };

        _emptyLabel=new Label
        {
            HorizontalOptions=LayoutOptions.Center,
            VerticalOptions=LayoutOptions.Center,
            FontSize=12,
            TextColor=Color.FromArgb("#8E97A6"),
            IsVisible=false
        };

        var grid = new Grid();
        grid.Add(_graphicsView);
        grid.Add(_centerLabel);
        grid.Add(_emptyLabel);
        Content=grid;
    }

    static void OnSegmentsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (SparkDonutChartView)bindable;

        if(oldValue is ObservableCollection<DonutSegment> oldCollection)
            oldCollection.CollectionChanged-=view.OnCollectionChanged;

        if(newValue is ObservableCollection<DonutSegment> newCollection)
            newCollection.CollectionChanged+=view.OnCollectionChanged;

        view.Refresh();
    }

    void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Refresh();

    void Refresh()
    {
        var hasData = Segments is { Count:>0 }&&Segments.Sum(s => s.Value)>0;
        _emptyLabel.Text=EmptyText;
        _emptyLabel.IsVisible=!hasData;
        _graphicsView.IsVisible=hasData;
        _centerLabel.IsVisible=hasData;
        _graphicsView.Invalidate();
    }

    class DonutDrawable : IDrawable
    {
        readonly SparkDonutChartView _owner;

        public DonutDrawable(SparkDonutChartView owner) => _owner=owner;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var segments = _owner.Segments;
            if(segments is null||segments.Count==0)
                return;

            var total = segments.Sum(s => s.Value);
            if(total<=0)
                return;

            var strokeWidth = _owner.StrokeWidth;
            var diameter = Math.Min(dirtyRect.Width, dirtyRect.Height)-strokeWidth;
            if(diameter<=0)
                return;

            var radius = diameter/2f;
            var centerX = dirtyRect.Width/2f;
            var centerY = dirtyRect.Height/2f;

            canvas.StrokeSize=strokeWidth;
            canvas.StrokeLineCap=LineCap.Butt;

            // Microsoft.Maui.Graphics angles: 0 = 3 o'clock, increasing counter-clockwise.
            // We start at 12 o'clock (90) and sweep clockwise as each segment is drawn.
            float startAngle = 90f;
            var rect = new RectF(centerX-radius, centerY-radius, diameter, diameter);

            foreach(var segment in segments)
            {
                if(segment.Value<=0)
                    continue;

                var sweep = (float)(segment.Value/total*360.0);
                var endAngle = startAngle-sweep;

                canvas.StrokeColor=segment.Color;
                canvas.DrawArc(rect, startAngle, endAngle, false, false);

                startAngle=endAngle;
            }
        }
    }
}