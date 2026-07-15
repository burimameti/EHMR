using EHMR.Domain.Entities;
using EHMR.ViewModels.Patients.Extensions; // reuses the shared FilterLookup type


namespace EHMR.ViewModels
{
    /// <summary>
    /// Data-entry lookups for the Medicine catalog form (add/edit a Medicine itself,
    /// not a PatientMedicine attachment - that one uses PatientEnumLookups.DosesFrequency).
    /// No "Сите"/All entries here since these are required fields, not filters.
    /// </summary>
    public static class MedicineEnumLookups
    {
        public static FilterLookup DosageForm
        {
            get;
        } = new(new[]
        {
        ("Таблета", "Tablet"),
        ("Капсула", "Capsule"),
        ("Сируп", "Syrup"),
        ("Инјекција", "Injection"),
        ("Крем", "Cream"),
        ("Маст", "Ointment"),
        ("Капки", "Drops"),
        ("Инхалер", "Inhaler"),
        ("Супозитор", "Suppository"),
        ("Друго", "Other")
    });

        public static FilterLookup Unit
        {
            get;
        } = new(new[]
        {
        ("mg", "mg"),
        ("g", "g"),
        ("ml", "ml"),
        ("mcg", "mcg"),
        ("IU", "IU"),
        ("%", "%")
    });
    }

    /// <summary>
    /// Filter-dimension lookups for a Medicine LIST page (includes "Сите"/All),
    /// mirroring PatientFilterLookups. Add here if/when a MedicineListViewModel
    /// needs dropdown filters by form or manufacturer, etc.
    /// </summary>
    public static class MedicineFilterLookups
    {
        public static FilterLookup DosageForm
        {
            get;
        } = new(new[] { ("Сите", "All") }
            .Concat(MedicineEnumLookups.DosageForm.DisplayValues
                .Select(display => (display, MedicineEnumLookups.DosageForm.ToInternal(display)))));
    }

    public static class MedicineDisplayExtensions
    {
        public static string ToDisplay(this string dosageFormInternal) =>
            MedicineEnumLookups.DosageForm.ToDisplay(dosageFormInternal);
    }

    public static class MedicineExtensions
    {
        /// <summary>Shallow clone for a Medicine catalog edit form's Cancel() snapshot.</summary>
        public static Medicine Clone(this Medicine source) => new()
        {
            Id=source.Id,
            Name=source.Name,
            GenericName=source.GenericName,
            Code=source.Code,
            DosageForm=source.DosageForm,
            Strength=source.Strength,
            Unit=source.Unit,
            DefaultDosage=source.DefaultDosage,
            Manufacturer=source.Manufacturer,
            IsActive=source.IsActive,
            CreatedAt=source.CreatedAt
        };
    }
}