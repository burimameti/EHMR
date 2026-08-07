// File: EHMR.Backups/Views/BackupPage.xaml.cs
using EHMR.Backups.ViewModels;
using Microsoft.Maui.Controls;
namespace EHMR.Backups.Views;

public partial class BackupPage : ContentPage
{
    public BackupPage(BackupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext=viewModel;
    }
}