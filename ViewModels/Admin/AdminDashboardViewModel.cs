using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using EHMR.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.ViewModels.Admin;


public partial class AdminDashboardViewModel : ObservableObject
{

    private readonly IUserService _userService;
    private readonly IDoctorService _doctorService;
    private readonly IMkb10CodeService _mkbImportService;
    private readonly IBackupHistoryRepository _backupService;
   private readonly INavigationService _navigationService;


    [ObservableProperty]
    private bool isBusy;



    [ObservableProperty]
    private ObservableCollection<FFMetricTileItem> cards = [];



    [ObservableProperty]
    private ObservableCollection<object> backupGridRows = [];



    public List<string> BackupGridColumns
    {
        get;
    } =
    [
        "Датум",
        "Големина",
        "Статус"
    ];



    public IAsyncRelayCommand RefreshCommand
    {
        get;
    }

    public IAsyncRelayCommand GoToUsersCommand
    {
        get;
    }

    public IAsyncRelayCommand GoToDoctorsCommand
    {
        get;
    }

    public IAsyncRelayCommand GoToBackupsCommand
    {
        get;
    }

    public IAsyncRelayCommand ImportMkbCommand
    {
        get;
    }



    public AdminDashboardViewModel(
        IUserService userService,
        IDoctorService doctorService,
        IMkb10CodeService mkbImportService,
        IBackupHistoryRepository backupService,
        INavigationService navigationService)
    {

        _userService=userService;
        _doctorService=doctorService;
        _mkbImportService=mkbImportService;
        _backupService=backupService;


        _navigationService=navigationService;
        RefreshCommand=
            new AsyncRelayCommand(LoadAsync);



        GoToUsersCommand=
    new AsyncRelayCommand(
        () => _navigationService.GoToAsync(AppRoutes.Users.List));

        GoToDoctorsCommand=
            new AsyncRelayCommand(
                () => Shell.Current.GoToAsync(AppRoutes.Doctors.List));



        GoToBackupsCommand=
            new AsyncRelayCommand(
                () => Shell.Current.GoToAsync(AppRoutes.Backup.History));



        ImportMkbCommand=
            new AsyncRelayCommand(
                () => Shell.Current.GoToAsync(AppRoutes.Mkb10Codes.List));



        BuildCards();

    }



    //[RelayCommand]
    //private async Task GoToDoctors()
    //{
    //    await _navigationService.GoToAsync($"{AppRoutes.Doctors.List}");
    //}

    //[RelayCommand]
    //private async Task GoToUsers()
    //{   
    //    await _navigationService.GoToAsync($"{AppRoutes.Users.List}");
    //}

    public async Task InitializeAsync()
    {
        await LoadAsync();
    }






    private async Task LoadAsync()
    {

        if(IsBusy)
            return;


        try
        {

            IsBusy=true;



            var users =
                await _userService.GetUsersAsync();



            var doctors =
                await _doctorService.GetAllAsync();



            var mkb =
                await _mkbImportService.GetAllAsync();



            var lastBackup =
                await _backupService.GetLastAsync();




            UpdateCards(
                users.Count,
                doctors.Count,
                mkb.Count,
                lastBackup?.CompletedAt);



            var backups =
                await _backupService.GetRecentAsync(10);



            BackupGridRows=
                new ObservableCollection<object>(backups);

        }
        finally
        {
            IsBusy=false;
        }

    }

    private void BuildCards()
    {
        Cards=
        [
            new()
            {
                Title="Корисници",
                Subtitle="Управување со корисници",
                Icon="\uf0c0",
                Variant=MetricTileVariant.Info,
                Value="—",
                Command=GoToUsersCommand
            },
            new()
            {
                Title="Доктори",
                Subtitle="Регистрирани доктори",
                Icon="\uf0f0",
                Variant=MetricTileVariant.Primary,
                Value="—",
                Command=GoToDoctorsCommand
            },
            new()
            {
                Title="МКБ-10",
                Subtitle="Шифрарник",
                Icon="\uf15c",
                Variant=MetricTileVariant.Warning,
                Value="—",
                Command=ImportMkbCommand
            },
            new()
            {
                Title="Бекапи",
                Subtitle="Последна копија",
                Icon="\uf0c7",
                Variant=MetricTileVariant.Success,
                Value="—",
                Command=GoToBackupsCommand
            }
            ];

    }

    private void UpdateCards(
        int users,
        int doctors,
        int mkb,
        DateTime? backupDate)
    {

        if(Cards.Count<4)
            return;



        Cards[0].Value=
            users.ToString();



        Cards[1].Value=
            doctors.ToString();



        Cards[2].Value=
            mkb.ToString();



        Cards[3].Value=
            backupDate?
            .ToString("dd.MM.yyyy HH:mm")
            ??
            "Нема";

    }

}




public partial class FFMetricTileItem : ObservableObject
{


    [ObservableProperty]
    private string title = "";


    [ObservableProperty]
    private string value = "";


    [ObservableProperty]
    private string subtitle = "";


    [ObservableProperty]
    private string icon = "";


    [ObservableProperty]
    private MetricTileVariant variant =
        MetricTileVariant.Primary;



    [ObservableProperty]
    private ICommand? command;



    [ObservableProperty]
    private object? commandParameter;


}