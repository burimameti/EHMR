using EHMR.Backups.Views;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Views;
using EHMR.Views.Admin;
using EHMR.Views.Appointments;
using EHMR.Views.Calendar;
using EHMR.Views.Doctors;
using EHMR.Views.Encounters;
using EHMR.Views.Patients;
using EHMR.Views.Prescription;
using EHMR.Views.Protocols;
using EHMR.Views.Reports;
using EHMR.Views.Therapies;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EHMR;

public partial class AppShell : Shell
{
    private readonly IAuthStateService _auth;
    private readonly INavigationCoordinator _coordinator;
    private readonly IAuthorizationService _authorization;
    private bool _isNavigating;

    private static readonly HashSet<string> RootRoutes =
    [
        AppRoutes.Dashboard,
        AppRoutes.CalendarPage,
        AppRoutes.Patients.List,
        AppRoutes.Encounters.List,
        AppRoutes.Appointments.List,
        AppRoutes.Protocols.List,
        AppRoutes.Medicines.List,
        AppRoutes.Reports.List,
        AppRoutes.Admin.AdminPanel,
        AppRoutes.Users.List,
        AppRoutes.Doctors.List,
        AppRoutes.Therapy.List,
        AppRoutes.Prescriptions.List,
        AppRoutes.Backup.Dashboard,
        AppRoutes.Backup.Backups,
        AppRoutes.Mkb10Codes.List
    ];

    public AppShell(
        IAuthStateService auth,
        IAuthorizationService authorization,
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

    public async Task HandleInitialNavigationAsync()
    {
        await HandleAuthChangedAsync();
    }

    private async void OnAuthStateChanged(object? sender, EventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await HandleAuthChangedAsync();
        });
    }

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
                    await GoToRouteAsync(route);
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
        if(_isNavigating)
            return;

        try
        {
            _isNavigating=true;

            var target = _auth.IsAuthenticated
                ? $"//{AppRoutes.Dashboard}"
                : $"//{AppRoutes.Login}";

            await GoToAsync(target);
        }
        finally
        {
            _isNavigating=false;
        }
    }

    private async Task GoToRouteAsync(string route)
    {
        if(string.IsNullOrWhiteSpace(route))
            return;

        var normalized=route.Trim('/');

        if(RootRoutes.Contains(normalized))
            await GoToAsync($"//{normalized}");
        else
            await GoToAsync(route);
    }

    private void RegisterRoutes()
    {
        // Detail/create/edit routes deliberately remain relative so they
        // can be opened from a root workspace and closed with Back.
        Routing.RegisterRoute(AppRoutes.Patients.Detail, typeof(PatientDetailFormPage));

        Routing.RegisterRoute(AppRoutes.Doctors.Detail, typeof(DoctorsDetailPage));

        Routing.RegisterRoute(AppRoutes.Appointments.Detail, typeof(AppointmentDetailPage));

        Routing.RegisterRoute(AppRoutes.Medicines.Detail, typeof(MedicineDetailFormPage));

        Routing.RegisterRoute(AppRoutes.Prescriptions.Detail, typeof(PrescriptionDetailFormPage));

        Routing.RegisterRoute(AppRoutes.Reports.Detail, typeof(DashboardReportPage));

        Routing.RegisterRoute(AppRoutes.Users.Detail, typeof(UserEditPage));

        Routing.RegisterRoute(AppRoutes.Therapy.Detail, typeof(TherapyDetailsPage));

        Routing.RegisterRoute(AppRoutes.Protocols.Detail, typeof(ProtocolDetailFormPage));

        Routing.RegisterRoute(AppRoutes.Encounters.Create, typeof(EncounterCreatePage));
        Routing.RegisterRoute(AppRoutes.Encounters.Edit, typeof(EncounterEditPage));
        Routing.RegisterRoute(AppRoutes.Encounters.Detail, typeof(EncounterDetailPage));

        Routing.RegisterRoute(AppRoutes.Backup.Restore, typeof(RestorePage));
        Routing.RegisterRoute(AppRoutes.Backup.History, typeof(BackupHistoryPage));
        Routing.RegisterRoute(AppRoutes.Backup.BackupDetails, typeof(BackupDetailPage));
        Routing.RegisterRoute(AppRoutes.Backup.Destinations, typeof(BackupDestinationsPage));

        Routing.RegisterRoute(AppRoutes.Mkb10.Detail, typeof(Mkb10CodeDetailPage));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _auth.AuthStateChanged-=OnAuthStateChanged;
    }
}
