using EHMR.ViewModels.Prescriptions;

namespace EHMR.Views.Prescription
{
    public partial class PrescriptionListPage : ContentPage
    {
        private readonly PrescriptionListViewModel _viewModel;
        private readonly MenuView _menuVew;

        public PrescriptionListPage(MenuView menuVew, PrescriptionListViewModel prescriptionListViewModel)
        {
            InitializeComponent();
            _menuVew=menuVew;
            _viewModel=prescriptionListViewModel;
            BindingContext=_viewModel;
            MenuHost.Content=_menuVew;
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