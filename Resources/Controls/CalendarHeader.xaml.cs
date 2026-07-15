using System.Collections.ObjectModel;
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
    ///   &lt;sc:CalendarHeader
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
    public partial class CalendarHeader : ContentView
    {
        public CalendarHeader()
        {
            InitializeComponent();
        }

        #region Tabs

        public static readonly BindableProperty TabsProperty =
            BindableProperty.Create(
                nameof(Tabs),
                typeof(ObservableCollection<SparkTabItem>),
                typeof(CalendarHeader),
                defaultValueCreator: _ => new ObservableCollection<SparkTabItem>());

        /// <summary>The tab strip at the top (e.g. "All Users 200,401", "Churned 12,904", ...). Any count.</summary>
        public ObservableCollection<SparkTabItem> Tabs
        {
            get => (ObservableCollection<SparkTabItem>)GetValue(TabsProperty);
            set => SetValue(TabsProperty, value);
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
                typeof(CalendarHeader));
              
   
        public static readonly BindableProperty ButtonsProperty =
            BindableProperty.Create(
                nameof(Buttons),
                typeof(ObservableCollection<SparkButtonItem>),
                typeof(CalendarHeader),
                defaultValueCreator: _ => new ObservableCollection<SparkButtonItem>());

        /// <summary>Action buttons on the right (e.g. "Search", "New", funnel icon). Any count.</summary>
        public ObservableCollection<SparkButtonItem> Buttons
        {
            get => (ObservableCollection<SparkButtonItem>)GetValue(ButtonsProperty);
            set => SetValue(ButtonsProperty, value);
        }

        #endregion

       

    }
}
