using System;

namespace EHMR.Domain.Entities;

/// <summary>
/// Состојбата на лиценцата. Во базата постои најмногу еден ваков запис.
///
/// Без активирана лиценца системот работи ограничено — до
/// <see cref="LicenseLimits.FreePatients"/> пациенти. Кога тоа ќе се исцрпи,
/// <see cref="IsLocked"/> се поставува и најавата се затвора додека не се
/// внесе клуч.
/// </summary>
public class AppLicense : BaseEntity
{
    public string LicenseKey { get; set; } = string.Empty;

    public string LicensedTo { get; set; } = string.Empty;

    public DateTime ValidUntil
    {
        get; set;
    }

    public int MaxAllowedPatients
    {
        get; set;
    }

    /// <summary>Дали е внесен важечки клуч. Без ова важат ограничувањата.</summary>
    public bool IsActivated
    {
        get; set;
    }

    /// <summary>
    /// Демо податоците се нудат само еднаш. Штом корисникот одлучи —
    /// прифати или одбие — ова останува трајно поставено.
    /// </summary>
    public bool DemoOffered
    {
        get; set;
    }

    /// <summary>Дали демо податоците навистина се внесени.</summary>
    public bool DemoDataSeeded
    {
        get; set;
    }

    /// <summary>
    /// Заклучен систем. Кога ова е поставено никој не може да се најави
    /// и не се дозволува повторно демо — само внесување лиценца.
    /// </summary>
    public bool IsLocked
    {
        get; set;
    }
}

/// <summary>Ограничувања што важат додека нема активирана лиценца.</summary>
public static class LicenseLimits
{
    /// <summary>Најмногу пациенти без лиценца.</summary>
    public const int FreePatients = 10;
}
