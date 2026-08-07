// File: EHMR.Backups/Views/RestorePage.xaml.cs
using EHMR.Backups.ViewModels;
using Microsoft.Maui.Controls;

namespace EHMR.Backups.Views;

public partial class RestorePage : ContentPage
{
    public RestorePage(RestoreViewModel viewModel)
    {
        InitializeComponent();
        BindingContext=viewModel;
    }
}