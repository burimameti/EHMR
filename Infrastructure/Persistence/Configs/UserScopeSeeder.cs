using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    // =====================================================
    // 8. SECURITY SCOPE
    // =====================================================

    public class UserScopeSeeder : IEntitySeeder
    {
        public int Order => 80;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Set<UserScope>().AnyAsync(ct))
                return;

            context.Set<UserScope>().AddRange(

    new UserScope
    {
        Id=SeedIds.Rule1,
        UserId=SeedIds.AdminUser,
        ScopeType="Admin",

        TargetId=SeedIds.Tenant,
        IsPrimaryScope=true
    },
    new UserScope
    {
        Id=SeedIds.Rule2,
        UserId=SeedIds.DocUser1,
        ScopeType="Hospital",

        TargetId=SeedIds.Tenant,
        IsPrimaryScope=true
    },
    new UserScope
    {
        Id=SeedIds.Rule3,
        UserId=SeedIds.DocUser2,
        ScopeType="Department",
        //Permissions=new List<string> { "ManagePrescriptions" },
        TargetId=SeedIds.Tenant,
        IsPrimaryScope=true
    }
);

            Console.WriteLine("Seeding Scopes...");
        }
    }
}