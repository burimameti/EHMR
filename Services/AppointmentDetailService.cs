using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using EHMR.ViewModels.Appointments;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Services
{
    public interface IAppointmentDetailService
    {
        Task<List<Medicine>> SearchMedicines(string query, CancellationToken token);
        Task<AppointmentDetailDto> GetAppointment(Guid id);
        Task<AppointmentDetailDto> GetAppointmentContext();
        Task<PatientContextDto> GetPatientContext(Guid patientId);
        Task UpdateAppointmentStatus(Guid appointmentId, AppointmentStatus newStatus);
        Task<List<Mkb10Code>> SearchDiagnoses(string query, CancellationToken token);
        Task<DateTime> GetNextAvailableSlot(Guid doctorId, Guid patientId, DateTime from, int durationMinutes = 30);
        Task SaveAppointment(Appointment appointment, List<Diagnosis> diagnoses, List<PatientMedicine> medicines);
        Task AutoCloseStaleAppointmentsAsync();
    }

    public class AppointmentDetailService : IAppointmentDetailService
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;
        private readonly IEncounterDetailService _encounterService;
        public AppointmentDetailService(
       IDbContextFactory<DesktopTherapyDbContext> factory,
       IEncounterDetailService encounterService)              // ← add
        {
            _factory=factory;
            _encounterService=encounterService;
        }



        //private static Encounter BuildEncounter(Appointment appointment, string encounterNumber) => new()
        //{
        //    Id=Guid.NewGuid(),
        //    AppointmentId=appointment.Id,
        //    PatientId=appointment.PatientId,
        //    DoctorId=appointment.DoctorId,
        //    EncounterNumber=encounterNumber,
        //    EncounterDate=appointment.ScheduledStart,
        //    ScheduledStart=appointment.ScheduledStart,
        //    ScheduledEnd=appointment.ScheduledEnd,
        //    ReasonForVisit=appointment.ReasonForVisit,
        //    Notes=appointment.ClinicalNotes,
        //    VisitSource="Appointment",
        //    Status=EncounterStatus.Scheduled,
        //    IsActive=true,
        //    CreatedAt=DateTime.UtcNow,
        //};

        // ─── Queries ──────────────────────────────────────────────────────────
        public async Task AutoCloseStaleAppointmentsAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            var cutoff = DateTime.Now.AddDays(-3);

            var stale = await db.Appointments
                .Where(a => (a.Status==AppointmentStatus.Scheduled||a.Status==AppointmentStatus.InProgress)
                            &&a.ScheduledStart<cutoff)
                .ToListAsync();

            if(stale.Count==0) return;

            var staleIds = stale.Select(a => a.Id).ToList();

            var encounters = await db.Encounters
                .Where(e => staleIds.Contains(e.AppointmentId!.Value)&&!e.IsLocked)
                .ToListAsync();

            var encounterByAppointmentId = encounters.ToDictionary(e => e.AppointmentId!.Value);

            foreach(var appt in stale)
            {
                appt.Status=AppointmentStatus.Cancelled;

                if(encounterByAppointmentId.TryGetValue(appt.Id, out var encounter))
                {
                    encounter.Status=EncounterStatus.Cancelled;
                    encounter.IsLocked=true;
                    encounter.EndTime=DateTime.Now;
                    encounter.DurationMinutes=encounter.StartTime.HasValue
                        ? (int)(DateTime.Now-encounter.StartTime.Value).TotalMinutes
                        : null;
                }
            }

            await db.SaveChangesAsync();
        }
        public async Task<AppointmentDetailDto> GetAppointment(Guid id)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var appointment = await db.Appointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Doctor).ThenInclude(x => x.User)
                .FirstAsync(x => x.Id==id);

            // Diagnoses now come from the linked Encounter, not AppointmentDiagnoses
            var encounter = await db.Encounters
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppointmentId==id);

            var diagnoses = encounter is not null
                ? await db.Diagnoses
                    .AsNoTracking()
                    .Where(x => x.EncounterId==encounter.Id)
                    .Include(x => x.Mkb10Code)
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => x.Mkb10Code!.Code)
                    .ToListAsync()
                : [];
var patients = await db.Patients
                .AsNoTracking()
                .Include(x => x.Doctor)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToListAsync();

            var doctors = await db.Doctors
                .AsNoTracking()
                .Include(x => x.User)
                .OrderBy(x => x.User.LastName)
                .ThenBy(x => x.User.FirstName)
                .ToListAsync();

            var history = await db.Appointments
                .AsNoTracking()
                .Where(x => x.PatientId==appointment.PatientId)
                .OrderBy(x => x.ScheduledStart)
                .ToListAsync();

            var currentIndex = history.FindIndex(x => x.Id==id);

            return new AppointmentDetailDto
            {
                Appointment=appointment,
                LinkedEncounter=encounter,
                Diagnoses=diagnoses,
                Patients=patients,
                Doctors=doctors,
                PreviousAppointment=currentIndex>0 ? history[currentIndex-1] : null,
                NextAppointment=currentIndex<history.Count-1 ? history[currentIndex+1] : null,
                TotalAppointments=history.Count,
                TotalDiagnoses=diagnoses.Count,
            };
        }

        public async Task<AppointmentDetailDto> GetAppointmentContext()
        {
            await AutoCloseStaleAppointmentsAsync();
            await _encounterService.AutoCloseStaleVisitsAsync();
            await using var db = await _factory.CreateDbContextAsync();

            var patients = await db.Patients
                .AsNoTracking()
                .Include(x => x.Doctor)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToListAsync();

            var doctors = await db.Doctors
                .AsNoTracking()
                .Include(x => x.User)
                .OrderBy(x => x.User.LastName)
                .ThenBy(x => x.User.FirstName)
                .ToListAsync();

            return new AppointmentDetailDto
            {
                Patients=patients,
                Doctors=doctors,
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

            var patientMedicines = await db.PatientMedicines
                .AsNoTracking()
                .Include(x=>x.Medicine)
                .Where(x => x.PatientId==patientId)
                .OrderByDescending(x => x.IsActive)
                .ToListAsync();

            // All diagnoses for this patient — both standalone and encounter-linked
            var diagnoses = await db.Diagnoses
                .AsNoTracking()
                .Where(x => x.PatientId==patientId)
                .Include(x => x.Mkb10Code)
                .Include(x => x.Encounter)
                .OrderByDescending(x => x.DiagnosedAt)
                .ToListAsync();
return new PatientContextDto
            {
                Appointments=appointments,
                Diagnoses=diagnoses,
                PatientMedicines=patientMedicines
            };
        }
        public async Task<List<Medicine>> SearchMedicines(string query, CancellationToken token)
        {
            await using var db = await _factory.CreateDbContextAsync();

            if(string.IsNullOrWhiteSpace(query))
                return new List<Medicine>();

            query=query.Trim();

            return await db.Medicines
                .AsNoTracking()
                .Where(x =>
                    x.Name.StartsWith(query)||x.GenericName.StartsWith(query)||x.Code.StartsWith(query))
                .OrderBy(x => x.Name)
                .Take(30)
                .ToListAsync(token);
        }
        public async Task<List<Mkb10Code>> SearchDiagnoses(string query, CancellationToken token)
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

        public Task<DateTime> GetNextAvailableSlot(Guid doctorId, Guid patientId, DateTime from, int durationMinutes = 30) =>
            _encounterService.GetNextAvailableSlot(doctorId, from, durationMinutes, patientId);

        // ─── Commands ─────────
        public async Task SaveAppointment(Appointment appointment, List<Diagnosis> diagnoses, List<PatientMedicine> medicines)
        {
            await using var db = await _factory.CreateDbContextAsync();
appointment.Patient=null;
            appointment.Doctor=null;
Encounter? encounterForMedicines = null;
            bool medicinesEditable = true;

            if(appointment.Id==Guid.Empty)
            {
                appointment.Id=Guid.NewGuid();

                if(string.IsNullOrWhiteSpace(appointment.AppointmentNumber))
                    appointment.AppointmentNumber=await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Appointment, "TER");

                db.Appointments.Add(appointment);
                await _encounterService.BuildEncounterAsync(db, appointment, "");

                await db.SaveChangesAsync();

                var savedEncounter = await db.Encounters
                    .FirstAsync(e => e.AppointmentId==appointment.Id);

                encounterForMedicines=savedEncounter;

                foreach(var d in diagnoses)
                {
                    db.Diagnoses.Add(new Diagnosis
                    {
                        Id=Guid.NewGuid(),
                        PatientId=appointment.PatientId,
                        EncounterId=savedEncounter.Id,
                        Mkb10CodeId=d.Mkb10CodeId,
                        DiagnosedAt=appointment.ScheduledStart,
                        IsPrimary=d.IsPrimary,
                        Severity=d.Severity,
                        ClinicalDescription=d.ClinicalDescription,
                        Status=DiagnosisStatus.Suspected,
                    });
                }
            }
            else
            {
                db.Appointments.Update(appointment);

                var encounter = await db.Encounters
                    .FirstOrDefaultAsync(e => e.AppointmentId==appointment.Id);

                encounterForMedicines=encounter;

                if(encounter is not null&&!encounter.IsLocked)
                {
                    encounter.DoctorId=appointment.DoctorId;
                    encounter.ScheduledStart=appointment.ScheduledStart;
                    encounter.ScheduledEnd=appointment.ScheduledEnd;
                    encounter.Notes=appointment.ClinicalNotes;
                    encounter.ReasonForVisit=null;

                    var (resolvedEncounter, resolvedAppointment)=
                        ReconcileStatus(encounter.Status, appointment.Status);

                    encounter.Status=resolvedEncounter;
                    appointment.Status=resolvedAppointment;

                    // ── Lock on ANY terminal outcome, not just Completed ──
                    var isTerminal = resolvedEncounter is EncounterStatus.Completed
                                                        or EncounterStatus.Cancelled;

                    if(isTerminal&&!encounter.IsLocked)
                    {
                        encounter.IsLocked=true;
                        encounter.EndTime=DateTime.Now;
                        encounter.DurationMinutes=encounter.StartTime.HasValue
                            ? (int)(DateTime.Now-encounter.StartTime.Value).TotalMinutes
                            : null;
                    }

                    medicinesEditable=true; // this closing-out pass still saves

                    await db.SaveChangesAsync();

                    var existingDiag = await db.Diagnoses
                        .Where(x => x.EncounterId==encounter.Id)
                        .ToListAsync();
                    db.Diagnoses.RemoveRange(existingDiag);

                    foreach(var d in diagnoses)
                    {
                        db.Diagnoses.Add(new Diagnosis
                        {
                            Id=Guid.NewGuid(),
                            PatientId=appointment.PatientId,
                            EncounterId=encounter.Id,
                            Mkb10CodeId=d.Mkb10CodeId,
                            DiagnosedAt=appointment.ScheduledStart,
                            IsPrimary=d.IsPrimary,
                            Severity=d.Severity,
                            ClinicalDescription=d.ClinicalDescription,
                            Status=d.Status,
                        });
                    }
                }
                else if(encounter is null)
                {
                    await _encounterService.BuildEncounterAsync(db, appointment, "");
                    await db.SaveChangesAsync();

                    encounterForMedicines=await db.Encounters
                        .FirstAsync(e => e.AppointmentId==appointment.Id);
                }
                else
                {
                    // encounter is not null AND encounter.IsLocked == true — visit already
                    // closed (Completed/Cancelled). Diagnoses already skipped by the
                    // guard above; freeze medicines for the same reason.
                    medicinesEditable=false;
                }
            }

            // ── Sync patient medicines (only while the encounter isn't locked) ──
            if(medicinesEditable&&medicines is { Count:>0 })
            {
                var existingEntities = await db.PatientMedicines
                    .Where(x => x.PatientId==appointment.PatientId)
                    .ToListAsync();

                var existingByMedicineId = existingEntities
                    .ToDictionary(x => x.MedicineId);

                foreach(var m in medicines)
                {
                    if(existingByMedicineId.TryGetValue(m.MedicineId, out var entity))
                    {
                        entity.EncounterId=encounterForMedicines?.Id??entity.EncounterId;
                        entity.Dosage=m.Dosage;
                        entity.DosesFrequency=m.DosesFrequency;
                        
                        entity.Notes=m.Notes;
                        entity.IsActive=m.IsActive;
                    }
                    else
                    {
                        db.PatientMedicines.Add(new PatientMedicine
                        {
                            Id=Guid.NewGuid(),
                            PatientId=appointment.PatientId,
                            EncounterId=encounterForMedicines?.Id,
                            MedicineId=m.MedicineId,
                            Dosage=m.Dosage,
                            DosesFrequency=m.DosesFrequency,
                          
                            Notes=m.Notes,
                            IsActive=m.IsActive,
                        });
                    }
                }
            }

            await db.SaveChangesAsync();
        }


        public async Task UpdateAppointmentStatus(Guid appointmentId, AppointmentStatus newStatus)
        {
            await _encounterService.UpdateAppointmentStatus(appointmentId, newStatus);
        }

    }
}
