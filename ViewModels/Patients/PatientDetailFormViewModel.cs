using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Infrastructure.Persistence;
using EHMR.Services.Dto;

using EHMR.ViewModels.Patients.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using static EHMR.Services.PatientService;

namespace EHMR.ViewModels;

public partial class PatientDetailFormViewModel : ObservableObject, IDisposable
{
    private readonly IPatientService _patientService;
    private readonly ISelectedItemService<Patient> _selectedItemService;
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _userDialogService;
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ILicenseService _licenseService;
    private readonly IPatientClinicalReportService _clinicalReportService;
    private readonly IAuthorizationService _authorizationService;

    private PatientEditDto? _originalPatient;
    private bool _isNewPatientMode;
    private bool _isModalReturnMode;

    private readonly List<Guid> _deletedPatientMkb10CodeIds = [];
    private readonly List<Guid> _deletedMedicineIds = [];
    private readonly List<Guid> _deletedDocumentIds = [];

    /// <summary>Споделен едитор за скорови (опис + бројка, предлози по употреба).</summary>
    public ScoreEditorViewModel ScoreEditor
    {
        get;
    }

    private bool _childrenLoaded;
    public bool IsNewPatient => _isNewPatientMode;

    public bool CanViewPatient => _authorizationService.CanPerform(Modules.Patients, ModuleAction.View);
    public bool CanViewNationalId =>
        _authorizationService.HasRole(UserRole.Admin)||_authorizationService.HasRole(UserRole.SuperAdmin);
    public bool CanCreatePatient => _authorizationService.CanPerform(Modules.Patients, ModuleAction.Create);
    public bool CanEditPatient => _authorizationService.CanPerform(Modules.Patients, ModuleAction.Edit);
    public bool CanDeletePatient => _authorizationService.CanPerform(Modules.Patients, ModuleAction.Delete);
    public bool CanActivatePatient => _authorizationService.CanPerform(Modules.Patients, ModuleAction.Activate);
    public bool CanDeactivatePatient => _authorizationService.CanPerform(Modules.Patients, ModuleAction.Deactivate);
    public bool CanManageAdministration => _authorizationService.CanPerform(Modules.Administration, ModuleAction.Manage);
    public bool CanSavePatient => _isNewPatientMode ? CanCreatePatient : CanEditPatient;
public bool CanSavePatientForm => CanSavePatient;
    public string MaskedNationalId => MaskNationalId(Patient.NationalId);

    public bool CanReactivatePatient => CanActivatePatient&&!_isNewPatientMode&&Patient.Status==PatientStatus.Inactive;
    public bool CanShowActiveStatusOption => _isNewPatientMode||Patient.Status!=PatientStatus.Inactive||CanActivatePatient;

    private bool _isOfferingDoctorCreation;

    public string HeaderTitle => _isNewPatientMode ? "Нов Пациент" : "Детали за пациент";
    public string HeaderSubtitle => _isNewPatientMode ? "Креирање ново пациентско досие" : "";

    [ObservableProperty]
    private PatientEditDto patient = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(InputBgColor))]
    [NotifyPropertyChangedFor(nameof(InputBorderColor))]
    private bool isReadOnly = true;

    [ObservableProperty] private string pageTitle = string.Empty;

    public bool IsEditMode => !IsReadOnly;

    public Color InputBgColor => IsReadOnly ? Color.FromArgb("#F8FAFC") : Color.FromArgb("#FFFFFF");
    public Color InputBorderColor => IsReadOnly ? Color.FromArgb("#CBD5E1") : Color.FromArgb("#2563EB");

    public ObservableCollection<string> GenderOptions { get; } = PatientEnumLookups.Gender.ToObservableCollection();
    public ObservableCollection<string> StatusOptions { get; } = PatientEnumLookups.Status.ToObservableCollection();
    private readonly FilterLookup _cityLookup = PatientFilterLookups.BuildCityLookupForForm();
    public ObservableCollection<string> CityOptions => _cityLookup.ToObservableCollection();
    public ObservableCollection<string> RelationOptions
    {
        get;
    } = new()
    {
        "Сопруг / Сопруга", "Родител", "Дете", "Брат / Сестра", "Пријател", "Друго"
    };

    [ObservableProperty] private string selectedGenderDisplay = string.Empty;
    [ObservableProperty] private bool isPatientActive = true;
    [ObservableProperty] private string inactiveReason = string.Empty;

    public bool IsPatientInactive => !IsPatientActive;

    partial void OnIsPatientActiveChanged(bool value)
    {
        Patient.Status=value ? PatientStatus.Active : PatientStatus.Inactive;
        if(value)
        {
            InactiveReason=string.Empty;
            Patient.InactiveReason=string.Empty;
        }
        OnPropertyChanged(nameof(IsPatientInactive));
        OnPropertyChanged(nameof(CanReactivatePatient));
        OnPropertyChanged(nameof(CanShowActiveStatusOption));
        OnPropertyChanged(nameof(Patient));
    }

    partial void OnInactiveReasonChanged(string value)
        => Patient.InactiveReason=value??string.Empty;

    [RelayCommand]
    private void SetActive()
    {
        if(!IsEditMode||!CanEditPatient)
            return;

        // Reactivation of an inactive patient is a sensitive operation and is Admin-only.
        if(!_isNewPatientMode&&Patient.Status==PatientStatus.Inactive&&!CanActivatePatient)
        {
            _userDialogService.ShowAlertAsync(
                "Недозволена акција",
                "Само администратор може да реактивира неактивен пациент.",
                "ОК");
            return;
        }

        IsPatientActive=true;
        OnPropertyChanged(nameof(CanReactivatePatient));
    }

    [RelayCommand]
    private void SetInactive()
    {
        if(IsEditMode&&CanEditPatient)
            IsPatientActive=false;
    }

    [ObservableProperty] private string selectedStatusDisplay = string.Empty;
    [ObservableProperty] private string selectedRelationDisplay = string.Empty;
    [ObservableProperty] private string selectedCityDisplay = string.Empty;

    partial void OnSelectedRelationDisplayChanged(string value) => Patient.EmergencyRelationship=value??string.Empty;
    partial void OnSelectedCityDisplayChanged(string value) =>
        Patient.City=_cityLookup.ToInternal(value??string.Empty);

    public PatientDetailFormViewModel(
        IPatientService patientService,
        ISelectedItemService<Patient> selectedItemService,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ILicenseService licenseService,
        IPatientClinicalReportService clinicalReportService,
        IAuthorizationService authorizationService)
    {
        _licenseService=licenseService;
        _patientService=patientService;
        _selectedItemService=selectedItemService;
        _navigationService=navigationService;
        _userDialogService=userDialogService;
        _dbFactory=dbFactory;
        _clinicalReportService=clinicalReportService;
        _authorizationService=authorizationService;

        // MKB10 A-Z sections, исто као во EncounterBaseViewModel.
        for(var letter = 'A'; letter<='Z'; letter++)
            MkbAlphabetSections.Add(new MkbAlphabetSection(letter.ToString(), false));

        ScoreEditor=new ScoreEditorViewModel(dbFactory);

        InitializeForm();
    }

    private void InitializeForm()
    {
        var selectedPatient = _selectedItemService.SelectedItem;

        if(selectedPatient is { Id: var id, FirstName: "APPOINTMENT_CONTEXT" }&&id==Guid.Empty)
        {
            _isModalReturnMode=true;
            selectedPatient=null;
        }

        if(selectedPatient==null)
        {
            _isNewPatientMode=true;
            _selectedItemService.OpenInEditMode=true;

            Patient=CreateBlankForRegistration();
            PageTitle="Нов Пациент";
            IsReadOnly=!CanCreatePatient;
            ScoreEditor.IsReadOnly=IsReadOnly;

            SyncDisplayFromPatient();
            _=LoadApplicationRegimesAsync();
            // MKB results remain empty until the user searches or selects an A-Z section.

            OnPropertyChanged(nameof(IsNewPatient));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(HeaderSubtitle));
            return;
        }

        IsReadOnly=!_selectedItemService.OpenInEditMode||!CanEditPatient;
        ScoreEditor.IsReadOnly=IsReadOnly;

        // Everything (edit fields + children) now comes from one DTO fetch.
        _=LoadPatientAsync(selectedPatient.Id);

        OnPropertyChanged(nameof(IsNewPatient));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(MaskedNationalId));

        OnPropertyChanged(nameof(CanSavePatientForm));
    }

    private async Task LoadPatientAsync(Guid patientId)
    {
        try
        {
            var full = await _patientService.GetByIdAsync(patientId, includeChildren: true);
            if(full==null) return;

            Patient=MapToEditDto(full);
            _originalPatient=CloneEditDto(Patient);

            PageTitle=$"Досие: {Patient.FirstName} {Patient.LastName}";

            // Inactive patient records are read-only regardless of the route that opened them.
            if(Patient.Status==PatientStatus.Inactive)
                IsReadOnly=true;

            SyncDisplayFromPatient();

            PatientMkb10Codes=new ObservableCollection<PatientMkb10CodeDto>(full.PatientMkb10Codes);

            // Тековни терапии = IsActive. Претходни = !IsActive (рачно внесени или затворени).
            AttachedMedicines=new ObservableCollection<AttachedMedicineRow>(
                full.Medicines
                    .Where(m => m.IsActive)
                    .Select(m => new AttachedMedicineRow(m)));

            PreviousMedicines=new ObservableCollection<PreviousMedicineRow>(
                full.Medicines
                    .Where(m => !m.IsActive)
                    .Select(m => new PreviousMedicineRow(m)));

            OnPropertyChanged(nameof(CanSavePatientForm));
            OnPropertyChanged(nameof(MaskedNationalId));

            Documents=new ObservableCollection<PatientDocumentDto>(full.Documents);
            SelectedDocumentPreview=null;

            SelectedDoctorDisplay=full.DoctorDisplay;
            DoctorSearchText=SelectedDoctorDisplay;

            await LoadApplicationRegimesAsync();
            await LoadHistoryAsync(patientId);

            _childrenLoaded=true;
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"[PatientDetail] Failed to load patient {patientId}: {ex}");
            await _userDialogService.ShowAlertAsync(
                "Грешка",
                $"Податоците за пациентот не може да се вчитаат: {ex.Message}",
                "ОК");
        }
    }

    // ═══════════════════════════════════════════ ИСТОРИЈА НА ПАЦИЕНТОТ ═══════════════════════════════════════════
    // Прикажувањето е само за читање, па се вчитуваат директно како ентитети —
    // PatientDto носи MKB-10 доделувања, лекови и документи.

    [ObservableProperty] private ObservableCollection<PatientMkb10CodeDto> mkb10History = new();
    [ObservableProperty] private ObservableCollection<Encounter> encounterHistory = new();
    [ObservableProperty] private ObservableCollection<PatientScore> scoreHistory = new();

    public string CurrentPatientScore => ScoreHistory.FirstOrDefault()?.DisplayText??"—";
    public DateTime? CurrentPatientScoreDate => ScoreHistory.FirstOrDefault()?.RecordedAt;

    [ObservableProperty] private ObservableCollection<Appointment> appointmentHistory = new();
[ObservableProperty] private ObservableCollection<Prescription> prescriptionHistory = new();
    [ObservableProperty] private ObservableCollection<PatientMedicine> medicineHistory = new();

    private async Task LoadHistoryAsync(Guid patientId)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            Mkb10History=new ObservableCollection<PatientMkb10CodeDto>(
                await db.Set<PatientMkb10Assignment>()
                    .AsNoTracking()
                    .Where(x => x.PatientId==patientId)
                    .OrderByDescending(x => x.DiagnosedAt)
                    .Select(x => new PatientMkb10CodeDto
                    {
                        Id=x.Id,
                        PatientId=x.PatientId,
                        EncounterId=x.EncounterId,
                        Mkb10CodeId=x.Mkb10CodeId,
                        Mkb10Code=x.Mkb10Code!=null ? x.Mkb10Code.Code : string.Empty,
                        Mkb10Description=x.Mkb10Code!=null ? x.Mkb10Code.Description??string.Empty : string.Empty,
                        DiagnosedAt=x.DiagnosedAt,
                        IsPrimary=x.IsPrimary,
                        Severity=x.Severity,
                        ClinicalDescription=x.ClinicalDescription,
                        Status=x.Status
                    })
                    .ToListAsync());

            EncounterHistory=new ObservableCollection<Encounter>(
                await db.Encounters
                    .AsNoTracking()
                    .Where(x => x.PatientId==patientId)
                    .OrderByDescending(x => x.EncounterDate)
                    .ToListAsync());

            ScoreHistory=new ObservableCollection<PatientScore>(
                await db.PatientScores
                    .AsNoTracking()
                    .Where(x => x.PatientId==patientId)
                    .OrderByDescending(x => x.RecordedAt)
                    .ToListAsync());
            OnPropertyChanged(nameof(CurrentPatientScore));
            OnPropertyChanged(nameof(CurrentPatientScoreDate));
            ScoreEditor.Load(ScoreHistory);

            AppointmentHistory=new ObservableCollection<Appointment>(
                await db.Appointments
                    .AsNoTracking()
                    .Where(x => x.PatientId==patientId)
                    .OrderByDescending(x => x.ScheduledStart)
                    .ToListAsync());

PrescriptionHistory=new ObservableCollection<Prescription>(
                await db.Prescriptions
                    .AsNoTracking()
                    .Where(x => x.PatientId==patientId)
                    .ToListAsync());

            MedicineHistory=new ObservableCollection<PatientMedicine>(
                await db.PatientMedicines
                    .AsNoTracking()
                    .Include(x => x.Medicine)
                    .Where(x => x.PatientId==patientId)
                    .ToListAsync());
        }
        catch(Exception ex)
        {
            // Историјата е придружна информација — нејзиниот пад не смее да го
            // сруши отворањето на досието.
            Debug.WriteLine($"Не успеа вчитување на историјата за пациентот: {ex}");
        }
    }

    private static PatientEditDto MapToEditDto(PatientDto source) => new()
    {
        Id=source.Id,
        FirstName=source.FirstName,
        LastName=source.LastName,
        NationalId=source.NationalId,
        SzboNumber=source.SzboNumber,
        BirthDate=source.BirthDate,
        Gender=source.Gender,
        DoctorId=source.DoctorId,
        Phone=source.Phone,
        Email=source.Email,
        Address=source.Address,
        City=source.City,
        PostalCode=source.PostalCode,
        EmergencyContactName=source.EmergencyContactName,
        EmergencyContactPhone=source.EmergencyContactPhone,
        EmergencyRelationship=source.EmergencyRelationship,
        Allergies=source.Allergies,
        Status=source.Status,
        InactiveReason=source.InactiveReason
    };

    private static PatientEditDto CloneEditDto(PatientEditDto p) => new()
    {
        Id=p.Id,
        FirstName=p.FirstName,
        LastName=p.LastName,
        NationalId=p.NationalId,
        SzboNumber=p.SzboNumber,
        BirthDate=p.BirthDate,
        Gender=p.Gender,
        DoctorId=p.DoctorId,
        Phone=p.Phone,
        Email=p.Email,
        Address=p.Address,
        City=p.City,
        PostalCode=p.PostalCode,
        EmergencyContactName=p.EmergencyContactName,
        EmergencyContactPhone=p.EmergencyContactPhone,
        EmergencyRelationship=p.EmergencyRelationship,
        Allergies=p.Allergies,
        Status=p.Status,
        InactiveReason=p.InactiveReason
    };

    // ------------------------------------------------------------------ //
    // Commands
    // ------------------------------------------------------------------ //
    [RelayCommand]
    private async Task ToggleEditModeAsync()
    {
        if(!IsReadOnly)
        {
            IsReadOnly=true;
            return;
        }

        if(!_isNewPatientMode&&!CanEditPatient)
        {
            await _userDialogService.ShowAlertAsync("Недозволена акција", "Немате авторизација за измена на пациент.", "ОК");
            return;
        }

        if(!_isNewPatientMode&&Patient.Status==PatientStatus.Inactive&&!CanActivatePatient)
        {
            await _userDialogService.ShowAlertAsync(
                "Пациентот е неактивен",
                "Податоците за неактивен пациент се заклучени. Само администратор може да го реактивира пациентот.",
                "ОК");
            return;
        }

        IsReadOnly=false;
    }

    private bool _isSaving;

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if(IsSaving) return;

        if(!CanSavePatient)
        {
            await _userDialogService.ShowAlertAsync("Недозволена акција", "Немате авторизација за зачувување на пациент.", "ОК");
            return;
        }

        if(string.IsNullOrWhiteSpace(Patient.FirstName)||string.IsNullOrWhiteSpace(Patient.LastName))
        {
            await _userDialogService.ShowAlertAsync("Валидација", "Името и презимето се задолжителни.", "OK");
            return;
        }

        if(!string.IsNullOrWhiteSpace(Patient.NationalId)&&Patient.NationalId.Length!=13)
        {
            await _userDialogService.ShowAlertAsync("Валидација", "ЕМБГ мора да содржи точно 13 цифри.", "OK");
            return;
        }

        Patient.NationalId=string.IsNullOrWhiteSpace(Patient.NationalId)
            ? null
            : Patient.NationalId.Trim();

        Patient.SzboNumber=Patient.SzboNumber?.Trim()??string.Empty;

        if(Patient.Status==PatientStatus.Inactive&&string.IsNullOrWhiteSpace(Patient.InactiveReason))
        {
            await _userDialogService.ShowAlertAsync("Валидација", "Причината за неактивен пациент е задолжителна.", "ОК");
            return;
        }

        if(Patient.Status==PatientStatus.Inactive&&!Documents.Any(x => x.DocumentType==PatientDocumentType.Resenie))
        {
            await _userDialogService.ShowAlertAsync("Валидација", "За неактивен пациент мора да се прикачи решение за неактивност.", "ОК");
            return;
        }

        if(Patient.DoctorId==Guid.Empty)
        {
            await _userDialogService.ShowAlertAsync("Валидација", "Реуматолог не е доделен.", "OK");
            return;
        }

        var emptyScore = ScoreEditor.Items.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Description));
        if(emptyScore!=null)
        {
            await _userDialogService.ShowAlertAsync("Валидација", "Описот на скорот е задолжителен.", "OK");
            return;
        }

        // Лимитот важи само за нови пациенти — измена на постоечки останува можна
        // и по заклучување, за да не се изгуби пристап до веќе внесените досиеја.
        if(_isNewPatientMode)
        {
            var check = await _licenseService.CanAddPatientAsync();

            if(!check.Allowed)
            {
                await _userDialogService.ShowAlertAsync(
                    "Лиценца",
                    check.Message??"Лимитот е достигнат.",
                    "OK");
                return;
            }
        }

        var confirmed = await _userDialogService.ShowConfirmationAsync(
            "Потврда",
            "Дали сте сигурни дека сакате да ги зачувате податоците?",
            "Зачувај",
            "Откажи");

        if(!confirmed)
            return;

        var saveModel = new PatientSaveModel
        {
            Patient=Patient,
            IsNewPatient=_isNewPatientMode,

            PatientMkb10Codes=
            [
                .. PatientMkb10Codes.Select(x => new PatientMkb10CodeSaveModel
                {
                    Id = x.Id,
                    // PatientMkb10Codes entered from the Patient form belong to the patient,
                    // not to a specific Encounter.                   
                    Mkb10CodeId = x.Mkb10CodeId,
                    DiagnosedAt = x.DiagnosedAt,
                    IsPrimary = x.IsPrimary,
                    Severity = x.Severity,
                    ClinicalDescription = x.ClinicalDescription,
                    Status = x.Status
                })
            ],

            Medicines=
            [
                .. AttachedMedicines.Select(x => ToMedicineSave(x.PatientMedicine, true)),
                .. PreviousMedicines.Select(x => ToMedicineSave(x.PatientMedicine, false))
            ],

            Scores=
            [
                .. ScoreEditor.Items.Select(x => new PatientScoreSaveModel
                {
                    Id = x.Id,
                    Description = x.Description,
                    Number = x.Number,
                    RecordedAt = x.RecordedAt
                })
            ],
            DeletedScoreIds= [.. ScoreEditor.DeletedIds],

            Documents=
            [
                .. Documents.Select(x => new PatientDocumentSaveModel
                {
                    Id = x.Id,
                    DocumentType = x.DocumentType,
                    Title = x.Title,
                    Description = x.Description,
                    FileName = x.FileName,
                    StoredPath = x.StoredPath,
                    ContentType = x.ContentType,
                    UploadedAt = x.UploadedAt
                })
            ],

            DeletedPatientMkb10CodeIds=_deletedPatientMkb10CodeIds,
            DeletedMedicineIds=_deletedMedicineIds,
            DeletedDocumentIds=_deletedDocumentIds
        };

        try
        {
            IsSaving=true;
            await _patientService.SavePatientAsync(saveModel);

            _isNewPatientMode=false; // patient now exists in DB; every future save is an update
            OnPropertyChanged(nameof(IsNewPatient));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(HeaderSubtitle));

            _deletedPatientMkb10CodeIds.Clear();
            _deletedMedicineIds.Clear();
            _deletedDocumentIds.Clear();

            foreach(var medicine in AttachedMedicines)
                medicine.MarkPersisted();
            foreach(var medicine in PreviousMedicines)
                medicine.MarkPersisted();

            _childrenLoaded=false;

            await LoadPatientAsync(Patient.Id); // ScoreEditor.Load() ги чисти и неговите deleted ids

            await _userDialogService.ShowAlertAsync("Успешно", "Пациентот е успешно зачуван.", "OK");

            _selectedItemService.OpenInEditMode=false;

            if(_isModalReturnMode)
            {
                _selectedItemService.SelectedItem=new Patient
                {
                    Id=Patient.Id,
                    FirstName=Patient.FirstName,
                    LastName=Patient.LastName
                };
                await _navigationService.GoToAsync("..");
            }
            else
            {
                _selectedItemService.SelectedItem=null;
                await _navigationService.GoToAsync(AppRoutes.Patients.List);
            }
        }
        catch(InvalidOperationException ex)
        {
            await _userDialogService.ShowAlertAsync("Валидација", ex.Message, "OK");
        }
        catch(DbUpdateConcurrencyException)
        {
            Debug.WriteLine("CONCURRENCY ERROR");

            await _userDialogService.ShowAlertAsync(
                "Конфликт",
                "Податоците се променети или избришани. Освежете и обидете се повторно.",
                "OK");
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex.ToString());

            await _userDialogService.ShowAlertAsync(
                "Грешка",
                "Настана грешка при зачувувањето. Обидете се повторно.",
                "OK");
        }
        finally
        {
            IsSaving=false;
        }
    }

    private static string MaskNationalId(string? nationalId)
    {
        if(string.IsNullOrWhiteSpace(nationalId)) return "—";
        var value=new string(nationalId.Where(char.IsLetterOrDigit).ToArray());
        if(value.Length<=4) return new string('•', value.Length);
        return new string('•', value.Length-4)+value[^4..];
    }

    private PatientMedicineSaveModel ToMedicineSave(PatientMedicineDto m, bool active) => new()
    {
        Id=m.Id,
        MedicineId=m.MedicineId,
        Dosage=m.Dosage,
        ApplicationRegimeId=_applicationRegimes.FirstOrDefault(r => string.Equals(r.Regime, m.ApplicationRegime, StringComparison.OrdinalIgnoreCase))?.Id,
        Quantity=0m,
        IsActive=active
    };

    private void SyncDisplayFromPatient()
    {
        SelectedGenderDisplay=PatientEnumLookups.Gender.ToDisplay(Patient.Gender.ToString());
        SelectedStatusDisplay=PatientEnumLookups.Status.ToDisplay(Patient.Status.ToString());
        IsPatientActive=Patient.Status!=PatientStatus.Inactive;
        InactiveReason=Patient.InactiveReason??string.Empty;
        SelectedRelationDisplay=Patient.EmergencyRelationship;
        SelectedCityDisplay=_cityLookup.ToDisplay(Patient.City);
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        var confirmed = await _userDialogService.ShowConfirmationAsync(
            "Откажи",
            "Дали сте сигурни дека сакате да се вратите назад без да ги зачувате промените?",
            "Да",
            "Не");
        if(!confirmed) return;

        _selectedItemService.SelectedItem=null;
        await _navigationService.GoToAsync($"//{AppRoutes.Dashboard}");
    }

    public static PatientEditDto CreateBlankForRegistration() => new()
    {
        FirstName=string.Empty,
        LastName=string.Empty,
        NationalId=null,
        SzboNumber=string.Empty,
        DoctorId=Guid.Empty,
        BirthDate=DateTime.Today.AddYears(-30),
        Gender=Gender.Male,
        Phone=string.Empty,
        Email=string.Empty,
        Address=string.Empty,
        City=string.Empty,
        PostalCode=string.Empty,
        EmergencyContactName=string.Empty,
        EmergencyContactPhone=string.Empty,
        EmergencyRelationship=string.Empty,
        BloodType=string.Empty,
        Allergies=string.Empty,
        Status=PatientStatus.Active
    };

    // =====================================================
    // DOCTOR SEARCH + ATTACH
    // =====================================================

    private CancellationTokenSource _doctorSearchCts = new();

    [ObservableProperty] private ObservableCollection<DoctorDto> doctorSearchResults = new();
    [ObservableProperty] private string doctorSearchText = string.Empty;
    [ObservableProperty] private bool showDoctorDropdown;
    [ObservableProperty] private string selectedDoctorDisplay = string.Empty;
    public bool CanClearDoctor => IsEditMode&&!string.IsNullOrWhiteSpace(SelectedDoctorDisplay);

    [ObservableProperty]
    private bool useCyrillicDoctorSearch = true;

    partial void OnDoctorSearchTextChanged(string value) => DebounceDoctorSearch(value);
    partial void OnSelectedDoctorDisplayChanged(string value) => OnPropertyChanged(nameof(CanClearDoctor));

    partial void OnIsReadOnlyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanClearDoctor));
        if(ScoreEditor!=null)
            ScoreEditor.IsReadOnly=value;
    }

    private async void DebounceDoctorSearch(string query)
    {
        _doctorSearchCts?.Cancel();
        _doctorSearchCts?.Dispose();
        _doctorSearchCts=new CancellationTokenSource();
        var token = _doctorSearchCts.Token;

        if(string.IsNullOrWhiteSpace(query)||query.Length<2)
        {
            DoctorSearchResults.Clear();
            ShowDoctorDropdown=false;
            return;
        }

        try
        {
            await Task.Delay(400, token);
            if(token.IsCancellationRequested) return;
            await SearchDoctorsAsync(query);
        }
        catch(OperationCanceledException) { }
    }

    [RelayCommand]
    private async Task SearchDoctorsAsync(string query)
    {
        DoctorSearchResults.Clear();
        if(string.IsNullOrWhiteSpace(query))
        {
            ShowDoctorDropdown=false;
            return;
        }

        try
        {
            var matches = await _patientService.SearchDoctorsAsync(query);
            DoctorSearchResults=new ObservableCollection<DoctorDto>(matches);
            ShowDoctorDropdown=DoctorSearchResults.Count>0;
        }
        catch(OperationCanceledException) { }
    }

    [RelayCommand]
    private void ClearDoctor()
    {
        if(!IsEditMode||!CanEditPatient) return;

        Patient.DoctorId=Guid.Empty;
        SelectedDoctorDisplay=string.Empty;
        DoctorSearchText=string.Empty;
        DoctorSearchResults.Clear();
        ShowDoctorDropdown=false;
    }

    [RelayCommand]
    private void SelectDoctor(DoctorDto doctor)
    {
        if(doctor==null||!CanEditPatient) return;

        Patient.DoctorId=doctor.Id;
        SelectedDoctorDisplay=doctor.DisplayName;
        DoctorSearchText=string.Empty;
        DoctorSearchResults.Clear();
        ShowDoctorDropdown=false;
    }

    // =====================================================
    // MKB10 MKB-10 ASSIGNMENT SEARCH + ATTACH (A-Z секции)
    // =====================================================

    private CancellationTokenSource _mkbSearchCts = new();
    private int _mkbSearchVersion;

    [ObservableProperty] private ObservableCollection<PatientMkb10CodeDto> patientMkb10Codes = new();
    [ObservableProperty] private ObservableCollection<Mkb10CodeDto> mkbResults = new();
    [ObservableProperty] private string mkbCodeSearchText = string.Empty;
    [ObservableProperty] private string mkbDescriptionSearchText = string.Empty;
    [ObservableProperty] private bool showMkbDropdown;
    [ObservableProperty] private string selectedMkb10Display = string.Empty;

    public ObservableCollection<MkbAlphabetSection> MkbAlphabetSections { get; } = new();

    [ObservableProperty] private string selectedMkbSection = string.Empty;

    partial void OnMkbCodeSearchTextChanged(string value) => _ = DebounceSearchMkb10Async();
    partial void OnMkbDescriptionSearchTextChanged(string value) => _ = DebounceSearchMkb10Async();

    private async Task DebounceSearchMkb10Async()
    {
        var version = ++_mkbSearchVersion;
        var previous = _mkbSearchCts;
        var current = new CancellationTokenSource();
        _mkbSearchCts = current;
        previous.Cancel();
        previous.Dispose();
        var token = current.Token;

        if(string.IsNullOrWhiteSpace(MkbCodeSearchText) &&
           string.IsNullOrWhiteSpace(MkbDescriptionSearchText))
        {
            MkbResults.Clear();
            ShowMkbDropdown = false;
            return;
        }

        try
        {
            await Task.Delay(180, token);
            if(token.IsCancellationRequested || version != _mkbSearchVersion)
                return;

            await SearchMkbAsync(token);
        }
        catch(OperationCanceledException)
        {
            // Expected when the user continues typing.
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"MKB search failed: {ex}");
            if(version == _mkbSearchVersion)
            {
                MkbResults.Clear();
                ShowMkbDropdown = false;
            }
        }
    }

    [RelayCommand]
    private async Task SelectMkbSectionAsync(MkbAlphabetSection section)
    {
        if(section==null)
            return;

        _mkbSearchCts.Cancel();
        SelectedMkbSection=section.Letter;
        foreach(var item in MkbAlphabetSections)
            item.IsSelected=item.Letter==SelectedMkbSection;

        await SearchMkbAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task SearchMkbAsync(CancellationToken token = default)
    {
        var codeTerm = MkbCodeSearchText?.Trim() ?? string.Empty;
        var descriptionTerm = MacedonianTransliterator.ToCyrillic(MkbDescriptionSearchText?.Trim() ?? string.Empty);
        var hasTextSearch = !string.IsNullOrWhiteSpace(codeTerm)
            || !string.IsNullOrWhiteSpace(descriptionTerm);

        // A-Z is a browse filter only. When the user types a code or description,
        // ignore the previously selected letter (e.g. searching M05 while A was selected).
        var sectionFilter = hasTextSearch ? null : SelectedMkbSection;
        if(!hasTextSearch && string.IsNullOrWhiteSpace(sectionFilter))
        {
            MkbResults.Clear();
            ShowMkbDropdown=false;
            return;
        }

        try
        {
            var results = await _patientService.SearchMkb10CodesAsync(
                codeTerm,
                token,
                sectionFilter,
                descriptionTerm);

            if(token.IsCancellationRequested) return;

            MkbResults=new ObservableCollection<Mkb10CodeDto>(results);
            ShowMkbDropdown=results.Count>0;
        }
        catch(OperationCanceledException) { }
    }

    [RelayCommand]
    private void AddMkb(Mkb10CodeDto code)
    {
        if(code==null||!CanEditPatient) return;
        if(PatientMkb10Codes.Any(x => x.Mkb10CodeId==code.Id)) return;

        var assignment = new PatientMkb10CodeDto
        {
            Id=Guid.Empty, // Empty -> SaveAsync treats it as INSERT
            PatientId=Patient.Id,
            EncounterId=null,
            Mkb10CodeId=code.Id,
            Mkb10Code=code.Code,
            Mkb10Description=code.Description,
            Severity="Не е дефиниран",
            DiagnosedAt=DateTime.UtcNow,
            IsPrimary=PatientMkb10Codes.Count==0,
            Status=PatientMkb10AssignmentStatus.Active
        };

        PatientMkb10Codes.Add(assignment);

        MkbCodeSearchText=string.Empty;
        MkbDescriptionSearchText=string.Empty;
        MkbResults.Clear();
        ShowMkbDropdown=false;
    }

    [RelayCommand]
    private void RemoveMkb(PatientMkb10CodeDto assignment)
    {
        if(assignment==null||!CanEditPatient) return;
        if(assignment.Id!=Guid.Empty) _deletedPatientMkb10CodeIds.Add(assignment.Id);
        PatientMkb10Codes.Remove(assignment);
    }

    // =====================================================
    // ТЕКОВНИ ТЕРАПИИ (status = активен)
    // =====================================================

    private CancellationTokenSource _medicineSearchCts = new();

    [ObservableProperty] private ObservableCollection<string> applicationRegimeOptions = new();
    private List<ApplicationRegimeDto> _applicationRegimes = [];
    [ObservableProperty] private string newApplicationRegimeText = string.Empty;

    [ObservableProperty] private ObservableCollection<AttachedMedicineRow> attachedMedicines = new();
    [ObservableProperty] private ObservableCollection<PreviousMedicineRow> previousMedicines = new();
    [ObservableProperty] private ObservableCollection<MedicineDto> medicineResults = new();
    [ObservableProperty] private string medicineSearchText = string.Empty;
    [ObservableProperty] private bool showMedicineDropdown;

    partial void OnMedicineSearchTextChanged(string value) => DebounceSearchMedicine(value);

    private async void DebounceSearchMedicine(string query)
    {
        _medicineSearchCts?.Cancel();
        _medicineSearchCts?.Dispose();
        _medicineSearchCts=new CancellationTokenSource();
        var token = _medicineSearchCts.Token;

        if(string.IsNullOrWhiteSpace(query))
        {
            MedicineResults.Clear();
            ShowMedicineDropdown=false;
            return;
        }

        try
        {
            await Task.Delay(400, token);
            if(token.IsCancellationRequested) return;
            await SearchMedicinesAsync(query, token);
        }
        catch(OperationCanceledException) { }
    }

    private async Task LoadApplicationRegimesAsync()
    {
        try
        {
            var regimes = await _patientService.GetApplicationRegimesAsync();
            _applicationRegimes=regimes;
            ApplicationRegimeOptions=new ObservableCollection<string>(
                regimes.Select(x => x.Regime));
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task OpenApplicationRegimesAsync()
        => await _navigationService.GoToAsync(AppRoutes.ApplicationRegimes.List);

    [RelayCommand]
    private async Task AddApplicationRegimeAsync()
    {
        var value = await _userDialogService.ShowPromptAsync(
            "Нов режим на апликација",
            "Внесете нов режим на апликација за лекот.",
            "Додај",
            "Откажи",
            "Пример: Поткожно");

        if(string.IsNullOrWhiteSpace(value))
            return;

        try
        {
            var regime = await _patientService.AddApplicationRegimeAsync(value.Trim());
            await LoadApplicationRegimesAsync();

            await _userDialogService.ShowAlertAsync(
                "Успешно",
                $"Режимот „{regime.Regime}“ е додаден и достапен во изборот.",
                "ОК");
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", $"Режимот не може да се зачува: {ex.Message}", "ОК");
        }
    }

    private async Task EnsureApplicationRegimeExistsAsync()
    {
        if(ApplicationRegimeOptions.Count>0)
            return;

        await AddApplicationRegimeAsync();
    }

    [RelayCommand]
    private async Task SearchMedicinesAsync(string query, CancellationToken token = default)
    {
        if(string.IsNullOrWhiteSpace(query))
        {
            MedicineResults.Clear();
            ShowMedicineDropdown=false;
            return;
        }

        try
        {
            var results = await _patientService.SearchMedicinesAsync(query);
            if(token.IsCancellationRequested) return;

            MedicineResults=new ObservableCollection<MedicineDto>(results);
            ShowMedicineDropdown=results.Count>0;
        }
        catch(OperationCanceledException) { }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", $"Пребарувањето на лекови не успеа: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void AddMedicine(MedicineDto medicine)
    {
        if(medicine==null||!IsEditMode||!CanSavePatient) return;

        if(AttachedMedicines.Any(r => r.PatientMedicine.MedicineId==medicine.Id&&r.PatientMedicine.IsActive))
            return;

        var patientMedicine = new PatientMedicineDto
        {
            Id=Guid.Empty,
            PatientId=Patient.Id,
            MedicineId=medicine.Id,
            MedicineName=medicine.Name,
            Dosage=medicine.DefaultDosage,
            ApplicationRegimeId=null,
            ApplicationRegime=string.Empty,
            Quantity=0,
            IsActive=true
        };

        var row = new AttachedMedicineRow(patientMedicine);
        row.MarkNew();
        AttachedMedicines.Add(row);

        OnPropertyChanged(nameof(CanSavePatientForm));

        if(ApplicationRegimeOptions.Count==0)
            _=EnsureApplicationRegimeExistsAsync();

        MedicineSearchText=string.Empty;
        MedicineResults.Clear();
        ShowMedicineDropdown=false;
    }

    [RelayCommand]
    private void RemoveMedicine(AttachedMedicineRow row)
    {
        if(row==null||!CanEditPatient||!row.CanDelete) return;

        AttachedMedicines.Remove(row);
    }

    private static string MedicineResolutionKey(PatientMedicineDto medicine)
        => $"Решение за лек|{medicine.MedicineId}|{medicine.MedicineName}";

    private PatientDocumentDto? FindMedicineResolution(PatientMedicineDto medicine)
        => Documents.FirstOrDefault(d =>
            d.DocumentType == PatientDocumentType.Resenie &&
            string.Equals(d.Description, MedicineResolutionKey(medicine), StringComparison.Ordinal));

    [RelayCommand]
    private async Task PreviewMedicineResolutionAsync(AttachedMedicineRow row)
    {
        if(row is null) return;
        var document = FindMedicineResolution(row.PatientMedicine);
        if(document is null)
        {
            await _userDialogService.ShowAlertAsync("Решение за лек", "За овој лек сè уште нема прикачено решение.", "Во ред");
            return;
        }
        await PreviewDocument(document);
    }

    [RelayCommand]
    private async Task UploadMedicineResolutionAsync(AttachedMedicineRow row)
    {
        if(row is null || !CanEditPatient || !IsEditMode) return;
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = $"Изберете решение за {row.MedicineName}" });
            if(file is null) return;

            var patientFolder = Path.Combine(FileSystem.AppDataDirectory, "patient-documents", Patient.Id.ToString());
            Directory.CreateDirectory(patientFolder);
            var storedPath = Path.Combine(patientFolder, $"{Guid.NewGuid()}_{file.FileName}");
            await using(var source = await file.OpenReadAsync())
            await using(var destination = File.Create(storedPath))
                await source.CopyToAsync(destination);

            var key = MedicineResolutionKey(row.PatientMedicine);
            var existing = FindMedicineResolution(row.PatientMedicine);
            if(existing is not null) Documents.Remove(existing);

            Documents.Add(new PatientDocumentDto
            {
                Id = Guid.Empty,
                PatientId = Patient.Id,
                DocumentType = PatientDocumentType.Resenie,
                Title = $"Решение за лек: {row.MedicineName}",
                Description = key,
                FileName = file.FileName,
                StoredPath = storedPath,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            });
            OnPropertyChanged(nameof(CanSavePatientForm));
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", $"Прикачувањето на решението не успеа: {ex.Message}", "Во ред");
        }
    }

    [RelayCommand]
    private async Task MoveMedicineToInactiveAsync(AttachedMedicineRow row)
    {
        if(row is null || !CanEditPatient || !IsEditMode)
            return;

        if(FindMedicineResolution(row.PatientMedicine) is null)
        {
            await _userDialogService.ShowAlertAsync(
                "Недостасува решение",
                $"Прикачете решение за лекот „{row.MedicineName}“ пред да го означите како неактивен.",
                "Во ред");
            return;
        }

        var confirmed = await _userDialogService.ShowConfirmationAsync(
            "Преместување на неактивен лек",
            $"Лекот „{row.MedicineName}“ ќе биде означен како неактивен. Дали сакате да продолжите?",
            "Продолжи",
            "Откажи");
        if(!confirmed)
            return;
        var medicine = row.PatientMedicine;
        medicine.IsActive = false;
        AttachedMedicines.Remove(row);
        var previous = new PreviousMedicineRow(medicine);
        if(row.CanDelete) previous.MarkNew(); else previous.MarkPersisted();
        PreviousMedicines.Insert(0, previous);
        OnPropertyChanged(nameof(CanSavePatientForm));
    }

    // =====================================================
    // ПРЕТХОДНИ ЛЕКОВИ (рачно, status = неактивен)
    // =====================================================

    [RelayCommand]
    private void RemovePreviousMedicine(PreviousMedicineRow row)
    {
        if(row==null||!CanEditPatient||!row.CanDelete) return;
        PreviousMedicines.Remove(row);

        OnPropertyChanged(nameof(CanSavePatientForm));
    }

    // =====================================================
    // DOCUMENT UPLOAD / PREVIEW / DOWNLOAD
    // =====================================================

    [ObservableProperty] private ObservableCollection<PatientDocumentDto> documents = new();
    [ObservableProperty] private bool isUploadingDocument;
    [ObservableProperty] private bool isDownloadingDocument;
    [ObservableProperty] private PatientDocumentDto? selectedDocumentPreview;

    public bool HasDocumentPreview => SelectedDocumentPreview is not null;

    partial void OnSelectedDocumentPreviewChanged(PatientDocumentDto? value)
        => OnPropertyChanged(nameof(HasDocumentPreview));

[RelayCommand]
    private async Task UploadDocumentAsync()
    {
        if(!CanEditPatient||IsUploadingDocument) return;

        try
        {
            IsUploadingDocument=true;

            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle="Изберете документ" });
            if(file==null) return;

            var patientFolder = Path.Combine(FileSystem.AppDataDirectory, "patient-documents", Patient.Id.ToString());
            Directory.CreateDirectory(patientFolder);

            var storedFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var storedPath = Path.Combine(patientFolder, storedFileName);

            await using(var source = await file.OpenReadAsync())
            await using(var dest = File.Create(storedPath))
            {
                await source.CopyToAsync(dest);
            }

            var newDoc = new PatientDocumentDto
            {
                Id=Guid.Empty,
                PatientId=Patient.Id,
                DocumentType=IsPatientInactive ? PatientDocumentType.Resenie : PatientDocumentType.Other,
                Title=IsPatientInactive ? "Решение за неактивност" : file.FileName,
                Description=IsPatientInactive ? InactiveReason : string.Empty,
                FileName=file.FileName,
                StoredPath=storedPath,
                ContentType=file.ContentType,
                UploadedAt=DateTime.UtcNow
            };

            Documents.Add(newDoc);
            SelectedDocumentPreview=newDoc;
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", $"Прикачувањето не успеа: {ex.Message}", "OK");
        }
        finally
        {
            IsUploadingDocument=false;
        }
    }

    [RelayCommand]
    private void RemoveDocument(PatientDocumentDto document)
    {
        if(document==null||!CanEditPatient) return;
        if(document.Id!=Guid.Empty)
        {
            _deletedDocumentIds.Add(document.Id);
}
        if(SelectedDocumentPreview==document) SelectedDocumentPreview=null;
        Documents.Remove(document);
    }

    [RelayCommand]
    private async Task PreviewDocument(PatientDocumentDto? document)
    {
        if(document is null||string.IsNullOrWhiteSpace(document.StoredPath)) return;

        SelectedDocumentPreview=document;

        if(!File.Exists(document.StoredPath))
        {
            await _userDialogService.ShowAlertAsync("Документ", "Документот не е пронајден на дискот.", "Во ред");
            return;
        }

        // За не-слики (PDF, DOCX...) нема inline преглед — се отвора со стандардна апликација.
        if(!IsImageDocument(document))
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest(
                document.FileName,
                new ReadOnlyFile(document.StoredPath)));
        }
    }

    [RelayCommand]
    private void ClosePreview() => SelectedDocumentPreview=null;

    [RelayCommand]
    private async Task DownloadDocumentAsync(PatientDocumentDto? document)
    {
        document??=SelectedDocumentPreview;

        if(document is null||string.IsNullOrWhiteSpace(document.StoredPath)||!File.Exists(document.StoredPath))
        {
            await _userDialogService.ShowAlertAsync("Документ", "Документот не е пронајден на дискот.", "Во ред");
            return;
        }

        if(IsDownloadingDocument) return;

        try
        {
            IsDownloadingDocument=true;

            await using var stream = File.OpenRead(document.StoredPath);
            var result = await FileSaver.Default.SaveAsync(document.FileName, stream, CancellationToken.None);

            if(!result.IsSuccessful&&result.Exception is not null)
                await _userDialogService.ShowAlertAsync("Грешка", $"Преземањето не успеа: {result.Exception.Message}", "OK");
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", $"Преземањето не успеа: {ex.Message}", "OK");
        }
        finally
        {
            IsDownloadingDocument=false;
        }
    }

    public static bool IsImageDocument(PatientDocumentDto document)
    {
        if(!string.IsNullOrWhiteSpace(document.ContentType))
            return document.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        var ext = Path.GetExtension(document.FileName)?.ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".bmp";
    }

    public void Dispose()
    {
        _doctorSearchCts.Cancel();
        _doctorSearchCts.Dispose();
        _mkbSearchCts.Cancel();
        _mkbSearchCts.Dispose();
        _medicineSearchCts.Cancel();
        _medicineSearchCts.Dispose();
        ScoreEditor.Dispose();
        GC.SuppressFinalize(this);
    }
}

public partial class PreviousMedicineRow : ObservableObject
{
    public PreviousMedicineRow(PatientMedicineDto medicine) => PatientMedicine=medicine;

    public bool CanDelete { get; private set; } = false;

    public void MarkNew()
    {
        CanDelete=true;
        OnPropertyChanged(nameof(CanDelete));
    }

    public void MarkPersisted()
    {
        CanDelete=false;
        OnPropertyChanged(nameof(CanDelete));
    }

    public PatientMedicineDto PatientMedicine
    {
        get;
    }
    public string MedicineName => PatientMedicine.MedicineName;
    public string MedicineNameBilingual => EHMR.Helpers.MacedonianTransliterator.ToBilingual(MedicineName);
    public string Dosage => PatientMedicine.Dosage;
}

public partial class MkbAlphabetSection : ObservableObject
{
    public MkbAlphabetSection(string letter, bool isSelected)
    {
        Letter=letter;
        IsSelected=isSelected;
    }

    public string Letter
    {
        get;
    }

    [ObservableProperty]
    private bool isSelected;
}