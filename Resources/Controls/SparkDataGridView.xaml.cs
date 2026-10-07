using EHMR.Resources.Theming;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;

namespace EHMR.Resources.Controls
{
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
                BuildPager();
        }

        private int _columnOffset;
        private double _responsiveScale = 1d;
        private double _lastResponsiveWidth = -1;

        private void OnGridSizeChanged(object? sender, EventArgs e)
        {
            var width = Width;
            if(width<=0||Math.Abs(width-_lastResponsiveWidth)<2)
                return;

            _lastResponsiveWidth=width;
            _responsiveScale=width>=1500 ? 1d
                : width>=1250 ? 0.94d
                : width>=1050 ? 0.88d
                : width>=900 ? 0.82d
                : 0.76d;

            BuildGrid();
            BuildPager();
        }

        private double R(double value) => Math.Round(value*_responsiveScale, 1);

        private GridLength ResponsiveColumnWidth(GridLength width)
        {
            return width.GridUnitType==GridUnitType.Absolute
                ? new GridLength(R(width.Value), GridUnitType.Absolute)
                : width;
        }

        #region Bindable properties

        public static readonly BindableProperty AutoSelectFirstRowProperty =
            BindableProperty.Create(nameof(AutoSelectFirstRow), typeof(bool), typeof(SparkDataGridView), false,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());
        public bool AutoSelectFirstRow
        {
            get => (bool)GetValue(AutoSelectFirstRowProperty);
            set => SetValue(AutoSelectFirstRowProperty, value);
        }

        public static readonly BindableProperty AllowHorizontalScrollProperty =
            BindableProperty.Create(nameof(AllowHorizontalScroll), typeof(bool), typeof(SparkDataGridView), false,
                propertyChanged: (b, o, n) =>
                {
                    var view = (SparkDataGridView)b;
                    view.HostScroll.Orientation=(bool)n ? ScrollOrientation.Both : ScrollOrientation.Vertical;
                    view.BuildGrid();
                });
        public bool AllowHorizontalScroll
        {
            get => (bool)GetValue(AllowHorizontalScrollProperty);
            set => SetValue(AllowHorizontalScrollProperty, value);
        }

        public static readonly BindableProperty ShowCheckboxColumnProperty =
            BindableProperty.Create(nameof(ShowCheckboxColumn), typeof(bool), typeof(SparkDataGridView), false,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());
        public bool ShowCheckboxColumn
        {
            get => (bool)GetValue(ShowCheckboxColumnProperty);
            set => SetValue(ShowCheckboxColumnProperty, value);
        }

        public static readonly BindableProperty SelectedRowProperty =
            BindableProperty.Create(nameof(SelectedRow), typeof(SparkGridRow), typeof(SparkDataGridView),
                default(SparkGridRow), BindingMode.TwoWay);
        public SparkGridRow SelectedRow
        {
            get => (SparkGridRow)GetValue(SelectedRowProperty);
            set => SetValue(SelectedRowProperty, value);
        }

        public static readonly BindableProperty SelectionChangedCommandProperty =
            BindableProperty.Create(nameof(SelectionChangedCommand), typeof(ICommand), typeof(SparkDataGridView));
        public ICommand SelectionChangedCommand
        {
            get => (ICommand)GetValue(SelectionChangedCommandProperty);
            set => SetValue(SelectionChangedCommandProperty, value);
        }

        public static readonly BindableProperty ColumnsProperty =
            BindableProperty.Create(nameof(Columns), typeof(ObservableCollection<SparkGridColumn>),
                typeof(SparkDataGridView), defaultValueCreator: _ => new ObservableCollection<SparkGridColumn>(),
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());
        public ObservableCollection<SparkGridColumn> Columns
        {
            get => (ObservableCollection<SparkGridColumn>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly BindableProperty RowsProperty =
            BindableProperty.Create(nameof(Rows), typeof(ObservableCollection<SparkGridRow>),
                typeof(SparkDataGridView), defaultValueCreator: _ => new ObservableCollection<SparkGridRow>(),
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());
        public ObservableCollection<SparkGridRow> Rows
        {
            get => (ObservableCollection<SparkGridRow>)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public static readonly BindableProperty RowTappedCommandProperty =
            BindableProperty.Create(nameof(RowTappedCommand), typeof(ICommand), typeof(SparkDataGridView));
        public ICommand RowTappedCommand
        {
            get => (ICommand)GetValue(RowTappedCommandProperty);
            set => SetValue(RowTappedCommandProperty, value);
        }

        public static readonly BindableProperty EditRowCommandProperty =
            BindableProperty.Create(nameof(EditRowCommand), typeof(ICommand), typeof(SparkDataGridView));
        public ICommand EditRowCommand
        {
            get => (ICommand)GetValue(EditRowCommandProperty);
            set => SetValue(EditRowCommandProperty, value);
        }

        public static readonly BindableProperty HyperlinkCommandProperty =
            BindableProperty.Create(nameof(HyperlinkCommand), typeof(ICommand), typeof(SparkDataGridView));
        public ICommand HyperlinkCommand
        {
            get => (ICommand)GetValue(HyperlinkCommandProperty);
            set => SetValue(HyperlinkCommandProperty, value);
        }

        public static readonly BindableProperty CurrentPageProperty =
            BindableProperty.Create(nameof(CurrentPage), typeof(int), typeof(SparkDataGridView), 1,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildPager());
        public int CurrentPage
        {
            get => (int)GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, value);
        }

        public static readonly BindableProperty TotalPagesProperty =
            BindableProperty.Create(nameof(TotalPages), typeof(int), typeof(SparkDataGridView), 1,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildPager());
        public int TotalPages
        {
            get => (int)GetValue(TotalPagesProperty);
            set => SetValue(TotalPagesProperty, value);
        }

        public static readonly BindableProperty PageChangedCommandProperty =
            BindableProperty.Create(nameof(PageChangedCommand), typeof(ICommand), typeof(SparkDataGridView));
        public ICommand PageChangedCommand
        {
            get => (ICommand)GetValue(PageChangedCommandProperty);
            set => SetValue(PageChangedCommandProperty, value);
        }

        public static readonly BindableProperty PreviousPageCommandProperty =
            BindableProperty.Create(nameof(PreviousPageCommand), typeof(ICommand), typeof(SparkDataGridView));
        public ICommand PreviousPageCommand
        {
            get => (ICommand)GetValue(PreviousPageCommandProperty);
            set => SetValue(PreviousPageCommandProperty, value);
        }

        public static readonly BindableProperty NextPageCommandProperty =
            BindableProperty.Create(nameof(NextPageCommand), typeof(ICommand), typeof(SparkDataGridView));
        public ICommand NextPageCommand
        {
            get => (ICommand)GetValue(NextPageCommandProperty);
            set => SetValue(NextPageCommandProperty, value);
        }

        public static readonly BindableProperty ShowPaginationProperty =
            BindableProperty.Create(nameof(ShowPagination), typeof(bool), typeof(SparkDataGridView), true,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildPager());
        public bool ShowPagination
        {
            get => (bool)GetValue(ShowPaginationProperty);
            set => SetValue(ShowPaginationProperty, value);
        }

        #endregion

        #region Build
        private static Color HeaderBg => ResolveColorResource("SparkTextPrimary", "#1E293B");
        private static Color HeaderTextColor => ResolveColorResource("SparkBackground", "#FFFFFF");
        private static Color RowMutedTextColor => ResolveColorResource("SparkTextSecondary", "#64748B");
        private static Color RowTextColor => ResolveColorResource("SparkTextPrimary", "#1E293B");
        private static Color BorderColor => ResolveColorResource("SparkBorder", "#CBD5E1");
        private static Color RowAltBg => ResolveColorResource("SparkSurfaceAlt", "#F8FAFC");
        private static Color RowBg => ResolveColorResource("SparkBackground", "#FFFFFF");

        private static Color BadgeNeutralBg => ResolveColorResource("SparkBadgeNeutralBg", "#F1F5F9");
        private static Color BadgeNeutralText => ResolveColorResource("SparkBadgeNeutralText", "#475569");
        private static Color BadgeSuccessBg => ResolveColorResource("SparkBadgeSuccessBg", "#ECFDF5");
        private static Color BadgeSuccessText => ResolveColorResource("SparkBadgeSuccessText", "#059669");
        private static Color BadgeDangerBg => ResolveColorResource("SparkBadgeDangerBg", "#FEF2F2");
        private static Color BadgeDangerText => ResolveColorResource("SparkBadgeDangerText", "#DC2626");

        private static Color HyperlinkColor => ResolveColorResource("SparkAccentTeal", "#0EA5B8");
        private static Color AccentColor => ResolveColorResource("SparkAccentTeal", "#0EA5B8");
        private static Color AccentColorMuted => ResolveColorResource("SparkTextSecondary", "#64748B");
        private readonly Dictionary<string, bool> _sortAscending = new(StringComparer.OrdinalIgnoreCase);

        private void BuildGrid()
        {
            GridRoot.Children.Clear();
            GridRoot.RowDefinitions.Clear();
            GridRoot.ColumnDefinitions.Clear();

            if(Columns==null||Columns.Count==0) return;

            int checkboxColumnIndex = 0;
            int rowNumberColumnIndex = ShowCheckboxColumn ? 1 : 0;
            _columnOffset=(ShowCheckboxColumn ? 1 : 0)+(ShowRowNumbers ? 1 : 0);

            if(ShowCheckboxColumn)
                GridRoot.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(R(36)) });
            if(ShowRowNumbers)
                GridRoot.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(R(40)) });
            foreach(var column in Columns)
                GridRoot.ColumnDefinitions.Add(new ColumnDefinition { Width=ResponsiveColumnWidth(column.Width) });

            GridRoot.RowDefinitions.Add(new RowDefinition { Height=new GridLength(R(40)) });
            if(ShowCheckboxColumn) AddCheckboxHeaderCell(checkboxColumnIndex);
            if(ShowRowNumbers) AddRowNumberHeaderCell(rowNumberColumnIndex);
            for(int c = 0; c<Columns.Count; c++)
            {
                AddHeaderCell(Columns[c], c+_columnOffset);
                if(c<Columns.Count-1) AddColumnDivider(0, c+_columnOffset);
            }

            var rows = Rows??new ObservableCollection<SparkGridRow>();

            if(AutoSelectFirstRow&&rows.Count>0&&(SelectedRowIndex<0||SelectedRowIndex>=rows.Count))
            {
                SelectedRowIndex=0;
                return;
            }
            if(!AutoSelectFirstRow&&SelectedRowIndex>=rows.Count)
                SelectedRowIndex=-1;

            for(int r = 0; r<rows.Count; r++)
            {
                GridRoot.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });

                int rowIndex = r+1;
                bool isSelected = r==SelectedRowIndex;
                var rowBg = isSelected ? SelectedRowBg : (r%2==0 ? RowBg : RowAltBg);

                if(ShowCheckboxColumn) AddCheckboxCell(r, rows[r], rowIndex, rowBg, checkboxColumnIndex);
                if(ShowRowNumbers) AddRowNumberCell(r, rowIndex, rowBg, rowNumberColumnIndex);

                for(int c = 0; c<Columns.Count; c++)
                {
                    AddDataCell(Columns[c], rows[r], c+_columnOffset, rowIndex, rowBg);
                    if(c<Columns.Count-1) AddColumnDivider(rowIndex, c+_columnOffset);
                }

                AddRowDivider(rowIndex);
                AttachRowTap(rows[r], rowIndex, r);
            }

            SelectedRow=(SelectedRowIndex>=0&&SelectedRowIndex<rows.Count) ? rows[SelectedRowIndex] : null;
            BuildPager();
        }

        private void AddCheckboxHeaderCell(int columnIndex)
        {
            var border = new Border
            {
                Background=new SolidColorBrush(HeaderBg),
                Stroke=Colors.Transparent,
                Padding=new Thickness(R(4), 0)
            };
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
                HeightRequest=R(40)
            };
            var checkbox = new CheckBox
            {
                IsChecked=localRowIndex==SelectedRowIndex,
                Color=AccentColor,
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center
            };
            checkbox.CheckedChanged+=(s, e) =>
            {
                if(!e.Value) return;
                SelectedRowIndex=localRowIndex;
                SelectionChangedCommand?.Execute(row.Tag??row);
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
                Background=new SolidColorBrush(Color.FromArgb("#5E6E79")),
                Stroke=Colors.Transparent,
                Padding=new Thickness(4, 0)
            };
            border.Content=new Label
            {
                Text="#",
                TextColor=HeaderTextColor,
                FontSize=R(12),
                HorizontalTextAlignment=TextAlignment.Center,
                HorizontalOptions=LayoutOptions.Fill
            };
            Grid.SetRow(border, 0);
            Grid.SetColumn(border, columnIndex);
            GridRoot.Children.Add(border);
        }

        private void AddRowNumberCell(int localRowIndex, int gridRowIndex, Color rowBg, int columnIndex)
        {
            var border = new Border
            {
                BackgroundColor=rowBg,
                StrokeThickness=0,
                Padding=new Thickness(4, 0),
                HeightRequest=40
            };
            border.Content=new Label
            {
                Text=(localRowIndex+1).ToString(),
                TextColor=RowMutedTextColor,
                FontSize=R(12),
                HorizontalTextAlignment=TextAlignment.Center,
                VerticalTextAlignment=TextAlignment.Center,
                HorizontalOptions=LayoutOptions.Fill
            };
            Grid.SetRow(border, gridRowIndex);
            Grid.SetColumn(border, columnIndex);
            GridRoot.Children.Add(border);
        }

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
                Background=new SolidColorBrush(Color.FromArgb("#5E6E79")),
                Stroke=Colors.Transparent,
                Padding=new Thickness(R(10), 0),
                HorizontalOptions=LayoutOptions.Fill
            };
            var row = new HorizontalStackLayout
            {
                Spacing=R(4),
                VerticalOptions=LayoutOptions.Center,
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
                FontSize=R(11),
                VerticalOptions=LayoutOptions.Center
            });
            var isSortable = column.Sortable
                && column.CellType != SparkGridCellType.Actions
                && column.CellType != SparkGridCellType.Button
                && column.CellType != SparkGridCellType.QuickPreview;

            if(isSortable)
            {
                row.Children.Add(new Label
                {
                    Text=GetSortArrow(column.Key),
                    TextColor=HeaderTextColor,
                    FontSize=R(11),
                    VerticalOptions=LayoutOptions.Center
                });

                border.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command=column.HeaderTapCommand ?? new Command(() => SortRows(column.Key)),
                    CommandParameter=column.Key
                });
            }
            else if(column.HeaderTapCommand!=null)
            {
                border.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command=column.HeaderTapCommand,
                    CommandParameter=column.Key
                });
            }

            border.Content=row;

            Grid.SetRow(border, 0);
            Grid.SetColumn(border, columnIndex);
            GridRoot.Children.Add(border);
        }

        private string GetSortArrow(string key)
        {
            if(!_sortAscending.TryGetValue(key, out var ascending))
                return "↕";
            return ascending ? "↑" : "↓";
        }

        private void SortRows(string key)
        {
            var rows=Rows?.ToList()??new List<SparkGridRow>();
            if(rows.Count<2) return;

            var ascending=!_sortAscending.TryGetValue(key, out var current)||!current;
            _sortAscending[key]=ascending;

            rows.Sort((left,right) =>
            {
                left.TryGetValue(key,out var lv);
                right.TryGetValue(key,out var rv);
                var comparison=StringComparer.CurrentCultureIgnoreCase.Compare(
                    GetSortableValue(lv),GetSortableValue(rv));
                return ascending ? comparison : -comparison;
            });

            Rows=new ObservableCollection<SparkGridRow>(rows);
        }

        private static string GetSortableValue(object? value)
        {
            return value switch
            {
                null=>string.Empty,
                SparkBadgeValue badge=>badge.Text??string.Empty,
                SparkButtonItem button=>button.Label??string.Empty,
                _=>Convert.ToString(value,CultureInfo.CurrentCulture)??string.Empty
            };
        }

        public static readonly BindableProperty PageInfoTextProperty =
            BindableProperty.Create(nameof(PageInfoText), typeof(string), typeof(SparkDataGridView), string.Empty,
                propertyChanged: (b, oldValue, newValue) =>
                {
                    var grid = (SparkDataGridView)b;
                    if(grid.PagerInfoLabel!=null)
                        grid.PagerInfoLabel.Text=newValue?.ToString()??string.Empty;
                });
        public string PageInfoText
        {
            get => (string)GetValue(PageInfoTextProperty);
            set => SetValue(PageInfoTextProperty, value);
        }

        private void AddDataCell(SparkGridColumn column, SparkGridRow row, int columnIndex, int rowIndex, Color rowBg)
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

            var pointer = new PointerGestureRecognizer();
            pointer.PointerEntered += (_, _) => SetRowHover(rowIndex, true);
            pointer.PointerExited += (_, _) => SetRowHover(rowIndex, false);
            border.GestureRecognizers.Add(pointer);

            // Every list-grid field exposes its complete value on hover.
            // Action cells contain a collection of buttons; never expose the
            // collection type as a tooltip (for example System.Collections.Generic.List).
            if(column.CellType!=SparkGridCellType.Actions)
            {
                var tooltip = ResolveCellTooltip(value);
                if(!string.IsNullOrWhiteSpace(tooltip))
                    ToolTipProperties.SetText(border, tooltip);
            }

            View content = column.CellType switch
            {
                SparkGridCellType.Badge => BuildBadge(value as SparkBadgeValue),
                SparkGridCellType.Currency => BuildText(FormatCurrency(value), true),
                SparkGridCellType.Number => BuildText(value?.ToString()??string.Empty, true),
                SparkGridCellType.Avatar => BuildAvatar(value?.ToString()),
                SparkGridCellType.Actions => BuildActions(row),
                SparkGridCellType.Button => BuildSingleButton(value as SparkButtonItem, row),
                SparkGridCellType.QuickPreview => BuildQuickPreview(value?.ToString()),
                SparkGridCellType.Hyperlink => BuildHyperlink(value?.ToString(), row, rowIndex-1),
                _ => BuildText(value?.ToString()??string.Empty, false)
            };

            content.HorizontalOptions=column.CellType switch
            {
                SparkGridCellType.Badge => LayoutOptions.Center,
                SparkGridCellType.Actions => LayoutOptions.Center,
                SparkGridCellType.Button => LayoutOptions.Center,
                _ => LayoutOptions.Fill
            };
            content.VerticalOptions=LayoutOptions.Center;

            Grid.SetRow(border, rowIndex);
            Grid.SetColumn(border, columnIndex);
            border.Content=content;
            GridRoot.Children.Add(border);
        }

        private void SetRowHover(int gridRowIndex, bool isHover)
        {
            var hoverBackground = Color.FromArgb("#F1F5F9");

            foreach (var cell in GridRoot.Children
                         .OfType<Border>()
                         .Where(b => Grid.GetRow(b) == gridRowIndex))
            {
                if (isHover)
                {
                    cell.BackgroundColor = hoverBackground;
                    continue;
                }

                var localIndex = gridRowIndex - 1;
                cell.BackgroundColor = localIndex == SelectedRowIndex
                    ? SelectedRowBg
                    : (localIndex % 2 == 0 ? RowBg : RowAltBg);
            }
        }

        private static string? ResolveCellTooltip(object? value)
        {
            if(value is null)
                return null;

            if(value is SparkBadgeValue badge)
                return string.IsNullOrWhiteSpace(badge.Text) ? null : badge.Text;

            if(value is SparkButtonItem button)
                return string.IsNullOrWhiteSpace(button.Label) ? null : button.Label;

            var text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private View BuildQuickPreview(string? tooltip)
        {
            var button = new Button
            {
                Text = "🔍",
                FontSize = R(16),
                TextColor = Color.FromArgb("#35AEB9"),
                BackgroundColor = Colors.Transparent,
                BorderWidth = 0,
                Padding = new Thickness(6, 0),
                WidthRequest = R(38),
                HeightRequest = R(36),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            if(!string.IsNullOrWhiteSpace(tooltip))
                ToolTipProperties.SetText(button, tooltip);

            return button;
        }

        private View BuildHyperlink(string? text, SparkGridRow row, int localRowIndex)
        {
            var label = new Label
            {
                Text=text??string.Empty,
                TextColor=HyperlinkColor,
                TextDecorations=TextDecorations.Underline,
                FontSize=R(13),
                HorizontalOptions=LayoutOptions.Start,
                VerticalOptions=LayoutOptions.Center,
                HorizontalTextAlignment=TextAlignment.Start,
                VerticalTextAlignment=TextAlignment.Center,
                LineBreakMode=LineBreakMode.TailTruncation,
                MaxLines=1
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped+=(_, _) =>
            {
                var param = row.Tag??row;
                SelectedRowIndex=localRowIndex;
                if(HyperlinkCommand?.CanExecute(param)==true)
                    HyperlinkCommand.Execute(param);
            };
            label.GestureRecognizers.Add(tap);

            return label;
        }

        private View BuildSingleButton(SparkButtonItem item, SparkGridRow row)
        {
            if(item==null) return new Label();
            var button = BuildActionIcon(item.Label??"", item.Command, item.CommandParameter,
                ResolveActionColor(item), ResolveActionTextColor(item));
            button.IsEnabled = item.IsEnabled;
            button.Opacity = item.IsEnabled ? 1 : 0.45;
            if(!string.IsNullOrWhiteSpace(item.Label))
                ToolTipProperties.SetText(button, item.Label);
            return button;
        }

        private View BuildActions(SparkGridRow row)
        {
            if(!row.TryGetValue("Actions", out var value)) return new Label();
            if(value is not IEnumerable<SparkButtonItem> actions) return new Label();

            var layout = new HorizontalStackLayout
            {
                Spacing=R(8),
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center
            };
            foreach(var action in actions)
            {
                var button = BuildActionIcon(
                    action.Label??"", action.Command,
                    action.CommandParameter??row.Tag??row,
                    ResolveActionColor(action), ResolveActionTextColor(action));
                button.IsEnabled=action.IsEnabled;
                button.Opacity=action.IsEnabled ? 1 : 0.45;
                if(!string.IsNullOrWhiteSpace(action.Label))
                    ToolTipProperties.SetText(button, action.Label);
                layout.Children.Add(button);
            }

            return layout;
        }

        private static Color ResolveActionColor(SparkButtonItem action)
        {
            var label = action.Label?.Trim()??string.Empty;

            // "Повеќе" is kept here as a compatibility alias for older list VMs.
            if(label.Equals("Детали", StringComparison.OrdinalIgnoreCase)||
               label.Equals("Повеќе", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb("#73FBFD");

            if(label.Equals("Промени", StringComparison.OrdinalIgnoreCase))
                return ResolveColorResource("SurfaceAlt", "#1A2436");

            if(label.Equals("Исчисти", StringComparison.OrdinalIgnoreCase)||action.IsPrimary)
                return ResolveColorResource("SparkButtonSecondaryBg", "#475569");

            return ResolveColorResource("TextMuted", "#64748B");
        }

        private static Color ResolveActionTextColor(SparkButtonItem action)
        {
            var label = action.Label?.Trim()??string.Empty;
            if(label.Equals("Повеќе", StringComparison.OrdinalIgnoreCase)||
               label.Equals("Детали", StringComparison.OrdinalIgnoreCase))
                return ResolveColorResource("SparkTextPrimary", "#1E2733");
            return Colors.White;
        }

        private static Color ResolveColorResource(string key, string fallback)
        {
            if(Application.Current?.Resources.TryGetValue(key, out var value)==true&&value is Color color)
                return color;
            return Color.FromArgb(fallback);
        }

        private View BuildActionIcon(string text, ICommand command, object commandParameter, Color color, Color textColor)
        {
            var border = new Border
            {
                Padding=new Thickness(R(6), R(4)),
                Background=new SolidColorBrush(color),
                Stroke=new SolidColorBrush(color),
                StrokeThickness=1,
                StrokeShape=new RoundRectangle { CornerRadius=12 },
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center,
                MinimumWidthRequest=R(90),
                MinimumHeightRequest=R(26)
            };

            border.Content=new Label
            {
                Text=text,
                FontSize=R(11),
                FontAttributes=FontAttributes.Bold,
                TextColor=textColor,
                HorizontalTextAlignment=TextAlignment.Center,
                VerticalTextAlignment=TextAlignment.Center
            };

            border.Opacity = 1;
            border.IsEnabled = true;

            var normalBackground = color;
            var hoverBackground = Color.FromArgb("#6B7881");
            var pointer = new PointerGestureRecognizer();
            pointer.PointerEntered += (_, _) =>
            {
                if (border.IsEnabled)
                    border.Background = new SolidColorBrush(hoverBackground);
            };
            pointer.PointerExited += (_, _) =>
            {
                border.Background = new SolidColorBrush(normalBackground);
            };
            border.GestureRecognizers.Add(pointer);

            border.GestureRecognizers.Add(
                new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        if(border.IsEnabled)
                            command?.Execute(commandParameter);
                    })
                });

            return border;
        }

        private void AttachRowTap(SparkGridRow row, int rowIndex, int localIndex)
        {
            for(int c = 0; c<Columns.Count; c++)
            {
                if(Columns[c].CellType==SparkGridCellType.Actions||
                   Columns[c].CellType==SparkGridCellType.Button||
                   Columns[c].CellType==SparkGridCellType.Hyperlink)
                    continue;

                var cell = GridRoot.Children
                    .OfType<Border>()
                    .FirstOrDefault(b => Grid.GetRow(b)==rowIndex&&Grid.GetColumn(b)==c+_columnOffset);
                if(cell==null) continue;

                cell.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command=new Command(() =>
                    {
                        SelectedRowIndex=localIndex;
                        RowTappedCommand?.Execute(row.Tag??row);
                    })
                });
            }
        }

        private Label BuildText(string text, bool rightAlign = false)
        {
            return new Label
            {
                Text=text,
                TextColor=RowTextColor,
                FontSize=R(12),
                Margin=new Thickness(R(8), 0, 0, 0),
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
            BindableProperty.Create(nameof(ShowRowNumbers), typeof(bool), typeof(SparkDataGridView), false,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());
        public bool ShowRowNumbers
        {
            get => (bool)GetValue(ShowRowNumbersProperty);
            set => SetValue(ShowRowNumbersProperty, value);
        }

        public static readonly BindableProperty SelectedRowIndexProperty =
            BindableProperty.Create(nameof(SelectedRowIndex), typeof(int), typeof(SparkDataGridView), -1,
                propertyChanged: (b, o, n) => ((SparkDataGridView)b).BuildGrid());
        public int SelectedRowIndex
        {
            get => (int)GetValue(SelectedRowIndexProperty);
            set => SetValue(SelectedRowIndexProperty, value);
        }
        private static Color SelectedRowBg => ResolveColorResource("SparkAccentTealBg", "#E8F7F8");

        #endregion

        private View BuildBadge(SparkBadgeValue badge)
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
                Padding=new Thickness(R(8), R(3)),
                HorizontalOptions=LayoutOptions.Center,
                StrokeShape=new RoundRectangle { CornerRadius=10 }
            };
            pill.Content=new Label
            {
                Text=badge.Text,
                TextColor=fg,
                FontSize=11,
                FontAttributes=FontAttributes.Bold,
                HorizontalTextAlignment=TextAlignment.Center
            };
            return pill;
        }

        private View BuildAvatar(string name)
        {
            var initials = string.IsNullOrWhiteSpace(name)
                ? "?"
                : string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2).Select(p => p[0])).ToUpperInvariant();

            var circle = new Border
            {
                BackgroundColor=Color.FromArgb("#DCE2E6"),
                Stroke=Colors.Transparent,
                WidthRequest=R(26),
                HeightRequest=R(26),
                Padding=0,
                StrokeShape=new RoundRectangle { CornerRadius=R(13) },
                HorizontalOptions=LayoutOptions.Start
            };
            circle.Content=new Label
            {
                Text=initials,
                TextColor=Color.FromArgb("#50616D"),
                FontSize=R(11),
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

        #endregion

        private static Color PagerActiveBg => ResolveColorResource("SparkAccentTeal", "#0EA5B8");
        private static Color PagerActiveText => ResolveColorResource("SparkBackground", "#FFFFFF");
        private static Color PagerInactiveText => ResolveColorResource("SparkTextSecondary", "#64748B");
        public static Color PagerBackground => ResolveColorResource("SparkSurfaceAlt", "#F8FAFC");
        public static Color PagerBorder => ResolveColorResource("SparkBorder", "#CBD5E1");
        public static Color PagerBorderColor => ResolveColorResource("SparkBorder", "#CBD5E1");
        public static Color PagerActiveBackground => ResolveColorResource("SparkAccentTeal", "#0EA5B8");
        public static Color PagerActiveForeground => Colors.White;
        public static Color PagerForeground => ResolveColorResource("SparkTextSecondary", "#64748B");
        public static Color PagerDisabledForeground => Color.FromArgb("#CBD5E1");

        private void BuildPager()
        {
            if(PagerRow==null) return;

            PagerRow.Children.Clear();
            PagerInfoLabel.Text=PageInfoText;

            bool hasPages = TotalPages>1;
            PagerContainer.IsVisible=ShowPagination&&(hasPages||!string.IsNullOrWhiteSpace(PageInfoText));
            if(!PagerContainer.IsVisible) return;

            PagerRow.Children.Add(BuildArrowButton("\u2039", CurrentPage>1, PreviousPageCommand));
            foreach(int page in GetPageWindow(CurrentPage, TotalPages))
                PagerRow.Children.Add(page==-1 ? BuildEllipsis() : BuildPageNumber(page));
            PagerRow.Children.Add(BuildArrowButton("\u203A", CurrentPage<TotalPages, NextPageCommand));
        }

        private View BuildArrowButton(string glyph, bool enabled, ICommand? command)
        {
            return new Border
            {
                WidthRequest=R(34),
                HeightRequest=R(34),
                Padding=0,
                StrokeShape=new RoundRectangle { CornerRadius=8 },
                Background=new SolidColorBrush(Colors.White),
                Stroke=new SolidColorBrush(PagerBorderColor),
                StrokeThickness=1,
                Opacity=enabled ? 1 : .45,
                Content=new Label
                {
                    Text=glyph,
                    FontSize=R(16),
                    HorizontalTextAlignment=TextAlignment.Center,
                    VerticalTextAlignment=TextAlignment.Center,
                    TextColor=PagerInactiveText
                },
                GestureRecognizers=
                {
                    new TapGestureRecognizer { Command=enabled ? command : null }
                }
            };
        }

        private static IEnumerable<int> GetPageWindow(int current, int total)
        {
            var pages = new SortedSet<int> { 1, total };
            for(int i = current-1; i<=current+1; i++)
                if(i>=1&&i<=total) pages.Add(i);

            int previous = 0;
            foreach(int page in pages)
            {
                if(previous!=0&&page-previous>1) yield return -1;
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
                StrokeShape=new RoundRectangle { CornerRadius=8 },
                Stroke=active ? PagerActiveBg : PagerBorder,
                StrokeThickness=1,
                Background=new SolidColorBrush(active ? PagerActiveBg : Colors.White),
                Content=new Label
                {
                    Text=page.ToString(),
                    FontSize=13,
                    FontAttributes=FontAttributes.Bold,
                    HorizontalTextAlignment=TextAlignment.Center,
                    VerticalTextAlignment=TextAlignment.Center,
                    VerticalOptions=LayoutOptions.Center,
                    HorizontalOptions=LayoutOptions.Center,
                    TextColor=active ? PagerActiveText : PagerInactiveText
                }
            };
            border.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command=PageChangedCommand,
                CommandParameter=page
            });
            return border;
        }

        private View BuildEllipsis()
        {
            return new Label
            {
                Text="…",
                WidthRequest=R(24),
                HorizontalTextAlignment=TextAlignment.Center,
                VerticalTextAlignment=TextAlignment.Center,
                VerticalOptions=LayoutOptions.Center,
                TextColor=PagerInactiveText,
                FontSize=13
            };
        }
    }
}