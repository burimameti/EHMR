using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Reports;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EHMR.ViewModels;

public partial class ReportViewModel : BaseViewModel<DynamicReportRow>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

    private bool _suppressAdvancedFilterSideEffects;
    private ReportDefinition? _activeReport;

    // ---- Hub state ----
    [ObservableProperty] private bool isShowingDetails;
    [ObservableProperty] private string hubSearchText = string.Empty;
    [ObservableProperty] private string userName = ""; // TODO: wire to your real session/user service

    public ObservableCollection<ReportDefinition> AvailableReports { get; } = new();
    private readonly List<ReportDefinition> _allReportDefinitions;

    // ---- Details state ----
    [ObservableProperty] private DateTime startDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime endDate = DateTime.Today;
    [ObservableProperty] private string advancedFilterText = string.Empty;

    [ObservableProperty] private string metric1Title = "Вкупно записи";
    [ObservableProperty] private int metric1Value;
    [ObservableProperty] private string metric2Title = "Критични аларми";
    [ObservableProperty] private int metric2Value;
    [ObservableProperty] private string metric3Title = "Стапка на доследност";
    [ObservableProperty] private int metric3Value;

    public ObservableCollection<DynamicReportColumn> FormattedColumns { get; } = new();
    public ObservableCollection<DynamicReportRow> ProcessedRows { get; } = new();

    protected override string ModuleName => Modules.Reports;

    public ReportViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IAuthorizationService authService)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _dbFactory=dbFactory;

        _allReportDefinitions=
        [
            new() {   Icon = "⏱️", Title = "Пропуштени терапии",  Category = ReportCategory.MissedTherapies,  Description = "Циклуси означени како пропуштени во избраниот период." },
            new() { Icon = "🛡️", Title = "Aудит",                Category = ReportCategory.SecurityAuditing,    Description = "Хронолошки преглед на системски акции и настани." },
            new() { Icon = "📅", Title = "Статус на термини",   Category = ReportCategory.AppointmentStatuses,   Description = "Статус на закажани, завршени и откажани прегледи." },
            new() { Icon = "🧑‍⚕️", Title = "Пациенти",           Category = ReportCategory.Patients,   Description = "Ново регистрирани пациенти во избраниот период." }
        ];

        RefreshAvailableReports();

        PropertyChanged+=OnViewModelPropertyChanged;

        EvaluatePermissions();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName==nameof(HubSearchText))
        {
            RefreshAvailableReports();
            return;
        }

        if(e.PropertyName==nameof(AdvancedFilterText))
        {
            if(_suppressAdvancedFilterSideEffects) return;
            ApplyPipeline();
        }
    }

    private void RefreshAvailableReports()
    {
        var term = HubSearchText?.Trim()??"";
        var filtered = string.IsNullOrWhiteSpace(term)
            ? _allReportDefinitions
            : _allReportDefinitions.Where(r =>
                r.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Category.ToString().Equals(term, StringComparison.OrdinalIgnoreCase)).ToList();

        AvailableReports.Clear();
        foreach(var r in filtered) AvailableReports.Add(r);
    }

    [RelayCommand]
    private async Task SelectReport(ReportDefinition report)
    {
        if(report is null) return;

        _activeReport=report;
        BuildColumnsFor(report.Category);
        IsShowingDetails=true;
        await GenerateReportAsync();
    }

    [RelayCommand]
    private void BackToHub()
    {
        IsShowingDetails=false;
        _activeReport=null;
    }

    // ============================================================
    // DATA LOAD
    // ============================================================
    [RelayCommand]
    public async Task ExecuteReportGenerationAsync() => await GenerateReportAsync();

    private async Task GenerateReportAsync()
    {
        if(IsBusy||_activeReport is null) return;
        try
        {
            IsBusy=true;
            ClearError();

            var startRange = StartDate.Date;
            var endRange = EndDate.Date.AddDays(1).AddTicks(-1);

            await using var db = await _dbFactory.CreateDbContextAsync();

            AllItems=_activeReport.Category switch
            {
                ReportCategory.MissedTherapies => await LoadMissedTherapiesAsync(db, startRange, endRange),
                ReportCategory.Auditing => await LoadAuditingAsync(db, startRange, endRange),
                ReportCategory.AppointmentStatuses => await LoadAppointmentStatusesAsync(db, startRange, endRange),
                ReportCategory.Patients => await LoadPatientsAsync(db, startRange, endRange),
                _ => []
            };

            ApplyPipeline();
            RecomputeMetrics();
        }
        catch(Exception ex)
        {
            OnError($"Грешка при генерирање извештај: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    private static async Task<List<DynamicReportRow>> LoadMissedTherapiesAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange)
    {
        var data = await db.TherapyCycles
            .Include(p => p.Patient)
            .AsNoTracking()
            .Where(c => c.Status==TherapyStatus.Missed
                        &&c.StartDate>=startRange&&c.EndDate<=endRange)
            .OrderByDescending(c => c.EndDate)
            .ToListAsync();

        return data.Select(c => new DynamicReportRow
        {
            Cells=
            [
                c.Patient.NationalId,
                c.Patient.FullName,
                $"Ц-#{c.CycleNumber}",
                c.StartDate.Value.ToString("dd.MM.yyyy"),
                string.IsNullOrEmpty(c.Notes) ? "Нема внесено причина од лекар!" : c.Notes
            ],
            IsAlertSeverity=string.IsNullOrEmpty(c.Notes)
        }).ToList();
    }

    // NOTE: AuditLog entity guessed — adjust field names to match the real entity.
    private static async Task<List<DynamicReportRow>> LoadAuditingAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange)
    {
        var data = await db.AuditLogs
            .AsNoTracking()
            .Where(a => a.Timestamp>=startRange&&a.Timestamp<=endRange)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        return data.Select(a => new DynamicReportRow
        {
            Cells=
            [
                a.Description,
                a.Action,
                a.Data,
                a.Timestamp.ToString("dd.MM.yyyy HH:mm"),
                a.EntityName
            ],
            IsAlertSeverity=a.AfterValue is null || a.BeforeValue is null
        }).ToList();
    }

    private static async Task<List<DynamicReportRow>> LoadAppointmentStatusesAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange)
    {
        var data = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .AsNoTracking()
            .Where(a => a.ScheduledStart>=startRange&&a.ScheduledStart<=endRange)
            .OrderByDescending(a => a.ScheduledStart)
            .ToListAsync();

        return data.Select(a => new DynamicReportRow
        {
            Cells=
            [
                a.Patient?.FullName??"",
                a.Doctor?.FullName??"",
                StatusLabel(a.Status),
                a.ScheduledStart.ToString("dd.MM.yyyy HH:mm"),
                a.ReasonForVisit??""
            ],
            IsAlertSeverity=a.Status is AppointmentStatus.Cancelled or AppointmentStatus.Missed
        }).ToList();
    }

    // NOTE: Patient fields guessed (IdNumber/Department/CreatedAt/Diagnosis/Allergies) — adjust to match the real entity.
    private static async Task<List<DynamicReportRow>> LoadPatientsAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange)
    {
        var data = await db.Patients
            .AsNoTracking()
            .Where(p => p.CreatedAt>=startRange&&p.CreatedAt<=endRange)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return data.Select(p => new DynamicReportRow
        {
            Cells=
            [
                p.FullName,
                p.Phone,
                p.SSN??"",p.Address, p.City,
                p.CreatedAt.ToString("dd.MM.yyyy"),
                $"Dg: {p.Diagnoses.Select(x=>x.Mkb10CodeId)}. Алергии: {(string.IsNullOrEmpty(p.Allergies) ? "нема" : p.Allergies)}"
            ],
            IsAlertSeverity=!string.IsNullOrEmpty(p.Allergies)
        }).ToList();
    }

    private void RecomputeMetrics()
    {
        Metric1Value=AllItems.Count;
        Metric2Value=AllItems.Count(x => x.IsAlertSeverity);
        Metric3Value=Metric1Value>0
            ? (int)Math.Round((double)(Metric1Value-Metric2Value)/Metric1Value*100)
            : 100;
    }

    private static string StatusLabel(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => "Закажан",
        AppointmentStatus.CheckedIn => "Пријавен",
        AppointmentStatus.Completed => "Завршен",
        AppointmentStatus.Cancelled => "Откажан",
        AppointmentStatus.Missed => "Пропуштен",
        AppointmentStatus.InProgress => "Во тек",
        AppointmentStatus.ReScheduled => "Презакажан",
        _ => status.ToString()
    };

    private void BuildColumnsFor(ReportCategory type)
    {
        var (h1, h2, h3, h4, h5)=type switch
        {
            ReportCategory.MissedTherapies => ("ПАЦИЕНТ", "ПРОТОКОЛ", "ЦИКЛУС", "ИСТЕЧЕН РОК", "ОБРАЗЛОЖЕНИЕ"),
            ReportCategory.Auditing => ("КОРИСНИК", "АКЦИЈА / НАСТАН", "МОДУЛ", "ВРЕМЕ", "ДЕТАЛИ ОД АУДИТ ПАТЕКА"),
            ReportCategory.AppointmentStatuses => ("ПАЦИЕНТ", "ДОКТОР / ТЕРАПЕВТ", "СТАТУС", "ТЕРМИН", "ЗАБЕЛЕШКА ОД ПРЕГЛЕД"),
            ReportCategory.Patients => ("ПАЦИЕНТ (ИМЕ/ПРЕЗИМЕ)", "МАТИЧЕН БРОЈ", "ОДДЕЛЕНИЕ", "КРЕИРАН НА", "ДИЈАГНОЗА / АЛЕРГИИ"),
            _ => ("", "", "", "", "")
        };

        FormattedColumns.Clear();
        FormattedColumns.Add(new DynamicReportColumn { HeaderName=h1 });
        FormattedColumns.Add(new DynamicReportColumn { HeaderName=h2 });
        FormattedColumns.Add(new DynamicReportColumn { HeaderName=h3 });
        FormattedColumns.Add(new DynamicReportColumn { HeaderName=h4 });
        FormattedColumns.Add(new DynamicReportColumn { HeaderName=h5 });
    }

    // ================= PIPELINE HOOKS =================
    protected override IEnumerable<DynamicReportRow> ApplySearch(IEnumerable<DynamicReportRow> items, string search)
    {
        var term = AdvancedFilterText?.Trim()??"";
        if(string.IsNullOrWhiteSpace(term)) return items;

        return items.Where(row => row.Cells.Any(c => c?.Contains(term, StringComparison.OrdinalIgnoreCase)??false));
    }

    protected override IEnumerable<DynamicReportRow> ApplyFilters(IEnumerable<DynamicReportRow> items) => items;

    protected override IEnumerable<DynamicReportRow> ApplySort(IEnumerable<DynamicReportRow> query) => query;

    protected override void ResetFilters()
    {
        _suppressAdvancedFilterSideEffects=true;
        AdvancedFilterText=string.Empty;
        _suppressAdvancedFilterSideEffects=false;
    }

    // No Spark header/buttons on this page — satisfy base abstract members with no-ops.
    protected override void SyncSparkPickersFromFilters()
    {
    }
    protected override void BuildSparkButtons()
    {
    }

    protected override void OnPageProjected(ObservableCollection<DynamicReportRow> page)
    {
        ProcessedRows.Clear();
        foreach(var row in page) ProcessedRows.Add(row);
    }

    // ============================================================
    // EXPORT
    // ============================================================
    [RelayCommand]
    public async Task ExportToPdfAsync()
    {
        if(IsBusy||_activeReport is null) return;
        try
        {
            IsBusy=true;

            var headers = FormattedColumns.Select(c => c.HeaderName).ToList();
            var htmlBlueprint = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; padding: 30px; color: #0F172A; }}
                    h2 {{ color: #2563EB; border-bottom: 2px solid #E2E8F0; padding-bottom: 10px; }}
                    table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
                    th {{ background-color: #0F172A; color: white; padding: 12px; text-align: left; font-size: 12px; }}
                    td {{ padding: 12px; border-bottom: 1px solid #E2E8F0; font-size: 13px; }}
                    .alert {{ background-color: #FEF2F2; color: #991B1B; padding: 6px; border-radius: 4px; }}
                </style>
            </head>
            <body>
                <h2>ИЗВЕШТАЈ: {_activeReport.Title}</h2>
                <p>Опсег: {StartDate:dd.MM.yyyy} до {EndDate:dd.MM.yyyy}</p>
                <table>
                    <thead><tr>{string.Join("", headers.Select(h => $"<th>{h}</th>"))}</tr></thead>
                    <tbody>";

            foreach(var row in AllItems)
            {
                var cellsHtml = string.Join("", row.Cells.Select(c => $"<td>{c}</td>"));
                htmlBlueprint+=$"<tr class='{(row.IsAlertSeverity ? "alert" : "")}'>{cellsHtml}</tr>";
            }

            htmlBlueprint+="</tbody></table></body></html>";

            string fileName = $"Report_{_activeReport.Category}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            string targetFile = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(targetFile, htmlBlueprint);

            await Launcher.Default.OpenAsync(new OpenFileRequest { File=new ReadOnlyFile(targetFile) });
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

    [RelayCommand]
    public async Task ExportToExcelAsync()
    {
        // TODO: wire real XLSX export (e.g. ClosedXML) — placeholder to satisfy the binding for now.
        await UserDialogService.ShowAlertAsync("Извоз во Excel", "Извозот во Excel сè уште не е имплементиран.", "OK");
    }
}