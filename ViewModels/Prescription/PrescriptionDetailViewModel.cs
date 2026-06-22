using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Office2010.Excel;
using EHMR.Domain.Entities;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Prescriptions;

public partial class PrescriptionDetailFormViewModel : ObservableObject
{
    [ObservableProperty]
    private string pageTitle = "Рецепт";

    [ObservableProperty]
    private bool isReadOnly;

    [ObservableProperty]
    private Guid id;

    [ObservableProperty]
    private Patient? selectedPatient;

    [ObservableProperty]
    private string medication = string.Empty;

    [ObservableProperty]
    private string dosage = string.Empty;

    [ObservableProperty]
    private string instructions = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Patient> patientsList = [];

    public bool IsEditMode => !IsReadOnly;

    [RelayCommand]
    private async Task Save()
    {
        // validation

        if(SelectedPatient==null)
            return;

        var prescription = new Prescription
        {
            Id=Guid.NewGuid(),
            PatientId=SelectedPatient.Id
        };

        // use domain methods if available
        // prescription.Update(...);

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Back()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        IsReadOnly=!IsReadOnly;
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }
}