using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class AuditLogSeeder : IEntitySeeder
    {
        public int Order => 99;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Set<AuditLog>().AnyAsync(ct))
                return;

            context.Set<AuditLog>().Add(
                new AuditLog
                {
                    Action="SYSTEM_INIT",
                    EntityName="System",
                    CreatedAt=DateTime.UtcNow,
                    Description="Initial system audit log entry"
                });
            await context.SaveChangesAsync();
        }
    }
}