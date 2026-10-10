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
        var existing = await context.ApplicationRegimes.ToListAsync(ct);
        var changed = false;

        foreach(var name in Regimes)
        {
            var regime = existing.FirstOrDefault(x =>
                string.Equals(x.Regime, name, StringComparison.OrdinalIgnoreCase));

            if(regime is null)
            {
                context.ApplicationRegimes.Add(new ApplicationRegime
                {
                    Id = Guid.NewGuid(),
                    Regime = name,
                    IsActive = true
                });
                changed = true;
                continue;
            }

            // Keep canonical seed options selectable on every seed run.
            if(!regime.IsActive)
            {
                regime.IsActive = true;
                changed = true;
            }
        }

        if(changed)
            await context.SaveChangesAsync(ct);
    }
}
