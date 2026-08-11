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
            GroupTitle = "Почента страна",
            Route = AppRoutes.Dashboard,
            Module = Modules.Dashboard,
            Icon = new IconDefinition { Glyph = "\uf00a", Font = IconFontType.FontAwesomeSolid } // Grid / Home
        },


    
          new()
        {
        //    GroupTitle = "Календар",
        //    Module = Modules.Calendar,
        //    Icon = new IconDefinition { Glyph = "\uf073", Font = IconFontType.FontAwesomeSolid }, // Calendar
        //    Items =
        //    [
        //        new()
        //        {
                    GroupTitle = "Календар",
                    Route = AppRoutes.CalendarPage,
                    Module = Modules.Calendar,
                    Icon = new IconDefinition { Glyph = "\uf133", Font = IconFontType.FontAwesomeSolid } // Alternative Calendar
            //    }
            //]
        },
        // ==========================
        // ENCOUNTERS (ПРЕГЛЕДИ)
        // ==========================
        new()
        {
            GroupTitle = "\u041F\u0440\u0435\u0433\u043B\u0435\u0434\u0438",
            Route = AppRoutes.Encounters.List,
            Module = Modules.Encounters,
            Icon = new IconDefinition { Glyph = "\uf0f1", Font = IconFontType.FontAwesomeSolid },
            Items =
            [
                new()
                {
                    Title = "\u041B\u0438\u0441\u0442\u0430 \u043D\u0430 \u043F\u0440\u0435\u0433\u043B\u0435\u0434\u0438",
                    Route = AppRoutes.Encounters.List,
                    Module = Modules.Encounters,
                    Icon = new IconDefinition { Glyph = "\uf0ae", Font = IconFontType.FontAwesomeSolid }
                },
                new()
                {
                    Title = "\u041D\u043E\u0432 \u043F\u0440\u0435\u0433\u043B\u0435\u0434",
                    Route = AppRoutes.Encounters.Create,
                    Module = Modules.Encounters,
                    Icon = new IconDefinition { Glyph = "\uf067", Font = IconFontType.FontAwesomeSolid }
                }
            ]
        },
        // ==========================
        // APPOINTMENTS (ТЕРМИНИ)
        // ==========================
        new()
        {
            //GroupTitle = "Термин",
            //Module = Modules.Appointments,
            //Icon = new IconDefinition { Glyph = "\uf274", Font = IconFontType.FontAwesomeSolid }, // Calendar Check
            //Items =
            //[
            //    new()
            //    {
                    GroupTitle = "Термини",
                    Route = AppRoutes.Appointments.List,
                    Module = Modules.Appointments,
                    Icon = new IconDefinition { Glyph = "\uf017", Font = IconFontType.FontAwesomeSolid } // Clock
            //    }
            //]
        },

        //// ==========================
        //// THERAPY
        //// ==========================
        //new()
        //{
        //    //GroupTitle = "Терапии",
        //    //Module = Modules.Therapy,
        //    //Icon = new IconDefinition { Glyph = "\uf0c3", Font = IconFontType.FontAwesomeSolid }, // Flask / Vial
        //    //Items =
        //    //[
        //    //    new()
        //    //    {
        //            GroupTitle = "Терапии",
        //            Route = AppRoutes.Therapy.List,
        //            Module = Modules.Therapy,
        //            Icon = new IconDefinition { Glyph = "\uf1b1", Font = IconFontType.FontAwesomeSolid } // Cubes
        //    //    }
        //    //]
        //},

        // ==========================
        // PROTOCOLS
        // ==========================
        new()
        {
            //GroupTitle = "Протоколи",
            //Module = Modules.Protocols,
            //Icon = new IconDefinition { Glyph = "\uf15c", Font = IconFontType.FontAwesomeSolid }, // File Medical
            //Items =
            //[
            //    new()
            //    {
                    GroupTitle = "Протоколи",
                    Route = AppRoutes.Protocols.List,
                    Module = Modules.Protocols,
                    Icon = new IconDefinition { Glyph = "\uf03a", Font = IconFontType.FontAwesomeSolid } // List
            //    }
            //]
        },

        // ==========================
        // MKB-10
        // ==========================
     
        // new(){ GroupTitle = "Рецепти",
        //            Route = AppRoutes.Prescriptions.List,
        //            Module = Modules.Inventory,
        //            Icon = new IconDefinition { Glyph = "\uf461", Font = IconFontType.FontAwesomeSolid } // Prescription Clipboard
        //    //    }
        //    //]
        //},
        // ==========================
        // MEDICINES
        // ==========================
        new()
        {
            //GroupTitle = "Лекови",
            //Module = Modules.Inventory,
            //Icon = new IconDefinition { Glyph = "\uf46b", Font = IconFontType.FontAwesomeSolid }, // Pill
            //Items =
            //[
            //    new()
            //    {
                    GroupTitle = "Лекови",
                    Route = AppRoutes.Medicines.List,
                    Module = Modules.Inventory,
                    Icon = new IconDefinition { Glyph = "\uf484", Font = IconFontType.FontAwesomeSolid } // Medicine Bottle
                //},
                //new()
                //{
                   
            //    }
            //]
        },//new()
                //{
                  

        // ==========================
        // REPORTS
        // ==========================
        new (){
        //{
        //    GroupTitle = "Извештаи",
        //    Module = Modules.Reports,
        //    Icon = new IconDefinition { Glyph = "\uf201", Font = IconFontType.FontAwesomeSolid }, // Chart Line
        //    Items =
        //    [
        //        new()
        //        {
                    GroupTitle = "Извештаи",
                    Route = AppRoutes.Reports.List,
                    Module = Modules.Reports,
                    Icon = new IconDefinition { Glyph = "\uf1c3", Font = IconFontType.FontAwesomeSolid } // File Excel
            //    }
            //]
        },

        // ==========================
        // CALENDAR
        // ==========================
      
        //   new()
        //{
        //    GroupTitle = "ИМПОРТ МКБ-10",
        //    Module = Modules.MKBCodes,
        //    Icon = new IconDefinition { Glyph = "\uf02d", Font = IconFontType.FontAwesomeSolid }, // Book
        //    Items =
        //    [
        //        new()
        //        {
        //            Title = "МКБ Кодови",
        //            Route = AppRoutes.Mkb10Codes.List,
        //            Module = Modules.MKBCodes,
        //            Icon = new IconDefinition { Glyph = "\uf02b", Font = IconFontType.FontAwesomeSolid } // Tag
        //        }
        //    ]

        //},   new()
        //{
        //    GroupTitle = "БЕКАП",
        //    Module = Modules.BackupDashboard,
        //    Icon = new IconDefinition { Glyph = "\uf02d", Font = IconFontType.FontAwesomeSolid }, // Book
        //    Items =
        //    [
        //        new()
        //        {
        //            Title = "Главна страна",
        //            Route = AppRoutes.Backup.Dashboard,
        //            Module = Modules.BackupDashboard,
        //            Icon = new IconDefinition { Glyph = "\uf02b", Font = IconFontType.FontAwesomeSolid } // Tag
        //        },
        //        new()
        //        {
        //            Title = "Бекап",
        //            Route = AppRoutes.Backup.Backups,
        //            Module = Modules.BackupDashboard,
        //            Icon = new IconDefinition { Glyph = "\uf02b", Font = IconFontType.FontAwesomeSolid } // Tag
        //        },
        //        new()
        //        {
        //            Title = "Враќање",
        //            Route = AppRoutes.Backup.Restore,
        //            Module = Modules.BackupDashboard,
        //            Icon = new IconDefinition { Glyph = "\uf02b", Font = IconFontType.FontAwesomeSolid } // Tag
        //        },
        //        new()
        //        {
        //            Title = "Историја",
        //            Route = AppRoutes.Backup.History,
        //            Module = Modules.BackupDashboard,
        //            Icon = new IconDefinition { Glyph = "\uf02b", Font = IconFontType.FontAwesomeSolid } // Tag
        //        }
        //    ]

        //},
        // ==========================
        // ADMINISTRATION
        // ==========================
        new()
        {
            GroupTitle = "Администрација",
              Route = AppRoutes.Admin.AdminPanel,
            Module = Modules.Administration,
            Icon = new IconDefinition { Glyph = "\uf13e", Font = IconFontType.FontAwesomeSolid }, // Shield User Lock
            Items =
            [
                new()
                {
                    Title = "Корисници",
                    Route = AppRoutes.Admin.AdminPanel,
                    Module = Modules.Administration,
                    Icon = new IconDefinition { Glyph = "\uf508", Font = IconFontType.FontAwesomeSolid } // User Cog
                },new()
                {
                    Title = "",
                    Route = AppRoutes.Admin.AdminPanel,
                    Module = Modules.Administration,
                    Icon = new IconDefinition { Glyph = "\uf508", Font = IconFontType.FontAwesomeSolid } // User Cog
                },
                 new()
                {
                    Title = "Реуматолози",
                    Route = AppRoutes.Doctors.List,
                    Module = Modules.Doctors,
                    Icon = new IconDefinition { Glyph = "\uf508", Font = IconFontType.FontAwesomeSolid } // User Cog
                }

            ]
        }
    ];
    private static readonly IReadOnlyDictionary<string, string> ParentRoutes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AppRoutes.Patients.Detail]=AppRoutes.Patients.List,
            [AppRoutes.Doctors.Detail]=AppRoutes.Doctors.List,
            [AppRoutes.Appointments.Detail]=AppRoutes.Appointments.List,
            [AppRoutes.Medicines.Detail]=AppRoutes.Medicines.List,
            [AppRoutes.Prescriptions.Detail]=AppRoutes.Prescriptions.List,
            [AppRoutes.Reports.Detail]=AppRoutes.Reports.List,
            [AppRoutes.Users.Detail]=AppRoutes.Admin.AdminPanel,
            [AppRoutes.Therapy.Detail]=AppRoutes.Therapy.List,
            [AppRoutes.Protocols.Detail]=AppRoutes.Protocols.List,
            [AppRoutes.Encounters.Create]=AppRoutes.Encounters.List,
            [AppRoutes.Encounters.Edit]=AppRoutes.Encounters.List,
            [AppRoutes.Encounters.Detail]=AppRoutes.Encounters.List
        };

    public static string ResolveMenuRoute(string route)
    {
        if(string.IsNullOrWhiteSpace(route))
            return string.Empty;

        var normalized=route.Trim('/');
        return ParentRoutes.TryGetValue(normalized, out var parent)
            ? parent
            : normalized;
    }
}
