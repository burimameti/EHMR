// File: EHMR.Backups/Views/BackupHistoryPage.xaml.cs
using DocumentFormat.OpenXml.Office.CustomUI;
using EHMR.Backups.ViewModels;
using EHMR.Views;

namespace EHMR.Backups.Views;

public partial class BackupHistoryPage : ContentPage
{
    private readonly BackupHistoryViewModel _viewModel;
    private readonly MenuView _menu;
    public BackupHistoryPage(BackupHistoryViewModel viewModel, MenuView menu)
    {
        InitializeComponent();
        _menu=menu;
        BindingContext=_viewModel=viewModel;
        MenuHost.Content=menu;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
