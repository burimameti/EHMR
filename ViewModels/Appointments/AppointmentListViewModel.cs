using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Desktop.Core.ViewModels;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Domain.Search;
using EHMR.Infrastructure.Persistence;
using EHMR.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace EHMR.ViewModels;

public partial class AppointmentListViewModel
    : BaseViewModel<Appointment>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Appointment> _selectedItemService;
    private readonly IAuthorizationService _authService;
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

    public IReadOnlyList<AppointmentStatusOption> StatusFilters =
[
    new() { Filter = AppointmentStatusFilter.Active,    Label = "Активни" },
    new() { Filter = AppointmentStatusFilter.All,       Label = "Сите" },
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
            if(SetProperty(ref _selectedStatus, value)) _=ApplyFilterAsync();
        }
    }

    private DateTime _filterDate = DateTime.Today;

    public DateTime FilterDate
    {
        get => _filterDate;
        set
        {
            if(SetProperty(ref _filterDate, value)) _=ApplyFilterAsync();
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
            if(value) _=ApplyFilterAsync(); else _=LoadAsync();
        }
    }

    public bool CanManageAppointments =>
        _authService.CanAccessModule(Modules.Appointments);

    public AppointmentListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<Appointment> selectedItemService,

        IAuthorizationService authService,
        IAppointmentSearchQueryHandler autocomplete)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _dbFactory=dbFactory;
        _selectedItemService=selectedItemService;
        _authService=authService;
        _autocomplete=autocomplete;
        _selectedStatus=StatusFilters.First(x =>
        x.Filter==AppointmentStatusFilter.Active);

        // SearchText is declared on the base view model, so a partial
        // OnSearchTextChanged hook here would never fire (partial method
        // hooks only resolve within the class that owns the [ObservableProperty]).
        // Subscribing to PropertyChanged works no matter which class owns it.
        PropertyChanged+=OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName!=nameof(SearchText)) return;

        // SearchText was changed programmatically (e.g. SelectSuggestionAsync /
        // ClearFiltersAsync set it while drilling into or leaving a history view).
        // In that case the change itself IS the side effect we want - re-running
        // the autocomplete search here would just reopen the dropdown right
        // after we closed it.
        if(_suppressSearchTextSideEffects) return;

        if(_activePersonId is not null)
        {
            // user started typing a fresh query - leave history drill-down mode
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
            _allItems=items;
            await ApplyFilterAsync();
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

    protected override IEnumerable<Appointment> FilterItems(string searchText, IEnumerable<Appointment> items)
    {
        if(_activePersonId is not null)
        {
            // Drilled into one person's complete record: id match, no date filter,
            // sorted most-recent-first since this is a history view.
            var history = _activePersonType==SearchEntityType.Doctor
                ? items.Where(x => x.Doctor!=null&&x.Doctor.Id.ToString()==_activePersonId)
                : items.Where(x => x.Patient!=null&&x.Patient.Id.ToString()==_activePersonId);

            if(SelectedStatus.Filter==AppointmentStatusFilter.Active)
                history=history.Where(x => x.Status==AppointmentStatus.Scheduled||x.Status==AppointmentStatus.CheckedIn);
            else if(SelectedStatus.Filter!=AppointmentStatusFilter.All)
            {
                var status = Enum.Parse<AppointmentStatus>(
                    SelectedStatus.Filter.ToString());
                history=history.Where(x => x.Status==status);
            }
            return history.OrderByDescending(x => x.ScheduledStart);
        }

        var query = items;
        if(!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query=query.Where(x =>
                (x.Patient?.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
                (x.Doctor?.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
                (x.ReasonForVisit?.Contains(term, StringComparison.OrdinalIgnoreCase)??false));
        }

        if(SelectedStatus.Filter==AppointmentStatusFilter.Active)
            query=query.Where(x => x.Status==AppointmentStatus.Scheduled||x.Status==AppointmentStatus.CheckedIn);
        else if(SelectedStatus.Filter!=AppointmentStatusFilter.All)
        {
            var status = Enum.Parse<AppointmentStatus>(
                SelectedStatus.Filter.ToString());

            query=query.Where(x => x.Status==status);
        }

        if(FilterByDate)
            query=query.Where(x => x.ScheduledStart.Date==FilterDate.Date);

        return query;
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
        _=ApplyFilterAsync();
    }

    [RelayCommand]
    private async Task SelectSuggestionAsync(SearchSuggestionDto suggestion)
    {
        if(suggestion is null) return;

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

        // Show the complete history by default: drop the date filter and
        // widen status to "all" - the person can narrow it again from here.
        FilterByDate=false;
        SelectedStatus=StatusFilters.First(x => x.Filter==AppointmentStatusFilter.All);

        await ApplyFilterAsync();
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

    [RelayCommand]
    private async Task SelectAsync(Appointment appointment)
    {
        _selectedItemService.SelectedItem=appointment;
        await NavigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    private static bool CanEdit(Appointment a) =>
        a.Status is AppointmentStatus.Scheduled or AppointmentStatus.CheckedIn&&a.ScheduledStart.Date>=DateTime.Today;

    private static bool CanCancel(Appointment a) =>
        a.Status==AppointmentStatus.Scheduled&&a.ScheduledStart.Date>=DateTime.Today;

    private static bool IsPast(Appointment a) => a.ScheduledStart<DateTime.Now;

    [RelayCommand]
    private async Task NewAppointment()
    {
        if(!CanManageAppointments)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за додавање на нови термини.", "OK");
            return;
        }

        _selectedItemService.SelectedItem=null; // Нов термин
        await this.NavigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
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
        SelectedStatus=StatusFilters.First(x => x.Filter==AppointmentStatusFilter.Active);
        await ApplyFilterAsync();
    }
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