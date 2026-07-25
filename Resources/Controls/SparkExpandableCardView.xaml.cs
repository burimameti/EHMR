using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.Resources.Controls;

public partial class SparkExpandableCard : ContentView
{

    private readonly ObservableCollection<object> _items = [];

    private bool _isAnimating;

    private async void ExpandTapped(
        object? sender,
        TappedEventArgs e)
    {
        if(_isAnimating)
            return;

        IsExpanded=!IsExpanded;
    }
    public SparkExpandableCard()
    {
        InitializeComponent();

        Loaded+=OnLoaded;
        Unloaded+=OnUnloaded;

        ItemsHost.ItemsSource=_items;

        ItemsHost.SelectionChanged+=OnSelectionChanged;
    }

    private bool _isLoaded;
    private void ApplyCardContent()
    {
        if(ContentHost==null)
            return;

        if(CardContent==null)
            return;

        ContentHost.Content=CardContent;

        ContentHost.IsVisible=true;

        ItemsHost.IsVisible=false;

        EmptyHost.IsVisible=false;
    }
    private void UpdateEmptyView()
    {
        if(EmptyHost==null)
            return;

        bool isEmpty = ItemCount==0;

        EmptyHost.IsVisible=isEmpty;

        ItemsHost.IsVisible=
            !isEmpty&&
            ItemsSource!=null;

        ContentHost.IsVisible=
            !isEmpty&&
            CardContent!=null&&
            ItemsSource==null;
    }
    private void UpdateCount()
    {
        ItemCount=_items.Count;

        if(FooterLabel!=null)
        {
            FooterLabel.Text=
                $"Showing {Math.Min(ItemCount, MaxVisibleItems)} of {ItemCount}";
        }
    }
    private void ApplyItemsSource()
    {
        if(ItemsHost==null)
            return;

        if(ItemsSource==null)
        {
            _items.Clear();

            UpdateCount();
            UpdateFooter();
            UpdateEmptyView();

            return;
        }

        _items.Clear();
        foreach(var item in ItemsSource.Cast<object>())
            _items.Add(item);

        ItemsHost.ItemsSource=_items;

        ItemsHost.ItemTemplate=ItemTemplate;

        ItemsHost.EmptyView=
            EmptyView??
            new Label
            {
                Text=EmptyViewText,
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center,
                TextColor=Colors.Gray,
                FontSize=12
            };

        ContentHost.IsVisible=false;

        ItemsHost.IsVisible=true;

        UpdateCount();

        UpdateEmptyView();
    }
    private void OnLoaded(object? sender, EventArgs e)
    {
        if(_isLoaded)
            return;

        _isLoaded=true;

        ApplyVariant(Variant);
        ApplyItemsSource();
        ApplyCardContent();
        UpdateCount();
        UpdateExpandState(IsExpanded);
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        ItemsHost.SelectionChanged-=OnSelectionChanged;
        _isLoaded=false;
    }

    private void OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        var item =
            e.CurrentSelection.FirstOrDefault();

        if(item==null)
            return;

        SelectedItem=item;

        if(SelectionChangedCommand?.CanExecute(item)==true)
            SelectionChangedCommand.Execute(item);

        if(ItemTappedCommand?.CanExecute(item)==true)
            ItemTappedCommand.Execute(item);

        ItemsHost.SelectedItem=null;
    }

    #region Header
    public static readonly BindableProperty SearchContentProperty =
    BindableProperty.Create(
        nameof(SearchContent),
        typeof(View),
        typeof(SparkExpandableCard),
        propertyChanged: OnSearchChanged);

    public View? SearchContent
    {
        get => (View?)GetValue(SearchContentProperty);
        set => SetValue(SearchContentProperty, value);
    }

    private static void OnSearchChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is not SparkExpandableCard card)
            return;

        card.SearchHost.Content=(View?)newValue;

        card.SearchHost.IsVisible=
            newValue!=null;
    }
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(SparkExpandableCard),
            string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(
            nameof(Subtitle),
            typeof(string),
            typeof(SparkExpandableCard),
            string.Empty,
            propertyChanged: OnSubtitleChanged);

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool HasSubtitle =>
        !string.IsNullOrWhiteSpace(Subtitle);

    private static void OnSubtitleChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is SparkExpandableCard card)
            card.OnPropertyChanged(nameof(HasSubtitle));
    }

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(
            nameof(Icon),
            typeof(string),
            typeof(SparkExpandableCard),
            string.Empty);

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    #endregion

    #region Expand

    public static readonly BindableProperty IsExpandedProperty =
        BindableProperty.Create(
            nameof(IsExpanded),
            typeof(bool),
            typeof(SparkExpandableCard),
            false,
            propertyChanged: OnExpandedChanged);

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    private static void OnExpandedChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is SparkExpandableCard card)
            card.UpdateExpandState((bool)newValue);
    }

    #endregion

    #region Variant

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(
            nameof(Variant),
            typeof(SparkCardVariant),
            typeof(SparkExpandableCard),
            SparkCardVariant.Default,
            propertyChanged: OnVariantChanged);

    public SparkCardVariant Variant
    {
        get => (SparkCardVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    private static void OnVariantChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is SparkExpandableCard card)
            card.ApplyVariant((SparkCardVariant)newValue);
    }

    #endregion

    #region Content

    public static readonly BindableProperty CardContentProperty =
        BindableProperty.Create(
            nameof(CardContent),
            typeof(View),
            typeof(SparkExpandableCard),
            null,
            propertyChanged: OnCardContentChanged);

    public View? CardContent
    {
        get => (View?)GetValue(CardContentProperty);
        set => SetValue(CardContentProperty, value);
    }

    private static void OnCardContentChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is SparkExpandableCard card)
            card.ApplyCardContent();
    }

    #endregion

    #region CollectionView

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(SparkExpandableCard),
            null,
            propertyChanged: OnItemsSourceChanged);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty ItemTemplateProperty =
        BindableProperty.Create(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(SparkExpandableCard));

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly BindableProperty EmptyViewProperty =
        BindableProperty.Create(
            nameof(EmptyView),
            typeof(object),
            typeof(SparkExpandableCard));

    public object? EmptyView
    {
        get => GetValue(EmptyViewProperty);
        set => SetValue(EmptyViewProperty, value);
    }

    public static readonly BindableProperty EmptyViewTextProperty =
        BindableProperty.Create(
            nameof(EmptyViewText),
            typeof(string),
            typeof(SparkExpandableCard),
            "No records.");

    public string EmptyViewText
    {
        get => (string)GetValue(EmptyViewTextProperty);
        set => SetValue(EmptyViewTextProperty, value);
    }

    private static void OnItemsSourceChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if(bindable is SparkExpandableCard card)
            card.ApplyItemsSource();
    }

    #endregion

    #region Selection

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(
            nameof(SelectedItem),
            typeof(object),
            typeof(SparkExpandableCard),
            null,
            BindingMode.TwoWay);

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly BindableProperty SelectionChangedCommandProperty =
        BindableProperty.Create(
            nameof(SelectionChangedCommand),
            typeof(ICommand),
            typeof(SparkExpandableCard));

    public ICommand? SelectionChangedCommand
    {
        get => (ICommand?)GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommand),
            typeof(ICommand),
            typeof(SparkExpandableCard));

    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }

    #endregion

    #region Options

    public static readonly BindableProperty ShowCountProperty =
        BindableProperty.Create(
            nameof(ShowCount),
            typeof(bool),
            typeof(SparkExpandableCard),
            true);

    public bool ShowCount
    {
        get => (bool)GetValue(ShowCountProperty);
        set => SetValue(ShowCountProperty, value);
    }

    public static readonly BindableProperty ShowFooterProperty =
        BindableProperty.Create(
            nameof(ShowFooter),
            typeof(bool),
            typeof(SparkExpandableCard),
            false);

    public bool ShowFooter
    {
        get => (bool)GetValue(ShowFooterProperty);
        set => SetValue(ShowFooterProperty, value);
    }

    public static readonly BindableProperty MaxVisibleItemsProperty =
        BindableProperty.Create(
            nameof(MaxVisibleItems),
            typeof(int),
            typeof(SparkExpandableCard),
            5);

    public int MaxVisibleItems
    {
        get => (int)GetValue(MaxVisibleItemsProperty);
        set => SetValue(MaxVisibleItemsProperty, value);
    }

    #endregion

    #region ReadOnly

    private int _itemCount;

    public int ItemCount
    {
        get => _itemCount;
        private set
        {
            if(_itemCount==value)
                return;

            _itemCount=value;
            OnPropertyChanged(nameof(ItemCount));
        }
    }

    #endregion

    private async void UpdateExpandState(bool expanded)
    {
        if(ExpandableArea==null||ExpandIcon==null)
            return;

        if(_isAnimating)
            return;

        _isAnimating=true;

        try
        {
            if(expanded)
            {
                ExpandableArea.IsVisible=true;

                ExpandIcon.RotateTo(180, 180, Easing.CubicOut);

                ExpandableArea.Opacity=0;

                await ExpandableArea.FadeTo(
                    1,
                    180,
                    Easing.CubicOut);
            }
            else
            {
                ExpandIcon.RotateTo(
                    0,
                    180,
                    Easing.CubicOut);

                await ExpandableArea.FadeTo(
                    0,
                    150,
                    Easing.CubicIn);

                ExpandableArea.IsVisible=false;
            }
        }
        finally
        {
            _isAnimating=false;
        }
    }

    private void UpdateFooter()
    {
        if(FooterGrid==null)
            return;

        FooterGrid.IsVisible=
            ShowFooter&&
            ItemCount>0;

        if(!FooterGrid.IsVisible)
            return;

        FooterLabel.Text=
            $"Showing {Math.Min(ItemCount, MaxVisibleItems)} of {ItemCount}";
        UpdateFooter();
    }
    private void ApplyVariant(
     SparkCardVariant variant)
    {
        Icon=variant switch
        {
            SparkCardVariant.Patient => "👤",

            SparkCardVariant.Encounter => "📋",

            SparkCardVariant.Diagnosis => "🩺",

            SparkCardVariant.Appointment => "📅",

            SparkCardVariant.Therapy => "💉",

            SparkCardVariant.TherapyCycle => "🔄",

            SparkCardVariant.Prescription => "💊",

            SparkCardVariant.Document => "📄",

            SparkCardVariant.Warning => "⚠",

            SparkCardVariant.Success => "✔",

            SparkCardVariant.Medicine => "💉",

            _ => "•"
        };
    }
}