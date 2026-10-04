using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Domain.Search;
using EHMR.Helpers;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using EHMR.Services;
using EHMR.ViewModels.Patients.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EHMR.ViewModels.Reports;

public partial class ReportListViewModel : BaseViewModel<GenericReportRow>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAppointmentSearchQueryHandler _autocomplete;
    private readonly IReportExportService _reportExportService;

    [ObservableProperty] private string patientSearchText = string.Empty;

    [ObservableProperty] private string selectedPatientStatus = "Сите";
    [ObservableProperty] private string selectedPatientCity = "Сите";
    [ObservableProperty] private string selectedRheumatologist = "Сите";
    [ObservableProperty] private string selectedDiagnosis = "Сите";
    [ObservableProperty] private string selectedMedicine = "Сите";
    [ObservableProperty] private string selectedGender = "Сите";
    [ObservableProperty] private string selectedScore = "Сите";
    [ObservableProperty] private ObservableCollection<SearchSuggestionDto> patientSuggestions = new();
    [ObservableProperty] private SearchSuggestionDto? selectedPatientSuggestion;
    [ObservableProperty] private bool showPatientSuggestions;
    [ObservableProperty] private bool isPatientHistoryMode;
    [ObservableProperty] private bool isScoreHistoryMode;
    [ObservableProperty] private string selectedPatientLabel = string.Empty;

    [ObservableProperty] private bool isMedicineFilterEnabled;
    [ObservableProperty] private ObservableCollection<MedicineFilterOption> medicineFilterOptions = new();
    [ObservableProperty] private MedicineFilterOption? selectedMedicineFilter;

    public bool IsMedicineConsumptionSelected => SelectedReportType.Type==ReportType.MedicineConsumption;

    private bool _isSelectingPatientSuggestion;
    private string? _selectedPatientId;

    private bool _suppressSearchTextSideEffects;

    [ObservableProperty] private DateTime startDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime endDate = DateTime.Today;

    [ObservableProperty] private string col1Header = "ИМЕ И ПРЕЗИМЕ";
    [ObservableProperty] private string col2Header = "ПРОТОКОЛ / ТЕРАПИЈА";
    [ObservableProperty] private string col3Header = "ЦИКЛУС";
    [ObservableProperty] private string col4Header = "ДАТУМ";
    [ObservableProperty] private string col5Header = "МЕДИЦИНСКО ОБРАЗЛОЖЕНИЕ";

    [ObservableProperty] private string metric1Title = "Вкупно Протоколи";
    [ObservableProperty] private int metric1Value;
    [ObservableProperty] private string metric2Title = "Бараат внимание";
    [ObservableProperty] private int metric2Value;
    [ObservableProperty] private string metric3Title = "Стапка на Конзистентност";
    [ObservableProperty] private string metric3ValueText = "100%";

    protected override string ModuleName => Modules.Reports;

    public IReadOnlyList<ReportTypeOption> ReportTypes
    {
        get;
    } =
    [
        new() { Type = ReportType.MissedTherapies,      Label = "Пропуштени терапии" },
        new() { Type = ReportType.Auditing,              Label = "Аудит" },
        new() { Type = ReportType.AppointmentStatuses,   Label = "Статус на термини" },
        new() { Type = ReportType.Patients,               Label = "Пациенти" },
        new() { Type = ReportType.MedicineConsumption,   Label = "Потрошувачка по лек" }
    ];

    private ReportTypeOption _selectedReportType;

    public ReportTypeOption SelectedReportType
    {
        get => _selectedReportType;
        set
        {
            if(!SetProperty(ref _selectedReportType, value)) return;
            ApplyColumnLayout(value.Type);
            OnPropertyChanged(nameof(IsMedicineConsumptionSelected));
            if(value.Type!=ReportType.MedicineConsumption)
            {
                IsMedicineFilterEnabled=false;
                SelectedMedicineFilter=null;
            }
            else
            {
                _=LoadMedicineFilterOptionsAsync();
            }
            // Rebuild report pickers after the report data is loaded.
            // Patient picker values come from AllItems, so rebuilding them here
            // would happen too early and produce empty picker lists.
            BuildSparkGridColumns();
            _=GenerateReportAsync();
        }
    }

    private ReportStatusOption _statusFilter;

    public ReportStatusOption StatusFilter
    {
        get => _statusFilter;
        set
        {
            if(!SetProperty(ref _statusFilter, value)) return;
            SyncSparkPickersFromFilters();
            ApplyPipeline();
        }
    }
    private readonly INavigationService _navigationService;

    public ReportListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IAuthorizationService authService,
        ISelectedItemService<GenericReportRow> selectedItemService,
        IAppointmentSearchQueryHandler autocomplete,
        IReportExportService reportExportService)
        : base(navigationService, userDialogService, menuService, authService, selectedItemService)
    {
        _dbFactory=dbFactory;
        _autocomplete=autocomplete;
        _reportExportService=reportExportService;
        _selectedReportType=ReportTypes.First(x => x.Type==ReportType.MissedTherapies);
        _statusFilter=new ReportStatusOption { Label="ИТНО / СИТЕ" };
        _navigationService=navigationService;

        PageSize=10;

        PropertyChanged+=OnViewModelPropertyChanged;

        ApplyColumnLayout(SelectedReportType.Type);
        EvaluatePermissions();
        InitializeSparkControls();
    }

    partial void OnIsMedicineFilterEnabledChanged(bool value)
    {
        if(SelectedReportType.Type!=ReportType.MedicineConsumption)
            return;

        if(!value)
        {
            SelectedMedicineFilter=null;
            _=GenerateReportAsync();
            return;
        }

        _=LoadMedicineFilterOptionsAsync();
    }

    partial void OnSelectedMedicineFilterChanged(MedicineFilterOption? value)
    {
        if(SelectedReportType.Type==ReportType.MedicineConsumption && IsMedicineFilterEnabled)
            _=GenerateReportAsync();
    }

    private async Task LoadMedicineFilterOptionsAsync()
    {
        if(SelectedReportType.Type!=ReportType.MedicineConsumption)
            return;

        await using var db=await _dbFactory.CreateDbContextAsync();
        var medicines=await db.Medicines
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ThenBy(m => m.Strength)
            .ToListAsync();

        var currentId=SelectedMedicineFilter?.Id;
        MedicineFilterOptions=new ObservableCollection<MedicineFilterOption>(
            medicines.Select(m => new MedicineFilterOption(m.Id, m.FullName)));

        if(currentId.HasValue)
            SelectedMedicineFilter=MedicineFilterOptions.FirstOrDefault(x => x.Id==currentId.Value);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName!=nameof(SearchText)) return;
        if(_suppressSearchTextSideEffects) return;

        ApplyPipeline();
    }

    // ============================================================
    // PATIENT HISTORY SEARCH
    // Same suggestion interaction used by the Dashboard patient explorer.
    // Selecting a patient scopes the report to that patient's history.
    // ============================================================

    partial void OnPatientSearchTextChanged(string value)
    {
        if(_isSelectingPatientSuggestion)
            return;

        var term=value?.Trim()??string.Empty;

        if(string.IsNullOrWhiteSpace(term))
        {
            ClearPatientSelection();
            return;
        }

        _=SearchPatientsAsync(term);
    }

    partial void OnSelectedPatientSuggestionChanged(SearchSuggestionDto? value)
    {
        if(value is null)
            return;

        _isSelectingPatientSuggestion=true;
        PatientSearchText=value.DisplayText;
        _isSelectingPatientSuggestion=false;

        _selectedPatientId=value.Id;
        SelectedPatientLabel=value.DisplayText;
        IsPatientHistoryMode=true;
        IsScoreHistoryMode=false;
        BuildSparkGridColumns();

        PatientSuggestions.Clear();
        ShowPatientSuggestions=false;

        _=GenerateReportAsync();
    }

    private async Task SearchPatientsAsync(string term)
    {
        var cyrillicTerm=Helpers.MacedonianTransliterator.ToCyrillic(term);

        var result=await DebouncedSuggestionSearchAsync(
            term,
            async (q, token) =>
            {
                var primary=await _autocomplete.Handle(
                    new AppointmentSearchQuery(q, new[] { SearchEntityType.Patient }, 8),
                    token);

                if(cyrillicTerm!=q)
                {
                    var secondary=await _autocomplete.Handle(
                        new AppointmentSearchQuery(cyrillicTerm, new[] { SearchEntityType.Patient }, 8),
                        token);

                    primary=primary
                        .Concat(secondary)
                        .GroupBy(x => x.Id)
                        .Select(x => x.First())
                        .Take(8)
                        .ToList();
                }

                return primary;
            });

        if(result is null)
            return;

        PatientSuggestions=new ObservableCollection<SearchSuggestionDto>(result);
        ShowPatientSuggestions=PatientSuggestions.Count>0;
    }

    [RelayCommand]
    private void ClearPatientHistory()
    {
        ClearPatientSelection();
        _=GenerateReportAsync();
    }

    [RelayCommand]
    private async Task GeneratePatientHistoryReportAsync()
    {
        if(!SelectedPatientGuid.HasValue || IsBusy)
            return;

        // Export exactly the patient-scoped data currently represented by the report grid.
        await ExportToPdfAsync();
    }

    [RelayCommand]
    private async Task NewReportAsync()
    {
        if(!CanView)
            return;

        await _navigationService.GoToAsync(AppRoutes.Reports.Detail);
    }

    private void ClearPatientSelection()
    {
        _selectedPatientId=null;
        IsPatientHistoryMode=false;
        IsScoreHistoryMode=false;
        SelectedPatientLabel=string.Empty;
        BuildSparkGridColumns();
        PatientSuggestions.Clear();
        ShowPatientSuggestions=false;
        SelectedPatientSuggestion=null;

        if(!string.IsNullOrEmpty(PatientSearchText))
        {
            _isSelectingPatientSuggestion=true;
            PatientSearchText=string.Empty;
            _isSelectingPatientSuggestion=false;
        }
    }

    private Guid? SelectedPatientGuid =>
        Guid.TryParse(_selectedPatientId, out var id) ? id : null;

    // ============================================================
    // COLUMN / METRIC LAYOUT PER REPORT TYPE
    // ============================================================
    private void ApplyColumnLayout(ReportType type)
    {
        switch(type)
        {
            case ReportType.MissedTherapies:
                Col1Header="ПАЦИЕНТ(Име/Презиме)"; Col2Header="ПРОТОКОЛ"; Col3Header="ЦИКЛУС"; Col4Header="ИСТЕЧЕН РОК"; Col5Header="ОБРАЗЛОЖЕНИЕ";
                Metric1Title="Пропуштени Протоколи"; Metric2Title="Неразјаснети"; Metric3Title="Легитимирана Доследност";
                break;

            case ReportType.Auditing:
                Col1Header="КОРИСНИК"; Col2Header="АКЦИЈА / НАСТАН"; Col3Header="МОДУЛ"; Col4Header="ВРЕМЕ"; Col5Header="ДЕТАЛИ ОД АУДИТ ПАТЕКА";
                Metric1Title="Вкупно активности"; Metric2Title="Безбедносни Критични"; Metric3Title="Системски Статус";
                break;

            case ReportType.AppointmentStatuses:
                Col1Header="ИМЕ И ПРЕЗИМЕ"; Col2Header="РЕУМАТОЛОГ"; Col3Header="СТАТУС"; Col4Header="ТЕРМИН"; Col5Header="ЗАБЕЛЕШКА ОД ПРЕГЛЕД";
                Metric1Title="Закажани Прегледи"; Metric2Title="Откажани Термини"; Metric3Title="Ефикасност на Сали";
                break;

            case ReportType.Patients:
                Col1Header="ПАЦИЕНТ"; Col2Header="ЕЗБО"; Col3Header="ПОЛ"; Col4Header="ТЕЛЕФОН"; Col5Header="ПОСЛ. СКОР";
                Metric1Title="Пациенти"; Metric2Title="Активни"; Metric3Title="Со внесен скор";
                break;

            case ReportType.MedicineConsumption:
                Col1Header="ЛЕК"; Col2Header="ВКУПНА ПОТРОШЕНА КОЛИЧИНА"; Col3Header="ПАЦИЕНТИ"; Col4Header="МАКЕДОНСКА КЛАСИФИКАЦИЈА НА БОЛЕСТИ (МКБ-10)"; Col5Header="ПЕРИОД";
                Metric1Title="Вкупно потрошена количина"; Metric2Title="Различни лекови"; Metric3Title="Пациенти";
                break;
        }
    }

    // ============================================================
    // DATA LOAD — one real EF Core query per report type
    [RelayCommand]
    public async Task GenerateReportAsync()
    {
        if(!CanView || IsBusy) return;
        try
        {
            IsBusy=true;
            ClearError();

            System.Diagnostics.Debug.WriteLine($"\n[ReportListViewModel] ========== GENERATE REPORT ==========");
            System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Report type: {SelectedReportType.Type}");

            var startRange = StartDate.Date;
            var endRange = EndDate.Date.AddDays(1).AddTicks(-1);

            System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Date range: {startRange:dd.MM.yyyy} to {endRange:dd.MM.yyyy}");

            await using var db = await _dbFactory.CreateDbContextAsync();
            List<GenericReportRow> rows;

            try
            {
                switch(SelectedReportType.Type)
                {
                    case ReportType.MissedTherapies:
                        System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Loading missed therapies...");
                        rows=await LoadMissedTherapiesAsync(db, startRange, endRange, SelectedPatientGuid);
                        System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Loaded {rows.Count} missed therapies");
                        break;

                    case ReportType.Auditing:
                        System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Loading audit logs...");
                        rows=await LoadAuditingAsync(db, startRange, endRange, SelectedPatientGuid);
                        System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Loaded {rows.Count} audit logs");
                        break;

                    case ReportType.AppointmentStatuses:
                        System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Loading appointment statuses...");
                        rows=await LoadAppointmentStatusesAsync(db, startRange, endRange, SelectedPatientGuid);
                        System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Loaded {rows.Count} appointments");
                        break;

                    case ReportType.Patients:
                        System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Loading patients...");
                        rows=SelectedPatientGuid.HasValue
                            ? await LoadPatientHistoryAsync(db, SelectedPatientGuid.Value)
                            : await LoadPatientsAsync(db, startRange, endRange, null);
                        System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Loaded {rows.Count} patients");
                        break;

                    case ReportType.MedicineConsumption:
                        System.Diagnostics.Debug.WriteLine("[ReportListViewModel] Loading medicine consumption...");
                        rows=await LoadMedicineConsumptionAsync(
                            db,
                            startRange,
                            endRange,
                            SelectedPatientGuid,
                            IsMedicineFilterEnabled ? SelectedMedicineFilter?.Id : null);
                        System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Loaded {rows.Count} medicine consumption rows");
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Unknown report type!");
                        rows= [];
                        break;
                }
            }
            catch(Exception dbEx)
            {
                System.Diagnostics.Debug.WriteLine($"\n❌ [ReportListViewModel] Database query error!");
                System.Diagnostics.Debug.WriteLine($"❌ Exception: {dbEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"❌ Message: {dbEx.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {dbEx.StackTrace}");
                throw;
            }

           
            // DO NOT directly assign to GridRows - it expects ObservableCollection<SparkGridRow>
            System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Setting AllItems and applying pipeline...");
            AllItems=rows;

            // Picker options are data-driven for the Patients report.
            // Build them only after AllItems has been populated so the
            // picker Items collections contain the actual report values.
            BuildSparkPickers();
            BuildSparkGridColumns();

            System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Applying pipeline...");
            ApplyPipeline();

            System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] Recomputing metrics...");
            RecomputeMetrics();

            System.Diagnostics.Debug.WriteLine($"[ReportListViewModel] ========== GENERATE REPORT COMPLETE ==========\n");
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"\n❌ [ReportListViewModel] EXCEPTION in GenerateReportAsync!");
            System.Diagnostics.Debug.WriteLine($"❌ Type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"❌ Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");

            OnError($"Грешка при генерирање извештај: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }


    private static async Task<List<GenericReportRow>> LoadMissedTherapiesAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange, Guid? patientId)
    {
        var data = await db.TherapyCycles
            .Include(p => p.Patient)
            .AsNoTracking()
            .Where(c => c.Status==TherapyStatus.Missed
                        &&c.StartDate>=startRange&&c.EndDate<=endRange
                        &&(!patientId.HasValue||c.PatientId==patientId.Value))
            .OrderByDescending(c => c.EndDate)
            .ToListAsync();

        return data.Select(c => new GenericReportRow
        {
            PrimaryHeader=c.Patient.LastName,
            SecondaryHeader=c.Patient.FirstName,
            HighlightValue=$"Ц-#{c.TherapyCyleNumber}",
            DateValue=c.StartDate?.ToString("dd.MM.yyyy"),
            InformationalText=string.IsNullOrEmpty(c.Notes) ? "Нема внесено причина од реуматолог!" : c.Notes,
            IsAlertSeverity=string.IsNullOrEmpty(c.Notes)
        }).ToList();
    }

    private static async Task<List<GenericReportRow>> LoadAuditingAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange, Guid? patientId)
    {
        var data = await db.AuditLogs
            .AsNoTracking()
            .Where(a => a.Timestamp>=startRange&&a.Timestamp<=endRange)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        return data.Select(a => new GenericReportRow
        {
            PrimaryHeader=a.UserId.ToString(),
            SecondaryHeader=a.Action,
            HighlightValue=a.AfterValue,
            DateValue=a.Timestamp.ToString("dd.MM.yyyy"),
            InformationalText=a.Description,
            IsAlertSeverity=a.AfterValue!=a.BeforeValue
        }).ToList();
    }

    private static async Task<List<GenericReportRow>> LoadAppointmentStatusesAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange, Guid? patientId)
    {
        var data = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .AsNoTracking()
            .Where(a => a.ScheduledStart>=startRange&&a.ScheduledStart<=endRange
                        &&(!patientId.HasValue||a.PatientId==patientId.Value))
            .OrderByDescending(a => a.ScheduledStart)
            .ToListAsync();

        return data.Select(a => new GenericReportRow
        {
            PrimaryHeader=a.Patient?.FullName??"",
            SecondaryHeader=a.Doctor?.FullName??"",
            HighlightValue=StatusLabel(a.Status),
            DateValue=a.ScheduledStart.ToString("dd.MM.yyyy"),
            InformationalText=a.ReasonForVisit??"",
            IsAlertSeverity=a.Status==AppointmentStatus.Cancelled
        }).ToList();
    }

    private async Task<List<GenericReportRow>> LoadPatientHistoryAsync(
        DesktopTherapyDbContext db, Guid patientId)
    {
        // Current encounter + maximum five previous encounters.
        var encounters=await db.Encounters
            .AsNoTracking()
            .Include(e => e.Doctor).ThenInclude(d => d.User)
            .Include(e => e.Diagnoses).ThenInclude(d => d.Mkb10Code)
            .Where(e => e.PatientId==patientId)
            .OrderByDescending(e => e.ScheduledStart ?? e.EncounterDate)
            .Take(6)
            .ToListAsync();

        if(encounters.Count==0)
            return [];

        var encounterIds=encounters.Select(e => e.Id).ToList();

        var scores=await db.PatientScores
            .AsNoTracking()
            .Where(s => s.PatientId==patientId && encounterIds.Contains(s.EncounterId))
            .OrderByDescending(s => s.RecordedAt)
            .ToListAsync();

        var medicines=await db.PatientMedicines
            .AsNoTracking()
            .Include(pm => pm.Medicine)
            .Include(pm => pm.ApplicationRegime)
            .Where(pm => pm.PatientId==patientId &&
                         ((pm.EncounterId.HasValue && encounterIds.Contains(pm.EncounterId.Value)) ||
                          (!pm.EncounterId.HasValue && pm.IsActive)))
            .OrderByDescending(pm => pm.StartDate)
            .ToListAsync();

        var rows=new List<GenericReportRow>(encounters.Count);

        foreach(var encounter in encounters)
        {
            var score=scores
                .Where(s => s.EncounterId==encounter.Id)
                .OrderByDescending(s => s.RecordedAt)
                .FirstOrDefault();

            var diagnoses=encounter.Diagnoses
                .Where(d => d.Mkb10Code is not null)
                .OrderByDescending(d => d.IsPrimary)
                .ThenBy(d => d.Mkb10Code!.Code)
                .Select(d => $"{d.Mkb10Code!.Code} - {d.Mkb10Code.Description}")
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            // Maximum five real medicines per encounter. For the latest encounter,
            // active patient-level medicines without EncounterId are also current therapy.
            var encounterMedicines=medicines
                .Where(pm => pm.EncounterId==encounter.Id ||
                             (!pm.EncounterId.HasValue && encounter.Id==encounters[0].Id))
                .Take(5)
                .ToList();

            rows.Add(new GenericReportRow
            {
                PrimaryHeader=SelectedPatientLabel,
                HistoryDateValue=(encounter.ScheduledStart ?? encounter.EncounterDate).ToString("dd.MM.yyyy"),
                HistoryEncounterValue=encounter.Doctor?.FullName ?? string.Empty,
                HistoryScoreValue=score?.ScoreText ?? string.Empty,
                HistoryMedicineValue=string.Join("; ", encounterMedicines.Select(pm =>
                    pm.Medicine?.FullName ?? pm.Medicine?.Name ?? "Непознат лек")),
                HistoryQuantityValue=string.Join("; ", encounterMedicines
                    .Where(pm => pm.Quantity > 0)
                    .Select(pm => pm.Quantity.ToString("0.##"))),
                HistoryRegimenValue=string.Join("; ", encounterMedicines.Select(pm =>
                {
                    var dosage=string.IsNullOrWhiteSpace(pm.Dosage)
                        ? pm.Medicine?.DefaultDosage ?? string.Empty
                        : pm.Dosage;
                    var frequency=pm.ApplicationRegime?.Regime;
                    if(string.IsNullOrWhiteSpace(frequency))
                        frequency=pm.DosesFrequency.ToString();

                    return string.Join(", ", new[] { dosage, frequency }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
                }).Where(x => !string.IsNullOrWhiteSpace(x))),
                HistoryDosageValue=string.Join("; ", diagnoses),
                HistoryFrequencyValue=encounter.ReasonForVisit ?? string.Empty,
                HistoryMedicineStatusValue=EncounterStatusLabel(encounter.Status)
            });

        return rows;
    }

    private static async Task<List<GenericReportRow>> LoadPatientsAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange, Guid? patientId)
    {
        var data = await db.Patients
            .Include(p => p.Doctor).ThenInclude(d => d.User)
            .Include(p => p.Diagnoses).ThenInclude(d => d.Mkb10Code)
            .Include(p => p.PatientMedicines).ThenInclude(pm => pm.Medicine)
            .Include(p => p.Scores)
            .AsNoTracking()
            .Where(p => p.CreatedAt>=startRange&&p.CreatedAt<=endRange
                        &&(!patientId.HasValue||p.Id==patientId.Value))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return data.Select(p =>
        {
            var lastScore=p.Scores
                .OrderByDescending(s => s.RecordedAt)
                .Select(s => s.ScoreText)
                .FirstOrDefault() ?? "";

            var diagnoses=string.Join(", ", p.Diagnoses
                .Where(d => d.Mkb10Code!=null)
                .Select(d => d.Mkb10Code!.Code)
                .Distinct());

            var medicines=string.Join(", ", p.PatientMedicines
                .Where(pm => pm.Medicine!=null)
                .Select(pm => pm.Medicine!.Name)
                .Distinct());

            return new GenericReportRow
            {
                PrimaryHeader=p.FullName,
                SecondaryHeader=p.SzboNumber,
                GenderValue=p.Gender.ToDisplay(),
                PhoneValue=p.Phone,
                LastScoreValue=lastScore,
                AddressValue=p.Address,
                CityValue=p.City,
                MedicineValue=medicines,
                DiagnosisValue=diagnoses,
                StatusValue=p.Status.ToDisplay(),
                RheumatologistValue=p.Doctor?.FullName ?? "",
                IsAlertSeverity=false
            };
        }).ToList();
    }

    private void RecomputeMetrics()
    {
        if(SelectedReportType.Type==ReportType.MedicineConsumption)
        {
            Metric1Value=(int)Math.Round(AllItems.Sum(x => x.MedicineConsumptionValue));
            Metric2Value=AllItems.Count;
            Metric3ValueText=AllItems.Sum(x => x.MedicinePatientCount).ToString();
            return;
        }

        Metric1Value=AllItems.Count;
        Metric2Value=AllItems.Count(x => x.IsAlertSeverity);
        Metric3ValueText=Metric1Value>0
            ? $"{Math.Round((double)(Metric1Value-Metric2Value)/Metric1Value*100)}%"
            : "100%";
    }

    private static async Task<List<GenericReportRow>> LoadMedicineConsumptionAsync(
        DesktopTherapyDbContext db,
        DateTime startRange,
        DateTime endRange,
        Guid? patientId,
        Guid? medicineId)
    {
        var data=await db.PatientMedicines
            .Include(pm => pm.Patient)
            .Include(pm => pm.Medicine)
            .Include(pm => pm.Patient).ThenInclude(p => p.Diagnoses).ThenInclude(d => d.Mkb10Code)
            .AsNoTracking()
            .Where(pm => (!patientId.HasValue || pm.PatientId==patientId.Value)
                         &&(!medicineId.HasValue || pm.MedicineId==medicineId.Value)
                         &&pm.StartDate<=endRange
                         &&(!pm.EndDate.HasValue || pm.EndDate.Value>=startRange))
            .ToListAsync();

        var grouped=data
            .GroupBy(pm => pm.Medicine?.FullName??pm.Medicine?.Name??"Непознат лек")
            .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var rows=new List<GenericReportRow>();
        foreach(var group in grouped)
        {
            decimal total=0;
            var patientNames=new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var diagnoses=new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

            foreach(var pm in group)
            {
                // Consumption is the recorded Quantity for each therapy record.
                // Frequency/application count is intentionally not multiplied.
                total+=pm.Quantity;

                if(pm.Patient is not null)
                    patientNames.Add(pm.Patient.FullName);

                foreach(var diagnosis in pm.Patient?.Diagnoses??[])
                {
                    var code=diagnosis.Mkb10Code?.Code?.Trim();
                    var description=diagnosis.Mkb10Code?.Description?.Trim();
                    if(string.IsNullOrWhiteSpace(code)&&string.IsNullOrWhiteSpace(description)) continue;
                    diagnoses.Add(string.IsNullOrWhiteSpace(description) ? code! : $"{code} — {description}");
                }
            }

            rows.Add(new GenericReportRow
            {
                PrimaryHeader=group.Key,
                HighlightValue=total.ToString("0.##"),
                SecondaryHeader=patientNames.Count.ToString(),
                DateValue=$"{startRange:dd.MM.yyyy} – {endRange:dd.MM.yyyy}",
                InformationalText=string.Join("; ", diagnoses.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)),
                MedicineConsumptionValue=total,
                MedicinePatientCount=patientNames.Count,
                MedicineDiagnosisValue=string.Join("; ", diagnoses.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase))
            });
        }

        return rows;
    }

    private static string EncounterStatusLabel(EncounterStatus status) => status switch
    {
        EncounterStatus.Scheduled => "Закажан",
        EncounterStatus.InProgress => "Во тек",
        EncounterStatus.Completed => "Завршен",
        EncounterStatus.Cancelled => "Откажан",
        _ => status.ToString()
    };

    private static string StatusLabel(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => "Закажан",
        AppointmentStatus.InProgress => "Во тек",
        AppointmentStatus.Completed => "Завршен",
        AppointmentStatus.Cancelled => "Откажан",
        _ => status.ToString()
    };

    // ================= PIPELINE HOOKS =================
    protected override IEnumerable<GenericReportRow> ApplySearch(IEnumerable<GenericReportRow> items, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return items;

        var term = search.Trim();
        return items.Where(x =>
            (x.PrimaryHeader?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.SecondaryHeader?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.InformationalText?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.HistoryScoreValue?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.HistoryMedicineValue?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.HistoryDosageValue?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.HistoryFrequencyValue?.Contains(term, StringComparison.OrdinalIgnoreCase)??false));
    }

    protected override IEnumerable<GenericReportRow> ApplyFilters(IEnumerable<GenericReportRow> items)
    {
        if(SelectedReportType.Type==ReportType.Patients)
        {
            if(SelectedPatientStatus!="Сите")
                items=items.Where(x => x.StatusValue==SelectedPatientStatus);
            if(SelectedPatientCity!="Сите")
                items=items.Where(x => x.CityValue==SelectedPatientCity);
            if(SelectedRheumatologist!="Сите")
                items=items.Where(x => x.RheumatologistValue==SelectedRheumatologist);
            if(SelectedDiagnosis!="Сите")
                items=items.Where(x => x.DiagnosisValue.Contains(SelectedDiagnosis, StringComparison.OrdinalIgnoreCase));
            if(SelectedMedicine!="Сите")
                items=items.Where(x => x.MedicineValue.Contains(SelectedMedicine, StringComparison.OrdinalIgnoreCase));
            if(SelectedGender!="Сите")
                items=items.Where(x => x.GenderValue==SelectedGender);
            if(SelectedScore!="Сите")
                items=items.Where(x => x.LastScoreValue==SelectedScore);
            return items;
        }

        if(string.IsNullOrWhiteSpace(StatusFilter?.Label)||StatusFilter.Label=="ИТНО / СИТЕ")
            return items;

        return items.Where(x => x.HighlightValue==StatusFilter.Label);
    }

    protected override IEnumerable<GenericReportRow> ApplySort(IEnumerable<GenericReportRow> query) =>
        query;

    protected override void ResetFilters()
    {
        _suppressSearchTextSideEffects=true;
        SearchText=string.Empty;
        _suppressSearchTextSideEffects=false;

        StatusFilter=new ReportStatusOption { Label="ИТНО / СИТЕ" };
        SelectedPatientStatus="Сите";
        SelectedPatientCity="Сите";
        SelectedRheumatologist="Сите";
        SelectedDiagnosis="Сите";
        SelectedMedicine="Сите";
        SelectedGender="Сите";
        SelectedScore="Сите";
        IsMedicineFilterEnabled=false;
        SelectedMedicineFilter=null;
        ClearPatientSelection();
    }

    // ============================================================
    // SPARK CONTROLS
    // ============================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private SparkPickerItem _reportTypePicker;
    private SparkPickerItem _statusPicker;
    private SparkPickerItem _cityPicker;
    private SparkPickerItem _rheumatologistPicker;
    private SparkPickerItem _diagnosisPicker;
    private SparkPickerItem _medicinePicker;
    private SparkPickerItem _genderPicker;
    private SparkPickerItem _scorePicker;

    private void InitializeSparkControls()
    {
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
    }

    private void BuildSparkPickers()
    {
        Pickers.Clear();

        if(SelectedReportType.Type==ReportType.Patients && IsPatientHistoryMode)
            return;

        _reportTypePicker=MakePicker("Извештај", ReportTypes.Select(x => x.Label), SelectedReportType.Label,
            selected =>
            {
                var match = ReportTypes.FirstOrDefault(x => x.Label==selected);
                if(match!=null) SelectedReportType=match;
            });

        if(SelectedReportType.Type==ReportType.Patients)
            BuildPatientReportPickers();
        else if(SelectedReportType.Type!=ReportType.MedicineConsumption)
        {
            _statusPicker=MakePicker("Статус", new[] { "ИТНО / СИТЕ" }, StatusFilter.Label,
                selected => StatusFilter=new ReportStatusOption { Label=selected });
            Pickers.Add(_statusPicker);
        }

        Pickers.Insert(0, _reportTypePicker);
    }

    private void BuildPatientReportPickers()
    {
        var rows=AllItems.Where(x => x!=null).ToList();

        _statusPicker=MakePicker("Статус", new[] { "Сите" }.Concat(rows.Select(x => x.StatusValue).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()), SelectedPatientStatus,
            selected =>
            {
                SelectedPatientStatus=selected;
                ApplyPipeline();
            });
        _cityPicker=MakePicker("Град", new[] { "Сите" }.Concat(rows.Select(x => x.CityValue).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()), SelectedPatientCity,
            selected =>
            {
                SelectedPatientCity=selected;
                ApplyPipeline();
            });
        _rheumatologistPicker=MakePicker("Реуматолог", new[] { "Сите" }.Concat(rows.Select(x => x.RheumatologistValue).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()), SelectedRheumatologist,
            selected =>
            {
                SelectedRheumatologist=selected;
                ApplyPipeline();
            });
        _diagnosisPicker=MakePicker("Дијагноза", new[] { "Сите" }.Concat(rows.Select(x => x.DiagnosisValue).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()), SelectedDiagnosis,
            selected =>
            {
                SelectedDiagnosis=selected;
                ApplyPipeline();
            });
        _medicinePicker=MakePicker("Лек", new[] { "Сите" }.Concat(rows.Select(x => x.MedicineValue).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()), SelectedMedicine,
            selected =>
            {
                SelectedMedicine=selected;
                ApplyPipeline();
            });
        _genderPicker=MakePicker("Пол", new[] { "Сите" }.Concat(rows.Select(x => x.GenderValue).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()), SelectedGender,
            selected =>
            {
                SelectedGender=selected;
                ApplyPipeline();
            });
        _scorePicker=MakePicker("Скор", new[] { "Сите" }.Concat(rows.Select(x => x.LastScoreValue).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()), SelectedScore,
            selected =>
            {
                SelectedScore=selected;
                ApplyPipeline();
            });

        Pickers.Add(_statusPicker);
        Pickers.Add(_cityPicker);
        Pickers.Add(_rheumatologistPicker);
        Pickers.Add(_diagnosisPicker);
        Pickers.Add(_medicinePicker);
        Pickers.Add(_genderPicker);
        Pickers.Add(_scorePicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_reportTypePicker!=null) _reportTypePicker.SelectedItem=SelectedReportType.Label;

        if(SelectedReportType.Type==ReportType.Patients)
        {
            if(_statusPicker!=null) _statusPicker.SelectedItem=SelectedPatientStatus;
            if(_cityPicker!=null) _cityPicker.SelectedItem=SelectedPatientCity;
            if(_rheumatologistPicker!=null) _rheumatologistPicker.SelectedItem=SelectedRheumatologist;
            if(_diagnosisPicker!=null) _diagnosisPicker.SelectedItem=SelectedDiagnosis;
            if(_medicinePicker!=null) _medicinePicker.SelectedItem=SelectedMedicine;
            if(_genderPicker!=null) _genderPicker.SelectedItem=SelectedGender;
            if(_scorePicker!=null) _scorePicker.SelectedItem=SelectedScore;
        }
        else if(_statusPicker!=null)
            _statusPicker.SelectedItem=StatusFilter.Label;
    }

    protected override void BuildSparkButtons()
    {
        Buttons.Clear();
        Buttons.Add(new SparkButtonItem
        {
            Label="Исчисти",
            IsPrimary=true,
            Command=ClearFiltersCommand
        });
        Buttons.Add(new SparkButtonItem
        {
            Label="Извези PDF",
            IsPrimary=false,
            IsEnabled=CanExport,
            Command=ExportToPdfCommand
        });
    }

    private void BuildSparkGridColumns()
    {
        if(SelectedReportType.Type==ReportType.Patients && IsPatientHistoryMode && IsScoreHistoryMode)
        {
            GridColumns=new ObservableCollection<SparkGridColumn>
            {
                new() { Header="ДАТУМ", Key="HistoryDate", Width=new GridLength(1.2, GridUnitType.Star) },
                new() { Header="РЕУМАТОЛОГ", Key="HistoryEncounter", Width=new GridLength(2.0, GridUnitType.Star) },
                new() { Header="СКОР", Key="HistoryScore", Width=new GridLength(1.2, GridUnitType.Star) }
            };
            return;
        }

        if(SelectedReportType.Type==ReportType.Patients && IsPatientHistoryMode)
        {
            GridColumns=new ObservableCollection<SparkGridColumn>
            {
                new() { Header="ДАТУМ", Key="HistoryDate", Width=new GridLength(1.0, GridUnitType.Star) },
                new() { Header="ПРЕГЛЕД / РЕУМАТОЛОГ", Key="HistoryEncounter", Width=new GridLength(1.8, GridUnitType.Star) },
                new() { Header="СКОР", Key="HistoryScore", Width=new GridLength(1.0, GridUnitType.Star) },
                new() { Header="ЛЕК", Key="HistoryMedicine", Width=new GridLength(2.0, GridUnitType.Star) },
                new() { Header="КОЛИЧИНА", Key="HistoryQuantity", Width=new GridLength(1.0, GridUnitType.Star) },
                new() { Header="ДОЗА / РЕЖИМ", Key="HistoryRegimen", Width=new GridLength(1.8, GridUnitType.Star) },
                new() { Header="МКБ-10", Key="HistoryDosage", Width=new GridLength(2.4, GridUnitType.Star) },
                new() { Header="ПРИЧИНА", Key="HistoryFrequency", Width=new GridLength(1.7, GridUnitType.Star) },
                new() { Header="СТАТУС", Key="HistoryStatus", Width=new GridLength(1.1, GridUnitType.Star) }
            };
            return;
        }

        if(SelectedReportType.Type==ReportType.Patients)
        {
            GridColumns=new ObservableCollection<SparkGridColumn>
            {
                new() { Header="ПАЦИЕНТ", Key="Primary", Width=new GridLength(2, GridUnitType.Star) },
                new() { Header="ЕЗБО", Key="Szbo", Width=new GridLength(1.2, GridUnitType.Star) },
                new() { Header="ПОЛ", Key="Gender", Width=new GridLength(0.8, GridUnitType.Star) },
                new() { Header="ТЕЛЕФОН", Key="Phone", Width=new GridLength(1.3, GridUnitType.Star) },
                new() { Header="ПОСЛ. СКОР", Key="Score", Width=new GridLength(1.1, GridUnitType.Star) },
                new() { Header="АДРЕСА", Key="Address", Width=new GridLength(1.7, GridUnitType.Star) },
                new() { Header="ГРАД", Key="City", Width=new GridLength(1.1, GridUnitType.Star) },
                new() { Header="ЛЕК", Key="Medicine", Width=new GridLength(1.7, GridUnitType.Star) },
                new() { Header="ДИЈАГНОЗА", Key="Diagnosis", Width=new GridLength(1.7, GridUnitType.Star) }
            };
            return;
        }

        if(SelectedReportType.Type==ReportType.MedicineConsumption)
        {
            GridColumns=new ObservableCollection<SparkGridColumn>
            {
                new() { Header="ЛЕК", Key="MedicineConsumptionMedicine", Width=new GridLength(2, GridUnitType.Star) },
                new() { Header="ВКУПНА ПОТРОШЕНА КОЛИЧИНА", Key="MedicineConsumptionQuantity", Width=new GridLength(1.6, GridUnitType.Star) },
                new() { Header="ПАЦИЕНТИ", Key="MedicineConsumptionPatients", Width=new GridLength(1, GridUnitType.Star) },
                new() { Header="МКБ-10 КОД + ОПИС", Key="MedicineConsumptionDiagnosis", Width=new GridLength(2.8, GridUnitType.Star) },
                new() { Header="ПЕРИОД", Key="MedicineConsumptionPeriod", Width=new GridLength(1.6, GridUnitType.Star) }
            };
            return;
        }

        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header=Col1Header, Key="Primary", Width=new GridLength(2, GridUnitType.Star) },
            new() { Header=Col2Header, Key="Secondary", Width=new GridLength(2, GridUnitType.Star) },
            new() { Header=Col3Header, Key="Highlight", CellType=SparkGridCellType.Badge, Width=new GridLength(1, GridUnitType.Star) },
            new() { Header=Col4Header, Key="Date", Width=new GridLength(1, GridUnitType.Star) },
            new() { Header=Col5Header, Key="Info", Width=new GridLength(2, GridUnitType.Star) }
        };
    }

    protected override void OnPageProjected(ObservableCollection<GenericReportRow> page)
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var r in page)
        {
            var row = new SparkGridRow { Tag=r };
            row["Primary"]=r.PrimaryHeader??"";
            row["Secondary"]=r.SecondaryHeader??"";
            row["Highlight"]=new SparkBadgeValue(r.HighlightValue??"", r.IsAlertSeverity ? SparkBadgeTone.Danger : SparkBadgeTone.Neutral);
            row["Date"]=r.DateValue??"";
            row["Info"]=r.InformationalText??"";

            if(SelectedReportType.Type==ReportType.Patients && IsPatientHistoryMode)
            {
                row["HistoryDate"]=r.HistoryDateValue??"";
                row["HistoryEncounter"]=r.HistoryEncounterValue??"";
                row["HistoryScore"]=r.HistoryScoreValue??"";
                row["HistoryMedicine"]=r.HistoryMedicineValue??"";
                row["HistoryQuantity"]=r.HistoryQuantityValue??"";
                row["HistoryRegimen"]=r.HistoryRegimenValue??"";
                row["HistoryDosage"]=r.HistoryDosageValue??"";
                row["HistoryFrequency"]=r.HistoryFrequencyValue??"";
                row["HistoryStatus"]=r.HistoryMedicineStatusValue??"";
            }
            else if(SelectedReportType.Type==ReportType.MedicineConsumption)
            {
                row["MedicineConsumptionMedicine"]=r.PrimaryHeader??"";
                row["MedicineConsumptionQuantity"]=r.MedicineConsumptionValue.ToString("0.##");
                row["MedicineConsumptionPatients"]=r.MedicinePatientCount.ToString();
                row["MedicineConsumptionDiagnosis"]=r.MedicineDiagnosisValue??"";
                row["MedicineConsumptionPeriod"]=r.DateValue??"";
            }
            else if(SelectedReportType.Type==ReportType.Patients)
            {
                row["Szbo"]=r.SecondaryHeader??"";
                row["Gender"]=r.GenderValue??"";
                row["Phone"]=r.PhoneValue??"";
                row["Score"]=r.LastScoreValue??"";
                row["Address"]=r.AddressValue??"";
                row["City"]=r.CityValue??"";
                row["Medicine"]=r.MedicineValue??"";
                row["Diagnosis"]=r.DiagnosisValue??"";
            }
            rows.Add(row);
        }

        GridRows=rows;
    }

    // ============================================================
    // PDF EXPORT — kept as HTML->Launcher, unchanged in approach
    // ============================================================
    [RelayCommand]
    public async Task ExportToPdfAsync()
    {
        if(!CanView || !CanExport || IsBusy) return;

        try
        {
            IsBusy=true;

            var rows=FilteredItems.ToList();
            var selectedMedicine=IsMedicineConsumptionSelected && IsMedicineFilterEnabled
                ? SelectedMedicineFilter
                : null;

            var reportTitle=selectedMedicine is null
                ? SelectedReportType.Label
                : $"{SelectedReportType.Label} – {selectedMedicine.Display}";

            var exportColumns=GridColumns.ToList();
            var exportRows=GridRows.ToList();

            await _reportExportService.ExportToPdfAsync(
                reportTitle: reportTitle,
                insitutionName: "КЛИНИКА ЗА РЕУМАТОЛОГИЈА - СКОПЈЕ",
                generatedBy: "Систем",
                startDate: StartDate,
                endDate: EndDate,
                columns: exportColumns,
                rows: exportRows,
                selectedMedicine: selectedMedicine?.Display,
                selectedMedicineTotalQuantity: selectedMedicine is null
                    ? null
                    : rows.Sum(x => x.MedicineConsumptionValue));
        }
        catch(Exception ex)
        {
            OnError($"Грешка при извоз на PDF: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

}

public enum ReportType
{
    MissedTherapies,
    Auditing,
    AppointmentStatuses,
    Patients,
    MedicineConsumption
}

public sealed class ReportTypeOption
{
    public ReportType Type
    {
        get; init;
    }
    public string Label { get; init; } = "";
    public override string ToString() => Label;
}

public sealed class ReportStatusOption
{
    public string Label { get; init; } = "";
    public override string ToString() => Label;
}

public class GenericReportRow
{
    public string PrimaryHeader { get; set; } = "";
    public string SecondaryHeader { get; set; } = "";
    public string HighlightValue { get; set; } = "";
    public string DateValue { get; set; } = "";
    public string InformationalText { get; set; } = "";
    public bool IsAlertSeverity { get; set; }

    public string StatusValue { get; set; } = "";
    public string GenderValue { get; set; } = "";
    public string PhoneValue { get; set; } = "";
    public string LastScoreValue { get; set; } = "";
    public string AddressValue { get; set; } = "";
    public string CityValue { get; set; } = "";
    public string MedicineValue { get; set; } = "";
    public string DiagnosisValue { get; set; } = "";
    public string RheumatologistValue { get; set; } = "";

    public string HistoryDateValue { get; set; } = "";
    public string HistoryEncounterValue { get; set; } = "";
    public string HistoryScoreValue { get; set; } = "";
    public string HistoryMedicineValue { get; set; } = "";
    public string HistoryQuantityValue { get; set; } = "";
    public string HistoryRegimenValue { get; set; } = "";
    public string HistoryDosageValue { get; set; } = "";
    public string HistoryFrequencyValue { get; set; } = "";
    public string HistoryMedicineStatusValue { get; set; } = "";
    public decimal MedicineConsumptionValue { get; set; }
    public int MedicinePatientCount { get; set; }
    public string MedicineDiagnosisValue { get; set; } = "";
}

public sealed record MedicineFilterOption(Guid Id, string Display)
{
    public override string ToString() => Display;
}