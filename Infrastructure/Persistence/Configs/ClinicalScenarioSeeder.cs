using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>
/// Поврзани демо сценарија за проверка на Dashboard, Термини и Прегледи.
/// Секој запис има фиксно ID и се додава само ако недостасува, па seeder-от е
/// безбеден и за база што веќе содржи други демо податоци.
/// </summary>
public sealed class ClinicalScenarioSeeder : IEntitySeeder
{
    public int Order => 56;

    private static readonly Guid[] AppointmentIds =
    [
        Guid.Parse("00000000-0000-0000-0000-000000007001"),
        Guid.Parse("00000000-0000-0000-0000-000000007002"),
        Guid.Parse("00000000-0000-0000-0000-000000007003"),
        Guid.Parse("00000000-0000-0000-0000-000000007004"),
        Guid.Parse("00000000-0000-0000-0000-000000007005"),
        Guid.Parse("00000000-0000-0000-0000-000000007006"),
        Guid.Parse("00000000-0000-0000-0000-000000007007")
    ];

    private static readonly Guid[] EncounterIds =
    [
        Guid.Parse("00000000-0000-0000-0000-000000008001"),
        Guid.Parse("00000000-0000-0000-0000-000000008002"),
        Guid.Parse("00000000-0000-0000-0000-000000008003"),
        Guid.Parse("00000000-0000-0000-0000-000000008004"),
        Guid.Parse("00000000-0000-0000-0000-000000008005"),
        Guid.Parse("00000000-0000-0000-0000-000000008006"),
        Guid.Parse("00000000-0000-0000-0000-000000008007")
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var requiredPatients=await context.Patients
            .Where(x => x.Id==SeedIds.Patient13||x.Id==SeedIds.Patient2||x.Id==SeedIds.Patient3)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var requiredDoctors=await context.Doctors
            .Where(x => x.Id==SeedIds.Doctor5||x.Id==SeedIds.Doctor2||x.Id==SeedIds.Doctor3)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if(requiredPatients.Count<3||requiredDoctors.Count<3)
            return;

        var today=DateTime.Today;
        var appointments=BuildAppointments(today);
        var existingAppointmentIds=await context.Appointments
            .Where(x => AppointmentIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);
        var missingAppointments=appointments
            .Where(x => !existingAppointmentIds.Contains(x.Id))
            .ToList();

        if(missingAppointments.Count>0)
        {
            await context.Appointments.AddRangeAsync(missingAppointments, ct);
            await context.SaveChangesAsync(ct);
        }

        var encounters=BuildEncounters(today);
        var existingEncounterIds=await context.Encounters
            .Where(x => EncounterIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);
        var missingEncounters=encounters
            .Where(x => !existingEncounterIds.Contains(x.Id))
            .ToList();

        if(missingEncounters.Count>0)
        {
            await context.Encounters.AddRangeAsync(missingEncounters, ct);
            await context.SaveChangesAsync(ct);
        }

        await SeedEncounterMedicinesAsync(context, ct);
    }

    private static List<Appointment> BuildAppointments(DateTime today) =>
    [
        Appointment(0, SeedIds.Patient13, SeedIds.Doctor5, today.AddMonths(-8).AddHours(9), AppointmentStatus.Completed, "Прв реуматолошки преглед", "Почетна проценка и лабораториски насоки."),
        Appointment(1, SeedIds.Patient13, SeedIds.Doctor5, today.AddMonths(-5).AddHours(10), AppointmentStatus.Completed, "Контрола по воведена терапија", "Намалена утринска вкочанетост."),
        Appointment(2, SeedIds.Patient13, SeedIds.Doctor5, today.AddMonths(-2).AddHours(11), AppointmentStatus.Completed, "Редовна контрола", "Стабилна состојба, терапијата се продолжува."),
        Appointment(3, SeedIds.Patient13, SeedIds.Doctor5, today.AddDays(-14).AddHours(9).AddMinutes(30), AppointmentStatus.Completed, "Контрола на болка во зглобови", "Добар одговор на терапијата."),
        Appointment(4, SeedIds.Patient13, SeedIds.Doctor5, today.AddDays(3).AddHours(10), AppointmentStatus.Scheduled, "Следна контролна посета", "Закажана контрола со нови лабораториски резултати."),
       // Appointment(5, SeedIds.Patient2, SeedIds.Doctor2, today.AddHours(12), AppointmentStatus.CheckedIn, "Акутна болка и оток на колено", "Пациентот е пријавен и чека преглед."),
      //  Appointment(6, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(-1).AddHours(13), AppointmentStatus.Missed, "Контрола на хронична терапија", "Пациентот не се појави.")
    ];

    private static Appointment Appointment(
        int index,
        Guid patientId,
        Guid doctorId,
        DateTime start,
        AppointmentStatus status,
        string reason,
        string notes) => new()
    {
        Id=AppointmentIds[index],
        AppointmentNumber=$"TER-DEMO-{index+1:000}",
        PatientId=patientId,
        DoctorId=doctorId,
        ScheduledStart=start,
        ScheduledEnd=start.AddMinutes(30),
        Status=status,
        ReasonForVisit=reason,
        ClinicalNotes=notes,
        CreatedAt=start.AddDays(-7)
    };

    private static List<Encounter> BuildEncounters(DateTime today) =>
    [
        CompletedEncounter(0, SeedIds.Patient13, SeedIds.Doctor5, today.AddMonths(-8).AddHours(9), "Болка во мали зглобови и утринска вкочанетост", "Почетна реуматолошка проценка", "Потребни лабораториски анализи и контролен преглед."),
        CompletedEncounter(1, SeedIds.Patient13, SeedIds.Doctor5, today.AddMonths(-5).AddHours(10), "Утринската вкочанетост е намалена", "Делумен клинички одговор", "Продолжување на терапијата со следење на крвна слика."),
        CompletedEncounter(2, SeedIds.Patient13, SeedIds.Doctor5, today.AddMonths(-2).AddHours(11), "Повремена болка при оптоварување", "Стабилна хронична состојба", "Продолжи со редовна терапија и умерена активност."),
        CompletedEncounter(3, SeedIds.Patient13, SeedIds.Doctor5, today.AddDays(-14).AddHours(9).AddMinutes(30), "Болка во зглобови со интензитет 3/10", "Добар одговор на терапија", "Контрола за три месеци со лабораториски резултати."),
        ScheduledEncounter(4, SeedIds.Patient13, SeedIds.Doctor5, today.AddDays(3).AddHours(10), EncounterStatus.Scheduled, "Следна контролна посета"),
       // ScheduledEncounter(5, SeedIds.Patient2, SeedIds.Doctor2, today.AddHours(12), EncounterStatus.CheckedIn, "Акутна болка и оток на колено"),
       // ScheduledEncounter(6, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(-1).AddHours(13), EncounterStatus.NoShow, "Контрола на хронична терапија")
    ];

    private static Encounter CompletedEncounter(
        int index,
        Guid patientId,
        Guid doctorId,
        DateTime start,
        string complaint,
        string assessment,
        string plan) => new()
    {
        Id=EncounterIds[index],
        AppointmentId=AppointmentIds[index],
        PatientId=patientId,
        DoctorId=doctorId,
        EncounterNumber=$"PREG-DEMO-{index+1:000}",
        EncounterType="Outpatient",
        Status=EncounterStatus.Completed,
        Priority="Routine",
        EncounterDate=start,
        ScheduledStart=start,
        ScheduledEnd=start.AddMinutes(30),
        CheckInTime=start.AddMinutes(-10),
        StartTime=start,
        EndTime=start.AddMinutes(30),
        CheckOutTime=start.AddMinutes(35),
        DurationMinutes=30,
        ChiefComplaint=complaint,
        ReasonForVisit=complaint,
        HistoryOfPresentIllness="Симптомите и текот на терапијата се разгледани со пациентот.",
        Assessment=assessment,
        Plan=plan,
        Notes="Демо клиничка белешка за проверка на приказот во детали.",
        ClinicalNotes="Демо клиничка белешка за проверка на приказот во детали.",
        VisitSource="Appointment",
        IsLocked=true,
        IsActive=true,
        CreatedAt=start,
        UpdatedAt=start.AddMinutes(30),
        CreatedBy=SeedIds.AdminUser,
        UpdatedBy=SeedIds.AdminUser
    };

    private static Encounter ScheduledEncounter(
        int index,
        Guid patientId,
        Guid doctorId,
        DateTime start,
        EncounterStatus status,
        string reason) => new()
    {
        Id=EncounterIds[index],
        AppointmentId=AppointmentIds[index],
        PatientId=patientId,
        DoctorId=doctorId,
        EncounterNumber=$"PREG-DEMO-{index+1:000}",
        EncounterType="Outpatient",
        Status=status,
        Priority="Routine",
        EncounterDate=start,
        ScheduledStart=start,
        ScheduledEnd=start.AddMinutes(30),
        CheckInTime=status==EncounterStatus.Scheduled ? start.AddMinutes(-10) : null,
        DurationMinutes=30,
        ReasonForVisit=reason,
        VisitSource="Appointment",
        IsLocked=false,
        IsActive=true,
        CreatedAt=start.AddDays(-3),
        CreatedBy=SeedIds.AdminUser
    };

    private static async Task SeedEncounterMedicinesAsync(
        DesktopTherapyDbContext context,
        CancellationToken ct)
    {
        var regimes = await context.ApplicationRegimes
            .Where(x => x.IsActive)
            .ToListAsync(ct);
        var oral = regimes.FirstOrDefault(x => x.Regime == "Орално")?.Id;
        var subcutaneous = regimes.FirstOrDefault(x => x.Regime == "Поткожно")?.Id;

        var medicines = new[]
        {
            new { Id = SeedIds.Med2, RegimeId = oral, Frequency = DosesFrequency.TwiceDaily, Dosage = "1 таблета од 400 mg", Notes = "По јадење, краткотрајно за болка." },
            new { Id = SeedIds.Med6, RegimeId = oral, Frequency = DosesFrequency.Daily, Dosage = "1 таблета од 5 mg", Notes = "Да се зема секое утро." },
            new { Id = SeedIds.Med9, RegimeId = subcutaneous, Frequency = DosesFrequency.Weekly, Dosage = "7.5 mg еднаш неделно", Notes = "Редовна контрола на крвна слика." }
        };

        // Ensure the new medicine-first patient-level assignments exist.
        foreach(var medicine in medicines)
        {
            var assignmentExists = await context.PatientMedicines.AnyAsync(
                x => x.PatientId == SeedIds.Patient13
                    && x.EncounterId == null
                    && x.MedicineId == medicine.Id,
                ct);
            if(assignmentExists)
                continue;

            context.PatientMedicines.Add(new PatientMedicine
            {
                Id = Guid.Parse(medicine.Id == SeedIds.Med2
                    ? "00000000-0000-0000-0000-000000009101"
                    : medicine.Id == SeedIds.Med6
                        ? "00000000-0000-0000-0000-000000009102"
                        : "00000000-0000-0000-0000-000000009103"),
                PatientId = SeedIds.Patient13,
                MedicineId = medicine.Id,
                ApplicationRegimeId = medicine.RegimeId,
                DosesFrequency = medicine.Frequency,
                Dosage = medicine.Dosage,
                Notes = medicine.Notes,
                IsActive = true,
                Quantity = 0m
            });
        }
        await context.SaveChangesAsync(ct);

        // Each demo encounter gets its own quantity snapshot for every medicine
        // assigned to the patient. Quantities deliberately vary by visit, including
        // zero, and never overwrite the patient-level assignment quantity.
        decimal[,] quantities =
        {
            { 1m, 0m, 0m },
            { 5m, 1m, 0m },
            { 0m, 0m, 2m },
            { 0m, 1m, 5m }
        };

        var seededEncounterIds = EncounterIds.Take(4).ToArray();
        var existingRows = await context.PatientMedicines
            .Where(x => x.EncounterId.HasValue && seededEncounterIds.Contains(x.EncounterId.Value))
            .ToListAsync(ct);

        for(var encounterIndex = 0; encounterIndex < seededEncounterIds.Length; encounterIndex++)
        {
            for(var medicineIndex = 0; medicineIndex < medicines.Length; medicineIndex++)
            {
                var medicine = medicines[medicineIndex];
                var encounterId = seededEncounterIds[encounterIndex];
                var quantity = quantities[encounterIndex, medicineIndex];
                var existing = existingRows.FirstOrDefault(
                    x => x.EncounterId == encounterId && x.MedicineId == medicine.Id);

                if(existing is not null)
                {
                    // These fixed demo encounter rows are intentionally refreshed
                    // so a database seeded with the old model receives the new values.
                    existing.Quantity = quantity;
                    existing.ApplicationRegimeId = medicine.RegimeId;
                    existing.DosesFrequency = medicine.Frequency;
                    existing.Dosage = medicine.Dosage;
                    existing.Notes = medicine.Notes;
                    existing.IsActive = true;
                    continue;
                }

                context.PatientMedicines.Add(new PatientMedicine
                {
                    Id = Guid.Parse($"00000000-0000-0000-0000-{(910000 + encounterIndex * 10 + medicineIndex):D12}"),
                    PatientId = SeedIds.Patient13,
                    EncounterId = encounterId,
                    MedicineId = medicine.Id,
                    ApplicationRegimeId = medicine.RegimeId,
                    DosesFrequency = medicine.Frequency,
                    Dosage = medicine.Dosage,
                    Notes = medicine.Notes,
                    IsActive = true,
                    Quantity = quantity
                });
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
}
