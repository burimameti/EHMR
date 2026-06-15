using System;
using Microsoft.Maui.Controls;
using EHMR.ViewModels;

namespace EHMR.Views;

public partial class UserEditPage : ContentPage
{
    private readonly UserEditViewModel _viewModel;
    private readonly MenuView _menuView;

    public UserEditPage(UserEditViewModel viewModel, MenuView menuView)
    {
        InitializeComponent();

        _viewModel=viewModel;
        BindingContext=_viewModel;

        _menuView=menuView;
        MenuHost.Content=menuView;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Паметно иницијализирање на податоците кога страницата ќе се прикаже на екран
        if(_viewModel!=null)
        {
            await _viewModel.InitializeAsync();
        }
    }
}