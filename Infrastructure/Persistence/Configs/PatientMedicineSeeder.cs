using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EHMR.Domain.Entities;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class PatientMedicineSeeder : IEntitySeeder
{
    public int Order => 9; // По уфрлање на лекови и пациенти

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.PatientMedicines.AnyAsync(ct))
            return;

        var patientMedicines = new List<PatientMedicine>
        {
            // Пациент 1 терапија (Марјан - прима Метформин за шеќер и Амлодипин за притисок)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient1,
                MedicineId = SeedIds.Med5, // Метформин
                DosesFrequency = DosesFrequency.TwiceDaily,
                Dosage = "1 таблета од 850mg",
                StartDate = DateTime.UtcNow.AddMonths(-5),
                EndDate = null,
                Notes = "Да се зема строго за време на оброк.",
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient1,
                MedicineId = SeedIds.Med6, // Амлодипин
                DosesFrequency = DosesFrequency.Daily,
                Dosage = "1 таблета од 5mg",
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = null,
                Notes = "Редовна наутро за крвен притисок.",
                IsActive = true
            },

            // Пациент 2 терапија (Билјана - прима Омепразол за желудник)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient2,
                MedicineId = SeedIds.Med7, // Омепразол
                DosesFrequency = DosesFrequency.Daily,
                Dosage = "1 капсула од 20mg",
                StartDate = DateTime.UtcNow.AddDays(-14),
                EndDate = DateTime.UtcNow.AddDays(14), // Краткотрајна хронична терапија
                Notes = "Наутро на гладно, 30 минути пред појадок.",
                IsActive = true
            },

            // Пациент 3 терапија (Зоран - Онколошки пациент на Метотрексат)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient3,
                MedicineId = SeedIds.Med9, // Метотрексат
                DosesFrequency = DosesFrequency.Weekly,
                Dosage = "3 таблети одеднаш (7.5mg вкупно)",
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = null,
                Notes = "Да се зема исклучиво во Вторник. Потребна редовна крвна слика.",
                IsActive = true
            }
        };

        await context.PatientMedicines.AddRangeAsync(patientMedicines, ct);
        await context.SaveChangesAsync(ct);
    }
}