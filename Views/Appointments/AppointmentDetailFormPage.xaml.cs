using EHMR.ViewModels;
using Windows.Graphics.Display;

namespace EHMR.Views
{
    public partial class AppointmentDetailFormPage : ContentPage
    {
        private readonly AppointmentDetailFormViewModel _viewModel;
        private readonly MenuView _menu;

        public AppointmentDetailFormPage(AppointmentDetailFormViewModel viewModel, MenuView menu)
        {
            InitializeComponent();
            _viewModel=viewModel;
            _menu=menu;
            BindingContext=_viewModel;
            MenuHost.Content=menu;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if(BindingContext is AppointmentDetailFormViewModel viewModel)
            {
                // Проверува дали има зачуван повратен пациент од модалот и автоматски го селектира
                viewModel.CheckAndApplyReturnedPatient();
            }
        }
    }
}