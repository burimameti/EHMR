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
                    Route = AppRoutes.Calendar,
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
           // GroupTitle = "Прегледи",
           // Module = Modules.Encounters,
           // Icon = new IconDefinition { Glyph = "\uf0f1", Font = IconFontType.FontAwesomeSolid }, // Stethoscope
            //Items =
            //[
                //new()
               // {
                         GroupTitle = "Прегледи",
                   // Title = "Листа на прегледи",
                    Route = AppRoutes.Encounters.List,
                    Module = Modules.Encounters,
                    Icon = new IconDefinition { Glyph = "\uf0ae", Font = IconFontType.FontAwesomeSolid } // Tasks / List
               // }
            //]
        },  new()
        {
            //GroupTitle = "Пациенти",
            //Module = Modules.Patients,
            //Icon = new IconDefinition { Glyph = "\uf0c0", Font = IconFontType.FontAwesomeSolid }, // Users
            //Items =
            //[
               
                    GroupTitle = "Медицински Картони",
                    Route = AppRoutes.Patients.List,
                    Module = Modules.Patients,
                    Icon = new IconDefinition { Glyph = "\uf2bd", Font = IconFontType.FontAwesomeSolid } // User Card
                
            //]
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

        // ==========================
        // THERAPY
        // ==========================
        new()
        {
            //GroupTitle = "Терапии",
            //Module = Modules.Therapy,
            //Icon = new IconDefinition { Glyph = "\uf0c3", Font = IconFontType.FontAwesomeSolid }, // Flask / Vial
            //Items =
            //[
            //    new()
            //    {
                    GroupTitle = "Циклуси",
                    Route = AppRoutes.Therapy.List,
                    Module = Modules.Therapy,
                    Icon = new IconDefinition { Glyph = "\uf1b1", Font = IconFontType.FontAwesomeSolid } // Cubes
            //    }
            //]
        },

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
     
         new(){ GroupTitle = "Рецепти",
                    Route = AppRoutes.Prescriptions.List,
                    Module = Modules.Inventory,
                    Icon = new IconDefinition { Glyph = "\uf461", Font = IconFontType.FontAwesomeSolid } // Prescription Clipboard
            //    }
            //]
        },
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
            //Items =
            //[
            //    new()
            //    {
            //        Title = "Админ",
            //        Route = AppRoutes.Admin.AdminPanel,
            //        Module = Modules.Administration,
            //        Icon = new IconDefinition { Glyph = "\uf508", Font = IconFontType.FontAwesomeSolid } // User Cog
            //    },
            //     new()
            //    {
            //        Title = "Доктори",
            //        Route = AppRoutes.Doctors.List,
            //        Module = Modules.Doctors,
            //        Icon = new IconDefinition { Glyph = "\uf508", Font = IconFontType.FontAwesomeSolid } // User Cog
            //    }

            //]
        }
    ];
}