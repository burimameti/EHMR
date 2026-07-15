using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Domain.Entities.Reports
{
    public sealed class PatientsReportProvider : IReportProvider
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

        private string _selectedAllergyFilter = "Сите";
        private string _selectedCityFilter = "Сите";

        private SparkTabItem? _allTab, _allergyTab;
        private SparkPickerItem? _cityPicker;

        public event Action? FiltersChanged;

        public PatientsReportProvider(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
        {
            _dbFactory=dbFactory;
        }

        public string Key => "patients";
        public string Title => "Пациенти";
        public string Description => "Регистрирани пациенти во избраниот временски период.";
        public string Icon => "🧑‍⚕️";
        public ReportType Type => ReportType.Patients;
        public ReportCategory Category => ReportCategory.Clinical;

        public IEnumerable<SparkGridColumn> Columns =>
        [
            new() { Header = "ПАЦИЕНТ", Key = "Patient", Width = new GridLength(180) },
            new() { Header = "ТЕЛЕФОН", Key = "Phone", Width = new GridLength(150) },
            new() { Header = "МАТИЧЕН БРОЈ", Key = "NationalId", Width = new GridLength(150) },
            new() { Header = "АДРЕСА", Key = "Address", Width = new GridLength(180) },
            new() { Header = "ГРАД", Key = "City", Width = new GridLength(90) },
            new() { Header = "КРЕИРАН НА", Key = "Created", Width = new GridLength(110) },
            new() { Header = "ДИЈАГНОЗА / АЛЕРГИИ", Key = "Medical", Width = GridLength.Star }
        ];

        // =====================================================
        // TABS
        // =====================================================
        public IEnumerable<SparkTabItem> BuildTabs()
        {
            _allTab=new SparkTabItem { Title="Сите", Value="0", IsSelected=true };
            _allergyTab=new SparkTabItem { Title="Со алергии", Value="0" };

            _allTab.Command=new RelayCommand(() => SelectAllergyFilter(_allTab, "Сите"));
            _allergyTab.Command=new RelayCommand(() => SelectAllergyFilter(_allergyTab, "Со алергии"));

            return [_allTab, _allergyTab];
        }

        private void SelectAllergyFilter(SparkTabItem tab, string value)
        {
            foreach(var t in new[] { _allTab, _allergyTab })
                if(t!=null) t.IsSelected=false;

            tab.IsSelected=true;
            _selectedAllergyFilter=value;

            FiltersChanged?.Invoke();
        }

        // =====================================================
        // PICKER — Град, полнет динамички од базата при секој GenerateAsync
        // =====================================================
        public IEnumerable<SparkPickerItem> BuildPickers()
        {
            _cityPicker=new SparkPickerItem { Placeholder="Град" };
            _cityPicker.Items.Add("Сите");
            _cityPicker.SelectedItem=_selectedCityFilter;

            _cityPicker.PropertyChanged+=(_, e) =>
            {
                if(e.PropertyName==nameof(SparkPickerItem.SelectedItem)&&_cityPicker.SelectedItem is string s)
                {
                    _selectedCityFilter=s;
                    FiltersChanged?.Invoke();
                }
            };

            return [_cityPicker];
        }

        private void RefreshCityPickerItems(List<Patient> patients)
        {
            if(_cityPicker==null) return;

            var cities = patients
                .Select(x => x.City)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var currentSelection = _cityPicker.SelectedItem as string??"Сите";

            _cityPicker.Items.Clear();
            _cityPicker.Items.Add("Сите");
            foreach(var city in cities)
                _cityPicker.Items.Add(city!);

            // ако избраниот град веќе не постои во новиот range, врати на "Сите" без да пукнеш FiltersChanged повторно
            _cityPicker.SelectedItem=_cityPicker.Items.Contains(currentSelection) ? currentSelection : "Сите";
            _selectedCityFilter=(string)_cityPicker.SelectedItem;
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

            var patients = await db.Patients
                .Include(p => p.Diagnoses)
                    .ThenInclude(d => d.Mkb10Code)
                .AsNoTracking()
                .Where(x => x.CreatedAt>=from&&x.CreatedAt<=to)
                .ToListAsync();

            RefreshCityPickerItems(patients);

            var filtered = patients.AsEnumerable();

            if(_selectedAllergyFilter=="Со алергии")
                filtered=filtered.Where(x => !string.IsNullOrWhiteSpace(x.Allergies));

            if(_selectedCityFilter!="Сите")
                filtered=filtered.Where(x => x.City==_selectedCityFilter);

            return filtered.Select(x =>
            {
                var hasAllergy = !string.IsNullOrWhiteSpace(x.Allergies);

                return new DynamicReportRow
                {
                    Cells=
                    [
                        x.FullName ?? "-",
                        x.Phone ?? "-",
           
                        x.Address ?? "-",
                        x.City ?? "-",
                        x.CreatedAt.ToString("dd.MM.yyyy"),
                        BuildMedicalInfo(x)
                    ],
                    IsAlertSeverity=hasAllergy
                };
            }).ToList();
        }

        private static string BuildMedicalInfo(Patient patient)
        {
            var diagnosis = patient.Diagnoses==null
                ? ""
                : string.Join(", ", patient.Diagnoses.Where(x => x.Mkb10Code!=null).Select(x => x.Mkb10Code!.Code));

            var allergies = string.IsNullOrWhiteSpace(patient.Allergies) ? "Нема алергии" : patient.Allergies;

            return $"Дијагнози: {diagnosis}; Алергии: {allergies}";
        }

        public ReportMetrics CalculateMetrics(IEnumerable<DynamicReportRow> rows)
        {
            var list = rows.ToList();
            var allergies = list.Count(x => x.IsAlertSeverity);

            return new ReportMetrics
            {
                Title1="Пациенти",
                Value1=list.Count,
                Title2="Со алергии",
                Value2=allergies,
                Title3="Здравствен запис",
                Value3=list.Count==0 ? 100 : (int)((double)(list.Count-allergies)/list.Count*100)
            };
        }
    }
}