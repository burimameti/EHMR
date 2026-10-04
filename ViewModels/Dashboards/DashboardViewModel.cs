using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Extensions;
using EHMR.Helpers;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using EHMR.Services;
using EHMR.ViewModels.Admin;
using EHMR.ViewModels.Dashboard.Models;
using EHMR.ViewModels.Dashboards.Models;
using EHMR.ViewModels.Patients.Extensions;
using Microsoft.EntityFrameworkCore;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using Microsoft.Maui;


namespace EHMR.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty] private DashboardState state = new();
    [ObservableProperty] private ObservableCollection<DashboardAppointmentItem> selectedDateAppointments = new();
    [ObservableProperty] private bool isDateCardExpanded = true;

    [RelayCommand]
    private void ToggleDateCard() => IsDateCardExpanded=!IsDateCardExpanded;

    // =========================================================
    // SERVICES / FIELDS
    // =========================================================
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAuthStateService _auth;
    private readonly ISelectedItemService<Patient> _selectedPatient;
    private readonly ISelectedItemService<Appointment> _selectedAppointment;
    private readonly ISelectedItemService<Encounter> _selectedEncounter;
    private readonly IUserDialogService _userDialogService;
    private readonly INavigationService navigationService;
    private readonly IAuthorizationService _policyService;
    private readonly IAlertService _alertService;
    private readonly IPreferencesService _preferencesService;
    private bool _isNavigating;
    private bool _isSelectingPatientSuggestion;
    private int _patientPreviewVersion;
    private const string LastAlertSweepKey = "alerts_last_sweep_date";
    private readonly FilterLookup _cityLookup = PatientFilterLookups.BuildCityLookup();

    // ─── FIX: do NOT use [ObservableProperty] + partial void OnSelectedDateChanged
    // together with a backing field that is set during init — MAUI's generated
    // setter skips the callback when the value equals the default.  Instead we
    // manage the field manually so we can always call RefreshForDate().
    private DateTime _selectedDate = DateTime.Today;
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if(_selectedDate==value) return;
            _selectedDate=value;
            OnPropertyChanged();
            OnSelectedDateChanged(value);
        }
    }

    [ObservableProperty] private bool showTabs = false;

    private const int PageSize = 5;
    private List<DashboardPatientAggregate> _allPatients = [];
    private List<DashboardPatientAggregate> _searchResults = [];

    private const string ChurnedStatusName = "Inactive";

    // =========================================================
    // CONSTRUCTOR
    // =========================================================
    public ICommand SearchCommand
    {
        get;
    }

    public DashboardViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ISelectedItemService<Patient> selectedPatient,
        ISelectedItemService<Appointment> selectedAppointment,
        ISelectedItemService<Encounter> selectedEncounter,
        IUserDialogService userDialogService,
        IAuthStateService auth,
        INavigationService navigationService,
        MenuViewModel menu,
        IAuthorizationService policyService,
        IAlertService alertService,
        IPreferencesService preferencesService)
    {
        _dbFactory=dbFactory;
        _auth=auth;
        _selectedPatient=selectedPatient;
        _selectedAppointment=selectedAppointment;
        _selectedEncounter=selectedEncounter;
        _userDialogService=userDialogService;
        this.navigationService=navigationService;
        _alertService=alertService;
        _preferencesService=preferencesService;
        Menu=menu;
        _policyService=policyService;
        CityFilterNames=_cityLookup.ToObservableCollection();
        SearchCommand=new Command<string>(query => ApplySearch(query));
    }

    public MenuViewModel Menu
    {
        get;
    }

    // =========================================================
    // PATIENT PREVIEW PANEL
    // =========================================================
    [ObservableProperty] private DashboardPatientAggregate? previewPatient;
    [ObservableProperty] private ObservableCollection<DashboardEncounterItem> previewRecentVisits = new();
    [ObservableProperty] private ObservableCollection<int> availableEncounterYears = new();
    [ObservableProperty] private int selectedEncounterYear = DateTime.Today.Year;
    [ObservableProperty] private bool hasEncounterYearOptions;
    private List<Encounter> _previewPatientEncounters = [];
    [ObservableProperty] private ObservableCollection<SparkGridColumn> recentEncounterColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> recentEncounterRows = new();
    [ObservableProperty] private bool isPreviewPanelOpen;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPatientGridMode))]
    private bool isEncounterHistoryMode;

    public bool IsPatientGridMode => !IsEncounterHistoryMode;

    [RelayCommand]
    private async Task PreviewPatientAsync(object? param)
    {
        var item = param as DashboardPatientAggregate;
        if(item is null)
        {
            ClosePreviewPanel();
            return;
        }

        var previewVersion=++_patientPreviewVersion;
        PreviewPatient=item;
        IsPreviewPanelOpen=true;
        IsEncounterHistoryMode=true;
        PreviewRecentVisits=new ObservableCollection<DashboardEncounterItem>();
        BuildSelectedPatientEncounterGrid();
        BuildSparkButtons();

        await using var db = await _dbFactory.CreateDbContextAsync();

        var lastVisits = await db.Encounters
            .AsNoTracking()
            .Include(e => e.Doctor)
            .Where(e => e.PatientId==item.Patient.Id)
            .OrderByDescending(e => e.ScheduledStart??e.EncounterDate)
            .ToListAsync();

        if(previewVersion!=_patientPreviewVersion) return;

        _previewPatientEncounters=lastVisits;
        var years=lastVisits
            .Select(GetEncounterYear)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        AvailableEncounterYears=new ObservableCollection<int>(years);
        HasEncounterYearOptions=years.Count>0;
        SelectedEncounterYear=years.Contains(DateTime.Today.Year)
            ? DateTime.Today.Year
            : years.FirstOrDefault(DateTime.Today.Year);
        ApplyPreviewEncounterYear();
        IsEncounterHistoryMode=true;
        BuildSelectedPatientEncounterGrid();
        BuildSparkButtons();
    }

    partial void OnSelectedEncounterYearChanged(int value)
    {
        if(IsEncounterHistoryMode&&_previewPatientEncounters.Count>0)
            ApplyPreviewEncounterYear();
    }

    private void ApplyPreviewEncounterYear()
    {
        if(PreviewPatient?.Patient is not { } patient)
            return;

        var visits=_previewPatientEncounters
            .Where(x => GetEncounterYear(x)==SelectedEncounterYear)
            .OrderByDescending(x => x.ScheduledStart??x.EncounterDate)
            .Select(e => new DashboardEncounterItem
            {
                Source=e,
                PatientName=patient.FullName,
                NationalId=patient.NationalId,
                SzboNumber=patient.SzboNumber,
                Time=(e.ScheduledStart??e.EncounterDate).ToString("dd.MM.yyyy"),
                StatusText=EncounterStatusDisplay.TryGetValue(e.Status, out var label)
                    ? label
                    : e.Status.ToString(),
                StatusColor=EncounterStatusToColor(e.Status)
            });

        PreviewRecentVisits=new ObservableCollection<DashboardEncounterItem>(visits);
        BuildSelectedPatientEncounterGrid();
    }

    private static int GetEncounterYear(Encounter encounter) =>
        (encounter.ScheduledStart??encounter.EncounterDate).Year;
    [RelayCommand]
    private void ClosePreviewPanel()
    {
        _patientPreviewVersion++;
        PreviewPatient=null;
        PreviewRecentVisits=new ObservableCollection<DashboardEncounterItem>();
        _previewPatientEncounters=[];
        AvailableEncounterYears=new ObservableCollection<int>();
        HasEncounterYearOptions=false;
        SelectedEncounterYear=DateTime.Today.Year;
        RecentEncounterRows=new ObservableCollection<SparkGridRow>();
        IsPreviewPanelOpen=false;
        IsEncounterHistoryMode=false;
        BuildSparkGridColumns();
        _searchResults.Clear();
        FilteredPatients=new ObservableCollection<DashboardPatientAggregate>();
        GridRows=new ObservableCollection<SparkGridRow>();
        TotalPages=0;
        BuildSparkButtons();
    }

    [RelayCommand]
    private async Task PreviewDashboardDocument(PatientDocument document)
    {
        if(document is null||string.IsNullOrWhiteSpace(document.StoredPath)||!System.IO.File.Exists(document.StoredPath))
            return;
        await Launcher.Default.OpenAsync(new OpenFileRequest(document.Title, new ReadOnlyFile(document.StoredPath)));
    }
    // =========================================================
    // INFO
    // =========================================================
    public string CurrentDate => DateTime.Now.ToString("dd MMM yyyy");
    public User? UserName => _auth?.CurrentUser;
    public UserRole UserRole => _auth.CurrentUser?.Role??UserRole.Doctor;
    public bool CanView => _policyService.CanPerform(Modules.Dashboard, ModuleAction.View);
    public bool CanManageAppointments => _policyService.CanPerform(Modules.Appointments, ModuleAction.Create);
    public bool CanCreatePatient => _policyService.CanPerform(Modules.Patients, ModuleAction.Create);
    public bool CanCreateEncounter => _policyService.CanPerform(Modules.Encounters, ModuleAction.Create);

    // =========================================================
    // STATE
    // =========================================================
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoaded;

    // =========================================================
    // FILTERS
    // =========================================================
    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string selectedGender = "All";
    [ObservableProperty] private string selectedBloodType = "All";
    [ObservableProperty] private string selectedCity = "All";
    [ObservableProperty] private string selectedAgeGroup = "All";
    [ObservableProperty] private string statusText;
    [ObservableProperty] private bool useCyrillicSearch = true;

    public ObservableCollection<string> StatusFilters { get; } = PatientFilterLookups.Status.ToObservableCollection();
    public ObservableCollection<string> GenderFilters { get; } = PatientFilterLookups.Gender.ToObservableCollection();
    public ObservableCollection<string> BloodTypeFilters { get; } = PatientFilterLookups.BloodType.ToObservableCollection();
    public ObservableCollection<string> AgeGroups { get; } = PatientFilterLookups.AgeGroup.ToObservableCollection();
    public ObservableCollection<string> CityFilterNames
    {
        get;
    }

    public string SelectedStatusDisplay
    {
        get => PatientFilterLookups.Status.ToDisplay(SelectedStatus);
        set
        {
            var v = PatientFilterLookups.Status.ToInternal(value);
            if(SelectedStatus==v) return;
            SelectedStatus=v;
            CurrentPage=1;
            ApplySearch();
            OnPropertyChanged();
        }
    }

    public string SelectedGenderDisplay
    {
        get => PatientFilterLookups.Gender.ToDisplay(SelectedGender);
        set
        {
            var v = PatientFilterLookups.Gender.ToInternal(value);
            if(SelectedGender==v) return;
            SelectedGender=v;
            CurrentPage=1;
            ApplySearch();
            OnPropertyChanged();
        }
    }

    public string SelectedBloodTypeDisplay
    {
        get => PatientFilterLookups.BloodType.ToDisplay(SelectedBloodType);
        set
        {
            var v = PatientFilterLookups.BloodType.ToInternal(value);
            if(SelectedBloodType==v) return;
            SelectedBloodType=v;
            CurrentPage=1;
            ApplySearch();
            OnPropertyChanged();
        }
    }

    public string SelectedCityDisplay
    {
        get => _cityLookup.ToDisplay(SelectedCity);
        set
        {
            var v = _cityLookup.ToInternal(value);
            if(SelectedCity==v) return;
            SelectedCity=v;
            CurrentPage=1;
            ApplySearch();
            OnPropertyChanged();
        }
    }

    public string SelectedAgeGroupDisplay
    {
        get => PatientFilterLookups.AgeGroup.ToDisplay(SelectedAgeGroup);
        set
        {
            var v = PatientFilterLookups.AgeGroup.ToInternal(value);
            if(SelectedAgeGroup==v) return;
            SelectedAgeGroup=v;
            CurrentPage=1;
            ApplySearch();
            OnPropertyChanged();
        }
    }

    // =========================================================
    // PATIENT SEARCH + PAGINATION
    // =========================================================
    [ObservableProperty] private string patientSearchText = "";
    [ObservableProperty] private ObservableCollection<DashboardPatientAggregate> patientSuggestions = new();
    [ObservableProperty] private DashboardPatientAggregate? selectedPatientSuggestion;
    [ObservableProperty] private bool showPatientSuggestions;
    [ObservableProperty] private ObservableCollection<DashboardPatientAggregate> filteredPatients = new();
    [ObservableProperty] private string filteredPatientsCount = "";
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages;

    public string PageInfoText =>
        TotalPages<=0 ? string.Empty : $"Страна {CurrentPage} од {TotalPages}";

    // =========================================================
    // INITIALIZE / REFRESH
    // =========================================================

    [RelayCommand]
    public async Task Initialize()
    {
        if(IsBusy) return;
        ResyncToToday();
        await LoadGlobalAsync();
        await LoadSelectedDateAppointmentsAsync();
        IsLoaded=true;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        ResyncToToday();
        await LoadGlobalAsync();
        await LoadSelectedDateAppointmentsAsync();
    }

    // =========================================================
    // LOAD GLOBAL (patients, KPIs, alerts, therapy cycles)
    // =========================================================
    [RelayCommand]
    public async Task LoadGlobalAsync()
    {
        if(IsBusy) return;
        IsBusy=true;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var isScoped = _policyService.IsScopedToOwnData;
            Guid? doctorId = isScoped ? _policyService.CurrentDoctorId : null;

            // ── PATIENTS ──────────────────────────────────────────────────────────
            var patientsQuery = db.Patients
                .AsNoTracking()
                .Include(p => p.Appointments)
                .Include(p => p.Encounters)
                .AsQueryable();

            if(isScoped)
                patientsQuery=patientsQuery.Where(p => p.DoctorId==doctorId);

            var patients = await patientsQuery.ToListAsync();
            _allPatients=patients.Select(CreateDashboardAggregate).ToList();

            // ── ENCOUNTERS for selected date (KPI counters) ───────────────────────
            var encountersQuery = db.Encounters
                .AsNoTracking()
                .Include(x => x.Patient)
                .Where(x => x.ScheduledStart.HasValue&&x.ScheduledStart.Value.Date==SelectedDate.Date)
                .AsQueryable();

            if(isScoped)
                encountersQuery=encountersQuery.Where(x => x.Patient.DoctorId==doctorId);

            var encounters = await encountersQuery.OrderBy(x => x.ScheduledStart).ToListAsync();

            State.CompletedToday=encounters.Count(x => x.Status==EncounterStatus.Completed);
            State.WaitingToday=encounters.Count(x => x.Status==EncounterStatus.Scheduled||x.Status==EncounterStatus.InProgress);
            State.Encounters=new ObservableCollection<Encounter>(encounters);

            // ── UPCOMING APPOINTMENTS (future, for KPI only) ──────────────────────
            var upcomingQuery = db.Appointments
                .AsNoTracking()
                .Where(x => x.ScheduledStart.Date>=DateTime.Today)
                .Where(x => x.Status==AppointmentStatus.Scheduled
                         || x.Status==AppointmentStatus.InProgress)
                .AsQueryable();

            if(isScoped)
                upcomingQuery=upcomingQuery.Where(x => x.DoctorId==doctorId);

            var upcomingCount = await upcomingQuery.CountAsync();

            // ── ALERT SWEEP ───────────────────────────────────────────────────────
            Debug.WriteLine("Checking for critical alerts...");
            await RunAlertSweepIfNeededAsync();
            Debug.WriteLine("After checking for critical alerts.");

            // ── ALERTS ────────────────────────────────────────────────────────────
            var allAlertsQuery =
                from a in db.Alerts.AsNoTracking()
                join p in db.Patients.AsNoTracking() on a.PatientId equals p.Id
                where !a.IsResolved
                select new
                {
                    a.Id,
                    a.PatientId,
                    a.CreatedAt,
                    a.Level,
                    a.Message,
                    PatientName = p.FirstName+" "+p.LastName,
                    p.DoctorId
                };

            if(isScoped)
                allAlertsQuery=allAlertsQuery.Where(x => x.DoctorId==doctorId);

            var allAlertRows = await allAlertsQuery.OrderByDescending(x => x.CreatedAt).ToListAsync();
            var criticalRows = allAlertRows.Where(x => x.Level==AlertLevel.Critical).ToList();

            State.CriticalAlerts=criticalRows.Count;
            State.CriticalAlertsSummaryText=BuildCriticalAlertsSummary(criticalRows.Select(x => x.PatientName).ToList());
            State.AlertSummaries=BuildAlertSummaries(allAlertRows.Select(x => x.Level));
            State.ActiveAlerts=new ObservableCollection<DashboardAlertItem>(
                allAlertRows.Take(20).Select(x => new DashboardAlertItem
                {
                    Id=x.Id,
                    PatientId=x.PatientId,
                    PatientName=x.PatientName,
                    Message=x.Message,
                    CreatedAtText=x.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy"),
                    Level=x.Level,
                    Command=new RelayCommand(() => OpenAlertPatientAsync(x.PatientId))
                }));

            // ── KPI ───────────────────────────────────────────────────────────────
            State.TotalPatients=_allPatients.Count;
            State.UpcomingAppointmentsCount=upcomingCount;

            // ── THERAPY CYCLES ────────────────────────────────────────────────────
            var cyclesQuery =
                from c in db.TherapyCycles.AsNoTracking()
                join p in db.Patients.AsNoTracking() on c.PatientId equals p.Id
                select new
                {
                    c.Status,
                    c.EndDate,
                    p.DoctorId
                };

            if(isScoped)
                cyclesQuery=cyclesQuery.Where(x => x.DoctorId==doctorId);

            var cycles = await cyclesQuery.ToListAsync();

            State.ActiveTherapies=cycles.Count(x => x.Status==TherapyStatus.Active);
            State.CompletedCycles=cycles.Count(x => x.Status==TherapyStatus.Completed);
            State.MissedCycles=cycles.Count(x => x.Status==TherapyStatus.Missed);
            State.OverdueCycles=cycles.Count(x =>
                x.Status==TherapyStatus.Active&&
                x.EndDate.HasValue&&
                x.EndDate.Value.Date<DateTime.Today);

            // ── RESET SEARCH ──────────────────────────────────────────────────────
            _searchResults=new List<DashboardPatientAggregate>();
            FilteredPatients=new ObservableCollection<DashboardPatientAggregate>();
            FilteredPatientsCount="";
            TotalPages=0;
            CurrentPage=1;

            InitializeSparkControls();
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Dashboard Error: {ex}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // =========================================================
    // APPOINTMENTS FOR THE SELECTED DATE
    // SelectedDate starts at today, so this is also the initial dashboard list.
    // =========================================================
    private async Task LoadSelectedDateAppointmentsAsync()
    {
        var targetDate = SelectedDate.Date;

        try
        {
            var items = await GetAppointmentItemsAsync(targetDate);

            if(targetDate!=SelectedDate.Date)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
                SelectedDateAppointments=new ObservableCollection<DashboardAppointmentItem>(items));
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"[SelectedDateAppointments] date={targetDate:dd.MM.yyyy}: {ex}");
        }
    }

    private async Task<List<DashboardAppointmentItem>> GetAppointmentItemsAsync(DateTime date)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var start = date.Date;
        var end = start.AddDays(1);
        var isScoped = _policyService.IsScopedToOwnData;
        Guid? doctorId = isScoped ? _policyService.CurrentDoctorId : null;

        var query = db.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Where(x => x.ScheduledStart>=start&&x.ScheduledStart<end)
            .AsQueryable();

        if(isScoped)
            query=query.Where(x => x.DoctorId==doctorId);

        var appointments = await query
            .OrderBy(x => x.ScheduledStart)
            .Take(10)
            .ToListAsync();

        return appointments.Select(MapAppointment).ToList();
    }

    private static DashboardAppointmentItem MapAppointment(Appointment appointment)
    {
        var statusText = AppointmentStatusDisplay.TryGetValue(appointment.Status, out var label)
            ? label
            : "Закажан";

        return new DashboardAppointmentItem
        {
            Source=appointment,
            PatientName=appointment.Patient.FullName,
            ScheduledStart=appointment.ScheduledStart,
            Time=appointment.ScheduledStart.ToString("HH:mm"),
            RelativeDay=statusText,
            StatusText=statusText,
            StatusColor=appointment.Status switch
            {
                AppointmentStatus.Completed => Color.FromArgb("#10B981"),
                AppointmentStatus.InProgress => Color.FromArgb("#F59E0B"),
                AppointmentStatus.Cancelled => Color.FromArgb("#EF4444"),
                _ => Color.FromArgb("#3B82F6")
            }
        };
    }

    // =========================================================
    // SELECTED DATE CHANGED  ← called from the manual property setter
    // =========================================================
    private void OnSelectedDateChanged(DateTime value)
    {
        // ── Day strip: re-center if the date fell outside the visible window ──
        if(value.Date<DayStripStartDate.Date||value.Date>DayStripStartDate.AddDays(6).Date)
        {
            DayStripStartDate=value.Date.AddDays(-3);
            BuildDayStrip();
        }
        else
        {
            foreach(var d in State.DayStrip)
                d.IsSelected=d.Date.Date==value.Date;
        }

        _=LoadSelectedDateAppointmentsAsync();
    }

    private static readonly Dictionary<AppointmentStatus, string> AppointmentStatusDisplay = new()
    {
        [AppointmentStatus.Completed]="Завршен",
        [AppointmentStatus.InProgress]="Во тек",
        [AppointmentStatus.Cancelled]="Откажан",
    };

    // =========================================================
    // SEARCH + PAGINATION
    // =========================================================
    public void ApplySearch(string? query = null)
    {
        if(query!=null&&PatientSearchText!=query)
            PatientSearchText=query;

        if(IsEncounterHistoryMode)
            return;

        _searchResults.Clear();
        FilteredPatients=new ObservableCollection<DashboardPatientAggregate>();
        FilteredPatientsCount="";
        GridRows=new ObservableCollection<SparkGridRow>();
        TotalPages=0;
        CurrentPage=1;
    }

    partial void OnCurrentPageChanged(int value)
{
    OnPropertyChanged(nameof(PageInfoText));

    if(IsEncounterHistoryMode)
        BuildSelectedPatientEncounterGrid();
}
    partial void OnTotalPagesChanged(int value) => OnPropertyChanged(nameof(PageInfoText));

    [RelayCommand]
    private void NextPage()
    {
        if(CurrentPage>=TotalPages) return;
        CurrentPage++;
        ProjectPage();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if(CurrentPage<=1) return;
        CurrentPage--;
        ProjectPage();
    }

    [RelayCommand]
    private void PageChanged(int page)
    {
        if(page<1||page>TotalPages||page==CurrentPage) return;
        CurrentPage=page;
        ProjectPage();
    }

    private void ProjectPage()
    {
        var page = _searchResults
            .Skip((CurrentPage-1)*PageSize)
            .Take(PageSize)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            FilteredPatients=new ObservableCollection<DashboardPatientAggregate>(page);
            FilteredPatientsCount=$"{_searchResults.Count} резултати";
        });
    }

    partial void OnPatientSearchTextChanged(string value)
    {
        if(_isSelectingPatientSuggestion) return;

        var query=value?.Trim()??string.Empty;

        if(string.IsNullOrWhiteSpace(query))
        {
            PatientSuggestions.Clear();
            ShowPatientSuggestions=false;
            FilteredPatients=new ObservableCollection<DashboardPatientAggregate>();
            FilteredPatientsCount="";
            TotalPages=0;
            CurrentPage=1;
            return;
        }

        var cyrillicQuery=UseCyrillicSearch
            ? MacedonianTransliterator.ToCyrillic(query)
            : query;

        PatientSuggestions=new ObservableCollection<DashboardPatientAggregate>(
            _allPatients
                .Where(x =>
                    x.Patient.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)||
                    x.Patient.FullName.Contains(cyrillicQuery, StringComparison.OrdinalIgnoreCase)||
                    x.Patient.PatientNumber.Contains(query, StringComparison.OrdinalIgnoreCase)||
                    (!string.IsNullOrWhiteSpace(x.Patient.NationalId)&&x.Patient.NationalId.Contains(query, StringComparison.OrdinalIgnoreCase))||
                    x.Patient.SzboNumber.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Patient.FullName)
                .Take(8));
        ShowPatientSuggestions=PatientSuggestions.Count>0;

        _searchResults.Clear();
        FilteredPatients=new ObservableCollection<DashboardPatientAggregate>();
        FilteredPatientsCount="";
        GridRows=new ObservableCollection<SparkGridRow>();
        TotalPages=0;
        CurrentPage=1;
    }

    partial void OnSelectedPatientSuggestionChanged(DashboardPatientAggregate? value)
    {
        if(value is null)
            return;

        _isSelectingPatientSuggestion=true;
        PatientSearchText=value.Patient.FullName;
        _isSelectingPatientSuggestion=false;
        ShowPatientSuggestions=false;
        PatientSuggestions.Clear();
        _=PreviewPatientAsync(value);
    }

    partial void OnUseCyrillicSearchChanged(bool value)
    {
        OnPatientSearchTextChanged(PatientSearchText);
    }

    partial void OnFilteredPatientsChanged(ObservableCollection<DashboardPatientAggregate> value)
    {
        if(!IsEncounterHistoryMode) RefreshSparkGridRows();
    }

    [RelayCommand]
    private void SelectedStatusChanged(string? v)
    {
        SelectedStatus=PatientFilterLookups.Status.ToInternal(v); CurrentPage=1; ApplySearch();
    }
    [RelayCommand]
    private void SelectedGenderChanged(string? v)
    {
        SelectedGender=PatientFilterLookups.Gender.ToInternal(v); CurrentPage=1; ApplySearch();
    }
    [RelayCommand]
    private void SelectedBloodTypeChanged(string? v)
    {
        SelectedBloodType=PatientFilterLookups.BloodType.ToInternal(v); CurrentPage=1; ApplySearch();
    }
    [RelayCommand]
    private void SelectedCityChanged(string? v)
    {
        SelectedCity=_cityLookup.ToInternal(v); CurrentPage=1; ApplySearch();
    }
    [RelayCommand]
    private void SelectedAgeGroupChanged(string? v)
    {
        SelectedAgeGroup=PatientFilterLookups.AgeGroup.ToInternal(v); CurrentPage=1; ApplySearch();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        if(IsEncounterHistoryMode)
        {
            ClosePreviewPanel();
            return;
        }

        SelectedStatus="All";
        SelectedGender="All";
        SelectedBloodType="All";
        SelectedCity="All";
        SelectedAgeGroup="All";

        OnPropertyChanged(nameof(SelectedStatusDisplay));
        OnPropertyChanged(nameof(SelectedGenderDisplay));
        OnPropertyChanged(nameof(SelectedBloodTypeDisplay));
        OnPropertyChanged(nameof(SelectedCityDisplay));
        OnPropertyChanged(nameof(SelectedAgeGroupDisplay));

        CurrentPage=1;
        PatientSearchText="";

        SyncSparkPickersFromFilters();
    }

    // =========================================================
    // SPARK HEADER ACTIONS
    // =========================================================
    private void BuildSparkButtons()
    {
        Buttons.Clear();

        if(IsEncounterHistoryMode&&PreviewPatient?.Patient is { } patient)
        {
            Buttons.Add(new SparkButtonItem { Label="Освежи", IsPrimary=false, IsEnabled=true, Command=RefreshCommand });
            Buttons.Add(new SparkButtonItem { Label="Нов преглед", IsPrimary=true, IsEnabled=CanCreateEncounter, Command=NewEncounterForSelectedCommand, CommandParameter=patient });
            Buttons.Add(new SparkButtonItem { Label="Нов извештај", IsPrimary=false, IsEnabled=true, Command=OpenNewReportCommand });
            return;
        }

        Buttons.Add(new SparkButtonItem { Label="Исчисти", IsPrimary=false, IsEnabled=true, Command=ClearFiltersCommand });
        Buttons.Add(new SparkButtonItem { Label="Нов преглед", IsPrimary=true, IsEnabled=CanCreateEncounter, Command=NewEncounterCommand });
        //Buttons.Add(new SparkButtonItem { Label="Нов извештај", IsPrimary=false, IsEnabled=true, Command=OpenNewReportCommand });
    }

    [RelayCommand]
    private async Task OpenNewReport()
    {
        await navigationService.GoToAsync(AppRoutes.Reports.List);
    }
    // =========================================================
    // NAVIGATION
    // =========================================================
    [RelayCommand]
    private async Task NavigateToPatients(string? statusFilter = null)
    {
        var q = new Dictionary<string, object>();
        if(!string.IsNullOrWhiteSpace(PatientSearchText)) q["search"]=PatientSearchText;
        if(!string.IsNullOrWhiteSpace(statusFilter)) q["statusFilter"]=statusFilter;
        await Shell.Current.GoToAsync($"///{AppRoutes.Patients.List}", q);
    }

    [RelayCommand]
    private async Task NavigateToAppointments(string? statusFilter = null)
    {
        var q = new Dictionary<string, object>();
        if(!string.IsNullOrWhiteSpace(statusFilter)) q["statusFilter"]=statusFilter;
        await Shell.Current.GoToAsync(AppRoutes.Appointments.List, q);
    }

    [RelayCommand]
    private async Task NavigateToEncounters(string? statusFilter = null)
    {
        var q = new Dictionary<string, object>();
        if(!string.IsNullOrWhiteSpace(statusFilter)) q["statusFilter"]=statusFilter;
        await Shell.Current.GoToAsync(AppRoutes.Encounters.List, q);
    }

    [RelayCommand]
    private async Task NavigateToTherapies() =>
        await Shell.Current.GoToAsync(AppRoutes.Therapy.List);

    private async Task OpenAlertPatientAsync(Guid patientId)
    {
        var patient=_allPatients.FirstOrDefault(x => x.Patient.Id==patientId);
        if(patient is null) return;
        _selectedPatient.SelectedItem=patient.Patient;
        _selectedPatient.OpenInEditMode=false;
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task NavigateToAlerts(string? level = null)
    {
        var query=new Dictionary<string, object>();

        if(!string.IsNullOrWhiteSpace(level))
            query["level"]=level;

        await navigationService.GoToAsync(AppRoutes.Alerts.List, query);
    }

    // ─── HYPERLINK in the patient grid → navigate to patient detail ──────────
    // row.Tag = DashboardPatientAggregate, so we cast and reuse SelectCommand logic.
    [RelayCommand]
    private async Task OpenPatientFromGrid(object? param)
    {
        if(_isNavigating) return;
        if(param is not DashboardPatientAggregate item||item.Patient is null) return;

        try
        {
            _isNavigating=true;
            _selectedPatient.SelectedItem=item.Patient;
            _selectedPatient.OpenInEditMode=false;
            await navigationService.GoToAsync(AppRoutes.Patients.Detail);
        }
        finally
        {
            _isNavigating=false;
        }
    }

    [RelayCommand]
    private async Task OpenDashboardGridItem(object? item)
    {
        if(item is DashboardPatientAggregate patient)
            await PreviewPatientAsync(patient);
        else if(item is DashboardEncounterItem encounter)
            await OpenEncounterFromPreview(encounter);
    }

    // ─── HYPERLINK in the preview panel encounter list ────────────────────────
    [RelayCommand]
    private async Task OpenEncounterFromPreview(DashboardEncounterItem? item)
    {
        if(item?.Source is null) return;
        _selectedEncounter.SelectedItem=item.Source;
        _selectedEncounter.OpenInEditMode=false;
        await navigationService.GoToAsync(AppRoutes.Encounters.Detail);
    }

    [RelayCommand]
    private async Task OpenPatient(Patient? patient)
    {
        if(patient is null) return;
        _selectedPatient.SelectedItem=patient;
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    // ─── "Денешни термини" row tap → appointment detail ───────────────────────
    [RelayCommand]
    private async Task OpenAppointment(DashboardAppointmentItem? item)
    {
        if(item?.Source is null) return;
        _selectedAppointment.SelectedItem=item.Source;
        await navigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    [RelayCommand]
    private async Task NewAppointment()
    {
        if(!CanManageAppointments)
        {
            await _userDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за додавање на нови термини.", "OK");
            return;
        }
        _selectedAppointment.SelectedItem=null;
        await navigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    [RelayCommand]
    private async Task NewEncounter()
    {
        if(!CanCreateEncounter)
        {
            await _userDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за додавање на нов преглед.", "OK");
            return;
        }
        _selectedPatient.SelectedItem=null;
        await navigationService.GoToAsync(AppRoutes.Encounters.Create);
    }

    [RelayCommand]
    private async Task AddPatient()
    {
        _selectedPatient.SelectedItem=null;
        _selectedPatient.OpenInEditMode=true;
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task Select(DashboardPatientAggregate? item)
    {
        if(item?.Patient is null) return;
        _selectedPatient.SelectedItem=item.Patient;
        _selectedPatient.OpenInEditMode=false;
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task Edit(Patient? patient)
    {
        if(patient is null) return;
        _selectedPatient.SelectedItem=patient;
        _selectedPatient.OpenInEditMode=true;
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task NewEncounterForSelected(Patient? patient)
    {
        if(patient is null)
        {
            await _userDialogService.ShowAlertAsync("Внимание", "Одберете пациент прво.");
            return;
        }
        var confirmed = await _userDialogService.ShowConfirmationAsync(
            "Нов преглед",
            "Дали сте сигурни дека сакате да започнете нов преглед?",
            "Да",
            "Не");
        if(!confirmed) return;

        _selectedPatient.SelectedItem=patient;
        await navigationService.GoToAsync(AppRoutes.Encounters.Create);
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private NotificationLevel MapSeverity(string dbSeverity) =>
        dbSeverity?.ToLower() switch
        {
            "info" => NotificationLevel.Info,
            "warning" => NotificationLevel.Warning,
            "critical" => NotificationLevel.Error,
            "error" => NotificationLevel.Error,
            _ => NotificationLevel.Info
        };

    private string GetRelativeTime(DateTime createdAt)
    {
        var span = DateTime.Now-createdAt;
        if(span.TotalMinutes<1) return "сега";
        if(span.TotalHours<1) return $"пред {(int)span.TotalMinutes} мин.";
        if(span.TotalDays<1) return $"пред {(int)span.TotalHours} часа";
        if(span.TotalDays<7) return $"пред {(int)span.TotalDays} дена";
        return createdAt.ToString("dd.MM.yyyy");
    }

    // =========================================================
    // KPI TILES
    // =========================================================
    public ObservableCollection<FFMetricTileItem> Kpis { get; } = new();

    public FFMetricTileItem? Kpi0 => Kpis.Count > 0 ? Kpis[0] : null;
    public FFMetricTileItem? Kpi1 => Kpis.Count > 1 ? Kpis[1] : null;
    public FFMetricTileItem? Kpi2 => Kpis.Count > 2 ? Kpis[2] : null;
    public FFMetricTileItem? Kpi3 => Kpis.Count > 3 ? Kpis[3] : null;
    public FFMetricTileItem? Kpi4 => Kpis.Count > 4 ? Kpis[4] : null;

    private void RefreshKpiBindings()
    {
        OnPropertyChanged(nameof(Kpi0));
        OnPropertyChanged(nameof(Kpi1));
        OnPropertyChanged(nameof(Kpi2));
        OnPropertyChanged(nameof(Kpi3));
        OnPropertyChanged(nameof(Kpi4));
    }

    private void BuildKpiTiles()
    {
        Kpis.Clear();

        Kpis.Add(new FFMetricTileItem { Title="ПАЦИЕНТИ ВКУПНО", Value=State.TotalPatients.ToString("N0"), Icon="\uf0c0", Variant=MetricTileVariant.Primary, Command=NavigateToPatientsCommand });
        Kpis.Add(new FFMetricTileItem { Title="АКТИВНИ ТЕРМИНИ", Value=State.UpcomingAppointmentsCount.ToString("N0"), Icon="\uf133", Variant=MetricTileVariant.Info, Command=NavigateToAppointmentsCommand });
        Kpis.Add(new FFMetricTileItem { Title="ЗАВРШЕНИ ДЕНЕС ПРЕГЛЕДИ", Value=State.CompletedToday.ToString("N0"), Icon="\uf058", Variant=MetricTileVariant.Success, Command=NavigateToEncountersCommand, CommandParameter="Completed" });
        Kpis.Add(new FFMetricTileItem { Title="ЗАКАЖАНИ И ПРИЈАВЕНИ", Value=State.WaitingToday.ToString("N0"), Icon="\uf254", Variant=MetricTileVariant.Warning, Command=NavigateToEncountersCommand, CommandParameter="Scheduled" });
        Kpis.Add(new FFMetricTileItem
        {
            Title="ВО ТЕК",
            Value=State.ActiveTherapies.ToString("N0"),
            Icon="\uf492",
            Variant=State.OverdueCycles>0 ? MetricTileVariant.Warning : MetricTileVariant.Neutral,
            ShowMore=false
        });

        RefreshKpiBindings();
    }

    // =========================================================
    // TAB STRIP
    // =========================================================
    public ObservableCollection<SparkTabItem> Tabs { get; } = new();

    private SparkTabItem _allPatientsTab;
    private SparkTabItem _churnedTab;
    private SparkTabItem _warning;
    private SparkTabItem _encounters;

    private void BuildSparkTabs()
    {
        Tabs.Clear();

        _allPatientsTab=new SparkTabItem { Title="Сите пациенти", IsSelected=true };
        var upcomingTab = new SparkTabItem { Title="Закажани прегледи" };
        var inprogressTab = new SparkTabItem { Title="Прегледи во тек" };
        _warning=new SparkTabItem { Title="Критични" };
        _encounters=new SparkTabItem { Title="Прегледи" };
        _churnedTab=new SparkTabItem { Title="Неактивни Пациенти" };

        _allPatientsTab.Command=new RelayCommand(() => SelectTab(_allPatientsTab, () => SelectedStatusDisplay=PatientFilterLookups.Status.ToDisplay("All")));
        upcomingTab.Command=new RelayCommand(() => SelectTab(upcomingTab, () => NavigateToEncountersCommand.Execute("Scheduled")));
        inprogressTab.Command=new RelayCommand(() => SelectTab(inprogressTab, () => NavigateToEncountersCommand.Execute("InProgress")));
        _warning.Command=new RelayCommand(() => SelectTab(_warning, () => NavigateToAlertsCommand.Execute("Active")));
        _churnedTab.Command=new RelayCommand(() => SelectTab(_churnedTab, () => SelectedStatusDisplay=PatientFilterLookups.Status.ToDisplay(ChurnedStatusName)));

        Tabs.Add(_allPatientsTab);
        Tabs.Add(_churnedTab);
        Tabs.Add(upcomingTab);
        Tabs.Add(inprogressTab);
        Tabs.Add(_warning);

        RefreshSparkTabCounts();
    }

    private void SelectTab(SparkTabItem tab, Action action)
    {
        foreach(var t in Tabs) t.IsSelected=false;
        tab.IsSelected=true;
        action();
    }

    private void RefreshSparkTabCounts()
    {
        if(_allPatientsTab==null) return;
        _allPatientsTab.Value=State.TotalPatients.ToString("N0");
        _warning.Value=State.CriticalAlerts.ToString("N0");
        _encounters.Value=State.Appointments.Count.ToString("N0");
    }

    // =========================================================
    // PICKERS
    // =========================================================
    public ObservableCollection<SparkPickerItem> Pickers { get; } = new();
    private SparkPickerItem _statusPicker, _genderPicker, _bloodTypePicker, _cityPicker, _ageGroupPicker;

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _statusPicker=MakePicker("Статус", StatusFilters, SelectedStatusDisplay, s => SelectedStatusDisplay=s);
        _genderPicker=MakePicker("Пол", GenderFilters, SelectedGenderDisplay, s => SelectedGenderDisplay=s);
        _bloodTypePicker=MakePicker("Крвна група", BloodTypeFilters, SelectedBloodTypeDisplay, s => SelectedBloodTypeDisplay=s);
        _cityPicker=MakePicker("Град", CityFilterNames, SelectedCityDisplay, s => SelectedCityDisplay=s);
        _ageGroupPicker=MakePicker("Возрасна група", AgeGroups, SelectedAgeGroupDisplay, s => SelectedAgeGroupDisplay=s);

        Pickers.Add(_statusPicker);
        Pickers.Add(_genderPicker);
        Pickers.Add(_bloodTypePicker);
        Pickers.Add(_cityPicker);
        Pickers.Add(_ageGroupPicker);
    }

    private static SparkPickerItem MakePicker(string placeholder, IEnumerable<string> items,
        string initialSelection, Action<string> onSelected)
    {
        var picker = new SparkPickerItem { Placeholder=placeholder };
        foreach(var item in items) picker.Items.Add(item);
        picker.SelectedItem=initialSelection;
        picker.PropertyChanged+=(_, e) =>
        {
            if(e.PropertyName==nameof(SparkPickerItem.SelectedItem)&&picker.SelectedItem is string s)
                onSelected(s);
        };
        return picker;
    }

    private void ResyncToToday()
    {
        // Set the backing field without starting a second database request.
        // Initialize/Refresh explicitly load SelectedDateAppointments afterwards.
        _selectedDate=DateTime.Today;
        OnPropertyChanged(nameof(SelectedDate));

        if(DayStripStartDate.Date!=DateTime.Today.AddDays(-3))
            DayStripStartDate=DateTime.Today.AddDays(-3);
    }

    private void SyncSparkPickersFromFilters()
    {
        if(_statusPicker==null) return;
        _statusPicker.SelectedItem=SelectedStatusDisplay;
        _genderPicker.SelectedItem=SelectedGenderDisplay;
        _bloodTypePicker.SelectedItem=SelectedBloodTypeDisplay;
        _cityPicker.SelectedItem=SelectedCityDisplay;
        _ageGroupPicker.SelectedItem=SelectedAgeGroupDisplay;
    }

    // =========================================================
    // BUTTONS
    // =========================================================
    public ObservableCollection<SparkButtonItem> Buttons { get; } = new();

    // =========================================================
    // GRID
    // =========================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "ЕЗБО БРОЈ",      Key = "SzboNumber", Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "ИМЕ И ПРЕЗИМЕ", Key = "FullName", Width = new GridLength(2.4, GridUnitType.Star), CellType = SparkGridCellType.Hyperlink },
            new() { Header = "ЕМБГ", Key = "NationalId", Width = new GridLength(1.45, GridUnitType.Star) },
            new() { Header = "ПОЛ", Key = "Gender", Width = new GridLength(0.8, GridUnitType.Star) },
            new() { Header = "ВОЗРАСТ", Key = "Age", Width = new GridLength(0.8, GridUnitType.Star), CellType = SparkGridCellType.Number },
            new() { Header = "ТЕЛЕФОН", Key = "Phone", Width = new GridLength(1.35, GridUnitType.Star) },
            new() { Header = "ГРАД", Key = "City", Width = new GridLength(1.15, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", Width = new GridLength(1.1, GridUnitType.Star), CellType = SparkGridCellType.Badge },
            new() { Header = "ОПЦИИ", Key = "Actions", Width = GridLength.Auto, CellType = SparkGridCellType.Actions }
        };
    }

    private void BuildSelectedPatientEncounterGrid()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header="ИМЕ И ПРЕЗИМЕ", Key="PatientName", Width=new GridLength(2.4, GridUnitType.Star) },
            new() { Header="ЕЗБО БРОЈ", Key="SzboNumber", Width=new GridLength(1.25, GridUnitType.Star) },
            new() { Header="ДАТУМ НА ПРЕГЛЕД", Key="Date", Width=new GridLength(1.5, GridUnitType.Star) },
            new() { Header="СТАТУС", Key="Status", Width=new GridLength(1.1, GridUnitType.Star), CellType=SparkGridCellType.Badge },
            new() { Header="ОПЦИИ", Key="Actions", Width=GridLength.Auto, CellType=SparkGridCellType.Actions }
        };

        GridRows=new ObservableCollection<SparkGridRow>(PreviewRecentVisits.Select(item =>
        {
            var row=new SparkGridRow { Tag=item };
            row["PatientName"]=item.PatientName;
            row["SzboNumber"]=item.SzboNumber;
            row["Date"]=item.Source.ScheduledStart?.ToString("dd.MM.yyyy")??"—";
            row["Status"]=new SparkBadgeValue(item.StatusText, EncounterStatusToTone(item.Source.Status));
            row["Actions"]=new List<SparkButtonItem>
            {
                new() { IsPrimary=true, Label="Детали", Command=OpenEncounterFromPreviewCommand, CommandParameter=item }
            };
            return row;
        }));
    }

    private void BuildRecentEncounterColumns()
    {
        RecentEncounterColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "СЗБО БРОЈ", Key = "SzboNumber", Width = new GridLength(1.25, GridUnitType.Star) },
            new() { Header = "ИМЕ И ПРЕЗИМЕ", Key = "Patient", Width = new GridLength(2.2, GridUnitType.Star), CellType = SparkGridCellType.Hyperlink },
            new() { Header = "ДАТУМ НА ПРЕГЛЕД", Key = "Date", Width = new GridLength(1.5, GridUnitType.Star) }
        };
    }

    private void RefreshRecentEncounterRows(IEnumerable<DashboardEncounterItem> encounters)
    {
        RecentEncounterRows=new ObservableCollection<SparkGridRow>(encounters.Select(item =>
        {
            var row = new SparkGridRow { Tag=item };
            row["SzboNumber"]=item.SzboNumber;
            row["Date"]=item.Time;
            row["Patient"]=item.PatientName;
            return row;
        }));
    }
    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var item in FilteredPatients)
        {
            var patient = item.Patient;
            var row = new SparkGridRow { Tag=item };

            row["SzboNumber"]=patient.SzboNumber;
            row["FullName"]=patient.FullName;
            row["City"]=patient.City;
            row["NationalId"]=patient.NationalId;
            row["Gender"]=patient.Gender.ToDisplay();
            row["Age"]=patient.Age;
            row["Phone"]=patient.Phone;
            row["City"]=patient.City;
            row["LastActivity"]=item.LastActivity is { } last ? last.ToString("dd.MM.yyyy") : "—";
            row["NextAppointment"]=item.ActiveAppointment is { } next ? next.ScheduledStart.ToString("dd.MM.yyyy") : "—";

            row["Status"]=new SparkBadgeValue(
                StateDisplay.TryGetValue(item.State, out var lbl) ? lbl : item.State.ToString(),
                StateToTone(item.State));

            row["Actions"]=new List<SparkButtonItem>
            {
                new() { IsPrimary = true, IconGlyph = "👁", Label = "Детали",  Command = SelectCommand,  CommandParameter = item },
                new() {                   IconGlyph = "✎",  Label = "Промени", Command = EditCommand,    CommandParameter = patient }
            };

            row["Alerts"]=item.HasAlerts ? "⚠" : "";

            rows.Add(row);
        }

        GridRows=rows;
    }

    private static readonly Dictionary<DashboardPatientState, string> StateDisplay = new()
    {
        [DashboardPatientState.None]="—",
        [DashboardPatientState.Scheduled]="Закажан",
        [DashboardPatientState.InProgress]="Во тек",
        [DashboardPatientState.Completed]="Завршен",
        [DashboardPatientState.Cancelled]="Откажан",
        [DashboardPatientState.Critical]="Критично"
    };

    private static SparkBadgeTone StateToTone(DashboardPatientState state) => state switch
    {
        DashboardPatientState.Completed => SparkBadgeTone.Success,
        DashboardPatientState.InProgress => SparkBadgeTone.Success,
        DashboardPatientState.Scheduled => SparkBadgeTone.Neutral,
        DashboardPatientState.Cancelled => SparkBadgeTone.Danger,
        DashboardPatientState.Critical => SparkBadgeTone.Danger,
        _ => SparkBadgeTone.Neutral
    };

    private static SparkBadgeTone StatusToTone(PatientStatus status) => status switch
    {
        PatientStatus.Active => SparkBadgeTone.Success,
        PatientStatus.Inactive => SparkBadgeTone.Danger,
        PatientStatus.Chronic => SparkBadgeTone.Warning,
        PatientStatus.Deceased => SparkBadgeTone.Danger,
        _ => SparkBadgeTone.Neutral
    };

    // =========================================================
    // DAY STRIP
    // =========================================================
    private static readonly string[] MkDayAbbrev = { "Нед", "Пон", "Вто", "Сре", "Чет", "Пет", "Саб" };

    [ObservableProperty] private DateTime dayStripStartDate = DateTime.Today.AddDays(-3);

    private void BuildDayStrip()
    {
        State.DayStrip.Clear();

        var encounterCountsByDate = _allPatients
            .SelectMany(p => p.Encounters)
            .Where(e => e.ScheduledStart.HasValue)
            .GroupBy(e => e.ScheduledStart!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        for(int i = 0; i<7; i++)
        {
            var date = DayStripStartDate.AddDays(i);
            var capturedDate = date; // capture for lambda
            var day = new DashboardDayItem
            {
                Date=date,
                DayLabel=MkDayAbbrev[(int)date.DayOfWeek],
                DayNumber=date.Day.ToString(),
                IsSelected=date.Date==SelectedDate.Date,
                EncounterCount=encounterCountsByDate.TryGetValue(date.Date, out var count) ? count : 0
            };
            // ─── FIX: capture capturedDate, not date (loop variable closure bug)
            day.Command=new RelayCommand(() => SelectedDate=capturedDate);
            State.DayStrip.Add(day);
        }
    }

    [RelayCommand]
    private void PreviousWeek()
    {
        DayStripStartDate=DayStripStartDate.AddDays(-7);
        BuildDayStrip();
    }

    [RelayCommand]
    private void NextWeek()
    {
        DayStripStartDate=DayStripStartDate.AddDays(7);
        BuildDayStrip();
    }

    [RelayCommand]
    private void GoToToday()
    {
        var alreadyToday = SelectedDate.Date==DateTime.Today;
        SelectedDate=DateTime.Today;
        DayStripStartDate=DateTime.Today.AddDays(-3);
        BuildDayStrip();

        if(alreadyToday)
            _=LoadSelectedDateAppointmentsAsync();
    }

    // =========================================================
    // DAILY ENCOUNTERS (in-memory refresh, fast)
    // =========================================================
    private void RefreshDailyEncounters()
    {
        var items = _allPatients
            .SelectMany(p => p.Encounters.Select(e => (Patient: p.Patient, Encounter: e)))
            .Where(x => x.Encounter.ScheduledStart.HasValue&&
                        x.Encounter.ScheduledStart.Value.Date==SelectedDate.Date)
            .OrderBy(x => x.Encounter.ScheduledStart)
            .Select(x => new DashboardEncounterItem
            {
                Source=x.Encounter,
                PatientName=x.Patient.FullName,
                SzboNumber=x.Patient.SzboNumber,
                Time=x.Encounter.ScheduledStart!.Value.ToString("HH:mm"),
                StatusText=EncounterStatusDisplay.TryGetValue(x.Encounter.Status, out var lbl) ? lbl : x.Encounter.Status.ToString(),
                StatusColor=EncounterStatusToColor(x.Encounter.Status)
            })
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            State.DailyEncounters.Clear();
            foreach(var item in items) State.DailyEncounters.Add(item);
        });
    }

    private static readonly Dictionary<EncounterStatus, string> EncounterStatusDisplay = new()
    {
        [EncounterStatus.Scheduled]="Закажан",
        [EncounterStatus.InProgress]="Во тек",
        [EncounterStatus.Completed]="Завршен",
        [EncounterStatus.Cancelled]="Откажан",
    };

    private static Color EncounterStatusToColor(EncounterStatus status) => status switch
    {
        EncounterStatus.Completed => Color.FromArgb("#16A34A"),
        EncounterStatus.InProgress => Color.FromArgb("#2563EB"),
        EncounterStatus.Scheduled => Color.FromArgb("#64748B"),
        EncounterStatus.Cancelled => Color.FromArgb("#DC2626"),
        _ => Color.FromArgb("#94A3B8")
    };

    private static SparkBadgeTone EncounterStatusToTone(EncounterStatus status) => status switch
    {
        EncounterStatus.Completed => SparkBadgeTone.Success,
        EncounterStatus.Cancelled => SparkBadgeTone.Danger,
        _ => SparkBadgeTone.Neutral
    };
    // =========================================================
    // ALERTS
    // =========================================================
    private static string BuildCriticalAlertsSummary(List<string> patientNames)
    {
        if(patientNames.Count==0) return string.Empty;
        const int max = 2;
        if(patientNames.Count<=max) return string.Join(", ", patientNames);
        var shown = string.Join(", ", patientNames.Take(max));
        var remaining = patientNames.Count-max;
        return remaining==1 ? $"{shown} и уште 1" : $"{shown} и уште {remaining}";
    }

    private ObservableCollection<DashboardAlertSummaryItem> BuildAlertSummaries(IEnumerable<AlertLevel> levels)
    {
        var counts = levels.GroupBy(l => l).ToDictionary(g => g.Key, g => g.Count());
        var items = new ObservableCollection<DashboardAlertSummaryItem>();

        var orderedLevels = new[] { AlertLevel.Critical, AlertLevel.Warning, AlertLevel.Info }
            .Concat(counts.Keys.Except(new[] { AlertLevel.Critical, AlertLevel.Warning, AlertLevel.Info }))
            .Distinct();

        foreach(var level in orderedLevels)
        {
            var count = counts.TryGetValue(level, out var c) ? c : 0;
            items.Add(new DashboardAlertSummaryItem
            {
                Level=level,
                Count=count,
                Label=level.ToLabel(),
                Icon=level.ToIcon(),
                AccentColor=level.ToAccentColor(),
                BackgroundColor=level.ToBackgroundColor(),
                Command=new RelayCommand(() => NavigateToAlertsCommand.Execute(level.ToString()))
            });
        }

        return items;
    }

    private async Task RunAlertSweepIfNeededAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if(_preferencesService.ContainsKey(LastAlertSweepKey)&&
            await _preferencesService.LoadAsync(LastAlertSweepKey)==today)
            return;

        try
        {
            var created = await _alertService.RunDailySweepAsync();
            Debug.WriteLine($"Alert sweep created {created} new alert(s).");
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Alert sweep failed: {ex}");
        }

        await _preferencesService.SaveAsync(LastAlertSweepKey, today);
    }

    // =========================================================
    // WIRING
    // =========================================================
    private void InitializeSparkControls()
    {
        BuildSparkTabs();
        BuildSparkButtons();
        BuildSparkGridColumns();
        BuildRecentEncounterColumns();
        RefreshSparkGridRows();
        BuildDayStrip();
        RefreshDailyEncounters();
        BuildKpiTiles();
    }
}
