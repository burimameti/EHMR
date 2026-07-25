using EHMR.ViewModels.Admin;

namespace EHMR.Views.Admin;

public partial class AdminPage : ContentPage
{
    private readonly MenuView _menu;
    public AdminPage(AdminDashboardViewModel viewModel, MenuView menu)
    {
        InitializeComponent();
        _menu=menu;
        BindingContext=viewModel;
        MenuHost.Content = menu;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if(BindingContext is AdminDashboardViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}