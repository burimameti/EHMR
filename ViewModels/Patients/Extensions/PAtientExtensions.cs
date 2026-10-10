using EHMR.Domain.Entities;
using EHMR.Helpers;
using EHMR.UI.Lookup;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EHMR.ViewModels.Patients.Extensions;

/// <summary>
/// Bidirectional display/internal-value map for one patient filter dimension.
/// </summary>
public sealed class FilterLookup
{
    private readonly Dictionary<string, string> _displayToInternal = new();
    private readonly Dictionary<string, string> _internalToDisplay = new();

    public IReadOnlyList<string> DisplayValues { get; }

    public static FilterLookup Empty { get; internal set; } = new(Array.Empty<(string, string)>());

    public FilterLookup(IEnumerable<(string Display, string Internal)> pairs)
    {
        var ordered = pairs.ToList();

        foreach(var (display, internalValue) in ordered)
        {
            _displayToInternal[display] = internalValue;
            _internalToDisplay[internalValue] = display;
        }

        DisplayValues = ordered.Select(p => p.Display).ToList();
    }

    public string ToInternal(string? display) =>
        string.IsNullOrWhiteSpace(display) ? "All" :
        _displayToInternal.TryGetValue(display, out var value) ? value : "All";

    public string ToDisplay(string? internalValue) =>
        string.IsNullOrWhiteSpace(internalValue) ? "Сите" :
        _internalToDisplay.TryGetValue(internalValue, out var value) ? value : "Сите";

    public ObservableCollection<string> ToObservableCollection() => new(DisplayValues);
}

/// <summary>
/// Fixed patient-list filter lookups. These include "Сите" because they are
/// filters, not values saved to a patient record. City values are data-driven.
/// </summary>
public static class PatientFilterLookups
{
    public static FilterLookup Status { get; } = new(new[]
    {
        ("Сите", "All"),
        ("Активни", "Active"),
        ("Неактивни", "Inactive")
    });

    public static FilterLookup Gender { get; } = new(new[]
    {
        ("Сите", "All"),
        ("Машки", "Male"),
        ("Женски", "Female")
    });

    public static FilterLookup BloodType { get; } = new(new[]
    {
        ("Сите", "All"),
        ("A+", "A+"), ("A-", "A-"),
        ("B+", "B+"), ("B-", "B-"),
        ("AB+", "AB+"), ("AB-", "AB-"),
        ("O+", "O+"), ("O-", "O-")
    });

    public static FilterLookup AgeGroup { get; } = new(new[]
    {
        ("Сите", "All"),
        ("0-18", "0-18"),
        ("19-35", "19-35"),
        ("36-50", "36-50"),
        ("51-65", "51-65"),
        ("65+", "65+")
    });

    public static FilterLookup BuildCityLookup() => BuildCityLookupCore(includeAll: true);

    /// <summary>City picker values for patient forms; excludes the filter-only "Сите" option.</summary>
    public static FilterLookup BuildCityLookupForForm() => BuildCityLookupCore(includeAll: false);

    private static FilterLookup BuildCityLookupCore(bool includeAll)
    {
        var pairs = new List<(string Display, string Internal)>();
        if(includeAll)
            pairs.Add(("Сите", "All"));

        foreach(var city in MacedoniaCityLookup.All)
        {
            var internalValue = city?.Key ?? city?.ToString();
            var displayValue = city?.Display ?? city?.Key ?? city?.ToString();

            if(string.IsNullOrWhiteSpace(internalValue) || string.IsNullOrWhiteSpace(displayValue))
                continue;

            if(pairs.All(p => p.Display != displayValue))
                pairs.Add((displayValue, internalValue));
        }

        return new FilterLookup(pairs);
    }
}

/// <summary>Display helpers for patient fields.</summary>
public static class PatientDisplayExtensions
{
    public static string ToDisplay(this Gender gender) =>
        PatientEnumLookups.Gender.ToDisplay(gender.ToString());

    public static string ToDisplay(this PatientStatus status) =>
        PatientEnumLookups.Status.ToDisplay(status.ToString());
}

/// <summary>
/// Lookups for patient create/edit fields. Unlike list filters, these do not
/// contain the filter-only "Сите" option.
/// </summary>
public static class PatientEnumLookups
{
    public static FilterLookup Gender { get; } = new(new[]
    {
        ("Машки", "Male"),
        ("Женски", "Female")
    });

    public static FilterLookup Status { get; } = new(new[]
    {
        ("Активни", "Active"),
        ("Неактивни", "Inactive")
    });

    public static FilterLookup BloodType { get; } = new(new[]
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
        "0-18" => age is >= 0 and <= 18,
        "19-35" => age is >= 19 and <= 35,
        "36-50" => age is >= 36 and <= 50,
        "51-65" => age is >= 51 and <= 65,
        "65+" => age > 65,
        _ => true
    };
}

public static class PatientExtensions
{
    /// <summary>
    /// Creates a scalar-only snapshot for patient edit forms.
    /// Navigation properties and child collections are intentionally excluded.
    /// </summary>
    public static Patient CreateSnapshot(this Patient source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new Patient
        {
            Id = source.Id,
            FirstName = source.FirstName,
            LastName = source.LastName,
            NationalId = source.NationalId,
            SzboNumber = source.SzboNumber,
            DoctorId = source.DoctorId,
            BirthDate = source.BirthDate,
            Gender = source.Gender,
            Phone = source.Phone,
            Email = source.Email,
            Address = source.Address,
            City = source.City,
            PostalCode = source.PostalCode,
            EmergencyContactName = source.EmergencyContactName,
            EmergencyContactPhone = source.EmergencyContactPhone,
            EmergencyRelationship = source.EmergencyRelationship,
            Allergies = source.Allergies,
            BloodType = source.BloodType,
            Status = source.Status,
            InactiveReason = source.InactiveReason,
            RegistrationDate = source.RegistrationDate,
            IsDeleted = source.IsDeleted,
            CreatedAt = source.CreatedAt
        };
    }
}
