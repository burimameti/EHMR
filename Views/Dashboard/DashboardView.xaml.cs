using EHMR.ViewModels;
using System.Diagnostics;

namespace EHMR.Views;

public partial class DashboardView : ContentPage
{
    private readonly DashboardViewModel _vm;

    public DashboardView(
         DashboardViewModel vm,
         MenuView menu
       )
    {
        InitializeComponent();

        _vm=vm;

        BindingContext=_vm;
        MenuHost.Content=menu;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Debug.WriteLine("Dashboard OnAppearing");

        if(BindingContext is DashboardViewModel vm)
        {
            Debug.WriteLine($"VM found: {vm.GetHashCode()}");
            _=vm.Initialize();
        }
    }
}