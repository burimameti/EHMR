using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Domain.Entities.Reports;

/// <summary>
/// Reports total medicine quantities recorded during the selected period.
/// One row represents one medicine across all patients.
/// </summary>
public sealed class MedicineUsageReportProvider : IReportProvider
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private string _selectedMedicineFilter="Сите";
    private SparkPickerItem? _medicinePicker;
    private bool _refreshingPicker;

    public MedicineUsageReportProvider(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
        => _dbFactory=dbFactory;

    public string Key=>"medicine-usage";
    public string Title=>"Потрошувачка на лекови";
    public string Description=>"Вкупни количини по лек за избраниот период.";
    public string Icon=>"💊";
    public ReportType Type=>ReportType.MedicineUsage;
    public ReportCategory Category=>ReportCategory.Clinical;

    public event Action? FiltersChanged;

    public IEnumerable<SparkGridColumn> Columns =>
    [
        new() { Header="ЛЕК", Key="Medicine", Width=new GridLength(220) },
        new() { Header="ГЕНЕРИЧКО ИМЕ", Key="GenericName", Width=new GridLength(190) },
        new() { Header="ШИФРА", Key="Code", Width=new GridLength(120) },
        new() { Header="ВКУПНА КОЛИЧИНА", Key="Quantity", Width=new GridLength(150) },
        new() { Header="ПЕРИОД", Key="Period", Width=GridLength.Star }
    ];

    public IEnumerable<SparkTabItem> BuildTabs()
        => [new SparkTabItem { Title="Сите", Value="0", IsSelected=true }];

    public IEnumerable<SparkPickerItem> BuildPickers()
    {
        _medicinePicker=new SparkPickerItem { Placeholder="Лек" };
        _medicinePicker.Items.Add("Сите");
        _medicinePicker.SelectedItem=_selectedMedicineFilter;

        _medicinePicker.PropertyChanged+=(_, e) =>
        {
            if(_refreshingPicker||e.PropertyName!=nameof(SparkPickerItem.SelectedItem))
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
            .Include(x => x.Medicine)
            .Where(x => x.Medicine!=null)
            .ToListAsync();

        var grouped=medicines
            .GroupBy(x => x.MedicineId)
            .Select(g =>
            {
                var first=g.First();

                return new DynamicReportRow
                {
                    Cells=
                    [
                        first.Medicine!.Name,
                        first.Medicine.GenericName,
                        first.Medicine.Code,
                        g.Sum(x => x.Quantity).ToString("0.################"),
                        $"{from:dd.MM.yyyy} - {to.AddDays(-1):dd.MM.yyyy}"
                    ],
                    IsAlertSeverity=false
                };
            })
            .OrderBy(x => x.Cells[0])
            .ToList();

        RefreshMedicinePicker(grouped);

        if(_selectedMedicineFilter!="Сите")
        {
            grouped=grouped
                .Where(x => string.Equals(
                    x.Cells[0],
                    _selectedMedicineFilter,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

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

            foreach(var medicine in rows
                .Select(x => x.Cells[0])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x))
            {
                _medicinePicker.Items.Add(medicine);
            }

            _medicinePicker.SelectedItem=
                _medicinePicker.Items.Contains(selected)
                    ? selected
                    : "Сите";

            _selectedMedicineFilter=_medicinePicker.SelectedItem?.ToString()??"Сите";
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
        {
            if(decimal.TryParse(
                row.Cells[3],
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value))
            {
                quantity+=value;
            }
        }

        return new ReportMetrics
        {
            Title1="Лекови",
            Value1=list.Count,
            Title2="Вкупна количина",
            Value2=quantity>int.MaxValue ? int.MaxValue : (int)quantity,
            Title3="Период",
            Value3=1
        };
    }
}