using Microsoft.Maui.Controls.Shapes;

namespace EHMR.Resources.Controls;

/// <summary>
/// Единствена надворешна карта за CRUD секции: бела квадратна површина
/// со teal линија лево. Search/grid контролите не цртаат сопствена карта.
/// Responsive: секогаш ја пополнува ширината на родителот, паддингот се намалува
/// на тесни прозорци, а по избор (ScrollOverflow) содржината добива хоризонтален
/// скрол наместо да излегува надвор од прозорецот.
/// </summary>
public sealed class FFSectionCard : Border
{
    private readonly Grid _layout;
    private readonly ContentView _contentHost;
    private readonly ScrollView _overflowScroll;
    private View? _userContent;
    private bool _wrappingContent;

    // ── Bindable properties ───────────────────────────────────────────────

    public static readonly BindableProperty ContentPaddingProperty =
        BindableProperty.Create(nameof(ContentPadding), typeof(Thickness), typeof(FFSectionCard),
            new Thickness(18), propertyChanged: (b, _, _) => ((FFSectionCard)b).ApplyPadding());

    public static readonly BindableProperty CompactPaddingProperty =
        BindableProperty.Create(nameof(CompactPadding), typeof(Thickness), typeof(FFSectionCard),
            new Thickness(10), propertyChanged: (b, _, _) => ((FFSectionCard)b).ApplyPadding());

    public static readonly BindableProperty CompactBreakpointProperty =
        BindableProperty.Create(nameof(CompactBreakpoint), typeof(double), typeof(FFSectionCard),
            720d, propertyChanged: (b, _, _) => ((FFSectionCard)b).ApplyPadding());

    /// <summary>
    /// Кога е true, содржината се става во хоризонтален ScrollView.
    /// НЕ го користи за data grid / star-колони — таму ја ломи распоредот.
    /// Користи го само за редови со Auto колони што не можеш да ги претвориш во FlexLayout.
    /// </summary>
    public static readonly BindableProperty ScrollOverflowProperty =
        BindableProperty.Create(nameof(ScrollOverflow), typeof(bool), typeof(FFSectionCard),
            false, propertyChanged: (b, _, _) => ((FFSectionCard)b).ApplyContent());

    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public Thickness CompactPadding
    {
        get => (Thickness)GetValue(CompactPaddingProperty);
        set => SetValue(CompactPaddingProperty, value);
    }

    public double CompactBreakpoint
    {
        get => (double)GetValue(CompactBreakpointProperty);
        set => SetValue(CompactBreakpointProperty, value);
    }

    public bool ScrollOverflow
    {
        get => (bool)GetValue(ScrollOverflowProperty);
        set => SetValue(ScrollOverflowProperty, value);
    }

    // ── Ctor ──────────────────────────────────────────────────────────────

    public FFSectionCard()
    {
        Padding=0;
        Margin=new Thickness(0);
        StrokeThickness=1;
        StrokeShape=new RoundRectangle { CornerRadius=0 };
        Shadow=null;
        HorizontalOptions=LayoutOptions.Fill;
        MinimumWidthRequest=0;
        SetDynamicResource(BackgroundColorProperty, "White");
        SetDynamicResource(StrokeProperty, "BorderColor");

        var rail = new BoxView { WidthRequest=4, HorizontalOptions=LayoutOptions.Fill };
        rail.SetDynamicResource(BoxView.ColorProperty, "SidebarActiveBg");

        _overflowScroll=new ScrollView
        {
            Orientation=ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility=ScrollBarVisibility.Default,
            VerticalScrollBarVisibility=ScrollBarVisibility.Never
        };

        _contentHost=new ContentView
        {
            Padding=ContentPadding,
            HorizontalOptions=LayoutOptions.Fill,
            MinimumWidthRequest=0
        };

        _layout=new Grid
        {
            ColumnDefinitions=
            {
                new ColumnDefinition(new GridLength(4)),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing=0,
            IsClippedToBounds=true // ништо не излегува надвор од картата
        };
        _layout.Add(rail, 0);
        _layout.Add(_contentHost, 1);

        _wrappingContent=true;
        Content=_layout;
        _wrappingContent=false;

        SizeChanged+=(_, _) => ApplyPadding();
    }

    // ── Responsive helpers ────────────────────────────────────────────────

    private void ApplyPadding()
    {
        if(_contentHost is null) return;

        var compact = Width>0&&Width<CompactBreakpoint;
        _contentHost.Padding=compact ? CompactPadding : ContentPadding;
    }

    private void ApplyContent()
    {
        if(_contentHost is null) return;

        if(ScrollOverflow&&_userContent is not null)
        {
            _contentHost.Content=null;
            _overflowScroll.Content=_userContent;
            _contentHost.Content=_overflowScroll;
        }
        else
        {
            _overflowScroll.Content=null;
            _contentHost.Content=_userContent;
        }
    }

    // ── XAML content redirect ─────────────────────────────────────────────

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if(_contentHost is null||_layout is null||_wrappingContent||
           propertyName!=ContentProperty.PropertyName||
           Content is not View content||ReferenceEquals(content, _layout))
            return;

        _wrappingContent=true;
        _userContent=content;
        Content=_layout;
        ApplyContent();
        ApplyPadding();
        _wrappingContent=false;
    }
}