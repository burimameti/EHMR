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
        // PATIENTS (ПАЦИЕНТИ)
        // ==========================
        new()
        {
            GroupTitle="Регистар на пациенти",
            Route=AppRoutes.Patients.List,
            Module=Modules.Patients,
            Icon=AppGroupIcons.Patients,
            Items=
            [
                new()
                {
                    Title="Листа на пациенти",
                    Route=AppRoutes.Patients.List,
                    Module=Modules.Patients,
                    RequiredAction=ModuleAction.View,
                    Icon=new IconDefinition { Glyph="\uf03a", Font=IconFontType.FontAwesomeSolid }
                },
                new()
                {
                    Title="Нов пациент",
                    Route=AppRoutes.Patients.Detail,
                    Module=Modules.Patients,
                    RequiredAction=ModuleAction.Create,
                    StartsNewRecord=true,
                    Icon=new IconDefinition { Glyph="\uf234", Font=IconFontType.FontAwesomeSolid }
                }
            ]
        },
        // ==========================
        // ENCOUNTERS (ПРЕГЛЕДИ)
        // ==========================
        new()
        {
            GroupTitle = "Прегледи",
            Route = AppRoutes.Encounters.List,
            Module = Modules.Encounters,
            Icon = new IconDefinition { Glyph = "\uf0f1", Font = IconFontType.FontAwesomeSolid },
            Items =
            [
                new()
                {
                    Title = "Листа на прегледи",
                    Route = AppRoutes.Encounters.List,
                    Module = Modules.Encounters,
                    Icon = new IconDefinition { Glyph = "\uf0ae", Font = IconFontType.FontAwesomeSolid }
                },
                new()
                {
                    Title = "Нов преглед",
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
            GroupTitle = "Термини",
            Route = AppRoutes.Appointments.List,
            Module = Modules.Appointments,
            Icon = new IconDefinition { Glyph = "\uf274", Font = IconFontType.FontAwesomeSolid },
            Items =
            [
                new()
                {
                    Title = "Листа на термини",
                    Route = AppRoutes.Appointments.List,
                    Module = Modules.Appointments,
                    RequiredAction = ModuleAction.View,
                    Icon = new IconDefinition { Glyph = "\uf03a", Font = IconFontType.FontAwesomeSolid }
                },
            ]
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
        // PRESCRIPTIONS
        // ==========================
        //new()
        //{
        //    GroupTitle = "Рецепти",
        //    Route = AppRoutes.Prescriptions.List,
        //    Module = Modules.Prescriptions,
        //    Icon = new IconDefinition { Glyph = "\\uf328", Font = IconFontType.FontAwesomeSolid }
        //},

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
        // MKB-10
        // ==========================
        //new()
        //{
        //    GroupTitle = "MKB-10",
        //    Route = AppRoutes.Mkb10Codes.List,
        //    Module = Modules.MKBCodes,
        //    Icon = new IconDefinition { Glyph = "\uf02d", Font = IconFontType.FontAwesomeSolid }
        //},

        // ==========================
        // BACKUPS
        // ==========================
        //new()
        //{
        //    GroupTitle = "Резервни копии",
        //    Route = AppRoutes.Backup.Dashboard,
        //    Module = Modules.BackupDashboard,
        //    Icon = new IconDefinition { Glyph = "\uf1da", Font = IconFontType.FontAwesomeSolid },
        //    Items =
        //    [
        //        new() { Title = "Контролна табла", Route = AppRoutes.Backup.Dashboard, Module = Modules.BackupDashboard, RequiredAction = ModuleAction.View },
        //        new() { Title = "Историја", Route = AppRoutes.Backup.History, Module = Modules.Backups, RequiredAction = ModuleAction.View }
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
            Icon = new IconDefinition { Glyph = "\uf13e", Font = IconFontType.FontAwesomeSolid },
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
