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

            // Every list-grid field exposes its complete value on hover.
            // The visible cell may stay truncated; the tooltip contains the full value.
            var tooltip = ResolveCellTooltip(value);
            if(!string.IsNullOrWhiteSpace(tooltip))
                ToolTipProperties.SetText(border, tooltip);

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
                TextColor = Color.FromArgb("#0F766E"),
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