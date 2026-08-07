using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace EHMR.Backups.ViewModels
{
    /// <summary>
    /// Управување со дестинациите за резервни копии.
    ///
    /// Порано ова беше школка: LoadAsync само чистеше листа со коментар
    /// „Later this will come from BackupDestination table". Сега чита и пишува
    /// во табелата преку <see cref="IBackupDestinationRepository"/>.
    /// </summary>
    public partial class BackupDestinationViewModel : ObservableObject
    {
        private readonly IStorageProviderResolver _resolver;
        private readonly IBackupDestinationRepository _repository;

        public BackupDestinationViewModel(
            IStorageProviderResolver resolver,
            IBackupDestinationRepository repository)
        {
            _resolver=resolver;
            _repository=repository;
        }

        public ObservableCollection<BackupDestination> Destinations
        {
            get;
        } = new();

        /// <summary>Клучевите што ги поддржува StorageProviderResolver.</summary>
        public ObservableCollection<string> AvailableKeys
        {
            get;
        } = new(new[] { "local", "network", "cloud" });

        [ObservableProperty] private BackupDestination? selectedDestination;

        [ObservableProperty] private string statusMessage = string.Empty;

        [ObservableProperty] private bool isBusy;

        // ── поле за уредување ────────────────────────────────────────────
        [ObservableProperty] private string editName = string.Empty;
        [ObservableProperty] private string editKey = "local";
        [ObservableProperty] private string editPath = string.Empty;
        [ObservableProperty] private bool editIsActive = true;

        public bool HasSelection => SelectedDestination is not null;

        partial void OnSelectedDestinationChanged(BackupDestination? value)
        {
            OnPropertyChanged(nameof(HasSelection));

            if(value is null)
                return;

            EditName=value.Name;
            EditKey=string.IsNullOrWhiteSpace(value.Key) ? "local" : value.Key;
            EditPath=value.Path;
            EditIsActive=value.IsActive;
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if(IsBusy)
                return;

            try
            {
                IsBusy=true;

                var items = await _repository.GetAllAsync();

                Destinations.Clear();
                foreach(var item in items)
                    Destinations.Add(item);

                StatusMessage=Destinations.Count==0
                    ? "Нема зачувани дестинации."
                    : $"Вчитани {Destinations.Count} дестинации.";
            }
            catch(Exception ex)
            {
                StatusMessage=ex.Message;
            }
            finally
            {
                IsBusy=false;
            }
        }

        [RelayCommand]
        private void New()
        {
            SelectedDestination=null;

            EditName=string.Empty;
            EditKey="local";
            EditPath=string.Empty;
            EditIsActive=true;

            StatusMessage="Нова дестинација.";
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if(string.IsNullOrWhiteSpace(EditName)||string.IsNullOrWhiteSpace(EditPath))
            {
                StatusMessage="Името и патеката се задолжителни.";
                return;
            }

            try
            {
                IsBusy=true;

                if(SelectedDestination is null)
                {
                    var created = new BackupDestination
                    {
                        Name=EditName.Trim(),
                        Key=EditKey,
                        Path=EditPath.Trim(),
                        IsActive=EditIsActive
                    };

                    await _repository.AddAsync(created);
                    StatusMessage="Дестинацијата е додадена.";
                }
                else
                {
                    SelectedDestination.Name=EditName.Trim();
                    SelectedDestination.Key=EditKey;
                    SelectedDestination.Path=EditPath.Trim();
                    SelectedDestination.IsActive=EditIsActive;

                    await _repository.UpdateAsync(SelectedDestination);
                    StatusMessage="Дестинацијата е зачувана.";
                }

                await LoadAsync();
            }
            catch(Exception ex)
            {
                StatusMessage=ex.Message;
            }
            finally
            {
                IsBusy=false;
            }
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if(SelectedDestination is null)
                return;

            try
            {
                IsBusy=true;

                await _repository.DeleteAsync(SelectedDestination.Id);
                SelectedDestination=null;

                StatusMessage="Дестинацијата е избришана.";

                await LoadAsync();
            }
            catch(Exception ex)
            {
                StatusMessage=ex.Message;
            }
            finally
            {
                IsBusy=false;
            }
        }

        /// <summary>
        /// Проверува дали за оваа дестинација воопшто има регистриран провајдер.
        /// Провајдерите се градат од appsettings при подигање, па дестинација
        /// зачувана само во базата нема да има провајдер додека не се рестартира.
        /// </summary>
        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            if(SelectedDestination is null)
                return;

            try
            {
                var provider = _resolver.Resolve(SelectedDestination.Id);
                StatusMessage=$"Врската е исправна: {provider.Name} ({provider.Key})";
            }
            catch(Exception ex)
            {
                StatusMessage=ex.Message;
            }

            await Task.CompletedTask;
        }
    }
}
