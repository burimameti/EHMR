using EHMR.Backups.Interfaces;

using EHMR.Views;

namespace EHMR
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = default!;

        private static Exception? _startupException;
        private static readonly TaskCompletionSource<bool> _databaseReady = new();

        public App(IServiceProvider services)
        {
            InitializeComponent();

            // Logger.Init() MORA да е прво нешто што се извршува - ако ова падне,
            // сакаме барем да пробаме fallback патека пред да продолжиме понатаму.
            Logger.Init();
            Logger.Log("App constructor started");

            AppDomain.CurrentDomain.UnhandledException+=(s, e) =>
            {
                Logger.LogException("AppDomain UnhandledException",
                    e.ExceptionObject as Exception??new Exception("Unknown exception object"));
            };

            // Логира ВСЕКОЈ throw во моментот кога се случува, дури и ако нешто
            // подоцна го фати и "проголта". Ова е единствениот начин да видиме
            // исклучоци што Shell/MAUI internals ги имаат catch-нато тивко.
            AppDomain.CurrentDomain.FirstChanceException+=(s, e) =>
            {
                // Do not synchronously write every first-chance exception to disk.
                // MAUI/WinUI and EF Core legitimately throw/catch many internal
                // exceptions; logging all of them was adding significant I/O during
                // navigation and hiding the exceptions that actually matter.
                var ex=e.Exception;
                var isEfParameterException =
                    ex.Message.Contains("LINQ query parameter expression", StringComparison.OrdinalIgnoreCase) ||
                    ex.InnerException?.Message.Contains("unbound variable", StringComparison.OrdinalIgnoreCase)==true;
                var isXamlException = ex.GetType().Name.Contains("XamlParseException", StringComparison.Ordinal);
                var isFormatException = ex is FormatException;
                var isComException = ex is System.Runtime.InteropServices.COMException;
                var isRoutingException =
                    ex.Message.Contains("Relative routing to shell elements", StringComparison.OrdinalIgnoreCase);

                if(isEfParameterException || isXamlException || isFormatException || isComException || isRoutingException)
                    Logger.LogException("FirstChanceException", ex);
            };

            TaskScheduler.UnobservedTaskException+=(s, e) =>
            {
                Logger.LogException("TaskScheduler UnobservedTaskException", e.Exception);
                e.SetObserved();
            };

            ServiceProvider=services;
            Logger.Log("ServiceProvider assigned, starting database initialization task");

            _=Task.Run(async () => await InitializeDatabaseAsync());
        }

        private async Task InitializeDatabaseAsync()
        {
            Logger.Log("InitializeDatabaseAsync started");
            try
            {
                using var scope = ServiceProvider.CreateScope();
                Logger.Log("DI scope created, resolving DatabaseMigrationService");

                var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
                Logger.Log("DatabaseMigrationService resolved, running MigrateAsync");

                await migrator.MigrateAsync();
                Logger.Log("Database migration completed successfully");

                _databaseReady.TrySetResult(true);
            }
            catch(Exception ex)
            {
                _startupException=ex;
                Logger.LogException("Database initialization", ex);
                _databaseReady.TrySetResult(false);
            }
        }

        private async Task StartBackupSchedulerAsync()
        {
            try
            {
                Logger.Log("Starting backup scheduler");
                var scheduler = ServiceProvider.GetRequiredService<IBackupScheduler>();
                await scheduler.StartAsync();
                Logger.Log("Backup scheduler started successfully");
            }
            catch(Exception ex)
            {
                Logger.LogException("StartBackupSchedulerAsync", ex);
                // Не смее да спречи стартување на апликацијата - веќе точно.
            }
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            Logger.Log("CreateWindow called");

            var loadingPage = new LoadingPage();
            var window = new Window(loadingPage);

            window.Width=1800;
            window.Height=1250;
            window.MinimumWidth=1600;
            window.MinimumHeight=1200;

            window.Created+=async (s, e) =>
            {
                Logger.Log("Window.Created fired - waiting for database readiness");
                try
                {
                    await _databaseReady.Task;
                    Logger.Log("Database readiness task completed");

                    if(_startupException is not null)
                    {
                        Logger.Log("Startup exception was set - throwing to show ErrorPage");
                        throw new InvalidOperationException(
                            $"Иницијализацијата на базата не успеа. Види лог: {Logger.GetLogFilePath()}",
                            _startupException);
                    }

                    Logger.Log("Resolving AppShell from DI");
                    var shell = ServiceProvider.GetRequiredService<AppShell>();

                    Logger.Log("Calling shell.HandleInitialNavigationAsync (checks login state)");
                    await shell.HandleInitialNavigationAsync();
                    Logger.Log("HandleInitialNavigationAsync completed successfully");

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Logger.Log("Swapping window.Page to AppShell");
                        window.Page=shell;
                    });
                }
                catch(Exception ex)
                {
                    Logger.LogException("Window startup / navigation", ex);

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