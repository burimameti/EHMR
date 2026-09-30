namespace EHMR.Domain.Entities;

public class ApplicationRegime : BaseEntity
{
    public string Regime { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<PatientMedicine> PatientMedicines { get; set; } = [];
}
