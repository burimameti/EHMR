using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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

        public string Contact => !string.IsNullOrWhiteSpace(Patient.Phone) ? Patient.Phone : Patient.Email??"—";

        public string LatestEncounterStatus => EncounterHistory.FirstOrDefault()?.Status switch
        {
            EncounterStatus.Scheduled => "Закажан",
            EncounterStatus.InProgress => "Во тек",
            EncounterStatus.Completed => "Завршен",
            EncounterStatus.Cancelled => "Откажан",
            EncounterStatus.NoShow => "Не се пријавил",
            _ => "Нема прегледи"
        };

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
                        EncounterStatus.Scheduled => DashboardPatientState.Scheduled,
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


