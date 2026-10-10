using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;
namespace EHMR.Infrastructure.Persistence.Configs;

public class EncounterSeeder : IEntitySeeder
{
    public int Order => 55;
    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {

        var existingEncounter = await context.Encounters
            .FirstOrDefaultAsync(x => x.EncounterNumber=="PREG-20260627-0001", ct);
        if(existingEncounter is not null)
        {
            await SeedEncounterMedicinesAsync(context, existingEncounter, ct);
            return;
        }
        var visitDate = new DateTime(2026, 06, 27);
        var encounter = new Encounter
        {
            Id=SeedIds.Encounter1,

            AppointmentId=SeedIds.Appt1,
            PatientId=SeedIds.Patient1,
            DoctorId=SeedIds.Doctor1,
            EncounterNumber="PREG-20260627-0001",
            EncounterType="Outpatient",
            Status=EncounterStatus.InProgress,
            Priority="Routine",
            ScheduledStart=visitDate.AddHours(9),
            ScheduledEnd=visitDate.AddHours(9).AddMinutes(30),
            CheckInTime=visitDate.AddHours(8).AddMinutes(54),
            StartTime=visitDate.AddHours(9).AddMinutes(2),
            EndTime=visitDate.AddHours(9).AddMinutes(24),
            CheckOutTime=visitDate.AddHours(9).AddMinutes(31),
            DurationMinutes=22,
            EncounterDate=visitDate.AddHours(9),
            ChiefComplaint="Рутинска контрола на хипертензија.",
            ReasonForVisit="Тримесечна евалуација по прилагодување на терапијата.",

            HistoryOfPresentIllness=
                "Пациентот пријавува подобрени домашни мерења на крвен притисок, во просек 126/78 mmHg. Негира главоболки, вртоглавица, болка во градите, палпитации или отежнато дишење. Пријавува добра придржаност кон терапијата, без несакани ефекти.",
            Assessment=
                "Есенцијалната хипертензија останува добро контролирана. Нема докази за кардиоваскуларни компликации. Пациентот покажува добра соработливост во текот на третманот.",
            Plan=
                "Продолжи со Losartan 50 mg еднаш дневно. Се препорачува DASH дијета, редовна физичка активност и домашно мерење на крвен притисок. Контролен преглед за три месеци, или порано доколку се јават симптоми.",
            Notes=
                "Пациентот пристигна навреме, буден, соработлив, без акутен дистрес. Физикалниот преглед беше во нормални граници. Терапискиот режим е разгледан. Дадени се совети за начин на живот.",
            //            ClinicalNotes=
            //@"SUBJECTIVE
            //Patient feels well with no new complaints.
            //OBJECTIVE
            //Blood pressure stable.
            //Heart rate regular.
            //No edema.
            //Physical examination unremarkable.
            //ASSESSMENT
            //Controlled essential hypertension.
            //PLAN
            //Continue current therapy.
            //Follow-up in 3 months.
            //Patient educated regarding diet, exercise, and medication adherence.",
            // =====================================================
            // Administrative
            // =====================================================
            VisitSource="Appointment",

            IsLocked=true,
            IsActive=true,
            CreatedAt=visitDate.AddHours(9),
            UpdatedAt=visitDate.AddHours(9).AddMinutes(35),
            CreatedBy=SeedIds.AdminUser,
            UpdatedBy=SeedIds.AdminUser
        };
        try
        {
            context.Encounters.Add(encounter);
            await context.SaveChangesAsync(ct);

            await SeedEncounterMedicinesAsync(context, encounter, ct);
        }
        catch(Exception ex)
        {
            throw new Exception("Encounter seeding failed", ex);
        }
    }

    private static async Task SeedEncounterMedicinesAsync(
        DesktopTherapyDbContext context,
        Encounter encounter,
        CancellationToken ct)
    {
        var assignments = await context.PatientMedicines
            .Where(x => x.PatientId == encounter.PatientId
                && x.EncounterId == null
                && x.IsActive)
            .ToListAsync(ct);
        var existingRows = await context.PatientMedicines
            .Where(x => x.EncounterId == encounter.Id)
            .ToListAsync(ct);

        foreach(var assignment in assignments)
        {
            var quantity = assignment.MedicineId == SeedIds.Med5 ? 2m : 0m;
            var existing = existingRows.FirstOrDefault(x => x.MedicineId == assignment.MedicineId);
            if(existing is not null)
            {
                existing.Quantity = quantity;
                existing.ApplicationRegimeId = assignment.ApplicationRegimeId;
           
                existing.Dosage = assignment.Dosage;
       
                existing.IsActive = true;
                continue;
            }

            context.PatientMedicines.Add(new PatientMedicine
            {
                Id = Guid.NewGuid(),
                PatientId = assignment.PatientId,
                EncounterId = encounter.Id,
                MedicineId = assignment.MedicineId,
                ApplicationRegimeId = assignment.ApplicationRegimeId,
        
                Dosage = assignment.Dosage,
            
                IsActive = true,
                Quantity = quantity
            });
        }

        await context.SaveChangesAsync(ct);
    }
}