using System;
using System.Collections.Generic;

namespace EHMR.Domain.Entities;

public class Appointment : BaseEntity
{
    public string AppointmentNumber { get; set; } = "";
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

    public Encounter? Encounter
    {
        get; set;
    }

    public override string ToString() =>
    $"{ScheduledStart:dd.MM.yyyy HH:mm} - {ReasonForVisit}";
}



public enum AppointmentStatus
{
    Scheduled = 0,
    CheckedIn = 1,
    Completed = 2,
    Cancelled = 3,
    Missed = 4,
    InProgress = 5,
    ReScheduled = 6,
}