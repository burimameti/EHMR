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
