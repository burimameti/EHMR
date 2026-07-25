using EHMR.Backups.ViewModels;
using EHMR.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Backups.Views
{

    public partial class BackupDashboardPage : ContentPage
    {
        private readonly BackupDashboardViewModel _viewModel;
        private readonly MenuView _menu;
        public BackupDashboardPage(BackupDashboardViewModel viewModel, MenuView menu)
        {
            InitializeComponent();
            _menu=menu;
            BindingContext=_viewModel=viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}
