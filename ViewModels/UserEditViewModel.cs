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

    public bool IsEditMode => !IsReadOnly;

    public List<UserRole> Roles => Enum.GetValues<UserRole>().ToList();
    public List<UserPosition> Positions => Enum.GetValues<UserPosition>().ToList();

    private UserRole _selectedRole;

    public UserRole SelectedRole
    {
        get => _selectedRole;
        set
        {
            if(SetProperty(ref _selectedRole, value))
            {
                if(User!=null)
                {
                    User.Role=value;
                }
                ApplyDefaultPermissionsForRole();
            }
        }
    }

    public UserEditViewModel(
        IUserService service,
        ISelectedItemService<UserAdminDto> userSelectionService,
        IUserDialogService dialogService)
    {
        _service=service;
        _userSelectionService=userSelectionService;
        _dialogService=dialogService;
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

            PageTitle="➕ Нова Корисничка Сметка";
            IsReadOnly=false;
        }
        else
        {
            _isNewUserMode=false;
            _originalUser=selectedUser;

            User=CloneUser(selectedUser);
            PageTitle=$"Корисник: {User.Username}";
            IsReadOnly=true;
        }

        SystemPermissions.Clear();
        foreach(var mod in allSystemModules)
        {
            bool isChecked = User.Modules!=null&&User.Modules.Contains(mod, StringComparer.OrdinalIgnoreCase);
            SystemPermissions.Add(new PermissionCheckWrapper(mod, isChecked));
        }

        _selectedRole=User.Role;
        OnPropertyChanged(nameof(SelectedRole));

        if(_isNewUserMode)
        {
            ApplyDefaultPermissionsForRole();
        }
    }

    [RelayCommand]
    public void ToggleEditMode()
    {
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
        {
            wrapper.IsSelected=defaultModules.Contains(wrapper.PermissionValue);
        }
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
        {
            User=CloneUser(_originalUser);
        }

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

    private static UserAdminDto CloneUser(UserAdminDto source)
    {
        return new UserAdminDto
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
}