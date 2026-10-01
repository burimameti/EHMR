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
    Task<List<ApplicationRegime>> GetApplicationRegimesAsync(CancellationToken ct = default);
    Task<ApplicationRegime> AddApplicationRegimeAsync(string regime, CancellationToken ct = default);
    Task AutoCloseStaleVisitsAsync(int staleAfterDays = 3);
    Task SaveEncounter(
        Encounter encounter,
        List<Diagnosis> diagnoses,
        List<Prescription> prescriptions,
        List<PatientMedicine> medicines,
        List<Guid> deletedMedicineIds,
        string? scoreText = null,
        DateTime? nextFollowUpDate = null); 

    Task UpdateEncounterClinicalData(
        Guid encounterId,
        List<Diagnosis> diagnoses,
        string? remarks);
    Task<EncounterDetailDto> GetEncounter(Guid id);
    Task<List<Patient>> GetPatients();
    Task BuildEncounterAsync(DesktopTherapyDbContext db, Appointment appointment, string encounterNumber);
    Task<List<Doctor>> GetDoctors();
    Task<DateTime> GetNextAvailableSlot(Guid doctorId, DateTime from, int durationMinutes = 30, Guid? patientId = null);
    Task<PatientContextDto> GetPatientContext(Guid patientId);
    Task<Appointment?> GetAppointment(Guid id);
    Task<IEnumerable<Appointment?>> GetAppointments(Guid appointmentId);
    Task<List<TherapyCycle>> GetTherapyCycles(Guid patientId);
    Task<Appointment> CreateAppointment(Appointment appointment);

    Task<TherapyCycle> CreateTherapyCycle(TherapyCycle cycle);
    Task AttachTherapyCycleDocumentAsync(
        Guid patientId,
        Guid therapyCycleId,
        string fileName,
        string storedPath,
        string contentType,
        long fileSize,
        PatientDocumentType documentType = PatientDocumentType.Resenie);
    Task<List<Mkb10Code>> SearchDiagnoses(
        string query,
        CancellationToken token,
        string? codeSection = null,
        string? descriptionQuery = null);

   Task UpdateAppointmentStatus(Guid? appointmentId, AppointmentStatus newStatus);


}
