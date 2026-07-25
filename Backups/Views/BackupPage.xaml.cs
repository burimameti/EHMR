// File: EHMR.Backups/Views/BackupPage.xaml.cs
using EHMR.Backups.ViewModels;

namespace EHMR.Backups.Views;

public partial class BackupPage : ContentPage
{
    public BackupPage(BackupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext=viewModel;
    }
}