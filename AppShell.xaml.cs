using EHMR.Constants;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using EHMR.ViewModels;
using EHMR.Views;
using EHMR.Views.Prescription;
using EHMR.Views.Therapies;

namespace EHMR;

public partial class AppShell : Shell
{
    private readonly IAuthStateService _auth;
    private readonly INavigationCoordinator _coordinator;
    private readonly IAuthorizationPolicy _policy;
    private bool _isNavigating;

    // СЕГА КОРИСТИМЕ ЧИСТ КЛЕШ БЕЗ ДУПЛИРАЊА
    public AppShell(
        IAuthStateService auth,
        INavigationCoordinator coordinator,
        IAuthorizationPolicy policy)
    {
        InitializeComponent();

        _auth=auth;
        _coordinator=coordinator;
        _policy=policy;

        RegisterRoutes();
        SetupCoordinator();

        // Се претплатуваме САМО ЕДНАШ
        _auth.AuthStateChanged+=OnAuthStateChanged;
    }

    // Овој метод ќе го повикаме безбедно ОД НАДВОР дури откако Shell ќе се прикаже на екран
    public async Task HandleInitialNavigationAsync()
    {
        ApplyAuthorizationRules();
        await HandleAuthChangedAsync();
    }

    private async void OnAuthStateChanged(object? sender, EventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            ApplyAuthorizationRules();
            await HandleAuthChangedAsync();
        });
    }

    private void ApplyAuthorizationRules()
    {
        foreach(var item in Items)
        {
            //if(item is ShellItem shellItem)
            //{
            //    shellItem.IsVisible=_policy.CanAccess(shellItem.Route);
            //}
        }
    }

    private void SetupCoordinator()
    {
        _coordinator.RegisterHandler(async route =>
        {
            if(_isNavigating) return;
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

    private async Task HandleAuthChangedAsync()
    {
        // Ако уште не е вчитан хандлерот во оперативниот систем, навигирај преку оваа (this) инстанца, а не преку Shell.Current
        if(_isNavigating) return;

        try
        {
            _isNavigating=true;
            var target = _auth.IsAuthenticated ? "//dashboard" : "//login";

            // Наместо Shell.Current, користиме директно GoToAsync бидејќи сме внатре во самата Shell класа
            await GoToAsync(target);
        }
        finally
        {
            _isNavigating=false;
        }
    }

    private void RegisterRoutes()
    {
        // =========================
        // DASHBOARD
        // =========================
        Routing.RegisterRoute(AppRoutes.Dashboard, typeof(DashboardView));

        // =========================
        // PATIENTS
        // =========================
        Routing.RegisterRoute("patientslist", typeof(PatientListPage));
        Routing.RegisterRoute("patientsdetail", typeof(PatientDetailFormPage));

        // =========================
        // APPOINTMENTS
        // =========================
        Routing.RegisterRoute("appointmentslist", typeof(AppointmentListPage));
        Routing.RegisterRoute("appointmentsdetail", typeof(AppointmentDetailFormPage));

        // =========================
        // MEDICINE / PHARMACY
        // =========================
        Routing.RegisterRoute(AppRoutes.Medicines.List, typeof(MedicineListPage));
        Routing.RegisterRoute(AppRoutes.Medicines.Detail, typeof(MedicineDetailFormPage));
        Routing.RegisterRoute(AppRoutes.Prescriptions.List, typeof(PrescriptionListPage));
        Routing.RegisterRoute(AppRoutes.Prescriptions.Detail, typeof(PrescriptionDetailFormPage));
        // =========================
        // CALENDAR / ADMIN UI
        // =========================
        Routing.RegisterRoute(AppRoutes.Calendar, typeof(CalendarDashboardPage));
        Routing.RegisterRoute(AppRoutes.Reports.List, typeof(ReportPage));
        Routing.RegisterRoute(AppRoutes.Users.List, typeof(UsersPage));
        Routing.RegisterRoute(AppRoutes.Users.Detail, typeof(UserEditPage));

        // =========================
        // THERAPY MODULE
        // =========================
        Routing.RegisterRoute("therapylist", typeof(PlanPage));
        Routing.RegisterRoute("therapydetails", typeof(TherapyDetailsPage));
        Routing.RegisterRoute(AppRoutes.TherapyCycle.List, typeof(TherapyCyclesPage));
        // Routing.RegisterRoute(AppRoutes.TherapyCycle.Details, typeof(TherapyCycleDetailsPage));
        // =========================
        // PROTOCOLS
        // =========================
        Routing.RegisterRoute("protocolslist", typeof(ProtocolRegistryPage));
        Routing.RegisterRoute("protocolsdetails", typeof(ProtocolDetailFormPage));

        // =========================
        // RULES / CONFIGURATION
        // =========================
        Routing.RegisterRoute("rulesschedule-medicine", typeof(ScheduleMedicineRuleDetailFormPage));
    }
}