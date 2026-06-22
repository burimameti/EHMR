using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Domain.Entities.Rbac;

namespace EHMR.ViewModels;

public partial class CalendarDashboardViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly INavigationService _navigationService;
    private readonly ISelectedItemService<Appointment> _appointmentSelectionService;
    private readonly ISelectedItemService<TherapyCycle> _cycleSelectionService;

    private DateTime _currentDate;

    [ObservableProperty] private string _currentMonthYearText = string.Empty;
    [ObservableProperty] private int _criticalCyclesCount;
    [ObservableProperty] private string _adherenceRateText = "0% Извршеност";
    [ObservableProperty] private double _adherenceRateValue;
    [ObservableProperty] private int _activePlansCount;
    [ObservableProperty] private int _todaysAppointmentsCount;

    [ObservableProperty] private CalendarDayDto? _selectedCalendarDay;
    [ObservableProperty] private CalendarEventDto? _selectedCalendarEvent;

    // View States
    [ObservableProperty] private bool _isMonthViewActive = true;

    [ObservableProperty] private bool _isDayViewActive;
    [ObservableProperty] private bool _isEventViewActive;
    [ObservableProperty] private bool _isStatsViewActive = true;

    [ObservableProperty] private ObservableCollection<HourlyTimelineSlotDto> _hourlyTimelineSlots = new();
    public ObservableCollection<CalendarDayDto> CalendarDays { get; set; } = new();
    public ObservableCollection<OverdueCycleDto> CriticalCycles { get; set; } = new();

    public bool CanDeleteAppointments => CurrentUserRole=="Admin"||CurrentUserRole=="Doctor";
    public string CurrentUserRole { get; set; } = "Doctor";

    public CalendarDashboardViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        ISelectedItemService<Appointment> appointmentSelectionService,
        ISelectedItemService<TherapyCycle> cycleSelectionService)
    {
        _dbFactory=dbFactory;
        _navigationService=navigationService;
        _appointmentSelectionService=appointmentSelectionService;
        _cycleSelectionService=cycleSelectionService;

        _currentDate=DateTime.Today;
    }

    [RelayCommand]
    private void SwitchToMonthView()
    {
        IsMonthViewActive=true;
        IsDayViewActive=false;
        IsStatsViewActive=true;
    }

    [RelayCommand]
    private void SwitchToDayView()
    {
        if(SelectedCalendarDay==null)
        {
            var todayDto = CalendarDays.FirstOrDefault(d => d.IsToday);
            if(todayDto!=null)
            {
                SelectedCalendarDay=todayDto;
            }
        }

        IsMonthViewActive=false;
        IsDayViewActive=true;
        IsStatsViewActive=false;

        if(SelectedCalendarDay!=null)
        {
            BuildHourlyTimeline(SelectedCalendarDay);
        }
    }

    [RelayCommand]
    private void CloseContextPanel()
    {
        IsDayViewActive=false;
        IsEventViewActive=false;
        IsStatsViewActive=true;
        SelectedCalendarDay=null;
        SelectedCalendarEvent=null;
    }

    // Нов релеј команда со која овозможуваме затворање од горното мени во XAML
    [RelayCommand]
    private void CloseDayView() => CloseContextPanel();

    public void BuildHourlyTimeline(CalendarDayDto selectedDay)
    {
        if(selectedDay==null) return;

        var tempSlots = new List<HourlyTimelineSlotDto>();

        // Го менуваме опсегот за да го опфати целиот ден (0-23)
        for(int hour = 0; hour<=23; hour++)
        {
            var eventsInHour = selectedDay.Events
                .Where(e => e.ScheduledTime.Hour==hour)
                .ToList();

            var slot = new HourlyTimelineSlotDto
            {
                HourText=$"{hour:D2}:00",
                HourValue=hour,
                SlotEvents=new ObservableCollection<CalendarEventDto>(eventsInHour)
            };

            tempSlots.Add(slot);
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            HourlyTimelineSlots.Clear();
            foreach(var slot in tempSlots)
            {
                HourlyTimelineSlots.Add(slot);
            }
        });
    }

    [RelayCommand]
    private async Task CreateNewAppointmentForSelectedDayAsync()
    {
        if(SelectedCalendarDay==null) return;
        var targetDate = new DateTime(_currentDate.Year, _currentDate.Month, SelectedCalendarDay.DayNumber);

        _appointmentSelectionService.SelectedItem=null;
        await _navigationService.GoToAsync($"appointmentsdetail?date={targetDate.Ticks}");
    }

    // Специјална команда за креирање термин со точен изгласан час од Timeline-от
    [RelayCommand]
    private async Task CreateNewAppointmentForSpecificHourAsync(HourlyTimelineSlotDto slot)
    {
        if(SelectedCalendarDay==null||slot==null||slot.HourValue==-1) return;

        var targetDateTime = new DateTime(_currentDate.Year, _currentDate.Month, SelectedCalendarDay.DayNumber, slot.HourValue, 0, 0);
        _appointmentSelectionService.SelectedItem=null;
        await _navigationService.GoToAsync($"{AppRoutes.Appointments.Detail}?date={targetDateTime.Ticks}");
    }

    [RelayCommand]
    private async Task DeleteEventAsync(CalendarEventDto eventDto)
    {
        if(!CanDeleteAppointments||eventDto==null) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        if(eventDto.EventType=="Appointment")
        {
            var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id==eventDto.Id);
            if(appointment!=null)
            {
                db.Appointments.Remove(appointment);
                await db.SaveChangesAsync();
            }
        }

        await LoadDashboardDataAsync();
        CloseContextPanel();
    }

    [RelayCommand]
    private async Task NextMonthAsync() => await ChangeMonth(1);

    [RelayCommand]
    private async Task PreviousMonthAsync() => await ChangeMonth(-1);

    [RelayCommand]
    private async Task CurrentMonthAsync()
    {
        _currentDate=DateTime.Today;
        await LoadDashboardDataAsync();
    }

    private async Task ChangeMonth(int monthsToId)
    {
        _currentDate=_currentDate.AddMonths(monthsToId);
        await LoadDashboardDataAsync();
    }

    // ОВИЕ СЕ КОМАНДИТЕ ШТО ТИ ГИ БАРАШЕ CODE-BEHIND ФАЈЛОТ СЕГА СЕ ТУКА ТОЧНО ДЕФИНИРАНИ
    [RelayCommand]
    public async Task ProcessCycleSelectionAsync(Guid cycleId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var cycle = await db.TherapyCycles
            .Include(c => c.Appointments)
            .FirstOrDefaultAsync(c => c.Id==cycleId);

        if(cycle!=null)
        {
            _cycleSelectionService.SelectedItem=cycle;
            await _navigationService.GoToAsync(AppRoutes.Therapy.Detail);
        }
    }

    [RelayCommand]
    public async Task ProcessAppointmentSelectionAsync(Guid appointmentId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var appointment = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id==appointmentId);

        if(appointment!=null)
        {
            _appointmentSelectionService.SelectedItem=appointment;
            await _navigationService.GoToAsync(AppRoutes.Appointments.Detail);
        }
    }

    [RelayCommand]
    public async Task ProcessGenericEventSelectionAsync(Guid eventId)
    {
        await App.Current.MainPage.DisplayAlert("Настан", $"Избравте општ настан со ID: {eventId}", "ОК");
    }

    [RelayCommand]
    private async Task CreateNewTherapyCycle()
    {
        // Логика за нов тераписки циклус рута
        await _navigationService.GoToAsync(AppRoutes.Therapy.Detail);
    }

    public async Task LoadDashboardDataAsync()
    {
        try
        {
            var monthText = _currentDate.ToString("MMMM yyyy").ToUpper();
            var startOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            await using var db = await _dbFactory.CreateDbContextAsync();

            var cyclesInMonth = await db.TherapyCycles
                .Include(c => c.Appointments)
                    .ThenInclude(s => s.Patient)
                .AsNoTracking()
                .Where(c =>
                    c.Appointments.Any(x => x.ScheduledStart<=endOfMonth&&x.ScheduledEnd>=startOfMonth)
                )
                .ToListAsync();

            var appointmentsInMonth = await db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.ScheduledStart>=startOfMonth&&a.ScheduledStart<=endOfMonth)
                .ToListAsync();

            var todayDate = DateTime.Today;
            int todaysAppointmentsCount = appointmentsInMonth.Count(a => a.ScheduledStart.Date==todayDate);

            int dayOfWeekOffset = ((int)startOfMonth.DayOfWeek==0) ? 6 : (int)startOfMonth.DayOfWeek-1;
            var tempDays = new List<CalendarDayDto>();

            for(int i = 0; i<dayOfWeekOffset; i++)
            {
                tempDays.Add(new CalendarDayDto { DayNumber=0, IsIsEmptySlot=true });
            }

            for(int day = 1; day<=endOfMonth.Day; day++)
            {
                var targetDay = new DateTime(_currentDate.Year, _currentDate.Month, day);
                var dayEvents = new List<CalendarEventDto>();

                var dayCycles = cyclesInMonth
                    .Where(c => c.Appointments.Any(x => x.ScheduledStart.Date==targetDay.Date))
                    .Select(c => new CalendarEventDto
                    {
                        Id=c.Id,
                        Title=$"{c.Patient.FullName} (Ц-#{c.CycleNumber})",
                        ScheduledTime=c.Appointments
                            .Where(x => x.ScheduledStart.Date==targetDay.Date)
                            .OrderBy(x => x.ScheduledEnd)
                            .Select(x => x.ScheduledEnd)
                            .FirstOrDefault(),
                        Status=(c.Status==TherapyStatus.Planned&&c.Appointments.Any(x => x.ScheduledStart.Date<DateTime.Today)) ? "Overdue" : c.Status.ToString(),
                        EventType="Cycle"
                    })
                    .ToList();
                dayEvents.AddRange(dayCycles);

                var dayAppointments = appointmentsInMonth
                    .Where(a => a.ScheduledStart.Date==targetDay.Date)
                    .Select(a => new CalendarEventDto
                    {
                        Id=a.Id,
                        Title=$"{a.Patient.LastName} ({a.ScheduledStart:HH:mm})",
                        ScheduledTime=a.ScheduledStart,
                        Status=a.Status.ToString(),
                        EventType="Appointment"
                    })
                    .ToList();
                dayEvents.AddRange(dayAppointments);

                tempDays.Add(new CalendarDayDto
                {
                    DayNumber=day,
                    IsToday=targetDay.Date==DateTime.Today,
                    IsIsEmptySlot=false,
                    Events=dayEvents,
                    AppointmentCount=dayAppointments.Count,
                    CompletedCount=dayCycles.Count(c => c.Status=="Completed"||c.Status==TherapyStatus.Completed.ToString()),
                    MissedCount=dayCycles.Count(c => c.Status=="Overdue")
                });
            }

            //var activePlans = await db.TreatmentPlans.CountAsync(p => p.Status==TherapyStatus.Active);

            var overdue = db.TherapyCycles
                .Include(p => p.Appointments);
            var over = overdue
                .Where(x =>
                    x.Appointments.Any(a => a.ScheduledStart<DateTime.Today)&&
                    (x.Status==TherapyStatus.Planned||x.Status==TherapyStatus.Active))
                .Take(5)
                .Select(c => new OverdueCycleDto
                {
                    PatientName=c.Patient.LastName,

                    CycleInfo=$"Циклус {c.CycleNumber}",
                    Status="ДОЦНИ"
                }).ToListAsync();

            var totalCycles = cyclesInMonth.Count;
            var completedCycles = cyclesInMonth.Count(c => c.Status==TherapyStatus.Completed);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentMonthYearText=monthText;

                CalendarDays.Clear();
                foreach(var d in tempDays) CalendarDays.Add(d);

                //ActivePlansCount=activePlans;
                TodaysAppointmentsCount=todaysAppointmentsCount;

                CriticalCycles.Clear();
                foreach(var item in over.Result) CriticalCycles.Add(item);
                CriticalCyclesCount=CriticalCycles.Count;

                if(totalCycles>0)
                {
                    AdherenceRateValue=(double)completedCycles/totalCycles;
                    AdherenceRateText=$"{Math.Round(AdherenceRateValue*100)}%";
                }
                else
                {
                    AdherenceRateValue=0;
                    AdherenceRateText="0%";
                }

                // Ако имаме тековно селектиран ден, обнови го неговиот преглед во реално време
                if(SelectedCalendarDay!=null)
                {
                    var updatedDay = CalendarDays.FirstOrDefault(d => d.DayNumber==SelectedCalendarDay.DayNumber&&!d.IsIsEmptySlot);
                    if(updatedDay!=null)
                    {
                        BuildHourlyTimeline(updatedDay);
                    }
                }
            });
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Грешка: {ex.Message}");
        }
    }
}

// ─── DATA TRANSFER OBJECTS ───

public class HourlyTimelineSlotDto
{
    public string HourText
    {
        get; set;
    } // "09:00"

    public int HourValue
    {
        get; set;
    }   // 9

    public ObservableCollection<CalendarEventDto> SlotEvents { get; set; } = new();
}

public class CalendarDayDto
{
    public int DayNumber
    {
        get; set;
    }

    public bool IsToday
    {
        get; set;
    }

    public bool IsIsEmptySlot
    {
        get; set;
    }

    public List<CalendarEventDto> Events { get; set; } = new();
    public string DisplayDayNumber => IsIsEmptySlot ? string.Empty : DayNumber.ToString();

    public int CompletedCount
    {
        get; set;
    }

    public int MissedCount
    {
        get; set;
    }

    public int AppointmentCount
    {
        get; set;
    }
}

public class CalendarEventDto
{
    public Guid Id
    {
        get; set;
    }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime ScheduledTime
    {
        get; set;
    }

    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class OverdueCycleDto
{
    public string PatientName { get; set; } = string.Empty;
    public string ScheduleName { get; set; } = string.Empty;
    public string CycleInfo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}