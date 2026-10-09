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
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace EHMR.ViewModels.Encounters;

public partial class EncountersListViewModel : BaseViewModel<Encounter>, IQueryAttributable
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISelectedItemService<Encounter> _selectedItemService;

    private string _pendingSearch = string.Empty;
    private string _pendingStatus = string.Empty;

    [ObservableProperty] private string selectedStatus = "All";
    //[ObservableProperty] private string selectedPriority = "All";
    //[ObservableProperty] private string selectedEncounterType = "All";
    [ObservableProperty] private string selectedDoctor = "All";
    [ObservableProperty] private bool useCyrillicSearch = true;

    protected override string ModuleName => Modules.Encounters;
    protected override string PermissionDeniedMessage => "Немате авторизација за креирање прегледи.";
    protected override Func<Encounter, Guid?>? DoctorOwnerSelector => e => e.DoctorId;
    // Base DetailRoute is not used directly here since Select/New/Edit each
    // navigate to a different route (Detail/Create/Edit) — see overrides below.
    protected override string DetailRoute => AppRoutes.Encounters.Detail;

    // =====================================================
    // CACHED UI LOOKUPS
    // =====================================================
    public ObservableCollection<string> StatusFilters
    {
        get;
    } =
        new(new[] { "Сите" }.Concat(EncounterStatusSchema.Display.Values.ToObservableCollection()));


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
                ApplyPipeline();
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





    // =====================================================
    // SPARK GRID
    // =====================================================
    [ObservableProperty] private ObservableCollection<Encounter> encounterSuggestions = new();
    [ObservableProperty] private bool showEncounterSuggestions;
    [ObservableProperty] private Encounter? selectedEncounterSuggestion;
    private bool _isSelectingEncounterSuggestion;

    [ObservableProperty] private ObservableCollection<Encounter> filteredEncounters = new();
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    partial void OnFilteredEncountersChanged(ObservableCollection<Encounter> value) => RefreshSparkGridRows();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "БРОЈ", Key = "EncounterNumber", Width = new GridLength(1.1, GridUnitType.Star) },
            new() { Header = "ИМЕ И ПРЕЗИМЕ", Key = "PatientName", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "СЗБО БРОЈ", Key = "SzboNumber", Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "РЕУМАТОЛОГ", Key = "DoctorName", Width = new GridLength(1.8, GridUnitType.Star) },


            new() { Header = "ДАТУМ", Key = "Date", Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header = "Preview", Key = "QuickPreview", CellType = SparkGridCellType.QuickPreview, Width = new GridLength(0.75, GridUnitType.Star), Sortable = false },
            new() { Header = "ОПЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
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
            row["SzboNumber"]=e.Patient?.SzboNumber??"—";
            row["DoctorName"]=e.Doctor?.User!=null ? $"{e.Doctor.User.FirstName} {e.Doctor.User.LastName}" : "";

            row["Date"]=(e.ScheduledStart??e.EncounterDate).ToString("dd.MM.yyyy");
            row["QuickPreview"]="Preview";
            // Select goes to Detail (view), Edit goes to a different route (Edit) —
            // can't use AddDefaultActions here since both actions use base commands
            // that point at the same DetailRoute; these navigate to different routes.
            var actions = new List<SparkButtonItem>
            {
                new SparkButtonItem
                {
                    IsPrimary=true,
                    IconGlyph="👁",
                    Label="Детали",
                    Command=SelectCommand,
                    CommandParameter=e
                }
            };

            if(CanUpdate)
                actions.Add(new SparkButtonItem { IconGlyph="✎", Label="Промени", Command=EditCommand, CommandParameter=e });

            row["Actions"]=actions;
            rows.Add(row);
        }
        GridRows=rows;
    }



    [RelayCommand]
    private async Task Preview(Encounter? encounter)
    {
        if(encounter is null)
            return;

        _selectedItemService.SelectedItem=encounter;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Preview);
    }

    private static SparkBadgeTone StatusToTone(EncounterStatus status) => status switch
    {
        EncounterStatus.Completed => SparkBadgeTone.Success,
        EncounterStatus.Cancelled => SparkBadgeTone.Danger,
        EncounterStatus.InProgress => SparkBadgeTone.Warning,
        _ => SparkBadgeTone.Neutral
    };

    // ============================================================
    // PICKERS
    // ============================================================
    private SparkPickerItem _statusPicker;

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _statusPicker=MakePicker("Статус", StatusFilters, SelectedStatusDisplay,
            selected => SelectedStatusDisplay=selected);

        Pickers.Add(_statusPicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_statusPicker==null) return;
        _statusPicker.SelectedItem=SelectedStatusDisplay;

    }

    // Buttons collection lives in BaseViewModel<T>; base default "✕ Исчисти" applies.

    // ============================================================
    // TABS
    // ============================================================
    private SparkTabItem _allTab, _waitingTab, _inProgressTab, _completedTab, _cancelledTab;

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
    }

    private void SelectTab(SparkTabItem tab, Action action)
    {
        foreach(var t in Tabs)
            t.IsSelected=false;

        tab.IsSelected=true;

        action();

        OnPropertyChanged(nameof(SelectedStatusDisplay));

        ApplyPipeline();

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
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
        RefreshSparkGridRows();
    }

    // =====================================================
    // STATS
    // =====================================================
    [ObservableProperty] private int totalEncounters;
    [ObservableProperty] private int waitingCount;
    [ObservableProperty] private int inProgressCount;
    [ObservableProperty] private int completedCount;
    [ObservableProperty] private int cancelledCount;

    // =====================================================
    // CTOR
    // =====================================================
    public EncountersListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IAuthorizationService authService,
        ISelectedItemService<Encounter> selectedItemService)
        : base(navigationService, userDialogService, menuService, authService, selectedItemService)
    {
        _dbFactory=dbFactory;
        _authorizationService=authService;
        _selectedItemService=selectedItemService;

        PageSize=10;
        EvaluatePermissions();
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

            CalculateTabCounts();

            InitializeSparkControls();

            ApplyPendingQuery();
            ApplyPipeline();
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

    protected override void OnPageProjected(ObservableCollection<Encounter> page)
    {
        FilteredEncounters=page;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            RefreshSparkGridRows();
        });
    }

    private void ApplyPendingQuery()
    {
        if(!string.IsNullOrWhiteSpace(_pendingSearch))
            SearchText=_pendingSearch;

        if(!string.IsNullOrWhiteSpace(_pendingStatus))
        {
            SelectedStatus=_pendingStatus;
            OnPropertyChanged(nameof(SelectedStatusDisplay));

            if(Tabs!=null&&Tabs.Count>0)
            {
                foreach(var t in Tabs) t.IsSelected=false;

                SparkTabItem? targetTab = null;

                if(string.Equals(_pendingStatus, "All", StringComparison.OrdinalIgnoreCase))
                    targetTab=_allTab;
                else if(string.Equals(_pendingStatus, "Scheduled", StringComparison.OrdinalIgnoreCase))
                    targetTab=_waitingTab;
                else if(string.Equals(_pendingStatus, "InProgress", StringComparison.OrdinalIgnoreCase))
                    targetTab=_inProgressTab;
                else if(string.Equals(_pendingStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                    targetTab=_completedTab;
                else if(string.Equals(_pendingStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    targetTab=_cancelledTab;

                if(targetTab!=null)
                    targetTab.IsSelected=true;
            }
        }

        _pendingSearch=string.Empty;
        _pendingStatus=string.Empty;
    }

    // =====================================================
    // PIPELINE HOOKS
    // =====================================================
    partial void OnUseCyrillicSearchChanged(bool value)
    {
        ApplyPipeline();
        OnSearchTextChanged(SearchText);
    }

    protected override void OnSearchTextChanged(string value)
    {
        if(_isSelectingEncounterSuggestion) return;

        var term = value?.Trim()??string.Empty;
        if(string.IsNullOrWhiteSpace(term))
        {
            EncounterSuggestions.Clear();
            ShowEncounterSuggestions=false;
            return;
        }

        EncounterSuggestions=new ObservableCollection<Encounter>(
            ApplySearch(AllItems, term)
                .OrderByDescending(x => x.ScheduledStart??x.EncounterDate)
                .Take(8));
        ShowEncounterSuggestions=EncounterSuggestions.Count>0;
    }

    partial void OnSelectedEncounterSuggestionChanged(Encounter? value)
    {
        if(value is null) return;

        _isSelectingEncounterSuggestion=true;
        SearchText=value.EncounterNumber;
        _isSelectingEncounterSuggestion=false;
        ShowEncounterSuggestions=false;
        EncounterSuggestions.Clear();
    }

    protected override IEnumerable<Encounter> ApplySearch(IEnumerable<Encounter> query, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return query;

        var term=search.Trim();
        var tokens=term.Split(' ', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        var firstToken=tokens[0];
        var lastToken=tokens.Length>1 ? tokens[1] : null;
        var firstAlternates=GetSearchVariants(firstToken);
        var lastAlternates=lastToken is null ? Array.Empty<string>() : GetSearchVariants(lastToken);

        return query.Where(x =>
            (x.EncounterNumber??"").Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (x.Patient!=null &&
                StartsWithAny(x.Patient.FirstName, firstAlternates) &&
                (lastToken is null || StartsWithAny(x.Patient.LastName, lastAlternates))) ||
            (x.Patient!=null && x.Patient.SzboNumber.Contains(term, StringComparison.OrdinalIgnoreCase)));

        static string[] GetSearchVariants(string value)
        {
            var variants=new[]
            {
                value,
                EHMR.Helpers.MacedonianTransliterator.ToCyrillic(value),
                EHMR.Helpers.MacedonianTransliterator.ToLatin(value)
            };

            return variants
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        static bool StartsWithAny(string? value, IEnumerable<string> variants)
            => !string.IsNullOrWhiteSpace(value)
                && variants.Any(v => value.StartsWith(v, StringComparison.OrdinalIgnoreCase)
        //(x.Doctor!=null&&((x.Doctor.User.FirstName+" "+x.Doctor.User.LastName).Contains(term, StringComparison.OrdinalIgnoreCase)||
        //                    (x.Doctor.User.FirstName+" "+x.Doctor.User.LastName).Contains(cyrillicTerm, StringComparison.OrdinalIgnoreCase)))||
        //(x.ChiefComplaint??"").Contains(term, StringComparison.OrdinalIgnoreCase)||
        //(x.ChiefComplaint??"").Contains(cyrillicTerm, StringComparison.OrdinalIgnoreCase)||
        //(x.ClinicalNotes??"").Contains(term, StringComparison.OrdinalIgnoreCase)||
        //(x.ClinicalNotes??"").Contains(cyrillicTerm, StringComparison.OrdinalIgnoreCase)
        );
    }

    protected override IEnumerable<Encounter> ApplyFilters(IEnumerable<Encounter> query)
    {
        if(SelectedStatus!="All")
            query=query.Where(x => string.Equals(x.Status.ToString(), SelectedStatus, StringComparison.OrdinalIgnoreCase));



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
    // NAVIGATION — override base because routes differ (Create/Detail/Edit)
    // =====================================================
    protected override async Task New()
    {
        if(!CanCreate)
        {
            await UserDialogService.ShowAlertAsync(PermissionDeniedTitle, PermissionDeniedMessage, "OK");
            return;
        }

        SelectedItemService.SelectedItem=default;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Create);
    }

    protected override async Task Select(Encounter item)
    {
        if(item is null) return;

        SelectedItemService.SelectedItem=item;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Detail);
    }

    protected override async Task Edit(Encounter item)
    {
        if(item is null) return;

        if(!CanUpdate)
        {
            await UserDialogService.ShowAlertAsync(PermissionDeniedTitle, "Немате авторизација за уредување прегледи.", "OK");
            return;
        }

        SelectedItemService.SelectedItem=item;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Edit);
    }
    private void CalculateTabCounts()
    {
        if(AllItems==null)
            return;

        TotalEncounters=AllItems.Count;

        WaitingCount=
            AllItems.Count(x =>
                x.Status==EncounterStatus.Scheduled);

        InProgressCount=
            AllItems.Count(x =>
                x.Status==EncounterStatus.InProgress);

        CompletedCount=
            AllItems.Count(x =>
                x.Status==EncounterStatus.Completed);

        CancelledCount=
            AllItems.Count(x =>
                x.Status==EncounterStatus.Cancelled);
    }
    protected override void ResetFilters()
    {
        SearchText=string.Empty;

        SelectedStatus="All";

        SelectedDoctor="All";
        _filterByDate=false;
        _filterDate=DateTime.Today;

        OnPropertyChanged(nameof(FilterByDate));
        OnPropertyChanged(nameof(FilterDate));
        OnPropertyChanged(nameof(SelectedStatusDisplay));

    }
}