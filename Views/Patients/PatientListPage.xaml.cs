using EHMR.Domain.Entities;
using EHMR.Resources.Controls;
using EHMR.ViewModels.Patients;

namespace EHMR.Views;

public partial class PatientListPage : ContentPage
{
    private readonly PatientListViewModel _viewModel;

    public PatientListPage(PatientListViewModel viewModel, MenuView menu)
    {
        InitializeComponent();

        _viewModel=viewModel;
        BindingContext=_viewModel;

        MenuHost.Content=menu;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if(BindingContext is PatientListViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }

    private void OnSelectedStatusChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;

        if(picker.SelectedIndex<0)
            return;

        if(BindingContext is not PatientListViewModel vm)
            return;

        if(vm.StatusFilters.Count<=picker.SelectedIndex)
            return;

        var selected = vm.StatusFilters[picker.SelectedIndex];

        if(vm.SelectedStatusChangedCommand?.CanExecute(selected)==true)
            vm.SelectedStatusChangedCommand.Execute(selected);
    }
   
    private void OnSelectedGenderChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;

        if(picker.SelectedIndex<0)
            return;

        if(BindingContext is not PatientListViewModel vm)
            return;

        if(vm.GenderFilters.Count<=picker.SelectedIndex)
            return;

        var selected = vm.GenderFilters[picker.SelectedIndex];

        if(vm.SelectedGenderChangedCommand?.CanExecute(selected)==true)
            vm.SelectedGenderChangedCommand.Execute(selected);
    }

    private void OnSelectedAgeGroupChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;

        if(picker.SelectedIndex<0)
            return;

        if(BindingContext is not PatientListViewModel vm)
            return;

        if(vm.AgeGroups.Count<=picker.SelectedIndex)
            return;

        var selected = vm.AgeGroups[picker.SelectedIndex];

        if(vm.SelectedAgeGroupChangedCommand?.CanExecute(selected)==true)
            vm.SelectedAgeGroupChangedCommand.Execute(selected);
    }

    private void OnSelectedCityChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;

        if(picker.SelectedIndex<0)
            return;

        if(BindingContext is not PatientListViewModel vm)
            return;

        if(vm.CityFilterNames.Count<=picker.SelectedIndex)
            return;

        var selected = vm.CityFilterNames[picker.SelectedIndex];

        if(vm.SelectedCityChangedCommand?.CanExecute(selected)==true)
            vm.SelectedCityChangedCommand.Execute(selected);
    }

    private void OnSelectedBloodTypeChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;

        if(picker.SelectedIndex<0)
            return;

        if(BindingContext is not PatientListViewModel vm)
            return;

        if(vm.BloodTypeFilters.Count<=picker.SelectedIndex)
            return;

        var selected = vm.BloodTypeFilters[picker.SelectedIndex];

        if(vm.SelectedBloodTypeChangedCommand?.CanExecute(selected)==true)
            vm.SelectedBloodTypeChangedCommand.Execute(selected);
    }

    private async void OnActionMenuTapped(object sender, TappedEventArgs e)
    {
        var patient = e.Parameter as Patient;
        if(patient==null)
            return;

        var action = await DisplayActionSheet(
            patient.FullName,
            "Откажи",
            null,
            "👁 Преглед",
            "✎ Уреди");

        switch(action)
        {
            case "👁 Преглед":
                await _viewModel.SelectCommand.ExecuteAsync(patient);
                break;

            case "✎ Уреди":
                await _viewModel.EditCommand.ExecuteAsync(patient);
                break;
        }
    }
}