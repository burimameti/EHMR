using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EHMR.ViewModels.Reports;

public partial class ReportListViewModel : BaseViewModel<GenericReportRow>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

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
        new() { Type = ReportType.Patients,               Label = "Пациенти" }
    ];

    private ReportTypeOption _selectedReportType;

    public ReportTypeOption SelectedReportType
    {
        get => _selectedReportType;
        set
        {
            if(!SetProperty(ref _selectedReportType, value)) return;
            ApplyColumnLayout(value.Type);
            SyncSparkPickersFromFilters();
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
        ISelectedItemService<GenericReportRow> selectedItemService)
        : base(navigationService, userDialogService, menuService, authService, selectedItemService)
    {
        _dbFactory=dbFactory;
        _selectedReportType=ReportTypes.First(x => x.Type==ReportType.MissedTherapies);
        _statusFilter=new ReportStatusOption { Label="ИТНО / СИТЕ" };
        _navigationService=navigationService;

        PageSize=10;

        PropertyChanged+=OnViewModelPropertyChanged;

        ApplyColumnLayout(SelectedReportType.Type);
        EvaluatePermissions();
        InitializeSparkControls();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName!=nameof(SearchText)) return;
        if(_suppressSearchTextSideEffects) return;

        ApplyPipeline();
    }

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
                Col1Header="ПАЦИЕНТ (ИМЕ/ПРЕЗИМЕ)"; Col2Header="МАТИЧЕН БРОЈ"; Col3Header="ОДДЕЛЕНИЕ"; Col4Header="КРЕИРАН НА"; Col5Header="ДИЈАГНОЗА / АЛЕРГИИ";
                Metric1Title="Нови Пациенти"; Metric2Title="Хронични Случаи"; Metric3Title="Активни Картони";
                break;
        }
    }

    // ============================================================
    // DATA LOAD — one real EF Core query per report type
    // ============================================================
    [RelayCommand]
    public async Task GenerateReportAsync()
    {
        if(IsBusy) return;
        try
        {
            IsBusy=true;
            ClearError();

            var startRange = StartDate.Date;
            var endRange = EndDate.Date.AddDays(1).AddTicks(-1);

            await using var db = await _dbFactory.CreateDbContextAsync();
            List<GenericReportRow> rows;

            switch(SelectedReportType.Type)
            {
                case ReportType.MissedTherapies:
                    rows=await LoadMissedTherapiesAsync(db, startRange, endRange);
                    break;

                case ReportType.Auditing:
                    rows=await LoadAuditingAsync(db, startRange, endRange);
                    break;

                case ReportType.AppointmentStatuses:
                    rows=await LoadAppointmentStatusesAsync(db, startRange, endRange);
                    break;

                case ReportType.Patients:
                    rows=await LoadPatientsAsync(db, startRange, endRange);
                    break;

                default:
                    rows= [];
                    break;
            }

            AllItems=rows;
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

    private static async Task<List<GenericReportRow>> LoadMissedTherapiesAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange)
    {
        var data = await db.TherapyCycles
            .Include(p => p.Patient)
            .AsNoTracking()
            .Where(c => c.Status==TherapyStatus.Missed
                        &&c.StartDate>=startRange&&c.EndDate<=endRange)
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
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange)
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
            DateValue=a.Timestamp.ToString("dd.MM.yyyy HH:mm"),
            InformationalText=a.Description,
            IsAlertSeverity=a.AfterValue!=a.BeforeValue
        }).ToList();
    }

    private static async Task<List<GenericReportRow>> LoadAppointmentStatusesAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange)
    {
        var data = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .AsNoTracking()
            .Where(a => a.ScheduledStart>=startRange&&a.ScheduledStart<=endRange)
            .OrderByDescending(a => a.ScheduledStart)
            .ToListAsync();

        return data.Select(a => new GenericReportRow
        {
            PrimaryHeader=a.Patient?.FullName??"",
            SecondaryHeader=a.Doctor?.FullName??"",
            HighlightValue=StatusLabel(a.Status),
            DateValue=a.ScheduledStart.ToString("dd.MM.yyyy HH:mm"),
            InformationalText=a.ReasonForVisit??"",
            IsAlertSeverity=a.Status is AppointmentStatus.Cancelled or AppointmentStatus.Missed
        }).ToList();
    }

    private static async Task<List<GenericReportRow>> LoadPatientsAsync(
        DesktopTherapyDbContext db, DateTime startRange, DateTime endRange)
    {
        var data = await db.Patients
            .AsNoTracking()
            .Where(p => p.CreatedAt>=startRange&&p.CreatedAt<=endRange)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return data.Select(p => new GenericReportRow
        {
            PrimaryHeader=p.FullName,
            SecondaryHeader=PrivacyMaskHelper.MaskNationalId(p.NationalId),
            HighlightValue=p.Phone??"",
            DateValue=p.CreatedAt.ToString("dd.MM.yyyy"),
            InformationalText=$"Dg: {p.Diagnoses.Select(x => x.Mkb10Code.Code)}. Алергии: {(string.IsNullOrEmpty(p.Allergies) ? "нема" : p.Allergies)}",
            IsAlertSeverity=!string.IsNullOrEmpty(p.Allergies)
        }).ToList();
    }

    private void RecomputeMetrics()
    {
        Metric1Value=AllItems.Count;
        Metric2Value=AllItems.Count(x => x.IsAlertSeverity);
        Metric3ValueText=Metric1Value>0
            ? $"{Math.Round((double)(Metric1Value-Metric2Value)/Metric1Value*100)}%"
            : "100%";
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

    // ================= PIPELINE HOOKS =================
    protected override IEnumerable<GenericReportRow> ApplySearch(IEnumerable<GenericReportRow> items, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return items;

        var term = search.Trim();
        return items.Where(x =>
            (x.PrimaryHeader?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.SecondaryHeader?.Contains(term, StringComparison.OrdinalIgnoreCase)??false)||
            (x.InformationalText?.Contains(term, StringComparison.OrdinalIgnoreCase)??false));
    }

    protected override IEnumerable<GenericReportRow> ApplyFilters(IEnumerable<GenericReportRow> items)
    {
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
    }

    // ============================================================
    // SPARK CONTROLS
    // ============================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private SparkPickerItem _reportTypePicker;
    private SparkPickerItem _statusPicker;

    private void InitializeSparkControls()
    {
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
    }

    private void BuildSparkPickers()
    {
        Pickers.Clear();

        _reportTypePicker=MakePicker("Извештај", ReportTypes.Select(x => x.Label), SelectedReportType.Label,
            selected =>
            {
                var match = ReportTypes.FirstOrDefault(x => x.Label==selected);
                if(match!=null) SelectedReportType=match;
            });

        _statusPicker=MakePicker("Статус", new[] { "ИТНО / СИТЕ" }, StatusFilter.Label,
            selected => StatusFilter=new ReportStatusOption { Label=selected });

        Pickers.Add(_reportTypePicker);
        Pickers.Add(_statusPicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_reportTypePicker!=null) _reportTypePicker.SelectedItem=SelectedReportType.Label;
        if(_statusPicker!=null) _statusPicker.SelectedItem=StatusFilter.Label;
    }

    protected override void BuildSparkButtons()
    {
        Buttons.Clear();
        Buttons.Add(new SparkButtonItem
        {
            Label="✕ Исчисти",
            IsPrimary=true,
            Command=ClearFiltersCommand
        });
        Buttons.Add(new SparkButtonItem
        {
            Label="Извези PDF",
            IsPrimary=false,
            Command=ExportToPdfCommand
        });
    }

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = Col1Header, Key = "Primary",  Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = Col2Header, Key = "Secondary", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = Col3Header, Key = "Highlight", CellType = SparkGridCellType.Badge, Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = Col4Header, Key = "Date",      Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = Col5Header, Key = "Info",      Width = new GridLength(2, GridUnitType.Star) }
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
        if(IsBusy) return;
        try
        {
            IsBusy=true;

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
                <h2>ИЗВЕШТАЈ: {SelectedReportType.Label}</h2>
                <p>Опсег: {StartDate:dd.MM.yyyy} до {EndDate:dd.MM.yyyy}</p>
                <table>
                    <thead>
                        <tr>
                            <th>{Col1Header}</th><th>{Col2Header}</th><th>{Col3Header}</th><th>{Col4Header}</th><th>{Col5Header}</th>
                        </tr>
                    </thead>
                    <tbody>";

            foreach(var item in AllItems)
            {
                htmlBlueprint+=$@"
                    <tr>
                        <td><b>{item.PrimaryHeader}</b></td>
                        <td>{item.SecondaryHeader}</td>
                        <td>{item.HighlightValue}</td>
                        <td>{item.DateValue}</td>
                        <td><span class='{(item.IsAlertSeverity ? "alert" : "")}'>{item.InformationalText}</span></td>
                    </tr>";
            }

            htmlBlueprint+="</tbody></table></body></html>";

            string fileName = $"Report_{SelectedReportType.Type}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            string targetFile = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(targetFile, htmlBlueprint);

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File=new ReadOnlyFile(targetFile)
            });
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
    Patients
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
    public bool IsAlertSeverity
    {
        get; set;
    }
}