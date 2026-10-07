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

            CurrentDiagnoses=patient.Diagnoses
                .Where(x => x.Status==DiagnosisStatus.Active||x.Status==DiagnosisStatus.Chronic)
                .OrderByDescending(x => x.IsPrimary)
                .ThenByDescending(x => x.DiagnosedAt)
                .Select(x =>
                    !string.IsNullOrWhiteSpace(x.Mkb10Code?.Code)
                        ? $"{x.Mkb10Code.Code} — {x.Mkb10Code.Description}"
                        : !string.IsNullOrWhiteSpace(x.ClinicalDescription)
                            ? x.ClinicalDescription
                            : x.DiagnosisNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray(),

            CurrentMedicines=patient.PatientMedicines
                .Where(x => x.IsActive&&x.Medicine!=null)
               
                .Select(x =>
                    string.IsNullOrWhiteSpace(x.Dosage)
                        ? x.Medicine!.FullName
                        : $"{x.Medicine.FullName} — {x.Dosage}")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray(),

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

        public string[] CurrentDiagnoses { get; init; } = [];

        public string[] CurrentMedicines { get; init; } = [];

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
                        _ => DashboardPatientState.None
                    };
                }

                if(ActiveAppointment!=null)
                    return DashboardPatientState.Scheduled;

                // If there is no active appointment/encounter, still expose the
                // latest recorded encounter status instead of falling back to "—".
                // This keeps completed/cancelled visits visible in the patient grid.
                var latest = EncounterHistory.FirstOrDefault();
                return latest?.Status switch
                {
                    EncounterStatus.Completed => DashboardPatientState.Completed,
                    EncounterStatus.Cancelled => DashboardPatientState.Cancelled,
                    EncounterStatus.InProgress => DashboardPatientState.InProgress,
                    EncounterStatus.Scheduled => DashboardPatientState.Scheduled,
                    _ => DashboardPatientState.None
                };
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


