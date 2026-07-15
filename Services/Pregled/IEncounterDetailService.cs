using EHMR.Domain.Entities;
using static EHMR.Domain.Entities.Rbac.AppRoutes;

namespace EHMR.Services;

public interface IEncounterDetailService
{
    Task<EncounterDetailDto> GetEncounter(Guid id);
    Task<List<Patient>> GetPatients();
    Task<List<Doctor>> GetDoctors();
    Task<PatientContextDto> GetPatientContext(Guid patientId);
    Task<Appointment?> GetAppointment(Guid id);
    Task<IEnumerable<Appointment?>> GetAppointments(Guid appointmentId);
    Task<List<TherapyCycle>> GetTherapyCycles(Guid patientId);
    Task<Appointment> CreateAppointment(Appointment appointment);

    Task<TherapyCycle> CreateTherapyCycle(TherapyCycle cycle);
    Task<List<Mkb10Code>> SearchDiagnoses(
        string query,
        CancellationToken token);
   
    Task SaveEncounter(
        Encounter encounter,
         List<Diagnosis> diagnoses,
        List<Prescription> prescriptions);
}
