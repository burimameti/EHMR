using EHMR.Backups.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace EHMR.Backups.Services;

public sealed class BackupVerifier : IBackupVerifier
{
    private readonly string _connectionString;


    public BackupVerifier(
        IConfiguration configuration)
    {
        _connectionString=
            configuration.GetConnectionString("Default")
            ??throw new InvalidOperationException(
                "Database connection string missing.");
    }


    public async Task<bool> VerifyAsync(
        string backupFile,
        CancellationToken cancellationToken = default)
    {
        if(!File.Exists(backupFile))
            return false;


        if(new FileInfo(backupFile).Length==0)
            return false;


        try
        {
            await using var connection =
                new SqlConnection(_connectionString);


            await connection.OpenAsync(cancellationToken);


            const string sql = """
            RESTORE VERIFYONLY
            FROM DISK = @file
            WITH CHECKSUM;
            """;


            await using var command =
                new SqlCommand(sql, connection);


            command.Parameters.AddWithValue(
                "@file",
                backupFile);


            command.CommandTimeout=0;


            await command.ExecuteNonQueryAsync(
                cancellationToken);


            return true;
        }
        catch(SqlException)
        {
            // Неисправна или нечитлива копија — тоа е неуспешна проверка,
            // не пад на целата операција.
            return false;
        }
    }



    public async Task<string> GenerateChecksumAsync(
        string file,
        CancellationToken cancellationToken = default)
    {
        await using var stream =
            File.OpenRead(file);


        using var sha =
            SHA256.Create();


        var hash =
            await sha.ComputeHashAsync(
                stream,
                cancellationToken);


        return Convert.ToHexString(hash);
    }



    public async Task<bool> VerifyChecksumAsync(
        string file,
        string checksum,
        CancellationToken cancellationToken = default)
    {
        var current =
            await GenerateChecksumAsync(
                file,
                cancellationToken);


        return string.Equals(
            current,
            checksum,
            StringComparison.OrdinalIgnoreCase);
    }
}