using EHMR.Backups.Models;

namespace EHMR.Backups.Interfaces
{
    // =====================================================================
    //  ENGINES — top-level orchestration
    // =====================================================================

    public interface IBackupEngine
    {
        Task<OperationResult<BackupResult>> ExecuteAsync(
            BackupSettings settings,
            IProgress<BackupProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }

    public interface IRestoreEngine
    {
        Task<OperationResult<bool>> ExecuteAsync(
            RestoreSettings settings,
            IProgress<RestoreProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }

    // =====================================================================
    //  DATABASE PROVIDERS — per-DBMS backup/restore implementation
    // =====================================================================

    public interface IDatabaseBackupProvider
    {
        /// <summary>Whether this provider can handle the given connection string.</summary>
        bool CanHandle(string connectionString);

        /// <summary>Creates a backup file in the given destination folder, returns full path.</summary>
        Task<string> CreateBackupAsync(
            string destination,
            CancellationToken cancellationToken = default);

        Task RestoreAsync(
            string backupFile,
            CancellationToken cancellationToken = default);

        Task<bool> VerifyBackupAsync(
            string backupFile,
            CancellationToken cancellationToken = default);

        Task<long> GetDatabaseSizeAsync(
            CancellationToken cancellationToken = default);
    }

    public interface IDatabaseProviderResolver
    {
        /// <summary>Resolves the configured/default database provider.</summary>
        IDatabaseBackupProvider Resolve();

        /// <summary>Resolves a database provider by type (SqlServer, PostgreSql, ...).</summary>
        IDatabaseBackupProvider Resolve(DatabaseProviderType type);
    }

    // =====================================================================
    //  STORAGE PROVIDERS — where backup files physically go
    // =====================================================================

    public interface IBackupStorageProvider
    {
        Guid DestinationId
        {
            get;
        }

        /// <summary>Stable provider key, e.g. "network", "s3", "local".</summary>
        string Key
        {
            get;
        }

        string Name
        {
            get;
        }

        Task<string> SaveAsync(
            string file,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string file,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            string file,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<string>> ListAsync(
            CancellationToken cancellationToken = default);
    }

    public interface IStorageProviderResolver
    {
        IBackupStorageProvider Resolve(Guid destinationId);
        IBackupStorageProvider Resolve(string key);
    }

    // =====================================================================
    //  SUPPORTING SERVICES — compression / encryption / verification
    // =====================================================================

    public interface ICompressionService
    {
        /// <summary>Compresses the file, returns path to the compressed file.</summary>
        Task<string> CompressAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>Decompresses the file, returns path to the decompressed file.</summary>
        Task<string> DecompressAsync(
            string filePath,
            CancellationToken cancellationToken = default);
    }

    public interface IBackupSecurityProvider
    {
        /// <summary>Encrypts the file, returns path to the encrypted file.</summary>
        Task<string> EncryptAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>Decrypts the file, returns path to the decrypted file.</summary>
        Task<string> DecryptAsync(
            string filePath,
            CancellationToken cancellationToken = default);
    }

    public interface IBackupVerifier
    {
        Task<bool> VerifyAsync(
            string filePath,
            CancellationToken cancellationToken = default);
    }

    // =====================================================================
    //  HISTORY REPOSITORY — persistence for dashboard/history views
    // =====================================================================

    public interface IBackupHistoryRepository
    {
        Task<Guid> RecordStartAsync(
            BackupSettings settings,
            CancellationToken cancellationToken = default);

        Task RecordCompletionAsync(
            Guid historyId,
            OperationResult<BackupResult> result,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BackupHistory>> GetRecentAsync(
            int count,
            CancellationToken cancellationToken = default);

        Task<BackupHistory?> GetLastAsync(
            CancellationToken cancellationToken = default);

        Task<BackupHistory?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task DeleteOlderThanAsync(
            DateTime cutoffUtc,
            CancellationToken cancellationToken = default);
    }

    // =====================================================================
    //  DESTINATION REPOSITORY — CRUD for configured storage destinations
    // =====================================================================

    public interface IBackupDestinationRepository
    {
        Task<IReadOnlyList<BackupDestination>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<BackupDestination?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<Guid> AddAsync(
            BackupDestination destination,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            BackupDestination destination,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}