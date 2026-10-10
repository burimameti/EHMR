using System.ComponentModel.DataAnnotations.Schema;

namespace EHMR.Domain.Entities;

/// <summary>
/// Join entity between Patient and Medicine. Carries the per-patient
/// prescribing details (frequency and dosage) that don't
/// belong on the Medicine catalog entity itself, since the same medicine
/// can be prescribed differently to different patients.
/// </summary>
public class PatientMedicine : BaseEntity
{
    public Guid PatientId
    {
        get; set;
    }
    public Patient? Patient
    {
        get; set;
    }

    public Guid? EncounterId { get; set; }
    public Encounter? Encounter { get; set; }

    public Guid? ApplicationRegimeId { get; set; }
    public ApplicationRegime? ApplicationRegime { get; set; }

    public Guid? ResolutionDocumentId { get; set; }
    public PatientDocument? ResolutionDocument { get; set; }

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

    public Guid MedicineId
    {
        get; set;
    }
    public Medicine? Medicine
    {
        get; set;
    }


    /// <summary>How often THIS patient takes THIS medicine — independent of any other medicine they're on.</summary>
    public DosesFrequency DosesFrequency { get; set; } = DosesFrequency.Daily;

    /// <summary>Free-text actual dosage for this patient, e.g. "1 tablet" or "5ml" — defaults from Medicine.DefaultDosage but can be overridden.</summary>
    public string Dosage { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public string PharmaceuticalReference { get; set; } = string.Empty;

    /// <summary>Start date of this patient-specific medicine record; defaults to the date it is added.</summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>Patient-specific quantity, matching the numeric quantity used in encounter medicine entry.</summary>
    public decimal Quantity { get; set; } = 1m;

    /// <summary>False once discontinued/completed — kept instead of deleting so prescription history survives.</summary>
    public bool IsActive { get; set; } = true;

    public override string ToString() =>
        $"{Medicine?.Name} — {Dosage} ({DosesFrequency})";
}
