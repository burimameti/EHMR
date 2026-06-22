using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using static EHMR.Domain.Entities.Rbac.AppRoutes;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public class PrescriptionSeeder : IEntitySeeder
{
    public int Order => 59;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Prescriptions.AnyAsync(ct))
            return;

        var prescriptions = new List<Prescription>
        {
            new Prescription
            {
                Id = SeedIds.Presc1,
                PatientId = SeedIds.Patient1,
                Dosage= "1 таблетка дневно",
                Medication = "Парацетамол",
                Instructions = "При болка или температура над 38 градуса",
                UpdatedAt = DateTime.UtcNow,
                EncounterId = SeedIds.Encounter1,
                CreatedAt = DateTime.UtcNow
            }
        };
        try
        {
            await context.Prescriptions.AddRangeAsync(prescriptions);
            await context.SaveChangesAsync(ct);
        }
        catch(Exception ex)
        {
            Console.Write($"Error Occured {ex.Message}");
        }
    }
}