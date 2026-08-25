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
    private readonly IUserDialogService _dialog;

    protected override string ModuleName => Modules.Doctors;

    public DoctorsDetailViewModel(
        IDoctorService doctorService,
        ISelectedItemService<Doctor> selectedItemService,
        INavigationService navigationService,
        IUserDialogService dialog,
        IMenuService menu,
        IAuthorizationService authorization)
        : base(
            navigationService,
            dialog,
            menu,
            authorization,
            selectedItemService)
    {
        _doctorService=doctorService;
        _dialog=dialog;

        EvaluatePermissions();
    }

    // =====================================================
    // CURRENT DOCTOR
    // =====================================================

    [ObservableProperty]
    private Doctor doctors = new()
    {
        User=new User()
    };

    private bool _isExistingDoctor;
    private Doctor? _snapshot;

    // =====================================================
    // PAGE STATE
    // =====================================================

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private bool isReadOnly;

    [ObservableProperty]
    private string pageTitle = "Нов реуматолог";

    [ObservableProperty]
    private bool canShowDelete;

    [ObservableProperty]
    private bool canShowSave;

    [ObservableProperty]
    private bool canShowEditToggle;

    // =====================================================
    // GENDER
    // =====================================================

    [ObservableProperty]
    private bool isMale = true;

    [ObservableProperty]
    private bool isFemale;

    partial void OnIsMaleChanged(bool value)
    {
        if(!value)
            return;

        if(IsFemale)
            IsFemale=false;

        Doctors.Gender=Gender.Male;
    }

    partial void OnIsFemaleChanged(bool value)
    {
        if(!value)
            return;

        if(IsMale)
            IsMale=false;

        Doctors.Gender=Gender.Female;
    }

    // =====================================================
    // STATUS
    // =====================================================

    [ObservableProperty]
    private bool isActive = true;

    partial void OnIsActiveChanged(bool value)
    {
        Doctors.IsActive=value;
    }

    // =====================================================
    // LOAD
    // =====================================================

    [RelayCommand]
    public async Task LoadAsync()
    {
        var selected = SelectedItemService.SelectedItem;

        if(selected is not null)
        {
            Doctors=selected.Clone();

            EnsureUserExists();

            _isExistingDoctor=true;

            IsEditMode=false;
            IsReadOnly=true;

            PageTitle="Детали за реуматолог";
        }
        else
        {
            Doctors=CreateNewDoctor();

            _isExistingDoctor=false;

            IsEditMode=true;
            IsReadOnly=false;

            PageTitle="Нов реуматолог";
        }

        SyncDisplayFromDoctor();

        _snapshot=null;

        RefreshButtonVisibility();
    }

    // =====================================================
    // NEW DOCTOR
    // =====================================================

    private static Doctor CreateNewDoctor()
    {
        return new Doctor
        {
            User=new User(),

            ContactPhone=string.Empty,
            Email=string.Empty,

            Gender=Gender.Male,
            Status=Status.Active
        };
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private void EnsureUserExists()
    {
        Doctors.User??=new User();
    }

    private void SyncDisplayFromDoctor()
    {
        EnsureUserExists();

        IsMale=Doctors.Gender==Gender.Male;
        IsFemale=Doctors.Gender==Gender.Female;

        IsActive=Doctors.IsActive;
    }

    private void RefreshButtonVisibility()
    {
        CanShowDelete=
            IsReadOnly&&
            _isExistingDoctor&&
            CanDelete;

        CanShowSave=
            IsEditMode&&
            CanUpdate;

        CanShowEditToggle=
            IsReadOnly&&
            _isExistingDoctor&&
            CanUpdate;
    }

    partial void OnIsEditModeChanged(bool value)
        => RefreshButtonVisibility();

    partial void OnIsReadOnlyChanged(bool value)
        => RefreshButtonVisibility();

    // =====================================================
    // EDIT
    // =====================================================

    [RelayCommand]
    private void ToggleEditMode()
    {
        if(!_isExistingDoctor)
            return;

        _snapshot=Doctors.Clone();

        IsEditMode=true;
        IsReadOnly=false;

        PageTitle="Уреди реуматолог";
    }

    // =====================================================
    // SAVE
    // =====================================================

    [RelayCommand]
    private async Task Save()
    {
        await ExecuteSafeAsync(
            async () =>
            {
                EnsureUserExists();

                NormalizeBeforeSave();

                if(!await ValidateDoctorAsync())
                    return;

                if(_isExistingDoctor)
                {
                    await _doctorService.UpdateAsync(Doctors);
                }
                else
                {
                    await _doctorService.AddAsync(Doctors);
                }

                SelectedItemService.SelectedItem=null;

                await NavigationService.GoBackAsync();
            },
            "Грешка при зачувување на податоци");
    }

    private void NormalizeBeforeSave()
    {
        EnsureUserExists();

        Doctors.User.FirstName=
            Doctors.User.FirstName?.Trim()
            ??string.Empty;

        Doctors.User.LastName=
            Doctors.User.LastName?.Trim()
            ??string.Empty;

        Doctors.ContactPhone=
            Doctors.ContactPhone?.Trim()
            ??string.Empty;

        Doctors.Email=
            string.IsNullOrWhiteSpace(Doctors.Email)
                ? null
                : Doctors.Email.Trim();

        Doctors.Gender=
            IsFemale
                ? Gender.Female
                : Gender.Male;

        Doctors.IsActive=IsActive;
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private async Task<bool> ValidateDoctorAsync()
    {
        if(string.IsNullOrWhiteSpace(
               Doctors.User.FirstName))
        {
            await _dialog.ShowAlertAsync(
                "Валидација",
                "Името на реуматологот е задолжително.",
                "Во ред");

            return false;
        }

        if(string.IsNullOrWhiteSpace(
               Doctors.User.LastName))
        {
            await _dialog.ShowAlertAsync(
                "Валидација",
                "Презимето на реуматологот е задолжително.",
                "Во ред");

            return false;
        }

        return true;
    }

    // =====================================================
    // DELETE
    // =====================================================

    [RelayCommand]
    private async Task Delete()
    {
        if(!_isExistingDoctor)
            return;

        await ExecuteSafeAsync(
            async () =>
            {
                await _doctorService.DeleteAsync(
                    Doctors.Id);

                SelectedItemService.SelectedItem=null;

                await NavigationService.GoBackAsync();
            },
            "Грешка при бришење");
    }

    // =====================================================
    // CANCEL
    // =====================================================

    [RelayCommand]
    private async Task Cancel()
    {
        if(_isExistingDoctor&&
           _snapshot is not null)
        {
            Doctors=_snapshot;
            _snapshot=null;

            SyncDisplayFromDoctor();
        }

        SelectedItemService.SelectedItem=null;

        await NavigationService.GoBackAsync();
    }

    // =====================================================
    // BASE
    // =====================================================

    protected override IEnumerable<Doctor> ApplyFilters(
        IEnumerable<Doctor> query)
        => query;

    protected override void ResetFilters()
    {
    }
}