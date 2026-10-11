using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class PatientMedicineSeeder : IEntitySeeder
{
    public int Order => 9;

    private static readonly (Guid PatientId, Guid MedicineId, string Dosage, string Frequency, bool Active)[] Assignments =
    [
        (SeedIds.Patient1, SeedIds.Med5, "1 таблета од 850mg", "Неделно", true),
        (SeedIds.Patient1, SeedIds.Med6, "1 таблета од 5mg", "Месечно", true),
        (SeedIds.Patient2, SeedIds.Med7, "1 капсула од 20mg", "На 2 месеци", true),
        (SeedIds.Patient2, SeedIds.Med4, "1 доза според план", "Двонеделно", true),
        (SeedIds.Patient3, SeedIds.Med9, "3 таблети одеднаш (7.5mg вкупно)", "На 3 месеци", true),
        (SeedIds.Patient3, SeedIds.Med8, "1 таблета според план", "На 4 месеци", true),
        (SeedIds.Patient4, SeedIds.Med10, "1 доза според план", "На 5 месеци", true),
        (SeedIds.Patient4, SeedIds.Med3, "1 доза според план", "Тринеделно", true),
        (SeedIds.Patient5, SeedIds.Med2, "1 доза според план", "На 6 месеци", false)
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var regimes = await context.ApplicationRegimes.Where(x => x.IsActive).ToListAsync(ct);
        if (regimes.Count == 0)
            return;

        // ApplicationRegime is the administration route, while DosingFrequency is the interval.
        var oralId = regimes.FirstOrDefault(x =>
            string.Equals(x.Regime, "Орално", StringComparison.OrdinalIgnoreCase))?.Id
            ?? regimes[0].Id;

        var patientIds = new[] { SeedIds.Patient1, SeedIds.Patient2, SeedIds.Patient3, SeedIds.Patient4, SeedIds.Patient5 };
        var existing = await context.PatientMedicines
            .Where(x => patientIds.Contains(x.PatientId) && x.EncounterId == null)
            .ToListAsync(ct);

        var desiredKeys = Assignments.Select(x => (x.PatientId, x.MedicineId)).ToHashSet();

        // Reconcile legacy patient-card seed rows without touching encounter administrations.
        var obsolete = existing.Where(x => !desiredKeys.Contains((x.PatientId, x.MedicineId))).ToList();
        if (obsolete.Count > 0)
            context.PatientMedicines.RemoveRange(obsolete);

        foreach (var seed in Assignments)
        {
            var row = existing.FirstOrDefault(x =>
                x.PatientId == seed.PatientId && x.MedicineId == seed.MedicineId);
            if (row is null)
            {
                row = new PatientMedicine
                {
                    Id = Guid.Parse($"00000000-0000-0000-0000-{(920000 + Array.IndexOf(Assignments, seed)):D12}"),
                    PatientId = seed.PatientId,
                    MedicineId = seed.MedicineId
                };
                context.PatientMedicines.Add(row);
            }

            row.ApplicationRegimeId = oralId;
            row.Dosage = seed.Dosage;
            row.DosingFrequency = seed.Frequency;
            row.Quantity = 0m;
            row.IsActive = seed.Active;
        }

        await context.SaveChangesAsync(ct);
    }
}
