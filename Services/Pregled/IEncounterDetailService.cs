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
    Task<List<string>> GetScoreSuggestionsAsync(CancellationToken ct = default);

    Task<List<ApplicationRegime>> GetApplicationRegimesAsync(CancellationToken ct = default);
    Task<ApplicationRegime> AddApplicationRegimeAsync(string regime, CancellationToken ct = default);
    Task<ApplicationRegime> UpdateApplicationRegimeAsync(Guid id, string regime, CancellationToken ct = default);
    Task DeactivateApplicationRegimeAsync(Guid id, CancellationToken ct = default);
    Task AutoCloseStaleVisitsAsync(int staleAfterDays = 3);
    Task SaveEncounter(
        Encounter encounter,
        List<PatientMkb10Assignment> mkb10Assignments,
        List<Prescription> prescriptions,
        List<PatientMedicine> medicines,
        List<Guid> deletedMedicineIds,
        string? scoreText = null,
        DateTime? nextFollowUpDate = null,
        List<PatientScore>? encounterScores = null); 

    Task UpdateEncounterClinicalData(
        Guid encounterId,
        List<PatientMkb10Assignment> mkb10Assignments,
        string? remarks);
    Task<EncounterDetailDto> GetEncounter(Guid id);
    Task<List<Patient>> GetPatients();
    Task BuildEncounterAsync(DesktopTherapyDbContext db, Appointment appointment, string encounterNumber);
    Task<List<Doctor>> GetDoctors();
    Task<DateTime> GetNextAvailableSlot(Guid doctorId, DateTime from, int durationMinutes = 30, Guid? patientId = null);
    Task<PatientContextDto> GetPatientContext(Guid patientId);
    Task<Appointment?> GetAppointment(Guid id);
    Task<IEnumerable<Appointment?>> GetAppointments(Guid appointmentId);
    Task<Appointment> CreateAppointment(Appointment appointment);
    Task<List<Mkb10Code>> SearchMkb10Codes(
        string query,
        CancellationToken token,
        string? codeSection = null,
        string? descriptionQuery = null);

   Task UpdateAppointmentStatus(Guid? appointmentId, AppointmentStatus newStatus);


}
