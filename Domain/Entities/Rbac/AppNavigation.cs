using EHMR.Constants;

namespace EHMR.Domain.Entities.Rbac;

public static class AppNavigation
{
    public static IReadOnlyList<NavigationGroup> AllGroups =>
    [
        Main("Почетна страна", AppRoutes.Dashboard, Modules.Dashboard, "\uf00a"),
        Main("Календар", AppRoutes.Calendar, Modules.Calendar, "\uf133"),
        Main("Пациенти", AppRoutes.Patients.List, Modules.Patients, "\uf0c0"),
        Main("Прегледи", AppRoutes.Encounters.List, Modules.Encounters, "\uf0f0"),
        Main("Термини", AppRoutes.Appointments.List, Modules.Appointments, "\uf274"),
        Main("Протоколи", AppRoutes.Protocols.List, Modules.Protocols, "\uf03a"),
        Main("Лекови", AppRoutes.Medicines.List, Modules.Inventory, "\uf484"),
        Main("МКБ-10", AppRoutes.Mkb10.List, Modules.MKBCodes, "\uf02d"),
        Main("Рецепти", AppRoutes.Prescriptions.List, Modules.Prescriptions, "\uf328"),
        Main("Извештаи", AppRoutes.Reports.List, Modules.Reports, "\uf1c3"),

        new()
        {
            GroupTitle = "Администрација",
            Route = AppRoutes.Admin.AdminPanel,
            Module = Modules.Administration,
            Icon = new IconDefinition { Glyph = "\uf013", Font = IconFontType.FontAwesomeSolid },
            Items =
            [
                new()
                {
                    Title = "Преглед",
                    Route = AppRoutes.Admin.AdminPanel,
                    Module = Modules.Administration,
                    RequiredAction = ModuleAction.View,
                    Icon = new IconDefinition { Glyph = "\uf00a", Font = IconFontType.FontAwesomeSolid }
                },
                new()
                {
                    Title = "Корисници",
                    Route = AppRoutes.Users.List,
                    Module = Modules.Administration,
                    RequiredAction = ModuleAction.View,
                    Icon = new IconDefinition { Glyph = "\uf0c0", Font = IconFontType.FontAwesomeSolid }
                },
                new()
                {
                    Title = "Реуматолози",
                    Route = AppRoutes.Doctors.List,
                    Module = Modules.Doctors,
                    RequiredAction = ModuleAction.View,
                    Icon = new IconDefinition { Glyph = "\uf0f0", Font = IconFontType.FontAwesomeSolid }
                }
            ]
        }
    ];

    private static NavigationGroup Main(string title, string route, string module, string glyph)
        => new()
        {
            GroupTitle = title,
            Route = route,
            Module = module,
            Icon = new IconDefinition
            {
                Glyph = glyph,
                Font = IconFontType.FontAwesomeSolid
            }
        };

    private static readonly IReadOnlyDictionary<string, string> ParentRoutes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AppRoutes.Patients.Detail] = AppRoutes.Patients.List,
            [AppRoutes.Doctors.Detail] = AppRoutes.Doctors.List,
            [AppRoutes.Appointments.Detail] = AppRoutes.Appointments.List,
            [AppRoutes.Medicines.Detail] = AppRoutes.Medicines.List,
            [AppRoutes.Prescriptions.Detail] = AppRoutes.Prescriptions.List,
            [AppRoutes.Reports.Detail] = AppRoutes.Reports.List,
            [AppRoutes.Users.Detail] = AppRoutes.Admin.AdminPanel,
            [AppRoutes.Therapy.Detail] = AppRoutes.Therapy.List,
            [AppRoutes.Protocols.Detail] = AppRoutes.Protocols.List,
            [AppRoutes.Encounters.Create] = AppRoutes.Encounters.List,
            [AppRoutes.Encounters.Edit] = AppRoutes.Encounters.List,
            [AppRoutes.Encounters.Detail] = AppRoutes.Encounters.List
        };

    public static string ResolveMenuRoute(string route)
    {
        if(string.IsNullOrWhiteSpace(route))
            return string.Empty;

        var normalized = route.Trim('/');
        return ParentRoutes.TryGetValue(normalized, out var parent)
            ? parent
            : normalized;
    }
}
