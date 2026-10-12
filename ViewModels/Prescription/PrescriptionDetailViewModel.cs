using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Domain.Entities.Rbac;
using EHMR.Services.Dto;
using EHMR.Services;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Prescriptions;

public partial class PrescriptionDetailFormViewModel : ObservableObject
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly IPatientService _patientService; // Потребно за полнење на PatientsList
    private readonly ISelectedItemService<Prescription> _selectedPrescriptionService;
    private readonly IUserDialogService _dialogService;
    private readonly IAuthorizationService _authorization;
    private readonly FormSessionLifecycle _formSessions;
    private bool _hasInitialized;
    private bool _isNewMode;
    private Prescription? _loadedPrescription;

    [ObservableProperty] private string pageTitle = "Нов Рецепт";
    [ObservableProperty] private bool isReadOnly;
    [ObservableProperty] private Guid id;
    [ObservableProperty] private PatientDto? selectedPatient;
    [ObservableProperty] private string medication = string.Empty;
    [ObservableProperty] private string dosage = string.Empty;
    [ObservableProperty] private string instructions = string.Empty;
    [ObservableProperty] private string status = "Активни";
    [ObservableProperty] private ObservableCollection<PatientDto> patientsList = [];

    public bool CanView => _authorization.CanPerform(Modules.Prescriptions, ModuleAction.View);
    public bool CanCreate => _authorization.CanPerform(Modules.Prescriptions, ModuleAction.Create);
    public bool CanUpdate => _authorization.CanPerform(Modules.Prescriptions, ModuleAction.Edit);
    public bool CanDelete => _authorization.CanPerform(Modules.Prescriptions, ModuleAction.Delete);
    public bool CanSave => Id==Guid.Empty ? CanCreate : CanUpdate;
    public bool CanEditForm => Id==Guid.Empty ? CanCreate : CanUpdate;
    public bool IsEditMode => !IsReadOnly;

    public PrescriptionDetailFormViewModel(
        IPrescriptionService prescriptionService,
        IPatientService patientService,
        ISelectedItemService<Prescription> selectedPrescriptionService,
        IUserDialogService dialogService,
        IAuthorizationService authorization,
        FormSessionLifecycle formSessions)
    {
        _prescriptionService=prescriptionService;
        _patientService=patientService;
        _selectedPrescriptionService=selectedPrescriptionService;
        _dialogService=dialogService;
        _authorization=authorization;
        _formSessions=formSessions;
    }

    /// <summary>
    /// Loads the prescription form when the page becomes visible. Create entries
    /// are reset only when this same form instance is re-entered after completion.
    /// </summary>
    public async Task OnPageAppearingAsync()
    {
        if(!CanView)
            return;

        if(_hasInitialized && _isNewMode)
        {
            _formSessions.BeginCreateEntry(this, ResetForNewPrescriptionSession);
        }

        await LoadPatientsAsync();

        var currentPrescription = _selectedPrescriptionService.SelectedItem;
        if(currentPrescription != null)
        {
            _loadedPrescription=currentPrescription;
            _selectedPrescriptionService.SelectedItem=null; // consume navigation context; keep the loaded record for re-entry
        }
        else if(_hasInitialized && !_isNewMode)
        {
            currentPrescription=_loadedPrescription;
        }

        if(currentPrescription != null)
        {
            _isNewMode=false;
            Id=currentPrescription.Id;
            Medication=currentPrescription.Medication ?? string.Empty;
            Dosage=currentPrescription.Dosage ?? string.Empty;
            Instructions=currentPrescription.Instructions ?? string.Empty;
            Status=currentPrescription.Status ?? "Активни";
            PageTitle=$"Рецепт: {currentPrescription.Medication}";
            IsReadOnly=true;
            SelectedPatient=PatientsList.FirstOrDefault(p => p.Id==currentPrescription.PatientId);
        }
        else
        {
            _isNewMode=true;
            if(!_hasInitialized)
                ResetForNewPrescriptionSession();

            IsReadOnly=false;
            PageTitle="Нов Рецепт";
        }

        _hasInitialized=true;
        _formSessions.MarkEntryCompleted(this);
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanEditForm));
        OnPropertyChanged(nameof(IsEditMode));
    }

    private async Task LoadPatientsAsync()
    {
        var patients = await _patientService.GetAllAsync();
        PatientsList=new ObservableCollection<PatientDto>(patients);
    }

    private void ResetForNewPrescriptionSession()
    {
        Id=Guid.Empty;
        SelectedPatient=null;
        Medication=string.Empty;
        Dosage=string.Empty;
        Instructions=string.Empty;
        Status="Активни";
        PageTitle="Нов Рецепт";
        IsReadOnly=false;
    }

    [RelayCommand]
    private async Task Save()
    {
        var isNew = Id==Guid.Empty;
        if((isNew && !CanCreate) || (!isNew && !CanUpdate)) return;

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
        if(Id!=Guid.Empty && !CanUpdate) return;
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