// File: EHMR.Backups/Providers/NetworkStorageProvider.cs
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;

namespace EHMR.Backups.Providers;

public sealed class NetworkStorageProvider : IBackupStorageProvider
{
    private readonly string _networkPath;

    public Guid DestinationId
    {
        get;
    }
    public string Key => "network";
    public string Name
    {
        get;
    }

    public NetworkStorageProvider(BackupDestinationOptions options)
    {
        if(string.IsNullOrWhiteSpace(options.Path))
            throw new InvalidOperationException("Network storage path is not configured.");

        DestinationId=options.Id;
        Name=options.Name;
        _networkPath=options.Path;

        Directory.CreateDirectory(_networkPath);
    }

    public async Task<string> SaveAsync(string file, CancellationToken cancellationToken = default)
    {
        var destination = Path.Combine(_networkPath, Path.GetFileName(file));

        await using var source = File.OpenRead(file);
        await using var target = File.Create(destination);
        await source.CopyToAsync(target, cancellationToken);

        return destination;
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
        => Task.FromResult<IEnumerable<string>>(Directory.GetFiles(_networkPath));
}