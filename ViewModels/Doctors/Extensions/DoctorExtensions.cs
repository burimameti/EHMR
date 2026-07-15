using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.ViewModels.Patients.Extensions;
using System.Collections.Generic;

namespace EHMR.ViewModels.Doctors.Extensions
{
    /// <summary>
    /// Static factory for the fixed (non-data-driven) doctor FILTER lookups —
    /// these include a "Сите" (All) entry since they drive the list page's
    /// dropdown/tab filters, not data entry.
    ///
    /// Reuses the existing FilterLookup type from
    /// EHMR.ViewModels.Patients.Extensions rather than duplicating it — the
    /// class is entity-agnostic (just string<->string pairs), so there's no
    /// need for a Doctors-specific copy.
    ///
    /// NOTE: Doctor doesn't currently have a Status enum in this codebase —
    /// "active/inactive" is tracked via the IsActive bool. Status here still
    /// gives us a proper internal<->display pair ("Active"/"Inactive") so the
    /// list VM can compare against a stable internal value instead of hard-
    /// coding Cyrillic strings, and the mapping to IsActive happens once, in
    /// ApplyFilters/RefreshSparkTabCounts.
    /// </summary>
    public static class DoctorFilterLookups
    {
        public static FilterLookup Status
        {
            get;
        } = new(new[]
        {
            ("Сите", "All"),
            ("Активен", "Active"),
            ("Неактивен", "Inactive"),
        });

        /// <summary>
        /// Fixed specialty list. Internal values are the strings assumed to be
        /// stored on Doctor.Specialty — adjust/replace with a Build...Lookup()
        /// (data-driven, e.g. from a SpecialtyLookup source) if specialties
        /// come from the database rather than a fixed set.
        /// </summary>
        public static FilterLookup Specialty
        {
            get;
        } = new(new[]
        {
            ("Сите", "All"),
            ("Ревматологија", "Rheumatology"),
            ("Кардиологија", "Cardiology"),
            ("Неврологија", "Neurology"),
            ("Психијатрија", "Psychiatry"),
            ("Психологија", "Psychology"),
        });
    

    public static Doctor Clone(this Doctor source)
        {
            if(source is null)
                return new Doctor { User=new User() };

            return new Doctor
            {
                Id=source.Id,
                Specialty=source.Specialty,
                LicenseNumber=source.LicenseNumber,
                ContactPhone=source.ContactPhone,
                Email=source.Email,
                Gender=source.Gender,
                Status=source.Status,
                User=source.User is null
                    ? new User()
                    : new User
                    {
                        Id=source.User.Id,
                        FirstName=source.User.FirstName,
                        LastName=source.User.LastName,
                        // add any other User properties your entity has
                    }
            };
        }
    } }