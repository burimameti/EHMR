using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using static EHMR.Domain.Entities.User;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class UserSeeder : IEntitySeeder
    {
        public int Order => 1;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            var exists = await context.Users.AnyAsync(ct);
            if(exists)
                return;

            context.Users.AddRange(
                new User
                {
                    Id=SeedIds.AdminUser,
                    Username="admin",
                    PasswordHash="HASH1",
                    FirstName="Игор",
                    LastName="Ангеловски",
                    Role=UserRole.Admin,
                    Position=UserPosition.SuperAdmin,
                    IsActive=true
                },
                new User
                {
                    Id=SeedIds.DocUser1,
                    Username="dr.mitrev",
                    PasswordHash="HASH2",
                    FirstName="Никола",
                    LastName="Митрев",
                    Role=UserRole.Doctor,
                    Position=UserPosition.Regular,
                    IsActive=true
                },
                new User
                {
                    Id=SeedIds.DocUser2,
                    Username="nurse.test",
                    PasswordHash="HASH3",
                    FirstName="Тест",
                    LastName="Сестра",
                    Role=UserRole.MainNurse,
                    Position=UserPosition.Regular,
                    IsActive=true
                }
            );
            context.UserModules.AddRange(
    new UserModule
    {
        UserId=SeedIds.AdminUser,
        ModuleKey=Modules.Administration,
        IsEnabled=true
    },
    new UserModule
    {
        UserId=SeedIds.AdminUser,
        ModuleKey=Modules.Patients,
        IsEnabled=true
    },
    new UserModule
    {
        UserId=SeedIds.DocUser1,
        ModuleKey=Modules.Patients,
        IsEnabled=true
    },
    new UserModule
    {
        UserId=SeedIds.DocUser1,
        ModuleKey=Modules.Therapy,
        IsEnabled=true
    },
    new UserModule
    {
        UserId=SeedIds.DocUser2,
        ModuleKey=Modules.Therapy,
        IsEnabled=true
    }
);
            await context.SaveChangesAsync(ct);
        }
    }
}