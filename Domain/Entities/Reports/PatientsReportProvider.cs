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
        private string _selectedRheumatologistFilter = "Сите";
        private string _selectedDiagnosisFilter = "Сите";
        private string _selectedMedicineFilter = "Сите";
        private string _selectedGenderFilter = "Сите";
        private string _selectedScoreFilter = "Сите";
        private Guid? _selectedMedicineId;
        private readonly Dictionary<string, Guid> _medicineIdsByDisplay = new(StringComparer.CurrentCultureIgnoreCase);
        private decimal? _selectedMedicineTotalQuantity;

        public bool IsMedicineFilterEnabled { get; private set; }
        public string? SelectedMedicineForExport =>
            IsMedicineFilterEnabled && _selectedMedicineFilter!="Сите"
                ? _selectedMedicineFilter
                : null;
        public decimal? SelectedMedicineTotalQuantityForExport =>
            IsMedicineFilterEnabled && _selectedMedicineFilter!="Сите"
                ? _selectedMedicineTotalQuantity
                : null;

        public void SetMedicineFilterEnabled(bool enabled)
        {
            IsMedicineFilterEnabled=enabled;
            if(!enabled)
            {
                _selectedMedicineFilter="Сите";
                _selectedMedicineId=null;
                _selectedMedicineTotalQuantity=null;
            }
            FiltersChanged?.Invoke();
        }

        private SparkTabItem? _allTab, _allergyTab, _activeTab, _inactiveTab;

        private SparkPickerItem? _statusPicker;
        private SparkPickerItem? _cityPicker;
        private SparkPickerItem? _rheumatologistPicker;
        private SparkPickerItem? _diagnosisPicker;
        private SparkPickerItem? _medicinePicker;
        private SparkPickerItem? _genderPicker;
        private SparkPickerItem? _scorePicker;

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
            new() { Header = "ПАЦИЕНТ", Key = "Patient", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "ЕЗБО", Key = "Szbo", Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", Width = new GridLength(1.1, GridUnitType.Star) },
            new() { Header = "ПОЛ", Key = "Gender", Width = new GridLength(0.9, GridUnitType.Star) },
            new() { Header = "РЕУМАТОЛОГ", Key = "Rheumatologist", Width = new GridLength(1.5, GridUnitType.Star) },
            new() { Header = "ТЕЛЕФОН", Key = "Phone", Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header = "ПОСЛ. СКОР", Key = "Score", Width = new GridLength(1.1, GridUnitType.Star) },
            new() { Header = "АДРЕСА", Key = "Address", Width = new GridLength(1.7, GridUnitType.Star) },
            new() { Header = "ГРАД", Key = "City", Width = new GridLength(1.1, GridUnitType.Star) },
            new() { Header = "ЛЕК", Key = "Medicine", Width = new GridLength(1.7, GridUnitType.Star) },
            new() { Header = "ДИЈАГНОЗА", Key = "Diagnosis", Width = new GridLength(1.7, GridUnitType.Star) }
        ];

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

            _rheumatologistPicker=CreatePicker(
                "Реуматолог",
                _selectedRheumatologistFilter,
                value => _selectedRheumatologistFilter=value);

            _diagnosisPicker=CreatePicker(
                "Дијагноза",
                _selectedDiagnosisFilter,
                value => _selectedDiagnosisFilter=value);

            _medicinePicker=CreatePicker(
                "Лек",
                _selectedMedicineFilter,
                value =>
                {
                    _selectedMedicineFilter=value;
                    _selectedMedicineId=_medicineIdsByDisplay.TryGetValue(value, out var id) ? id : null;
                });

            _genderPicker=CreatePicker(
                "Пол",
                _selectedGenderFilter,
                value => _selectedGenderFilter=value);

            _scorePicker=CreatePicker(
                "Скор",
                _selectedScoreFilter,
                value => _selectedScoreFilter=value);

            var pickers = new List<SparkPickerItem>
            {
                _statusPicker,
                _cityPicker,
                _rheumatologistPicker,
                _diagnosisPicker,
                _genderPicker,
                _scorePicker
            };

            if(IsMedicineFilterEnabled)
                pickers.Insert(4, _medicinePicker);

            return pickers;
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
                        .ThenInclude(x => x.Encounter)
                    .AsNoTracking()
                    .Where(x =>
                        (x.RegistrationDate>=from&&x.RegistrationDate<=to)||
                        x.Scores.Any(s => s.RecordedAt>=from&&s.RecordedAt<=to)||
                        x.PatientMedicines.Any(pm =>
                            pm.StartDate<=to &&
                            (!pm.EndDate.HasValue||pm.EndDate.Value>=from)))
                    .ToListAsync(cts.Token);

                stopwatch.Stop();
                Debug.WriteLine($"[Patients] [4] Query completed in {stopwatch.ElapsedMilliseconds}ms ✓");
                Debug.WriteLine($"[Patients] [5] Patients loaded: {patients.Count}");

                Debug.WriteLine($"[Patients] [6] Refreshing pickers...");
                RefreshPickers(patients);

                Debug.WriteLine($"[Patients] [7] Applying filters...");
                var filtered = ApplyFilters(patients, from, to);
                var filtered_list = filtered.ToList();
                Debug.WriteLine($"[Patients] [8] After filtering: {filtered_list.Count} patients");

                Debug.WriteLine($"[Patients] [9] Building report rows...");
                var rows = filtered_list
                    .Select(patient => CreateRow(patient, from, to))
                    .ToList();

                _selectedMedicineTotalQuantity = _selectedMedicineId.HasValue
                    ? filtered_list
                        .SelectMany(x => x.PatientMedicines)
                        .Where(pm => pm.MedicineId==_selectedMedicineId.Value
                                     &&pm.StartDate<=to
                                     &&(!pm.EndDate.HasValue||pm.EndDate.Value>=from))
                        .Sum(pm => pm.Quantity)
                    : null;

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

            Debug.WriteLine($"[Patients]   - Rheumatologist picker...");
            RefreshPicker(_rheumatologistPicker,
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

            Debug.WriteLine($"[Patients]   - Gender picker...");
            RefreshPicker(_genderPicker,
                Enum.GetValues<Gender>()
                    .Select(x => x.ToDisplay()));

            RefreshPicker(_scorePicker,
                patients
                    .SelectMany(x => x.Scores)
                    .Select(x => x.ScoreText));
        }

        private IEnumerable<Patient> ApplyFilters(
            IEnumerable<Patient> patients,
            DateTime from,
            DateTime to)
        {
            var query = patients;

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

            if(_selectedAllergyFilter=="Со алергии")
            {
                query=query.Where(x => !string.IsNullOrWhiteSpace(x.Allergies));
                Debug.WriteLine($"[Patients]   - Allergy filter: With allergies");
            }

            if(_selectedCityFilter!="Сите")
            {
                query=query.Where(x => x.City==_selectedCityFilter);
                Debug.WriteLine($"[Patients]   - City filter: {_selectedCityFilter}");
            }

            if(_selectedRheumatologistFilter!="Сите")
            {
                query=query.Where(x => x.Doctor?.FullName==_selectedRheumatologistFilter);
                Debug.WriteLine($"[Patients]   - Rheumatologist filter: {_selectedRheumatologistFilter}");
            }

            if(_selectedDiagnosisFilter!="Сите")
            {
                query=query.Where(x => x.Diagnoses.Any(d => d.Mkb10Code?.Code==_selectedDiagnosisFilter));
                Debug.WriteLine($"[Patients]   - Diagnosis filter: {_selectedDiagnosisFilter}");
            }

            if(_selectedMedicineFilter!="Сите")
            {
                // A medicine belongs to the report when its patient-specific
                // prescription window overlaps the selected report period.
                // This is intentionally not limited to the patient's current
                // active medicine list.
                query=query.Where(x =>
                    x.PatientMedicines.Any(pm =>
                        pm.MedicineId==_selectedMedicineId &&
                        pm.StartDate<=to &&
                        (!pm.EndDate.HasValue||pm.EndDate.Value>=from)));

                Debug.WriteLine($"[Patients]   - Medicine filter: {_selectedMedicineFilter} ({from:dd.MM.yyyy} - {to:dd.MM.yyyy})");
            }

            if(_selectedGenderFilter!="Сите"&&
                Enum.TryParse<Gender>(_selectedGenderFilter, out var gender))
            {
                query=query.Where(x => x.Gender==gender);
                Debug.WriteLine($"[Patients]   - Gender filter: {_selectedGenderFilter}");
            }

            if(_selectedScoreFilter!="Сите")
            {
                query=query.Where(x => x.Scores
                    .OrderByDescending(s => s.RecordedAt)
                    .Select(s => s.ScoreText)
                    .FirstOrDefault()==_selectedScoreFilter);
                Debug.WriteLine($"[Patients]   - Score filter: {_selectedScoreFilter}");
            }

            return query;
        }

        private static DynamicReportRow CreateRow(Patient patient, DateTime from, DateTime to)
        {
            return new DynamicReportRow
            {
                Cells=
                [
                    patient.FullName ?? "-",
                    patient.SzboNumber ?? "-",
                    patient.Status.ToDisplay(),
                    patient.Gender.ToDisplay(),
                    patient.Doctor?.FullName ?? "-",
                    patient.Phone ?? "-",
                    patient.Scores
                        .OrderByDescending(x => x.RecordedAt)
                        .Select(x => x.ScoreText)
                        .FirstOrDefault() ?? "Нема скор",
                    patient.Address ?? "-",
                    patient.City ?? "-",
                    BuildMedicinesInfo(patient, from, to),
                    BuildDiagnosisInfo(patient)
                ],
                IsAlertSeverity=false
            };
        }

        private static string BuildScoreHistory(Patient patient)
        {
            if(patient.Scores==null||patient.Scores.Count==0)
                return "Нема историја";

            return string.Join(" | ",
                patient.Scores
                    .OrderByDescending(x => x.RecordedAt)
                    .Select(score =>
                    {
                        var date=score.RecordedAt.ToString("dd.MM.yyyy");
                        var encounter=score.Encounter?.EncounterNumber;

                        return string.IsNullOrWhiteSpace(encounter)
                            ? $"{score.ScoreText} — {date}"
                            : $"{score.ScoreText} — {date} ({encounter})";
                    }));
        }

        private static string BuildMedicinesInfo(
            Patient patient,
            DateTime from,
            DateTime to)
        {
            if(patient.PatientMedicines==null||patient.PatientMedicines.Count==0)
                return "Нема лекови";

            var medicines = patient.PatientMedicines
                .Where(x =>
                    x.Medicine!=null &&
                    x.StartDate<=to &&
                    (!x.EndDate.HasValue||x.EndDate.Value>=from))
                .GroupBy(x => x.Medicine!.Name)
                .Select(group =>
                {
                    var quantity=group.Sum(x => x.Quantity);
                    return $"{group.Key} — количина: {quantity:0.################}";
                });

            var result=string.Join(", ", medicines);

            return string.IsNullOrWhiteSpace(result) ? "Нема лекови" : result;
        }

        private static string BuildDiagnosisInfo(Patient patient)
        {
            if(patient.Diagnoses==null || patient.Diagnoses.Count==0)
                return "Нема дијагноза";

            return string.Join(", ",
                patient.Diagnoses
                    .Where(x => x.Mkb10Code!=null)
                    .Select(x => x.Mkb10Code!.Code)
                    .Distinct());
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