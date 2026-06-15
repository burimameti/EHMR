using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _dialogService;
    private readonly IAuthStateService _authStateService;
    private readonly IPreferencesService _preferencesService;
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = "123456";

    [ObservableProperty]
    private bool rememberMe;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public LoginViewModel(
        INavigationService navigationService,
        IUserDialogService dialogService,
        IAuthStateService authStateService,
        IPreferencesService preferencesService,
        IDbContextFactory<DesktopTherapyDbContext> dbFactory)
    {
        _navigationService=navigationService;
        _dialogService=dialogService;
        _authStateService=authStateService;
        _preferencesService=preferencesService;
        _dbFactory=dbFactory;

        LoadSavedCredentials();
    }

    // =========================================================
    // LOGIN FLOW
    // =========================================================

    [RelayCommand]
    private async Task Login()
    {
        if(IsBusy)
            return;

        if(string.IsNullOrWhiteSpace(Username)||string.IsNullOrWhiteSpace(Password))
        {
            await SetErrorAsync("Внесете корисничко име и лозинка.");
            return;
        }

        try
        {
            IsBusy=true;
            ErrorMessage=string.Empty;

            await using var db = await _dbFactory.CreateDbContextAsync();

            // ОБВРЗНО: Вклучи ги и експлицитните кориснички модули од базата
            var user = await db.Users
                .AsNoTracking()
                .Include(u => u.Scopes)
                .Include(u => u.Modules)
                .FirstOrDefaultAsync(u => u.Username==Username);

            if(user is null)
            {
                await SetErrorAsync("Корисникот не постои.");
                return;
            }

            if(!user.IsActive)
            {
                await SetErrorAsync("Корисничкиот профил е деактивиран.");
                return;
            }

            if(!string.Equals(user.PasswordHash, Password, StringComparison.Ordinal))
            {
                await SetErrorAsync("Неточна лозинка.");
                return;
            }

            // 1. Постави ги ролите за сесијата
            var roles = new List<string> { user.Role.ToString() };

            // 2. ДИНАМИЧНО ПРЕСМЕТУВАЊЕ НА ДОЗВОЛЕНИТЕ МОДУЛИ (Сесија)
            // Ги земаме дефолтните за улогата, па ги применуваме експлицитните правила од базата
            var allowedModules = new List<string>();

            // Земаме листа од сите 8 константи на системот за да евалуираме
            var allSystemModules = new List<string>
            {
                Domain.Entities.Modules.Dashboard,
                Domain.Entities.Modules.Patients,
                Domain.Entities.Modules.Appointments,
                Domain.Entities.Modules.Therapy,
                Domain.Entities.Modules.Protocols,
                Domain.Entities.Modules.Inventory,
                Domain.Entities.Modules.Reports,
                Domain.Entities.Modules.Administration
            };

            foreach(var moduleKey in allSystemModules)
            {
                // Ја користиме веќе дефинираната паметна логика од самиот домен (User.cs)
                if(user.IsAuthorizedToModule(moduleKey))
                {
                    allowedModules.Add(moduleKey);
                }
            }

            PersistCredentials();

            // 3. ПОЛНЕЊЕ НА АУТЕНТИКАЦИСКИОТ СТАТУС
            // И во permissions и во modules ги праќаме дозволените модули за CanAccess/Policy да работат без багови
            _authStateService.SetState(
                user.Id.ToString(),
                roles,
                allowedModules, // Се мапира во _auth.Permissions
                allowedModules  // Се мапира во _auth.Modules
            );

            _authStateService.UserName=user.Username;

            // Пренасочување кон почетната страница
            await _navigationService.GoToAsync("//dashboard");
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"LOGIN ERROR: {ex}");
            await SetErrorAsync("Системска грешка. Базата не е достапна.");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private async Task SetErrorAsync(string message)
    {
        ErrorMessage=message;
        await _dialogService.ShowAlertAsync("Најава", message, "OK");
    }

    private void LoadSavedCredentials()
    {
        if(!_preferencesService.ContainsKey("saved_username"))
            return;

        Username=_preferencesService.Load("saved_username");
        Password=_preferencesService.Load("saved_password");
        RememberMe=true;
    }

    private void PersistCredentials()
    {
        if(!RememberMe)
        {
            _preferencesService.Remove("saved_username");
            _preferencesService.Remove("saved_password");
            return;
        }

        _preferencesService.Save("saved_username", Username);
        _preferencesService.Save("saved_password", Password);
    }
}