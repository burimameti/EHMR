using EHMR.Domain.Entities;
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
    /// Static factory for the fixed (non-data-driven) patient filter lookups.
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
        ("Активен", "Active"),
        ("Неактивен", "Inactive")
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
    }
    public static class PatientDisplayExtensions
    {
        public static string ToDisplay(this Gender gender) =>
            PatientEnumLookups.Gender.ToDisplay(gender.ToString());

        public static string ToDisplay(this PatientStatus status) =>
            PatientEnumLookups.Status.ToDisplay(status.ToString());
    }

   
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

        public static FilterLookup Status
        {
            get;
        } = new(new[]
        {
        ("Активен", "Active"),
        ("Неактивен", "Inactive"),
        ("Критичен", "Critical")
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
        public static Patient Clone(this Patient source) => new()
        {
            Id=source.Id,
            FirstName=source.FirstName,
            LastName=source.LastName,
            NationalId=source.NationalId,
            BirthDate=source.BirthDate,
            Gender=source.Gender,
            Phone=source.Phone,
            Email=source.Email,
            Address=source.Address,
            City=source.City,
            Status=source.Status
        };

    
    } }
