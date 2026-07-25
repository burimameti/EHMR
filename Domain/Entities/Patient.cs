using System;

namespace EHMR.Domain.Entities;

/// <summary>
/// Represents the master patient record.
/// Holds demographic information and references to the patient's clinical history.
/// </summary>
public class Patient : BaseEntity
{
    #region Personal Information
    public string PatientNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string NationalId { get; set; } = string.Empty;

    public DateTime BirthDate
    {
        get; set;
    }

    public Gender Gender
    {
        get; set;
    }

    #endregion

    #region Primary Doctor

    /// <summary>
    /// Primary physician responsible for this patient.
    /// </summary>
    public Guid DoctorId
    {
        get; set;
    }

    public Doctor? Doctor
    {
        get; set;
    }

    #endregion

    #region Contact Information

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    #endregion

    #region Emergency Contact

    public string EmergencyContactName { get; set; } = string.Empty;

    public string EmergencyContactPhone { get; set; } = string.Empty;

    public string EmergencyRelationship { get; set; } = string.Empty;

    #endregion

    #region Medical Summary

    public string BloodType { get; set; } = string.Empty;

    public string Allergies { get; set; } = string.Empty;

    public PatientStatus Status { get; set; } = PatientStatus.Active;

    #endregion

    #region Audit

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    public bool IsDeleted
    {
        get; set;
    }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Patient diagnoses (ICD-10 / MKB-10).
    /// </summary>
    public ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();

    /// <summary>
    /// Current and historical medications prescribed to the patient.
    /// </summary>
    public ICollection<PatientMedicine> PatientMedicines { get; set; } = new List<PatientMedicine>();

    public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
    /// <summary>
    /// Patient appointments.
    /// </summary>
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<TherapyCycle> TherapyCycles { get; set; } = [];
    public ICollection<Prescription> Prescriptions { get; set; } = [];
    public ICollection<Encounter> Encounters { get; set; } = [];

    #endregion

    #region Computed Properties

    public string FullName => $"{FirstName} {LastName}";

    public int Age =>
        DateTime.Today.Year
        -BirthDate.Year
        -(DateTime.Today.DayOfYear<BirthDate.DayOfYear ? 1 : 0);

    /// <summary>
    /// Last completed appointment.
    /// </summary>
    public DateTime? LastVisitDate =>
        Appointments?.Count>0
            ? Appointments
                .Where(a => a.Status==AppointmentStatus.Completed)
                .OrderByDescending(a => a.ScheduledStart)
                .Select(a => (DateTime?)a.ScheduledStart)
                .FirstOrDefault()
            : null;

    /// <summary>
    /// Next scheduled appointment.
    /// </summary>

    public DateTime? NextAppointmentDate =>
        Appointments
            .Where(a => a.Status==AppointmentStatus.Scheduled
                     &&a.ScheduledStart>DateTime.UtcNow)
            .OrderBy(a => a.ScheduledStart)
            .Select(a => (DateTime?)a.ScheduledStart)
            .FirstOrDefault();

    public override string ToString() => FullName;

    #endregion
}

public enum Gender
{
    Male,
    Female,
    Other
}

public enum DosesFrequency
{
    Daily,
    TwiceDaily,
    ThreeTimesDaily,
    EveryOtherDay,
    EveryThreeDays,
    Weekly,
    Monthly,
    Other
}