using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities.Reports
{
    public sealed class PatientsReportProvider : IReportProvider
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

        private string _selectedAllergyFilter = "Сите";
        private string _selectedCityFilter = "Сите";
        private string _selectedDoctorFilter = "Сите";
        private string _selectedDiagnosisFilter = "Сите";
        private string _selectedMedicineFilter = "Сите";
        private string _selectedGenderFilter = "Сите";
        private SparkTabItem? _allTab, _allergyTab;
        private SparkPickerItem? _cityPicker;
        private SparkPickerItem? _doctorPicker;
        private SparkPickerItem? _diagnosisPicker;
        private SparkPickerItem? _medicinePicker;
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
                  new() { Header = "ПОЛ", Key = "Gender", Width = new GridLength(90) },
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

            _genderPicker=CreatePicker(
     "Пол",
     _selectedGenderFilter,
     value => _selectedGenderFilter=value);

            return
            [
                _cityPicker,
        _doctorPicker,
        _diagnosisPicker,
        _medicinePicker,
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

                Debug.WriteLine($"Picker '{placeholder}' changed -> {value}");

                setter(value);

                Debug.WriteLine("FiltersChanged invoked");

                FiltersChanged?.Invoke();
            };

            return picker;
        }
      private void RefreshPicker(
    SparkPickerItem? picker,
    IEnumerable<string?> values)
{
    if (picker == null)
        return;

    _refreshingPickers = true;

    try
    {
        var selected = picker.SelectedItem as string ?? "Сите";

        picker.Items.Clear();
        picker.Items.Add("Сите");

        foreach (var value in values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x))
        {
            picker.Items.Add(value!);
        }

        picker.SelectedItem =
            picker.Items.Contains(selected)
                ? selected
                : "Сите";
    }
    finally
    {
        _refreshingPickers = false;
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
        // GENERATE
        // =====================================================
        public async Task<List<DynamicReportRow>> GenerateAsync(DateTime from, DateTime to)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var patients = await db.Patients
                .Include(x => x.Doctor)
                    .ThenInclude(x => x!.User)
                .Include(x => x.PatientMedicines)
                    .ThenInclude(x => x.Medicine)
                .Include(x => x.Diagnoses)
                    .ThenInclude(x => x.Mkb10Code)
                .AsNoTracking()
                .Where(x => x.RegistrationDate>=from&&
                            x.RegistrationDate<=to)
                .ToListAsync();

            RefreshPickers(patients);

            return ApplyFilters(patients)
                .Select(CreateRow)
                .ToList();
        }
        private void RefreshPickers(List<Patient> patients)
        {
            RefreshPicker(_cityPicker,
                patients.Select(x => x.City));

            RefreshPicker(_doctorPicker,
                patients
                    .Where(x => x.Doctor!=null)
                    .Select(x => x.Doctor!.FullName));

            RefreshPicker(_diagnosisPicker,
                patients
                    .SelectMany(x => x.Diagnoses)
                    .Where(x => x.Mkb10Code!=null)
                    .Select(x => x.Mkb10Code!.Code));

            RefreshPicker(_medicinePicker,
                patients
                    .SelectMany(x => x.PatientMedicines)
                    .Where(x => x.Medicine!=null)
                    .Select(x => x.Medicine!.Name));

            RefreshPicker(_genderPicker,
                Enum.GetValues<Gender>()
                    .Select(x => x.ToString()));
        }
        private IEnumerable<Patient> ApplyFilters(IEnumerable<Patient> patients)
        {
            var query = patients;

            if(_selectedAllergyFilter=="Со алергии")
            {
                query=query.Where(x =>
                    !string.IsNullOrWhiteSpace(x.Allergies));
            }

            if(_selectedCityFilter!="Сите")
            {
                query=query.Where(x =>
                    x.City==_selectedCityFilter);
            }

            if(_selectedDoctorFilter!="Сите")
            {
                query=query.Where(x =>
                    x.Doctor?.FullName==_selectedDoctorFilter);
            }

            if(_selectedDiagnosisFilter!="Сите")
            {
                query=query.Where(x =>
                    x.Diagnoses.Any(d =>
                        d.Mkb10Code?.Code==_selectedDiagnosisFilter));
            }

            if(_selectedMedicineFilter!="Сите")
            {
                query=query.Where(x =>
                    x.PatientMedicines.Any(pm =>
                        pm.Medicine?.Name==_selectedMedicineFilter));
            }

            if(_selectedGenderFilter!="Сите"&&
                Enum.TryParse<Gender>(_selectedGenderFilter, out var gender))
            {
                query=query.Where(x =>
                    x.Gender==gender);
            }

            return query;
        }
        private static DynamicReportRow CreateRow(Patient patient)
        {
            var hasAllergy = !string.IsNullOrWhiteSpace(patient.Allergies);

            return new DynamicReportRow
            {
                Cells=
                [
                    patient.FullName ?? "-",
                    patient.Gender.ToString(),
                    patient.Phone ?? "-",
                    PrivacyMaskHelper.MaskNationalId(patient.NationalId) ?? "-",
                    patient.Address ?? "-",
                    patient.City ?? "-",
                    patient.CreatedAt.ToString("dd.MM.yyyy"),
                    BuildMedicalInfo(patient)
                ],
                IsAlertSeverity=hasAllergy
            };
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