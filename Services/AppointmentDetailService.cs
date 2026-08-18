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
        Task SaveAppointment(Appointment appointment, List<Diagnosis> diagnoses, TherapyCycle? cycle, List<PatientMedicine> medicines);
        Task GenerateNextTherapyCycle(Appointment appointment);
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

        public async Task<AppointmentDetailDto> GetAppointment(Guid id)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var appointment = await db.Appointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Doctor).ThenInclude(x => x.User)
                .Include(x => x.TherapyCycle)
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

            var cycles = await db.TherapyCycles
                .AsNoTracking()
                .Where(x => x.PatientId==appointment.PatientId)
                .Include(x => x.Appointments)
                .OrderBy(x => x.TherapyCyleNumber)
                .ToListAsync();

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
                TherapyCycles=cycles,
                Patients=patients,
                Doctors=doctors,
                PreviousAppointment=currentIndex>0 ? history[currentIndex-1] : null,
                NextAppointment=currentIndex<history.Count-1 ? history[currentIndex+1] : null,
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

            var cycles = await db.TherapyCycles
                .AsNoTracking()
                .Where(x => x.PatientId==patientId)
                .Include(x => x.Appointments)
                .OrderByDescending(x => x.TherapyCyleNumber)
                .ToListAsync();

            return new PatientContextDto
            {
                Appointments=appointments,
                Diagnoses=diagnoses,
                PatientMedicines=patientMedicines,
                TherapyCycles=cycles
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
                    x.Name.StartsWith(query)||x.GenericName.StartsWith(query)||x.Code.StartsWith(query)||
                    EF.Functions.Like(x.FullName
                    , $"%{query}%"))
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
        public async Task SaveAppointment(Appointment appointment, List<Diagnosis> diagnoses, TherapyCycle? cycle, List<PatientMedicine> medicines)
        {
            await using var db = await _factory.CreateDbContextAsync();
            appointment.TherapyCycleId=cycle?.Id;

            if(appointment.Id==Guid.Empty)
            {
                // ── Create 
                appointment.Id=Guid.NewGuid();

                if(string.IsNullOrWhiteSpace(appointment.AppointmentNumber))
                    appointment.AppointmentNumber=await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Appointment, "TER");

                await _encounterService.BuildEncounterAsync(db, appointment, "");

                db.Appointments.Add(appointment);

                await db.SaveChangesAsync();

                foreach(var d in diagnoses)
                {
                    db.Diagnoses.Add(new Diagnosis
                    {
                        Id=Guid.NewGuid(),
                        PatientId=appointment.PatientId,
                        EncounterId=appointment.Encounter.Id,
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
                // ── Edit ─────────────────────────────────────────────────────
                db.Appointments.Update(appointment);

                var encounter = await db.Encounters
                    .FirstOrDefaultAsync(e => e.AppointmentId==appointment.Id);

                if(encounter is not null&&!encounter.IsLocked)
                {
                    // Sync scheduling fields
                    encounter.DoctorId=appointment.DoctorId;
                    encounter.ScheduledStart=appointment.ScheduledStart;
                    encounter.ScheduledEnd=appointment.ScheduledEnd;
                    encounter.Notes=appointment.ClinicalNotes;
                    encounter.ReasonForVisit=null;

                    // ── Reconcile status — both sides converge ────────────────
                    var (resolvedEncounter, resolvedAppointment)=
                        ReconcileStatus(encounter.Status, appointment.Status);

                    encounter.Status=resolvedEncounter;
                    appointment.Status=resolvedAppointment;

                    if(resolvedEncounter==EncounterStatus.Completed&&!encounter.IsLocked)
                    {
                        encounter.IsLocked=true;
                        encounter.EndTime=DateTime.Now;
                        encounter.DurationMinutes=encounter.StartTime.HasValue
                            ? (int)(DateTime.Now-encounter.StartTime.Value).TotalMinutes
                            : null;
                    }

                    await db.SaveChangesAsync();

                    // Re-sync diagnoses only if not locked
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
                    // Edge case: appointment exists but encounter was never created
                    // (data from before this architecture) — create it now
                    await _encounterService.BuildEncounterAsync(db, appointment, "");
                }
            }

            // ── Sync patient medicines ──────────────────────────────────────
            // PatientMedicine is patient-scoped, not appointment/encounter-scoped,
            // so we only ADD newly-picked medicines that the patient doesn't already
            // have. We never delete here — unassigning a medicine from a patient is
            // a separate, explicit action outside this screen.
            if(medicines is { Count:>0 })
            {
                var existingMedicineIds = await db.PatientMedicines
                    .Where(x => x.PatientId==appointment.PatientId)
                    .Select(x => x.MedicineId)
                    .ToListAsync();

                foreach(var m in medicines)
                {
                    if(existingMedicineIds.Contains(m.MedicineId))
                        continue;

                    db.PatientMedicines.Add(new PatientMedicine
                    {
                        Id=Guid.NewGuid(),
                        PatientId=appointment.PatientId,
                        MedicineId=m.MedicineId,
                        Dosage=m.Dosage,
                       // Frequency=m.Frequency,
                        IsActive=true,
                        StartDate=appointment.ScheduledStart,
                    });
                }
            }

            await db.SaveChangesAsync();
        }


        public async Task UpdateAppointmentStatus(Guid appointmentId, AppointmentStatus newStatus)
        {
            await _encounterService.UpdateAppointmentStatus(appointmentId, newStatus);
        }

        public async Task GenerateNextTherapyCycle(Appointment appointment)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var lastCycle = await db.TherapyCycles
                .Where(x => x.PatientId==appointment.PatientId)
                .OrderByDescending(x => x.TherapyCyleNumber)
                .FirstOrDefaultAsync();

            var cycleNumber = lastCycle is null||string.IsNullOrWhiteSpace(lastCycle.TherapyCyleNumber)
                ? await SequenceHelper.GenerateNumberAsync(db, SequenceNames.TherapyCycle, "TER")
                : lastCycle.TherapyCyleNumber;

            var cycle = new TherapyCycle
            {
                Id=Guid.NewGuid(),
                PatientId=appointment.PatientId,
                TherapyCyleNumber=cycleNumber,
                Status=TherapyStatus.Scheduled,
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
                Status=AppointmentStatus.Scheduled,
            };

            db.Appointments.Add(nextAppointment);

    
            await _encounterService.BuildEncounterAsync(db, nextAppointment, "");

            await db.SaveChangesAsync();
        }


        private static EncounterStatus MapToEncounterStatus(AppointmentStatus status) => status switch
        {
            AppointmentStatus.Scheduled => EncounterStatus.Scheduled,
            AppointmentStatus.CheckedIn => EncounterStatus.InProgress,
            AppointmentStatus.InProgress => EncounterStatus.InProgress,
            AppointmentStatus.Completed => EncounterStatus.Completed,
            AppointmentStatus.Cancelled => EncounterStatus.Cancelled,
            AppointmentStatus.Missed => EncounterStatus.NoShow,
            AppointmentStatus.ReScheduled => EncounterStatus.Cancelled,
            _ => EncounterStatus.Scheduled
        };

        private static (EncounterStatus encounter, AppointmentStatus appointment)
         ReconcileStatus(EncounterStatus encounterStatus, AppointmentStatus appointmentStatus)
        {
            var resolved = ResolveLeadingStatus(encounterStatus, appointmentStatus);
            return (
                resolved,                           // already EncounterStatus — no mapping needed
                MapToAppointmentStatus(resolved)    // map once to get the appointment side
            );
        }

        // Lifecycle order — higher = further along
        private static int LifecycleOrder(EncounterStatus s) => s switch
        {
            EncounterStatus.Scheduled => 0,
            EncounterStatus.CheckedIn => 1,
            EncounterStatus.InProgress => 2,
            EncounterStatus.Completed => 4,
            EncounterStatus.Cancelled => 5,
            EncounterStatus.NoShow => 5,
            _ => 0
        };

        private static int LifecycleOrder(AppointmentStatus s) => s switch
        {
            AppointmentStatus.Scheduled => 0,
            AppointmentStatus.CheckedIn => 1,
            AppointmentStatus.InProgress => 2,
            AppointmentStatus.Completed => 4,
            AppointmentStatus.Cancelled => 5,
            AppointmentStatus.Missed => 5,
            AppointmentStatus.ReScheduled => 5,
            _ => 0
        };

        private static EncounterStatus ResolveLeadingStatus(
          EncounterStatus e, AppointmentStatus a)
        {
            // Terminal states on encounter side always win
            if(e is EncounterStatus.Completed or EncounterStatus.Cancelled or EncounterStatus.NoShow)
                return e;

            // Terminal states on appointment side — map and win
            if(a is AppointmentStatus.Completed or AppointmentStatus.Cancelled
                   or AppointmentStatus.Missed or AppointmentStatus.ReScheduled)
                return MapToEncounterStatus(a);

            // Non-terminal — whichever is further ahead wins
            return LifecycleOrder(e)>=LifecycleOrder(a)
                ? e
                : MapToEncounterStatus(a);
        }


        private static AppointmentStatus MapToAppointmentStatus(EncounterStatus s) => s switch
        {
            EncounterStatus.Scheduled => AppointmentStatus.Scheduled,
            EncounterStatus.CheckedIn => AppointmentStatus.CheckedIn,
            EncounterStatus.InProgress => AppointmentStatus.InProgress,
            EncounterStatus.Completed => AppointmentStatus.Completed,
            EncounterStatus.Cancelled => AppointmentStatus.Cancelled,
            EncounterStatus.NoShow => AppointmentStatus.Missed,
            _ => AppointmentStatus.Scheduled
        };
    }
}