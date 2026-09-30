namespace EHMR.Domain.Entities.Rbac
{
    public static class AppRoutes
    {
        public const string Login = "login";
        public const string Dashboard = "dashboard";
        public const string Calendar = "calendar";
        public const string CalendarPage = "MainPage";
        public static class Patients
        {
            public const string List = "patients";
            public const string Detail = "patientsdetail";
        }
        public static class Admin
        {
            public const string AdminPanel = "AdminPage";

        }

        public static class Backup
        {
            public const string Dashboard = "BackupDashboardPage";
            public const string History = "BackupHistoryPage";
            public const string BackupDetails = "BackupDetailsPage";
            public const string Backups = "BackupPage";
            public const string Restore = "RestorePage";
            public const string Destinations = "BackupDestinationsPage";
        }

        public static class Doctors
        {
            public const string List = "doctorslist";
            public const string Detail = "doctorsdetail";
        }
        public static class Mkb10
        {
            public const string List = "mkb10Codelist";
            public const string Detail = "mkb10Codedetail";

        }
        public static class Encounters
        {
            public const string List = "encounterslist";
            public const string Create = "encounterscreate";
            public const string Edit = "encountersedit";
            public const string Detail = "encountersdetail";
        }
        public static class Protocols
        {
            public const string List = "protocolslist";
            public const string Detail = "protocoldetail";
        }

        public static class Diagnoses
        {
            public const string List = "diagnoseslist";
        }

        public static class Documents
        {
            public const string List = "documentslist";
            public const string Preview = "documentslist";
        }

        public static class Medicines
        {
            public const string List = "medicineslist";
            public const string Detail = "medicinesdetail";
        }

        public static class ApplicationRegimes
        {
            public const string List = "applicationregimeslist";
        }

        public static class Prescriptions
        {
            public const string List = "prescriptionslist";
            public const string Detail = "prescriptiondetail";
        }

        public static class Users
        {
            public const string List = "userslist";
            public const string Detail = "userdetail";
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
            public const string Detail = "therapydetail";
        }

        public static class Mkb10Codes
        {
            public const string List = "mkbcodes";

        }

        public static class Reports
        {
            public const string List = "reportlist"; public const string Detail = "reportdetail";
        }
    }
}