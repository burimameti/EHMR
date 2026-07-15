using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Popups;

public partial class CreateTherapyCyclePopupViewModel : ObservableObject
{
    private readonly IEncounterDetailService _encounterService;
    private readonly Guid _patientId;

    private static readonly Dictionary<string, TherapyStatus> StatusMap = new()
    {
        ["Активен"]=TherapyStatus.Active,
        ["Планиран"]=TherapyStatus.Planned
    };

    public Action<object?>? RequestClose
    {
        get; set;
    }


    public CreateTherapyCyclePopupViewModel(
        IEncounterDetailService encounterService,
        Guid patientId,
        string? prefillNotes)
    {
        _encounterService=encounterService;
        _patientId=patientId;

        Notes=prefillNotes??string.Empty;

        StartDate=DateTime.Today;

        StatusDisplay=StatusOptions.First();
    }


    public List<string> StatusOptions
    {
        get;
    } =
        StatusMap.Keys.ToList();


    [ObservableProperty]
    private string statusDisplay = string.Empty;


    [ObservableProperty]
    private string notes = string.Empty;


    [ObservableProperty]
    private DateTime startDate;


    [ObservableProperty]
    private DateTime? endDate;


    [ObservableProperty]
    private bool hasError;


    [ObservableProperty]
    private string errorMessage = string.Empty;


    [ObservableProperty]
    private bool isBusy;


    [RelayCommand]
    private async Task SaveAsync()
    {
        if(string.IsNullOrWhiteSpace(Notes))
        {
            HasError=true;
            ErrorMessage="Внесете опис за циклусот";
            return;
        }


        if(IsBusy)
            return;


        try
        {
            IsBusy=true;
            HasError=false;

            var status =
                StatusMap.TryGetValue(StatusDisplay, out var s)
                    ? s
                    : TherapyStatus.Planned;


            var cycle = new TherapyCycle
            {
                Id=Guid.NewGuid(),
                PatientId=_patientId,
                Notes=Notes,
                Status=status,
                StartDate=StartDate,
                EndDate=EndDate
            };


            var saved =
                await _encounterService.CreateTherapyCycle(cycle);


            RequestClose?.Invoke(saved);
        }
        catch(Exception ex)
        {
            HasError=true;
            ErrorMessage=$"Грешка при зачувување: {ex.Message}";
        }
        finally
        {
            IsBusy=false;
        }
    }


    [RelayCommand]
    public void Cancel()
    {
        RequestClose?.Invoke(null);
    }
}