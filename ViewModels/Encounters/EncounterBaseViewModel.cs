using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Services;
using EHMR.ViewModels.Constants;
using EHMR.ViewModels.Patients.Extensions;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Encounters;

public abstract partial class EncounterBaseViewModel : ObservableObject, IDisposable
{
    protected readonly IEncounterDetailService EncounterService;

    protected readonly INavigationService NavigationService;
    protected readonly IUserDialogService UserDialogService;

    protected CancellationTokenSource SearchCts = new();

    private bool _isInitializing;

    // NOVO: guard за flow-от кога контекстот (appointment -> patient/doctor/therapy-cycle)
    // се применува програмски. Без ова, поставувањето на SelectedPatient/SelectedAppointment
    // внатре во ApplyAppointmentContextAsync тригерира reset-cascade (OnSelectedPatientChanged
    // -> OnSelectedAppointmentChanged -> ClearEncounterContext) кој веднаш го брише истиот
    // AppointmentId/TherapyCycleId штотуку поставен неколку линии погоре.
    protected bool _isApplyingContext;

    // Guards against overlapping "no results -> offer to create" dialogs.
    // Without this, fast typing (or clicking "+ Нов ..." додека веќе имате отворено
    // дијалог за понуда) стака повеќе попапи еден врз друг. Откажувањето на врвниот
    // потоа остава друг во полузатворена состојба -> изгледа како "dispose didn't happen".
    private bool _isOfferingCycleCreation;
    private bool _isOfferingAppointmentCreation;

    // =====================================================
    // CONSTRUCTOR
    // =====================================================
    protected EncounterBaseViewModel(
        IEncounterDetailService encounterService,
        INavigationService navigationService,
        IUserDialogService userDialogService)
    {
        EncounterService=encounterService;
        NavigationService=navigationService;
        UserDialogService=userDialogService;

        for(var letter='A'; letter<='Z'; letter++)
            MkbAlphabetSections.Add(new MkbAlphabetSection(letter.ToString(), letter=='A'));
    }

    // =====================================================
    // CORE MODEL
    // =====================================================

    [ObservableProperty]
    protected Encounter encounter = new();

    [ObservableProperty]
    private string scoreText = string.Empty;

    [ObservableProperty]
    private PatientScore? score;

    [ObservableProperty]
    private string currentPatientScore = string.Empty;

    [ObservableProperty]
    private DateTime? currentPatientScoreDate;

    [ObservableProperty]
    private string pageTitle = string.Empty;

    private bool _isPatientLockedFromContext;
    public bool IsPatientLockedFromContext
    {
        get => _isPatientLockedFromContext;
        set => SetProperty(ref _isPatientLockedFromContext, value);
    }

    // =====================================================
    // UI STATE
    // =====================================================
    public enum AppointmentTabFilter
    {
        Upcoming, Pending, Past
    }
    public enum EncounterTabFilter
    {
        All, InProgress, Completed, Cancelled
    }
    public enum PrescriptionTabFilter
    {
        Active, All
    }
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private bool isReadOnly = true;

    [ObservableProperty]
    private bool hasError;
    [ObservableProperty]
    private string errorMessage = string.Empty;

    // =====================================================
    // ENCOUNTER CONTEXT
    // =====================================================

    [ObservableProperty]
    private Appointment? linkedAppointment;
    [ObservableProperty]
    private ObservableCollection<TherapyCycle> availableTherapyCycles = new();
    [ObservableProperty]
    private TherapyCycle? selectedTherapyCycle;
    public bool HasAppointment =>
        LinkedAppointment!=null;
    public bool HasTherapyCycle =>
        SelectedTherapyCycle!=null;
    public bool IsWalkIn =>
        !HasAppointment;
    public bool HasEncounterContext =>
        HasAppointment||HasTherapyCycle;
    public string ContextDisplay
    {
        get
        {
            if(HasAppointment)
                return "Appointment";

            if(HasTherapyCycle)
                return "Therapy Cycle";

            return "Walk-in";
        }
    }
    public string TherapyCycleDisplay =>
        SelectedTherapyCycle?.Notes
        ??"Без терапевтски циклус";
    public string TherapyCycleStatus =>
        SelectedTherapyCycle is null
            ? string.Empty
            : SelectedTherapyCycle.Status?.ToDisplay() ?? string.Empty;

    // Mirrors TherapyCycleDisplay - drives the "selected appointment" label
    // under the appointment search box in the UI.
    public string AppointmentDisplay =>
        LinkedAppointment==null
            ? "Без термин"
            : $"{LinkedAppointment.ScheduledStart:dd.MM.yyyy HH:mm}";

    partial void OnLinkedAppointmentChanged(Appointment? value)
    {
        OnPropertyChanged(nameof(HasAppointment));
        OnPropertyChanged(nameof(IsWalkIn));
        OnPropertyChanged(nameof(HasEncounterContext));
        OnPropertyChanged(nameof(ContextDisplay));
        OnPropertyChanged(nameof(HasPatientContext));
        OnPropertyChanged(nameof(AppointmentDisplay));
    }
    partial void OnSelectedTherapyCycleChanged(TherapyCycle? value)
    {
        Encounter.TherapyCycleId=value?.Id;

        OnPropertyChanged(nameof(HasTherapyCycle));
        OnPropertyChanged(nameof(HasEncounterContext));
        OnPropertyChanged(nameof(ContextDisplay));
        OnPropertyChanged(nameof(TherapyCycleDisplay)); OnPropertyChanged(nameof(HasPatientContext));
        OnPropertyChanged(nameof(TherapyCycleStatus));
    }
    // =====================================================
    // PATIENT CONTEXT
    // =====================================================
    [ObservableProperty]
    private ObservableCollection<PatientMedicine> patientDiagnosisHistory = new();

    [ObservableProperty]
    private ObservableCollection<Encounter> patientEncounterHistory = new();

    [ObservableProperty]
    private ObservableCollection<Prescription> patientActivePrescriptions = new();

    [ObservableProperty]
    private bool isPatientContextLoading;

    public bool HasPatientContext => SelectedPatient!=null;

    //LoadPatientContext 
    // ===================== RAW PATIENT CONTEXT (непроменето од service) =====================
    [ObservableProperty] private ObservableCollection<Diagnosis> patientDiagnoses = new();
    [ObservableProperty] private ObservableCollection<Encounter> patientEncounters = new();
    [ObservableProperty] private ObservableCollection<Appointment> patientAppointments = new();
    [ObservableProperty] private ObservableCollection<TherapyCycle> patientTherapyCyclesHistory = new();
    [ObservableProperty] private ObservableCollection<Prescription> patientPrescriptions = new();
    [ObservableProperty] private ObservableCollection<PatientMedicine> patientMedicines = new();
    [ObservableProperty] private ObservableCollection<PatientDocument> patientDocuments = new();

    [ObservableProperty] private bool isPatientLoading;



    // ===================== TAB STATE =====================
    [ObservableProperty] private AppointmentTabFilter appointmentTab = AppointmentTabFilter.Upcoming;
    [ObservableProperty] private EncounterTabFilter encounterTab = EncounterTabFilter.All;
    [ObservableProperty] private PrescriptionTabFilter prescriptionTab = PrescriptionTabFilter.Active;
    partial void OnEncounterChanging(Encounter? value)
    {
        OnPropertyChanged(nameof(EncounterStatusDisplay));
    }

    public string EncounterStatusDisplay =>
        EncounterStatusSchema.ToDisplay(Encounter?.Status.ToString()??string.Empty);
    partial void OnAppointmentTabChanged(AppointmentTabFilter value) => OnPropertyChanged(nameof(FilteredAppointments));
    partial void OnEncounterTabChanged(EncounterTabFilter value) => OnPropertyChanged(nameof(FilteredEncounters));

    partial void OnPrescriptionTabChanged(PrescriptionTabFilter value) => OnPropertyChanged(nameof(FilteredPrescriptions));

    // ===================== FILTERED (COMPUTED) VIEWS =====================
    [ObservableProperty]
    private ObservableCollection<ApplicationRegime> applicationRegimeOptions = new();

    public string CurrentDiagnosesSummary => string.Join(", ", PatientDiagnoses
        .Where(x => x.Mkb10Code!=null)
        .Select(x => $"{x.Mkb10Code!.Code} - {x.Mkb10Code.Description}")
        .Take(3));

    public string ActiveMedicinesSummary => string.Join(", ", PatientMedicines
        .Where(x => x.IsActive)
        .Select(x => x.Medicine?.Name ?? "Лек")
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(4));

    public string ActiveTherapySummary => string.Join(", ", PatientTherapyCyclesHistory
        .Where(x => x.Status==TherapyStatus.Active || x.Status==TherapyStatus.Planned)
        .Select(x => string.IsNullOrWhiteSpace(x.DecisionText) ? (x.Notes ?? "Терапија") : x.DecisionText)
        .Take(2));

    public IEnumerable<Appointment> FilteredAppointments => AppointmentTab switch
    {
        AppointmentTabFilter.Upcoming => PatientAppointments
            .Where(a => a.Status==AppointmentStatus.Scheduled&&a.ScheduledStart>DateTime.Now)
            .OrderBy(a => a.ScheduledStart),

        AppointmentTabFilter.Pending => PatientAppointments
            .Where(a => a.Status==AppointmentStatus.InProgress||a.Status==AppointmentStatus.InProgress)
            .OrderBy(a => a.ScheduledStart),

        AppointmentTabFilter.Past => PatientAppointments
            .OrderByDescending(a => a.ScheduledStart),

        _ => PatientAppointments
    };

    public IEnumerable<Encounter> FilteredEncounters => EncounterTab switch
    {
        EncounterTabFilter.InProgress => PatientEncounters.Where(e => e.Status==EncounterStatus.InProgress),
        EncounterTabFilter.Completed => PatientEncounters.Where(e => e.Status==EncounterStatus.Completed),
        EncounterTabFilter.Cancelled => PatientEncounters.Where(e => e.Status==EncounterStatus.Cancelled),
        _ => PatientEncounters
    };

    public IEnumerable<Prescription> FilteredPrescriptions => PrescriptionTab switch
    {
        // Ако во базата се чува како "Active", тука правиме соодветна проверка
        PrescriptionTabFilter.Active => PatientPrescriptions.Where(p => p.Status=="Active"||p.Status=="Активни"),
        _ => PatientPrescriptions
    };

    [RelayCommand] private void SetAppointmentTab(AppointmentTabFilter tab) => AppointmentTab=tab;
    [RelayCommand] private void SetEncounterTab(EncounterTabFilter tab) => EncounterTab=tab;
    [RelayCommand] private void SetPrescriptionTab(PrescriptionTabFilter tab) => PrescriptionTab=tab;

    // ===================== LOADER =====================
    protected async Task LoadPatientContextAsync(Guid patientId)
    {
        if(patientId==Guid.Empty)
        {
            PatientDiagnoses.Clear();
            PatientEncounters.Clear();
            PatientAppointments.Clear();
            PatientTherapyCyclesHistory.Clear();
            PatientPrescriptions.Clear();
            PatientMedicines.Clear();
            PatientDocuments.Clear();
            return;
        }

        IsPatientLoading=true;
        try
        {
            var ctx = await EncounterService.GetPatientContext(patientId);

            PatientDiagnoses=new ObservableCollection<Diagnosis>(ctx.Diagnoses);
            PatientEncounters=new ObservableCollection<Encounter>(ctx.EncounterHistory);
            PatientAppointments=new ObservableCollection<Appointment>(ctx.Appointments);
            PatientTherapyCyclesHistory=new ObservableCollection<TherapyCycle>(ctx.TherapyCycles);
            PatientPrescriptions=new ObservableCollection<Prescription>(ctx.Prescriptions);
            PatientMedicines=new ObservableCollection<PatientMedicine>(ctx.PatientMedicines);
            ApplicationRegimeOptions=new ObservableCollection<ApplicationRegime>(
                await EncounterService.GetApplicationRegimesAsync());
            CurrentPatientScore=ctx.LatestScore?.ScoreText??string.Empty;
            CurrentPatientScoreDate=ctx.LatestScore?.RecordedAt;
            PatientDocuments=new ObservableCollection<PatientDocument>(ctx.Documents);

            OnPropertyChanged(nameof(FilteredAppointments));
            OnPropertyChanged(nameof(FilteredEncounters));
            OnPropertyChanged(nameof(FilteredPrescriptions));
            OnPropertyChanged(nameof(CurrentDiagnosesSummary));
            OnPropertyChanged(nameof(ActiveMedicinesSummary));
            OnPropertyChanged(nameof(ActiveTherapySummary));
            OnPropertyChanged(nameof(HasPatientContext));
            OnPropertyChanged(nameof(HasEncounterContext));
        }
        finally
        {
            IsPatientContextLoading=false;
            IsPatientLoading=false;
        }
    }

    protected async Task LoadTherapyCyclesForPatientAsync(Guid patientId)
    {
        if(patientId==Guid.Empty)
        {
            AvailableTherapyCycles.Clear();
            SelectedTherapyCycle=null;
            return;
        }
        var context = await EncounterService.GetPatientContext(patientId);
        AvailableTherapyCycles=
            new ObservableCollection<TherapyCycle>(
                context.TherapyCycles
                    .Where(x =>
                        x.Status==TherapyStatus.Active||
                        x.Status==TherapyStatus.Planned)
            );
        SelectedTherapyCycle=
            AvailableTherapyCycles
                .FirstOrDefault(x =>
                    x.Id==Encounter.TherapyCycleId);
    }
    protected async Task ApplyAppointmentContextAsync(Appointment appointment)
    {
        // Guard-от го блокира reset-cascade-от во OnSelectedPatientChanged /
        // OnSelectedAppointmentChanged додека сите полиња на контекстот се
        // применуваат. Ова е фикс за bug-от кадешто SelectedPatient/SelectedDoctor
        // assignment-ите веднаш го бришеа LinkedAppointment/AppointmentId/TherapyCycleId
        // штотуку поставени неколку линии погоре.
        _isApplyingContext=true;
        try
        {
            LinkedAppointment=appointment;
            Encounter.AppointmentId=appointment.Id;
            Encounter.PatientId=appointment.PatientId;
            Encounter.DoctorId=appointment.DoctorId;
            Encounter.TherapyCycleId=appointment.TherapyCycleId;
            Encounter.ReasonForVisit=null;

            SelectedPatient=
                Patients.FirstOrDefault(x =>
                    x.Id==appointment.PatientId);
            SelectedDoctor=
                Doctors.FirstOrDefault(x =>
                    x.Id==appointment.DoctorId);

            // Експлицитно ги вчитуваме овие тука (наместо да се потпираме на
            // fire-and-forget-от од OnSelectedPatientChanged, кој е блокиран
            // додека guard-от е активен) - вака страничните карти (дијагнози,
            // историја, циклуси, термини) веднаш имаат податоци за пациентот.
            await LoadPatientContextAsync(appointment.PatientId);
            await LoadTherapyCyclesForPatientAsync(appointment.PatientId);
            await LoadAppointmentsForPatientAsync(appointment.PatientId);
        }
        finally
        {
            _isApplyingContext=false;
        }
    }
    partial void OnSelectedPatientChanged(Patient? value)
    {
        Encounter.PatientId=
            value?.Id??Guid.Empty;
        OnPropertyChanged(nameof(HasPatientContext));
        OnPropertyChanged(nameof(HasEncounterContext));
        if(_isInitializing||_isApplyingContext)
            return;
        Diagnoses.Clear();

        SelectedTherapyCycle=null;

        LinkedAppointment=null;

        SelectedAppointment=null;

        // Reset any in-flight search UI state when the patient changes,
        // otherwise stale dropdown results from the previous patient
        // can remain visible for a moment.
        CycleSearchText=string.Empty;
        CycleSearchResults.Clear();
        ShowCycleDropdown=false;

        AppointmentSearchText=string.Empty;
        AppointmentSearchResults.Clear();
        ShowAppointmentDropdown=false;

        var patientId = value?.Id??Guid.Empty;

        _=LoadTherapyCyclesForPatientAsync(patientId);
        _=LoadAppointmentsForPatientAsync(patientId);
        _=LoadPatientContextAsync(
                 patientId);
    }

    // =====================================================
    // APPOINTMENT LINKING
    // =====================================================

    [ObservableProperty]
    private ObservableCollection<Appointment> availableAppointments = new();
    [ObservableProperty]
    private Appointment? selectedAppointment;
    partial void OnSelectedAppointmentChanged(Appointment? value)
    {
        if(_isInitializing||_isApplyingContext)
            return;
        if(value==null)
        {
            ClearEncounterContext();
            return;
        }
        _=ApplyAppointmentContextAsync(value);
    }
    protected async Task LoadAppointmentsForPatientAsync(Guid patientId)
    {
        if(patientId==Guid.Empty)
        {
            AvailableAppointments.Clear();
            SelectedAppointment=null;
            return;
        }
        var context = await EncounterService.GetAppointments(patientId);
        AvailableAppointments=
            new ObservableCollection<Appointment>(
                context
                    .Where(x =>
                        x.Status==AppointmentStatus.Scheduled||
                        x.Status==AppointmentStatus.InProgress)
            );
        SelectedAppointment=
            AvailableAppointments
                .FirstOrDefault(x =>
                    x.Id==Encounter.AppointmentId);
    }
    [RelayCommand]
    protected async Task AddNewAppointmentAsync()
    {
        if(SelectedPatient?.Status==PatientStatus.Inactive)
        {
            await UserDialogService.ShowAlertAsync("Пациентот е неактивен", "За неактивен пациент не може да се креира или менува термин.", "ОК");
            return;
        }

        if(SelectedPatient==null)
        {
            OnError("Изберете пациент пред да додадете термин");
            return;
        }

        if(_isOfferingAppointmentCreation)
            return;

        await ExecuteSafeAsync(async () =>
        {
            _isOfferingAppointmentCreation=true;
            try
            {
                var newAppointment =
                    await UserDialogService
                        .ShowCreateAppointmentPopupAsync(
                            SelectedPatient.Id,
                            SelectedDoctor?.Id);
                if(newAppointment==null)
                    return;
                AvailableAppointments.Add(newAppointment);

                SelectedAppointment=newAppointment;
            }
            finally
            {
                _isOfferingAppointmentCreation=false;
            }
        },
        "Грешка при креирање термин");
    }

    // =====================================================
    // APPOINTMENT SEARCH (mirrors THERAPY CYCLE SEARCH)
    // =====================================================

    protected CancellationTokenSource AppointmentSearchCts = new();

    [ObservableProperty]
    protected ObservableCollection<Appointment> appointmentSearchResults = new();

    [ObservableProperty]
    protected string appointmentSearchText = string.Empty;

    [ObservableProperty]
    protected bool showAppointmentDropdown;

    partial void OnAppointmentSearchTextChanging(string value)
    {
        _=SearchAppointmentsAsync(value);
    }

    [RelayCommand]
    protected async Task SearchAppointmentsAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query))
        {
            AppointmentSearchResults.Clear();
            ShowAppointmentDropdown=false;
            return;
        }

        AppointmentSearchCts.Cancel();
        AppointmentSearchCts.Dispose();
        AppointmentSearchCts=new CancellationTokenSource();

        var token = AppointmentSearchCts.Token;
        try
        {
            await Task.Delay(300, token); // debounce
        }
        catch(TaskCanceledException)
        {
            return;
        }

        var matches =
            AvailableAppointments
                .Where(x =>
                    (x.ClinicalNotes??string.Empty)
                        .Contains(query, StringComparison.OrdinalIgnoreCase)
                    ||
                    x.ScheduledStart
                        .ToString("dd.MM.yyyy HH:mm")
                        .Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        AppointmentSearchResults=
            new ObservableCollection<Appointment>(matches);

        ShowAppointmentDropdown=
            matches.Count>0;

        if(matches.Count==0&&!token.IsCancellationRequested)
        {
            await OfferToCreateAppointmentAsync(query);
        }
    }

    protected async Task OfferToCreateAppointmentAsync(string searchedTerm)
    {
        if(SelectedPatient?.Status==PatientStatus.Inactive)
            return;

        if(SelectedPatient==null)
            return;

        // Сличен како горниот - спречува преклопување на попапи
        if(_isOfferingAppointmentCreation)
            return;

        _isOfferingAppointmentCreation=true;
        try
        {
            var shouldCreate =
                await UserDialogService.ShowConfirmationAsync(
                    "Нема резултати",
                    $"Не е пронајден термин за „{searchedTerm}“. Дали сакате да креирате нов?",
                    "Креирај",
                    "Откажи");
            if(!shouldCreate)
                return;

            var newAppointment =
                await UserDialogService
                    .ShowCreateAppointmentPopupAsync(
                        SelectedPatient.Id,
                        SelectedDoctor?.Id);
            if(newAppointment==null)
                return;

            AvailableAppointments.Add(newAppointment);

            SelectedAppointment=newAppointment;
            AppointmentSearchText=string.Empty;

            AppointmentSearchResults.Clear();

            ShowAppointmentDropdown=false;
        }
        finally
        {
            _isOfferingAppointmentCreation=false;
        }
    }

    [RelayCommand]
    protected void SelectAppointment(Appointment appointment)
    {
        if(appointment==null)
            return;

        SelectedAppointment=appointment; // triggers ApplyAppointmentContextAsync via OnSelectedAppointmentChanged

        AppointmentSearchText=string.Empty;
        AppointmentSearchResults.Clear();
        ShowAppointmentDropdown=false;
    }

    // =====================================================
    // THERAPY CYCLE SEARCH + CREATE (optional)
    // =====================================================

    protected CancellationTokenSource CycleSearchCts = new();
    [ObservableProperty]
    protected ObservableCollection<TherapyCycle> cycleSearchResults = new();
    [ObservableProperty]
    protected string cycleSearchText = string.Empty;
    [ObservableProperty]
    protected bool showCycleDropdown;
    partial void OnCycleSearchTextChanging(string value)
    {
        _=SearchTherapyCyclesAsync(value);
    }
    [RelayCommand]
    protected async Task SearchTherapyCyclesAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query))
        {
            CycleSearchResults.Clear();
            ShowCycleDropdown=false;
            return;
        }
        CycleSearchCts.Cancel();
        CycleSearchCts.Dispose();
        CycleSearchCts=new CancellationTokenSource();

        var token = CycleSearchCts.Token;
        try
        {
            await Task.Delay(350, token); // debounce, mirrors MKB search feel
        }
        catch(TaskCanceledException)
        {
            return;
        }
        var matches =
            AvailableTherapyCycles
                .Where(x =>
                    (x.Notes??string.Empty)
                        .Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        CycleSearchResults=
            new ObservableCollection<TherapyCycle>(matches);

        ShowCycleDropdown=
            matches.Count>0;

        if(matches.Count==0&&!token.IsCancellationRequested)
        {
            await OfferToCreateTherapyCycleAsync(query);
        }
    }
    protected async Task OfferToCreateTherapyCycleAsync(string searchedTerm)
    {
        if(SelectedPatient?.Status==PatientStatus.Inactive)
            return;

        if(SelectedPatient==null)
            return;

        // Re-entrancy guard: без ова, брзо типување (секој клучен удар
        // повторно го активира дебонсираното пребарување) или кликање на "+ Нов циклус"
        // додека типуваната понуда за дијалог сѐ уште е отворена може да создаде
        // повеќе од еден popup. Откажувањето на едниот потоаостаава дека другото
        // popup's RequestClose укажува на дијалог што веќе е "употребено" - изгледа како
        // popup-от не се затвора/раскинува правилно.
        if(_isOfferingCycleCreation)
            return;

        _isOfferingCycleCreation=true;
        try
        {
            var shouldCreate =
                await UserDialogService.ShowConfirmationAsync(
                    "Нема резултати",
                    $"Не е пронајден терапевтски циклус за „{searchedTerm}“. Дали сакате да креирате нов?",
                    "Креирај",
                    "Откажи");
            if(!shouldCreate)
                return;

            var newCycle =
                await UserDialogService
                    .ShowCreateTherapyCyclePopupAsync(
                        SelectedPatient.Id,
                        searchedTerm);
            if(newCycle==null)
                return;

            AvailableTherapyCycles.Add(newCycle);

            SelectedTherapyCycle=newCycle;
            CycleSearchText=string.Empty;

            CycleSearchResults.Clear();

            ShowCycleDropdown=false;
        }
        finally
        {
            _isOfferingCycleCreation=false;
        }
    }
    [RelayCommand]
    protected async Task AddNewTherapyCycleAsync()
    {
        if(SelectedPatient?.Status==PatientStatus.Inactive)
        {
            await UserDialogService.ShowAlertAsync("Пациентот е неактивен", "За неактивен пациент не може да се креира или менува терапија.", "ОК");
            return;
        }

        if(SelectedPatient==null)
        {
            OnError("Изберете пациент пред да додадете циклус");
            return;
        }

        // Исто така, заштита тука - спречува рачниот копче "+ Нов циклус"
        // да отвори второ креирано popup додека дијалогот предложен од пребарувањето
        // веќе се прикажува.
        if(_isOfferingCycleCreation)
            return;

        await ExecuteSafeAsync(async () =>
        {
            _isOfferingCycleCreation=true;
            try
            {
                var newCycle =
                    await UserDialogService
                        .ShowCreateTherapyCyclePopupAsync(
                            SelectedPatient.Id,
                            null);
                if(newCycle==null)
                    return;
                AvailableTherapyCycles.Add(newCycle);

                SelectedTherapyCycle=newCycle;
            }
            finally
            {
                _isOfferingCycleCreation=false;
            }
        },
        "Грешка при креирање циклус");
    }
    [RelayCommand]
    protected void SelectTherapyCycle(TherapyCycle cycle)
    {
        if(cycle==null)
            return;
        SelectedTherapyCycle=cycle;

        CycleSearchText=string.Empty;

        CycleSearchResults.Clear();

        ShowCycleDropdown=false;
    }

    // =====================================================
    // NAVIGATION
    // =====================================================

    [RelayCommand]
    protected virtual async Task CancelAsync()
    {
        await NavigationService.GoToAsync(
            AppRoutes.Encounters.List);
    }
    // =====================================================
    // ERROR HANDLING
    // =====================================================

    protected void OnError(string message)
    {
        ErrorMessage=message;
        HasError=true;
    }
    protected void ClearError()
    {
        ErrorMessage=string.Empty;
        HasError=false;
    }
    // =====================================================
    // SAFE EXECUTION
    // =====================================================

    protected async Task ExecuteSafeAsync(
        Func<Task> action,
        string errorMessage)
    {
        if(IsBusy)
            return;
        try
        {
            IsBusy=true;
            ClearError();

            await action();
        }
        catch(Exception ex)
        {
            OnError(
                $"{errorMessage}: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }    // =====================================================
    // LOOKUPS
    // =====================================================

    [ObservableProperty]
    protected ObservableCollection<Patient> patients = new();
    [ObservableProperty]
    protected ObservableCollection<Doctor> doctors = new();
    protected List<string> PatientList { get; set; } = new();

    protected List<string> DoctorList { get; set; } = new();
    [ObservableProperty]
    protected Patient? selectedPatient;
    [ObservableProperty]
    protected Doctor? selectedDoctor;
    partial void OnSelectedDoctorChanged(Doctor? value)
    {
        if(Encounter==null)
            return;
        Encounter.DoctorId=
            value?.Id??Guid.Empty;
    }
    // =====================================================
    // ENCOUNTER LOOKUP DISPLAY
    // =====================================================

    public ObservableCollection<string> EncounterTypeOptions =>
        EncounterFormLookups
            .EncounterType
            .ToObservableCollection();
    public ObservableCollection<string> PriorityOptions =>
        EncounterFormLookups
            .Priority
            .ToObservableCollection();
    public ObservableCollection<string> StatusOptions =>
        EncounterStatusSchema
            .Values
            .ToObservableCollection();
    public string EncounterTypeDisplay
    {
        get => EncounterFormLookups.EncounterType.ToDisplay(Encounter.EncounterType);
        set
        {
            var val = EncounterFormLookups.EncounterType.ToInternal(value);
            if(Encounter.EncounterType==val)
                return;
            Encounter.EncounterType=val;   // ← fixed: store the internal key
            OnPropertyChanged();
        }
    }

    public string PriorityDisplay
    {
        get => EncounterFormLookups.Priority.ToDisplay(Encounter.Priority);
        set
        {
            var val = EncounterFormLookups.Priority.ToInternal(value);
            if(Encounter.Priority==val)
                return;
            Encounter.Priority=val;   // ← fixed
            OnPropertyChanged();
        }
    }
    public string StatusDisplay
    {
        get =>
            EncounterStatusSchema
                .ToDisplay(
                    Encounter.Status.ToString());
        set
        {
            var key =
                EncounterStatusSchema
                    .ToKeyFromDisplay(value);
            if(Enum.TryParse<EncounterStatus>(
                    key,
                    out var status)
                &&
                Encounter.Status!=status)
            {
                Encounter.Status=status;
                OnPropertyChanged();
            }
        }
    }
    // =====================================================
    // SCHEDULE
    // =====================================================
    public DateTime ScheduledStartDate
    {
        get =>
            Encounter.ScheduledStart
            ?.Date
            ??DateTime.Today;
        set
        {
            var time =
                Encounter.ScheduledStart
                ?.TimeOfDay
                ??TimeSpan.Zero;
            Encounter.ScheduledStart=
                value.Date+time;
            OnPropertyChanged();
        }
    }
    public TimeSpan ScheduledStartTime
    {
        get =>
            Encounter.ScheduledStart
            ?.TimeOfDay
            ??new TimeSpan(9, 0, 0);
        set
        {
            var date =
                Encounter.ScheduledStart
                ?.Date
                ??DateTime.Today;
            Encounter.ScheduledStart=
                date+value;
            OnPropertyChanged();
        }
    }
    // =====================================================
    // INITIALIZATION
    // =====================================================
    //  public List<string> EncounterTypeOptions = new List<string>();
    public virtual async Task InitializeAsync(Guid? encounterId = null)
    {
        await ExecuteSafeAsync(async () =>
        {
            _isInitializing=true;
            try
            {
                await LoadLookupsAsync();
                await SearchMkbAsync(string.Empty);

                // ── NEW ENCOUNTER — early exit, no DB call needed ────────────
                if(encounterId==null||encounterId==Guid.Empty)
                {
                    Encounter=new Encounter
                    {
                        Id=Guid.NewGuid(),
                        EncounterDate=DateTime.Now,
                        CreatedAt=DateTime.UtcNow,
                    };
                    Diagnoses.Clear();
                    Prescriptions.Clear();
                    EncounterMedicines.Clear();
                    IsEditMode=true;
                    IsReadOnly=false;
                    return;  // ← was missing: code fell through and called .Value on null
                }

                // ── EXISTING ENCOUNTER ────────────────────────────────────────
                var dto = await EncounterService.GetEncounter(encounterId.Value);
                Encounter=dto.Encounter;

                // Display value conversions
                if(!string.IsNullOrEmpty(Encounter.EncounterType))
                {
                    var display = EncounterFormLookups.EncounterType.DisplayValues
                        .FirstOrDefault(dv => EncounterFormLookups.EncounterType
                            .ToInternal(dv)
                            .Equals(Encounter.EncounterType, StringComparison.OrdinalIgnoreCase));
                    if(!string.IsNullOrEmpty(display))
                        EncounterTypeDisplay=display;   // ← re-invokes the buggy setter, re-corrupting the value
                }

                if(!string.IsNullOrEmpty(Encounter.Priority))
                {
                    var display = EncounterFormLookups.Priority.DisplayValues
                        .FirstOrDefault(dv => EncounterFormLookups.Priority
                            .ToInternal(dv)
                            .Equals(Encounter.Priority, StringComparison.OrdinalIgnoreCase));
                    if(!string.IsNullOrEmpty(display))
                        PriorityDisplay=display;
                }

                Diagnoses=new ObservableCollection<Diagnosis>(dto.Diagnoses);
                Prescriptions=new ObservableCollection<Prescription>(dto.Prescriptions);
                EncounterDiagnosisNotes=Encounter.ClinicalNotes??string.Empty;
                Score=dto.Score;
                ScoreText=dto.Score?.ScoreText??string.Empty;

                SelectedPatient=Patients.FirstOrDefault(x => x.Id==Encounter.PatientId);
                SelectedDoctor=Doctors.FirstOrDefault(x => x.Id==Encounter.DoctorId);

                if(Encounter.AppointmentId.HasValue)
                    LinkedAppointment=await EncounterService
                        .GetAppointment(Encounter.AppointmentId.Value);

                await LoadTherapyCyclesForPatientAsync(Encounter.PatientId);
                await LoadPatientContextAsync(Encounter.PatientId);
                await LoadAppointmentsForPatientAsync(Encounter.PatientId);

                // Seed so RemoveMedicine knows what existed at load time
                foreach(var pm in PatientMedicines)
                    _originalPatientMedicineIds.Add(pm.Id);

                IsEditMode=false;
                IsReadOnly=true;
            }
            finally
            {
                _isInitializing=false;
            }
        }, "Грешка при вчитување на прегледот");
    }
    // =====================================================
    // LOAD LOOKUPS
    // =====================================================

    protected async Task LoadLookupsAsync()
    {
        var patients =
            await EncounterService
                .GetPatients();
        Patients=
            new ObservableCollection<Patient>(
                patients);
        PatientList=
            Patients
                .Select(x => x.FullName)
                .ToList();
        var doctors =
            await EncounterService
                .GetDoctors();
        Doctors=
            new ObservableCollection<Doctor>(
                doctors);
        DoctorList=
            Doctors
                .Select(x => x.FullName)
                .ToList();
    }
    // =====================================================
    // LOAD EXISTING VIEW MODE
    // =====================================================

    protected async Task LoadForViewAsync(
        Guid? selectedId,
        string error)
    {
        if(selectedId==null||
            selectedId==Guid.Empty)
        {
            OnError(error);
            return;
        }
        await InitializeAsync(
            selectedId.Value);
        IsEditMode=false;
        IsReadOnly=true;
    }    // =====================================================
    // DIAGNOSIS / MKB10
    // =====================================================

    public ObservableCollection<MkbAlphabetSection> MkbAlphabetSections { get; } = new();

    [ObservableProperty]
    protected string selectedMkbSection = "A";

    [ObservableProperty]
    protected ObservableCollection<Mkb10Code> mkbResults = new();
    [ObservableProperty]
    protected ObservableCollection<Diagnosis> diagnoses = new();
    [ObservableProperty]
    protected string mkbCodeSearchText = string.Empty;

    [ObservableProperty]
    protected string mkbDescriptionSearchText = string.Empty;

    [ObservableProperty]
    protected bool showMkbDropdown;

    [ObservableProperty]
    protected ObservableCollection<Prescription> prescriptions = new();

    [ObservableProperty]
    private string encounterDiagnosisNotes = string.Empty;
    partial void OnMkbCodeSearchTextChanged(string value) =>
        _=SearchMkbAsync(value??string.Empty);

    partial void OnMkbDescriptionSearchTextChanged(string value) =>
        _=SearchMkbAsync(value??string.Empty);

    [RelayCommand]
    protected async Task SelectMkbSectionAsync(MkbAlphabetSection section)
    {
        if(section==null)
            return;

        SelectedMkbSection=section.Letter;
        foreach(var item in MkbAlphabetSections)
            item.IsSelected=item.Letter==SelectedMkbSection;

        await SearchMkbAsync(string.Empty);
    }

    // =====================================================
    // SEARCH MKB — секогаш ограничено на избраната A-Z секција
    // =====================================================

    [RelayCommand]
    protected async Task SearchMkbAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(SelectedMkbSection))
        {
            MkbResults.Clear();
            ShowMkbDropdown=false;
            return;
        }

        SearchCts.Cancel();
        SearchCts.Dispose();
        SearchCts=new CancellationTokenSource();

        await ExecuteSafeAsync(async () =>
        {
            try
            {
                var result=await EncounterService.SearchDiagnoses(
                    MkbCodeSearchText,
                    SearchCts.Token,
                    SelectedMkbSection,
                    MacedonianTransliterator.ToCyrillic(MkbDescriptionSearchText));

                MkbResults=new ObservableCollection<Mkb10Code>(result);
                ShowMkbDropdown=MkbResults.Count>0;
            }
            catch(OperationCanceledException)
            {
                // Корисникот продолжил со пребарување или избрал друга секција.
            }
        }, "Грешка при пребарување дијагнози");
    }
    // =====================================================
    // ADD DIAGNOSIS
    // =====================================================

    [RelayCommand]
    protected void AddMkb(Mkb10Code code)
    {
        if(code==null)
            return;
        if(Diagnoses.Any(x =>
            x.Mkb10CodeId==code.Id))
            return;
        var diagnosis = new Diagnosis
        {
            Id=Guid.NewGuid(),

            EncounterId=
                Encounter.Id==Guid.Empty
                ? null
                : Encounter.Id,
            PatientId=
                Encounter.PatientId,
            Mkb10CodeId=code.Id,

            Mkb10Code=code,
            DiagnosedAt=DateTime.Now,
            IsPrimary=
                Diagnoses.Count==0,
            Status=
                DiagnosisStatus.Active
        };
        Diagnoses.Add(diagnosis);
        MkbResults.Remove(code);
        ShowMkbDropdown=MkbResults.Count>0;
    }

    // =====================================================
    // REMOVE DIAGNOSIS
    // =====================================================

    [RelayCommand]
    protected void RemoveMkb(Diagnosis diagnosis)
    {
        if(diagnosis==null)
            return;
        if(Diagnoses.Contains(diagnosis))
        {
            Diagnoses.Remove(diagnosis);
        }
    }
    // =====================================================
     // MEDICINE SEARCH + ATTACH (encounter-scoped, mirrors MKB10 diagnosis flow)
     // =====================================================
    protected CancellationTokenSource MedicineSearchCts = new();

    [ObservableProperty]
    protected ObservableCollection<Medicine> medicineResults = new();

    // Medicines being attached to THIS encounter (separate from PatientMedicines,
    // which is the read-only full history loaded via LoadPatientContextAsync)
    [ObservableProperty]
    protected ObservableCollection<PatientMedicine> encounterMedicines = new();

    [ObservableProperty]
    protected string medicineSearchText = string.Empty;

    [ObservableProperty]
    protected bool showMedicineDropdown;

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
    protected async Task SearchMedicinesAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query)||query.Length<2)
        {
            MedicineResults.Clear();
            ShowMedicineDropdown=false;
            return;
        }

        MedicineSearchCts.Cancel();
        MedicineSearchCts.Dispose();
        MedicineSearchCts=new CancellationTokenSource();

        await ExecuteSafeAsync(async () =>
        {
            try
            {
                var result = await EncounterService.SearchMedicines(query, MedicineSearchCts.Token);
                MedicineResults=new ObservableCollection<Medicine>(result);
                ShowMedicineDropdown=MedicineResults.Count>0;
            }
            catch(OperationCanceledException)
            {
                // user continued typing
            }
        },
        "Грешка при пребарување лекови");
    }

    // =====================================================
    // ADD MEDICINE — Dosage doubles as "quantity" (e.g. "1" tablet/dose)
    // =====================================================

    [RelayCommand]
    protected void AddMedicine(Medicine medicine)
    {
        if(medicine==null) return;

        if(EncounterMedicines.Any(x => x.MedicineId==medicine.Id&&x.IsActive))
            return;

        // A patient normally has one current primary therapy in this encounter form.
        // Selecting a different medicine replaces that current therapy; the removed
        // row is not deleted from history and is later marked inactive by the service.
        if(EncounterMedicines.Count==1)
        {
            var current=EncounterMedicines[0];
            if(current.MedicineId!=medicine.Id)
                RemoveMedicine(current);
        }

        var previous=PatientMedicines
            .FirstOrDefault(x => x.MedicineId==medicine.Id&&x.IsActive);

        var patientMedicine = new PatientMedicine
        {
            Id=Guid.NewGuid(),
            PatientId=Encounter.PatientId,
            EncounterId=Encounter.Id==Guid.Empty ? null : Encounter.Id,
            MedicineId=medicine.Id,
            Medicine=medicine,
            Dosage=previous?.Dosage??"1",
            ApplicationRegimeId=previous?.ApplicationRegimeId,
            ApplicationRegime=previous?.ApplicationRegime,
            Quantity=previous?.Quantity??1,
            DosesFrequency=previous?.DosesFrequency??DosesFrequency.Other,
            StartDate=DateTime.Now,
            IsActive=true
        };

        EncounterMedicines.Add(patientMedicine);

        MedicineSearchText=string.Empty;
        MedicineResults.Clear();
        ShowMedicineDropdown=false;
    }
    private readonly List<Guid> _deletedMedicineIds = [];
    protected IReadOnlyList<Guid> DeletedMedicineIds => _deletedMedicineIds;

    // =====================================================
    // REMOVE MEDICINE
    // =====================================================

    [RelayCommand]
    protected void RemoveMedicine(PatientMedicine medicine)
    {
        if(medicine==null) return;

        // Only mark for DB deletion if it's a real, already-persisted row.
        // A row added and removed within the same session (Id was just
        // Guid.NewGuid()'d in AddMedicine and never saved) doesn't need
        // a delete — it simply never gets sent to SaveEncounter.
        if(medicine.Id!=Guid.Empty&&PatientMedicinesHadIdBeforeThisSession(medicine.Id))
            _deletedMedicineIds.Add(medicine.Id);

        EncounterMedicines.Remove(medicine);
        PatientMedicines.Remove(medicine);
    }

    // Tracks ids that existed in PatientMedicines at load time (before any
    // AddMedicine calls this session), so RemoveMedicine can tell "existing
    // history row being deleted" apart from "session-added row being undone".
    private readonly HashSet<Guid> _originalPatientMedicineIds = [];

    private bool PatientMedicinesHadIdBeforeThisSession(Guid id) =>
        _originalPatientMedicineIds.Contains(id);
    // =====================================================
    // EDIT MODE
    // =====================================================

    [RelayCommand]
    public async Task ToggleEditMode()
    {
        if(!IsEditMode&&SelectedPatient?.Status==PatientStatus.Inactive)
        {
            await UserDialogService.ShowAlertAsync("Пациентот е неактивен", "Податоците за неактивен пациент се заклучени и не може да се менуваат.", "ОК");
            return;
        }

        IsEditMode=!IsEditMode;

        IsReadOnly=!IsEditMode;
    }
    // =====================================================
    // CONTEXT RESET
    // =====================================================

    protected void ClearEncounterContext()
    {
        LinkedAppointment=null;

        SelectedTherapyCycle=null;
        Encounter.AppointmentId=null;

        Encounter.TherapyCycleId=null;
    }
    // =====================================================
    // DISPOSE
    // =====================================================
    public void Dispose()
    {
        SearchCts.Cancel();
        SearchCts.Dispose();

        CycleSearchCts.Cancel();
        CycleSearchCts.Dispose();

        AppointmentSearchCts.Cancel();
        AppointmentSearchCts.Dispose();

        MedicineSearchCts.Cancel();
        MedicineSearchCts.Dispose();

        GC.SuppressFinalize(this);
    }
}
public partial class MkbAlphabetSection : ObservableObject
{
    public MkbAlphabetSection(string letter, bool isSelected)
    {
        Letter=letter;
        IsSelected=isSelected;
    }

    public string Letter { get; }

    [ObservableProperty]
    private bool isSelected;
}