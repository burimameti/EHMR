using System;
using System.IO;

namespace EHMR
{
    /// <summary>
    /// Единствен логер за апликацијата. Пишува во {InstallFolder}\logs\app.log.
    /// Ако install папката не е запишлива (случува се на некои машини),
    /// автоматски преминува на %LOCALAPPDATA%\EHMR\logs\app.log.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new();
        private static string _logPath = ResolveLogPath();

        private static string ResolveLogPath()
        {
            // Прв обид: папка "logs" веднаш до .exe (install folder)
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
                // Fallback: install папката не е запишлива (типично кога апликацијата
                // е инсталирана во C:\Program Files без "users-modify" пермисии).
                var fallbackDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "EHMR", "logs");
                Directory.CreateDirectory(fallbackDir);
                return Path.Combine(fallbackDir, "app.log");
            }
        }

        public static void Init()
        {
            Log("=== App started ===");
            Log($"Log file location: {_logPath}");
        }

        public static void Log(string message)
        {
            try
            {
                lock(_lock)
                {
                    var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logPath, line);
                }
            }
            catch
            {
                // Логирањето никогаш не смее да ја урне апликацијата.
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
    }
}