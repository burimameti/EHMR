namespace EHMR.Domain.Entities.Reports
{

    public enum ReportType
    {
        MissedTherapies,
        Auditing,
        AppointmentStatuses,
        Patients
    }


    public enum ReportCategory
    {
        Clinical,
        Operational,
        Security
    }



    public sealed class ReportDefinition
    {

        public string Key
        {
            get;
            init;
        }
            = string.Empty;



        public string Title
        {
            get;
            init;
        }
            = string.Empty;



        public string Description
        {
            get;
            init;
        }
            = string.Empty;



        public string Icon
        {
            get;
            init;
        }
            = string.Empty;



        public ReportCategory Category
        {
            get;
            init;
        }



        public ReportType Type
        {
            get;
            init;
        }



        public Type ProviderType
        {
            get;
            init;
        }
            = null!;

    }





    public sealed class DynamicReportColumn
    {

        public string HeaderName
        {
            get;
            init;
        }
            = string.Empty;



        public string Key
        {
            get;
            init;
        }
            = string.Empty;



        public double Width
        {
            get;
            init;
        }
            = 160;

    }





    public sealed class DynamicReportRow
    {

        public List<string> Cells
        {
            get;
            init;
        }
            = new();



        public bool IsAlertSeverity
        {
            get;
            init;
        }



        public string this[int index]
        {
            get
            {
                if(index<0||index>=Cells.Count)
                    return string.Empty;

                return Cells[index];
            }
        }

    }

}