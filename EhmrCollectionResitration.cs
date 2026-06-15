using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Infrastructure.Persistence.Configs;
using EHMR.Infrastructure.Persistence.Seeders;
using EHMR.Services;
using EHMR.ViewModels;
using EHMR.ViewModels.Calendar;
using EHMR.Views;

using EHMR.Views;

using EHMR.Views.Therapies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

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
            // =========================
            services.AddDbContext<DesktopTherapyDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            // =========================
            // FACTORY (for background/threaded usage)
            // =========================
            services.AddDbContextFactory<DesktopTherapyDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
            services.AddScoped<IEntitySeeder, AlertSeeder>();
            services.AddScoped<IEntitySeeder, AppointmentSeeder>();
            services.AddScoped<IEntitySeeder, AuditLogSeeder>();
            services.AddScoped<IEntitySeeder, CycleMedicationDoseSeeder>();
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
            services.AddScoped<IEntitySeeder, InventoryTransactionSeeder>();
            services.AddScoped<IEntitySeeder, TreatmentPlanSeeder>();
            services.AddScoped<IEntitySeeder, TherapyProtocolSeeder>();
            //  services.AddScoped<IEntitySeeder, TherapyProtocolMedicineSeeder>();
            services.AddScoped<IEntitySeeder, ScheduleMedicineRuleSeeder>();

            services.AddScoped<IEntitySeeder, TherapyCycleSeeder>();

            services.AddScoped<IEntitySeeder, TherapyDoseSeeder>();
            services.AddScoped<IEntitySeeder, TherapyScheduleSeeder>();

            services.AddScoped<IEntitySeeder, PrescriptionSeeder>();
            services.AddScoped<IEntitySeeder, PrescriptionMedicineSeeder>();

            services.AddScoped<SeederRunner>();

            return services;
        }

        private static IServiceCollection RegisterApiServices(this IServiceCollection services)
        {
            // services.AddSingleton<IApiService, ApiService>();
            // services.AddSingleton<IApiClient, ApiClient>();
            services.AddSingleton<IAuthStateService, AuthStateService>();
            services.AddSingleton<IUserAdminService, UserAdminService>();
            // services.AddSingleton<IAuthService, AuthService>();
            //services.AddSingleton<IAuthStateService, AuthStateService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();
            services.AddSingleton<IAuthorizationPolicy, AuthorizationPolicy>();
            // services.AddSingleton<IAnalyticsService, AnalyticsService>();
            services.AddSingleton<IPolicyEngine, PolicyEngine>();
            // services.AddSingleton<ILicenseService, LicenseService>();
            // services.AddSingleton<IPermissionService, PermissionService>();

            services.AddSingleton<IPreferencesService, SecurePreferencesService>();
            services.AddSingleton<INavigationCoordinator, NavigationCoordinator>();
            //  services.AddSingleton<IAppointmentService, AppointmentService>();
            // services.AddSingleton<IDocumentService, DocumentService>();
            // services.AddSingleton<IPrescriptionService, PrescriptionService>();
            services.AddSingleton<INavigationCoordinator, NavigationCoordinator>();
            services.AddSingleton<IUserDialogService, UserDialogService>();
            //services.AddSingleton<ICalendarEngine, CalendarEngine>();
            //services.AddSingleton<ITimelineEngine, TimelineEngine>();

            services.AddSingleton(typeof(ISelectedItemService<>), typeof(SelectedItemService<>));
            // services.AddSingleton<INotificationService, NotificationService>();
            // services.AddSingleton<IPatientService, PatientService>();
            // services.AddSingleton<ITherapyWorkflowService, TherapyWorkflowService>();
            //  services.AddSingleton<ITherapyCycleService, TherapyCycleService>();
            //  services.AddSingleton<ITherapyScheduleService, TherapyScheduleService>();
            //  services.AddSingleton<IDashboardService, DashboardService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INavigationHost, NavigationHost>();
            services.AddSingleton<INavigationDataStore, NavigationDataStore>();
            services.AddSingleton<INavigationEvents, NavigationEvents>();
            services.AddSingleton<IMenuService, MenuService>();

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

            services.AddTransient<LoginView>();
            //services.AddTransient<RegisterPage>();
            services.AddTransient<MedicinePage>();
            services.AddTransient<PatientListPage>();
            //services.AddTransient<GetStarted>();
            services.AddTransient<PatientDetailFormPage>();

            services.AddTransient<DashboardViewModel>();
            services.AddTransient<DashboardView>();

            //services.AddSingleton<LoginViewModel>();
            //services.AddTransient<RegisterViewModel>();
            //services.AddTransient<DashboardViewModel>();
            //services.AddTransient<LogoutViewModel>();

            services.AddSingleton<MenuViewModel>();
            services.AddSingleton<ReportPage>();
            services.AddSingleton<ReportViewModel>();

            services.AddTransient<AppointmentListPage>();
            services.AddTransient<AppointmentListViewModel>();
            services.AddTransient<AppointmentDetailFormPage>();
            services.AddTransient<AppointmentDetailFormViewModel>();
            services.AddTransient<CalendarDashboardPage>();
            services.AddTransient<CalendarDashboardViewModel>();
            services.AddTransient<PlansViewModel>();
            services.AddTransient<PlanPage>();

            services.AddTransient<TherapyDetailsViewModel>();
            services.AddTransient<TherapyDetailsPage>();
            services.AddTransient<ProtocolRegistryPage>();
            services.AddTransient<ProtocolRegistryViewModel>();
            //services.AddTransient<TherapyPlanningViewModel>();
            //services.AddTransient<TherapyPlanningPage>();

            services.AddTransient<TherapyCyclesPage>();
            services.AddTransient<TherapyCyclesViewModel>();

            services.AddTransient<ScheduleMedicineRuleDetailFormPage>();

            services.AddTransient<MedicineDetailFormPage>();
            services.AddTransient<MedicineListPage>();
            services.AddTransient<MedicineDetailFormViewModel>();
            services.AddTransient<MedicineListViewModel>();

            services.AddTransient<UsersViewModel>();
            services.AddTransient<UsersPage>();
            services.AddTransient<UserEditViewModel>();
            services.AddTransient<UserEditPage>();

            services.AddTransient<ScheduleMedicineRuleDetailFormViewModel>();
            ////Calndar
            //// Register ViewModels
            //services.AddSingleton<CalendarViewModel>();
            //services.AddTransient<AppointmentCreateViewModel>();
            //services.AddTransient<AppointmentEditViewModel>();

            //// Register Pages
            //services.AddSingleton<Views.CalendarPage>();
            //services.AddTransient<Views.AppointmentCreatePage>();
            //services.AddTransient<Views.AppointmentEditPage>();

            return services;
        }
    }
}