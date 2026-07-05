using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using EHMR.ViewModels.Appointments;
using Microsoft.EntityFrameworkCore;

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

    public EncounterDetailService(
        IDbContextFactory<DesktopTherapyDbContext> factory)
    {
        _factory=factory;
    }

    public async Task<EncounterDetailDto> GetEncounter(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var encounter = await db.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
                .ThenInclude(x => x.User)
            .Include(x => x.Appointment)
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

        var patients = await db.Patients
            .AsNoTracking()
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync();

        var doctors = await db.Doctors
            .AsNoTracking()
            .Include(x => x.User)
            .OrderBy(x => x.User.LastName)
            .ThenBy(x => x.User.FirstName)
            .ToListAsync();

        var history = await db.Encounters
            .AsNoTracking()
            .Where(x => x.PatientId==encounter.PatientId)
            .OrderBy(x => x.EncounterDate)
            .ToListAsync();

        var currentIndex = history.FindIndex(x => x.Id==id);

        Encounter? previous = null;
        Encounter? next = null;

        if(currentIndex>0)
            previous=history[currentIndex-1];

        if(currentIndex<history.Count-1)
            next=history[currentIndex+1];

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
    public async Task<EncounterPatientContextDto> GetPatientContext(Guid patientId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var encounters = await db.Encounters
            .AsNoTracking()
            .Where(x => x.PatientId==patientId)
            .Include(x => x.Doctor)
                .ThenInclude(x => x.User)
            .Include(x => x.Appointment)
            .OrderByDescending(x => x.EncounterDate)
            .ToListAsync();

        var diagnoses = await db.Diagnoses
            .AsNoTracking()
            .Where(x => x.PatientId==patientId)
            .Include(x => x.Mkb10Code)
            .OrderByDescending(x => x.DiagnosedAt)
            .ToListAsync();

        var prescriptions = await db.Prescriptions
            .AsNoTracking()
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return new EncounterPatientContextDto
        {
            EncounterHistory=encounters,

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

            Prescriptions=prescriptions
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

    public async Task SaveEncounter(
        Encounter encounter,
        List<Diagnosis> diagnoses,
        List<Prescription> prescriptions)
    {
        await using var db = await _factory.CreateDbContextAsync();

        // --------------------------------------------------
        // Save Encounter
        // --------------------------------------------------
        var exists = encounter.Id!=Guid.Empty
            &&await db.Encounters.AsNoTracking().AnyAsync(e => e.Id==encounter.Id);

        if(!exists)
        {
            if(encounter.Id==Guid.Empty)
                encounter.Id=Guid.NewGuid();

            if(encounter.EncounterDate==default)
                encounter.EncounterDate=DateTime.Now;

            db.Encounters.Add(encounter);
        }
        else
        {
            db.Encounters.Update(encounter);
        }

        await db.SaveChangesAsync();

        // --------------------------------------------------
        // Replace Diagnoses
        // --------------------------------------------------
        var existingDiagnoses = await db.Diagnoses
            .Where(x => x.EncounterId==encounter.Id)
            .ToListAsync();
        db.Diagnoses.RemoveRange(existingDiagnoses);

        foreach(var diagnosis in diagnoses)
        {
            diagnosis.Id=Guid.NewGuid();
            diagnosis.PatientId=encounter.PatientId;
            diagnosis.EncounterId=encounter.Id;
            if(diagnosis.DiagnosedAt==default)
                diagnosis.DiagnosedAt=encounter.EncounterDate;
            db.Diagnoses.Add(diagnosis);
        }

        // --------------------------------------------------
        // Replace Prescriptions
        // --------------------------------------------------
        var existingPrescriptions = await db.Prescriptions
            .Where(x => x.EncounterId==encounter.Id)
            .ToListAsync();
        db.Prescriptions.RemoveRange(existingPrescriptions);

        foreach(var prescription in prescriptions)
        {
            prescription.Id=Guid.NewGuid();
            prescription.PatientId=encounter.PatientId;
            prescription.EncounterId=encounter.Id;
            db.Prescriptions.Add(prescription);
        }

        await db.SaveChangesAsync();
    }


}