using System.Collections.ObjectModel;
using System.Windows.Input;


namespace EHMR.Resources.Controls
{
    /// <summary>
    /// Reusable, fully data-driven header: N tabs (with count badges) + a search bar
    /// + N pickers + N action buttons, all properly laid out.
    ///
    /// Usage from a page/dashboard definition:
    ///
    ///   &lt;sc:SparkExplorerHeaderView
    ///       Tabs="{Binding ExplorerTabs}"
    ///       Pickers="{Binding ExplorerPickers}"
    ///       Buttons="{Binding ExplorerButtons}"
    ///       SearchText="{Binding SearchText}"
    ///       SearchPlaceholder="Show columns by search status"
    ///       ShowCyrillicToggle="True"
    ///       UseCyrillicInput="{Binding UseCyrillicSearch, Mode=TwoWay}" /&gt;
    ///
    /// Each Tab/Picker/Button item carries its own Title/Value/Command, so the
    /// number of tabs, buttons, and pickers is entirely driven by the
    /// view model — nothing is hardcoded in XAML.
    ///
    /// ⚠️ NOTE: Grid column headers should bind to SparkDataGridView.Columns directly,
    /// NOT to SparkExplorerHeaderView. This view only handles the search/filter toolbar.
    /// </summary>
    public partial class SparkExplorerHeaderView : ContentView
    {
        private double _responsiveScale = 1d;
        private double _lastResponsiveWidth = -1;

        public SparkExplorerHeaderView()
        {
            InitializeComponent();
        }

        private void OnHeaderSizeChanged(object? sender, EventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            var width = Width;
            if (width <= 0 || Math.Abs(width - _lastResponsiveWidth) < 2)
                return;

            _lastResponsiveWidth = width;
            _responsiveScale = width >= 1500 ? 1d
                : width >= 1250 ? 0.94d
                : width >= 1050 ? 0.88d
                : width >= 900 ? 0.82d
                : 0.76d;

            var s = _responsiveScale;

            ExplorerGrid.Padding = new Thickness(0, 0, 6 * s, Math.Max(1, 1 * s));
            SecondaryToolbarGrid.Padding = new Thickness(10 * s);
            SecondaryToolbarGrid.ColumnSpacing = 8 * s;
            SecondaryToolbarGrid.MinimumHeightRequest = 64 * s;

            HeaderSearchBox.HeightRequest = 42 * s;
            HeaderSearchBox.HorizontalOptions = LayoutOptions.Fill;
            HeaderSearchBox.MinimumWidthRequest = Math.Max(220, 260 * s);
            HeaderSearchBox.Margin = new Thickness(0);

            CyrillicToggleLayout.WidthRequest = 100 * s;
            CyrillicToggleLayout.Spacing = 4 * s;
            CyrillicToggleLayout.IsVisible = ShowCyrillicToggle && width >= 900;

            PickerLayout.Spacing = 8 * s;
            ActionLayout.Spacing = 4 * s;

            foreach (var child in PickerLayout.Children.OfType<FFPicker>())
            {
                child.MinimumWidthRequest = Math.Max(100, 110 * s);
                child.HeightRequest = 42 * s;
            }

            foreach (var child in ActionLayout.Children.SelectMany(v => v is HorizontalStackLayout h ? h.Children : Array.Empty<IView>()))
            {
                if (child is FFButton button)
                {
                    button.HeightRequest = 44 * s;
                    button.ContentPadding = new Thickness(16 * s, 0);
                }
            }

            // Dense desktop widths: keep the toolbar on one line and let the
            // search area absorb the available space rather than stacking controls.
            SecondaryToolbarGrid.ColumnDefinitions.Clear();
            SecondaryToolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            if (CyrillicToggleLayout.IsVisible)
                SecondaryToolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            else
                SecondaryToolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });
            SecondaryToolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            SecondaryToolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        #region Search Visibility
        public static readonly BindableProperty TabsVisibleProperty =
    BindableProperty.Create(
        nameof(TabsVisible),
        typeof(bool),
        typeof(SparkExplorerHeaderView),
        true); // default = visible

        /// <summary>Show/hide the entire tab strip (Row 1).</summary>
        public bool TabsVisible
        {
            get => (bool)GetValue(TabsVisibleProperty);
            set => SetValue(TabsVisibleProperty, value);
        }
        public static readonly BindableProperty SearchVisibleProperty =
            BindableProperty.Create(
                nameof(SearchVisible),
                typeof(bool),
                typeof(SparkExplorerHeaderView),
                true); // default = visible

        public bool SearchVisible
        {
            get => (bool)GetValue(SearchVisibleProperty);
            set => SetValue(SearchVisibleProperty, value);
        }

        #endregion

        #region Tabs

        public static readonly BindableProperty TabsProperty =
            BindableProperty.Create(
                nameof(Tabs),
                typeof(ObservableCollection<SparkTabItem>),
                typeof(SparkExplorerHeaderView),
                defaultValueCreator: _ => new ObservableCollection<SparkTabItem>());

        /// <summary>The tab strip at the top (e.g. "All Users 200,401", "Churned 12,904", ...). Any count.</summary>
        public ObservableCollection<SparkTabItem> Tabs
        {
            get => (ObservableCollection<SparkTabItem>)GetValue(TabsProperty);
            set => SetValue(TabsProperty, value);
        }

        #endregion

        #region Pickers

        public static readonly BindableProperty PickersProperty =
            BindableProperty.Create(
                nameof(Pickers),
                typeof(ObservableCollection<SparkPickerItem>),
                typeof(SparkExplorerHeaderView),
                defaultValueCreator: _ => new ObservableCollection<SparkPickerItem>());

        /// <summary>Dropdowns shown after the search bar (e.g. "Status", "Gender", etc.). Any count, including zero.</summary>
        public ObservableCollection<SparkPickerItem> Pickers
        {
            get => (ObservableCollection<SparkPickerItem>)GetValue(PickersProperty);
            set => SetValue(PickersProperty, value);
        }

        #endregion

        #region Buttons

        public static readonly BindableProperty ButtonsProperty =
            BindableProperty.Create(
                nameof(Buttons),
                typeof(ObservableCollection<SparkButtonItem>),
                typeof(SparkExplorerHeaderView),
                defaultValueCreator: _ => new ObservableCollection<SparkButtonItem>());

        /// <summary>Action buttons on the right (e.g. "Clear Filters", "New"). Any count.</summary>
        public ObservableCollection<SparkButtonItem> Buttons
        {
            get => (ObservableCollection<SparkButtonItem>)GetValue(ButtonsProperty);
            set => SetValue(ButtonsProperty, value);
        }

        #endregion

        #region Search

        public static readonly BindableProperty ShowCyrillicToggleProperty =
      BindableProperty.Create(
          nameof(ShowCyrillicToggle),
          typeof(bool),
          typeof(SparkExplorerHeaderView),
          true); // default = visible — Cyrillic toggle shows unless explicitly turned off

        /// <summary>Show/hide the Cyrillic input toggle checkbox. Defaults to true.</summary>
        public bool ShowCyrillicToggle
        {
            get => (bool)GetValue(ShowCyrillicToggleProperty);
            set => SetValue(ShowCyrillicToggleProperty, value);
        }

        public static readonly BindableProperty UseCyrillicInputProperty =
            BindableProperty.Create(
                nameof(UseCyrillicInput),
                typeof(bool),
                typeof(SparkExplorerHeaderView),
                true,
                BindingMode.TwoWay);

        /// <summary>Whether Cyrillic transliteration is enabled for the search input.</summary>
        public bool UseCyrillicInput
        {
            get => (bool)GetValue(UseCyrillicInputProperty);
            set => SetValue(UseCyrillicInputProperty, value);
        }

        public static readonly BindableProperty SearchPlaceholderProperty =
            BindableProperty.Create(
                nameof(SearchPlaceholder),
                typeof(string),
                typeof(SparkExplorerHeaderView),
                "Пребарување...");

        /// <summary>Placeholder text shown inside the search bar.</summary>
        public string SearchPlaceholder
        {
            get => (string)GetValue(SearchPlaceholderProperty);
            set => SetValue(SearchPlaceholderProperty, value);
        }

        public static readonly BindableProperty SearchCommandProperty =
            BindableProperty.Create(
                nameof(SearchCommand),
                typeof(ICommand),
                typeof(SparkExplorerHeaderView));

        /// <summary>Fired when the user types in the search bar (debounced).</summary>
        public ICommand SearchCommand
        {
            get => (ICommand)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public static readonly BindableProperty SuggestionsProperty =
            BindableProperty.Create(
                nameof(Suggestions),
                typeof(System.Collections.IEnumerable),
                typeof(SparkExplorerHeaderView));

        public System.Collections.IEnumerable? Suggestions
        {
            get => (System.Collections.IEnumerable?)GetValue(SuggestionsProperty);
            set => SetValue(SuggestionsProperty, value);
        }

        public static readonly BindableProperty SuggestionTemplateProperty =
            BindableProperty.Create(
                nameof(SuggestionTemplate),
                typeof(DataTemplate),
                typeof(SparkExplorerHeaderView));

        public DataTemplate? SuggestionTemplate
        {
            get => (DataTemplate?)GetValue(SuggestionTemplateProperty);
            set => SetValue(SuggestionTemplateProperty, value);
        }

        public static readonly BindableProperty ShowSuggestionsProperty =
            BindableProperty.Create(
                nameof(ShowSuggestions),
                typeof(bool),
                typeof(SparkExplorerHeaderView),
                false,
                propertyChanged: (bindable, _, value) =>
                {
                    if(value is true)
                        ((SparkExplorerHeaderView)bindable).ShowSuggestionScrollbar();
                });

        public bool ShowSuggestions
        {
            get => (bool)GetValue(ShowSuggestionsProperty);
            set => SetValue(ShowSuggestionsProperty, value);
        }

        private double _suggestionThumbStartY;

        private int SuggestionCount => Suggestions?.Cast<object>().Count()??0;

        private void ShowSuggestionScrollbar()
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), () =>
                UpdateSuggestionScrollThumb(0, Math.Min(Math.Max(0, SuggestionCount-1), 2)));
        }

        private void OnSuggestionListScrolled(object? sender, ItemsViewScrolledEventArgs e)
        {
            UpdateSuggestionScrollThumb(e.FirstVisibleItemIndex, e.LastVisibleItemIndex);
        }

        private void UpdateSuggestionScrollThumb(int firstVisibleIndex, int lastVisibleIndex)
        {
            var count=SuggestionCount;
            var railHeight=SuggestionScrollRail.Height;
            if(count<=0||railHeight<=0)
                return;

            var visibleCount=Math.Max(1, lastVisibleIndex-firstVisibleIndex+1);
            var thumbHeight=count<=visibleCount
                ? railHeight
                : Math.Max(30, railHeight*visibleCount/count);
            var maxTravel=Math.Max(0, railHeight-thumbHeight);
            var maxFirst=Math.Max(1, count-visibleCount);

            SuggestionScrollThumb.HeightRequest=thumbHeight;
            SuggestionScrollThumb.TranslationY=maxTravel*Math.Clamp(firstVisibleIndex/(double)maxFirst, 0, 1);
        }

        private void OnSuggestionThumbPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            var count=SuggestionCount;
            var maxTravel=Math.Max(0, SuggestionScrollRail.Height-SuggestionScrollThumb.Height);
            if(count<=1||maxTravel<=0)
                return;

            if(e.StatusType==GestureStatus.Started)
            {
                _suggestionThumbStartY=SuggestionScrollThumb.TranslationY;
                return;
            }

            if(e.StatusType!=GestureStatus.Running)
                return;

            var y=Math.Clamp(_suggestionThumbStartY+e.TotalY, 0, maxTravel);
            SuggestionScrollThumb.TranslationY=y;
            var targetIndex=(int)Math.Round((y/maxTravel)*(count-1));
            SuggestionList.ScrollTo(targetIndex, position: ScrollToPosition.Start, animate: false);
        }

        public static readonly BindableProperty SelectedSuggestionProperty =
            BindableProperty.Create(
                nameof(SelectedSuggestion),
                typeof(object),
                typeof(SparkExplorerHeaderView),
                null,
                BindingMode.TwoWay);

        public object? SelectedSuggestion
        {
            get => GetValue(SelectedSuggestionProperty);
            set => SetValue(SelectedSuggestionProperty, value);
        }

        public static readonly BindableProperty TrailingButtonProperty =
            BindableProperty.Create(
                nameof(TrailingButton),
                typeof(SparkButtonItem),
                typeof(SparkExplorerHeaderView));

        /// <summary>Single icon-only action button (e.g. export), if needed.</summary>
        public SparkButtonItem TrailingButton
        {
            get => (SparkButtonItem)GetValue(TrailingButtonProperty);
            set => SetValue(TrailingButtonProperty, value);
        }

        private CancellationTokenSource? _searchDebounceCts;

        public static readonly BindableProperty SearchTextProperty =
            BindableProperty.Create(
                nameof(SearchText),
                typeof(string),
                typeof(SparkExplorerHeaderView),
                string.Empty,
                BindingMode.TwoWay,
                propertyChanged: (b, o, n) =>
                {
                    ((SparkExplorerHeaderView)b).OnSearchTextChanged((string)n);
                });

        /// <summary>The current search query text. Two-way binding.</summary>
        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        private async void OnSearchTextChanged(string newValue)
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts=new CancellationTokenSource();
            var token = _searchDebounceCts.Token;

            try
            {
                await Task.Delay(250, token); // debounce
                if(!token.IsCancellationRequested&&SearchCommand?.CanExecute(newValue)==true)
                {
                    SearchCommand.Execute(newValue);
                }
            }
            catch(TaskCanceledException)
            {
                // expected on rapid typing — ignore
            }
        }

        #endregion

        // ═══════════════════════════════════════════════════════════════════
        // REMOVED: HeaderColumns property and BuildHeaderColumns() method
        //
        // Reason: The SparkExplorerHeaderView should ONLY handle the search/
        // filter toolbar (tabs, search, pickers, buttons). It should NOT
        // manage data grid column headers — that's the job of SparkDataGridView.
        //
        // The grid columns should bind directly to:
        //   <controls:SparkDataGridView Columns="{Binding GridColumns}" ... />
        //
        // NOT to SparkExplorerHeaderView.
        // ═══════════════════════════════════════════════════════════════════
    }
}
