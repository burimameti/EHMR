using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class DocumentSeeder : IEntitySeeder
    {
        public int Order => 66;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Set<PatientDocument>().AnyAsync(ct))
                return;

            context.Set<PatientDocument>().Add(
                new PatientDocument
                {
                    PatientId=SeedIds.Patient1,
                    EncounterId=SeedIds.Encounter1,
                    Title="initial_report.pdf",
                    FileUrl="/documents/initial_report.pdf",
                    UploadedAt=DateTime.UtcNow
                }); await context.SaveChangesAsync();
        }
    }
}