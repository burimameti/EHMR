using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _dialogService;
    private readonly IAuthStateService _authStateService;
    private readonly IPreferencesService _preferencesService;
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ILicenseService _licenseService;

    [ObservableProperty] private string username = "admin";
    [ObservableProperty] private string password="123456";
    [ObservableProperty] private bool rememberMe;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;

    public LoginViewModel(
        INavigationService navigationService,
        IUserDialogService dialogService,
        IAuthStateService authStateService,
        IPreferencesService preferencesService,
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ILicenseService licenseService)
    {
        Logger.Log("LoginViewModel constructor started");
        try
        {
            _navigationService=navigationService;
            _dialogService=dialogService;
            _authStateService=authStateService;
            _preferencesService=preferencesService;
            _dbFactory=dbFactory;
            _licenseService=licenseService;

            // NOTE: still synchronous-over-async (GetAwaiter().GetResult()) inside
            // LoadSavedCredentials(). Now wrapped in try/catch + logged so a failure
            // here can no longer produce a silent black-screen crash. Ideally this
            // should be moved to an async Init method called after construction -
            // see note below.
            LoadSavedCredentials();

            Logger.Log("LoginViewModel constructor completed successfully");
        }
        catch(Exception ex)
        {
            Logger.LogException("LoginViewModel constructor", ex);
            // Re-throw so it still surfaces to your existing ErrorPage flow in
            // App.xaml.cs rather than leaving the VM half-initialized.
            throw;
        }
    }

    // ═══════════════════════════════════════════ ЛИЦЕНЦА ═══════════════════════════════════════════

    [ObservableProperty] private bool isSystemLocked;
    [ObservableProperty] private string licenseKey = string.Empty;
    [ObservableProperty] private string licensedTo = string.Empty;

    public async Task RefreshLicenseStateAsync()
    {
        Logger.Log("RefreshLicenseStateAsync called");
        try
        {
            IsSystemLocked=await _licenseService.IsLockedAsync();
            Logger.Log($"License locked state: {IsSystemLocked}");

            if(IsSystemLocked)
                ErrorMessage="Системот е заклучен. Внесете лиценцен клуч за да продолжите.";
        }
        catch(Exception ex)
        {
            Logger.LogException("RefreshLicenseStateAsync", ex);
            ErrorMessage="Грешка при проверка на лиценца.";
        }
    }

    [RelayCommand]
    private async Task ActivateLicense()
    {
        Logger.Log("ActivateLicense command invoked");
        try
        {
            var result = await _licenseService.ActivateAsync(LicenseKey, LicensedTo);

            if(!result.Allowed)
            {
                Logger.Log($"License activation rejected: {result.Message}");
                await SetErrorAsync(result.Message??"Активацијата не успеа.");
                return;
            }

            IsSystemLocked=false;
            ErrorMessage=string.Empty;
            LicenseKey=string.Empty;

            Logger.Log("License activated successfully");

            await _dialogService.ShowAlertAsync(
                "Активирано",
                "Лиценцата е активирана. Може да се најавите.",
                "OK");
        }
        catch(Exception ex)
        {
            Logger.LogException("ActivateLicense", ex);
            await SetErrorAsync("Грешка при активирање на лиценца.");
        }
    }

    // =========================
    // LOGIN
    // =========================
    [RelayCommand]
    private async Task Login()
    {
        if(IsBusy)
            return;

        Logger.Log($"Login attempt started for username: {Username}");

        if(string.IsNullOrWhiteSpace(Username)||
            string.IsNullOrWhiteSpace(Password))
        {
            Logger.Log("Login aborted - missing username or password");
            await SetErrorAsync("Внесете корисничко име и лозинка.");
            return;
        }

        try
        {
            IsBusy=true;
            ErrorMessage=string.Empty;

            Logger.Log("Checking license lock state");
            if(await _licenseService.IsLockedAsync())
            {
                Logger.Log("Login blocked - system is locked");
                IsSystemLocked=true;
                await SetErrorAsync(
                    "Системот е заклучен. Лиценцата треба да се активира за да продолжите.");
                return;
            }

            Logger.Log("Creating DB context");
            await using var db = await _dbFactory.CreateDbContextAsync();
            Logger.Log("DB context created, querying user");
            
            var user = await db.Users
                .AsNoTracking()
                .Include(x => x.Modules)
                .Include(x => x.ModulePermissions)
                .Include(x => x.Scopes)
                .FirstOrDefaultAsync(x => x.Username==Username);

            if(user is null)
            {
                Logger.Log($"Login failed - user '{Username}' not found");
                await SetErrorAsync("Корисникот не постои.");
                return;
            }

            if(!user.IsActive)
            {
                Logger.Log($"Login failed - user '{Username}' is deactivated");
                await SetErrorAsync("Корисникот е деактивиран.");
                return;
            }

            // ⚠️ NOTE: replace later with hashing service
            if(user.PasswordHash!=Password)
            {
                Logger.Log($"Login failed - incorrect password for '{Username}'");
                await SetErrorAsync("Неточна лозинка.");
                return;
            }

            Guid? doctorId = null;
            if(user.Role==UserRole.Doctor)
            {
                Logger.Log("User is a Doctor - resolving doctorId");
                doctorId=await db.Doctors
                    .Where(d => d.UserId==user.Id)
                    .Select(d => (Guid?)d.Id)
                    .FirstOrDefaultAsync();
            }

            // =========================
            // AUTH STATE (ONLY IMPORTANT PART)
            // =========================
            Logger.Log("Setting auth state");
            _authStateService.SetUser(user, doctorId);

            Logger.Log("Persisting credentials");
            await PersistCredentials();

            Logger.Log("Navigating to //dashboard");
            await _navigationService.GoToAsync("//dashboard");
            Logger.Log("Login completed successfully");
        }
        catch(Exception ex)
        {
            Logger.LogException("Login", ex);
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
        try
        {
            if(!_preferencesService.ContainsKey("saved_username"))
            {
                Logger.Log("No saved credentials found");
                return;
            }

            Logger.Log("Loading saved credentials");
            Username=_preferencesService.LoadAsync("saved_username").GetAwaiter().GetResult();
            Password=_preferencesService.LoadAsync("saved_password").GetAwaiter().GetResult();
            RememberMe=true;
            Logger.Log("Saved credentials loaded successfully");
        }
        catch(Exception ex)
        {
            Logger.LogException("LoadSavedCredentials", ex);
            // Don't rethrow - a corrupt saved credential shouldn't block the whole
            // login screen from loading. Just start with a blank form.
            Username=string.Empty;
            Password=string.Empty;
            RememberMe=false;
        }
    }

    private async Task PersistCredentials()
    {
        try
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
        catch(Exception ex)
        {
            Logger.LogException("PersistCredentials", ex);
            // Non-fatal - don't block navigation to dashboard just because
            // "remember me" failed to save.
        }
    }
}