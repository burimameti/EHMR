// File: EHMR.Backups/Services/EncryptionService.cs
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using EHMR.Backups.Encryption;

namespace EHMR.Backups.Services;

public sealed class EncryptionOptions
{
    /// <summary>Base64-encoded 32-byte (256-bit) AES key.</summary>
    public string Key { get; set; } = string.Empty;
    public const string SectionName = "Encryption";
}

public sealed class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IOptions<EncryptionOptions> options)
    {
        var secret = options.Value.Key;

        if(string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Encryption key is not configured.");

        _key=SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret));
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
    public async Task<byte[]> EncryptAsync(byte[] plainData, CancellationToken cancellationToken = default)
    {
        using var aes = Aes.Create();
        aes.Key=_key;
        aes.GenerateIV();

        using var ms = new MemoryStream();
        await ms.WriteAsync(aes.IV, cancellationToken); // prepend IV

        await using(var cryptoStream = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        {
            await cryptoStream.WriteAsync(plainData, cancellationToken);
        }

        return ms.ToArray();
    }

    public async Task<byte[]> DecryptAsync(byte[] cipherData, CancellationToken cancellationToken = default)
    {
        using var aes = Aes.Create();
        aes.Key=_key;

        var iv = cipherData[..16];
        var cipherText = cipherData[16..];

        aes.IV=iv;

        using var ms = new MemoryStream();
        await using(var cryptoStream = new CryptoStream(new MemoryStream(cipherText), aes.CreateDecryptor(), CryptoStreamMode.Read))
        {
            await cryptoStream.CopyToAsync(ms, cancellationToken);
        }

        return ms.ToArray();
    }
}