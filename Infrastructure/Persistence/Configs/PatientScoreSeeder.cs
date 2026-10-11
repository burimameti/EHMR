using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>
/// Seeds both patient-card scores (EncounterId = null) and visit-specific scores.
/// Encounter scores are unique per encounter; patient-level scores are independently
/// identified by their fixed seed IDs and may coexist for the same patient.
/// </summary>
public sealed class PatientScoreSeeder : IEntitySeeder
{
    public int Order => 57;

    private static readonly (Guid Id, Guid PatientId, Guid? EncounterId, string ScoreName, string Number, int DaysOffset)[] Scores =
    [
        // Patient-card history: these rows are not linked to an encounter.
        (Guid.Parse("00000000-0000-0000-0000-000000009111"), SeedIds.Patient3,
            null, "DAS28", "5.8", -300),
        // Each encounter owns its own score value.
        (Guid.Parse("00000000-0000-0000-0000-000000009101"), SeedIds.Patient3,
            Guid.Parse("00000000-0000-0000-0000-000000008001"), "DAS28", "7", -240),
        (Guid.Parse("00000000-0000-0000-0000-000000009102"), SeedIds.Patient3,
            Guid.Parse("00000000-0000-0000-0000-000000008002"), "DAS28", "6", -150),
        (Guid.Parse("00000000-0000-0000-0000-000000009103"), SeedIds.Patient3,
            Guid.Parse("00000000-0000-0000-0000-000000008003"), "DAS28", "5", -60),
        (Guid.Parse("00000000-0000-0000-0000-000000009104"), SeedIds.Patient3,
            Guid.Parse("00000000-0000-0000-0000-000000008004"), "DAS28", "3", -14),
        (Guid.Parse("00000000-0000-0000-0000-000000009105"), SeedIds.Patient1,
            SeedIds.Encounter1, "DAS28", "8", -90)
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var encounterIds = Scores
            .Where(x => x.EncounterId.HasValue)
            .Select(x => x.EncounterId!.Value)
            .Distinct()
            .ToArray();

        var existingEncounterIds = await context.PatientScores
            .Where(x => x.EncounterId.HasValue && encounterIds.Contains(x.EncounterId.Value))
            .Select(x => x.EncounterId!.Value)
            .ToListAsync(ct);

        var existingSeedIds = await context.PatientScores
            .Where(x => Scores.Select(s => s.Id).Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var rows = Scores
            .Where(x => x.EncounterId.HasValue
                ? !existingEncounterIds.Contains(x.EncounterId.Value)
                : !existingSeedIds.Contains(x.Id))
            .Where(x => !existingSeedIds.Contains(x.Id))
            .Select(x => new PatientScore
            {
                Id = x.Id,
                PatientId = x.PatientId,
                EncounterId = x.EncounterId,
                ScoreText = x.ScoreName,
                Number = x.Number,
                RecordedAt = DateTime.UtcNow.AddDays(x.DaysOffset)
            })
            .ToList();

        if (rows.Count == 0)
            return;

        await context.PatientScores.AddRangeAsync(rows, ct);
        await context.SaveChangesAsync(ct);
    }
}
