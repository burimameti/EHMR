
using EHMR.Backups.Interfaces;
using EHMR.Backups.Services;


namespace EHMR.Backups.Scheduler;

public sealed class BackupScheduler : IDisposable
{
    private readonly IBackupEngine _backupEngine;

    private readonly IServiceScopeFactory _scopeFactory;

    private PeriodicTimer? _timer;

    private CancellationTokenSource? _cts;



    public bool IsRunning => _timer!=null;



    public BackupScheduler(
        IBackupEngine backupEngine,
        IServiceScopeFactory scopeFactory)
    {
        _backupEngine=backupEngine;
        _scopeFactory=scopeFactory;
    }



    public async Task StartAsync(
        CancellationToken cancellationToken = default)
    {
        if(_timer!=null)
            return;


        _cts=CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);

        _timer=new PeriodicTimer(
            TimeSpan.FromMinutes(1));


        _=Task.Run(
            SchedulerLoop,
            _cts.Token);

        await Task.CompletedTask;
    }



    private async Task SchedulerLoop()
    {
        while(await _timer!.WaitForNextTickAsync(_cts!.Token))
        {
            await EvaluateAsync();
        }
    }



    private async Task EvaluateAsync()
    {
        using var scope =
            _scopeFactory.CreateScope();

        // TODO
        // Load BackupConfiguration
        // Determine if backup should run
        // Execute BackupEngine
    }



    public async Task RunNowAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope =
            _scopeFactory.CreateScope();

        // TODO
        // Load default BackupConfiguration
        // Convert to BackupSettings
        // await _backupEngine.ExecuteAsync(...)
    }



    public async Task StopAsync(
        CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();

        _timer?.Dispose();

        _timer=null;

        await Task.CompletedTask;
    }



    public void Dispose()
    {
        _cts?.Cancel();

        _timer?.Dispose();

        _cts?.Dispose();
    }
}