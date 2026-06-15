using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class DiagnosisSeeder : IEntitySeeder
    {
        public int Order => 52;
        // AFTER EncounterSeeder (51) AND PatientSeeder (10)

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Set<Diagnosis>().AnyAsync(ct))
                return;

            var patient = await context.Set<Patient>()
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            var encounter = await context.Set<Encounter>()
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            if(patient==null||encounter==null)
                throw new InvalidOperationException(
                    "DiagnosisSeeder requires Patient and Encounter to be seeded first.");

            context.Set<Diagnosis>().AddRange(
                new Diagnosis
                {
                    Id=Guid.NewGuid(),
                    PatientId=patient.Id,
                    EncounterId=encounter.Id,

                    Code="C91.0",
                    System="ICD-10",
                    ClinicalDescription="Acute lymphoblastic leukemia confirmed via hematology panel",
                    Severity="Severe",
                    IsPrimary=true,
                    DiagnosedAt=DateTime.UtcNow
                },
                new Diagnosis
                {
                    Id=Guid.NewGuid(),
                    PatientId=patient.Id,
                    EncounterId=encounter.Id,

                    Code="D50.9",
                    System="ICD-10",
                    ClinicalDescription="Iron deficiency anemia secondary to chronic condition",
                    Severity="Moderate",
                    IsPrimary=false,
                    DiagnosedAt=DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
            Console.WriteLine("Seeding Diagnosis...");
        }
    }
}