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
using Microsoft.Extensions.Options;
using System.Reflection;
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

        private static IServiceCollection RegisterDb(this IServiceCollection services)
        {
            const string connectionString =
                "Server=.\\SQLEXPRESS;Database=TherapyTrackerDesktopPoc;User Id=t24test;Password=t24test;MultipleActiveResultSets=True;TrustServerCertificate=True;";

            // =========================
            // EF CORE CONTEXT (Scoped)
            //// =========================
            //services.AddDbContext<DesktopTherapyDbContext>(options =>
            //{
            //    options.UseSqlServer(connectionString);
            //});

            // =========================
            // FACTORY (for background/threaded usage)

            services.AddSingleton<IDbExceptionParserProvider,
                              DbExceptionParserProvider>();
            // =========================
            services.AddDbContextFactory<DesktopTherapyDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
            services.AddScoped<IEntitySeeder, AlertSeeder>();
            services.AddScoped<IEntitySeeder, AppointmentSeeder>();
            services.AddScoped<IEntitySeeder, AuditLogSeeder>();

            services.AddScoped<IEntitySeeder, DoctorSeeder>();
            services.AddScoped<IEntitySeeder, DiagnosisSeeder>();
            services.AddScoped<IEntitySeeder, DocumentSeeder>();
            services.AddScoped<IEntitySeeder, EncounterSeeder>();

            services.AddScoped<IEntitySeeder, UserSeeder>();
            services.AddScoped<IEntitySeeder, UserScopeSeeder>();

            services.AddScoped<IEntitySeeder, PatientSeeder>();

            services.AddScoped<IEntitySeeder, MedicineSeeder>();
            services.AddScoped<IEntitySeeder, NotificationSeeder>();
            services.AddScoped<IEntitySeeder, InventorySeeder>();
            services.AddScoped<IEntitySeeder, PatientMedicineSeeder>();
            services.AddScoped<IEntitySeeder, TherapyProtocolSeeder>();
            //  services.AddScoped<IEntitySeeder, TherapyProtocolMedicineSeeder>();

            services.AddScoped<IEntitySeeder, TherapyCycleSeeder>();

            services.AddScoped<IEntitySeeder, PrescriptionSeeder>();
            services.AddSingleton<IReportProvider, MissedTherapiesReportProvider>();

           services.AddSingleton<IReportProvider, PatientsReportProvider>();

            services.AddSingleton<IReportProvider, AppointmentStatusesReportProvider>();

            services.AddSingleton<IReportProvider, AuditReportProvider>();

            //   
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddSingleton<IBackupSecurityProvider, BackupSecurityProvider>();

            services.AddSingleton<IDatabaseBackupProvider, SqlServerBackupProvider>();
            services.AddSingleton<IDatabaseProviderResolver, DatabaseProviderResolver>();
            services.AddSingleton<IBackupStorageProvider>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<BackupOptions>>().Value;
                var dest = opts.Destinations.FirstOrDefault(d => d.Key=="local");
                if(dest is null)
                    throw new InvalidOperationException(
                        $"No backup destination with Key='local' found. Found: {string.Join(", ", opts.Destinations.Select(d => d.Key))}");

                return new LocalStorageProvider(dest);
            });
            // register CloudStorageProvider similarly if/when you use it

            services.AddSingleton<IStorageProviderResolver, StorageProviderResolver>();

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
            //services.AddSingleton<IBackupScheduler, BackupScheduler>();
            services.AddSingleton<ICompressionService, CompressionService>();
            // services.AddSingleton<IAnalyticsService, AnalyticsService>();
            // services.AddSingleton<IPolicyEngine, PolicyEngine>();
            // services.AddSingleton<ILicenseService, LicenseService>();
            // services.AddSingleton<IPermissionService, PermissionService>();
            services.AddSingleton<IAppointmentSearchQueryHandler, AppointmentSearchQueryHandler>();
            services.AddSingleton<IPreferencesService, SecurePreferencesService>();
            services.AddSingleton<INavigationCoordinator, NavigationCoordinator>();
            //  services.AddSingleton<IAppointmentService, AppointmentService>();
            // services.AddSingleton<IDocumentService, DocumentService>();
            // services.AddSingleton<IPrescriptionService, PrescriptionService>();
            services.AddSingleton<INavigationCoordinator, NavigationCoordinator>();
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
            services.AddScoped<IBackupHistoryRepository, BackupHistoryRepository>();
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
            services.AddTransient<PatientListViewModel>();
            //services.AddTransient<PatientDetailsViewModel>();
            services.AddTransient<MedicineListViewModel>();
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