using EHMR.ViewModels;
using Microsoft.Maui.Controls;
using System;

namespace EHMR.Views.Reports
{
    public partial class DashboardReportPage:ContentPage
    {

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

            // The report hub uses two cards on desktop and one card on compact widths.
            // The main page/sidebar split also tightens before the analytics panel stacks.
            double availableWidth = Width > 0 ? Width : 1680;
            bool narrowShell = availableWidth < 1180;
            PageLayout.ColumnDefinitions.Clear();
            PageLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(narrowShell ? 230 : 285) });
            PageLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            if(ReportsItemsLayout != null)
                ReportsItemsLayout.Span = availableWidth < 1320 ? 1 : 2;

            if(ReportControlGrid != null && ReportTitleBlock != null && PeriodControl != null && FromControl != null && ToControl != null && ApplyPeriodButton != null && GenerateButton != null)
            {
                bool stackedControls = availableWidth < 1650;

                ReportControlGrid.ColumnDefinitions.Clear();
                ReportControlGrid.RowDefinitions.Clear();

                if(stackedControls)
                {
                    ReportControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                    ReportControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                    for(int i = 0; i < 4; i++)
                        ReportControlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    Grid.SetColumn(ReportTitleBlock, 0);
                    Grid.SetRow(ReportTitleBlock, 0);
                    Grid.SetColumnSpan(ReportTitleBlock, 2);

                    Grid.SetColumn(PeriodControl, 0);
                    Grid.SetRow(PeriodControl, 1);
                    PeriodControl.HorizontalOptions = LayoutOptions.Fill;
                    Grid.SetColumnSpan(PeriodControl, 2);

                    Grid.SetColumn(FromControl, 0);
                    Grid.SetRow(FromControl, 2);
                    FromControl.HorizontalOptions = LayoutOptions.Fill;
                    Grid.SetColumnSpan(FromControl, 1);

                    Grid.SetColumn(ToControl, 1);
                    Grid.SetRow(ToControl, 2);
                    ToControl.HorizontalOptions = LayoutOptions.Fill;
                    Grid.SetColumnSpan(ToControl, 1);

                    Grid.SetColumn(ApplyPeriodButton, 0);
                    Grid.SetRow(ApplyPeriodButton, 3);
                    Grid.SetColumnSpan(ApplyPeriodButton, 1);

                    Grid.SetColumn(GenerateButton, 1);
                    Grid.SetRow(GenerateButton, 3);
                    Grid.SetColumnSpan(GenerateButton, 1);

                    ApplyPeriodButton.HorizontalOptions = LayoutOptions.Fill;
                    GenerateButton.HorizontalOptions = LayoutOptions.Fill;
                }
                else
                {
                    ReportControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                    for(int i = 0; i < 5; i++)
                        ReportControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    ReportControlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    Grid.SetColumn(ReportTitleBlock, 0);
                    Grid.SetColumnSpan(ReportTitleBlock, 1);
                    Grid.SetRow(ReportTitleBlock, 0);
                    Grid.SetColumn(PeriodControl, 1);
                    Grid.SetColumn(FromControl, 2);
                    PeriodControl.HorizontalOptions = LayoutOptions.Start;
                    FromControl.HorizontalOptions = LayoutOptions.Start;
                    Grid.SetColumn(ToControl, 3);
                    ToControl.HorizontalOptions = LayoutOptions.Start;
                    Grid.SetColumn(ApplyPeriodButton, 4);
                    Grid.SetColumn(GenerateButton, 5);
                    Grid.SetRow(PeriodControl, 0);
                    Grid.SetRow(FromControl, 0);
                    Grid.SetRow(ToControl, 0);
                    Grid.SetRow(ApplyPeriodButton, 0);
                    Grid.SetRow(GenerateButton, 0);

                    ApplyPeriodButton.HorizontalOptions = LayoutOptions.End;
                    GenerateButton.HorizontalOptions = LayoutOptions.End;
                }
            }


            if(DetailsGrid == null || AnalyticsSidebar == null)
                return;

            // Keep the analytics panel beside the report on normal desktop widths.
            // Below this breakpoint the report gets the full content width and the
            // analytics panel moves underneath instead of squeezing the grid/header.
            bool compact = Width > 0 && Width < 1280;

            DetailsGrid.ColumnDefinitions.Clear();
            DetailsGrid.RowDefinitions.Clear();

            if(compact)
            {
                DetailsGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width=GridLength.Star
                });
                DetailsGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height=GridLength.Auto
                });
                DetailsGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height=GridLength.Auto
                });

                Grid.SetColumn(AnalyticsSidebar, 0);
                Grid.SetRow(AnalyticsSidebar, 1);
            }
            else
            {
                DetailsGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width=GridLength.Star
                });
                DetailsGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width=new GridLength(320)
                });
                DetailsGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height=GridLength.Auto
                });

                Grid.SetColumn(AnalyticsSidebar, 1);
                Grid.SetRow(AnalyticsSidebar, 0);
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
