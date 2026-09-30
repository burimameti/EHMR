using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Domain.Entities.Reports;

/// <summary>
/// Reports patient medicine quantities recorded during a selected period.
/// One row represents one patient + one medicine; quantities are summed so
/// repeated prescriptions/records for the same patient and medicine are combined.
/// </summary>
public sealed class MedicineUsageReportProvider : IReportProvider
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private string _selectedMedicineFilter = "Сите";
    private SparkPickerItem? _medicinePicker;
    private bool _refreshingPicker;

    public MedicineUsageReportProvider(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
        => _dbFactory=dbFactory;

    public string Key => "medicine-usage";
    public string Title => "Потрошувачка на лекови";
    public string Description => "Пациенти по лек и вкупна евидентирана количина за избраниот период.";
    public string Icon => "💊";
    public ReportType Type => ReportType.MedicineUsage;
    public ReportCategory Category => ReportCategory.Clinical;

    public event Action? FiltersChanged;

    public IEnumerable<SparkGridColumn> Columns =>
    [
        new() { Header="ПАЦИЕНТ", Key="Patient", Width=new GridLength(190) },
        new() { Header="ЛЕК", Key="Medicine", Width=new GridLength(190) },
        new() { Header="ГЕНЕРИЧКО ИМЕ", Key="GenericName", Width=new GridLength(170) },
        new() { Header="ШИФРА", Key="Code", Width=new GridLength(110) },
        new() { Header="РЕЖИМ", Key="Regime", Width=new GridLength(170) },
        new() { Header="КОЛИЧИНА", Key="Quantity", Width=new GridLength(110) },
        new() { Header="СКОР", Key="Score", Width=new GridLength(120) },
        new() { Header="ПЕРИОД", Key="Period", Width=new GridLength(170) }
    ];

    public IEnumerable<SparkTabItem> BuildTabs()
    {
        return [new SparkTabItem { Title="Сите", Value="0", IsSelected=true }];
    }

    public IEnumerable<SparkPickerItem> BuildPickers()
    {
        _medicinePicker=new SparkPickerItem { Placeholder="Лек" };
        _medicinePicker.Items.Add("Сите");
        _medicinePicker.SelectedItem=_selectedMedicineFilter;
        _medicinePicker.PropertyChanged+=(_, e) =>
        {
            if(_refreshingPicker || e.PropertyName!=nameof(SparkPickerItem.SelectedItem))
                return;

            if(_medicinePicker.SelectedItem is string value)
            {
                _selectedMedicineFilter=value;
                FiltersChanged?.Invoke();
            }
        };
        return [_medicinePicker];
    }

    public IEnumerable<SparkButtonItem> BuildButtons()
    {
        return
        [
            new SparkButtonItem
            {
                Label="Освежи",
                IsPrimary=true,
                Command=new CommunityToolkit.Mvvm.Input.RelayCommand(() => FiltersChanged?.Invoke())
            }
        ];
    }

    public async Task<List<DynamicReportRow>> GenerateAsync(DateTime from, DateTime to)
    {
        await using var db=await _dbFactory.CreateDbContextAsync();

        var medicines=await db.PatientMedicines
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Medicine)
            .Include(x => x.ApplicationRegime)
            .Where(x => x.StartDate < to && (x.EndDate == null || x.EndDate >= from))
            .ToListAsync();

        var scores=await db.PatientScores
            .AsNoTracking()
            .Where(x => medicines.Select(m => m.PatientId).Contains(x.PatientId))
            .ToListAsync();

        var scoreByPatient=scores
            .GroupBy(x => x.PatientId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.RecordedAt).First().ScoreText);

        var grouped=medicines
            .Where(x => x.Patient!=null && x.Medicine!=null)
            .GroupBy(x => new
            {
                x.PatientId,
                MedicineId=x.MedicineId
            })
            .Select(g =>
            {
                var first=g.OrderBy(x => x.StartDate).First();
                var quantity=g.Sum(x => x.Quantity);
                var score=scoreByPatient.TryGetValue(g.Key.PatientId, out var value)
                    ? value
                    : "Нема скор";

                return new DynamicReportRow
                {
                    Cells=
                    [
                        first.Patient!.FullName,
                        first.Medicine!.Name,
                        first.Medicine.GenericName,
                        first.Medicine.Code,
                        first.ApplicationRegime?.Regime ?? "Нема режим",
                        quantity.ToString("0.##"),
                        score,
                        $"{from:dd.MM.yyyy} - {to.AddDays(-1):dd.MM.yyyy}"
                    ],
                    IsAlertSeverity=false
                };
            })
            .ToList();

        RefreshMedicinePicker(grouped);

        if(_selectedMedicineFilter!="Сите")
            grouped=grouped
                .Where(x => string.Equals(x.Cells[1], _selectedMedicineFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

        return grouped;
    }

    private void RefreshMedicinePicker(IEnumerable<DynamicReportRow> rows)
    {
        if(_medicinePicker==null)
            return;

        _refreshingPicker=true;
        try
        {
            var selected=_selectedMedicineFilter;
            _medicinePicker.Items.Clear();
            _medicinePicker.Items.Add("Сите");
            foreach(var medicine in rows.Select(x => x.Cells[1]).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
                _medicinePicker.Items.Add(medicine);
            _medicinePicker.SelectedItem=_medicinePicker.Items.Contains(selected) ? selected : "Сите";
            _selectedMedicineFilter=_medicinePicker.SelectedItem?.ToString() ?? "Сите";
        }
        finally
        {
            _refreshingPicker=false;
        }
    }

    public ReportMetrics CalculateMetrics(IEnumerable<DynamicReportRow> rows)
    {
        var list=rows.ToList();
        decimal quantity=0;
        foreach(var row in list)
            if(decimal.TryParse(row.Cells[5], out var value)) quantity+=value;

        return new ReportMetrics
        {
            Title1="Пациенти",
            Value1=list.Select(x => x.Cells[0]).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            Title2="Лекови",
            Value2=list.Select(x => x.Cells[1]).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            Title3="Вкупна количина",
            Value3=quantity > int.MaxValue ? int.MaxValue : (int)quantity
        };
    }
}
