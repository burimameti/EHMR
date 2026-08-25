using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Infrastructure.Persistence.Configs
{
    public class NotificationSeeder : IEntitySeeder
    {
        public int Order => 98;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Set<Notification>().AnyAsync(ct))
                return;

            var admin = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id==SeedIds.AdminUser, ct);

            if(admin==null)
                return;

            context.Set<Notification>().AddRange(
                new Notification
                {
                    Id=Guid.NewGuid(),
                    UserId=admin.Id,
                    Title="System initialized",
                    Message="ЕХМР Успешно стратуваше",
                    IsRead=false,
                    CreatedAt=DateTime.UtcNow
                },
                new Notification
                {
                    Id=Guid.NewGuid(),
                    UserId=admin.Id,
                    Title="Seed data loaded",
                    Message="Initial clinical dataset has been populated",
                    IsRead=false,
                    CreatedAt=DateTime.UtcNow
                }
            );
        }
    }
}