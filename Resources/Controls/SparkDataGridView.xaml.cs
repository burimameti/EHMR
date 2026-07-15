
using EHMR.Resources.Theming;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;

namespace EHMR.Resources.Controls
{
    /// <summary>
    /// A lightweight, style-matched data grid for MAUI (there is no native DataGrid).
    /// Fully driven by two collections:
    ///   Columns — header text, data key, cell type (Text/Badge/Currency/Number/Avatar), width
    ///   Rows    — one SparkGridRow (dictionary) per record, keyed the same as Columns
    ///
    /// Usage:
    ///   &lt;sc:SparkDataGridView Columns="{Binding Columns}" Rows="{Binding Rows}"
    ///                          RowTappedCommand="{Binding OpenCustomerCommand}" /&gt;
    ///
    /// The grid rebuilds itself whenever Columns or Rows is reassigned. For frequent
    /// in-place row edits, reassign the Rows collection rather than mutating items in place.
    /// </summary>
    public partial class SparkDataGridView : ContentView
    {
        public SparkDataGridView()
        {
            InitializeComponent();
        }
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if(Handler!=null)
            {
                BuildPager();
            }
        }
        // Computed once per BuildGrid() — how many synthetic (non-data) columns sit
        // before the real Columns start. Used everywhere a data-column index needs
        // to be translated into a grid-column index (AttachRowTap, dividers, etc.)
        // instead of every call site re-deriving it (that's what caused the
        // AttachRowTap / row-number-cell mismatches before).
        private int _columnOffset;

        #region Bindable properties
        public static readonly BindableProperty AutoSelectFirstRowProperty =
    BindableProperty.Create(
        nameof(AutoSelectFirstRow),
        typeof(bool),
        typeof(SparkDataGridView),
        true,
        propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());

        /// <summary>Default true: row 0 is highlighted the moment data loads. Set False for
        /// grids like PatientListPage where no row should look "current" until the user acts.</summary>
        public bool AutoSelectFirstRow
        {
            get => (bool)GetValue(AutoSelectFirstRowProperty);
            set => SetValue(AutoSelectFirstRowProperty, value);
        }
        public static readonly BindableProperty AllowHorizontalScrollProperty =
            BindableProperty.Create(
                nameof(AllowHorizontalScroll),
                typeof(bool),
                typeof(SparkDataGridView),
                false,
                propertyChanged: (b, o, n) =>
                {
                    var view = (SparkDataGridView)b;
                    view.HostScroll.Orientation=(bool)n ? ScrollOrientation.Both : ScrollOrientation.Vertical;
                    view.BuildGrid();
                });

        /// <summary>Default False: vertical scroll only, one Fill column stretches to the container edge
        /// (use for grids that fit on screen, e.g. the patient list). Set True only for grids with more
        /// fixed-width columns than fit on screen — Fill columns are ignored in that mode.</summary>
        public bool AllowHorizontalScroll
        {
            get => (bool)GetValue(AllowHorizontalScrollProperty);
            set => SetValue(AllowHorizontalScrollProperty, value);
        }

        public static readonly BindableProperty ShowCheckboxColumnProperty =
            BindableProperty.Create(
                nameof(ShowCheckboxColumn),
                typeof(bool),
                typeof(SparkDataGridView),
                true,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());

        /// <summary>Adds a checkbox column left of the row-number column. Default true.</summary>
        public bool ShowCheckboxColumn
        {
            get => (bool)GetValue(ShowCheckboxColumnProperty);
            set => SetValue(ShowCheckboxColumnProperty, value);
        }

        // FIX: "propertyDefaultValueCreator" is not a real parameter of BindableProperty.Create —
        // that was a typo that would not compile. SparkGridRow is a reference type so the
        // implicit default (null) is fine without any creator/default argument at all.
        public static readonly BindableProperty SelectedRowProperty =
            BindableProperty.Create(
                nameof(SelectedRow),
                typeof(SparkGridRow),
                typeof(SparkDataGridView),
                default(SparkGridRow),
                BindingMode.TwoWay);

        /// <summary>The actual selected row object (Tag included) — exposes what SelectedRowIndex points to,
        /// for binding from the ViewModel. Set externally (e.g. from a future "select" button) to drive
        /// selection back into the grid — BuildGrid will sync SelectedRowIndex to match on next rebuild.</summary>
        public SparkGridRow SelectedRow
        {
            get => (SparkGridRow)GetValue(SelectedRowProperty);
            set => SetValue(SelectedRowProperty, value);
        }

        public static readonly BindableProperty SelectionChangedCommandProperty =
            BindableProperty.Create(
                nameof(SelectionChangedCommand),
                typeof(ICommand),
                typeof(SparkDataGridView));

        /// <summary>Fired (with row.Tag) whenever the checkbox selection changes — wire a button/toolbar
        /// action to this in the next phase without touching grid internals.</summary>
        public ICommand SelectionChangedCommand
        {
            get => (ICommand)GetValue(SelectionChangedCommandProperty);
            set => SetValue(SelectionChangedCommandProperty, value);
        }

        public static readonly BindableProperty ColumnsProperty =
            BindableProperty.Create(
                nameof(Columns),
                typeof(ObservableCollection<SparkGridColumn>),
                typeof(SparkDataGridView),
                defaultValueCreator: _ => new ObservableCollection<SparkGridColumn>(),
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());

        public ObservableCollection<SparkGridColumn> Columns
        {
            get => (ObservableCollection<SparkGridColumn>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly BindableProperty RowsProperty =
            BindableProperty.Create(
                nameof(Rows),
                typeof(ObservableCollection<SparkGridRow>),
                typeof(SparkDataGridView),
                defaultValueCreator: _ => new ObservableCollection<SparkGridRow>(),
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());

        public ObservableCollection<SparkGridRow> Rows
        {
            get => (ObservableCollection<SparkGridRow>)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public static readonly BindableProperty RowTappedCommandProperty =
            BindableProperty.Create(
                nameof(RowTappedCommand),
                typeof(ICommand),
                typeof(SparkDataGridView));

        /// <summary>Invoked with the tapped SparkGridRow's Tag (falls back to the row itself) as parameter.</summary>
        public ICommand RowTappedCommand
        {
            get => (ICommand)GetValue(RowTappedCommandProperty);
            set => SetValue(RowTappedCommandProperty, value);
        }

        public static readonly BindableProperty EditRowCommandProperty =
            BindableProperty.Create(
                nameof(EditRowCommand),
                typeof(ICommand),
                typeof(SparkDataGridView));

        /// <summary>Invoked (with the row's Tag) when the pencil icon in an Actions column is tapped.
        /// Leave unset if you don't use an Actions column.</summary>
        public ICommand EditRowCommand
        {
            get => (ICommand)GetValue(EditRowCommandProperty);
            set => SetValue(EditRowCommandProperty, value);
        }

        public static readonly BindableProperty CurrentPageProperty =
            BindableProperty.Create(
                nameof(CurrentPage),
                typeof(int),
                typeof(SparkDataGridView),
                1,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildPager());

        public int CurrentPage
        {
            get => (int)GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, value);
        }
  
        public static readonly BindableProperty TotalPagesProperty =
            BindableProperty.Create(
                nameof(TotalPages),
                typeof(int),
                typeof(SparkDataGridView),
                1,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildPager());

        public int TotalPages
        {
            get => (int)GetValue(TotalPagesProperty);
            set => SetValue(TotalPagesProperty, value);
        }

        public static readonly BindableProperty PageChangedCommandProperty =
            BindableProperty.Create(
                nameof(PageChangedCommand),
                typeof(ICommand),
                typeof(SparkDataGridView));

        /// <summary>Invoked with the tapped page number (int) when a numbered pager button is tapped.</summary>
        public ICommand PageChangedCommand
        {
            get => (ICommand)GetValue(PageChangedCommandProperty);
            set => SetValue(PageChangedCommandProperty, value);
        }

        public static readonly BindableProperty PreviousPageCommandProperty =
            BindableProperty.Create(
                nameof(PreviousPageCommand),
                typeof(ICommand),
                typeof(SparkDataGridView));

        public ICommand PreviousPageCommand
        {
            get => (ICommand)GetValue(PreviousPageCommandProperty);
            set => SetValue(PreviousPageCommandProperty, value);
        }

        public static readonly BindableProperty NextPageCommandProperty =
            BindableProperty.Create(
                nameof(NextPageCommand),
                typeof(ICommand),
                typeof(SparkDataGridView));

        public ICommand NextPageCommand
        {
            get => (ICommand)GetValue(NextPageCommandProperty);
            set => SetValue(NextPageCommandProperty, value);
        }

        public static readonly BindableProperty ShowPaginationProperty =
            BindableProperty.Create(
                nameof(ShowPagination),
                typeof(bool),
                typeof(SparkDataGridView),
                true,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildPager());

        /// <summary>Set False to hide the built-in pager (e.g. for a short, unpaginated list).</summary>
        public bool ShowPagination
        {
            get => (bool)GetValue(ShowPaginationProperty);
            set => SetValue(ShowPaginationProperty, value);
        }

        #endregion

        #region Build

        private static readonly Color HeaderTextColor = Color.FromArgb("#FFFFFF");
        private static readonly Color RowTextColor = Color.FromArgb("#2E3A4E");
        private static readonly Color RowMutedTextColor = Color.FromArgb("#5B6B85");
        private static readonly Color BorderColor = Color.FromArgb("#F0F2F5");
        private static readonly Color RowAltBg = Color.FromArgb("#F7F9FB");
        private static readonly Color RowBg = Colors.White;

        private static readonly Color BadgeNeutralBg = Color.FromArgb("#EDF1F5");
        private static readonly Color BadgeNeutralText = Color.FromArgb("#5B6B85");
        private static readonly Color BadgeSuccessBg = Color.FromArgb("#E7F6E9");
        private static readonly Color BadgeSuccessText = Color.FromArgb("#3CB35B");
        private static readonly Color BadgeDangerBg = Color.FromArgb("#FDE8E8");
        private static readonly Color BadgeDangerText = Color.FromArgb("#E0554F");

        private void BuildGrid()
        {
            GridRoot.Children.Clear();
            GridRoot.RowDefinitions.Clear();
            GridRoot.ColumnDefinitions.Clear();

            if(Columns==null||Columns.Count==0)
                return;

            // Synthetic leading columns, in on-screen order: checkbox (0) then row-number (1).
            // _columnOffset is the single source of truth other methods (AttachRowTap,
            // AddRowDivider) read from, instead of each recomputing ShowRowNumbers/ShowCheckboxColumn
            // math themselves and risking drift.
            int checkboxColumnIndex = 0;
            int rowNumberColumnIndex = ShowCheckboxColumn ? 1 : 0;
            _columnOffset=(ShowCheckboxColumn ? 1 : 0)+(ShowRowNumbers ? 1 : 0);

            if(ShowCheckboxColumn)
                GridRoot.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(36) });

            if(ShowRowNumbers)
                GridRoot.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(40) });

            foreach(var column in Columns)
            {
                GridRoot.ColumnDefinitions.Add(new ColumnDefinition { Width=column.Width });
            }

            // Header
            GridRoot.RowDefinitions.Add(new RowDefinition { Height=new GridLength(44) });

            if(ShowCheckboxColumn)
                AddCheckboxHeaderCell(checkboxColumnIndex);

            if(ShowRowNumbers)
                AddRowNumberHeaderCell(rowNumberColumnIndex);

            for(int c = 0; c<Columns.Count; c++)
            {
                AddHeaderCell(Columns[c], c+_columnOffset);
                if(c<Columns.Count-1)
                    AddColumnDivider(0, c+_columnOffset);
            }

            // Rows
            var rows = Rows??new ObservableCollection<SparkGridRow>();

            // FIX: default-select the first row the moment data lands, instead of requiring
            // a tap before anything looks "current". Clamp in case the page has fewer rows
            // than the previous SelectedRowIndex (e.g. last page of results).
           
            if(AutoSelectFirstRow&&rows.Count>0&&(SelectedRowIndex<0||SelectedRowIndex>=rows.Count))
            {
                SelectedRowIndex=0;
                return;
            }

            // ако AutoSelectFirstRow е false, само clamp-увај без force-select на 0
            if(!AutoSelectFirstRow&&SelectedRowIndex>=rows.Count)
                SelectedRowIndex=-1;
            for(int r = 0; r<rows.Count; r++)
            {
                GridRoot.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });

                int rowIndex = r+1;
                bool isSelected = r==SelectedRowIndex;
                var rowBg = isSelected ? SelectedRowBg : (r%2==0 ? RowBg : RowAltBg);
                //CheckBoxForNow will be diableds
                //if(ShowCheckboxColumn)
                //    AddCheckboxCell(r, rows[r], rowIndex, rowBg, checkboxColumnIndex);

                if(ShowRowNumbers)
                    AddRowNumberCell(r, rowIndex, rowBg, rowNumberColumnIndex);

                for(int c = 0; c<Columns.Count; c++)
                {
                    AddDataCell(Columns[c], rows[r], c+_columnOffset, rowIndex, rowBg);
                    if(c<Columns.Count-1)
                        AddColumnDivider(rowIndex, c+_columnOffset);
                }

                AddRowDivider(rowIndex);
                AttachRowTap(rows[r], rowIndex, r);
            }

            // keep the TwoWay SelectedRow in sync with whatever index BuildGrid landed on
            // (covers the clamp-to-0 case above, and any external SelectedRowIndex set).
            SelectedRow=(SelectedRowIndex>=0&&SelectedRowIndex<rows.Count) ? rows[SelectedRowIndex] : null;

            BuildPager();
        }

        private void AddCheckboxHeaderCell(int columnIndex)
        {
            var border = new Border
            {
                Background=new SolidColorBrush(Color.FromArgb("#5B6B79")),
                Stroke=Colors.Transparent,
                Padding=new Thickness(4, 0)
            };
            // empty for now — "select all" checkbox is a natural fit here later,
            // deliberately left as a stub since that's next-phase too
            border.Content=new Label { Text="", HorizontalOptions=LayoutOptions.Fill };
            Grid.SetRow(border, 0);
            Grid.SetColumn(border, columnIndex);
            GridRoot.Children.Add(border);
        }

        private void AddCheckboxCell(int localRowIndex, SparkGridRow row, int gridRowIndex, Color rowBg, int columnIndex)
        {
            var border = new Border
            {
                BackgroundColor=rowBg,
                StrokeThickness=0,
                Padding=new Thickness(4, 0),
                HeightRequest=44
            };

            var checkbox = new CheckBox
            {
                IsChecked=localRowIndex==SelectedRowIndex,
                Color=Color.FromArgb("#21B6C4"), // same tone as PagerActiveBg
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center
            };

            checkbox.CheckedChanged+=(s, e) =>
            {
                if(!e.Value) return; // ignore uncheck — rebuild already clears the others

                SelectedRowIndex=localRowIndex; // triggers BuildGrid via propertyChanged
                SelectionChangedCommand?.Execute(row.Tag??row);
                // NOTE: SelectedRow is set inside BuildGrid (after the rebuild this line
                // triggers), so it always reflects the row at the current SelectedRowIndex —
                // setting it here too would just be overwritten on rebuild.
            };

            border.Content=checkbox;
            Grid.SetRow(border, gridRowIndex);
            Grid.SetColumn(border, columnIndex);
            GridRoot.Children.Add(border);
        }

        private void AddRowNumberHeaderCell(int columnIndex)
        {
            var border = new Border
            {
                Background=new SolidColorBrush(Color.FromArgb("#5B6B79")),
                Stroke=Colors.Transparent,
                Padding=new Thickness(4, 0)
            };
            border.Content=new Label
            {
                Text="#",
                TextColor=HeaderTextColor,
                FontSize=12,
                HorizontalTextAlignment=TextAlignment.Center,
                HorizontalOptions=LayoutOptions.Fill
            };
            Grid.SetRow(border, 0);
            Grid.SetColumn(border, columnIndex);
            GridRoot.Children.Add(border);
        }

        private void AddRowNumberCell(int localRowIndex, int gridRowIndex, Color rowBg, int columnIndex)
        {
            // Page-relative for now (1, 2, 3, ... within the current page). If you want
            // globally continuous numbering across pages instead, multiply by page size
            // once SparkDataGridView exposes a PageSize bindable — CurrentPage alone
            // isn't enough since page size isn't currently passed to this control.
            var border = new Border
            {
                BackgroundColor=rowBg,
                StrokeThickness=0,
                Padding=new Thickness(4, 0),
                HeightRequest=44
            };
            border.Content=new Label
            {
                Text=(localRowIndex+1).ToString(),
                TextColor=RowMutedTextColor,
                FontSize=12,
                HorizontalTextAlignment=TextAlignment.Center,
                VerticalTextAlignment=TextAlignment.Center,
                HorizontalOptions=LayoutOptions.Fill
            };
            Grid.SetRow(border, gridRowIndex);
            Grid.SetColumn(border, columnIndex);
            GridRoot.Children.Add(border);
        }

        /// <summary>One hairline under the row, spanning all columns (synthetic + data) — replaces
        /// per-cell 4-sided borders so the table reads as continuous rows instead of boxed cells.</summary>
        private void AddRowDivider(int rowIndex)
        {
            var divider = new BoxView
            {
                Color=BorderColor,
                HeightRequest=1,
                VerticalOptions=LayoutOptions.End
            };
            Grid.SetRow(divider, rowIndex);
            Grid.SetColumn(divider, 0);
            // FIX: was Columns.Count only, which left the checkbox/row-number columns
            // without the bottom hairline. Span the full synthetic+data width instead.
            Grid.SetColumnSpan(divider, _columnOffset+Columns.Count);
            GridRoot.Children.Add(divider);
        }

        private void AddColumnDivider(int rowIndex, int columnIndex)
        {
            var divider = new BoxView
            {
                Color=BorderColor,
                WidthRequest=1,
                HorizontalOptions=LayoutOptions.End
            };
            Grid.SetRow(divider, rowIndex);
            Grid.SetColumn(divider, columnIndex);
            GridRoot.Children.Add(divider);
        }

        private void AddHeaderCell(SparkGridColumn column, int columnIndex)
        {
            var border = new Border
            {
                Background=new SolidColorBrush(Color.FromArgb("#5B6B79")),
                Stroke=Colors.Transparent,
                Padding=new Thickness(12, 0),
                HorizontalOptions=LayoutOptions.Fill
            };
            var row = new HorizontalStackLayout
            {
                Spacing=4,
                VerticalOptions=LayoutOptions.Center,
                // Badge/Actions headers center over their centered data cells;
                // everything else stays start-aligned to match its data cells.
                HorizontalOptions=column.CellType switch
                {
                    SparkGridCellType.Badge => LayoutOptions.Center,
                    SparkGridCellType.Actions => LayoutOptions.Center,
                    _ => LayoutOptions.Start
                }
            };

            row.Children.Add(new Label
            {
                Text=column.Header,
                TextColor=HeaderTextColor,
                FontSize=12,
                VerticalOptions=LayoutOptions.Center
            });

            if(column.Sortable)
            {
                row.Children.Add(new Label
                {
                    Text="\u25BE", // ▾
                    TextColor=HeaderTextColor,
                    FontSize=10,
                    VerticalOptions=LayoutOptions.Center
                });
            }

            border.Content=row;

            if(column.HeaderTapCommand!=null)
            {
                var tap = new TapGestureRecognizer
                {
                    Command=column.HeaderTapCommand,
                    CommandParameter=column.Key
                };
                border.GestureRecognizers.Add(tap);
            }

            Grid.SetRow(border, 0);
            Grid.SetColumn(border, columnIndex);
            GridRoot.Children.Add(border);
        }

        public static readonly BindableProperty PageInfoTextProperty =
            BindableProperty.Create(
                nameof(PageInfoText),
                typeof(string),
                typeof(SparkDataGridView),
                string.Empty,
                propertyChanged: (b, oldValue, newValue) =>
                {
                    var grid = (SparkDataGridView)b;

                    if(grid.PagerInfoLabel!=null)
                    {
                        grid.PagerInfoLabel.Text=
                            newValue?.ToString()??string.Empty;
                    }
                });

        /// <summary>Simple text pager mode (e.g. "21 резултати"), rendered centered between the
        /// ‹ › arrows. Used when the consumer page manages its own count/paging text instead of
        /// driving the numbered-pill pager via CurrentPage/TotalPages — set this, leave TotalPages
        /// at its default (1), and BuildPager() falls back to the simple text layout automatically.</summary>
        public string PageInfoText
        {
            get => (string)GetValue(PageInfoTextProperty);
            set => SetValue(PageInfoTextProperty, value);
        }

        private void AddDataCell(
            SparkGridColumn column,
            SparkGridRow row,
            int columnIndex,
            int rowIndex,
            Color rowBg)
        {
            row.TryGetValue(column.Key, out var value);

            var border = new Border
            {
                BackgroundColor=rowBg,
                StrokeThickness=0,
                Padding=new Thickness(12, 0),
                HeightRequest=44,
                HorizontalOptions=LayoutOptions.Fill,
                VerticalOptions=LayoutOptions.Fill
            };

            View content = column.CellType switch
            {
                SparkGridCellType.Badge => BuildBadge(value as SparkBadgeValue),
                SparkGridCellType.Currency => BuildText(FormatCurrency(value), true),
                SparkGridCellType.Number => BuildText(value?.ToString()??string.Empty, true),
                SparkGridCellType.Avatar => BuildAvatar(value?.ToString()),
                SparkGridCellType.Actions => BuildActions(row),
                SparkGridCellType.Button => BuildSingleButton(value as SparkButtonItem, row),  // ← ново
                _ => BuildText(value?.ToString()??string.Empty, false)
            };

            content.HorizontalOptions=column.CellType switch
            {
                SparkGridCellType.Badge => LayoutOptions.Center,
                SparkGridCellType.Actions => LayoutOptions.Center,
                SparkGridCellType.Button => LayoutOptions.Center,   // ← додади
                _ => LayoutOptions.Fill
            };
            content.VerticalOptions=LayoutOptions.Center;

            Grid.SetRow(border, rowIndex);
            Grid.SetColumn(border, columnIndex);

            border.Content=content;

            GridRoot.Children.Add(border);
        }
        private static View BuildSingleButton(SparkButtonItem item, SparkGridRow row)
        {
            if(item==null) return new Label();

            return BuildActionIcon(
                item.Label??"",
                item.Command,
                item.CommandParameter,
                item.IsPrimary ? "#21B6C4" : "#69D3DD");
        }
        private View BuildActions(SparkGridRow row)
        {
            if(!row.TryGetValue("Actions", out var value))
                return new Label();

            if(value is not IEnumerable<SparkButtonItem> actions)
                return new Label();


            var layout = new HorizontalStackLayout
            {
                Spacing=8,
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center
            };


            foreach(var action in actions)
            {
                layout.Children.Add(
                    BuildActionIcon(
                        action.Label??"",
                        action.Command,
                        action.CommandParameter??row.Tag??row,
                        action.IsPrimary
                            ? "#21B6C4"
                            : "#8FA2AB"));
            }


            return layout;
        }
       
        private static View BuildActionIcon(string text, ICommand command, object commandParameter, string color)
        {
            return new Border
            {
                Padding=new Thickness(4, 4),
                BackgroundColor=Color.FromArgb(color),
                StrokeThickness=0,
                StrokeShape=new RoundRectangle { CornerRadius=12 },
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center,
                MinimumWidthRequest=90,
                MinimumHeightRequest=26,

                Content=new Label
                {
                    Text=text,
                    FontSize=11,
                    TextColor=Colors.White,
                    HorizontalTextAlignment=TextAlignment.Center,
                    VerticalTextAlignment=TextAlignment.Center
                },

                GestureRecognizers=
                {
                    new TapGestureRecognizer
                    {
                        Command = command,
                        CommandParameter = commandParameter
                    }
                }
            };
        }

        private void AttachRowTap(SparkGridRow row, int rowIndex, int localIndex)
        {
            for(int c = 0; c<Columns.Count; c++)
            {
                if(Columns[c].CellType==SparkGridCellType.Actions||Columns[c].CellType==SparkGridCellType.Button) continue;

                // FIX: was "c + (ShowRowNumbers ? 1 : 0)", which ignored the checkbox column
                // entirely — once ShowCheckboxColumn pushed data columns one further right,
                // this lookup missed every cell and row-tap silently stopped firing.
                var cell = GridRoot.Children
                    .OfType<Border>()
                    .FirstOrDefault(b => Grid.GetRow(b)==rowIndex&&Grid.GetColumn(b)==c+_columnOffset);

                if(cell==null) continue;

                var tap = new TapGestureRecognizer
                {
                    Command=new Command(() =>
                    {
                        SelectedRowIndex=localIndex;          // triggers rebuild + highlight
                        RowTappedCommand?.Execute(row.Tag??row);
                    })
                };
                cell.GestureRecognizers.Add(tap);
            }
        }

        private Label BuildText(string text, bool rightAlign = false)
        {
            return new Label
            {
                Text=text,
                TextColor=Colors.Black,
                FontSize=13,
                Margin=new Thickness(4, 0, 0, 0),
                HorizontalOptions=LayoutOptions.Fill,
                VerticalOptions=LayoutOptions.Center,
                HorizontalTextAlignment=TextAlignment.Start,
                VerticalTextAlignment=TextAlignment.Center,
                LineBreakMode=LineBreakMode.TailTruncation,
                MaxLines=1
            };
        }

        #region Row numbers + default selection

        public static readonly BindableProperty ShowRowNumbersProperty =
            BindableProperty.Create(
                nameof(ShowRowNumbers),
                typeof(bool),
                typeof(SparkDataGridView),
                true,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());

        /// <summary>Adds a leading "#" column showing the row's position on the current page. Default true.</summary>
        public bool ShowRowNumbers
        {
            get => (bool)GetValue(ShowRowNumbersProperty);
            set => SetValue(ShowRowNumbersProperty, value);
        }

        public static readonly BindableProperty SelectedRowIndexProperty =
            BindableProperty.Create(
                nameof(SelectedRowIndex),
                typeof(int),
                typeof(SparkDataGridView),
                0,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());

        /// <summary>Zero-based index (within the current page's Rows) that's visually highlighted.
        /// Defaults to 0 — the first row is selected as soon as data loads, without the user tapping anything.</summary>
        public int SelectedRowIndex
        {
            get => (int)GetValue(SelectedRowIndexProperty);
            set => SetValue(SelectedRowIndexProperty, value);
        }

        private static readonly Color SelectedRowBg = Color.FromArgb("#E7F7FA"); // same tint family as BuildAvatar's circle bg

        #endregion

        private static View BuildBadge(SparkBadgeValue badge)
        {
            if(badge==null) return new Label();

            var (bg, fg)=badge.Tone switch
            {
                SparkBadgeTone.Success => (BadgeSuccessBg, BadgeSuccessText),
                SparkBadgeTone.Danger => (BadgeDangerBg, BadgeDangerText),
                _ => (BadgeNeutralBg, BadgeNeutralText)
            };

            var pill = new Border
            {
                BackgroundColor=bg,
                Stroke=Colors.Transparent,
                Padding=new Thickness(8, 3),
                HorizontalOptions=LayoutOptions.Center,
                StrokeShape=new RoundRectangle { CornerRadius=10 }
            };
            pill.Content=new Label
            {
                Text=badge.Text,
                TextColor=fg,
                FontSize=12,
                FontAttributes=FontAttributes.Bold,
                HorizontalTextAlignment=TextAlignment.Center
            };
            return pill;
        }

        private static View BuildAvatar(string name)
        {
            var initials = string.IsNullOrWhiteSpace(name)
                ? "?"
                : string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2).Select(p => p[0])).ToUpperInvariant();

            var circle = new Border
            {
                BackgroundColor=Color.FromArgb("#E7F7FA"),
                Stroke=Colors.Transparent,
                WidthRequest=26,
                HeightRequest=26,
                Padding=0,
                StrokeShape=new RoundRectangle { CornerRadius=13 },
                HorizontalOptions=LayoutOptions.Start
            };
            circle.Content=new Label
            {
                Text=initials,
                TextColor=Color.FromArgb("#16374A"),
                FontSize=11,
                FontAttributes=FontAttributes.Bold,
                HorizontalTextAlignment=TextAlignment.Center,
                VerticalTextAlignment=TextAlignment.Center
            };
            return circle;
        }

        private static string FormatCurrency(object value)
        {
            if(value==null) return string.Empty;
            if(value is IFormattable f) return "$"+f.ToString("N0", CultureInfo.InvariantCulture);
            return value.ToString();
        }

        // ---------- Pager ----------

        private static readonly Color PagerActiveBg = Color.FromArgb("#21B6C4");
        private static readonly Color PagerActiveText = Colors.White;
        private static readonly Color PagerInactiveText = Color.FromArgb("#2E3A4E");
        private static readonly Color PagerDisabledText = Color.FromArgb("#C7CDD6");
        private static readonly Color PagerHoverBg = Color.FromArgb("#EDF1F5");
        public static Color PagerBackground => Colors.White;
        public static Color PagerBorder => FFColors.Gray300;
        public static Color PagerBorderColor => FFColors.Gray300;
        public static Color PagerActiveBackground => FFColors.Azure600;
        public static Color PagerActiveForeground => FFColors.White;

        public static Color PagerForeground => FFColors.Slate700;
        public static Color PagerDisabledForeground => FFColors.Gray400;
        private void BuildPager()
        {
            if(PagerRow==null)
                return;

            PagerRow.Children.Clear();

            PagerInfoLabel.Text=PageInfoText;

            bool hasPages = TotalPages>1;

            PagerContainer.IsVisible=
                ShowPagination&&
                (hasPages||!string.IsNullOrWhiteSpace(PageInfoText));

            if(!PagerContainer.IsVisible)
                return;

              PagerRow.Children.Add(
                BuildArrowButton(
                    "\u2039",
                    CurrentPage>1,
                    PreviousPageCommand));


            foreach(int page in GetPageWindow(CurrentPage, TotalPages))
            {
                PagerRow.Children.Add(
                    page==-1
                        ? BuildEllipsis()
                        : BuildPageNumber(page));
            }


            PagerRow.Children.Add(
                BuildArrowButton(
                    "\u203A",
                    CurrentPage<TotalPages,
                    NextPageCommand));
        }

     
       
        private View BuildArrowButton(
    string glyph,
    bool enabled,
    ICommand? command)
        {
            return new Border
            {
                WidthRequest=34,
                HeightRequest=34,
                Padding=0,

                StrokeShape=new RoundRectangle
                {
                    CornerRadius=8
                },

                Background=new SolidColorBrush(Colors.White),

                Stroke=new SolidColorBrush(PagerBorderColor),

                StrokeThickness=1,

                Opacity=enabled ? 1 : .45,

                Content=new Label
                {
                    Text=glyph,
                    FontSize=16,
                    HorizontalTextAlignment=TextAlignment.Center,
                    VerticalTextAlignment=TextAlignment.Center,
                    TextColor=PagerInactiveText
                },

                GestureRecognizers=
        {
            new TapGestureRecognizer
            {
                Command = enabled ? command : null
            }
        }
            };
        }

        private static IEnumerable<int> GetPageWindow(int current, int total)
        {
            var pages = new SortedSet<int>
    {
        1,
        total
    };

            for(int i = current-1; i<=current+1; i++)
            {
                if(i>=1&&i<=total)
                    pages.Add(i);
            }

            int previous = 0;

            foreach(int page in pages)
            {
                if(previous!=0&&page-previous>1)
                    yield return -1;

                yield return page;

                previous=page;
            }
        }
        private View BuildPageNumber(int page)
        {
            bool active = page==CurrentPage;

            var border = new Border
            {
                WidthRequest=34,
                HeightRequest=34,
                StrokeShape=new RoundRectangle
                {
                    CornerRadius=8
                },

                Stroke=active
                    ? PagerActiveBg
                    : PagerBorder,

                StrokeThickness=1,

                Background=new SolidColorBrush(
                    active
                        ? PagerActiveBg
                        : Colors.White),

                Content=new Label
                {
                    Text=page.ToString(),
                    FontSize=13,
                    FontAttributes=FontAttributes.Bold,
                    HorizontalTextAlignment=TextAlignment.Center,
                    VerticalTextAlignment=TextAlignment.Center,
                    VerticalOptions=LayoutOptions.Center,
                    HorizontalOptions=LayoutOptions.Center,
                    TextColor=active
                        ? PagerActiveText
                        : PagerInactiveText
                }
            };

            border.GestureRecognizers.Add(
                new TapGestureRecognizer
                {
                    Command=PageChangedCommand,
                    CommandParameter=page
                });

            return border;
        }



        private static View BuildEllipsis()
        {
            return new Label
            {
                Text="…",
                WidthRequest=24,
                HorizontalTextAlignment=TextAlignment.Center,
                VerticalTextAlignment=TextAlignment.Center,
                VerticalOptions=LayoutOptions.Center,
                TextColor=PagerInactiveText,
                FontSize=13
            };
        }

        #endregion
    }
}