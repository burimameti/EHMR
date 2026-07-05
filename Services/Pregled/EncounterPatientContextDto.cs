using EHMR.Domain.Entities;
using EHMR.ViewModels.Appointments;

namespace EHMR.Services;

public class EncounterPatientContextDto
{
    public List<DiagnosisHistoryItem> DiagnosisHistory { get; set; } = [];

    public List<Encounter> EncounterHistory { get; set; } = [];

    public List<Prescription> Prescriptions { get; set; } = [];
}
