using EHMR.Domain.Entities;
using EHMR.Helpers;
using EHMR.UI.Lookup;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Patients.Extensions
{
    /// <summary>
    /// Bidirectional MK-display <-> internal-value map for a single filter dimension.
    /// Built once per dimension; the VM only consumes ToDisplayList()/ToInternal()/ToDisplay().
    /// </summary>
    public sealed class FilterLookup
    {
        private readonly Dictionary<string, string> _displayToInternal = new();
        private readonly Dictionary<string, string> _internalToDisplay = new();

        public IReadOnlyList<string> DisplayValues
        {
            get;
        }
        public static FilterLookup Empty
        {
            get;
            internal set;
        }

        public FilterLookup(IEnumerable<(string Display, string Internal)> pairs)
        {
            var ordered = pairs.ToList();

            foreach(var (display, @internal) in ordered)
            {
                _displayToInternal[display]=@internal;
                _internalToDisplay[@internal]=display;
            }

            DisplayValues=ordered.Select(p => p.Display).ToList();
        }

        public string ToInternal(string? display) =>
            string.IsNullOrWhiteSpace(display) ? "All" :
            _displayToInternal.TryGetValue(display, out var value) ? value : "All";

        public string ToDisplay(string? internalValue) =>
            string.IsNullOrWhiteSpace(internalValue) ? "Сите" :
            _internalToDisplay.TryGetValue(internalValue, out var value) ? value : "Сите";

        public ObservableCollection<string> ToObservableCollection() =>
            new(DisplayValues);
    }

    /// <summary>
    /// Static factory for the fixed (non-data-driven) patient FILTER lookups —
    /// these all include a "Сите" (All) entry since they drive the list page's
    /// dropdown filters, not data entry. For the create/edit FORM, use
    /// PatientEnumLookups instead (no "All" option there).
    /// City is excluded here since it's built dynamically from MacedoniaCityLookup.
    /// </summary>
    public static class PatientFilterLookups
    {
        public static FilterLookup Status
        {
            get;
        } = new(new[]
        {
        ("Сите", "All"),
        ("Активни", "Active"),
        ("Неактивни", "Inactive")
    
    });

        public static FilterLookup Gender
        {
            get;
        } = new(new[]
        {
        ("Сите", "All"),
        ("Машки", "Male"),
        ("Женски", "Female")
    });

        public static FilterLookup BloodType
        {
            get;
        } = new(new[]
        {
        ("Сите", "All"),
        ("A+", "A+"), ("A-", "A-"),
        ("B+", "B+"), ("B-", "B-"),
        ("AB+", "AB+"), ("AB-", "AB-"),
        ("O+", "O+"), ("O-", "O-")
    });

        public static FilterLookup AgeGroup
        {
            get;
        } = new(new[]
        {
        ("Сите", "All"),
        ("0-18", "0-18"),
        ("19-35", "19-35"),
        ("36-50", "36-50"),
        ("51-65", "51-65"),
        ("65+", "65+")
    });

        /// <summary>Built dynamically since cities come from MacedoniaCityLookup, not a fixed list.</summary>
        public static FilterLookup BuildCityLookup()
        {
            var pairs = new List<(string Display, string Internal)> { ("Сите", "All") };

            foreach(var city in MacedoniaCityLookup.All)
            {
                var internalValue = city?.Key??city?.ToString();
                var displayValue = city?.Display??city?.Key??city?.ToString();

                if(string.IsNullOrWhiteSpace(internalValue)||string.IsNullOrWhiteSpace(displayValue))
                    continue;

                if(pairs.All(p => p.Display!=displayValue))
                    pairs.Add((displayValue, internalValue));
            }

            return new FilterLookup(pairs);
        }

        /// <summary>
        /// Same as BuildCityLookup() but without the "Сите"/All entry — for the
        /// PATIENT FORM city picker, where "All" isn't a valid city to save.
        /// </summary>
        public static FilterLookup BuildCityLookupForForm()
        {
            var pairs = new List<(string Display, string Internal)>();

            foreach(var city in MacedoniaCityLookup.All)
            {
                var internalValue = city?.Key??city?.ToString();
                var displayValue = city?.Display??city?.Key??city?.ToString();

                if(string.IsNullOrWhiteSpace(internalValue)||string.IsNullOrWhiteSpace(displayValue))
                    continue;

                if(pairs.All(p => p.Display!=displayValue))
                    pairs.Add((displayValue, internalValue));
            }

            return new FilterLookup(pairs);
        }
    }

    public static class PatientDisplayExtensions
    {
        public static string ToDisplay(this Gender gender) =>
            PatientEnumLookups.Gender.ToDisplay(gender.ToString());

        public static string ToDisplay(this PatientStatus status) =>
            PatientEnumLookups.Status.ToDisplay(status.ToString());

        public static string ToDisplay(this DosesFrequency frequency) =>
            PatientEnumLookups.DosesFrequency.ToDisplay(frequency.ToString());

        public static string ToDisplay(this TherapyStatus status) => status switch
        {
            TherapyStatus.Planned => "Планирана",
            TherapyStatus.Active => "Активна",
            TherapyStatus.Scheduled => "Закажана",
            TherapyStatus.Completed => "Завршена",
            TherapyStatus.Suspended => "Суспендирана",
            TherapyStatus.Canceled => "Откажана",
            TherapyStatus.Missed => "Пропуштена",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Lookups for DATA ENTRY (create/edit form pickers) — no "Сите"/All entry,
    /// since every one of these is a required field on the entity, not a filter.
    /// Values here must stay in exact sync with the enums in EHMR.Domain.Entities.
    /// </summary>
    public static class PatientEnumLookups
    {
        public static FilterLookup Gender
        {
            get;
        } = new(new[]
        {
        ("Машки", "Male"),
        ("Женски", "Female")
    });

        // Matches PatientStatus: Active, Inactive, Chronic, Deceased
        // (was previously out of sync with the entity - had a nonexistent "Critical").
        public static FilterLookup Status
        {
            get;
        } = new(new[]
        {
        ("Активни", "Active"),
        ("Неактивни", "Inactive")
    });

        // Matches DosesFrequency exactly - used both on the old single-patient
        // frequency picker and now per-row on AttachedMedicineRow.
        public static FilterLookup DosesFrequency
        {
            get;
        } = new(new[]
        {
        ("Дневно", "Daily"),
        ("Двапати", "TwiceDaily"),
        ("Трипати", "ThreeTimesDaily"),
        ("Секој втор ден", "EveryOtherDay"),
        ("Секој трет ден", "EveryThreeDays"),
        ("Неделно", "Weekly"),
        ("Месечно", "Monthly"),
        ("Друго", "Other")
    });

        // Data-entry version of blood type (no "Сите") - use this on the form,
        // PatientFilterLookups.BloodType on the list page's filter dropdown.
        public static FilterLookup BloodType
        {
            get;
        } = new(new[]
        {
        ("A+", "A+"), ("A-", "A-"),
        ("B+", "B+"), ("B-", "B-"),
        ("AB+", "AB+"), ("AB-", "AB-"),
        ("O+", "O+"), ("O-", "O-")
    });
    }

    public static class AgeGroupExtensions
    {
        public static bool IsInAgeGroup(this int age, string ageGroup) => ageGroup switch
        {
            "0-18" => age is >=0 and <=18,
            "19-35" => age is >=19 and <=35,
            "36-50" => age is >=36 and <=50,
            "51-65" => age is >=51 and <=65,
            "65+" => age>65,
            _ => true
        };
    }

    public static class PatientExtensions
    {
        /// <summary>
        /// Creates a snapshot used by Patient edit forms.
        /// Only scalar properties are copied.
        /// Navigation properties and child collections are intentionally excluded.
        /// </summary>
        public static Patient CreateSnapshot(this Patient source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return new Patient
            {
                Id=source.Id,

                FirstName=source.FirstName,
                LastName=source.LastName,

                NationalId=source.NationalId,
                SzboNumber=source.SzboNumber,

                DoctorId=source.DoctorId,

                BirthDate=source.BirthDate,
                Gender=source.Gender,

                Phone=source.Phone,
                Email=source.Email,

                Address=source.Address,
                City=source.City,
                PostalCode=source.PostalCode,

                EmergencyContactName=source.EmergencyContactName,
                EmergencyContactPhone=source.EmergencyContactPhone,
                EmergencyRelationship=source.EmergencyRelationship,

                Allergies=source.Allergies,
                BloodType=source.BloodType,

                Status=source.Status,
                InactiveReason=source.InactiveReason,

                RegistrationDate=source.RegistrationDate,

                IsDeleted=source.IsDeleted,

                CreatedAt=source.CreatedAt
            };
        }
    }
}