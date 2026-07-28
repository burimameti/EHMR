using EHMR.Domain.Entities;

namespace EHMR.Domain.Interfaces;

/// <summary>Резултат од обид за внес кога важат ограничувањата.</summary>
public sealed record LicenseCheck(bool Allowed, string? Message)
{
    public static LicenseCheck Ok() => new(true, null);
    public static LicenseCheck Denied(string message) => new(false, message);
}

public interface ILicenseService
{
    /// <summary>Го чита записот за лиценца; го создава ако го нема.</summary>
    Task<AppLicense> GetStateAsync(CancellationToken ct = default);

    /// <summary>Дали системот е заклучен — тогаш никој не се најавува.</summary>
    Task<bool> IsLockedAsync(CancellationToken ct = default);

    /// <summary>
    /// Дали смее да се внесе уште еден пациент. Без лиценца лимитот е
    /// <see cref="LicenseLimits.FreePatients"/>.
    /// </summary>
    Task<LicenseCheck> CanAddPatientAsync(CancellationToken ct = default);

    /// <summary>Дали демото воопшто треба да се понуди — само еднаш, и не по заклучување.</summary>
    Task<bool> ShouldOfferDemoAsync(CancellationToken ct = default);

    /// <summary>Го бележи одговорот на понудата за демо. Се повикува и при одбивање.</summary>
    Task RecordDemoDecisionAsync(bool accepted, CancellationToken ct = default);

    /// <summary>Внесување клуч. Го отклучува системот ако клучот е важечки.</summary>
    Task<LicenseCheck> ActivateAsync(string licenseKey, string licensedTo, CancellationToken ct = default);

    /// <summary>Го заклучува системот. Се повикува кога лимитот е исцрпен.</summary>
    Task LockAsync(CancellationToken ct = default);
}
