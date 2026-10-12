using System;
using EHMR.ViewModels;
using EHMR.Views;

namespace EHMR.Views.Patients
{
    public partial class PatientDetailFormPage : ContentPage
    {
        private readonly PatientDetailFormViewModel _viewModel;
        private readonly MenuView _menu;

        public PatientDetailFormPage(PatientDetailFormViewModel viewModel, MenuView menuView)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _menu = menuView;
            BindingContext = _viewModel;
            MenuHost.Content = _menu;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await _viewModel.OnPageAppearingAsync();
                await _viewModel.RefreshHistoryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PatientDetail] Patient history refresh failed: {ex}");
            }
        }
    }
}
