using EHMR.ViewModels;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace EHMR.Views;

public partial class DashboardView : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardView(DashboardViewModel vm, MenuView menu)
    {
        InitializeComponent();

        _viewModel=vm;
        BindingContext=_viewModel;
        MenuHost.Content=menu;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Debug.WriteLine("Dashboard OnAppearing");
        Dispatcher.Dispatch(() => _ = _viewModel.Initialize());
    }

    private void OnCalendarIconTapped(object sender, TappedEventArgs e)
    {
        CalendarDatePicker.IsVisible=true;
        CalendarDatePicker.Focus();
    }

    private void CalendarDatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        CalendarDatePicker.IsVisible=false;
    }
}