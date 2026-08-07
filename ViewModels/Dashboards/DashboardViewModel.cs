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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    // =========================================================
    // SERVICES / FIELDS
    // =========================================================
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAuthStateService _auth;
    private readonly ISelectedItemService<Patient> _selectedPatient;
    private readonly ISelectedItemService<Appointment> _selectedAppointment;
    private readonly IUserDialogService _userDialogService;
    private readonly INavigationService navigationService;
    private readonly IAuthorizationService _policyService;
    private readonly IAlertService _alertService;
    private readonly IPreferencesService _preferencesService;
    private const string LastAlertSweepKey = "alerts_last_sweep_date";
    private readonly FilterLookup _cityLookup = PatientFilterLookups.BuildCityLookup();
    [ObservableProperty] private DateTime selectedDate = DateTime.Today;



    private const int PageSize = 5;
    private List<DashboardPatientAggregate> _allPatients = [];
    private List<DashboardPatientAggregate> _searchResults = [];

    // TODO: point this at whatever your PatientStatus enum actually calls the "left the practice" state
    // (e.g. PatientStatus.Inactive, PatientStatus.Discharged). Left as a string so it's a one-line fix.
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
    // INFO
    // =========================================================
    public string CurrentDate => DateTime.Now.ToString("dd MMM yyyy");
    public User? UserName => _auth?.CurrentUser;
    public UserRole UserRole => _auth.CurrentUser?.Role??UserRole.Doctor; // least-privilege fallback, never Admin
    public bool CanManageAppointments => _policyService.CanAccessModule(Modules.Appointments);
    public bool CanCreatePatient => _policyService.CanAccessModule(Modules.Patients);

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
    [ObservableProperty] private bool useCyrillicSearch;
    //
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
            var internalValue = PatientFilterLookups.Status.ToInternal(value);
            if(SelectedStatus==internalValue) return;

            SelectedStatus=internalValue;
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
            var internalValue = PatientFilterLookups.Gender.ToInternal(value);
            if(SelectedGender==internalValue) return;

            SelectedGender=internalValue;
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
            var internalValue = PatientFilterLookups.BloodType.ToInternal(value);
            if(SelectedBloodType==internalValue) return;

            SelectedBloodType=internalValue;
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
            var internalValue = _cityLookup.ToInternal(value);
            if(SelectedCity==internalValue) return;

            SelectedCity=internalValue;
            CurrentPage=1;
            ApplySearch();               // FIX: was ApplySearch(internalValue), which
                                         // overwrote PatientSearchText with the city's
                                         // internal filter value instead of just re-running
                                         // the existing search under the new city filter.
            OnPropertyChanged();
        }
    }

    public string SelectedAgeGroupDisplay
    {
        get => PatientFilterLookups.AgeGroup.ToDisplay(SelectedAgeGroup);
        set
        {
            var internalValue = PatientFilterLookups.AgeGroup.ToInternal(value);
            if(SelectedAgeGroup==internalValue) return;

            SelectedAgeGroup=internalValue;
            CurrentPage=1;
            ApplySearch();
            OnPropertyChanged();
        }
    }




    // =========================================================
    // PATIENT SEARCH + PAGINATION
    // =========================================================
    [ObservableProperty] private string patientSearchText = "";
    [ObservableProperty] private ObservableCollection<DashboardPatientAggregate> filteredPatients = new();

    [ObservableProperty] private string filteredPatientsCount = "";
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages;
    // =========================================================
    // PAGINATION INFO
    // =========================================================

    public string PageInfoText =>
        TotalPages<=0
            ? string.Empty
            : $"Страна {CurrentPage} од {TotalPages}";
    // =========================================================
    // APPOINTMENTS
    // =========================================================

    
    [RelayCommand]
    public async Task Initialize()
    {
        if(IsBusy) return;
        ResyncToToday();          // NEW
        await LoadGlobalAsync();
        IsLoaded=true;
    }


    [RelayCommand]
    public async Task Refresh()
    {
        ResyncToToday();
        await LoadGlobalAsync();
    }


    [RelayCommand]
    public async Task LoadGlobalAsync()
    {
        if(IsBusy) return;

        IsBusy=true;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // =====================================================
            // DOCTOR SCOPE — resolved once, applied to every query below.
            // =====================================================
            var isScoped = _policyService.IsScopedToOwnData;
            Guid? doctorId = isScoped ? _policyService.CurrentDoctorId : null;

            // =====================================================
            // PATIENTS
            // =====================================================
            var patientsQuery = db.Patients
                .AsNoTracking()
                .Include(p => p.Appointments)
                .Include(p => p.Encounters)
                .AsQueryable();

            if(isScoped)
                patientsQuery=patientsQuery.Where(p => p.DoctorId==doctorId);

            var patients = await patientsQuery.ToListAsync();

            _allPatients=patients
                .Select(CreateDashboardAggregate)
                .ToList();

            // =====================================================
            // TODAY ENCOUNTERS
            // =====================================================
            var encountersQuery = db.Encounters
                .AsNoTracking()
                .Include(x => x.Patient)
                .Where(x =>
                    x.ScheduledStart.HasValue&&
                    x.ScheduledStart.Value.Date==DateTime.Today)
                .AsQueryable();

            if(isScoped)
                encountersQuery=encountersQuery.Where(x => x.Patient.DoctorId==doctorId);

            var encounters = await encountersQuery
                .OrderBy(x => x.ScheduledStart)
                .ToListAsync();

            State.CompletedToday=encounters.Count(x => x.Status==EncounterStatus.Completed);
            State.WaitingToday=encounters.Count(x => x.Status==EncounterStatus.Scheduled||x.Status==EncounterStatus.CheckedIn);
            State.NoShowToday=encounters.Count(x => x.Status==EncounterStatus.NoShow);

            // =====================================================
            // TODAY APPOINTMENTS
            // =====================================================
            var todayAppointmentsQuery = db.Appointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Where(x => x.ScheduledStart.Date==DateTime.Today)
                .AsQueryable();

            if(isScoped)
                todayAppointmentsQuery=todayAppointmentsQuery.Where(x => x.DoctorId==doctorId);

            var todayAppointments = await todayAppointmentsQuery
                .OrderBy(x => x.ScheduledStart)
                .ToListAsync();

            // =====================================================
            // UPCOMING APPOINTMENTS
            // =====================================================
            var upcomingAppointmentsQuery = db.Appointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Where(x => x.ScheduledStart.Date>=DateTime.Today)
                .AsQueryable();

            if(isScoped)
                upcomingAppointmentsQuery=upcomingAppointmentsQuery.Where(x => x.DoctorId==doctorId);

            var upcomingAppointments = await upcomingAppointmentsQuery
                .OrderBy(x => x.ScheduledStart)
                .ToListAsync();

            // =====================================================
            // ALERT SWEEP — must finish before we read alerts below
            // =====================================================
            Debug.WriteLine("Checking for critical alerts...");
            await RunAlertSweepIfNeededAsync();
            Debug.WriteLine("After Checking for critical alerts...");

            // =====================================================
            // ALERTS — ALL LEVELS (was critical-only)
            // TODO: Alert.cs not yet reviewed — can't confirm how it relates
            // to Patient/Doctor. Defaulting to 0 for scoped (Doctor) users
            // rather than showing every practice's alerts, since "unknown
            // ownership" should fail closed, not open.
            // =====================================================
            var allAlertsQuery =
                from a in db.Alerts.AsNoTracking()
                join p in db.Patients.AsNoTracking() on a.PatientId equals p.Id
                where !a.IsResolved
                select new
                {
                    a.Id,
                    a.CreatedAt,
                    a.Level,
                    PatientName = p.FirstName+" "+p.LastName,
                    p.DoctorId
                };

            if(isScoped)
                allAlertsQuery=allAlertsQuery.Where(x => x.DoctorId==doctorId);

            var allAlertRows = await allAlertsQuery
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var criticalRows = allAlertRows.Where(x => x.Level==AlertLevel.Critical).ToList();

            State.CriticalAlerts=criticalRows.Count;
            State.CriticalAlertsSummaryText=BuildCriticalAlertsSummary(criticalRows.Select(x => x.PatientName).ToList());
            State.AlertSummaries=BuildAlertSummaries(allAlertRows.Select(x => x.Level));

            Debug.WriteLine($"AlertSummaries: {State.AlertSummaries.Count}, HasAnyAlerts: {State.HasAnyAlerts}");

            // =====================================================
            // KPI
            // =====================================================
            State.TotalPatients=_allPatients.Count;

            // NOTE — pre-existing bug, unrelated to doctor scoping:
            // this assigns TODAY's appointment count to a property named
            // UpcomingAppointmentsCount. If the KPI card is meant to show
            // "upcoming" (i.e. future) appointments, this should read
            // upcomingAppointments.Count, not todayAppointments.Count.
            // Left as todayAppointments.Count here to preserve existing
            // behavior — flagging so you can decide which one is correct.
            State.UpcomingAppointmentsCount=todayAppointments.Count;

            // =====================================================
            // THERAPY CYCLE KPI
            // These State properties existed but were never assigned, so the
            // numbers behind them were always 0.
            // =====================================================
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

            // Still running but past its planned end date.
            State.OverdueCycles=cycles.Count(x =>
                x.Status==TherapyStatus.Active&&
                x.EndDate.HasValue&&
                x.EndDate.Value.Date<DateTime.Today);

            // =====================================================
            // WEEK RANGE FOR ANALYTICS
            // =====================================================
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-6+i))
                .ToList();

            // =====================================================
            // KEEP SEARCH STATE
            // =====================================================
            _searchResults=new List<DashboardPatientAggregate>();

            FilteredPatients=new ObservableCollection<DashboardPatientAggregate>();
            FilteredPatientsCount="";
            TotalPages=0;
            CurrentPage=1;

            // =====================================================
            // ENCOUNTERS COLLECTION
            // =====================================================
            State.Encounters=new ObservableCollection<Encounter>(encounters);

            var appointmentItems = upcomingAppointments
                .Take(5)
                .Select(a => new DashboardAppointmentItem
                {
                    Source=a,
                    PatientName=$"{a.Patient.FirstName} {a.Patient.LastName}",
                    ScheduledStart=a.ScheduledStart,
                    Time=a.ScheduledStart.ToString("HH:mm"),
                    RelativeDay=a.ScheduledStart.Date==DateTime.Today
                        ? "Денес"
                        : $"Закажано на :{a.ScheduledStart}",
                    StatusText=a.Status.ToString(),
                    StatusColor=a.Status switch
                    {
                        AppointmentStatus.Completed => Color.FromArgb("#10B981"),
                        AppointmentStatus.InProgress => Color.FromArgb("#F59E0B"),
                        AppointmentStatus.Cancelled => Color.FromArgb("#EF4444"),
                        AppointmentStatus.Missed => Color.FromArgb("#94A3B8"),
                        _ => Color.FromArgb("#3B82F6")
                    }
                });

            State.Appointments=new ObservableCollection<DashboardAppointmentItem>(appointmentItems);

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

    public void ApplySearch(string? query = null)
    {
        if(query!=null)
            PatientSearchText=query;

        if(string.IsNullOrWhiteSpace(PatientSearchText))
        {
            _searchResults=new List<DashboardPatientAggregate>();
            FilteredPatients=new ObservableCollection<DashboardPatientAggregate>();
            FilteredPatientsCount="";
           // TotalPages=0;
           // CurrentPage=1;
            return;
        }

        IEnumerable<DashboardPatientAggregate> result = _allPatients;
        var search = PatientSearchText.Trim();

        result=result.Where(x =>
     x.Patient.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
     ||
     x.Patient.NationalId.Contains(search, StringComparison.OrdinalIgnoreCase)
     ||
     x.Patient.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)
     ||
     x.Patient.City.Contains(search, StringComparison.OrdinalIgnoreCase));

        // ФИЛТРИ
        // Filters
        if(SelectedStatus!="All")
        {
            result=result.Where(x =>
                x.Patient.Status.ToString()
                    .Equals(SelectedStatus, StringComparison.OrdinalIgnoreCase));
        }

        if(SelectedGender!="All")
        {
            result=result.Where(x =>
                x.Patient.Gender.ToString()
                    .Equals(SelectedGender, StringComparison.OrdinalIgnoreCase));
        }

        if(SelectedBloodType!="All")
        {
            result=result.Where(x =>
                x.Patient.BloodType.Equals(
                    SelectedBloodType,
                    StringComparison.OrdinalIgnoreCase));
        }

        if(SelectedCity!="All")
        {
            result=result.Where(x =>
                x.Patient.City.Equals(
                    SelectedCity,
                    StringComparison.OrdinalIgnoreCase));
        }

        if(SelectedAgeGroup!="All")
        {
            result=result.Where(x =>
                x.Patient.Age.IsInAgeGroup(SelectedAgeGroup));
        }

        _searchResults=result.OrderBy(x => x.State).ToList();
        TotalPages=Math.Max(1, (int)Math.Ceiling(_searchResults.Count/(double)PageSize));
        CurrentPage=1;

        ProjectPage();
    }
    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(PageInfoText));
    }
    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(PageInfoText));
    }
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

    /// <summary>Jump to a specific page — wired to SparkDataGridView's PageChangedCommand.</summary>
    [RelayCommand]
    private void PageChanged(int page)
    {
        if(page<1||page>TotalPages||page==CurrentPage) return;
        CurrentPage=page;
        ProjectPage();
    }

    /// <summary>Slices _searchResults into the current page and pushes to FilteredPatients.</summary>
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
        if(UseCyrillicSearch&&!string.IsNullOrEmpty(value))
        {
            var converted = MacedonianTransliterator.ToCyrillic(value);
            if(converted!=value)
            {
                PatientSearchText=converted;
                return;
            }
        }

        if(string.IsNullOrWhiteSpace(value))
        {
            FilteredPatients=new ObservableCollection<DashboardPatientAggregate>();
            FilteredPatientsCount="";
            TotalPages=0;
            CurrentPage=1;
            return;
        }

        ApplySearch();
    }


    partial void OnUseCyrillicSearchChanged(bool value)
    {
        if(!value||string.IsNullOrWhiteSpace(PatientSearchText))
            return;

        PatientSearchText=MacedonianTransliterator.ToCyrillic(PatientSearchText);
        ApplySearch();
    }
    partial void OnFilteredPatientsChanged(ObservableCollection<DashboardPatientAggregate> value) => RefreshSparkGridRows();

    [RelayCommand]
    private void SelectedStatusChanged(string? value)
    {
        SelectedStatus=PatientFilterLookups.Status.ToInternal(value);
        CurrentPage=1;
        ApplySearch();
    }

    [RelayCommand]
    private void SelectedGenderChanged(string? value)
    {
        SelectedGender=PatientFilterLookups.Gender.ToInternal(value);
        CurrentPage=1;
        ApplySearch();
    }

    [RelayCommand]
    private void SelectedBloodTypeChanged(string? value)
    {
        SelectedBloodType=PatientFilterLookups.BloodType.ToInternal(value);
        CurrentPage=1;
        ApplySearch();
    }

    [RelayCommand]
    private void SelectedCityChanged(string? value)
    {
        SelectedCity=_cityLookup.ToInternal(value);
        CurrentPage=1;
        ApplySearch();
    }

    [RelayCommand]
    private void SelectedAgeGroupChanged(string? value)
    {
        SelectedAgeGroup=PatientFilterLookups.AgeGroup.ToInternal(value);
        CurrentPage=1;
        ApplySearch();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedStatus="All";
        SelectedGender="All";
        SelectedBloodType="All";
        SelectedCity="All";
        SelectedAgeGroup="All";

        // refresh display values for SparkPickers
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
    // NAVIGATION
    // =========================================================
    [RelayCommand]
    private async Task NavigateToPatients(string? statusFilter = null)
    {
        var query = new Dictionary<string, object>();

        if(!string.IsNullOrWhiteSpace(PatientSearchText))
            query["search"]=PatientSearchText;

        if(!string.IsNullOrWhiteSpace(statusFilter))
            query["statusFilter"]=statusFilter;

        await Shell.Current.GoToAsync(AppRoutes.Patients.List, query);
    }

    [RelayCommand]
    private async Task NavigateToAppointments(string? statusFilter = null)
    {
        var query = new Dictionary<string, object>();

        if(!string.IsNullOrWhiteSpace(statusFilter))
            query["statusFilter"]=statusFilter;

        await Shell.Current.GoToAsync(AppRoutes.Appointments.List, query);
    }

   
    [RelayCommand]
    private async Task NavigateToEncounters(string? statusFilter = null)
    {
        var query = new Dictionary<string, object>();

        if(!string.IsNullOrWhiteSpace(statusFilter))
            query["statusFilter"]=statusFilter;

        await Shell.Current.GoToAsync(AppRoutes.Encounters.List, query);
    }
    [RelayCommand]
    private async Task NavigateToTherapies() =>
        await Shell.Current.GoToAsync(AppRoutes.Therapy.List);

    [RelayCommand]
    private async Task NavigateToAlerts() =>
        await Shell.Current.GoToAsync("notifications?filter=critical");

    [RelayCommand]
    private async Task OpenPatient(Patient? patient)
    {
        if(patient is null) return;
        _selectedPatient.SelectedItem=patient;
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

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
            await _userDialogService.ShowAlertAsync(
                "Пристапот е одбиен",
                "Немате авторизација за додавање на нови термини.",
                "OK");
            return;
        }
        _selectedAppointment.SelectedItem=null;
        await navigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    /// <summary>
    /// TODO: swap the TODO alert for EnsureEncounterAsync/Encounter navigation
    /// once that entity work lands (see EHMR.Application pending work).
    /// </summary>
    [RelayCommand]
    private async Task NewEncounter()
    {
        if(!CanManageAppointments)
        {
            await _userDialogService.ShowAlertAsync(
                "Пристапот е одбиен",
                "Немате авторизација за додавање на нов преглед.",
                "OK");
            return;
        }

        _selectedPatient.SelectedItem=null;
        await navigationService.GoToAsync(AppRoutes.Encounters.Create);
    }

    //[RelayCommand]
    //private async Task OpenKpi(string type)
    //{
    //    switch(type)
    //    {
    //        case "patients": await NavigateToPatients(); break; 
    //        case "appointments": await NavigateToAppointments(); break;
    //        case "alerts": await NavigateToAlerts(); break;
    //    }
    //}
    // =========================================================
    // QUERY ATTRIBUTES
    // =========================================================
  
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
        if(item?.Patient is null)
            return;

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
    //
    // The dashboard already computed these numbers on every load but rendered
    // none of them — only four collections were bound in the view. Surfacing
    // them through the existing FFMetricTile control keeps the dashboard on the
    // same design system as the admin dashboard instead of hand-rolled cards.
    // =========================================================
    public ObservableCollection<FFMetricTileItem> Kpis { get; } = new();

    private void BuildKpiTiles()
    {
        Kpis.Clear();

        Kpis.Add(new FFMetricTileItem
        {
            Title="Пациенти",
            Value=State.TotalPatients.ToString("N0"),
            Subtitle="вкупно во системот",
            Icon="",
            Variant=MetricTileVariant.Primary,
            Command=NavigateToPatientsCommand
        });

        Kpis.Add(new FFMetricTileItem
        {
            Title="Термини денес",
            Value=State.UpcomingAppointmentsCount.ToString("N0"),
            Subtitle="закажани за денес",
            Icon="",
            Variant=MetricTileVariant.Info,
            Command=NavigateToAppointmentsCommand
        });

        Kpis.Add(new FFMetricTileItem
        {
            Title="Завршени денес",
            Value=State.CompletedToday.ToString("N0"),
            Subtitle="завршени прегледи",
            Icon="",
            Variant=MetricTileVariant.Success,
            Command=NavigateToEncountersCommand,
            CommandParameter="Completed"
        });

        Kpis.Add(new FFMetricTileItem
        {
            Title="Чекаат",
            Value=State.WaitingToday.ToString("N0"),
            Subtitle="закажани и пријавени",
            Icon="",
            Variant=MetricTileVariant.Warning,
            Command=NavigateToEncountersCommand,
            CommandParameter="Scheduled"
        });

        Kpis.Add(new FFMetricTileItem
        {
            Title="Не се јавиле",
            Value=State.NoShowToday.ToString("N0"),
            Subtitle="пропуштени денес",
            Icon="",
            Variant=MetricTileVariant.Danger,
            Command=NavigateToEncountersCommand,
            CommandParameter="NoShow"
        });

        Kpis.Add(new FFMetricTileItem
        {
            Title="Терапии во тек",
            Value=State.ActiveTherapies.ToString("N0"),
            Subtitle=State.OverdueCycles>0
                ? $"{State.OverdueCycles} со поминат рок"
                : "сите во рок",
            Icon="",
            Variant=State.OverdueCycles>0
                ? MetricTileVariant.Warning
                : MetricTileVariant.Neutral,
            Command=NavigateToTherapiesCommand
        });
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
       // var activeTherapiesTab = new SparkTabItem { Title="Tерапии во тек" };
        var upcomingTab = new SparkTabItem { Title="Закажани прегледи" };
        var inprogressPregledi = new SparkTabItem { Title="Прегледи во тек" };
        var neDojade = new SparkTabItem { Title="Пропуштени прегледи" };
        _warning=new SparkTabItem { Title="Критични" };
        _encounters=new SparkTabItem { Title="Прегледи" };
        _churnedTab=new SparkTabItem { Title="Неактивни Пациенти" };

    _allPatientsTab.Command=new RelayCommand(() => SelectTab(_allPatientsTab, () =>
            SelectedStatusDisplay=PatientFilterLookups.Status.ToDisplay("All")));

      

        upcomingTab.Command=new RelayCommand(() => SelectTab(upcomingTab,
            () => NavigateToEncountersCommand.Execute("Scheduled")));
        inprogressPregledi.Command=new RelayCommand(() => SelectTab(inprogressPregledi,
          () => NavigateToEncountersCommand.Execute("InProgress")));
        neDojade.Command=new RelayCommand(() => SelectTab(neDojade,
   () => NavigateToEncountersCommand.Execute("NoShow")));
        _warning.Command=new RelayCommand(() => SelectTab(_warning,
            () => NavigateToAlertsCommand.Execute("Active")));
    
        _churnedTab.Command=new RelayCommand(() => SelectTab(_churnedTab,
            () => SelectedStatusDisplay=PatientFilterLookups.Status.ToDisplay(ChurnedStatusName)));

        Tabs.Add(_allPatientsTab);
        Tabs.Add(_churnedTab);
        Tabs.Add(upcomingTab);

        Tabs.Add(inprogressPregledi);
        Tabs.Add(neDojade);
        Tabs.Add(_warning); 


        RefreshSparkTabCounts();
    }

    /// <summary>Marks one tab selected and clears the rest, then runs the tab's own action.</summary>
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

    public ObservableCollection<SparkPickerItem> Pickers { get; } = new();

    private SparkPickerItem _statusPicker, _genderPicker, _bloodTypePicker, _cityPicker, _ageGroupPicker;

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _statusPicker=MakePicker("Статус", StatusFilters, SelectedStatusDisplay,
            selected => SelectedStatusDisplay=selected);

        _genderPicker=MakePicker("Пол", GenderFilters, SelectedGenderDisplay,
            selected => SelectedGenderDisplay=selected);

        _bloodTypePicker=MakePicker("Крвна група", BloodTypeFilters, SelectedBloodTypeDisplay,
            selected => SelectedBloodTypeDisplay=selected);

        _cityPicker=MakePicker("Град", CityFilterNames, SelectedCityDisplay,
            selected => SelectedCityDisplay=selected);

        _ageGroupPicker=MakePicker("Возрасна група", AgeGroups, SelectedAgeGroupDisplay,
            selected => SelectedAgeGroupDisplay=selected);

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
        if(SelectedDate.Date!=DateTime.Today)
            SelectedDate=DateTime.Today;

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

    private void BuildSparkButtons()
    {
        Buttons.Clear();
        Buttons.Add(new SparkButtonItem
        {
            Label="✕ Исчисти",
            IsPrimary=true,
            Command=ClearFiltersCommand
        });
    }

    // =========================================================
    // GRID  (ЕМБГ / ПАЦИЕНТ / ПОЛ / ВОЗРАСТ / КРВ / ТЕЛЕФОН / СТАТУС / АКЦИИ)
    // =========================================================

    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
    {
        new() { Header = "ЕМБГ", Key = "NationalId", Width = new GridLength(1.3, GridUnitType.Star) },
        new() { Header = "ПАЦИЕНТ", Key = "FullName", Width = new GridLength(2.8, GridUnitType.Star) },
        new() { Header = "ПОЛ", Key = "Gender", Width = new GridLength(0.8, GridUnitType.Star) },
        new() { Header = "ВОЗРАСТ", Key = "Age", CellType = SparkGridCellType.Number, Width = new GridLength(0.9, GridUnitType.Star) },
        new() { Header = "КРВ", Key = "BloodType", Width = new GridLength(0.8, GridUnitType.Star) },
        new() { Header = "ТЕЛЕФОН", Key = "Phone", Width = new GridLength(1.5, GridUnitType.Star) },
        new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(1.2, GridUnitType.Star) },
        // FIX: Badge → Button. This was the root cause — Badge expects a SparkBadgeValue,
        // this column carries a SparkButtonItem.
        new() { Header = "ЗАКАЖИ ПРЕГЛЕД", Key = "Pregled", CellType = SparkGridCellType.Button, Width = new GridLength(1.4, GridUnitType.Star) },
        new() { Header = "АКЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
    };
    }
    //partial void OnFilteredPatientsChanged(ObservableCollection<Patient> value) => RefreshSparkGridRows();
    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var item in FilteredPatients)
        {
            var patient = item.Patient;

            var row = new SparkGridRow { Tag=item };

            row["NationalId"]=PrivacyMaskHelper.MaskNationalId(patient.NationalId);
            row["FullName"]=patient.FullName;
            row["City"]=patient.City;
            row["Gender"]=patient.Gender.ToDisplay();
            row["Age"]=patient.Age;
            row["BloodType"]=patient.BloodType;
     
            row["Phone"]=patient.Phone;
         
            row["LastActivity"]=item.LastActivity is { } last
                ? last.ToString("dd.MM.yyyy")
                : "—";

            row["NextAppointment"]=item.ActiveAppointment is { } next
                ? next.ScheduledStart.ToString("dd.MM.yyyy HH:mm")
                : "—";

            // FIX: label and tone now both come from item.State (the encounter
            // workflow state), instead of mixing item.State for the text with
            // item.Patient.Status (a different enum entirely) for the color.
            // Label is also localized instead of falling back to the raw enum name.
            row["Status"]=new SparkBadgeValue(
                StateDisplay.TryGetValue(item.State, out var label) ? label : item.State.ToString(),
                StateToTone(item.State));
            var actions = new List<SparkButtonItem>
            {
                new SparkButtonItem
                {
                    IsPrimary=true,
                    IconGlyph="👁",
                    Label="Детали",
                    Command=SelectCommand,
                    CommandParameter=item
                },
                new SparkButtonItem { IconGlyph="✎", Label="Промени", Command=EditCommand, CommandParameter=patient }
            };
            row["Alerts"]=item.HasAlerts ? "⚠" : "";
            row["Actions"]=actions;
            row["Pregled"]=new SparkButtonItem
            {
                IconGlyph="\uD83D\uDCC5", // 📅
                Label="Закажи преглед",
                IsPrimary=true,
                Command=NewEncounterForSelectedCommand,
                CommandParameter=patient
            };

            rows.Add(row);
        }

        GridRows=rows;
    }

    private static readonly Dictionary<DashboardPatientState, string> StateDisplay = new()
    {
        [DashboardPatientState.None]="—",
        [DashboardPatientState.Scheduled]="Закажан",
        [DashboardPatientState.Waiting]="Чека",
        [DashboardPatientState.CheckedIn]="Пријавен",
        [DashboardPatientState.InProgress]="Во тек",
        [DashboardPatientState.Completed]="Завршен",
        [DashboardPatientState.Cancelled]="Откажан",
        [DashboardPatientState.NoShow]="Не дојде",
        [DashboardPatientState.Critical]="Критично"
    };

    private static SparkBadgeTone StateToTone(DashboardPatientState state) => state switch
    {
        DashboardPatientState.Completed => SparkBadgeTone.Success,
        DashboardPatientState.InProgress => SparkBadgeTone.Success,
        DashboardPatientState.CheckedIn => SparkBadgeTone.Warning,
        DashboardPatientState.Waiting => SparkBadgeTone.Warning,
        DashboardPatientState.Scheduled => SparkBadgeTone.Neutral,
        DashboardPatientState.NoShow => SparkBadgeTone.Danger,
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
    // ENCOUNTERS BY DAY — replaces the old notifications/audit card
    // =========================================================

    private static readonly string[] MkDayAbbrev = { "Нед", "Пон", "Вто", "Сре", "Чет", "Пет", "Саб" };

    [ObservableProperty] private DateTime dayStripStartDate = DateTime.Today.AddDays(-3);

    private void BuildDayStrip()
    {
        State.DayStrip.Clear();

        // FIX: day strip previously had no idea which dates actually had encounters —
        // it was just 7 blank date shells. Now each tile gets a real count so the UI
        // can surface "there's something here" before the user taps it.
        var encounterCountsByDate = _allPatients
            .SelectMany(p => p.Encounters)
            .Where(e => e.ScheduledStart.HasValue)
            .GroupBy(e => e.ScheduledStart!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        for(int i = 0; i<7; i++)
        {
            var date = DayStripStartDate.AddDays(i);
            var day = new DashboardDayItem
            {
                Date=date,
                DayLabel=MkDayAbbrev[(int)date.DayOfWeek],
                DayNumber=date.Day.ToString(),
                IsSelected=date.Date==SelectedDate.Date,
                EncounterCount=encounterCountsByDate.TryGetValue(date.Date, out var count) ? count : 0
            };
            day.Command=new RelayCommand(() => SelectedDate=day.Date);
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
        SelectedDate=DateTime.Today;
        DayStripStartDate=DateTime.Today.AddDays(-3);
        BuildDayStrip();
    }

    // FIX: previously only toggled IsSelected on the 7 items already in the strip, so picking
    // a date outside the visible week (e.g. via a calendar picker) had no visible effect.
    // Now it re-centers the strip on whatever date was picked.
    partial void OnSelectedDateChanged(DateTime value)
    {
        if(value.Date<DayStripStartDate.Date||value.Date>DayStripStartDate.AddDays(6).Date)
        {
            DayStripStartDate=value.Date.AddDays(-3);
            BuildDayStrip();
        }
        else
        {
            foreach(var d in State.DayStrip) d.IsSelected=d.Date.Date==value.Date;
        }

        RefreshDailyEncounters();
    }
    private static string BuildCriticalAlertsSummary(List<string> patientNames)
    {
        if(patientNames.Count==0) return string.Empty;

        const int maxNamesShown = 2;

        if(patientNames.Count<=maxNamesShown)
            return string.Join(", ", patientNames);

        var shown = string.Join(", ", patientNames.Take(maxNamesShown));
        var remaining = patientNames.Count-maxNamesShown;

        return remaining==1
            ? $"{shown} и уште 1"
            : $"{shown} и уште {remaining}";
    }
    private ObservableCollection<DashboardAlertSummaryItem> BuildAlertSummaries(IEnumerable<AlertLevel> levels)
    {
        var counts = levels
            .GroupBy(l => l)
            .ToDictionary(g => g.Key, g => g.Count());

        var items = new ObservableCollection<DashboardAlertSummaryItem>();

        var orderedLevels = new[] { AlertLevel.Critical, AlertLevel.Warning, AlertLevel.Info }
            .Concat(counts.Keys.Except(new[] { AlertLevel.Critical, AlertLevel.Warning, AlertLevel.Info }))
            .Distinct();

        foreach(var level in orderedLevels)
        {
            var count = counts.TryGetValue(level, out var c) ? c : 0;
            // FIX: no longer skipping count == 0 — chip always renders, just shows "0"

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
        {
            return; // already ran today on this machine
        }

        try
        {
            var created = await _alertService.RunDailySweepAsync();
            System.Diagnostics.Debug.WriteLine($"Alert sweep created {created} new alert(s).");
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Alert sweep failed: {ex}");
        }

        await _preferencesService.SaveAsync(LastAlertSweepKey, today);
    }

    private void RefreshDailyEncounters()
    {
        var items = _allPatients
            .SelectMany(p => p.Encounters.Select(e => (Patient: p.Patient, Encounter: e)))
            .Where(x => x.Encounter.ScheduledStart.HasValue
                     &&x.Encounter.ScheduledStart.Value.Date==SelectedDate.Date)   // FIX
            .OrderBy(x => x.Encounter.ScheduledStart)
            .Select(x => new DashboardEncounterItem
            {
                Source=x.Encounter,
                PatientName=x.Patient.FullName,
                Time=x.Encounter.ScheduledStart!.Value.ToString("HH:mm"),
                StatusText=EncounterStatusDisplay.TryGetValue(x.Encounter.Status, out var label)
                    ? label
                    : x.Encounter.Status.ToString(),
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
        [EncounterStatus.CheckedIn]="Пријавен",
        [EncounterStatus.InProgress]="Во тек",
        [EncounterStatus.Completed]="Завршен",
        [EncounterStatus.Cancelled]="Откажан",
        [EncounterStatus.NoShow]="Не дојде"
    };

    private static Color EncounterStatusToColor(EncounterStatus status) => status switch
    {
        EncounterStatus.Completed => Color.FromArgb("#16A34A"),
        EncounterStatus.InProgress => Color.FromArgb("#2563EB"),
        EncounterStatus.CheckedIn => Color.FromArgb("#0EA5E9"),
        EncounterStatus.Scheduled => Color.FromArgb("#64748B"),
        EncounterStatus.NoShow => Color.FromArgb("#DC2626"),
        EncounterStatus.Cancelled => Color.FromArgb("#DC2626"),
        _ => Color.FromArgb("#94A3B8")
    };
    // =========================================================
    // WIRING
    // =========================================================
    private void InitializeSparkControls()
    {
        BuildSparkTabs();
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
        RefreshSparkGridRows();
        BuildDayStrip();
        RefreshDailyEncounters();
        BuildKpiTiles();
    }


}


