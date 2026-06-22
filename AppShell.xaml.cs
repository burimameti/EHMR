using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Views;
using EHMR.Views.Appointments;
using EHMR.Views.Prescription;
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
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
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

        // Patients
        Routing.RegisterRoute(AppRoutes.Patients.List, typeof(PatientListPage));
        Routing.RegisterRoute(AppRoutes.Patients.Detail, typeof(PatientDetailFormPage));

        // Appointments
        Routing.RegisterRoute(AppRoutes.Appointments.List, typeof(AppointmentListPage));
        Routing.RegisterRoute(AppRoutes.Appointments.Detail, typeof(AppointmentDetailPage));

        // Medicines
        Routing.RegisterRoute(AppRoutes.Medicines.List, typeof(MedicineListPage));
        Routing.RegisterRoute(AppRoutes.Medicines.Detail, typeof(MedicineDetailFormPage));

        // Prescriptions
        Routing.RegisterRoute(AppRoutes.Prescriptions.List, typeof(PrescriptionListPage));

        // Calendar
        Routing.RegisterRoute(AppRoutes.Calendar, typeof(CalendarDashboardPage));

        // Reports
        Routing.RegisterRoute(AppRoutes.Reports.List, typeof(ReportPage));

        // Users
        Routing.RegisterRoute(AppRoutes.Users.List, typeof(UsersPage));

        // Therapy
        Routing.RegisterRoute(AppRoutes.Therapy.Detail, typeof(TherapyDetailsPage));
        Routing.RegisterRoute(AppRoutes.Therapy.List, typeof(TherapyCyclesPage));

        // Protocols
        Routing.RegisterRoute(AppRoutes.Protocols.List, typeof(ProtocolRegistryPage));
        Routing.RegisterRoute(AppRoutes.Protocols.Detail, typeof(ProtocolDetailFormPage));

        // MKB
        Routing.RegisterRoute(AppRoutes.Mkb10Codes.List, typeof(MbkImportExportPage));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _auth.AuthStateChanged-=OnAuthStateChanged;
    }
}