using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities;
using EHMR.ViewModels.Patients.Extensions;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

/// <summary>
/// Wraps a PatientMedicine so each attached-medicine row in the UI can bind
/// its own DosesFrequency picker independently, without EncounterFormLookups-style
/// static lookups needing to know about "rows".
/// </summary>
public partial class AttachedMedicineRow : ObservableObject
{
    public PatientMedicine PatientMedicine
    {
        get;
    }

    public AttachedMedicineRow(PatientMedicine patientMedicine)
    {
        PatientMedicine=patientMedicine;
        dosage=patientMedicine.Dosage;
        dosesFrequencyDisplay=PatientEnumLookups.DosesFrequency.ToDisplay(patientMedicine.DosesFrequency.ToString());
    }

    public string MedicineName => PatientMedicine.Medicine?.Name??"(?)";
    public string MedicineForm => PatientMedicine.Medicine?.DosageForm??string.Empty;

    [ObservableProperty] private string dosage;
    partial void OnDosageChanged(string value) => PatientMedicine.Dosage=value??string.Empty;

    [ObservableProperty] private string dosesFrequencyDisplay;
    partial void OnDosesFrequencyDisplayChanged(string value)
    {
        var internalValue = PatientEnumLookups.DosesFrequency.ToInternal(value);
        if(Enum.TryParse<DosesFrequency>(internalValue, out var parsed))
            PatientMedicine.DosesFrequency=parsed;
    }

    public ObservableCollection<string> DosesFrequencyOptions { get; } = PatientEnumLookups.DosesFrequency.ToObservableCollection();
}
