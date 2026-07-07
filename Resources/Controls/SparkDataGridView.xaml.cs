
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
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

        #region Bindable properties
        public static readonly BindableProperty AllowHorizontalScrollProperty =
    BindableProperty.Create(
        nameof(AllowHorizontalScroll),
        typeof(bool),
        typeof(SparkDataGridView),
        false,
        propertyChanged: (b, o, n) =>
        {
            var view = (SparkDataGridView)b;
            view.HostScroll.Orientation = (bool)n ? ScrollOrientation.Both : ScrollOrientation.Vertical;
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

        private static readonly Color HeaderTextColor = Color.FromArgb("#8B95A6");
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

            // Build columns
            foreach(var column in Columns)
            {
                GridRoot.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width=column.Width
                });
            }

            // Header
            GridRoot.RowDefinitions.Add(new RowDefinition
            {
                Height=new GridLength(44)
            });

            for(int c = 0; c<Columns.Count; c++)
            {
                AddHeaderCell(Columns[c], c);
                if(c<Columns.Count-1)
                    AddColumnDivider(0, c);
            }

            // Rows
            var rows = Rows??new ObservableCollection<SparkGridRow>();

            for(int r = 0; r<rows.Count; r++)
            {
                GridRoot.RowDefinitions.Add(new RowDefinition
                {
                    Height=GridLength.Auto
                });

                int rowIndex = r+1;
                var rowBg = r%2==0 ? RowBg : RowAltBg;

                for(int c = 0; c<Columns.Count; c++)
                {
                    AddDataCell(Columns[c], rows[r], c, rowIndex, rowBg);
                    if(c<Columns.Count-1)
                        AddColumnDivider(rowIndex, c);
                }

                AddRowDivider(rowIndex);
                AttachRowTap(rows[r], rowIndex);
            }

            System.Diagnostics.Debug.WriteLine($"Children = {GridRoot.Children.Count}");
            System.Diagnostics.Debug.WriteLine($"Rows = {GridRoot.RowDefinitions.Count}");
            System.Diagnostics.Debug.WriteLine($"Columns = {GridRoot.ColumnDefinitions.Count}");
            BuildPager();
        }

        /// <summary>One hairline under the row, spanning all columns — replaces per-cell 4-sided
        /// borders so the table reads as continuous rows (blueprint) instead of boxed cells with
        /// vertical dividers.</summary>
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
            Grid.SetColumnSpan(divider, Columns.Count);
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
                BackgroundColor=Colors.Transparent,
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
        default(string),
        propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildPager());

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
                SparkGridCellType.Badge =>
                    BuildBadge(value as SparkBadgeValue),

                SparkGridCellType.Currency =>
                    BuildText(FormatCurrency(value), true),

                SparkGridCellType.Number =>
                    BuildText(value?.ToString()??string.Empty, true),

                SparkGridCellType.Avatar =>
                    BuildAvatar(value?.ToString()),

                SparkGridCellType.Actions =>
                    BuildActions(row),

                _ =>
                    BuildText(value?.ToString()??string.Empty, false)
            };

            // Badge and Actions cells center to match their centered headers;
            // Text/Currency/Number/Avatar fill and let their own content alignment
            // (set inside BuildText/BuildAvatar) control the actual text position.
            content.HorizontalOptions=column.CellType switch
            {
                SparkGridCellType.Badge => LayoutOptions.Center,
                SparkGridCellType.Actions => LayoutOptions.Center,
                _ => LayoutOptions.Fill
            };
            content.VerticalOptions=LayoutOptions.Center;

            Grid.SetRow(border, rowIndex);
            Grid.SetColumn(border, columnIndex);

            border.Content=content;

            GridRoot.Children.Add(border);
        }
        private View BuildActions(SparkGridRow row)
        {
            return new HorizontalStackLayout
            {
                Spacing=8,
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center,
                Children=
        {
            RowTappedCommand != null
                ? BuildActionIcon("Преглед", RowTappedCommand, row, "#69D3DD")
                : null,

            EditRowCommand != null
                ? BuildActionIcon("Промени", EditRowCommand, row, "#8FA2AB")
                : null
        }
            };
        }

        private static View BuildActionIcon(string text, ICommand command, SparkGridRow row, string color)
        {
            return new Border
            {
                Padding=new Thickness(4, 4),
                BackgroundColor=Color.FromArgb(color),
                StrokeThickness=0,
                StrokeShape=new RoundRectangle { CornerRadius=12 },
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center,
                MinimumWidthRequest=90, MinimumHeightRequest=26,

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
                CommandParameter = row.Tag ?? row
            }
        }
            };
        }

        private void AttachRowTap(SparkGridRow row, int rowIndex)
        {
            if(RowTappedCommand==null) return;

            for(int c = 0; c<Columns.Count; c++)
            {
                if(Columns[c].CellType==SparkGridCellType.Actions) continue; // has its own tap targets

                var cell = GridRoot.Children
                    .OfType<Border>()
                    .FirstOrDefault(b => Grid.GetRow(b)==rowIndex&&Grid.GetColumn(b)==c);

                if(cell==null) continue;
                var tap = new TapGestureRecognizer
                {
                    Command=RowTappedCommand,
                    CommandParameter=row.Tag??row
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

        private void BuildPager()
        {
            if(PagerRow==null) return;

            PagerRow.Children.Clear();

            bool hasPillMode = TotalPages>1;
            bool hasTextMode = !string.IsNullOrEmpty(PageInfoText);

            bool visible = ShowPagination&&(hasPillMode||hasTextMode);
            PagerContainer.IsVisible=visible;
            if(!visible) return;

            // Arrow enabled-state: pill mode derives it from CurrentPage/TotalPages;
            // text mode has no page count to check, so trust the command's own CanExecute.
            bool prevEnabled = hasPillMode ? CurrentPage>1 : (PreviousPageCommand?.CanExecute(null)??true);
            bool nextEnabled = hasPillMode ? CurrentPage<TotalPages : (NextPageCommand?.CanExecute(null)??true);

            PagerRow.Children.Add(BuildArrowButton("\u2039", prevEnabled, PreviousPageCommand, null)); // ‹

            if(hasPillMode)
            {
                foreach(var page in GetPageWindow(CurrentPage, TotalPages))
                {
                    PagerRow.Children.Add(page==-1
                        ? BuildEllipsis()
                        : BuildPageNumberButton(page));
                }
            }
            else
            {
                PagerRow.Children.Add(new Label
                {
                    Text=PageInfoText,
                    Style=(Style)Application.Current.Resources["SparkPagerLabelStyle"],
                    VerticalOptions=LayoutOptions.Center
                });
            }

            PagerRow.Children.Add(BuildArrowButton("\u203A", nextEnabled, NextPageCommand, null)); // ›
        }

        /// <summary>Always includes page 1 and TotalPages, plus a window around CurrentPage,
        /// with -1 markers for gaps (rendered as "…"). e.g. current=6,total=10 -> 1,…,5,6,7,…,10</summary>
        private static IEnumerable<int> GetPageWindow(int current, int total)
        {
            var pages = new SortedSet<int> { 1, total };
            for(int p = current-1; p<=current+1; p++)
                if(p>=1&&p<=total) pages.Add(p);

            int? prev = null;
            foreach(var p in pages)
            {
                if(prev.HasValue&&p-prev.Value>1) yield return -1;
                yield return p;
                prev=p;
            }
        }

        private View BuildPageNumberButton(int page)
        {
            bool isActive = page==CurrentPage;
            var btn = new Button
            {
                Text=page.ToString(),
                WidthRequest=30,
                HeightRequest=30,
                Padding=0,
                FontSize=13,
                CornerRadius=6,
                BackgroundColor=isActive ? PagerActiveBg : Colors.Transparent,
                TextColor=isActive ? PagerActiveText : PagerInactiveText,
                BorderWidth=0,
                Command=PageChangedCommand,
                CommandParameter=page
            };
            return btn;
        }

        private static View BuildArrowButton(string glyph, bool enabled, ICommand command, object parameter)
        {
            return new Button
            {
                Text=glyph,
                WidthRequest=30,
                HeightRequest=30,
                Padding=0,
                FontSize=14,
                CornerRadius=6,
                BackgroundColor=Colors.Transparent,
                TextColor=enabled ? PagerInactiveText : PagerDisabledText,
                BorderWidth=0,
                Command=command,
                CommandParameter=parameter
            };
        }

        private static View BuildEllipsis() => new Label
        {
            Text="…",
            WidthRequest=20,
            TextColor=PagerInactiveText,
            HorizontalTextAlignment=TextAlignment.Center,
            VerticalOptions=LayoutOptions.Center
        };

        #endregion
    }
}