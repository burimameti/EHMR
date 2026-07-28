using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Backups.Services;

/// <summary>
/// CRUD над дестинациите за резервни копии.
///
/// <see cref="IBackupDestinationRepository"/> постоеше како интерфејс без ниту една
/// имплементација — иако табелата <c>BackupDestinations</c> е во базата. Дестинациите
/// се читаа само од <c>appsettings.json</c>, што значеше дека нова дестинација бара
/// рачно менување на фајл и рестарт.
/// </summary>
public sealed class BackupDestinationRepository : IBackupDestinationRepository
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

    public BackupDestinationRepository(IDbContextFactory<DesktopTherapyDbContext> factory)
    {
        _factory=factory;
    }

    public async Task<IReadOnlyList<BackupDestination>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        return await context.BackupDestinations
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<BackupDestination?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        return await context.BackupDestinations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id==id, cancellationToken);
    }

    public async Task<Guid> AddAsync(
        BackupDestination destination,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        if(destination.Id==Guid.Empty)
            destination.Id=Guid.NewGuid();

        context.BackupDestinations.Add(destination);
        await context.SaveChangesAsync(cancellationToken);

        return destination.Id;
    }

    public async Task UpdateAsync(
        BackupDestination destination,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var existing = await context.BackupDestinations
            .FirstOrDefaultAsync(x => x.Id==destination.Id, cancellationToken);

        if(existing is null)
            return;

        existing.Key=destination.Key;
        existing.Name=destination.Name;
        existing.Path=destination.Path;
        existing.IsActive=destination.IsActive;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);

        var entity = await context.BackupDestinations
            .FirstOrDefaultAsync(x => x.Id==id, cancellationToken);

        if(entity is null)
            return;

        context.BackupDestinations.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
