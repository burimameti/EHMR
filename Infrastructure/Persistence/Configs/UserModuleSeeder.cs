using EHMR.Domain.Entities.Rbac;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class UserModuleSeeder : IEntitySeeder
    {
        public int Order => 2;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            var modules = new List<Module>();

            // ADMIN FULL ACCESS
            modules.AddRange(new[]
            {
            new Module { UserId = SeedIds.AdminUser, ModuleKey = Modules.Administration, IsEnabled = true },
            new Module { UserId = SeedIds.AdminUser, ModuleKey = Modules.Patients, IsEnabled = true },
                        new Module { UserId = SeedIds.AdminUser, ModuleKey = Modules.BackupDashboard, IsEnabled = true },
            new Module { UserId = SeedIds.AdminUser, ModuleKey = Modules.Reports, IsEnabled = true }
        });

            // DOCTORS DEFAULT ACCESS
            var doctors = new[]
            {
            SeedIds.DocUser1, SeedIds.DocUser2, SeedIds.DocUser3, SeedIds.DocUser4, SeedIds.DocUser5,
            SeedIds.DocUser6, SeedIds.DocUser7, SeedIds.DocUser8, SeedIds.DocUser9, SeedIds.DocUser10
        };

            foreach(var doc in doctors)
            {
                modules.Add(new Module { UserId=doc, ModuleKey=Modules.Patients, IsEnabled=true });
                modules.Add(new Module { UserId=doc, ModuleKey=Modules.Appointments, IsEnabled=true });
            }

            // NURSE — patient and appointment workflows replace the retired therapy-cycle module.
            modules.Add(new Module { UserId = SeedIds.NurseUser, ModuleKey = Modules.Patients, IsEnabled = true });
            modules.Add(new Module { UserId = SeedIds.NurseUser, ModuleKey = Modules.Appointments, IsEnabled = true });

            var existingAssignments = await context.UserModules
                .Select(x => new { x.UserId, x.ModuleKey })
                .ToListAsync(ct);

            var missingAssignments = modules
                .Where(candidate => !existingAssignments.Any(existing =>
                    existing.UserId == candidate.UserId
                    && existing.ModuleKey == candidate.ModuleKey))
                .ToList();

            if(missingAssignments.Count == 0)
                return;

            await context.UserModules.AddRangeAsync(missingAssignments, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}