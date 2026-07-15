using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class CalendarDashboardViewModel : BaseViewModel<Encounter>, IQueryAttributable
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

    private DateTime _currentDate;

    protected override string ModuleName => "Calendar";

    [ObservableProperty] private string _currentMonthYearText = string.Empty;
    [ObservableProperty] private bool _isViewingCurrentMonth = true;

    [ObservableProperty] private int _monthlyEncountersCount;
    [ObservableProperty] private int _todaysEncountersCount;
    [ObservableProperty] private int _upcomingEncountersCount;

    [ObservableProperty] private CalendarDayDto? _selectedCalendarDay;

    [ObservableProperty] private bool _isMonthViewActive = true;
    [ObservableProperty] private bool _isDayViewActive;
    [ObservableProperty] private bool _isStatsViewActive = true;

    private DateTime _startOfMonth;
    private DateTime _endOfMonth;
    [ObservableProperty]
    private CalendarMode currentMode;

    [ObservableProperty] private ObservableCollection<HourlyTimelineSlotDto> _hourlyTimelineSlots = new();

    public ObservableCollection<CalendarDayDto> CalendarDays { get; } = new();

    // Buttons доаѓа од BaseViewModel<T> — не се redeclara тука.

    public bool CanDeleteEncounters => CurrentUserRole=="Admin"||CurrentUserRole=="Doctor";
    public string CurrentUserRole { get; set; } = "Doctor";

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
        EvaluatePermissions();
        BuildSparkButtons();
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
    private async Task GoToTodayAsync()
    {
        _currentDate=DateTime.Today;
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

        await using var db = await _dbFactory.CreateDbContextAsync();

        var encounters = await LoadMonthEncountersAsync(db);

        UpdateStatistics(encounters);

        var calendarDays = BuildCalendarDays(encounters);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            RefreshCalendar(calendarDays);
            BuildWeekTabs(calendarDays);
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

    private void UpdateStatistics(List<Encounter> encounters)
    {
        var today = DateTime.Today;

        MonthlyEncountersCount=encounters.Count;

        TodaysEncountersCount=encounters.Count(x =>
            (x.ScheduledStart??x.EncounterDate).Date==today);

        UpcomingEncountersCount=encounters.Count(x =>
            (x.ScheduledStart??x.EncounterDate).Date>today);
    }

    private List<CalendarDayDto> BuildCalendarDays(List<Encounter> encounters)
    {
        var grouped = encounters
            .GroupBy(x => (x.ScheduledStart??x.EncounterDate).Date)
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

            grouped.TryGetValue(date.Date, out var dayEncounters);

            var events = dayEncounters?
                .OrderBy(x => x.ScheduledStart??x.EncounterDate)
                .Select(CreateCalendarEvent)
                .ToList()
                ??new List<CalendarEventDto>();

            result.Add(new CalendarDayDto
            {
                Date=date,
                IsCurrentMonth=true,
                IsEmptySlot=false,
                Events=events
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

    private CalendarEventDto CreateCalendarEvent(Encounter encounter)
    {
        var (bg, text)=GetStatusColors(encounter.Status.ToString());

        return new CalendarEventDto
        {
            Id=encounter.Id,
            Title=encounter.Patient?.FullName??"",
            Duration=TimeSpan.FromMinutes(30),
            Subtitle=encounter.Doctor?.FullName??"",
            PatientName=encounter.Patient?.FullName??"",
            DoctorName=encounter.Doctor?.FullName??"",
            ScheduledTime=encounter.ScheduledStart??encounter.EncounterDate,
            Status=encounter.Status.ToString(),
            EventType="Encounter",
            IconGlyph="\uf073",
            BackgroundColor=bg,
            TextColor=text
        };
    }

    private async Task<List<Encounter>> LoadMonthEncountersAsync(DesktopTherapyDbContext db)
    {
        var query = db.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
                .ThenInclude(x => x.User)
            .Where(x =>
                (x.ScheduledStart??x.EncounterDate)>=_startOfMonth&&
                (x.ScheduledStart??x.EncounterDate)<=_endOfMonth)
            .AsQueryable();

        if(!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query=query.Where(x =>
                (x.Patient!=null&&(x.Patient.FirstName+" "+x.Patient.LastName).Contains(term))||
                (x.EncounterNumber??"").Contains(term));
        }

        return await query.ToListAsync();
    }

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

            HourlyTimelineSlots.Add(new HourlyTimelineSlotDto
            {
                HourText=$"{hour:D2}:00",
                HourValue=hour,
                SlotEvents=new ObservableCollection<CalendarEventDto>(eventsInHour)
            });
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
// (непроменето)
// ═══════════════════════════════════════════ DTOs ═══════════════════════════════════════════

public class HourlyTimelineSlotDto
{
    public string HourText { get; set; } = string.Empty;
    public int HourValue
    {
        get; set;
    }
    public ObservableCollection<CalendarEventDto> SlotEvents { get; set; } = new();
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

public class OverdueCycleDto
{
    public string PatientName { get; set; } = string.Empty;
    public string ScheduleName { get; set; } = string.Empty;
    public string CycleInfo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}