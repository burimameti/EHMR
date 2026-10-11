using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class PatientMedicineSeeder : IEntitySeeder
{
    public int Order => 9;

    private static readonly (Guid PatientId, Guid MedicineId, string Dosage, string Regime, bool Active)[] Assignments =
    [
        (SeedIds.Patient1, SeedIds.Med5, "1 таблета од 850mg", "Еднаш неделно", true),
        (SeedIds.Patient1, SeedIds.Med6, "1 таблета од 5mg", "Еднаш месечно", true),
        (SeedIds.Patient2, SeedIds.Med7, "1 капсула од 20mg", "Еднаш на два месеци", true),
        (SeedIds.Patient2, SeedIds.Med4, "1 доза според план", "Еднаш на две недели", true),
        (SeedIds.Patient3, SeedIds.Med9, "3 таблети одеднаш (7.5mg вкупно)", "Еднаш на три месеци", true),
        (SeedIds.Patient3, SeedIds.Med8, "1 таблета според план", "Еднаш на четири месеци", true),
        (SeedIds.Patient4, SeedIds.Med10, "1 доза според план", "Еднаш на пет месеци", true),
        (SeedIds.Patient4, SeedIds.Med3, "1 доза според план", "Еднаш на три недели", true),
        (SeedIds.Patient5, SeedIds.Med2, "1 доза според план", "Еднаш на шест месеци", false)
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var regimes = await context.ApplicationRegimes.Where(x => x.IsActive).ToListAsync(ct);
        var regimeIds = regimes.ToDictionary(x => x.Regime, x => x.Id, StringComparer.OrdinalIgnoreCase);
        var patientIds = new[] { SeedIds.Patient1, SeedIds.Patient2, SeedIds.Patient3, SeedIds.Patient4, SeedIds.Patient5 };
        var existing = await context.PatientMedicines
            .Where(x => patientIds.Contains(x.PatientId) && x.EncounterId == null)
            .ToListAsync(ct);

        var desiredKeys = Assignments.Select(x => (x.PatientId, x.MedicineId)).ToHashSet();
        var obsolete = existing.Where(x => !desiredKeys.Contains((x.PatientId, x.MedicineId))).ToList();
        if (obsolete.Count > 0)
            context.PatientMedicines.RemoveRange(obsolete);

        for (var i = 0; i < Assignments.Length; i++)
        {
            var seed = Assignments[i];
            if (!regimeIds.TryGetValue(seed.Regime, out var regimeId))
                continue;

            var row = existing.FirstOrDefault(x => x.PatientId == seed.PatientId && x.MedicineId == seed.MedicineId);
            if (row is null)
            {
                row = new PatientMedicine
                {
                    Id = Guid.Parse($"00000000-0000-0000-0000-{(920000 + i):D12}"),
                    PatientId = seed.PatientId,
                    MedicineId = seed.MedicineId
                };
                context.PatientMedicines.Add(row);
            }

            row.ApplicationRegimeId = regimeId;
            row.Dosage = seed.Dosage;
            row.Quantity = 0m;
            row.IsActive = seed.Active;
        }

        await context.SaveChangesAsync(ct);
    }
}
