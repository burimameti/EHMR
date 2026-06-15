using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Graphics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Infrastructure.Persistence;
using EHMR.Domain.Interfaces;
using EHMR.Domain.Entities;

namespace EHMR.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAuthStateService _auth;

    public MenuViewModel Menu
    {
        get;
    }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoaded;

    // ГЛОБАЛНИ KPI СВОЈСТВА (Директно генерирани со ObservableProperty за итен UI рефреш)
    [ObservableProperty] private int totalPatients;

    [ObservableProperty] private int activeTherapies;
    [ObservableProperty] private int upcomingAppointmentsCount;
    [ObservableProperty] private int criticalAlerts;
    [ObservableProperty] private int completedCycles;
    [ObservableProperty] private int overdueCycles;
    [ObservableProperty] private int missedCycles;

    [ObservableProperty] private double adherenceProgress;
    [ObservableProperty] private Color adherenceColor = Colors.Gray;
    [ObservableProperty] private string riskLevel = "Loading...";
    [ObservableProperty] private string completionPercentage = "0%";
    [ObservableProperty] private string completionStatus = "Loading...";

    // КОЛЕКЦИИ ВО КОРЕНОТ
    [ObservableProperty] private ObservableCollection<Patient> patients = new();

    public ObservableCollection<DashboardAppointmentItem> Appointments { get; } = new();
    public ObservableCollection<DashboardNotification> Notifications { get; } = new();

    // ТОП-БАР ИНФОРМАЦИИ
    public string CurrentDate => DateTime.Now.ToString("dd MMM yyyy");

    public string UserName => _auth?.UserName??"Корисник";
    public UserRole UserRole => UserRole.Admin;

    public DashboardViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        IAuthStateService auth,
        MenuViewModel menu)
    {
        _dbFactory=dbFactory;
        _auth=auth;
        Menu=menu;
    }

    [RelayCommand]
    public async Task Initialize()
    {
        if(IsBusy) return;
        await LoadGlobalAsync();
        IsLoaded=true;
    }

    [RelayCommand]
    public async Task Refresh() => await LoadGlobalAsync();

    [RelayCommand]
    public async Task LoadGlobalAsync()
    {
        if(IsBusy) return;
        IsBusy=true;

        try
        {
            // 1. КЛУЧЕН ФИКС: Му даваме 100ms на оперативниот систем целосно да го иницијализира UI
            // пред да ја нападнеме базата. Ова го решава "багот" со рестарт.
            await Task.Delay(100);

            await using var db = await _dbFactory.CreateDbContextAsync();

            // 2. ФИКС: Наместо паралелно (кое знае да ја заклучи нишката на старт),
            // ги влечеме податоците брзо и линеарно со AsNoTracking
            var dbPatients = await db.Patients.AsNoTracking().ToListAsync();

            var unreadNotifications = await db.Notifications.AsNoTracking()
                .Where(x => !x.IsRead)
                .OrderByDescending(x => x.CreatedAt)
                .Take(10).ToListAsync();

            var cycles = await db.TherapyCycles.AsNoTracking().ToListAsync();

            var upcomingAppointmentsData = await db.Appointments.AsNoTracking()
                .Include(a => a.Patient).Include(a => a.TherapyCycle)
                .Where(a => a.ScheduledStart>=DateTime.Today&&a.Status==AppointmentStatus.Scheduled)
                .OrderBy(a => a.ScheduledStart)
                .Take(5).ToListAsync();

            // Пресметки во меморија...
            TotalPatients=dbPatients.Count;
            ActiveTherapies=cycles.Count(x => x.Status==TherapyStatus.Active);
            MissedCycles=cycles.Count(x => x.Status==TherapyStatus.Missed);
            CompletedCycles=cycles.Count(x => x.Status==TherapyStatus.Completed);
            CriticalAlerts=unreadNotifications.Count(x => x.Severity==NotificationSeverity.Critical);
            UpcomingAppointmentsCount=upcomingAppointmentsData.Count;

            OverdueCycles=cycles.Count(x => x.Status==TherapyStatus.Missed||
                (x.PlannedEndDate<DateTime.Now&&x.Status!=TherapyStatus.Completed));

            var totalTrackedCycles = cycles.Count(x =>
                x.Status==TherapyStatus.Completed||
                x.Status==TherapyStatus.Active||
                           x.Status==TherapyStatus.Scheduled||
                x.Status==TherapyStatus.Missed||
                x.Status==TherapyStatus.Suspended);

            var adherenceRate = totalTrackedCycles==0 ? 0 : ((double)CompletedCycles/totalTrackedCycles)*100;

            CompletionStatus=adherenceRate switch
            {
                >=90 => "Excellent",
                >=75 => "Good",
                >=60 => "Moderate",
                >=40 => "Critical",
                _ => "High Risk"
            };

            AdherenceProgress=adherenceRate/100d;
            CompletionPercentage=$"{Math.Round(adherenceRate)}%";

            AdherenceColor=adherenceRate switch
            {
                >=90 => Colors.LimeGreen,
                >=75 => Colors.DodgerBlue,
                >=60 => Colors.Orange,
                _ => Colors.Red
            };

            // 3. ФИКС: Колекциите ги полниме експлицитно на Главната UI нишка
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Patients=new ObservableCollection<Patient>(dbPatients);

                Appointments.Clear();
                foreach(var a in upcomingAppointmentsData)
                {
                    Appointments.Add(new DashboardAppointmentItem
                    {
                        PatientName=$"{a.Patient.FirstName} {a.Patient.LastName}",
                        Diagnosis=a.Patient.PrimaryDiagnosis,
                        ScheduledStart=a.ScheduledStart,
                        Time=a.ScheduledStart.ToString("HH:mm"),
                        RelativeDay=a.ScheduledStart.Date==DateTime.Today ? "Денес" : a.ScheduledStart.ToString("dd.MM"),
                        StatusColor=a.ScheduledStart<DateTime.Now.AddHours(1) ? "#DC2626" : "#2563EB"
                    });
                }

                Notifications.Clear();
                foreach(var n in unreadNotifications)
                {
                    Notifications.Add(new DashboardNotification
                    {
                        Title=n.Title,
                        Time=GetRelativeTime(n.CreatedAt),
                        Level=MapSeverity(n.Severity.ToString())
                    });
                }
            });
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    private NotificationLevel MapSeverity(string dbSeverity)
    {
        return dbSeverity?.ToLower() switch
        {
            "info" => NotificationLevel.Info,
            "warning" => NotificationLevel.Warning,
            "critical" => NotificationLevel.Error,
            "error" => NotificationLevel.Error,
            _ => NotificationLevel.Info
        };
    }

    private string GetRelativeTime(DateTime createdAt)
    {
        var span = DateTime.Now-createdAt;
        if(span.TotalMinutes<1) return "сега";
        if(span.TotalHours<1) return $"пред {(int)span.TotalMinutes} мин.";
        if(span.TotalDays<1) return $"пред {(int)span.TotalHours} часа";
        if(span.TotalDays<7) return $"пред {(int)span.TotalDays} дена";
        return createdAt.ToString("dd.MM.yyyy");
    }
}