using EHMR.Domain.Entities.Rbac;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

public class UserScopeSeeder : IEntitySeeder
{
    public int Order => 3;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.UserScopes.AnyAsync(ct))
            return;

        var scopes = new List<UserScope>
        {
            new()
            {
                Id = SeedIds.AdminScope,
                UserId = SeedIds.AdminUser,
                ScopeType = ScopeType.Admin,
                TargetId = SeedIds.Tenant,
            },
              new()
            {
                Id = SeedIds.SuperAdminUser,
                UserId = SeedIds.SuperAdminUser,
                ScopeType = ScopeType.Admin,
                TargetId = SeedIds.Tenant,
            },
            new()
            {
                Id = SeedIds.Doctor2Scope,
                UserId = SeedIds.DocUser2,
                ScopeType =  ScopeType.Department,
                TargetId = SeedIds.Tenant,
            }
        };

        await context.UserScopes.AddRangeAsync(scopes, ct);
        await context.SaveChangesAsync(ct);
    }
}