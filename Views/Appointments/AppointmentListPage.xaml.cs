using EHMR.ViewModels;
using EHMR.ViewModels.Appointments;
using EHMR.ViewModels.Patients;
using Microsoft.Maui.Controls;
using System;
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
        private void OnSelectedStatusChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;

            if(picker.SelectedIndex<0)
                return;

            if(BindingContext is not AppointmentListViewModel vm)
                return;

            if(vm.StatusFilters.Count<=picker.SelectedIndex)
                return;

            var selected = vm.StatusFilters[picker.SelectedIndex];

            if(vm.SelectSuggestionCommand?.CanExecute(selected)==true)
                vm.SelectSuggestionCommand.Execute(selected);
        }
        //private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        //{
        //    if(BindingContext is AppointmentListViewModel vm)
        //    {
        //        vm.SearchText=e.NewTextValue;
        //    }
        //}
    }
}