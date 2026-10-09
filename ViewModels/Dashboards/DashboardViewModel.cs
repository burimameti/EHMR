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

using EHMR.ViewModels.Dashboard.Models;

using Microsoft.EntityFrameworkCore;

using System.Collections.ObjectModel;
using System.Diagnostics;


namespace EHMR.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    // =========================================================
    // STATE
    // =========================================================
    [ObservableProperty] private DashboardState state = new();
    [ObservableProperty] private ObservableCollection<DashboardAppointmentItem> selectedDateAppointments = new();
    [ObservableProperty] private bool isDateCardExpanded = true;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoaded;

    [RelayCommand]
    private void ToggleDateCard() => IsDateCardExpanded=!IsDateCardExpanded;

    // =========================================================
    // SERVICES / FIELDS
    // =========================================================
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAuthStateService _auth;
    private readonly ISelectedItemService<Patient> _selectedPatient;
    private readonly ISelectedItemService<Encounter> _selectedEncounter;
    private readonly IUserDialogService _userDialogService;
    private readonly INavigationService navigationService;
    private readonly IAuthorizationService _policyService;
    private readonly IAlertService _alertService;
    private readonly IPreferencesService _preferencesService;
    private bool _isSelectingPatientSuggestion;
    private int _patientPreviewVersion;
    private const string LastAlertSweepKey = "alerts_last_sweep_date";
    private List<DashboardPatientAggregate> _allPatients = [];

    // Manual property so the date-changed logic always fires.
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

    // =========================================================
    // CONSTRUCTOR
    // =========================================================
    public DashboardViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ISelectedItemService<Patient> selectedPatient,
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
        _selectedEncounter=selectedEncounter;
        _userDialogService=userDialogService;
        this.navigationService=navigationService;
        _alertService=alertService;
        _preferencesService=preferencesService;
        Menu=menu;
        _policyService=policyService;
        InitializeSparkControls();
    }

    public MenuViewModel Menu
    {
        get;
    }

    // =========================================================
    // PERMISSIONS
    // =========================================================
    public bool CanView => _policyService.CanPerform(Modules.Dashboard, ModuleAction.View);
    public bool CanCreateEncounter => _policyService.CanPerform(Modules.Encounters, ModuleAction.Create);

    // =========================================================
    // HEADER BUTTONS (new collection each time -> control re-renders)
    // =========================================================
    [ObservableProperty] private ObservableCollection<SparkButtonItem> buttons = new();

    // Kept only because the XAML still binds Tabs / TabsVisible.
    public ObservableCollection<SparkTabItem> Tabs { get; } = new();
    [ObservableProperty] private bool showTabs = false;

    private void BuildSparkButtons()
    {
        var list = new List<SparkButtonItem>
        {
            new SparkButtonItem
            {
                Label = "Освежи",
                IsPrimary = true,
                IsEnabled = true,
                Command = RefreshCommand
            }
        };

        if(PreviewPatient?.Patient is { } patient)
        {
            list.Add(new SparkButtonItem
            {
                Label="Нов преглед",
                IsPrimary=true,
                IsEnabled=true,
                Command=NewEncounterForSelectedCommand,
                CommandParameter=patient
            });
        }
        else
        {
            list.Add(new SparkButtonItem
            {
                Label="Нов преглед",
                IsPrimary=true,
                IsEnabled=true, // permission is checked inside NewEncounter()
                Command=NewEncounterCommand
            });
        }

        Buttons=new ObservableCollection<SparkButtonItem>(list);
    }

    // =========================================================
    // PATIENT PREVIEW PANEL
    // =========================================================
    [ObservableProperty] private DashboardPatientAggregate? previewPatient;
    [ObservableProperty] private ObservableCollection<DashboardEncounterItem> previewRecentVisits = new();
    [ObservableProperty] private ObservableCollection<int> availableEncounterYears = new();
    [ObservableProperty] private int selectedEncounterYear = DateTime.Today.Year;
    [ObservableProperty] private bool hasEncounterYearOptions;
    [ObservableProperty] private ObservableCollection<string> previewDiagnoses = new();
    [ObservableProperty] private ObservableCollection<string> previewMedicines = new();
    [ObservableProperty] private bool isPreviewPanelOpen;
    [ObservableProperty] private bool isEncounterHistoryMode;
    private List<Encounter> _previewPatientEncounters = [];

    [RelayCommand]
    private async Task PreviewPatientAsync(object? param)
    {
        var item = param as DashboardPatientAggregate;
        if(item is null)
        {
            ClosePreviewPanel();
            return;
        }

        var previewVersion = ++_patientPreviewVersion;
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
        var years = lastVisits
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

        var visits = _previewPatientEncounters
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
        PreviewDiagnoses=new ObservableCollection<string>();
        PreviewMedicines=new ObservableCollection<string>();
        PreviewRecentVisits=new ObservableCollection<DashboardEncounterItem>();
        _previewPatientEncounters= [];
        AvailableEncounterYears=new ObservableCollection<int>();
        HasEncounterYearOptions=false;
        SelectedEncounterYear=DateTime.Today.Year;
        IsPreviewPanelOpen=false;
        IsEncounterHistoryMode=false;
        GridColumns=new ObservableCollection<SparkGridColumn>();
        GridRows=new ObservableCollection<SparkGridRow>();
        TotalPages=0;
        CurrentPage=1;
        BuildSparkButtons();
    }

    // =========================================================
    // PATIENT SEARCH (suggestions only)
    // =========================================================
    [ObservableProperty] private string patientSearchText = "";
    [ObservableProperty] private ObservableCollection<DashboardPatientAggregate> patientSuggestions = new();
    [ObservableProperty] private DashboardPatientAggregate? selectedPatientSuggestion;
    [ObservableProperty] private bool showPatientSuggestions;
    [ObservableProperty] private bool useCyrillicSearch = true;

    partial void OnPatientSearchTextChanged(string value)
    {
        if(_isSelectingPatientSuggestion) return;

        var query = value?.Trim()??string.Empty;

        if(string.IsNullOrWhiteSpace(query))
        {
            PatientSuggestions=new ObservableCollection<DashboardPatientAggregate>();
            ShowPatientSuggestions=false;
            return;
        }

        var tokens=query.Split(' ', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        var firstToken=tokens[0];
        var lastToken=tokens.Length>1 ? tokens[1] : null;
        var firstAlternates=GetSearchVariants(firstToken);
        var lastAlternates=lastToken is null ? Array.Empty<string>() : GetSearchVariants(lastToken);

        PatientSuggestions=new ObservableCollection<DashboardPatientAggregate>(
            _allPatients
                .Where(x =>
                    StartsWithAny(x.Patient.FirstName, firstAlternates) &&
                    (lastToken is null || StartsWithAny(x.Patient.LastName, lastAlternates)))
                .OrderBy(x => x.Patient.LastName)
                .ThenBy(x => x.Patient.FirstName)
                .Take(8));
        ShowPatientSuggestions=PatientSuggestions.Count>0;
    }

    static string[] GetSearchVariants(string value)
    {
        var variants=new[]
        {
            value,
            MacedonianTransliterator.ToCyrillic(value),
            MacedonianTransliterator.ToLatin(value)
        };

        return variants
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    static bool StartsWithAny(string? value, IEnumerable<string> variants)
        => !string.IsNullOrWhiteSpace(value)
            && variants.Any(v => value.StartsWith(v, StringComparison.OrdinalIgnoreCase));

    partial void OnSelectedPatientSuggestionChanged(DashboardPatientAggregate? value)
    {
        if(value is null)
            return;

        _isSelectingPatientSuggestion=true;
        PatientSearchText=value.Patient.FullName;
        _isSelectingPatientSuggestion=false;
        ShowPatientSuggestions=false;
        PatientSuggestions=new ObservableCollection<DashboardPatientAggregate>();
        _=PreviewPatientAsync(value);
    }

    partial void OnUseCyrillicSearchChanged(bool value) =>
        OnPatientSearchTextChanged(PatientSearchText);

    // =========================================================
    // GRID (encounter history of the selected patient)
    // =========================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages;

    // XAML still binds these; paging is not used for the encounter grid.
    [RelayCommand]
    private void NextPage()
    {
    }
    [RelayCommand]
    private void PreviousPage()
    {
    }
    [RelayCommand]
    private void PageChanged(int page)
    {
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
            var row = new SparkGridRow { Tag=item };
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
    // INITIALIZE / REFRESH
    // =========================================================
    [RelayCommand]
    public async Task Initialize()
    {
        if(IsBusy) return;
        ResyncToToday();
        await LoadGlobalAsync();
        await LoadSelectedDateAppointmentsAsync();
        await LoadTodayEncountersAsync();
        IsLoaded=true;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        ResyncToToday();
        await LoadGlobalAsync();
        await LoadSelectedDateAppointmentsAsync();
        await LoadTodayEncountersAsync();
    }

    private void ResyncToToday()
    {
        _selectedDate=DateTime.Today;
        OnPropertyChanged(nameof(SelectedDate));

        if(DayStripStartDate.Date!=DateTime.Today.AddDays(-3))
            DayStripStartDate=DateTime.Today.AddDays(-3);
    }

    // =========================================================
    // LOAD GLOBAL (patients for suggestions + alert chips)
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

            // ── PATIENTS (suggestions + day strip counts) ─────────────────────────
            var patientsQuery = db.Patients
                .AsNoTracking()
                .Include(p => p.Appointments)
                .Include(p => p.Encounters)
                .Include(p => p.Diagnoses)
                    .ThenInclude(d => d.Mkb10Code)
                .Include(p => p.PatientMedicines)
                    .ThenInclude(pm => pm.Medicine)
                .AsQueryable();

            if(isScoped)
                patientsQuery=patientsQuery.Where(p => p.DoctorId==doctorId);

            var patients = await patientsQuery.ToListAsync();
            _allPatients=patients.Select(CreateDashboardAggregate).ToList();

            // ── ALERT SWEEP ───────────────────────────────────────────────────────
            await RunAlertSweepIfNeededAsync();

            // ── ALERT CHIPS ───────────────────────────────────────────────────────
            var alertsQuery =
                from a in db.Alerts.AsNoTracking()
                join p in db.Patients.AsNoTracking() on a.PatientId equals p.Id
                where !a.IsResolved
                select new
                {
                    a.Level,
                    p.DoctorId
                };

            if(isScoped)
                alertsQuery=alertsQuery.Where(x => x.DoctorId==doctorId);

            var alertRows = await alertsQuery.ToListAsync();
            State.AlertSummaries=BuildAlertSummaries(alertRows.Select(x => x.Level));

            BuildDayStrip();
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
    // TODAY'S ENCOUNTERS (displayed beside date-based appointments)
    // =========================================================
    private async Task LoadTodayEncountersAsync()
    {
        var start = DateTime.Today;
        var end = start.AddDays(1);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.Encounters
                .AsNoTracking()
                .Include(e => e.Patient)
                .Where(e => e.EncounterDate >= start && e.EncounterDate < end)
                .AsQueryable();

            if (_policyService.IsScopedToOwnData && _policyService.CurrentDoctorId is Guid doctorId)
                query = query.Where(e => e.DoctorId == doctorId);

            var encounters = await query
                .OrderByDescending(e => e.ScheduledStart ?? e.EncounterDate)
                .Take(10)
                .ToListAsync();

            var items = encounters.Select(e => new DashboardEncounterItem
            {
                Source = e,
                PatientName = e.Patient != null ? e.Patient.FullName : "—",
                Time = (e.ScheduledStart ?? e.EncounterDate).ToString("HH:mm"),
                StatusText = EncounterStatusDisplay.TryGetValue(e.Status, out var label)
                    ? label
                    : e.Status.ToString(),
                StatusColor = EncounterStatusToColor(e.Status)
            }).ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
                State.DailyEncounters = new ObservableCollection<DashboardEncounterItem>(items));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TodayEncounters] {ex}");
        }
    }

    // =========================================================
    // APPOINTMENTS FOR THE SELECTED DATE
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

    private static readonly Dictionary<AppointmentStatus, string> AppointmentStatusDisplay = new()
    {
        [AppointmentStatus.Completed]="Завршен",
        [AppointmentStatus.InProgress]="Во тек",
        [AppointmentStatus.Cancelled]="Откажан",
    };

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

    private void OnSelectedDateChanged(DateTime value)
    {
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
            var capturedDate = date;
            var day = new DashboardDayItem
            {
                Date=date,
                DayLabel=MkDayAbbrev[(int)date.DayOfWeek],
                DayNumber=date.Day.ToString(),
                IsSelected=date.Date==SelectedDate.Date,
                EncounterCount=encounterCountsByDate.TryGetValue(date.Date, out var count) ? count : 0
            };
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
    // NAVIGATION / ACTIONS
    // =========================================================
    [RelayCommand]
    private async Task NavigateToAlerts(string? level = null)
    {
        var query = new Dictionary<string, object>();

        if(!string.IsNullOrWhiteSpace(level))
            query["level"]=level;

        await navigationService.GoToAsync(AppRoutes.Alerts.List, query);
    }

    [RelayCommand]
    private async Task OpenDashboardGridItem(object? item)
    {
        if(item is DashboardPatientAggregate patient)
            await PreviewPatientAsync(patient);
        else if(item is DashboardEncounterItem encounter)
            await OpenEncounterFromPreview(encounter);
    }

    [RelayCommand]
    private async Task OpenEncounterFromPreview(DashboardEncounterItem? item)
    {
        if(item?.Source is null) return;
        _selectedEncounter.SelectedItem=item.Source;
        _selectedEncounter.OpenInEditMode=false;
        await navigationService.GoToAsync(AppRoutes.Encounters.Detail);
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
    private async Task NewEncounterForSelected(Patient? patient)
    {
        if(patient is null)
        {
            await _userDialogService.ShowAlertAsync("Внимание", "Одберете пациент прво.");
            return;
        }

        if(!CanCreateEncounter)
        {
            await _userDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за додавање на нов преглед.", "OK");
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
    // ALERTS
    // =========================================================
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
        BuildSparkButtons();
        BuildDayStrip();
    }
}