using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class DatabaseMigrationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationService>? _logger;
    private readonly IConfiguration _configuration;

    private static readonly TimeSpan DemoResetInterval = TimeSpan.FromDays(2);

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
            // Ако демо базата е постара од 2 дена, ја бришеме за да се
            // прегенерира чиста (нови демо податоци при следното подигање).
            TryResetDemoSqliteIfExpired(db);

            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            var pending = await db.Database.GetPendingMigrationsAsync();

            //  if(pending.Any())
            {
                //BackupDatabase(db);
                await db.Database.MigrateAsync();
            }
        }

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

        // Ако имаме демо податоци во SQLite, го „допираме" маркерот за да
        // знаеме од кога да броиме до следното автоматско ресетирање.
        if(db.Database.IsSqlite()&&includeDemo)
        {
            TouchDemoMarker(db);
        }
    }

    /// <summary>
    /// Ако базата е SQLite и демо-маркерот е постар од 2 дена, ги брише
    /// датотеката на базата (вкл. -wal/-shm) и маркерот, за да EnsureCreatedAsync
    /// ја изгради базата од нула со свежи демо податоци.
    /// </summary>
    private void TryResetDemoSqliteIfExpired(DesktopTherapyDbContext db)
    {
        var dbPath = GetSqliteDbPath(db);
        if(string.IsNullOrWhiteSpace(dbPath))
            return;

        var markerPath = GetDemoMarkerPath(dbPath);

        if(!File.Exists(markerPath))
            return;

        var lastReset = File.GetLastWriteTimeUtc(markerPath);

        if(DateTime.UtcNow-lastReset<DemoResetInterval)
            return;

        try
        {
            // Затвора се сите пулирани конекции пред бришење, инаку
            // датотеката ќе биде заклучена.
            SqliteConnection.ClearAllPools();

            if(File.Exists(dbPath))
                File.Delete(dbPath);

            var wal = dbPath+"-wal";
            var shm = dbPath+"-shm";
            if(File.Exists(wal)) File.Delete(wal);
            if(File.Exists(shm)) File.Delete(shm);

            File.Delete(markerPath);

            _logger?.LogInformation(
                "Демо SQLite базата е ресетирана ({Path}). Последно ресетирање: {LastReset}.",
                dbPath, lastReset);
        }
        catch(Exception ex)
        {
            _logger?.LogError(ex, "Неуспешно ресетирање на демо SQLite базата на {Path}", dbPath);
        }
    }

    private void TouchDemoMarker(DesktopTherapyDbContext db)
    {
        var dbPath = GetSqliteDbPath(db);
        if(string.IsNullOrWhiteSpace(dbPath))
            return;

        var markerPath = GetDemoMarkerPath(dbPath);

        try
        {
            File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
        }
        catch(Exception ex)
        {
            _logger?.LogError(ex, "Неуспешно запишување на демо маркерот на {Path}", markerPath);
        }
    }

    private static string? GetSqliteDbPath(DesktopTherapyDbContext db)
    {
        var conn = db.Database.GetConnectionString();
        if(string.IsNullOrWhiteSpace(conn))
            return null;

        var builder = new SqliteConnectionStringBuilder(conn);
        return string.IsNullOrWhiteSpace(builder.DataSource) ? null : builder.DataSource;
    }

    private static string GetDemoMarkerPath(string dbPath) => dbPath+".demoseed";

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