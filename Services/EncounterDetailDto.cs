using EHMR.Domain.Entities;

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

    public PatientScore? Score { get; set; }

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
