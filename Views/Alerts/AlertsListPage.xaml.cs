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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
