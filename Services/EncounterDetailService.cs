using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;



namespace EHMR.Services;

public class EncounterDetailService : IEncounterDetailService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;
    private readonly IAuthStateService _authorizationService;

    public EncounterDetailService(
        IDbContextFactory<DesktopTherapyDbContext> factory,
        IAuthStateService authorization)
    {
        _factory=factory;
        _authorizationService=authorization;
    }

    private static DateTime EffectiveDate(Encounter e) =>
        e.ScheduledStart??e.EncounterDate;

    // ── Lookups ─────────────────────────────────────────────────────────────

    public async Task<List<Patient>> GetPatients()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Patients
            .AsNoTracking()
            .Include(x => x.PatientMedicines)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync();
    }

    public async Task<List<Doctor>> GetDoctors()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Doctors
            .AsNoTracking()
            .Include(x => x.User)
            .OrderBy(x => x.User.LastName)
            .ThenBy(x => x.User.FirstName)
            .ToListAsync();
    }

    // ── Encounter ────────────────────────────────────────────────────────────

    public async Task<EncounterDetailDto> GetEncounter(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var encounter = await db.Encounters
            .AsNoTracking()
            .Include(x => x.Patient).ThenInclude(x => x.PatientMedicines)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Include(x => x.Appointment)
            .Include(x => x.TherapyCycle)
            .FirstAsync(x => x.Id==id);

        var diagnoses = await db.Diagnoses
            .AsNoTracking()
            .Where(x => x.EncounterId==id)        // ← no more AppointmentDiagnoses
            .Include(x => x.Mkb10Code)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.Mkb10Code!.Code)
            .ToListAsync();

        var prescriptions = await db.Prescriptions
            .AsNoTracking()
            .Where(x => x.EncounterId==id)
            .OrderBy(x => x.Medication)
            .ToListAsync();

        var patients = await GetPatients();
        var doctors = await GetDoctors();

        var history = await db.Encounters
            .AsNoTracking()
            .Where(x => x.PatientId==encounter.PatientId)
            .ToListAsync();

        history=history.OrderBy(EffectiveDate).ToList();
        var currentIndex = history.FindIndex(x => x.Id==id);

        return new EncounterDetailDto
        {
            Encounter=encounter,
            Diagnoses=diagnoses,
            Prescriptions=prescriptions,
            Patients=patients,
            Doctors=doctors,
            PreviousEncounter=currentIndex>0 ? history[currentIndex-1] : null,
            NextEncounter=currentIndex>=0&&currentIndex<history.Count-1
                                    ? history[currentIndex+1] : null,
            TotalEncounters=history.Count,
            TotalDiagnoses=diagnoses.Count,
            TotalPrescriptions=prescriptions.Count
        };
    }

    // ── Appointment ──────────────────────────────────────────────────────────

    public async Task<Appointment?> GetAppointment(Guid appointmentId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Include(x => x.TherapyCycle)
            .FirstOrDefaultAsync(x => x.Id==appointmentId);
    }

    public async Task<IEnumerable<Appointment>> GetAppointments(Guid patientId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Appointments
            .AsNoTracking()
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Include(x => x.TherapyCycle)
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.ScheduledStart)
            .ToListAsync();
        // NO AppointmentDiagnoses include — that table is gone
    }

    public async Task<Appointment> CreateAppointment(Appointment appointment)
    {
        await using var db = await _factory.CreateDbContextAsync();

        appointment.Id=appointment.Id==Guid.Empty ? Guid.NewGuid() : appointment.Id;

        if(string.IsNullOrWhiteSpace(appointment.AppointmentNumber))
            appointment.AppointmentNumber=
                await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Appointment, "TER");

        // Encounter is always born with its appointment
        var encounterNumber =
            await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Encounter, "ENC");

        await BuildEncounterAsync(db,appointment, encounterNumber);

        db.Appointments.Add(appointment);
        //db.Encounters.Add(encounter);

        await db.SaveChangesAsync();
        return appointment;
    }

    // ── TherapyCycle ─────────────────────────────────────────────────────────

    public async Task<TherapyCycle> CreateTherapyCycle(TherapyCycle cycle)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.TherapyCycles.Add(cycle);
        await db.SaveChangesAsync();
        return cycle;
    }

    public async Task<List<TherapyCycle>> GetTherapyCycles(Guid patientId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.TherapyCycles
            .AsNoTracking()
            .Where(x => x.PatientId==patientId&&
                       (x.Status==TherapyStatus.Active||x.Status==TherapyStatus.Planned))
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();
    }

    // ── Patient Context ──────────────────────────────────────────────────────

    public async Task<PatientContextDto> GetPatientContext(Guid patientId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var patient = await db.Patients
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Doctor)

            // Problem list — no AppointmentDiagnoses anymore
            .Include(p => p.Diagnoses).ThenInclude(d => d.Mkb10Code)
            .Include(p => p.Diagnoses).ThenInclude(d => d.Encounter)

            // Encounter history
            .Include(p => p.Encounters).ThenInclude(e => e.Doctor).ThenInclude(d => d.User)
            .Include(p => p.Encounters).ThenInclude(e => e.TherapyCycle)
            // Encounter diagnoses via the Diagnosis.EncounterId FK
            .Include(p => p.Encounters).ThenInclude(e => e.Diagnoses).ThenInclude(d => d.Mkb10Code)

            // Appointments — scheduling shell only, no diagnosis join
            .Include(p => p.Appointments).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
            .Include(p => p.Appointments).ThenInclude(a => a.TherapyCycle)

            .Include(p => p.TherapyCycles).ThenInclude(t => t.Appointments)

            .Include(p => p.Prescriptions)

            .Include(p => p.PatientMedicines).ThenInclude(pm => pm.Medicine)

            .Include(p => p.Documents)

            .FirstOrDefaultAsync(p => p.Id==patientId);

        if(patient is null)
            return new PatientContextDto();

        return new PatientContextDto
        {
            Patient=patient,
            PrimaryDoctor=patient.Doctor,

            Diagnoses=patient.Diagnoses
                                .OrderByDescending(d => d.DiagnosedAt)
                                .ToList(),

            EncounterHistory=patient.Encounters
                                .OrderByDescending(e => e.EncounterDate)
                                .ToList(),

            Appointments=patient.Appointments
                                .OrderByDescending(a => a.ScheduledStart)
                                .ToList(),

            TherapyCycles=patient.TherapyCycles
                                .OrderByDescending(t => t.StartDate)
                                .ToList(),

            Prescriptions=patient.Prescriptions
                                .OrderByDescending(p => p.IssuedDate)
                                .ToList(),

            PatientMedicines=patient.PatientMedicines
                                .OrderByDescending(pm => pm.StartDate)
                                .ToList(),

            Documents=patient.Documents
                                .Where(d => !d.IsDeleted)
                                .OrderByDescending(d => d.UploadedAt)
                                .ToList()
        };
    }

    // ── Search ───────────────────────────────────────────────────────────────

    public async Task<List<Mkb10Code>> SearchDiagnoses(string query, CancellationToken token)
    {
        await using var db = await _factory.CreateDbContextAsync();
        if(string.IsNullOrWhiteSpace(query)) return [];

        query=query.Trim();
        return await db.Mkb10Codes
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(query)||
                        EF.Functions.Like(x.Description, $"%{query}%"))
            .OrderBy(x => x.Code)
            .Take(30)
            .ToListAsync(token);
    }

    public async Task<List<Medicine>> SearchMedicines(string term, CancellationToken ct = default)
    {
        if(string.IsNullOrWhiteSpace(term)) return [];
        term=term.Trim();

        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Medicines
            .AsNoTracking()
            .Where(m => m.IsActive&&(
                m.Name.Contains(term)||
                m.GenericName.Contains(term)||
                m.Code.Contains(term)))
            .OrderBy(m => m.Name)
            .Take(20)
            .ToListAsync(ct);
    }

    // ── Status Sync ──────────────────────────────────────────────────────────

    public async Task UpdateAppointmentStatus(Guid? appointmentId, AppointmentStatus newStatus)
    {
        await using var db = await _factory.CreateDbContextAsync();

        if(appointmentId==null||appointmentId==Guid.Empty)
            return;
        var appointment = await db.Appointments.FirstAsync(x => x.Id==appointmentId);
        var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.AppointmentId==appointmentId);

        appointment.Status=newStatus;

        if(encounter is not null)
        {
            var (resolvedEncounter, resolvedAppointment)=
                ReconcileStatus(encounter.Status, newStatus);

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
        }

        await db.SaveChangesAsync();
    }

    public async Task UpdateEncounterStatus(Guid encounterId, EncounterStatus newStatus)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var encounter = await db.Encounters.FirstAsync(x => x.Id==encounterId);
        var appointment = encounter.AppointmentId.HasValue
            ? await db.Appointments.FirstOrDefaultAsync(x => x.Id==encounter.AppointmentId.Value)
            : null;

        encounter.Status=newStatus;

        if(appointment is not null)
        {
            var (resolvedEncounter, resolvedAppointment)=
                ReconcileStatus(newStatus, appointment.Status);

            encounter.Status=resolvedEncounter;
            appointment.Status=resolvedAppointment;
        }

        if(encounter.Status==EncounterStatus.Completed&&!encounter.IsLocked)
        {
            encounter.IsLocked=true;
            encounter.EndTime=DateTime.Now;
            encounter.DurationMinutes=encounter.StartTime.HasValue
                ? (int)(DateTime.Now-encounter.StartTime.Value).TotalMinutes
                : null;
        }

        encounter.UpdatedAt=DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
    public async Task SaveEncounter(
        Encounter encounter,
        List<Diagnosis> diagnoses,
        List<Prescription> prescriptions,
        List<PatientMedicine> medicines,
        List<Guid> deletedMedicineIds)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var exists = encounter.Id!=Guid.Empty
                &&await db.Encounters.AsNoTracking().AnyAsync(e => e.Id==encounter.Id);

            // Guard: one active encounter per appointment slot
            if(encounter.AppointmentId is { } apptId&&apptId!=Guid.Empty)
            {
                var duplicateActive = await db.Encounters.AsNoTracking().AnyAsync(e =>
                    e.AppointmentId==apptId&&
                    e.Id!=encounter.Id&&
                    e.Status!=EncounterStatus.Cancelled&&
                    e.Status!=EncounterStatus.NoShow);

                if(duplicateActive)
                    throw new InvalidOperationException(
                        "Овој термин веќе има активен преглед.");
            }

            encounter.Diagnoses.Clear(); // detach nav collection — diagnoses saved separately below

            if(!exists)
            {
                encounter.Id=encounter.Id==Guid.Empty ? Guid.NewGuid() : encounter.Id;

                if(string.IsNullOrWhiteSpace(encounter?.EncounterNumber))
                    encounter.EncounterNumber=
                        await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Encounter, "ENC");

                encounter.CreatedAt=DateTime.UtcNow;

                if(encounter.CreatedBy==Guid.Empty)
                    encounter.CreatedBy=_authorizationService.CurrentUser.Id;

                encounter.IsActive=true;

                if(encounter.EncounterDate==default)
                    encounter.EncounterDate=
                        encounter.StartTime??encounter.ScheduledStart??DateTime.Now;

                // If linked to an appointment, sync its status to InProgress
              

                db.Encounters.Add(encounter);
            }
            else
            {
                encounter.UpdatedAt=DateTime.UtcNow;
                encounter.UpdatedBy=_authorizationService.CurrentUser.Id;
                db.Encounters.Update(encounter);
            }

            await db.SaveChangesAsync();

            // ── Diagnoses: full replace scoped to this encounter ─────────────
            var existingDiagnoses = await db.Diagnoses
                .Where(x => x.EncounterId==encounter.Id)
                .ToListAsync();
            db.Diagnoses.RemoveRange(existingDiagnoses);

            foreach(var d in diagnoses)
            {
                d.Id=Guid.NewGuid();
                d.PatientId=encounter.PatientId;
                d.EncounterId=encounter.Id;
                d.Mkb10Code=null; // detach nav property — FK is enough
                if(d.DiagnosedAt==default) d.DiagnosedAt=encounter.EncounterDate;
                if(string.IsNullOrWhiteSpace(d.DiagnosisNumber))
                    d.DiagnosisNumber=
                        await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Diagnosis, "DX");
                db.Diagnoses.Add(d);
            }

            // ── Prescriptions: full replace scoped to this encounter ─────────
            var existingPrescriptions = await db.Prescriptions
                .Where(x => x.EncounterId==encounter.Id)
                .ToListAsync();
            db.Prescriptions.RemoveRange(existingPrescriptions);

            foreach(var p in prescriptions)
            {
                p.Id=Guid.NewGuid();
                p.PatientId=encounter.PatientId;
                p.EncounterId=encounter.Id;
                db.Prescriptions.Add(p);
            }

            // ── PatientMedicines: targeted upsert/delete only ────────────────
            // Never full-replace — PatientMedicine belongs to patient lifetime,
            // not to this encounter. Only touch what changed this session.
            foreach(var id in deletedMedicineIds)
            {
                var entity = await db.PatientMedicines.FirstOrDefaultAsync(x => x.Id==id);
                if(entity is not null)
                    db.PatientMedicines.Remove(entity);
            }

            foreach(var vm in medicines.Where(x => !deletedMedicineIds.Contains(x.Id)))
            {
                var entity = await db.PatientMedicines.FirstOrDefaultAsync(x => x.Id==vm.Id);
                if(entity is null)
                {
                    db.PatientMedicines.Add(new PatientMedicine
                    {
                        Id=vm.Id==Guid.Empty ? Guid.NewGuid() : vm.Id,
                        PatientId=encounter.PatientId,
                        MedicineId=vm.MedicineId,
                        Dosage=vm.Dosage,
                        DosesFrequency=vm.DosesFrequency,
                        StartDate=vm.StartDate,
                        EndDate=vm.EndDate,
                        Notes=vm.Notes,
                        IsActive=vm.IsActive
                    });
                }
                else
                {
                    entity.MedicineId=vm.MedicineId;
                    entity.Dosage=vm.Dosage;
                    entity.DosesFrequency=vm.DosesFrequency;
                    entity.StartDate=vm.StartDate;
                    entity.EndDate=vm.EndDate;
                    entity.Notes=vm.Notes;
                    entity.IsActive=vm.IsActive;
                }
            }

            if(encounter.AppointmentId is { } linkedApptId&&linkedApptId!=Guid.Empty)
            {
                var appt = await db.Appointments
                    .FirstOrDefaultAsync(x => x.Id==linkedApptId);

                if(appt is not null)
                {
                    var (resolvedEncounter, resolvedAppointment)=
                        ReconcileStatus(encounter.Status, appt.Status);

                    encounter.Status=resolvedEncounter;
                    appt.Status=resolvedAppointment;

                    // Lock and timestamp when completing
                    if(resolvedEncounter==EncounterStatus.Completed&&!encounter.IsLocked)
                    {
                        encounter.IsLocked=true;
                        encounter.EndTime=DateTime.Now;
                        encounter.DurationMinutes=encounter.StartTime.HasValue
                            ? (int)(DateTime.Now-encounter.StartTime.Value).TotalMinutes
                            : null;
                    }

                    await db.SaveChangesAsync();
                }
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch(DbUpdateConcurrencyException ex)
        {
            foreach(var entry in ex.Entries)
            {
                var dbValues = await entry.GetDatabaseValuesAsync();
                System.Diagnostics.Debug.WriteLine($"{entry.Entity.GetType().Name} {entry.State}");
                System.Diagnostics.Debug.WriteLine(dbValues is null
                    ? "  DB row is NULL -> id does not exist in the database."
                    : "  DB row EXISTS -> genuine value mismatch.");
            }
            await tx.RollbackAsync();
            throw;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    public async Task BuildEncounterAsync(DesktopTherapyDbContext db, Appointment appointment, string encounterNumber)
    {
       

        if(string.IsNullOrWhiteSpace(encounterNumber))
            encounterNumber=await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Encounter, "ENC");

        db.Encounters.Add(new Encounter
        {
            Id=Guid.NewGuid(),
            AppointmentId=appointment.Id,
            PatientId=appointment.PatientId,
            DoctorId=appointment.DoctorId,
            EncounterNumber=encounterNumber,
            EncounterDate=appointment.ScheduledStart,
            ScheduledStart=appointment.ScheduledStart,
            ScheduledEnd=appointment.ScheduledEnd,
            ReasonForVisit=appointment.ReasonForVisit,
            Notes=appointment.ClinicalNotes,
            VisitSource="Appointment",
            Status=EncounterStatus.Scheduled,
            IsActive=true,
            CreatedAt=DateTime.UtcNow,
        });

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