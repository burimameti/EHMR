// File: EHMR.Backups/ViewModels/RestoreViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;

namespace EHMR.Backups.ViewModels;

public partial class RestoreViewModel : ObservableObject
{
    private readonly IRestoreEngine _restoreEngine;

    [ObservableProperty]
    private RestoreSettings settings = new();

    [ObservableProperty]
    private RestoreProgress progress = new();

    [ObservableProperty]
    private string selectedFile = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public RestoreViewModel(IRestoreEngine restoreEngine)
    {
        _restoreEngine=restoreEngine;
    }

    public void SelectBackupFile(string file)
    {
        SelectedFile=file;
        Settings.BackupFile=file;
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if(IsBusy) return;

        if(string.IsNullOrWhiteSpace(Settings.BackupFile))
        {
            StatusMessage="Бекап фајлот е задолжителен.";
            return;
        }

        if(!File.Exists(Settings.BackupFile))
        {
            StatusMessage="Избраниот фајл не постои.";
            return;
        }

        try
        {
            IsBusy=true;
            StatusMessage="Започнува враќање...";

            var result = await _restoreEngine.ExecuteAsync(
                Settings,
                new Progress<RestoreProgress>(value =>
                {
                    Progress.Percentage=value.Percentage;
                    Progress.Message=value.Message;
                    StatusMessage=value.Message;
                }));

            StatusMessage=result.Success
                ? "Враќањето е успешно завршено."
                : result.Message;
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
    private void Reset()
    {
        Settings=new RestoreSettings();
        Progress=new RestoreProgress();
        SelectedFile=string.Empty;
        StatusMessage=string.Empty;
    }
}