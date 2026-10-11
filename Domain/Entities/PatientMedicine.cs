using System.ComponentModel.DataAnnotations.Schema;

namespace EHMR.Domain.Entities;

/// <summary>
/// Patient-level medicine assignment or encounter-specific administration snapshot.
/// EncounterId is null for the patient-card assignment and populated for a visit record.
/// Quantity belongs to this row; encounter quantities never update the patient assignment.
/// </summary>
public class PatientMedicine : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid? ApplicationRegimeId { get; set; }
    public ApplicationRegime? ApplicationRegime { get; set; }

    [NotMapped]
    public string ApplicationRegimeDisplay
    {
        get => ApplicationRegime?.Regime ?? string.Empty;
        set
        {
            var regimeValue = value ?? string.Empty;
            if (ApplicationRegime is null)
                ApplicationRegime = new ApplicationRegime();
            ApplicationRegime.Regime = regimeValue;
        }
    }

    public Guid MedicineId { get; set; }
    public Medicine? Medicine { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public bool IsActive { get; set; } = true;

    public override string ToString() => $"{Medicine?.Name} — {Dosage}";
}
