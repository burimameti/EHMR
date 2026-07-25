using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EHMR.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _dialogService;
    private readonly IAuthStateService _authStateService;
    private readonly IPreferencesService _preferencesService;
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

    [ObservableProperty] private string username;
    [ObservableProperty] private string password = "HASH_ADMIN";
    [ObservableProperty] private bool rememberMe;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;

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

    // =========================
    // LOGIN
    // =========================
    [RelayCommand]
    private async Task Login()
    {
        if(IsBusy)
            return;

        if(string.IsNullOrWhiteSpace(Username)||
            string.IsNullOrWhiteSpace(Password))
        {
            await SetErrorAsync("Внесете корисничко име и лозинка.");
            return;
        }

        try
        {
            IsBusy=true;
            ErrorMessage=string.Empty;

            await using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.Users
                .AsNoTracking()
                .Include(x => x.Modules)
                .Include(x => x.Scopes)
                .FirstOrDefaultAsync(x => x.Username==Username);

            if(user is null)
            {
                await SetErrorAsync("Корисникот не постои.");
                return;
            }

            if(!user.IsActive)
            {
                await SetErrorAsync("Корисникот е деактивиран.");
                return;
            }

            // ⚠️ NOTE: replace later with hashing service
            if(user.PasswordHash!=Password)
            {
                await SetErrorAsync("Неточна лозинка.");
                return;
            }
            Guid? doctorId = null;
            if(user.Role==UserRole.Doctor)
            {
                doctorId=await db.Doctors
                    .Where(d => d.UserId==user.Id)
                    .Select(d => (Guid?)d.Id)
                    .FirstOrDefaultAsync();
            }
            // =========================
            // AUTH STATE (ONLY IMPORTANT PART)
            // =========================
            _authStateService.SetUser(user, doctorId);
            await PersistCredentials();

            await _navigationService.GoToAsync("//dashboard");
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex);
            await SetErrorAsync("Системска грешка.");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // =========================
    // HELPERS
    // =========================
    private async Task SetErrorAsync(string message)
    {
        ErrorMessage=message;
        await _dialogService.ShowAlertAsync("Најава", message, "OK");
    }

    private void LoadSavedCredentials()
    {
        if(!_preferencesService.ContainsKey("saved_username"))
            return;

        Username= _preferencesService.LoadAsync("saved_username").GetAwaiter().GetResult();
        Password= _preferencesService.LoadAsync("saved_password").GetAwaiter().GetResult();
        RememberMe=true;
    }

    private async Task  PersistCredentials()
    {
        if(!RememberMe)
        {
            _preferencesService.Remove("saved_username");
            _preferencesService.Remove("saved_password");
            return;
        }

       await _preferencesService.SaveAsync("saved_username", Username);
        await _preferencesService.SaveAsync("saved_password", Password);
    }
}