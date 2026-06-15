using System;

namespace EHMR.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId
    {
        get; set;
    }

    public Patient Patient { get; set; } = null!;

    public Guid DoctorId
    {
        get; set;
    }

    public Doctor Doctor { get; set; } = null!;

    public DateTime ScheduledStart
    {
        get; set;
    }

    public DateTime ScheduledEnd
    {
        get; set;
    }

    public string ReasonForVisit { get; set; } = string.Empty;
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
        = AppointmentStatus.Scheduled;
}