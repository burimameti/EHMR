namespace EHMR.Domain.Entities;

// THE BRIDGE TABLE: Defines individual drug parameters inside the recurring sequence
public class ScheduleMedicineRule : BaseEntity
{
    public Guid TherapyScheduleId
    {
        get; set;
    }

    public TherapySchedule TherapySchedule { get; set; } = null!;

    public Guid MedicineId
    {
        get; set;
    }

    public Medicine Medicine { get; set; } = null!;

    public string Dosage { get; set; } = string.Empty;       // e.g., "75 mg/m2" or "375 mg/m2"

    public int AdministrationDayOffset
    {
        get; set;
    }         // e.g., Day 1 of the cycle, Day 5 of the cycle, etc.

    public int Quantity
    {
        get; set;
    }
}