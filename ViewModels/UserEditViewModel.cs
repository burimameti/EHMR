using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using EHMR.Domain.Entities.Rbac;

namespace EHMR.ViewModels;

public partial class UserEditViewModel : ObservableObject
{
    private readonly IUserService _service;
    private readonly ISelectedItemService<UserAdminDto> _userSelectionService;
    private readonly IUserDialogService _dialogService;
    private readonly IAuthStateService _authStateService;

    private UserAdminDto? _originalUser;
    private bool _isNewUserMode;

    [ObservableProperty]
    private UserAdminDto _user = new();

    [ObservableProperty]
    private ObservableCollection<PermissionCheckWrapper> _systemPermissions = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    private bool _isReadOnly = true;

    [ObservableProperty]
    private string _pageTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowEditButton))]
    [NotifyPropertyChangedFor(nameof(CanShowDeleteButton))]
    private bool _canManageTargetUser = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowDeleteButton))]
    private bool _isSelfEdit;

    public bool IsEditMode => !IsReadOnly;
    public bool CanShowEditButton => IsReadOnly&&CanManageTargetUser;
    public bool CanShowDeleteButton => !_isNewUserMode&&CanManageTargetUser&&!IsSelfEdit;
    private List<UserRole> _roles = new();
    public List<UserRole> Roles => _roles;

    private List<UserPosition> _positions = Enum.GetValues<UserPosition>().ToList();
    public List<UserPosition> Positions => _positions;
    //public List<UserRole> Roles
    //{
    //    get
    //    {
    //        var actorRole = _authStateService.CurrentUser?.Role;
    //        return actorRole is null
    //            ? Enum.GetValues<UserRole>().ToList()
    //            : RoleHierarchy.AssignableRolesFor(actorRole.Value).ToList();
    //    }
    //}

    //public List<UserPosition> Positions => Enum.GetValues<UserPosition>().ToList();

    private UserRole _selectedRole;
    public UserRole SelectedRole
    {
        get => _selectedRole;
        set
        {
            if(SetProperty(ref _selectedRole, value))
            {
                if(User!=null)
                    User.Role=value;
                ApplyDefaultPermissionsForRole();
            }
        }
    }

    private UserPosition _selectedPosition;
    public UserPosition SelectedPosition
    {
        get => _selectedPosition;
        set
        {
            if(SetProperty(ref _selectedPosition, value))
            {
                if(User!=null)
                    User.Position=value;
            }
        }
    }

    public int SelectedPositionIndex
    {
        get => Positions.IndexOf(_selectedPosition);
        set
        {
            if(value>=0&&value<Positions.Count)
                SelectedPosition=Positions[value];
        }
    }

    public UserEditViewModel(
        IUserService service,
        ISelectedItemService<UserAdminDto> userSelectionService,
        IUserDialogService dialogService,
        IAuthStateService authStateService)
    {
        _service=service;
        _userSelectionService=userSelectionService;
        _dialogService=dialogService;
        _authStateService=authStateService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var allSystemModules = Modules.GetAll().ToList();
            var selected = _userSelectionService.SelectedItem;
            InitializeForm(selected, allSystemModules);
        }
        catch(Exception ex)
        {
            await _dialogService.ShowAlertAsync("Грешка", $"Проблем при вчитување на формата: {ex.Message}", "OK");
        }
    }

    private void InitializeForm(UserAdminDto? selectedUser, List<string> allSystemModules)
    {
        var actor = _authStateService.CurrentUser;

        if(selectedUser==null)
        {
            _isNewUserMode=true;
            _originalUser=null;

            User=new UserAdminDto
            {
                Username=string.Empty,
                FirstName=string.Empty,
                LastName=string.Empty,
                Role=UserRole.Nurse,
                Position=UserPosition.Regular,
                IsActive=true,
                Modules=new List<string>()
            };

            PageTitle="➕ Нов";
            IsReadOnly=false;
            IsSelfEdit=false;
            CanManageTargetUser=true;
        }
        else
        {
            _isNewUserMode=false;
            _originalUser=selectedUser;

            User=CloneUser(selectedUser);
            PageTitle=$"Корисник: {User.Username}";
            IsReadOnly=true;

            IsSelfEdit=actor!=null&&actor.Id==selectedUser.Id;
            CanManageTargetUser=actor==null||RoleHierarchy.CanManage(actor.Role, selectedUser.Role);
        }

        SystemPermissions.Clear();
        foreach(var mod in allSystemModules)
        {
            bool isChecked = User.Modules!=null&&User.Modules.Contains(mod, StringComparer.OrdinalIgnoreCase);
            SystemPermissions.Add(new PermissionCheckWrapper(mod, isChecked));
        }

        _selectedRole=User.Role;
        _selectedPosition=User.Position;
        var actorRole = _authStateService.CurrentUser?.Role;
        _roles=actorRole is null
            ? Enum.GetValues<UserRole>().ToList()
            : RoleHierarchy.AssignableRolesFor(actorRole.Value).ToList();
        OnPropertyChanged(nameof(Roles));
        OnPropertyChanged(nameof(SelectedRole));
        OnPropertyChanged(nameof(SelectedPosition));
        OnPropertyChanged(nameof(SelectedPositionIndex));
        OnPropertyChanged(nameof(Roles));
        OnPropertyChanged(nameof(CanShowEditButton));
        OnPropertyChanged(nameof(CanShowDeleteButton));

        if(_isNewUserMode)
            ApplyDefaultPermissionsForRole();
    }

    [RelayCommand]
    public void ToggleEditMode()
    {
        if(!CanManageTargetUser) return;
        IsReadOnly=false;
        PageTitle=$"✎ Уреди: {User.Username}";
    }

    [RelayCommand]
    public void TogglePermission(PermissionCheckWrapper item)
    {
        if(IsReadOnly||item==null) return;
        item.IsSelected=!item.IsSelected;
    }

    [RelayCommand]
    public void ApplyDefaultPermissionsForRole()
    {
        if(User==null) return;
        var defaultModules = Modules.GetDefaultsForRole(User.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var wrapper in SystemPermissions)
            wrapper.IsSelected=defaultModules.Contains(wrapper.PermissionValue);
    }

    [RelayCommand]
    public async Task Cancel()
    {
        if(_isNewUserMode)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        if(_originalUser!=null)
            User=CloneUser(_originalUser);

        IsReadOnly=true;
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if(string.IsNullOrWhiteSpace(User.Username))
        {
            await _dialogService.ShowAlertAsync("Предупредување", "Внесете корисничко име.", "ОК");
            return;
        }

        if(!CanManageTargetUser)
        {
            await _dialogService.ShowAlertAsync("Пристап одбиен", "Немате доволно овластувања за оваа промена.", "OK");
            return;
        }

        var actor = _authStateService.CurrentUser;

        if(actor!=null&&!RoleHierarchy.AssignableRolesFor(actor.Role).Contains(User.Role))
        {
            await _dialogService.ShowAlertAsync("Пристап одбиен", "Не смеете да доделите таа улога.", "OK");
            return;
        }

        if(!_isNewUserMode&&IsSelfEdit&&_originalUser!=null&&User.Role!=_originalUser.Role)
        {
            await _dialogService.ShowAlertAsync("Не е дозволено", "Не можете сами да си ја промените улогата.", "OK");
            return;
        }

        try
        {
            User.Modules=SystemPermissions
                .Where(x => x.IsSelected)
                .Select(x => x.PermissionValue)
                .ToList();

            if(_isNewUserMode)
            {
                await _service.CreateAsync(User);
                await _dialogService.ShowAlertAsync("Успешно", "Корисничката сметка е генерирана.", "ОК");
            }
            else
            {
                await _service.UpdateAsync(User);

                if(_originalUser!=null)
                {
                    _originalUser.Username=User.Username;
                    _originalUser.FirstName=User.FirstName;
                    _originalUser.LastName=User.LastName;
                    _originalUser.Role=User.Role;
                    _originalUser.Position=User.Position;
                    _originalUser.IsActive=User.IsActive;
                    _originalUser.Modules=new List<string>(User.Modules);
                }

                await _dialogService.ShowAlertAsync("Успешно", "Промените се зачувани.", "ОК");
            }

            IsReadOnly=true;
            await Shell.Current.GoToAsync("..");
        }
        catch(Exception ex)
        {
            await _dialogService.ShowAlertAsync("Грешка при зачувување", ex.Message, "ОК");
        }
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if(_isNewUserMode||_originalUser==null) return;

        if(!CanManageTargetUser)
        {
            await _dialogService.ShowAlertAsync("Пристап одбиен", "Немате доволно овластувања да го избришете овој корисник.", "OK");
            return;
        }

        if(IsSelfEdit)
        {
            await _dialogService.ShowAlertAsync("Не е дозволено", "Не можете да го избришете сопствениот кориснички профил.", "OK");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Бришење корисник",
            $"Дали сте сигурни дека сакате да го избришете корисникот '{_originalUser.Username}'?",
            "Избриши",
            "Откажи");

        if(!confirmed) return;

        try
        {
            await _service.DeleteAsync(_originalUser.Id);
            await _dialogService.ShowAlertAsync("Успешно", "Корисникот е избришан.", "ОК");
            await Shell.Current.GoToAsync("..");
        }
        catch(Exception ex)
        {
            await _dialogService.ShowAlertAsync("Грешка при бришење", ex.Message, "ОК");
        }
    }

    private static UserAdminDto CloneUser(UserAdminDto source) => new()
    {
        Id=source.Id,
        Username=source.Username,
        FirstName=source.FirstName,
        LastName=source.LastName,
        Role=source.Role,
        Position=source.Position,
        IsActive=source.IsActive,
        Modules=source.Modules!=null ? new List<string>(source.Modules) : new List<string>()
    };
}