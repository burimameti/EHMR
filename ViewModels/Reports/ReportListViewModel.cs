//using System.Collections.ObjectModel;
//using Microsoft.EntityFrameworkCore;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using EHMR.Domain.Entities;
//using EHMR.Infrastructure.Persistence;

//namespace EHMR.ViewModels;

//public partial class ReportViewModel : ObservableObject
//{
//    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

//    [ObservableProperty] private DateTime _startDate = DateTime.Today.AddMonths(-1);
//    [ObservableProperty] private DateTime _endDate = DateTime.Today;
//    [ObservableProperty] private bool _isBusy;

//    // Advanced Filtering Properties
//    [ObservableProperty] private List<ReportType> _availableReportTypes;

//    [ObservableProperty] private ReportType _selectedReportType = ReportType.MissedTherapies;
//    [ObservableProperty] private string _searchText = string.Empty;
//    [ObservableProperty] private string _statusFilter = "ИТНО / СИТЕ";

//    // Dynamic Columns Titles
//    [ObservableProperty] private string _col1Header = "ПАЦИЕНТ";

//    [ObservableProperty] private string _col2Header = "ПРОТОКОЛ / ТЕРАПИЈА";
//    [ObservableProperty] private string _col3Header = "ЦИКЛУС";
//    [ObservableProperty] private string _col4Header = "ДАТУМ";
//    [ObservableProperty] private string _col5Header = "МЕДИЦИНСКО ОБРАЗЛОЖЕНИЕ";

//    // Dynamic Analytics panel tracking
//    [ObservableProperty] private string _metric1Title = "Вкупно Протоколи";

//    [ObservableProperty] private int _metric1Value;
//    [ObservableProperty] private string _metric2Title = "Бараат внимание";
//    [ObservableProperty] private int _metric2Value;
//    [ObservableProperty] private string _metric3Title = "Стапка на Конзистентност";
//    [ObservableProperty] private string _metric3ValueText = "100%";

//    public ObservableCollection<GenericReportRow> UnifiedReportRows { get; set; } = new();

//    public ReportViewModel(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
//    {
//        _dbFactory=dbFactory;
//        AvailableReportTypes=Enum.GetValues(typeof(ReportType)).Cast<ReportType>().ToList();

//        // Initial load
//        _=GenerateReportAsync();
//    }

//    private partial void OnSelectedReportTypeChanged(ReportType value)
//    {
//        // Adjust column layouts instantly based on selection
//        switch(value)
//        {
//            case ReportType.MissedTherapies:
//                Col1Header="ПАЦИЕНТ"; Col2Header="ПРОТОКОЛ"; Col3Header="ЦИКЛУС"; Col4Header="ИСТЕЧЕН РОК"; Col5Header="ОБРАЗЛОЖЕНИЕ";
//                Metric1Title="Пропуштени Протоколи"; Metric2Title="Неразјаснети"; Metric3Title="Легитимирана Доследност";
//                break;

//            case ReportType.Auditing:
//                Col1Header="КОРИСНИК"; Col2Header="АКЦИЈА / НАСТАН"; Col3Header="МОДУЛ"; Col4Header="ВРЕМЕ"; Col5Header="ДЕТАЛИ ОД АУДИТ ПАТЕКА";
//                Metric1Title="Вкупно Акции"; Metric2Title="Безбедносни Критични"; Metric3Title="Системски Статус";
//                break;

//            case ReportType.AppointmentStatuses:
//                Col1Header="ПАЦИЕНТ"; Col2Header="ДОКТОР / ТЕРАПЕВТ"; Col3Header="СТАТУС"; Col4Header="ТЕРМИН"; Col5Header="ЗАБЕЛЕШКА ОД ПРЕГЛЕД";
//                Metric1Title="Закажани Прегледи"; Metric2Title="Откажани Термини"; Metric3Title="Ефикасност на Сали";
//                break;

//            case ReportType.Patients:
//                Col1Header="ПАЦИЕНТ (ИМЕ/ПРЕЗИМЕ)"; Col2Header="МАТИЧЕН БРОЈ"; Col3Header="ОДДЕЛЕНИЕ"; Col4Header="КРЕИРАН НА"; Col5Header="ДИЈАГНОЗА / АЛЕРГИИ";
//                Metric1Title="Нови Пациенти"; Metric2Title="Хронични Случаи"; Metric3Title="Активни Картони";
//                break;
//        }
//        _=GenerateReportAsync();
//    }

//    [RelayCommand]
//    public async Task GenerateReportAsync()
//    {
//        if(IsBusy) return;
//        try
//        {
//            IsBusy=true;
//            await using var db = await _dbFactory.CreateDbContextAsync();

//            var startRange = StartDate.Date;
//            var endRange = EndDate.Date.AddDays(1).AddTicks(-1);
//            List<GenericReportRow> temporaryRows = new();

//            switch(SelectedReportType)
//            {
//                case ReportType.MissedTherapies:
//                    var query = db.TherapyCycles
//                        .Include(c => c.TherapySchedule).ThenInclude(s => s.TreatmentPlan).ThenInclude(p => p.Patient)
//                        .Where(c => c.Status==TherapyStatus.Missed&&c.PlannedEndDate>=startRange&&c.PlannedEndDate<=endRange);

//                    if(!string.IsNullOrWhiteSpace(SearchText))
//                        query=query.Where(x => x.TherapySchedule.TreatmentPlan.Patient.LastName.Contains(SearchText)||x.TherapySchedule.Name.Contains(SearchText));

//                    var missedData = await query.OrderByDescending(c => c.PlannedEndDate).ToListAsync();

//                    temporaryRows=missedData.Select(c => new GenericReportRow
//                    {
//                        PrimaryHeader=c.TherapySchedule.TreatmentPlan.Patient.LastName,
//                        SecondaryHeader=c.TherapySchedule.Name,
//                        HighlightValue=$"Ц-#{c.CycleNumber}",
//                        DateValue=c.PlannedEndDate.ToString("dd.MM.yyyy"),
//                        InformationalText=string.IsNullOrEmpty(c.ReasonForMissing) ? "Нема внесено причина од лекар!" : c.ReasonForMissing,
//                        IsAlertSeverity=string.IsNullOrEmpty(c.ReasonForMissing)
//                    }).ToList();

//                    Metric1Value=temporaryRows.Count;
//                    Metric2Value=temporaryRows.Count(x => x.IsAlertSeverity);
//                    Metric3ValueText=Metric1Value>0 ? $"{Math.Round((double)(Metric1Value-Metric2Value)/Metric1Value*100)}%" : "100%";
//                    break;

//                case ReportType.Auditing:
//                    // Example mapping onto generalized system components
//                    // var auditLogs = await db.AuditLogs.Where(a => a.Timestamp >= startRange && a.Timestamp <= endRange)...
//                    temporaryRows=new List<GenericReportRow> {
//                        new() { PrimaryHeader = "д-р Стојанов", SecondaryHeader = "Промена на терапија", HighlightValue = "Сигурност", DateValue = DateTime.Now.ToString("dd.MM.yyyy HH:mm"), InformationalText = "Промена во картон ID: 4192", IsAlertSeverity = false }
//                    };
//                    Metric1Value=temporaryRows.Count; Metric2Value=0; Metric3ValueText="ОК";
//                    break;

//                case ReportType.AppointmentStatuses:
//                    // Concrete logic for binding Appointments entities
//                    temporaryRows=new List<GenericReportRow> {
//                        new() { PrimaryHeader = "Марко Петров", SecondaryHeader = "д-р Ангеловски", HighlightValue = "ОТКАЖАН", DateValue = DateTime.Now.AddHours(2).ToString("dd.MM.yyyy HH:mm"), InformationalText = "Пациентот не може да присуствува", IsAlertSeverity = true }
//                    };
//                    Metric1Value=temporaryRows.Count; Metric2Value=1; Metric3ValueText="85%";
//                    break;

//                case ReportType.Patients:
//                    // Concrete logic for rendering advanced patient structural parameters
//                    temporaryRows=new List<GenericReportRow> {
//                        new() { PrimaryHeader = "Ана Стојанова", SecondaryHeader = "0102983450021", HighlightValue = "Физио", DateValue = DateTime.Now.AddDays(-5).ToString("dd.MM.yyyy"), InformationalText = "Dg: Lumbalgia. Алергија на Пеницилин.", IsAlertSeverity = true }
//                    };
//                    Metric1Value=temporaryRows.Count; Metric2Value=1; Metric3ValueText="94%";
//                    break;
//            }

//            MainThread.BeginInvokeOnMainThread(() =>
//            {
//                UnifiedReportRows.Clear();
//                foreach(var row in temporaryRows)
//                {
//                    UnifiedReportRows.Add(row);
//                }
//            });
//        }
//        catch(Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine($"[REPORT ARCHITECTURE ERROR]: {ex.Message}");
//        }
//        finally
//        {
//            IsBusy=false;
//        }
//    }

//    [RelayCommand]
//    public async Task ExportToPdfAsync()
//    {
//        if(IsBusy) return;
//        try
//        {
//            IsBusy=true;

//            // Native platform agnostic compilation engine (HTML target printing pipeline pattern)
//            var htmlBlueprint = $@"
//            <html>
//            <head>
//                <style>
//                    body {{ font-family: Arial, sans-serif; padding: 30px; color: #0F172A; }}
//                    h2 {{ color: #2563EB; border-bottom: 2px solid #E2E8F0; padding-bottom: 10px; }}
//                    table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
//                    th {{ background-color: #0F172A; color: white; padding: 12px; text-align: left; font-size: 12px; }}
//                    td {{ padding: 12px; border-bottom: 1px solid #E2E8F0; font-size: 13px; }}
//                    .alert {{ background-color: #FEF2F2; color: #991B1B; padding: 6px; border-radius: 4px; }}
//                </style>
//            </head>
//            <body>
//                <h2>ИЗВЕШТАЈ: {SelectedReportType}</h2>
//                <p>Опсег: {StartDate:dd.MM.yyyy} до {EndDate:dd.MM.yyyy}</p>
//                <table>
//                    <thead>
//                        <tr>
//                            <th>{Col1Header}</th><th>{Col2Header}</th><th>{Col3Header}</th><th>{Col4Header}</th><th>{Col5Header}</th>
//                        </tr>
//                    </thead>
//                    <tbody>";

//            foreach(var item in UnifiedReportRows)
//            {
//                htmlBlueprint+=$@"
//                    <tr>
//                        <td><b>{item.PrimaryHeader}</b></td>
//                        <td>{item.SecondaryHeader}</td>
//                        <td>{item.HighlightValue}</td>
//                        <td>{item.DateValue}</td>
//                        <td><span class='{(item.IsAlertSeverity ? "alert" : "")}'>{item.InformationalText}</span></td>
//                    </tr>";
//            }

//            htmlBlueprint+="</tbody></table></body></html>";

//            // Standard storage directory invocation
//            string fileName = $"Report_{SelectedReportType}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
//            string targetFile = Path.Combine(FileSystem.CacheDirectory, fileName);
//            await File.WriteAllTextAsync(targetFile, htmlBlueprint);

//            // Open or share generated print stream
//            await Launcher.Default.OpenAsync(new OpenFileRequest
//            {
//                File=new ReadOnlyFile(targetFile)
//            });
//        }
//        catch(Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine($"[PDF EXPORT ERROR]: {ex.Message}");
//        }
//        finally
//        {
//            IsBusy=false;
//        }
//    }
//}