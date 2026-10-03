using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Encounters;

public partial class EncounterEditViewModel : EncounterBaseViewModel
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;
    private readonly IPatientClinicalReportService _clinicalReportService;
    private readonly IAuthorizationService _authorizationService;

    [ObservableProperty]
    private bool scheduleNextFollowUp;

    [ObservableProperty]
    private DateTime nextFollowUpDate=DateTime.Today.AddDays(7);

    [ObservableProperty]
    private string? selectedFollowUpInterval;

    [ObservableProperty]
    private ObservableCollection<Encounter> recentEncounters=[];

    [ObservableProperty]
    private PatientDocument? latestPatientDocument;

    public DateTime FollowUpMinimumDate => DateTime.Today;

    public ObservableCollection<string> FollowUpIntervalOptions { get; } =
        new()
        {
            "1 недела",
            "2 недели",
            "3 недели",
            "1 месец",
            "3 месеци",
            "6 месеци",
            "1 година"
        };

    public bool HasEncounterMedicines => EncounterMedicines.Count>0;

    partial void OnSelectedFollowUpIntervalChanged(string? value)
    {
        if(string.IsNullOrWhiteSpace(value)) return;

        NextFollowUpDate=value switch
        {
            "1 недела" => DateTime.Today.AddDays(7),
            "2 недели" => DateTime.Today.AddDays(14),
            "3 недели" => DateTime.Today.AddDays(21),
            "1 месец" => DateTime.Today.AddMonths(1),
            "3 месеци" => DateTime.Today.AddMonths(3),
            "6 месеци" => DateTime.Today.AddMonths(6),
            "1 година" => DateTime.Today.AddYears(1),
            _ => NextFollowUpDate
        };
    }
    public EncounterEditViewModel(
        IEncounterDetailService service,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        ISelectedItemService<Encounter> selectedItemService,
        IPatientClinicalReportService clinicalReportService,
        IAuthorizationService authorizationService)
        : base(service, navigationService, userDialogService, authorizationService)
    {
        _selectedItemService=selectedItemService;
        _clinicalReportService=clinicalReportService;
        _authorizationService=authorizationService;
        PageTitle="Промена на преглед";
        EncounterMedicines.CollectionChanged+=OnEncounterMedicinesChanged;
    }

    private void OnEncounterMedicinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(HasEncounterMedicines));

    private void RefreshSidePanel()
    {
        RecentEncounters=new ObservableCollection<Encounter>(
            PatientEncounters.OrderByDescending(e => e.EncounterDate));

        LatestPatientDocument=PatientDocuments
            .Where(d => !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .FirstOrDefault();
    }

    [RelayCommand]
    private async Task GenerateClinicalReportAsync()
    {
        if(!CanPrint)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате овластување за печатење/генерирање извештај.", "ОК");
            return;
        }

        if(Encounter.PatientId==Guid.Empty) return;

        try
        {
            var path=await _clinicalReportService.GeneratePdfAsync(
                Encounter.PatientId,
                Encounter.Id,
                Encounter.AppointmentId,
                    $"Детален извештај - {SelectedPatient?.FullName??"Пациент"}");

            await Launcher.Default.OpenAsync(new OpenFileRequest(
                System.IO.Path.GetFileName(path),
                new ReadOnlyFile(path)));
        }
        catch(Exception ex)
        {
            await UserDialogService.ShowAlertAsync(
                "Извештај",
                $"PDF извештајот не може да се генерира: {ex.Message}",
                "ОК");
        }
    }

    public bool CanAdminEditEncounter =>
        ( _authorizationService.HasRole(UserRole.Admin) || _authorizationService.HasRole(UserRole.SuperAdmin) ) && CanUpdate;

    private bool CanModifyEncounter => CanAdminEditEncounter;
    public bool CanChangeStatus => CanAdminEditEncounter || CanApprove;

    public async Task LoadAsync()
    {
        var selected = _selectedItemService.SelectedItem;
        if(selected==null||selected.Id==Guid.Empty)
        {
            OnError("Не е избран преглед за промена.");
            return;
        }

        // InitializeAsync() already loads the patient context (Diagnoses, PatientMedicines
        // full history, etc.) via LoadPatientContextAsync for the existing-encounter path —
        // EncounterMedicines starts empty here, so only medicines the user adds/removes
        // during THIS edit session get touched on save; the rest of the patient's
        // medicine history is left completely alone.
        await InitializeAsync(selected.Id);

        EncounterMedicines.CollectionChanged-=OnEncounterMedicinesChanged;
        EncounterMedicines=new ObservableCollection<PatientMedicine>(
            PatientMedicines.Where(x => x.EncounterId==Encounter.Id));
        EncounterMedicines.CollectionChanged+=OnEncounterMedicinesChanged;
        RefreshSidePanel();

        // load the cycle picker for this encounter's patient and preselect its current cycle
   

        if(!CanAdminEditEncounter)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Уредување на веќе зачуван преглед е дозволено само за администратор.", "ОК");
            await NavigationService.GoBackAsync();
            return;
        }

        IsEditMode=true;
        IsReadOnly=false;
        _selectedItemService.SelectedItem=null; // consume — prevents stale ID on next navigation
    }

    [RelayCommand]
    private async Task PreviewDocument(PatientDocument doc)
    {
        if(doc is null||string.IsNullOrWhiteSpace(doc.StoredPath)) return;

        if(!File.Exists(doc.StoredPath))
        {
            await UserDialogService.ShowAlertAsync("Документ", "Документот не е пронајден на дискот.", "Во ред");
            return;
        }

        await Launcher.Default.OpenAsync(new OpenFileRequest(
            doc.Title,
            new ReadOnlyFile(doc.StoredPath)));
    }

    [RelayCommand]
    private async Task OpenEncounter(Encounter encounter)
    {
        if(encounter is null||encounter.Id==Guid.Empty) return;

        _selectedItemService.SelectedItem=encounter;
        _selectedItemService.OpenInEditMode=false;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Detail);
    }

    [RelayCommand]
    private async Task Cancel()
        => await NavigationService.GoBackAsync();

    [RelayCommand]
    public async Task SaveAsync()
    {
        if(!CanModifyEncounter)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате овластување за промена на овој преглед.", "ОК");
            return;
        }

        if(Encounter.Status==EncounterStatus.Completed||Encounter.IsLocked)
        {
            await UserDialogService.ShowAlertAsync("Заклучен преглед", "Завршен преглед не може да се менува.", "ОК");
            return;
        }

        if(SelectedPatient?.Status==PatientStatus.Inactive)
        {
            await UserDialogService.ShowAlertAsync("Пациентот е неактивен", "Податоците за неактивен пациент се заклучени и не може да се менуваат.", "ОК");
            return;
        }

        if(EncounterMedicines.Any(x => string.IsNullOrWhiteSpace(x.ApplicationRegime?.Regime)))
        {
            await UserDialogService.ShowAlertAsync(
                "Валидација",
                "За секој додаден лек мора да изберете режим на апликација.",
                "Во ред");
            return;
        }

        ResolveApplicationRegimes(EncounterMedicines);

        await ExecuteSafeAsync(async () =>
        {
            Encounter.SetNotes(EncounterDiagnosisNotes);
            await EncounterService.SaveEncounter(
                Encounter,
                Diagnoses.ToList(),
                Prescriptions.ToList(),
                EncounterMedicines.ToList(),
                DeletedMedicineIds.ToList(),
                ScoreText,
                ScheduleNextFollowUp ? NextFollowUpDate.Date.AddHours(9) : null);

            await UserDialogService.ShowMessageAsync("Податоци за преглед се успешно зачувани", "");
            await NavigationService.GoToAsync(AppRoutes.Encounters.List);
        }, "Неуспешно зачувување");
    }

    [RelayCommand]
    public async Task ChangeStatusAsync(EncounterStatus newStatus)
    {
        var statusAllowed = newStatus switch
        {
            EncounterStatus.Completed => CanApprove,
            EncounterStatus.Cancelled => CanUpdate,
            EncounterStatus.Scheduled => CanUpdate,
            EncounterStatus.InProgress => CanUpdate,
            _ => false
        };

        if(!statusAllowed)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате овластување за оваа промена на статусот.", "ОК");
            return;
        }

        if(Encounter.Status==EncounterStatus.Completed||Encounter.IsLocked)
        {
            await UserDialogService.ShowAlertAsync("Заклучен преглед", "Завршен преглед не може да се менува.", "ОК");
            return;
        }

        if(SelectedPatient?.Status==PatientStatus.Inactive)
        {
            await UserDialogService.ShowAlertAsync("Пациентот е неактивен", "Податоците за неактивен пациент се заклучени и не може да се менуваат.", "ОК");
            return;
        }

        if(Encounter.IsLocked&&newStatus!=EncounterStatus.Completed)
        {
            await UserDialogService.ShowAlertAsync(
                "Заклучен преглед",
                "Овој преглед е заклучен и не може да се менува.",
                "Во ред");
            return;
        }

        var previous = Encounter.Status;
        Encounter.Status=newStatus;

        // Lock the encounter when completed — clinical record is frozen
        if(newStatus==EncounterStatus.Completed)
            Encounter.IsLocked=true;

        await ExecuteSafeAsync(async () =>
        {
            await EncounterService.UpdateAppointmentStatus(
                Encounter.AppointmentId,
                MapToAppointmentStatus(newStatus));

        }, "Грешка при промена на статус");

        OnPropertyChanged(nameof(EncounterStatusDisplay));
    }


    private static AppointmentStatus MapToAppointmentStatus(EncounterStatus s) => s switch
    {
        EncounterStatus.Scheduled => AppointmentStatus.Scheduled,
        EncounterStatus.InProgress => AppointmentStatus.InProgress,
        EncounterStatus.Completed => AppointmentStatus.Completed,
        EncounterStatus.Cancelled => AppointmentStatus.Cancelled,
        _ => AppointmentStatus.Scheduled
    };
}
