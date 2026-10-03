using EHMR.Backups.Encryption;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using System.Linq.Expressions;


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
    private readonly IDbExceptionParserProvider _exceptionParser;
    private IDbContextTransaction? _currentTransaction;
    private readonly IEncryptionService _encryptionService;

    protected TherapyTrackerDbContext(
     DbContextOptions options,
     IDbExceptionParserProvider exceptionParser,
     IEncryptionService encryptionService)
     : base(options)
    {
        _exceptionParser=exceptionParser;
        _encryptionService=encryptionService;
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
    public DbSet<PatientMedicine> PatientMedicines => Set<PatientMedicine>();
    public DbSet<ApplicationRegime> ApplicationRegimes => Set<ApplicationRegime>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TherapyCycle> TherapyCycles => Set<TherapyCycle>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<TherapyProtocol> TherapyProtocols { get; set; } = null!;
    public DbSet<UserScope> UserScopes => Set<UserScope>();
    public DbSet<Domain.Entities.Rbac.Module> UserModules => base.Set<Domain.Entities.Rbac.Module>();
    public DbSet<UserModulePermission> UserModulePermissions => Set<UserModulePermission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ReportHistory> ReportHistories => Set<ReportHistory>();
    public DbSet<Mkb10Code> Mkb10Codes => Set<Mkb10Code>();
    public DbSet<PatientScore> PatientScores => Set<PatientScore>();

    public DbSet<Sequence> Sequences => Set<Sequence>();



    public DbSet<BackupHistory> BackupHistories => Set<BackupHistory>();

    public DbSet<BackupDestination> BackupDestinations => Set<BackupDestination>();

    /// <summary>Состојба на лиценцата — најмногу еден запис.</summary>
    public DbSet<AppLicense> AppLicenses => Set<AppLicense>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch(DbUpdateException ex)
        {
            _exceptionParser?.ParseAndRaise(ex);
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePrecision(modelBuilder);
        ConfigureSoftDelete(modelBuilder);
        ConfigureMappings(modelBuilder);
        ConfigurePatient(modelBuilder);
        ConfigurePatientMedicine(modelBuilder);
        ConfigureApplicationRegime(modelBuilder);
        ConfigurePatientDocument(modelBuilder);
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
            var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    // --- 3. DYNAMIC REFLECTION MAPPING LOADER ---
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
            .Where(t => typeof(IMappingConfiguration).IsAssignableFrom(t)&&!t.IsInterface&&!t.IsAbstract);
    }

    // --- 4. EXPLICIT PATIENT SCHEMA FLUENT MAPPING ---
    private void ConfigurePatient(ModelBuilder modelBuilder)
    {
        var nationalIdConverter =
            new ValueConverter<string, string>(
                v => _encryptionService.Encrypt(v),
                v => _encryptionService.Decrypt(v));


        // ============================================
        // SEQUENCE CONFIGURATION
        // ============================================
        modelBuilder.Entity<Sequence>()
            .HasKey(x => new { x.Name, x.SequenceDate });
        modelBuilder.Entity<Sequence>()
    .Property(x => x.Name)
    .HasMaxLength(50);
        // If DateOnly isn't natively supported by your provider/EF version, add:
        modelBuilder.Entity<Sequence>()
            .Property(x => x.SequenceDate)
            .HasConversion(
                d => d.ToDateTime(TimeOnly.MinValue),
                d => DateOnly.FromDateTime(d));

        modelBuilder.Entity<Doctor>()
       .HasOne(d => d.User)
       .WithOne(u => u.Doctor)
       .HasForeignKey<Doctor>(d => d.UserId)
       .OnDelete(DeleteBehavior.Restrict);

        // ============================================
        // PATIENT CONFIGURATION
        // ============================================

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(x => x.Id);


            // Encrypted National ID
            entity.Property(x => x.NationalId)
                  .HasConversion(nationalIdConverter)
                  .HasMaxLength(500);

            entity.Property(x => x.SzboNumber)
                  .IsRequired()
                  .HasMaxLength(64);



            // ============================================
            // Personal Information
            // ============================================

            entity.Property(x => x.FirstName)
                  .IsRequired()
                  .HasMaxLength(100);


            entity.Property(x => x.LastName)
                  .IsRequired()
                  .HasMaxLength(100);


            entity.Property(x => x.BirthDate)
                  .IsRequired();


            entity.Property(x => x.Gender)
                  .HasConversion<string>()
                  .HasMaxLength(20);



            // ============================================
            // Contact Information
            // ============================================

            entity.Property(x => x.Phone)
                  .HasMaxLength(30);


            entity.Property(x => x.Email)
                  .HasMaxLength(256);


            entity.Property(x => x.Address)
                  .HasMaxLength(250);


            entity.Property(x => x.City)
                  .HasMaxLength(100);


            entity.Property(x => x.PostalCode)
                  .HasMaxLength(20);



            // ============================================
            // Emergency Contact
            // ============================================

            entity.Property(x => x.EmergencyContactName)
                  .HasMaxLength(150);


            entity.Property(x => x.EmergencyContactPhone)
                  .HasMaxLength(30);


            entity.Property(x => x.EmergencyRelationship)
                  .HasMaxLength(80);



            // ============================================
            // Medical Information
            // ============================================

            entity.Property(x => x.BloodType)
                  .HasMaxLength(10);


            entity.Property(x => x.Allergies)
                  .HasMaxLength(1000);


            entity.Property(x => x.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30);


            entity.Property(x => x.RegistrationDate)
                  .IsRequired();


            entity.Property(x => x.IsDeleted)
                  .HasDefaultValue(false);



            // ============================================
            // Indexes
            // ============================================

            // IMPORTANT:
            // Encrypted values cannot be searched normally.
            // Consider adding NationalIdHash later.
            entity.HasIndex(x => x.LastName);

            entity.HasIndex(x => x.SzboNumber)
                  .IsUnique()
                  .HasFilter("[SzboNumber] <> ''");

            entity.HasIndex(x => x.DoctorId);

            entity.HasIndex(x => x.Status);



            // ============================================
            // Relationships
            // ============================================

            entity.HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);



            entity.HasMany(x => x.Diagnoses)
                .WithOne(x => x.Patient)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);



            entity.HasMany(x => x.PatientMedicines)
                .WithOne(x => x.Patient)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);



            entity.HasMany(x => x.Documents)
                .WithOne(x => x.Patient)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);



            entity.HasMany(x => x.Prescriptions)
                .WithOne(x => x.Patient)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);



            entity.HasMany(x => x.Encounters)
                .WithOne(x => x.Patient)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);



            entity.HasMany(x => x.TherapyCycles)
                .WithOne(x => x.Patient)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.NoAction);






            // ============================================
            // Computed Properties
            // ============================================

            entity.Ignore(x => x.Age);

            entity.Ignore(x => x.FullName);

            entity.Ignore(x => x.LastVisitDate);

            entity.Ignore(x => x.NextAppointmentDate);
        });
    }
    private static void ConfigureApplicationRegime(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationRegime>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Regime).IsRequired().HasMaxLength(200);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasIndex(x => x.Regime).IsUnique();

        });
    }

    private static void ConfigurePatientMedicine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientMedicine>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.DosesFrequency).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Dosage).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.PharmaceuticalReference).HasMaxLength(200);
            entity.Property(x => x.StartDate).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(x => x.Patient)
                .WithMany(x => x.PatientMedicines)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Encounter)
                .WithMany()
                .HasForeignKey(x => x.EncounterId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.ApplicationRegime)
                .WithMany(x => x.PatientMedicines)
                .HasForeignKey(x => x.ApplicationRegimeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Medicine)
                .WithMany()
                .HasForeignKey(x => x.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.MedicineId);
            entity.HasIndex(x => x.ApplicationRegimeId);
            entity.HasIndex(x => x.EncounterId);
            entity.HasIndex(x => new { x.PatientId, x.MedicineId, x.IsActive });
        });
    }

    private static void ConfigurePatientDocument(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientDocument>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            entity.Property(x => x.StoredPath).IsRequired().HasMaxLength(500);
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.Property(x => x.IsCritical).HasDefaultValue(false);
            entity.Property(x => x.UploadedAt).IsRequired();

            entity.HasIndex(x => x.PatientId);
            entity.HasIndex(x => x.EncounterId);
            entity.HasIndex(x => x.TherapyCycleId);

            entity.HasOne(x => x.TherapyCycle)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.TherapyCycleId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Patient)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // --- 6. GLOBAL RELATIONSHIP MATRIX MAPS ---
    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // Inventory -> Medicine
        //modelBuilder.Entity<Inventory>()
        //    .HasOne(x => x.Medicine)
        //    .WithMany()
        //    .HasForeignKey(x => x.MedicineId)
        //    .OnDelete(DeleteBehavior.Restrict);

        // Appointment -> Doctor
        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Appointment -> TherapyCycle
        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.TherapyCycle)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.TherapyCycleId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Alert>()
    .HasOne(a => a.Patient)
    .WithMany() // add a `public ICollection<Alert> Alerts` on Patient if you want the reverse nav; not required
    .HasForeignKey(a => a.PatientId)
    .OnDelete(DeleteBehavior.Restrict);
        // PatientScore -> Patient / Encounter
        modelBuilder.Entity<PatientScore>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ScoreText).IsRequired().HasMaxLength(200);
            entity.Property(x => x.RecordedAt).IsRequired();

            entity.HasOne(x => x.Patient)
                .WithMany(x => x.Scores)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Encounter)
                .WithOne(x => x.PatientScore)
                .HasForeignKey<PatientScore>(x => x.EncounterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.PatientId, x.RecordedAt });
            entity.HasIndex(x => x.EncounterId).IsUnique();
        });

        // Diagnosis -> Encounter
        modelBuilder.Entity<Diagnosis>()
            .HasOne(x => x.Encounter)
            .WithMany(x => x.Diagnoses)
            .HasForeignKey(x => x.EncounterId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Encounter>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prescription Configuration (FIXED: Enum Conversion setup if needed, status standardizing)
        modelBuilder.Entity<Prescription>()
            .Property(p => p.Status)
            .HasMaxLength(50)
            .HasDefaultValue("Active");

        // UserScope -> User
        modelBuilder.Entity<UserScope>()
            .HasOne(x => x.User)
            .WithMany(x => x.Scopes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserModulePermission>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ModuleKey)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(x => x.Actions)
                .HasConversion<int>();

            entity.HasIndex(x => new { x.UserId, x.ModuleKey })
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.ModulePermissions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // --- 7. SQL INDEX OPTIMIZATIONS LAYER ---
    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>().HasIndex("FirstName", "LastName");
        modelBuilder.Entity<Appointment>().HasIndex(x => x.ScheduledStart);
      //  modelBuilder.Entity<Inventory>().HasIndex(x => x.MedicineId);
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

public interface IMappingConfiguration
{
    void ApplyConfiguration(ModelBuilder modelBuilder);
}