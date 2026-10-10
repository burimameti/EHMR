using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Infrastructure.Persistence.Configs
{
    public class AppointmentSeeder : IEntitySeeder
    {
        public int Order => 50;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Set<Appointment>().AnyAsync(ct))
                return;

            var now = DateTime.UtcNow;

            var appointments = new List<Appointment>
            {
                // ===================== СКЕДЖУЛИРАНИ =====================
                new Appointment
                {
                    Id=SeedIds.Appt1,
                    PatientId = SeedIds.Patient1,
                    DoctorId = SeedIds.Doctor1,

                    ScheduledStart = now.AddDays(1).AddHours(9),
                    ScheduledEnd = now.AddDays(1).AddHours(9).AddMinutes(30),
                    ReasonForVisit = "Прва консултација поради болки во грбот",
                    ClinicalNotes = "Пациентот пријавува хронична болка во лумбален дел.",
                    Status = AppointmentStatus.Scheduled
                },
                new Appointment
                {
                    Id=SeedIds.Appt2,
                    PatientId = SeedIds.Patient2,
                    DoctorId = SeedIds.Doctor2,
                    ScheduledStart = now.AddDays(1).AddHours(10),
                    ScheduledEnd = now.AddDays(1).AddHours(10).AddMinutes(45),
                    ReasonForVisit = "Контролен преглед по терапија",
                    ClinicalNotes = "",
                    Status = AppointmentStatus.Scheduled
                },
                new Appointment
                {
                    PatientId = SeedIds.Patient3,
                    DoctorId = SeedIds.Doctor1,
                    ScheduledStart = now.AddDays(2).AddHours(11),
                    ScheduledEnd = now.AddDays(2).AddHours(11).AddMinutes(30),
                    ReasonForVisit = "Рехабилитација на колено",
                    ClinicalNotes = "",
                    Status = AppointmentStatus.Scheduled
                },

                // ===================== CHECKED IN =====================
                new Appointment
                {
                    PatientId = SeedIds.Patient4,
                    DoctorId = SeedIds.Doctor2,
                    ScheduledStart = now.AddHours(-1),
                    ScheduledEnd = now.AddHours(-0.5),
                    ReasonForVisit = "Физикална терапија - рамото",
                    ClinicalNotes = "Пациентот е пристигнат и е во тек преглед.",
                    Status = AppointmentStatus.InProgress
                },
                new Appointment
                {
                    PatientId = SeedIds.Patient5,
                    DoctorId = SeedIds.Doctor1,
                    ScheduledStart = now.AddHours(-2),
                    ScheduledEnd = now.AddHours(-1.5),
                    ReasonForVisit = "Контрола на повреда",
                    ClinicalNotes = "Чека на терапија.",
                    Status = AppointmentStatus.InProgress
                },

                // ===================== COMPLETED =====================
                new Appointment
                {
                    PatientId = SeedIds.Patient6,
                    DoctorId = SeedIds.Doctor1,
                    ScheduledStart = now.AddDays(-1).AddHours(9),
                    ScheduledEnd = now.AddDays(-1).AddHours(9).AddMinutes(30),
                    ReasonForVisit = "Терапија за врат",
                    ClinicalNotes = "Успешно завршена терапија, подобрување забележано.",
                    Status = AppointmentStatus.Completed
                },
                new Appointment
                {
                    PatientId = SeedIds.Patient7,
                    DoctorId = SeedIds.Doctor2,
                    ScheduledStart = now.AddDays(-2).AddHours(10),
                    ScheduledEnd = now.AddDays(-2).AddHours(10).AddMinutes(40),
                    ReasonForVisit = "Рехабилитација после повреда",
                    ClinicalNotes = "Пациентот реагира позитивно на терапијата.",
                    Status = AppointmentStatus.Completed
                },

                // ===================== CANCELLED =====================
                new Appointment
                {
                    PatientId = SeedIds.Patient8,
                    DoctorId = SeedIds.Doctor1,
                    ScheduledStart = now.AddDays(1).AddHours(12),
                    ScheduledEnd = now.AddDays(1).AddHours(12).AddMinutes(30),
                    ReasonForVisit = "Масажа терапија",
                    ClinicalNotes = "Откажано од пациент поради лични причини.",
                    Status = AppointmentStatus.Cancelled
                },
                new Appointment
                {
                    PatientId = SeedIds.Patient9,
                    DoctorId = SeedIds.Doctor2,
                    ScheduledStart = now.AddDays(2).AddHours(14),
                    ScheduledEnd = now.AddDays(2).AddHours(14).AddMinutes(30),
                    ReasonForVisit = "Контролен преглед",
                    ClinicalNotes = "Откажано.",
                    Status = AppointmentStatus.Cancelled
                },

                // ===================== CANCELLED =====================
                new Appointment
                {
                    PatientId = SeedIds.Patient10,
                    DoctorId = SeedIds.Doctor1,
                    ScheduledStart = now.AddDays(-1).AddHours(11),
                    ScheduledEnd = now.AddDays(-1).AddHours(11).AddMinutes(30),
                    ReasonForVisit = "Терапија за грб",
                    ClinicalNotes = "Терминот е откажан.",
                    Status = AppointmentStatus.Cancelled
                },
                new Appointment
                {
                    PatientId = SeedIds.Patient11,
                    DoctorId = SeedIds.Doctor2,
                    ScheduledStart = now.AddDays(-3).AddHours(10),
                    ScheduledEnd = now.AddDays(-3).AddHours(10).AddMinutes(30),
                    ReasonForVisit = "Контрола",
                    ClinicalNotes = "Терминот е откажан.",
                    Status = AppointmentStatus.Cancelled
                },

                // ===================== ДОПОЛНИТЕЛНИ СЛУЧАИ =====================
                new Appointment
                {
                    PatientId = SeedIds.Patient12,
                    DoctorId = SeedIds.Doctor1,
                    ScheduledStart = now.AddDays(3).AddHours(9),
                    ScheduledEnd = now.AddDays(3).AddHours(9).AddMinutes(45),
                    ReasonForVisit = "Рехабилитација на зглоб",
                    ClinicalNotes = "",
                    Status = AppointmentStatus.Scheduled
                },
                new Appointment
                {
                    PatientId = SeedIds.Patient13,
                    DoctorId = SeedIds.Doctor2,
                    ScheduledStart = now.AddDays(-4).AddHours(10),
                    ScheduledEnd = now.AddDays(-4).AddHours(10).AddMinutes(30),
                    ReasonForVisit = "Терапија",
                    ClinicalNotes = "Завршено успешно.",
                    Status = AppointmentStatus.Completed
                },
                new Appointment
                {
                    PatientId = SeedIds.Patient14,
                    DoctorId = SeedIds.Doctor1,
                    ScheduledStart = now.AddDays(4).AddHours(13),
                    ScheduledEnd = now.AddDays(4).AddHours(13).AddMinutes(30),
                    ReasonForVisit = "Прва проценка",
                    ClinicalNotes = "",
                    Status = AppointmentStatus.Scheduled
                },
                new Appointment
                {
                    PatientId = SeedIds.Patient15,
                    DoctorId = SeedIds.Doctor2,
                    ScheduledStart = now.AddHours(-5),
                    ScheduledEnd = now.AddHours(-4.5),
                    ReasonForVisit = "Физикална терапија",
                    ClinicalNotes = "Пациентот во процес на терапија.",
                    Status = AppointmentStatus.InProgress
                },

                new Appointment
                {
                    PatientId = SeedIds.Patient16,
                    DoctorId = SeedIds.Doctor1,
                    ScheduledStart = now.AddDays(5).AddHours(10),
                    ScheduledEnd = now.AddDays(5).AddHours(10).AddMinutes(30),
                    ReasonForVisit = "Рутинска контрола",
                    ClinicalNotes = "",
                    Status = AppointmentStatus.Scheduled
                },
                new Appointment
                {
                    PatientId = SeedIds.Patient17,
                    DoctorId = SeedIds.Doctor2,
                    ScheduledStart = now.AddDays(-6).AddHours(9),
                    ScheduledEnd = now.AddDays(-6).AddHours(9).AddMinutes(30),
                    ReasonForVisit = "Терапија за болка",
                    ClinicalNotes = "Добар напредок.",
                    Status = AppointmentStatus.Completed
                },
                new Appointment
                {
                    PatientId = SeedIds.Patient18,
                    DoctorId = SeedIds.Doctor1,
                    ScheduledStart = now.AddDays(6).AddHours(11),
                    ScheduledEnd = now.AddDays(6).AddHours(11).AddMinutes(45),
                    ReasonForVisit = "Ортопедска проценка",
                    ClinicalNotes = "",
                    Status = AppointmentStatus.Scheduled
                }
            };

            await context.Set<Appointment>().AddRangeAsync(appointments, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}