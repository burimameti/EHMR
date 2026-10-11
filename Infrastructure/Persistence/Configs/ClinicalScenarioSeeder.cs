using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>Deterministic demo appointments and completed encounters, spread across two months.</summary>
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
        Guid.Parse("00000000-0000-0000-0000-000000007007"),
        Guid.Parse("00000000-0000-0000-0000-000000007008"),
        Guid.Parse("00000000-0000-0000-0000-000000007009"),
        Guid.Parse("00000000-0000-0000-0000-000000007010")
    ];

    private static readonly Guid[] EncounterIds =
    [
        Guid.Parse("00000000-0000-0000-0000-000000008001"),
        Guid.Parse("00000000-0000-0000-0000-000000008002"),
        Guid.Parse("00000000-0000-0000-0000-000000008003"),
        Guid.Parse("00000000-0000-0000-0000-000000008004"),
        Guid.Parse("00000000-0000-0000-0000-000000008005"),
        Guid.Parse("00000000-0000-0000-0000-000000008006")
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var patientIds = new[] { SeedIds.Patient1, SeedIds.Patient2, SeedIds.Patient3, SeedIds.Patient4, SeedIds.Patient5 };
        var found = await context.Patients.Where(x => patientIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct);
        if (found.Count != patientIds.Length)
            return;

        var doctors = new[] { SeedIds.Doctor1, SeedIds.Doctor2, SeedIds.Doctor3, SeedIds.Doctor4, SeedIds.Doctor5 };
        var doctorIds = await context.Doctors.Where(x => doctors.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct);
        if (doctorIds.Count == 0)
            return;

        var today = DateTime.Today;
        var appointments = BuildAppointments(today);
        var existingAppointments = await context.Appointments.Where(x => AppointmentIds.Contains(x.Id)).ToListAsync(ct);
        foreach (var item in appointments)
        {
            var existing = existingAppointments.FirstOrDefault(x => x.Id == item.Id);
            if (existing == null) context.Appointments.Add(item);
            else
            {
                existing.PatientId = item.PatientId;
                existing.DoctorId = item.DoctorId;
                existing.AppointmentNumber = item.AppointmentNumber;
                existing.ScheduledStart = item.ScheduledStart;
                existing.ScheduledEnd = item.ScheduledEnd;
                existing.Status = item.Status;
                existing.ReasonForVisit = item.ReasonForVisit;
                existing.ClinicalNotes = item.ClinicalNotes;
            }
        }
        await context.SaveChangesAsync(ct);

        var encounters = BuildEncounters(today);
        var existingEncounters = await context.Encounters.Where(x => EncounterIds.Contains(x.Id)).ToListAsync(ct);
        foreach (var item in encounters)
        {
            var existing = existingEncounters.FirstOrDefault(x => x.Id == item.Id);
            if (existing == null) context.Encounters.Add(item);
            else
            {
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
                existing.Assessment = item.Assessment;
                existing.Plan = item.Plan;
                existing.IsLocked = true;
                existing.IsActive = true;
            }
        }
        await context.SaveChangesAsync(ct);
    }

    private static List<Appointment> BuildAppointments(DateTime today) =>
    [
        MakeAppointment(0, SeedIds.Patient1, SeedIds.Doctor1, today.AddDays(4).AddHours(9), AppointmentStatus.Scheduled, "Контролен преглед"),
        MakeAppointment(1, SeedIds.Patient2, SeedIds.Doctor2, today.AddDays(-52).AddHours(10), AppointmentStatus.Completed, "Прв завршен преглед"),
        MakeAppointment(2, SeedIds.Patient2, SeedIds.Doctor2, today.AddDays(-21).AddHours(10), AppointmentStatus.Completed, "Втор завршен преглед"),
        MakeAppointment(3, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(-58).AddHours(9), AppointmentStatus.Completed, "Почетна проценка"),
        MakeAppointment(4, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(-44).AddHours(9), AppointmentStatus.Completed, "Контрола на терапија"),
        MakeAppointment(5, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(-29).AddHours(9), AppointmentStatus.Completed, "Редовна контрола"),
        MakeAppointment(6, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(-12).AddHours(9), AppointmentStatus.Completed, "Последна завршена контрола"),
        MakeAppointment(7, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(8).AddHours(10), AppointmentStatus.Scheduled, "Следна контролна посета"),
        MakeAppointment(8, SeedIds.Patient4, SeedIds.Doctor4, today.AddDays(15).AddHours(11), AppointmentStatus.Scheduled, "Закажана контрола"),
        MakeAppointment(9, SeedIds.Patient4, SeedIds.Doctor4, today.AddDays(-5).AddHours(11), AppointmentStatus.Cancelled, "Откажан термин")
    ];

    private static Appointment MakeAppointment(int i, Guid patientId, Guid doctorId, DateTime start, AppointmentStatus status, string reason) => new()
    {
        Id = AppointmentIds[i],
        AppointmentNumber = $"TER-DEMO-{i + 1:000}",
        PatientId = patientId,
        DoctorId = doctorId,
        ScheduledStart = start,
        ScheduledEnd = start.AddMinutes(30),
        Status = status,
        ReasonForVisit = reason,
        ClinicalNotes = status == AppointmentStatus.Cancelled ? "Терминот е откажан." : "Демо запис за тестирање.",
        CreatedAt = start.AddDays(-2)
    };

    private static List<Encounter> BuildEncounters(DateTime today) =>
    [
        MakeEncounter(0, SeedIds.Patient2, SeedIds.Doctor2, today.AddDays(-52).AddHours(10), "Прв завршен преглед"),
        MakeEncounter(1, SeedIds.Patient2, SeedIds.Doctor2, today.AddDays(-21).AddHours(10), "Втор завршен преглед"),
        MakeEncounter(2, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(-58).AddHours(9), "Почетна проценка"),
        MakeEncounter(3, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(-44).AddHours(9), "Контрола на терапија"),
        MakeEncounter(4, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(-29).AddHours(9), "Редовна контрола"),
        MakeEncounter(5, SeedIds.Patient3, SeedIds.Doctor3, today.AddDays(-12).AddHours(9), "Последна завршена контрола")
    ];

    private static Encounter MakeEncounter(int i, Guid patientId, Guid doctorId, DateTime start, string reason) => new()
    {
        Id = EncounterIds[i],
        AppointmentId = AppointmentIds[i + (i < 2 ? 1 : 1)],
        PatientId = patientId,
        DoctorId = doctorId,
        EncounterNumber = $"PREG-DEMO-{i + 1:000}",
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
    };
}
