using EHMR.Constants;

namespace EHMR.Domain.Entities.Rbac;

public static class AppNavigation
{
    public static IReadOnlyList<NavigationGroup> AllGroups =>
    [
        // ==========================
        // DASHBOARD
        // ==========================
        new()
        {
            GroupTitle = "Dashboard",
            Route = AppRoutes.Dashboard,
            Module = Modules.Dashboard
        },

        // ==========================
        // PATIENTS
        // ==========================
        new()
        {
            GroupTitle = "Пациенти",
            Module = Modules.Patients,
            Items =
            [
                new()
                {
                    Title = "Листа на пациенти",
                    Route = AppRoutes.Patients.List,
                    Module = Modules.Patients
                }
            ]
        },

        // ==========================
        // APPOINTMENTS
        // ==========================
        new()
        {
            GroupTitle = "Прегледи",
            Module = Modules.Appointments,
            Items =
            [
                new()
                {
                    Title = "Листа на прегледи",
                    Route = AppRoutes.Appointments.List,
                    Module = Modules.Appointments
                }
            ]
        },

        // ==========================
        // THERAPY
        // ==========================
        new()
        {
            GroupTitle = "Терапии",
            Module = Modules.Therapy,
            Items =
            [
                new()
                {
                    Title = "Терапевтски циклуси",
                    Route = AppRoutes.Therapy.List,
                    Module = Modules.Therapy
                }
            ]
        },

        // ==========================
        // PROTOCOLS
        // ==========================
        new()
        {
            GroupTitle = "Протоколи",
            Module = Modules.Protocols,
            Items =
            [
                new()
                {
                    Title = "Регистар на протоколи",
                    Route = AppRoutes.Protocols.List,
                    Module = Modules.Protocols
                }
            ]
        },

        // ==========================
        // MKB-10
        // ==========================
        new()
        {
            GroupTitle = "МКБ-10",
            Module = Modules.MKBCodes,
            Items =
            [
                new()
                {
                    Title = "МКБ Кодови",
                    Route = AppRoutes.Mkb10Codes.List,
                    Module = Modules.MKBCodes
                }
            ]
        },

        // ==========================
        // MEDICINES
        // ==========================
        new()
        {
            GroupTitle = "Лекови",
            Module = Modules.Inventory,
            Items =
            [
                new()
                {
                    Title = "Листа на лекови",
                    Route = AppRoutes.Medicines.List,
                    Module = Modules.Inventory
                },

                new()
                {
                    Title = "Рецепти",
                    Route = AppRoutes.Prescriptions.List,
                    Module = Modules.Inventory
                }
            ]
        },

        // ==========================
        // REPORTS
        // ==========================
        new()
        {
            GroupTitle = "Извештаи",
            Module = Modules.Reports,
            Items =
            [
                new()
                {
                    Title = "Извештаи",
                    Route = AppRoutes.Reports.List,
                    Module = Modules.Reports
                }
            ]
        },

        // ==========================
        // CALENDAR
        // ==========================
        new()
        {
            GroupTitle = "Календар",
            Module = Modules.Calendar,
            Items =
            [
                new()
                {
                    Title = "Календар",
                    Route = AppRoutes.Calendar,
                    Module = Modules.Calendar
                }
            ]
        },

        // ==========================
        // ADMINISTRATION
        // ==========================
        new()
        {
            GroupTitle = "Администрација",
            Module = Modules.Administration,
            Items =
            [
                new()
                {
                    Title = "Корисници",
                    Route = AppRoutes.Users.List,
                    Module = Modules.Administration
                }
            ]
        }
    ];
}