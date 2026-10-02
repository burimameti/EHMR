using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Entities.Reports;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Resources.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace EHMR.ViewModels;

public partial class ReportViewModel : BaseViewModel<DynamicReportRow>
{
    #region Services
    private readonly IReportHistoryService _reportHistoryService;
    private readonly ReportRegistry _registry;
    private readonly IReportExportService _reportExportService;
    private IReportProvider? _activeProvider;
    private ReportDefinition? _activeReport;
    private int _generationId;
    private CancellationTokenSource? _generateCts;
    [ObservableProperty]
    private string activeReportTitle = string.Empty;
    private readonly List<ReportDefinition> _allReports = new();
    [ObservableProperty]
    private string activeReportIcon = string.Empty;
    #endregion

    #region Hub

    [ObservableProperty]
    private bool isShowingDetails;

    [ObservableProperty]
    private string hubSearchText = string.Empty;

    public ObservableCollection<ReportDefinition> AvailableReports { get; } = new();

    public ObservableCollection<string> CategoryOptions { get; } = new();

    public ObservableCollection<string> TypeOptions { get; } = new();

    #endregion

    #region Filters

    [ObservableProperty]
    private string selectedCategory = "Сите";

    [ObservableProperty]
    private string selectedType = "Сите";

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private bool isMedicineSearchEnabled;

    public bool IsPatientsReport => _activeReport?.Type==ReportType.Patients;

    [ObservableProperty]
    private DateTime startDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime endDate = DateTime.Today;

    #endregion
    #region Period Selection

    public ObservableCollection<string> PeriodTypeOptions
    {
        get;
    } = new()
    {
        "Месечно", "Квартално", "Полугодишно", "Годишно", "Прилагодено"
    };

    [ObservableProperty]
    private string selectedPeriodTypeLabel = "Месечно";

    public bool IsCustomPeriod => SelectedPeriodTypeLabel=="Прилагодено";

    partial void OnSelectedPeriodTypeLabelChanged(string value)
    {
        OnPropertyChanged(nameof(IsCustomPeriod));
        RecalculatePeriodRange();
    }

    private void RecalculatePeriodRange()
    {
        var today = DateTime.Today;

        switch(SelectedPeriodTypeLabel)
        {
            case "Месечно":
                StartDate=new DateTime(today.Year, today.Month, 1);
                EndDate=StartDate.AddMonths(1).AddDays(-1);
                break;

            case "Квартално":
                var q = (today.Month-1)/3;
                StartDate=new DateTime(today.Year, q*3+1, 1);
                EndDate=StartDate.AddMonths(3).AddDays(-1);
                break;

            case "Полугодишно":
                StartDate=today.Month<=6
                    ? new DateTime(today.Year, 1, 1)
                    : new DateTime(today.Year, 7, 1);
                EndDate=StartDate.AddMonths(6).AddDays(-1);
                break;

            case "Годишно":
                StartDate=new DateTime(today.Year, 1, 1);
                EndDate=new DateTime(today.Year, 12, 31);
                break;

            case "Прилагодено":
                return;
        }
    }

    [RelayCommand]
    private async Task ApplyPeriodAndRegenerateAsync()
    {
        await GenerateReportAsync();
    }

    #endregion
    #region Hub KPI

    [ObservableProperty]
    private int hubTotalTemplates;

    [ObservableProperty]
    private int hubTotalCategories;

    [ObservableProperty]
    private int hubGeneratedToday;

    [ObservableProperty]
    private int hubFailedToday;

    #endregion

    #region Hub Grid - Templates

    [ObservableProperty]
    private ObservableCollection<SparkGridColumn> templateColumns = new();

    [ObservableProperty]
    private ObservableCollection<SparkGridRow> templateRows = new();

    [ObservableProperty]
    private int templateCurrentPage = 1;

    [ObservableProperty]
    private int templateTotalPages = 1;

    private const int TemplatePageSize = 6;

    private List<ReportDefinition> _filteredTemplates = new();

    #endregion

    #region Hub - History Activity Feed

    public ObservableCollection<SparkActivityItem> HistoryActivity { get; } = new();

    private readonly List<SparkActivityItem> _allHistoryActivity = new();

    [ObservableProperty]
    private int historyCurrentPage = 1;

    [ObservableProperty]
    private int historyTotalPages = 1;

    private const int HistoryPageSize = 8;

    #endregion

    #region Detail - Metrics

    [ObservableProperty]
    private string metric1Title = "Вкупно записи";

    [ObservableProperty]
    private int metric1Value;

    [ObservableProperty]
    private string metric2Title = "Критични";

    [ObservableProperty]
    private int metric2Value;

    [ObservableProperty]
    private string metric3Title = "Статистика";

    [ObservableProperty]
    private int metric3Value;

    #endregion

    #region Detail - Grid

    public ObservableCollection<DynamicReportRow> ProcessedRows { get; } = new();

    [ObservableProperty]
    private ObservableCollection<SparkGridColumn> gridColumns = new();

    [ObservableProperty]
    private ObservableCollection<SparkGridRow> gridRows = new();

    // Tabs доаѓа од BaseViewModel<T> — не се redeclara тука.

    protected override string ModuleName => Modules.Reports;

    #endregion

    public ReportViewModel(
        ReportRegistry registry,
        INavigationService navigationService,
        IReportExportService reportExportService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IReportHistoryService reportHistoryService,
        IAuthorizationService authService,
        ISelectedItemService<DynamicReportRow> selectedItemService)
        : base(navigationService, userDialogService, menuService, authService, selectedItemService)
    {
        _registry=registry;
        _reportHistoryService=reportHistoryService;
        _reportExportService=reportExportService;
        PageSize=10;

        BuildTemplateGridColumns();
        InitializeSparkControls();
        LoadReports();
        LoadHistorySeed();

        PropertyChanged+=OnViewModelPropertyChanged;

        EvaluatePermissions();
    }

    /// <summary>
    /// Opens the patient report dashboard directly. Other report providers remain
    /// registered for administration/system-auditing scenarios, but are not part
    /// of the standard Reports creation flow.
    /// </summary>
    public async Task OpenPatientsReportAsync()
    {
        var patientReport=_allReports.FirstOrDefault(x => x.Type==ReportType.Patients);
        if(patientReport is null)
            return;

        if(IsShowingDetails && _activeReport?.Type==ReportType.Patients)
            return;

        await SelectReport(patientReport);
    }

    // =====================================================
    // REPORT HUB
    // =====================================================

    private void LoadReports()
    {
        _allReports.Clear();

        _allReports.AddRange(
            _registry.Providers.Select(provider => new ReportDefinition
            {
                Key=provider.Key,
                Title=provider.Title,
                Description=provider.Description,
                Icon=provider.Icon,
                Category=provider.Category,
                Type=provider.Type,
                ProviderType=provider.GetType()
            }));

        BuildFilterOptions();
        RefreshAvailableReports();
    }

    private void BuildFilterOptions()
    {
        CategoryOptions.Clear();
        CategoryOptions.Add("Сите");

        foreach(var category in _allReports
            .Select(x => x.Category.ToString())
            .Distinct()
            .OrderBy(x => x))
        {
            CategoryOptions.Add(category);
        }

        TypeOptions.Clear();
        TypeOptions.Add("Сите");

        foreach(var type in _allReports
            .Select(x => x.Type.ToString())
            .Distinct()
            .OrderBy(x => x))
        {
            TypeOptions.Add(type);
        }
    }

    private void RefreshAvailableReports()
    {
        var reports = _allReports.AsEnumerable();

        if(!string.IsNullOrWhiteSpace(HubSearchText))
        {
            var search = HubSearchText.Trim();

            reports=reports.Where(x =>
                x.Title.Contains(search, StringComparison.OrdinalIgnoreCase)||
                x.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if(SelectedCategory!="Сите")
            reports=reports.Where(x => x.Category.ToString()==SelectedCategory);

        if(SelectedType!="Сите")
            reports=reports.Where(x => x.Type.ToString()==SelectedType);

        var filtered = reports.ToList();

        AvailableReports.Clear();

        foreach(var report in filtered)
            AvailableReports.Add(report);

        _filteredTemplates=filtered;
        TemplateCurrentPage=1;
        TemplateTotalPages=Math.Max(1,
            (int)Math.Ceiling(filtered.Count/(double)TemplatePageSize));

        RefreshTemplateGridRows();
        CalculateHubMetrics();
    }
    // =====================================================
    // EVENTS
    // =====================================================

    private static readonly HashSet<string> _hubFilterProperties =
   [
       nameof(HubSearchText),
    nameof(SelectedCategory),
    nameof(SelectedType)
   ];

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName is null)
            return;

        if(_hubFilterProperties.Contains(e.PropertyName))
        {
            RefreshAvailableReports();
            return;
        }

        if(e.PropertyName==nameof(SearchText))
        {
            ApplyPipeline();
        }
    }
    [RelayCommand]
    private async Task SelectReport(ReportDefinition? report)
    {
        if(report is null)
            return;

        ClearReportState();

        _activeReport=report;
        _activeProvider=_registry.Resolve(report.Key);

        if(_activeProvider is null)
            return;

        ActiveReportTitle=report.Title;
        ActiveReportIcon=report.Icon;
        OnPropertyChanged(nameof(IsPatientsReport));

        SearchText=string.Empty;

        SelectedPeriodTypeLabel="Месечно";
        RecalculatePeriodRange();

        InitializeSparkControls();

        _activeProvider.FiltersChanged+=OnProviderFiltersChanged;

        IsShowingDetails=true;

        await GenerateReportAsync();
    }

    partial void OnIsMedicineSearchEnabledChanged(bool value)
    {
        if(_activeProvider is PatientsReportProvider patientsProvider)
        {
            patientsProvider.SetMedicineFilterEnabled(value);
            InitializeSparkControls();
            _=GenerateReportAsync();
        }
    }

    private CancellationTokenSource? _filterDebounce;

    private async void OnProviderFiltersChanged()
    {
        _filterDebounce?.Cancel();
        _filterDebounce?.Dispose();

        _filterDebounce=new CancellationTokenSource();

        try
        {
            await Task.Delay(150, _filterDebounce.Token);

            await GenerateReportAsync();
        }
        catch(OperationCanceledException)
        {
        }
    }

    [RelayCommand]
    private async Task BackToHubAsync()
    {
        await NavigationService.GoToAsync(AppRoutes.Reports.List);
    }

    [RelayCommand]
    private void CreateNewTemplate()
    {
        // TODO: отвори wizard за нов report template кога ќе биде готов report builder-от.
    }

    // =====================================================
    // HUB - TEMPLATES GRID
    // =====================================================

    private void BuildTemplateGridColumns()
    {
        TemplateColumns.Clear();

        TemplateColumns.Add(new SparkGridColumn { Header="НАСЛОВ", Key="Title", Width=new GridLength(220) });
        TemplateColumns.Add(new SparkGridColumn { Header="КАТЕГОРИЈА", Key="Category", Width=new GridLength(150) });
        TemplateColumns.Add(new SparkGridColumn { Header="ТИП", Key="Type", Width=new GridLength(150) });
        TemplateColumns.Add(new SparkGridColumn { Header="ОПИС", Key="Description", Width=GridLength.Star });
        TemplateColumns.Add(new SparkGridColumn
        {
            Header="",
            Key="Action",
            CellType=SparkGridCellType.Actions,
            Width=new GridLength(140)
        });
    }

    private void RefreshTemplateGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();

        var page = _filteredTemplates
            .Skip((TemplateCurrentPage-1)*TemplatePageSize)
            .Take(TemplatePageSize);

        foreach(var template in page)
        {
            var row = new SparkGridRow { Tag=template };

            row["Title"]=template.Title;
            row["Category"]=template.Category.ToString();
            row["Type"]=template.Type.ToString();
            row["Description"]=template.Description;
            row["Action"]=new SparkButtonItem
            {
                Label="Генерирај",
                IsPrimary=true,
                Command=SelectReportCommand,
                CommandParameter=template
            };

            rows.Add(row);
        }

        TemplateRows=rows;
    }

    [RelayCommand]
    private void TemplateNextPage()
    {
        if(TemplateCurrentPage>=TemplateTotalPages)
            return;

        TemplateCurrentPage++;
        RefreshTemplateGridRows();
    }

    [RelayCommand]
    private void TemplatePreviousPage()
    {
        if(TemplateCurrentPage<=1)
            return;

        TemplateCurrentPage--;
        RefreshTemplateGridRows();
    }

    // =====================================================
    // HUB - HISTORY ACTIVITY FEED
    // =====================================================

    private void LoadHistorySeed()
    {
        HistoryTotalPages=1;
        RefreshHistoryActivity();
    }

    private void RefreshHistoryActivity()
    {
        HistoryActivity.Clear();

        var page = _allHistoryActivity
            .Skip((HistoryCurrentPage-1)*HistoryPageSize)
            .Take(HistoryPageSize);

        foreach(var item in page)
            HistoryActivity.Add(item);
    }

    [RelayCommand]
    private void HistoryNextPage()
    {
        if(HistoryCurrentPage>=HistoryTotalPages)
            return;

        HistoryCurrentPage++;
        RefreshHistoryActivity();
    }

    [RelayCommand]
    private void HistoryPreviousPage()
    {
        if(HistoryCurrentPage<=1)
            return;

        HistoryCurrentPage--;
        RefreshHistoryActivity();
    }

    private void AddHistoryEntry(string format, bool succeeded)
    {
        if(_activeReport==null)
            return;

        var who = string.IsNullOrWhiteSpace(UserName) ? "Систем" : UserName;
        var statusText = succeeded ? "успешно генериран" : "неуспешно генериран";

        var activity = new SparkActivityItem
        {
            Title=$"{_activeReport.Title} — {format}",
            Timestamp=DateTime.Now.ToString("dd.MM.yyyy"),
            Description=$"{statusText} од {who}",
            IconGlyph=succeeded ? "\uf00c" : "\uf00d"
        };

        _allHistoryActivity.Insert(0, activity);

        HistoryTotalPages=Math.Max(1, (int)Math.Ceiling(_allHistoryActivity.Count/(double)HistoryPageSize));
        HistoryCurrentPage=1;

        RefreshHistoryActivity();
        CalculateHubMetrics();
    }

    // =====================================================
    // HUB KPI
    // =====================================================

    private void CalculateHubMetrics()
    {
        var todayPrefix = DateTime.Today.ToString("dd.MM.yyyy");

        HubTotalTemplates=_registry.Providers.Count();
        HubTotalCategories=_registry.Providers.Select(x => x.Category.ToString()).Distinct().Count();
        HubGeneratedToday=_allHistoryActivity.Count(x => x.Timestamp.StartsWith(todayPrefix));
        HubFailedToday=_allHistoryActivity.Count(x => x.IconGlyph=="\uf00d"&&x.Timestamp.StartsWith(todayPrefix));
    }

    // =====================================================
    // SPARK INITIALIZATION (Detail)
    // =====================================================
    private void InitializeSparkControls()
    {
        if(_activeProvider==null)
            return;
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
    }
    private void BuildSparkTabs()
    {
        Tabs.Clear();

        if(_activeProvider==null)
            return;

        foreach(var tab in _activeProvider.BuildTabs())
            Tabs.Add(tab);
    }

    private void BuildSparkPickers()
    {
        Pickers.Clear();

        if(_activeProvider==null)
            return;

        foreach(var picker in _activeProvider.BuildPickers())
            Pickers.Add(picker);
    }

    protected override void BuildSparkButtons()
    {
        Buttons.Clear();

        if(_activeProvider==null)
            return;

        foreach(var button in _activeProvider.BuildButtons())
            Buttons.Add(button);
    }

    private void BuildSparkGridColumns()
    {
        int width = 130;
        GridColumns.Clear();

        if(_activeProvider==null)
            return;

        foreach(var column in _activeProvider.Columns)
            GridColumns.Add(column);


    }

    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();
        foreach(var reportRow in ProcessedRows)
        {
            var row = new SparkGridRow { Tag=reportRow };
            for(int i = 0; i<reportRow.Cells.Count&&i<GridColumns.Count; i++)
            {
                var key = GridColumns[i].Key;
                var value = reportRow.Cells[i];

                row[key]=key switch
                {
                    "NationalId" => PrivacyMaskHelper.MaskNationalId(value?.ToString()),
                   // "Phone" => PrivacyMaskHelper.MaskPhone(value?.ToString()),
                    _ => value
                };
            }

            rows.Add(row);
        }
        GridRows=rows;
    }

    private void CalculateMetrics()
    {
        if(_activeProvider==null)
            return;

        var metrics = _activeProvider.CalculateMetrics(AllItems);

        Metric1Title=metrics.Title1;
        Metric1Value=metrics.Value1;

        Metric2Title=metrics.Title2;
        Metric2Value=metrics.Value2;

        Metric3Title=metrics.Title3;
        Metric3Value=metrics.Value3;
    }

    // =====================================================
    // PIPELINE (Detail)
    // =====================================================

    protected override IEnumerable<DynamicReportRow> ApplySearch(IEnumerable<DynamicReportRow> items, string search)
    {
        if(string.IsNullOrWhiteSpace(search))
            return items;

        return items.Where(row =>
            row.Cells.Any(cell => cell?.ToString()?.Contains(search, StringComparison.OrdinalIgnoreCase)==true));
    }

    protected override IEnumerable<DynamicReportRow> ApplyFilters(IEnumerable<DynamicReportRow> items) => items;

    protected override IEnumerable<DynamicReportRow> ApplySort(IEnumerable<DynamicReportRow> items) => items;

    protected override void ResetFilters()
    {
        SearchText=string.Empty;
    }

    protected override void OnPageProjected(ObservableCollection<DynamicReportRow> page)
    {
        ProcessedRows.Clear();

        foreach(var row in page)
            ProcessedRows.Add(row);

        RefreshSparkGridRows();
    }

    // =====================================================
    // REPORT GENERATION
    // =====================================================

    private async Task GenerateReportAsync()
    {
        if(_activeProvider==null)
            return;

        _generateCts?.Cancel();
        _generateCts?.Dispose();

        _generateCts=new CancellationTokenSource();

        var token = _generateCts.Token;
        var generation = ++_generationId;

        try
        {
            await ExecuteSafeAsync(async () =>
            {
                var rows = await _activeProvider.GenerateAsync(
                    StartDate.Date,
                    EndDate.Date.AddDays(1));

                if(token.IsCancellationRequested)
                    return;

                if(generation!=_generationId)
                    return;

                AllItems=rows;

                ApplyPipeline();

                CalculateMetrics();

            }, "Грешка при генерирање извештај");
        }
        catch(OperationCanceledException)
        {
        }
    }



    // =====================================================
    // EXPORT
    // =====================================================
    // Во ViewModel-от додај динамичко генерирање на наслов:

    public string GetDynamicReportTitle()
    {
        var titleBuilder = new List<string>();

        // 1. Период префикс
        string periodPrefix = SelectedPeriodTypeLabel switch
        {
            "Месечно" => "Месечен извештај",
            "Квартално" => "Квартален извештај",
            "Полугодишно" => "Полугодишен извештај",
            "Годишно" => "Годишен извештај",
            _ => "Периодичен извештај"
        };

        titleBuilder.Add(periodPrefix);

        // 2. Основен наслов за категоријата
        string baseEntity = _activeReport?.Type==ReportType.Patients ? "за Пациенти" : ActiveReportTitle;
        titleBuilder.Add(baseEntity);

        // 3. Активни филтри според Placeholder
        var activeFilters = new List<string>();

        foreach(var picker in Pickers)
        {
            if(picker.SelectedItem!=null&&
                !string.IsNullOrWhiteSpace(picker.SelectedItem.ToString())&&
                !picker.SelectedItem.ToString()!.Equals("Сите", StringComparison.OrdinalIgnoreCase))
            {
                var selectedVal = picker.SelectedItem.ToString();

                // Наместо picker.Label, директно го земаме picker.Placeholder
                string filterType = picker.Placeholder??string.Empty;

                if(filterType.Contains("Пол", StringComparison.OrdinalIgnoreCase))
                {
                    activeFilters.Add($"по пол ({selectedVal})");
                }
                else if(filterType.Contains("Реуматолог", StringComparison.OrdinalIgnoreCase)||
                         filterType.Contains("Лекар", StringComparison.OrdinalIgnoreCase)||
                         filterType.Contains("Реуматолог", StringComparison.OrdinalIgnoreCase))
                {
                    activeFilters.Add($"за реуматолог {selectedVal}");
                }
                else if(filterType.Contains("Дијагноза", StringComparison.OrdinalIgnoreCase))
                {
                    activeFilters.Add($"по дијагноза ({selectedVal})");
                }
                else if(filterType.Contains("Град", StringComparison.OrdinalIgnoreCase))
                {
                    activeFilters.Add($"од град {selectedVal}");
                }
                else if(filterType.Contains("Лек", StringComparison.OrdinalIgnoreCase))
                {
                    activeFilters.Add($"со лек {selectedVal}");
                }
                else if(!string.IsNullOrWhiteSpace(filterType))
                {
                    activeFilters.Add($"по {filterType.ToLower()}: {selectedVal}");
                }
            }
        }

        if(activeFilters.Count>0)
        {
            titleBuilder.Add(string.Join(" ", activeFilters));
        }

        return string.Join(" ", titleBuilder);
    }
    [RelayCommand]
    private async Task ExecuteReportGenerationAsync()
    {
        if(_activeProvider==null || IsBusy)
            return;

        var confirmed=await UserDialogService.ShowConfirmationAsync(
            "Генерирање извештај",
            "Генерирањето на извештајот може да трае до 30 секунди. По истекот на периодот автоматски ќе се генерираат PDF и Excel датотеките. Ве молиме не ја затворајте страницата додека трае процесот.",
            "Генерирај",
            "Откажи");

        if(!confirmed)
            return;

        await ExecuteSafeAsync(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30));

            if(_activeProvider==null)
                return;

            var rows=await _activeProvider.GenerateAsync(
                StartDate.Date,
                EndDate.Date.AddDays(1));

            AllItems=rows;
            ApplyPipeline();
            CalculateMetrics();

            var dynamicTitle=GetDynamicReportTitle();
            var currentUser=string.IsNullOrWhiteSpace(UserName) ? "Систем" : UserName;
            var exportRows=BuildExportRows();

            var pdfPath=await _reportExportService.ExportToPdfAsync(
                reportTitle: dynamicTitle,
                insitutionName: "КЛИНИКА ЗА РЕУМАТОЛОГИЈА - СКОПЈЕ",
                generatedBy: currentUser,
                startDate: StartDate,
                endDate: EndDate,
                columns: GridColumns.ToList(),
                rows: exportRows,
                selectedMedicine: (_activeProvider as PatientsReportProvider)?.SelectedMedicineForExport,
                selectedMedicineTotalQuantity: (_activeProvider as PatientsReportProvider)?.SelectedMedicineTotalQuantityForExport);

            var pdfInfo=new FileInfo(pdfPath);

            await _reportHistoryService.AddAsync(new ReportHistory
            {
                ReportKey=_activeProvider.Key,
                ReportTitle=dynamicTitle,
                Format="PDF",
                FileName=Path.GetFileName(pdfPath),
                FilePath=pdfPath,
                FileSize=pdfInfo.Length,
                GeneratedBy=currentUser,
                Success=true,
                MachineName=Environment.MachineName,
                StartDate=StartDate,
                EndDate=EndDate
            });

            AddHistoryEntry("PDF", succeeded:true);

            await UserDialogService.ShowAlertAsync(
                "Извештајот е генериран",
                "Извештајот е успешно генериран во PDF формат.",
                "ОК");
        }, "Грешка при генерирање извештај");
    }

    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        if(_activeProvider==null)
            return;

        var dynamicTitle = GetDynamicReportTitle();
        var currentUser = !string.IsNullOrWhiteSpace(UserName) ? UserName : "Марко Марковски";

        await ExecuteSafeAsync(async () =>
        {
            try
            {
                var path = await _reportExportService.ExportToPdfAsync(
                    reportTitle: dynamicTitle,
                    insitutionName: "КЛИНИКА ЗА РЕУМАТОЛОГИЈА - СКОПЈЕ",
                    generatedBy: currentUser,
                    startDate: StartDate,
                    endDate: EndDate,
                    columns: GridColumns.ToList(),
                    rows: GridRows.ToList(),
                    selectedMedicine: (_activeProvider as PatientsReportProvider)?.SelectedMedicineForExport,
                    selectedMedicineTotalQuantity: (_activeProvider as PatientsReportProvider)?.SelectedMedicineTotalQuantityForExport
                );

                await UserDialogService.ShowAlertAsync("Успешно", $"Извештајот е зачуван на: {path}", "ОК");

                await _reportHistoryService.AddAsync(new ReportHistory
                {
                    ReportKey=_activeProvider.Key,
                    ReportTitle=dynamicTitle,
                    Format="PDF",
                    FileName=Path.GetFileName(path),
                    FilePath=path,
                    FileSize=new FileInfo(path).Length,
                    GeneratedBy=currentUser,
                    Success=true,
                    MachineName=Environment.MachineName,
                    StartDate=StartDate,
                    EndDate=EndDate
                });

                AddHistoryEntry("PDF", succeeded: true);
            }
            catch
            {
                AddHistoryEntry("PDF", succeeded: false);
                throw;
            }
        }, "Грешка при PDF export");
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        if(_activeProvider==null)
            return;

        await ExecuteSafeAsync(async () =>
        {
            try
            {
                var path = await _reportExportService.ExportToExcelAsync(
                    ActiveReportTitle,
                    GridColumns.ToList(),
                    BuildExportRows());

                await UserDialogService.ShowAlertAsync(
                    "Excel Export",
                    $"Извештајот е зачуван: {path}",
                    "OK");

                await _reportHistoryService.AddAsync(new ReportHistory
                {
                    ReportKey=_activeProvider.Key,
                    ReportTitle=ActiveReportTitle,
                    Format="EXCEL",
                    FileName=Path.GetFileName(path),
                    FilePath=path,
                    FileSize=new FileInfo(path).Length,
                    GeneratedBy=UserName,
                    Success=true,
                    MachineName=Environment.MachineName,
                    StartDate=StartDate,
                    EndDate=EndDate
                });
            }
            catch
            {
                AddHistoryEntry("Excel", succeeded: false);
                throw;
            }
        }, "Грешка при Excel export");
    }

    private List<SparkGridRow> BuildExportRows()
    {
        var rows = new List<SparkGridRow>();

        foreach(var reportRow in FilteredItems)
        {
            var row = new SparkGridRow { Tag=reportRow };

            for(int i = 0; i<reportRow.Cells.Count&&i<GridColumns.Count; i++)
                row[GridColumns[i].Key]=reportRow.Cells[i];

            rows.Add(row);
        }

        return rows;
    }

    // =====================================================
    // RESET / DISPOSE
    // =====================================================

    public void ClearReportState()
    {
        IsShowingDetails=false;

        if(_activeProvider is not null)
        {
            _activeProvider.FiltersChanged-=OnProviderFiltersChanged;

            if(_activeProvider is PatientsReportProvider patientsProvider)
                patientsProvider.SetMedicineFilterEnabled(false);
        }

        _activeProvider=null;
        _activeReport=null;
        IsMedicineSearchEnabled=false;
        OnPropertyChanged(nameof(IsPatientsReport));

        ActiveReportTitle=string.Empty;
        ActiveReportIcon=string.Empty;

        Tabs.Clear();
        Pickers.Clear();
        Buttons.Clear();

        GridColumns.Clear();
        GridRows.Clear();

        ProcessedRows.Clear();

        ResetPipeline();
    }
    // Базата има non-virtual Dispose(), па ова не е вистински override.
    // 'new' е тука само за да не крие тивко (CS0108); disposal преку

    // BaseViewModel<T> или IDisposable референца НЕМА да го повика ова.
    // Препорака: смени во базата 'public void Dispose()' -> 'public virtual void Dispose()'.
    public override void Dispose()
    {
        PropertyChanged-=OnViewModelPropertyChanged;

        if(_activeProvider!=null)
            _activeProvider.FiltersChanged-=OnProviderFiltersChanged;

        _generateCts?.Cancel();
        _generateCts?.Dispose();

        _filterDebounce?.Cancel();
        _filterDebounce?.Dispose();

        base.Dispose();
    }
}