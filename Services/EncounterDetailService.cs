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
            .Include(x => x.PatientMedicines).Include(x=>x.Doctor)
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

    public async Task<DateTime> GetNextAvailableSlot(
        Guid doctorId,
        DateTime from,
        int durationMinutes = 30,
        Guid? patientId = null)
    {
        if(doctorId==Guid.Empty)
            return from;

        var slotMinutes=Math.Max(5, durationMinutes);
        var candidate=NormalizeWorkingSlot(from, slotMinutes);

        await using var db = await _factory.CreateDbContextAsync();
        var searchUntil=candidate.AddDays(30);

        var appointments=await db.Appointments
            .AsNoTracking()
            .Where(x =>
                (x.DoctorId==doctorId||(patientId.HasValue&&x.PatientId==patientId.Value))&&
                x.Status!=AppointmentStatus.Cancelled&&
                x.ScheduledStart<searchUntil&&
                x.ScheduledEnd>candidate)
            .Select(x => new { Start=(DateTime?)x.ScheduledStart, End=(DateTime?)x.ScheduledEnd })
            .ToListAsync();

        var encounters=await db.Encounters
            .AsNoTracking()
            .Where(x =>
                (x.DoctorId==doctorId||(patientId.HasValue&&x.PatientId==patientId.Value))&&
                x.Status!=EncounterStatus.Cancelled&&
                x.ScheduledStart.HasValue&&
                x.ScheduledStart<searchUntil)
            .Select(x => new
            {
                Start=x.ScheduledStart,
                End=x.ScheduledEnd??x.ScheduledStart!.Value.AddMinutes(slotMinutes)
            })
            .ToListAsync();

        var occupied=appointments
            .Select(x => (Start:x.Start!.Value, End:x.End!.Value))
            .Concat(encounters.Select(x => (Start:x.Start!.Value, End:x.End)))
            .OrderBy(x => x.Start)
            .ToList();

        while(candidate<searchUntil)
        {
            var candidateEnd=candidate.AddMinutes(slotMinutes);
            var conflict=occupied.FirstOrDefault(x =>
                x.Start<candidateEnd&&x.End>candidate);

            if(conflict==default)
                return candidate;

            candidate=NormalizeWorkingSlot(conflict.End, slotMinutes);
        }

        return candidate;
    }
    private static DateTime NormalizeWorkingSlot(DateTime value, int durationMinutes)
    {
        var candidate=new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
        var remainder=candidate.Minute%30;
        if(remainder!=0)
            candidate=candidate.AddMinutes(30-remainder);
        else if(candidate<value)
            candidate=candidate.AddMinutes(30);

        while(candidate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            candidate=candidate.Date.AddDays(1).AddHours(9);

        if(candidate.TimeOfDay<TimeSpan.FromHours(9))
            candidate=candidate.Date.AddHours(9);

        if(candidate.TimeOfDay.Add(TimeSpan.FromMinutes(durationMinutes))>TimeSpan.FromHours(17))
        {
            candidate=candidate.Date.AddDays(1).AddHours(9);
            while(candidate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                candidate=candidate.Date.AddDays(1).AddHours(9);
        }

        return candidate;
    }
    // ── Encounter ────────────────────────────────────────────────────────────

    public async Task<EncounterDetailDto> GetEncounter(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var encounter = await db.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
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

        var score = await db.PatientScores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EncounterId==id && x.PatientId==encounter.PatientId);

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
            Score=score,
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
            await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Encounter, "PREG");

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

        // Starting a replacement therapy closes the currently active/planned
        // therapy as historical. We keep the old record for the patient history.
        if(cycle.PatientId.HasValue && cycle.Status==TherapyStatus.Active)
        {
            var current = await db.TherapyCycles
                .Where(x => x.PatientId==cycle.PatientId.Value
                            &&(x.Status==TherapyStatus.Active||x.Status==TherapyStatus.Planned))
                .ToListAsync();

            foreach(var previous in current)
            {
                previous.Status=TherapyStatus.Completed;
                previous.EndDate=cycle.StartDate==default
                    ? DateTime.Today
                    : cycle.StartDate;
            }
        }

        cycle.Id=cycle.Id==Guid.Empty ? Guid.NewGuid() : cycle.Id;
        db.TherapyCycles.Add(cycle);
        await db.SaveChangesAsync();
        return cycle;
    }

    public async Task AttachTherapyCycleDocumentAsync(
        Guid patientId,
        Guid therapyCycleId,
        string fileName,
        string storedPath,
        string contentType,
        long fileSize,
        PatientDocumentType documentType = PatientDocumentType.Resenie)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var cycleExists=await db.TherapyCycles
            .AsNoTracking()
            .AnyAsync(x => x.Id==therapyCycleId && x.PatientId==patientId);

        if(!cycleExists)
            throw new InvalidOperationException("Терапевтскиот циклус не постои за избраниот пациент.");

        db.PatientDocuments.Add(new PatientDocument
        {
            Id=Guid.NewGuid(),
            PatientId=patientId,
            TherapyCycleId=therapyCycleId,
            DocumentType=documentType,
            Title=string.IsNullOrWhiteSpace(fileName) ? "Решение" : fileName,
            Description="Документ приложен кон терапевтскиот циклус.",
            FileName=fileName,
            StoredPath=storedPath,
            ContentType=contentType??string.Empty,
            FileSize=fileSize,
            UploadedAt=DateTime.UtcNow
        });

        await db.SaveChangesAsync();
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
        await AutoCloseStaleVisitsAsync();
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
            .Include(p => p.PatientMedicines).ThenInclude(pm => pm.ApplicationRegime)

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
                                .ToList(),

            LatestScore=await db.PatientScores
                .AsNoTracking()
                .Where(s => s.PatientId==patientId)
                .OrderByDescending(s => s.RecordedAt)
                .FirstOrDefaultAsync()
        };
    }

    // ── Search ───────────────────────────────────────────────────────────────

    public async Task<List<Mkb10Code>> SearchDiagnoses(
        string query,
        CancellationToken token,
        string? codeSection = null,
        string? descriptionQuery = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        query=(query??string.Empty).Trim();
        descriptionQuery=(descriptionQuery??string.Empty).Trim();
        codeSection=string.IsNullOrWhiteSpace(codeSection)
            ? null
            : codeSection.Trim().ToUpperInvariant();

        var codes=db.Mkb10Codes.AsNoTracking().AsQueryable();

        if(codeSection!=null)
            codes=codes.Where(x => x.Code.StartsWith(codeSection));

        if(query.Length>0)
        {
            var codeQuery=query.ToUpperInvariant();
            codes=codes.Where(x => x.Code.Contains(codeQuery));
        }

        if(descriptionQuery.Length>0)
            codes=codes.Where(x => EF.Functions.Like(
                x.Description,
                $"%{descriptionQuery}%"));

        return await codes
            .OrderBy(x => x.Code)
            .Take(50)
            .ToListAsync(token);
    }

    public async Task<ApplicationRegime> AddApplicationRegimeAsync(string regime, CancellationToken ct = default)
    {
        regime=(regime??string.Empty).Trim();
        if(string.IsNullOrWhiteSpace(regime))
            throw new ArgumentException("Режимот на апликација е задолжителен.", nameof(regime));

        await using var db=await _factory.CreateDbContextAsync(ct);
        var existing=await db.ApplicationRegimes.FirstOrDefaultAsync(x => x.Regime==regime, ct);
        if(existing is not null)
            return existing;

        var entity=new ApplicationRegime
        {
            Id=Guid.NewGuid(),
            Regime=regime,
            IsActive=true
        };

        db.ApplicationRegimes.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<List<ApplicationRegime>> GetApplicationRegimesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.ApplicationRegimes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Regime)
            .ToListAsync(ct);
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

            var isTerminal = resolvedEncounter is EncounterStatus.Completed
                                                or EncounterStatus.Cancelled
    ;

            if(isTerminal&&!encounter.IsLocked)
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

        var isTerminal = encounter.Status is EncounterStatus.Completed
                                           or EncounterStatus.Cancelled;

        if(isTerminal&&!encounter.IsLocked)
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
    public async Task UpdateEncounterClinicalData(
        Guid encounterId,
        List<Diagnosis> diagnoses,
        string? remarks)
    {
        await using var db=await _factory.CreateDbContextAsync();
        await using var tx=await db.Database.BeginTransactionAsync();

        var encounter=await db.Encounters
            .FirstOrDefaultAsync(x => x.Id==encounterId)
            ??throw new InvalidOperationException("Прегледот не е пронајден.");

        encounter.SetNotes(remarks);
        encounter.UpdatedAt=DateTime.UtcNow;
        encounter.UpdatedBy=_authorizationService.CurrentUser.Id;

        var existing=await db.Diagnoses
            .Where(x => x.EncounterId==encounterId)
            .ToListAsync();
        db.Diagnoses.RemoveRange(existing);

        foreach(var diagnosis in diagnoses)
        {
            diagnosis.Id=Guid.NewGuid();
            diagnosis.PatientId=encounter.PatientId;
            diagnosis.EncounterId=encounter.Id;
            diagnosis.Mkb10Code=null;
            if(diagnosis.DiagnosedAt==default)
                diagnosis.DiagnosedAt=DateTime.Now;
            if(string.IsNullOrWhiteSpace(diagnosis.DiagnosisNumber))
                diagnosis.DiagnosisNumber=await SequenceHelper.GenerateNumberAsync(
                    db, SequenceNames.Diagnosis, "DX");
            db.Diagnoses.Add(diagnosis);
        }

        if(diagnoses.Count>0)
        {
            encounter.Complete(DateTime.Now);

            if(encounter.AppointmentId is { } appointmentId)
            {
                var appointment=await db.Appointments
                    .FirstOrDefaultAsync(x => x.Id==appointmentId);
                if(appointment is not null)
                    appointment.Status=AppointmentStatus.Completed;
            }
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task SaveEncounter(
        Encounter encounter,
        List<Diagnosis> diagnoses,
        List<Prescription> prescriptions,
        List<PatientMedicine> medicines,
        List<Guid> deletedMedicineIds,
        string? scoreText = null,
        DateTime? nextFollowUpDate = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            if(diagnoses.Count>0)
                encounter.Schedule(DateTime.Now);
            if(medicines.Count>0)
                encounter.Complete(DateTime.Now);
            var exists = encounter.Id!=Guid.Empty
                &&await db.Encounters.AsNoTracking().AnyAsync(e => e.Id==encounter.Id);

            // Guard: one active encounter per appointment slot
            if(encounter.AppointmentId is { } apptId&&apptId!=Guid.Empty)
            {
                var duplicateActive = await db.Encounters.AsNoTracking().AnyAsync(e =>
                    e.AppointmentId==apptId&&
                    e.Id!=encounter.Id&&
                    e.Status!=EncounterStatus.Cancelled);

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
                        await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Encounter, "PREG");

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
                {
                    entity.IsActive=false;
                    entity.EndDate=DateTime.UtcNow;
                }
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
                        EncounterId=encounter.Id,
                        MedicineId=vm.MedicineId,
                        ApplicationRegimeId=vm.ApplicationRegimeId,
                        Quantity=vm.Quantity,
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
                    entity.EncounterId=encounter.Id;
                    entity.MedicineId=vm.MedicineId;
                    entity.ApplicationRegimeId=vm.ApplicationRegimeId;
                    entity.Quantity=vm.Quantity;
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

                    await db.SaveChangesAsync();
                }
            }
            // One score belongs to one encounter and the patient. Blank score removes
            // the previous score for this encounter.
            var normalizedScore=scoreText?.Trim();
            var existingScore=await db.PatientScores
                .FirstOrDefaultAsync(x => x.EncounterId==encounter.Id && x.PatientId==encounter.PatientId);

            if(string.IsNullOrWhiteSpace(normalizedScore))
            {
                if(existingScore is not null)
                    db.PatientScores.Remove(existingScore);
            }
            else if(existingScore is null)
            {
                db.PatientScores.Add(new PatientScore
                {
                    Id=Guid.NewGuid(),
                    PatientId=encounter.PatientId,
                    EncounterId=encounter.Id,
                    ScoreText=normalizedScore,
                    RecordedAt=DateTime.UtcNow
                });
            }
            else
            {
                existingScore.ScoreText=normalizedScore;
                existingScore.RecordedAt=DateTime.UtcNow;
            }

            // Optional follow-up appointment. The appointment and its future encounter
            // are created in the same transaction so the next control is immediately
            // visible in the patient's schedule and the old therapy history remains intact.
            if(nextFollowUpDate is { } followUp && followUp.Date>=DateTime.Today)
            {
                var followUpAppointment=new Appointment
                {
                    Id=Guid.NewGuid(),
                    PatientId=encounter.PatientId,
                    DoctorId=encounter.DoctorId,
                    TherapyCycleId=encounter.TherapyCycleId,
                    ScheduledStart=followUp,
                    ScheduledEnd=followUp.AddMinutes(30),
                    ReasonForVisit="Следен контрол",
                    ClinicalNotes="Следен термин за контрола од прегледот.",
                    Status=AppointmentStatus.Scheduled
                };

                followUpAppointment.AppointmentNumber=await SequenceHelper.GenerateNumberAsync(
                    db, SequenceNames.Appointment, "TER");
                var followUpEncounterNumber=await SequenceHelper.GenerateNumberAsync(
                    db, SequenceNames.Encounter, "PREG");
                await BuildEncounterAsync(db, followUpAppointment, followUpEncounterNumber);
                db.Appointments.Add(followUpAppointment);
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
    public async Task AutoCloseStaleVisitsAsync(int staleAfterDays = 3)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var cutoff = DateTime.Now.AddDays(-staleAfterDays);

        // ── Appointments still open past the cutoff ──────────────────────
        var staleAppointments = await db.Appointments
            .Where(a => (a.Status==AppointmentStatus.Scheduled
                         ||a.Status==AppointmentStatus.InProgress
                         ||a.Status==AppointmentStatus.InProgress)
                        &&a.ScheduledStart<cutoff)
            .ToListAsync();

        if(staleAppointments.Count>0)
        {
            var staleIds = staleAppointments.Select(a => a.Id).ToList();

            var linkedEncounters = await db.Encounters
                .Where(e => e.AppointmentId.HasValue
                            &&staleIds.Contains(e.AppointmentId.Value)
                            &&!e.IsLocked)
                .ToListAsync();

            var encounterByAppointmentId = linkedEncounters
                .Where(e => e.AppointmentId.HasValue)
                .ToDictionary(e => e.AppointmentId!.Value);

            foreach(var appt in staleAppointments)
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
        }

        // ── Standalone encounters (walk-ins, no appointment) past the cutoff ──
        var staleEncounters = await db.Encounters
            .Where(e => !e.AppointmentId.HasValue
                        &&!e.IsLocked
                        &&(e.Status==EncounterStatus.Scheduled
                           ||e.Status==EncounterStatus.InProgress
                           ||e.Status==EncounterStatus.InProgress)
                        &&e.ScheduledStart.HasValue
                        &&e.ScheduledStart<cutoff)
            .ToListAsync();

        foreach(var encounter in staleEncounters)
        {
            encounter.Status=EncounterStatus.Cancelled;
            encounter.IsLocked=true;
            encounter.EndTime=DateTime.Now;
            encounter.DurationMinutes=encounter.StartTime.HasValue
                ? (int)(DateTime.Now-encounter.StartTime.Value).TotalMinutes
                : null;
        }

        if(staleAppointments.Count>0||staleEncounters.Count>0)
            await db.SaveChangesAsync();
    }
    // ── Private Helpers ──────────────────────────────────────────────────────

    public async Task BuildEncounterAsync(DesktopTherapyDbContext db, Appointment appointment, string encounterNumber)
    {
       

        if(string.IsNullOrWhiteSpace(encounterNumber))
            encounterNumber=await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Encounter, "PREG");

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
            ReasonForVisit=null,
            Notes=appointment.ClinicalNotes,
            VisitSource="Appointment",
            Status=EncounterStatus.Scheduled,
            IsActive=true,
            CreatedAt=DateTime.UtcNow,
        });

      //  await db.SaveChangesAsync();

    }

    private static EncounterStatus MapToEncounterStatus(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => EncounterStatus.Scheduled,
        AppointmentStatus.InProgress => EncounterStatus.InProgress,
        AppointmentStatus.Completed => EncounterStatus.Completed,
        AppointmentStatus.Cancelled => EncounterStatus.Cancelled,
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
        EncounterStatus.InProgress => 1,
        EncounterStatus.Completed => 4,
        EncounterStatus.Cancelled => 5,
        _ => 0
    };

    private static int LifecycleOrder(AppointmentStatus s) => s switch
    {
        AppointmentStatus.Scheduled => 0,
        AppointmentStatus.InProgress => 1,
        AppointmentStatus.Completed => 4,
        AppointmentStatus.Cancelled => 5,
        _ => 0
    };

    private static EncounterStatus ResolveLeadingStatus(
      EncounterStatus e, AppointmentStatus a)
    {
        // Terminal states on encounter side always win
        if(e is EncounterStatus.Completed or EncounterStatus.Cancelled)
            return e;

        // Terminal states on appointment side — map and win
        if(a is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            return MapToEncounterStatus(a);

        // Non-terminal — whichever is further ahead wins
        return LifecycleOrder(e)>=LifecycleOrder(a)
            ? e
            : MapToEncounterStatus(a);
    }


    private static AppointmentStatus MapToAppointmentStatus(EncounterStatus s) => s switch
    {
        EncounterStatus.Scheduled => AppointmentStatus.Scheduled,
        EncounterStatus.InProgress => AppointmentStatus.InProgress,
        EncounterStatus.Completed => AppointmentStatus.Completed,
        EncounterStatus.Cancelled => AppointmentStatus.Cancelled,
        _ => AppointmentStatus.Scheduled
    };
}
