using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class PatientScoreSeeder : IEntitySeeder
{
    public int Order => 57;

    private static readonly (Guid Id, Guid PatientId, Guid EncounterId, string Score, int DaysOffset)[] Scores =
    [
        (Guid.Parse("00000000-0000-0000-0000-000000009101"), SeedIds.Patient13,
            Guid.Parse("00000000-0000-0000-0000-000000008001"), "7", -240),
        (Guid.Parse("00000000-0000-0000-0000-000000009102"), SeedIds.Patient13,
            Guid.Parse("00000000-0000-0000-0000-000000008002"), "6", -150),
        (Guid.Parse("00000000-0000-0000-0000-000000009103"), SeedIds.Patient13,
            Guid.Parse("00000000-0000-0000-0000-000000008003"), "5", -60),
        (Guid.Parse("00000000-0000-0000-0000-000000009104"), SeedIds.Patient13,
            Guid.Parse("00000000-0000-0000-0000-000000008004"), "3", -14),
        (Guid.Parse("00000000-0000-0000-0000-000000009105"), SeedIds.Patient1,
            SeedIds.Encounter1, "8", -90)
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var encounterIds = Scores.Select(x => x.EncounterId).ToArray();

        var existing = await context.PatientScores
            .Where(x => encounterIds.Contains(x.EncounterId))
            .Select(x => x.EncounterId)
            .ToListAsync(ct);

        var rows = Scores
            .Where(x => !existing.Contains(x.EncounterId))
            .Select(x => new PatientScore
            {
                Id = x.Id,
                PatientId = x.PatientId,
                EncounterId = x.EncounterId,
                ScoreText = x.Score,
                RecordedAt = DateTime.UtcNow.AddDays(x.DaysOffset)
            })
            .ToList();

        if (rows.Count == 0)
            return;

        await context.PatientScores.AddRangeAsync(rows, ct);
        await context.SaveChangesAsync(ct);
    }
}
