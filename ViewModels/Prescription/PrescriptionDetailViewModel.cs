using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;

using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Prescriptions;

public partial class PrescriptionDetailFormViewModel : ObservableObject
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly IPatientService _patientService; // Потребно за полнење на PatientsList
    private readonly ISelectedItemService<Prescription> _selectedPrescriptionService;
    private readonly IUserDialogService _dialogService;

    [ObservableProperty] private string pageTitle = "Нов Рецепт";
    [ObservableProperty] private bool isReadOnly;
    [ObservableProperty] private Guid id;
    [ObservableProperty] private Patient? selectedPatient;
    [ObservableProperty] private string medication = string.Empty;
    [ObservableProperty] private string dosage = string.Empty;
    [ObservableProperty] private string instructions = string.Empty;
    [ObservableProperty] private string status = "Активни";
    [ObservableProperty] private ObservableCollection<Patient> patientsList = [];

    public bool IsEditMode => !IsReadOnly;

    public PrescriptionDetailFormViewModel(
        IPrescriptionService prescriptionService,
        IPatientService patientService,
        ISelectedItemService<Prescription> selectedPrescriptionService,
        IUserDialogService dialogService)
    {
        _prescriptionService=prescriptionService;
        _patientService=patientService;
        _selectedPrescriptionService=selectedPrescriptionService;
        _dialogService=dialogService;

        // Автоматско вчитавање при иницијализација
        _=InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // 1. Вчитај ги сите пациенти за Picker-от
        var patients = await _patientService.GetAllAsync();
        PatientsList=new ObservableCollection<Patient>(patients);

        // 2. Провери дали уредуваме постоечки рецепт преку SelectedItemService
        var currentPrescription = _selectedPrescriptionService.SelectedItem;
        if(currentPrescription!=null)
        {
            Id=currentPrescription.Id;
            Medication=currentPrescription.Medication??string.Empty;
            Dosage=currentPrescription.Dosage??string.Empty;
            Instructions=currentPrescription.Instructions??string.Empty;
            Status=currentPrescription.Status??"Активни";
            PageTitle=$"Рецепт: {currentPrescription.Medication}";
            IsReadOnly=true; // Започнува во преглед режим

            // Селектирај го точниот пациент во листата
            SelectedPatient=PatientsList.FirstOrDefault(p => p.Id==currentPrescription.PatientId);
        }
        else
        {
            Id=Guid.Empty;
            IsReadOnly=false; // Нов запис започнува директно во Edit режим
            PageTitle="Нов Рецепт";
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        // Валидација
        if(SelectedPatient==null)
        {
            await _dialogService.ShowAlertAsync("Грешка", "Ве молиме изберете пациент.", "ОК");
            return;
        }

        if(string.IsNullOrWhiteSpace(Medication))
        {
            await _dialogService.ShowAlertAsync("Грешка", "Полето за лек е задолжително.", "ОК");
            return;
        }

        var isNew = Id==Guid.Empty;

        var prescription = new Prescription
        {
            Id=isNew ? Guid.NewGuid() : Id,
            PatientId=SelectedPatient.Id,
            Medication=Medication.Trim(),
            Dosage=Dosage.Trim(),
            Instructions=Instructions.Trim(),
            Status=Status
        };

        bool success;
        if(isNew)
        {
            var result = await _prescriptionService.CreateAsync(prescription);
            success=result!=null;
        }
        else
        {
            success=await _prescriptionService.UpdateAsync(prescription);
        }

        if(success)
        {
             await _dialogService.ShowAlertAsync("Успешно", "Податоците се успешно зачувани.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await _dialogService.ShowAlertAsync("Грешка", "Проблем при зачувување на податоците.", "ОК");
        }
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        IsReadOnly=!IsReadOnly;
        OnPropertyChanged(nameof(IsEditMode));
    }

    [RelayCommand]
    private async Task Back()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }
}