using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using EHMR.ViewModels.Constants;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Encounters;

public partial class EncounterListViewModel : BaseViewModel<Encounter>, IQueryAttributable
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Encounter> _selectedItemService;

    private string _pendingSearch = string.Empty;
    private string _pendingStatus = string.Empty;

    // NOTE: local CurrentPage/TotalPages/PageSize removed — they were shadowing
    // BaseViewModel<T>'s paging properties (const PageSize=10 in particular never
    // actually reached the base's real PageSize, which stayed at its default 25).
    // PageSize is now set on the base property in the constructor.

    // =====================================================
    // BACKING FIELDS FOR INTERNAL KEYS
    // =====================================================
    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string selectedPriority = "All";
    [ObservableProperty] private string selectedEncounterType = "All";
    [ObservableProperty] private string selectedDoctor = "All";

    /// <summary>Module key used by BaseViewModel&lt;T&gt;.EvaluatePermissions().</summary>
    protected override string ModuleName => "encounters";

    // Aliases kept for XAML compatibility — forward to the base class's Can* flags.
    public bool CanCreateEncounter => CanCreate;
    public bool CanUpdateEncounter => CanUpdate;
    public bool CanDeleteEncounter => CanDelete;

    // =====================================================
    // CACHED UI LOOKUPS
    // =====================================================
    public ObservableCollection<string> StatusFilters
    {
        get;
    } =
        new(new[] { "Сите" }.Concat(EncounterStatusSchema.Display.Values.ToObservableCollection()));
    public ObservableCollection<string> PriorityFilters
    {
        get;
    } =
        new(new[] { "Сите" }.Concat(EncounterPrioritySchema.Display.Values.ToObservableCollection()));
    public ObservableCollection<string> EncounterTypeFilters
    {
        get;
    } =
        new(new[] { "Сите" }.Concat(EncounterTypeSchema.Display.Values));

    // =====================================================
    // DATE FILTER
    // =====================================================
    private bool _filterByDate;
    private DateTime _filterDate = DateTime.Today;

    public bool FilterByDate
    {
        get => _filterByDate;
        set
        {
            if(SetProperty(ref _filterByDate, value))
                ApplyPipeline(); // OnPipelineApplied() now handles RefreshStatistics()
        }
    }

    public DateTime FilterDate
    {
        get => _filterDate;
        set
        {
            if(SetProperty(ref _filterDate, value)&&FilterByDate)
                ApplyPipeline();
        }
    }

    public string SelectedStatusDisplay
    {
        get => EncounterStatusSchema.ToDisplay(SelectedStatus);
        set
        {
            var internalValue = EncounterStatusSchema.ToKeyFromDisplay(value);
            if(SelectedStatus==internalValue) return;
            SelectedStatus=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    public string SelectedPriorityDisplay
    {
        get => EncounterPrioritySchema.ToDisplay(SelectedPriority);
        set
        {
            var internalValue = EncounterPrioritySchema.ToKeyFromDisplay(value);
            if(SelectedPriority==internalValue) return;
            SelectedPriority=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    public string SelectedEncounterTypeDisplay
    {
        get => EncounterTypeSchema.ToDisplay(SelectedEncounterType);
        set
        {
            var internalValue = EncounterTypeSchema.ToKeyFromDisplay(value);
            if(SelectedEncounterType==internalValue) return;
            SelectedEncounterType=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    // =====================================================
    // SPARK GRID
    // =====================================================
    [ObservableProperty] private ObservableCollection<Encounter> filteredEncounters = new();
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    protected override void OnPageProjected(ObservableCollection<Encounter> page)
        => FilteredEncounters=page;

    partial void OnFilteredEncountersChanged(ObservableCollection<Encounter> value) => RefreshSparkGridRows();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "БРОЈ", Key = "EncounterNumber", Width = new GridLength(1.1, GridUnitType.Star) },
            new() { Header = "ПАЦИЕНТ", Key = "PatientName", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "ДОКТОР", Key = "DoctorName", Width = new GridLength(1.8, GridUnitType.Star) },
            new() { Header = "ТИП", Key = "EncounterType", Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "ПРИОРИТЕТ", Key = "Priority", CellType = SparkGridCellType.Badge, Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "ДАТУМ", Key = "Date", Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "АКЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var e in FilteredEncounters)
        {
            var row = new SparkGridRow { Tag=e };
            row["EncounterNumber"]=e.EncounterNumber;
            row["PatientName"]=e.Patient!=null ? $"{e.Patient.FirstName} {e.Patient.LastName}" : "";
            row["DoctorName"]=e.Doctor?.User!=null ? $"{e.Doctor.User.FirstName} {e.Doctor.User.LastName}" : "";
            row["EncounterType"]=e.EncounterType;
            row["Priority"]=new SparkBadgeValue(e.Priority, PriorityToTone(e.Priority));
            row["Date"]=(e.ScheduledStart??e.EncounterDate).ToString("dd.MM.yyyy HH:mm");
            row["Status"]=new SparkBadgeValue(e.Status.ToString(), StatusToTone(e.Status));
            rows.Add(row);
        }

        GridRows=rows;
    }

    private static SparkBadgeTone PriorityToTone(string priority) => priority?.ToLowerInvariant() switch
    {
        "stat" or "emergency" => SparkBadgeTone.Danger,
        "urgent" => SparkBadgeTone.Warning,
        _ => SparkBadgeTone.Neutral
    };

    private static SparkBadgeTone StatusToTone(EncounterStatus status) => status switch
    {
        EncounterStatus.Completed => SparkBadgeTone.Success,
        EncounterStatus.Cancelled or EncounterStatus.NoShow => SparkBadgeTone.Danger,
        EncounterStatus.InProgress => SparkBadgeTone.Warning,
        _ => SparkBadgeTone.Neutral
    };

    // ============================================================
    // PICKERS (Pickers collection + MakePicker helper now live in BaseViewModel<T>)
    // ============================================================
    private SparkPickerItem _statusPicker, _priorityPicker, _typePicker;

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _statusPicker=MakePicker("Статус", StatusFilters, SelectedStatusDisplay,
            selected => SelectedStatusDisplay=selected);

        _priorityPicker=MakePicker("Приоритет", PriorityFilters, SelectedPriorityDisplay,
            selected => SelectedPriorityDisplay=selected);

        _typePicker=MakePicker("Тип", EncounterTypeFilters, SelectedEncounterTypeDisplay,
            selected => SelectedEncounterTypeDisplay=selected);

        Pickers.Add(_statusPicker);
        Pickers.Add(_priorityPicker);
        Pickers.Add(_typePicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_statusPicker==null) return;
        _statusPicker.SelectedItem=SelectedStatusDisplay;
        _priorityPicker.SelectedItem=SelectedPriorityDisplay;
        _typePicker.SelectedItem=SelectedEncounterTypeDisplay;
    }

    // Buttons collection lives in BaseViewModel<T>; only content differs here,
    // and it's identical to the base default — so no override needed at all.
    // (Left BuildSparkButtons out entirely; base's default "✕ Исчисти" button applies.)

    // ============================================================
    // TABS (entity-specific — no base equivalent yet)
    // ============================================================
    private SparkTabItem _allTab, _waitingTab, _inProgressTab, _completedTab, _cancelledTab;
    public ObservableCollection<SparkTabItem> Tabs { get; } = new();

    private void BuildSparkTabs()
    {
        Tabs.Clear();

        _allTab=new SparkTabItem { Title="Сите прегледи", IsSelected=true };
        _waitingTab=new SparkTabItem { Title="Закажани" };
        _inProgressTab=new SparkTabItem { Title="Во тек" };
        _completedTab=new SparkTabItem { Title="Завршени" };
        _cancelledTab=new SparkTabItem { Title="Откажани" };

        _allTab.Command=new RelayCommand(() => SelectTab(_allTab,
            () => SelectedStatusDisplay=EncounterStatusSchema.ToDisplay("All")));

        _waitingTab.Command=new RelayCommand(() => SelectTab(_waitingTab,
            () => SelectedStatusDisplay=EncounterStatusSchema.ToDisplay(nameof(EncounterStatus.Scheduled))));

        _inProgressTab.Command=new RelayCommand(() => SelectTab(_inProgressTab,
            () => SelectedStatusDisplay=EncounterStatusSchema.ToDisplay(nameof(EncounterStatus.InProgress))));

        _completedTab.Command=new RelayCommand(() => SelectTab(_completedTab,
            () => SelectedStatusDisplay=EncounterStatusSchema.ToDisplay(nameof(EncounterStatus.Completed))));

        _cancelledTab.Command=new RelayCommand(() => SelectTab(_cancelledTab,
            () => SelectedStatusDisplay=EncounterStatusSchema.ToDisplay(nameof(EncounterStatus.Cancelled))));

        Tabs.Add(_allTab);
        Tabs.Add(_waitingTab);
        Tabs.Add(_inProgressTab);
        Tabs.Add(_completedTab);
        Tabs.Add(_cancelledTab);

        RefreshSparkTabCounts();
    }

    private void SelectTab(SparkTabItem tab, Action action)
    {
        foreach(var t in Tabs) t.IsSelected=false;
        tab.IsSelected=true;
        action();
        SyncSparkPickersFromFilters();
    }

    private void RefreshSparkTabCounts()
    {
        if(_allTab==null) return;
        _allTab.Value=TotalEncounters.ToString("N0");
        _waitingTab.Value=WaitingCount.ToString("N0");
        _inProgressTab.Value=InProgressCount.ToString("N0");
        _completedTab.Value=CompletedCount.ToString("N0");
        _cancelledTab.Value=CancelledCount.ToString("N0");
    }

    private void InitializeSparkControls()
    {
        BuildSparkTabs();
        BuildSparkPickers();
        BuildSparkButtons(); // base default, unless you want a custom set later
        BuildSparkGridColumns();
        RefreshSparkGridRows();
    }

    // =====================================================
    // STATS — now refreshed automatically via OnPipelineApplied()
    // =====================================================
    [ObservableProperty] private int totalEncounters;
    [ObservableProperty] private int waitingCount;
    [ObservableProperty] private int inProgressCount;
    [ObservableProperty] private int completedCount;
    [ObservableProperty] private int cancelledCount;

    protected override void OnPipelineApplied() => RefreshStatistics();

    private void RefreshStatistics()
    {
        var data = AllItems;
        if(data==null) return;

        TotalEncounters=data.Count;
        WaitingCount=data.Count(x => x.Status==EncounterStatus.Scheduled);
        InProgressCount=data.Count(x => x.Status==EncounterStatus.InProgress);
        CompletedCount=data.Count(x => x.Status==EncounterStatus.Completed);
        CancelledCount=data.Count(x => x.Status==EncounterStatus.Cancelled);

        RefreshSparkTabCounts();
    }

    // =====================================================
    // CTOR
    // =====================================================
    public EncounterListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IAuthorizationService authService,
        ISelectedItemService<Encounter> selectedItemService)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _dbFactory=dbFactory;
        _selectedItemService=selectedItemService;
  
        PageSize=10; // sets BaseViewModel<T>.PageSize
        EvaluatePermissions(); // base method, uses ModuleName
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _pendingSearch=query.TryGetValue("search", out var s) ? s?.ToString()??string.Empty : string.Empty;
        _pendingStatus=query.TryGetValue("statusFilter", out var f) ? f?.ToString()??string.Empty : string.Empty;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;
        try
        {
            IsBusy=true;
            ClearError();

            await using var db = await _dbFactory.CreateDbContextAsync();

            var loadedItems = await db.Encounters
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Doctor).ThenInclude(d => d.User)
                .Include(x => x.Appointment)
                .OrderByDescending(x => x.ScheduledStart??x.EncounterDate)
                .ToListAsync();

            AllItems=loadedItems;
            InitializeSparkControls();

            ApplyPendingQuery();
            ApplyPipeline(); // OnPipelineApplied() runs RefreshStatistics() automatically
        }
        catch(Exception ex)
        {
            OnError($"Грешка при вчитување: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    private void ApplyPendingQuery()
    {
        if(!string.IsNullOrWhiteSpace(_pendingSearch))
            SearchText=_pendingSearch;

        if(!string.IsNullOrWhiteSpace(_pendingStatus))
            SelectedStatus=_pendingStatus;

        _pendingSearch=string.Empty;
        _pendingStatus=string.Empty;
    }

    // =====================================================
    // PIPELINE HOOKS
    // =====================================================
    protected override IEnumerable<Encounter> ApplySearch(IEnumerable<Encounter> query, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return query;

        var term = search.Trim();
        return query.Where(x =>
            (x.EncounterNumber??"").Contains(term, StringComparison.OrdinalIgnoreCase)||
            (x.Patient!=null&&(x.Patient.FirstName+" "+x.Patient.LastName).Contains(term, StringComparison.OrdinalIgnoreCase))||
            (x.Doctor!=null&&(x.Doctor.User.FirstName+" "+x.Doctor.User.LastName).Contains(term, StringComparison.OrdinalIgnoreCase))||
            (x.ChiefComplaint??"").Contains(term, StringComparison.OrdinalIgnoreCase)||
            (x.ReasonForVisit??"").Contains(term, StringComparison.OrdinalIgnoreCase)
        );
    }

    protected override IEnumerable<Encounter> ApplyFilters(IEnumerable<Encounter> query)
    {
        if(SelectedStatus!="All")
            query=query.Where(x => string.Equals(x.Status.ToString(), SelectedStatus, StringComparison.OrdinalIgnoreCase));

        if(SelectedPriority!="All")
            query=query.Where(x => string.Equals(x.Priority, SelectedPriority, StringComparison.OrdinalIgnoreCase));

        if(SelectedEncounterType!="All")
            query=query.Where(x => string.Equals(x.EncounterType, SelectedEncounterType, StringComparison.OrdinalIgnoreCase));

        if(SelectedDoctor!="All"&&!string.IsNullOrWhiteSpace(SelectedDoctor))
        {
            query=query.Where(x => x.Doctor!=null&&
                (x.Doctor.User.FirstName+" "+x.Doctor.User.LastName)
                .Contains(SelectedDoctor, StringComparison.OrdinalIgnoreCase));
        }

        if(FilterByDate)
            query=query.Where(x => (x.ScheduledStart??x.EncounterDate).Date==FilterDate.Date);

        return query;
    }

    protected override IEnumerable<Encounter> ApplySort(IEnumerable<Encounter> query)
        => query.OrderByDescending(x => x.ScheduledStart??x.EncounterDate);

    // =====================================================
    // COMMANDS
    // =====================================================
    [RelayCommand] private async Task NewEncounter() => await NavigationService.GoToAsync(AppRoutes.Encounters.Create);

    [RelayCommand]
    private async Task OpenEncounter(Encounter? encounter)
    {
        if(encounter==null) return;
        _selectedItemService.SelectedItem=encounter;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Detail);
    }

    [RelayCommand]
    private async Task EditEncounter(Encounter? encounter)
    {
        if(encounter==null) return;
        _selectedItemService.SelectedItem=encounter;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Edit);
    }

    // ClearFilters command now lives in BaseViewModel<T>: it calls
    // ResetFilters() → ApplyPipeline() → SyncSparkPickersFromFilters().
    // OnPipelineApplied() (above) takes care of RefreshStatistics() automatically,
    // so no separate stats call or reentrancy guard is needed here.
    protected override void ResetFilters()
    {
        SearchText=string.Empty;

        // Set fields directly (not via the *Display setters) to avoid firing
        // ApplyPipeline() multiple times before the base command runs it once.
        SelectedStatus="All";
        SelectedPriority="All";
        SelectedEncounterType="All";
        SelectedDoctor="All";
        _filterByDate=false;   // direct field set — skips FilterByDate's own ApplyPipeline() call
        _filterDate=DateTime.Today;

        OnPropertyChanged(nameof(FilterByDate));
        OnPropertyChanged(nameof(FilterDate));
        OnPropertyChanged(nameof(SelectedStatusDisplay));
        OnPropertyChanged(nameof(SelectedPriorityDisplay));
        OnPropertyChanged(nameof(SelectedEncounterTypeDisplay));
    }
}