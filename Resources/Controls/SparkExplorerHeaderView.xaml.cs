using System.Collections.ObjectModel;
using System.Collections;
using System.Windows.Input;


namespace EHMR.Resources.Controls
{
    /// <summary>
    /// Reusable, fully data-driven header: N tabs (with count badges) + a search bar
    /// + N pickers + N action buttons, all in one row — matching the "Sparked"
    /// Customer Explorer screens.
    ///
    /// Usage from a page/dashboard definition:
    ///
    ///   &lt;sc:SparkExplorerHeaderView
    ///       Tabs="{Binding ExplorerTabs}"
    ///       Pickers="{Binding ExplorerPickers}"
    ///       Buttons="{Binding ExplorerButtons}"
    ///       SearchText="{Binding SearchText}"
    ///       SearchPlaceholder="Show columns by search status" /&gt;
    ///
    /// Each Tab/Picker/Button item carries its own Title/Value/Command, so the
    /// number of tabs (4, 5, 6, ...) and buttons is entirely driven by the
    /// view model / dashboard definition — nothing is hardcoded in XAML.
    /// </summary>
    public partial class SparkExplorerHeaderView : ContentView
    {
        public SparkExplorerHeaderView()
        {
            InitializeComponent();
        }

        #region Tabs
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

        /// <summary>Dropdowns shown after the search bar (e.g. "Prioritized Searches"). Any count, including zero.</summary>
        public ObservableCollection<SparkPickerItem> Pickers
        {
            get => (ObservableCollection<SparkPickerItem>)GetValue(PickersProperty);
            set => SetValue(PickersProperty, value);
        }

        #endregion

        #region Buttons
        public ObservableCollection<SparkGridColumn> HeaderColumns
        {
            get => (ObservableCollection<SparkGridColumn>)GetValue(HeaderColumnsProperty);
            set => SetValue(HeaderColumnsProperty, value);
        }

        public static readonly BindableProperty HeaderColumnsProperty =
            BindableProperty.Create(
                nameof(HeaderColumns),
                typeof(ObservableCollection<SparkGridColumn>),
                typeof(SparkExplorerHeaderView),
                propertyChanged: (b, o, n) =>
                {
                    ((SparkExplorerHeaderView)b).BuildHeaderColumns();
                });
        private void BuildHeaderColumns()
        {
            if(DataGridHeaderGrid==null) return; // guard if not present in this view

            DataGridHeaderGrid.ColumnDefinitions.Clear();
            foreach(var column in HeaderColumns)
                DataGridHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(column.Width));
        }

        public static readonly BindableProperty ButtonsProperty =
            BindableProperty.Create(
                nameof(Buttons),
                typeof(ObservableCollection<SparkButtonItem>),
                typeof(SparkExplorerHeaderView),
                defaultValueCreator: _ => new ObservableCollection<SparkButtonItem>());

        /// <summary>Action buttons on the right (e.g. "Search", "New", funnel icon). Any count.</summary>
        public ObservableCollection<SparkButtonItem> Buttons
        {
            get => (ObservableCollection<SparkButtonItem>)GetValue(ButtonsProperty);
            set => SetValue(ButtonsProperty, value);
        }

        #endregion

        #region Search

        public static readonly BindableProperty UseCyrillicInputProperty =
            BindableProperty.Create(
                nameof(UseCyrillicInput),
                typeof(bool),
                typeof(SparkExplorerHeaderView),
                true,
                BindingMode.TwoWay);

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

        /// <summary>Fired when the user presses Enter/Search on the search bar (in addition to the "Search" button, if present).</summary>
        public ICommand SearchCommand
        {
            get => (ICommand)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }
        public static readonly BindableProperty TrailingButtonProperty =
            BindableProperty.Create(
          nameof(TrailingButton),
        typeof(SparkButtonItem),
        typeof(SparkExplorerHeaderView));

        /// <summary>Single icon-only action after the pickers (e.g. export/share), per the mock.</summary>
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

        #region Suggestions
        public static readonly BindableProperty SuggestionsProperty =
            BindableProperty.Create(nameof(Suggestions), typeof(IEnumerable), typeof(SparkExplorerHeaderView));

        public IEnumerable? Suggestions
        {
            get => (IEnumerable?)GetValue(SuggestionsProperty);
            set => SetValue(SuggestionsProperty, value);
        }

        public static readonly BindableProperty SuggestionTemplateProperty =
            BindableProperty.Create(nameof(SuggestionTemplate), typeof(DataTemplate), typeof(SparkExplorerHeaderView));

        public DataTemplate? SuggestionTemplate
        {
            get => (DataTemplate?)GetValue(SuggestionTemplateProperty);
            set => SetValue(SuggestionTemplateProperty, value);
        }

        public static readonly BindableProperty ShowSuggestionsProperty =
            BindableProperty.Create(nameof(ShowSuggestions), typeof(bool), typeof(SparkExplorerHeaderView), false);

        public bool ShowSuggestions
        {
            get => (bool)GetValue(ShowSuggestionsProperty);
            set => SetValue(ShowSuggestionsProperty, value);
        }

        public static readonly BindableProperty SelectedSuggestionProperty =
            BindableProperty.Create(nameof(SelectedSuggestion), typeof(object), typeof(SparkExplorerHeaderView), null, BindingMode.TwoWay);

        public object? SelectedSuggestion
        {
            get => GetValue(SelectedSuggestionProperty);
            set => SetValue(SelectedSuggestionProperty, value);
        }

        private double _suggestionThumbStartY;

        private int SuggestionCount => Suggestions?.Cast<object>().Count() ?? 0;

        private void OnSuggestionListScrolled(object? sender, ItemsViewScrolledEventArgs e)
            => UpdateSuggestionScrollThumb(e.FirstVisibleItemIndex, e.LastVisibleItemIndex);

        private void UpdateSuggestionScrollThumb(int firstVisibleIndex, int lastVisibleIndex)
        {
            var count = SuggestionCount;
            var railHeight = SuggestionScrollRail.Height;
            if(count <= 0 || railHeight <= 0)
                return;

            var visibleCount = Math.Max(1, lastVisibleIndex - firstVisibleIndex + 1);
            var thumbHeight = count <= visibleCount
                ? railHeight
                : Math.Max(30, railHeight * visibleCount / count);
            var maxTravel = Math.Max(0, railHeight - thumbHeight);

            SuggestionScrollThumb.HeightRequest = thumbHeight;
            var maxFirst = Math.Max(1, count - visibleCount);
            SuggestionScrollThumb.TranslationY =
                maxTravel * Math.Clamp(firstVisibleIndex / (double)maxFirst, 0, 1);
        }

        private void OnSuggestionThumbPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            var count = SuggestionCount;
            var maxTravel = Math.Max(0, SuggestionScrollRail.Height - SuggestionScrollThumb.Height);
            if(count <= 1 || maxTravel <= 0)
                return;

            if(e.StatusType == GestureStatus.Started)
            {
                _suggestionThumbStartY = SuggestionScrollThumb.TranslationY;
                return;
            }

            if(e.StatusType != GestureStatus.Running)
                return;

            var y = Math.Clamp(_suggestionThumbStartY + e.TotalY, 0, maxTravel);
            SuggestionScrollThumb.TranslationY = y;

            var targetIndex = (int)Math.Round((y / maxTravel) * (count - 1));
            SuggestionList.ScrollTo(targetIndex, position: ScrollToPosition.Start, animate: false);
        }
        #endregion
    }
}
