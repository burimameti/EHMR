using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Infrastructure.Persistence;
using EHMR.Services.Dto;
using EHMR.UI.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EHMR.Services;

public class PatientService : IPatientService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;
    private readonly IAuthorizationService _authorizationService;

    public PatientService(IDbContextFactory<DesktopTherapyDbContext> factory, IAuthorizationService authorizationService)
    {
        _factory=factory;
        _authorizationService=authorizationService;
    }

    // =====================================================
    // READ
    // =====================================================

    public async Task<List<PatientDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var entities = await db.Patients
            .AsNoTracking()
            .Include(p => p.Doctor)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<List<Patient>> GetAllBaseAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Patients
            .AsNoTracking()
            .Include(p => p.Doctor)
            .Include(p => p.PatientMedicines).ThenInclude(pm => pm.Medicine)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<(List<PatientDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        if(page<1) page=1;
        if(pageSize<1) pageSize=20;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var query = db.Patients
            .AsNoTracking()
            .Include(p => p.Doctor)
            .AsQueryable();

        if(!string.IsNullOrWhiteSpace(searchTerm))
            query=ApplySearch(query, searchTerm.Trim());

        var totalCount = await query.CountAsync(ct);

        var entities = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page-1)*pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (entities.Select(MapToDto).ToList(), totalCount);
    }

    public async Task<List<PatientDto>> SearchAsync(string term, CancellationToken ct = default)
    {
        if(string.IsNullOrWhiteSpace(term))
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);

        var entities = await ApplySearch(
                db.Patients.AsNoTracking().Include(p => p.Doctor),
                term.Trim())
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(50)
            .ToListAsync(ct);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<PatientDto?> GetByIdAsync(Guid id, bool includeChildren = false, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        if(!includeChildren)
        {
            var simple = await db.Patients
                .AsNoTracking()
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(p => p.Id==id, ct);

            return simple==null ? null : MapToDto(simple);
        }

        var entity = await db.Patients
            .AsNoTracking()
            .Include(p => p.Doctor).ThenInclude(d => d.User)
            .Include(p => p.Diagnoses).ThenInclude(d => d.Mkb10Code)
            .Include(p => p.Encounters)
            .Include(p => p.PatientMedicines).ThenInclude(pm => pm.Medicine)
            .Include(p => p.PatientMedicines).ThenInclude(pm => pm.ApplicationRegime)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id==id, ct);

        return entity==null ? null : MapToDto(entity);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Patients.AsNoTracking().AnyAsync(p => p.Id==id, ct);
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Patients.CountAsync(ct);
    }

    private static IQueryable<Patient> ApplySearch(IQueryable<Patient> query, string term)
        => query.Where(p =>
            p.FirstName.Contains(term)||
            p.LastName.Contains(term)||
            (p.NationalId!=null&&p.NationalId.Contains(term))||
            p.SzboNumber.Contains(term)||
            p.Phone.Contains(term));

    // =====================================================
    // WRITE (scalar-only, no child collections)
    // =====================================================

    public async Task AddAsync(PatientEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await using var db = await _factory.CreateDbContextAsync(ct);

        var patient = new Patient { Id=dto.Id==Guid.Empty ? Guid.NewGuid() : dto.Id };

        CopyScalarFields(dto, patient);

        patient.CreatedAt=DateTime.UtcNow;
        patient.RegistrationDate=DateTime.UtcNow;

        await db.Patients.AddAsync(patient, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Updates only scalar fields on the tracked entity, so navigation properties
    /// (Doctor, Diagnoses, etc.) are never touched or re-inserted by mistake.
    /// </summary>
    public async Task UpdatePatientAsync(PatientEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await using var db = await _factory.CreateDbContextAsync(ct);

        var existing = await db.Patients.FirstOrDefaultAsync(p => p.Id==dto.Id, ct)
            ??throw new KeyNotFoundException($"Patient {dto.Id} not found.");

        CopyScalarFields(dto, existing);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Loads then removes, so a missing row returns false instead of throwing
    /// a concurrency exception that looks the same as a real conflict.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var existing = await db.Patients.FirstOrDefaultAsync(p => p.Id==id, ct);
        if(existing==null)
            return false;

        db.Patients.Remove(existing);
        await db.SaveChangesAsync(ct);

        return true;
    }

    // =====================================================
    // LOOKUP SEARCHES (used by pickers)
    // =====================================================

    public async Task<List<DoctorDto>> SearchDoctorsAsync(string term, CancellationToken ct = default)
    {
        if(string.IsNullOrWhiteSpace(term))
            return [];

        term=term.Trim();

        await using var db = await _factory.CreateDbContextAsync(ct);

        // IsActive is computed / not mapped, so filter on the mapped Status column
        // first, then match the computed FullName in memory.
        var data = await db.Doctors
            .Include(d => d.User)
            .Where(d => d.Status==Domain.Entities.Status.Active)
            .ToListAsync(ct);

        return data
            .Where(d =>
                (d.User!=null&&d.User.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase))||
                d.FullName.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(d => new DoctorDto
            {
                Id=d.Id,
                FirstName=d.User?.FirstName,
                LastName=d.User?.LastName
            })
            .ToList();
    }

    public async Task<List<Mkb10CodeDto>> SearchMkb10CodesAsync(
        string codeTerm,
        CancellationToken ct = default,
        string? section = null,
        string? descriptionTerm = null)
    {
        codeTerm=NormalizeMkbCodeTerm(codeTerm);
        descriptionTerm=(descriptionTerm??string.Empty).Trim();

        if(string.IsNullOrWhiteSpace(codeTerm)
            &&string.IsNullOrWhiteSpace(descriptionTerm)
            &&string.IsNullOrWhiteSpace(section))
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);

        var query = db.Mkb10Codes.AsQueryable();

        // The selected A-Z section is the primary scope; code/description only narrow inside it.
        if(!string.IsNullOrWhiteSpace(section))
        {
            var prefix = NormalizeMkbCodeTerm(section);
            query=query.Where(x => x.Code.StartsWith(prefix));
        }

        if(!string.IsNullOrWhiteSpace(codeTerm))
            query=query.Where(x => x.Code.Contains(codeTerm));

        if(!string.IsNullOrWhiteSpace(descriptionTerm))
        {
            var cyrillicDescription = MacedonianTransliterator.ToCyrillic(descriptionTerm);
            query=query.Where(x =>
                x.Description.Contains(descriptionTerm)
                ||x.Description.Contains(cyrillicDescription));
        }

        return await query
            .OrderBy(x => x.Code)
            .Take(20)
            .Select(x => new Mkb10CodeDto
            {
                Id=x.Id,
                Code=x.Code,
                Description=x.Description
            })
            .ToListAsync(ct);
    }

    // MKB-10 codes are Latin; accept Macedonian Cyrillic input so M06.9 and м06.9 behave the same.
    private static readonly Dictionary<char, char> CyrillicToLatin = new()
    {
        ['А']='A',
        ['Б']='B',
        ['В']='V',
        ['Г']='G',
        ['Д']='D',
        ['Е']='E',
        ['Ж']='Z',
        ['З']='Z',
        ['Ѕ']='D',
        ['И']='I',
        ['Ј']='J',
        ['К']='K',
        ['Л']='L',
        ['М']='M',
        ['Н']='N',
        ['О']='O',
        ['П']='P',
        ['Р']='R',
        ['С']='S',
        ['Т']='T',
        ['Ќ']='K',
        ['У']='U',
        ['Ф']='F',
        ['Х']='H',
        ['Ц']='C',
        ['Ч']='C',
        ['Џ']='D',
        ['Ш']='S'
    };

    private static string NormalizeMkbCodeTerm(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = value.Trim().ToUpperInvariant()
            .Select(c => CyrillicToLatin.TryGetValue(c, out var latin) ? latin : c)
            .ToArray();

        return new string(chars);
    }

    public async Task<List<MedicineDto>> SearchMedicinesAsync(string term, CancellationToken ct = default)
    {
        if(string.IsNullOrWhiteSpace(term))
            return [];

        term=term.Trim();

        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Medicines
            .Where(m =>
                m.IsActive&&
                (m.Name.Contains(term)||
                 m.GenericName.Contains(term)||
                 m.Code.Contains(term)))
            .OrderBy(m => m.Name)
            .Take(20)
            .Select(m => new MedicineDto
            {
                Id=m.Id,
                Name=m.Name,
                GenericName=m.GenericName,
                Code=m.Code,
                DosageForm=m.DosageForm,
                Strength=m.Strength,
                Unit=m.Unit,
                DefaultDosage=m.DefaultDosage,
                Manufacturer=m.Manufacturer,
                IsActive=m.IsActive
            })
            .ToListAsync(ct);
    }

    // =====================================================
    // APPLICATION REGIMES
    // =====================================================

    public async Task<List<ApplicationRegimeDto>> GetApplicationRegimesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.ApplicationRegimes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Regime)
            .Select(x => new ApplicationRegimeDto
            {
                Id=x.Id,
                Regime=x.Regime,
                IsActive=x.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<ApplicationRegimeDto> AddApplicationRegimeAsync(string regime, CancellationToken ct = default)
    {
        regime=(regime??string.Empty).Trim();
        if(string.IsNullOrWhiteSpace(regime))
            throw new ArgumentException("Режимот на апликација е задолжителен.", nameof(regime));

        await using var db = await _factory.CreateDbContextAsync(ct);

        var existing = await db.ApplicationRegimes.FirstOrDefaultAsync(x => x.Regime==regime, ct);
        if(existing!=null)
            return ToDto(existing);

        var entity = new ApplicationRegime
        {
            Id=Guid.NewGuid(),
            Regime=regime,
            IsActive=true
        };

        db.ApplicationRegimes.Add(entity);
        await db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<ApplicationRegimeDto> UpdateApplicationRegimeAsync(Guid id, string regime, CancellationToken ct = default)
    {
        regime=(regime??string.Empty).Trim();
        if(id==Guid.Empty)
            throw new ArgumentException("Невалиден режим.", nameof(id));
        if(string.IsNullOrWhiteSpace(regime))
            throw new ArgumentException("Режимот на апликација е задолжителен.", nameof(regime));

        await using var db = await _factory.CreateDbContextAsync(ct);

        var entity = await db.ApplicationRegimes.FirstOrDefaultAsync(x => x.Id==id, ct)
            ??throw new KeyNotFoundException("Режимот на апликација не постои.");

        if(await db.ApplicationRegimes.AnyAsync(x => x.Id!=id&&x.Regime==regime, ct))
            throw new InvalidOperationException("Веќе постои режим со истиот назив.");

        entity.Regime=regime;
        await db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<bool> DeleteApplicationRegimeAsync(Guid id, CancellationToken ct = default)
    {
        if(id==Guid.Empty)
            return false;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var entity = await db.ApplicationRegimes.FirstOrDefaultAsync(x => x.Id==id, ct);
        if(entity==null)
            return false;

        // Soft-delete so existing patient/encounter history keeps its application method.
        entity.IsActive=false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static ApplicationRegimeDto ToDto(ApplicationRegime e) => new()
    {
        Id=e.Id,
        Regime=e.Regime,
        IsActive=e.IsActive
    };

    // =====================================================
    // AGGREGATE SAVE (patient + diagnoses + medicines + documents)
    // =====================================================

    public async Task SavePatientAsync(PatientSaveModel model, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var patientDto = model.Patient;

        await using var db = await _factory.CreateDbContextAsync(ct);

        patientDto.SzboNumber=patientDto.SzboNumber?.Trim()??string.Empty;

        if(!string.IsNullOrWhiteSpace(patientDto.SzboNumber))
        {
            var szboExists = await db.Patients
                .AnyAsync(x => x.SzboNumber==patientDto.SzboNumber&&x.Id!=patientDto.Id, ct);
            if(szboExists)
                throw new InvalidOperationException("Веќе постои пациент со овој ЕЗБО број.");
        }

        try
        {
            if(model.IsNewPatient)
            {
                await InsertPatientAggregateAsync(db, model, ct);
                return;
            }

            await UpdatePatientAggregateAsync(db, model, ct);
        }
        catch(DbUpdateConcurrencyException ex)
        {
            // Inspect entries while db is still alive — never let EntityEntry escape this scope.
            foreach(var entry in ex.Entries)
            {
                var dbValues = await entry.GetDatabaseValuesAsync(ct);
                Debug.WriteLine($"{entry.Entity.GetType().Name} {entry.State}");
                Debug.WriteLine(dbValues==null
                    ? "  DB row is NULL -> this id does not exist in the database at all."
                    : "  DB row EXISTS -> genuine value mismatch.");
            }

            throw;
        }
    }

    private async Task InsertPatientAggregateAsync(DesktopTherapyDbContext db, PatientSaveModel model, CancellationToken ct)
    {
        var patientDto = model.Patient;

        var patient = new Patient
        {
            Id=patientDto.Id==Guid.Empty ? Guid.NewGuid() : patientDto.Id
        };
        patientDto.Id=patient.Id;

        patient.PatientNumber=await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Patient, "PAT");

        CopyScalarFields(patientDto, patient);

        patient.CreatedAt=DateTime.UtcNow;
        patient.RegistrationDate=DateTime.UtcNow;

        foreach(var vm in model.Diagnoses)
            patient.Diagnoses.Add(NewDiagnosis(patient.Id, vm));

        foreach(var vm in model.Medicines)
            patient.PatientMedicines.Add(NewPatientMedicine(patient.Id, vm));

        foreach(var vm in model.Documents)
            patient.Documents.Add(NewDocument(patient.Id, vm));

        await db.Patients.AddAsync(patient, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task UpdatePatientAggregateAsync(DesktopTherapyDbContext db, PatientSaveModel model, CancellationToken ct)
    {
        var patientDto = model.Patient;

        var existing = await db.Patients
            .Include(x => x.Diagnoses)
             .Include(x => x.PatientMedicines)
            .ThenInclude(x => x.ResolutionDocument)
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id==patientDto.Id, ct)
            ??throw new KeyNotFoundException($"Patient {patientDto.Id} not found.");

        if(existing.Status==PatientStatus.Inactive&&
            patientDto.Status==PatientStatus.Active&&
            !_authorizationService.HasRole(UserRole.Admin)&&
            !_authorizationService.HasRole(UserRole.SuperAdmin))
        {
            throw new UnauthorizedAccessException(
                "Само администратор може да реактивира неактивен пациент.");
        }

        CopyScalarFields(patientDto, existing);

        // NOTE: new children are added through db.<Set>.Add (not existing.<Nav>.Add).
        // They have client-generated Guid keys, so adding via the navigation of a tracked
        // parent makes EF mark them Modified instead of Added -> "DB row is NULL" concurrency error.

        // ---- diagnoses: delete ----
        foreach(var id in model.DeletedDiagnosisIds)
        {
            var entity = existing.Diagnoses.FirstOrDefault(x => x.Id==id);
            if(entity!=null)
                db.Remove(entity);
        }

        // ---- diagnoses: upsert (skip ids just deleted) ----
        foreach(var vm in model.Diagnoses.Where(x => !model.DeletedDiagnosisIds.Contains(x.Id)))
        {
            var entity = existing.Diagnoses.FirstOrDefault(x => x.Id==vm.Id);

            if(entity==null)
            {
                db.Diagnoses.Add(NewDiagnosis(existing.Id, vm));
                continue;
            }

            // Patient form owns this diagnosis; keep it independent of Encounter.
            entity.EncounterId=null;
            entity.Mkb10CodeId=vm.Mkb10CodeId;
            entity.DiagnosedAt=vm.DiagnosedAt;
            entity.IsPrimary=vm.IsPrimary;
            entity.Severity=vm.Severity;
            entity.ClinicalDescription=vm.ClinicalDescription;
            entity.Status=vm.Status;
        }

        // ---- medicines: close, never delete (clinical history) ----
        foreach(var id in model.DeletedMedicineIds)
        {
            var entity = existing.PatientMedicines.FirstOrDefault(x => x.Id==id);
            if(entity!=null)
            {
                entity.IsActive=false;
                entity.EndDate??=DateTime.UtcNow;
            }
        }

        // ---- medicines: upsert ----
        // MedicineId is always an existing Medicine.Id; the catalog itself is never modified here.
        foreach(var vm in model.Medicines.Where(x => !model.DeletedMedicineIds.Contains(x.Id)))
        {
            var entity = existing.PatientMedicines.FirstOrDefault(x => x.Id==vm.Id);

            if(entity==null)
            {
                db.PatientMedicines.Add(NewPatientMedicine(existing.Id, vm));
                continue;
            }

            entity.MedicineId=vm.MedicineId;
            entity.Dosage=vm.Dosage;
            entity.DosesFrequency=vm.DosesFrequency;
            entity.StartDate=vm.StartDate;
            entity.EndDate=vm.EndDate;
            entity.Notes=vm.Notes;
            entity.PharmaceuticalReference=vm.PharmaceuticalReference;
            entity.ApplicationRegimeId=vm.ApplicationRegimeId;
            entity.ResolutionDocumentId=vm.ResolutionDocumentId;
            entity.Quantity=vm.Quantity;
            entity.IsActive=vm.IsActive;
        }

        // ---- documents: delete ----
        foreach(var id in model.DeletedDocumentIds)
        {
            var entity = existing.Documents.FirstOrDefault(x => x.Id==id);
            if(entity!=null)
                db.Remove(entity);
        }

        // ---- documents: insert only (immutable once uploaded) ----
        foreach(var vm in model.Documents.Where(x => x.Id==Guid.Empty))
            db.PatientDocuments.Add(NewDocument(existing.Id, vm));

        await db.SaveChangesAsync(ct);
    }

    // ---- save-model -> new entity ----

    private static Diagnosis NewDiagnosis(Guid patientId, DiagnosisSaveModel vm) => new()
    {
        Id=Guid.NewGuid(),
        PatientId=patientId,
        // Patient-level diagnosis: never bind it to an Encounter.
        EncounterId=null,
        Mkb10CodeId=vm.Mkb10CodeId,
        DiagnosedAt=vm.DiagnosedAt,
        IsPrimary=vm.IsPrimary,
        Severity=vm.Severity,
        ClinicalDescription=vm.ClinicalDescription,
        Status=vm.Status
    };

    private static PatientMedicine NewPatientMedicine(Guid patientId, PatientMedicineSaveModel vm) => new()
    {
        Id=Guid.NewGuid(),
        PatientId=patientId,
        MedicineId=vm.MedicineId,
        Dosage=vm.Dosage,
        DosesFrequency=vm.DosesFrequency,
        StartDate=vm.StartDate,
        EndDate=vm.EndDate,
        Notes=vm.Notes,
        PharmaceuticalReference=vm.PharmaceuticalReference,
        ApplicationRegimeId=vm.ApplicationRegimeId,
        ResolutionDocumentId=vm.ResolutionDocumentId,
        Quantity=vm.Quantity,
        IsActive=vm.IsActive
    };

    private static PatientDocument NewDocument(Guid patientId, PatientDocumentSaveModel vm) => new()
    {
        Id=vm.Id==Guid.Empty ? Guid.NewGuid() : vm.Id,
        PatientId=patientId,
        DocumentType=vm.DocumentType,
        Title=vm.Title,
        Description=vm.Description,
        FileName=vm.FileName,
        StoredPath=vm.StoredPath,
        ContentType=vm.ContentType,
        UploadedAt=vm.UploadedAt
    };

    // =====================================================
    // MAPPING: entity -> dto
    // =====================================================

    private static PatientDto MapToDto(Patient p)
    {
        var now = DateTime.UtcNow;
        var age = now.Year-p.BirthDate.Year;
        if(p.BirthDate.Date>now.AddYears(-age)) age--;

        return new PatientDto
        {
            Id=p.Id,
            FirstName=p.FirstName,
            LastName=p.LastName,
            NationalId=p.NationalId,
            SzboNumber=p.SzboNumber,
            BirthDate=p.BirthDate,
            Gender=p.Gender,
            DoctorId=p.DoctorId,
            DoctorDisplay=p.Doctor!=null ? $"Д-р {p.Doctor.FullName}" : "",
            Phone=p.Phone,
            Email=p.Email,
            Address=p.Address,
            City=p.City,
            PostalCode=p.PostalCode,
            EmergencyContactName=p.EmergencyContactName,
            EmergencyContactPhone=p.EmergencyContactPhone,
            EmergencyRelationship=p.EmergencyRelationship,
            BloodType=p.BloodType,
            Allergies=p.Allergies,
            Status=p.Status,
            InactiveReason=p.InactiveReason,
            RegistrationDate=p.RegistrationDate,
            Age=age,
            LastVisitDate=p.Encounters?
                .Where(e => e.EncounterDate<=now)
                .OrderByDescending(e => e.EncounterDate)
                .Select(e => (DateTime?)e.EncounterDate)
                .FirstOrDefault(),
            NextAppointmentDate=null, // wire up once Appointment navigation/include is available
            Diagnoses=p.Diagnoses?.Select(MapDiagnosis).ToList()?? [],
            Medicines=p.PatientMedicines?.Select(MapMedicine).ToList()?? [],
            Documents=p.Documents?.Select(MapDocument).ToList()?? []
        };
    }

    private static DiagnosisDto MapDiagnosis(Diagnosis d) => new()
    {
        Id=d.Id,
        PatientId=d.PatientId,
        EncounterId=d.EncounterId,
        Mkb10CodeId=d.Mkb10CodeId,
        Mkb10Code=d.Mkb10Code?.Code??"",
        Mkb10Description=d.Mkb10Code?.Description??"",
        DiagnosedAt=d.DiagnosedAt,
        IsPrimary=d.IsPrimary,
        Severity=d.Severity,
        ClinicalDescription=d.ClinicalDescription,
        Status=d.Status
    };

    private static PatientMedicineDto MapMedicine(PatientMedicine pm) => new()
    {
        Id=pm.Id,
        PatientId=pm.PatientId,
        EncounterId=pm.EncounterId,
        MedicineId=pm.MedicineId,
        MedicineName=pm.Medicine?.Name??"",
        ApplicationRegimeId=pm.ApplicationRegimeId,
        ResolutionDocumentId=pm.ResolutionDocumentId,
        ApplicationRegime=pm.ApplicationRegime?.Regime??"",
        GenericName=pm.Medicine?.GenericName??"",
        Code=pm.Medicine?.Code??"",
        DosageForm=pm.Medicine?.DosageForm??"",
        Strength=pm.Medicine?.Strength??0,
        Unit=pm.Medicine?.Unit??"",
        DefaultDosage=pm.Medicine?.DefaultDosage??"",
        Manufacturer=pm.Medicine?.Manufacturer??"",
        DosesFrequency=pm.DosesFrequency,
        Dosage=pm.Dosage,
        Quantity=pm.Quantity,
        StartDate=pm.StartDate,
        EndDate=pm.EndDate,
        Notes=pm.Notes,
        IsActive=pm.IsActive
    };

    private static PatientDocumentDto MapDocument(PatientDocument doc) => new()
    {
        Id=doc.Id,
        PatientId=doc.PatientId,
        EncounterId=doc.EncounterId,
        TherapyCycleId=doc.TherapyCycleId,
        DocumentType=doc.DocumentType,
        Title=doc.Title,
        Description=doc.Description,
        FileName=doc.FileName,
        StoredPath=doc.StoredPath,
        ContentType=doc.ContentType,
        FileSize=doc.FileSize,
        IsCritical=doc.IsCritical,
        UploadedAt=doc.UploadedAt
    };

    // =====================================================
    // MAPPING: dto -> entity (scalar fields only)
    // =====================================================

    public static void CopyScalarFields(PatientEditDto source, Patient target)
    {
        target.FirstName=source.FirstName;
        target.LastName=source.LastName;
        target.NationalId=source.NationalId;
        target.SzboNumber=source.SzboNumber.Trim();
        target.DoctorId=source.DoctorId;
        target.BirthDate=source.BirthDate;
        target.Gender=source.Gender;
        target.Phone=source.Phone;
        target.Email=source.Email;
        target.Address=source.Address;
        target.City=source.City;
        target.PostalCode=source.PostalCode;
        target.Status=source.Status;
        target.InactiveReason=source.Status==PatientStatus.Inactive
            ? source.InactiveReason.Trim()
            : string.Empty;
        target.BloodType=source.BloodType;
        target.Allergies=source.Allergies;
        target.EmergencyContactName=source.EmergencyContactName;
        target.EmergencyContactPhone=source.EmergencyContactPhone;
        target.EmergencyRelationship=source.EmergencyRelationship;
    }

    // =====================================================
    // SAVE MODELS
    // =====================================================

    public class PatientScoreSaveModel
    {
        public Guid Id
        {
            get; set;
        }
        public string Description { get; set; } = "";
        public string Number { get; set; } = "";
        public DateTime RecordedAt
        {
            get; set;
        }
    }

    public sealed class PatientSaveModel
    {
        public required PatientEditDto Patient
        {
            get; init;
        }
        public bool IsNewPatient
        {
            get; init;
        }

        public List<DiagnosisSaveModel> Diagnoses { get; init; } = [];
        public List<PatientMedicineSaveModel> Medicines { get; init; } = [];
        public List<PatientDocumentSaveModel> Documents { get; init; } = [];
        public List<PatientScoreSaveModel> Scores { get; init; } = [];

        public List<Guid> DeletedDiagnosisIds { get; init; } = [];
        public List<Guid> DeletedMedicineIds { get; init; } = [];
        public List<Guid> DeletedDocumentIds { get; init; } = [];
        public List<Guid> DeletedScoreIds { get; init; } = [];
    }
}
