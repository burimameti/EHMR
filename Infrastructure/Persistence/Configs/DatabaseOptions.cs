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
    public sealed class DatabaseOptions
    {
        public const string SectionName = "Database";

        public DatabaseProvider Provider
        {
            get; set;
        } = DatabaseProvider.Sqlite;

        public string SqlServerConnection { get; set; } = "";

        /// <summary>
        /// Име или патека на SQLite фајлот. Релативна патека се решава
        /// од папката на апликацијата.
        /// </summary>
        public string SqliteDatabase { get; set; } = "therapy.db";

        /// <summary>Врската за активниот провајдер.</summary>
        public string BuildConnectionString()
            => Provider switch
            {
                DatabaseProvider.Sqlite => $"Data Source={ResolveSqlitePath()}",
                _ => SqlServerConnection
            };

        public string ResolveSqlitePath()
            => Path.IsPathRooted(SqliteDatabase)
                ? SqliteDatabase
                : Path.Combine(AppContext.BaseDirectory, SqliteDatabase);
    }
}
