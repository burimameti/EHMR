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
            if(_viewModel!=null)
            {
                await _viewModel.LoadAsync();
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if(BindingContext is AppointmentListViewModel vm)
            {
                vm.SearchText=e.NewTextValue;
            }
        }
    }
}