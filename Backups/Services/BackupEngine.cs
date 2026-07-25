// File: EHMR.Backups/Engine/BackupEngine.cs
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using Microsoft.Extensions.Configuration;

namespace EHMR.Backups.Engine;

public sealed class BackupEngine : IBackupEngine
{
    private readonly IDatabaseProviderResolver _databaseResolver;
    private readonly IStorageProviderResolver _storageResolver;
    private readonly IBackupSecurityProvider _securityProvider;
    private readonly ICompressionService _compressionService;
    private readonly IBackupVerifier _verifier;
    private readonly IConfiguration _configuration;

    public BackupEngine(
        IDatabaseProviderResolver databaseResolver,
        IStorageProviderResolver storageResolver,
        IBackupSecurityProvider securityProvider,
        ICompressionService compressionService,
        IBackupVerifier verifier, IConfiguration configuration)
    {
        _databaseResolver=databaseResolver;
        _configuration=configuration;
        _storageResolver=storageResolver;
        _securityProvider=securityProvider;
        _compressionService=compressionService;
        _verifier=verifier;
    }

    public async Task<OperationResult<BackupResult>> ExecuteAsync(
        BackupSettings settings,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        string? tempFile = null;

        try
        {
            progress?.Report(new BackupProgress { Percentage=5, Message="Initializing backup..." });

            var databaseProvider = _databaseResolver.Resolve();
            var storageProvider = _storageResolver.Resolve(settings.DestinationId);

            var tempDir = _configuration["Backup:StagingDirectory"];
            if(string.IsNullOrWhiteSpace(tempDir))
                tempDir=Path.Combine(AppContext.BaseDirectory, "BackupStaging"); // safe fallback
            Directory.CreateDirectory(tempDir);

            progress?.Report(new BackupProgress { Percentage=20, Message="Creating database backup..." });

            tempFile=await databaseProvider.CreateBackupAsync(tempDir, cancellationToken);
            var workingFile = tempFile;

            //if(!settings.Compress)
            //{
            //    progress?.Report(new BackupProgress { Percentage=40, Message="Compressing backup..." });
            //    workingFile=await _compressionService.CompressAsync(workingFile, cancellationToken);
            //}

            if(settings.Encrypt)
            {
                progress?.Report(new BackupProgress { Percentage=60, Message="Encrypting backup..." });
                workingFile=await _securityProvider.EncryptAsync(workingFile, cancellationToken);
            }

            progress?.Report(new BackupProgress { Percentage=80, Message="Saving backup..." });
            var finalFile = await storageProvider.SaveAsync(workingFile, cancellationToken);

            var verified = true;

            if(settings.VerifyAfterBackup)
            {
                progress?.Report(new BackupProgress { Percentage=90, Message="Verifying backup..." });
                verified=await _verifier.VerifyAsync(finalFile, cancellationToken);
            }

            if(!verified)
                return OperationResult<BackupResult>.Fail("Backup verification failed.");

            var info = new FileInfo(finalFile);

            var result = new BackupResult
            {
                FilePath=finalFile,
                DatabaseName=settings.DatabaseName,
                Type=settings.Type,
                IsEncrypted=settings.Encrypt,
                IsCompressed=settings.Compress,
                Size=info.Exists ? info.Length : 0,
                CreatedAt=DateTime.UtcNow,
                Duration=DateTime.UtcNow-started,
                Destination=settings.DestinationPath,
                Verified=verified
            };

            progress?.Report(new BackupProgress { Percentage=100, Message="Backup completed." });

            return OperationResult<BackupResult>.Ok(result, "Backup completed successfully.");
        }
        catch(Exception ex)
        {
            return OperationResult<BackupResult>.Fail(ex.Message);
        }
        finally
        {
            // Clean up any leftover local temp artifacts (uncompressed/unencrypted intermediate files).
            if(tempFile is not null&&File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { /* best effort */ }
            }
        }
    }
}