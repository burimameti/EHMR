namespace EHMR.Domain.Entities;

public class Encounter
{
    public const int DefaultDurationMinutes = 30;

    public Encounter()
    {
        var start=RoundUpToHalfHour(DateTime.Now);
        Schedule(start);
        CreatedAt=DateTime.Now;
    }

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
    public string EncounterType { get; set; } = "Амбулантски";

    // =========================================================
    // Workflow
    // =========================================================

    /// <summary>
    /// Scheduled, CheckedIn, Waiting, InProgress,
    /// Completed, Cancelled, NoShow
    /// </summary>
    public EncounterStatus Status { get; set; } = EncounterStatus.Scheduled;

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


    public string? ClinicalNotes { get; set; }

    public virtual ICollection<PatientDocument> Attachments { get; set; } = new List<PatientDocument>();

    public void Schedule(DateTime start, int durationMinutes = DefaultDurationMinutes)
    {
        if(durationMinutes<=0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Времетраењето мора да биде поголемо од нула.");

        Status=EncounterStatus.Scheduled;
        EncounterDate=start;
        ScheduledStart=start;
        ScheduledEnd=start.AddMinutes(durationMinutes);
        DurationMinutes=durationMinutes;
    }

    public void Complete(DateTime completedAt)
    {
        Status=EncounterStatus.Completed;
        EndTime=completedAt;
        IsLocked=true;

        var start=StartTime??ScheduledStart;
        DurationMinutes=start.HasValue
            ? Math.Max(0, (int)(completedAt-start.Value).TotalMinutes)
            : DurationMinutes;
    }

    public void SetNotes(string? notes)
    {
        var normalized=string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Notes=normalized;
        ClinicalNotes=normalized;
    }

    private static DateTime RoundUpToHalfHour(DateTime value)
    {
        var rounded=value.Date.AddHours(value.Hour);
        rounded=rounded.AddMinutes(value.Minute<30 ? 30 : 60);
        return DateTime.SpecifyKind(rounded, value.Kind);
    }

  
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
