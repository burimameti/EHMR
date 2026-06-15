using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class DoctorSeeder : IEntitySeeder
    {
        public int Order => 2;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Set<Doctor>().AnyAsync(ct))
                return;

            context.Set<Doctor>().AddRange(
                new Doctor
                {
                    Id=SeedIds.Doctor1,
                    UserId=SeedIds.DocUser1,
                    FirstName="Никола",
                    LastName="Митрев",
                    LicenseNumber="LM-001",
                    Specialty="Онкологија",
                    ContactPhone="+38970000001",
                    IsActive=true
                },
                new Doctor
                {
                    Id=SeedIds.Doctor2,
                    UserId=SeedIds.DocUser2,
                    FirstName="Тест",
                    LastName="Тест",
                    LicenseNumber="LM-002",
                    Specialty="Хематологија",
                    ContactPhone="+38970000002",
                    IsActive=true
                }
            ); await context.SaveChangesAsync();
        }
    }
}