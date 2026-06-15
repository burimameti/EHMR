using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Constants
{
    public static class AppRoutes
    {
        public const string Login = "login";
        public const string Dashboard = "dashboard";
        public const string MedicalRecords = "medical-records";

        //  public const string Patients = "patients";
        public const string Calendar = "calendar";

        public const string MedicineRule = "medicinerules";

        //public const string Appointments = "appointments";

        public static class Patients
        {
            public const string List = "patientslist";
            public const string Detail = "patientsdetail";
        }

        public static class Medicines
        {
            public const string List = "medicineslist";
            public const string Detail = "medicinesdetail";
        }

        public static class Prescriptions
        {
            public const string List = "prescriptionslist";
            public const string Detail = "prescriptionsdetail";
        }

        public static class Users
        {
            public const string List = "userslist";
            public const string Detail = "usersdetail";
            public const string Roles = "roles";
        }

        public static class Appointments
        {
            public const string List = "appointmentslist";
            public const string Detail = "appointmentsdetail";
        }

        public static class Therapy
        {
            public const string List = "therapylist";
            public const string Details = "therapydetails";
        }

        public static class TherapyCycle
        {
            public const string List = "therapycycles";
            public const string Details = "therapycycledetails";
        }

        public static class Cycle
        {
            public const string List = "cyclelist";
            public const string Details = "cycledetails";
        }

        public static class Schedules
        {
            public const string List = "scheduleslist";
            public const string Details = "schedulesdetails";
        }

        public static class Protocols
        {
            public const string List = "protocolslist";
            public const string Details = "protocolsdetails";
        }

        public static class Plans
        {
            public const string List = "planslist";
            public const string Details = "plansdetails";
        }

        public static class Reports
        {
            public const string List = "reportslist";
            public const string Details = "reportsdetails";
        }

        // PHARMACY
        public const string Pharmacy = "pharmacy";

        // LAB
        public const string LabResults = "labresults";

        // ADMIN
        // public const string Users = "admin/users";

        // OPTIONAL DETAIL ROUTES

        public const string RecordDetails = "medical-records/details";
    }

    public enum AppAction
    {
        View,
        Create,
        Edit,
        Delete,
        Manage,
        Approve,
        Assign,
        Export
    }

    public static class AppResource
    {
        public const string Patients = "Patients";
        public const string Appointments = "Appointments";
        public const string Therapies = "Therapies";
        public const string Prescriptions = "Prescriptions";
        public const string Tasks = "Tasks";
        public const string Alerts = "Alerts";
        public const string Encounters = "Encounters";
        public const string Reports = "Reports";
        public const string Dashboard = "Dashboard";
    }

    public sealed class PermissionPolicy
    {
        public string Resource { get; init; } = default!;

        public AppAction Action
        {
            get; init;
        }

        public string Permission { get; init; } = default!;
        public string Module { get; init; } = default!;
    }

    public static class PolicyRegistry
    {
        public static readonly IReadOnlyList<PermissionPolicy> Policies = new List<PermissionPolicy>
    {
        // ================= PATIENTS =================
        new()
        {
            Resource = AppResource.Patients,
            Action = AppAction.View,
            Permission = "patients.view",
            Module = AppResource.Patients
        },
        new()
        {
            Resource = AppResource.Patients,
            Action = AppAction.Manage,
            Permission = "patients.manage",
            Module = AppResource.Patients
        },

        // ================= APPOINTMENTS =================
        new()
        {
            Resource = AppResource.Appointments,
            Action = AppAction.View,
            Permission = "appointments.view",
            Module = AppResource.Appointments
        },

        // ================= THERAPIES =================
        new()
        {
            Resource = AppResource.Therapies,
            Action = AppAction.Manage,
            Permission = "therapy.manage",
            Module = AppResource.Therapies
        },

        // ================= PRESCRIPTIONS =================
        new()
        {
            Resource = AppResource.Prescriptions,
            Action = AppAction.Approve,
            Permission = "prescriptions.approve",
            Module = AppResource.Prescriptions
        },

        // ================= TASKS =================
        new()
        {
            Resource = AppResource.Tasks,
            Action = AppAction.Assign,
            Permission = "tasks.assign",
            Module = AppResource.Tasks
        },

        // ================= REPORTS =================
        new()
        {
            Resource = AppResource.Reports,
            Action = AppAction.Export,
            Permission = "reports.export",
            Module = AppResource.Reports
        }
    };
    }
}