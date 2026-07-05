using EHMR.Domain.Entities;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Globalization;

namespace EHMR.Converters;

internal static class AppointmentStatusColors
{
    public static Color GetBackground(AppointmentStatus? status) => status switch
    {
        AppointmentStatus.Scheduled => Color.FromArgb("#FEF3C7"), // Amber
        AppointmentStatus.CheckedIn => Color.FromArgb("#FFEDD5"), // Orange

        AppointmentStatus.Completed => Color.FromArgb("#DCFCE7"), // Green

        AppointmentStatus.Cancelled => Color.FromArgb("#FEE2E2"), // Red

        AppointmentStatus.Missed => Color.FromArgb("#FEE2E2"),    // Red

        null => Color.FromArgb("#FEE2E2"),

        _ => Color.FromArgb("#FEE2E2")
    };

    public static Color GetAccent(AppointmentStatus? status) => status switch
    {
        AppointmentStatus.Scheduled => Color.FromArgb("#D97706"),
        AppointmentStatus.CheckedIn => Color.FromArgb("#EA580C"),

        AppointmentStatus.Completed => Color.FromArgb("#16A34A"),

        AppointmentStatus.Cancelled => Color.FromArgb("#DC2626"),

        AppointmentStatus.Missed => Color.FromArgb("#B91C1C"),

        null => Color.FromArgb("#DC2626"),

        _ => Color.FromArgb("#DC2626")
    };
}

/// <summary>
/// Soft pastel pill background per appointment status, used behind the
/// status label in the grid.
/// </summary>
public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return AppointmentStatusColors.GetBackground(
      value is AppointmentStatus status ? status : null);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Saturated accent color per appointment status, used for the status dot
/// and the status text so they stay legible against the pastel background.
/// </summary>
public class StatusToAccentConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return AppointmentStatusColors.GetAccent(
            value is AppointmentStatus status ? status : null);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Combines a patient's first/last name into a two-letter initials string
/// for the avatar circle (e.g. "Ana Petrovska" -> "AP").
/// </summary>
public class InitialsConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        string first = values.Length>0 ? values[0] as string??"" : "";
        string last = values.Length>1 ? values[1] as string??"" : "";

        string initials = string.Concat(
            first.Length>0 ? first[0].ToString() : "",
            last.Length>0 ? last[0].ToString() : "");

        return initials.Length>0 ? initials.ToUpperInvariant() : "?";
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Deterministically maps a name to one of a small palette of soft avatar
/// background colors, so the grid doesn't render every row with an
/// identical flat color block.
/// </summary>
public class NameToAvatarColorConverter : IValueConverter
{
    private static readonly Color[] Palette =
    {
        Color.FromArgb("#DBEAFE"),
        Color.FromArgb("#DCFCE7"),
        Color.FromArgb("#FEF3C7"),
        Color.FromArgb("#FCE7F3"),
        Color.FromArgb("#E0E7FF"),
        Color.FromArgb("#FFE4E6"),
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string text = value as string??"?";
        int hash = 0;
        foreach(char c in text) hash+=c;
        return Palette[Math.Abs(hash)%Palette.Length];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// True when the bound string has real content. Used to hide the
/// "reason for visit" subtitle line under the patient name when there is
/// nothing to show, instead of leaving a blank gap.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Only "Scheduled" appointments can still be cancelled - once something is
/// Completed, Cancelled, or NoShow, hide the Cancel action instead of
/// letting the user click it again on a row it no longer applies to.
/// </summary>
public class StatusIsCancellableConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return value is AppointmentStatus status
               &&status==AppointmentStatus.Scheduled;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}