using Microsoft.Maui.Controls;
using EHMR.ViewModels;

namespace EHMR.Views.Therapies;

public partial class TherapyCyclesPage : ContentPage
{
    private readonly MenuView menuView;

    public TherapyCyclesPage(TherapyCyclesViewModel viewModel, MenuView menuView)
    {
        InitializeComponent();

        // Доделување на ViewModel како контекст за податоци
        BindingContext=viewModel;
        this.menuView=menuView;
        MenuHost.Content=this.menuView;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Автоматски повикај ја командата од ViewModel-от за да се наполнат податоците
        if(BindingContext is TherapyCyclesViewModel viewModel&&viewModel.LoadDataCommand.CanExecute(null))
        {
            await viewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }

}