using EHMR.Infrastructure.Persistence;
using EHMR.Views;
using Microsoft.EntityFrameworkCore;

namespace EHMR
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = default!;

        public App(IServiceProvider services)
        {
            InitializeComponent();
            ServiceProvider=services;

            _=Task.Run(async () => await InitializeDatabaseAsync());
        }

        private async Task InitializeDatabaseAsync()
        {
            using var scope = ServiceProvider.CreateScope();

            var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
            await migrator.MigrateAsync();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            // 1. НА СТАРТОТ: Го прикажуваме само LoadingPage екранот
            var loadingPage = new LoadingPage();
            var window = new Window(loadingPage);

            window.Width=1600;
            window.Height=950;
            window.MinimumWidth=1400;
            window.MinimumHeight=800;

            // 2. БЕЗБЕДНО ИНИЦИЈАЛИЗИРАЊЕ: Се активира кога прозорецот е подготвен на оперативниот систем
            window.Created+=async (s, e) =>
            {
                try
                {
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
                    Console.WriteLine($"Критична грешка при подигнување на системот: {ex}");

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