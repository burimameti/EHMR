// File: EHMR.Backups/Providers/SqlServerBackupProvider.cs
using EHMR.Backups.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace EHMR.Backups.Providers;

public sealed class SqlServerBackupProvider : IDatabaseBackupProvider
{
    private readonly string _connectionString;

    public SqlServerBackupProvider(IConfiguration configuration)
    {
        _connectionString=configuration.GetConnectionString("Default")
            ??throw new InvalidOperationException("SQL connection string missing.");
    }

    public bool CanHandle(string connectionString)
    {
        return connectionString.Contains("Initial Catalog", StringComparison.OrdinalIgnoreCase)
            ||connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> CreateBackupAsync(string destination, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destination);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var databaseName = connection.Database;
        var backupFile = Path.Combine(destination, $"{databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

        const string sql = """
            BACKUP DATABASE @dbName
            TO DISK = @file
            WITH
                INIT,
                FORMAT,

                CHECKSUM,
                STATS = 10;
            """;
        // Note: BACKUP DATABASE doesn't allow parameterizing the DB name directly,
        // so it's built safely via quoted identifier instead of raw interpolation.
        var safeSql = $"""
            BACKUP DATABASE [{databaseName.Replace("]", "]]")}]
            TO DISK = @file
            WITH
                INIT,
                FORMAT,
           
                CHECKSUM,
                STATS = 10;
            """;

        await using var command = new SqlCommand(safeSql, connection)
        {
            CommandTimeout=0
        };
        command.Parameters.AddWithValue("@file", backupFile);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return backupFile;
    }

    public async Task RestoreAsync(string backupFile, CancellationToken cancellationToken = default)
    {
        if(!File.Exists(backupFile))
            throw new FileNotFoundException(backupFile);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var databaseName = connection.Database;
        var safeName = databaseName.Replace("]", "]]");

        var sql = $"""
            USE master;

            ALTER DATABASE [{safeName}]
            SET SINGLE_USER
            WITH ROLLBACK IMMEDIATE;

            RESTORE DATABASE [{safeName}]
            FROM DISK = @file
            WITH
                REPLACE,
                RECOVERY,
                CHECKSUM,
                STATS = 10;

            ALTER DATABASE [{safeName}]
            SET MULTI_USER;
            """;

        await using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout=0
        };
        command.Parameters.AddWithValue("@file", backupFile);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> VerifyBackupAsync(string backupFile, CancellationToken cancellationToken = default)
    {
        if(!File.Exists(backupFile))
            return false;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            RESTORE VERIFYONLY
            FROM DISK = @file
            WITH CHECKSUM;
            """;

        await using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout=0
        };
        command.Parameters.AddWithValue("@file", backupFile);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch(SqlException)
        {
            return false;
        }
    }

    public async Task<long> GetDatabaseSizeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT SUM(size) * 8 * 1024
            FROM sys.database_files;
            """;

        await using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result==null||result==DBNull.Value ? 0 : Convert.ToInt64(result);
    }
}