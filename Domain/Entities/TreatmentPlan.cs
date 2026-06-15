using System;
using System.Collections.Generic;

namespace EHMR.Domain.Entities;

public class TreatmentPlan : BaseEntity
{
    public Guid PatientId
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

    public Patient? Patient
    {
        get; set;
    }

    public string ProtocolName { get; set; } = string.Empty; // e.g., "R-CHOP Regimen"

    public DateTime StartDate
    {
        get; set;
    }

    public DateTime EndDate
    {
        get; set;
    }

    public Guid TherapyProtocolId
    {
        get; set;
    }

    public TherapyProtocol? TherapyProtocol
    {
        get; set;
    }

    public TherapyStatus Status { get; set; } = TherapyStatus.Planned;

    // The schedules managed under this plan
    public ICollection<TherapySchedule> TherapySchedules { get; set; } = new List<TherapySchedule>();
}