// File: EHMR.Backups/Providers/StorageProviderResolver.cs
using EHMR.Backups.Interfaces;

namespace EHMR.Backups.Providers;

public sealed class StorageProviderResolver : IStorageProviderResolver
{
    private readonly IEnumerable<IBackupStorageProvider> _providers;

    public StorageProviderResolver(IEnumerable<IBackupStorageProvider> providers)
    {
        _providers=providers;
    }

    public IBackupStorageProvider Resolve(Guid destinationId)
    {
        return _providers.FirstOrDefault(x => x.DestinationId==destinationId)
            ??throw new InvalidOperationException($"Storage provider not found for destination: {destinationId}");
    }

    public IBackupStorageProvider Resolve(string key)
    {
        return _providers.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            ??throw new InvalidOperationException($"Storage provider not found for key: {key}");
    }
}