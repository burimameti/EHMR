using EHMR.ViewModels;

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
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // WinUI invalidation workaround — исто како на CalendarDashboardPage.
            //MainThread.BeginInvokeOnMainThread(async () =>
            //{
            //    await Task.Delay(50);

            //    HubContentGrid.IsVisible=false;
            //    HubContentGrid.IsVisible=!((ReportViewModel)BindingContext).IsShowingDetails;

            //    DetailContentGrid.IsVisible=false;
            //    DetailContentGrid.IsVisible=((ReportViewModel)BindingContext).IsShowingDetails;
            //});
        }
    }
}
