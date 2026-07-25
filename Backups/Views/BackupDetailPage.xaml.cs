using EHMR.Backups.ViewModels;

namespace EHMR.Backups.Views
{
    public partial class BackupDetailPage : ContentPage
    {
        private readonly BackupDetailsViewModel _viewModel;

        public BackupDetailPage(BackupDetailsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext=_viewModel=viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}
