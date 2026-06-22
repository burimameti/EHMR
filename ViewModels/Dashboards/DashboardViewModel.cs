using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAuthStateService _auth;
    private readonly ISelectedItemService<Patient> _selectedPatient;
    private readonly ISelectedItemService<Appointment> _selectedAppointment;
    private readonly IUserDialogService _userDialogService;

    public MenuViewModel Menu
    {
        get;
    }

    // =========================================================
    // STATE
    // =========================================================

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoaded;

    // =========================================================
    // KPI
    // =========================================================

    [ObservableProperty] private int totalPatients;
    [ObservableProperty] private int activeTherapies;
    [ObservableProperty] private int upcomingAppointmentsCount;
    [ObservableProperty] private int criticalAlerts;
    [ObservableProperty] private int completedCycles;
    [ObservableProperty] private int overdueCycles;
    [ObservableProperty] private int missedCycles;

    [ObservableProperty] private double adherenceProgress;
    [ObservableProperty] private Color adherenceColor = Colors.Gray;

    [ObservableProperty] private string riskLevel = "Loading...";
    [ObservableProperty] private string completionPercentage = "0%";
    [ObservableProperty] private string completionStatus = "Loading...";

    // =========================================================
    // PATIENT SEARCH + PAGINATION
    // =========================================================

    [ObservableProperty]
    private string patientSearchText = "";

    // Only populated after a search — empty on dashboard load
    [ObservableProperty]
    private ObservableCollection<Patient> filteredPatients = new();

    // True only while the user has typed something
    [ObservableProperty]
    private bool hasSearchResults;

    // Shown in the badge next to the panel title
    [ObservableProperty]
    private string filteredPatientsCount = "";

    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages;

    // Max results shown per page in the dashboard search panel
    private const int PageSize = 5;

    // Full in-memory patient list loaded once from DB
    private List<Patient> _allPatients = [];

    // Paged subset of the current search
    private List<Patient> _searchResults = [];

    // =========================================================
    // APPOINTMENTS
    // =========================================================

    [ObservableProperty]
    private ObservableCollection<DashboardAppointmentItem> appointments = new();

    // =========================================================
    // NOTIFICATIONS
    // =========================================================

    private ObservableCollection<DashboardNotification> _notifications = [];

    public ObservableCollection<DashboardNotification> Notifications
    {
        get => _notifications;
        set => SetProperty(ref _notifications, value);
    }

    // =========================================================
    // INFO
    // =========================================================

    public string CurrentDate => DateTime.Now.ToString("dd MMM yyyy");
    public User? UserName => _auth?.CurrentUser;
    public UserRole UserRole => UserRole.Admin;

    // =========================================================
    // SERVICES
    // =========================================================

    private readonly INavigationService navigationService;
    private readonly IAuthorizationService _policyService;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public DashboardViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ISelectedItemService<Patient> selectedPatient,
        ISelectedItemService<Appointment> selectedAppointment,
        IUserDialogService userDialogService,
        IAuthStateService auth,
        INavigationService navigationService,
        MenuViewModel menu,
        IAuthorizationService policyService)
    {
        _dbFactory=dbFactory;
        _auth=auth;
        _selectedPatient=selectedPatient;
        _selectedAppointment=selectedAppointment;
        _userDialogService=userDialogService;
        this.navigationService=navigationService;
        Menu=menu;
        _policyService=policyService;
    }

    // =========================================================
    // SEARCH — called from TextChanged in code-behind
    // =========================================================

    /// <summary>
    /// Called every time the search Entry text changes.
    /// Shows nothing when the query is blank; shows max PageSize rows otherwise.
    /// </summary>
    public void ApplySearch(string query)
    {
        PatientSearchText=query;

        // Clear everything when the user erases the search box
        if(string.IsNullOrWhiteSpace(query))
        {
            _searchResults= [];
            FilteredPatients.Clear();
            FilteredPatientsCount="";
            HasSearchResults=false;
            CurrentPage=1;
            TotalPages=0;
            return;
        }

        // Run the search against the in-memory list loaded at startup
        _searchResults=_allPatients
            .Where(p => p.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.LastName)
            .ToList();

        TotalPages=(int)Math.Ceiling((double)_searchResults.Count/PageSize);
        CurrentPage=1;
        HasSearchResults=true;

        ProjectPage();
    }

    // =========================================================
    // PAGINATION
    // =========================================================

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

    /// <summary>Slices _searchResults into the current page and pushes to FilteredPatients.</summary>
    private void ProjectPage()
    {
        var page = _searchResults
            .Skip((CurrentPage-1)*PageSize)
            .Take(PageSize)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            FilteredPatients.Clear();
            foreach(var p in page)
                FilteredPatients.Add(p);

            FilteredPatientsCount=$"{_searchResults.Count} резултати";
        });
    }

    // =========================================================
    // LOAD / REFRESH
    // =========================================================

    public bool CanManageAppointments => _policyService.CanAccessModule(Modules.Appointments);
    public bool CanCreatePatient => _policyService.CanAccessModule(Modules.Patients);

    [RelayCommand]
    public async Task Initialize()
    {
        if(IsBusy) return;
        await LoadGlobalAsync();
        IsLoaded=true;
    }

    [RelayCommand]
    public async Task Refresh() => await LoadGlobalAsync();

    [RelayCommand]
    public async Task LoadGlobalAsync()
    {
        if(IsBusy) return;
        IsBusy=true;

        try
        {
            await Task.Delay(100);
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Load all patients into memory once — search runs locally from here on
            _allPatients=await db.Patients
                .AsNoTracking()
                .OrderBy(p => p.LastName)
                .ToListAsync();

            var unreadNotifications = await db.Notifications.AsNoTracking()
                .Where(x => !x.IsRead)
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .ToListAsync();

            var cycles = await db.TherapyCycles.AsNoTracking().ToListAsync();

            var upcomingAppointmentsData = await db.Appointments.AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.ScheduledStart>=DateTime.Today
                         &&a.Status==AppointmentStatus.Scheduled)
                .OrderBy(a => a.ScheduledStart)
                .Take(5)
                .ToListAsync();

            // ── KPI ──
            TotalPatients=_allPatients.Count;
            ActiveTherapies=cycles.Count(x => x.Status==TherapyStatus.Active);
            MissedCycles=cycles.Count(x => x.Status==TherapyStatus.Missed);
            CompletedCycles=cycles.Count(x => x.Status==TherapyStatus.Completed);
            CriticalAlerts=unreadNotifications.Count(x => x.Severity==NotificationSeverity.Critical);
            UpcomingAppointmentsCount=upcomingAppointmentsData.Count;

            OverdueCycles=cycles.Count(x =>
                x.Status==TherapyStatus.Missed||
                (x.Appointments.Any(a => a.ScheduledStart<DateTime.Now)
                 &&x.Status!=TherapyStatus.Completed));

            var totalTracked = cycles.Count(x =>
                x.Status==TherapyStatus.Completed||
                x.Status==TherapyStatus.Active||
                x.Status==TherapyStatus.Scheduled||
                x.Status==TherapyStatus.Missed||
                x.Status==TherapyStatus.Suspended);

            var adherenceRate = totalTracked==0
                ? 0
                : ((double)CompletedCycles/totalTracked)*100;

            CompletionStatus=adherenceRate switch
            {
                >=90 => "Excellent",
                >=75 => "Good",
                >=60 => "Moderate",
                >=40 => "Critical",
                _ => "High Risk"
            };

            AdherenceProgress=adherenceRate/100d;
            CompletionPercentage=$"{Math.Round(adherenceRate)}%";
            AdherenceColor=adherenceRate switch
            {
                >=90 => Colors.LimeGreen,
                >=75 => Colors.DodgerBlue,
                >=60 => Colors.Orange,
                _ => Colors.Red
            };

            // ── Reset search panel — do NOT pre-populate FilteredPatients ──
            FilteredPatients.Clear();
            FilteredPatientsCount="";
            HasSearchResults=false;
            _searchResults= [];
            CurrentPage=1;
            TotalPages=0;
            PatientSearchText=string.Empty;

            // ── Appointments ──
            Appointments=new ObservableCollection<DashboardAppointmentItem>(
                upcomingAppointmentsData.Select(a => new DashboardAppointmentItem
                {
                    PatientName=$"{a.Patient.FirstName} {a.Patient.LastName}",
                    ScheduledStart=a.ScheduledStart,
                    Time=a.ScheduledStart.ToString("HH:mm"),
                    RelativeDay="Денес",
                    StatusColor=a.ScheduledStart<DateTime.Now.AddHours(1) ? "#DC2626" : "#2563EB"
                })
            );

            // ── Notifications ──
            Notifications=new ObservableCollection<DashboardNotification>(
                unreadNotifications.Select(n => new DashboardNotification
                {
                    Title=n.Title,
                    Time=GetRelativeTime(n.CreatedAt),
                    Level=MapSeverity(n.Severity.ToString())
                })
            );
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // =========================================================
    // NAVIGATION
    // =========================================================

    [RelayCommand]
    private async Task NavigateToPatients(string? filter = null) =>
        await Shell.Current.GoToAsync(AppRoutes.Patients.List);

    [RelayCommand]
    private async Task NavigateToAppointments() =>
        await Shell.Current.GoToAsync(AppRoutes.Appointments.List);

    [RelayCommand]
    private async Task NavigateToAlerts() =>
        await Shell.Current.GoToAsync("notifications?filter=critical");

    [RelayCommand]
    private async Task OpenPatient(Patient? patient)
    {
        if(patient is null) return;
        await Shell.Current.GoToAsync($"patient-detail?id={patient.Id}");
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

    [RelayCommand]
    private async Task OpenKpi(string type)
    {
        switch(type)
        {
            case "patients": await NavigateToPatients(AppRoutes.Patients.List); break;
            case "therapies": await Shell.Current.GoToAsync(AppRoutes.Therapy.List); break;
            case "appointments": await NavigateToAppointments(); break;
            case "alerts": await NavigateToAlerts(); break;
        }
    }

    [RelayCommand]
    private async Task AddPatient()
    {
        _selectedPatient.SelectedItem=null;
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
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
}