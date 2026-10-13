using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>Seeds fifteen demo patients with twenty-five completed appointments and encounters.</summary>
public sealed class ClinicalScenarioSeeder : IEntitySeeder
{
    public int Order => 56;

    private static readonly Guid[] PatientIds =
    [
        SeedIds.Patient1, SeedIds.Patient2, SeedIds.Patient3, SeedIds.Patient4, SeedIds.Patient5,
        SeedIds.Patient6, SeedIds.Patient7, SeedIds.Patient8, SeedIds.Patient9, SeedIds.Patient10,
        SeedIds.Patient11, SeedIds.Patient12, SeedIds.Patient13, SeedIds.Patient14, SeedIds.Patient15
    ];

    private static readonly Guid[] DoctorIds =
    [
        SeedIds.Doctor1, SeedIds.Doctor2, SeedIds.Doctor3, SeedIds.Doctor4, SeedIds.Doctor5,
        SeedIds.Doctor6, SeedIds.Doctor7, SeedIds.Doctor8, SeedIds.Doctor9, SeedIds.Doctor10
    ];

    private static readonly string[] Reasons =
    [
        "Почетна ревматолошка проценка", "Контролен преглед", "Проценка на терапија",
        "Следење на симптоми", "Редовна контрола"
    ];

    private static Guid AppointmentId(int number) =>
        Guid.Parse($"00000000-0000-0000-0000-{7000 + number:000000000000}");

    private static Guid EncounterId(int number) =>
        Guid.Parse($"00000000-0000-0000-0000-{8000 + number:000000000000}");

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var foundPatients = await context.Patients
            .Where(x => PatientIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (foundPatients.Count != PatientIds.Length)
            return;

        var foundDoctors = await context.Doctors
            .Where(x => DoctorIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (foundDoctors.Count == 0)
            return;

        var today = DateTime.Today;
        var appointments = BuildAppointments(today);
        var appointmentIds = appointments.Select(x => x.Id).ToArray();
        var existingAppointments = await context.Appointments
            .Where(x => appointmentIds.Contains(x.Id))
            .ToListAsync(ct);

        foreach (var item in appointments)
        {
            var existing = existingAppointments.FirstOrDefault(x => x.Id == item.Id);
            if (existing == null)
            {
                context.Appointments.Add(item);
                continue;
            }

            existing.PatientId = item.PatientId;
            existing.DoctorId = item.DoctorId;
            existing.AppointmentNumber = item.AppointmentNumber;
            existing.ScheduledStart = item.ScheduledStart;
            existing.ScheduledEnd = item.ScheduledEnd;
            existing.Status = item.Status;
            existing.ReasonForVisit = item.ReasonForVisit;
            existing.ClinicalNotes = item.ClinicalNotes;
        }

        await context.SaveChangesAsync(ct);

        var encounters = BuildEncounters(today);
        var encounterIds = encounters.Select(x => x.Id).ToArray();
        var existingEncounters = await context.Encounters
            .Where(x => encounterIds.Contains(x.Id))
            .ToListAsync(ct);

        foreach (var item in encounters)
        {
            var existing = existingEncounters.FirstOrDefault(x => x.Id == item.Id);
            if (existing == null)
            {
                context.Encounters.Add(item);
                continue;
            }

            existing.PatientId = item.PatientId;
            existing.AppointmentId = item.AppointmentId;
            existing.DoctorId = item.DoctorId;
            existing.EncounterNumber = item.EncounterNumber;
            existing.Status = EncounterStatus.Completed;
            existing.EncounterDate = item.EncounterDate;
            existing.ScheduledStart = item.ScheduledStart;
            existing.ScheduledEnd = item.ScheduledEnd;
            existing.StartTime = item.StartTime;
            existing.EndTime = item.EndTime;
            existing.DurationMinutes = item.DurationMinutes;
            existing.ChiefComplaint = item.ChiefComplaint;
            existing.ReasonForVisit = item.ReasonForVisit;
            existing.Assessment = item.Assessment;
            existing.Plan = item.Plan;
            existing.Notes = item.Notes;
            existing.ClinicalNotes = item.ClinicalNotes;
            existing.IsLocked = true;
            existing.IsActive = true;
        }

        await context.SaveChangesAsync(ct);
    }

    private static List<Appointment> BuildAppointments(DateTime today)
    {
        var result = new List<Appointment>();
        for (var i = 1; i <= 25; i++)
        {
            var start = today.AddDays(-(i * 2 + 1)).AddHours(9 + i % 4);
            result.Add(new Appointment
            {
                Id = AppointmentId(i),
                AppointmentNumber = $"TER-DEMO-{i:000}",
                PatientId = PatientIds[(i - 1) % PatientIds.Length],
                DoctorId = DoctorIds[(i - 1) % DoctorIds.Length],
                ScheduledStart = start,
                ScheduledEnd = start.AddMinutes(30),
                Status = AppointmentStatus.Completed,
                ReasonForVisit = Reasons[(i - 1) % Reasons.Length],
                ClinicalNotes = "Демо запис за тестирање.",
                CreatedAt = start.AddDays(-2)
            });
        }

        return result;
    }

    private static List<Encounter> BuildEncounters(DateTime today)
    {
        var result = new List<Encounter>();
        for (var i = 1; i <= 25; i++)
        {
            var start = today.AddDays(-(i * 2 + 1)).AddHours(9 + i % 4);
            var reason = Reasons[(i - 1) % Reasons.Length];
            result.Add(new Encounter
            {
                Id = EncounterId(i),
                AppointmentId = AppointmentId(i),
                PatientId = PatientIds[(i - 1) % PatientIds.Length],
                DoctorId = DoctorIds[(i - 1) % DoctorIds.Length],
                EncounterNumber = $"PREG-DEMO-{i:000}",
                EncounterType = "Outpatient",
                Status = EncounterStatus.Completed,
                Priority = "Routine",
                EncounterDate = start,
                ScheduledStart = start,
                ScheduledEnd = start.AddMinutes(30),
                StartTime = start,
                EndTime = start.AddMinutes(30),
                DurationMinutes = 30,
                ChiefComplaint = reason,
                ReasonForVisit = reason,
                Assessment = "Клиничката состојба е проценета.",
                Plan = "Продолжување со следење и контролен преглед.",
                Notes = "Демо клиничка белешка.",
                ClinicalNotes = "Демо клиничка белешка.",
                VisitSource = "Appointment",
                IsLocked = true,
                IsActive = true,
                CreatedAt = start,
                UpdatedAt = start.AddMinutes(30),
                CreatedBy = SeedIds.AdminUser
            });
        }

        return result;
    }
}
