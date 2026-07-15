using System;
using System.Collections.Generic;

namespace EHMR.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId
    {
        get; set;
    }

    public Patient? Patient
    {
        get; set;
    }

    public Guid DoctorId
    {
        get; set;
    }

    public Doctor? Doctor
    {
        get; set;
    }

    public Guid? TherapyCycleId
    {
        get; set;
    }

    public TherapyCycle? TherapyCycle
    {
        get; set;
    }

    public AppointmentStatus Status
    {
        get; set;
    }

    public DateTime ScheduledStart
    {
        get; set;
    }

    public DateTime ScheduledEnd
    {
        get; set;
    }

    public string ReasonForVisit { get; set; } = "";

    public string ClinicalNotes { get; set; } = "";

    public ICollection<AppointmentDiagnosis> AppointmentDiagnoses { get; set; } = [];

    public override string ToString() =>
    $"{ScheduledStart:dd.MM.yyyy HH:mm} - {ReasonForVisit}";
}

public class AppointmentDiagnosis
{
    public Guid AppointmentId
    {
        get; set;
    }

    public Appointment? Appointment
    {
        get; set;
    }

    public Guid DiagnosisId
    {
        get; set;
    }

    public Diagnosis? Diagnosis
    {
        get; set;
    }

    public Guid Mkb10CodeId
    {
        get; set;
    }

    public Mkb10Code Mkb10Code
    {
        get; set;
    }

    public bool IsPrimary
    {
        get; set;
    }

}

public enum AppointmentStatus
{
    Scheduled, CheckedIn, Completed, Cancelled, Missed, InProgress, ReScheduled,
}