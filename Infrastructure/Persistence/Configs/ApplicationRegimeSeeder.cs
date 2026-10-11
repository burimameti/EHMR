using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class ApplicationRegimeSeeder : IEntitySeeder
{
    public int Order => 5;

    private static readonly string[] Regimes =
    [
        "Еднаш неделно",
        "Еднаш на две недели",
        "Еднаш на три недели",
        "Еднаш месечно",
        "Еднаш на два месеци",
        "Еднаш на три месеци",
        "Еднаш на четири месеци",
        "Еднаш на пет месеци",
        "Еднаш на шест месеци",
        "Еднаш годишно"
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var existing = await context.ApplicationRegimes.ToListAsync(ct);
        var changed = false;
        var legacyRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Орално", "Поткожно", "Интравенски", "Интрамускулно",
            "Интраартикуларно", "Интраназално", "Топикално", "Сублингвално"
        };

        foreach (var item in existing.Where(x => legacyRoutes.Contains(x.Regime) && x.IsActive))
        {
            item.IsActive = false;
            changed = true;
        }

        foreach (var name in Regimes)
        {
            var regime = existing.FirstOrDefault(x => string.Equals(x.Regime, name, StringComparison.OrdinalIgnoreCase));
            if (regime is null)
            {
                context.ApplicationRegimes.Add(new ApplicationRegime { Id = Guid.NewGuid(), Regime = name, IsActive = true });
                changed = true;
            }
            else if (!regime.IsActive)
            {
                regime.IsActive = true;
                changed = true;
            }
        }

        if (changed)
            await context.SaveChangesAsync(ct);
    }
}
