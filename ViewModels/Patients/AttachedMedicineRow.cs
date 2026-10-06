using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities;
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
            PatientMedicine = patientMedicine;
        }

        public string MedicineName => PatientMedicine.MedicineName;
        public string GenericName => PatientMedicine.GenericName;
        public string Code => PatientMedicine.Code;
        public string DosageForm => PatientMedicine.DosageForm;
        public string StrengthDisplay => PatientMedicine.Strength == 0 ? "" : $"{PatientMedicine.Strength:g} {PatientMedicine.Unit}".Trim();
        public string DefaultDosage => PatientMedicine.DefaultDosage;
        public string Manufacturer => PatientMedicine.Manufacturer;
        public string? ApplicationRegime => PatientMedicine?.ApplicationRegime;
        public decimal Quantity => PatientMedicine.Quantity;
        public string Dosage => PatientMedicine.Dosage;
        public bool IsActive => PatientMedicine.IsActive;
        public bool CanDelete { get; private set; } = false;
        public Guid? ResolutionDocumentId => PatientMedicine.ResolutionDocumentId;

        public void MarkNew()
        {
            CanDelete=true;
            OnPropertyChanged(nameof(CanDelete));
        }

        public void MarkPersisted()
        {
            CanDelete=false;
            OnPropertyChanged(nameof(CanDelete));
        }

        public string RegimeDisplay =>
            string.IsNullOrWhiteSpace(PatientMedicine.ApplicationRegime)
                ? "Избери"
                : PatientMedicine.ApplicationRegime;
    }
}