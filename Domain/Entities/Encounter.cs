namespace EHMR.Domain.Entities;

public class Encounter
{
    public Guid Id
    {
        get; set;
    }

    // =========================================================
    // Relationships
    // =========================================================

    public Guid PatientId
    {
        get; set;
    }

    public Guid? AppointmentId
    {
        get; set;
    }

    public Guid DoctorId
    {
        get; set;
    }

  

    // =========================================================
    // Identification
    // =========================================================

    public string EncounterNumber { get; set; } = string.Empty;

    /// <summary>
    /// Outpatient, Inpatient, Emergency, Telehealth, FollowUp...
    /// </summary>
    public string EncounterType { get; set; } = "Outpatient";

    // =========================================================
    // Workflow
    // =========================================================

    /// <summary>
    /// Scheduled, CheckedIn, Waiting, InProgress,
    /// Completed, Cancelled, NoShow
    /// </summary>
    public EncounterStatus Status { get; set; }

    /// <summary>
    /// Routine, Urgent, Emergency, STAT
    /// </summary>
    public string Priority { get; set; } = "Routine";

    // =========================================================
    // Schedule
    // =========================================================

    public DateTime? ScheduledStart
    {
        get; set;
    }

    public DateTime? ScheduledEnd
    {
        get; set;
    }

    // =========================================================
    // Visit Timeline
    // =========================================================

    public DateTime? CheckInTime
    {
        get; set;
    }

    public DateTime? StartTime
    {
        get; set;
    }

    public DateTime? EndTime
    {
        get; set;
    }

    public DateTime? CheckOutTime
    {
        get; set;
    }

    public int? DurationMinutes
    {
        get; set;
    }

    // =========================================================
    // Clinical
    // =========================================================

    public string? ChiefComplaint
    {
        get; set;
    }

    public string? ReasonForVisit
    {
        get; set;
    }

    public string? HistoryOfPresentIllness
    {
        get; set;
    }

    public string? Assessment
    {
        get; set;
    }

    public string? Plan
    {
        get; set;
    }

    public string? Notes
    {
        get; set;
    }

    // =========================================================
    // Administrative
    // =========================================================

    /// <summary>
    /// Referral, WalkIn, Appointment, Transfer
    /// </summary>
    public string? VisitSource
    {
        get; set;
    }

    /// <summary>
    /// Pending, ReadyForBilling, Billed, Paid
    /// </summary>
    public string? BillingStatus
    {
        get; set;
    }

    public bool IsLocked
    {
        get; set;
    }

    public bool IsActive { get; set; } = true;

    // =========================================================
    // Legacy Compatibility
    // =========================================================

    /// <summary>
    /// Legacy field.
    /// Can map to ScheduledStart or StartTime.
    /// </summary>
    public DateTime EncounterDate
    {
        get; set;
    }

    // =========================================================
    // Audit
    // =========================================================

    public DateTime CreatedAt
    {
        get; set;
    }

    public DateTime? UpdatedAt
    {
        get; set;
    }

    public Guid CreatedBy
    {
        get; set;
    }

    public Guid? UpdatedBy
    {
        get; set;
    }
    public Guid? TherapyCycleId
    {
        get; set;
    }
    public virtual TherapyCycle? TherapyCycle
    {
        get; set;
    }
    // =========================================================
    // Navigation Properties
    // =========================================================

    public virtual Patient? Patient
    {
        get; set;
    }

    public virtual Appointment? Appointment
    {
        get; set;
    }

    public virtual Doctor? Doctor
    {
        get; set;
    }
    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();

    public virtual ICollection<Medicine> MedicationOrders { get; set; } = new List<Medicine>();


    public string? ClinicalNotes { get; set; } = string.Empty;

    public virtual ICollection<PatientDocument> Attachments { get; set; } = new List<PatientDocument>();

  
}

public enum EncounterStatus
{
    Scheduled = 0,
    CheckedIn = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5
}