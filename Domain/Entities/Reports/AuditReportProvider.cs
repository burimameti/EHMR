using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Domain.Entities.Reports
{
    public sealed class AuditReportProvider : IReportProvider
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

        private string _selectedSeverityFilter = "Сите";
        private string _selectedUserFilter = "Сите";

        private SparkTabItem? _allTab, _criticalTab;
        private SparkPickerItem? _userPicker;

        public event Action? FiltersChanged;

        public AuditReportProvider(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
        {
            _dbFactory=dbFactory;
        }

        public string Key => "audit";
        public string Title => "Системски Аудит";
        public string Description => "Хронолошки преглед на кориснички активности, промени и системски настани.";
        public string Icon => "🛡️";
        public ReportType Type => ReportType.Auditing;
        public ReportCategory Category => ReportCategory.Security;

        public IEnumerable<SparkGridColumn> Columns =>
        [
            new() { Header = "КОРИСНИК", Key = "User", Width = new GridLength(180) },
            new() { Header = "АКЦИЈА", Key = "Action", Width = new GridLength(150) },
            new() { Header = "ЕНТИТЕТ", Key = "Entity", Width = new GridLength(180) },
            new() { Header = "ВРЕМЕ", Key = "Time", Width = new GridLength(160) },
            new() { Header = "ДЕТАЛИ", Key = "Details", Width = GridLength.Star }
        ];

        // =====================================================
        // TABS
        // =====================================================
        public IEnumerable<SparkTabItem> BuildTabs()
        {
            _allTab=new SparkTabItem { Title="Сите", Value="0", IsSelected=true };
            _criticalTab=new SparkTabItem { Title="Критични", Value="0" };

            _allTab.Command=new RelayCommand(() => SelectSeverity(_allTab, "Сите"));
            _criticalTab.Command=new RelayCommand(() => SelectSeverity(_criticalTab, "Критични"));

            return [_allTab, _criticalTab];
        }

        private void SelectSeverity(SparkTabItem tab, string value)
        {
            foreach(var t in new[] { _allTab, _criticalTab })
                if(t!=null) t.IsSelected=false;

            tab.IsSelected=true;
            _selectedSeverityFilter=value;

            FiltersChanged?.Invoke();
        }

        // =====================================================
        // PICKER — Корисник, полнет динамички од базата
        // =====================================================
        public IEnumerable<SparkPickerItem> BuildPickers()
        {
            _userPicker=new SparkPickerItem { Placeholder="Корисник" };
            _userPicker.Items.Add("Сите");
            _userPicker.SelectedItem=_selectedUserFilter;

            _userPicker.PropertyChanged+=(_, e) =>
            {
                if(e.PropertyName==nameof(SparkPickerItem.SelectedItem)&&_userPicker.SelectedItem is string s)
                {
                    _selectedUserFilter=s;
                    FiltersChanged?.Invoke();
                }
            };

            return [_userPicker];
        }

        private void RefreshUserPickerItems(List<AuditLog> logs)
        {
            if(_userPicker==null) return;

            var users = logs
                .Select(x => x.UserId.ToString()==null ? x.UserId.ToString() : "SYSTEM")
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var currentSelection = _userPicker.SelectedItem as string??"Сите";

            _userPicker.Items.Clear();
            _userPicker.Items.Add("Сите");
            foreach(var user in users)
                _userPicker.Items.Add(user);

            _userPicker.SelectedItem=_userPicker.Items.Contains(currentSelection) ? currentSelection : "Сите";
            _selectedUserFilter=(string)_userPicker.SelectedItem;
        }

        // =====================================================
        // BUTTON
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

            var logs = await db.AuditLogs
                .AsNoTracking()
                .Where(x => x.Timestamp>=from&&x.Timestamp<=to)
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();

            RefreshUserPickerItems(logs);

            var filtered = logs.AsEnumerable();

            if(_selectedSeverityFilter=="Критични")
                filtered=filtered.Where(x => string.IsNullOrWhiteSpace(x.BeforeValue)||string.IsNullOrWhiteSpace(x.AfterValue));

            if(_selectedUserFilter!="Сите")
            {
                filtered=_selectedUserFilter=="SYSTEM"
                    ? filtered.Where(x => x.UserId==null)
                    : filtered.Where(x => x.UserId.ToString()==null &&x.UserId.ToString()==_selectedUserFilter);
            }

            return filtered.Select(x =>
            {
                var critical = string.IsNullOrWhiteSpace(x.BeforeValue)||string.IsNullOrWhiteSpace(x.AfterValue);

                return new DynamicReportRow
                {
                    Cells=
                    [
                        x.UserId.ToString()==null ? x.UserId.ToString() : "SYSTEM",
                        x.Action ?? "-",
                        x.EntityName ?? "-",
                        x.Timestamp.ToString("dd.MM.yyyy HH:mm"),
                        BuildDetails(x)
                    ],
                    IsAlertSeverity=critical
                };
            }).ToList();
        }

        private static string BuildDetails(AuditLog x)
        {
            var parts = new List<string>();

            if(!string.IsNullOrWhiteSpace(x.Description)) parts.Add(x.Description);
            if(!string.IsNullOrWhiteSpace(x.Data)) parts.Add(x.Data);
            if(!string.IsNullOrWhiteSpace(x.BeforeValue)) parts.Add($"Пред: {x.BeforeValue}");
            if(!string.IsNullOrWhiteSpace(x.AfterValue)) parts.Add($"После: {x.AfterValue}");

            return parts.Count==0 ? "-" : string.Join(" | ", parts);
        }

        public ReportMetrics CalculateMetrics(IEnumerable<DynamicReportRow> rows)
        {
            var list = rows.ToList();
            var alerts = list.Count(x => x.IsAlertSeverity);

            return new ReportMetrics
            {
                Title1="ВКУПНО АУДИТ ЗАПИСИ",
                Value1=list.Count,
                Title2="КРИТИЧНИ НАСТАНИ",
                Value2=alerts,
                Title3="БЕЗБЕДНОСТ",
                Value3=list.Count==0 ? 100 : (int)((double)(list.Count-alerts)/list.Count*100)
            };
        }
    }
}