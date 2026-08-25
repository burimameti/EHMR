using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using EHMR.Services;
using EHMR.ViewModels.Patients.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EHMR.ViewModels.Therapies;

public partial class TherapyCycleListViewModel : BaseViewModel<TherapyCycle>
{
    private readonly ITherapyService _therapyService;

    private bool _suppressSearchTextSideEffects;

    protected override string ModuleName => Modules.Therapy;
    protected override string DetailRoute => AppRoutes.Therapy.Detail;
    protected override string PermissionDeniedMessage =>
        "Немате авторизација за додавање на нов терапевтски циклус.";

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
        : base(navigationService, userDialogService, menuService, authService, selectedItemService)
    {
        _therapyService=therapyService;
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


    // ═══════════════════════════════════════════ УРЕДУВАЊЕ НА ЦИКЛУС ═══════════════════════════════════════════
    //
    // Целиот десен панел беше врзан за членови што не постоеја: SelectedCycle,
    // EditStatus, EditStartDate, EditEndDate, EditNotes, SaveCycleChangesCommand,
    // IsCycleSelected. Панелот се прикажуваше, се пополнуваше рачно и копчето
    // „Зачувај" не правеше ништо — корисникот мислеше дека зачувал.

    /// <summary>Македонски натписи за статусот, мапирани на TherapyStatus.</summary>
    public static FilterLookup StatusLookup { get; } = new(new[]
    {
        ("Планиран", "Planned"),
        ("Активен", "Active"),
        ("Закажан", "Scheduled"),
        ("Завршен", "Completed"),
        ("Суспендиран", "Suspended"),
        ("Откажан", "Canceled"),
        ("Пропуштен", "Missed")
    });

    public ObservableCollection<string> StatusOptions
    {
        get;
    } = new(StatusLookup.DisplayValues);

    [ObservableProperty] private TherapyCycle? selectedCycle;

    [ObservableProperty] private string editCycleNumber = string.Empty;
    [ObservableProperty] private string editStatus = "Планиран";
    [ObservableProperty] private DateTime editStartDate = DateTime.Today;
    [ObservableProperty] private DateTime editEndDate = DateTime.Today;
    [ObservableProperty] private string editNotes = string.Empty;

    public bool IsCycleSelected => SelectedCycle is not null;

    partial void OnSelectedCycleChanged(TherapyCycle? value)
    {
        OnPropertyChanged(nameof(IsCycleSelected));

        if(value is null)
            return;

        EditCycleNumber=value.TherapyCyleNumber??string.Empty;
        EditStatus=StatusLookup.ToDisplay(value.Status?.ToString());
        EditStartDate=value.StartDate??DateTime.Today;
        EditEndDate=value.EndDate??value.StartDate??DateTime.Today;
        EditNotes=value.Notes??string.Empty;
    }

    /// <summary>Клик на ред во гридот. SparkDataGridView праќа row.Tag, што овде е самиот циклус.</summary>
    [RelayCommand]
    private void SelectCycle(TherapyCycle? cycle) => SelectedCycle=cycle;

    [RelayCommand]
    private void EditCycle(TherapyCycle? cycle) => SelectedCycle=cycle;

    [RelayCommand]
    private async Task SaveCycleChangesAsync()
    {
        if(SelectedCycle is null)
            return;

        if(!CanUpdate)
        {
            await UserDialogService.ShowAlertAsync(
                PermissionDeniedTitle,
                "Немате авторизација за измена на терапевтски циклус.",
                "OK");
            return;
        }

        if(EditEndDate<EditStartDate)
        {
            OnError("Датумот на завршување не смее да биде пред датумот на почеток.");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            SelectedCycle.TherapyCyleNumber=EditCycleNumber?.Trim()??string.Empty;
            SelectedCycle.Status=Enum.Parse<TherapyStatus>(StatusLookup.ToInternal(EditStatus));
            SelectedCycle.StartDate=EditStartDate;
            SelectedCycle.EndDate=EditEndDate;
            SelectedCycle.Notes=EditNotes?.Trim();

            await _therapyService.UpdateCycleAsync(SelectedCycle);

            // Освежи ја листата за гридот да го покаже новиот статус.
            var saved = SelectedCycle;
            await LoadAsync();
            SelectedCycle=AllItems.FirstOrDefault(x => x.Id==saved.Id)??saved;

            await UserDialogService.ShowAlertAsync("Успешно", "Податоците се зачувани.", "OK");
        }, "Грешка при зачувување на податоци");
    }

    // ================= PIPELINE HOOKS =================
    protected override IEnumerable<TherapyCycle> ApplySearch(IEnumerable<TherapyCycle> items, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return items;

        var term = search.Trim();
        return items.Where(x =>
            (x.Patient?.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.TherapyCyleNumber?.ToString().Contains(term)??false)||
            (x.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase)??false));
    }

    protected override IEnumerable<TherapyCycle> ApplyFilters(IEnumerable<TherapyCycle> items)
    {
        if(SelectedStatus.Filter==TherapyCycleStatusFilter.All) return items;

        var status = Enum.Parse<TherapyStatus>(SelectedStatus.Filter.ToString());
        return items.Where(x => x.Status==status);
    }

    protected override IEnumerable<TherapyCycle> ApplySort(IEnumerable<TherapyCycle> query) =>
        query.OrderByDescending(x => x.StartDate);

    // ============================================================
    // TABS
    // ============================================================  

    private void BuildSparkTabs()
    {
        BuildTabFilters(
            StatusFilters,
            keySelector: o => o.Filter.ToString(),
            labelSelector: o => o.Label,
            isSelectedSelector: o => SelectedStatus.Filter==o.Filter,
            onSelect: o => SelectedStatus=o);

        RefreshSparkTabCounts();
    }
    private void RefreshSparkTabCounts()
    {
        foreach(var option in StatusFilters)
        {
            var count = option.Filter==TherapyCycleStatusFilter.All
                ? AllItems.Count
                : AllItems.Count(x => x.Status==Enum.Parse<TherapyStatus>(option.Filter.ToString()));

            RefreshTabCount(option.Filter.ToString(), count);
        }
    }


    private void SyncSparkTabsFromFilters()
     => SyncTabsFromKey(SelectedStatus.Filter.ToString());

    protected override void ResetFilters()
    {
        _suppressSearchTextSideEffects=true;
        SearchText=string.Empty;
        _suppressSearchTextSideEffects=false;

        SelectedStatus=StatusFilters.First(x => x.Filter==TherapyCycleStatusFilter.All);
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

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "ИМЕ И ПРЕЗИМЕ", Key = "Patient", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "ЦИКЛУС БР.", Key = "CycleNumber", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "ПОЧЕТОК", Key = "StartDate", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "КРАЈ", Key = "EndDate", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "ОПЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    protected override void OnPageProjected(ObservableCollection<TherapyCycle> page)
    {
        var rows = new ObservableCollection<SparkGridRow>();
        foreach(var c in page)
        {
            var row = new SparkGridRow { Tag=c };
            row["Patient"]=c.Patient?.FullName??"";
            row["CycleNumber"]=c.TherapyCyleNumber?.ToString()??"";
            row["StartDate"]=c.StartDate?.ToString("dd.MM.yyyy")??"";
            row["EndDate"]=c.EndDate?.ToString("dd.MM.yyyy")??"";
            row["Status"]=new SparkBadgeValue(StatusLabel(c.Status), StatusToTone(c.Status));
            AddDefaultActions(c, row);

            rows.Add(row);
        }
        GridRows=rows;
    }

    public IReadOnlyList<TherapyCycleStatusOption> StatusFilters
    {
        get;
    } =
    [
        new() { Filter = TherapyCycleStatusFilter.All,       Label = "Сите" },
        new() { Filter = TherapyCycleStatusFilter.Active,    Label = "Активен" },
        new() { Filter = TherapyCycleStatusFilter.Completed, Label = "Завршен" },
        new() { Filter = TherapyCycleStatusFilter.Canceled, Label = "Прекинат" },
        new() { Filter = TherapyCycleStatusFilter.Suspended, Label = "Одбиен" },
        new() { Filter = TherapyCycleStatusFilter.Missed,    Label = "Пропуштен" }
    ];
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