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
    public sealed class ReportPatientSuggestion
    {
        public Guid PatientId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string NationalId { get; init; } = string.Empty;
        public string SzboNumber { get; init; } = string.Empty;
    }

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
        private Guid? _selectedScorePatientId;
        private List<Patient> _loadedPatients = [];

        public bool IsMedicineFilterEnabled { get; private set; }
        public bool IsScoreSearchEnabled { get; private set; }
        public Guid? SelectedScorePatientId => _selectedScorePatientId;

        public void SetScoreSearchEnabled(bool enabled)
        {
            IsScoreSearchEnabled=enabled;
            if(!enabled) _selectedScorePatientId=null;
        }

        public IReadOnlyList<ReportPatientSuggestion> SearchScorePatients(string query)
        {
            if(string.IsNullOrWhiteSpace(query))
                return [];

            var terms=query.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return _loadedPatients
                .Where(x => terms.All(term =>
                    (x.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.PatientNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (!string.IsNullOrWhiteSpace(x.NationalId) && x.NationalId.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(x.SzboNumber) && x.SzboNumber.Contains(term, StringComparison.OrdinalIgnoreCase))))
                .OrderBy(x => x.FullName)
                .Take(8)
                .Select(x => new ReportPatientSuggestion
                {
                    PatientId=x.Id,
                    FullName=x.FullName ?? string.Empty,
                    NationalId=x.NationalId ?? string.Empty,
                    SzboNumber=x.SzboNumber ?? string.Empty
                })
                .ToList();
        }

        public void SelectScorePatient(Guid? patientId)
        {
            _selectedScorePatientId=patientId;
            IsScoreSearchEnabled=patientId.HasValue;
            FiltersChanged?.Invoke();
        }
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
            if(_medicinePicker is not null && !enabled)
                _medicinePicker.SelectedItem="Сите";
            if(!enabled)
            {
                _selectedMedicineFilter="Сите";
                _selectedMedicineId=null;
                _selectedMedicineTotalQuantity=null;
            }
        }

        public void ResetReportFilters()
        {
            _selectedStatusFilter="Сите";
            _selectedAllergyFilter="Сите";
            _selectedCityFilter="Сите";
            _selectedRheumatologistFilter="Сите";
            _selectedDiagnosisFilter="Сите";
            _selectedMedicineFilter="Сите";
            _selectedGenderFilter="Сите";
            _selectedScoreFilter="Сите";
            _selectedMedicineId=null;
            _selectedMedicineTotalQuantity=null;
            _selectedScorePatientId=null;
            IsScoreSearchEnabled=false;

            _refreshingPickers=true;
            try
            {
                foreach(var picker in new[]
                {
                    _statusPicker, _cityPicker, _rheumatologistPicker,
                    _diagnosisPicker, _medicinePicker, _genderPicker, _scorePicker
                })
                {
                    if(picker is null)
                        continue;

                    if(picker.Items.Contains("Сите"))
                        picker.SelectedItem="Сите";
                }
            }
            finally
            {
                _refreshingPickers=false;
            }

            foreach(var tab in new[] { _allTab, _activeTab, _inactiveTab, _allergyTab })
                if(tab is not null)
                    tab.IsSelected=false;

            if(_allTab is not null)
                _allTab.IsSelected=true;
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

        // Keep the original patient-report grid in every state.
        // Selecting a patient only narrows the rows; it must never replace the
        // original columns with a 3-column score-history grid.
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
            _selectedAllergyFilter="Сите";
            if(_allergyTab is not null)
                _allergyTab.IsSelected=false;

            if(_statusPicker is not null)
            {
                _refreshingPickers=true;
                try
                {
                    _statusPicker.SelectedItem=value switch
                    {
                        "Active" => "Активни",
                        "Inactive" => "Неактивни",
                        _ => "Сите"
                    };
                }
                finally
                {
                    _refreshingPickers=false;
                }
            }

            FiltersChanged?.Invoke();
        }

        private void SelectAllergyFilter(SparkTabItem tab, string value)
        {
            foreach(var t in new[] { _allTab, _activeTab, _inactiveTab, _allergyTab })
                if(t!=null) t.IsSelected=false;

            tab.IsSelected=true;
            _selectedAllergyFilter=value;
            _selectedStatusFilter="Сите";

            if(_statusPicker is not null)
            {
                _refreshingPickers=true;
                try
                {
                    _statusPicker.SelectedItem="Сите";
                }
                finally
                {
                    _refreshingPickers=false;
                }
            }

            FiltersChanged?.Invoke();
        }

        public SparkPickerItem? MedicinePicker => _medicinePicker;

        public IEnumerable<SparkPickerItem> BuildPickers()
        {
            _statusPicker=CreatePicker(
                "Статус",
                _selectedStatusFilter,
                value =>
                {
                    _selectedStatusFilter=value;
                    _selectedScorePatientId=null;
                    IsScoreSearchEnabled=false;
                });

            _cityPicker=CreatePicker(
                "Град",
                _selectedCityFilter,
                value =>
                {
                    _selectedCityFilter=value;
                    _selectedScorePatientId=null;
                    IsScoreSearchEnabled=false;
                });

            _rheumatologistPicker=CreatePicker(
                "Реуматолог",
                _selectedRheumatologistFilter,
                value =>
                {
                    _selectedRheumatologistFilter=value;
                    _selectedScorePatientId=null;
                    IsScoreSearchEnabled=false;
                });

            _diagnosisPicker=CreatePicker(
                "Дијагноза",
                _selectedDiagnosisFilter,
                value =>
                {
                    _selectedDiagnosisFilter=value;
                    _selectedScorePatientId=null;
                    IsScoreSearchEnabled=false;
                });

            _medicinePicker=CreatePicker(
                "Лек",
                _selectedMedicineFilter,
                value =>
                {
                    _selectedMedicineFilter=value;
                    _selectedMedicineId=_medicineIdsByDisplay.TryGetValue(value, out var id) ? id : null;

                    // Choosing any report picker returns the grid to the normal
                    // all-patient mode. The picker filter is then applied to all
                    // patients instead of remaining locked to the searched patient.
                    _selectedScorePatientId=null;
                    IsScoreSearchEnabled=false;
                });

            _genderPicker=CreatePicker(
                "Пол",
                _selectedGenderFilter,
                value =>
                {
                    _selectedGenderFilter=value;
                    _selectedScorePatientId=null;
                    IsScoreSearchEnabled=false;
                });

            _scorePicker=CreatePicker(
                "Скор",
                _selectedScoreFilter,
                value =>
                {
                    _selectedScoreFilter=value;
                    _selectedScorePatientId=null;
                    IsScoreSearchEnabled=false;
                });

            // Medicine is intentionally rendered below the "Пребарување по лек"
            // checkbox on the patient-report page. Score filtering is no longer part
            // of this dashboard UI.
            _medicinePicker.SelectedItem=_selectedMedicineFilter;
            var pickers = new List<SparkPickerItem>
            {
                _statusPicker,
                _cityPicker,
                _rheumatologistPicker,
                _diagnosisPicker,
                _genderPicker
            };

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

        //public IEnumerable<SparkButtonItem> BuildButtons()
        //{
        //    return
        //    [
        //        new SparkButtonItem
        //        {
        //            Label = "Освежи",
        //            IsPrimary = true,
        //            Command = new RelayCommand(() => FiltersChanged?.Invoke())
        //        }
        //    ];
        //}

        public async Task<List<DynamicReportRow>> GenerateAsync(DateTime from, DateTime to)
        {
            var allPeriod=from==DateTime.MinValue && to==DateTime.MaxValue;
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
                // Load the complete patient set so the single patient search behaves
                // like the Dashboard search and is not limited by the selected report period.
                var patients = await db.Patients
                    .Include(x => x.Doctor)
                        .ThenInclude(x => x!.User)
                    .Include(x => x.PatientMedicines)
                        .ThenInclude(x => x.Medicine)
                    .Include(x => x.PatientMedicines)
                        .ThenInclude(x => x.Encounter)
                    .Include(x => x.Diagnoses)
                        .ThenInclude(x => x.Mkb10Code)
                    .Include(x => x.TherapyCycles)
                    .Include(x => x.Scores)
                        .ThenInclude(x => x.Encounter)
                            .ThenInclude(x => x.Doctor)
                                .ThenInclude(x => x!.User)
                    .AsNoTracking()
                    .ToListAsync(cts.Token);

                stopwatch.Stop();
                Debug.WriteLine($"[Patients] [4] Query completed in {stopwatch.ElapsedMilliseconds}ms ✓");
                Debug.WriteLine($"[Patients] [5] Patients loaded: {patients.Count}");
                _loadedPatients=patients;

                Debug.WriteLine($"[Patients] [6] Refreshing pickers...");
                RefreshPickers(patients, from, to);

                Debug.WriteLine($"[Patients] [7] Applying filters...");
                var filtered = ApplyFilters(patients, from, to);
                var filtered_list = filtered.ToList();
                Debug.WriteLine($"[Patients] [8] After filtering: {filtered_list.Count} patients");

                Debug.WriteLine($"[Patients] [9] Building report rows...");
                List<DynamicReportRow> rows;
                if(_selectedScorePatientId.HasValue)
                {
                    // Patient search is a row filter only. Preserve every original
                    // patient-grid field and show the complete record for the
                    // selected patient.
                    var selectedPatient=patients.FirstOrDefault(x => x.Id==_selectedScorePatientId.Value);
                    rows=selectedPatient is null
                        ? []
                        : [CreateRow(selectedPatient, from, to, includeScoreHistory:true)];
                }
                else
                {
                    rows=filtered_list.Select(patient => CreateRow(patient, from, to)).ToList();
                }

                _selectedMedicineTotalQuantity = _selectedMedicineId.HasValue
                    ? patients
                        .SelectMany(x => x.PatientMedicines)
                        .Where(pm => pm.MedicineId==_selectedMedicineId.Value
                                   && pm.Encounter != null
                                   && IsWithinReportPeriod(pm.Encounter.EncounterDate, from, to))
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

        private void RefreshPickers(List<Patient> patients, DateTime from, DateTime to)
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
            _medicineIdsByDisplay.Clear();
            var medicineOptions=patients
                .SelectMany(x => x.PatientMedicines)
                .Where(x =>
                    x.Medicine!=null &&
                    x.Encounter!=null &&
                    IsWithinReportPeriod(x.Encounter.EncounterDate, from, to))
                .GroupBy(x => x.Medicine!.Id)
                .Select(g => g.First().Medicine!)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Strength)
                .ToList();

            foreach(var medicine in medicineOptions)
                _medicineIdsByDisplay[medicine.FullName]=medicine.Id;

            RefreshPicker(_medicinePicker, medicineOptions.Select(x => x.FullName));

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
            // Keep report rows period-scoped while the patient search itself
            // remains global across all registered patients.
            var allPeriod = from == DateTime.MinValue && to == DateTime.MaxValue;
            var query = allPeriod
                ? patients.AsEnumerable()
                : patients.Where(x =>
                    IsWithinReportPeriod(x.RegistrationDate, from, to)||
                    x.Scores.Any(s => IsWithinReportPeriod(s.RecordedAt, from, to))||
                    x.PatientMedicines.Any(pm =>
                        pm.Encounter != null &&
                        IsWithinReportPeriod(pm.Encounter.EncounterDate, from, to)));

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
                // Medicine filtering is encounter-based: only medicines recorded on
                // encounters inside the selected report period are included.
                query=query.Where(x =>
                    x.PatientMedicines.Any(pm =>
                        pm.MedicineId==_selectedMedicineId &&
                        pm.Encounter != null &&
                        IsWithinReportPeriod(pm.Encounter.EncounterDate, from, to)));

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

        private static DynamicReportRow CreateRow(
            Patient patient,
            DateTime from,
            DateTime to,
            bool includeScoreHistory=false)
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
                    includeScoreHistory
                        ? BuildScoreHistory(patient, from, to)
                        : patient.Scores
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

        private static string BuildScoreHistory(Patient patient, DateTime from, DateTime to)
        {
            if(patient.Scores==null||patient.Scores.Count==0)
                return "Нема историја";

            // The selected patient's history must obey the same inclusive-from,
            // exclusive-to period as the report query and medicine history.
            var periodScores=patient.Scores
                .Where(score => IsWithinReportPeriod(score.RecordedAt, from, to))
                .OrderByDescending(score => score.RecordedAt)
                .ToList();

            if(periodScores.Count==0)
                return "Нема историја во избраниот период";

            return string.Join(" | ",
                periodScores.Select(score =>
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
                    x.Encounter != null &&
                    IsWithinReportPeriod(x.Encounter.EncounterDate, from, to))
                .GroupBy(x => x.Medicine!.Name)
                .Select(group =>
                {
                    var quantity=group.Sum(x => x.Quantity);
                    return $"{group.Key} — количина: {quantity:0.################}";
                });

            var result=string.Join(", ", medicines);

            return string.IsNullOrWhiteSpace(result) ? "Нема лекови" : result;
        }

        private static bool IsWithinReportPeriod(DateTime value, DateTime from, DateTime to)
        {
            if(from==DateTime.MinValue && to==DateTime.MaxValue)
                return true;

            return value>=from && value<to;
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