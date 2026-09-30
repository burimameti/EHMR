using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities;
using EHMR.ViewModels.Dashboards.Models;
using System.Collections.ObjectModel;



namespace EHMR.ViewModels.Dashboard.Models;

public partial class DashboardState : ObservableObject
{
    // =====================================================
    // HEADER
    // =====================================================
    [ObservableProperty] private string headerTitle = string.Empty;
    [ObservableProperty] private string headerSubtitle = string.Empty;

    // =====================================================
    // KPI — patients / cycles
    // =====================================================
    [ObservableProperty] private int totalPatients;
    [ObservableProperty] private int activeTherapies;
    [ObservableProperty] private int overdueCycles;
    [ObservableProperty] private int missedCycles;
    [ObservableProperty] private int completedCycles;
    [ObservableProperty] private int pendingTasks;
    [ObservableProperty] private int unreadNotifications;

    // =====================================================
    // KPI — today's encounters
    // =====================================================
    [ObservableProperty] private int completedToday;
    [ObservableProperty] private int waitingToday;
    [ObservableProperty] private int upcomingAppointmentsCount;

    // =====================================================
    // ALERTS
    // =====================================================
    [ObservableProperty] private int criticalAlerts;
    [ObservableProperty] private string criticalAlertsSummaryText = string.Empty;
    [ObservableProperty] private ObservableCollection<DashboardAlertSummaryItem> alertSummaries = new();

    public bool HasCriticalAlerts => CriticalAlerts>0;
    public bool HasAnyAlerts => AlertSummaries.Any(x => x.Count>0);

    partial void OnCriticalAlertsChanged(int value) => OnPropertyChanged(nameof(HasCriticalAlerts));
    partial void OnAlertSummariesChanged(ObservableCollection<DashboardAlertSummaryItem> value) => OnPropertyChanged(nameof(HasAnyAlerts));

    // =====================================================
    // CLINICAL PERFORMANCE
    // =====================================================
    [ObservableProperty] private double adherenceRate;
    [ObservableProperty] private double adherenceProgress;
    [ObservableProperty] private Color adherenceColor = Colors.Gray;
    [ObservableProperty] private string riskLevel = string.Empty;
    [ObservableProperty] private string completionPercentage = "0%";
    [ObservableProperty] private string completionStatus = string.Empty;

    // =====================================================
    // COLLECTIONS
    // =====================================================
    [ObservableProperty] private ObservableCollection<DashboardAppointmentItem> appointments = new();
    [ObservableProperty] private ObservableCollection<Encounter> encounters = new();
    [ObservableProperty] private ObservableCollection<DashboardEncounterItem> dailyEncounters = new();
    [ObservableProperty] private ObservableCollection<DashboardDayItem> dayStrip = new();
    [ObservableProperty] private ObservableCollection<DashboardNotification> notifications = new();
    [ObservableProperty] private ObservableCollection<AnalyticsDashboard> analytics = new();

    // =====================================================
    // CHARTS
    // =====================================================
    [ObservableProperty] private ObservableCollection<AnalyticsChart> adherenceChart = new();
    [ObservableProperty] private ObservableCollection<AnalyticsChart> therapyChart = new();
    [ObservableProperty] private ObservableCollection<AnalyticsChart> alertChart = new();

    // =====================================================
    // ADVANCED KPI
    // =====================================================
    [ObservableProperty] private AnalyticsKpi analyticsKpis = new();
    [ObservableProperty] private DashboardChartState chart = new();
}