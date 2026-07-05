using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;


namespace EHMR.Infrastructure.Persistence;

public interface ISoftDelete
{
    bool IsDeleted
    {
        get; set;
    }
}

/// <summary>
/// Core structural Unit of Work Data Context.
/// Handles centralized clinical data integrity policies, soft-deletes, and precision scaling.
/// </summary>
public abstract class TherapyTrackerDbContext : DbContext, IUnitOfWork
{
    private readonly IDbExceptionParserProvider? _exceptionParser;
    private IDbContextTransaction? _currentTransaction;

    protected TherapyTrackerDbContext(DbContextOptions options,
     IDbExceptionParserProvider? exceptionParser = null)
     : base(options)
    {
        _exceptionParser=exceptionParser;
    }

    public bool HasActiveTransaction => _currentTransaction!=null;

    public IDbContextTransaction? GetCurrentTransaction() => _currentTransaction;

    // =====================================================
    // CORE CLINICAL DATA REGISTER SCHEMAS (DbSets)
    // =====================================================
    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TherapyCycle> TherapyCycles => Set<TherapyCycle>();

    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    public DbSet<TherapyProtocol> TherapyProtocols { get; set; } = null!;
    public DbSet<UserScope> UserScopes => Set<UserScope>();
    public DbSet<Domain.Entities.Rbac.Module> UserModules => base.Set<Domain.Entities.Rbac.Module>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Mkb10Code> Mkb10Codes => Set<Mkb10Code>();
    public DbSet<AppointmentDiagnosis> AppointmentDiagnoses => Set<AppointmentDiagnosis>();

    public override async Task<int> SaveChangesAsync(
      CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch(DbUpdateException ex)
        {
            _exceptionParser?.ParseAndRaise(ex);
            throw; // preserve original stack trace
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePrecision(modelBuilder);
        ConfigureSoftDelete(modelBuilder);
        ConfigureMappings(modelBuilder);
        ConfigurePatient(modelBuilder);
        //ConfigueProtocol(modelBuilder);
        ConfigureRelationships(modelBuilder);
        ConfigureIndexes(modelBuilder);
        ConfigureEnumConversions(modelBuilder);
    }

    // --- 1. DECIMAL PRECISION PIPELINE ---
    private static void ConfigurePrecision(ModelBuilder modelBuilder)
    {
        foreach(var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType==typeof(decimal)||p.ClrType==typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(4);
        }
    }

    // --- 2. AUTOMATED GLOBAL SOFT DELETE FILTERS ---
    private static void ConfigureSoftDelete(ModelBuilder modelBuilder)
    {
        foreach(var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if(!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");

            var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));

            var filter = Expression.Lambda(
                Expression.Equal(property, Expression.Constant(false)),
                parameter);

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(filter);
        }
    }

    // --- 3. DYNAMIC REFLECTION MAPPING LOADER ---
    private static readonly Type[] _mappingConfigurations =
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .Where(t => typeof(IMappingConfiguration).IsAssignableFrom(t)&&!t.IsInterface&&!t.IsAbstract)
            .ToArray();

    private static void ConfigureMappings(ModelBuilder modelBuilder)
    {
        foreach(var type in GetMappingConfigurations())
        {
            var config = (IMappingConfiguration)Activator.CreateInstance(type)!;
            config.ApplyConfiguration(modelBuilder);
        }
    }

    private static IEnumerable<Type> GetMappingConfigurations()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(t =>
                typeof(IMappingConfiguration).IsAssignableFrom(t)&&
                !t.IsInterface&&
                !t.IsAbstract);
    }

    // --- 4. EXPLICIT PATIENT SCHEMA FLUENT MAPPING ---
    private static void ConfigurePatient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Gender).HasMaxLength(20);

            entity.Property(x => x.Status).HasConversion<string>();
            entity.Ignore(x => x.Age); // Computed property runtime evaluation ignore
        });
    }

    // --- 5. PRESCRIPTION MANY-TO-MANY BRIDGE MAPPING ---

    // --- 6. GLOBAL RELATIONSHIP MATRIX MAPS ---
    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // =====================================================
        // Inventory -> Medicine
        // =====================================================
        modelBuilder.Entity<Inventory>()
            .HasOne(x => x.Medicine)
            .WithMany()
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // =====================================================
        // Appointment -> Patient
        // =====================================================
        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        // =====================================================
        // Appointment -> Doctor
        // =====================================================
        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // =====================================================
        // Appointment -> TherapyCycle
        // =====================================================
        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.TherapyCycle)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.TherapyCycleId)
            .OnDelete(DeleteBehavior.SetNull);

        // =====================================================
        // TherapyCycle -> Patient
        // =====================================================
        modelBuilder.Entity<TherapyCycle>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.TherapyCycles)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        // =====================================================
        // Diagnosis -> Patient
        // =====================================================
        modelBuilder.Entity<Diagnosis>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Diagnoses)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Diagnosis>()
          .HasOne(x => x.Encounter)
          .WithMany(x => x.Diagnoses)
          .HasForeignKey(x => x.EncounterId)
          .OnDelete(DeleteBehavior.NoAction);
        // =====================================================
        // AppointmentDiagnosis (many-to-many bridge)
        // =====================================================
        modelBuilder.Entity<AppointmentDiagnosis>()
            .HasKey(x => new
            {
                x.AppointmentId,
                x.Mkb10CodeId
            });

        modelBuilder.Entity<AppointmentDiagnosis>()
            .HasOne(x => x.Appointment)
            .WithMany(x => x.AppointmentDiagnoses)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AppointmentDiagnosis>()
            .HasOne(x => x.Mkb10Code)
            .WithMany()
            .HasForeignKey(x => x.Mkb10CodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional Diagnosis entity relation
        modelBuilder.Entity<AppointmentDiagnosis>()
            .HasOne<Diagnosis>()
            .WithMany()
            .HasForeignKey(x => x.DiagnosisId)
            .OnDelete(DeleteBehavior.NoAction);

        // =====================================================
        // Encounter -> Patient
        // =====================================================
        modelBuilder.Entity<Encounter>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Encounters)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        // =====================================================
        // Encounter -> Doctor
        // =====================================================
        modelBuilder.Entity<Encounter>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // =====================================================
        // Prescription -> Patient
        // =====================================================
        modelBuilder.Entity<Prescription>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Prescriptions)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        // =====================================================
        // PatientDocument -> Patient
        // =====================================================
        modelBuilder.Entity<PatientDocument>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

        // =====================================================
        // UserScope -> User
        // =====================================================
        modelBuilder.Entity<UserScope>()
            .HasOne(x => x.User)
            .WithMany(x => x.Scopes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    // --- 7. SQL INDEX OPTIMIZATIONS LAYER ---
    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>().HasIndex("FirstName", "LastName");
        modelBuilder.Entity<Appointment>().HasIndex(x => x.ScheduledStart);
        modelBuilder.Entity<Inventory>().HasIndex(x => x.MedicineId);
        modelBuilder.Entity<Medicine>().HasIndex(x => x.Name);
        modelBuilder.Entity<Diagnosis>().HasIndex(x => x.PatientId);
        modelBuilder.Entity<TherapyCycle>().HasIndex(x => x.PatientId);
    }

    // --- 8. STRING-CONVERTED STATE TRACKING LAYER ---
    private static void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(x => x.Role).HasConversion<string>();
        modelBuilder.Entity<TaskItem>().Property(x => x.Priority).HasConversion<string>();
        modelBuilder.Entity<TaskItem>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<TherapyCycle>().Property(x => x.Status).HasConversion<string>();

        modelBuilder.Entity<Appointment>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<Inventory>().Property(x => x.Status).HasConversion<string>();

        modelBuilder.Entity<Alert>().Property(x => x.Level).HasConversion<string>();
    }
}

// =====================================================
// REQUIRED ARCHITECTURAL DEPENDENCY INTERFACES
// =====================================================

public interface IUnitOfWork
{
    bool HasActiveTransaction
    {
        get;
    }

    IDbContextTransaction? GetCurrentTransaction();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IDbExceptionParserProvider
{
    void ParseAndRaise(DbUpdateException exception);
}

public interface IMappingConfiguration
{
    void ApplyConfiguration(ModelBuilder modelBuilder);
}