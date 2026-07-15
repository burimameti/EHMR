using EHMR.ViewModels;

namespace EHMR.Views.Doctors
{
    public partial class DoctorsDetailPage : ContentPage
    {
        private readonly DoctorsDetailViewModel _viewModel;
        private readonly MenuView menuView;

        public DoctorsDetailPage(DoctorsDetailViewModel viewModel, MenuView menuView)
        {
            InitializeComponent();
            _viewModel=viewModel;
            this.menuView=menuView;
            BindingContext=_viewModel;
            MenuHost.Content=this.menuView;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}