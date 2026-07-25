// File: EHMR.Backups/Engine/RestoreEngine.cs
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;

namespace EHMR.Backups.Engine;

public sealed class RestoreEngine : IRestoreEngine
{
    private readonly IDatabaseProviderResolver _databaseResolver;
    private readonly IStorageProviderResolver _storageResolver;
    private readonly IBackupSecurityProvider _securityProvider;
    private readonly ICompressionService _compressionService;

    public RestoreEngine(
        IDatabaseProviderResolver databaseResolver,
        IStorageProviderResolver storageResolver,
        IBackupSecurityProvider securityProvider,
        ICompressionService compressionService)
    {
        _databaseResolver=databaseResolver;
        _storageResolver=storageResolver;
        _securityProvider=securityProvider;
        _compressionService=compressionService;
    }

    public async Task<OperationResult<bool>> ExecuteAsync(
        RestoreSettings settings,
        IProgress<RestoreProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var workingFile = settings.BackupFile;
        var producedFiles = new List<string>();

        try
        {
            if(!File.Exists(workingFile))
                return OperationResult<bool>.Fail($"Backup file not found: {workingFile}");

            progress?.Report(new RestoreProgress { Percentage=10, Message="Preparing restore..." });

            if(settings.IsEncrypted)
            {
                progress?.Report(new RestoreProgress { Percentage=30, Message="Decrypting backup..." });
                workingFile=await _securityProvider.DecryptAsync(workingFile, cancellationToken);
                producedFiles.Add(workingFile);
            }

            if(workingFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                ||workingFile.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report(new RestoreProgress { Percentage=50, Message="Decompressing backup..." });
                workingFile=await _compressionService.DecompressAsync(workingFile, cancellationToken);
                producedFiles.Add(workingFile);
            }

            progress?.Report(new RestoreProgress { Percentage=70, Message="Restoring database..." });

            var databaseProvider = _databaseResolver.Resolve();
            await databaseProvider.RestoreAsync(workingFile, cancellationToken);

            progress?.Report(new RestoreProgress { Percentage=100, Message="Restore completed." });

            return OperationResult<bool>.Ok(true, "Restore completed successfully.");
        }
        catch(Exception ex)
        {
            return OperationResult<bool>.Fail(ex.Message);
        }
        finally
        {
            // Clean up any decrypted/decompressed intermediate files — don't leave
            // plaintext database dumps lying around in temp after a restore.
            foreach(var file in producedFiles)
            {
                if(File.Exists(file))
                {
                    try { File.Delete(file); } catch { /* best effort */ }
                }
            }
        }
    }
}