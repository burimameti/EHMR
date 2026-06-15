using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class AlertSeeder : IEntitySeeder
    {
        public int Order => 3;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Set<Alert>().AnyAsync(ct))
                return;

            context.Set<Alert>().AddRange(
                new Alert
                {
                    Id=Guid.NewGuid(),
                    Message="Low stock: Methotrexate",
                    Level=AlertLevel.Warning,
                    IsRead=false,
                    IsResolved=false
                },
                new Alert
                {
                    Id=Guid.NewGuid(),
                    Message="Cycle dose overdue detected",
                    Level=AlertLevel.Critical,
                    IsRead=false,
                    IsResolved=false
                }
            );
        }
    }
}