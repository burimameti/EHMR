namespace EHMR.Resources.Controls;

public partial class FFGridRowPresenter : ContentView
{
    private bool _isHover;

    public FFGridRowPresenter()
    {
        InitializeComponent();

        var pointer = new PointerGestureRecognizer();

        pointer.PointerEntered+=(_, _) => SetHover(true);
        pointer.PointerExited+=(_, _) => SetHover(false);

        GestureRecognizers.Add(pointer);

        BindingContextChanged+=OnBindingContextChanged;
    }

    // ✅ THIS replaces OnRowBound completely
    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        // reset hover state when reused in CollectionView recycling
        SetHover(false);
    }

    public void SetHover(bool isHover)
    {
        _isHover=isHover;

        VisualStateManager.GoToState(
            this,
            isHover ? "PointerOver" : "Normal");
    }

    // ================= CONTENT =================
    public static readonly BindableProperty ContentProperty =
        BindableProperty.Create(nameof(Content), typeof(View), typeof(FFGridRowPresenter));

    public View Content
    {
        get => (View)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    // ================= HEIGHT =================
    public static readonly BindableProperty RowHeightProperty =
        BindableProperty.Create(nameof(RowHeight), typeof(double), typeof(FFGridRowPresenter), 48.0);

    public double RowHeight
    {
        get => (double)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    // ================= COLORS =================
    public static readonly BindableProperty RowBackgroundProperty =
        BindableProperty.Create(nameof(RowBackground), typeof(Color), typeof(FFGridRowPresenter));

    public Color RowBackground
    {
        get => (Color)GetValue(RowBackgroundProperty);
        set => SetValue(RowBackgroundProperty, value);
    }

    public static readonly BindableProperty HoverRowBackgroundProperty =
        BindableProperty.Create(nameof(HoverRowBackground), typeof(Color), typeof(FFGridRowPresenter));

    public Color HoverRowBackground
    {
        get => (Color)GetValue(HoverRowBackgroundProperty);
        set => SetValue(HoverRowBackgroundProperty, value);
    }

    public static readonly BindableProperty SelectedRowBackgroundProperty =
        BindableProperty.Create(nameof(SelectedRowBackground), typeof(Color), typeof(FFGridRowPresenter));

    public Color SelectedRowBackground
    {
        get => (Color)GetValue(SelectedRowBackgroundProperty);
        set => SetValue(SelectedRowBackgroundProperty, value);
    }

    public static readonly BindableProperty DividerColorProperty =
        BindableProperty.Create(nameof(DividerColor), typeof(Color), typeof(FFGridRowPresenter));

    public Color DividerColor
    {
        get => (Color)GetValue(DividerColorProperty);
        set => SetValue(DividerColorProperty, value);
    }
    public Action<FFGridRowPresenter> HoverEnter
    {
        get;
        internal set;
    }
    public Action<FFGridRowPresenter> HoverExit
    {
        get;
        internal set;
    }
}