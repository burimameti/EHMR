using EHMR.ViewModels.Alerts;

namespace EHMR.Views.Alerts;

public partial class AlertsListPage : ContentPage
{
    private readonly AlertsListViewModel _viewModel;

    public AlertsListPage(AlertsListViewModel viewModel, MenuView menu)
    {
        InitializeComponent();
        _viewModel=viewModel;
        BindingContext=_viewModel;
        MenuHost.Content=menu;
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//dashboard", true);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
