using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;

using System;

namespace EHMR.ViewModels.Support;

/// <summary>
/// UI-facing wrapper around a diagnosis chip shown in the appointment form's diagnosis
/// multi-select. Wraps either a freshly picked Mkb10Code or an already-saved PatientMkb10Assignment row.
/// Carries its own RelayCommands (assigned by the view model when the item is created) so the
/// XAML can bind Command="{Binding RemoveCommand}" etc. directly inside a DataTemplate without
/// needing RelativeSource bindings or value converters — matching the simple-binding style the
/// rest of this codebase already uses.
/// </summary>
public partial class SelectableDiagnosis : ObservableObject
{
    public Guid Id
    {
        get; set;
    }

    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isPrimary;

    public string DisplayText => $"{Code} — {Description}";

    /// <summary>Removes this chip from the appointment's selected-diagnoses list.</summary>
    public IRelayCommand? RemoveCommand
    {
        get; set;
    }

    /// <summary>Marks this chip as the primary diagnosis (and un-marks any other).</summary>
    public IRelayCommand? SetPrimaryCommand
    {
        get; set;
    }

    /// <summary>Used only on quick-add suggestion chips (PatientRecentDiagnoses), not on
    /// chips already inside SelectedDiagnoses — adds this code to the selection.</summary>
    public IRelayCommand? QuickAddCommand
    {
        get; set;
    }

    /// <summary>Builds a fresh chip from a catalog entry the user picked from search/quick-add. Generates a new Id since this isn't backed by a saved PatientMkb10Assignment row yet.</summary>
    public static SelectableDiagnosis FromCatalog(Mkb10Code source, bool isPrimary) => new()
    {
        Id=Guid.NewGuid(),
        Code=source.Code,
        Description=source.Description,
        IsPrimary=isPrimary
    };

    /// <summary>Builds a chip from a PatientMkb10Assignment row already saved against this appointment (edit mode), reusing its real Id so saving later updates rather than re-inserts it.</summary>
    public static SelectableDiagnosis FromExisting(PatientMkb10Assignment source) => new()
    {
        Id=source.Id,
        Code=source.Mkb10Code.Code,
        Description=source.ClinicalDescription,
        IsPrimary=source.IsPrimary
    };
}
