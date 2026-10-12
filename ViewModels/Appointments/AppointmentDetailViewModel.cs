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
    private readonly IPatientClinicalReportService _clinicalReportService;

    private CancellationTokenSource _cts = new();
    private CancellationTokenSource _medicineCts = new();

    private Appointment? _originalAppointment;
    private Appointment? _loadedAppointmentSelection;
    private bool _hasLoadedExistingAppointment;
    private bool _isNewAppointmentMode;
    private bool _isModalReturnMode;
    private bool _isApplyingAutomaticSlot;
    private bool _suppressPatientSelection;
    private int _slotRequestVersion;

    protected override string ModuleName => Modules.Appointments;
    private bool _isLoading;
    public bool IsNewAppointment => _isNewAppointmentMode;
    public bool CanSaveAppointment =>
        IsNewAppointment
            ? CanCreate && CanSchedule
            : HasScheduleChange
                ? CanUpdate && CanSchedule
                : SelectedStatusForSave is { } status
                    && (status == _originalAppointment?.Status
                        ? CanUpdate
                        : status switch
                        {
                            AppointmentStatus.Cancelled => CanCancel,
                            AppointmentStatus.Completed => CanComplete,
                            AppointmentStatus.Scheduled => CanSchedule,
                            AppointmentStatus.InProgress => CanUpdate,
                            _ => false
                        });

    public bool CanEditAppointment =>
        IsReadOnly&&!_isNewAppointmentMode&&CanUpdate&&
        Appointment.Status is AppointmentStatus.Scheduled or AppointmentStatus.InProgress;

    public bool CanCancelAppointment =>
        !_isNewAppointmentMode &&
        CanCancel &&
        Appointment.Status is AppointmentStatus.Scheduled or AppointmentStatus.InProgress;

    public bool CanCompleteAppointment =>
        !_isNewAppointmentMode &&
        CanComplete &&
        Appointment.Status is AppointmentStatus.Scheduled or AppointmentStatus.InProgress;

    private bool HasScheduleChange =>
        _originalAppointment is not null &&
        (Appointment.ScheduledStart != _originalAppointment.ScheduledStart ||
         Appointment.ScheduledEnd != _originalAppointment.ScheduledEnd);

    private bool IsStatusChangeAuthorized =>
        _originalAppointment is not null &&
        Appointment.Status != _originalAppointment.Status &&
        Appointment.Status switch
        {
            AppointmentStatus.Cancelled => CanCancel,
            AppointmentStatus.Completed => CanComplete,
            AppointmentStatus.Scheduled => CanSchedule,
            AppointmentStatus.InProgress => CanUpdate,
            _ => false
        };

    private bool CanScheduleForCurrentChange =>
        !HasScheduleChange || CanSchedule;
    public bool ShowStatusEditor => IsEditMode&&!IsNewAppointment;

    // Mirrors Encounter's HasEncounterMedicines — drives the medicine table header row.
    public bool HasSelectedMedicines => SelectedMedicines.Count>0;

    // Header now owns identity + timing only ("Термин #.. · dd.MM.yyyy–HH:mm").
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
        IMenuService menuService,
        IPatientClinicalReportService clinicalReportService)
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
    [RelayCommand]
    private async Task GenerateClinicalReportAsync()
    {
        if(!CanPrint)
        {
            await UserDialogService.ShowAlertAsync(
                "Пристапот е одбиен",
                "Немате овластување за печатење/генерирање извештај.",
                "ОК");
            return;
        }

        if(Appointment.PatientId==Guid.Empty) return;

        try
        {
            var path=await _clinicalReportService.GeneratePdfAsync(
                Appointment.PatientId,null,
                Appointment.Id,
                $"Детален извештај - {SelectedPatientForAppointment?.FullName ?? "Пациент"}");

            await Launcher.Default.OpenAsync(new OpenFileRequest(
                Path.GetFileName(path),
                new ReadOnlyFile(path)));
        }
        catch(Exception ex)
        {
            await UserDialogService.ShowAlertAsync(
                "Извештај",
                $"PDF извештајот не може да се генерира: {ex.Message}",
                "ОК");
        }
    }


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
    // MKB-10 ASSIGNMENT / MKB SEARCH — property names match EncounterBaseViewModel 1:1
    // =========================

    [ObservableProperty] private string mkb10CodeSearchText = string.Empty;
    [ObservableProperty] private string mkb10DescriptionSearchText = string.Empty;
    [ObservableProperty] private bool showMkb10Dropdown;
    [ObservableProperty] private ObservableCollection<Mkb10Code> availableMkb10Codes = new();
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

    [ObservableProperty] private ObservableCollection<PatientMkb10Assignment> selectedMkb10Assignments = new();
    [ObservableProperty] private ObservableCollection<PatientMedicine> selectedMedicines = new();
    [ObservableProperty] private ObservableCollection<Patient> patientsList = new();
    [ObservableProperty] private ObservableCollection<Doctor> doctorsList = new();
    [ObservableProperty] private ObservableCollection<Appointment> appointmentHistory = new();
    [ObservableProperty] private ObservableCollection<PatientMkb10Assignment> patientMkb10History = new();
    [ObservableProperty] private ObservableCollection<PatientMedicine> patientMedicinesHistory = new();
    [ObservableProperty] private ObservableCollection<PatientScore> patientScoreHistory = new();
    [ObservableProperty] private ObservableCollection<string> applicationRegimeOptions = new();
    [ObservableProperty] private ObservableCollection<string> appointmentScores = new();
    [ObservableProperty] private string scoreSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<string> scoreSuggestions = new();
    [ObservableProperty] private bool showScoreSuggestions;
    private readonly List<string> _scoreOptions = new();

    partial void OnScoreSearchTextChanged(string value)
    {
        var query = value?.Trim() ?? string.Empty;
        var candidates = string.IsNullOrWhiteSpace(query) ? _scoreOptions :
            _scoreOptions.Where(x => x.Contains(query, StringComparison.OrdinalIgnoreCase)
                || x.Contains(MacedonianTransliterator.ToCyrillic(query), StringComparison.OrdinalIgnoreCase)
                || x.Contains(MacedonianTransliterator.ToLatin(query), StringComparison.OrdinalIgnoreCase)).ToList();
        ScoreSuggestions = new ObservableCollection<string>(candidates.Distinct(StringComparer.OrdinalIgnoreCase).Take(8));
        ShowScoreSuggestions = ScoreSuggestions.Count > 0;
    }

    [ObservableProperty] private bool scheduledStatusSelected;
    [ObservableProperty] private bool inProgressStatusSelected;
    [ObservableProperty] private bool completedStatusSelected;
    [ObservableProperty] private bool cancelledStatusSelected;
    [ObservableProperty] private AppointmentStatus? selectedStatusForSave;

    public string StatusDisplay => SelectedStatusForSave switch
    {
        AppointmentStatus.Scheduled => "Закажан",
        AppointmentStatus.InProgress => "Во тек",
        AppointmentStatus.Completed => "Завршен",
        AppointmentStatus.Cancelled => "Откажан",
        _ => "Не е избран"
    };

    partial void OnScheduledStatusSelectedChanged(bool value) { if(value) SelectStatusForSave(AppointmentStatus.Scheduled); }
    partial void OnInProgressStatusSelectedChanged(bool value) { if(value) SelectStatusForSave(AppointmentStatus.InProgress); }
    partial void OnCompletedStatusSelectedChanged(bool value) { if(value) SelectStatusForSave(AppointmentStatus.Completed); }
    partial void OnCancelledStatusSelectedChanged(bool value) { if(value) SelectStatusForSave(AppointmentStatus.Cancelled); }

    private void SelectStatusForSave(AppointmentStatus status)
    {
        SelectedStatusForSave=status;
        if(status!=AppointmentStatus.Scheduled) ScheduledStatusSelected=false;
        if(status!=AppointmentStatus.InProgress) InProgressStatusSelected=false;
        if(status!=AppointmentStatus.Completed) CompletedStatusSelected=false;
        if(status!=AppointmentStatus.Cancelled) CancelledStatusSelected=false;
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(CanSaveAppointment));
    }

    private void ResetStatusSelection()
    {
        ScheduledStatusSelected=false;
        InProgressStatusSelected=false;
        CompletedStatusSelected=false;
        CancelledStatusSelected=false;
        SelectedStatusForSave=null;
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(CanSaveAppointment));
    }

    public IEnumerable<PatientMkb10Assignment> CurrentMkb10Assignments =>
        PatientMkb10History
            .Where(x => x.Mkb10Code is not null)
            .GroupBy(x => x.Mkb10Code!.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.DiagnosedAt).First())
            .OrderByDescending(x => x.DiagnosedAt);

    private async Task LoadScoreOptionsAsync()
    {
        _scoreOptions.Clear();
        var globalOptions = await _service.GetScoreSuggestionsAsync();
        _scoreOptions.AddRange(globalOptions.Where(x => !string.IsNullOrWhiteSpace(x)));
        _scoreOptions.AddRange(PatientScoreHistory.Where(x => !string.IsNullOrWhiteSpace(x.ScoreText)).Select(x => x.ScoreText.Trim()));
        var distinct = _scoreOptions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _scoreOptions.Clear();
        _scoreOptions.AddRange(distinct);
        OnScoreSearchTextChanged(ScoreSearchText);
    }

    private IEnumerable<PatientMedicine> ActivePatientMedicineAssignments =>
        PatientMedicinesHistory.Where(x => x.IsActive && !x.EncounterId.HasValue && x.Medicine is not null)
            .GroupBy(x => x.MedicineId).Select(group => group.First())
            .OrderBy(x => x.Medicine!.BilingualName);

    public IEnumerable<PatientMedicine> CurrentPatientMedicines => ActivePatientMedicineAssignments;

    public string CurrentPatientScore => PatientScoreHistory
        .OrderByDescending(x => x.RecordedAt)
        .Select(x => x.DisplayText)
        .FirstOrDefault() ?? "—";

    [RelayCommand]
    private async Task AddPatientScoreAsync()
    {
        if(SelectedPatientForAppointment is null)
        {
            await UserDialogService.ShowAlertAsync("Скор", "Прво изберете пациент.", "Во ред");
            return;
        }
        if(string.IsNullOrWhiteSpace(ScoreSearchText))
        {
            await UserDialogService.ShowAlertAsync("Скор", "Внесете име на скор, на пример DAS28.", "Во ред");
            return;
        }
        var scoreName = ScoreSearchText.Trim();
        var number = await UserDialogService.ShowPromptAsync(
            "Вредност за прегледот",
            $"Внесете ја вредноста за „{scoreName}“. Вредноста ќе биде зачувана со прегледот поврзан со терминот.",
            "Додај", "Откажи", "Пример: 3.2");
        if(string.IsNullOrWhiteSpace(number)) return;
        var entry = $"{scoreName}: {number.Trim()}";
        if(!AppointmentScores.Contains(entry, StringComparer.OrdinalIgnoreCase))
            AppointmentScores.Add(entry);
        ScoreSearchText = string.Empty;
        ScoreSuggestions.Clear();
        ShowScoreSuggestions = false;
    }

    [RelayCommand]
    private async Task SelectScoreSuggestionAsync(string? score)
    {
        if(string.IsNullOrWhiteSpace(score)) return;
        ScoreSearchText = score;
        ShowScoreSuggestions = false;
        await AddPatientScoreAsync();
    }

    [RelayCommand]
    private void RemoveAppointmentScore(string? score)
    {
        if(!string.IsNullOrWhiteSpace(score)) AppointmentScores.Remove(score);
    }

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
        : $"Прв слободен термин: {Appointment.ScheduledStart:dd.MM.yyyy}";

    // =========================
    // SELECTIONS
    // =========================

    [ObservableProperty] private Patient? selectedPatientForAppointment;
    [ObservableProperty] private Doctor? selectedDoctorForAppointment;

    public IReadOnlyList<AppointmentStatusChoice> StatusOptions
    {
        get;
    } =
    [
        new(AppointmentStatus.Scheduled,   "Закажан"),
        new(AppointmentStatus.InProgress,  "Во тек"),
        new(AppointmentStatus.Completed,   "Завршен"),
        new(AppointmentStatus.Cancelled,   "Откажан"),
    ];

    public AppointmentStatusChoice? SelectedStatusOption
    {
        get => StatusOptions.FirstOrDefault(x => x.Value==Appointment.Status);
        set
        {
            if(value==null||Appointment.Status==value.Value) return;

            var allowed = value.Value switch
            {
                AppointmentStatus.Cancelled => CanCancelAppointment,
                AppointmentStatus.Completed => CanCompleteAppointment,
                AppointmentStatus.Scheduled => CanSchedule,
                AppointmentStatus.InProgress => CanUpdate,
                _ => false
            };

            if(!allowed)
            {
                _ = UserDialogService.ShowAlertAsync(
                    "Пристапот е одбиен",
                    "Немате овластување за оваа промена на статусот.",
                    "ОК");
                return;
            }

            Appointment.Status=value.Value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Appointment));
            OnPropertyChanged(nameof(CanEditAppointment));
            OnPropertyChanged(nameof(CanCancelAppointment));
            OnPropertyChanged(nameof(CanCompleteAppointment));
            OnPropertyChanged(nameof(CanSaveAppointment));
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

    partial void OnMkb10CodeSearchTextChanged(string value) =>
        _=SearchMkb10CodesAsync(value, Mkb10DescriptionSearchText);

    partial void OnMkb10DescriptionSearchTextChanged(string value) =>
        _=SearchMkb10CodesAsync(Mkb10CodeSearchText, value);


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

        await SearchMkb10CodesAsync(Mkb10CodeSearchText, Mkb10DescriptionSearchText);
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
        ResetStatusSelection();

        var selected = _selectedItemService.SelectedItem;

        Guid? prefillPatientId = null;
        if(selected is { Id: var sid }&&sid==Guid.Empty&&selected.PatientId!=Guid.Empty)
        {
            prefillPatientId=selected.PatientId;
            _isModalReturnMode=true;
            selected=null;
            _selectedItemService.SelectedItem=null; // consume the patient-prefill sentinel too
        }

        // SelectedItemService is a singleton. Consume navigation context once captured,
        // while retaining the record locally so this same edit page can reappear safely.
        if(selected is not null)
        {
            _loadedAppointmentSelection=selected;
            _selectedItemService.SelectedItem=null;
        }
        else if(_hasLoadedExistingAppointment && prefillPatientId is null)
        {
            selected=_loadedAppointmentSelection;
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
        _loadedAppointmentSelection=dto.Appointment;
        _hasLoadedExistingAppointment=true;
        var ctx = await _service.GetPatientContext(dto.Appointment.PatientId);

        _originalAppointment=dto.Appointment.Clone();
        Appointment=dto.Appointment;

        SelectedMkb10Assignments=new ObservableCollection<PatientMkb10Assignment>(dto.Mkb10Assignments);
        PatientsList=new ObservableCollection<Patient>(dto.Patients);
        DoctorsList=new ObservableCollection<Doctor>(dto.Doctors);
        AppointmentHistory=new ObservableCollection<Appointment>(ctx.Appointments);
        PatientMkb10History=new ObservableCollection<PatientMkb10Assignment>(ctx.Mkb10Assignments);
        PatientMedicinesHistory=new ObservableCollection<PatientMedicine>(ctx.PatientMedicines);
        PatientScoreHistory=new ObservableCollection<PatientScore>(ctx.Scores);
        await LoadScoreOptionsAsync();
        OnPropertyChanged(nameof(CurrentMkb10Assignments));
        OnPropertyChanged(nameof(CurrentPatientMedicines));
        OnPropertyChanged(nameof(CurrentPatientScore));
        SelectedMedicines=IsNewAppointment
            ? new ObservableCollection<PatientMedicine>()
            : new ObservableCollection<PatientMedicine>(ActivePatientMedicineAssignments);

        SelectedPatientForAppointment=PatientsList.FirstOrDefault(x => x.Id==Appointment.PatientId);
        SelectedDoctorForAppointment=DoctorsList.FirstOrDefault(x => x.Id==Appointment.DoctorId);

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
        OnPropertyChanged(nameof(CanEditAppointment));
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
        ApplicationRegimeOptions = new ObservableCollection<string>((await _service.GetApplicationRegimesAsync()).Select(x => x.Regime));
        PatientsList=new ObservableCollection<Patient>(lookups.Patients);
        DoctorsList=new ObservableCollection<Doctor>(lookups.Doctors);

        _suppressPatientSelection=true;
        SelectedPatientForAppointment=prefillPatientId is { } pid
            ? PatientsList.FirstOrDefault(x => x.Id==pid)
            : null;
        _suppressPatientSelection=false;

        // When opened from a patient's context, keep the same selected-patient
        // state as the encounter form: populate the search field and load context.
        PatientSearchText=SelectedPatientForAppointment?.FullName??string.Empty;
        PatientSuggestions.Clear();
        ShowPatientSuggestions=false;

        Appointment.PatientId=SelectedPatientForAppointment?.Id??Guid.Empty;
        SelectedMkb10Assignments=new ObservableCollection<PatientMkb10Assignment>();

        if(SelectedPatientForAppointment!=null)
        {
            await OnPatientChangedAsync(SelectedPatientForAppointment);
        }
        else
        {
            AppointmentHistory=new ObservableCollection<Appointment>();
            PatientMkb10History=new ObservableCollection<PatientMkb10Assignment>();
            PatientMedicinesHistory=new ObservableCollection<PatientMedicine>();
            PatientScoreHistory=new ObservableCollection<PatientScore>();
            _scoreOptions.Clear();
            ScoreSuggestions.Clear();
            ShowScoreSuggestions=false;
            OnPropertyChanged(nameof(CurrentMkb10Assignments));
            OnPropertyChanged(nameof(CurrentPatientMedicines));
            OnPropertyChanged(nameof(CurrentPatientScore));
            SelectedMedicines=new ObservableCollection<PatientMedicine>();
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
        PatientSearchText=patient.FullName;

        // Reset patient-scoped selections and search results when switching patients.
        AppointmentScores.Clear();
        ScoreSearchText=string.Empty;
        ScoreSuggestions.Clear();
        ShowScoreSuggestions=false;
        MedicineSearchText=string.Empty;
        MedicineResults.Clear();
        ShowMedicineDropdown=false;
        PatientSuggestions.Clear();
        ShowPatientSuggestions=false;

        var ctx = await _service.GetPatientContext(patient.Id);

        AppointmentHistory=new ObservableCollection<Appointment>(ctx.Appointments);
        PatientMkb10History=new ObservableCollection<PatientMkb10Assignment>(ctx.Mkb10Assignments);
        PatientMedicinesHistory=new ObservableCollection<PatientMedicine>(ctx.PatientMedicines);
        PatientScoreHistory=new ObservableCollection<PatientScore>(ctx.Scores);
        await LoadScoreOptionsAsync();
        OnPropertyChanged(nameof(CurrentMkb10Assignments));
        OnPropertyChanged(nameof(CurrentPatientMedicines));
        OnPropertyChanged(nameof(CurrentPatientScore));
        SelectedMedicines=IsNewAppointment
            ? new ObservableCollection<PatientMedicine>()
            : new ObservableCollection<PatientMedicine>(ActivePatientMedicineAssignments);

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
    // MKB-10 ASSIGNMENT SEARCH — mirrors EncounterBaseViewModel.SearchMkbAsync exactly.
    // Two parameters => can't be a [RelayCommand] (MVVMTK0007). Only ever called
    // internally from the On...Changed partials and SelectMkbSection.
    // =========================

    private async Task SearchMkb10CodesAsync(string codeQuery, string descriptionQuery)
    {
        var hasCode = !string.IsNullOrWhiteSpace(codeQuery);
        var hasDesc = !string.IsNullOrWhiteSpace(descriptionQuery);
        var hasSection = !string.IsNullOrWhiteSpace(SelectedMkbSection);

        if(!hasCode&&!hasDesc&&!hasSection)
        {
            AvailableMkb10Codes.Clear();
            ShowMkb10Dropdown=false;
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
                var byCode = await _service.SearchMkb10Codes(codeQuery.Trim(), token);
                result=result.UnionBy(byCode, x => x.Id).ToList();
            }

            if(hasDesc)
            {
                var term = MacedonianTransliterator.ToCyrillic(descriptionQuery.Trim());
                var byDesc = await _service.SearchMkb10Codes(term, token);
                result=result.UnionBy(byDesc, x => x.Id).ToList();

                if(!string.Equals(term, descriptionQuery.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var byRaw = await _service.SearchMkb10Codes(descriptionQuery.Trim(), token);
                    result=result.UnionBy(byRaw, x => x.Id).ToList();
                }
            }

            if(!hasCode&&!hasDesc&&hasSection)
            {
                var bySection = await _service.SearchMkb10Codes(SelectedMkbSection, token);
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

            AvailableMkb10Codes=new ObservableCollection<Mkb10Code>(result.Take(30));
            ShowMkb10Dropdown=AvailableMkb10Codes.Count>0;
        }
        catch(TaskCanceledException) { }
    }

    [RelayCommand]
    private void AddMkb10Code(Mkb10Code code)
    {
        if(code==null) return;
        if(SelectedMkb10Assignments.Any(x => x.Mkb10CodeId==code.Id)) return;

        SelectedMkb10Assignments.Add(new PatientMkb10Assignment
        {
            Mkb10CodeId=code.Id,
            Id=code.Id,
            Mkb10Code=code,
            IsPrimary=SelectedMkb10Assignments.Count==0
        });

        Mkb10CodeSearchText=string.Empty;
        Mkb10DescriptionSearchText=string.Empty;
        AvailableMkb10Codes.Clear();
        ShowMkb10Dropdown=false;

        foreach(var s in MkbAlphabetSections)
            s.IsSelected=false;
        SelectedMkbSection=string.Empty;
    }

    [RelayCommand]
    private void RemoveMkb10Assignment(PatientMkb10Assignment d)
    {
        if(d!=null) SelectedMkb10Assignments.Remove(d);
    }

    // =========================
    // MEDICINE SEARCH — mirrors EncounterBaseViewModel.SearchMedicinesAsync
    // =========================

    [RelayCommand]
    private Task SearchMedicinesAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query) || SelectedPatientForAppointment is null)
        {
            MedicineResults.Clear();
            ShowMedicineDropdown=false;
            return Task.CompletedTask;
        }
        var term=query.Trim();
        var approvedMedicines=ActivePatientMedicineAssignments
            .Where(x => !SelectedMedicines.Any(selected => selected.MedicineId==x.MedicineId))
            .Where(x => MedicineSearchMatches(x.Medicine?.Name, term)
                || MedicineSearchMatches(x.Medicine?.GenericName, term)
                || MedicineSearchMatches(x.Medicine?.BilingualName, term)
                || MedicineSearchMatches(x.Medicine?.Code, term))
            .Select(x => x.Medicine!).GroupBy(x => x.Id).Select(group => group.First()).Take(12).ToList();
        MedicineResults=new ObservableCollection<Medicine>(approvedMedicines);
        ShowMedicineDropdown=MedicineResults.Count>0;
        return Task.CompletedTask;
    }

    private static bool MedicineSearchMatches(string? source, string query)
    {
        if(string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(query)) return false;
        var variants = new[] { query, MacedonianTransliterator.ToCyrillic(query), MacedonianTransliterator.ToLatin(query) };
        return variants.Any(value => source.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    private void AddMedicine(Medicine medicine)
    {
        if(medicine is null || SelectedPatientForAppointment is null) return;
        if(SelectedMedicines.Any(x => x.MedicineId==medicine.Id)) return;
        var assignedMedicine=ActivePatientMedicineAssignments.FirstOrDefault(x => x.MedicineId==medicine.Id);
        if(assignedMedicine is null) return;
        SelectedMedicines.Add(new PatientMedicine
        {
            Id=Guid.Empty,
            MedicineId=assignedMedicine.MedicineId,
            Medicine=assignedMedicine.Medicine,
            PatientId=SelectedPatientForAppointment.Id,
            Dosage=assignedMedicine.Dosage,
            ApplicationRegimeId=assignedMedicine.ApplicationRegimeId,
            ApplicationRegime=assignedMedicine.ApplicationRegime,
            Quantity=assignedMedicine.Quantity,
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
        OnPropertyChanged(nameof(CanEditAppointment));
        OnPropertyChanged(nameof(ShowStatusEditor));
    }

    [RelayCommand]
    private async Task NewAppointmentForPatient(Patient patient)
    {
        if(patient is null) return;

        if(patient.Status != PatientStatus.Active)
        {
            await UserDialogService.ShowAlertAsync(
                "Неактивен пациент",
                "Не може да се креира термин за неактивен пациент.",
                "ОК");
            return;
        }

        if(!CanCreate || !CanSchedule)
        {
            await UserDialogService.ShowAlertAsync(
                "Пристапот е одбиен",
                "Немате овластување за креирање и закажување термин.",
                "ОК");
            return;
        }

        _selectedItemService.SelectedItem=new Appointment { Id=Guid.Empty, PatientId=patient.Id };
        await NavigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var confirmed = await UserDialogService.ShowConfirmationAsync(
            "Зачувај термин",
            "Дали сте сигурни дека сакате да го зачувате терминот?",
            "Да",
            "Не");
        if(!confirmed) return;

        if(IsNewAppointment && (!CanCreate || !CanSchedule))
        {
            await UserDialogService.ShowAlertAsync(
                "Пристапот е одбиен",
                "Немате овластување за креирање и закажување термин.",
                "ОК");
            return;
        }

        if(!IsNewAppointment && !CanSaveAppointment && (SelectedStatusForSave is not null || HasScheduleChange))
        {
            await UserDialogService.ShowAlertAsync(
                "Пристапот е одбиен",
                "Немате овластување за оваа промена на термин.",
                "ОК");
            return;
        }

        if(!IsNewAppointment &&
           _originalAppointment is not null &&
           HasScheduleChange &&
           !CanSchedule)
        {
            await UserDialogService.ShowAlertAsync(
                "Пристапот е одбиен",
                "Немате овластување за промена на распоредот на термин.",
                "ОК");
            return;
        }

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
        if(SelectedMedicines.All(x => !x.IsActive))
        {
            await UserDialogService.ShowAlertAsync("Валидација", "Доделете најмалку еден активен лек.", "OK");
            return;
        }
        if(PatientScoreHistory.Count==0 && AppointmentScores.Count==0)
        {
            await UserDialogService.ShowAlertAsync("Валидација", "Пациентот мора да има внесен скор пред зачувување.", "OK");
            return;
        }
        if(SelectedStatusForSave is null)
        {
            await UserDialogService.ShowAlertAsync("Валидација", "Изберете статус за терминот пред зачувување.", "OK");
            return;
        }
        var statusAuthorized = SelectedStatusForSave.Value switch
        {
            AppointmentStatus.Scheduled => CanSchedule,
            AppointmentStatus.InProgress => CanUpdate,
            AppointmentStatus.Completed => CanComplete,
            AppointmentStatus.Cancelled => CanCancel,
            _ => false
        };
        if(!statusAuthorized)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате овластување за избраниот статус.", "ОК");
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
            Appointment.Status=SelectedStatusForSave!.Value;

            await _service.SaveAppointment(
                Appointment,
                SelectedMkb10Assignments.ToList(),
                SelectedMedicines.ToList(),
                AppointmentScores.Select(entry =>
                {
                    var separator = entry.IndexOf(':');
                    return separator > 0
                        ? new PatientScore { ScoreText = entry[..separator].Trim(), Number = entry[(separator + 1)..].Trim() }
                        : new PatientScore { ScoreText = entry.Trim() };
                }).ToList());

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
                OnPropertyChanged(nameof(CanEditAppointment));
            }
        }, "Грешка при зачувување на податоци");
    }

    [RelayCommand]
    private async Task Cancel()
    {
        // In read-only detail mode, the header action is navigation—not a form cancel.
        // Keep the current page when cancelling edits, but return to the previous page
        // when the user is simply viewing an existing appointment.
        if(!_isNewAppointmentMode && IsReadOnly)
        {
            await NavigationService.GoToAsync("..");
            return;
        }

        var confirmed = await UserDialogService.ShowConfirmationAsync(
            "Откажи",
            "Дали сте сигурни дека сакате да се вратите назад без да ги зачувате промените?",
            "Да",
            "Не");
        if(!confirmed) return;

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
                SelectedStartTime=Appointment.ScheduledStart.TimeOfDay;
                SelectedEndTime=Appointment.ScheduledEnd.TimeOfDay;
            }
            IsReadOnly=true;
            OnPropertyChanged(nameof(CanEditAppointment));
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
            ScheduledStart=source.ScheduledStart,
            ScheduledEnd=source.ScheduledEnd,
            Status=source.Status,
            ReasonForVisit=source.ReasonForVisit,
            ClinicalNotes=source.ClinicalNotes,
            CreatedAt=source.CreatedAt,
            Patient=source.Patient,
            Doctor=source.Doctor
        };
    }
}

public sealed record AppointmentStatusChoice(AppointmentStatus Value, string Label)
{
    public override string ToString() => Label;
}