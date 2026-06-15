using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using EHMR.Domain.Entities;

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
    public DbSet<CycleMedicationDose> CycleMedicationDoses => Set<CycleMedicationDose>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<ScheduleMedicineRule> ScheduleMedicineRules => Set<ScheduleMedicineRule>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TherapyCycle> TherapyCycles => Set<TherapyCycle>();
    public DbSet<TherapySchedule> TherapySchedules => Set<TherapySchedule>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionMedicine> PrescriptionMedicines => Set<PrescriptionMedicine>();
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TherapyProtocol> TherapyProtocols { get; set; } = null!;
    public DbSet<UserScope> UserScopes => Set<UserScope>();
    public DbSet<UserModule> UserModules => Set<UserModule>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

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
        ConfigurePrescriptionMedicine(modelBuilder);
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
            entity.Property(x => x.PrimaryDiagnosis).HasMaxLength(300);
            entity.Property(x => x.ClinicalNotes).HasMaxLength(4000);
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Ignore(x => x.Age); // Computed property runtime evaluation ignore
        });
    }

    // --- 5. PRESCRIPTION MANY-TO-MANY BRIDGE MAPPING ---
    private static void ConfigurePrescriptionMedicine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrescriptionMedicine>(entity =>
        {
            entity.HasKey(x => new { x.PrescriptionId, x.MedicineId });
            entity.Property(x => x.Dosage).HasMaxLength(100);
            entity.Property(x => x.Frequency).HasMaxLength(100);

            entity.HasOne(x => x.Prescription)
                .WithMany(x => x.Medicines)
                .HasForeignKey(x => x.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // --- 6. GLOBAL RELATIONSHIP MATRIX MAPS ---
    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>()
            .HasMany(x => x.TreatmentPlans)
            .WithOne(x => x.Patient)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TherapySchedule>()
            .HasOne(x => x.TreatmentPlan)
            .WithMany(x => x.TherapySchedules)
            .HasForeignKey(x => x.TreatmentPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TherapySchedule>()
            .HasMany(x => x.Cycles)
            .WithOne(x => x.TherapySchedule)
            .HasForeignKey(x => x.TherapyScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Multi-Medicine Rules Relationship Config
        modelBuilder.Entity<ScheduleMedicineRule>()
            .HasOne(x => x.TherapySchedule)
            .WithMany(x => x.MedicineRules)
            .HasForeignKey(x => x.TherapyScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScheduleMedicineRule>()
            .HasOne(x => x.Medicine)
            .WithMany()
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cycle Medication Doses Config
        modelBuilder.Entity<CycleMedicationDose>()
        .HasOne(x => x.TherapyCycle)
        .WithMany(x => x.ScheduledDoses)
        .HasForeignKey(x => x.TherapyCycleId);

        modelBuilder.Entity<CycleMedicationDose>()
            .HasOne(x => x.Medicine)
            .WithMany()
            .HasForeignKey(x => x.MedicineId);

        modelBuilder.Entity<Inventory>()
            .HasOne(x => x.Medicine)
            .WithMany()
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Diagnosis>()
            .HasOne<Patient>()
            .WithMany(x => x.Diagnoses)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Encounter>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Encounters)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Encounter>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prescription>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Prescriptions)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TreatmentPlan>()
            .HasOne(p => p.TherapyProtocol)
            .WithMany(p => p.TreatmentPlans)
            .HasForeignKey(p => p.TherapyProtocolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientDocument>()
            .HasOne<Patient>()
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserScope>()
            .HasOne(x => x.User)
            .WithMany(x => x.Scopes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.InventoryId);
            entity.HasIndex(x => x.MedicineId);
            entity.HasIndex(x => x.CreatedAt);

            entity.Property(x => x.ReferenceNote)
                .HasMaxLength(500);

            entity.HasOne(x => x.Inventory)
                .WithMany()
                .HasForeignKey(x => x.InventoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // --- 7. SQL INDEX OPTIMIZATIONS LAYER ---
    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>().HasIndex("FirstName", "LastName");
        modelBuilder.Entity<Appointment>().HasIndex(x => x.ScheduledStart);
        modelBuilder.Entity<Inventory>().HasIndex(x => x.MedicineId);
        modelBuilder.Entity<Medicine>().HasIndex(x => x.Name);
        modelBuilder.Entity<Diagnosis>().HasIndex(x => x.PatientId);
        modelBuilder.Entity<TherapyCycle>().HasIndex(x => x.TherapyScheduleId);
        modelBuilder.Entity<ScheduleMedicineRule>().HasIndex(x => x.TherapyScheduleId);
        modelBuilder.Entity<CycleMedicationDose>().HasIndex(x => x.TherapyCycleId);
    }

    // --- 8. STRING-CONVERTED STATE TRACKING LAYER ---
    private static void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(x => x.Role).HasConversion<string>();
        modelBuilder.Entity<TaskItem>().Property(x => x.Priority).HasConversion<string>();
        modelBuilder.Entity<TaskItem>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<TherapyCycle>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<CycleMedicationDose>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<Appointment>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<Inventory>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<TreatmentPlan>().Property(x => x.Status).HasConversion<string>();
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