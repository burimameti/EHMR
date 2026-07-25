// File: EHMR.Backups/Views/RestorePage.xaml.cs
using EHMR.Backups.ViewModels;

namespace EHMR.Backups.Views;

public partial class RestorePage : ContentPage
{
    public RestorePage(RestoreViewModel viewModel)
    {
        InitializeComponent();
        BindingContext=viewModel;
    }
}