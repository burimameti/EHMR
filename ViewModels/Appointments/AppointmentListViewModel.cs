using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Domain.Search;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using EHMR.Services;
using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EHMR.ViewModels.Appointments;

public partial class AppointmentListViewModel
    : BaseViewModel<Appointment>, IQueryAttributable
{
    private string? _pendingStatus;
    private bool _pendingToday;
    [ObservableProperty] private bool useCyrillicSearch = true;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _pendingStatus=query.TryGetValue("statusFilter", out var f) ? f?.ToString() : null;
        _pendingToday=query.TryGetValue("dateFilter", out var d)&&d?.ToString()=="today";
    }

    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAppointmentSearchQueryHandler _autocomplete;

    private int _selectedIndex = -1;

    // Drill-down state. Set when the user picks a suggestion from the dropdown,
    // cleared when they type a fresh query or hit "Clear filters". We match by
    // id rather than by the (decorated) display text on purpose - that way a
    // doctor's "д-р " label or two same-named patients never break the match.
    private SearchEntityType? _activePersonType;
    private string? _activePersonId;

    // Flag to prevent side effects when we programmatically set SearchText
    private bool _isSelectingSuggestion;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetProperty(ref _selectedIndex, value);
    }

    [ObservableProperty] private Appointment? selectedAppointment;
    [ObservableProperty] private IReadOnlyList<SearchSuggestionDto> suggestions = [];
    [ObservableProperty] private SearchSuggestionDto? selectedSuggestion;
    [ObservableProperty] private bool showSuggestions;

    // Drives the "showing full history for X" banner above the grid.
    [ObservableProperty] private bool isViewingPersonHistory;
    [ObservableProperty] private string activeHistoryLabel = "";

    protected override Func<Appointment, Guid?>? DoctorOwnerSelector => e => e.DoctorId;

    public static IEnumerable<TextSpan> BuildHighlighted(string text, string query)
    {
        if(string.IsNullOrWhiteSpace(query)||string.IsNullOrWhiteSpace(text))
        {
            yield return new TextSpan { Text=text };
            yield break;
        }

        var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if(index<0)
        {
            yield return new TextSpan { Text=text };
            yield break;
        }

        yield return new TextSpan { Text=text[..index] };
        yield return new TextSpan { Text=text.Substring(index, query.Length), IsHighlighted=true };
        yield return new TextSpan { Text=text[(index+query.Length)..] };
    }

    public IReadOnlyList<AppointmentStatusOption> StatusFilters
    {
        get;
    } =
    [
        new() { Filter = AppointmentStatusFilter.All,       Label = "Сите" },
        new() { Filter = AppointmentStatusFilter.Scheduled, Label = "Закажан" },
        new() { Filter = AppointmentStatusFilter.InProgress, Label = "Во тек" },
        new() { Filter = AppointmentStatusFilter.Completed, Label = "Завршен" },
        new() { Filter = AppointmentStatusFilter.Cancelled, Label = "Откажан" },
    ];

    // =========================================================================
    // CONSTRUCTOR
    // =========================================================================
    public AppointmentListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<Appointment> selectedItemService,
        IAuthorizationService authService,
        IAppointmentSearchQueryHandler autocomplete)
        : base(navigationService, userDialogService, menuService, authService, selectedItemService)
    {
        _dbFactory=dbFactory;
        _autocomplete=autocomplete;
        _selectedStatus=StatusFilters.First(x => x.Filter==AppointmentStatusFilter.All);

        PageSize=10;
        SearchCommand=CommitSearchCommand;

        EvaluatePermissions();
        InitializeSparkControls();
    }

    // =========================================================================
    // SEARCH & SUGGESTIONS - THE FIX
    // =========================================================================

    /// <summary>
    /// When Cyrillic toggle is changed, re-search with current text using new setting.
    /// </summary>
    partial void OnUseCyrillicSearchChanged(bool value)
    {
        // Re-trigger search with current text but using new Cyrillic mode
        OnSearchTextChanged(SearchText);
    }

    /// <summary>
    /// Override of base OnSearchTextChanged - called when SearchText property changes.
    /// Handles showing/hiding suggestions WITHOUT calling ApplyPipeline.
    /// </summary>
    protected override void OnSearchTextChanged(string value)
    {
        // Skip if we're programmatically setting SearchText during suggestion selection
        if(_isSelectingSuggestion) return;

        // Clear person history when user types fresh search
        if(_activePersonId is not null)
        {
            _activePersonId=null;
            _activePersonType=null;
            IsViewingPersonHistory=false;
            ActiveHistoryLabel="";
        }

        var term = value?.Trim()??string.Empty;
        if(string.IsNullOrWhiteSpace(term))
        {
            Suggestions= [];
            ShowSuggestions=false;
            return;
        }

        // Trigger debounced search with Cyrillic support
        _=DebouncedSearchAsync(term);
    }

    /// <summary>
    /// Called when user selects a suggestion from dropdown (via partial method).
    /// </summary>
    partial void OnSelectedSuggestionChanged(SearchSuggestionDto? value)
    {
        if(value!=null)
            _=SelectSuggestionAsync(value);
    }

    /// <summary>
    /// Fetches autocomplete suggestions, with Cyrillic search support.
    /// </summary>
 
      private async Task DebouncedSearchAsync(string text)
    {
        var cyrillicTerm = UseCyrillicSearch
            ? Helpers.MacedonianTransliterator.ToCyrillic(text)
            : text;

        var result = await DebouncedSuggestionSearchAsync(text, async (q, token) =>
        {
            // Search with both the original term AND the Cyrillic transliteration
            var request = new AppointmentSearchQuery(q,
                new[] { SearchEntityType.Patient},
                8);

            var primary = await _autocomplete.Handle(request, token);

            // If Cyrillic differs from original, also search with transliterated term
            if(UseCyrillicSearch&&cyrillicTerm!=q)
            {
                var cyrillicRequest = new AppointmentSearchQuery(cyrillicTerm,
                    new[] { SearchEntityType.Patient, SearchEntityType.Doctor },
                    8);

                var cyrillicResults = await _autocomplete.Handle(cyrillicRequest, token);

                // Merge and deduplicate by Id
                primary=primary
                    .Concat(cyrillicResults)
                    .GroupBy(s => s.Id)
                    .Select(g => g.First())
                    .Take(8)
                    .ToList();
            }

            return primary;
        });

        if(result is null) return;

        Suggestions=result;
        SelectedIndex=-1;
        SelectedSuggestion=null;
        ShowSuggestions=result.Count>0;
    }
    

    /// <summary>
    /// User selected a suggestion from dropdown.
    /// Sets up drill-down mode and filters grid accordingly.
    /// </summary>
    [RelayCommand]
    private async Task SelectSuggestionAsync(SearchSuggestionDto suggestion)
    {
        if(suggestion is null)
            return;

        SelectedSuggestion=suggestion;
        _activePersonType=suggestion.Type;
        _activePersonId=suggestion.Id;
        IsViewingPersonHistory=true;
        ActiveHistoryLabel=suggestion.DisplayText;

        // Set SearchText without triggering OnSearchTextChanged side effects
        _isSelectingSuggestion=true;
        SearchText=suggestion.DisplayText;
        _isSelectingSuggestion=false;

        // Clear suggestions BEFORE applying pipeline
        Suggestions= [];
        SelectedIndex=-1;
        ShowSuggestions=false;

        // Reset filters to "All" for the drill-down view
        FilterByDate=false;
        SelectedStatus=StatusFilters.First(x => x.Filter==AppointmentStatusFilter.All);

        // NOW apply pipeline (safe - suggestions are cleared)
        ApplyPipeline();
    }

    /// <summary>
    /// User pressed Enter or clicked "Search" button.
    /// </summary>
    [RelayCommand]
    private void CommitSearch()
    {
        if(SelectedSuggestion!=null)
        {
            _=SelectSuggestionAsync(SelectedSuggestion);
            return;
        }

        // Clear suggestions dropdown
        ShowSuggestions=false;
        Suggestions= [];

        // Apply filters and refresh grid
        ApplyPipeline();
    }

    /// <summary>
    /// Keyboard navigation: move down in suggestions.
    /// </summary>
    [RelayCommand]
    private void MoveNext()
    {
        if(Suggestions.Count==0) return;
        SelectedIndex=Math.Min(SelectedIndex+1, Suggestions.Count-1);
        SelectedSuggestion=Suggestions[SelectedIndex];
    }

    /// <summary>
    /// Keyboard navigation: move up in suggestions.
    /// </summary>
    [RelayCommand]
    private void MovePrevious()
    {
        if(Suggestions.Count==0) return;
        SelectedIndex=Math.Max(SelectedIndex-1, 0);
        SelectedSuggestion=Suggestions[SelectedIndex];
    }

    /// <summary>
    /// Keyboard: user pressed Enter on selected suggestion.
    /// </summary>
    [RelayCommand]
    private async Task CommitSelectionAsync()
    {
        if(SelectedIndex<0||SelectedIndex>=Suggestions.Count) return;
        await SelectSuggestionAsync(Suggestions[SelectedIndex]);
    }

    // =========================================================================
    // LOAD & PENDING QUERIES
    // =========================================================================

    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;
        try
        {
            IsBusy=true;
            ClearError();

            await using var db = await _dbFactory.CreateDbContextAsync();
            var items = await db.Appointments
                .Include(x => x.Patient)
                .Include(x => x.Doctor).ThenInclude(x => x.User)
                .AsNoTracking()
                .OrderBy(x => x.ScheduledStart)
                .ToListAsync();

            AllItems=items;

            ApplyPendingQuery();
            ApplyPipeline();
        }
        catch(Exception ex)
        {
            OnError($"Грешка при вчитување прегледи: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    private void ApplyPendingQuery()
    {
        if(!string.IsNullOrWhiteSpace(_pendingStatus)&&
            Enum.TryParse<AppointmentStatusFilter>(_pendingStatus, true, out var parsed))
        {
            SelectedStatus=StatusFilters.First(x => x.Filter==parsed);
        }

        if(_pendingToday)
        {
            FilterDate=DateTime.Today;
            FilterByDate=true;
        }

        _pendingStatus=null;
        _pendingToday=false;
    }

    // =========================================================================
    // STATUS & DATE FILTERS
    // =========================================================================

    private AppointmentStatusOption _selectedStatus;

    public AppointmentStatusOption SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if(!SetProperty(ref _selectedStatus, value)) return;
            SyncSparkPickersFromFilters();
            ApplyPipeline();
        }
    }

    private DateTime _filterDate = DateTime.Today;

    public DateTime FilterDate
    {
        get => _filterDate;
        set
        {
            if(!SetProperty(ref _filterDate, value)) return;
            ApplyPipeline();
        }
    }

    private bool _filterByDate;

    public bool FilterByDate
    {
        get => _filterByDate;
        set
        {
            if(!SetProperty(ref _filterByDate, value)) return;
            ApplyPipeline();
        }
    }

    [RelayCommand]
    private void ToggleToday() => FilterByDate=!FilterByDate;

    // =========================================================================
    // EDIT / CANCEL / DELETE OPERATIONS
    // =========================================================================

    protected override async Task Edit(Appointment item)
    {
        if(item is null) return;

        if(!CanUpdate)
        {
            await UserDialogService.ShowAlertAsync(PermissionDeniedTitle, "Немате авторизација за уредување термини.", "OK");
            return;
        }

        if(!CanEdit(item))
        {
            await UserDialogService.ShowAlertAsync("Не е можно", "Овој термин не може да се уредува (веќе поминат или не е закажан).", "OK");
            return;
        }

        SelectedItemService.SelectedItem=item;
        SelectedItemService.OpenInEditMode=true;
        await NavigationService.GoToAsync(DetailRoute);
    }

    [RelayCommand]
    private async Task CancelAppointmentAsync(Appointment? appointment)
    {
        if(appointment is null) return;

        if(!CanDelete)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за откажување термини.", "OK");
            return;
        }

        if(!CanCancel(appointment))
        {
            await UserDialogService.ShowAlertAsync("Не е можно", "Овој термин не може да се откаже (веќе поминат или не е закажан).", "OK");
            return;
        }

        var confirmed = await UserDialogService.ShowConfirmationAsync(
            "Откажување термин",
            $"Дали сте сигурни дека сакате да го откажете терминот за {appointment.Patient?.FullName}?",
            "Да", "Не");

        if(!confirmed) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.Appointments.FindAsync(appointment.Id);
            if(entity!=null)
            {
                entity.Status=AppointmentStatus.Cancelled;
                await db.SaveChangesAsync();
            }

            // Full reload triggers RefreshSparkTabCounts() + ApplyPipeline()
            await LoadAsync();
        }
        catch(Exception ex)
        {
            OnError($"Грешка при откажување термин: {ex.Message}");
        }
    }

    private static bool CanEdit(Appointment a) =>
        a.Status is (AppointmentStatus.Scheduled or AppointmentStatus.InProgress)&&
        a.ScheduledStart.Date>=DateTime.Today;

    private static bool CanCancel(Appointment a) =>
        a.Status==AppointmentStatus.Scheduled&&a.ScheduledStart.Date>=DateTime.Today;

    // =========================================================================
    // PIPELINE HOOKS - SEARCH, FILTER, SORT
    // =========================================================================

    protected override IEnumerable<Appointment> ApplySearch(IEnumerable<Appointment> items, string search)
    {
        // If drill-down mode, don't filter by text
        if(_activePersonId is not null||string.IsNullOrWhiteSpace(search))
            return items;

        var term = search.Trim();
        var cyrillicTerm = UseCyrillicSearch ? Helpers.MacedonianTransliterator.ToCyrillic(term) : term;

        return items.Where(x =>
            // Patient search (latin + cyrillic)
            (x.Patient!=null&&(
                (x.Patient.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
                (x.Patient.FullName?.Contains(cyrillicTerm, StringComparison.OrdinalIgnoreCase)??false)
            ))||
            // Doctor search (latin + cyrillic)
            (x.Doctor!=null&&(
                (x.Doctor.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
                (x.Doctor.FullName?.Contains(cyrillicTerm, StringComparison.OrdinalIgnoreCase)??false)
            ))||
            // Clinical notes search (latin + cyrillic)
            (x.ClinicalNotes?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.ClinicalNotes?.Contains(cyrillicTerm, StringComparison.OrdinalIgnoreCase)??false));
    }

    protected override IEnumerable<Appointment> ApplyFilters(IEnumerable<Appointment> items)
    {
        // Drill-down by person (patient or doctor)
        if(_activePersonId is not null)
        {
            var history = _activePersonType==SearchEntityType.Doctor
                ? items.Where(x => x.Doctor!=null&&x.Doctor.Id.ToString()==_activePersonId)
                : items.Where(x => x.Patient!=null&&x.Patient.Id.ToString()==_activePersonId);

            return ApplyStatusFilter(history);
        }

        var query = ApplyStatusFilter(items);

        // Date filter
        if(FilterByDate)
            query=query.Where(x => x.ScheduledStart.Date==FilterDate.Date);

        return query;
    }

    private IEnumerable<Appointment> ApplyStatusFilter(IEnumerable<Appointment> items) =>
        SelectedStatus.Filter switch
        {
            AppointmentStatusFilter.All => items,
            _ => items.Where(x => x.Status==Enum.Parse<AppointmentStatus>(SelectedStatus.Filter.ToString()))
        };

    protected override IEnumerable<Appointment> ApplySort(IEnumerable<Appointment> query) =>
        _activePersonId is not null
            ? query.OrderByDescending(x => x.ScheduledStart)
            : query.OrderBy(x => x.ScheduledStart);

    protected override void ResetFilters()
    {
        _activePersonId=null;
        _activePersonType=null;
        IsViewingPersonHistory=false;
        ActiveHistoryLabel="";

        _isSelectingSuggestion=true;
        SearchText=string.Empty;
        _isSelectingSuggestion=false;

        SelectedSuggestion=null;
        Suggestions= [];
        ShowSuggestions=false;

        FilterDate=DateTime.Today;
        FilterByDate=false;
        SelectedStatus=StatusFilters.First(x => x.Filter==AppointmentStatusFilter.All);
    }

    // =========================================================================
    // TABS - STATUS FILTERING
    // =========================================================================

    private readonly Dictionary<AppointmentStatusFilter, SparkTabItem> _statusTabsByFilter = new();

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
    }

    private IEnumerable<Appointment> GetItemsForTabCounts()
    {
        IEnumerable<Appointment> items = ApplySearch(AllItems, SearchText);

        if(_activePersonId is not null)
        {
            items=_activePersonType==SearchEntityType.Doctor
                ? items.Where(x => x.Doctor!=null&&x.Doctor.Id.ToString()==_activePersonId)
                : items.Where(x => x.Patient!=null&&x.Patient.Id.ToString()==_activePersonId);
        }
        else if(FilterByDate)
        {
            items=items.Where(x => x.ScheduledStart.Date==FilterDate.Date);
        }

        return items;
    }

    private void RefreshSparkTabCounts()
    {
        if(_statusTabsByFilter.Count==0) return;

        var baseItems = GetItemsForTabCounts().ToList();

        foreach(var (filter, tab) in _statusTabsByFilter)
        {
            var count = filter switch
            {
                AppointmentStatusFilter.All => baseItems.Count,
                _ => baseItems.Count(x => x.Status==Enum.Parse<AppointmentStatus>(filter.ToString()))
            };

            tab.Value=count.ToString("N0");
        }
    }

    private void SyncSparkTabsFromFilters()
    {
        foreach(var (filter, tab) in _statusTabsByFilter)
            tab.IsSelected=filter==SelectedStatus.Filter;
    }

    // =========================================================================
    // SPARK CONTROLS - PICKERS, BUTTONS, GRID
    // =========================================================================

    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private SparkPickerItem _statusPicker;

    private void InitializeSparkControls()
    {
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
        AddClearFiltersButton();
    }

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "БРОЈ", Key = "AppointmentNumber", Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header = "ИМЕ И ПРЕЗИМЕ", Key = "Patient", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "РЕУМАТОЛОГ", Key = "Doctor", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "ДАТУМ", Key = "Date", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "ОПЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    protected override void OnPageProjected(ObservableCollection<Appointment> page)
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var a in page)
        {
            var row = new SparkGridRow { Tag=a };
            row["AppointmentNumber"]=a.AppointmentNumber;
            row["Patient"]=a.Patient?.FullName??"";
            row["Doctor"]=a.Doctor?.FullName??"";
            row["Date"]=a.ScheduledStart.ToString("dd.MM.yyyy");
            row["Status"]=new SparkBadgeValue(StatusLabel(a.Status), StatusToTone(a.Status));

            AddDefaultActions(a, row, detailLabel: "Детали", editLabel: "Промени", canEditPredicate: CanEdit);

            if(CanDelete&&CanCancel(a))
            {
                ((List<SparkButtonItem>)row["Actions"]).Add(new SparkButtonItem
                {
                    Label="Откажи термин",
                    Command=CancelAppointmentCommand,
                    CommandParameter=a
                });
            }

            rows.Add(row);
        }

        GridRows=rows;
    }

    private static string StatusLabel(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => "Закажан",
        AppointmentStatus.Completed => "Завршен",
        AppointmentStatus.Cancelled => "Откажан",
        AppointmentStatus.InProgress => "Во тек",
        _ => status.ToString()
    };

    private static SparkBadgeTone StatusToTone(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Completed => SparkBadgeTone.Success,
        AppointmentStatus.Cancelled => SparkBadgeTone.Danger,
        _ => SparkBadgeTone.Neutral
    };

    protected override string ModuleName => Modules.Appointments;
    protected override string DetailRoute => AppRoutes.Appointments.Detail;
    protected override string PermissionDeniedMessage => "Немате авторизација за додавање на нови термини.";

    public ICommand SearchCommand
    {
        get;
    }
}

// =========================================================================
// SUPPORTING TYPES
// =========================================================================

public enum AppointmentStatusFilter
{
    All,
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
}

public class TextSpan
{
    public string Text { get; set; } = "";
    public bool IsHighlighted
    {
        get; set;
    }
}

public sealed class AppointmentStatusOption
{
    public AppointmentStatusFilter Filter
    {
        get; init;
    }
    public string Label { get; init; } = "";
    public override string ToString() => Label;
}