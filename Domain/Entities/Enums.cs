namespace EHMR.Domain.Entities;

public enum PatientStatus
{
    Active, Inactive, Discharged, Deceased, Chronic, Recovered, UnderObservation
}

public enum TherapyStatus
{
    Planned,
    Active,
    Scheduled,
    Completed,
    Suspended,
    Canceled,
    Missed
}

public enum DoseStatus
{
    Planned = 1,
    Prepared = 2,
    Administered = 3,
    Missed = 4,
    Cancelled = 5,
    Skipped = 6,
    Delayed = 7,
    Pending = 8,
    Failed = 9,
}

public enum UserRole
{
    Admin, SuperAdmin, Doctor, MainNurse, Nurse, Staff
}