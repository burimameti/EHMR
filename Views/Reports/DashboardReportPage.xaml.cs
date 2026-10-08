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
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if(!_vm.IsShowingDetails)
                await _vm.OpenPatientsReportAsync();

            // Rebuild provider-backed picker items after report initialization,
            // including when the page is revisited with an existing ViewModel.
            _vm.RefreshReportControls();
        }

    }
}
