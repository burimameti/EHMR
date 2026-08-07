using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Infrastructure.Persistence.Configs
{
    public class AlertSeeder : IEntitySeeder
    {
        public int Order => 88;

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