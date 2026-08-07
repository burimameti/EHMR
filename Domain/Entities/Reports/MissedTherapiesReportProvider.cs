using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities.Reports
{
    public sealed class MissedTherapiesReportProvider : IReportProvider
    {
        private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
        private string _selectedReasonFilter = "Сите";
        private string _selectedPatientFilter = "Сите";
        private string _selectedDoctorFilter = "Сите";
        private string _selectedDiagnosisFilter = "Сите";
        private string _selectedMedicineFilter = "Сите";
        private string _selectedProtocolFilter = "Сите";
        private string _selectedCycleFilter = "Сите";
        private string _selectedCityFilter = "Сите";
        private string _selectedGenderFilter = "Сите";

        private SparkPickerItem? _patientPicker;
        private SparkPickerItem? _doctorPicker;
        private SparkPickerItem? _diagnosisPicker;
        private SparkPickerItem? _medicinePicker;
        private SparkPickerItem? _protocolPicker;
        private SparkPickerItem? _cyclePicker;
        private SparkPickerItem? _cityPicker;
        private SparkPickerItem? _genderPicker;
        private SparkPickerItem? _reasonPicker;
        // единствен извор на вистина за reason филтерот


        private SparkTabItem? _allTab, _withReasonTab, _withoutReasonTab;


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
      new() { Header = "ПАЦИЕНТ", Key = "Patient", Width = new GridLength(220) },
    new() { Header = "МАТИЧЕН БРОЈ", Key = "NationalId", Width = new GridLength(150) },
    new() { Header = "ДОКТОР", Key = "Doctor", Width = new GridLength(220) },
    new() { Header = "ДИЈАГНОЗА", Key = "Diagnosis", Width = new GridLength(170) },
    new() { Header = "ЦИКЛУС", Key = "Cycle", Width = new GridLength(100) },
    new() { Header = "СТАРТ", Key = "Start", Width = new GridLength(110) },
    new() { Header = "КРАЈ", Key = "End", Width = new GridLength(110) },
    new() { Header = "ПРОПУШТЕНА", Key = "Missed", Width = new GridLength(120) },
    new() { Header = "ПРИЧИНА", Key = "Reason", Width = new GridLength(130) },
    new() { Header = "ЗАБЕЛЕШКА", Key = "Notes", Width = GridLength.Star }
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
            _reasonPicker=CreatePicker("Причина", _selectedReasonFilter, x => _selectedReasonFilter=x);
            _patientPicker=CreatePicker("Пациент", _selectedPatientFilter, x => _selectedPatientFilter=x);
            _doctorPicker=CreatePicker("Доктор", _selectedDoctorFilter, x => _selectedDoctorFilter=x);
            _cityPicker=CreatePicker("Град", _selectedCityFilter, x => _selectedCityFilter=x);
            _cyclePicker=CreatePicker("Циклус", _selectedCycleFilter, x => _selectedCycleFilter=x);
            _genderPicker=CreatePicker("Пол", _selectedGenderFilter, x => _selectedGenderFilter=x);
            _diagnosisPicker=CreatePicker("Дијагноза", _selectedDiagnosisFilter, x => _selectedDiagnosisFilter=x);
            _medicinePicker=CreatePicker("Лек", _selectedMedicineFilter, x => _selectedMedicineFilter=x);

            _reasonPicker.Items.Clear();
            _reasonPicker.Items.Add("Сите");
            _reasonPicker.Items.Add("Со причина");
            _reasonPicker.Items.Add("Без причина");

            return
            [
                _reasonPicker, _patientPicker, _doctorPicker,
        _cityPicker, _cyclePicker, _genderPicker,
        _diagnosisPicker, _medicinePicker
            ];
        }
        private SparkPickerItem CreatePicker(
    string placeholder,
    string selected,
    Action<string> setter)
        {
            var picker = new SparkPickerItem
            {
                Placeholder=placeholder
            };

            picker.Items.Add("Сите");
            picker.SelectedItem=selected;

            picker.PropertyChanged+=(_, e) =>
            {
                if(e.PropertyName!=nameof(SparkPickerItem.SelectedItem))
                    return;

                if(picker.SelectedItem is not string value)
                    return;

                setter(value);
                FiltersChanged?.Invoke();
            };

            return picker;
        }

        private static void RefreshPicker(
            SparkPickerItem? picker,
            IEnumerable<string?> values)
        {
            if(picker==null)
                return;

            var selected = picker.SelectedItem as string??"Сите";

            picker.Items.Clear();
            picker.Items.Add("Сите");

            foreach(var item in values
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x))
            {
                picker.Items.Add(item!);
            }

            picker.SelectedItem=
                picker.Items.Contains(selected)
                    ? selected
                    : "Сите";
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
                    .ThenInclude(p => p.Doctor).ThenInclude(d => d.User)
                .Include(x => x.Patient)
                    .ThenInclude(p => p.Diagnoses)
                        .ThenInclude(d => d.Mkb10Code)
                .AsNoTracking()
                .Where(x => x.Status==TherapyStatus.Missed
                    &&x.StartDate>=from
                    &&(x.EndDate==null||x.EndDate<=to))
                .OrderByDescending(x => x.EndDate)
                .ToListAsync();
            RefreshPicker(_patientPicker,
                data.Select(x => x.Patient?.FullName));

            RefreshPicker(_doctorPicker,
                data.Select(x => x.Patient?.Doctor?.FullName));

            RefreshPicker(_cityPicker,
                data.Select(x => x.Patient?.City));

            RefreshPicker(_diagnosisPicker,
                data.SelectMany(x => x.Patient!.Diagnoses)
                    .Select(x => x.Mkb10Code!.Code));

            RefreshPicker(_medicinePicker,
                data.SelectMany(x => x.Patient!.PatientMedicines)
                    .Select(x => x.Medicine!.Name));

            //RefreshPicker(_protocolPicker,
            //    data.Select(x => x.Protocol!.Name));

            RefreshPicker(_cyclePicker,
                data.Select(x => $"Цикл #{x.TherapyCyleNumber}"));

            RefreshPicker(_genderPicker,
                Enum.GetValues<Gender>()
                    .Select(x => x.ToString()));
            RefreshTabCounts(data);

            var filtered = data.AsEnumerable();

            switch(_selectedReasonFilter)
            {
                case "Со причина":
                    filtered=filtered.Where(x => !string.IsNullOrWhiteSpace(x.Notes));
                    break;

                case "Без причина":
                    filtered=filtered.Where(x => string.IsNullOrWhiteSpace(x.Notes));
                    break;
            }

            if(_selectedPatientFilter!="Сите")
                filtered=filtered.Where(x =>
                    x.Patient?.FullName==_selectedPatientFilter);

            if(_selectedDoctorFilter!="Сите")
                filtered=filtered.Where(x =>
                    x.Patient?.Doctor?.FullName==_selectedDoctorFilter);

            if(_selectedCityFilter!="Сите")
                filtered=filtered.Where(x =>
                    x.Patient?.City==_selectedCityFilter);

            if(_selectedCycleFilter!="Сите")
                filtered=filtered.Where(x =>
                    $"Цикл #{x.TherapyCyleNumber}"==_selectedCycleFilter);

            if(_selectedGenderFilter!="Сите"&&
                Enum.TryParse<Gender>(_selectedGenderFilter, out var gender))
            {
                filtered=filtered.Where(x =>
                    x.Patient!.Gender==gender);
            }
            if(_selectedDiagnosisFilter!="Сите")
                filtered=filtered.Where(x =>
                    x.Patient!.Diagnoses.Any(d => d.Mkb10Code!.Code==_selectedDiagnosisFilter));

            if(_selectedMedicineFilter!="Сите")
                filtered=filtered.Where(x =>
                    x.Patient!.PatientMedicines.Any(pm => pm.Medicine!.Name==_selectedMedicineFilter));
            return filtered.Select(x =>
            {
                var noReason = string.IsNullOrWhiteSpace(x.Notes);

                var diagnosis = x.Patient?.Diagnoses?
                    .Select(d => d.Mkb10Code?.Code)
                    .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))
                    ??"-";

                var doctor = x.Patient?.Doctor?.FullName??"-";

                var cycle = x.TherapyCyleNumber !=null
                    ? $"Цикл #{x.TherapyCyleNumber}"
                    : "-";

                var missedDate = x.EndDate.HasValue
                    ? x.EndDate.Value.ToString("dd.MM.yyyy")
                    : "-";

                var startDate = x.StartDate.HasValue
                    ? x.StartDate.Value.ToString("dd.MM.yyyy")
                    : "-";

                return new DynamicReportRow
                {
                    Cells=
     [
         // 1 ПАЦИЕНТ
         x.Patient?.FullName ?? "-",

        // 2 МАТИЧЕН БРОЈ
        PrivacyMaskHelper.MaskNationalId(x.Patient?.NationalId) ?? "-",

        // 3 ДОКТОР
        doctor,

        // 4 ДИЈАГНОЗА
        diagnosis,

        // 5 ЦИКЛУС
        cycle,

        // 6 СТАРТ
        startDate,

        // 7 КРАЈ
        x.EndDate.HasValue
            ? x.EndDate.Value.ToString("dd.MM.yyyy")
            : "-",

        // 8 ПРОПУШТЕНА
        missedDate,

        // 9 ПРИЧИНА
        string.IsNullOrWhiteSpace(x.Notes)
            ? "Без причина"
            : "Со причина",

        // 10 ЗАБЕЛЕШКА
        x.Notes ?? "-"
     ],

                    IsAlertSeverity=noReason
                };
            })
  .ToList();
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