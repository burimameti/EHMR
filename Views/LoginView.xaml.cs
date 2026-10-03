using EHMR.ViewModels;
using Microsoft.Maui.Controls;

namespace EHMR.Views;

public partial class LoginView : ContentPage
{
    // Нема потреба од посебна private променлива,
    // ViewModel-от го ставаме директно во BindingContext.
    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();

        // Сега MAUI автоматски ќе ја донесе точната, жива инстанца на ViewModel-от
        BindingContext=viewModel;
        Appearing+=OnAppearing;
    }

    private async void OnAppearing(object? sender, EventArgs e)
    {
        await viewModel.InitializeAsync();
    }
}