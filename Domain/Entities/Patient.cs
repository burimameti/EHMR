using System;
using System.Collections.Generic;

namespace EHMR.Domain.Entities;

public class Patient : BaseEntity
{
    // Personal
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string SSN { get; set; } = string.Empty;

    public DateTime BirthDate
    {
        get; set;
    }

    // Contact
    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    // Emergency contact
    public string EmergencyContactName { get; set; } = string.Empty;

    public string EmergencyContactPhone { get; set; } = string.Empty;
    public string EmergencyRelationship { get; set; } = string.Empty;

    // Medical
    public string BloodType { get; set; } = string.Empty;

    public string Allergies { get; set; } = string.Empty;

    public PatientStatus Status { get; set; } = PatientStatus.Active;

    // Audit
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    public bool IsDeleted
    {
        get; set;
    }

    public Gender Gender
    {
        get; set;
    }

    public int Age =>
        DateTime.Today.Year-BirthDate.Year-
        (DateTime.Today.DayOfYear<BirthDate.DayOfYear ? 1 : 0);

    public List<Appointment> Appointments { get; set; } = new();
    public ICollection<TherapyCycle> TherapyCycles { get; set; } = [];
    public List<PatientDocument> Documents { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
    public List<Diagnosis> Diagnoses { get; set; } = new();
    public List<Encounter> Encounters { get; set; } = new();

    public string FullName => $"{FirstName} {LastName}";
}

public enum Gender
{
    Male,
    Female,
    Other
}