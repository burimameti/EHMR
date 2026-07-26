using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Globalization;

namespace EHMR.ViewModels.Calendar;

public partial class CalendarDashboardViewModel : BaseViewModel<Encounter>, IQueryAttributable
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

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
    [ObservableProperty] private string _noShowPercentageText = "0%";

    [ObservableProperty] private CalendarDayDto? _selectedCalendarDay;

    [ObservableProperty] private bool _isMonthViewActive = true;
    [ObservableProperty] private bool _isDayViewActive;
    [ObservableProperty] private bool _isStatsViewActive = true;

    private DateTime _startOfMonth;
    private DateTime _endOfMonth;

    [ObservableProperty]
    private CalendarMode currentMode;

    [ObservableProperty] private ObservableCollection<HourlyTimelineSlotDto> _hourlyTimelineSlots = new();
    [ObservableProperty] private ObservableCollection<CalendarDayDto> _weekDays = new();

    public ObservableCollection<CalendarDayDto> CalendarDays { get; } = new();

    public bool CanDeleteEncounters => CurrentUserRole=="Admin"||CurrentUserRole=="Doctor";
    public string CurrentUserRole { get; set; } = "Doctor";

    // ═══════════════════════════════════════════ STATUS FILTER ═══════════════════════════════════════════

    public ObservableCollection<string> Statuses
    {
        get;
    } = new(new[]
    {
        "Сите", "Scheduled", "CheckedIn", "InProgress", "Completed", "Cancelled", "NoShow"
    });

    [ObservableProperty] private string _selectedStatus = "Сите";

    // ═══════════════════════════════════════════ WAITLIST PANEL ═══════════════════════════════════════════

    [ObservableProperty] private ObservableCollection<WaitlistItemDto> _pendingEncounters = new();
    [ObservableProperty] private ObservableCollection<WaitlistItemDto> _inProgressEncounters = new();
    [ObservableProperty] private ObservableCollection<WaitlistItemDto> _scheduledEncounters = new();

    public int PendingCount => PendingEncounters.Count;
    public int InProgressCount => InProgressEncounters.Count;
    public int ScheduledCount => ScheduledEncounters.Count;

    [ObservableProperty] private bool _isPendingTabActive = true;
    [ObservableProperty] private bool _isInProgressTabActive;
    [ObservableProperty] private bool _isScheduledTabActive;

    public CalendarDashboardViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService dialog,
        IMenuService menu,
        IAuthorizationService authorization,
        ISelectedItemService<Encounter> encounterSelect)
        : base(navigationService, dialog, menu, authorization, encounterSelect)
    {
        _dbFactory=dbFactory;

        _currentDate=DateTime.Today;
        _currentWeekStart=StartOfWeek(_currentDate);

        EvaluatePermissions();
        BuildSparkButtons();
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        int diff = (7+(date.DayOfWeek-DayOfWeek.Monday))%7;
        return date.AddDays(-diff).Date;
    }

    // ═══════════════════════════════════════════ COMMANDS ═══════════════════════════════════════════

    [RelayCommand]
    private void SelectCalendarDay(CalendarDayDto? day)
    {
        if(day==null||day.IsEmptySlot)
            return;

        SelectedCalendarDay=day;
        IsMonthViewActive=false;
        IsDayViewActive=true;
        IsStatsViewActive=false;

        BuildHourlyTimeline();
    }

    [RelayCommand]
    private void CloseDayView()
    {
        IsDayViewActive=false;
        IsMonthViewActive=true;
        IsStatsViewActive=true;

        SelectedCalendarDay=null;

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
        if(Enum.TryParse<CalendarMode>(mode, true, out var parsed))
            CurrentMode=parsed;

        await LoadDashboardDataAsync();
    }

    [RelayCommand]
    private async Task GoToTodayAsync()
    {
        _currentDate=DateTime.Today;
        _currentWeekStart=StartOfWeek(_currentDate);

        await LoadDashboardDataAsync();

        var todayDay = CalendarDays.FirstOrDefault(x => x.IsToday);
        SelectCalendarDay(todayDay);
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
    private async Task NextWeekAsync()
    {
        _currentWeekStart=_currentWeekStart.AddDays(7);
        _currentDate=_currentWeekStart;
        await LoadDashboardDataAsync();
    }

    [RelayCommand]
    private async Task PreviousWeekAsync()
    {
        _currentWeekStart=_currentWeekStart.AddDays(-7);
        _currentDate=_currentWeekStart;
        await LoadDashboardDataAsync();
    }

    [RelayCommand]
    private async Task CreateNewEncounterForSelectedDayAsync()
    {
        var targetDate =
            SelectedCalendarDay!=null
                ? SelectedCalendarDay.Date
                : DateTime.Today;

        SelectedItemService.SelectedItem=null;
        await NavigationService.GoToAsync($"{AppRoutes.Encounters.Create}?date={targetDate.Ticks}");
    }

    [RelayCommand]
    private async Task CreateNewEncounterForSpecificHourAsync(HourlyTimelineSlotDto slot)
    {
        if(SelectedCalendarDay==null||slot==null)
            return;

        var targetDateTime =
            SelectedCalendarDay.Date.Date
                .AddHours(slot.HourValue);

        SelectedItemService.SelectedItem=null;
        await NavigationService.GoToAsync($"{AppRoutes.Encounters.Create}?date={targetDateTime.Ticks}");
    }

    [RelayCommand]
    public async Task ProcessEncounterSelectionAsync(Guid encounterId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var encounter = await db.Encounters
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id==encounterId);

        if(encounter==null)
            return;

        SelectedItemService.SelectedItem=encounter;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Detail);
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
    private async Task ApplyCalendarSearchAsync() => await LoadDashboardDataAsync();

    [RelayCommand]
    private void SelectWaitlistTab(string tab)
    {
        IsPendingTabActive=tab=="Pending";
        IsInProgressTabActive=tab=="InProgress";
        IsScheduledTabActive=tab=="Scheduled";
    }

    async partial void OnSelectedStatusChanged(string value) => await LoadDashboardDataAsync();

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

        UpdateStatistics(events);

        var calendarDays = BuildCalendarDays(events);
        var weekDays = BuildWeekDays(events);
        var hourlySlots = BuildWeeklyHourlyTimeline(weekDays);
        var waitlistBuckets = BuildWaitlistBuckets(events);

        // Awaited (not fire-and-forget) so any exception here is caught by
        // ExecuteSafeAsync and the busy state gets reset correctly.
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            RefreshCalendar(calendarDays);
            BuildWeekTabs(calendarDays);

            WeekDays.Clear();
            foreach(var d in weekDays) WeekDays.Add(d);

            HourlyTimelineSlots.Clear();
            foreach(var s in hourlySlots) HourlyTimelineSlots.Add(s);

            PendingEncounters.Clear();
            foreach(var w in waitlistBuckets.Pending) PendingEncounters.Add(w);

            InProgressEncounters.Clear();
            foreach(var w in waitlistBuckets.InProgress) InProgressEncounters.Add(w);

            ScheduledEncounters.Clear();
            foreach(var w in waitlistBuckets.Scheduled) ScheduledEncounters.Add(w);

            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(InProgressCount));
            OnPropertyChanged(nameof(ScheduledCount));
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

        CurrentMonthYearText=
            _startOfMonth
                .ToString("MMMM yyyy")
                .ToUpperInvariant();

        IsViewingCurrentMonth=
            _currentDate.Year==DateTime.Today.Year&&
            _currentDate.Month==DateTime.Today.Month;
    }

    private void UpdateStatistics(List<CalendarSourceItem> encounters)
    {
        var today = DateTime.Today;

        // Statistics stay scoped to the visible month, matching CurrentMonthYearText.
        var monthScoped = encounters
            .Where(x => x.EffectiveDate.Date>=_startOfMonth&&x.EffectiveDate.Date<=_endOfMonth)
            .ToList();

        MonthlyEncountersCount=monthScoped.Count;

        TodaysEncountersCount=monthScoped.Count(x =>
            x.EffectiveDate.Date==today);

        UpcomingEncountersCount=monthScoped.Count(x =>
            x.EffectiveDate.Date>today);

        int total = monthScoped.Count;
        int completed = monthScoped.Count(x => x.StatusText=="Completed");
        int noShow = monthScoped.Count(x => x.StatusText=="NoShow");

        CompletedPercentageText=total==0 ? "0%" : $"{completed*100/total}%";
        NoShowPercentageText=total==0 ? "0%" : $"{noShow*100/total}%";
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

    private static List<HourlyTimelineSlotDto> BuildWeeklyHourlyTimeline(List<CalendarDayDto> weekDays)
    {
        var slots = new List<HourlyTimelineSlotDto>();

        for(int hour = 0; hour<24; hour++)
        {
            var slot = new HourlyTimelineSlotDto
            {
                HourText=$"{hour:D2}:00",
                HourValue=hour
            };

            foreach(var day in weekDays)
            {
                var eventsInHour = day.Events
                    .Where(x => x.ScheduledTime.Hour==hour)
                    .OrderBy(x => x.ScheduledTime)
                    .ToList();

                slot.DayColumns.Add(new ObservableCollection<CalendarEventDto>(eventsInHour));
            }

            slots.Add(slot);
        }

        return slots;
    }

    private (List<WaitlistItemDto> Pending, List<WaitlistItemDto> InProgress, List<WaitlistItemDto> Scheduled)
        BuildWaitlistBuckets(List<CalendarSourceItem> allEvents)
    {
        var today = DateTime.Today;
        var todaysItems = allEvents.Where(x => x.EffectiveDate.Date==today).ToList();

        var pending = todaysItems
            .Where(x => x.StatusText is "Scheduled" or "CheckedIn")
            .OrderBy(x => x.EffectiveDate)
            .Select(ToWaitlistDto)
            .ToList();

        var inProgress = todaysItems
            .Where(x => x.StatusText=="InProgress")
            .OrderBy(x => x.EffectiveDate)
            .Select(ToWaitlistDto)
            .ToList();

        var scheduled = todaysItems
            .Where(x => x.StatusText is "Completed" or "Cancelled" or "NoShow")
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

        SelectedCalendarDay=CalendarDays.FirstOrDefault(x => x.Date.Date==previousSelected.Value.Date);

        if(SelectedCalendarDay!=null)
            BuildHourlyTimeline();
    }

    private static (Color Background, Color Text) GetStatusColors(string status) => status switch
    {
        "Scheduled" => (Color.FromArgb("#DBEAFE"), Color.FromArgb("#1D4ED8")),
        "CheckedIn" => (Color.FromArgb("#EDE9FE"), Color.FromArgb("#6D28D9")),
        "InProgress" => (Color.FromArgb("#FEF3C7"), Color.FromArgb("#B45309")),
        "Completed" => (Color.FromArgb("#DCFCE7"), Color.FromArgb("#15803D")),
        "Cancelled" => (Color.FromArgb("#FEE2E2"), Color.FromArgb("#B91C1C")),
        "NoShow" => (Color.FromArgb("#F3F4F6"), Color.FromArgb("#6B7280")),
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
    {
        var appointmentsQuery = db.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Include(x => x.Encounter)
            .Where(x => x.ScheduledStart>=rangeStart&&x.ScheduledStart<=rangeEnd)
            .AsQueryable();

        var encountersQuery = db.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Where(x => x.AppointmentId==null&&
                (x.ScheduledStart??x.EncounterDate)>=rangeStart&&
                (x.ScheduledStart??x.EncounterDate)<=rangeEnd)
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

        if(!string.IsNullOrWhiteSpace(SelectedStatus)&&SelectedStatus!="Сите")
        {
            appointmentsQuery=appointmentsQuery.Where(x => x.Status.ToString()==SelectedStatus);
            encountersQuery=encountersQuery.Where(x => x.Status.ToString()==SelectedStatus);
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
                EffectiveDate=enc.ScheduledStart??enc.EncounterDate,
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
    /// Builds the 24-hour timeline for the single selected day (day-view flyout).
    /// DayColumns will contain exactly one column (index 0) representing that day.
    /// </summary>
    public void BuildHourlyTimeline()
    {
        HourlyTimelineSlots.Clear();

        if(SelectedCalendarDay==null)
            return;

        var dayEvents = SelectedCalendarDay.Events;

        for(int hour = 0; hour<24; hour++)
        {
            var eventsInHour = dayEvents
                .Where(x => x.ScheduledTime.Hour==hour)
                .OrderBy(x => x.ScheduledTime)
                .ToList();

            var slot = new HourlyTimelineSlotDto
            {
                HourText=$"{hour:D2}:00",
                HourValue=hour
            };
            slot.DayColumns.Add(new ObservableCollection<CalendarEventDto>(eventsInHour));

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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
    }
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
    public string HourText { get; set; } = string.Empty;
    public int HourValue
    {
        get; set;
    }

    /// <summary>
    /// Week grid: 7 columns (Mon..Sun). Day view: 1 column for the selected day.
    /// </summary>
    public List<ObservableCollection<CalendarEventDto>> DayColumns { get; set; } = new();
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
    public int EventCount => Events.Count;
    public List<CalendarEventDto> Events { get; set; } = new();
    public List<CalendarEventDto> VisibleEvents => Events.Take(3).ToList();
    public bool HasHiddenEvents => Events.Count>3;
    public int HiddenEventsCount => Math.Max(Events.Count-3, 0);

    public string DayNameShort =>
        Date.ToString("ddd", new CultureInfo("mk-MK")).ToUpperInvariant();

    public string EncountersCountText =>
        EventCount==0 ? "нема прегледи" : $"{EventCount} прегледи";

    public int WeekIndex
    {
        get; set;
    }

    [ObservableProperty] private bool isWeekHighlighted;
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