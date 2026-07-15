using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities;
using System.Windows.Input;

namespace EHMR.ViewModels;

public partial class DashboardViewModel
{
    public enum DashboardMode
    {
        Global, Patient, Admin
    }

    private DashboardPatientAggregate CreateDashboardAggregate(Patient patient)


    {
        return new DashboardPatientAggregate
        {
            Patient=patient,

            Encounters=patient.Encounters
                .OrderByDescending(x => x.ScheduledStart)
                .ToList(),

            Appointments=patient.Appointments
                .OrderBy(x => x.ScheduledStart)
                .ToList(),

            HasAlerts=false // TODO load from Notifications
        };
    }
    public enum DashboardPatientState
    {
        None,

        Scheduled,

        Waiting,

        CheckedIn,

        InProgress,

        Completed,

        Cancelled,

        NoShow,

        Critical
    }

    public class DashboardPatientAggregate
    {
        public required Patient Patient
        {
            get; init;
        }

        public List<Encounter> Encounters { get; init; } = [];

        public List<Appointment> Appointments { get; init; } = [];

        public Encounter? ActiveEncounter =>
            Encounters.FirstOrDefault(x =>
                x.Status==EncounterStatus.Scheduled||
                x.Status==EncounterStatus.CheckedIn||
                x.Status==EncounterStatus.InProgress);

        public Appointment? ActiveAppointment =>
            Appointments
                .Where(x => x.ScheduledStart>=DateTime.Now)
                .OrderBy(x => x.ScheduledStart)
                .FirstOrDefault();

        public List<Encounter> EncounterHistory =>
            Encounters
                .OrderByDescending(x => x.ScheduledStart)
                .ToList();

        public List<Appointment> UpcomingAppointments =>
            Appointments
                .Where(x => x.ScheduledStart>=DateTime.Now)
                .OrderBy(x => x.ScheduledStart)
                .ToList();

        public DateTime? LastActivity =>
            EncounterHistory.FirstOrDefault()?.ScheduledStart;

        public DashboardPatientState State
        {
            get
            {
                if(ActiveEncounter!=null)
                {
                    return ActiveEncounter.Status switch
                    {
                        EncounterStatus.Scheduled => DashboardPatientState.Waiting,
                        EncounterStatus.CheckedIn => DashboardPatientState.CheckedIn,
                        EncounterStatus.InProgress => DashboardPatientState.InProgress,
                        EncounterStatus.Completed => DashboardPatientState.Completed,
                        EncounterStatus.Cancelled => DashboardPatientState.Cancelled,
                        EncounterStatus.NoShow => DashboardPatientState.NoShow,
                        _ => DashboardPatientState.None
                    };
                }

                if(ActiveAppointment!=null)
                    return DashboardPatientState.Scheduled;

                return DashboardPatientState.None;
            }
        }

        public bool HasActiveEncounter => ActiveEncounter!=null;

        public bool HasUpcomingAppointment => ActiveAppointment!=null;

        public bool HasAlerts
        {
            get; set;
        }
    }


}

public partial class DashboardDayItem : ObservableObject
{
    public DateTime Date
    {
        get; set;
    }
    public string DayLabel { get; set; } = string.Empty;
    public string DayNumber { get; set; } = string.Empty;
    [ObservableProperty] private bool isSelected;

    // NEW — how many encounters land on this date, so the strip can show a dot/badge
    // instead of every tile looking identical until you tap one.
    [ObservableProperty] private int encounterCount; 
    public bool HasEncounters => EncounterCount>0;

    public ICommand Command { get; set; } = null!;
    partial void OnEncounterCountChanged(int value) => OnPropertyChanged(nameof(HasEncounters));
}


public partial class DashboardEncounterItem : ObservableObject
{
    public Encounter Source { get; set; } = null!;

    [ObservableProperty] private string patientName = string.Empty;
    [ObservableProperty] private string time = string.Empty;
    [ObservableProperty] private string statusText = string.Empty;

    // Color, not string — same fix applied to DashboardAppointmentItem earlier;
    // BackgroundColor bindings need Color, a hex string fails to convert silently.
    [ObservableProperty] private Color statusColor = Colors.Transparent;
}

