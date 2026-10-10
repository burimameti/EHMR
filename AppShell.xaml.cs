using EHMR.Backups.Views;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Views;
using EHMR.Views.Admin;
using EHMR.Views.Alerts;
using EHMR.Views.Appointments;
using EHMR.Views.Calendar;
using EHMR.Views.Doctors;
using EHMR.Views.Encounters;
using EHMR.Views.Patients;
using EHMR.Views.Prescription;
using EHMR.Views.Protocols;
using EHMR.Views.Reports;
using EHMR.Views.Mkb10;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using EHMR.Views.Therapies;

namespace EHMR;

public partial class AppShell : Shell
{
    private readonly IAuthStateService _auth;
    private readonly INavigationCoordinator _coordinator;
    private readonly IAuthorizationService _authorization;
    private bool _isNavigating;

    public AppShell(
        IAuthStateService auth, IAuthorizationService authorization,
        INavigationCoordinator coordinator)
    {
        InitializeComponent();

        _auth=auth;
        _coordinator=coordinator;

        _authorization=authorization;
        RegisterRoutes();
        SetupCoordinator();

        _auth.AuthStateChanged+=OnAuthStateChanged;
    }

    // =========================
    // INITIAL NAVIGATION
    // =========================
    public async Task HandleInitialNavigationAsync()
    {
        await HandleAuthChangedAsync();
    }

    // =========================
    // AUTH CHANGED
    // =========================
    private async void OnAuthStateChanged(object? sender, EventArgs e)
    {
        // LoginViewModel navigates to Dashboard after SetUser().
        // Navigating here as well caused two overlapping GoToAsync calls,
        // which could make Shell switch pages unexpectedly.
        if(_auth.IsAuthenticated)
            return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var currentRoute = CurrentState?.Location.OriginalString?.TrimEnd('/');
            if(string.Equals(currentRoute, "//login", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(currentRoute, "login", StringComparison.OrdinalIgnoreCase))
                return;

            if(_isNavigating)
                return;

            await HandleAuthChangedAsync();
        });
    }

    // =========================
    // CENTRAL NAVIGATION PIPE
    // =========================
    private void SetupCoordinator()
    {
        _coordinator.RegisterHandler(async route =>
        {
            if(_isNavigating)
                return;
            if(!_authorization.CanAccessRoute(route))
                return;
            try
            {
                _isNavigating=true;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await GoToAsync(route);

                    FlyoutIsPresented=false;
                });
            }
            finally
            {
                _isNavigating=false;
            }
        });
    }

    // =========================
    // LOGIN / LOGOUT FLOW
    // =========================
    private async Task HandleAuthChangedAsync()
    {
        if(_isNavigating)
            return;

        try
        {
            _isNavigating=true;

            var target = _auth.IsAuthenticated
                ? "//dashboard"
                : "//login";

            await GoToAsync(target);
        }
        finally
        {
            _isNavigating=false;
        }
    }

    // =========================
    // ROUTE REGISTRATION
    // =========================
    private void RegisterRoutes()
    {
        // Dashboard
        Routing.RegisterRoute(AppRoutes.Dashboard, typeof(DashboardView));

        // Alerts
        Routing.RegisterRoute(AppRoutes.Alerts.List, typeof(AlertsListPage));

        // Patients
        Routing.RegisterRoute(AppRoutes.Patients.Detail, typeof(PatientDetailFormPage));
        Routing.RegisterRoute(AppRoutes.Doctors.List, typeof(DoctorsListPage));
        Routing.RegisterRoute(AppRoutes.Doctors.Detail, typeof(DoctorsDetailPage));
        // Appointments
        Routing.RegisterRoute(AppRoutes.Appointments.List, typeof(AppointmentListPage));
        Routing.RegisterRoute(AppRoutes.Appointments.Detail, typeof(AppointmentDetailPage));

        // Medicines
        Routing.RegisterRoute(AppRoutes.Medicines.List, typeof(MedicineListPage));
        Routing.RegisterRoute(AppRoutes.Medicines.Detail, typeof(MedicineDetailFormPage));
        Routing.RegisterRoute(AppRoutes.ApplicationRegimes.List, typeof(ApplicationRegimeListPage));

        // Prescriptions
        Routing.RegisterRoute(AppRoutes.Prescriptions.List, typeof(PrescriptionListPage));
        Routing.RegisterRoute(AppRoutes.Prescriptions.Detail, typeof(PrescriptionDetailFormPage));

        // Calendar
        Routing.RegisterRoute(AppRoutes.Calendar, typeof(CalendarDashboardPage));
        Routing.RegisterRoute(AppRoutes.CalendarPage, typeof(MainPage));

        // Reports
        Routing.RegisterRoute(AppRoutes.Reports.List, typeof(ReportHistoryPage));
        Routing.RegisterRoute(AppRoutes.Reports.Detail, typeof(DashboardReportPage));
        // Users
        Routing.RegisterRoute(AppRoutes.Users.List, typeof(UsersPage));
        Routing.RegisterRoute(AppRoutes.Users.Detail, typeof(UserEditPage));
        // Therapy

        // Protocols
        Routing.RegisterRoute(AppRoutes.Protocols.List, typeof(ProtocolRegistryPage));
        Routing.RegisterRoute(AppRoutes.Protocols.Detail, typeof(ProtocolDetailFormPage));

        Routing.RegisterRoute(AppRoutes.Encounters.Edit, typeof(EncounterEditPage));
        Routing.RegisterRoute(AppRoutes.Encounters.Create, typeof(EncounterCreatePage));
        Routing.RegisterRoute(AppRoutes.Encounters.List, typeof(EncounterListPage));
        Routing.RegisterRoute(AppRoutes.Encounters.Detail, typeof(EncounterDetailPage));
        Routing.RegisterRoute(AppRoutes.Encounters.Preview, typeof(EncounterQuickPreviewPage));

        //Бекап
        Routing.RegisterRoute(AppRoutes.Backup.Dashboard, typeof(BackupDashboardPage));
        Routing.RegisterRoute(AppRoutes.Backup.Backups, typeof(BackupPage));
        Routing.RegisterRoute(AppRoutes.Backup.Restore, typeof(RestorePage));
        Routing.RegisterRoute(AppRoutes.Backup.History, typeof(BackupHistoryPage));
        Routing.RegisterRoute(AppRoutes.Backup.BackupDetails, typeof(BackupDetailPage));
        Routing.RegisterRoute(AppRoutes.Backup.Destinations, typeof(BackupDestinationsPage));
        Routing.RegisterRoute(AppRoutes.Admin.AdminPanel, typeof(AdminPage));
        // MKB
        Routing.RegisterRoute(AppRoutes.Mkb10.List, typeof(Mkb10CodeListPage));
        Routing.RegisterRoute(AppRoutes.Mkb10.Detail, typeof(Mkb10CodeDetailPage));
        Routing.RegisterRoute(AppRoutes.Mkb10Codes.List, typeof(MbkImportExportPage));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _auth.AuthStateChanged-=OnAuthStateChanged;
    }
}