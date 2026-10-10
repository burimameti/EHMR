using EHMR.Abstraction;
using EHMR.Backups.Encryption;
using EHMR.Backups.Engine;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using EHMR.Backups.Providers;
using EHMR.Backups.Scheduler;
using EHMR.Backups.Services;
using EHMR.Backups.ViewModels;
using EHMR.Backups.Views;
using EHMR.Domain.Entities.Reports;
using EHMR.Domain.Interfaces;
using EHMR.Domain.Search;
using EHMR.Domain.SparkForm;
using EHMR.Infrastructure.Persistence;
using EHMR.Infrastructure.Persistence.Configs;
using EHMR.Infrastructure.Persistence.Seeders;
using EHMR.Infrastructure.Services;
using EHMR.Services;
using EHMR.ViewModels;
using EHMR.ViewModels.Admin;
using EHMR.ViewModels.Appointments;
using EHMR.ViewModels.Calendar;
using EHMR.ViewModels.Encounters;
using EHMR.ViewModels.Mkb10;
using EHMR.ViewModels.Patients;
using EHMR.ViewModels.Prescriptions;
using EHMR.ViewModels.Reports;
using EHMR.ViewModels.Support;
using EHMR.ViewModels.Therapies;
using EHMR.Views;
using EHMR.Views.Admin;
using EHMR.Views.Appointments;
using EHMR.Views.Calendar;
using EHMR.Views.Encounters;
using EHMR.Views.Mkb10;
using EHMR.Views.Patients;
using EHMR.Views.Prescription;
using EHMR.Views.Protocols;
using EHMR.Views.Reports;
using EHMR.Views.Therapies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR
{
    public static class EHMRServiceCollectionExtensions
    {
        // =====================================================
        // API / BACKEND SERVICES
        // =====================================================

        public static IServiceCollection RegisterEHMR(this IServiceCollection services)
        {
            // =========================
            // CORE SERVICES
            // =========================
            services.RegisterDb();
            services.RegisterApiServices();
            services.RegisterViewModels();
            services.RegisterPages();
            services.RegisterModules();

            // IMPORTANT: run migrations AFTER DI is built
            services.AddSingleton<DatabaseMigrationService>();

            return services;
        }

        /// <summary>
        /// Стандардната SQL Server врска. Се користи само ако appsettings.json нема
        /// секција "Database" — за да не се смени однесувањето на постоечки инсталации.
        /// </summary>
        private const string FallbackSqlServerConnection =
            "Server=.\\SQLEXPRESS;Database=TherapyTrackerDesktopPoc;User Id=t24test;Password=t24test;MultipleActiveResultSets=True;TrustServerCertificate=True;";

        private static DatabaseOptions ReadDatabaseOptions()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            var options =
                configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
                ??new DatabaseOptions();

            if(string.IsNullOrWhiteSpace(options.SqlServerConnection))
            {
                // Стар формат: ConnectionStrings:Default, инаку вградената врска.
                options.SqlServerConnection=
                    configuration.GetConnectionString("Default")
                    ??FallbackSqlServerConnection;
            }

            return options;
        }

        /// <summary>Го применува избраниот провајдер врз EF опциите.</summary>
        public static void ConfigureDatabase(DbContextOptionsBuilder builder, DatabaseOptions options)
        {
            switch(options.Provider)
            {
                case DatabaseProvider.Sqlite:
                    options.EnsureSqliteDirectory();
                    builder.UseSqlite(options.BuildConnectionString());
                    break;

                default:
                    builder.UseSqlServer(options.BuildConnectionString());
                    break;
            }
        }

        private static IServiceCollection RegisterDb(this IServiceCollection services)
        {
            services.AddSingleton<IDbExceptionParserProvider,
                              DbExceptionParserProvider>();

            // =====================================================
            // ИЗБОР НА БАЗА
            //
            // Провајдерот и врската се читаат од секцијата "Database" во
            // appsettings.json. Порано врската беше зашиена тука како константа,
            // а обете регистрации на контекстот беа закоментирани — оттука
            // „No service for type 'DesktopTherapyDbContext' has been registered".
            // =====================================================
            var databaseOptions = ReadDatabaseOptions();

            services.AddSingleton(databaseOptions);

            services.AddDbContextFactory<DesktopTherapyDbContext>(options =>
                ConfigureDatabase(options, databaseOptions));

            // DatabaseMigrationService и делови од апликацијата го бараат самиот
            // контекст, а AddDbContextFactory регистрира само IDbContextFactory<T>.
            // Овој scoped запис го добива од факторот, за да има еден извор.
            services.AddScoped(sp =>
                sp.GetRequiredService<IDbContextFactory<DesktopTherapyDbContext>>()
                  .CreateDbContext());
            // Order = 0 — каталогот на МКБ-10 се полни пред сите останати seeder-и.
            services.AddScoped<IEntitySeeder, Mkb10CatalogSeeder>();

            services.AddScoped<IEntitySeeder, AlertSeeder>();
            services.AddScoped<IEntitySeeder, AppointmentSeeder>();
            services.AddScoped<IEntitySeeder, AuditLogSeeder>();

            services.AddScoped<IEntitySeeder, DoctorSeeder>();
            services.AddScoped<IEntitySeeder, DiagnosisSeeder>();
            services.AddScoped<IEntitySeeder, DocumentSeeder>();
            services.AddScoped<IEntitySeeder, EncounterSeeder>();
            services.AddScoped<IEntitySeeder, ClinicalScenarioSeeder>();
            services.AddScoped<IEntitySeeder, PatientScoreSeeder>();

            services.AddScoped<IEntitySeeder, UserSeeder>();
            services.AddScoped<IEntitySeeder, UserScopeSeeder>();

            services.AddScoped<IEntitySeeder, PatientSeeder>();

            services.AddScoped<IEntitySeeder, MedicineSeeder>();
            services.AddScoped<IEntitySeeder, ApplicationRegimeSeeder>();
            services.AddScoped<IEntitySeeder, NotificationSeeder>();
            services.AddScoped<IEntitySeeder, PatientMedicineSeeder>();
            services.AddScoped<IEntitySeeder, FunctionalCoverageSeeder>();
            services.AddScoped<IEntitySeeder, TherapyProtocolSeeder>();
            //  services.AddScoped<IEntitySeeder, TherapyProtocolMedicineSeeder>();
            services.AddScoped<IEntitySeeder, PrescriptionSeeder>();
            services.AddSingleton<IReportProvider, MissedTherapiesReportProvider>();

           services.AddSingleton<IReportProvider, PatientsReportProvider>();
           services.AddSingleton<IReportProvider, MedicineUsageReportProvider>();

            services.AddSingleton<IReportProvider, AppointmentStatusesReportProvider>();

            services.AddSingleton<IReportProvider, AuditReportProvider>();

            //   
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddSingleton<IBackupSecurityProvider, BackupSecurityProvider>();

            services.AddSingleton<IDatabaseBackupProvider, SqlServerBackupProvider>();
            services.AddSingleton<IDatabaseProviderResolver, DatabaseProviderResolver>();
            // =========================
            // BACKUP DESTINATIONS
            //
            // Секоја дестинација од Backup:Destinations се регистрира како посебен
            // провајдер. Порано беше врзан само "local", па StorageProviderResolver
            // фрлаше за "cloud" и "network" иако провајдерите се напишани — а UI-то
            // ја гради листата на дестинации од регистрираните провајдери, значи
            // прикажуваше само една.
            // =========================
            services.AddSingleton<IEnumerable<IBackupStorageProvider>>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<BackupOptions>>().Value;
                var configuration = sp.GetRequiredService<IConfiguration>();

                var providers = new List<IBackupStorageProvider>();

                foreach(var dest in opts.Destinations)
                {
                    if(string.IsNullOrWhiteSpace(dest.Path))
                        continue;

                    switch(dest.Key?.ToLowerInvariant())
                    {
                        case "local":
                            providers.Add(new LocalStorageProvider(dest));
                            break;

                        case "network":
                            providers.Add(new NetworkStorageProvider(dest));
                            break;

                        case "cloud":
                            // Без стринг за поврзување BlobContainerClient фрла веднаш,
                            // што би го срушило подигањето на апликацијата. Затоа само
                            // кога е конфигуриран.
                            var azure = configuration["Azure:StorageConnectionString"];
                            if(!string.IsNullOrWhiteSpace(azure))
                                providers.Add(new CloudStorageProvider(dest, azure));
                            break;
                    }
                }

                return providers;
            });

            // Резолверот бара IEnumerable<IBackupStorageProvider>; сврти го кон горната
            // регистрација наместо кон поединечни AddSingleton повици.
            services.AddSingleton<IStorageProviderResolver>(sp =>
                new StorageProviderResolver(
                    sp.GetRequiredService<IEnumerable<IBackupStorageProvider>>()));

            services.AddSingleton<IBackupEngine, BackupEngine>();
            services.AddSingleton<IRestoreEngine, RestoreEngine>();
            services.AddSingleton<ReportRegistry>();
            services.AddSingleton<Mkb10ImportService>();
            services.AddScoped<SeederRunner>();

            return services;
        }

        private static IServiceCollection RegisterApiServices(this IServiceCollection services)
        {
            // services.AddSingleton<IApiService, ApiService>();
            // services.AddSingleton<IApiClient, ApiClient>();
            services.AddSingleton<IAuthStateService, AuthStateService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IPatientService, PatientService>();
            // services.AddSingleton<IAuthService, AuthService>();

            services.AddSingleton<IAuthorizationService, AuthorizationService>();
            services.AddSingleton<IBackupScheduler, BackupScheduler>();
            services.AddSingleton<ICompressionService, CompressionService>();
            // services.AddSingleton<IAnalyticsService, AnalyticsService>();
            // services.AddSingleton<IPolicyEngine, PolicyEngine>();
            services.AddSingleton<ILicenseService, LicenseService>();
            // services.AddSingleton<IPermissionService, PermissionService>();
            services.AddSingleton<IAppointmentSearchQueryHandler, AppointmentSearchQueryHandler>();
            services.AddSingleton<IPreferencesService, SecurePreferencesService>();
            services.AddSingleton<INavigationCoordinator, NavigationCoordinator>();
            //  services.AddSingleton<IAppointmentService, AppointmentService>();
            // services.AddSingleton<IDocumentService, DocumentService>();
            // services.AddSingleton<IPrescriptionService, PrescriptionService>();
            services.AddSingleton<IReportHistoryService, ReportHistoryService>();
            services.AddSingleton<IUserDialogService, UserDialogService>();
            services.AddSingleton<IAppointmentDetailService, AppointmentDetailService>();
            services.AddSingleton<ITherapyService, TherapyService>();
            //services.AddSingleton<ICalendarEngine, CalendarEngine>();
            //services.AddSingleton<ITimelineEngine, TimelineEngine>();
            services.AddScoped<IAlertService, AlertService>();
            services.AddSingleton(typeof(ISelectedItemService<>), typeof(SelectedItemService<>));
            // services.AddSingleton<INotificationService, NotificationService>();
            // services.AddSingleton<IPatientService, PatientService>();
            // services.AddSingleton<ITherapyWorkflowService, TherapyWorkflowService>();
            //  services.AddSingleton<ITherapyCycleService, TherapyCycleService>();
            //  services.AddSingleton<ITherapyScheduleService, TherapyScheduleService>();
            //  services.AddSingleton<IDashboardService, DashboardService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IEncounterDetailService, EncounterDetailService>();
            services.AddSingleton<INavigationHost, NavigationHost>();
            services.AddSingleton<INavigationDataStore, NavigationDataStore>();
            services.AddSingleton<INavigationEvents, NavigationEvents>();
            services.AddSingleton<IMenuService, MenuService>();
            services.AddSingleton<IPrescriptionService, PrescriptionService>();
            services.AddSingleton<IDoctorService, DoctorService>();
            services.AddSingleton<IMkb10CodeService, Mkb10CodeService>();
            services.AddSingleton<ISparkFormBuilder, SparkFormBuilder>();
            services.AddSingleton<IReportExportService, ReportExportService>();
            services.AddSingleton<IPatientClinicalReportService, PatientClinicalReportService>();
            services.AddScoped<IBackupHistoryRepository, BackupHistoryRepository>();
            services.AddScoped<IBackupDestinationRepository, BackupDestinationRepository>();
            services.AddScoped<IBackupVerifier, BackupVerifier>();
            // services.AddSingleton<ThemeService>();
            services.AddSingleton<IFileDialogService, MauiFileDialogService>();

            return services;
        }

        // =====================================================
        // VIEWMODELS
        // =====================================================
        private static IServiceCollection RegisterViewModels(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var viewModels = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("ViewModel")||t.Name.EndsWith("VM"))
                .Where(t => t.IsClass&&!t.IsAbstract);

            foreach(var vm in viewModels)
            {
                services.AddTransient(vm);
            }

            return services;
        }

        // =====================================================
        // PAGES
        // =====================================================
        private static IServiceCollection RegisterPages(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var pages = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Page"))
                .Where(t => t.IsClass&&!t.IsAbstract);

            foreach(var page in pages)
            {
                services.AddTransient(page);
            }

            return services;
        }

        // =====================================================
        // FEATURE MODULES (OPTIONAL CLEAN ARCH)
        // =====================================================
        private static IServiceCollection RegisterModules(this IServiceCollection services)
        {
            services.RegisterPatientModule();
            // services.RegisterTherapyModule();
            //  services.RegisterNotificationModule();
            services.AddUiCore();

            return services;
        }

        // =========================
        // PATIENT MODULE
        // =========================
        private static IServiceCollection RegisterPatientModule(this IServiceCollection services)
        {
            services.AddTransient<ReportViewModel>();
            return services;
        }

        // =========================
        // THERAPY MODULE
        // =========================
        //private static IServiceCollection RegisterTherapyModule(this IServiceCollection services)
        //{
        //    services.AddTransient<PatientTherapyDashboardViewModel>();
        //    services.AddTransient<TherapyCycleViewModel>();
        //    services.AddTransient<TherapyScheduleViewModel>();
        //    return services;
        //}

        // =========================
        // NOTIFICATION MODULE
        // =========================
        //private static IServiceCollection RegisterNotificationModule(this IServiceCollection services)
        //{
        //    services.AddTransient<NotificationViewModel>();
        //    return services;
        //}

        public static IServiceCollection AddUiCore(this IServiceCollection services)
        {
            services.AddSingleton<RootLayoutPage>();
            services.AddSingleton<LoadingPage>();
            // services.AddSingleton<SvgIconConverter>();
            // services.AddTransient<SplashPage>();
            //services.AddTransient<MenuPage>();
            services.AddTransient<MenuView>();
            // services.AddTransient<AppHeader>();
            services.AddSingleton<MenuViewModel>();

            services.AddTransient<LoginView>();
            //services.AddTransient<RegisterPage>();



            services.AddTransient<DashboardViewModel>();
            services.AddTransient<DashboardView>();

            //services.AddSingleton<LoginViewModel>();
            //services.AddTransient<RegisterViewModel>();
            //services.AddTransient<DashboardViewModel>();
            //services.AddTransient<LogoutViewModel>();



            services.AddTransient<AppointmentListPage>();
            services.AddTransient<AppointmentListViewModel>();
            services.AddTransient<DoctorsDetailViewModel>();
            services.AddTransient<DoctorsListViewModel>();
            services.AddTransient<AppointmentDetailPage>();
            services.AddTransient<AppointmentDetailViewModel>();

            services.AddTransient<CalendarDashboardPage>();
            services.AddTransient<CalendarDashboardViewModel>();
            services.AddTransient<MainPage>();
            services.AddTransient<EncounterCreateViewModel>();
            services.AddTransient<EncounterDetailViewModel>();
            services.AddTransient<EncountersListViewModel>();
            services.AddTransient<EncounterEditViewModel>();

            services.AddTransient<EncounterDetailPage>();
            services.AddTransient<EncounterQuickPreviewPage>();
            services.AddTransient<EncounterListPage>();
            services.AddTransient<EncounterCreatePage>();
            services.AddTransient<EncounterEditPage>();

            services.AddTransient<MedicineDetailFormPage>();
            services.AddTransient<MedicineListPage>();

            services.AddTransient<MedicineDetailFormViewModel>();
            services.AddTransient<MedicineListViewModel>();

            services.AddTransient<PatientListPage>();
            services.AddTransient<PatientDetailFormPage>();

            services.AddTransient<PatientListViewModel>();
            services.AddTransient<PatientDetailFormViewModel>();

            services.AddTransient<Mkb10CodeDetailViewModel>();
            services.AddTransient<Mkb10CodeListViewModel>();

            services.AddTransient<Mkb10CodeListPage>();
            services.AddTransient<Mkb10CodeDetailPage>();

            services.AddTransient<TherapyDetailsViewModel>();
            services.AddTransient<TherapyDetailsPage>();


            //services.AddTransient<TherapyPlanningViewModel>();
            //services.AddTransient<TherapyPlanningPage>();

            services.AddTransient<TherapyCyclesPage>();
            services.AddTransient<TherapyCycleListViewModel>();

            services.AddTransient<PrescriptionListViewModel>();
            services.AddTransient<PrescriptionDetailFormViewModel>();
            services.AddTransient<PrescriptionDetailFormPage>();
            services.AddTransient<PrescriptionListPage>();


            services.AddTransient<ProtocolRegistryPage>();
            services.AddTransient<ProtocolRegistryViewModel>();

            // FIX: was AddSingleton — these pages/viewmodels host report history data
            // that must reflect the latest DB state on every visit. A singleton page
            // is constructed once and never rebuilt, so its ReportViewModel only ever
            // loads history data on the very first navigation to this page.
            services.AddTransient<ReportPage>();
            services.AddTransient<DashboardReportPage>();

            services.AddTransient<ReportListViewModel>();

            services.AddTransient<BackupDashboardPage>();
            services.AddTransient<BackupPage>();
            services.AddTransient<RestorePage>();
            services.AddTransient<BackupHistoryPage>();



            services.AddTransient<BackupDashboardViewModel>();
            services.AddTransient<BackupViewModel>();
            services.AddTransient<RestoreViewModel>();
            services.AddTransient<BackupHistoryViewModel>();
            services.AddTransient<BackupDetailPage>();
            services.AddTransient<BackupDetailsViewModel>();
            services.AddTransient<BackupDestinationsPage>();
            services.AddTransient<BackupDestinationViewModel>();
            ///


            services.AddTransient<UsersViewModel>();
            services.AddTransient<UsersPage>();
            services.AddTransient<UserEditViewModel>();
            services.AddTransient<UserEditPage>();


            //Admin
            services.AddTransient<AdminDashboardViewModel>();
            services.AddTransient<AdminPage>();
            ////Calndar
            //// Register ViewModels
            //services.AddSingleton<CalendarViewModel>();
            //services.AddTransient<AppointmentCreateViewModel>();
            //services.AddTransient<AppointmentEditViewModel>();

            //// Register Pages
            //services.AddSingleton<Views.CalendarPage>();
            //services.AddTransient<Views.AppointmentCreatePage>();
            //services.AddTransient<Views.AppointmentEditPage>();

            services.AddTransient<MbkImportExportViewModel>();
            services.AddTransient<MbkImportExportPage>();

            return services;
        }
    }
}