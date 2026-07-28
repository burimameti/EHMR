using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Infrastructure.Persistence;
using EHMR.Services.Dto;
using EHMR.ViewModels.Patients;
using EHMR.ViewModels.Patients.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

    private PatientEditDto? _originalPatient;
    private bool _isNewPatientMode;
    private bool _isModalReturnMode;

    private readonly List<Guid> _deletedDiagnosisIds = [];
    private readonly List<Guid> _deletedMedicineIds = [];
    private readonly List<Guid> _deletedDocumentIds = [];

    private bool _childrenLoaded;
    public bool IsNewPatient => _isNewPatientMode;

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

    public ObservableCollection<string> BloodTypeOptions
    {
        get;
    } =
        new() { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

    public ObservableCollection<string> GenderOptions { get; } = PatientEnumLookups.Gender.ToObservableCollection();
    public ObservableCollection<string> StatusOptions { get; } = PatientEnumLookups.Status.ToObservableCollection();
    public ObservableCollection<string> CityOptions { get; } = new(PatientFilterLookups.BuildCityLookup().DisplayValues);

    public ObservableCollection<string> RelationOptions
    {
        get;
    } = new()
    {
        "Сопруг / Сопруга", "Родител", "Дете", "Брат / Сестра", "Пријател", "Друго"
    };

    [ObservableProperty] private string selectedGenderDisplay = string.Empty;
    [ObservableProperty] private string selectedStatusDisplay = string.Empty;
    [ObservableProperty] private string selectedRelationDisplay = string.Empty;
    [ObservableProperty] private string selectedCityDisplay = string.Empty;
    [ObservableProperty] private string selectedBloodTypeDisplay = string.Empty;

    partial void OnSelectedRelationDisplayChanged(string value) => Patient.EmergencyRelationship=value??string.Empty;
    partial void OnSelectedCityDisplayChanged(string value) => Patient.City=value??string.Empty;
    partial void OnSelectedBloodTypeDisplayChanged(string value) => Patient.BloodType=value??string.Empty;

    public PatientDetailFormViewModel(
        IPatientService patientService,
        ISelectedItemService<Patient> selectedItemService,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ILicenseService licenseService)
    {
        _licenseService=licenseService;
        _patientService=patientService;
        _selectedItemService=selectedItemService;
        _navigationService=navigationService;
        _userDialogService=userDialogService;
        _dbFactory=dbFactory;
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
            IsReadOnly=false;

            SyncDisplayFromPatient();

            OnPropertyChanged(nameof(IsNewPatient));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(HeaderSubtitle));
            return;
        }

        IsReadOnly=!_selectedItemService.OpenInEditMode;

        // Everything (edit fields + children) now comes from one DTO fetch —
        // no more manual entity cloning needed.
        _=LoadPatientAsync(selectedPatient.Id);

        OnPropertyChanged(nameof(IsNewPatient));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
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

            SyncDisplayFromPatient();

            // DTOs aren't EF-tracked, so no cloning/detaching gymnastics required —
            // just wrap what the service gave us.
            Diagnoses=new ObservableCollection<DiagnosisDto>(full.Diagnoses);
            AttachedMedicines=new ObservableCollection<AttachedMedicineRow>(
                full.Medicines.Select(m => new AttachedMedicineRow(m)));
            Documents=new ObservableCollection<PatientDocumentDto>(full.Documents);

            SelectedDoctorDisplay=full.DoctorDisplay;
            DoctorSearchText=SelectedDoctorDisplay;

            await LoadHistoryAsync(patientId);

            _childrenLoaded=true;
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    // ═══════════════════════════════════════════ ИСТОРИЈА НА ПАЦИЕНТОТ ═══════════════════════════════════════════
    //
    // Шесте картици со историја беа врзани за колекции што не постоеја во овој
    // ViewModel, па сите шест секогаш стоеја празни со порака „нема евидентирани…"
    // без разлика колку записи има пациентот.
    //
    // Прикажувањето е само за читање, па се вчитуваат директно како ентитети —
    // PatientDto носи само дијагнози, лекови и документи, не и прегледи,
    // термини, циклуси и рецепти.

    [ObservableProperty] private ObservableCollection<Diagnosis> diagnosisHistory = new();
    [ObservableProperty] private ObservableCollection<Encounter> encounterHistory = new();
    [ObservableProperty] private ObservableCollection<Appointment> appointmentHistory = new();
    [ObservableProperty] private ObservableCollection<TherapyCycle> therapyCycleHistory = new();
    [ObservableProperty] private ObservableCollection<Prescription> prescriptionHistory = new();
    [ObservableProperty] private ObservableCollection<PatientMedicine> medicineHistory = new();

    private async Task LoadHistoryAsync(Guid patientId)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            DiagnosisHistory=new ObservableCollection<Diagnosis>(
                await db.Diagnoses
                    .AsNoTracking()
                    .Where(x => x.PatientId==patientId)
                    .ToListAsync());

            EncounterHistory=new ObservableCollection<Encounter>(
                await db.Encounters
                    .AsNoTracking()
                    .Where(x => x.PatientId==patientId)
                    .OrderByDescending(x => x.EncounterDate)
                    .ToListAsync());

            AppointmentHistory=new ObservableCollection<Appointment>(
                await db.Appointments
                    .AsNoTracking()
                    .Where(x => x.PatientId==patientId)
                    .OrderByDescending(x => x.ScheduledStart)
                    .ToListAsync());

            TherapyCycleHistory=new ObservableCollection<TherapyCycle>(
                await db.TherapyCycles
                    .AsNoTracking()
                    .Where(x => x.PatientId==patientId)
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync());

            PrescriptionHistory=new ObservableCollection<Prescription>(
                await db.Prescriptions
                    .AsNoTracking()
                    .Where(x => x.PatientId==patientId)
                    .ToListAsync());

            // Редот на картичката покажува Medicine.Name — без Include останува празен.
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
        NationalId=PrivacyMaskHelper.MaskNationalId(source.NationalId),
        BirthDate=source.BirthDate,
        Gender=source.Gender,
        DoctorId=source.DoctorId,
        Phone=source.Phone,
        Email=source.Email,
        Address=source.Address,
        City=source.City,
        BloodType=source.BloodType,
        Allergies=source.Allergies,
        Status=source.Status
        // NOTE: PatientDto has no PostalCode / EmergencyContact* fields.
        // If GetByIdAsync doesn't surface those either, edits will silently
        // drop them on save — see note at the end of my reply.
    };

    private static PatientEditDto CloneEditDto(PatientEditDto p) => new()
    {
        Id=p.Id,
        FirstName=p.FirstName,
        LastName=p.LastName,
        NationalId=p.NationalId,
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
        BloodType=p.BloodType,
        Allergies=p.Allergies,
        Status=p.Status
    };

    // ------------------------------------------------------------------ //
    // Commands
    // ------------------------------------------------------------------ //

    [RelayCommand]
    private void ToggleEditMode() => IsReadOnly=!IsReadOnly;

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

        if(string.IsNullOrWhiteSpace(Patient.FirstName)||string.IsNullOrWhiteSpace(Patient.LastName))
        {
            await _userDialogService.ShowAlertAsync("Валидација", "Името и презимето се задолжителни.", "OK");
            return;
        }

        if(string.IsNullOrWhiteSpace(Patient.NationalId)||Patient.NationalId.Length!=13)
        {
            await _userDialogService.ShowAlertAsync("Валидација", "ЕМБГ мора да содржи точно 13 цифри.", "OK");
            return;
        }

        if(Patient.DoctorId==Guid.Empty)
        {
            await _userDialogService.ShowAlertAsync("Валидација", "Реуматолог не е доделен.", "OK");
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

        var saveModel = new PatientSaveModel
        {
            Patient=Patient,
            IsNewPatient=_isNewPatientMode,

            Diagnoses=
            [
                .. Diagnoses.Select(x => new DiagnosisSaveModel
            {
                Id = x.Id,
                EncounterId = x.EncounterId,
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
                .. AttachedMedicines.Select(x => new PatientMedicineSaveModel
            {
                Id = x.PatientMedicine.Id,
                MedicineId = x.PatientMedicine.MedicineId,
                Dosage = x.PatientMedicine.Dosage,
                DosesFrequency = x.PatientMedicine.DosesFrequency,
                StartDate = x.PatientMedicine.StartDate,
                EndDate = x.PatientMedicine.EndDate,
                Notes = x.PatientMedicine.Notes,
                IsActive = x.PatientMedicine.IsActive
            })
            ],

            Documents=
            [
                .. Documents.Select(x => new PatientDocumentSaveModel
            {
                Id = x.Id,
                FileName = x.FileName,
                StoredPath = x.StoredPath,
                ContentType = x.ContentType,
                UploadedAt = x.UploadedAt
            })
            ],

            DeletedDiagnosisIds=_deletedDiagnosisIds,
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

            _deletedDiagnosisIds.Clear();
            _deletedMedicineIds.Clear();
            _deletedDocumentIds.Clear();
            _childrenLoaded=false;

            await LoadPatientAsync(Patient.Id);

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
            // Catches anything else — e.g. a failure in LoadPatientAsync right after
            // a successful save, so the user always gets feedback instead of a
            // silent unhandled exception.
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

    private void SyncDisplayFromPatient()
    {
        SelectedGenderDisplay=PatientEnumLookups.Gender.ToDisplay(Patient.Gender.ToString());
        SelectedStatusDisplay=PatientEnumLookups.Status.ToDisplay(Patient.Status.ToString());
        SelectedRelationDisplay=Patient.EmergencyRelationship;
        SelectedCityDisplay=Patient.City;
        SelectedBloodTypeDisplay=Patient.BloodType;
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if(_isModalReturnMode)
        {
            _selectedItemService.SelectedItem=null;
            await _navigationService.GoToAsync(AppRoutes.Patients.List);
            return;
        }

        if(_isNewPatientMode)
        {
            await _navigationService.GoToAsync(AppRoutes.Patients.List);
            return;
        }

        if(_originalPatient!=null)
        {
            Patient=CloneEditDto(_originalPatient);

            _deletedDiagnosisIds.Clear();
            _deletedMedicineIds.Clear();
            _deletedDocumentIds.Clear();

            _childrenLoaded=false;
            _=LoadPatientAsync(Patient.Id); // pull real DB state back in
        }

        IsReadOnly=true;
    }

    public static PatientEditDto CreateBlankForRegistration() => new()
    {
        FirstName=string.Empty,
        LastName=string.Empty,
        NationalId=string.Empty,
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

    partial void OnDoctorSearchTextChanged(string value) => DebounceDoctorSearch(value);

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
    private void SelectDoctor(DoctorDto doctor)
    {
        if(doctor==null) return;

        Patient.DoctorId=doctor.Id;
        SelectedDoctorDisplay=doctor.DisplayName;
        DoctorSearchText=string.Empty;
        DoctorSearchResults.Clear();
        ShowDoctorDropdown=false;
    }

    // =====================================================
    // MKB10 DIAGNOSIS SEARCH + ATTACH
    // =====================================================

    private CancellationTokenSource _mkbSearchCts = new();

    [ObservableProperty] private ObservableCollection<DiagnosisDto> diagnoses = new();
    [ObservableProperty] private ObservableCollection<Mkb10CodeDto> mkbResults = new();
    [ObservableProperty] private string mkbSearchText = string.Empty;
    [ObservableProperty] private bool showMkbDropdown;
    [ObservableProperty] private string selectedMkb10Display = string.Empty;

    partial void OnMkbSearchTextChanged(string value) => DebounceSearchMkb10(value);

    private async void DebounceSearchMkb10(string query)
    {
        _mkbSearchCts?.Cancel();
        _mkbSearchCts?.Dispose();
        _mkbSearchCts=new CancellationTokenSource();
        var token = _mkbSearchCts.Token;

        if(string.IsNullOrWhiteSpace(query))
        {
            MkbResults.Clear();
            ShowMkbDropdown=false;
            return;
        }

        try
        {
            await Task.Delay(400, token);
            if(token.IsCancellationRequested) return;
            await SearchMkbAsync(query, token);
        }
        catch(OperationCanceledException) { }
    }

    [RelayCommand]
    private async Task SearchMkbAsync(string query, CancellationToken token = default)
    {
        if(string.IsNullOrWhiteSpace(query))
        {
            MkbResults.Clear();
            ShowMkbDropdown=false;
            return;
        }

        try
        {
            var results = await _patientService.SearchMkb10CodesAsync(query);
            if(token.IsCancellationRequested) return;

            MkbResults=new ObservableCollection<Mkb10CodeDto>(results);
            ShowMkbDropdown=results.Count>0;
        }
        catch(OperationCanceledException) { }
    }

    [RelayCommand]
    private void AddMkb(Mkb10CodeDto code)
    {
        if(code==null) return;
        if(Diagnoses.Any(x => x.Mkb10CodeId==code.Id)) return;

        var diagnosis = new DiagnosisDto
        {
            Id=Guid.Empty, // Empty -> SaveAsync treats it as INSERT
            PatientId=Patient.Id,
            Mkb10CodeId=code.Id,
            Mkb10Code=code.Code,
            Severity="Не е дефиниран",
            DiagnosedAt=DateTime.UtcNow,
            IsPrimary=Diagnoses.Count==0,
            Status=DiagnosisStatus.Active
        };

        Diagnoses.Add(diagnosis);

        MkbSearchText=string.Empty;
        MkbResults.Clear();
        ShowMkbDropdown=false;
    }

    [RelayCommand]
    private void RemoveMkb(DiagnosisDto diagnosis)
    {
        if(diagnosis==null) return;
        if(diagnosis.Id!=Guid.Empty) _deletedDiagnosisIds.Add(diagnosis.Id);
        Diagnoses.Remove(diagnosis);
    }

    // =====================================================
    // MEDICINE SEARCH + ATTACH
    // =====================================================

    private CancellationTokenSource _medicineSearchCts = new();

    [ObservableProperty] private ObservableCollection<AttachedMedicineRow> attachedMedicines = new();
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
        if(medicine==null) return;

        if(AttachedMedicines.Any(r => r.PatientMedicine.MedicineId==medicine.Id&&r.PatientMedicine.IsActive))
            return;

        var patientMedicine = new PatientMedicineDto
        {
            Id=Guid.Empty,
            PatientId=Patient.Id,
            MedicineId=medicine.Id,
            MedicineName=medicine.Name,
            Dosage=medicine.DefaultDosage,
            DosesFrequency=DosesFrequency.Other,
            StartDate=DateTime.UtcNow,
            IsActive=true
        };

        AttachedMedicines.Add(new AttachedMedicineRow(patientMedicine));

        MedicineSearchText=string.Empty;
        MedicineResults.Clear();
        ShowMedicineDropdown=false;
    }

    [RelayCommand]
    private void RemoveMedicine(AttachedMedicineRow row)
    {
        if(row==null) return;
        if(row.PatientMedicine.Id!=Guid.Empty) _deletedMedicineIds.Add(row.PatientMedicine.Id);
        AttachedMedicines.Remove(row);
    }

    // =====================================================
    // DOCUMENT UPLOAD
    // =====================================================

    [ObservableProperty] private ObservableCollection<PatientDocumentDto> documents = new();
    [ObservableProperty] private bool isUploadingDocument;

    [RelayCommand]
    private async Task UploadDocumentAsync()
    {
        if(IsUploadingDocument) return;

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

            Documents.Add(new PatientDocumentDto
            {
                Id=Guid.Empty,
                PatientId=Patient.Id,
                FileName=file.FileName,
                StoredPath=storedPath,
                ContentType=file.ContentType,
                UploadedAt=DateTime.UtcNow
            });
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
        if(document==null) return;
        if(document.Id!=Guid.Empty) _deletedDocumentIds.Add(document.Id);
        Documents.Remove(document);
    }

    public void Dispose()
    {
        _doctorSearchCts.Cancel();
        _doctorSearchCts.Dispose();
        _mkbSearchCts.Cancel();
        _mkbSearchCts.Dispose();
        _medicineSearchCts.Cancel();
        _medicineSearchCts.Dispose();
        GC.SuppressFinalize(this);
    }
}