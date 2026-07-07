using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using EHMR.ViewModels.Appointments;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    public class AppointmentDetailDto
    {
        public Appointment Appointment { get; set; } = new();

        public List<AppointmentDiagnosis> Diagnoses { get; set; } = [];

        public List<TherapyCycle> TherapyCycles { get; set; } = [];

        public List<Patient> Patients { get; set; } = [];

        public List<Doctor> Doctors { get; set; } = [];

        public Appointment? PreviousAppointment
        {
            get; set;
        }

        public Appointment? NextAppointment
        {
            get; set;
        }

        public int TotalAppointments
        {
            get; set;
        }

        public int TotalDiagnoses
        {
            get; set;
        }

        public int TotalCycles
        {
            get; set;
        }
    }

    public class PatientContextDto
    {
        public List<DiagnosisHistoryItem> DiagnosisHistory { get; set; } = [];

        public List<Appointment> AppointmentHistory { get; set; } = [];

        public List<TherapyCycle> TherapyCycles { get; set; } = [];
    }

    public interface IAppointmentDetailService
    {
        Task<AppointmentDetailDto> GetAppointment(Guid id);
        Task<AppointmentDetailDto> GetAppointmentContext();

        Task<PatientContextDto> GetPatientContext(Guid patientId);

        Task<List<Mkb10Code>> SearchDiagnoses(
            string query,
            CancellationToken token);

        Task SaveAppointment(
            Appointment appointment,
            List<AppointmentDiagnosis> diagnoses,
            TherapyCycle? cycle);

        Task GenerateNextTherapyCycle(
            Appointment appointment);
    }

    public class AppointmentDetailService : IAppointmentDetailService
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

        public AppointmentDetailService(IDbContextFactory<DesktopTherapyDbContext> factory)
        {
            _factory=factory;
        }

        public async Task<AppointmentDetailDto> GetAppointment(Guid id)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var appointment = await db.Appointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Doctor).ThenInclude(x => x.User)
                .Include(x => x.TherapyCycle)
                .FirstAsync(x => x.Id==id);

            var diagnoses = await db.AppointmentDiagnoses
                .AsNoTracking()
                .Where(x => x.AppointmentId==id)
                .Include(x => x.Mkb10Code)
                .ToListAsync();

            var cycles = await db.TherapyCycles
                .AsNoTracking()
                .Where(x => x.PatientId==appointment.PatientId)
                .Include(x => x.Appointments)
                .OrderBy(x => x.CycleNumber)
                .ToListAsync();

            var patients = await db.Patients
                .AsNoTracking()
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToListAsync();

            var doctors = await db.Doctors.Include(x => x.User)
                .AsNoTracking()
                .OrderBy(x => x.User.LastName)
                .ThenBy(x => x.User.FirstName)
                .ToListAsync();

            var history = await db.Appointments
                .Where(x => x.PatientId==appointment.PatientId)
                .OrderBy(x => x.ScheduledStart)
                .ToListAsync();

            var currentIndex = history.FindIndex(x => x.Id==id);

            Appointment? previous = null;
            Appointment? next = null;

            if(currentIndex>0)
                previous=history[currentIndex-1];

            if(currentIndex<history.Count-1)
                next=history[currentIndex+1];

            return new AppointmentDetailDto
            {
                Appointment=appointment,
                Diagnoses=diagnoses,
                TherapyCycles=cycles,
                Patients=patients,
                Doctors=doctors,
                PreviousAppointment=previous,
                NextAppointment=next,
                TotalAppointments=history.Count,
                TotalDiagnoses=diagnoses.Count,
                TotalCycles=cycles.Count
            };
        }
        public async Task<AppointmentDetailDto> GetAppointmentContext()
        {
            await using var db = await _factory.CreateDbContextAsync();


            var patients = await db.Patients
                .AsNoTracking()
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToListAsync();

            var doctors = await db.Doctors.Include(x => x.User)
                .AsNoTracking()
                .OrderBy(x => x.User.LastName)
                .ThenBy(x => x.User.FirstName)
                .ToListAsync();


       

            return new AppointmentDetailDto
            {
                Appointment=null,
                Diagnoses=null,
                TherapyCycles=null,
                Patients=patients,
                Doctors=doctors,
                PreviousAppointment=null,
                NextAppointment=null,
                TotalAppointments=0,
                TotalDiagnoses=0,
                TotalCycles=0
            };
        }
        public async Task<PatientContextDto> GetPatientContext(Guid patientId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var appointments = await db.Appointments
                .AsNoTracking()
                .Where(x => x.PatientId==patientId)
                .OrderByDescending(x => x.ScheduledStart)
                .ToListAsync();

            var diagnoses = await db.Diagnoses
                .AsNoTracking()
                .Where(x => x.PatientId==patientId)
                .OrderByDescending(x => x.DiagnosedAt)
                .ToListAsync();

            var cycles = await db.TherapyCycles
                .AsNoTracking()
                .Where(x => x.PatientId==patientId)
                .Include(x => x.Appointments)
                .OrderByDescending(x => x.CycleNumber)
                .ToListAsync();

            return new PatientContextDto
            {
                AppointmentHistory=appointments,

                DiagnosisHistory=diagnoses
                    .Select(d => new DiagnosisHistoryItem
                    {
                        Id=d.Id,
                        Code=d.Mkb10Code?.Code??"",
                        Description=d.ClinicalDescription,
                        DiagnosedAt=d.DiagnosedAt,
                        Severity=d.Severity,
                        IsPrimary=d.IsPrimary
                    })
                    .ToList(),

                TherapyCycles=cycles
            };
        }

        public async Task<List<Mkb10Code>> SearchDiagnoses(
          string query,
            CancellationToken token)
        {
            await using var db = await _factory.CreateDbContextAsync();

            if(string.IsNullOrWhiteSpace(query))
                return [];

            query=query.Trim();

            return await db.Mkb10Codes
                .AsNoTracking()
                .Where(x =>
                    x.Code.StartsWith(query)||
                    EF.Functions.Like(x.Description, $"%{query}%"))
                .OrderBy(x => x.Code)
                .Take(30)
                .ToListAsync(token);
        }

        public async Task SaveAppointment(Appointment appointment,
     List<AppointmentDiagnosis> diagnoses,
     TherapyCycle? cycle)
        {
            await using var db = await _factory.CreateDbContextAsync();

            appointment.TherapyCycleId=cycle?.Id;

            if(appointment.Id==Guid.Empty)
            {
                appointment.Id=Guid.NewGuid();

                db.Appointments.Add(appointment);
            }
            else
            {
                db.Appointments.Update(appointment);
            }

            await db.SaveChangesAsync();

            var existing = await db.AppointmentDiagnoses
                .Where(x => x.AppointmentId==appointment.Id)
                .ToListAsync();

            db.AppointmentDiagnoses.RemoveRange(existing);

            foreach(var d in diagnoses)
            {
                db.AppointmentDiagnoses.Add(
                    new AppointmentDiagnosis
                    {
                        AppointmentId=appointment.Id,
                        Mkb10CodeId=d.Mkb10CodeId,
                        DiagnosisId=d.DiagnosisId,
                        IsPrimary=d.IsPrimary
                    });
            }

            await db.SaveChangesAsync();
        }

        public async Task GenerateNextTherapyCycle(
    Appointment appointment)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var lastCycle = await db.TherapyCycles
                .Where(x => x.PatientId==appointment.PatientId)
                .OrderByDescending(x => x.CycleNumber)
                .FirstOrDefaultAsync();

            var nextNumber = lastCycle?.CycleNumber+1??1;

            var cycle = new TherapyCycle
            {
                Id=Guid.NewGuid(),
                PatientId=appointment.PatientId,
                CycleNumber=nextNumber
            };

            db.TherapyCycles.Add(cycle);

            var nextAppointment = new Appointment
            {
                Id=Guid.NewGuid(),
                PatientId=appointment.PatientId,
                DoctorId=appointment.DoctorId,
                TherapyCycleId=cycle.Id,
                ScheduledStart=appointment.ScheduledStart.AddDays(7),
                ScheduledEnd=appointment.ScheduledEnd.AddDays(7),
                Status=AppointmentStatus.Scheduled
            };

            db.Appointments.Add(nextAppointment);

            await db.SaveChangesAsync();
        }
    }
}