using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Converters;
using EHMR.Domain.Entities;
using EHMR.Services;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Encounters;

public abstract partial class EncounterBaseViewModel : ObservableObject, IDisposable
{
    protected readonly IEncounterDetailService EncounterService;
    protected CancellationTokenSource _cts = new();

    protected EncounterBaseViewModel(IEncounterDetailService encounterService)
    {
        EncounterService=encounterService;
    }

    // ================= CORE =================
    [ObservableProperty] protected Encounter encounter = new();
    [ObservableProperty] public string pageTitle = string.Empty;

    // ================= UI STATE =================
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isEditMode;
    [ObservableProperty] private bool isReadOnly = true;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    // ================= WORKFLOW STATUS (existing pattern) =================
    public ObservableCollection<EncounterStatusOption> StatusOptions { get; set; } = new();

    // ================= TYPE / PRIORITY / SCHEDULE =================
    public ObservableCollection<string> EncounterTypeOptions => EncounterFormLookups.EncounterType.ToObservableCollection();
    public ObservableCollection<string> PriorityOptions => EncounterFormLookups.Priority.ToObservableCollection();

    public string EncounterTypeDisplay
    {
        get => EncounterFormLookups.EncounterType.ToDisplay(Encounter.EncounterType);
        set
        {
            var internalValue = EncounterFormLookups.EncounterType.ToInternal(value);
            if(Encounter.EncounterType==internalValue)
                return;
            Encounter.EncounterType=internalValue;
            OnPropertyChanged();
        }
    }

    public string PriorityDisplay
    {
        get => EncounterFormLookups.Priority.ToDisplay(Encounter.Priority);
        set
        {
            var internalValue = EncounterFormLookups.Priority.ToInternal(value);
            if(Encounter.Priority==internalValue)
                return;
            Encounter.Priority=internalValue;
            OnPropertyChanged();
        }
    }

    public DateTime ScheduledStartDate
    {
        get => Encounter.ScheduledStart??DateTime.Today;
        set
        {
            var time = Encounter.ScheduledStart?.TimeOfDay??TimeSpan.Zero;
            Encounter.ScheduledStart=value.Date+time;
            OnPropertyChanged();
        }
    }

    public TimeSpan ScheduledStartTime
    {
        get => Encounter.ScheduledStart?.TimeOfDay??new TimeSpan(9, 0, 0);
        set
        {
            var date = Encounter.ScheduledStart?.Date??DateTime.Today;
            Encounter.ScheduledStart=date+value;
            OnPropertyChanged();
        }
    }

    private void LoadStatusOptions()
    {
        StatusOptions.Clear();
        var options = Enum.GetValues<EncounterStatus>()
            .Select(x => new EncounterStatusOption
            {
                Value=x,
                Label=EncounterStatusLocalization.ToMk(x)
            });

        foreach(var option in options)
        {
            StatusOptions.Add(option);
        }
    }

    protected void OnError(string message)
    {
        ErrorMessage=message;
        HasError=true;
    }

    protected void ClearError()
    {
        ErrorMessage=string.Empty;
        HasError=false;
    }

    // ================= SAFE EXECUTION =================
    protected async Task ExecuteSafeAsync(Func<Task> action, string defaultErrorMessage)
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;
            ClearError();
            await action();
        }
        catch(Exception ex)
        {
            OnError($"{defaultErrorMessage}: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // ================= LOOKUPS =================
    [ObservableProperty] protected ObservableCollection<Patient> patients = new();
    protected List<string> PatientList = new();
    protected List<string> DoctorList = new();
    [ObservableProperty] protected ObservableCollection<Doctor> doctors = new();

    [ObservableProperty] protected Patient? selectedPatient;
    [ObservableProperty] protected Doctor? selectedDoctor;

    // ================= MKB / DIAGNOSES =================
    [ObservableProperty] protected ObservableCollection<Mkb10Code> mkbResults = new();
    [ObservableProperty] protected ObservableCollection<Diagnosis> diagnoses = new();

    [ObservableProperty] protected string mkbSearchText = string.Empty;
    [ObservableProperty] protected bool showMkbDropdown;

    // ================= PRESCRIPTIONS =================
    [ObservableProperty] protected ObservableCollection<Prescription> prescriptions = new();
    [ObservableProperty] private string encounterDiagnosisNotes = string.Empty;

    // =====================================================
    // INIT
    // =====================================================
    public virtual async Task InitializeAsync(Guid? encounterId = null)
    {
        await ExecuteSafeAsync(async () =>
        {
            await LoadLookupsAsync();
            LoadStatusOptions();

            if(encounterId==null)
            {
                Encounter=new Encounter
                {
                    Id=Guid.NewGuid(),
                    EncounterDate=DateTime.Now
                };

                IsEditMode=true;
                IsReadOnly=false;
                EncounterDiagnosisNotes=string.Empty;
                Diagnoses.Clear();
                Prescriptions.Clear();
                return;
            }

            var dto = await EncounterService.GetEncounter(encounterId.Value);
            Encounter=dto.Encounter;
            Diagnoses=new ObservableCollection<Diagnosis>(dto.Diagnoses);
            Prescriptions=new ObservableCollection<Prescription>(dto.Prescriptions);

            EncounterDiagnosisNotes=Encounter.ClinicalNotes;
            SelectedPatient=Patients.FirstOrDefault(x => x.Id==Encounter.PatientId);
            SelectedDoctor=Doctors.FirstOrDefault(x => x.Id==Encounter.DoctorId);

            IsReadOnly=true;
            IsEditMode=false;
        }, "Грешка при вчитување на прегледот");
    }

    protected async Task LoadLookupsAsync()
    {
        Patients=new ObservableCollection<Patient>(await EncounterService.GetPatients());
        PatientList=Patients.Select(p => p.FullName).ToList();
        Doctors=new ObservableCollection<Doctor>(await EncounterService.GetDoctors());
        DoctorList=Doctors.Select(d => d.FullName).ToList();
    }

    // =====================================================
    // PROPERTY CHANGED INTERCEPTORS
    // =====================================================
    partial void OnMkbSearchTextChanging(string value)
    {
        if(string.IsNullOrWhiteSpace(value)||value.Length<2)
        {
            MkbResults.Clear();
            ShowMkbDropdown=false;
            return;
        }

        _=ExecuteSafeAsync(async () =>
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts=new CancellationTokenSource();

            try
            {
                var result = await EncounterService.SearchDiagnoses(value, _cts.Token);
                MkbResults=new ObservableCollection<Mkb10Code>(result);
                ShowMkbDropdown=MkbResults.Count>0;
            }
            catch(OperationCanceledException)
            {
                // Silently drop thread cancellations when user is actively typing fast
            }
        }, "Грешка при пребарување дијагнози");
    }

    // =====================================================
    // COMMANDS
    // =====================================================
    [RelayCommand]
    public void ToggleEditMode()
    {
        IsEditMode=!IsEditMode;
        IsReadOnly=!IsEditMode;
    }

    [RelayCommand]
    public void Cancel()
    {
        IsEditMode=false;
        IsReadOnly=true;
    }

    partial void OnSelectedPatientChanged(Patient? value)
    {
        if(Encounter!=null)
        {
            Encounter.PatientId=value?.Id??Guid.Empty;
            Diagnoses.Clear();
        }
    }

    partial void OnSelectedDoctorChanged(Doctor? value)
    {
        if(Encounter!=null)
        {
            Encounter.DoctorId=value?.Id??Guid.Empty;
        }
    }

    [RelayCommand]
    protected void RemoveMkb(Diagnosis diagnosis)
    {
        if(diagnosis==null) return;

        if(Diagnoses.Contains(diagnosis))
        {
            Diagnoses.Remove(diagnosis);
        }
    }

    [RelayCommand]
    protected void AddMkb(Mkb10Code code)
    {
        if(code==null) return;

        if(Diagnoses.Any(x => x.Mkb10CodeId==code.Id))
            return;

        var diagnosis = new Diagnosis
        {
            Id=Guid.NewGuid(),
            EncounterId=Encounter.Id==Guid.Empty ? null : Encounter.Id,
            PatientId=Encounter.PatientId,
            Mkb10CodeId=code.Id,
            Mkb10Code=code,
            DiagnosedAt=DateTime.Now,
            IsPrimary=Diagnoses.Count==0,
            Status=DiagnosisStatus.Active
        };

        Diagnoses.Add(diagnosis);

        MkbSearchText=string.Empty;
        MkbResults.Clear();
        ShowMkbDropdown=false;
    }

    [RelayCommand]
    protected async Task SearchMkbAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query)||query.Length<2)
        {
            MkbResults.Clear();
            ShowMkbDropdown=false;
            return;
        }

        _cts.Cancel();
        _cts.Dispose();
        _cts=new CancellationTokenSource();

        await ExecuteSafeAsync(async () =>
        {
            try
            {
                var result = await EncounterService.SearchDiagnoses(query, _cts.Token);

                MkbResults.Clear();
                foreach(var item in result)
                {
                    MkbResults.Add(item);
                }

                ShowMkbDropdown=MkbResults.Count>0;
            }
            catch(OperationCanceledException)
            {
                // Silently swallow cancellations when a user is typing rapidly
            }
        }, "Грешка при пребарување дијагнози");
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}