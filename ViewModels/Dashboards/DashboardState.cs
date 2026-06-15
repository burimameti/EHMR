using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class DashboardState : ObservableObject
{
    // =====================================================
    // HEADER
    // =====================================================

    [ObservableProperty]
    private string headerTitle = string.Empty;

    [ObservableProperty]
    private string headerSubtitle = string.Empty;

    // =====================================================
    // KPI
    // =====================================================

    [ObservableProperty]
    private int totalPatients;

    [ObservableProperty]
    private int activeTherapies;

    [ObservableProperty]
    private int overdueCycles;

    [ObservableProperty]
    private int missedCycles;

    [ObservableProperty]
    private int completedCycles;

    [ObservableProperty]
    private int criticalAlerts;

    [ObservableProperty]
    private int upcomingAppointments;

    [ObservableProperty]
    private int pendingTasks;

    [ObservableProperty]
    private int unreadNotifications;

    // =====================================================
    // CLINICAL PERFORMANCE
    // =====================================================

    [ObservableProperty]
    private double adherenceRate;

    [ObservableProperty]
    private double adherenceProgress;

    [ObservableProperty]
    private Color adherenceColor = Colors.Gray;

    [ObservableProperty]
    private string riskLevel = string.Empty;

    [ObservableProperty]
    private string completionPercentage = "0%";

    [ObservableProperty]
    private string completionStatus = string.Empty;

    //private partial void OnAdherenceRateChanged(double value)
    //{
    //    OnPropertyChanged(nameof(AdherenceProgress));
    //    OnPropertyChanged(nameof(AdherenceColor));
    //    OnPropertyChanged(nameof(RiskLevel));
    //}

    // =====================================================
    // COLLECTIONS
    // =====================================================

    [ObservableProperty]
    private ObservableCollection<DashboardAppointmentItem>
        appointments = new();

    [ObservableProperty]
    private ObservableCollection<DashboardNotification>
        notifications = new();

    [ObservableProperty]
    private ObservableCollection<AnalyticsDashboard>
        analytics = new();

    // =====================================================
    // CHARTS
    // =====================================================

    [ObservableProperty]
    private ObservableCollection<AnalyticsChart>
        adherenceChart = new();

    [ObservableProperty]
    private ObservableCollection<AnalyticsChart>
        therapyChart = new();

    [ObservableProperty]
    private ObservableCollection<AnalyticsChart>
        alertChart = new();

    // =====================================================
    // ADVANCED KPI
    // =====================================================

    [ObservableProperty]
    private AnalyticsKpi analyticsKpis = new();

    [ObservableProperty]
    private DashboardChartState chart = new();
}