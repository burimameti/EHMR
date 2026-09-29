namespace EHMR.Domain.Entities;
public class Medicine : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;

    /// <summary>ATC code or internal SKU — useful for exact lookups and avoiding duplicate entries.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Route/form: tablet, syrup, injection, cream, etc.</summary>
    public string DosageForm { get; set; } = string.Empty;

    /// <summary>Numeric strength, e.g. 500 — kept separate from Unit so it can be sorted/validated.</summary>
    public decimal Strength
    {
        get; set;
    }

    /// <summary>Unit for Strength: mg, ml, mcg, IU, etc.</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Suggested/default dosage text shown as a hint when attaching to a patient (e.g. "1 tablet twice daily"). Not authoritative — the actual per-patient value lives on PatientMedicine.</summary>
    public string DefaultDosage { get; set; } = string.Empty;

    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>Soft-disable without deleting — keeps historical PatientMedicine rows intact.</summary>
    public bool IsActive { get; set; } = true;

    public string FullName => Strength==0||string.IsNullOrWhiteSpace(Unit)
        ? Name
        : $"{Name} {Strength}{Unit}".Trim();

    public string PharmaceuticalReference
    {
        get;
        internal set;
    }

    public override string ToString() => FullName;
}
