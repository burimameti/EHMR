using EHMR.Resources.Theming;

using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Color = Microsoft.Maui.Graphics.Color;


namespace EHMR.Resources.Controls;

public partial class FFDataGrid : ContentView
{
    public FFDataGrid()
    {
        InitializeComponent();

        Loaded+=OnLoaded;
        Unloaded+=OnUnloaded;
    }

    #region Lifecycle

    private void OnLoaded(object? sender, EventArgs e)
    {
        FFThemeManager.ThemeChanged-=ThemeManager_ThemeChanged;
        FFThemeManager.ThemeChanged+=ThemeManager_ThemeChanged;

        ApplyCurrentTheme();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        FFThemeManager.ThemeChanged-=ThemeManager_ThemeChanged;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        ApplyCurrentTheme();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        FFThemeManager.ThemeChanged-=ThemeManager_ThemeChanged;

        base.OnHandlerChanging(args);
    }

    #endregion

    #region Theme

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(
            nameof(Variant),
            typeof(FFThemeVariant),
            typeof(FFDataGrid),
            FFThemeVariant.Hospital,
            propertyChanged: OnVariantChanged);

    public FFThemeVariant Variant
    {
        get => (FFThemeVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    private static void OnVariantChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is FFDataGrid grid&&
            newValue is FFThemeVariant)
        {
            // NOTE: this used to call grid.ApplyCurrentTheme() -> FFThemeManager.Get(Variant),
            // which only ever touched THIS grid's own bindable properties and never synced
            // Application.Current.Resources. That meant setting Variant="Hospital" here left
            // every DynamicResource-bound element on the page (buttons, filter card, badge
            // pills, row dividers, etc.) frozen on whatever theme was applied globally last —
            // which is why colors looked "random"/mismatched no matter what you picked.
            // ApplyCurrentTheme() now routes through FFThemeManager.ApplyTheme(Variant), which
            // pushes global resources AND fires ThemeChanged, so this grid's own properties
            // (via the ThemeChanged subscription below) and every DynamicResource binding on
            // the page update together, in sync.
            grid.ApplyCurrentTheme();
        }
    }

    private void ThemeManager_ThemeChanged(object? sender, FFThemeTokens tokens)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ApplyTheme(tokens);
        });
    }

    private void ApplyCurrentTheme()
    {
        // Was: ApplyTheme(FFThemeManager.Get(Variant));  <- local-only, never synced globally.
        // Now: goes through the single app-wide entry point so this grid's Variant is treated
        // as the source of truth for the WHOLE page's theme, not just this control's interior.
        FFThemeManager.ApplyTheme(Variant);
    }

    private void ApplyTheme(FFThemeTokens tokens)
    {
        if(tokens==null)
            return;

        //==================================================
        // SURFACE
        //==================================================

        SurfaceBackground=tokens.SurfaceBackground;
        HeaderBackground=tokens.HeaderBackground;
        FooterBackground=tokens.FooterBackground;

        //==================================================
        // EFFECTS
        //==================================================

        OverlayBackground=tokens.OverlayBackground;
        ShadowBrush=tokens.ShadowBrush;
        ShadowOpacity=tokens.ShadowOpacity;
        ShadowRadius=tokens.ShadowRadius;

        //==================================================
        // ROWS
        //==================================================

        RowBackground=tokens.RowBackground;
        AlternateRowBackground=tokens.AlternateRowBackground;
        HoverRowBackground=tokens.HoverRowBackground;
        SelectedRowBackground=tokens.SelectedRowBackground;

        HeaderHoverBackground=tokens.HeaderHoverBackground;
        CellHoverBackground=tokens.CellHoverBackground;
        SelectedCellBackground=tokens.SelectedCellBackground;

        RowHeight=tokens.RowHeight;

        //==================================================
        // TYPOGRAPHY
        //==================================================

        HeaderForeground=tokens.HeaderForeground;

        HeaderFontAttributes=tokens.HeaderFontAttributes;

        HeaderFontSize=tokens.HeaderFontSize;
        CellFontSize=tokens.CellFontSize;
        FooterFontSize=tokens.FooterFontSize;

        PrimaryTextColor=tokens.PrimaryTextColor;
        SecondaryTextColor=tokens.SecondaryTextColor;
        MutedTextColor=tokens.MutedTextColor;

        //==================================================
        // BORDER
        //==================================================

        BorderBrush=tokens.BorderBrush;
        DividerBrush=tokens.DividerBrush;
        BorderThickness=tokens.BorderThickness;

        //==================================================
        // SPACING
        //==================================================

        HeaderPadding=tokens.HeaderPadding;
        CellPadding=tokens.CellPadding;
        FooterPadding=tokens.FooterPadding;

        HeaderHeight=tokens.HeaderHeight;
        FooterHeight=tokens.FooterHeight;

        CornerRadius=tokens.CornerRadius;

        //==================================================
        // ACCENT
        //==================================================

        AccentBrush=tokens.AccentBrush;
        AccentForeground=tokens.AccentForeground;

        //==================================================
        // PAGER
        //==================================================

        PagerButtonBackground=tokens.PagerButtonBackground;
        PagerButtonHover=tokens.PagerButtonHover;
        PagerActiveBackground=tokens.PagerActiveBackground;
        PagerActiveForeground=tokens.PagerActiveForeground;

        //==================================================
        // BADGES
        //==================================================

        BadgeSuccess=tokens.BadgeSuccess;
        BadgeWarning=tokens.BadgeWarning;
        BadgeDanger=tokens.BadgeDanger;
        BadgeInfo=tokens.BadgeInfo;
    }

    #endregion

    #region Selection
    public static readonly BindableProperty RowSelectedCommandProperty =
    BindableProperty.Create(
        nameof(RowSelectedCommand),
        typeof(ICommand),
        typeof(FFDataGrid));

    public ICommand? RowSelectedCommand
    {
        get => (ICommand?)GetValue(RowSelectedCommandProperty);
        set => SetValue(RowSelectedCommandProperty, value);
    }
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if(e.CurrentSelection.FirstOrDefault() is not object item)
            return;

        SelectedItem=item;

        if(RowCommand?.CanExecute(item)==true)
            RowCommand.Execute(item);

        if(RowSelectedCommand?.CanExecute(item)==true)
            RowSelectedCommand.Execute(item);

        if(sender is CollectionView cv)
            cv.SelectedItem=null;
    }

    #endregion

    #region Theme / Appearance

    private static readonly Brush TransparentBrush = new SolidColorBrush(Colors.Transparent);
    private static readonly Brush WhiteBrush = new SolidColorBrush(Colors.White);
    private static readonly Brush BlackBrush = new SolidColorBrush(Colors.Black);

    //==================================================
    // PAGER
    //==================================================

    public static readonly BindableProperty PagerButtonBackgroundProperty =
        BindableProperty.Create(
            nameof(PagerButtonBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            Brush.Transparent);

    public Brush PagerButtonBackground
    {
        get => (Brush)GetValue(PagerButtonBackgroundProperty);
        set => SetValue(PagerButtonBackgroundProperty, value);
    }

    public static readonly BindableProperty PagerButtonHoverProperty =
        BindableProperty.Create(
            nameof(PagerButtonHover),
            typeof(Brush),
            typeof(FFDataGrid),
            Brush.Transparent);

    public Brush PagerButtonHover
    {
        get => (Brush)GetValue(PagerButtonHoverProperty);
        set => SetValue(PagerButtonHoverProperty, value);
    }

    public static readonly BindableProperty PagerActiveBackgroundProperty =
        BindableProperty.Create(
            nameof(PagerActiveBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            Brush.Transparent);

    public Brush PagerActiveBackground
    {
        get => (Brush)GetValue(PagerActiveBackgroundProperty);
        set => SetValue(PagerActiveBackgroundProperty, value);
    }

    public static readonly BindableProperty PagerActiveForegroundProperty =
        BindableProperty.Create(
            nameof(PagerActiveForeground),
            typeof(Brush),
            typeof(FFDataGrid),
            new SolidColorBrush(Colors.White));

    public Brush PagerActiveForeground
    {
        get => (Brush)GetValue(PagerActiveForegroundProperty);
        set => SetValue(PagerActiveForegroundProperty, value);
    }

    //==================================================
    // BADGES
    //==================================================

    public static readonly BindableProperty BadgeSuccessProperty =
        BindableProperty.Create(
            nameof(BadgeSuccess),
            typeof(Brush),
            typeof(FFDataGrid),
            Brush.Transparent);

    public Brush BadgeSuccess
    {
        get => (Brush)GetValue(BadgeSuccessProperty);
        set => SetValue(BadgeSuccessProperty, value);
    }

    public static readonly BindableProperty BadgeWarningProperty =
        BindableProperty.Create(
            nameof(BadgeWarning),
            typeof(Brush),
            typeof(FFDataGrid),
            Brush.Transparent);

    public Brush BadgeWarning
    {
        get => (Brush)GetValue(BadgeWarningProperty);
        set => SetValue(BadgeWarningProperty, value);
    }

    public static readonly BindableProperty BadgeDangerProperty =
        BindableProperty.Create(
            nameof(BadgeDanger),
            typeof(Brush),
            typeof(FFDataGrid),
            Brush.Transparent);

    public Brush BadgeDanger
    {
        get => (Brush)GetValue(BadgeDangerProperty);
        set => SetValue(BadgeDangerProperty, value);
    }

    public static readonly BindableProperty BadgeInfoProperty =
        BindableProperty.Create(
            nameof(BadgeInfo),
            typeof(Brush),
            typeof(FFDataGrid),
            Brush.Transparent);

    public Brush BadgeInfo
    {
        get => (Brush)GetValue(BadgeInfoProperty);
        set => SetValue(BadgeInfoProperty, value);
    }

    public static readonly BindableProperty SurfaceBackgroundProperty =
        BindableProperty.Create(
            nameof(SurfaceBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush SurfaceBackground
    {
        get => (Brush)GetValue(SurfaceBackgroundProperty);
        set => SetValue(SurfaceBackgroundProperty, value);
    }

    public static readonly BindableProperty HeaderBackgroundProperty =
        BindableProperty.Create(
            nameof(HeaderBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush HeaderBackground
    {
        get => (Brush)GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    public static readonly BindableProperty FooterBackgroundProperty =
        BindableProperty.Create(
            nameof(FooterBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush FooterBackground
    {
        get => (Brush)GetValue(FooterBackgroundProperty);
        set => SetValue(FooterBackgroundProperty, value);
    }

    //
    // ROWS
    //

    public static readonly BindableProperty RowBackgroundProperty =
        BindableProperty.Create(
            nameof(RowBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush RowBackground
    {
        get => (Brush)GetValue(RowBackgroundProperty);
        set => SetValue(RowBackgroundProperty, value);
    }

    public static readonly BindableProperty AlternateRowBackgroundProperty =
        BindableProperty.Create(
            nameof(AlternateRowBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush AlternateRowBackground
    {
        get => (Brush)GetValue(AlternateRowBackgroundProperty);
        set => SetValue(AlternateRowBackgroundProperty, value);
    }

    public static readonly BindableProperty HoverRowBackgroundProperty =
        BindableProperty.Create(
            nameof(HoverRowBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush HoverRowBackground
    {
        get => (Brush)GetValue(HoverRowBackgroundProperty);
        set => SetValue(HoverRowBackgroundProperty, value);
    }

    public static readonly BindableProperty SelectedRowBackgroundProperty =
        BindableProperty.Create(
            nameof(SelectedRowBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush SelectedRowBackground
    {
        get => (Brush)GetValue(SelectedRowBackgroundProperty);
        set => SetValue(SelectedRowBackgroundProperty, value);
    }

    public static readonly BindableProperty HeaderHoverBackgroundProperty =
        BindableProperty.Create(
            nameof(HeaderHoverBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush HeaderHoverBackground
    {
        get => (Brush)GetValue(HeaderHoverBackgroundProperty);
        set => SetValue(HeaderHoverBackgroundProperty, value);
    }

    public static readonly BindableProperty CellHoverBackgroundProperty =
        BindableProperty.Create(
            nameof(CellHoverBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush CellHoverBackground
    {
        get => (Brush)GetValue(CellHoverBackgroundProperty);
        set => SetValue(CellHoverBackgroundProperty, value);
    }

    public static readonly BindableProperty SelectedCellBackgroundProperty =
        BindableProperty.Create(
            nameof(SelectedCellBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush SelectedCellBackground
    {
        get => (Brush)GetValue(SelectedCellBackgroundProperty);
        set => SetValue(SelectedCellBackgroundProperty, value);
    }

    //
    // BORDER
    //

    public static readonly BindableProperty BorderBrushProperty =
        BindableProperty.Create(
            nameof(BorderBrush),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush BorderBrush
    {
        get => (Brush)GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public static readonly BindableProperty DividerBrushProperty =
        BindableProperty.Create(
            nameof(DividerBrush),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush DividerBrush
    {
        get => (Brush)GetValue(DividerBrushProperty);
        set => SetValue(DividerBrushProperty, value);
    }

    public static readonly BindableProperty BorderThicknessProperty =
        BindableProperty.Create(
            nameof(BorderThickness),
            typeof(double),
            typeof(FFDataGrid),
            1d);

    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    //
    // ACCENT
    //

    public static readonly BindableProperty AccentBrushProperty =
        BindableProperty.Create(
            nameof(AccentBrush),
            typeof(Brush),
            typeof(FFDataGrid),
            TransparentBrush);

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public static readonly BindableProperty AccentForegroundProperty =
        BindableProperty.Create(
            nameof(AccentForeground),
            typeof(Brush),
            typeof(FFDataGrid),
            WhiteBrush);

    public Brush AccentForeground
    {
        get => (Brush)GetValue(AccentForegroundProperty);
        set => SetValue(AccentForegroundProperty, value);
    }

    //
    // TEXT
    //

    public static readonly BindableProperty HeaderForegroundProperty =
        BindableProperty.Create(
            nameof(HeaderForeground),
            typeof(Brush),
            typeof(FFDataGrid),
            BlackBrush);

    public Brush HeaderForeground
    {
        get => (Brush)GetValue(HeaderForegroundProperty);
        set => SetValue(HeaderForegroundProperty, value);
    }

    public static readonly BindableProperty PrimaryTextColorProperty =
        BindableProperty.Create(
            nameof(PrimaryTextColor),
            typeof(Color),
            typeof(FFDataGrid),
            Colors.Black);

    public Color PrimaryTextColor
    {
        get => (Color)GetValue(PrimaryTextColorProperty);
        set => SetValue(PrimaryTextColorProperty, value);
    }

    public static readonly BindableProperty SecondaryTextColorProperty =
        BindableProperty.Create(
            nameof(SecondaryTextColor),
            typeof(Color),
            typeof(FFDataGrid),
            Colors.DarkGray);

    public Color SecondaryTextColor
    {
        get => (Color)GetValue(SecondaryTextColorProperty);
        set => SetValue(SecondaryTextColorProperty, value);
    }

    public static readonly BindableProperty MutedTextColorProperty =
        BindableProperty.Create(
            nameof(MutedTextColor),
            typeof(Color),
            typeof(FFDataGrid),
            Colors.Gray);

    public Color MutedTextColor
    {
        get => (Color)GetValue(MutedTextColorProperty);
        set => SetValue(MutedTextColorProperty, value);
    }

    //
    // TYPOGRAPHY
    //

    public static readonly BindableProperty HeaderFontSizeProperty =
        BindableProperty.Create(
            nameof(HeaderFontSize),
            typeof(double),
            typeof(FFDataGrid),
            14d);

    public double HeaderFontSize
    {
        get => (double)GetValue(HeaderFontSizeProperty);
        set => SetValue(HeaderFontSizeProperty, value);
    }

    public static readonly BindableProperty CellFontSizeProperty =
        BindableProperty.Create(
            nameof(CellFontSize),
            typeof(double),
            typeof(FFDataGrid),
            13d);

    public double CellFontSize
    {
        get => (double)GetValue(CellFontSizeProperty);
        set => SetValue(CellFontSizeProperty, value);
    }

    public static readonly BindableProperty FooterFontSizeProperty =
        BindableProperty.Create(
            nameof(FooterFontSize),
            typeof(double),
            typeof(FFDataGrid),
            12d);

    public double FooterFontSize
    {
        get => (double)GetValue(FooterFontSizeProperty);
        set => SetValue(FooterFontSizeProperty, value);
    }

    public static readonly BindableProperty HeaderFontAttributesProperty =
        BindableProperty.Create(
            nameof(HeaderFontAttributes),
            typeof(FontAttributes),
            typeof(FFDataGrid),
            FontAttributes.Bold);

    public FontAttributes HeaderFontAttributes
    {
        get => (FontAttributes)GetValue(HeaderFontAttributesProperty);
        set => SetValue(HeaderFontAttributesProperty, value);
    }

    //
    // SPACING
    //

    public static readonly BindableProperty HeaderPaddingProperty =
        BindableProperty.Create(
            nameof(HeaderPadding),
            typeof(Thickness),
            typeof(FFDataGrid),
            new Thickness(16, 10));

    public Thickness HeaderPadding
    {
        get => (Thickness)GetValue(HeaderPaddingProperty);
        set => SetValue(HeaderPaddingProperty, value);
    }

    public static readonly BindableProperty CellPaddingProperty =
        BindableProperty.Create(
            nameof(CellPadding),
            typeof(Thickness),
            typeof(FFDataGrid),
            new Thickness(14, 10));

    public Thickness CellPadding
    {
        get => (Thickness)GetValue(CellPaddingProperty);
        set => SetValue(CellPaddingProperty, value);
    }

    public static readonly BindableProperty FooterPaddingProperty =
        BindableProperty.Create(
            nameof(FooterPadding),
            typeof(Thickness),
            typeof(FFDataGrid),
            new Thickness(16, 8));

    public Thickness FooterPadding
    {
        get => (Thickness)GetValue(FooterPaddingProperty);
        set => SetValue(FooterPaddingProperty, value);
    }

    //
    // LAYOUT
    //

    public static readonly BindableProperty RowHeightProperty =
        BindableProperty.Create(
            nameof(RowHeight),
            typeof(double),
            typeof(FFDataGrid),
            52d);

    public double RowHeight
    {
        get => (double)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    public static readonly BindableProperty HeaderHeightProperty =
        BindableProperty.Create(
            nameof(HeaderHeight),
            typeof(double),
            typeof(FFDataGrid),
            44d);

    public double HeaderHeight
    {
        get => (double)GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    public static readonly BindableProperty FooterHeightProperty =
        BindableProperty.Create(
            nameof(FooterHeight),
            typeof(double),
            typeof(FFDataGrid),
            44d);

    public double FooterHeight
    {
        get => (double)GetValue(FooterHeightProperty);
        set => SetValue(FooterHeightProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(FFDataGrid),
            new CornerRadius(16));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    //
    // SHADOW
    //

    public static readonly BindableProperty OverlayBackgroundProperty =
        BindableProperty.Create(
            nameof(OverlayBackground),
            typeof(Brush),
            typeof(FFDataGrid),
            new SolidColorBrush(Color.FromArgb("#44000000")));

    public Brush OverlayBackground
    {
        get => (Brush)GetValue(OverlayBackgroundProperty);
        set => SetValue(OverlayBackgroundProperty, value);
    }

    public static readonly BindableProperty ShadowBrushProperty =
        BindableProperty.Create(
            nameof(ShadowBrush),
            typeof(Brush),
            typeof(FFDataGrid),
            BlackBrush);

    public Brush ShadowBrush
    {
        get => (Brush)GetValue(ShadowBrushProperty);
        set => SetValue(ShadowBrushProperty, value);
    }

    public static readonly BindableProperty ShadowOpacityProperty =
        BindableProperty.Create(
            nameof(ShadowOpacity),
            typeof(float),
            typeof(FFDataGrid),
            0.05f);

    public float ShadowOpacity
    {
        get => (float)GetValue(ShadowOpacityProperty);
        set => SetValue(ShadowOpacityProperty, value);
    }

    public static readonly BindableProperty ShadowRadiusProperty =
        BindableProperty.Create(
            nameof(ShadowRadius),
            typeof(double),
            typeof(FFDataGrid),
            14d);

    public double ShadowRadius
    {
        get => (double)GetValue(ShadowRadiusProperty);
        set => SetValue(ShadowRadiusProperty, value);
    }

    #region Data Source

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(FFDataGrid),
            default(IEnumerable),
            propertyChanged: OnItemsSourceChanged);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty PagedItemsProperty =
        BindableProperty.Create(
            nameof(PagedItems),
            typeof(IEnumerable),
            typeof(FFDataGrid),
            default(IEnumerable));

    public IEnumerable? PagedItems
    {
        get => (IEnumerable?)GetValue(PagedItemsProperty);
        set => SetValue(PagedItemsProperty, value);
    }

    public static readonly BindableProperty RowTemplateProperty =
        BindableProperty.Create(
            nameof(RowTemplate),
            typeof(DataTemplate),
            typeof(FFDataGrid));

    public DataTemplate? RowTemplate
    {
        get => (DataTemplate?)GetValue(RowTemplateProperty);
        set => SetValue(RowTemplateProperty, value);
    }

    public static readonly BindableProperty HeaderContentProperty =
        BindableProperty.Create(
            nameof(HeaderContent),
            typeof(IView),
            typeof(FFDataGrid));

    public IView? HeaderContent
    {
        get => (IView?)GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    #endregion

    #region Empty State

    public static readonly BindableProperty EmptyTitleProperty =
        BindableProperty.Create(
            nameof(EmptyTitle),
            typeof(string),
            typeof(FFDataGrid),
            "No records");

    public string EmptyTitle
    {
        get => (string)GetValue(EmptyTitleProperty);
        set => SetValue(EmptyTitleProperty, value);
    }

    public static readonly BindableProperty EmptyMessageProperty =
        BindableProperty.Create(
            nameof(EmptyMessage),
            typeof(string),
            typeof(FFDataGrid),
            string.Empty);

    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(FFDataGrid),
            false);

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    #endregion

    #region Selection

    public static readonly BindableProperty SelectionModeProperty =
        BindableProperty.Create(
            nameof(SelectionMode),
            typeof(SelectionMode),
            typeof(FFDataGrid),
            SelectionMode.Single);

    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(
            nameof(SelectedItem),
            typeof(object),
            typeof(FFDataGrid),
            null,
            BindingMode.TwoWay);

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    #endregion

    #region Paging

    public static readonly BindableProperty CurrentPageProperty =
        BindableProperty.Create(
            nameof(CurrentPage),
            typeof(int),
            typeof(FFDataGrid),
            1);

    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public static readonly BindableProperty TotalPagesProperty =
        BindableProperty.Create(
            nameof(TotalPages),
            typeof(int),
            typeof(FFDataGrid),
            1);

    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    public static readonly BindableProperty TotalItemsProperty =
        BindableProperty.Create(
            nameof(TotalItems),
            typeof(int),
            typeof(FFDataGrid),
            0);

    public int TotalItems
    {
        get => (int)GetValue(TotalItemsProperty);
        set => SetValue(TotalItemsProperty, value);
    }

    public static readonly BindableProperty PageNumbersProperty =
        BindableProperty.Create(
            nameof(PageNumbers),
            typeof(ObservableCollection<FFPageNumber>),
            typeof(FFDataGrid),
            new ObservableCollection<FFPageNumber>());

    public ObservableCollection<FFPageNumber> PageNumbers
    {
        get => (ObservableCollection<FFPageNumber>)GetValue(PageNumbersProperty);
        set => SetValue(PageNumbersProperty, value);
    }

    #endregion

    #region Commands

    public static readonly BindableProperty PrevPageCommandProperty =
        BindableProperty.Create(
            nameof(PrevPageCommand),
            typeof(ICommand),
            typeof(FFDataGrid));

    public ICommand? PrevPageCommand
    {
        get => (ICommand?)GetValue(PrevPageCommandProperty);
        set => SetValue(PrevPageCommandProperty, value);
    }

    public static readonly BindableProperty NextPageCommandProperty =
        BindableProperty.Create(
            nameof(NextPageCommand),
            typeof(ICommand),
            typeof(FFDataGrid));

    public ICommand? NextPageCommand
    {
        get => (ICommand?)GetValue(NextPageCommandProperty);
        set => SetValue(NextPageCommandProperty, value);
    }

    public static readonly BindableProperty RowCommandProperty =
        BindableProperty.Create(
            nameof(RowCommand),
            typeof(ICommand),
            typeof(FFDataGrid));

    public ICommand? RowCommand
    {
        get => (ICommand?)GetValue(RowCommandProperty);
        set => SetValue(RowCommandProperty, value);
    }

    #endregion

    #region ItemsSource Changed

    private static void OnItemsSourceChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is not FFDataGrid grid)
            return;

        if(newValue is IEnumerable items)
        {
            grid.TotalItems=items.Cast<object>().Count();

            if(grid.PagedItems==null)
                grid.PagedItems=items;
        }
    }

    #endregion

    #region Grid Behavior

    public static readonly BindableProperty ShowRowDividerProperty =
    BindableProperty.Create(
    nameof(ShowRowDivider),
    typeof(bool),
    typeof(FFDataGrid),
    true);

    public bool ShowRowDivider
    {
        get => (bool)GetValue(ShowRowDividerProperty);
        set => SetValue(ShowRowDividerProperty, value);
    }

    public static readonly BindableProperty ShowAlternateRowsProperty =
    BindableProperty.Create(
    nameof(ShowAlternateRows),
    typeof(bool),
    typeof(FFDataGrid),
    false);

    public bool ShowAlternateRows
    {
        get => (bool)GetValue(ShowAlternateRowsProperty);
        set => SetValue(ShowAlternateRowsProperty, value);
    }

    public static readonly BindableProperty DensityProperty =
    BindableProperty.Create(
    nameof(Density),
    typeof(FFGridDensity),
    typeof(FFDataGrid),
    FFGridDensity.Normal);

    public FFGridDensity Density
    {
        get => (FFGridDensity)GetValue(DensityProperty);
        set => SetValue(DensityProperty, value);
    }

    #endregion

    #region Models

    public sealed class FFPageNumber : BindableObject
    {
        private bool _isActive;

        public int Number
        {
            get; set;
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if(_isActive==value)
                    return;

                _isActive=value;
                OnPropertyChanged();
            }
        }

        public ICommand? Command
        {
            get; set;
        }

        public override string ToString() => Number.ToString();
    }

    public sealed class FFGridColumn
    {
        public string Header { get; set; } = "";

        public string PropertyName { get; set; } = "";

        public GridLength Width
        {
            get; set;
        }
            = GridLength.Star;

        public FFColumnType ColumnType
        {
            get; set;
        }
            = FFColumnType.Text;

        public bool IsSortable { get; set; } = true;

        public bool IsResizable { get; set; } = true;

        public bool IsVisible { get; set; } = true;

        public DataTemplate? CellTemplate
        {
            get; set;
        }

        public DataTemplate? HeaderTemplate
        {
            get; set;
        }

        public FFAlign Alignment
        {
            get; set;
        }
            = FFAlign.Left;
    }

    #endregion

    #region Enums

    public enum FFColumnType
    {
        Text,
        Number,
        Decimal,
        Currency,
        Percentage,
        Date,
        Time,
        DateTime,
        Boolean,
        Badge,
        Status,
        Icon,
        Image,
        Hyperlink,
        Progress,
        Button,
        Action,
        Custom
    }

    public enum FFGridDensity
    {
        Compact,
        Normal,
        Comfortable
    }

    public enum FFGridLines
    {
        None,
        Horizontal,
        Vertical,
        Both
    }

    public enum FFSelectionBehavior
    {
        None,
        Single,
        Multiple
    }

    #endregion

    #region Helpers

    public void Refresh()
    {
        PART_Items.ItemsSource=null;
        PART_Items.ItemsSource=PagedItems;
    }

    public void ClearSelection()
    {
        PART_Items.SelectedItem=null;
        SelectedItem=null;
    }

    public void ScrollToTop()
    {
        if(PagedItems==null)
            return;

        var first = PagedItems.Cast<object>().FirstOrDefault();

        if(first!=null)
            PART_Items.ScrollTo(first, position: ScrollToPosition.Start, animate: true);
    }

    #endregion
}


public class FFGridColumn
{
    public string Header { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public GridLength Width { get; set; } = GridLength.Auto;
    public FFColumnType ColumnType
    {
        get; set;
    }
    public DataTemplate? CellTemplate
    {
        get; set;
    }
    public DataTemplate? HeaderTemplate
    {
        get; set;
    }
    public bool IsSortable
    {
        get; set;
    }
    public bool IsResizable
    {
        get; set;
    }

}

public enum FFColumnType
{
    Text, Number, Date, Currency, Badge, Action
}

public enum FFAlign
{
    Left, Center, Right
}
#endregion