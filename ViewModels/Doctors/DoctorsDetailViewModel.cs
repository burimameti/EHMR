using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;

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
    // ПОЛ — Radio buttons: Машки / Женски
    // =====================================================
    [ObservableProperty] private bool isMale = true;
    [ObservableProperty] private bool isFemale;

    partial void OnIsMaleChanged(bool value)
    {
        if(value)
        {
            IsFemale=false;
            Doctors.Gender=Gender.Male;
        }
    }

    partial void OnIsFemaleChanged(bool value)
    {
        if(value)
        {
            IsMale=false;
            Doctors.Gender=Gender.Female;
        }
    }

    // =====================================================
    // СТАТУС — Toggle (само во Edit Mode)
    // Нов лекар: секогаш Active
    // Edit: корисникот може да го направи Inactive
    // =====================================================
    [ObservableProperty] private bool isActive = true;

    partial void OnIsActiveChanged(bool value)
    {
        Doctors.Status=value ? Status.Active : Status.Inactive;
    }

    private void SyncDisplayFromDoctor()
    {
        IsMale=Doctors.Gender==Gender.Male;
        IsFemale=Doctors.Gender==Gender.Female;

        IsActive=Doctors.Status==Status.Active;
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
            // Нов лекар — секогаш Active
            Doctors=new Doctor
            {
                User=new User(),
                Gender=Gender.Male,
                Status=Status.Active
            };
            _isExistingDoctor=false;

            IsEditMode=true;
            IsReadOnly=false;
            PageTitle="Нов лекар";
        }

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
                await _doctorService.UpdateAsync(Doctors);
            else
                await _doctorService.AddAsync(Doctors);

            await NavigationService.GoBackAsync();
        },
        "Грешка при зачувување на лекар");
    }

    // ================= DELETE =================
    [RelayCommand]
    private async Task Delete()
    {
        if(!_isExistingDoctor) return;

        await ExecuteSafeAsync(async () =>
        {
            await _doctorService.DeleteAsync(Doctors.Id);
            await NavigationService.GoBackAsync();
        },
        "Грешка при бришење");
    }

    // ================= CANCEL / ОТКАЖИ =================
    // Секогаш оди назад:
    //   - Нов лекар → GoBack (без зачувување)
    //   - Edit постоечки → врати snapshot, оди назад
    [RelayCommand]
    private async Task Cancel()
    {
        if(_isExistingDoctor&&_snapshot is not null)
        {
            Doctors=_snapshot;
            _snapshot=null;
            SyncDisplayFromDoctor();
        }

        await NavigationService.GoBackAsync();
    }

    protected override IEnumerable<Doctor> ApplyFilters(IEnumerable<Doctor> query) => query;
    protected override void ResetFilters()
    {
    }
}