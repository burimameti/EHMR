using System;
using System.Collections.Generic;
using EHMR.Domain.Entities;

namespace EHMR.Constants;

public class IconDefinition
{
    public string Glyph { get; set; } = string.Empty;

    public IconFontType Font
    {
        get; set;
    }

    public IconDefinition()
    {
    }

    public IconDefinition(string glyph, IconFontType font)
    {
        Glyph=glyph;
        Font=font;
    }
}

public enum IconFontType
{
    FontAwesomeSolid,
    Fluent
}

public static class FontResolver
{
    public static string GetFontFamily(IconFontType type)
    {
        return type switch
        {
            IconFontType.FontAwesomeSolid => "FASolid",
            IconFontType.Fluent => "FontIcons",
            _ => "FASolid"
        };
    }
}

public static class AppNavigation
{
    public static IReadOnlyList<NavigationGroup> AllGroups
    {
        get;
    } = new List<NavigationGroup>
    {
        // 1. DASHBOARD ГРУПА
        new()
        {
            GroupTitle = "Основна Страна",
            Icon = AppGroupIcons.Overview,
            Route = "dashboard",
            Module = Domain.Entities.Modules.Dashboard, // Врзано со доменскиот Dashboard модул
            RequiredPermissions = new() { Domain.Entities.Modules.Dashboard },
            Items = new() // Нема подменија
        },

        // 2. ПАЦИЕНТИ ГРУПА

        // 3. КАЛЕНДАР И ТЕРМИНИ ГРУПА
        new()
        {
            GroupTitle = "Прегледи",
            Icon = AppGroupIcons.Appointment,
            Module = Domain.Entities.Modules.Appointments,
            RequiredPermissions = new() { Domain.Entities.Modules.Appointments },
            Items = new()
            {
                new()
                {
                    Title = "Листа на Прeгледи",
                    Route = AppRoutes.Appointments.List,
                    Icon = AppIcons.Appointment,
                    Module = Domain.Entities.Modules.Appointments,
                    RequiredPermissions = new() { Domain.Entities.Modules.Appointments }
                }
            }
        },
          new()
        {
            GroupTitle = "Календар",
            Icon = AppGroupIcons.Appointment,
            Module = Domain.Entities.Modules.Appointments,
            RequiredPermissions = new() { Domain.Entities.Modules.Appointments },
            Items = new()
            {
                new()
                {
                    Title = "Календарски Преглед",
                    Route = AppRoutes.Calendar,
                    Icon = AppIcons.Appointment,
                    Module = Domain.Entities.Modules.Appointments,
                    RequiredPermissions = new() { Domain.Entities.Modules.Appointments }
                },
            }
        },
        // 4. ТЕРАПИИ ГРУПА
        new()
        {
            GroupTitle = "Терапии",
            Icon = AppGroupIcons.Appointment,
            Module = Domain.Entities.Modules.Therapy,
            RequiredPermissions = new() { Domain.Entities.Modules.Therapy },
            Items = new()
            {
                new()
                {
                    Title = "Листа на терапии",
                    Route = AppRoutes.TherapyCycle.List,
                    Icon = AppIcons.Therapy,
                    Module = Domain.Entities.Modules.Therapy,
                    RequiredPermissions = new() { Domain.Entities.Modules.Therapy }
                }
            }
        },

        // 5. ПРОТОКОЛИ ГРУПА
        new()
        {
            GroupTitle = "Медицнински Протоколи",
            Icon = AppGroupIcons.MedicalRecords,
            Module = Domain.Entities.Modules.Protocols,
            RequiredPermissions = new() { Domain.Entities.Modules.Protocols },
            Items = new()
            {
                new()
                {
                    Title = "Листа на Протоколи",
                    Route = AppRoutes.Protocols.List,
                    Icon = AppIcons.Records,
                    Module = Domain.Entities.Modules.Protocols,
                    RequiredPermissions = new() { Domain.Entities.Modules.Protocols }
                }
            }
        },
          new()
        {
            GroupTitle = "Пациенти",
            Icon = AppGroupIcons.Patients,
            Module = Domain.Entities.Modules.Patients,
            RequiredPermissions = new() { Domain.Entities.Modules.Patients },
            Items = new()
            {
                new()
                {
                    Title = "Листа на пациенти",
                    Route = AppRoutes.Patients.List,
                    Icon = AppIcons.Patients,
                    Module = Domain.Entities.Modules.Patients,
                    RequiredPermissions = new() { Domain.Entities.Modules.Patients }
                }
            }
        },
        // 6. ИНВЕНТАР / АПТЕКА ГРУПА (Спојува Рецепти и Лекови под чадорот на Inventory)
        new()
        {
            GroupTitle = "Инвентар",
            Icon = AppGroupIcons.Prescriptions,
            Module = Domain.Entities.Modules.Inventory,
            RequiredPermissions = new() { Domain.Entities.Modules.Inventory },
            Items = new()
            {
                new()
                {
                    Title = "Рецепти",
                    Route = AppRoutes.Prescriptions.List,
                    Icon = AppIcons.Prescription,
                    Module = Domain.Entities.Modules.Inventory,
                    RequiredPermissions = new() { Domain.Entities.Modules.Inventory }
                },
                new()
                {
                    Title = "Регистар на Лекови",
                    Route = AppRoutes.Medicines.List,
                    Icon = AppIcons.Prescription,
                    Module = Domain.Entities.Modules.Inventory,
                    RequiredPermissions = new() { Domain.Entities.Modules.Inventory }
                }
            }
        },

        // 7. ИЗВЕШТАИ ГРУПА
        new()
        {
            GroupTitle = "Извештаи",
            Icon = AppGroupIcons.Report,
            Module = Domain.Entities.Modules.Reports,
            RequiredPermissions = new() { Domain.Entities.Modules.Reports },
            Items = new()
            {
                new()
                {
                    Title = "Генератор на извештаи",
                    Route = AppRoutes.Reports.List,
                    Icon = AppIcons.Report,
                    Module = Domain.Entities.Modules.Reports,
                    RequiredPermissions = new() { Domain.Entities.Modules.Reports }
                }
            }
        },

        // 8. ДОКУМЕНТИ / ЛАБОРАТОРИЈА (Врзано со Patients модулот, бидејќи тоа се историски медицински податоци на пациенти)
        new()
        {
            GroupTitle = "Документи и Резултати",
            Icon = AppGroupIcons.Laboratory,
            Module = Domain.Entities.Modules.Patients,
            RequiredPermissions = new() { Domain.Entities.Modules.Patients },
            Items = new()
            {
                new()
                {
                    Title = "Упат",
                    Route = "labresults",
                    Icon = AppIcons.Lab,
                    Module = Domain.Entities.Modules.Patients,
                    RequiredPermissions = new() { Domain.Entities.Modules.Patients }
                },
                new()
                {
                    Title = "Лабораториски резултати",
                    Route = "labresults",
                    Icon = AppIcons.Lab,
                    Module = Domain.Entities.Modules.Patients,
                    RequiredPermissions = new() { Domain.Entities.Modules.Patients }
                },

                new()
                {
                    Title = "Потврди",
                    Route = "labresults",
                    Icon = AppIcons.Lab,
                    Module = Domain.Entities.Modules.Patients,
                    RequiredPermissions = new() { Domain.Entities.Modules.Patients }
                },

                new()
                {
                    Title = "Општ Документ",
                    Route = "labresults",
                    Icon = AppIcons.Lab,
                    Module = Domain.Entities.Modules.Patients,
                    RequiredPermissions = new() { Domain.Entities.Modules.Patients }
                }
            }
        },

        // 9. АДМИНИСТРАЦИЈА ГРУПА
        new()
        {
            GroupTitle = "Администрација",
            Icon = AppGroupIcons.Administration,
            Module = Domain.Entities.Modules.Administration,
            RequiredPermissions = new() { Domain.Entities.Modules.Administration },
            Items = new()
            {
                new()
                {
                    Title = "Кориснички сметки",
                    Route = AppRoutes.Users.List,
                    Icon = AppIcons.Users,
                    Module = Domain.Entities.Modules.Administration,
                    RequiredPermissions = new() { Domain.Entities.Modules.Administration }
                },
                new()
                {
                    Title = "Контрола на Улоги",
                    Route = AppRoutes.Users.Roles,
                    Icon = AppIcons.Roles,
                    Module = Domain.Entities.Modules.Administration,
                    RequiredPermissions = new() { Domain.Entities.Modules.Administration }
                }
            }
        }
    };
}