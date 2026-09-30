using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
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

        // EF миграциите се врзани за провајдерот — оние во Migrations/ се генерирани
        // за SQL Server и не поминуваат на SQLite. За SQLite шемата се создава
        // директно од моделот.
        if(db.Database.IsSqlite())
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

            if(pending.Count > 0)
                _logger?.LogInformation("Applying {Count} pending database migrations: {Migrations}", pending.Count, string.Join(", ", pending));

            await db.Database.MigrateAsync();

            // Older local databases may have been created before the migration was
            // compiled into the application. Make the PatientMedicine schema safe
            // before any EF query can touch PharmaceuticalReference.
            await EnsurePatientMedicineSchemaAsync(db);
        }

        await NormalizeLegacyStatusesAsync(db);

        // Демо податоците се нудат точно еднаш, при првото подигање.
        // Одлуката — прифатена или одбиена — се памти во AppLicense и повеќе
        // не се прашува.
        var license = scope.ServiceProvider.GetRequiredService<ILicenseService>();

        var includeDemo = false;

        if(await license.ShouldOfferDemoAsync())
        {
            includeDemo=await AskForDemoDataAsync();
            await license.RecordDemoDecisionAsync(includeDemo);
        }
        else
        {
            var state = await license.GetStateAsync();
            includeDemo=state.DemoDataSeeded;
        }

        var runner = scope.ServiceProvider.GetRequiredService<SeederRunner>();
        await runner.RunAsync(includeDemo);
    }

    private async Task EnsurePatientMedicineSchemaAsync(DesktopTherapyDbContext db)
    {
        if(!db.Database.IsSqlServer())
            return;

        var connection=db.Database.GetDbConnection();
        await using var command=connection.CreateCommand();
        command.CommandText="""
            IF COL_LENGTH('dbo.PatientMedicines', 'PharmaceuticalReference') IS NULL
            BEGIN
                ALTER TABLE dbo.PatientMedicines
                ADD PharmaceuticalReference nvarchar(200) NOT NULL CONSTRAINT DF_PatientMedicines_PharmaceuticalReference DEFAULT '';
            END
            """;

        if(connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await command.ExecuteNonQueryAsync();
        _logger?.LogInformation("Verified PatientMedicines.PharmaceuticalReference schema.");
    }

    private static async Task NormalizeLegacyStatusesAsync(DesktopTherapyDbContext db)
    {
        // Values 4+ were previously used by removed Missed/NoShow statuses.
        // Keep existing data valid by mapping all obsolete terminal values to Cancelled.
        var appointments = await db.Appointments
            .Where(x => (int)x.Status>3)
            .ToListAsync();

        foreach(var appointment in appointments)
            appointment.Status=AppointmentStatus.Cancelled;

        var encounters = await db.Encounters
            .Where(x => (int)x.Status>3)
            .ToListAsync();

        foreach(var encounter in encounters)
            encounter.Status=EncounterStatus.Cancelled;

        if(appointments.Count>0||encounters.Count>0)
            await db.SaveChangesAsync();
    }

    /// <summary>
    /// Прашува дали да се внесат демо податоци. Дијалогот бара UI нишка —
    /// иницијализацијата се врти во позадина.
    /// </summary>
    private static async Task<bool> AskForDemoDataAsync()
    {
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;

                if(page is null)
                    return false;

                return await page.DisplayAlert(
                    "Демо податоци",
                    "Дали сакате да се внесат демо податоци за проба? "+
                    "Ова се нуди само еднаш.",
                    "Да",
                    "Не");
            });
        }
        catch
        {
            // Ако дијалогот не може да се прикаже, не внесувај ништо.
            return false;
        }
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