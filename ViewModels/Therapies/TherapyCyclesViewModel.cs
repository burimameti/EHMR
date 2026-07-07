using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using EHMR.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EHMR.ViewModels.Therapies;

public partial class TherapyCycleListViewModel : BaseViewModel<TherapyCycle>
{
    private readonly ITherapyService _therapyService;
    private readonly ISelectedItemService<TherapyCycle> _selectedItemService;

    private bool _suppressSearchTextSideEffects;

    [ObservableProperty] private TherapyCycle? selectedCycle;

    protected override string ModuleName => Modules.Therapy;
    public ObservableCollection<SparkTabItem> Tabs { get; } = new();
    public bool CanManageCycles => CanCreate;

    public IReadOnlyList<TherapyCycleStatusOption> StatusFilters
    {
        get;
    } =
    [
        new() { Filter = TherapyCycleStatusFilter.All,       Label = "Сите" },
        // TODO: swap these for the real TherapyStatus enum members
        new() { Filter = TherapyCycleStatusFilter.Active,    Label = "Активен" },
        new() { Filter = TherapyCycleStatusFilter.Completed, Label = "Завршен" },
        new() { Filter = TherapyCycleStatusFilter.Canceled, Label = "Прекинат" },
                new() { Filter = TherapyCycleStatusFilter.Suspended, Label = "Одбиен" },
        new() { Filter = TherapyCycleStatusFilter.Missed,    Label = "Пропуштен" }
    ];

    private TherapyCycleStatusOption _selectedStatus;

    public TherapyCycleStatusOption SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if(!SetProperty(ref _selectedStatus, value)) return;
            SyncSparkPickersFromFilters();
            SyncSparkTabsFromFilters();
            ApplyPipeline();
        }
    }

    public TherapyCycleListViewModel(
        ITherapyService therapyService,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<TherapyCycle> selectedItemService,
        IAuthorizationService authService)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _therapyService=therapyService;
        _selectedItemService=selectedItemService;
        _selectedStatus=StatusFilters.First(x => x.Filter==TherapyCycleStatusFilter.All);

        PageSize=10;

        PropertyChanged+=OnViewModelPropertyChanged;

        EvaluatePermissions();
        InitializeSparkControls();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName!=nameof(SearchText)) return;
        if(_suppressSearchTextSideEffects) return;

        ApplyPipeline();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;
        try
        {
            IsBusy=true;
            ClearError();
            var items = await _therapyService.GetCyclesAsync();
            AllItems=items;

            // Tabs/pickers were built in the ctor, before AllItems existed —
            // counts need a refresh now that real data is in.
            RefreshSparkTabCounts();

            ApplyPipeline();
        }
        catch(Exception ex)
        {
            OnError($"Грешка при вчитување терапевтски циклуси: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    [RelayCommand]
    private async Task SelectAsync(TherapyCycle cycle)
    {
        _selectedItemService.SelectedItem=cycle;
        // TODO: confirm the actual route
        await NavigationService.GoToAsync(AppRoutes.Therapy.Detail);
    }

    [RelayCommand]
    private async Task NewCycle()
    {
        if(!CanManageCycles)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за додавање на нов терапевтски циклус.", "OK");
            return;
        }

        _selectedItemService.SelectedItem=null;
        await NavigationService.GoToAsync(AppRoutes.Therapy.Detail);
    }

    protected override void ResetFilters()
    {
        _suppressSearchTextSideEffects=true;
        SearchText=string.Empty;
        _suppressSearchTextSideEffects=false;

        SelectedStatus=StatusFilters.First(x => x.Filter==TherapyCycleStatusFilter.All);
    }

    // ================= PIPELINE HOOKS =================
    protected override IEnumerable<TherapyCycle> ApplySearch(IEnumerable<TherapyCycle> items, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return items;

        var term = search.Trim();
        return items.Where(x =>
            (x.Patient?.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.CycleNumber?.ToString().Contains(term)??false)||
            (x.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase)??false));
    }

    protected override IEnumerable<TherapyCycle> ApplyFilters(IEnumerable<TherapyCycle> items)
    {
        if(SelectedStatus.Filter==TherapyCycleStatusFilter.All) return items;

        // TODO: match against real TherapyStatus enum values
        var status = Enum.Parse<TherapyStatus>(SelectedStatus.Filter.ToString());
        return items.Where(x => x.Status==status);
    }

    protected override IEnumerable<TherapyCycle> ApplySort(IEnumerable<TherapyCycle> query) =>
        query.OrderByDescending(x => x.StartDate);

    // ============================================================
    // TABS  (status breakdown w/ live counts — same shape as
    // Patients/Appointments; here every non-"All" filter maps
    // 1:1 to a TherapyStatus value, no merged buckets needed.)
    // ============================================================
    private readonly Dictionary<TherapyCycleStatusFilter, SparkTabItem> _statusTabsByFilter = new();

    private void BuildSparkTabs()
    {
        Tabs.Clear();
        _statusTabsByFilter.Clear();

        foreach(var option in StatusFilters)
        {
            var tab = new SparkTabItem
            {
                Title=option.Label,
                IsSelected=SelectedStatus.Filter==option.Filter
            };

            tab.Command=new RelayCommand(() => SelectTab(tab, () => SelectedStatus=option));

            Tabs.Add(tab);
            _statusTabsByFilter[option.Filter]=tab;
        }

        RefreshSparkTabCounts();
    }

    /// <summary>Marks one tab selected and clears the rest, then runs the tab's own action.</summary>
    private void SelectTab(SparkTabItem tab, Action action)
    {
        foreach(var t in Tabs) t.IsSelected=false;
        tab.IsSelected=true;
        action();
    }

    /// <summary>Counts come from AllItems (the full unfiltered set), not the current search/status result.</summary>
    private void RefreshSparkTabCounts()
    {
        foreach(var (filter, tab) in _statusTabsByFilter)
        {
            var count = filter==TherapyCycleStatusFilter.All
                ? AllItems.Count
                : AllItems.Count(x => x.Status==Enum.Parse<TherapyStatus>(filter.ToString()));

            tab.Value=count.ToString("N0");
        }
    }

    /// <summary>Keeps tab highlighting correct regardless of whether SelectedStatus changed via a tab tap or the Picker.</summary>
    private void SyncSparkTabsFromFilters()
    {
        foreach(var (filter, tab) in _statusTabsByFilter)
            tab.IsSelected=filter==SelectedStatus.Filter;
    }

    // ============================================================
    // SPARK CONTROLS
    // ============================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private SparkPickerItem _statusPicker;

    private void InitializeSparkControls()
    {
        BuildSparkTabs();
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
    }

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _statusPicker=MakePicker("Статус", StatusFilters.Select(x => x.Label), SelectedStatus.Label,
            selected =>
            {
                var match = StatusFilters.FirstOrDefault(x => x.Label==selected);
                if(match!=null) SelectedStatus=match;
            });

        Pickers.Add(_statusPicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_statusPicker==null) return;
        _statusPicker.SelectedItem=SelectedStatus.Label;
    }

    protected override void BuildSparkButtons()
    {
        Buttons.Clear();
        Buttons.Add(new SparkButtonItem
        {
            Label="✕ Исчисти",
            IsPrimary=true,
            Command=ClearFiltersCommand
        });
    }

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "ПАЦИЕНТ", Key = "Patient", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "ЦИКЛУС БР.", Key = "CycleNumber", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "ПОЧЕТОК", Key = "StartDate", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "КРАЈ", Key = "EndDate", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "АКЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    protected override void OnPageProjected(ObservableCollection<TherapyCycle> page)
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var c in page)
        {
            var row = new SparkGridRow { Tag=c };
            row["Patient"]=c.Patient?.FullName??"";
            row["CycleNumber"]=c.CycleNumber?.ToString()??"";
            row["StartDate"]=c.StartDate?.ToString("dd.MM.yyyy")??"";
            row["EndDate"]=c.EndDate?.ToString("dd.MM.yyyy")??"";
            row["Status"]=new SparkBadgeValue(StatusLabel(c.Status), StatusToTone(c.Status));
            rows.Add(row);
        }

        GridRows=rows;
    }

    // TODO: replace with real TherapyStatus enum members
    private static string StatusLabel(TherapyStatus? status) => status switch
    {
        TherapyStatus.Planned => "Планиран",
        TherapyStatus.Active => "Активен",
        TherapyStatus.Completed => "Завршен",
        TherapyStatus.Canceled => "Прекинат",
        TherapyStatus.Scheduled => "Закажан",
        TherapyStatus.Suspended => "Одбиен",
        TherapyStatus.Missed => "Пропуштен",
        _ => status?.ToString()??""
    };

    private static SparkBadgeTone StatusToTone(TherapyStatus? status) => status switch
    {
        TherapyStatus.Completed => SparkBadgeTone.Success,
        TherapyStatus.Canceled => SparkBadgeTone.Danger,
        TherapyStatus.Planned => SparkBadgeTone.Warning,
        TherapyStatus.Active => SparkBadgeTone.Success,
        TherapyStatus.Suspended => SparkBadgeTone.Danger,
        TherapyStatus.Missed => SparkBadgeTone.Danger,
        _ => SparkBadgeTone.Neutral
    };
}

public enum TherapyCycleStatusFilter
{
    All,
    Active,
    Completed,
    Canceled, Suspended, Missed,
    Scheduled
}

public sealed class TherapyCycleStatusOption
{
    public TherapyCycleStatusFilter Filter
    {
        get; init;
    }
    public string Label { get; init; } = "";
    public override string ToString() => Label;
}
