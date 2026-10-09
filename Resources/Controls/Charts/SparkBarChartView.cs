using System.Collections.ObjectModel;
using System.Collections.Specialized;
using EHMR.Backups.Models;
using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Controls.Charts;

/// <summary>
/// Simple bar chart with a value label above each bar and a category label below.
/// Bind <see cref="Data"/> to an ObservableCollection&lt;ChartDataPoint&gt;.
/// Shows <see cref="EmptyText"/> instead of the chart when there's no data.
/// </summary>
public class SparkBarChartView : ContentView
{
    private static Color ResolveBrandAccent()
    {
        if (Application.Current?.Resources.TryGetValue("BrandAccent", out var value) == true && value is Color color)
            return color;

        return Color.FromArgb("#0D9488");
    }

    readonly GraphicsView _graphicsView;
    readonly Label _emptyLabel;
    readonly BarChartDrawable _drawable;

    public static readonly BindableProperty DataProperty = BindableProperty.Create(
        nameof(Data),
        typeof(ObservableCollection<ChartDataPoint>),
        typeof(SparkBarChartView),
        propertyChanged: OnDataChanged);

    public ObservableCollection<ChartDataPoint> Data
    {
        get => (ObservableCollection<ChartDataPoint>)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public static readonly BindableProperty EmptyTextProperty = BindableProperty.Create(
        nameof(EmptyText),
        typeof(string),
        typeof(SparkBarChartView),
        "No data",
        propertyChanged: (b, _, _) => ((SparkBarChartView)b).Refresh());

    public string EmptyText
    {
        get => (string)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    public static readonly BindableProperty BarColorProperty = BindableProperty.Create(
        nameof(BarColor),
        typeof(Color),
        typeof(SparkBarChartView),
        ResolveBrandAccent(),
        propertyChanged: (b, _, _) => ((SparkBarChartView)b).Refresh());

    public Color BarColor
    {
        get => (Color)GetValue(BarColorProperty);
        set => SetValue(BarColorProperty, value);
    }

    public SparkBarChartView()
    {
        _drawable=new BarChartDrawable(this);
        _graphicsView=new GraphicsView { Drawable=_drawable };

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
        grid.Add(_emptyLabel);
        Content=grid;
    }

    static void OnDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (SparkBarChartView)bindable;

        if(oldValue is ObservableCollection<ChartDataPoint> oldCollection)
            oldCollection.CollectionChanged-=view.OnCollectionChanged;

        if(newValue is ObservableCollection<ChartDataPoint> newCollection)
            newCollection.CollectionChanged+=view.OnCollectionChanged;

        view.Refresh();
    }

    void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Refresh();

    void Refresh()
    {
        var hasData = Data is { Count:>0 };
        _emptyLabel.Text=EmptyText;
        _emptyLabel.IsVisible=!hasData;
        _graphicsView.IsVisible=hasData;
        _graphicsView.Invalidate();
    }

    class BarChartDrawable : IDrawable
    {
        readonly SparkBarChartView _owner;

        public BarChartDrawable(SparkBarChartView owner) => _owner=owner;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var points = _owner.Data;
            if(points is null||points.Count==0)
                return;

            const float labelHeight = 18f;
            const float valueHeight = 16f;
            const float barSpacing = 8f;

            var chartTop = valueHeight;
            var chartBottom = dirtyRect.Height-labelHeight;
            var chartHeight = chartBottom-chartTop;
            if(chartHeight<=0)
                return;

            var maxValue = points.Max(p => p.Value);
            if(maxValue<=0)
                maxValue=1;

            var barCount = points.Count;
            var totalSpacing = barSpacing*(barCount-1);
            var barWidth = (dirtyRect.Width-totalSpacing)/barCount;
            if(barWidth<=0)
                return;

            canvas.FontSize=10;
            var x = dirtyRect.X;

            foreach(var point in points)
            {
                var barHeight = (float)(point.Value/maxValue*chartHeight);
                var barTop = chartBottom-barHeight;

                canvas.FillColor=_owner.BarColor;
                canvas.FillRoundedRectangle(new RectF(x, barTop, barWidth, barHeight), 4);

                canvas.FontColor=Color.FromArgb("#1F2A37");
                canvas.DrawString(
                    point.Value.ToString("0.#"),
                    x, barTop-valueHeight, barWidth, valueHeight,
                    HorizontalAlignment.Center, VerticalAlignment.Bottom);

                canvas.FontColor=Color.FromArgb("#8E97A6");
                canvas.DrawString(
                    point.Label,
                    x, chartBottom+2, barWidth, labelHeight,
                    HorizontalAlignment.Center, VerticalAlignment.Top);

                x+=barWidth+barSpacing;
            }
        }
    }
}