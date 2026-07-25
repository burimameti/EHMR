// File: EHMR.Infrastructure/Security/ColumnEncryptor.cs
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using EHMR.Backups.Services; // reuse EncryptionOptions (Key) from before

namespace EHMR.Infrastructure.Security;
public interface IColumnEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
public sealed class ColumnEncryptor : IColumnEncryptor
{
    private readonly byte[] _key;

    public ColumnEncryptor(IOptions<EncryptionOptions> options)
    {
        var keyBase64 = options.Value.Key;

        if(string.IsNullOrWhiteSpace(keyBase64))
            throw new InvalidOperationException("Encryption key is not configured.");

        _key=Convert.FromBase64String(keyBase64);

        if(_key.Length!=32)
            throw new InvalidOperationException("Encryption key must be 256 bits (32 bytes).");
    }

    public string Encrypt(string plainText)
    {
        if(string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key=_key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV so it travels with the ciphertext — same pattern as EncryptionService.
        var result = new byte[aes.IV.Length+cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        if(string.IsNullOrEmpty(cipherText))
            return cipherText;

        var fullBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key=_key;
        aes.IV=fullBytes[..16];

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = fullBytes[16..];
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}