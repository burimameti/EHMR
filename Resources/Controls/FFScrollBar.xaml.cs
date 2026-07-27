namespace EHMR.Resources.Controls;

/// <summary>
/// Постојано видлива вертикална scrollbar лента.
///
/// Системскиот WinUI scrollbar се крие автоматски и е претенок за оваа употреба,
/// па оваа контрола ја црта лентата рачно и останува на екран.
///
/// Се врзува за <see cref="ScrollView"/> преку <see cref="AttachTo"/> — контролата
/// го следи скролот и го поместува ScrollView-от кога се влече палецот.
/// </summary>
public partial class FFScrollBar : ContentView
{
    private ScrollView? _target;
    private double _panStartY;
    private bool _isDragging;

    public FFScrollBar()
    {
        InitializeComponent();
        SizeChanged+=(_, _) => UpdateThumb();
    }

    // =====================================================
    // ИЗГЛЕД
    // =====================================================

    public static readonly BindableProperty BarWidthProperty =
        BindableProperty.Create(nameof(BarWidth), typeof(double), typeof(FFScrollBar), 10d);

    public double BarWidth
    {
        get => (double)GetValue(BarWidthProperty);
        set => SetValue(BarWidthProperty, value);
    }

    public static readonly BindableProperty TrackColorProperty =
        BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(FFScrollBar),
            Color.FromArgb("#EDF1F5"));

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public static readonly BindableProperty ThumbColorProperty =
        BindableProperty.Create(nameof(ThumbColor), typeof(Color), typeof(FFScrollBar),
            Color.FromArgb("#94A3B8"));

    public Color ThumbColor
    {
        get => (Color)GetValue(ThumbColorProperty);
        set => SetValue(ThumbColorProperty, value);
    }

    /// <summary>Минимална висина на палецот за да остане фатлив кај долга содржина.</summary>
    public static readonly BindableProperty MinimumThumbHeightProperty =
        BindableProperty.Create(nameof(MinimumThumbHeight), typeof(double), typeof(FFScrollBar), 32d);

    public double MinimumThumbHeight
    {
        get => (double)GetValue(MinimumThumbHeightProperty);
        set => SetValue(MinimumThumbHeightProperty, value);
    }

    // =====================================================
    // ВРЗУВАЊЕ ЗА SCROLLVIEW
    // =====================================================

    /// <summary>
    /// Ја врзува лентата за даден ScrollView. Повикај го еднаш, по InitializeComponent
    /// на страната што ја користи.
    /// </summary>
    public void AttachTo(ScrollView scrollView)
    {
        if(_target is not null)
        {
            _target.Scrolled-=OnTargetScrolled;
            _target.SizeChanged-=OnTargetSizeChanged;
        }

        _target=scrollView;

        _target.Scrolled+=OnTargetScrolled;
        _target.SizeChanged+=OnTargetSizeChanged;

        UpdateThumb();
    }

    private void OnTargetSizeChanged(object? sender, EventArgs e) => UpdateThumb();

    private void OnTargetScrolled(object? sender, ScrolledEventArgs e)
    {
        // Кога корисникот влече, палецот веќе е позициониран од самото влечење.
        if(!_isDragging)
            UpdateThumb();
    }

    // =====================================================
    // ПРЕСМЕТКА
    // =====================================================

    private double Viewport => _target?.Height??0;

    private double Extent =>
        _target?.Content is null
            ? 0
            : Math.Max(_target.Content.Height, Viewport);

    private double MaxScroll => Math.Max(Extent-Viewport, 0);

    private double TrackHeight => TrackHost.Height;

    private void UpdateThumb()
    {
        var track = TrackHeight;
        var extent = Extent;

        if(_target is null||track<=0||extent<=0)
            return;

        // Целата содржина се гледа — нема што да се скрола.
        if(MaxScroll<=0)
        {
            Thumb.HeightRequest=track;
            Thumb.TranslationY=0;
            IsEnabled=false;
            return;
        }

        IsEnabled=true;

        var ratio = Viewport/extent;
        var thumbHeight = Math.Max(track*ratio, MinimumThumbHeight);
        thumbHeight=Math.Min(thumbHeight, track);

        Thumb.HeightRequest=thumbHeight;

        var progress = _target.ScrollY/MaxScroll;
        progress=Math.Clamp(progress, 0, 1);

        Thumb.TranslationY=progress*(track-thumbHeight);
    }

    // =====================================================
    // ВЛЕЧЕЊЕ
    // =====================================================

    private async void OnThumbPan(object? sender, PanUpdatedEventArgs e)
    {
        if(_target is null)
            return;

        var track = TrackHeight;
        var thumbHeight = Thumb.Height;
        var travel = track-thumbHeight;

        if(travel<=0||MaxScroll<=0)
            return;

        switch(e.StatusType)
        {
            case GestureStatus.Started:
                _isDragging=true;
                _panStartY=Thumb.TranslationY;
                break;

            case GestureStatus.Running:
                {
                    var y = Math.Clamp(_panStartY+e.TotalY, 0, travel);
                    Thumb.TranslationY=y;

                    var progress = y/travel;
                    await _target.ScrollToAsync(0, progress*MaxScroll, false);
                    break;
                }

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _isDragging=false;
                UpdateThumb();
                break;
        }
    }
}
