
using Microsoft.EntityFrameworkCore;
using EHMR.Domain.Entities;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class PatientMedicineSeeder : IEntitySeeder
{
    public int Order => 9; 

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var applicationRegimes = await context.ApplicationRegimes
            .Where(x => x.IsActive)
            .ToListAsync(ct);

        if(applicationRegimes.Count == 0)
            return;

        var oral = applicationRegimes.FirstOrDefault(x => x.Regime == "Орално")?.Id;
        var subcutaneous = applicationRegimes.FirstOrDefault(x => x.Regime == "Поткожно")?.Id;

        var existingMedicines = await context.PatientMedicines
            .Where(x => x.ApplicationRegimeId == null)
            .ToListAsync(ct);

        foreach(var item in existingMedicines)
            item.ApplicationRegimeId = item.DosesFrequency == DosesFrequency.Weekly ? subcutaneous : oral;

        if(existingMedicines.Count > 0)
            await context.SaveChangesAsync(ct);

        if(await context.PatientMedicines.AnyAsync(ct))
        {
            // Seed-managed demo assignments have zero on the patient card;
            // encounter administrations are stored separately per visit.
            var demoAssignments = await context.PatientMedicines
                .Where(x => x.EncounterId == null && (
                    (x.PatientId == SeedIds.Patient1 && (x.MedicineId == SeedIds.Med5 || x.MedicineId == SeedIds.Med6))
                    || (x.PatientId == SeedIds.Patient2 && x.MedicineId == SeedIds.Med7)
                    || (x.PatientId == SeedIds.Patient3 && x.MedicineId == SeedIds.Med9)))
                .ToListAsync(ct);
            foreach(var assignment in demoAssignments)
                assignment.Quantity = 0m;
            if(demoAssignments.Count > 0)
                await context.SaveChangesAsync(ct);
            return;
        }

        var patientMedicines = new List<PatientMedicine>
        {
            // Пациент 1 терапија (Марјан - прима Метформин за шеќер и Амлодипин за притисок)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient1,
                MedicineId = SeedIds.Med5, // Метформин
                ApplicationRegimeId = oral,
                DosesFrequency = DosesFrequency.TwiceDaily,
                Dosage = "1 таблета од 850mg",
                Notes = "Да се зема строго за време на оброк.",
                IsActive = true,
                Quantity = 0m
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient1,
                MedicineId = SeedIds.Med6, // Амлодипин
                ApplicationRegimeId = oral,
                DosesFrequency = DosesFrequency.Daily,
                Dosage = "1 таблета од 5mg",
                Notes = "Редовна наутро за крвен притисок.",
                IsActive = true
            },

            // Пациент 2 терапија (Билјана - прима Омепразол за желудник)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient2,
                MedicineId = SeedIds.Med7, // Омепразол
                ApplicationRegimeId = oral,
                DosesFrequency = DosesFrequency.Daily,
                Dosage = "1 капсула од 20mg",
                Notes = "Наутро на гладно, 30 минути пред појадок.",
                IsActive = true
            },

            // Пациент 3 терапија (Зоран - Онколошки пациент на Метотрексат)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient3,
                MedicineId = SeedIds.Med9, // Метотрексат
                ApplicationRegimeId = subcutaneous,
                DosesFrequency = DosesFrequency.Weekly,
                Dosage = "3 таблети одеднаш (7.5mg вкупно)",
                Notes = "Да се зема исклучиво во Вторник. Потребна редовна крвна слика.",
                IsActive = true
            }
        };

        await context.PatientMedicines.AddRangeAsync(patientMedicines, ct);
        await context.SaveChangesAsync(ct);
    }
}