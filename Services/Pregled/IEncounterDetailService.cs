using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static EHMR.Domain.Entities.Rbac.AppRoutes;

namespace EHMR.Services;

public interface IEncounterDetailService
{

    Task<List<Medicine>> SearchMedicines(string term, CancellationToken ct = default);

    Task SaveEncounter(
        Encounter encounter,
        List<Diagnosis> diagnoses,
        List<Prescription> prescriptions,
        List<PatientMedicine> medicines,
        List<Guid> deletedMedicineIds); 
    Task<EncounterDetailDto> GetEncounter(Guid id);
    Task<List<Patient>> GetPatients();
    Task BuildEncounterAsync(DesktopTherapyDbContext db, Appointment appointment, string encounterNumber);
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

   Task UpdateAppointmentStatus(Guid? appointmentId, AppointmentStatus newStatus);


}
