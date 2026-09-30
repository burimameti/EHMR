using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using EHMR.ViewModels.Patients.Extensions;
using Microsoft.EntityFrameworkCore;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;


namespace EHMR.ViewModels.Calendar;

public partial class CalendarDashboardViewModel : BaseViewModel<Encounter>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Appointment> _appointmentSelect;
    private int _encounterNavigationVersion;
    private bool _isEncounterNavigationInProgress;
    private DateTime _currentDate;
    private DateTime _currentWeekStart;

    protected override string ModuleName => "Calendar";
    protected override Func<Encounter, Guid?>? DoctorOwnerSelector => e => e.DoctorId;

    [ObservableProperty] private string _currentMonthYearText = string.Empty;
    [ObservableProperty] private bool _isViewingCurrentMonth = true;

    [ObservableProperty] private int _monthlyEncountersCount;
    [ObservableProperty] private int _todaysEncountersCount;
    [ObservableProperty] private int _upcomingEncountersCount;

    [ObservableProperty] private string _completedPercentageText = "0%";

    [ObservableProperty] private CalendarDayDto? _selectedCalendarDay;

    [ObservableProperty] private bool _isMonthViewActive = true;
    [ObservableProperty] private bool _isDayViewActive;
    [ObservableProperty] private bool _isStatsViewActive = true;

    private DateTime _startOfMonth;
    private DateTime _endOfMonth;

    [ObservableProperty]
    private CalendarMode currentMode;

    // ═══════════════════════════════════════════ ПРЕГЛЕДИ / ТЕРМИНИ ═══════════════════════════════════════════

    [ObservableProperty] private CalendarContentMode _contentMode = CalendarContentMode.Encounters;

    public bool IsEncounterContentActive => ContentMode==CalendarContentMode.Encounters;
    public bool IsAppointmentContentActive => ContentMode==CalendarContentMode.Appointments;

    private bool ShowsAppointments => ContentMode==CalendarContentMode.Appointments;

    // Натписите низ страната се менуваат заедно со режимот.
    public string PageTitle => ShowsAppointments ? "Календар на термини" : "Календар на регледи";
    public string ContentTotalTitle => ShowsAppointments ? "Вкупно термини" : "Вкупно прегледи";
    public string ContentNewButtonText => ShowsAppointments ? "Нов термин" : "Нов преглед";
    public string WaitlistTitle => ShowsAppointments ? "Термини" : "Прегледи";

    partial void OnContentModeChanged(CalendarContentMode value)
    {
        OnPropertyChanged(nameof(IsEncounterContentActive));
        OnPropertyChanged(nameof(IsAppointmentContentActive));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ContentTotalTitle));
        OnPropertyChanged(nameof(ContentNewButtonText));
        OnPropertyChanged(nameof(WaitlistTitle));

        // Статусите се различни по режим — освежи ја листата и врати го изборот на „Сите".
        // Вчитувањето е потиснато тука: SetContentModeAsync го прави еднаш, по промената.
        _suppressStatusReload=true;
        try
        {
            Statuses.Clear();
            foreach(var status in StatusLookup.DisplayValues)
                Statuses.Add(status);

            SelectedStatus="Сите";
        }
        finally
        {
            _suppressStatusReload=false;
        }
    }

    private bool _suppressStatusReload;

    [RelayCommand]
    private async Task SetContentModeAsync(string mode)
    {
        if(!Enum.TryParse<CalendarContentMode>(mode, true, out var parsed))
            return;

        if(parsed==ContentMode)
            return;

        ContentMode=parsed;
        await LoadDashboardDataAsync();
    }

    // Which of the three layouts on the page is showing. Week is the default.
    public bool IsWeekModeActive => CurrentMode==CalendarMode.Week;
    public bool IsMonthModeActive => CurrentMode==CalendarMode.Month;
    public bool IsDayModeActive => CurrentMode==CalendarMode.Day;

    /// <summary>Header label for the single-day layout, e.g. "Понеделник, 27 јули 2026".</summary>
    public string SelectedDayTitle =>
        SelectedCalendarDay is null
            ? string.Empty
            : SelectedCalendarDay.Date.ToString("dddd, dd MMMM yyyy", new CultureInfo("mk-MK"));

    partial void OnCurrentModeChanged(CalendarMode value)
    {
        OnPropertyChanged(nameof(IsWeekModeActive));
        OnPropertyChanged(nameof(IsMonthModeActive));
        OnPropertyChanged(nameof(IsDayModeActive));
    }

    partial void OnSelectedCalendarDayChanged(CalendarDayDto? value)
        => OnPropertyChanged(nameof(SelectedDayTitle));

    [ObservableProperty] private ObservableCollection<HourlyTimelineSlotDto> _hourlyTimelineSlots = new();
    [ObservableProperty] private ObservableCollection<CalendarDayDto> _weekDays = new();

    // ═══════════════════════════════════════════ ЗАГЛАВЈА ПО ДЕН ═══════════════════════════════════════════
    //
    // Седумте заглавја се врзуваат за овие обични својства, не за WeekDays[0..6].
    // Индексираното врзување се пресметува при градење на страната, кога колекцијата
    // е сè уште празна, и не се освежува кога подоцна ќе се наполни — заглавјата
    // остануваа празни, а сината значка се прикажуваше насекаде затоа што паднатото
    // врзување на IsVisible се враќа на true.

    public CalendarDayDto? WeekDay0 => WeekDays.ElementAtOrDefault(0);
    public CalendarDayDto? WeekDay1 => WeekDays.ElementAtOrDefault(1);
    public CalendarDayDto? WeekDay2 => WeekDays.ElementAtOrDefault(2);
    public CalendarDayDto? WeekDay3 => WeekDays.ElementAtOrDefault(3);
    public CalendarDayDto? WeekDay4 => WeekDays.ElementAtOrDefault(4);
    public CalendarDayDto? WeekDay5 => WeekDays.ElementAtOrDefault(5);
    public CalendarDayDto? WeekDay6 => WeekDays.ElementAtOrDefault(6);

    private void NotifyWeekDayHeaders()
    {
        OnPropertyChanged(nameof(WeekDay0));
        OnPropertyChanged(nameof(WeekDay1));
        OnPropertyChanged(nameof(WeekDay2));
        OnPropertyChanged(nameof(WeekDay3));
        OnPropertyChanged(nameof(WeekDay4));
        OnPropertyChanged(nameof(WeekDay5));
        OnPropertyChanged(nameof(WeekDay6));
    }

    public ObservableCollection<CalendarDayDto> CalendarDays { get; } = new();

    public bool CanDeleteEncounters => CurrentUserRole=="Admin"||CurrentUserRole=="Doctor";
    public string CurrentUserRole { get; set; } = "Doctor";

    // ═══════════════════════════════════════════ STATUS FILTER ═══════════════════════════════════════════

    /// <summary>
    /// AppointmentStatus и EncounterStatus користат исти четири статуси.
    /// </summary>
    public static FilterLookup EncounterStatusLookup { get; } = new(new[]
    {
        ("Сите", "All"),
        ("Закажан", "Scheduled"),
        ("Пријавен"),
        ("Во тек", "InProgress"),
        ("Завршен", "Completed"),
        ("Откажан", "Cancelled"),
    });

    public static FilterLookup AppointmentStatusLookup { get; } = new(new[]
    {
        ("Сите", "All"),
        ("Закажан", "Scheduled"),
        ("Во тек", "InProgress"),
        ("Завршен", "Completed"),
        ("Откажан", "Cancelled"),
        ("Закажан", "Scheduled")
    });

    private FilterLookup StatusLookup =>
        ContentMode==CalendarContentMode.Appointments
            ? AppointmentStatusLookup
            : EncounterStatusLookup;

    public ObservableCollection<string> Statuses
    {
        get;
    } = new(EncounterStatusLookup.DisplayValues);

    /// <summary>The Macedonian label shown in the picker.</summary>
    [ObservableProperty] private string _selectedStatus = "Сите";

    /// <summary>The enum name the query filters on.</summary>
    private string SelectedStatusValue => StatusLookup.ToInternal(SelectedStatus);

    // ═══════════════════════════════════════════ WAITLIST PANEL ═══════════════════════════════════════════

    [ObservableProperty] private ObservableCollection<WaitlistItemDto> _pendingEncounters = new();
    [ObservableProperty] private ObservableCollection<WaitlistItemDto> _inProgressEncounters = new();
    [ObservableProperty] private ObservableCollection<WaitlistItemDto> _scheduledEncounters = new();

    public int PendingCount => PendingEncounters.Count;
    public int InProgressCount => InProgressEncounters.Count;
    public int ScheduledCount => ScheduledEncounters.Count;

    // ═══════════════════════════════════════════ WAITLIST PANEL COLLAPSE ═══════════════════════════════════════════

    [ObservableProperty] private bool _isWaitlistExpanded = true;

    /// <summary>Ширина на десниот панел: полна кога е отворен, само лентата со копчето кога е собран.</summary>
    public double WaitlistPanelWidth => IsWaitlistExpanded ? 340 : 34;

    /// <summary>Стрелката се врти според состојбата.</summary>
    public string WaitlistToggleGlyph => IsWaitlistExpanded ? "›" : "‹";

    partial void OnIsWaitlistExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(WaitlistPanelWidth));
        OnPropertyChanged(nameof(WaitlistToggleGlyph));
    }

    [RelayCommand]
    private void ToggleWaitlistPanel() => IsWaitlistExpanded=!IsWaitlistExpanded;

    [ObservableProperty] private bool _isPendingTabActive = true;
    [ObservableProperty] private bool _isInProgressTabActive;
    [ObservableProperty] private bool _isScheduledTabActive;

    public CalendarDashboardViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService dialog,
        IMenuService menu,
        IAuthorizationService authorization,
        ISelectedItemService<Encounter> encounterSelect,
        ISelectedItemService<Appointment> appointmentSelect)
        : base(navigationService, dialog, menu, authorization, encounterSelect)
    {
        _dbFactory=dbFactory;
        _appointmentSelect=appointmentSelect;

        _currentDate=DateTime.Today;
        _currentWeekStart=StartOfWeek(_currentDate);
        CurrentMode=CalendarMode.Week;

        // SearchText живее во BaseViewModel и неговиот setter вика ApplyPipeline(),
        // што работи врз AllItems — колекција што календарот воопшто не ја полни.
        // Затоа пишувањето во полето за пребарување немаше никаков ефект: терминот
        // се читаше во LoadEventsInRangeAsync, но никој не го повикуваше повторно.
        PropertyChanged+=OnSelfPropertyChanged;

        EvaluatePermissions();
        BuildSparkButtons();
    }
    private int InvalidateEncounterNavigation()
    {
        return Interlocked.Increment(ref _encounterNavigationVersion);
    }

    private bool IsCurrentEncounterNavigation(int version)
    {
        return version==Volatile.Read(ref _encounterNavigationVersion);
    }
    private static DateTime StartOfWeek(DateTime date)
    {
        int diff = (7+(date.DayOfWeek-DayOfWeek.Monday))%7;
        return date.AddDays(-diff).Date;
    }

    // ═══════════════════════════════════════════ COMMANDS ═══════════════════════════════════════════

    /// <summary>
    /// Set by pages that render the 7-column week grid (MainPage). Those pages have no
    /// day-view flyout, so selecting a day must only move the highlight — collapsing the
    /// timeline to a single day there empties six of the seven visible columns.
    /// </summary>
    public bool UsesWeekTimeline
    {
        get; set;
    }

    [RelayCommand]
    private async Task SelectCalendarDayAsync(CalendarDayDto? day)
    {
        if(day==null||day.IsEmptySlot)
            return;

        SelectedCalendarDay=day;
        HighlightSelectedDay();

        if(UsesWeekTimeline)
        {
            // Picking a day opens the single-day layout for it and re-points the side panel
            // at that day's encounters/appointments.
            _currentDate=day.Date;
            _currentWeekStart=StartOfWeek(day.Date);
            CurrentMode=CalendarMode.Day;

            await LoadDashboardDataAsync();
            return;
        }

        IsMonthViewActive=false;
        IsDayViewActive=true;
        IsStatsViewActive=false;

        BuildHourlyTimeline();
    }

    private void HighlightSelectedDay()
    {
        var selected = SelectedCalendarDay?.Date.Date;

        foreach(var day in WeekDays)
            day.IsSelected=day.Date.Date==selected;

        foreach(var day in CalendarDays)
            day.IsSelected=day.Date.Date==selected;
    }

    [RelayCommand]
    private void CloseDayView()
    {
        IsDayViewActive=false;
        IsMonthViewActive=true;
        IsStatsViewActive=true;

        SelectedCalendarDay=null;
        HighlightSelectedDay();
        RefreshWaitlist();

        HourlyTimelineSlots.Clear();
    }

    [RelayCommand]
    private void ChangeMode(CalendarMode section)
    {
        CurrentMode=section;
    }

    [RelayCommand]
    private async Task SetCalendarViewAsync(string mode)
    {
        // The XAML sends "Day" / "Week" / "Month"; an unparsed value used to leave the mode
        // untouched, which made the buttons look dead.
        if(!Enum.TryParse<CalendarMode>(mode, true, out var parsed))
            return;

        CurrentMode=parsed;

        if(parsed==CalendarMode.Day)
        {
            // Day mode needs a concrete day; fall back to today when nothing is picked.
            _currentDate=SelectedCalendarDay?.Date??DateTime.Today;
            _currentWeekStart=StartOfWeek(_currentDate);

            SelectedCalendarDay??=
                WeekDays.FirstOrDefault(x => x.IsToday)
                ??CalendarDays.FirstOrDefault(x => x.IsToday);
        }

        await LoadDashboardDataAsync();
    }

    [RelayCommand]
    private async Task GoToTodayAsync()
    {
        _currentDate=DateTime.Today;
        _currentWeekStart=StartOfWeek(_currentDate);

        await LoadDashboardDataAsync();

        // Само означи го денешниот ден. Не го менувај приказот — „Денес" значи
        // оди на денешен датум, не префрли се во дневен приказ.
        SelectedCalendarDay=
            WeekDays.FirstOrDefault(x => x.IsToday)
            ??CalendarDays.FirstOrDefault(x => x.IsToday);

        HighlightSelectedDay();
        RefreshWaitlist();
    }

    /// <summary>
    /// The ‹ › arrows step by whatever the active mode shows, so the header text and the
    /// visible days always move together.
    /// </summary>
    [RelayCommand]
    private async Task NextPeriodAsync() => await ShiftPeriodAsync(1);

    [RelayCommand]
    private async Task PreviousPeriodAsync() => await ShiftPeriodAsync(-1);

    private async Task ShiftPeriodAsync(int direction)
    {
        switch(CurrentMode)
        {
            case CalendarMode.Month:
                _currentDate=_currentDate.AddMonths(direction);
                _currentWeekStart=StartOfWeek(_currentDate);
                break;

            case CalendarMode.Day:
                _currentDate=_currentDate.AddDays(direction);
                _currentWeekStart=StartOfWeek(_currentDate);
                break;

            default:
                _currentWeekStart=_currentWeekStart.AddDays(7*direction);
                _currentDate=_currentWeekStart;
                break;
        }

        await LoadDashboardDataAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync() => await ChangeMonth(1);

    [RelayCommand]
    private async Task PreviousMonthAsync() => await ChangeMonth(-1);

    private async Task ChangeMonth(int months)
    {
        _currentDate=_currentDate.AddMonths(months);
        _currentWeekStart=StartOfWeek(_currentDate);
        await LoadDashboardDataAsync();
    }

    [RelayCommand]
    private async Task NextWeekAsync() => await ShiftPeriodAsync(1);

    [RelayCommand]
    private async Task PreviousWeekAsync() => await ShiftPeriodAsync(-1);
    [RelayCommand]
    private async Task CreateNewEncounterForSelectedDayAsync()
    {
        InvalidateEncounterNavigation();

        var targetDate =
            SelectedCalendarDay?.Date
            ??DateTime.Today;

        if(ShowsAppointments)
        {
            _appointmentSelect.SelectedItem=null;

            await NavigationService.GoToAsync(
                AppRoutes.Appointments.Detail);

            return;
        }

        SelectedItemService.SelectedItem=null;

        await NavigationService.GoToAsync(
            $"{AppRoutes.Encounters.Create}?date={targetDate.Ticks}");
    }

    [RelayCommand]
    private async Task CreateNewEncounterForSpecificHourAsync(
     HourlyTimelineSlotDto slot)
    {
        if(SelectedCalendarDay==null||slot==null)
            return;

        InvalidateEncounterNavigation();

        var targetDateTime =
            SelectedCalendarDay.Date.Date
                .AddHours(slot.HourValue);

        SelectedItemService.SelectedItem=null;

        await NavigationService.GoToAsync(
            $"{AppRoutes.Encounters.Create}?date={targetDateTime.Ticks}");
    }

    /// <summary>
    /// Отвора детали за ставката од календарот. Во режим „Термини" Id-то е на термин,
    /// не на преглед — порано клик врз картичка во тој режим тивко не правеше ништо.
    /// </summary>
    [RelayCommand]
    public async Task ProcessEncounterSelectionAsync(Guid itemId)
    {
        if(itemId==Guid.Empty)
            return;

        // Every selection gets its own version.
        // Opening NEW later will increment this and invalidate this operation.
        var navigationVersion =
            Interlocked.Increment(ref _encounterNavigationVersion);

        try
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            if(ShowsAppointments)
            {
                var appointment = await db.Appointments
                    .AsNoTracking()
                    .Include(x => x.Patient)
                    .Include(x => x.Doctor)
                        .ThenInclude(x => x.User)
                    .FirstOrDefaultAsync(x => x.Id==itemId);

                // Something else happened while DB was loading.
                if(!IsCurrentEncounterNavigation(navigationVersion))
                    return;

                if(appointment==null)
                    return;

                _appointmentSelect.SelectedItem=appointment;

                // Check once more immediately before navigation.
                if(!IsCurrentEncounterNavigation(navigationVersion))
                    return;

                await NavigationService.GoToAsync(
                    AppRoutes.Appointments.Detail);

                return;
            }

            var encounter = await db.Encounters
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id==itemId);

            // User may have clicked "New Encounter" while this query ran.
            if(!IsCurrentEncounterNavigation(navigationVersion))
                return;

            if(encounter==null)
                return;

            SelectedItemService.SelectedItem=encounter;

            // Prevent stale navigation as well as stale SelectedItem.
            if(!IsCurrentEncounterNavigation(navigationVersion))
            {
                SelectedItemService.SelectedItem=null;
                return;
            }

            await NavigationService.GoToAsync(
                AppRoutes.Encounters.Detail);
        }
        catch(Exception ex)
        {
            Debug.WriteLine(
                $"Encounter navigation failed: {ex}");
        }
    }

    [RelayCommand]
    private async Task DeleteEncounterAsync(CalendarEventDto eventDto)
    {
        if(!CanDeleteEncounters||eventDto==null)
            return;

        await ExecuteSafeAsync(async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var encounter = await db.Encounters.FirstOrDefaultAsync(x => x.Id==eventDto.Id);
            if(encounter!=null)
            {
                db.Encounters.Remove(encounter);
                await db.SaveChangesAsync();
            }

            await LoadDashboardDataCoreAsync();
            CloseDayView();
        }, "Грешка при бришење на прегледот");
    }

    [RelayCommand]
    private async Task ApplyCalendarSearchAsync()
    {
        _searchDebounceCts?.Cancel();
        await LoadDashboardDataAsync();
    }

    // ═══════════════════════════════════════════ SEARCH ═══════════════════════════════════════════

    private CancellationTokenSource? _searchDebounceCts;

    private const int SearchDebounceMs = 350;

    public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

    [RelayCommand]
    private void ClearSearch() => SearchText=string.Empty;

    private async void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName!=nameof(SearchText))
            return;

        OnPropertyChanged(nameof(HasSearchText));

        await DebouncedSearchReloadAsync();
    }

    /// <summary>
    /// Го чека корисникот да престане да пишува, па повторно го вчитува опсегот.
    /// Пребарувањето се извршува во базата (LoadEventsInRangeAsync), не во меморија,
    /// па не смее да оди по притисок на секое копче.
    /// </summary>
    private async Task DebouncedSearchReloadAsync()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts=new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        try
        {
            await Task.Delay(SearchDebounceMs, token);

            // LoadDashboardDataAsync тивко излегува ако веќе тече вчитување,
            // па почекај да заврши за да не се изгуби последниот внес.
            while(IsBusy)
                await Task.Delay(50, token);

            await LoadDashboardDataAsync();
        }
        catch(OperationCanceledException)
        {
            // Пристигна понов внес — овој пат нема што да се прави.
        }
    }

    public override void Dispose()
    {
        PropertyChanged-=OnSelfPropertyChanged;

        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts=null;

        base.Dispose();
    }

    /// <summary>
    /// The KPI cards above the grid open the encounters list, filtered to what the card counts.
    /// Pass an EncounterStatus name, or null/empty for "all".
    /// </summary>
    [RelayCommand]
    private async Task OpenEncountersAsync(string? statusFilter)
    {
        var query = new Dictionary<string, object>();

        if(!string.IsNullOrWhiteSpace(statusFilter))
            query["statusFilter"]=statusFilter;

        await NavigationService.GoToAsync(AppRoutes.Encounters.List, query);
    }

    [RelayCommand]
    private void SelectWaitlistTab(string tab)
    {
        IsPendingTabActive=tab=="Pending";
        IsInProgressTabActive=tab=="InProgress";
        IsScheduledTabActive=tab=="Scheduled";
    }

    async partial void OnSelectedStatusChanged(string value)
    {
        if(_suppressStatusReload)
            return;

        await LoadDashboardDataAsync();
    }

    // ═══════════════════════════════════════════ SPARK WEEK TABS ═══════════════════════════════════════════

    public ObservableCollection<SparkTabItem> WeekTabs { get; } = new();

    private int? _highlightedWeekIndex;

    private void BuildWeekTabs(List<CalendarDayDto> days)
    {
        WeekTabs.Clear();

        var weekGroups = days
            .Where(d => d.IsCurrentMonth)
            .GroupBy(d => d.WeekIndex)
            .OrderBy(g => g.Key)
            .ToList();

        int weekNumber = 1;
        foreach(var group in weekGroups)
        {
            var weekIndex = group.Key;
            var encounterCount = group.Sum(d => d.EventCount);

            var tab = new SparkTabItem
            {
                Title=$"Недела {weekNumber}",
                Value=encounterCount.ToString("N0")
            };

            tab.Command=new RelayCommand(() => ToggleWeekHighlight(tab, weekIndex));

            WeekTabs.Add(tab);
            weekNumber++;
        }
    }

    private void ToggleWeekHighlight(SparkTabItem tab, int weekIndex)
    {
        if(_highlightedWeekIndex==weekIndex)
        {
            _highlightedWeekIndex=null;
            foreach(var t in WeekTabs) t.IsSelected=false;
        }
        else
        {
            _highlightedWeekIndex=weekIndex;
            foreach(var t in WeekTabs) t.IsSelected=false;
            tab.IsSelected=true;
        }

        ApplyWeekHighlight();
    }

    private void ApplyWeekHighlight()
    {
        foreach(var day in CalendarDays)
            day.IsWeekHighlighted=_highlightedWeekIndex!=null&&day.WeekIndex==_highlightedWeekIndex;
    }

    // ═══════════════════════════════════════════ DATA LOAD ═══════════════════════════════════════════

    public async Task LoadDashboardDataAsync()
        => await ExecuteSafeAsync(LoadDashboardDataCoreAsync, "Грешка при вчитување на календарот");

    private async Task LoadDashboardDataCoreAsync()
    {
        InitializeCurrentMonth();

        // Widen the query range so it always covers both the visible month grid
        // AND the visible week grid, even when the week spans a month boundary.
        var queryStart = _startOfMonth<_currentWeekStart ? _startOfMonth : _currentWeekStart;
        var queryEndCandidate = _currentWeekStart.AddDays(6);
        var queryEnd = _endOfMonth>queryEndCandidate ? _endOfMonth : queryEndCandidate;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var events = await LoadEventsInRangeAsync(db, queryStart, queryEnd);
        _loadedEvents=events;

        // Претходниот период се вчитува одделно за споредбите во KPI картичките.
        // Оди низ истиот филтер (пребарување + статус), за да е споредбата фер.
        var (previousStart, previousEnd)=PreviousPeriod;
        var previousEvents = await LoadEventsInRangeAsync(db, previousStart, previousEnd);

        UpdateStatistics(events, previousEvents);

        var calendarDays = BuildCalendarDays(events);
        var weekDays = BuildWeekDays(events);

        // Day mode collapses the timeline to the picked day in column 0; week/month keep the
        // seven columns. Built from the freshly loaded weekDays, not from the stale
        // SelectedCalendarDay instance left over from the previous load.
        var hourlySlots =
            CurrentMode==CalendarMode.Day
                ? BuildSingleDayTimeline(
                    weekDays.FirstOrDefault(d => d.Date.Date==(SelectedCalendarDay?.Date.Date??_currentDate.Date)))
                : BuildWeeklyHourlyTimeline(weekDays);

        // Awaited (not fire-and-forget) so any exception here is caught by
        // ExecuteSafeAsync and the busy state gets reset correctly.
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            BuildWeekTabs(calendarDays);

            WeekDays.Clear();
            foreach(var d in weekDays) WeekDays.Add(d);
            NotifyWeekDayHeaders();

            HourlyTimelineSlots.Clear();
            foreach(var s in hourlySlots) HourlyTimelineSlots.Add(s);

            // Last: RefreshCalendar re-applies the day-view timeline when one is open, and it
            // has to win over the weekly slots written just above rather than be overwritten.
            RefreshCalendar(calendarDays);

            // After RefreshCalendar, so the panel buckets against the re-resolved selected day.
            RefreshWaitlist();
        });
    }

    private void InitializeCurrentMonth()
    {
        _startOfMonth=new DateTime(
            _currentDate.Year,
            _currentDate.Month,
            1);

        _endOfMonth=_startOfMonth
            .AddMonths(1)
            .AddDays(-1);

        // The label has to describe what the grid is actually showing, otherwise stepping
        // through weeks looks like nothing moved whenever the week stays inside one month.
        var weekEnd = _currentWeekStart.AddDays(6);

        CurrentMonthYearText=CurrentMode switch
        {
            CalendarMode.Day =>
                _currentDate.ToString("dd MMMM yyyy").ToUpperInvariant(),

            CalendarMode.Week when _currentWeekStart.Month==weekEnd.Month =>
                $"{_currentWeekStart:dd} – {weekEnd:dd MMMM yyyy}".ToUpperInvariant(),

            CalendarMode.Week =>
                $"{_currentWeekStart:dd MMM} – {weekEnd:dd MMM yyyy}".ToUpperInvariant(),

            _ => _startOfMonth.ToString("MMMM yyyy").ToUpperInvariant()
        };

        IsViewingCurrentMonth=
            _currentDate.Year==DateTime.Today.Year&&
            _currentDate.Month==DateTime.Today.Month;
    }

    // ═══════════════════════════════════════════ KPI ПЕРИОДИ ═══════════════════════════════════════════

    /// <summary>Периодот што календарот моментално го прикажува.</summary>
    private (DateTime Start, DateTime End) CurrentPeriod => CurrentMode switch
    {
        CalendarMode.Day => (_currentDate.Date, _currentDate.Date),
        CalendarMode.Month => (_startOfMonth, _endOfMonth),
        _ => (_currentWeekStart, _currentWeekStart.AddDays(6))
    };

    /// <summary>Претходниот еквивалентен период, за споредба.</summary>
    private (DateTime Start, DateTime End) PreviousPeriod
    {
        get
        {
            var (start, end)=CurrentPeriod;

            return CurrentMode switch
            {
                CalendarMode.Day => (start.AddDays(-1), end.AddDays(-1)),
                // Претходниот месец завршува ден пред почетокот на тековниот.
                CalendarMode.Month => (start.AddMonths(-1), start.AddDays(-1)),
                _ => (start.AddDays(-7), end.AddDays(-7))
            };
        }
    }

    [ObservableProperty] private string _kpiPeriodText = string.Empty;
    [ObservableProperty] private string _kpiComparisonText = string.Empty;

    [ObservableProperty] private string _totalDeltaText = string.Empty;
    [ObservableProperty] private KpiDeltaTone _totalDeltaTone;

    [ObservableProperty] private string _completedDeltaText = string.Empty;
    [ObservableProperty] private KpiDeltaTone _completedDeltaTone;

    [ObservableProperty] private string _noShowDeltaText = string.Empty;
    [ObservableProperty] private KpiDeltaTone _noShowDeltaTone;

    private void UpdateStatistics(
        List<CalendarSourceItem> encounters,
        List<CalendarSourceItem> previousEncounters)
    {
        var today = DateTime.Today;
        var (periodStart, periodEnd)=CurrentPeriod;

        // Бројките се врзани за периодот што се гледа, за да се совпаднат
        // со насловот над календарот.
        var scoped = encounters
            .Where(x => x.EffectiveDate.Date>=periodStart&&x.EffectiveDate.Date<=periodEnd)
            .ToList();

        MonthlyEncountersCount=scoped.Count;

        TodaysEncountersCount=scoped.Count(x => x.EffectiveDate.Date==today);
        UpcomingEncountersCount=scoped.Count(x => x.EffectiveDate.Date>today);

        var completedRate = RateOf(scoped, "Completed");

        CompletedPercentageText=$"{completedRate:0}%";

        // ── споредба со претходниот период ──
        var previousCompletedRate = RateOf(previousEncounters, "Completed");

        TotalDeltaText=FormatCountDelta(scoped.Count, previousEncounters.Count);
        TotalDeltaTone=ToneFor(scoped.Count-previousEncounters.Count, higherIsBetter: true);

        CompletedDeltaText=FormatRateDelta(completedRate, previousCompletedRate);
        CompletedDeltaTone=ToneFor(completedRate-previousCompletedRate, higherIsBetter: true);


        (KpiPeriodText, KpiComparisonText)=CurrentMode switch
        {
            CalendarMode.Day => ("Овој ден", "од вчера"),
            CalendarMode.Month => ("Овој месец", "од минатиот месец"),
            _ => ("Оваа недела", "од минатата недела")
        };
    }

    /// <summary>Процент на ставки со даден статус во множеството.</summary>
    private static double RateOf(List<CalendarSourceItem> items, string status)
        => items.Count==0
            ? 0
            : items.Count(x => x.StatusText==status)*100.0/items.Count;

    /// <summary>Релативна промена во број на прегледи.</summary>
    private static string FormatCountDelta(int current, int previous)
    {
        if(previous==0)
            return current==0 ? string.Empty : "ново";

        var change = (current-previous)*100.0/previous;
        return $"{(change>=0 ? "+" : "")}{change:0}%";
    }

    /// <summary>Промена на стапка, прикажана како процент.</summary>
    private static string FormatRateDelta(double current, double previous)
    {
        var diff = current-previous;

        if(Math.Abs(diff)<0.5)
            return "0%";

        return $"{(diff>=0 ? "+" : "")}{diff:0}%";
    }

    private static KpiDeltaTone ToneFor(double diff, bool higherIsBetter)
    {
        if(Math.Abs(diff)<0.5)
            return KpiDeltaTone.Neutral;

        var improved = higherIsBetter ? diff>0 : diff<0;
        return improved ? KpiDeltaTone.Positive : KpiDeltaTone.Negative;
    }

    private List<CalendarDayDto> BuildCalendarDays(List<CalendarSourceItem> allEvents)
    {
        var monthEvents = allEvents
            .Where(x => x.EffectiveDate.Date>=_startOfMonth&&x.EffectiveDate.Date<=_endOfMonth)
            .ToList();

        var grouped = monthEvents
            .GroupBy(x => x.EffectiveDate.Date)
            .ToDictionary(x => x.Key, x => x.ToList());

        var result = new List<CalendarDayDto>();
        var firstDayOfMonth = _startOfMonth;
        var lastDayOfMonth = _endOfMonth;

        int offset = firstDayOfMonth.DayOfWeek switch
        {
            DayOfWeek.Sunday => 6,
            _ => (int)firstDayOfMonth.DayOfWeek-1
        };

        for(int i = offset; i>0; i--)
        {
            var date = firstDayOfMonth.AddDays(-i);
            result.Add(new CalendarDayDto
            {
                Date=date,
                IsEmptySlot=true,
                IsCurrentMonth=false
            });
        }

        int totalDays = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);

        for(int day = 1; day<=totalDays; day++)
        {
            var date = new DateTime(_currentDate.Year, _currentDate.Month, day);
            grouped.TryGetValue(date.Date, out var dayEvents);

            var dayDtoEvents = dayEvents?
                .OrderBy(x => x.EffectiveDate)
                .Select(x => CreateCalendarEvent(x))
                .ToList()
                ??new List<CalendarEventDto>();

            result.Add(new CalendarDayDto
            {
                Date=date,
                IsCurrentMonth=true,
                IsEmptySlot=false,
                Events=dayDtoEvents
            });
        }

        int remaining = 42-result.Count;
        for(int i = 1; i<=remaining; i++)
        {
            var date = lastDayOfMonth.AddDays(i);
            result.Add(new CalendarDayDto
            {
                Date=date,
                IsEmptySlot=true,
                IsCurrentMonth=false
            });
        }

        for(int i = 0; i<result.Count; i++)
            result[i].WeekIndex=i/7;

        return result;
    }

    private List<CalendarDayDto> BuildWeekDays(List<CalendarSourceItem> allEvents)
    {
        var weekEnd = _currentWeekStart.AddDays(6);

        var weekEvents = allEvents
            .Where(x => x.EffectiveDate.Date>=_currentWeekStart&&x.EffectiveDate.Date<=weekEnd)
            .ToList();

        var grouped = weekEvents
            .GroupBy(x => x.EffectiveDate.Date)
            .ToDictionary(x => x.Key, x => x.ToList());

        var days = new List<CalendarDayDto>();
        for(int i = 0; i<7; i++)
        {
            var date = _currentWeekStart.AddDays(i);
            grouped.TryGetValue(date.Date, out var dayEvents);

            days.Add(new CalendarDayDto
            {
                Date=date,
                IsCurrentMonth=date.Month==_currentDate.Month,
                IsEmptySlot=false,
                Events=dayEvents?
                    .OrderBy(x => x.EffectiveDate)
                    .Select(CreateCalendarEvent)
                    .ToList()??new List<CalendarEventDto>()
            });
        }

        return days;
    }

    /// <summary>
    /// The clinic day opens at 07:00, so the timeline is rotated to start there and wrap through
    /// midnight to 06:00 — all 24 slots, in the order the day is actually worked.
    /// </summary>
    private const int DayStartHour = 7;

    private static IEnumerable<int> TimelineHours()
        => Enumerable.Range(0, 24).Select(i => (DayStartHour+i)%24);

    private static List<HourlyTimelineSlotDto> BuildWeeklyHourlyTimeline(List<CalendarDayDto> weekDays)
    {
        var slots = new List<HourlyTimelineSlotDto>();

        foreach(var hour in TimelineHours())
        {
            var slot = new HourlyTimelineSlotDto
            {
                HourText=$"{hour:D2}:00",
                HourValue=hour
            };

            for(int column = 0; column<HourlyTimelineSlotDto.WeekColumnCount; column++)
            {
                if(column>=weekDays.Count)
                    break;

                slot.SetColumn(column, weekDays[column].Events
                    .Where(x => x.ScheduledTime.Hour==hour)
                    .OrderBy(x => x.ScheduledTime));
            }

            slots.Add(slot);
        }

        return slots;
    }

    /// <summary>
    /// Events currently loaded for the visible range, kept so the side panel can be re-bucketed
    /// for a newly picked day without another round trip to the database.
    /// </summary>
    private List<CalendarSourceItem> _loadedEvents = new();

    /// <summary>The day the side panel is listing — the picked day, or today when nothing is picked.</summary>
    private DateTime WaitlistDate => SelectedCalendarDay?.Date.Date??DateTime.Today;

    [ObservableProperty] private string _waitlistDateText = string.Empty;

    private void RefreshWaitlist()
    {
        var buckets = BuildWaitlistBuckets(_loadedEvents);

        PendingEncounters.Clear();
        foreach(var w in buckets.Pending) PendingEncounters.Add(w);

        InProgressEncounters.Clear();
        foreach(var w in buckets.InProgress) InProgressEncounters.Add(w);

        ScheduledEncounters.Clear();
        foreach(var w in buckets.Scheduled) ScheduledEncounters.Add(w);

        var date = WaitlistDate;
        WaitlistDateText=date==DateTime.Today
            ? "Денес"
            : date.ToString("dd MMMM yyyy");

        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(InProgressCount));
        OnPropertyChanged(nameof(ScheduledCount));
    }

    private (List<WaitlistItemDto> Pending, List<WaitlistItemDto> InProgress, List<WaitlistItemDto> Scheduled)
        BuildWaitlistBuckets(List<CalendarSourceItem> allEvents)
    {
        var target = WaitlistDate;
        var todaysItems = allEvents.Where(x => x.EffectiveDate.Date==target).ToList();

        var pending = todaysItems
            .Where(x => x.StatusText is "Scheduled" or "InProgress")
            .OrderBy(x => x.EffectiveDate)
            .Select(ToWaitlistDto)
            .ToList();

        var inProgress = todaysItems
            .Where(x => x.StatusText=="InProgress")
            .OrderBy(x => x.EffectiveDate)
            .Select(ToWaitlistDto)
            .ToList();

        var scheduled = todaysItems
            .Where(x => x.StatusText is "Completed" or "Cancelled" or "Scheduled")
            .OrderBy(x => x.EffectiveDate)
            .Select(ToWaitlistDto)
            .ToList();

        return (pending, inProgress, scheduled);
    }

    private static WaitlistItemDto ToWaitlistDto(CalendarSourceItem x) => new()
    {
        Id=x.Id,
        PatientName=x.Patient?.FullName??"",
        PatientInitials=BuildInitials(x.Patient?.FirstName, x.Patient?.LastName),
        PatientContact=x.Patient?.Phone??"",
        TimeRangeText=x.EffectiveDate.ToString("HH:mm"),
        DoctorName=x.Doctor?.FullName??""
    };

    private static string BuildInitials(string? first, string? last)
    {
        var f = string.IsNullOrWhiteSpace(first) ? "?" : first[..1];
        var l = string.IsNullOrWhiteSpace(last) ? "?" : last[..1];
        return (f+l).ToUpperInvariant();
    }

    private void RefreshCalendar(List<CalendarDayDto> days)
    {
        var previousSelected = SelectedCalendarDay?.Date;

        CalendarDays.Clear();
        foreach(var day in days)
            CalendarDays.Add(day);

        ApplyWeekHighlight();

        if(previousSelected==null)
            return;

        SelectedCalendarDay=
            CalendarDays.FirstOrDefault(x => x.Date.Date==previousSelected.Value.Date)
            ??WeekDays.FirstOrDefault(x => x.Date.Date==previousSelected.Value.Date);

        HighlightSelectedDay();

        // Only the day-view flyout replaces the timeline. On a week grid the seven columns
        // built by the caller must stay as they are.
        if(SelectedCalendarDay!=null&&!UsesWeekTimeline&&IsDayViewActive)
            BuildHourlyTimeline();
    }

    private static (Color Background, Color Text) GetStatusColors(string status) => status switch
    {
        "Scheduled" => (Color.FromArgb("#DBEAFE"), Color.FromArgb("#1D4ED8")),
        "InProgress" => (Color.FromArgb("#FEF3C7"), Color.FromArgb("#B45309")),
        "Completed" => (Color.FromArgb("#DCFCE7"), Color.FromArgb("#15803D")),
        "Cancelled" => (Color.FromArgb("#FEE2E2"), Color.FromArgb("#B91C1C")),
        _ => (Colors.LightGreen, Colors.DarkGreen)
    };

    private CalendarEventDto CreateCalendarEvent(CalendarSourceItem encounter)
    {
        var (bg, text)=GetStatusColors(encounter.StatusText);

        return new CalendarEventDto
        {
            Id=encounter.Id,
            Title=encounter.Patient?.FullName??"",
            Duration=TimeSpan.FromMinutes(30),
            Subtitle=encounter.Doctor?.FullName??"",
            PatientName=encounter.Patient?.FullName??"",
            DoctorName=encounter.Doctor?.FullName??"",
            ScheduledTime=encounter.EffectiveDate,
            Status=encounter.StatusText,
            EventType="Encounter",
            IconGlyph="\uf073",
            BackgroundColor=bg,
            TextColor=text
        };
    }

    private async Task<List<CalendarSourceItem>> LoadEventsInRangeAsync(
        DesktopTherapyDbContext db, DateTime rangeStart, DateTime rangeEnd)
        => ShowsAppointments
            ? await LoadAppointmentsInRangeAsync(db, rangeStart, rangeEnd)
            : await LoadEncountersInRangeAsync(db, rangeStart, rangeEnd);

    /// <summary>
    /// Режим „Термини": сите закажани термини во опсегот — и оние што веќе имаат
    /// отворен преглед. Во режим „Прегледи" тие се прикажуваат како преглед, тука
    /// како термин, зашто тоа е она што се закажува.
    /// </summary>
    private async Task<List<CalendarSourceItem>> LoadAppointmentsInRangeAsync(
        DesktopTherapyDbContext db, DateTime rangeStart, DateTime rangeEnd)
    {
        rangeStart=rangeStart.Date;
        var rangeEndExclusive = rangeEnd.Date.AddDays(1);

        var query = db.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Include(x => x.Encounter)
            .Where(x => x.ScheduledStart>=rangeStart&&x.ScheduledStart<rangeEndExclusive)
            .AsQueryable();

        if(!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query=query.Where(x =>
                x.Patient!=null&&(x.Patient.FirstName+" "+x.Patient.LastName).Contains(term));
        }

        var statusValue = SelectedStatusValue;
        if(statusValue!="All")
            query=query.Where(x => x.Status.ToString()==statusValue);

        var appointments = await query.ToListAsync();

        return appointments
            .Select(appt => new CalendarSourceItem
            {
                Id=appt.Id,
                Kind=CalendarItemKind.Appointment,
                EffectiveDate=appt.ScheduledStart,
                StatusText=appt.Status.ToString(),
                Patient=appt.Patient,
                Doctor=appt.Doctor,
                AppointmentId=appt.Id,
                EncounterId=appt.Encounter?.Id
            })
            .ToList();
    }

    private async Task<List<CalendarSourceItem>> LoadEncountersInRangeAsync(
        DesktopTherapyDbContext db, DateTime rangeStart, DateTime rangeEnd)
    {
        // rangeEnd arrives as a date at midnight. Comparing with <= against it dropped every
        // event on the last day of the range that had a time on it — i.e. all of Sunday.
        rangeStart=rangeStart.Date;
        var rangeEndExclusive = rangeEnd.Date.AddDays(1);

        var appointmentsQuery = db.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Include(x => x.Encounter)
            .Where(x => x.ScheduledStart>=rangeStart&&x.ScheduledStart<rangeEndExclusive)
            .AsQueryable();

        var encountersQuery = db.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Where(x => x.AppointmentId==null&&
                (x.ScheduledStart??x.StartTime??x.CheckInTime??x.EncounterDate)>=rangeStart&&
                (x.ScheduledStart??x.StartTime??x.CheckInTime??x.EncounterDate)<rangeEndExclusive)
            .AsQueryable();

        if(!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            appointmentsQuery=appointmentsQuery.Where(x =>
                x.Patient!=null&&(x.Patient.FirstName+" "+x.Patient.LastName).Contains(term));
            encountersQuery=encountersQuery.Where(x =>
                (x.Patient!=null&&(x.Patient.FirstName+" "+x.Patient.LastName).Contains(term))||
                (x.EncounterNumber??"").Contains(term));
        }

        var statusValue = SelectedStatusValue;
        if(statusValue!="All")
        {
            appointmentsQuery=appointmentsQuery.Where(x => x.Status.ToString()==statusValue);
            encountersQuery=encountersQuery.Where(x => x.Status.ToString()==statusValue);
        }

        var appointments = await appointmentsQuery.ToListAsync();
        var standaloneEncounters = await encountersQuery.ToListAsync();

        var result = new List<CalendarSourceItem>();

        foreach(var appt in appointments)
        {
            if(appt.Encounter!=null)
            {
                result.Add(new CalendarSourceItem
                {
                    Id=appt.Encounter.Id,
                    Kind=CalendarItemKind.Encounter,
                    EffectiveDate=appt.ScheduledStart,
                    StatusText=appt.Encounter.Status.ToString(),
                    Patient=appt.Patient,
                    Doctor=appt.Doctor,
                    AppointmentId=appt.Id,
                    EncounterId=appt.Encounter.Id
                });
            }
            else
            {
                result.Add(new CalendarSourceItem
                {
                    Id=appt.Id,
                    Kind=CalendarItemKind.Appointment,
                    EffectiveDate=appt.ScheduledStart,
                    StatusText=appt.Status.ToString(),
                    Patient=appt.Patient,
                    Doctor=appt.Doctor,
                    AppointmentId=appt.Id,
                    EncounterId=null
                });
            }
        }

        foreach(var enc in standaloneEncounters)
        {
            result.Add(new CalendarSourceItem
            {
                Id=enc.Id,
                Kind=CalendarItemKind.Encounter,
                EffectiveDate=ResolveEncounterTime(enc),
                StatusText=enc.Status.ToString(),
                Patient=enc.Patient,
                Doctor=enc.Doctor,
                AppointmentId=null,
                EncounterId=enc.Id
            });
        }

        return result;
    }

    /// <summary>
    /// Timeline for the single-day layout: the day's events all sit in column 0, which is what
    /// the day layout binds. Empty slots still render so the hour rulers stay continuous.
    /// </summary>
    private static List<HourlyTimelineSlotDto> BuildSingleDayTimeline(CalendarDayDto? day)
    {
        var slots = new List<HourlyTimelineSlotDto>();
        var dayEvents = day?.Events??new List<CalendarEventDto>();

        foreach(var hour in TimelineHours())
        {
            var slot = new HourlyTimelineSlotDto
            {
                HourText=$"{hour:D2}:00",
                HourValue=hour
            };

            slot.SetColumn(0, dayEvents
                .Where(x => x.ScheduledTime.Hour==hour)
                .OrderBy(x => x.ScheduledTime));

            slots.Add(slot);
        }

        return slots;
    }

    /// <summary>
    /// Which clock time an encounter occupies on the grid. An encounter started straight from
    /// the waitlist has no ScheduledStart and an EncounterDate at midnight — using that alone
    /// parked every in-progress visit in the 00:00 row instead of the hour it actually began.
    /// </summary>
    private static DateTime ResolveEncounterTime(Encounter encounter)
    {
        if(encounter.ScheduledStart.HasValue)
            return encounter.ScheduledStart.Value;

        if(encounter.StartTime.HasValue)
            return encounter.StartTime.Value;

        if(encounter.CheckInTime.HasValue)
            return encounter.CheckInTime.Value;

        return encounter.EncounterDate;
    }

    /// <summary>
    /// Builds the timeline for the single selected day (day-view flyout on CalendarDashboardPage).
    /// The day's events land in the column matching its weekday so a week grid bound to the same
    /// collection still lines up; the other six columns stay empty rather than missing.
    /// </summary>
    public void BuildHourlyTimeline()
    {
        HourlyTimelineSlots.Clear();

        if(SelectedCalendarDay==null)
            return;

        var dayEvents = SelectedCalendarDay.Events;

        var columnIndex = (int)(SelectedCalendarDay.Date.Date-_currentWeekStart).TotalDays;
        if(columnIndex<0||columnIndex>=HourlyTimelineSlotDto.WeekColumnCount)
            columnIndex=0;

        foreach(var hour in TimelineHours())
        {
            var slot = new HourlyTimelineSlotDto
            {
                HourText=$"{hour:D2}:00",
                HourValue=hour
            };

            slot.SetColumn(columnIndex, dayEvents
                .Where(x => x.ScheduledTime.Hour==hour)
                .OrderBy(x => x.ScheduledTime));

            HourlyTimelineSlots.Add(slot);
        }
    }

    protected override void BuildSparkButtons()
    {
        Buttons.Clear();
        Buttons.Add(new SparkButtonItem { IconGlyph="\uf053", Command=PreviousMonthCommand });
        Buttons.Add(new SparkButtonItem { Label="Денес", Command=GoToTodayCommand });
        Buttons.Add(new SparkButtonItem { IconGlyph="\uf054", Command=NextMonthCommand });
        Buttons.Add(new SparkButtonItem { IconGlyph="\uf067", Label="Нов преглед", Command=CreateNewEncounterForSelectedDayCommand });
    }

    // ═══════════════════════════════════════════ BASE PIPELINE ═══════════════════════════════════════════

    protected override IEnumerable<Encounter> ApplySearch(IEnumerable<Encounter> query, string search)
    {
        if(string.IsNullOrWhiteSpace(search))
            return query;

        search=search.Trim();

        return query.Where(x =>
            (x.Patient?.FullName??"").Contains(search, StringComparison.OrdinalIgnoreCase)||
            (x.EncounterNumber??"").Contains(search, StringComparison.OrdinalIgnoreCase)||
            (x.Doctor?.User?.LastName??"").Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    protected override IEnumerable<Encounter> ApplyFilters(IEnumerable<Encounter> query) => query;

    protected override IEnumerable<Encounter> ApplySort(IEnumerable<Encounter> query) =>
        query.OrderBy(x => x.ScheduledStart??x.EncounterDate).ThenBy(x => x.Patient?.LastName);

    protected override void OnPageProjected(ObservableCollection<Encounter> page)
    {
    }
  
    protected override void ResetFilters() => SearchText=string.Empty;

}

// ═══════════════════════════════════════════ DTOs ═══════════════════════════════════════════

public enum CalendarItemKind
{
    Appointment, Encounter
}

internal class CalendarSourceItem
{
    public Guid Id
    {
        get; set;
    }
    public CalendarItemKind Kind
    {
        get; set;
    }
    public DateTime EffectiveDate
    {
        get; set;
    }
    public string StatusText { get; set; } = string.Empty;
    public Patient? Patient
    {
        get; set;
    }
    public Doctor? Doctor
    {
        get; set;
    }
    public Guid? AppointmentId
    {
        get; set;
    }
    public Guid? EncounterId
    {
        get; set;
    }
}

public class HourlyTimelineSlotDto
{
    public const int WeekColumnCount = 7;

    public string HourText { get; set; } = string.Empty;
    public int HourValue
    {
        get; set;
    }

    /// <summary>Часот со AM/PM, како во дизајнот: „09:00 AM".</summary>
    public string HourDisplay =>
        DateTime.Today.AddHours(HourValue).ToString("hh:mm tt", CultureInfo.InvariantCulture);

    /// <summary>
    /// Always exactly 7 columns (Mon..Sun). The week grid binds DayColumns[0..6] by index,
    /// so a shorter list silently blanks out every column past the end — the list is padded
    /// on construction instead of being sized to whatever the caller happened to have.
    /// </summary>
    public List<ObservableCollection<CalendarEventDto>> DayColumns { get; } =
        Enumerable.Range(0, WeekColumnCount)
            .Select(_ => new ObservableCollection<CalendarEventDto>())
            .ToList();

    /// <summary>
    /// Every event in this hour across all columns — used by the single-day timeline
    /// on CalendarDashboardPage, which has no per-weekday columns.
    /// </summary>
    public ObservableCollection<CalendarEventDto> SlotEvents { get; } = new();

    public void SetColumn(int index, IEnumerable<CalendarEventDto> events)
    {
        if(index<0||index>=WeekColumnCount)
            return;

        var column = DayColumns[index];
        column.Clear();

        foreach(var e in events)
        {
            column.Add(e);
            SlotEvents.Add(e);
        }
    }
}

public partial class CalendarDayDto : ObservableObject
{
    public DateTime Date
    {
        get; set;
    }
    public int DayNumber => Date.Day;
    public bool IsEmptySlot
    {
        get; set;
    }
    public bool IsCurrentMonth
    {
        get; set;
    }
    public bool IsToday => Date.Date==DateTime.Today;

    /// <summary>За заглавјето: сивиот број се крие кај денешниот ден, без конвертор.</summary>
    public bool IsNotToday => !IsToday;
    public int EventCount => Events.Count;
    public List<CalendarEventDto> Events { get; set; } = new();
    public List<CalendarEventDto> VisibleEvents => Events.Take(3).ToList();
    public bool HasHiddenEvents => Events.Count>3;
    public int HiddenEventsCount => Math.Max(Events.Count-3, 0);

    public string DayNameShort =>
        Date.ToString("ddd", new CultureInfo("mk-MK")).ToUpperInvariant();

    public string EncountersCountText =>
        EventCount switch
        {
            0 => "нема прегледи",
            1 => "1 преглед",
            _ => $"{EventCount} прегледи"
        };

    public int WeekIndex
    {
        get; set;
    }

    [ObservableProperty] private bool isWeekHighlighted;

    [ObservableProperty] private bool isSelected;
}

public class CalendarEventDto
{
    public Guid Id
    {
        get; set;
    }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DateTime ScheduledTime
    {
        get; set;
    }
    public TimeSpan Duration
    {
        get; set;
    }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uf073";
    public Color BackgroundColor { get; set; } = Colors.LightGreen;
    public Color TextColor { get; set; } = Colors.DarkGreen;

    /// <summary>\u201e10:00-10:30" \u2014 \u0432\u0440\u0435\u043c\u0435\u043d\u0441\u043a\u0438 \u043e\u043f\u0441\u0435\u0433 \u043a\u0430\u043a\u043e \u0432\u043e \u0434\u0438\u0437\u0430\u0458\u043d\u043e\u0442.</summary>
    public string TimeRangeText =>
        $"{ScheduledTime:HH:mm}-{ScheduledTime.Add(Duration):HH:mm}";

    /// <summary>\u041d\u0430\u0441\u043b\u043e\u0432\u043e\u0442 \u043d\u0430 \u043a\u0430\u0440\u0442\u0438\u0446\u0430\u0442\u0430: \u043e\u043f\u0441\u0435\u0433 + \u043f\u0430\u0446\u0438\u0435\u043d\u0442 \u0432\u043e \u0435\u0434\u0435\u043d \u0440\u0435\u0434.</summary>
    public string CardTitle => $"{TimeRangeText} {PatientName}".Trim();
}

public class WaitlistItemDto
{
    public Guid Id
    {
        get; set;
    }
    public string PatientName { get; set; } = string.Empty;
    public string PatientInitials { get; set; } = string.Empty;
    public string PatientContact { get; set; } = string.Empty;
    public string TimeRangeText { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
}

public class OverdueCycleDto
{
    public string PatientName { get; set; } = string.Empty;
    public string ScheduleName { get; set; } = string.Empty;
    public string CycleInfo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}