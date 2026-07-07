using EHMR.ViewModels.Therapies;

namespace EHMR.Views.Therapies;

public partial class TherapyCyclesPage : ContentPage
{
    private readonly MenuView menuView;

    public TherapyCyclesPage(TherapyCycleListViewModel viewModel, MenuView menuView)
    {
        InitializeComponent();

        // Доделување на ViewModel како контекст за податоци
        BindingContext=viewModel;
        this.menuView=menuView;
        MenuHost.Content=this.menuView;
    }

    // Автоматски повикај ја командата од ViewModel-от за да се наполнат податоците
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if(BindingContext is TherapyCycleListViewModel vm)
        {
            await vm.LoadAsync();
        }
    }
}