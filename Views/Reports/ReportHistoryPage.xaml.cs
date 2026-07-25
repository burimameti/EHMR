using EHMR.ViewModels;
using EHMR.ViewModels.Reports;

namespace EHMR.Views.Reports
{
    public partial class ReportHistoryPage : ContentPage
    {
        private readonly ReportHistoryViewModel _vm;
        private readonly MenuView _menu;
        public ReportHistoryPage(ReportHistoryViewModel vm, MenuView menu)
        {
            InitializeComponent();
            _vm=vm;
            _menu=menu;
            BindingContext=_vm;
            MenuHost.Content=_menu;
        }

        protected override async void OnAppearing()
        {
            // Автоматски иницијализирај ја формата со вредностите од ISelectedItemService
            if(BindingContext is ReportHistoryViewModel vm)
            {
                await _vm.LoadAsync();
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
