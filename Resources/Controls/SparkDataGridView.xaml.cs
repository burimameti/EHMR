
            var layout = new HorizontalStackLayout
            {
                Spacing=R(8),
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center
            };
            foreach(var action in actions)
                layout.Children.Add(BuildActionIcon(
                    DisplayActionLabel(action.Label), action.Command,
                    action.CommandParameter??row.Tag??row,
                    ResolveActionColor(action), ResolveActionTextColor(action)));

            return layout;
        }

        private static Color ResolveActionColor(SparkButtonItem action)
        {
            var label=action.Label?.Trim()??string.Empty;
            if(label.Equals("Повеќе", StringComparison.OrdinalIgnoreCase) ||
               label.Equals("Детали", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb("#73FBFD");
            if(label.Equals("Промени", StringComparison.OrdinalIgnoreCase))
                return ResolveColorResource("SurfaceAlt", "#1A2436");
            if(label.Equals("Исчисти", StringComparison.OrdinalIgnoreCase)||action.IsPrimary)
                return ResolveColorResource("SparkButtonSecondaryBg", "#475569");
            return ResolveColorResource("TextMuted", "#64748B");
        }

        private static Color ResolveActionTextColor(SparkButtonItem action)
        {
            var label=action.Label?.Trim()??string.Empty;
            if(label.Equals("Повеќе", StringComparison.OrdinalIgnoreCase)||label.Equals("Детали", StringComparison.OrdinalIgnoreCase))
                return ResolveColorResource("SparkTextPrimary", "#1E2733");
            return Colors.White;
        }
        private static string DisplayActionLabel(string? label)
        {
            return string.Equals(label?.Trim(), "Повеќе", StringComparison.OrdinalIgnoreCase)
                ? "Детали"
                : label?.Trim() ?? string.Empty;
        }

        private static Color ResolveColorResource(string key, string fallback)
        {
            if(Application.Current?.Resources.TryGetValue(key, out var value)==true&&value is Color color)
                return color;
            return Color.FromArgb(fallback);
        }

        private View BuildActionIcon(string text, ICommand command, object commandParameter, Color color, Color textColor)
        {
            return new Border
            {
                Padding=new Thickness(R(4), R(4)),
                BackgroundColor=color,
                StrokeThickness=0,
                StrokeShape=new RoundRectangle { CornerRadius=12 },
                HorizontalOptions=LayoutOptions.Center,
                VerticalOptions=LayoutOptions.Center,
                MinimumWidthRequest=R(90),
                MinimumHeightRequest=R(26),
                Content=new Label
                {
                    Text=text,
                    FontSize=R(11),
                    TextColor=textColor,
                    HorizontalTextAlignment=TextAlignment.Center,
                    VerticalTextAlignment=TextAlignment.Center
                },
                GestureRecognizers=
                {