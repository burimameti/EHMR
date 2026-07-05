using EHMR.Domain.Entities;
using static EHMR.Domain.Entities.Rbac.AppRoutes;

namespace EHMR.Services;

public interface IEncounterDetailService
{
    Task<EncounterDetailDto> GetEncounter(Guid id);
    Task<List<Patient>> GetPatients();
    Task<List<Doctor>> GetDoctors();
    Task<EncounterPatientContextDto> GetPatientContext(Guid patientId);

    Task<List<Mkb10Code>> SearchDiagnoses(
        string query,
        CancellationToken token);
   
    Task SaveEncounter(
        Encounter encounter,
         List<Diagnosis> diagnoses,
        List<Prescription> prescriptions);
}
