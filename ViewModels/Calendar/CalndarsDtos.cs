namespace EHMR.ViewModels.Calendar;

public enum CalendarSection
{
    Calendar,
    Day,
    Week,
    Agenda,
    Patients,
    Appointments,
    Encounters,
    Cycles,
    Statistics
}

public enum CalendarMode
{
    Month,
    Week,
    Day,
    Agenda
}

/// <summary>Што прикажува календарот — прегледи или термини.</summary>
public enum CalendarContentMode
{
    Encounters,
    Appointments
}