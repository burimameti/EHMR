// File: EHMR.Backups/Engine/BackupEngine.cs
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


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

        // Секој фајл создаден локално по пат, за да не остане ништо во staging.
        var staged = new List<string>();

        try
        {
            progress?.Report(new BackupProgress { Percentage=5, Message="Се подготвува резервна копија..." });

            var databaseProvider = _databaseResolver.Resolve();
            var storageProvider = _storageResolver.Resolve(settings.DestinationId);

            var tempDir = _configuration["Backup:StagingDirectory"];
            if(string.IsNullOrWhiteSpace(tempDir))
                tempDir=Path.Combine(AppContext.BaseDirectory, "BackupStaging"); // safe fallback
            Directory.CreateDirectory(tempDir);

            progress?.Report(new BackupProgress { Percentage=20, Message="Се создава копија на базата..." });

            var rawFile = await databaseProvider.CreateBackupAsync(tempDir, cancellationToken);
            staged.Add(rawFile);

            // ── 1. ПРОВЕРКА ─────────────────────────────────────────────────
            // Мора да е тука, врз сировиот .bak. Порано се правеше на крајот, врз
            // веќе шифрираниот фајл — SQL Server не може да го прочита, па секој
            // backup со Encrypt=true пријавуваше неуспех иако бил исправен.
            if(settings.VerifyAfterBackup)
            {
                progress?.Report(new BackupProgress { Percentage=35, Message="Се проверува копијата..." });

                if(!await _verifier.VerifyAsync(rawFile, cancellationToken))
                    return OperationResult<BackupResult>.Fail(
                        "Проверката на резервната копија не успеа — копијата не е читлива.");
            }

            var workingFile = rawFile;

            // ── 2. КОМПРЕСИЈА ───────────────────────────────────────────────
            // Условот беше обратен (`if(!settings.Compress)`) и целиот блок закоментиран.
            if(settings.Compress)
            {
                progress?.Report(new BackupProgress { Percentage=50, Message="Се компресира копијата..." });
                workingFile=await _compressionService.CompressAsync(workingFile, cancellationToken);
                staged.Add(workingFile);
            }

            // ── 3. ШИФРИРАЊЕ ────────────────────────────────────────────────
            if(settings.Encrypt)
            {
                progress?.Report(new BackupProgress { Percentage=65, Message="Се шифрира копијата..." });
                workingFile=await _securityProvider.EncryptAsync(workingFile, cancellationToken);
                staged.Add(workingFile);
            }

            // Големината се чита сега, додека фајлот е локален. Кај cloud дестинација
            // SaveAsync враќа URL, па FileInfo врз резултатот дава нула.
            var uploadInfo = new FileInfo(workingFile);
            var size = uploadInfo.Exists ? uploadInfo.Length : 0;

            var checksum = await _verifier.GenerateChecksumAsync(workingFile, cancellationToken);

            // ── 4. ЗАЧУВУВАЊЕ ───────────────────────────────────────────────
            progress?.Report(new BackupProgress { Percentage=80, Message="Се зачувува копијата..." });
            var finalFile = await storageProvider.SaveAsync(workingFile, cancellationToken);

            // ── 5. ПРОВЕРКА НА ЗАЧУВАНОТО ───────────────────────────────────
            // Само кај дестинации што враќаат локална патека. Cloud враќа URL —
            // таму се потпираме на проверката од чекор 1.
            var verified = true;

            if(settings.VerifyAfterBackup&&File.Exists(finalFile))
            {
                progress?.Report(new BackupProgress { Percentage=92, Message="Се проверува зачуваната копија..." });

                verified=await _verifier.VerifyChecksumAsync(finalFile, checksum, cancellationToken);

                if(!verified)
                    return OperationResult<BackupResult>.Fail(
                        "Контролната сума не се совпаѓа — копијата е оштетена при зачувување.");
            }

            var result = new BackupResult
            {
                FilePath=finalFile,
                DatabaseName=settings.DatabaseName,
                Type=settings.Type,
                IsEncrypted=settings.Encrypt,
                IsCompressed=settings.Compress,
                Size=size,
                CreatedAt=DateTime.UtcNow,
                Duration=DateTime.UtcNow-started,
                Destination=settings.DestinationPath,
                Verified=verified
            };

            progress?.Report(new BackupProgress { Percentage=100, Message="Резервната копија е завршена." });

            return OperationResult<BackupResult>.Ok(result, "Резервната копија е успешно направена.");
        }
        catch(Exception ex)
        {
            return OperationResult<BackupResult>.Fail(ex.Message);
        }
        finally
        {
            // Порано се бришеше само сировиот .bak — а шифрирањето веќе го имаше
            // избришано него и оставаше .enc фајл во staging засекогаш.
            foreach(var file in staged)
            {
                if(File.Exists(file))
                {
                    try { File.Delete(file); } catch { /* best effort */ }
                }
            }
        }
    }
}