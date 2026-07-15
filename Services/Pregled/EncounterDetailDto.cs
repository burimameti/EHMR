using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using EHMR.ViewModels.Appointments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace EHMR.Services;

#region DTOs
public class DtoObject {

    public Guid Id{get;set; }
    public string FullName {get;set; }

}
public class EncounterDetailDto
{
    public Encounter Encounter { get; set; } = new();

    public List<Diagnosis> Diagnoses { get; set; } = [];

    public List<Prescription> Prescriptions { get; set; } = [];

    public List<Patient> Patients { get; set; } = [];

    public List<Doctor> Doctors { get; set; } = [];

    public Encounter? PreviousEncounter
    {
        get; set;
    }

    public Encounter? NextEncounter
    {
        get; set;
    }

    public int TotalEncounters
    {
        get; set;
    }

    public int TotalDiagnoses
    {
        get; set;
    }

    public int TotalPrescriptions
    {
        get; set;
    }
    public IEnumerable<PatientDocument>? Attachments
    {
        get;
        internal set;
    }
}
public class EncounterLookupDto
{
    public List<Patient> Patients { get; set; } = [];
    public List<Doctor> Doctors { get; set; } = [];
}
#endregion

public class EncounterDetailService : IEncounterDetailService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

    public EncounterDetailService(IDbContextFactory<DesktopTherapyDbContext> factory)
    {
        _factory=factory;
    }

    private static DateTime EffectiveDate(Encounter e) => e.ScheduledStart??e.EncounterDate;

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
            .Where(x => x.EncounterId==id)
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

        // sort in memory with the same effective-date rule the list page uses —
        // EF can't translate the null-coalescing comparer into SQL cleanly here
        history=history.OrderBy(EffectiveDate).ToList();

        var currentIndex = history.FindIndex(x => x.Id==id);

        Encounter? previous = currentIndex>0 ? history[currentIndex-1] : null;
        Encounter? next = currentIndex>=0&&currentIndex<history.Count-1 ? history[currentIndex+1] : null;

        return new EncounterDetailDto
        {
            Encounter=encounter,
            Diagnoses=diagnoses,
            Prescriptions=prescriptions,
            Patients=patients,
            Doctors=doctors,
            PreviousEncounter=previous,
            NextEncounter=next,
            TotalEncounters=history.Count,
            TotalDiagnoses=diagnoses.Count,
            TotalPrescriptions=prescriptions.Count
        };
    }

    public async Task<List<Patient>> GetPatients()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Patients
            .AsNoTracking()
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .ToListAsync();
    }

    public async Task<List<Doctor>> GetDoctors()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var doctor = await db.Doctors
            .AsNoTracking()
            .Include(x => x.User)
           
            .ToListAsync();
    return doctor;
    
    }

    /// <summary>Cycles a patient can attach a new encounter to — active/planned only,
    /// so closed-out cycles don't clutter the picker.</summary>
    //public async Task<List<TherapyCycle>> GetActiveCyclesForPatient(Guid patientId)
    //{
    //    await using var db = await _factory.CreateDbContextAsync();
    //    return await db.TherapyCycles
    //        .AsNoTracking()
    //        .Where(x => x.PatientId==patientId
    //            &&(x.Status==TherapyStatus.Active||x.Status==TherapyStatus.Planned))
    //        .OrderByDescending(x => x.StartDate)
    //        .ToListAsync();
    //}
    public async Task<Appointment?> GetAppointment(Guid appointmentId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Appointments
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.TherapyCycle)
            .FirstOrDefaultAsync(x => x.Id==appointmentId);
    }
    public async Task<IEnumerable<Appointment?>> GetAppointments(Guid appointmentId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Appointments
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.TherapyCycle)
            .Where(x => x.PatientId==appointmentId).ToListAsync();
    }
    public async Task<Appointment> CreateAppointment(Appointment appointment)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        return appointment;
    }

    public async Task<TherapyCycle> CreateTherapyCycle(TherapyCycle appointment)
    {
        await using var db = await _factory.CreateDbContextAsync();

        db.TherapyCycles.Add(appointment);
        await db.SaveChangesAsync();
        return appointment;
    }
    public async Task<List<TherapyCycle>> GetTherapyCycles(Guid patientId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.TherapyCycles
            .Where(x =>
                x.PatientId==patientId&&
                (x.Status==TherapyStatus.Active||
                 x.Status==TherapyStatus.Planned))
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();
    }


    public async Task<PatientContextDto> GetPatientContext(Guid patientId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var patient = await db.Patients
            .AsNoTracking()
            .AsSplitQuery() // избегнува cartesian explosion со толку collections
            .Include(p => p.Doctor)

            .Include(p => p.Diagnoses).ThenInclude(d => d.Mkb10Code)
            .Include(p => p.Diagnoses).ThenInclude(d => d.Encounter)

            .Include(p => p.Encounters).ThenInclude(e => e.Doctor)
            .Include(p => p.Encounters).ThenInclude(e => e.TherapyCycle)

            .Include(p => p.Appointments).ThenInclude(a => a.Doctor)
            .Include(p => p.Appointments).ThenInclude(a => a.TherapyCycle)
            .Include(p => p.Appointments).ThenInclude(a => a.AppointmentDiagnoses)
                .ThenInclude(ad => ad.Mkb10Code)

            .Include(p => p.TherapyCycles)
                .ThenInclude(t => t.Appointments)

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

    public async Task<List<Mkb10Code>> SearchDiagnoses(string query, CancellationToken token)
    {
        await using var db = await _factory.CreateDbContextAsync();
        if(string.IsNullOrWhiteSpace(query)) return [];

        query=query.Trim();
        return await db.Mkb10Codes
            .AsNoTracking()
            .Where(x => x.Code.StartsWith(query)||EF.Functions.Like(x.Description, $"%{query}%"))
            .OrderBy(x => x.Code)
            .Take(30)
            .ToListAsync(token);
    }

    public async Task SaveEncounter(Encounter encounter, List<Diagnosis> diagnoses, List<Prescription> prescriptions)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var exists = encounter.Id!=Guid.Empty
                &&await db.Encounters.AsNoTracking().AnyAsync(e => e.Id==encounter.Id);

            // guard against a second *active* encounter on the same appointment slot —
            // adjust the status list if your workflow allows re-opening after NoShow/Cancelled
            if(encounter.AppointmentId is { } apptId&&apptId!=Guid.Empty)
            {
                var duplicateActive = await db.Encounters.AsNoTracking().AnyAsync(e =>
                    e.AppointmentId==apptId
                    &&e.Id!=encounter.Id
                    &&e.Status!=EncounterStatus.Cancelled
                    &&e.Status!=EncounterStatus.NoShow);

                if(duplicateActive)
                    throw new InvalidOperationException("Овој термин веќе има активен преглед.");
            }

            // ВАЖНО: чисти ја пред Add/Update - инаку EF cascade-tracking-ира
            // целиот graph (вкл. Mkb10Code navigation) преку encounter.Diagnoses
            // и се обидува повторно да insert-ира постоечкиот Mkb10Code -> PK violation
            encounter.Diagnoses.Clear();

            if(!exists)
            {
                if(encounter.Id==Guid.Empty) encounter.Id=Guid.NewGuid();
                if(encounter.EncounterDate==default) encounter.EncounterDate=DateTime.Now;
                db.Encounters.Add(encounter);
            }
            else
            {
                db.Encounters.Update(encounter);
            }
            await db.SaveChangesAsync();

            var existingDiagnoses = await db.Diagnoses.Where(x => x.EncounterId==encounter.Id).ToListAsync();
            db.Diagnoses.RemoveRange(existingDiagnoses);

            foreach(var diagnosis in diagnoses)
            {
                diagnosis.Id=Guid.NewGuid();
                diagnosis.PatientId=encounter.PatientId;
                diagnosis.EncounterId=encounter.Id;
                diagnosis.Mkb10Code=null; // само FK Id се чува, navigation не се re-insert-ира
                if(diagnosis.DiagnosedAt==default) diagnosis.DiagnosedAt=encounter.EncounterDate;
                db.Diagnoses.Add(diagnosis);
            }

            var existingPrescriptions = await db.Prescriptions.Where(x => x.EncounterId==encounter.Id).ToListAsync();
            db.Prescriptions.RemoveRange(existingPrescriptions);

            foreach(var prescription in prescriptions)
            {
                prescription.Id=Guid.NewGuid();
                prescription.PatientId=encounter.PatientId;
                prescription.EncounterId=encounter.Id;
                db.Prescriptions.Add(prescription);
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}