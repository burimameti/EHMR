using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using EHMR.ViewModels;

namespace EHMR.Views;

public partial class PatientListPage : ContentPage
{
    private readonly PatientListViewModel _viewModel;

    public PatientListPage(PatientListViewModel viewModel, MenuView menu)
    {
        InitializeComponent();

        _viewModel=viewModel;
        BindingContext=_viewModel;

        // Поставување на глобалното лево мени
        MenuHost.Content=menu;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // ЕДИНСТВЕНО место каде што се повикува вчитување на податоците од базата при влез во страницата
        if(_viewModel!=null)
        {
            await _viewModel.OnAppearingAsync();
        }
    }
}