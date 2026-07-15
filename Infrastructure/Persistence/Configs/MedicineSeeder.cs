using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class MedicineSeeder : IEntitySeeder
{
    public int Order => 4;

    public async Task SeedAsync(
        DesktopTherapyDbContext context,
        CancellationToken ct = default)
    {
        if(await context.Medicines.AnyAsync(ct))
            return;

        var medicines = new List<Medicine>
        {
            new()
            {
                Id = SeedIds.Med1,
                Name = "Парацетамол",
                GenericName = "Paracetamol",
                Code = "N02BE01",
                DosageForm = "Таблета",
                Strength = 500,
                Unit = "mg",
                DefaultDosage = "1 таблета по потреба на секои 6-8 часа",
                Manufacturer = "Алкалоид",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Med2,
                Name = "Ибупрофен",
                GenericName = "Ibuprofen",
                Code = "M01AE01",
                DosageForm = "Таблета",
                Strength = 400,
                Unit = "mg",
                DefaultDosage = "1 таблета двапати дневно по јадење",
                Manufacturer = "Хемофарм",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Med3,
                Name = "Амоксицилин",
                GenericName = "Amoxicillin",
                Code = "J01CA04",
                DosageForm = "Капсула",
                Strength = 500,
                Unit = "mg",
                DefaultDosage = "1 капсула трипати дневно",
                Manufacturer = "Sandoz",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Med4,
                Name = "Азитромицин",
                GenericName = "Azithromycin",
                Code = "J01FA10",
                DosageForm = "Таблета",
                Strength = 500,
                Unit = "mg",
                DefaultDosage = "1 таблета еднаш дневно (3 дена)",
                Manufacturer = "Pfizer",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Med5,
                Name = "Метформин",
                GenericName = "Metformin",
                Code = "A10BA02",
                DosageForm = "Таблета",
                Strength = 850,
                Unit = "mg",
                DefaultDosage = "1 таблета двапати дневно со оброк",
                Manufacturer = "Merck",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Med6,
                Name = "Амлодипин",
                GenericName = "Amlodipine",
                Code = "C08CA01",
                DosageForm = "Таблета",
                Strength = 5,
                Unit = "mg",
                DefaultDosage = "1 таблета еднаш дневно",
                Manufacturer = "KRKA",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Med7,
                Name = "Омепразол",
                GenericName = "Omeprazole",
                Code = "A02BC01",
                DosageForm = "Капсула",
                Strength = 20,
                Unit = "mg",
                DefaultDosage = "1 капсула наутро пред јадење",
                Manufacturer = "Алкалоид",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Med8,
                Name = "Инсулин гларгин",
                GenericName = "Insulin Glargine",
                Code = "A10AE04",
                DosageForm = "Инјекција",
                Strength = 100,
                Unit = "IU/ml",
                DefaultDosage = "Еднаш дневно според препорака на лекар",
                Manufacturer = "Sanofi",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Med9,
                Name = "Метотрексат",
                GenericName = "Methotrexate",
                Code = "L04AX03",
                DosageForm = "Таблета",
                Strength = 2.5m,
                Unit = "mg",
                DefaultDosage = "Еднаш неделно според препорака на лекар",
                Manufacturer = "Pfizer",
                IsActive = true
            },

            new()
            {
                Id = SeedIds.Med10,
                Name = "Цисплатин",
                GenericName = "Cisplatin",
                Code = "L01XA01",
                DosageForm = "Интравенска инфузија",
                Strength = 75,
                Unit = "mg/m²",
                DefaultDosage = "Интравенски според онколошки протокол",
                Manufacturer = "Accord Healthcare",
                IsActive = true
            }
        };

        await context.Medicines.AddRangeAsync(medicines, ct);
        await context.SaveChangesAsync(ct);
    }
}