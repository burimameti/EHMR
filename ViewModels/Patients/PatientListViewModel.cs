using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Constants;
using EHMR.Desktop.Core.ViewModels;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

public partial class PatientListViewModel : BaseViewModel<Patient>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Patient> _selectedItemService;
    private readonly IAuthStateService _authService;

    [ObservableProperty]
    private Patient? _selectedPatient;

    // Својства за UI Пермисии (Видливост на контроли во XAML)
    [ObservableProperty]
    private bool _canCreatePatients;

    [ObservableProperty]
    private bool _canUpdatePatients;

    [ObservableProperty]
    private bool _canDeletePatients;

    private string _selectedStatus = "Сите";

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if(SetProperty(ref _selectedStatus, value))
            {
                _=ApplyFilterAsync();
            }
        }
    }

    private string _selectedGender = "Сите";

    public string SelectedGender
    {
        get => _selectedGender;
        set
        {
            if(SetProperty(ref _selectedGender, value))
            {
                _=ApplyFilterAsync();
            }
        }
    }

    public List<string> StatusFilters { get; } = ["Сите", "Active", "Inactive", "Critical"];
    public List<string> GenderFilters { get; } = ["Сите", "Male", "Female"];

    // =========================================================
    // CTOR
    // =========================================================
    public PatientListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<Patient> selectedItemService,
        IAuthStateService authService)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _dbFactory=dbFactory;
        _selectedItemService=selectedItemService;
        _authService=authService;

        // Првична евалуација на безбедносните дозволи
        EvaluatePermissions();
    }

    /// <summary>
    /// Ги проверува стринговите за дозволи зачувани во AuthState корисничкиот сесиски профил.
    /// </summary>
    private void EvaluatePermissions()
    {
        CanCreatePatients=_authService.Permissions.Contains(Modules.Patients);
        CanUpdatePatients=_authService.Permissions.Contains(Modules.Patients);
    }

    // =========================================================
    // LIFECYCLE OVERRIDES
    // =========================================================
    public override async Task OnAppearingAsync()
    {
        // СУШТИНСКА ПОПРАВКА: Освежи ги дозволите секој пат кога екранот станува активен
        EvaluatePermissions();

        // Главна заштита при самото отворање на погледот
        if(!_authService.Permissions.Contains(Modules.Patients, StringComparer.OrdinalIgnoreCase))
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате соодветни безбедносни пермисии за преглед на пациенти.", "OK");
            await NavigationService.GoToAsync($"//{AppRoutes.Dashboard}");
            return;
        }

        await LoadAsync();
    }

    protected override async Task OnRefreshAsync()
    {
        await LoadAsync();
    }

    // =========================================================
    // DATA LOADING & FILTERING
    // =========================================================
    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;
            ClearError();

            await using var db = await _dbFactory.CreateDbContextAsync();

            var patients = await db.Patients
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            _allItems=patients;

            await ApplyFilterAsync();
        }
        catch(Exception ex)
        {
            OnError($"Неуспешно вчитување на листата: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    protected override IEnumerable<Patient> FilterItems(string searchText, IEnumerable<Patient> items)
    {
        var query = items;

        if(!string.IsNullOrWhiteSpace(searchText))
        {
            query=query.Where(x =>
                (x.FirstName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                (x.LastName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                (x.Email?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                ($"{x.FirstName} {x.LastName}").Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        if(SelectedStatus!="Сите")
        {
            query=query.Where(x => x.Status.ToString()==SelectedStatus);
        }

        if(SelectedGender!="Сите")
        {
            query=query.Where(x => x.Gender.ToString()==SelectedGender);
        }

        return query;
    }

    // =========================================================
    // OPERATIONS & COMMANDS
    // =========================================================
    [RelayCommand]
    private async Task AddAsync()
    {
        if(!CanCreatePatients)
        {
            await UserDialogService.ShowAlertAsync("Акцијата е одбиена", "Немате авторизација за додавање нови пациенти во системот.", "OK");
            return;
        }

        _selectedItemService.SelectedItem=null;
        await NavigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task SelectAsync(Patient patient)
    {
        if(patient==null) return;

        _selectedItemService.SelectedItem=patient;
        await NavigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task EditAsync(Patient selectedPatient)
    {
        if(selectedPatient==null) return;

        if(!CanUpdatePatients)
        {
            await UserDialogService.ShowAlertAsync("Акцијата е одбиена", "Немате авторизација за измена на податоци за пациенти.", "OK");
            return;
        }

        _selectedItemService.SelectedItem=selectedPatient;
        await NavigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task DeleteAsync(Patient patient)
    {
        if(patient==null) return;

        if(!CanDeletePatients)
        {
            await UserDialogService.ShowAlertAsync("Акцијата е одбиена", "Немате безбедносна дозвола за бришење на пациенти.", "OK");
            return;
        }

        bool confirm = await UserDialogService.ShowConfirmationAsync(
            "Потврда за бришење",
            $"Дали сте сигурни дека сакате трајно да го избришете пациентот {patient.FirstName} {patient.LastName}?");

        if(!confirm) return;

        try
        {
            IsBusy=true;

            await using var db = await _dbFactory.CreateDbContextAsync();

            // Закачување на објектот доколку доаѓа од AsNoTracking состојба
            db.Patients.Entry(patient).State=EntityState.Deleted;
            await db.SaveChangesAsync();

            _allItems.Remove(patient);
            await ApplyFilterAsync();

            await UserDialogService.ShowAlertAsync("Избришано", "Пациентот е успешно отстранет од медицинскиот систем.", "OK");
        }
        catch(Exception ex)
        {
            OnError($"Неуспешно бришење на записот: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText=string.Empty;
        SelectedStatus="Сите";
        SelectedGender="Сите";
        await ApplyFilterAsync();
    }
}