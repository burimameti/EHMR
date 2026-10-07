using EHMR.ViewModels.Reports;
using Microsoft.Maui.Controls;

namespace EHMR.Views.Reports
{
    public partial class ReportsListPage : ContentPage
    {
        private readonly ReportListViewModel _viewModel;

        public ReportsListPage(ReportListViewModel viewModel, MenuView menu)
        {
            InitializeComponent();

            _viewModel=viewModel;
            BindingContext=_viewModel;
            MenuHost.Content=menu;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if(BindingContext is ReportListViewModel vm)
                Dispatcher.Dispatch(() => _ = vm.LoadAsync());
        }
    }
}
