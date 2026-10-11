using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>
/// Appointments for the demo patients are seeded together with their matching encounters
/// by ClinicalScenarioSeeder. Kept as a registered seeder for compatibility with the
/// existing seeder registration.
/// </summary>
public sealed class AppointmentSeeder : IEntitySeeder
{
    public int Order => 50;

    public Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        // Avoid adding the old, unrelated appointment set. ClinicalScenarioSeeder (Order 56)
        // owns the deterministic ten-appointment demo scenario.
        return Task.CompletedTask;
    }
}
