using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Domain.Entities.Reports
{
    public sealed class AppointmentStatusesReportProvider : IReportProvider
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

        // единствен извор на вистина за статус филтерот — и tabs и picker пишуваат овде
        private string _selectedStatusFilter = "Сите";

        private SparkTabItem? _allTab, _completedTab, _problematicTab;
        private SparkPickerItem? _statusPicker;

        public event Action? FiltersChanged;

        public AppointmentStatusesReportProvider(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
        {
            _dbFactory=dbFactory;
        }

        public string Key => "appointment-statuses";
        public string Title => "Статус на термини";
        public string Description => "Преглед на закажани, завршени и откажани термини.";
        public string Icon => "📅";
        public ReportType Type => ReportType.AppointmentStatuses;
        public ReportCategory Category => ReportCategory.Operational;

        public IEnumerable<SparkGridColumn> Columns =>
        [
            new() { Header = "ПАЦИЕНТ", Key = "Patient", Width = new GridLength(220) },
            new() { Header = "РЕУМАТОЛОГ", Key = "Doctor", Width = new GridLength(200) },
            new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(130) },
            new() { Header = "ТЕРМИН", Key = "Date", Width = new GridLength(160) },
            new() { Header = "ЗАБЕЛЕШКА", Key = "Note", Width = GridLength.Star }
        ];

        // =====================================================
        // TABS — реални quick-filter presets, синхронизирани со picker-от
        // =====================================================
        public IEnumerable<SparkTabItem> BuildTabs()
        {
            _allTab=new SparkTabItem { Title="Сите", Value="0", IsSelected=true };
            _completedTab=new SparkTabItem { Title="Завршени", Value="0" };
            _problematicTab=new SparkTabItem { Title="Проблематични", Value="0" };

            _allTab.Command=new RelayCommand(() => SelectStatus(_allTab, "Сите"));
            _completedTab.Command=new RelayCommand(() => SelectStatus(_completedTab, "Завршени"));
            _problematicTab.Command=new RelayCommand(() => SelectStatus(_problematicTab, "Проблематични"));

            return [_allTab, _completedTab, _problematicTab];
        }

        private void SelectStatus(SparkTabItem tab, string status)
        {
            foreach(var t in new[] { _allTab, _completedTab, _problematicTab })
                if(t!=null) t.IsSelected=false;

            tab.IsSelected=true;
            _selectedStatusFilter=status;

            if(_statusPicker!=null)
                _statusPicker.SelectedItem=status;

            FiltersChanged?.Invoke();
        }

        // =====================================================
        // PICKER — реално хранење назад преку PropertyChanged
        // =====================================================
        public IEnumerable<SparkPickerItem> BuildPickers()
        {
            _statusPicker=new SparkPickerItem { Placeholder="Статус" };

            foreach(var item in new[] { "Сите", "Закажан", "Во тек", "Завршен", "Откажан", "Не се пријавил" })
                _statusPicker.Items.Add(item);

            _statusPicker.SelectedItem=_selectedStatusFilter;

            _statusPicker.PropertyChanged+=(_, e) =>
            {
                if(e.PropertyName==nameof(SparkPickerItem.SelectedItem)&&_statusPicker.SelectedItem is string s)
                {
                    _selectedStatusFilter=s;
                    SyncTabsFromPicker(s);
                    FiltersChanged?.Invoke();
                }
            };

            return [_statusPicker];
        }

        private void SyncTabsFromPicker(string status)
        {
            // "Завршен" од picker-от одговара на "Завршени" tab; сѐ друго освен "Сите" паѓа под "Проблематични"
            if(_allTab==null) return;

            var mappedTab = status switch
            {
                "Сите" => _allTab,
                "Завршен" => _completedTab,
                _ when status!="Сите" => _problematicTab,
                _ => _allTab
            };

            foreach(var t in new[] { _allTab, _completedTab, _problematicTab })
                if(t!=null) t.IsSelected=false;

            mappedTab!.IsSelected=true;
        }

        // =====================================================
        // BUTTON — тргнато дупликат "Експорт" (веќе постои во header-от), заменето со "Освежи"
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
        // GENERATE — date range од DB, статус филтер во меморија (за да можеме да ги пресметаме tab броевите од истиот сет)
        // =====================================================
        public async Task<List<DynamicReportRow>> GenerateAsync(DateTime from, DateTime to)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var appointments = await db.Appointments
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                .AsNoTracking()
                .Where(x => x.ScheduledStart>=from&&x.ScheduledStart<=to)
                .OrderByDescending(x => x.ScheduledStart)
                .ToListAsync();

            RefreshTabCounts(appointments);

            var filtered = _selectedStatusFilter switch
            {
                "Сите" => appointments,
                "Завршени" or "Завршен" => appointments.Where(x => x.Status==AppointmentStatus.Completed),
                "Проблематични" => appointments.Where(x => x.Status==AppointmentStatus.Cancelled),
                "Закажан" => appointments.Where(x => x.Status==AppointmentStatus.Scheduled),
                "Во тек" => appointments.Where(x => x.Status==AppointmentStatus.InProgress),
                "Откажан" => appointments.Where(x => x.Status==AppointmentStatus.Cancelled),
                _ => appointments
            };

            return filtered.Select(x =>
            {
                var alert = x.Status==AppointmentStatus.Cancelled;

                return new DynamicReportRow
                {
                    Cells=
                    [
                        x.Patient?.FullName ?? "-",
                        x.Doctor?.FullName ?? "-",
                        StatusLabel(x.Status),
                        x.ScheduledStart.ToString("dd.MM.yyyy HH:mm"),
                        string.IsNullOrWhiteSpace(x.ReasonForVisit) ? "-" : x.ReasonForVisit
                    ],
                    IsAlertSeverity=alert
                };
            }).ToList();
        }

        private void RefreshTabCounts(List<Appointment> appointments)
        {
            if(_allTab==null) return;

            _allTab.Value=appointments.Count.ToString("N0");
            _completedTab!.Value=appointments.Count(x => x.Status==AppointmentStatus.Completed).ToString("N0");
            _problematicTab!.Value=appointments.Count(x => x.Status==AppointmentStatus.Cancelled).ToString("N0");
        }

        public ReportMetrics CalculateMetrics(IEnumerable<DynamicReportRow> rows)
        {
            var list = rows.ToList();
            var alerts = list.Count(x => x.IsAlertSeverity);

            return new ReportMetrics
            {
                Title1="Вкупно термини",
                Value1=list.Count,
                Title2="Откажани",
                Value2=alerts,
                Title3="Реализација",
                Value3=list.Count==0 ? 100 : (int)((double)(list.Count-alerts)/list.Count*100)
            };
        }

        private static string StatusLabel(AppointmentStatus status) => status switch
        {
            AppointmentStatus.Scheduled => "Закажан",
            AppointmentStatus.InProgress => "Во тек",
            AppointmentStatus.Completed => "Завршен",
            AppointmentStatus.Cancelled => "Откажан",
            _ => status.ToString()
        };

        private static SparkBadgeTone StatusTone(AppointmentStatus status) => status switch
        {
            AppointmentStatus.Completed => SparkBadgeTone.Success,
            AppointmentStatus.Cancelled => SparkBadgeTone.Danger,
            AppointmentStatus.InProgress => SparkBadgeTone.Warning,
            _ => SparkBadgeTone.Neutral
        };
    }
}