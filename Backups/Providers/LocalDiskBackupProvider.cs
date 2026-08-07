// File: EHMR.Backups/Providers/LocalStorageProvider.cs
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EHMR.Backups.Providers;

public sealed class LocalStorageProvider : IBackupStorageProvider
{
    private readonly string _localPath;

    public Guid DestinationId
    {
        get;
    }
    public string Key => "local";
    public string Name
    {
        get;
    }

    public LocalStorageProvider(BackupDestinationOptions options)
    {
        if(string.IsNullOrWhiteSpace(options.Path))
            throw new InvalidOperationException("Local storage path is not configured.");

        DestinationId=options.Id;
        Name=options.Name;
        _localPath=options.Path;

        // Не создавај папка во конструктор — тој се извршува при градење на DI
        // контејнерот. Се создава при првото запишување.
    }

    private void EnsureDirectory()
        => Directory.CreateDirectory(_localPath);

    public Task<string> SaveAsync(string file, CancellationToken cancellationToken = default)
    {
        EnsureDirectory();

        // If SqlServerBackupProvider already wrote the .bak into this same folder,
        // there's nothing to move. Only copy if it landed somewhere else.
        var destination = Path.Combine(_localPath, Path.GetFileName(file));
        if(!string.Equals(Path.GetFullPath(file), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(file, destination, overwrite: true);
        }
        return Task.FromResult(destination);
    }

    public Task DeleteAsync(string file, CancellationToken cancellationToken = default)
    {
        if(File.Exists(file))
            File.Delete(file);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string file, CancellationToken cancellationToken = default)
        => Task.FromResult(File.Exists(file));

    public Task<IEnumerable<string>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<string>>(Directory.GetFiles(_localPath));
}