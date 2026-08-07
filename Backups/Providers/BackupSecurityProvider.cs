// File: EHMR.Backups/Services/BackupSecurityProvider.cs
using EHMR.Backups.Encryption;
using EHMR.Backups.Interfaces;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Backups.Services;

public sealed class BackupSecurityProvider : IBackupSecurityProvider
{
    private readonly IEncryptionService _encryptionService;

    public BackupSecurityProvider(IEncryptionService encryptionService)
    {
        _encryptionService=encryptionService;
    }

    public async Task<string> EncryptAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var plainData = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var cipherData = await _encryptionService.EncryptAsync(plainData, cancellationToken);

        var encryptedPath = Path.ChangeExtension(filePath, ".enc");
        await File.WriteAllBytesAsync(encryptedPath, cipherData, cancellationToken);

        // Clean up the unencrypted intermediate file so plaintext doesn't linger on disk.
        File.Delete(filePath);

        return encryptedPath;
    }

    public async Task<string> DecryptAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var cipherData = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var plainData = await _encryptionService.DecryptAsync(cipherData, cancellationToken);

        var decryptedPath = Path.ChangeExtension(filePath, ".dec");
        await File.WriteAllBytesAsync(decryptedPath, plainData, cancellationToken);

        return decryptedPath;
    }
}