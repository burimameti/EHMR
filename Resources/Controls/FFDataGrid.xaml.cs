using EHMR.Resources.Theming;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.Resources.Controls;

public partial class FFDataGrid : ContentView
{
    public FFDataGrid()
    {
        InitializeComponent();
    }

    // =================================================================
    // 1. THEME ENGINE CONTROLLER PROFILES
    // =================================================================
    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(nameof(Variant), typeof(FFThemeVariant), typeof(FFDataGrid), FFThemeVariant.Sparked, propertyChanged: OnVariantChanged);

    public FFThemeVariant Variant
    {
        get => (FFThemeVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    private static void OnVariantChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if(bindable is FFDataGrid dataGrid&&newValue is FFThemeVariant newVariant)
        {
            var tokens = FFThemeManager.Get(newVariant);
            dataGrid.OverlayBackground=tokens.OverlayBackground;
            dataGrid.ShadowBrush=tokens.ShadowBrush;
            dataGrid.ShadowOpacity=tokens.ShadowOpacity;
            dataGrid.SurfaceBackground=tokens.SurfaceBackground;
            dataGrid.HeaderBackground=tokens.HeaderBackground;
            dataGrid.FooterBackground=tokens.FooterBackground;
            dataGrid.HeaderForeground=tokens.HeaderForeground;
            dataGrid.RowBackground=tokens.RowBackground;
            dataGrid.AlternateRowBackground=tokens.AlternateRowBackground;
            dataGrid.BorderBrush=tokens.BorderBrush;
            dataGrid.DividerBrush=tokens.DividerBrush;
            dataGrid.AccentBrush=tokens.AccentBrush;
            dataGrid.AccentForeground=tokens.AccentForeground;
            dataGrid.PrimaryTextColor=tokens.PrimaryTextColor;
            dataGrid.SecondaryTextColor=tokens.SecondaryTextColor;
            dataGrid.MutedTextColor=tokens.MutedTextColor;
            dataGrid.CornerRadius=tokens.CornerRadius;
        }
    }
    public static readonly BindableProperty PageNumbersProperty =
    BindableProperty.Create(nameof(PageNumbers), typeof(ObservableCollection<int>), typeof(FFDataGrid),
        new ObservableCollection<int>());

    public ObservableCollection<int> PageNumbers
    {
        get => (ObservableCollection<int>)GetValue(PageNumbersProperty);
        set => SetValue(PageNumbersProperty, value);
    }


    public static readonly BindableProperty OverlayBackgroundProperty =
    BindableProperty.Create(nameof(OverlayBackground), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Color.FromArgb("#66000000")));
    public Brush OverlayBackground
    {
        get => (Brush)GetValue(OverlayBackgroundProperty); set => SetValue(OverlayBackgroundProperty, value);
    }

    public static readonly BindableProperty ShadowBrushProperty =
        BindableProperty.Create(nameof(ShadowBrush), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.Black));
    public Brush ShadowBrush
    {
        get => (Brush)GetValue(ShadowBrushProperty); set => SetValue(ShadowBrushProperty, value);
    }

    public static readonly BindableProperty ShadowOpacityProperty =
        BindableProperty.Create(nameof(ShadowOpacity), typeof(float), typeof(FFDataGrid), 0.15f);
    public float ShadowOpacity
    {
        get => (float)GetValue(ShadowOpacityProperty); set => SetValue(ShadowOpacityProperty, value);
    }

    // =================================================================
    // 2. STYLING DEPENDENCY REGISTRATION PROPERTIES
    // =================================================================
    public static readonly BindableProperty AccentForegroundProperty = BindableProperty.Create(nameof(AccentForeground), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.White));
    public Brush AccentForeground
    {
        get => (Brush)GetValue(AccentForegroundProperty); set => SetValue(AccentForegroundProperty, value);
    }

    public static readonly BindableProperty AccentBrushProperty = BindableProperty.Create(nameof(AccentBrush), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.Transparent));
    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value);
    }

    public static readonly BindableProperty SurfaceBackgroundProperty = BindableProperty.Create(nameof(SurfaceBackground), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.Transparent));
    public Brush SurfaceBackground
    {
        get => (Brush)GetValue(SurfaceBackgroundProperty); set => SetValue(SurfaceBackgroundProperty, value);
    }

    public static readonly BindableProperty HeaderBackgroundProperty = BindableProperty.Create(nameof(HeaderBackground), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.Transparent));
    public Brush HeaderBackground
    {
        get => (Brush)GetValue(HeaderBackgroundProperty); set => SetValue(HeaderBackgroundProperty, value);
    }

    public static readonly BindableProperty FooterBackgroundProperty = BindableProperty.Create(nameof(FooterBackground), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.Transparent));
    public Brush FooterBackground
    {
        get => (Brush)GetValue(FooterBackgroundProperty); set => SetValue(FooterBackgroundProperty, value);
    }

    public static readonly BindableProperty HeaderForegroundProperty = BindableProperty.Create(nameof(HeaderForeground), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.Transparent));
    public Brush HeaderForeground
    {
        get => (Brush)GetValue(HeaderForegroundProperty); set => SetValue(HeaderForegroundProperty, value);
    }

    public static readonly BindableProperty RowBackgroundProperty = BindableProperty.Create(nameof(RowBackground), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.Transparent));
    public Brush RowBackground
    {
        get => (Brush)GetValue(RowBackgroundProperty); set => SetValue(RowBackgroundProperty, value);
    }

    public static readonly BindableProperty AlternateRowBackgroundProperty = BindableProperty.Create(nameof(AlternateRowBackground), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.Transparent));
    public Brush AlternateRowBackground
    {
        get => (Brush)GetValue(AlternateRowBackgroundProperty); set => SetValue(AlternateRowBackgroundProperty, value);
    }

    public static readonly BindableProperty BorderBrushProperty = BindableProperty.Create(nameof(BorderBrush), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.Transparent));
    public Brush BorderBrush
    {
        get => (Brush)GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value);
    }

    public static readonly BindableProperty DividerBrushProperty = BindableProperty.Create(nameof(DividerBrush), typeof(Brush), typeof(FFDataGrid), new SolidColorBrush(Colors.Transparent));
    public Brush DividerBrush
    {
        get => (Brush)GetValue(DividerBrushProperty); set => SetValue(DividerBrushProperty, value);
    }

    public static readonly BindableProperty PrimaryTextColorProperty = BindableProperty.Create(nameof(PrimaryTextColor), typeof(Color), typeof(FFDataGrid), Colors.Black);
    public Color PrimaryTextColor
    {
        get => (Color)GetValue(PrimaryTextColorProperty); set => SetValue(PrimaryTextColorProperty, value);
    }

    public static readonly BindableProperty SecondaryTextColorProperty = BindableProperty.Create(nameof(SecondaryTextColor), typeof(Color), typeof(FFDataGrid), Colors.DarkGray);
    public Color SecondaryTextColor
    {
        get => (Color)GetValue(SecondaryTextColorProperty); set => SetValue(SecondaryTextColorProperty, value);
    }

    public static readonly BindableProperty MutedTextColorProperty = BindableProperty.Create(nameof(MutedTextColor), typeof(Color), typeof(FFDataGrid), Colors.Gray);
    public Color MutedTextColor
    {
        get => (Color)GetValue(MutedTextColorProperty); set => SetValue(MutedTextColorProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(FFDataGrid), 0.0);
    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly BindableProperty HeaderHeightProperty = BindableProperty.Create(nameof(HeaderHeight), typeof(double), typeof(FFDataGrid), 54.0);
    public double HeaderHeight
    {
        get => (double)GetValue(HeaderHeightProperty); set => SetValue(HeaderHeightProperty, value);
    }

    // =================================================================
    // 3. PIPELINE COLLECTIONS & ITEM DATA INTERFACES
    // =================================================================
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(FFDataGrid), null);
    public System.Collections.IEnumerable ItemsSource
    {
        get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty PagedItemsProperty = BindableProperty.Create(nameof(PagedItems), typeof(System.Collections.IEnumerable), typeof(FFDataGrid), null);
    public System.Collections.IEnumerable PagedItems
    {
        get => (System.Collections.IEnumerable)GetValue(PagedItemsProperty); set => SetValue(PagedItemsProperty, value);
    }

    public static readonly BindableProperty RowTemplateProperty = BindableProperty.Create(nameof(RowTemplate), typeof(DataTemplate), typeof(FFDataGrid), null);
    public DataTemplate RowTemplate
    {
        get => (DataTemplate)GetValue(RowTemplateProperty); set => SetValue(RowTemplateProperty, value);
    }

    public static readonly BindableProperty HeaderContentProperty = BindableProperty.Create(nameof(HeaderContent), typeof(View), typeof(FFDataGrid), null);
    public View HeaderContent
    {
        get => (View)GetValue(HeaderContentProperty); set => SetValue(HeaderContentProperty, value);
    }

    public static readonly BindableProperty HasColumnsProperty = BindableProperty.Create(nameof(HasColumns), typeof(bool), typeof(FFDataGrid), true);
    public bool HasColumns
    {
        get => (bool)GetValue(HasColumnsProperty); set => SetValue(HasColumnsProperty, value);
    }

    public static readonly BindableProperty IsNotEmptyProperty = BindableProperty.Create(nameof(IsNotEmpty), typeof(bool), typeof(FFDataGrid), true);
    public bool IsNotEmpty
    {
        get => (bool)GetValue(IsNotEmptyProperty); set => SetValue(IsNotEmptyProperty, value);
    }

    public static readonly BindableProperty IsEmptyProperty = BindableProperty.Create(nameof(IsEmpty), typeof(bool), typeof(FFDataGrid), false);
    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty); set => SetValue(IsEmptyProperty, value);
    }

    public static readonly BindableProperty EmptyTitleProperty = BindableProperty.Create(nameof(EmptyTitle), typeof(string), typeof(FFDataGrid), string.Empty);
    public string EmptyTitle
    {
        get => (string)GetValue(EmptyTitleProperty); set => SetValue(EmptyTitleProperty, value);
    }

    public static readonly BindableProperty EmptyMessageProperty = BindableProperty.Create(nameof(EmptyMessage), typeof(string), typeof(FFDataGrid), string.Empty);
    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty); set => SetValue(EmptyMessageProperty, value);
    }

    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(FFDataGrid), false);
    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value);
    }

    // =================================================================
    // 4. ASYNCHRONOUS PAGINATION SYSTEMS
    // =================================================================
    public static readonly BindableProperty CurrentPageProperty = BindableProperty.Create(nameof(CurrentPage), typeof(int), typeof(FFDataGrid), 1);
    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty); set => SetValue(CurrentPageProperty, value);
    }

    public static readonly BindableProperty TotalItemsProperty = BindableProperty.Create(nameof(TotalItems), typeof(int), typeof(FFDataGrid), 0);
    public int TotalItems
    {
        get => (int)GetValue(TotalItemsProperty); set => SetValue(TotalItemsProperty, value);
    }

    public static readonly BindableProperty TotalPagesProperty = BindableProperty.Create(nameof(TotalPages), typeof(int), typeof(FFDataGrid), 0);
    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty); set => SetValue(TotalPagesProperty, value);
    }

    public static readonly BindableProperty PrevPageCommandProperty = BindableProperty.Create(nameof(PrevPageCommand), typeof(ICommand), typeof(FFDataGrid), null);
    public ICommand PrevPageCommand
    {
        get => (ICommand)GetValue(PrevPageCommandProperty); set => SetValue(PrevPageCommandProperty, value);
    }

    public static readonly BindableProperty NextPageCommandProperty = BindableProperty.Create(nameof(NextPageCommand), typeof(ICommand), typeof(FFDataGrid), null);
    public ICommand NextPageCommand
    {
        get => (ICommand)GetValue(NextPageCommandProperty); set => SetValue(NextPageCommandProperty, value);
    }

    public static readonly BindableProperty RowCommandProperty = BindableProperty.Create(nameof(RowCommand), typeof(ICommand), typeof(FFDataGrid), null);
    public ICommand RowCommand
    {
        get => (ICommand)GetValue(RowCommandProperty); set => SetValue(RowCommandProperty, value);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(e.CurrentSelection.FirstOrDefault() is object selectedItem)
        {
            if(RowCommand!=null&&RowCommand.CanExecute(selectedItem))
            {
                RowCommand.Execute(selectedItem);
            }
            if(sender is CollectionView collectionView)
            {
                collectionView.SelectedItem=null;
            }
        }
    }
}

// =================================================================
// 5. DATA MODEL SUPPORT STRUCTURE CONFIGURATIONS
// =================================================================

public abstract class FFDataGridColumn
{
    public string Header { get; set; } = string.Empty;
    public double Width { get; set; } = -1;

    public bool Sortable { get; set; } = true;

    public FFColumnType Type { get; set; } = FFColumnType.Text;

    public FFAlign Alignment { get; set; } = FFAlign.Left;

    public string Format { get; set; } = string.Empty;

    // =========================================================
    // CORE RENDER CONTRACT
    // =========================================================
    internal abstract View CreateHeader();

    internal abstract View CreateCell(object item);
}

public enum FFColumnType
{
    Text, Number, Date, Currency, Badge, Action
}
public enum FFAlign
{
    Left, Center, Right
}