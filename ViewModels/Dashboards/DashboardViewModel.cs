using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using EHMR.Converters;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;

using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using EHMR.ViewModels.Patients.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAuthStateService _auth;
    private readonly ISelectedItemService<Patient> _selectedPatient;
    private readonly ISelectedItemService<Appointment> _selectedAppointment;
    private readonly IUserDialogService _userDialogService;

    private string? _pendingSearch;
    private string? _pendingStatus;

    private readonly FilterLookup _cityLookup = PatientFilterLookups.BuildCityLookup();

    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string selectedGender = "All";
    [ObservableProperty] private string selectedBloodType = "All";
    [ObservableProperty] private string selectedCity = "All";
    [ObservableProperty] private string selectedAgeGroup = "All";

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
            if(SelectedStatus==internalValue)
                return;

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
            if(SelectedGender==internalValue)
                return;

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
            if(SelectedBloodType==internalValue)
                return;

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
            if(SelectedCity==internalValue)
                return;

            SelectedCity=internalValue;
            CurrentPage=1;
            ApplySearch(internalValue);
            OnPropertyChanged();
        }
    }

    public string SelectedAgeGroupDisplay
    {
        get => PatientFilterLookups.AgeGroup.ToDisplay(SelectedAgeGroup);
        set
        {
            var internalValue = PatientFilterLookups.AgeGroup.ToInternal(value);
            if(SelectedAgeGroup==internalValue)
                return;

            SelectedAgeGroup=internalValue;
            CurrentPage=1;
            ApplySearch();
            OnPropertyChanged();
        }
    }
    //public List<FFDataGridColumn> ColumnsPatient
    //{
    //    get; set;
    //}
    [ObservableProperty]
    private List<int> weeklyPatients = new();

    [ObservableProperty]
    private List<int> weeklyAppointments = new();

    [ObservableProperty]
    private List<string> chartLabels = new();
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
    private ObservableCollection<FFGridColumn<Patient>> patientColumns = new();
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
    [ObservableProperty]
    private ObservableCollection<Appointment> appList = new();

    // =========================================================
    // NOTIFICATIONS
    // =========================================================

    private ObservableCollection<DashboardNotification> _notifications = [];

    public ObservableCollection<DashboardNotification> Notifications
    {
        get => _notifications;
        set => SetProperty(ref _notifications, value);
    }
    [ObservableProperty]
    private ObservableCollection<ChartPoint> patientTrend = new();

    [ObservableProperty]
    private ObservableCollection<ChartPoint> appointmentTrend = new();

    [ObservableProperty]
    private ObservableCollection<ChartPoint> therapyStats = new();


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
        CityFilterNames=_cityLookup.ToObservableCollection();
      
        SearchCommand=new Command<string>(query => ApplySearch(query));

    }


    /// <summary>
    /// Called every time the search Entry text changes.
    /// Shows nothing when the query is blank; shows max PageSize rows otherwise.
    /// </summary>
    public void ApplySearch(string? query = null)
    {
        if(query!=null)
            PatientSearchText=query;


        IEnumerable<Patient> result = _allPatients;


        // SEARCH
        if(!string.IsNullOrWhiteSpace(PatientSearchText))
        {
            var search = PatientSearchText.Trim();

            result=result.Where(x =>
                (!string.IsNullOrWhiteSpace(x.FullName)&&
                 x.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))

                ||

                (!string.IsNullOrWhiteSpace(x.NationalId)&&
                 x.NationalId.Contains(search, StringComparison.OrdinalIgnoreCase))

                ||

                (!string.IsNullOrWhiteSpace(x.Phone)&&
                 x.Phone.Contains(search, StringComparison.OrdinalIgnoreCase))

                ||

                (!string.IsNullOrWhiteSpace(x.City)&&
                 x.City.Contains(search, StringComparison.OrdinalIgnoreCase))
            );
        }



        // FILTERS

        if(SelectedStatus!="All")
        {
            result=result.Where(x =>
                x.Status.ToString()
                .Equals(
                    SelectedStatus,
                    StringComparison.OrdinalIgnoreCase));
        }


        if(SelectedGender!="All")
        {
            result=result.Where(x =>
                x.Gender.ToString()
                .Equals(
                    SelectedGender,
                    StringComparison.OrdinalIgnoreCase));
        }


        if(SelectedBloodType!="All")
        {
            result=result.Where(x =>
                x.BloodType.Equals(
                    SelectedBloodType,
                    StringComparison.OrdinalIgnoreCase));
        }


        if(SelectedCity!="All")
        {
            result=result.Where(x =>
                x.City.Equals(
                    SelectedCity,
                    StringComparison.OrdinalIgnoreCase));
        }


        if(SelectedAgeGroup!="All")
        {
            result=result.Where(x =>
                x.Age.IsInAgeGroup(
                    SelectedAgeGroup));
        }



        _searchResults=result
            .OrderBy(x => x.LastName)
            .ToList();



        TotalPages=Math.Max(1, (int)Math.Ceiling(_searchResults.Count/(double)PageSize));


        CurrentPage=1;


        HasSearchResults=_searchResults.Any();


        ProjectPage();
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

    /// <summary>Slices _searchResults into the current page and pushes to FilteredPatients.</summary>
private void ProjectPage()
{
    var page = _searchResults
        .Skip((CurrentPage - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    MainThread.BeginInvokeOnMainThread(() =>
    {
        // Pushing a fresh collection instantly triggers a single, clean UI refresh
        FilteredPatients = new ObservableCollection<Patient>(page);
        
        FilteredPatientsCount = $"{_searchResults.Count} резултати";
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
    private void InitializeChartState()
    {
        PatientTrend=new ObservableCollection<ChartPoint>();
        AppointmentTrend=new ObservableCollection<ChartPoint>();
        TherapyStats=new ObservableCollection<ChartPoint>();
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
            await using var db = await _dbFactory.CreateDbContextAsync();
           
            // =====================================================
            // BASE DATA LOAD
            // =====================================================

            _allPatients=await db.Patients
                .AsNoTracking()
                .OrderBy(p => p.LastName)
                .ToListAsync();



            var notifications = await db.Notifications
                .AsNoTracking()
                .Where(x => !x.IsRead)
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .ToListAsync();

            var cycles = await db.TherapyCycles
                .AsNoTracking()
                .Include(x => x.Appointments)
                .ToListAsync();

            var appointments = await db.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
             .Where(a =>
    a.ScheduledStart.Date>=DateTime.Today)
                .OrderBy(a => a.ScheduledStart)
                .ToListAsync();

            // =====================================================
            // KPI CALCULATION
            // =====================================================

            TotalPatients=_allPatients.Count;
            ActiveTherapies=cycles.Count(x => x.Status==TherapyStatus.Active);
            CompletedCycles=cycles.Count(x => x.Status==TherapyStatus.Completed);
            MissedCycles=cycles.Count(x => x.Status==TherapyStatus.Missed);
            CriticalAlerts=notifications.Count(x => x.Severity==NotificationSeverity.Critical);
            UpcomingAppointmentsCount=appointments.Count;

            OverdueCycles=cycles.Count(x =>
                x.Status==TherapyStatus.Missed||
                (x.Appointments.Any(a => a.ScheduledStart<DateTime.Now)
                 &&x.Status!=TherapyStatus.Completed));

            // =====================================================
            // ADHERENCE KPI
            // =====================================================

            var tracked = cycles.Count(x =>
                x.Status==TherapyStatus.Completed||
                x.Status==TherapyStatus.Active||
                x.Status==TherapyStatus.Missed||
                x.Status==TherapyStatus.Suspended||
                x.Status==TherapyStatus.Scheduled);

            var rate = tracked==0
                ? 0
                : (double)CompletedCycles/tracked;

            AdherenceProgress=rate;
            CompletionPercentage=$"{Math.Round(rate*100)}%";

            CompletionStatus=rate switch
            {
                >=0.90 => "Excellent",
                >=0.75 => "Good",
                >=0.60 => "Moderate",
                >=0.40 => "Critical",
                _ => "High Risk"
            };

            AdherenceColor=rate switch
            {
                >=0.90 => Colors.LimeGreen,
                >=0.75 => Colors.DodgerBlue,
                >=0.60 => Colors.Orange,
                _ => Colors.Red
            };

            // =====================================================
            // WEEKLY TREND (REAL DATA)
            // =====================================================

            InitializeChartState();

            // =====================================================
            // WEEKLY LABELS BASE
            // =====================================================
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-6+i))
                .ToList();

            ChartLabels=last7Days
                .Select(d => d.ToString("ddd"))
                .ToList();


            // =====================================================
            // CHART DATA (REAL BUSINESS DATA — NOT HARDCODED)
            // =====================================================

            // =====================================================
            // PATIENT TREND (REAL DATA EXAMPLE)
            // =====================================================
            PatientTrend=new ObservableCollection<ChartPoint>(
                last7Days.Select(day => new ChartPoint
                {
                    Label=day.ToString("ddd"),
                    Value=_allPatients.Count(p =>
                        p.CreatedAt.Date==day.Date) // adjust if no CreatedAt
                })
            );

            // =====================================================
            // APPOINTMENT TREND
            // =====================================================
            AppointmentTrend=new ObservableCollection<ChartPoint>(
                last7Days.Select(day => new ChartPoint
                {
                    Label=day.ToString("ddd"),
                    Value=appointments.Count(a =>
                        a.ScheduledStart.Date==day.Date)
                })
            );

            // =====================================================
            // THERAPY STATS
            // =====================================================
            TherapyStats=new ObservableCollection<ChartPoint>
{
    new() { Label = "\\Завршени", Value = cycles.Count(x => x.Status == TherapyStatus.Completed) },
    new() { Label = "Во тек", Value = cycles.Count(x => x.Status == TherapyStatus.Active) },
    new() { Label = "Чекаат", Value = cycles.Count(x => x.Status == TherapyStatus.Scheduled) }
};

            // =====================================================
            // SEARCH RESET
            // =====================================================

            if(string.IsNullOrWhiteSpace(PatientSearchText))
            {
                // Populate search results with all loaded patients if search string is empty
                _searchResults=_allPatients.ToList();
                TotalPages=(int)Math.Ceiling((double)_searchResults.Count/PageSize);
                CurrentPage=1;
                HasSearchResults=false;
                ProjectPage(); // This slices the list and populates FilteredPatients
            }
            _searchResults=_allPatients.ToList();

            CurrentPage=1;

            ApplySearch();


            // =====================================================
            // APPOINTMENTS UI MODEL
            // =====================================================

            Appointments=new ObservableCollection<DashboardAppointmentItem>(
    appointments.Take(5).Select(a => new DashboardAppointmentItem
    {
        Source=a,
        PatientName=$"{a.Patient.FirstName} {a.Patient.LastName}",
        ScheduledStart=a.ScheduledStart,
        Time=a.ScheduledStart.ToString("HH:mm"),
        RelativeDay=a.ScheduledStart.Date==DateTime.Today ? "Денес" : " Друг Ден",
        StatusColor=a.ScheduledStart<DateTime.Now.AddHours(1) ? "#DC2626" : "#2563EB"
    })
);

            // =====================================================
            // NOTIFICATIONS
            // =====================================================

            Notifications=new ObservableCollection<DashboardNotification>(
                notifications.Select(n => new DashboardNotification
                {
                    Title=n.Title,
                    Time=GetRelativeTime(n.CreatedAt),
                    Level=MapSeverity(n.Severity.ToString())
                })
            );

            InitializeSparkControls();
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

    // was missing entirely even though XAML binds NavigateToTherapiesCommand
    [RelayCommand]
    private async Task NavigateToTherapies(string? statusFilter = null)
    {
        var query = new Dictionary<string, object>();

        if(!string.IsNullOrWhiteSpace(statusFilter))
            query["statusFilter"]=statusFilter;

        await Shell.Current.GoToAsync(AppRoutes.Therapy.List, query);
    }

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
    [RelayCommand]
    private async Task Select(Patient? patient)
    {
        await OpenPatient(patient);
    }

    [RelayCommand]
    private async Task Edit(Patient? patient)
    {
        if(patient is null) return;
        _selectedPatient.SelectedItem=patient;
        // Assuming your edit page routes to details or a dedicated editor page
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }
    // =========================================================
    // HELPERS
    // =========================================================
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

        OnPropertyChanged(nameof(SelectedStatusDisplay));
        OnPropertyChanged(nameof(SelectedGenderDisplay));
        OnPropertyChanged(nameof(SelectedBloodTypeDisplay));
        OnPropertyChanged(nameof(SelectedCityDisplay));
        OnPropertyChanged(nameof(SelectedAgeGroupDisplay));

        CurrentPage=1;
        PatientSearchText="";
        ApplySearch();
    }
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

    // ============================================================
    // TAB STRIP  ("All Users" / "Churned" from the mock)
    // ============================================================
    public ObservableCollection<SparkTabItem> Tabs { get; } = new();

    private SparkTabItem _allPatientsTab;
    private SparkTabItem _churnedTab;
    private SparkTabItem _warning;
    private SparkTabItem _encounters;

    // TODO: point this at whatever your PatientStatus enum actually calls the "left the practice" state
    // (e.g. PatientStatus.Inactive, PatientStatus.Discharged). Left as a string so it's a one-line fix.
    private const string ChurnedStatusName = "Inactive";
    private const string EncounterStatusName = "Inactive";
    private const string WarningStatusName = "Inactive";
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private int churnedCount;

    private void BuildSparkTabs()
    {
        _allPatientsTab=new SparkTabItem { Title="Сите пациенти", IsSelected=true };
        _churnedTab=new SparkTabItem { Title="Неактивни" };
        _encounters=new SparkTabItem { Title="Прегледи" };
        //_churnedTab=new SparkTabItem { Title="Терапии" };
        _warning=new SparkTabItem { Title="Критични" };

        // Tabs act as quick status filters over the existing grid, same as the "All Users"/"Churned"
        // tabs in the mock — they don't navigate away. Swap the bodies for NavigateToPatients/
        // NavigateToChurned if you'd rather have them jump to a different page instead.
        _allPatientsTab.Command=new RelayCommand(() =>
        {
            _allPatientsTab.IsSelected=true;
            _churnedTab.IsSelected=false;
            _warning.IsSelected=false;
            _encounters.IsSelected=false;
            SelectedStatusDisplay =PatientFilterLookups.Status.ToDisplay("All");
        });

        _churnedTab.Command=new RelayCommand(() =>
        {
            _allPatientsTab.IsSelected=false;
            _churnedTab.IsSelected=true;
            _warning.IsSelected=false;
            _encounters.IsSelected=false;
            SelectedStatusDisplay=PatientFilterLookups.Status.ToDisplay(ChurnedStatusName);
        });
        _encounters.Command=new RelayCommand(() =>
        {
            _allPatientsTab.IsSelected=false;
            _encounters.IsSelected=true;
            _warning.IsSelected=false;
            _churnedTab.IsSelected=false;
            SelectedStatusDisplay=PatientFilterLookups.Status.ToDisplay(EncounterStatusName);
        });
        _warning.Command=new RelayCommand(() =>
        {
            _allPatientsTab.IsSelected=false;
            _encounters.IsSelected=false;
            _warning.IsSelected=true;
            _churnedTab.IsSelected=false;
            SelectedStatusDisplay=PatientFilterLookups.Status.ToDisplay(WarningStatusName);
        });
        Tabs.Add(_allPatientsTab);
        Tabs.Add(_warning);

        Tabs.Add(_encounters);
        Tabs.Add(_churnedTab);

        RefreshSparkTabCounts();
    }

    /// <summary>Call after TotalPatients/ChurnedCount change (already called at the end of LoadGlobalAsync below).</summary>
    private void RefreshSparkTabCounts()
    {
        if(_allPatientsTab==null) return;
        _allPatientsTab.Value=TotalPatients.ToString("N0");
        _churnedTab.Value=ChurnedCount.ToString("N0");
        _warning.Value=CriticalAlerts.ToString("N0");
        _encounters.Value=Appointments.Count.ToString("N0");
    }

    // ============================================================
    // PICKERS  (Status / Gender / BloodType / City / AgeGroup)
    // ============================================================
    public ObservableCollection<SparkPickerItem> Pickers { get; } = new();

    private SparkPickerItem _statusPicker, _genderPicker, _bloodTypePicker, _cityPicker, _ageGroupPicker;

    private void BuildSparkPickers()
    {
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

    /// <summary>Call after ClearFilters() resets the SelectedXDisplay properties, so the pickers show "Сите" again.</summary>
    private void SyncSparkPickersFromFilters()
    {
        if(_statusPicker==null) return;
        _statusPicker.SelectedItem=SelectedStatusDisplay;
        _genderPicker.SelectedItem=SelectedGenderDisplay;
        _bloodTypePicker.SelectedItem=SelectedBloodTypeDisplay;
        _cityPicker.SelectedItem=SelectedCityDisplay;
        _ageGroupPicker.SelectedItem=SelectedAgeGroupDisplay;
    }

    // ============================================================
    // BUTTONS  ("✕ Исчисти")
    // ============================================================
    public ObservableCollection<SparkButtonItem> Buttons { get; } = new();

    private void BuildSparkButtons()
    {
        Buttons.Add(new SparkButtonItem
        {
            Label="✕ Исчисти",
            IsPrimary=true,
            Command=ClearFiltersCommand
        });
    }

    // ============================================================
    // GRID  (columns match ЕМБГ / ПАЦИЕНТ / ПОЛ / ВОЗРАСТ / КРВ / ТЕЛЕФОН / СТАТУС / АКЦИИ)
    // ============================================================
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private ObservableCollection<SparkGridColumn> gridColumns = new();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
{
    new()
    {
        Header = "ЕМБГ",
        Key = "NationalId",
        Width = new GridLength(1.3, GridUnitType.Star)
    },

    new()
    {
        Header = "ПАЦИЕНТ",
        Key = "FullName",
        Width = new GridLength(2.8, GridUnitType.Star)
    },

    new()
    {
        Header = "ПОЛ",
        Key = "Gender",
        Width = new GridLength(0.8, GridUnitType.Star)
    },

    new()
    {
        Header = "ВОЗРАСТ",
        Key = "Age",
        CellType = SparkGridCellType.Number,
        Width = new GridLength(0.9, GridUnitType.Star)
    },

    new()
    {
        Header = "КРВ",
        Key = "BloodType",
        Width = new GridLength(0.8, GridUnitType.Star)
    },

    new()
    {
        Header = "ТЕЛЕФОН",
        Key = "Phone",
        Width = new GridLength(1.5, GridUnitType.Star)
    },

    new()
    {
        Header = "СТАТУС",
        Key = "Status",
        CellType = SparkGridCellType.Badge,
        Width = new GridLength(1.2, GridUnitType.Star)
    },

    new()
    {
        Header = "АКЦИИ",
        Key = "Actions",
        CellType = SparkGridCellType.Actions,
        Width = new GridLength(0.9, GridUnitType.Star)
    }
};
    }

    /// <summary>CommunityToolkit hook — fires automatically whenever FilteredPatients is reassigned (ProjectPage() does this).</summary>
    partial void OnFilteredPatientsChanged(ObservableCollection<Patient> value) => RefreshSparkGridRows();

    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var p in FilteredPatients)
        {
            var row = new SparkGridRow { Tag=p };
            row["NationalId"]=p.NationalId;
            row["FullName"]=p.FullName;
            row["Gender"]=p.Gender.ToString();
            row["Age"]=p.Age;
            row["BloodType"]=p.BloodType;
            row["Phone"]=p.Phone;
            row["Status"]=new SparkBadgeValue(p.Status.ToString(), StatusToTone(p.Status.ToString()));
            rows.Add(row);
        }

        GridRows=rows;
    }

    private static SparkBadgeTone StatusToTone(string status) => status?.ToLowerInvariant() switch
    {
        "active" => SparkBadgeTone.Success,
        "inactive" or "discharged" or "missed" => SparkBadgeTone.Danger,
        _ => SparkBadgeTone.Neutral
    };

    // ============================================================
    // WIRING — call this once from the constructor (after the existing
    // field assignments), and call RefreshSparkTabCounts() at the end of
    // LoadGlobalAsync (right after TotalPatients/ChurnedCount are set),
    // and SyncSparkPickersFromFilters() at the end of ClearFilters().
    // ============================================================
    private void InitializeSparkControls()
    {
        BuildSparkTabs();
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
        RefreshSparkGridRows();
    }
}