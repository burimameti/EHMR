using EHMR.Domain.Entities;
using EHMR.ViewModels.Appointments;

namespace EHMR.Services;



public class PatientContextDto
{
    public Patient? Patient
    {
        get; set;
    }
    public Doctor? PrimaryDoctor
    {
        get; set;
    }

    // Дијагнози — целосна историја
    public List<Diagnosis> Diagnoses { get; set; } = new();

    // Прегледи (Encounters) — сите статуси, филтрирање се прави на VM ниво
    public List<Encounter> EncounterHistory { get; set; } = new();

    // Термини (Appointments) — сите статуси
    public List<Appointment> Appointments { get; set; } = new();

    // Рецепти (Prescription entity)
    public List<Prescription> Prescriptions { get; set; } = new();

    // Активни/историски терапии на лекови (PatientMedicine join entity)
    public List<PatientMedicine> PatientMedicines { get; set; } = new();

    // Документи (наод/упат/лаб итн.)
    public List<PatientDocument> Documents { get; set; } = new();

    // Complete score history for the patient, including patient-level and prior-encounter scores.
    public List<PatientScore> Scores { get; set; } = new();

    // Latest score recorded for compatibility with existing summary bindings.
    public PatientScore? LatestScore { get; set; }
}
