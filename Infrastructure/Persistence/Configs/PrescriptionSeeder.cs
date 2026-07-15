using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EHMR.Domain.Entities;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;


namespace EHMR.Infrastructure.Persistence.Configs;

public sealed class PrescriptionSeeder : IEntitySeeder
{
    public int Order => 59; // Последен во овој синџир

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Prescriptions.AnyAsync(ct))
            return;

        var prescriptions = new List<Prescription>
        {
            // Рецепт за Пациент 1 (Итна акутна состојба за болка)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient1,
             
                EncounterId = null, // Може да се врзе со Encounter доколку има генерирано
                Medication = "Ибупрофен 400мг таблети",
                Dosage = "400mg",
                Instructions = "По потреба, максимум 3 пати на ден по јадење. Не на празен желудник.",
                Status = "Active", // Или соодветната вредност од вашиот домен/енум кој го средивме во базата
                IssuedDate = DateTime.UtcNow.AddDays(-2),
                ExpiryDate = DateTime.UtcNow.AddDays(28),
                Notes = "За ублажување на акутна болка во долниот дел на грбот."
            },

            // Рецепт за Пациент 2 (Антибиотик кој мора да се испие до крај)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient2,

                EncounterId = null,
                Medication = "Амоксицилин 500мг капсули",
                Dosage = "500mg",
                Instructions = "1 капсула на секои 8 часа (вкупно 10 дена). Да не се прекинува предвреме.",
                Status = "Active",
                IssuedDate = DateTime.UtcNow.AddDays(-4),
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                Notes = "Терапија за респираторна инфекција."
            },

            // Рецепт за Пациент 3 (Хроничен рецепт)
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = SeedIds.Patient3,

                EncounterId = null,
                Medication = "Инсулин гларгин (Lantus)",
                Dosage = "100 IU/ml",
                Instructions = "Апликација од 18 единици секоја вечер во исто време субкутано.",
                Status = "Active",
                IssuedDate = DateTime.UtcNow.AddMonths(-1),
                ExpiryDate = DateTime.UtcNow.AddMonths(2),
                Notes = "Подигнување на месечно ниво во матичната аптека."
            }
        };

        await context.Prescriptions.AddRangeAsync(prescriptions, ct);
        await context.SaveChangesAsync(ct);
    }
}