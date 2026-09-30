using EHMR.ViewModels;

namespace EHMR.Views;

public partial class ApplicationRegimeListPage : ContentPage
{
    public ApplicationRegimeListPage(ApplicationRegimeListViewModel viewModel, MenuView menuView)
    {
        InitializeComponent();
        BindingContext=viewModel;
        MenuHost.Content=menuView;
        Loaded += async (_, _) => await viewModel.LoadAsync();
    }
}