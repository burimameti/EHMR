using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using EHMR.ViewModels.Doctors.Extensions;

namespace EHMR.ViewModels;

public partial class DoctorsDetailViewModel : BaseViewModel<Doctor>
{
    private readonly IDoctorService _doctorService;

    protected override string ModuleName => Modules.Doctors;

    public DoctorsDetailViewModel(
        IDoctorService doctorService,
        ISelectedItemService<Doctor> selectedItemService,
        INavigationService navigationService,
        IUserDialogService dialog,
        IMenuService menu,
        IAuthorizationService authorization)
        : base(navigationService, dialog, menu, authorization, selectedItemService)
    {
        _doctorService=doctorService;
        EvaluatePermissions();
    }

    [ObservableProperty]
    private Doctor doctors = new();

    private bool _isExistingDoctor;
    private Doctor? _snapshot;

    [ObservableProperty] private bool isEditMode;
    [ObservableProperty] private bool isReadOnly;
    [ObservableProperty] private string pageTitle = "Нов лекар";

    [ObservableProperty] private bool canShowDelete;
    [ObservableProperty] private bool canShowSave;
    [ObservableProperty] private bool canShowEditToggle;

    // =====================================================
    // СВОЈСТВА ЗА ДВОЈНО МАПИРАЊЕ (МАКЕДОНСКИ <-> ENUM)
    // =====================================================
    [ObservableProperty] private string selectedGenderDisplay = string.Empty;
    [ObservableProperty] private string selectedStatusDisplay = string.Empty;

    partial void OnSelectedGenderDisplayChanged(string value)
    {
        if(string.IsNullOrEmpty(value)) return;

        // Претворање од македонски текст во англиски Enum за базата
        Doctors.Gender=value switch
        {
            "Машки" => Gender.Male,
            "Женски" => Gender.Female,
            _ => Gender.Other
        };
    }

    partial void OnSelectedStatusDisplayChanged(string value)
    {
        if(string.IsNullOrEmpty(value)) return;

        // Претворање од македонски текст во англиски Enum за базата
        Doctors.Status=value switch
        {
            "Активен" => Status.Active,
            "Неактивен" => Status.Inactive,
            _ => Status.Suspended
        };
    }

    private void SyncDisplayFromDoctor()
    {
        SelectedGenderDisplay=Doctors.Gender switch
        {
            Gender.Male => "Машки",
            Gender.Female => "Женски",
            _ => "Друго"
        };

        SelectedStatusDisplay=Doctors.Status switch
        {
            Status.Active => "Активен",
            Status.Inactive => "Неактивен",
            _ => "Суспендиран"
        };
    }

    private void RefreshButtonVisibility()
    {
        CanShowDelete=IsReadOnly&&_isExistingDoctor&&CanDelete;
        CanShowSave=IsEditMode&&CanUpdate;
        CanShowEditToggle=IsReadOnly&&CanUpdate;
    }

    partial void OnIsEditModeChanged(bool value) => RefreshButtonVisibility();
    partial void OnIsReadOnlyChanged(bool value) => RefreshButtonVisibility();

    // ================= LOAD =================
    [RelayCommand]
    public async Task LoadAsync()
    {
        if(SelectedItemService.SelectedItem is not null)
        {
            Doctors=SelectedItemService.SelectedItem.Clone();
            _isExistingDoctor=true;

            IsEditMode=false;
            IsReadOnly=true;
            PageTitle="Детали за лекар";
        }
        else
        {
            Doctors=new Doctor { User=new User(), Gender=Gender.Male, Status=Status.Active };
            _isExistingDoctor=false;

            IsEditMode=true;
            IsReadOnly=false;
            PageTitle="Нов лекар";
        }

        // Наполни ги македонските стрингови во интерфејсот
        SyncDisplayFromDoctor();

        _snapshot=null;
        RefreshButtonVisibility();
    }

    // ================= EDIT TOGGLE =================
    [RelayCommand]
    private void ToggleEditMode()
    {
        _snapshot=Doctors.Clone();

        IsEditMode=true;
        IsReadOnly=false;
        PageTitle="Измени лекар";
    }

    // ================= SAVE =================
    [RelayCommand]
    private async Task Save()
    {
        await ExecuteSafeAsync(async () =>
        {
            if(_isExistingDoctor)
            {
                await _doctorService.UpdateAsync(Doctors);
            }
            else
            {
                await _doctorService.AddAsync(Doctors);
            }
            await NavigationService.GoBackAsync();
        },
        "Грешка при зачувување на лекар");
    }

    // ================= DELETE =================
    [RelayCommand]
    private async Task Delete()
    {
        if(!_isExistingDoctor)
            return;

        await ExecuteSafeAsync(async () =>
        {
            await _doctorService.DeleteAsync(Doctors.Id);
            await NavigationService.GoBackAsync();
        },
        "Грешка при бришење");
    }

    // ================= CANCEL =================
    [RelayCommand]
    private async Task Cancel()
    {
        if(_isExistingDoctor)
        {
            if(_snapshot is not null)
            {
                Doctors=_snapshot;
            }
            _snapshot=null;

            // Врати го преводот во првобитна состојба
            SyncDisplayFromDoctor();

            IsEditMode=false;
            IsReadOnly=true;
            PageTitle="Детали за лекар";
        }
        else
        {
            await NavigationService.GoBackAsync();
        }
    }

    protected override IEnumerable<Doctor> ApplyFilters(IEnumerable<Doctor> query) => query;
    protected override void ResetFilters()
    {
    }
}