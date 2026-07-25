using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Services.Dto;

namespace EHMR.ViewModels
{
    public partial class AttachedMedicineRow : ObservableObject
    {
        public PatientMedicineDto PatientMedicine
        {
            get;
        }

        public AttachedMedicineRow(PatientMedicineDto patientMedicine)
        {
            PatientMedicine=patientMedicine;
        }

        public string MedicineName => PatientMedicine.MedicineName;
        public string Dosage => PatientMedicine.Dosage;
        public bool IsActive => PatientMedicine.IsActive;
    }
}