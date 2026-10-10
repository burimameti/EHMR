
using Microsoft.EntityFrameworkCore;
using EHMR.Domain.Entities;
using System;
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

        // ApplicationRegime is the route/method of administration, not frequency.
        var oral = applicationRegimes.FirstOrDefault(
            x => string.Equals(x.Regime, "Орално", StringComparison.OrdinalIgnoreCase))?.Id
            ?? applicationRegimes.FirstOrDefault()?.Id;
        var existingMedicines = await context.PatientMedicines
            .Where(x => x.ApplicationRegimeId == null)
            .ToListAsync(ct);

        foreach(var item in existingMedicines)
            item.ApplicationRegimeId ??= oral;

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

                Dosage = "1 таблета од 850mg",

                IsActive = true,
                Quantity = 0m
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient1,
                MedicineId = SeedIds.Med6, // Амлодипин
                ApplicationRegimeId = oral,

                Dosage = "1 таблета од 5mg",

                IsActive = true,
                Quantity = 0m
            },

            // Пациент 2 терапија (Билјана - прима Омепразол за желудник)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient2,
                MedicineId = SeedIds.Med7, // Омепразол
                ApplicationRegimeId = oral,

                Dosage = "1 капсула од 20mg",

                IsActive = true,
                Quantity = 0m
            },

            // Пациент 3 терапија (Зоран - Онколошки пациент на Метотрексат)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient3,
                MedicineId = SeedIds.Med9, // Метотрексат
                ApplicationRegimeId = oral,

                Dosage = "3 таблети одеднаш (7.5mg вкупно)",

                IsActive = true,
                Quantity = 0m
            }
        };

        await context.PatientMedicines.AddRangeAsync(patientMedicines, ct);
        await context.SaveChangesAsync(ct);
    }
}