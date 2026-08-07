using EHMR.ViewModels.Prescriptions;
using Microsoft.Maui.Controls;

namespace EHMR.Views.Prescription
{
    public partial class PrescriptionDetailFormPage : ContentPage
    {
        private readonly MenuView _menuVew;
        private readonly PrescriptionDetailFormViewModel _viewModel;

        public PrescriptionDetailFormPage(MenuView menuVew, PrescriptionDetailFormViewModel prescriptionDetailFormViewModel)
        {
            InitializeComponent();
            _menuVew=menuVew;
            _viewModel=prescriptionDetailFormViewModel;
            BindingContext=_viewModel;
            MenuHost.Content=_menuVew;
        }
    }
}