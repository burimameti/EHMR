// File: EHMR.Backups/ViewModels/BackupViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.Backups.ViewModels;

public partial class BackupViewModel : ObservableObject
{
    private readonly IBackupEngine _backupEngine;
    private readonly IBackupHistoryRepository _historyRepository;
    private readonly IEnumerable<IBackupStorageProvider> _storageProviders;

    [ObservableProperty]
    private BackupSettings settings = new();

    [ObservableProperty]
    private BackupProgress progress = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private List<BackupDestination> destinations = new();

    public BackupViewModel(
     IBackupEngine backupEngine,
     IBackupHistoryRepository historyRepository,
     IEnumerable<IBackupStorageProvider> storageProviders,
     IConfiguration configuration)   // додади ако веќе не постои некаде
    {
        _backupEngine=backupEngine;
        _historyRepository=historyRepository;
        _storageProviders=storageProviders;

        Destinations=_storageProviders
            .Select(p => new BackupDestination { Id=p.DestinationId, Key=p.Key, Name=p.Name })
            .ToList();

        if(Destinations.Count>0)
            Settings.DestinationId=Destinations[0].Id;

        Settings.DatabaseName=configuration["Backup:DatabaseName"]
            ??ExtractDatabaseName(configuration.GetConnectionString("Default"));
    }
    private static string ExtractDatabaseName(string? connectionString)
    {
        if(string.IsNullOrWhiteSpace(connectionString))
            return string.Empty;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach(var part in parts)
        {
            var kv = part.Split('=', 2);
            if(kv.Length==2&&
                (kv[0].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase)||
                 kv[0].Trim().Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase)))
            {
                return kv[1].Trim();
            }
        }

        return string.Empty;
    }
    [RelayCommand]
    private async Task StartBackupAsync()
    {
        if(IsBusy) return;
        if(Settings.DestinationId==Guid.Empty)
        {
            StatusMessage="Изберете дестинација пред да продолжите.";
            return;
        }

        Guid historyId;
        try
        {
            IsBusy=true;
            StatusMessage="Стартување на бекап...";
            historyId=await _historyRepository.RecordStartAsync(Settings);

            var result = await _backupEngine.ExecuteAsync(
      Settings,
      new Progress<BackupProgress>(value =>
      {
          Progress=new BackupProgress
          {
              Percentage=value.Percentage,
              Message=value.Message
          };
          StatusMessage=value.Message;
      }));

            await _historyRepository.RecordCompletionAsync(historyId, result);
            StatusMessage=result.Success
                ? "Бекапот е успешно завршен."
                : $"Грешка: {result.Message}";
        }
        catch(Exception ex)
        {
            StatusMessage=$"Грешка: {ex.Message}";
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            IsBusy=false;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        Settings=new BackupSettings
        {
            DestinationId=Destinations.Count>0 ? Destinations[0].Id : Guid.Empty
        };
        Progress=new BackupProgress();
        StatusMessage=string.Empty;
    }
}