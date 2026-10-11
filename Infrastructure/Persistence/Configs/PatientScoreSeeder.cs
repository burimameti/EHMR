using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>One patient-card score and one score for every completed demo encounter.</summary>
public sealed class PatientScoreSeeder : IEntitySeeder
{
    public int Order => 57;

    private sealed record Seed(Guid Id, Guid PatientId, Guid? EncounterId, string Name, string Value, int DaysAgo);

    private static readonly Seed[] Seeds =
    [
        new(Guid.Parse("00000000-0000-0000-0000-000000009111"), SeedIds.Patient3, null, "DAS28", "5.8", 58),
        new(Guid.Parse("00000000-0000-0000-0000-000000009101"), SeedIds.Patient2, Guid.Parse("00000000-0000-0000-0000-000000008001"), "DAS28", "6.2", 52),
        new(Guid.Parse("00000000-0000-0000-0000-000000009102"), SeedIds.Patient2, Guid.Parse("00000000-0000-0000-0000-000000008002"), "DAS28", "5.7", 21),
        new(Guid.Parse("00000000-0000-0000-0000-000000009103"), SeedIds.Patient3, Guid.Parse("00000000-0000-0000-0000-000000008003"), "DAS28", "7.0", 58),
        new(Guid.Parse("00000000-0000-0000-0000-000000009104"), SeedIds.Patient3, Guid.Parse("00000000-0000-0000-0000-000000008004"), "DAS28", "6.1", 44),
        new(Guid.Parse("00000000-0000-0000-0000-000000009105"), SeedIds.Patient3, Guid.Parse("00000000-0000-0000-0000-000000008005"), "DAS28", "5.4", 29),
        new(Guid.Parse("00000000-0000-0000-0000-000000009106"), SeedIds.Patient3, Guid.Parse("00000000-0000-0000-0000-000000008006"), "DAS28", "4.8", 12)
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        // Remove the legacy second patient-card demo score and old seeded score rows.
        var legacyIds = new[]
        {
            Guid.Parse("00000000-0000-0000-0000-000000009112"),
            Guid.Parse("00000000-0000-0000-0000-000000009105")
        };
        var legacy = await context.PatientScores.Where(x => legacyIds.Contains(x.Id)).ToListAsync(ct);
        if (legacy.Count > 0)
        {
            context.PatientScores.RemoveRange(legacy);
            await context.SaveChangesAsync(ct);
        }

        foreach (var seed in Seeds)
        {
            var row = await context.PatientScores.FirstOrDefaultAsync(x => x.Id == seed.Id, ct);
            if (row == null && seed.EncounterId.HasValue)
                row = await context.PatientScores.FirstOrDefaultAsync(x => x.EncounterId == seed.EncounterId, ct);

            if (row == null)
            {
                row = new PatientScore { Id = seed.Id };
                context.PatientScores.Add(row);
            }

            row.PatientId = seed.PatientId;
            row.EncounterId = seed.EncounterId;
            row.ScoreText = seed.Name;
            row.Number = seed.Value;
            row.RecordedAt = DateTime.UtcNow.Date.AddDays(-seed.DaysAgo);
        }

        await context.SaveChangesAsync(ct);
    }
}
