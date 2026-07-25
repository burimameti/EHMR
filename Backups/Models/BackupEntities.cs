// File: EHMR.Backups/Models/BackupModels.cs
using System;
using System.Collections.Generic;

namespace EHMR.Backups.Models
{
    // =====================================================================
    //  ENUMS
    // =====================================================================

    public enum DatabaseProviderType
    {
        SqlServer = 0,
        PostgreSql = 1,
        MySql = 2,
        Sqlite = 3
    }

    public enum BackupType
    {
        Full = 0,
        Differential = 1,
        Log = 2
    }

    public enum BackupRunStatus
    {
        Pending = 0,
        Running = 1,
        Succeeded = 2,
        Failed = 3,
        Verifying = 4,
        VerificationFailed = 5
    }

    // =====================================================================
    //  BACKUP — settings / progress / result
    // =====================================================================

    public sealed class BackupSettings
    {
        public string DatabaseName { get; set; } = string.Empty;
        public BackupType Type { get; set; } = BackupType.Full;

        /// <summary>Which storage destination to save the backup to.</summary>
        public Guid DestinationId
        {
            get; set;
        }

        /// <summary>Resolved/display path of the destination (filled by engine or UI).</summary>
        public string DestinationPath { get; set; } = string.Empty;

        public bool Compress { get; set; } = true;
        public bool Encrypt { get; set; } = true;
        public bool VerifyAfterBackup { get; set; } = true;
    }

    public sealed class BackupProgress
    {
        public int Percentage
        {
            get; set;
        }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class BackupResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public BackupType Type
        {
            get; set;
        }
        public bool IsEncrypted
        {
            get; set;
        }
        public bool IsCompressed
        {
            get; set;
        }
        public long Size
        {
            get; set;
        }
        public DateTime CreatedAt
        {
            get; set;
        }
        public TimeSpan Duration
        {
            get; set;
        }
        public string Destination { get; set; } = string.Empty;
        public bool Verified
        {
            get; set;
        }
    }

    // =====================================================================
    //  RESTORE — settings / progress
    // =====================================================================

    public sealed class RestoreSettings
    {
        public string BackupFile { get; set; } = string.Empty;
        public bool IsEncrypted
        {
            get; set;
        }
        public Guid DestinationId
        {
            get; set;
        }
    }

    public sealed class RestoreProgress
    {
        public int Percentage
        {
            get; set;
        }
        public string Message { get; set; } = string.Empty;
    }

    // =====================================================================
    //  DESTINATIONS — where backups are stored (UI-facing model)
    // =====================================================================

    public sealed class BackupDestination
    {
        public Guid Id
        {
            get; set;
        }

        /// <summary>Stable provider key, e.g. "network", "s3", "local".</summary>
        public string Key { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    // =====================================================================
    //  HISTORY — persisted record of each backup run (EF entity)
    // =====================================================================

    public sealed class BackupHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string DatabaseName { get; set; } = string.Empty;
        public BackupType Type
        {
            get; set;
        }
        public BackupRunStatus Status
        {
            get; set;
        }

        public string FilePath { get; set; } = string.Empty;
        public long Size
        {
            get; set;
        }

        public bool IsCompressed
        {
            get; set;
        }
        public bool IsEncrypted
        {
            get; set;
        }
        public bool Verified
        {
            get; set;
        }

        public Guid DestinationId
        {
            get; set;
        }
        public string DestinationName { get; set; } = string.Empty;

        public DateTime StartedAt
        {
            get; set;
        }
        public DateTime? CompletedAt
        {
            get; set;
        }
        public TimeSpan? Duration
        {
            get; set;
        }

        public string? ErrorMessage
        {
            get; set;
        }
    }

    // =====================================================================
    //  CONFIGURATION — bound from appsettings.json
    // =====================================================================

    public sealed class BackupOptions
    {
        /// <summary>Standard 5-field cron expression, e.g. "0 2 * * *" = every day at 02:00.</summary>
        public string ScheduleCron { get; set; } = "0 2 * * *";

        public int RetentionDays { get; set; } = 30;

        public bool Compress { get; set; } = true;
        public bool Encrypt { get; set; } = true;
        public bool VerifyAfterBackup { get; set; } = true;

        public List<BackupDestinationOptions> Destinations { get; set; } = new();
    }

    public sealed class BackupDestinationOptions
    {
        public Guid Id
        {
            get; set;
        }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    // =====================================================================
    //  OPERATION RESULT — generic success/failure wrapper
    // =====================================================================

    public sealed class OperationResult<T>
    {
        public bool Success
        {
            get; private set;
        }
        public string Message { get; private set; } = string.Empty;
        public T? Data
        {
            get; private set;
        }

        private OperationResult()
        {
        }

        public static OperationResult<T> Ok(T data, string message = "")
            => new()
            {
                Success=true,
                Data=data,
                Message=message
            };

        public static OperationResult<T> Fail(string message)
            => new()
            {
                Success=false,
                Message=message,
                Data=default
            };
    }
}