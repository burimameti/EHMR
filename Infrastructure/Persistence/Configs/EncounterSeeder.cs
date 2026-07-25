using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public class EncounterSeeder : IEntitySeeder
{
    public int Order => 55;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
  
   var exists = await context.Encounters
        .AnyAsync(x => x.EncounterNumber == "ENC-20260627-0001", ct);

    if(exists)
            return;

        var visitDate = new DateTime(2026, 06, 27);

        var encounter = new Encounter
        {
            Id=SeedIds.Encounter1,
  
            AppointmentId=SeedIds.Appt1,
            PatientId=SeedIds.Patient1,
            DoctorId=SeedIds.Doctor1,

            EncounterNumber="ENC-20260627-0001",
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

            ChiefComplaint="Routine hypertension follow-up.",
            ReasonForVisit="Three-month evaluation after medication adjustment.",

         

            HistoryOfPresentIllness=
                "Patient reports improved home blood pressure readings averaging 126/78 mmHg. Denies headaches, dizziness, chest pain, palpitations, or dyspnea. Reports good medication adherence with no adverse effects.",

            Assessment=
                "Essential hypertension remains well controlled. No evidence of cardiovascular complications. Patient demonstrates good compliance with treatment.",

            Plan=
                "Continue Losartan 50 mg once daily. Encourage DASH diet, regular physical activity, and home blood pressure monitoring. Return for follow-up in three months or sooner if symptoms develop.",

            Notes=
                "Patient arrived on time, alert, cooperative, and in no acute distress. Physical examination was within normal limits. Medication regimen reviewed. Lifestyle counseling provided.",

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
        }
        catch(Exception ex)
        {
            throw new Exception("Encounter seeding failed", ex);
        }
    }
}