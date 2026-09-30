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
using System.Windows.Input;

namespace EHMR.ViewModels.Mkb10;

public partial class Mkb10CodeListViewModel : BaseViewModel<Mkb10Code>, IQueryAttributable
{
    // ================= SERVICES =================
    private readonly IMkb10CodeService _mkb10Service;

    // ================= FILTER STATE =================
    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string selectedChapter = "All";

    [ObservableProperty] private string filteredCodesCount = "";
    [ObservableProperty] private ObservableCollection<Mkb10Code> filteredCodes = new();

    // ================= QUERY STATE (deep-link support) =================
    private string? _pendingSearch;
    private string? _pendingStatus;
    private readonly FilterLookup _statusLookup = Mkb10FilterLookups.Status;
    private FilterLookup _chapterLookup = FilterLookup.Empty;

    // ================= BASE OVERRIDES =================
    protected override string ModuleName => "mkb10";
    protected override string DetailRoute => AppRoutes.Mkb10.Detail;
    protected override string PermissionDeniedMessage => "Немате авторизација за додавање нова МКБ-10 шифра.";

    public ObservableCollection<string> StatusFilters { get; } = Mkb10FilterLookups.Status.ToObservableCollection();
    public ObservableCollection<string> ChapterFilterNames
    {
        get;
    }

    public string SelectedStatusDisplay
    {
        get => _statusLookup.ToDisplay(SelectedStatus);
        set
        {
            var internalValue = _statusLookup.ToInternal(value);
            if(SelectedStatus==internalValue) return;
            SelectedStatus=internalValue;
            OnPropertyChanged();
        }
    }

    public string SelectedChapterDisplay
    {
        get => _chapterLookup.ToDisplay(SelectedChapter);
        set
        {
            var internalValue = _chapterLookup.ToInternal(value);
            if(SelectedChapter==internalValue) return;
            SelectedChapter=internalValue;
            OnPropertyChanged();
        }
    }

    /// <summary>Alias kept for XAML compatibility — forwards to the base class's SearchText.</summary>
    public string Mkb10SearchText
    {
        get => SearchText;
        set => SearchText=value;
    }

    public ICommand SearchCommand
    {
        get;
    }

    // ================= CTOR =================
    public Mkb10CodeListViewModel(
        IMkb10CodeService mkb10Service,
        ISelectedItemService<Mkb10Code> selectedItemService,
        INavigationService navigationService,
        IUserDialogService dialog,
        IMenuService menu,
        IAuthorizationService authorization)
        : base(navigationService, dialog, menu, authorization, selectedItemService)
    {
        _mkb10Service=mkb10Service;

        PageSize=25;

        SearchCommand=new Command<string>(q => SearchText=q);

        ChapterFilterNames=new ObservableCollection<string>();

        PropertyChanged+=OnViewModelPropertyChanged;

        EvaluatePermissions();
    }

    // =========================================================
    // LOAD
    // =========================================================
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch(e.PropertyName)
        {
            case nameof(SearchText):
                OnPropertyChanged(nameof(Mkb10SearchText));
                break;

            case nameof(SelectedStatus):
            case nameof(SelectedChapter):
                ApplyPipeline();
                SyncSparkPickersFromFilters();
                break;
        }
    }

    private void BuildChapterLookup()
    {
        _chapterLookup=Mkb10FilterLookups.BuildChapterLookup(AllItems);

        ChapterFilterNames.Clear();
        foreach(var item in _chapterLookup.ToObservableCollection())
            ChapterFilterNames.Add(item);

        if(!ChapterFilterNames.Contains(SelectedChapterDisplay))
        {
            SelectedChapter="All";
            OnPropertyChanged(nameof(SelectedChapterDisplay));
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy=true;
        try
        {
            var data = await _mkb10Service.GetAllAsync();

            AllItems=data.OrderBy(x => x.Code).ToList();

            BuildChapterLookup();
            InitializeSparkControls();

            if(!string.IsNullOrWhiteSpace(_pendingSearch)) SearchText=_pendingSearch;
            if(!string.IsNullOrWhiteSpace(_pendingStatus)) SelectedStatus=_pendingStatus;
            _pendingSearch=null;
            _pendingStatus=null;

            ApplyPipeline();
        }
        finally
        {
            IsBusy=false;
        }
    }

    // =========================================================
    // PIPELINE HOOKS
    // =========================================================
    protected override IEnumerable<Mkb10Code> ApplySearch(IEnumerable<Mkb10Code> query, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return query;

        search=search.Trim().ToUpperInvariant();

        return query.Where(x =>
            $"{x.Code} {x.Description} {x.Chapter}"
                .ToUpperInvariant()
                .Contains(search));
    }

    protected override IEnumerable<Mkb10Code> ApplyFilters(IEnumerable<Mkb10Code> query)
    {
        if(SelectedStatus=="Active")
            query=query.Where(x => x.IsActive);
        else if(SelectedStatus=="Inactive")
            query=query.Where(x => !x.IsActive);

        if(SelectedChapter!="All")
            query=query.Where(x => x.Chapter==SelectedChapter);

        return query;
    }

    protected override IEnumerable<Mkb10Code> ApplySort(IEnumerable<Mkb10Code> query)
        => query.OrderBy(x => x.Code);

    protected override void OnPageProjected(ObservableCollection<Mkb10Code> page)
    {
        FilteredCodes=page;
        FilteredCodesCount=$"{TotalItems:N0} резултати";

        RefreshSparkGridRows();
    }

    protected override void ResetFilters()
    {
        SelectedStatus="All";
        SelectedChapter="All";
        SearchText="";

        OnPropertyChanged(nameof(SelectedStatusDisplay));
        OnPropertyChanged(nameof(SelectedChapterDisplay));
    }

    // =========================================================
    // QUERY ATTRIBUTES
    // =========================================================
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _pendingSearch=query.TryGetValue("search", out var s) ? s?.ToString() : null;
        _pendingStatus=query.TryGetValue("statusFilter", out var f) ? f?.ToString() : null;
    }

    // ============================================================
    // TABS
    // ============================================================
    private readonly Dictionary<string, SparkTabItem> _statusTabsByInternal = new();

    private void BuildSparkTabs()
    {
        Tabs.Clear();
        _statusTabsByInternal.Clear();

        foreach(var display in StatusFilters)
        {
            var internalValue = Mkb10FilterLookups.Status.ToInternal(display);

            var tab = new SparkTabItem
            {
                Title=display,
                IsSelected=SelectedStatus==internalValue
            };

            tab.Command=new RelayCommand(() => SelectTab(tab, () => SelectedStatusDisplay=display));

            Tabs.Add(tab);
            _statusTabsByInternal[internalValue]=tab;
        }
    }

    private void RefreshSparkTabCounts()
    {
        foreach(var (internalValue, tab) in _statusTabsByInternal)
        {
            tab.Value=internalValue switch
            {
                "All" => AllItems.Count.ToString("N0"),
                "Active" => AllItems.Count(x => x.IsActive).ToString("N0"),
                "Inactive" => AllItems.Count(x => !x.IsActive).ToString("N0"),
                _ => "0"
            };
        }
    }

    partial void OnSelectedStatusChanged(string value)
    {
        foreach(var (internalValue, tab) in _statusTabsByInternal)
            tab.IsSelected=internalValue==value;
    }

    // ============================================================
    // PICKERS
    // ============================================================
    private SparkPickerItem _statusPicker, _chapterPicker;

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _statusPicker=MakePicker("Статус", StatusFilters, SelectedStatusDisplay, s => SelectedStatusDisplay=s);
        _chapterPicker=MakePicker("Поглавје", ChapterFilterNames, SelectedChapterDisplay, s => SelectedChapterDisplay=s);

        Pickers.Add(_statusPicker);
        Pickers.Add(_chapterPicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_statusPicker==null) return;
        _statusPicker.SelectedItem=SelectedStatusDisplay;
        _chapterPicker.SelectedItem=SelectedChapterDisplay;

        foreach(var (internalValue, tab) in _statusTabsByInternal)
            tab.IsSelected=internalValue==SelectedStatus;
    }

    // ============================================================
    // GRID
    // ============================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "ШИФРА", Key = "Code", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "ОПИС", Key = "Description", Width = new GridLength(3, GridUnitType.Star) },
            new() { Header = "ПОГЛАВЈЕ", Key = "Chapter", Width = new GridLength(1.5, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "IsActive", CellType = SparkGridCellType.Badge, Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "ОПЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    partial void OnFilteredCodesChanged(ObservableCollection<Mkb10Code> value) => RefreshSparkGridRows();

    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();
        foreach(var c in FilteredCodes)
        {
            var row = new SparkGridRow { Tag=c };
            row["Code"]=c.Code;
            row["Description"]=c.Description;
            row["Chapter"]=c.Chapter;
            row["IsActive"]=new SparkBadgeValue(
                c.IsActive ? "Активен" : "Неактивен",
                c.IsActive ? SparkBadgeTone.Success : SparkBadgeTone.Danger);

            AddDefaultActions(c, row);

            rows.Add(row);
        }
        GridRows=rows;
    }

    // ============================================================
    // WIRING
    // ============================================================
    private bool _sparkInitialized;

    private void InitializeSparkControls()
    {
        if(_sparkInitialized)
        {
            RefreshSparkGridRows();
            SyncSparkPickersFromFilters();
            return;
        }
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();

        _sparkInitialized=true;
    }
}