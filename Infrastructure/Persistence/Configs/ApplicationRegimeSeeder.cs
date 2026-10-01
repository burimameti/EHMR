using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class ApplicationRegimeSeeder : IEntitySeeder
{
    public int Order => 5;

    private static readonly string[] Regimes =
    [
        "Орално",
        "Поткожно",
        "Интравенски",
        "Интрамускулно",
        "Интраартикуларно",
        "Интраназално",
        "Топикално",
        "Сублингвално"
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var existing = await context.ApplicationRegimes
            .ToListAsync(ct);

        var missing = Regimes
            .Where(name => !existing.Any(x =>
                string.Equals(x.Regime, name, StringComparison.OrdinalIgnoreCase)))
            .Select(name => new ApplicationRegime
            {
                Id = Guid.NewGuid(),
                Regime = name,
                IsActive = true
            })
            .ToList();

        if (missing.Count == 0)
            return;

        await context.ApplicationRegimes.AddRangeAsync(missing, ct);
        await context.SaveChangesAsync(ct);
    }
}
