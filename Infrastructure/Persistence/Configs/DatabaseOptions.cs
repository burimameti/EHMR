namespace EHMR.Infrastructure.Persistence.Configs
{
    public enum DatabaseProvider
    {
        SqlServer,
        Sqlite
    }

    /// <summary>
    /// Избор на база, читан од секцијата <c>Database</c> во appsettings.json.
    ///
    /// Порано врската беше зашиена на три места: во RegisterDb, во
    /// DesktopTherapyDbContext.DefaultConnection и во appsettings (кое го читаше
    /// само design-time факторот). Оваа класа е единствениот извор.
    /// </summary>
    /// 
    public sealed class DatabaseOptions
    {
        public const string SectionName = "Database";

        public DatabaseProvider Provider
        {
            get; set;
        } = DatabaseProvider.Sqlite;

        public string SqlServerConnection { get; set; } = "";

        /// <summary>
        /// Име или патека на SQLite фајлот.
        /// </summary>
        public string SqliteDatabase { get; set; } = "therapy.db";

        public string BuildConnectionString()
            => Provider switch
            {
                DatabaseProvider.Sqlite => $"Data Source={ResolveSqlitePath()}",
                _ => SqlServerConnection
            };

        public string ResolveSqlitePath()
            => Path.IsPathRooted(SqliteDatabase)
                ? SqliteDatabase
                : Path.Combine(DataDirectory, SqliteDatabase);

        public static string DataDirectory
            => Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "EHMR");

        public void EnsureSqliteDirectory()
        {
            if(Provider!=DatabaseProvider.Sqlite)
                return;

            var folder = Path.GetDirectoryName(ResolveSqlitePath());

            if(!string.IsNullOrWhiteSpace(folder))
                Directory.CreateDirectory(folder);
        }

        /// <summary>
        /// Ја брише SQLite базата ако последната промена е постара
        /// од зададениот период.
        /// </summary>
        public bool DeleteSqliteDatabaseIfOlderThan(TimeSpan maxAge)
        {
            if(Provider!=DatabaseProvider.Sqlite)
                return false;

            var dbPath = ResolveSqlitePath();

            if(!File.Exists(dbPath))
                return false;

            var lastWriteUtc = File.GetLastWriteTimeUtc(dbPath);
            var age = DateTime.UtcNow-lastWriteUtc;

            if(age<=maxAge)
                return false;

            DeleteIfExists(dbPath);

            // SQLite journal files
            DeleteIfExists(dbPath+"-wal");
            DeleteIfExists(dbPath+"-shm");

            return true;
        }

        private static void DeleteIfExists(string path)
        {
            if(File.Exists(path))
                File.Delete(path);
        }
    }
    //public sealed class DatabaseOptions
    //{
    //    public const string SectionName = "Database";

    //    public DatabaseProvider Provider
    //    {
    //        get; set;
    //    } = DatabaseProvider.Sqlite;

    //    public string SqlServerConnection { get; set; } = "";

    //    /// <summary>
    //    /// Име или патека на SQLite фајлот. Релативна патека се решава
    //    /// од папката на апликацијата.
    //    /// </summary>
    //    public string SqliteDatabase { get; set; } = "therapy.db";

    //    /// <summary>Врската за активниот провајдер.</summary>
    //    public string BuildConnectionString()
    //        => Provider switch
    //        {
    //            DatabaseProvider.Sqlite => $"Data Source={ResolveSqlitePath()}",
    //            _ => SqlServerConnection
    //        };

    //    /// <summary>
    //    /// Апсолутна патека се користи како што е зададена. Релативната оди во
    //    /// %LOCALAPPDATA%\EHMR, не во папката на програмата — инсталирана
    //    /// апликација живее во „Program Files", каде обичен корисник нема право
    //    /// да пишува и базата не би можела да се создаде.
    //    /// </summary>
    //    public string ResolveSqlitePath()
    //        => Path.IsPathRooted(SqliteDatabase)
    //            ? SqliteDatabase
    //            : Path.Combine(DataDirectory, SqliteDatabase);

    //    /// <summary>Папката за податоци на корисникот.</summary>
    //    public static string DataDirectory
    //        => Path.Combine(
    //            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    //            "EHMR");

    //    /// <summary>
    //    /// SQLite не создава папка сам — отворањето на врска кон непостоечка
    //    /// папка паѓа со „unable to open database file".
    //    /// </summary>
    //    public void EnsureSqliteDirectory()
    //    {
    //        if(Provider!=DatabaseProvider.Sqlite)
    //            return;

    //        var folder = Path.GetDirectoryName(ResolveSqlitePath());

    //        if(!string.IsNullOrWhiteSpace(folder))
    //            Directory.CreateDirectory(folder);
    //    }
    //}
}
