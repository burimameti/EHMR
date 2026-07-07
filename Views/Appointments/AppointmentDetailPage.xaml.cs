using EHMR.ViewModels.Appointments;
using EHMR.Views;

namespace EHMR.Views.Appointments;

public partial class AppointmentDetailPage : ContentPage
{
    private readonly AppointmentDetailViewModel _viewModel;
    private readonly MenuView _menu;

    public AppointmentDetailPage(AppointmentDetailViewModel viewModel, MenuView menu)
    {
        InitializeComponent();

        _viewModel=viewModel;
        _menu=menu;

        BindingContext=_viewModel;
        //MenuHost.Content=menu;
    }
    private async void OnDiagnosisSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if(_viewModel.SearchDiagnosesCommand.CanExecute(e.NewTextValue))
            await _viewModel.SearchDiagnosesCommand.ExecuteAsync(e.NewTextValue);
    }

    private async void OnPatientPickerChanged(object sender, EventArgs e)
    {
        if(sender is Picker { SelectedItem: EHMR.Domain.Entities.Patient patient })
        {
            if(_viewModel.PatientChangedCommand.CanExecute(patient))
                await _viewModel.PatientChangedCommand.ExecuteAsync(patient);
        }
    }
  
    private void OnPatientSelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;

        if(picker.SelectedIndex<0)
            return;

        var vm = BindingContext as AppointmentDetailViewModel;
        if(vm==null) return;

        if(vm.PatientsList==null||vm.PatientsList.Count==0)
            return;

        var selectedPatient = vm.PatientsList[picker.SelectedIndex];

        vm.SelectedPatientForAppointment=selectedPatient;

        if(vm.PatientChangedCommand?.CanExecute(selectedPatient)==true)
        {
            vm.PatientChangedCommand.Execute(selectedPatient);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            if(BindingContext is AppointmentDetailViewModel vm)
            {
                await vm.LoadAsync();
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}