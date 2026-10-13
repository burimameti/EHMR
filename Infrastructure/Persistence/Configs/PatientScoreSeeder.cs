using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>Seeds only five demo scores, one per patient, to keep patient details uncluttered.</summary>
public sealed class PatientScoreSeeder : IEntitySeeder
{
    public int Order => 57;

    private sealed record Seed(Guid Id, Guid PatientId, Guid EncounterId, string Name, string Value, int DaysAgo);

    private static readonly Seed[] Seeds =
    [
        new(Guid.Parse("00000000-0000-0000-0000-000000009121"), SeedIds.Patient1, Guid.Parse("00000000-0000-0000-0000-000000008001"), "DAS28", "3.9", 51),
        new(Guid.Parse("00000000-0000-0000-0000-000000009122"), SeedIds.Patient2, Guid.Parse("00000000-0000-0000-0000-000000008002"), "DAS28", "5.7", 49),
        new(Guid.Parse("00000000-0000-0000-0000-000000009123"), SeedIds.Patient3, Guid.Parse("00000000-0000-0000-0000-000000008003"), "DAS28", "5.1", 47),
        new(Guid.Parse("00000000-0000-0000-0000-000000009124"), SeedIds.Patient4, Guid.Parse("00000000-0000-0000-0000-000000008004"), "DAS28", "4.6", 45),
        new(Guid.Parse("00000000-0000-0000-0000-000000009125"), SeedIds.Patient5, Guid.Parse("00000000-0000-0000-0000-000000008005"), "DAS28", "5.0", 43)
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        // Remove the older demo score batch, which populated several score rows per patient.
        var legacyIds = Enumerable.Range(9101, 16)
            .Select(number => Guid.Parse($"00000000-0000-0000-0000-{number:000000000000}"))
            .ToArray();
        var legacyRows = await context.PatientScores
            .Where(x => legacyIds.Contains(x.Id))
            .ToListAsync(ct);
        if (legacyRows.Count > 0)
            context.PatientScores.RemoveRange(legacyRows);

        foreach (var seed in Seeds)
        {
            var row = await context.PatientScores.FirstOrDefaultAsync(x => x.Id == seed.Id, ct);
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
