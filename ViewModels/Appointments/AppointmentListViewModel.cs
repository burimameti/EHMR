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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace EHMR.ViewModels.Appointments;

public partial class AppointmentListViewModel
    : BaseViewModel<Appointment>, IQueryAttributable
{
    private string? _pendingStatus;
    private bool _pendingToday;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _pendingStatus=query.TryGetValue("statusFilter", out var f) ? f?.ToString() : null;
        _pendingToday=query.TryGetValue("dateFilter", out var d)&&d?.ToString()=="today";
    }

    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAppointmentSearchQueryHandler _autocomplete;

    private int _selectedIndex = -1;
    private readonly Dictionary<string, IReadOnlyList<SearchSuggestionDto>> _cache = new();
    private CancellationTokenSource? _searchCts;

    // Drill-down state. Set when the user picks a suggestion from the dropdown,
    // cleared when they type a fresh query or hit "Clear filters". We match by
    // id rather than by the (decorated) display text on purpose - that way a
    // doctor's "д-р " label or two same-named patients never break the match.
    private SearchEntityType? _activePersonType;
    private string? _activePersonId;
    private bool _suppressSearchTextSideEffects;

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
        new() { Filter = AppointmentStatusFilter.Active,    Label = "Активни" },
        new() { Filter = AppointmentStatusFilter.Scheduled, Label = "Закажан" },
        new() { Filter = AppointmentStatusFilter.CheckedIn, Label = "Пријавен" },
        new() { Filter = AppointmentStatusFilter.Completed, Label = "Завршен" },
        new() { Filter = AppointmentStatusFilter.Cancelled, Label = "Откажан" },
        new() { Filter = AppointmentStatusFilter.Missed,    Label = "Пропуштен" }
    ];

    private async Task DebouncedSearchAsync(string text)
    {
        _searchCts?.Cancel();
        _searchCts=new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(250, token);
            text=text.Trim();

            if(string.IsNullOrWhiteSpace(text))
            {
                Suggestions= [];
                SelectedIndex=-1;
                SelectedSuggestion=null;
                ShowSuggestions=false;
                return;
            }

            if(_cache.TryGetValue(text, out var cached))
            {
                Suggestions=cached;
                SelectedIndex=-1;
                SelectedSuggestion=null;
                ShowSuggestions=cached.Count>0;
                return;
            }

            var request = new AppointmentSearchQuery(text,
                new[] { SearchEntityType.Patient, SearchEntityType.Doctor },
                8);

            var result = await _autocomplete.Handle(request, token);
            if(token.IsCancellationRequested) return;

            if(_cache.Count>50) _cache.Remove(_cache.Keys.First());
            _cache[text]=result;

            Suggestions=result;
            SelectedIndex=-1;
            SelectedSuggestion=null;
            ShowSuggestions=result.Count>0;
        }
        catch(OperationCanceledException)
        {
            // a newer keystroke superseded this search - nothing to do
        }
    }

    private AppointmentStatusOption _selectedStatus;

    public AppointmentStatusOption SelectedStatus
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

    private DateTime _filterDate = DateTime.Today;

    public DateTime FilterDate
    {
        get => _filterDate;
        set
        {
            if(SetProperty(ref _filterDate, value)) ApplyPipeline();
        }
    }

    public enum DateFilterMode
    {
        All, Today, Tomorrow, ThisWeek, ThisMonth, Custom
    }

    private bool _filterByDate;

    public bool FilterByDate
    {
        get => _filterByDate;
        set
        {
            SetProperty(ref _filterByDate, value);
            BuildSparkButtons();
            ApplyPipeline();
        }
    }

    protected override string ModuleName => Modules.Appointments;
    protected override string DetailRoute => AppRoutes.Appointments.Detail;
    protected override string PermissionDeniedMessage => "Немате авторизација за додавање на нови термини.";

    public ICommand SearchCommand
    {
        get;
    }

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
        _selectedStatus=StatusFilters.First(x => x.Filter==AppointmentStatusFilter.Active);

        PageSize=10;

        SearchCommand=CommitSearchCommand;

        PropertyChanged+=OnViewModelPropertyChanged;

        EvaluatePermissions();
        InitializeSparkControls();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName!=nameof(SearchText)) return;

        if(_suppressSearchTextSideEffects) return;

        if(_activePersonId is not null)
        {
            _activePersonId=null;
            _activePersonType=null;
            IsViewingPersonHistory=false;
            ActiveHistoryLabel="";
        }

        _=DebouncedSearchAsync(SearchText);
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
            var items = await db.Appointments
                .Include(x => x.Patient)
                .Include(x => x.Doctor).ThenInclude(x => x.User)
                .AsNoTracking()
                .OrderBy(x => x.ScheduledStart)
                .ToListAsync();
            AllItems=items;

            RefreshSparkTabCounts();

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

    [RelayCommand]
    private void CommitSearch()
    {
        if(SelectedSuggestion!=null)
        {
            _=SelectSuggestionAsync(SelectedSuggestion);
            return;
        }

        ShowSuggestions=false;
        ApplyPipeline();
    }

    [RelayCommand]
    private Task SelectSuggestionAsync(SearchSuggestionDto suggestion)
    {
        if(suggestion is null)
            return Task.CompletedTask;

        SelectedSuggestion=suggestion;
        _activePersonType=suggestion.Type;
        _activePersonId=suggestion.Id;
        IsViewingPersonHistory=true;
        ActiveHistoryLabel=suggestion.DisplayText;

        _suppressSearchTextSideEffects=true;
        SearchText=suggestion.DisplayText;
        _suppressSearchTextSideEffects=false;

        Suggestions= [];
        SelectedIndex=-1;
        ShowSuggestions=false;

        FilterByDate=false;
        SelectedStatus=StatusFilters.First(x => x.Filter==AppointmentStatusFilter.All);

        ApplyPipeline();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void MoveNext()
    {
        if(Suggestions.Count==0) return;
        SelectedIndex=Math.Min(SelectedIndex+1, Suggestions.Count-1);
        SelectedSuggestion=Suggestions[SelectedIndex];
    }

    [RelayCommand]
    private void MovePrevious()
    {
        if(Suggestions.Count==0) return;
        SelectedIndex=Math.Max(SelectedIndex-1, 0);
        SelectedSuggestion=Suggestions[SelectedIndex];
    }

    [RelayCommand]
    private async Task CommitSelectionAsync()
    {
        if(SelectedIndex<0||SelectedIndex>=Suggestions.Count) return;
        await SelectSuggestionAsync(Suggestions[SelectedIndex]);
    }

    // ================= SELECT / NEW / EDIT (override base for extra business rules) =================
    // Base already does the permission check + navigation; here we ONLY add
    // the appointment-specific "CanEdit" business rule (status + date) on top.
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

            await LoadAsync();
        }
        catch(Exception ex)
        {
            OnError($"Грешка при откажување термин: {ex.Message}");
        }
    }

    private static bool CanEdit(Appointment a) =>
        a.Status is AppointmentStatus.Scheduled or AppointmentStatus.CheckedIn&&a.ScheduledStart.Date>=DateTime.Today;

    private static bool CanCancel(Appointment a) =>
        a.Status==AppointmentStatus.Scheduled&&a.ScheduledStart.Date>=DateTime.Today;

    private static bool IsPast(Appointment a) => a.ScheduledStart<DateTime.Now;

    [RelayCommand]
    private void ToggleToday() => FilterByDate=!FilterByDate;

    protected override void ResetFilters()
    {
        _activePersonId=null;
        _activePersonType=null;
        IsViewingPersonHistory=false;
        ActiveHistoryLabel="";

        _suppressSearchTextSideEffects=true;
        SearchText=string.Empty;
        _suppressSearchTextSideEffects=false;

        SelectedSuggestion=null;
        Suggestions= [];
        ShowSuggestions=false;

        FilterDate=DateTime.Today;
        FilterByDate=true;
        SelectedStatus=StatusFilters.First(x => x.Filter==AppointmentStatusFilter.All);
    }

    // ================= PIPELINE HOOKS =================
    protected override IEnumerable<Appointment> ApplySearch(IEnumerable<Appointment> items, string search)
    {
        if(_activePersonId is not null||string.IsNullOrWhiteSpace(search))
            return items;

        var term = search.Trim();
        return items.Where(x =>
            (x.Patient?.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.Doctor?.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.ReasonForVisit?.Contains(term, StringComparison.OrdinalIgnoreCase)??false));
    }

    protected override IEnumerable<Appointment> ApplyFilters(IEnumerable<Appointment> items)
    {
        if(_activePersonId is not null)
        {
            var history = _activePersonType==SearchEntityType.Doctor
                ? items.Where(x => x.Doctor!=null&&x.Doctor.Id.ToString()==_activePersonId)
                : items.Where(x => x.Patient!=null&&x.Patient.Id.ToString()==_activePersonId);

            if(SelectedStatus.Filter==AppointmentStatusFilter.Active)
                history=history.Where(x => x.Status==AppointmentStatus.Scheduled||x.Status==AppointmentStatus.CheckedIn);
            else if(SelectedStatus.Filter!=AppointmentStatusFilter.All)
            {
                var status = Enum.Parse<AppointmentStatus>(SelectedStatus.Filter.ToString());
                history=history.Where(x => x.Status==status);
            }
            return history;
        }

        var query = items;

        if(SelectedStatus.Filter==AppointmentStatusFilter.Active)
            query=query.Where(x => x.Status==AppointmentStatus.Scheduled||x.Status==AppointmentStatus.CheckedIn);
        else if(SelectedStatus.Filter!=AppointmentStatusFilter.All)
        {
            var status = Enum.Parse<AppointmentStatus>(SelectedStatus.Filter.ToString());
            query=query.Where(x => x.Status==status);
        }

        if(FilterByDate)
            query=query.Where(x => x.ScheduledStart.Date==FilterDate.Date);

        return query;
    }

    protected override IEnumerable<Appointment> ApplySort(IEnumerable<Appointment> query) =>
        _activePersonId is not null
            ? query.OrderByDescending(x => x.ScheduledStart)
            : query.OrderBy(x => x.ScheduledStart);

    // ============================================================
    // TABS
    // ============================================================
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

        RefreshSparkTabCounts();
    }

    private void RefreshSparkTabCounts()
    {
        foreach(var (filter, tab) in _statusTabsByFilter)
        {
            var count = filter switch
            {
                AppointmentStatusFilter.All => AllItems.Count,
                AppointmentStatusFilter.Active => AllItems.Count(x =>
                    x.Status==AppointmentStatus.Scheduled||x.Status==AppointmentStatus.CheckedIn),
                _ => AllItems.Count(x => x.Status==Enum.Parse<AppointmentStatus>(filter.ToString()))
            };

            tab.Value=count.ToString("N0");
        }
    }

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
            Label="Денес",
            IsPrimary=FilterByDate,
            Command=ToggleTodayCommand
        });
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
            new() { Header = "ДОКТОР", Key = "Doctor", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "ДАТУМ", Key = "Date", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "ВРЕМЕ", Key = "Time", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "АКЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    protected override void OnPageProjected(ObservableCollection<Appointment> page)
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var a in page)
        {
            var row = new SparkGridRow { Tag=a };
            row["Patient"]=a.Patient?.FullName??"";
            row["Doctor"]=a.Doctor?.FullName??"";
            row["Date"]=a.ScheduledStart.ToString("dd.MM.yyyy");
            row["Time"]=a.ScheduledStart.ToString("HH:mm");
            row["Status"]=new SparkBadgeValue(StatusLabel(a.Status), StatusToTone(a.Status));

            var actions = new List<SparkButtonItem>
            {
                new SparkButtonItem
                {
                    IsPrimary=true,
                    IconGlyph="👁",
                    Label="Детали",
                    Command=SelectCommand,
                    CommandParameter=a
                }
            };

            // RBAC (може ли воопшто) + бизнис-правило (има ли смисла сега)
            if(CanUpdate&&CanEdit(a))
                actions.Add(new SparkButtonItem { IconGlyph="✎", Label="Промени", Command=EditCommand, CommandParameter=a });

            if(CanDelete&&CanCancel(a))
                actions.Add(new SparkButtonItem { Label="Откажи Термин", Command=CancelAppointmentCommand, CommandParameter=a });

            row["Actions"]=actions;

            rows.Add(row);
        }

        GridRows=rows;
    }

    private static string StatusLabel(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => "Закажан",
        AppointmentStatus.CheckedIn => "Пријавен",
        AppointmentStatus.Completed => "Завршен",
        AppointmentStatus.Cancelled => "Откажан",
        AppointmentStatus.Missed => "Пропуштен",
        AppointmentStatus.InProgress => "Во тек",
        AppointmentStatus.ReScheduled => "Презакажан",
        _ => status.ToString()
    };

    private static SparkBadgeTone StatusToTone(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Completed => SparkBadgeTone.Success,
        AppointmentStatus.Cancelled or AppointmentStatus.Missed => SparkBadgeTone.Danger,
        _ => SparkBadgeTone.Neutral
    };
}

public enum AppointmentStatusFilter
{
    All,
    Active,
    Scheduled,
    CheckedIn,
    Completed,
    Cancelled,
    Missed
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