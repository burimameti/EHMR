using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace EHMR.Backups.Services
{



    public sealed class BackupHistoryRepository : IBackupHistoryRepository
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

        public BackupHistoryRepository(IDbContextFactory<DesktopTherapyDbContext> factory )
        {
            _factory=factory;
        }
        public async Task<Guid> RecordStartAsync(
     BackupSettings settings,
     CancellationToken cancellationToken = default)
        {
            await using var context = await _factory.CreateDbContextAsync(cancellationToken);

            var history = new BackupHistory
            {
                Id=Guid.NewGuid(),
                DatabaseName=settings.DatabaseName,
                Type=settings.Type,
                Status=BackupRunStatus.Running,
                DestinationId=settings.DestinationId,
                DestinationName=settings.DestinationPath,
                IsCompressed=settings.Compress,
                IsEncrypted=settings.Encrypt,
                StartedAt=DateTime.UtcNow
            };

            context.BackupHistories.Add(history);
            await context.SaveChangesAsync(cancellationToken);

            return history.Id;
        }

        public async Task RecordCompletionAsync(
            Guid historyId,
            OperationResult<BackupResult> result,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _factory.CreateDbContextAsync(cancellationToken);

            var history = await context.BackupHistories
                .FirstOrDefaultAsync(x => x.Id==historyId, cancellationToken);

            if(history==null)
                return;

            history.CompletedAt=DateTime.UtcNow;

            if(result.Success&&result.Data!=null)
            {
                history.Status=result.Data.Verified
                    ? BackupRunStatus.Succeeded
                    : BackupRunStatus.VerificationFailed;

                history.FilePath=result.Data.FilePath;
                history.Size=result.Data.Size;
                history.Verified=result.Data.Verified;
                history.Duration=result.Data.Duration;
            }
            else
            {
                history.Status=BackupRunStatus.Failed;
                history.ErrorMessage=result.Message;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<BackupHistory>> GetRecentAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _factory.CreateDbContextAsync(cancellationToken);

            return await context.BackupHistories
                .OrderByDescending(x => x.StartedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<BackupHistory?> GetLastAsync(
            CancellationToken cancellationToken = default)
        {
            await using var context = await _factory.CreateDbContextAsync(cancellationToken);

            return await context.BackupHistories
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<BackupHistory?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _factory.CreateDbContextAsync(cancellationToken);

            return await context.BackupHistories
                .FirstOrDefaultAsync(x => x.Id==id, cancellationToken);
        }

        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _factory.CreateDbContextAsync(cancellationToken);

            var entity = await context.BackupHistories.FindAsync([id], cancellationToken);

            if(entity==null)
                return;

            context.BackupHistories.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        public async Task DeleteOlderThanAsync(
            DateTime cutoffUtc,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _factory.CreateDbContextAsync(cancellationToken);

            var records = await context.BackupHistories
                .Where(x => x.StartedAt<cutoffUtc)
                .ToListAsync(cancellationToken);

            context.BackupHistories.RemoveRange(records);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
