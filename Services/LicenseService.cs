using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Services;

/// <summary>
/// Ограничувања и активација.
///
/// Правила:
/// — без активирана лиценца: најмногу <see cref="LicenseLimits.FreePatients"/> пациенти;
/// — кога тој лимит ќе се достигне, системот се заклучува и најавата се затвора;
/// — демо податоците се нудат точно еднаш и никогаш повторно;
/// — заклучен систем се отклучува само со внесување клуч.
/// </summary>
public sealed class LicenseService : ILicenseService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

    public LicenseService(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
    {
        _dbFactory=dbFactory;
    }

    public async Task<AppLicense> GetStateAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await GetOrCreateAsync(db, ct);
    }

    public async Task<bool> IsLockedAsync(CancellationToken ct = default)
    {
        var state = await GetStateAsync(ct);
        return state.IsLocked&&!state.IsActivated;
    }

    public async Task<LicenseCheck> CanAddPatientAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var state = await GetOrCreateAsync(db, ct);

        if(state.IsActivated)
            return LicenseCheck.Ok();

        var limit = state.MaxAllowedPatients>0
            ? state.MaxAllowedPatients
            : LicenseLimits.FreePatients;

        var count = await db.Patients.CountAsync(ct);

        if(count<limit)
            return LicenseCheck.Ok();

        // Лимитот е достигнат — системот се заклучува и најавата се затвора.
        state.IsLocked=true;
        await db.SaveChangesAsync(ct);

        return LicenseCheck.Denied(
            $"Достигнат е лимитот од {limit} пациенти. Лиценцата треба да се активира.");
    }

    public async Task<bool> ShouldOfferDemoAsync(CancellationToken ct = default)
    {
        var state = await GetStateAsync(ct);
        return !state.DemoOffered&&!state.IsLocked;
    }

    public async Task RecordDemoDecisionAsync(bool accepted, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var state = await GetOrCreateAsync(db, ct);

        // Се бележи и одбивањето — понудата важи еднаш, без разлика на одговорот.
        state.DemoOffered=true;
        state.DemoDataSeeded=accepted;

        await db.SaveChangesAsync(ct);
    }

    public async Task<LicenseCheck> ActivateAsync(
        string licenseKey,
        string licensedTo,
        CancellationToken ct = default)
    {
        if(string.IsNullOrWhiteSpace(licenseKey))
            return LicenseCheck.Denied("Внесете лиценцен клуч.");

        if(!IsKeyValid(licenseKey))
            return LicenseCheck.Denied("Лиценцниот клуч не е важечки.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var state = await GetOrCreateAsync(db, ct);

        state.LicenseKey=licenseKey.Trim();
        state.LicensedTo=licensedTo?.Trim()??string.Empty;
        state.IsActivated=true;
        state.IsLocked=false;
        state.ValidUntil=DateTime.UtcNow.AddYears(1);
        state.MaxAllowedPatients=0; // 0 = без ограничување

        await db.SaveChangesAsync(ct);

        return LicenseCheck.Ok();
    }

    public async Task LockAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var state = await GetOrCreateAsync(db, ct);
        state.IsLocked=true;

        await db.SaveChangesAsync(ct);
    }

    // =====================================================

    private static async Task<AppLicense> GetOrCreateAsync(
        DesktopTherapyDbContext db,
        CancellationToken ct)
    {
        var state = await db.AppLicenses.FirstOrDefaultAsync(ct);

        if(state is not null)
            return state;

        state=new AppLicense
        {
            Id=Guid.NewGuid(),
            MaxAllowedPatients=LicenseLimits.FreePatients
        };

        db.AppLicenses.Add(state);
        await db.SaveChangesAsync(ct);

        return state;
    }

    /// <summary>
    /// Проверка на формат: EHMR-XXXX-XXXX-XXXX со контролна сума.
    ///
    /// Ова е локална проверка, не криптографска — секој што го чита кодот може
    /// да генерира важечки клуч. Доволно е да спречи случајно внесување, но не
    /// е заштита од намерно заобиколување.
    /// </summary>
    private static bool IsKeyValid(string key)
    {
        var parts = key.Trim().ToUpperInvariant().Split('-');

        if(parts.Length!=4||parts[0]!="EHMR")
            return false;

        if(parts.Skip(1).Any(p => p.Length!=4||!p.All(char.IsLetterOrDigit)))
            return false;

        // Последниот блок е контролна сума на претходните два.
        var sum = parts[1].Concat(parts[2]).Sum(c => c);
        var expected = (sum%10000).ToString("D4");

        return parts[3]==expected;
    }
}
