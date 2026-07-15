using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.ViewModels.Patients.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;


namespace EHMR.ViewModels;

public partial class PatientDetailFormViewModel : ObservableObject, IDisposable
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Patient> _selectedItemService;
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _userDialogService;

    private Patient? _originalPatient;
    private bool _isNewPatientMode;
    private bool _isModalReturnMode;
    public bool IsNewPatient => _isNewPatientMode;

    // Guards against overlapping "no results -> offer to create" dialogs,
    // same pattern as EncounterBaseViewModel.
    private bool _isOfferingDoctorCreation;

    public string HeaderTitle =>
        _isNewPatientMode
            ? "Нов Пациент"
            : $"Детали за пациент";

    public string HeaderSubtitle =>
        _isNewPatientMode
            ? "Креирање ново пациентско досие"
            : $"";

    [ObservableProperty] private Patient patient = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(InputBgColor))]
    [NotifyPropertyChangedFor(nameof(InputBorderColor))]
    private bool isReadOnly = true;

    [ObservableProperty] private string pageTitle = string.Empty;

    public bool IsEditMode => !IsReadOnly;
    public Color InputBgColor =>
        IsReadOnly
        ? Color.FromArgb("#F8FAFC")
        : Color.FromArgb("#FFFFFF");

    public Color InputBorderColor =>
        IsReadOnly
        ? Color.FromArgb("#CBD5E1")
        : Color.FromArgb("#2563EB");

    public ObservableCollection<string> GenderOptions { get; } = PatientEnumLookups.Gender.ToObservableCollection();
    public ObservableCollection<string> StatusOptions { get; } = PatientEnumLookups.Status.ToObservableCollection();

    public ObservableCollection<string> CityOptions
    {
        get;
    } = new(PatientFilterLookups.BuildCityLookup().DisplayValues); // skip "Сите"

    public ObservableCollection<string> RelationOptions
    {
        get;
    } = new()
    {
        "Сопруг / Сопруга",
        "Родител",
        "Дете",
        "Брат / Сестра",
        "Пријател",
        "Друго"
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTab2Active))]
    [NotifyPropertyChangedFor(nameof(IsTab3Active))]
    [NotifyPropertyChangedFor(nameof(IsTab4Active))]
    private int activeTab = 1;

    public bool IsTab1Active => ActiveTab==1;
    public bool IsTab2Active => ActiveTab==2;
    public bool IsTab3Active => ActiveTab==3;
    public bool IsTab4Active => ActiveTab==4;

    [ObservableProperty] private string selectedGenderDisplay = string.Empty;
    [ObservableProperty] private string selectedStatusDisplay = string.Empty;
    [ObservableProperty] private string selectedRelationDisplay = string.Empty;

    partial void OnActiveTabChanged(int value) =>
        OnPropertyChanged(nameof(IsTab1Active));

    partial void OnSelectedRelationDisplayChanged(string value) =>
        Patient.EmergencyRelationship=value??string.Empty;

    [RelayCommand]
    private void SelectTab(string tab)
    {
        if(int.TryParse(tab, out var t))
            ActiveTab=t;
    }

    public PatientDetailFormViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ISelectedItemService<Patient> selectedItemService,
        INavigationService navigationService,
        IUserDialogService userDialogService)
    {
        _dbFactory=dbFactory;
        _selectedItemService=selectedItemService;
        _navigationService=navigationService;
        _userDialogService=userDialogService;

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
            Patient=CreateBlankForRegistration();
            PageTitle="Нов Пациент";
            IsReadOnly=false;

            SyncDisplayFromPatient();

            OnPropertyChanged(nameof(IsNewPatient));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(HeaderSubtitle));
            return;
        }

        _originalPatient=selectedPatient;
        Patient=selectedPatient.Clone();

        PageTitle=$"Досие: {Patient.FullName}";
        IsReadOnly=!_selectedItemService.OpenInEditMode;

        SyncDisplayFromPatient();
        _=LoadExistingChildrenAsync(Patient.Id);

        OnPropertyChanged(nameof(IsNewPatient));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
    }

    /// <summary>
    /// Patient.Diagnoses / PatientMedicines / Documents come back on Clone() only if
    /// your Clone() deep-copies them; to be safe we re-load them explicitly from the
    /// DB with the same Include shape SaveAsync will need for diffing later.
    /// </summary>
    private async Task LoadExistingChildrenAsync(Guid patientId)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var full = await db.Patients.Include(x=>x.Doctor).ThenInclude(x=>x.User)
                .Include(p => p.Diagnoses).ThenInclude(d => d.Mkb10Code).Include(x=>x.Encounters)
                .Include(p => p.PatientMedicines).ThenInclude(pm => pm.Medicine)
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.Id==patientId);

            if(full==null) return;

            Diagnoses=new ObservableCollection<Diagnosis>(full.Diagnoses);
            AttachedMedicines=new ObservableCollection<AttachedMedicineRow>(
                full.PatientMedicines.Select(pm => new AttachedMedicineRow(pm)));
            Documents=new ObservableCollection<PatientDocument>(full.Documents);
        }
        catch { /* non-critical for read-only view; SaveAsync will still work off local state */ }
    }

    // ------------------------------------------------------------------ //
    // Commands
    // ------------------------------------------------------------------ //

    [RelayCommand]
    private void ToggleEditMode() => IsReadOnly=!IsReadOnly;

    [RelayCommand]
    private async Task SaveAsync()
    {
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
            await _userDialogService.ShowAlertAsync("Валидација", "Реуматолог не е доделен на пациентот полето е задолжително.", "OK");
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if(_isNewPatientMode)
            {
                Patient.Id=Guid.NewGuid();
                Patient.RegistrationDate=DateTime.UtcNow;
                Patient.CreatedAt=DateTime.UtcNow;

                foreach(var diagnosis in Diagnoses)
                {
                    diagnosis.PatientId=Patient.Id;
                    Patient.Diagnoses.Add(diagnosis);
                }
                foreach(var row in AttachedMedicines)
                {
                    row.PatientMedicine.PatientId=Patient.Id;
                    Patient.PatientMedicines.Add(row.PatientMedicine);
                }
                foreach(var doc in Documents)
                {
                    doc.PatientId=Patient.Id;
                    Patient.Documents.Add(doc);
                }

                await db.Patients.AddAsync(Patient);
            }
            else
            {
                var existing = await db.Patients
                    .Include(p => p.Diagnoses)
                    .Include(p => p.PatientMedicines)
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(x => x.Id==Patient.Id);
                if(existing==null) return;

                existing.FirstName=Patient.FirstName;
                existing.LastName=Patient.LastName;
                existing.NationalId=Patient.NationalId;
                existing.DoctorId=Patient.DoctorId;
                existing.BirthDate=Patient.BirthDate;
                existing.Gender=Patient.Gender;
                existing.Phone=Patient.Phone;
                existing.Email=Patient.Email;
                existing.Address=Patient.Address;
                existing.City=Patient.City;
                existing.PostalCode=Patient.PostalCode;
                existing.Status=Patient.Status;
                existing.BloodType=Patient.BloodType;
                existing.Allergies=Patient.Allergies;
                existing.EmergencyContactName=Patient.EmergencyContactName;
                existing.EmergencyContactPhone=Patient.EmergencyContactPhone;
                existing.EmergencyRelationship=Patient.EmergencyRelationship;

                // --- Diagnoses: remove what's gone, add what's new ---
                var keepDiagnosisIds = Diagnoses.Select(d => d.Id).ToHashSet();
                foreach(var toRemove in existing.Diagnoses.Where(d => !keepDiagnosisIds.Contains(d.Id)).ToList())
                    existing.Diagnoses.Remove(toRemove);
                foreach(var d in Diagnoses.Where(d => existing.Diagnoses.All(x => x.Id!=d.Id)))
                {
                    d.PatientId=existing.Id;
                    existing.Diagnoses.Add(d);
                }

                // --- Medicines: remove what's gone, add/update what's kept ---
                var keepMedicineIds = AttachedMedicines.Select(r => r.PatientMedicine.Id).ToHashSet();
                foreach(var toRemove in existing.PatientMedicines.Where(pm => !keepMedicineIds.Contains(pm.Id)).ToList())
                    existing.PatientMedicines.Remove(toRemove);
                foreach(var row in AttachedMedicines)
                {
                    var match = existing.PatientMedicines.FirstOrDefault(pm => pm.Id==row.PatientMedicine.Id);
                    if(match==null)
                    {
                        row.PatientMedicine.PatientId=existing.Id;
                        existing.PatientMedicines.Add(row.PatientMedicine);
                    }
                    else
                    {
                        match.Dosage=row.PatientMedicine.Dosage;
                        match.DosesFrequency=row.PatientMedicine.DosesFrequency;
                        match.EndDate=row.PatientMedicine.EndDate;
                        match.IsActive=row.PatientMedicine.IsActive;
                    }
                }

                // --- Documents: add-only here (deleting an uploaded document is a separate, explicit action) ---
                var keepDocIds = Documents.Select(d => d.Id).ToHashSet();
                foreach(var toRemove in existing.Documents.Where(d => !keepDocIds.Contains(d.Id)).ToList())
                    existing.Documents.Remove(toRemove);
                foreach(var doc in Documents.Where(d => existing.Documents.All(x => x.Id!=d.Id)))
                {
                    doc.PatientId=existing.Id;
                    existing.Documents.Add(doc);
                }

                db.Patients.Update(existing);
            }

            await db.SaveChangesAsync();
            await _userDialogService.ShowAlertAsync("Успешно", "Пациентот е успешно зачуван.", "OK");

            if(_isModalReturnMode)
            {
                _selectedItemService.SelectedItem=Patient;
                await _navigationService.GoToAsync("..");
            }
            else
            {
                _selectedItemService.SelectedItem=null;
                await _navigationService.GoToAsync(AppRoutes.Patients.List);
            }
            _selectedItemService.OpenInEditMode=false;
            _selectedItemService.SelectedItem=null;
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", ex.Message, "OK");
        }
    }

    private void SyncDisplayFromPatient()
    {
        SelectedGenderDisplay=PatientEnumLookups.Gender.ToDisplay(Patient.Gender.ToString());
        SelectedStatusDisplay=PatientEnumLookups.Status.ToDisplay(Patient.Status.ToString());
        SelectedRelationDisplay=Patient.EmergencyRelationship;
        DoctorSearchText=SelectedDoctorDisplay;
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

        if(_originalPatient!=null) Patient=_originalPatient.Clone();
        IsReadOnly=true;
    }

    public static Patient CreateBlankForRegistration() => new()
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
        RegistrationDate=DateTime.UtcNow,
        Status=PatientStatus.Active
    };

    // =====================================================
    // DOCTOR SEARCH + ATTACH  (mirrors EncounterBaseViewModel's Appointment search)
    // =====================================================

    private CancellationTokenSource _doctorSearchCts = new();

    [ObservableProperty] private ObservableCollection<Doctor> doctorSearchResults = new();
    [ObservableProperty] private string doctorSearchText = string.Empty;
    [ObservableProperty] private bool showDoctorDropdown;
    [ObservableProperty] private string selectedDoctorDisplay = string.Empty;

    partial void OnDoctorSearchTextChanging(string value) => _=SearchDoctorsAsync(value);

    [RelayCommand]
    private async Task SearchDoctorsAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query))
        {
            DoctorSearchResults.Clear();
            ShowDoctorDropdown=false;
            return;
        }

        _doctorSearchCts.Cancel();
        _doctorSearchCts.Dispose();
        _doctorSearchCts=new CancellationTokenSource();
        var token = _doctorSearchCts.Token;

        try { await Task.Delay(300, token); }
        catch(TaskCanceledException) { return; }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var matches = await db.Doctors
                .Include(d => d.User)
                .Where(d => d.IsActive&&
                    (d.User.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase)||
                     d.User.LastName.Contains(query, StringComparison.OrdinalIgnoreCase)||
                     d.Specialty.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToListAsync(token);

            if(token.IsCancellationRequested) return;

            DoctorSearchResults=new ObservableCollection<Doctor>(matches);
            ShowDoctorDropdown=matches.Count>0;

            if(matches.Count==0)
                await OfferNoDoctorFoundAsync(query);
        }
        catch(OperationCanceledException) { /* user kept typing */ }
    }

    private async Task OfferNoDoctorFoundAsync(string searchedTerm)
    {
        // Doctors aren't created ad-hoc from the patient form (unlike appointments/cycles
        // in the Encounter view) — a doctor account requires a User + license number, so
        // this just informs rather than offering to create one inline.
        if(_isOfferingDoctorCreation) return;
        _isOfferingDoctorCreation=true;
        try
        {
            await _userDialogService.ShowAlertAsync(
                "Нема резултати",
                $"Не е пронајден лекар за „{searchedTerm}“. Проверете во администрацијата на кориснициte.",
                "OK");
        }
        finally { _isOfferingDoctorCreation=false; }
    }

    [RelayCommand]
    private void SelectDoctor(Doctor doctor)
    {
        if(doctor==null) return;

        Patient.DoctorId=doctor.Id;
        SelectedDoctorDisplay=$"Д-р {doctor.User?.FirstName} {doctor.User?.LastName}";
        DoctorSearchText=string.Empty;
        DoctorSearchResults.Clear();
        ShowDoctorDropdown=false;
    }

    // =====================================================
    // MKB10 DIAGNOSIS SEARCH + ATTACH  (mirrors EncounterBaseViewModel exactly)
    // =====================================================

    private CancellationTokenSource _mkbSearchCts = new();

    [ObservableProperty] private ObservableCollection<Diagnosis> diagnoses = new();
    [ObservableProperty] private ObservableCollection<Mkb10Code> mkbResults = new();
    [ObservableProperty] private string mkbSearchText = string.Empty;
    [ObservableProperty] private bool showMkbDropdown;

    partial void OnMkbSearchTextChanging(string value)
    {
        if(string.IsNullOrWhiteSpace(value)||value.Length<2)
        {
            MkbResults.Clear();
            ShowMkbDropdown=false;
            return;
        }
        _=SearchMkbAsync(value);
    }

    [RelayCommand]
    private async Task SearchMkbAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query)||query.Length<2)
        {
            MkbResults.Clear();
            ShowMkbDropdown=false;
            return;
        }

        _mkbSearchCts.Cancel();
        _mkbSearchCts.Dispose();
        _mkbSearchCts=new CancellationTokenSource();
        var token = _mkbSearchCts.Token;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var matches = await db.Mkb10Codes
                .Where(m => m.Code.Contains(query, StringComparison.OrdinalIgnoreCase)||
                            m.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .ToListAsync(token);

            if(token.IsCancellationRequested) return;

            MkbResults=new ObservableCollection<Mkb10Code>(matches);
            ShowMkbDropdown=matches.Count>0;
        }
        catch(OperationCanceledException) { /* user kept typing */ }
    }

    [RelayCommand]
    private void AddMkb(Mkb10Code code)
    {
        if(code==null) return;
        if(Diagnoses.Any(x => x.Mkb10CodeId==code.Id)) return;

        var diagnosis = new Diagnosis
        {
            Id=Guid.NewGuid(),
            PatientId=Patient.Id,
            Mkb10CodeId=code.Id,
            Mkb10Code=code,
            DiagnosedAt=DateTime.Now,
            IsPrimary=Diagnoses.Count==0,
            Status=DiagnosisStatus.Active
        };

        Diagnoses.Add(diagnosis);
        MkbSearchText=string.Empty;
        MkbResults.Clear();
        ShowMkbDropdown=false;
    }

    [RelayCommand]
    private void RemoveMkb(Diagnosis diagnosis)
    {
        if(diagnosis!=null&&Diagnoses.Contains(diagnosis))
            Diagnoses.Remove(diagnosis);
    }

    // =====================================================
    // MEDICINE SEARCH + ATTACH (replaces the old BloodType-only medical section)
    // =====================================================

    private CancellationTokenSource _medicineSearchCts = new();

    [ObservableProperty] private ObservableCollection<AttachedMedicineRow> attachedMedicines = new();
    [ObservableProperty] private ObservableCollection<Medicine> medicineResults = new();
    [ObservableProperty] private string medicineSearchText = string.Empty;
    [ObservableProperty] private bool showMedicineDropdown;

    partial void OnMedicineSearchTextChanging(string value)
    {
        if(string.IsNullOrWhiteSpace(value)||value.Length<2)
        {
            MedicineResults.Clear();
            ShowMedicineDropdown=false;
            return;
        }
        _=SearchMedicinesAsync(value);
    }

    [RelayCommand]
    private async Task SearchMedicinesAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query)||query.Length<2)
        {
            MedicineResults.Clear();
            ShowMedicineDropdown=false;
            return;
        }

        _medicineSearchCts.Cancel();
        _medicineSearchCts.Dispose();
        _medicineSearchCts=new CancellationTokenSource();
        var token = _medicineSearchCts.Token;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var matches = await db.Medicines
                .Where(m => m.IsActive&&
                    (m.Name.Contains(query, StringComparison.OrdinalIgnoreCase)||
                     m.GenericName.Contains(query, StringComparison.OrdinalIgnoreCase)||
                     m.Code.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .Take(25)
                .ToListAsync(token);

            if(token.IsCancellationRequested) return;

            MedicineResults=new ObservableCollection<Medicine>(matches);
            ShowMedicineDropdown=matches.Count>0;
        }
        catch(OperationCanceledException) { /* user kept typing */ }
    }

    [RelayCommand]
    private void AddMedicine(Medicine medicine)
    {
        if(medicine==null) return;
        if(AttachedMedicines.Any(r => r.PatientMedicine.MedicineId==medicine.Id&&r.PatientMedicine.IsActive))
            return;

        var patientMedicine = new PatientMedicine
        {
            Id=Guid.NewGuid(),
            PatientId=Patient.Id,
            MedicineId=medicine.Id,
            Medicine=medicine,
            Dosage=medicine.DefaultDosage,
            DosesFrequency=DosesFrequency.Daily,
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
        if(row!=null&&AttachedMedicines.Contains(row))
            AttachedMedicines.Remove(row);
    }

    // =====================================================
    // DOCUMENT UPLOAD
    // =====================================================

    [ObservableProperty] private ObservableCollection<PatientDocument> documents = new();
    [ObservableProperty] private bool isUploadingDocument;

    [RelayCommand]
    private async Task UploadDocumentAsync()
    {
        if(IsUploadingDocument) return;

        try
        {
            IsUploadingDocument=true;

            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle="Изберете документ"
            });
            if(file==null) return;

            // Copy into app-local storage so the source file (e.g. a USB stick or
            // Downloads folder the user later clears) doesn't take the record with it.
            var patientFolder = Path.Combine(FileSystem.AppDataDirectory, "patient-documents", Patient.Id.ToString());
            Directory.CreateDirectory(patientFolder);

            var storedFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var storedPath = Path.Combine(patientFolder, storedFileName);

            await using(var source = await file.OpenReadAsync())
            await using(var dest = File.Create(storedPath))
            {
                await source.CopyToAsync(dest);
            }

            var document = new PatientDocument
            {
                Id=Guid.NewGuid(),
                PatientId=Patient.Id,
                FileName=file.FileName,
                StoredPath=storedPath,
                ContentType=file.ContentType,
                UploadedAt=DateTime.UtcNow
            };

            Documents.Add(document);
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
    private void RemoveDocument(PatientDocument document)
    {
        if(document!=null&&Documents.Contains(document))
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