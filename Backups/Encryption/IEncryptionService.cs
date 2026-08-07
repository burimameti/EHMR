using System.Threading;
using System.Threading.Tasks;

namespace EHMR.Backups.Encryption
{
    // File: EHMR.Backups/Services/IEncryptionService.cs


    /// <summary>
    /// Low-level byte-oriented encryption primitive. BackupSecurityProvider wraps this
    /// to work with file paths instead of raw byte arrays.
    /// </summary>
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        Task<byte[]> EncryptAsync(byte[] plainData, CancellationToken cancellationToken = default);
        Task<byte[]> DecryptAsync(byte[] cipherData, CancellationToken cancellationToken = default);
    }
}