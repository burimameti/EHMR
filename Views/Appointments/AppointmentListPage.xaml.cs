using EHMR.ViewModels;

namespace EHMR.Views
{
    public partial class AppointmentListPage : ContentPage
    {
        private readonly AppointmentListViewModel _viewModel;

        public AppointmentListPage(AppointmentListViewModel viewModel, MenuView menu)
        {
            InitializeComponent();
            _viewModel=viewModel;
            BindingContext=_viewModel;
            MenuHost.Content=menu;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // ЕДИНСТВЕНО место каде што се повикува вчитување на податоците од базата при влез во страницата
            if(_viewModel!=null)
            {
                await _viewModel.LoadAsync();
            }
        }
    }
}