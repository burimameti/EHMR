using System;
using System.IO;
using System.Linq;

namespace EHMR
{
    /// <summary>
    /// Bounded application logger. The active log is rotated at 5 MB and
    /// old rotated logs are removed by age/count so diagnostics cannot grow
    /// without limit during long-running installations.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new();

        private const long MaxLogBytes = 5L * 1024L * 1024L;
        private const int MaxRotatedFiles = 5;
        private const int MaxLogAgeDays = 14;

        private static string _logPath = ResolveLogPath();

        private static string ResolveLogPath()
        {
            var installDir = AppDomain.CurrentDomain.BaseDirectory;
            var installLogsDir = Path.Combine(installDir, "logs");

            try
            {
                Directory.CreateDirectory(installLogsDir);
                var testFile = Path.Combine(installLogsDir, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return Path.Combine(installLogsDir, "app.log");
            }
            catch
            {
                var fallbackDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "EHMR", "logs");

                Directory.CreateDirectory(fallbackDir);
                return Path.Combine(fallbackDir, "app.log");
            }
        }

        public static void Init()
        {
            lock(_lock)
            {
                CleanupOldLogs();
                RotateIfNeeded();
            }

            Log("=== App started ===");
            Log($"Log file location: {_logPath}");
        }

        public static void Log(string message)
        {
            try
            {
                lock(_lock)
                {
                    RotateIfNeeded();

                    var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logPath, line);
                }
            }
            catch
            {
                // Logging must never affect application stability.
            }
        }

        public static void LogException(string context, Exception ex)
        {
            Log($"EXCEPTION in {context}: {ex}");

            var inner=ex.InnerException;
            var depth=1;
            while(inner!=null)
            {
                Log($"INNER[{depth}]: {inner}");
                inner=inner.InnerException;
                depth++;
            }
        }

        public static string GetLogFilePath() => _logPath;

        private static void RotateIfNeeded()
        {
            try
            {
                if(!File.Exists(_logPath))
                    return;

                var info = new FileInfo(_logPath);
                if(info.Length<MaxLogBytes)
                    return;

                var directory = Path.GetDirectoryName(_logPath);
                if(string.IsNullOrWhiteSpace(directory))
                    return;

                var archive = Path.Combine(
                    directory,
                    $"app_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log");

                File.Move(_logPath, archive);
                CleanupOldLogs();
            }
            catch
            {
                // Logging is best-effort.
            }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var directory = Path.GetDirectoryName(_logPath);
                if(string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                    return;

                var cutoff = DateTime.Now.AddDays(-MaxLogAgeDays);

                foreach(var file in Directory.EnumerateFiles(directory, "app_*.log")
                    .Select(path => new FileInfo(path))
                    .Where(file => file.LastWriteTime<cutoff))
                {
                    try { file.Delete(); } catch { }
                }

                foreach(var file in Directory.EnumerateFiles(directory, "app_*.log")
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(file => file.LastWriteTime)
                    .Skip(MaxRotatedFiles))
                {
                    try { file.Delete(); } catch { }
                }
            }
            catch
            {
                // Cleanup is best-effort.
            }
        }
    }
}