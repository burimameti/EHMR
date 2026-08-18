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
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
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

    partial void OnSelectedSuggestionChanged(SearchSuggestionDto? value)
    {
        if(value!=null)
            _=SelectSuggestionAsync(value);
    }
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
        new() { Filter = AppointmentStatusFilter.Active,    Label = "Активни" },
        new() { Filter = AppointmentStatusFilter.Scheduled, Label = "Закажан" },
        new() { Filter = AppointmentStatusFilter.CheckedIn, Label = "Пријавен" },
        new() { Filter = AppointmentStatusFilter.Completed, Label = "Завршен" },
        new() { Filter = AppointmentStatusFilter.Cancelled, Label = "Откажан" },
        new() { Filter = AppointmentStatusFilter.Missed,    Label = "Пропуштен" }
    ];

    private async Task DebouncedSearchAsync(string text)
    {
        var result = await DebouncedSuggestionSearchAsync(text, async (q, token) =>
        {
            var request = new AppointmentSearchQuery(q,
                new[] { SearchEntityType.Patient, SearchEntityType.Doctor },
                8);
            return await _autocomplete.Handle(request, token);
        });

        if(result is null) return; // cancelled од понова тестирка

        Suggestions=result;
        SelectedIndex=-1;
        SelectedSuggestion=null;
        ShowSuggestions=result.Count>0;
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
            RefreshSparkTabCounts();
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
            RefreshSparkTabCounts();
            ApplyPipeline();
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
            if(!SetProperty(ref _filterByDate, value)) return;
            RefreshSparkTabCounts();
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

        RefreshSparkTabCounts();
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

            ApplyPendingQuery();
            RefreshSparkTabCounts();
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
        RefreshSparkTabCounts();
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

        RefreshSparkTabCounts();
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
        SelectedItemService.OpenInEditMode=true;  // ← ДОДАЈ ОВО
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

            // Full reload also drives RefreshSparkTabCounts() + ApplyPipeline() again.
            await LoadAsync();
        }
        catch(Exception ex)
        {
            OnError($"Грешка при откажување термин: {ex.Message}");
        }
    }

    private static bool CanEdit(Appointment a) =>
        a.Status is (AppointmentStatus.Scheduled or AppointmentStatus.CheckedIn)&&
        a.ScheduledStart.Date>=DateTime.Today;

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
        FilterByDate=false;
        SelectedStatus=StatusFilters.First(x => x.Filter==AppointmentStatusFilter.All);

        RefreshSparkTabCounts();
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
            (x.ClinicalNotes?.Contains(term, StringComparison.OrdinalIgnoreCase)??false));
    }

    protected override IEnumerable<Appointment> ApplyFilters(IEnumerable<Appointment> items)
    {
        if(_activePersonId is not null)
        {
            var history = _activePersonType==SearchEntityType.Doctor
                ? items.Where(x => x.Doctor!=null&&x.Doctor.Id.ToString()==_activePersonId)
                : items.Where(x => x.Patient!=null&&x.Patient.Id.ToString()==_activePersonId);

            return ApplyStatusFilter(history);
        }

        var query = ApplyStatusFilter(items);

        if(FilterByDate)
            query=query.Where(x => x.ScheduledStart.Date==FilterDate.Date);

        return query;
    }

    // Status filtering was duplicated in ApplyFilters (once for the person-history
    // branch, once for the normal branch) and again in RefreshSparkTabCounts.
    // Pulled out once so all three stay in sync by construction.
    private IEnumerable<Appointment> ApplyStatusFilter(IEnumerable<Appointment> items) =>
        SelectedStatus.Filter switch
        {
            AppointmentStatusFilter.All => items,
            AppointmentStatusFilter.Active => items.Where(x =>
                x.Status==AppointmentStatus.Scheduled||x.Status==AppointmentStatus.CheckedIn),
            _ => items.Where(x => x.Status==Enum.Parse<AppointmentStatus>(SelectedStatus.Filter.ToString()))
        };

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

    // Mirrors ApplySearch + the date/person parts of ApplyFilters, but WITHOUT
    // the status filter, so per-status counts can be computed against the same
    // context (search term / date filter / drilled-down person) the grid uses.
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
                AppointmentStatusFilter.Active => baseItems.Count(x =>
                    x.Status==AppointmentStatus.Scheduled||x.Status==AppointmentStatus.CheckedIn),
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

    // "Денес" is now a plain bound Button directly in AppointmentListPage.xaml
    // (Command="{Binding ToggleTodayCommand}", styled off FilterByDate via
    // DataTrigger) instead of a SparkButtonItem, so it no longer needs to be
    // built or synced here at all.
    protected override void BuildSparkButtons()
    {
        Buttons.Clear();
        AddClearFiltersButton();
    }

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {      new() { Header = "БРОЈ", Key = "AppointmentNumber", Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header = "ИМЕ И ПРЕЗИМЕ", Key = "Patient", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "РЕУМАТОЛОГ", Key = "Doctor", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "ДАТУМ", Key = "Date", Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "ВРЕМЕ", Key = "Time", Width = new GridLength(1, GridUnitType.Star) },
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
            row["Time"]=a.ScheduledStart.ToString("HH:mm");
            row["Status"]=new SparkBadgeValue(StatusLabel(a.Status), StatusToTone(a.Status));

            // Select + (условен) Edit — сега преку base helper, RBAC + бизнис-правило заедно
            AddDefaultActions(a, row, detailLabel: "Повеќе", editLabel: "Промени", canEditPredicate: CanEdit);

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