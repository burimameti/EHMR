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
        Task<PatientScore> SavePatientScore(PatientScore score);
        Task UpdateAppointmentStatus(Guid appointmentId, AppointmentStatus newStatus);
        Task<List<Mkb10Code>> SearchMkb10Codes(string query, CancellationToken token);
        Task<DateTime> GetNextAvailableSlot(Guid doctorId, Guid patientId, DateTime from, int durationMinutes = 30);
        Task SaveAppointment(Appointment appointment, List<PatientMkb10Assignment> mkb10Assignments, List<PatientMedicine> medicines);
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

            // MKB-10 assignments now come from the linked Encounter, not appointment-level assignments
            var encounter = await db.Encounters
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppointmentId==id);

            var mkb10Assignments = encounter is not null
                ? await db.PatientMkb10Assignments
                    .AsNoTracking()
                    .Where(x => x.EncounterId==encounter.Id)
                    .Include(x => x.Mkb10Code)
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => x.Mkb10Code!.Code)
                    .ToListAsync()
                : [];
var patients = await db.Patients
                .AsNoTracking()
                .Where(x => x.Status == PatientStatus.Active)
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
                Mkb10Assignments=mkb10Assignments,
                Patients=patients,
                Doctors=doctors,
                PreviousAppointment=currentIndex>0 ? history[currentIndex-1] : null,
                NextAppointment=currentIndex<history.Count-1 ? history[currentIndex+1] : null,
                TotalAppointments=history.Count,
                TotalMkb10Assignments=mkb10Assignments.Count,
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

        public async Task<PatientScore> SavePatientScore(PatientScore score)
        {
            if(score is null) throw new ArgumentNullException(nameof(score));
            if(score.PatientId == Guid.Empty) throw new InvalidOperationException("Изберете пациент пред внесување скор.");
            if(string.IsNullOrWhiteSpace(score.ScoreText)) throw new InvalidOperationException("Внесете име на скор.");
            if(string.IsNullOrWhiteSpace(score.Number)) throw new InvalidOperationException("Внесете вредност на скор.");

            await using var db = await _factory.CreateDbContextAsync();
            var patientExists = await db.Patients.AnyAsync(x => x.Id == score.PatientId);
            if(!patientExists) throw new InvalidOperationException("Пациентот не е пронајден.");

            score.Id = Guid.NewGuid();
            score.EncounterId = null;
            score.RecordedAt = DateTime.UtcNow;
            db.PatientScores.Add(score);
            await db.SaveChangesAsync();
            return score;
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

            // All MKB-10 assignments for this patient — both standalone and encounter-linked
            var mkb10Assignments = await db.PatientMkb10Assignments
                .AsNoTracking()
                .Where(x => x.PatientId==patientId)
                .Include(x => x.Mkb10Code)
                .Include(x => x.Encounter)
                .OrderByDescending(x => x.DiagnosedAt)
                .ToListAsync();

            var scores = await db.PatientScores
                .AsNoTracking()
                .Where(x => x.PatientId==patientId)
                .OrderByDescending(x => x.RecordedAt)
                .ToListAsync();

            return new PatientContextDto
            {
                Appointments=appointments,
                Mkb10Assignments=mkb10Assignments,
                PatientMedicines=patientMedicines,
                Scores=scores
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
        public async Task<List<Mkb10Code>> SearchMkb10Codes(string query, CancellationToken token)
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
        public async Task SaveAppointment(Appointment appointment, List<PatientMkb10Assignment> mkb10Assignments, List<PatientMedicine> medicines)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();

            appointment.Patient=null;
            appointment.Doctor=null;
Encounter? encounterForMedicines = null;
            bool medicinesEditable = true;

            if(appointment.Id==Guid.Empty)
            {
                var patientIsActive = await db.Patients
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == appointment.PatientId && x.Status == PatientStatus.Active);
                if (!patientIsActive)
                    throw new InvalidOperationException("Не може да се креира термин за непостоечки или неактивен пациент.");

                appointment.Id=Guid.NewGuid();

                if(string.IsNullOrWhiteSpace(appointment.AppointmentNumber))
                    appointment.AppointmentNumber=await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Appointment, "TER");

                db.Appointments.Add(appointment);
                await _encounterService.BuildEncounterAsync(db, appointment, "");

                await db.SaveChangesAsync();

                var savedEncounter = await db.Encounters
                    .FirstAsync(e => e.AppointmentId==appointment.Id);

                encounterForMedicines=savedEncounter;

                foreach(var d in mkb10Assignments)
                {
                    var assignmentNumber = await SequenceHelper.GenerateNumberAsync(
                        db, SequenceNames.PatientMkb10Assignment, "MKB");
                    db.PatientMkb10Assignments.Add(new PatientMkb10Assignment
                    {
                        Id=Guid.NewGuid(),
                        AssignmentNumber=assignmentNumber,
                        PatientId=appointment.PatientId,
                        EncounterId=savedEncounter.Id,
                        Mkb10CodeId=d.Mkb10CodeId,
                        DiagnosedAt=appointment.ScheduledStart,
                        IsPrimary=d.IsPrimary,
                        Severity=d.Severity,
                        ClinicalDescription=d.ClinicalDescription,
                        Status=d.Status,
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
                    encounter.ReasonForVisit=appointment.ReasonForVisit;

                    //var (resolvedEncounter, resolvedAppointment)=
                    //    ReconcileStatus(encounter.Status, appointment.Status);

                    //encounter.Status=resolvedEncounter;
                    //appointment.Status=resolvedAppointment;

                    //// ── Lock on ANY terminal outcome, not just Completed ──
                    //var isTerminal = resolvedEncounter is EncounterStatus.Completed
                    //                                    or EncounterStatus.Cancelled;

                    //if(isTerminal&&!encounter.IsLocked)
                    //{
                    //    encounter.IsLocked=true;
                    //    encounter.EndTime=DateTime.Now;
                    //    encounter.DurationMinutes=encounter.StartTime.HasValue
                    //        ? (int)(DateTime.Now-encounter.StartTime.Value).TotalMinutes
                    //        : null;
                    //}

                    medicinesEditable=true; // this closing-out pass still saves

                    await db.SaveChangesAsync();

                    var existingAssignments = await db.PatientMkb10Assignments
                        .Where(x => x.EncounterId==encounter.Id)
                        .ToListAsync();
                    db.PatientMkb10Assignments.RemoveRange(existingAssignments);

                    foreach(var d in mkb10Assignments)
                    {
                        var assignmentNumber = await SequenceHelper.GenerateNumberAsync(
                            db, SequenceNames.PatientMkb10Assignment, "MKB");
                        db.PatientMkb10Assignments.Add(new PatientMkb10Assignment
                        {
                            Id=Guid.NewGuid(),
                            AssignmentNumber=assignmentNumber,
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
                    // closed (Completed/Cancelled). MKB-10 assignments are already skipped by the
                    // guard above; freeze medicines for the same reason.
                    medicinesEditable=false;
                }
            }

            // Store medicine values on this encounter only. Never move or update
            // the patient's active assignment while saving an appointment.
            if(medicinesEditable && encounterForMedicines is not null && medicines is { Count: > 0 })
            {
                var existingEntities = await db.PatientMedicines
                    .Where(x => x.EncounterId == encounterForMedicines.Id)
                    .ToListAsync();

                var existingByMedicineId = existingEntities
                    .GroupBy(x => x.MedicineId)
                    .ToDictionary(group => group.Key, group => group.First());

                foreach(var medicine in medicines)
                {
                    if(existingByMedicineId.TryGetValue(medicine.MedicineId, out var existing))
                    {
                        existing.Dosage = medicine.Dosage;
                  
                        existing.ApplicationRegimeId = medicine.ApplicationRegimeId;
                 
                        existing.IsActive = medicine.IsActive;
                        existing.Quantity = medicine.Quantity;
                    }
                    else
                    {
                        db.PatientMedicines.Add(new PatientMedicine
                        {
                            Id = Guid.NewGuid(),
                            PatientId = appointment.PatientId,
                            EncounterId = encounterForMedicines.Id,
                            MedicineId = medicine.MedicineId,
                            ApplicationRegimeId = medicine.ApplicationRegimeId,
                            Dosage = medicine.Dosage,
                 
                            IsActive = medicine.IsActive,
                            Quantity = medicine.Quantity
                        });
                    }
                }
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }


        public async Task UpdateAppointmentStatus(Guid appointmentId, AppointmentStatus newStatus)
        {
            await _encounterService.UpdateAppointmentStatus(appointmentId, newStatus);
        }

    }
}
