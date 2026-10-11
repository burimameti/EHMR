using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>
/// The demo encounter set is seeded alongside its appointments by ClinicalScenarioSeeder.
/// This compatibility seeder intentionally no longer creates a separate orphan encounter.
/// </summary>
public sealed class EncounterSeeder : IEntitySeeder
{
    public int Order => 55;

    public Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        => Task.CompletedTask;
}
