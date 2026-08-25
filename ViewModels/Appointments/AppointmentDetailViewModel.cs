using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace EHMR.ViewModels.Appointments;

public partial class AppointmentDetailViewModel : BaseDetailViewModel<Appointment>
{
    private readonly IAppointmentDetailService _service;
    private readonly ISelectedItemService<Appointment> _selectedItemService;

    private CancellationTokenSource _cts = new();
    private CancellationTokenSource _medicineCts = new();

    private Appointment? _originalAppointment;
    private bool _isNewAppointmentMode;
    private bool _isModalReturnMode;
    private bool _isApplyingAutomaticSlot;
    private bool _suppressPatientSelection;
    private int _slotRequestVersion;

    protected override string ModuleName => Modules.Appointments;
    private bool _isLoading;
    public bool IsNewAppointment => _isNewAppointmentMode;
    public bool CanSaveAppointment => IsNewAppointment ? CanCreate : CanUpdate;
    public bool CanEditAppointment =>
        !_isNewAppointmentMode&&CanUpdate&&
        Appointment.Status is (AppointmentStatus.Scheduled or AppointmentStatus.CheckedIn)&&
        Appointment.ScheduledStart.Date>=DateTime.Today;
    public bool ShowStatusEditor => IsEditMode&&!IsNewAppointment;

    // Mirrors Encounter's HasEncounterMedicines — drives the medicine table header row.
    public bool HasSelectedMedicines => SelectedMedicines.Count>0;

    // Header now owns identity + timing only ("Термин #.. · dd.MM.yyyy HH:mm–HH:mm").
    // Patient/doctor identity lives exclusively in the ПАЦИЕНТ И РЕУМАТОЛОГ card below —
    // no more duplicating the same two names in both places.
    public string HeaderTitle =>
        _isNewAppointmentMode
            ? "Нов Термин"
            : $"Термин {(Appointment.AppointmentNumber is { Length:>0 } num ? $"№{num}" : string.Empty)}".TrimEnd();

    public string HeaderSubtitle =>
        _isNewAppointmentMode
            ? "Закажување нов термин"
            : $"{Appointment.ScheduledStart:dd.MM.yyyy} • {Appointment.ScheduledStart:HH:mm}–{Appointment.ScheduledEnd:HH:mm}";

    public AppointmentDetailViewModel(
        IAuthorizationService authorizationService,
        IAppointmentDetailService service,
        INavigationService navigation,
        ISelectedItemService<Appointment> selected,
        IUserDialogService userDialogService,
        ISelectedItemService<Appointment> selectedItemService,
        IMenuService menuService)
        : base(navigation, userDialogService, menuService, authorizationService, selectedItemService)
    {
        _service=service;
        _selectedItemService=selected;
        InitMkbAlphabet();
        SelectedMedicines.CollectionChanged+=OnSelectedMedicinesCollectionChanged;
        EvaluatePermissions();
    }

    private void OnSelectedMedicinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        OnPropertyChanged(nameof(HasSelectedMedicines));

    // =========================
    // APPOINTMENT
    // =========================

    [ObservableProperty] private Appointment appointment = new();

    // =========================
    // PATIENT SEARCH
    // =========================

    [ObservableProperty] private string patientSearchText = string.Empty;
    [ObservableProperty] private bool useCyrillicPatientSearch = true;
    [ObservableProperty] private ObservableCollection<Patient> patientSuggestions = new();
    [ObservableProperty] private Patient? patientSuggestionSelection;
    [ObservableProperty] private bool showPatientSuggestions;

    // =========================
    // DIAGNOSIS / MKB SEARCH — property names match EncounterBaseViewModel 1:1
    // =========================

    [ObservableProperty] private string diagnosisSearchText = string.Empty;
    [ObservableProperty] private string diagnosisDescriptionSearchText = string.Empty;
    [ObservableProperty] private bool showDiagnosisDropdown;
    [ObservableProperty] private ObservableCollection<Mkb10Code> availableDiagnoses = new();
    [ObservableProperty] private ObservableCollection<MkbAlphabetSection> mkbAlphabetSections = new();
    [ObservableProperty] private string selectedMkbSection = string.Empty;

    // =========================
    // MEDICINE SEARCH
    // =========================

    [ObservableProperty] private string medicineSearchText = string.Empty;
    [ObservableProperty] private bool showMedicineDropdown;
    [ObservableProperty] private ObservableCollection<Medicine> medicineResults = new();

    // =========================
    // COLLECTIONS
    // =========================

    [ObservableProperty] private ObservableCollection<Diagnosis> selectedDiagnoses = new();
    [ObservableProperty] private ObservableCollection<PatientMedicine> selectedMedicines = new();
    [ObservableProperty] private ObservableCollection<TherapyCycle> visibleTherapies = new();
    [ObservableProperty] private ObservableCollection<Patient> patientsList = new();
    [ObservableProperty] private ObservableCollection<Doctor> doctorsList = new();
    [ObservableProperty] private ObservableCollection<Appointment> appointmentHistory = new();
    [ObservableProperty] private ObservableCollection<Diagnosis> patientDiagnosisHistory = new();
    [ObservableProperty] private ObservableCollection<PatientMedicine> patientMedicinesHistory = new();

    // =========================
    // UI STATE
    // =========================

    [ObservableProperty] private string pageTitle = "Детали за термин";

    // =========================
    // TIME / SCHEDULING
    // =========================

    [ObservableProperty] private TimeSpan selectedStartTime;
    [ObservableProperty] private TimeSpan selectedEndTime;
    [ObservableProperty] private DateTime preferredAppointmentDate = DateTime.Today;
    [ObservableProperty] private string selectedSchedulingHorizon = "Следен слободен термин";

    public IReadOnlyList<string> SchedulingHorizonOptions
    {
        get;
    } =
        ["Следен слободен термин", "За 1 недела", "За 2 недели", "За 3 недели", "За 1 месец"];

    public string AutomaticSlotText => SelectedPatientForAppointment is null
        ? "Изберете пациент за автоматски термин"
        : SelectedDoctorForAppointment is null
        ? "Пациентот нема доделен матичен реуматолог"
        : $"Прв слободен термин: {Appointment.ScheduledStart:dd.MM.yyyy HH:mm}";

    // =========================
    // SELECTIONS
    // =========================

    [ObservableProperty] private Patient? selectedPatientForAppointment;
    [ObservableProperty] private Doctor? selectedDoctorForAppointment;
    [ObservableProperty] private TherapyCycle? selectedTherapyCycle;

    public IReadOnlyList<AppointmentStatusChoice> StatusOptions
    {
        get;
    } =
    [
        new(AppointmentStatus.Scheduled,   "Закажан"),
        new(AppointmentStatus.CheckedIn,   "Пријавен"),
        new(AppointmentStatus.InProgress,  "Во тек"),
        new(AppointmentStatus.Completed,   "Завршен"),
        new(AppointmentStatus.Cancelled,   "Откажан"),
        new(AppointmentStatus.Missed,      "Пропуштен"),
        new(AppointmentStatus.ReScheduled, "Презакажан"),
    ];

    public AppointmentStatusChoice? SelectedStatusOption
    {
        get => StatusOptions.FirstOrDefault(x => x.Value==Appointment.Status);
        set
        {
            if(value==null||Appointment.Status==value.Value) return;
            Appointment.Status=value.Value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Appointment));
            OnPropertyChanged(nameof(CanEditAppointment));
        }
    }

    // =========================
    // PROPERTY CALLBACKS
    // =========================

    partial void OnSelectedDoctorForAppointmentChanged(Doctor? value)
    {
        if(value!=null&&IsNewAppointment)
            _=AssignNextAvailableSlotAsync();
        OnPropertyChanged(nameof(AutomaticSlotText));
    }

    partial void OnSelectedPatientForAppointmentChanged(Patient? value)
    {
        if(!_suppressPatientSelection&&value!=null&&IsNewAppointment)
            _=OnPatientChangedAsync(value);
        OnPropertyChanged(nameof(AutomaticSlotText));
    }

    partial void OnPatientSuggestionSelectionChanged(Patient? value)
    {
        if(value is not null)
            SelectPatientSuggestionCommand.Execute(value);
    }

    partial void OnPatientSearchTextChanged(string value)
    {
        var query = value?.Trim()??string.Empty;
        if(query.Length<1||!IsNewAppointment)
        {
            PatientSuggestions.Clear();
            ShowPatientSuggestions=false;
            return;
        }

        var cyrillicQuery = UseCyrillicPatientSearch
            ? MacedonianTransliterator.ToCyrillic(query)
            : query;

        PatientSuggestions=new ObservableCollection<Patient>(PatientsList
            .Where(p =>
                p.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)||
                p.FullName.Contains(cyrillicQuery, StringComparison.OrdinalIgnoreCase)||
                p.PatientNumber.Contains(query, StringComparison.OrdinalIgnoreCase)||
                p.NationalId.Contains(query, StringComparison.OrdinalIgnoreCase)||
                p.SzboNumber.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.FullName)
            .Take(8));

        ShowPatientSuggestions=PatientSuggestions.Count>0;
    }

    partial void OnUseCyrillicPatientSearchChanged(bool value) =>
        OnPatientSearchTextChanged(PatientSearchText);

    partial void OnDiagnosisSearchTextChanged(string value) =>
        _=SearchDiagnosesAsync(value, DiagnosisDescriptionSearchText);

    partial void OnDiagnosisDescriptionSearchTextChanged(string value) =>
        _=SearchDiagnosesAsync(DiagnosisSearchText, value);


    partial void OnPreferredAppointmentDateChanged(DateTime value)
    {

        if(!_isApplyingAutomaticSlot&&SelectedDoctorForAppointment!=null&&IsNewAppointment)
            _=AssignNextAvailableSlotAsync();

        if(!IsNewAppointment&&IsEditMode)
            SyncAppointmentScheduleFromPickers();
    }

    partial void OnSelectedStartTimeChanged(TimeSpan value)
    {

        if(!IsNewAppointment&&IsEditMode)
            SyncAppointmentScheduleFromPickers();
    }

    private void SyncAppointmentScheduleFromPickers()
    {
        var duration = Appointment.ScheduledEnd-Appointment.ScheduledStart;
        if(duration<=TimeSpan.Zero) duration=TimeSpan.FromMinutes(30);

        var newStart = PreferredAppointmentDate.Date+SelectedStartTime;
        Appointment.ScheduledStart=newStart;
        Appointment.ScheduledEnd=newStart+duration;
        OnPropertyChanged(nameof(Appointment));
        OnPropertyChanged(nameof(HeaderSubtitle));
    }
    partial void OnSelectedSchedulingHorizonChanged(string value)
    {
        if(!IsNewAppointment) return;

        var date = value switch
        {
            "За 1 недела" => DateTime.Today.AddDays(7),
            "За 2 недели" => DateTime.Today.AddDays(14),
            "За 3 недели" => DateTime.Today.AddDays(21),
            "За 1 месец" => DateTime.Today.AddMonths(1),
            _ => DateTime.Today
        };

        PreferredAppointmentDate=date;
        if(SelectedDoctorForAppointment!=null)
            _=AssignNextAvailableSlotAsync();
    }

    partial void OnMedicineSearchTextChanged(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            MedicineResults.Clear();
            ShowMedicineDropdown=false;
            return;
        }
        _=SearchMedicinesAsync(value);
    }

    partial void OnSelectedMedicinesChanging(ObservableCollection<PatientMedicine> value)
    {
        // Old collection is about to be replaced — detach so we don't leak the handler.
        SelectedMedicines.CollectionChanged-=OnSelectedMedicinesCollectionChanged;
    }

    partial void OnSelectedMedicinesChanged(ObservableCollection<PatientMedicine> value)
    {
        value.CollectionChanged+=OnSelectedMedicinesCollectionChanged;
        OnPropertyChanged(nameof(HasSelectedMedicines));
    }

    // =========================
    // MKB ALPHABET
    // =========================

    private void InitMkbAlphabet()
    {
        MkbAlphabetSections=new ObservableCollection<MkbAlphabetSection>(
            Enumerable.Range('A', 26).Select(c => new MkbAlphabetSection((char)c)));
    }

    [RelayCommand]
    private async Task SelectMkbSection(MkbAlphabetSection section)
    {
        if(section is null) return;

        // Toggle: tapping the already-active letter clears the filter
        var wasSelected = section.IsSelected;
        foreach(var s in MkbAlphabetSections)
            s.IsSelected=false;

        SelectedMkbSection=wasSelected ? string.Empty : section.Letter.ToString();
        if(!wasSelected)
            section.IsSelected=true;

        await SearchDiagnosesAsync(DiagnosisSearchText, DiagnosisDescriptionSearchText);
    }

    // =========================
    // SLOT ASSIGNMENT
    // =========================

    private async Task AssignNextAvailableSlotAsync()
    {
        if(SelectedPatientForAppointment is null||SelectedDoctorForAppointment is null)
            return;

        var requestVersion = ++_slotRequestVersion;
        var from = PreferredAppointmentDate.Date==DateTime.Today
            ? DateTime.Now
            : PreferredAppointmentDate.Date.AddHours(8).AddMinutes(30);

        var slot = await _service.GetNextAvailableSlot(
            SelectedDoctorForAppointment.Id,
            SelectedPatientForAppointment.Id,
            from, 30);

        if(requestVersion!=_slotRequestVersion) return;

        _isApplyingAutomaticSlot=true;
        try
        {
            Appointment.ScheduledStart=slot;
            Appointment.ScheduledEnd=slot.AddMinutes(30);
            PreferredAppointmentDate=slot.Date;
            SelectedStartTime=slot.TimeOfDay;
            SelectedEndTime=slot.AddMinutes(30).TimeOfDay;
            OnPropertyChanged(nameof(Appointment));
            OnPropertyChanged(nameof(AutomaticSlotText));
            OnPropertyChanged(nameof(HeaderSubtitle));
        }
        finally { _isApplyingAutomaticSlot=false; }
    }

    // =========================
    // LOAD
    // =========================

    public async Task LoadAsync()
    {

        var selected = _selectedItemService.SelectedItem;

        Guid? prefillPatientId = null;
        if(selected is { Id: var sid }&&sid==Guid.Empty&&selected.PatientId!=Guid.Empty)
        {
            prefillPatientId=selected.PatientId;
            _isModalReturnMode=true;
            selected=null;
        }

        if(selected==null)
        {
            await LoadNewAppointmentAsync(prefillPatientId);
            return;
        }

        _isNewAppointmentMode=false;

        var id = selected.Id;
        if(id==Guid.Empty) return;

        var dto = await _service.GetAppointment(id);
        var ctx = await _service.GetPatientContext(dto.Appointment.PatientId);

        _originalAppointment=dto.Appointment.Clone();
        Appointment=dto.Appointment;

        SelectedDiagnoses=new ObservableCollection<Diagnosis>(dto.Diagnoses);
        VisibleTherapies=new ObservableCollection<TherapyCycle>(ctx.TherapyCycles);
        PatientsList=new ObservableCollection<Patient>(dto.Patients);
        DoctorsList=new ObservableCollection<Doctor>(dto.Doctors);
        AppointmentHistory=new ObservableCollection<Appointment>(ctx.Appointments);
        PatientDiagnosisHistory=new ObservableCollection<Diagnosis>(ctx.Diagnoses);
        PatientMedicinesHistory=new ObservableCollection<PatientMedicine>(ctx.PatientMedicines);
        SelectedMedicines=new ObservableCollection<PatientMedicine>(ctx.PatientMedicines);

        SelectedPatientForAppointment=PatientsList.FirstOrDefault(x => x.Id==Appointment.PatientId);
        SelectedDoctorForAppointment=DoctorsList.FirstOrDefault(x => x.Id==Appointment.DoctorId);
        SelectedTherapyCycle=VisibleTherapies.FirstOrDefault(x => x.Id==Appointment.TherapyCycleId);

        PreferredAppointmentDate=Appointment.ScheduledStart.Date;
        SelectedStartTime=Appointment.ScheduledStart.TimeOfDay;
        SelectedEndTime=Appointment.ScheduledEnd.TimeOfDay;

        PageTitle=$"Термин: {SelectedPatientForAppointment?.FullName}";
        IsReadOnly=!_selectedItemService.OpenInEditMode;
        EvaluatePermissions();

        OnPropertyChanged(nameof(IsNewAppointment));
        OnPropertyChanged(nameof(HasSelectedMedicines));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
    }

    // Rewritten so every property here is assigned exactly once, on exactly one path:
    //  - no prefill patient  -> this method owns the "empty state" assignments
    //  - prefill patient set -> OnPatientChangedAsync owns the real-data assignments
    //    (including its own HeaderTitle/HeaderSubtitle/HasSelectedMedicines notifications),
    //    so we don't pre-seed those collections here just to overwrite them a moment later.
    private async Task LoadNewAppointmentAsync(Guid? prefillPatientId)
    {
        _isNewAppointmentMode=true;
        _originalAppointment=null;
        Appointment=CreateBlankAppointment();

        var lookups = await _service.GetAppointmentContext();
        PatientsList=new ObservableCollection<Patient>(lookups.Patients);
        DoctorsList=new ObservableCollection<Doctor>(lookups.Doctors);

        _suppressPatientSelection=true;
        SelectedPatientForAppointment=prefillPatientId is { } pid
            ? PatientsList.FirstOrDefault(x => x.Id==pid)
            : null;
        _suppressPatientSelection=false;

        Appointment.PatientId=SelectedPatientForAppointment?.Id??Guid.Empty;
        SelectedDiagnoses=new ObservableCollection<Diagnosis>();

        if(SelectedPatientForAppointment!=null)
        {
            await OnPatientChangedAsync(SelectedPatientForAppointment);
        }
        else
        {
            AppointmentHistory=new ObservableCollection<Appointment>();
            PatientDiagnosisHistory=new ObservableCollection<Diagnosis>();
            PatientMedicinesHistory=new ObservableCollection<PatientMedicine>();
            SelectedMedicines=new ObservableCollection<PatientMedicine>();
            VisibleTherapies=new ObservableCollection<TherapyCycle>();
            SelectedTherapyCycle=null;
            SelectedDoctorForAppointment=null;
        }

        PreferredAppointmentDate=Appointment.ScheduledStart.Date;
        SelectedStartTime=Appointment.ScheduledStart.TimeOfDay;
        SelectedEndTime=Appointment.ScheduledEnd.TimeOfDay;
        PageTitle="Нов Термин";
        IsReadOnly=false;

        OnPropertyChanged(nameof(IsNewAppointment));

        // OnPatientChangedAsync already raised these three when it ran above — only raise
        // them here on the empty-state path, so each notification fires exactly once.
        if(SelectedPatientForAppointment is null)
        {
            OnPropertyChanged(nameof(HasSelectedMedicines));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(HeaderSubtitle));
        }
    }

    public async Task OnPatientChangedAsync(Patient patient)
    {
        if(patient==null) return;

        Appointment.PatientId=patient.Id;
        SelectedDoctorForAppointment=DoctorsList.FirstOrDefault(x => x.Id==patient.DoctorId);
        Appointment.DoctorId=SelectedDoctorForAppointment?.Id??Guid.Empty;

        var ctx = await _service.GetPatientContext(patient.Id);

        AppointmentHistory=new ObservableCollection<Appointment>(ctx.Appointments);
        PatientDiagnosisHistory=new ObservableCollection<Diagnosis>(ctx.Diagnoses);
        PatientMedicinesHistory=new ObservableCollection<PatientMedicine>(ctx.PatientMedicines);
        SelectedMedicines=new ObservableCollection<PatientMedicine>(ctx.PatientMedicines);
        VisibleTherapies=new ObservableCollection<TherapyCycle>(ctx.TherapyCycles);
        SelectedTherapyCycle=VisibleTherapies.FirstOrDefault();

        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(AutomaticSlotText));
        OnPropertyChanged(nameof(HasSelectedMedicines));
    }

    private static Appointment CreateBlankAppointment()
    {
        var start = DateTime.Today.AddHours(9);
        return new Appointment
        {
            Id=Guid.Empty,
            ScheduledStart=start,
            ScheduledEnd=start.AddMinutes(30),
            Status=AppointmentStatus.Scheduled
        };
    }

    [RelayCommand]
    private void SelectPatientSuggestion(Patient? patient)
    {
        if(patient is null) return;
        PatientSearchText=patient.FullName;
        ShowPatientSuggestions=false;
        PatientSuggestions.Clear();
        SelectedPatientForAppointment=patient;
    }

    [RelayCommand]
    private Task PatientChanged(Patient patient) => OnPatientChangedAsync(patient);

    // =========================
    // DIAGNOSIS SEARCH — mirrors EncounterBaseViewModel.SearchMkbAsync exactly.
    // Two parameters => can't be a [RelayCommand] (MVVMTK0007). Only ever called
    // internally from the On...Changed partials and SelectMkbSection.
    // =========================

    private async Task SearchDiagnosesAsync(string codeQuery, string descriptionQuery)
    {
        var hasCode = !string.IsNullOrWhiteSpace(codeQuery);
        var hasDesc = !string.IsNullOrWhiteSpace(descriptionQuery);
        var hasSection = !string.IsNullOrWhiteSpace(SelectedMkbSection);

        if(!hasCode&&!hasDesc&&!hasSection)
        {
            AvailableDiagnoses.Clear();
            ShowDiagnosisDropdown=false;
            return;
        }

        _cts.Cancel();
        _cts.Dispose();
        _cts=new CancellationTokenSource();
        var token = _cts.Token;

        try { await Task.Delay(300, token); }
        catch(TaskCanceledException) { return; }

        try
        {
            List<Mkb10Code> result = [];

            if(hasCode)
            {
                var byCode = await _service.SearchDiagnoses(codeQuery.Trim(), token);
                result=result.UnionBy(byCode, x => x.Id).ToList();
            }

            if(hasDesc)
            {
                var term = MacedonianTransliterator.ToCyrillic(descriptionQuery.Trim());
                var byDesc = await _service.SearchDiagnoses(term, token);
                result=result.UnionBy(byDesc, x => x.Id).ToList();

                if(!string.Equals(term, descriptionQuery.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var byRaw = await _service.SearchDiagnoses(descriptionQuery.Trim(), token);
                    result=result.UnionBy(byRaw, x => x.Id).ToList();
                }
            }

            if(!hasCode&&!hasDesc&&hasSection)
            {
                var bySection = await _service.SearchDiagnoses(SelectedMkbSection, token);
                result=result.UnionBy(bySection, x => x.Id).ToList();
            }

            if(hasSection)
            {
                result=result
                    .Where(x => !string.IsNullOrEmpty(x.Code)&&
                                x.Code.StartsWith(SelectedMkbSection, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if(token.IsCancellationRequested) return;

            AvailableDiagnoses=new ObservableCollection<Mkb10Code>(result.Take(30));
            ShowDiagnosisDropdown=AvailableDiagnoses.Count>0;
        }
        catch(TaskCanceledException) { }
    }

    [RelayCommand]
    private void AddDiagnosis(Mkb10Code code)
    {
        if(code==null) return;
        if(SelectedDiagnoses.Any(x => x.Mkb10CodeId==code.Id)) return;

        SelectedDiagnoses.Add(new Diagnosis
        {
            Mkb10CodeId=code.Id,
            Id=code.Id,
            Mkb10Code=code,
            IsPrimary=SelectedDiagnoses.Count==0
        });

        DiagnosisSearchText=string.Empty;
        DiagnosisDescriptionSearchText=string.Empty;
        AvailableDiagnoses.Clear();
        ShowDiagnosisDropdown=false;

        foreach(var s in MkbAlphabetSections)
            s.IsSelected=false;
        SelectedMkbSection=string.Empty;
    }

    [RelayCommand]
    private void RemoveDiagnosis(Diagnosis d)
    {
        if(d!=null) SelectedDiagnoses.Remove(d);
    }

    // =========================
    // MEDICINE SEARCH — mirrors EncounterBaseViewModel.SearchMedicinesAsync
    // =========================

    [RelayCommand]
    private async Task SearchMedicinesAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query))
        {
            MedicineResults.Clear();
            ShowMedicineDropdown=false;
            return;
        }

        _medicineCts.Cancel();
        _medicineCts.Dispose();
        _medicineCts=new CancellationTokenSource();
        var token = _medicineCts.Token;

        try { await Task.Delay(250, token); }
        catch(TaskCanceledException) { return; }

        try
        {
            var searchTerm = MacedonianTransliterator.ToCyrillic(query);
            var byTerm = await _service.SearchMedicines(searchTerm, token);
            var result = byTerm;

            if(!string.Equals(searchTerm, query, StringComparison.OrdinalIgnoreCase))
            {
                var byRaw = await _service.SearchMedicines(query, token);
                result=byTerm.UnionBy(byRaw, x => x.Id).ToList();
            }

            if(token.IsCancellationRequested) return;

            MedicineResults=new ObservableCollection<Medicine>(result.Take(30));
            ShowMedicineDropdown=MedicineResults.Count>0;
        }
        catch(TaskCanceledException) { }
    }

    [RelayCommand]
    private void AddMedicine(Medicine medicine)
    {
        if(medicine==null) return;
        if(SelectedMedicines.Any(x => x.MedicineId==medicine.Id)) return;

        SelectedMedicines.Add(new PatientMedicine
        {
            MedicineId=medicine.Id,
            Medicine=medicine,
            PatientId=SelectedPatientForAppointment?.Id??Guid.Empty,
            Dosage="1", // matches Encounter convention: Dosage doubles as quantity
            DosesFrequency=DosesFrequency.Other,
            StartDate=DateTime.Now,
            IsActive=true
        });

        MedicineSearchText=string.Empty;
        MedicineResults.Clear();
        ShowMedicineDropdown=false;
    }

    [RelayCommand]
    private void RemoveMedicine(PatientMedicine m)
    {
        if(m!=null) SelectedMedicines.Remove(m);
    }

    // =========================
    // EDIT / SAVE / CANCEL
    // =========================

    [RelayCommand]
    private void ToggleEditMode()
    {
        ToggleEdit();
        OnPropertyChanged(nameof(ShowStatusEditor));
    }

    [RelayCommand]
    private async Task NewAppointmentForPatient(Patient patient)
    {
        if(patient is null) return;
        if(!CanCreate)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за додавање термини.", "OK");
            return;
        }
        _selectedItemService.SelectedItem=new Appointment { Id=Guid.Empty, PatientId=patient.Id };
        await NavigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if(SelectedPatientForAppointment==null)
        {
            await UserDialogService.ShowAlertAsync("Валидација", "Изберете пациент.", "OK");
            return;
        }
        if(SelectedDoctorForAppointment==null)
        {
            await UserDialogService.ShowAlertAsync("Валидација", "Избраниот пациент нема доделен матичен реуматолог.", "OK");
            return;
        }

        if(IsNewAppointment)
            await AssignNextAvailableSlotAsync();

        if(Appointment.ScheduledEnd<=Appointment.ScheduledStart)
        {
            await UserDialogService.ShowAlertAsync("Валидација", "Не може да се одреди валиден слободен термин.", "OK");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            Appointment.PatientId=SelectedPatientForAppointment.Id;
            Appointment.DoctorId=SelectedDoctorForAppointment.Id;
            Appointment.TherapyCycleId=SelectedTherapyCycle?.Id;

            await _service.SaveAppointment(
                Appointment,
                SelectedDiagnoses.ToList(),
                SelectedTherapyCycle,
                SelectedMedicines.ToList());

            await UserDialogService.ShowAlertAsync("Успешно", "Терминот е успешно зачуван.", "OK");

            if(_isModalReturnMode)
            {
                _selectedItemService.SelectedItem=Appointment;
                await NavigationService.GoToAsync("..");
            }
            else if(_isNewAppointmentMode)
            {
                _selectedItemService.SelectedItem=null;
                await NavigationService.GoToAsync(AppRoutes.Appointments.List);
            }
            else
            {
                _originalAppointment=Appointment.Clone();
                IsReadOnly=true;
            }
        }, "Грешка при зачувување на податоци");
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await ExecuteSafeAsync(async () =>
        {
            if(_isModalReturnMode)
            {
                _selectedItemService.SelectedItem=null;
                await NavigationService.GoToAsync("..");
                return;
            }
            if(_isNewAppointmentMode)
            {
                await NavigationService.GoToAsync(AppRoutes.Appointments.List);
                return;
            }
            if(_originalAppointment!=null)
            {
                Appointment=_originalAppointment.Clone();
                SelectedPatientForAppointment=PatientsList.FirstOrDefault(x => x.Id==Appointment.PatientId);
                SelectedDoctorForAppointment=DoctorsList.FirstOrDefault(x => x.Id==Appointment.DoctorId);
                SelectedTherapyCycle=VisibleTherapies.FirstOrDefault(x => x.Id==Appointment.TherapyCycleId);
                SelectedStartTime=Appointment.ScheduledStart.TimeOfDay;
                SelectedEndTime=Appointment.ScheduledEnd.TimeOfDay;
            }
            IsReadOnly=true;
            await Task.CompletedTask;
        }, "Грешка при откажување");
    }

    [RelayCommand]
    private async Task SelectAppointmentAsync(Appointment appointment)
    {
        if(appointment==null) return;
        _selectedItemService.SelectedItem=appointment;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GenerateNextCycleAsync()
    {
        await _service.GenerateNextTherapyCycle(Appointment);
        await LoadAsync();
    }
}

// =============================================================================
// SUPPORTING TYPES
// =============================================================================

public partial class MkbAlphabetSection : ObservableObject
{
    public char Letter
    {
        get;
    }
    [ObservableProperty] private bool isSelected;
    public MkbAlphabetSection(char letter) => Letter=letter;
}

public static class AppointmentExtensions
{
    public static Appointment Clone(this Appointment source)
    {
        if(source is null) return new Appointment();
        return new Appointment
        {
            Id=source.Id,
            AppointmentNumber=source.AppointmentNumber,
            PatientId=source.PatientId,
            DoctorId=source.DoctorId,
            TherapyCycleId=source.TherapyCycleId,
            ScheduledStart=source.ScheduledStart,
            ScheduledEnd=source.ScheduledEnd,
            Status=source.Status,
            ReasonForVisit=source.ReasonForVisit,
            ClinicalNotes=source.ClinicalNotes,
            CreatedAt=source.CreatedAt,
            Patient=source.Patient,
            Doctor=source.Doctor,
            TherapyCycle=source.TherapyCycle
        };
    }
}

public sealed record AppointmentStatusChoice(AppointmentStatus Value, string Label)
{
    public override string ToString() => Label;
}