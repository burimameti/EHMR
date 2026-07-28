using EHMR.Backups.ViewModels;

namespace EHMR.Backups.Views;

public partial class BackupDestinationsPage : ContentPage
{
    private readonly BackupDestinationViewModel _viewModel;

    public BackupDestinationsPage(BackupDestinationViewModel viewModel)
    {
        InitializeComponent();

        _viewModel=viewModel;
        BindingContext=_viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
