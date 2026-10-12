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

        private bool _isInitializing;

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Avoid overlapping async loads if Shell raises OnAppearing repeatedly.
            if(_isInitializing)
                return;

            _isInitializing=true;
            try
            {
                await _viewModel.OnPageAppearingAsync();
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrescriptionDetail] Initialization failed: {ex}");
            }
            finally
            {
                _isInitializing=false;
            }
        }
    }
}