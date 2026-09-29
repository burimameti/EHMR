using EHMR.ViewModels;
using Microsoft.Maui.Controls;
using System;
using EHMR.Resources.Controls;

namespace EHMR.Views.Reports
{
    public partial class DashboardReportPage:ContentPage
    {
        private double _responsiveScale = 1.0;

    private readonly ReportViewModel _vm;
    private readonly MenuView _menu;

    public DashboardReportPage(ReportViewModel vm, MenuView menu)
    {
        InitializeComponent();
        _vm=vm;
        _menu=menu;
        BindingContext=_vm;
        MenuHost.Content=_menu;
    }
        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if(PageLayout == null)
                return;

            double availableWidth = Width > 0 ? Width : 1680;
            double contentWidth = Math.Max(760, availableWidth - (availableWidth < 1180 ? 230 : 285));

            // Responsive means the same layout becomes denser; it does not switch
            // the report toolbar into a different arrangement.
            _responsiveScale = Math.Clamp(contentWidth / 1680d, 0.72d, 1.0d);

            bool narrowShell = availableWidth < 1180;
            PageLayout.ColumnDefinitions.Clear();
            PageLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(narrowShell ? 230 : 285) });
            PageLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            if(ReportsItemsLayout != null)
                ReportsItemsLayout.Span = contentWidth < 1100 ? 1 : 2;

            ApplyResponsiveSizing(_responsiveScale);

            if(ReportControlGrid != null)
            {
                // Keep one row. Star columns allow every control to shrink with the
                // available width instead of being clipped or moved to new rows.
                ReportControlGrid.ColumnDefinitions.Clear();
                ReportControlGrid.RowDefinitions.Clear();
                ReportControlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                ReportControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });
                ReportControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star) });
                ReportControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.82, GridUnitType.Star) });
                ReportControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.82, GridUnitType.Star) });
                ReportControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.70, GridUnitType.Star) });
                ReportControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.78, GridUnitType.Star) });

                Grid.SetColumn(ReportTitleBlock, 0);
                Grid.SetColumnSpan(ReportTitleBlock, 1);
                Grid.SetRow(ReportTitleBlock, 0);
                Grid.SetColumn(PeriodControl, 1);
                Grid.SetColumn(FromControl, 2);
                Grid.SetColumn(ToControl, 3);
                Grid.SetColumn(ApplyPeriodButton, 4);
                Grid.SetColumn(GenerateButton, 5);
                Grid.SetRow(PeriodControl, 0);
                Grid.SetRow(FromControl, 0);
                Grid.SetRow(ToControl, 0);
                Grid.SetRow(ApplyPeriodButton, 0);
                Grid.SetRow(GenerateButton, 0);

                PeriodControl.HorizontalOptions = LayoutOptions.Fill;
                FromControl.HorizontalOptions = LayoutOptions.Fill;
                ToControl.HorizontalOptions = LayoutOptions.Fill;
                ApplyPeriodButton.HorizontalOptions = LayoutOptions.Fill;
                GenerateButton.HorizontalOptions = LayoutOptions.Fill;
            }

            if(DetailsGrid == null || AnalyticsSidebar == null)
                return;

            bool compact = contentWidth < 1180;
            DetailsGrid.ColumnDefinitions.Clear();
            DetailsGrid.RowDefinitions.Clear();

            if(compact)
            {
                DetailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Star });
                DetailsGrid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
                DetailsGrid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
                Grid.SetColumn(AnalyticsSidebar, 0);
                Grid.SetRow(AnalyticsSidebar, 1);
            }
            else
            {
                DetailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Star });
                DetailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(Math.Max(250, 320 * _responsiveScale)) });
                DetailsGrid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
                Grid.SetColumn(AnalyticsSidebar, 1);
                Grid.SetRow(AnalyticsSidebar, 0);
            }
        }

        private void ApplyResponsiveSizing(double scale)
        {
            if(ReportTitleBlock == null)
                return;

            // Keep typography readable while reducing the physical footprint of the
            // controls at smaller desktop resolutions.
            if(ReportTitleBlock.Children.Count >= 2)
            {
                if(ReportTitleBlock.Children[0] is Label title)
                    title.FontSize = 15 * scale;
                if(ReportTitleBlock.Children[1] is Label date)
                    date.FontSize = 12 * scale;
            }

            if(PeriodControl?.Children.Count > 0 && PeriodControl.Children[0] is Label periodLabel)
                periodLabel.FontSize = 13 * scale;
            if(FromControl?.Children.Count > 0 && FromControl.Children[0] is Label fromLabel)
                fromLabel.FontSize = 13 * scale;
            if(ToControl?.Children.Count > 0 && ToControl.Children[0] is Label toLabel)
                toLabel.FontSize = 13 * scale;

            if(ApplyPeriodButton != null)
            {
                ApplyPeriodButton.HeightRequestEx = 45 * scale;
                ApplyPeriodButton.ContentPadding = new Thickness(12 * scale, 0);
                ApplyPeriodButton.FontSizeEx = 13 * scale;
            }
            if(GenerateButton != null)
            {
                GenerateButton.HeightRequestEx = 45 * scale;
                GenerateButton.ContentPadding = new Thickness(12 * scale, 0);
                GenerateButton.FontSizeEx = 13 * scale;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if(BindingContext is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
