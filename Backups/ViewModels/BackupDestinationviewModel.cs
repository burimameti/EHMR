using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using System.Collections.ObjectModel;

namespace EHMR.Backups.ViewModels
{


    public partial class BackupDestinationViewModel : ObservableObject
    {
        private readonly IStorageProviderResolver _resolver;



        public BackupDestinationViewModel(
            IStorageProviderResolver resolver)
        {
            _resolver=resolver;
        }



        public ObservableCollection<BackupDestination> Destinations
        {
            get;
        }
            = new();



        [ObservableProperty]
        private BackupDestination? selectedDestination;



        [ObservableProperty]
        private string statusMessage = string.Empty;



        [RelayCommand]
        private async Task LoadAsync()
        {
            try
            {
                Destinations.Clear();


                // Later this will come from BackupDestination table
                // For now providers registration

                await Task.CompletedTask;


                StatusMessage=
                    "Destinations loaded.";
            }
            catch(Exception ex)
            {
                StatusMessage=ex.Message;
            }
        }



        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            if(SelectedDestination==null)
                return;


            try
            {
                var provider =
                    _resolver.Resolve(
                        SelectedDestination.Id);


                StatusMessage=
                    $"Connection OK: {provider.GetType().Name}";
            }
            catch(Exception ex)
            {
                StatusMessage=
                    ex.Message;
            }


            await Task.CompletedTask;
        }
    }
}
