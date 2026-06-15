namespace EHMR.Domain.Entities;

public class TherapySchedule : BaseEntity
{
    public Guid TreatmentPlanId
    {
        get; set;
    }

    public TreatmentPlan TreatmentPlan { get; set; } = null!;

    public string Name { get; set; } = string.Empty; // e.g., "Cyclical Cocktail Phase 1"

    public int FrequencyInDays
    {
        get; set;
    }        // e.g., Repeat every 21 days

    public int GraceDays
    {
        get; set;
    }

    public bool IsActive { get; set; } = true;

    public DateTime StartDate
    {
        get; set;
    }

    public DateTime? EndDate
    {
        get; set;
    }

    // MANY MEDICINES LINKED TO THIS SCHEDULE
    public ICollection<ScheduleMedicineRule> MedicineRules { get; set; } = new List<ScheduleMedicineRule>();

    public ICollection<TherapyCycle> Cycles { get; set; } = new List<TherapyCycle>();
}