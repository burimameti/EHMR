using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities.Rbac
{
    public static class AppGroupIcons
    {
        public static IconDefinition Overview => new("\xf0e4", IconFontType.FontAwesomeSolid);
        public static IconDefinition Patients => new("\xf4a6", IconFontType.FontAwesomeSolid);
        public static IconDefinition Appointment => new("\xf073", IconFontType.FontAwesomeSolid);
        public static IconDefinition MedicalRecords => new("\xf47a", IconFontType.FontAwesomeSolid);
        public static IconDefinition Prescriptions => new("\xf46b", IconFontType.FontAwesomeSolid);
        public static IconDefinition Report => new("\xf47e", IconFontType.FontAwesomeSolid);
        public static IconDefinition Laboratory => new("\xf491", IconFontType.FontAwesomeSolid);
        public static IconDefinition Administration => new("\xf508", IconFontType.FontAwesomeSolid);
    }

    public static class AppIcons
    {
        public static IconDefinition Dashboard => new("\xf0e4", IconFontType.FontAwesomeSolid);    // tachometer
        public static IconDefinition Patients => new("\xf4a6", IconFontType.FontAwesomeSolid);     // user-injured
        public static IconDefinition Records => new("\xf47a", IconFontType.FontAwesomeSolid);      // notes-medical
        public static IconDefinition Therapy => new("\xf21e", IconFontType.FontAwesomeSolid);      // pills
        public static IconDefinition Prescription => new("\xf46b", IconFontType.FontAwesomeSolid); // file-medical
        public static IconDefinition Report => new("\xf47e", IconFontType.FontAwesomeSolid);     // mortar-pestle
        public static IconDefinition Lab => new("\xf491", IconFontType.FontAwesomeSolid);
        public static IconDefinition Appointment => new("\xf073", IconFontType.FontAwesomeSolid);         // microscope
        public static IconDefinition Users => new("\xf508", IconFontType.FontAwesomeSolid);        // users-cog
        public static IconDefinition Roles => new("\xf505", IconFontType.FontAwesomeSolid);        // user-shield
    }
}