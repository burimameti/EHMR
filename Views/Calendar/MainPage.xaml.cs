using EHMR.ViewModels;
using EHMR.ViewModels.Calendar;

namespace EHMR.Views.Calendar;

public partial class MainPage : ContentPage
{
    public MainPage(CalendarDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext=viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Автоматско освежување на податоците при секое отворање на страната
        if(BindingContext is CalendarDashboardViewModel vm)
        {
            await vm.LoadDashboardDataAsync();
        }
    }
}