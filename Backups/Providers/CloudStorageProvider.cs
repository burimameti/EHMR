// File: EHMR.Backups/Providers/CloudStorageProvider.cs
using Azure.Storage.Blobs;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Backups.Providers;

public sealed class CloudStorageProvider : IBackupStorageProvider
{
    private readonly BlobContainerClient _containerClient;

    public Guid DestinationId
    {
        get;
    }
    public string Key => "cloud";
    public string Name
    {
        get;
    }

    public CloudStorageProvider(BackupDestinationOptions options, string connectionString)
    {
        if(string.IsNullOrWhiteSpace(options.Path))
            throw new InvalidOperationException("Cloud container name is not configured (use Path for container name).");

        DestinationId=options.Id;
        Name=options.Name;

        // CreateIfNotExists() е мрежен повик кон Azure. Во конструктор значи дека
        // градењето на DI контејнерот чека на мрежа — се одложува до прво запишување.
        _containerClient=new BlobContainerClient(connectionString, options.Path);
    }

    private bool _containerChecked;

    private void EnsureContainer()
    {
        if(_containerChecked)
            return;

        _containerClient.CreateIfNotExists();
        _containerChecked=true;
    }

    public async Task<string> SaveAsync(string file, CancellationToken cancellationToken = default)
    {
        EnsureContainer();

        var blobName = Path.GetFileName(file);
        var blobClient = _containerClient.GetBlobClient(blobName);

        await using var stream = File.OpenRead(file);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string file, CancellationToken cancellationToken = default)
    {
        var blobName = Path.GetFileName(file);
        await _containerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(string file, CancellationToken cancellationToken = default)
    {
        var blobName = Path.GetFileName(file);
        var blobClient = _containerClient.GetBlobClient(blobName);
        return await blobClient.ExistsAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        var names = new List<string>();

        await foreach(var blob in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            names.Add(blob.Name);
        }

        return names;
    }
}