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
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if(BindingContext is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
