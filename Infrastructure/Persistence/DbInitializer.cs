using EHMR.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class DatabaseMigrationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationService>? _logger;
    private readonly IConfiguration _configuration;

    public DatabaseMigrationService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<DatabaseMigrationService>? logger = null)
    {
        _serviceProvider=serviceProvider;
        _configuration=configuration;
        _logger=logger;
    }

    public async Task MigrateAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DesktopTherapyDbContext>();

        if(!IsMigrationAllowed())
            return;

        var pending = await db.Database.GetPendingMigrationsAsync();

        if(pending.Any())
        {
            BackupDatabase(db);
            await db.Database.MigrateAsync();
        }

        var runner = scope.ServiceProvider.GetRequiredService<SeederRunner>();
        await runner.RunAsync();
    }

    private bool IsMigrationAllowed()
    {
        // var env = _configuration["APP_ENV"]
        //            ??Environment.GetEnvironmentVariable("APP_ENV");

        // return env is "Development" or "Test";
        return true;
    }

    private void BackupDatabase(DesktopTherapyDbContext db)
    {
        try
        {
            var conn = db.Database.GetConnectionString();
            if(string.IsNullOrWhiteSpace(conn)) return;

            var builder = new SqlConnectionStringBuilder(conn);
            var dbName = builder.InitialCatalog;

            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EHMR_Backups");

            Directory.CreateDirectory(backupDir);

            var file = Path.Combine(
                backupDir,
                $"{dbName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak");

            using var sql = new SqlConnection(conn);
            sql.Open();

            using var cmd = sql.CreateCommand();
            cmd.CommandText=
                $"BACKUP DATABASE [{dbName}] TO DISK = '{file}' WITH INIT";

            cmd.ExecuteNonQuery();

            _logger?.LogInformation("Backup created at {Path}", file);
        }
        catch(Exception ex)
        {
            _logger?.LogError(ex, "Database backup failed");
            throw;
        }
    }

    private async Task ApplyMigrations(DesktopTherapyDbContext db)
    {
        try
        {
            await db.Database.MigrateAsync();
            _logger?.LogInformation("Database migration completed successfully.");
        }
        catch(Exception ex)
        {
            _logger?.LogError(ex, "Database migration failed");
            throw;
        }
    }
}