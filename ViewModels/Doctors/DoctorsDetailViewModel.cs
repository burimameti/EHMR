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

    /// <summary>
    /// ENTITY state — true when this page represents an EXISTING doctor
    /// that was loaded from the selection service. Not bound in XAML;
    /// only used here to decide Save's Update-vs-Add branch, Delete
    /// availability, and what Cancel should do.
    /// </summary>
    private bool _isExistingDoctor;

    /// <summary>
    /// Snapshot taken the moment we enter edit mode on an EXISTING doctor,
    /// so Cancel can revert in-place instead of just navigating back.
    /// </summary>
    private Doctor? _snapshot;

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private bool isReadOnly;

    [ObservableProperty]
    private string pageTitle = "Нов лекар";

    [ObservableProperty]
    private bool canShowDelete;

    [ObservableProperty]
    private bool canShowSave;

    [ObservableProperty]
    private bool canShowEditToggle;

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
            Doctors=new Doctor { User=new User() };
            _isExistingDoctor=false;

            IsEditMode=true;
            IsReadOnly=false;
            PageTitle="Нов лекар";
        }

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

            IsEditMode=false;
            IsReadOnly=true;
            PageTitle="Детали за лекар";
        }
        else
        {
            await NavigationService.GoBackAsync();
        }
    }

    protected override IEnumerable<Doctor> ApplyFilters(IEnumerable<Doctor> query)
        => query;

    protected override void ResetFilters()
    {
    }
}