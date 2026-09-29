using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using EHMR.ViewModels.Patients.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


namespace EHMR.Domain.Entities.Reports
{
    public sealed class PatientsReportProvider : IReportProvider
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

        private string _selectedStatusFilter = "Сите";
        private string _selectedAllergyFilter = "Сите";
        private string _selectedCityFilter = "Сите";
        private string _selectedDoctorFilter = "Сите";
        private string _selectedDiagnosisFilter = "Сите";
        private string _selectedMedicineFilter = "Сите";
        private string _selectedTherapyStatusFilter = "Сите";
        private string _selectedGenderFilter = "Сите";

        private SparkTabItem? _allTab, _allergyTab, _activeTab, _inactiveTab;

        private SparkPickerItem? _statusPicker;
        private SparkPickerItem? _cityPicker;
        private SparkPickerItem? _doctorPicker;
        private SparkPickerItem? _diagnosisPicker;
        private SparkPickerItem? _medicinePicker;
        private SparkPickerItem? _therapyStatusPicker;
        private SparkPickerItem? _genderPicker;

        public event Action? FiltersChanged;
        private bool _refreshingPickers;

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
            new() { Header = "СТАТУС", Key = "Status", Width = new GridLength(90) },
            new() { Header = "ПОЛ", Key = "Gender", Width = new GridLength(90) },
            new() { Header = "ТЕЛЕФОН", Key = "Phone", Width = new GridLength(150) },
            new() { Header = "СКОР", Key = "Score", Width = new GridLength(120) },
            new() { Header = "АДРЕСА", Key = "Address", Width = new GridLength(180) },
            new() { Header = "ГРАД", Key = "City", Width = new GridLength(90) },
            new() { Header = "КРЕИРАН НА", Key = "Created", Width = new GridLength(110) },
            new() { Header = "ЛЕКОВИ", Key = "Medicines", Width = new GridLength(150) },
            new() { Header = "ДИЈАГНОЗА / АЛЕРГИИ", Key = "Medical", Width = GridLength.Star }
        ];

        // =====================================================
        // TABS — Пациент статус + Алергии
        // =====================================================
        public IEnumerable<SparkTabItem> BuildTabs()
        {
            _allTab=new SparkTabItem { Title="Сите", Value="0", IsSelected=true };
            _activeTab=new SparkTabItem { Title="Активни", Value="0" };
            _inactiveTab=new SparkTabItem { Title="Неактивни", Value="0" };
            _allergyTab=new SparkTabItem { Title="Со алергии", Value="0" };

            _allTab.Command=new RelayCommand(() => SelectStatusFilter(_allTab, "Сите"));
            _activeTab.Command=new RelayCommand(() => SelectStatusFilter(_activeTab, "Active"));
            _inactiveTab.Command=new RelayCommand(() => SelectStatusFilter(_inactiveTab, "Inactive"));
            _allergyTab.Command=new RelayCommand(() => SelectAllergyFilter(_allergyTab, "Со алергии"));

            return [_allTab, _activeTab, _inactiveTab, _allergyTab];
        }

        private void SelectStatusFilter(SparkTabItem tab, string value)
        {
            foreach(var t in new[] { _allTab, _activeTab, _inactiveTab })
                if(t!=null) t.IsSelected=false;

            tab.IsSelected=true;
            _selectedStatusFilter=value;

            FiltersChanged?.Invoke();
        }

        private void SelectAllergyFilter(SparkTabItem tab, string value)
        {
            foreach(var t in new[] { _allergyTab })
                if(t!=null) t.IsSelected=false;

            tab.IsSelected=true;
            _selectedAllergyFilter=value;

            FiltersChanged?.Invoke();
        }

        // =====================================================
        // PICKERS
        // =====================================================
        public IEnumerable<SparkPickerItem> BuildPickers()
        {
            _statusPicker=CreatePicker(
                "Статус",
                _selectedStatusFilter,
                value => _selectedStatusFilter=value);

            _cityPicker=CreatePicker(
                "Град",
                _selectedCityFilter,
                value => _selectedCityFilter=value);

            _doctorPicker=CreatePicker(
                "Доктор",
                _selectedDoctorFilter,
                value => _selectedDoctorFilter=value);

            _diagnosisPicker=CreatePicker(
                "Дијагноза",
                _selectedDiagnosisFilter,
                value => _selectedDiagnosisFilter=value);

            _medicinePicker=CreatePicker(
                "Лек",
                _selectedMedicineFilter,
                value => _selectedMedicineFilter=value);

            _therapyStatusPicker=CreatePicker(
                "Статус на терапија",
                _selectedTherapyStatusFilter,
                value => _selectedTherapyStatusFilter=value);

            _genderPicker=CreatePicker(
                "Пол",
                _selectedGenderFilter,
                value => _selectedGenderFilter=value);

            return
            [
                _statusPicker,
                _cityPicker,
                _doctorPicker,
                _diagnosisPicker,
                _medicinePicker,
                _therapyStatusPicker,
                _genderPicker
            ];
        }

        private SparkPickerItem CreatePicker(
            string placeholder,
            string selectedValue,
            Action<string> setter)
        {
            var picker = new SparkPickerItem
            {
                Placeholder=placeholder
            };

            picker.Items.Add("Сите");
            picker.SelectedItem=selectedValue;

            picker.PropertyChanged+=(_, e) =>
            {
                if(_refreshingPickers)
                    return;

                if(e.PropertyName!=nameof(SparkPickerItem.SelectedItem))
                    return;

                if(picker.SelectedItem is not string value)
                    return;

                Debug.WriteLine($"[Patients] Picker '{placeholder}' changed -> {value}");
                setter(value);
                FiltersChanged?.Invoke();
            };

            return picker;
        }

        private void RefreshPicker(
            SparkPickerItem? picker,
            IEnumerable<string?> values)
        {
            if(picker==null)
                return;

            _refreshingPickers=true;

            try
            {
                var selected = picker.SelectedItem as string??"Сите";

                picker.Items.Clear();
                picker.Items.Add("Сите");

                foreach(var value in values
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x))
                {
                    picker.Items.Add(value!);
                }

                picker.SelectedItem=
                    picker.Items.Contains(selected)
                        ? selected
                        : "Сите";
            }
            finally
            {
                _refreshingPickers=false;
            }
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
                    IsPrimary = true,
                    Command = new RelayCommand(() => FiltersChanged?.Invoke())
                }
            ];
        }

        // =====================================================
        // GENERATE — WITH COMPREHENSIVE LOGGING
        // =====================================================
        public async Task<List<DynamicReportRow>> GenerateAsync(DateTime from, DateTime to)
        {
            try
            {
                Debug.WriteLine($"\n[Patients] ========== START GENERATE ==========");
                Debug.WriteLine($"[Patients] Period: {from:dd.MM.yyyy} to {to:dd.MM.yyyy}");

                Debug.WriteLine($"[Patients] [1] Creating DbContext...");
                await using var db = await _dbFactory.CreateDbContextAsync();
                Debug.WriteLine($"[Patients] [2] DbContext created ✓");

                using var cts = new System.Threading.CancellationTokenSource(System.TimeSpan.FromSeconds(60));
                var stopwatch = Stopwatch.StartNew();

                Debug.WriteLine($"[Patients] [3] Starting DB query...");
                var patients = await db.Patients
                    .Include(x => x.Doctor)
                        .ThenInclude(x => x!.User)
                    .Include(x => x.PatientMedicines)
                        .ThenInclude(x => x.Medicine)
                    .Include(x => x.Diagnoses)
                        .ThenInclude(x => x.Mkb10Code)
.Include(x => x.TherapyCycles)
                    .Include(x => x.Scores)
                    .AsNoTracking()
                    .Where(x => x.RegistrationDate>=from&&
                                x.RegistrationDate<=to)
                    .ToListAsync(cts.Token);

                stopwatch.Stop();
                Debug.WriteLine($"[Patients] [4] Query completed in {stopwatch.ElapsedMilliseconds}ms ✓");
                Debug.WriteLine($"[Patients] [5] Patients loaded: {patients.Count}");

                Debug.WriteLine($"[Patients] [6] Refreshing pickers...");
                RefreshPickers(patients);

                Debug.WriteLine($"[Patients] [7] Applying filters...");
                var filtered = ApplyFilters(patients);
                var filtered_list = filtered.ToList();
                Debug.WriteLine($"[Patients] [8] After filtering: {filtered_list.Count} patients");

                Debug.WriteLine($"[Patients] [9] Building report rows...");
                var rows = filtered_list
                    .Select(CreateRow)
                    .ToList();

                Debug.WriteLine($"[Patients] [10] Report generation complete ✓");
                Debug.WriteLine($"[Patients] Final row count: {rows.Count}");
                Debug.WriteLine($"[Patients] ========== END GENERATE ==========\n");

                return rows;
            }
            catch(OperationCanceledException ex)
            {
                Debug.WriteLine($"\n❌ [Patients] TIMEOUT ERROR!");
                Debug.WriteLine($"❌ Exception: {ex.Message}");
                Debug.WriteLine($"[Patients] ========== END GENERATE (FAILED) ==========\n");
                throw;
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"\n❌ [Patients] EXCEPTION!");
                Debug.WriteLine($"❌ Type: {ex.GetType().Name}");
                Debug.WriteLine($"❌ Message: {ex.Message}");
                Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                Debug.WriteLine($"[Patients] ========== END GENERATE (FAILED) ==========\n");
                throw;
            }
        }

        private void RefreshPickers(List<Patient> patients)
        {
            Debug.WriteLine($"[Patients]   - Status picker...");
            RefreshPicker(_statusPicker,
                new[] { "Активни", "Неактивни" });

            Debug.WriteLine($"[Patients]   - City picker...");
            RefreshPicker(_cityPicker,
                patients.Select(x => x.City));

            Debug.WriteLine($"[Patients]   - Doctor picker...");
            RefreshPicker(_doctorPicker,
                patients
                    .Where(x => x.Doctor!=null)
                    .Select(x => x.Doctor!.FullName));

            Debug.WriteLine($"[Patients]   - Diagnosis picker...");
            RefreshPicker(_diagnosisPicker,
                patients
                    .SelectMany(x => x.Diagnoses)
                    .Where(x => x.Mkb10Code!=null)
                    .Select(x => x.Mkb10Code!.Code));

            Debug.WriteLine($"[Patients]   - Medicine picker...");
            RefreshPicker(_medicinePicker,
                patients
                    .SelectMany(x => x.PatientMedicines)
                    .Where(x => x.Medicine!=null)
                    .Select(x => x.Medicine!.Name));

            Debug.WriteLine($"[Patients]   - Therapy status picker...");
            RefreshPicker(_therapyStatusPicker,
                patients
                    .SelectMany(x => x.TherapyCycles)
                    .Where(x => x.Status.HasValue)
                    .Select(x => x.Status!.Value.ToDisplay())
                    .Distinct());

            Debug.WriteLine($"[Patients]   - Gender picker...");
            RefreshPicker(_genderPicker,
                Enum.GetValues<Gender>()
                    .Select(x => x.ToString()));
        }

        private IEnumerable<Patient> ApplyFilters(IEnumerable<Patient> patients)
        {
            var query = patients;

            // Patient Status filter
            if(_selectedStatusFilter!="Сите")
            {
                var status = _selectedStatusFilter switch
                {
                    "Активни" => PatientStatus.Active,
                    "Неактивни" => PatientStatus.Inactive,
                    _ => (PatientStatus?)null
                };

                if(status.HasValue)
                {
                    query=query.Where(x => x.Status==status.Value);
                    Debug.WriteLine($"[Patients]   - Status filter: {_selectedStatusFilter}");
                }
            }

            // Allergy filter
            if(_selectedAllergyFilter=="Со алергии")
            {
                query=query.Where(x =>
                    !string.IsNullOrWhiteSpace(x.Allergies));
                Debug.WriteLine($"[Patients]   - Allergy filter: With allergies");
            }

            // City filter
            if(_selectedCityFilter!="Сите")
            {
                query=query.Where(x =>
                    x.City==_selectedCityFilter);
                Debug.WriteLine($"[Patients]   - City filter: {_selectedCityFilter}");
            }

            // Doctor filter
            if(_selectedDoctorFilter!="Сите")
            {
                query=query.Where(x =>
                    x.Doctor?.FullName==_selectedDoctorFilter);
                Debug.WriteLine($"[Patients]   - Doctor filter: {_selectedDoctorFilter}");
            }

            // Diagnosis filter
            if(_selectedDiagnosisFilter!="Сите")
            {
                query=query.Where(x =>
                    x.Diagnoses.Any(d =>
                        d.Mkb10Code?.Code==_selectedDiagnosisFilter));
                Debug.WriteLine($"[Patients]   - Diagnosis filter: {_selectedDiagnosisFilter}");
            }

            // Medicine filter
            if(_selectedMedicineFilter!="Сите")
            {
                query=query.Where(x =>
                    x.PatientMedicines.Any(pm =>
                        pm.Medicine?.Name==_selectedMedicineFilter));
                Debug.WriteLine($"[Patients]   - Medicine filter: {_selectedMedicineFilter}");
            }

            // Therapy Status filter
            if(_selectedTherapyStatusFilter!="Сите"&&
                Enum.TryParse<TherapyStatus>(_selectedTherapyStatusFilter, out var therapyStatus))
            {
                query=query.Where(x =>
                    x.TherapyCycles.Any(tc => tc.Status==therapyStatus));
                Debug.WriteLine($"[Patients]   - Therapy status filter: {_selectedTherapyStatusFilter}");
            }

            // Gender filter
            if(_selectedGenderFilter!="Сите"&&
                Enum.TryParse<Gender>(_selectedGenderFilter, out var gender))
            {
                query=query.Where(x =>
                    x.Gender==gender);
                Debug.WriteLine($"[Patients]   - Gender filter: {_selectedGenderFilter}");
            }

            return query;
        }

        private static DynamicReportRow CreateRow(Patient patient)
        {
            var hasAllergy = !string.IsNullOrWhiteSpace(patient.Allergies);
            var hasMissedTherapy = patient.TherapyCycles.Any(x => x.Status==TherapyStatus.Missed);

            return new DynamicReportRow
            {
                Cells=
                [
                    patient.FullName ?? "-",
                    patient.Status.ToDisplay(),
                    patient.Gender.ToDisplay(),
                    patient.Phone ?? "-",
                    patient.Scores
                        .OrderByDescending(x => x.RecordedAt)
                        .Select(x => x.ScoreText)
                        .FirstOrDefault() ?? "Нема скор",
                    patient.Address ?? "-",
                    patient.City ?? "-",
                    patient.CreatedAt.ToString("dd.MM.yyyy"),
                    BuildMedicinesInfo(patient),
                    BuildMedicalInfo(patient)
                ],
                // Alert if patient has allergies OR missed therapies
                IsAlertSeverity=hasAllergy||hasMissedTherapy
            };
        }

        private static string BuildMedicinesInfo(Patient patient)
        {
            if(patient.PatientMedicines==null||patient.PatientMedicines.Count==0)
                return "Нема лекови";

            var medicines = string.Join(", ",
                patient.PatientMedicines
                    .Where(x => x.Medicine!=null)
                    .Select(x => x.Medicine!.Name));

            return string.IsNullOrWhiteSpace(medicines) ? "Нема лекови" : medicines;
        }

        private static string BuildMedicalInfo(Patient patient)
        {
            var diagnosis = patient.Diagnoses==null
                ? ""
                : string.Join(", ", patient.Diagnoses.Where(x => x.Mkb10Code!=null).Select(x => x.Mkb10Code!.Code));

            var allergies = string.IsNullOrWhiteSpace(patient.Allergies) ? "Нема алергии" : patient.Allergies;

            var missedTherapies = patient.TherapyCycles
                .Where(x => x.Status==TherapyStatus.Missed)
                .Count();

            var therapyInfo = missedTherapies>0 ? $" | ⚠️ Пропуштени терапии: {missedTherapies}" : "";

            return $"Дијагнози: {diagnosis}; Алергии: {allergies}{therapyInfo}";
        }

        public ReportMetrics CalculateMetrics(IEnumerable<DynamicReportRow> rows)
        {
            var list = rows.ToList();
            var withAlertSeverity = list.Count(x => x.IsAlertSeverity);

            return new ReportMetrics
            {
                Title1="Пациенти",
                Value1=list.Count,
                Title2="Со алергии / пропуштени терапии",
                Value2=withAlertSeverity,
                Title3="Здравствен запис",
                Value3=list.Count==0 ? 100 : (int)((double)(list.Count-withAlertSeverity)/list.Count*100)
            };
        }
    }
}