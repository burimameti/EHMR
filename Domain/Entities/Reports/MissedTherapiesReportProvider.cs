using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Domain.Entities.Reports
{
    public sealed class MissedTherapiesReportProvider : IReportProvider
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

        // единствен извор на вистина за reason филтерот
        private string _selectedReasonFilter = "Сите";

        private SparkTabItem? _allTab, _withReasonTab, _withoutReasonTab;
        private SparkPickerItem? _reasonPicker;

        public event Action? FiltersChanged;

        public MissedTherapiesReportProvider(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
        {
            _dbFactory=dbFactory;
        }

        public string Key => "missed-therapies";
        public string Title => "Пропуштени терапии";
        public string Description => "Циклуси означени како пропуштени во избраниот период.";
        public string Icon => "⏱️";
        public ReportType Type => ReportType.MissedTherapies;
        public ReportCategory Category => ReportCategory.Clinical;

        public IEnumerable<SparkGridColumn> Columns =>
        [
            new() { Header = "ПАЦИЕНТ ", Key = "Patient", Width = new GridLength(220) },
            new() { Header = "МАТИЧЕН БРОЈ", Key = "NationalId", Width = new GridLength(170) },
            new() { Header = "ЦИКЛУС", Key = "Cycle", Width = new GridLength(130) },
            new() { Header = "ДАТУМ", Key = "Date", Width = new GridLength(150) },
            new() { Header = "ЗАБЕЛЕШКА", Key = "Note", Width = GridLength.Star }
        ];

        // =====================================================
        // TABS
        // =====================================================
        public IEnumerable<SparkTabItem> BuildTabs()
        {
            _allTab=new SparkTabItem { Title="Сите", Value="0", IsSelected=true };
            _withReasonTab=new SparkTabItem { Title="Со причина", Value="0" };
            _withoutReasonTab=new SparkTabItem { Title="Без причина", Value="0" };

            _allTab.Command=new RelayCommand(() => SelectReason(_allTab, "Сите"));
            _withReasonTab.Command=new RelayCommand(() => SelectReason(_withReasonTab, "Со причина"));
            _withoutReasonTab.Command=new RelayCommand(() => SelectReason(_withoutReasonTab, "Без причина"));

            return [_allTab, _withReasonTab, _withoutReasonTab];
        }

        private void SelectReason(SparkTabItem tab, string reason)
        {
            foreach(var t in new[] { _allTab, _withReasonTab, _withoutReasonTab })
                if(t!=null) t.IsSelected=false;

            tab.IsSelected=true;
            _selectedReasonFilter=reason;

            if(_reasonPicker!=null)
                _reasonPicker.SelectedItem=reason;

            FiltersChanged?.Invoke();
        }

        // =====================================================
        // PICKER
        // =====================================================
        public IEnumerable<SparkPickerItem> BuildPickers()
        {
            _reasonPicker=new SparkPickerItem { Placeholder="Причина" };

            foreach(var item in new[] { "Сите", "Со причина", "Без причина" })
                _reasonPicker.Items.Add(item);

            _reasonPicker.SelectedItem=_selectedReasonFilter;

            _reasonPicker.PropertyChanged+=(_, e) =>
            {
                if(e.PropertyName==nameof(SparkPickerItem.SelectedItem)&&_reasonPicker.SelectedItem is string s)
                {
                    _selectedReasonFilter=s;
                    SyncTabsFromPicker(s);
                    FiltersChanged?.Invoke();
                }
            };

            return [_reasonPicker];
        }

        private void SyncTabsFromPicker(string reason)
        {
            if(_allTab==null) return;

            var mappedTab = reason switch
            {
                "Сите" => _allTab,
                "Со причина" => _withReasonTab,
                "Без причина" => _withoutReasonTab,
                _ => _allTab
            };

            foreach(var t in new[] { _allTab, _withReasonTab, _withoutReasonTab })
                if(t!=null) t.IsSelected=false;

            mappedTab!.IsSelected=true;
        }

        // =====================================================
        // BUTTON — тргнато дупликат "Експорт", заменето со "Освежи"
        // =====================================================
        public IEnumerable<SparkButtonItem> BuildButtons()
        {
            return
            [
                new SparkButtonItem
                {
                    Label = "Освежи",
                    IsPrimary = false,
                    Command = new RelayCommand(() => FiltersChanged?.Invoke())
                }
            ];
        }

        // =====================================================
        // GENERATE
        // =====================================================
        public async Task<List<DynamicReportRow>> GenerateAsync(DateTime from, DateTime to)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var data = await db.TherapyCycles
                .Include(x => x.Patient)
                .AsNoTracking()
                .Where(x => x.Status==TherapyStatus.Missed
                    &&x.StartDate>=from
                    &&(x.EndDate==null||x.EndDate<=to))
                .OrderByDescending(x => x.EndDate)
                .ToListAsync();

            RefreshTabCounts(data);

            var filtered = _selectedReasonFilter switch
            {
                "Со причина" => data.Where(x => !string.IsNullOrWhiteSpace(x.Notes)),
                "Без причина" => data.Where(x => string.IsNullOrWhiteSpace(x.Notes)),
                _ => data
            };

            return filtered.Select(x =>
            {
                var noReason = string.IsNullOrWhiteSpace(x.Notes);

                return new DynamicReportRow
                {
                    Cells=
                    [
                        x.Patient?.FullName ?? "-",
                        x.Patient?.NationalId ?? "-",
                        $"Цикл #{x.CycleNumber}",
                        x.StartDate.HasValue ? x.StartDate.Value.ToString("dd.MM.yyyy") : "-",
                        x.Notes ?? "-"
                    ],
                    IsAlertSeverity=noReason
                };
            }).ToList();
        }

        private void RefreshTabCounts(List<TherapyCycle> data)
        {
            if(_allTab==null) return;

            _allTab.Value=data.Count.ToString("N0");
            _withReasonTab!.Value=data.Count(x => !string.IsNullOrWhiteSpace(x.Notes)).ToString("N0");
            _withoutReasonTab!.Value=data.Count(x => string.IsNullOrWhiteSpace(x.Notes)).ToString("N0");
        }

        public ReportMetrics CalculateMetrics(IEnumerable<DynamicReportRow> rows)
        {
            var list = rows.ToList();
            var withoutReason = list.Count(x => x.IsAlertSeverity);

            return new ReportMetrics
            {
                Title1="Пропуштени терапии",
                Value1=list.Count,
                Title2="Без причина",
                Value2=withoutReason,
                Title3="Пополнетост",
                Value3=list.Count==0 ? 100 : (int)((double)(list.Count-withoutReason)/list.Count*100)
            };
        }
    }
}