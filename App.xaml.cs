using EHMR.Backups.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Views;
using Microsoft.EntityFrameworkCore;

namespace EHMR
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = default!;

        /// <summary>
        /// Ако подигањето падне, исклучокот се чува овде за да може да се прикаже
        /// наместо празен екран.
        /// </summary>
        private static Exception? _startupException;

        /// <summary>Сигнализира дека иницијализацијата на базата завршила (успешно или не).</summary>
        private static readonly TaskCompletionSource<bool> _databaseReady = new();

        public App(IServiceProvider services)
        {
            InitializeComponent();
            ServiceProvider=services;

            _=Task.Run(async () => await InitializeDatabaseAsync());
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();

                var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
                await migrator.MigrateAsync();

                // await StartBackupSchedulerAsync();

                _databaseReady.TrySetResult(true);
            }
            catch(Exception ex)
            {
                // Ова беше без try/catch и се вртеше како fire-and-forget задача.
                // Секој пад при миграција (SQL Server не работи, одбиена врска,
                // неуспешна миграција) исчезнуваше тивко како непронајден исклучок
                // — оттука црниот екран без ниту една порака.
                _startupException=ex;
                LogStartupFailure("Иницијализација на базата", ex);

                _databaseReady.TrySetResult(false);
            }
        }

        /// <summary>
        /// Запишува во %LOCALAPPDATA%\EHMR\startup.log. Console.WriteLine во
        /// unpackaged WinUI апликација не оди никаде видливо.
        /// </summary>
        private static void LogStartupFailure(string stage, Exception ex)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "EHMR");

                Directory.CreateDirectory(dir);

                File.AppendAllText(
                    Path.Combine(dir, "startup.log"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  [{stage}]{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // Ако ни логирањето падне, нема што повеќе да се направи.
            }
        }

        /// <summary>
        /// Го подига распоредот за автоматски копии по миграциите.
        /// Неуспехот тука не смее да го спречи стартувањето на апликацијата.
        /// </summary>
        private async Task StartBackupSchedulerAsync()
        {
            try
            {
                var scheduler = ServiceProvider.GetRequiredService<IBackupScheduler>();
                await scheduler.StartAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Не успеа подигање на распоредот за резервни копии: {ex}");
            }
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            // 1. НА СТАРТОТ: Го прикажуваме само LoadingPage екранот
            var loadingPage = new LoadingPage();
            var window = new Window(loadingPage);

            window.Width=1800;
            window.Height=1250;
            window.MinimumWidth=1600;
            window.MinimumHeight=1200;

            // 2. БЕЗБЕДНО ИНИЦИЈАЛИЗИРАЊЕ: Се активира кога прозорецот е подготвен на оперативниот систем
            window.Created+=async (s, e) =>
            {
                try
                {
                    // Почекај ја базата — ако падне, прикажи ја грешката наместо празен екран.
                    await _databaseReady.Task;

                    if(_startupException is not null)
                        throw new InvalidOperationException(
                            "Иницијализацијата на базата не успеа. Види %LOCALAPPDATA%\\EHMR\\startup.log",
                            _startupException);

                    // Го земаме AppShell од Dependency Injection контејнерот
                    var shell = ServiceProvider.GetRequiredService<AppShell>();

                    // Изврши ја првичната проверка (дали е логиран, кои менија му се видливи)
                    // ПРЕД да го прикажеме самиот Shell
                    await shell.HandleInitialNavigationAsync();

                    // 3. ТРАНЗИЦИЈА: Кога сè е успешно проверено, го заменуваме LoadingPage со спремниот AppShell
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        window.Page=shell;
                    });
                }
                catch(Exception ex)
                {
                    LogStartupFailure("Подигнување на системот", ex);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        window.Page=new ErrorPage(ex);
                    });
                }
            };

            return window;
        }
    }
}