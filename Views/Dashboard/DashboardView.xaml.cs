using EHMR.Domain.Entities;
using EHMR.Resources.Controls;
using EHMR.ViewModels;
using EHMR.ViewModels.Patients;
using System.Diagnostics;
using System.Windows.Input;

namespace EHMR.Views;

public partial class DashboardView : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardView(
         DashboardViewModel vm,
         MenuView menu
       )
    {
        InitializeComponent();

        _viewModel=vm;

        BindingContext=_viewModel;
        MenuHost.Content=menu;
     
    }
    

    // constructor

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if(BindingContext is DashboardViewModel vm)
        {
            vm.ApplySearch(e.NewTextValue);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Debug.WriteLine("Dashboard OnAppearing");

        if(BindingContext is DashboardViewModel vm)
        {
            Debug.WriteLine($"VM found: {vm.GetHashCode()}");
            _=vm.Initialize();
        }
    }
    private void OnSelectedStatusChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;

        if(picker.SelectedIndex<0)
            return;

        if(BindingContext is not DashboardViewModel vm)
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

        if(BindingContext is not DashboardViewModel vm)
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

        if(BindingContext is not DashboardViewModel vm)
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

        if(BindingContext is not DashboardViewModel vm)
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
    private CancellationTokenSource _searchCts;
    private async void PatientSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if(_viewModel==null)
            return;

        _searchCts?.Cancel();
        _searchCts=new CancellationTokenSource();

        var token = _searchCts.Token;
        var query = e.NewTextValue;

        try
        {
            await Task.Delay(250, token); // debounce

            if(!token.IsCancellationRequested)
            {
                 _viewModel.ApplySearch(query);
            }
        }
        catch(TaskCanceledException)
        {
            // ignore
        }
    }
}