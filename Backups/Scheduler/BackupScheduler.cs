using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using EHMR.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace EHMR.Backups.Scheduler;

/// <summary>
/// Прави резервна копија по распоред и ги чисти постарите.
///
/// Порано ова беше празна школка: <c>EvaluateAsync</c> и <c>RunNowAsync</c> содржеа
/// само TODO коментари, класата воопшто не беше регистрирана во DI, а
/// <c>ScheduleCron</c> и <c>RetentionDays</c> од конфигурацијата никој не ги читаше.
/// </summary>
public sealed class BackupScheduler : IBackupScheduler, IDisposable
{
    private readonly IBackupEngine _backupEngine;
    private readonly IStorageProviderResolver _storageResolver;
    private readonly IPreferencesService _preferences;
    private readonly IOptions<BackupOptions> _options;

    // IBackupHistoryRepository е регистриран како Scoped. Овој распоредувач е Singleton,
    // па не смее да го држи директно — се отвора scope при секое извршување.
    private readonly IServiceScopeFactory _scopeFactory;

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Последната минута во која распоредот се извршил. Се чува меѓу подигања за
    /// да не се направи втора копија ако апликацијата се рестартира во истата минута.
    /// </summary>
    private const string LastRunKey = "backup_scheduler_last_run";

    private const string MinuteFormat = "yyyy-MM-ddTHH:mm";

    public bool IsRunning => _timer is not null;

    public DateTime? LastRunUtc
    {
        get; private set;
    }

    public BackupScheduler(
        IBackupEngine backupEngine,
        IStorageProviderResolver storageResolver,
        IServiceScopeFactory scopeFactory,
        IPreferencesService preferences,
        IOptions<BackupOptions> options)
    {
        _backupEngine=backupEngine;
        _storageResolver=storageResolver;
        _scopeFactory=scopeFactory;
        _preferences=preferences;
        _options=options;
    }

    // =====================================================
    // ЖИВОТЕН ЦИКЛУС
    // =====================================================

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if(_timer is not null)
            return;

        // Нема смисла да се врти тајмер за неисправен израз.
        if(!CronSchedule.TryParse(_options.Value.ScheduleCron, out _))
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Backup] Неисправен ScheduleCron: '{_options.Value.ScheduleCron}'. Распоредот не е активиран.");
            return;
        }

        _cts=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer=new PeriodicTimer(TimeSpan.FromMinutes(1));

        var stored = await _preferences.LoadAsync(LastRunKey);
        if(DateTime.TryParse(stored, out var parsed))
            LastRunUtc=parsed;

        _=Task.Run(SchedulerLoopAsync, _cts.Token);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();
        _timer?.Dispose();
        _timer=null;

        return Task.CompletedTask;
    }

    private async Task SchedulerLoopAsync()
    {
        try
        {
            while(await _timer!.WaitForNextTickAsync(_cts!.Token))
                await EvaluateAsync(_cts.Token);
        }
        catch(OperationCanceledException)
        {
            // Нормално гасење.
        }
    }

    // =====================================================
    // ОДЛУЧУВАЊЕ
    // =====================================================

    private async Task EvaluateAsync(CancellationToken cancellationToken)
    {
        try
        {
            if(!CronSchedule.TryParse(_options.Value.ScheduleCron, out var schedule))
                return;

            var now = DateTime.Now;

            if(!schedule!.Matches(now))
                return;

            // Тајмерот може да чукне двапати во иста минута — оди само еднаш.
            var stamp = now.ToString(MinuteFormat);
            if(await _preferences.LoadAsync(LastRunKey)==stamp)
                return;

            await _preferences.SaveAsync(LastRunKey, stamp);
            LastRunUtc=now;

            await RunNowAsync(cancellationToken);
            await ApplyRetentionAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            // Распоредот никогаш не смее да ја сруши апликацијата.
            System.Diagnostics.Debug.WriteLine($"[Backup] Распоредот падна: {ex}");
        }
    }

    // =====================================================
    // ИЗВРШУВАЊЕ
    // =====================================================

    public async Task<OperationResult<BackupResult>> RunNowAsync(
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        var destination =
            options.Destinations.FirstOrDefault(d => d.Key=="local")
            ??options.Destinations.FirstOrDefault();

        if(destination is null)
            return OperationResult<BackupResult>.Fail(
                "Нема конфигурирана дестинација во Backup:Destinations.");

        var settings = new BackupSettings
        {
            DestinationId=destination.Id,
            DestinationPath=destination.Path,
            Type=BackupType.Full,
            Compress=options.Compress,
            Encrypt=options.Encrypt,
            VerifyAfterBackup=options.VerifyAfterBackup
        };

        using var scope = _scopeFactory.CreateScope();
        var history = scope.ServiceProvider.GetRequiredService<IBackupHistoryRepository>();

        // Записот се отвора пред извршување за да се види и неуспешен обид во историјата.
        var historyId = await history.RecordStartAsync(settings, cancellationToken);

        var result = await _backupEngine.ExecuteAsync(settings, progress: null, cancellationToken);

        await history.RecordCompletionAsync(historyId, result, cancellationToken);

        return result;
    }

    // =====================================================
    // ЧИСТЕЊЕ ПО РОК
    // =====================================================

    public async Task<int> ApplyRetentionAsync(CancellationToken cancellationToken = default)
    {
        var retentionDays = _options.Value.RetentionDays;

        if(retentionDays<=0)
            return 0;

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        using var scope = _scopeFactory.CreateScope();
        var history = scope.ServiceProvider.GetRequiredService<IBackupHistoryRepository>();

        // Земи ги записите пред бришење за да се избришат и самите фајлови.
        // DeleteOlderThanAsync брише само редови во базата — без ова, старите
        // копии остануваат на диск засекогаш иако исчезнуваат од историјата.
        var recent = await history.GetRecentAsync(int.MaxValue, cancellationToken);
        var expired = recent.Where(x => x.StartedAt<cutoff).ToList();

        foreach(var record in expired)
        {
            if(string.IsNullOrWhiteSpace(record.FilePath))
                continue;

            try
            {
                var provider = _storageResolver.Resolve(record.DestinationId);
                await provider.DeleteAsync(record.FilePath, cancellationToken);
            }
            catch(Exception ex)
            {
                // Недостижна дестинација не смее да го запре чистењето на останатите.
                System.Diagnostics.Debug.WriteLine(
                    $"[Backup] Не успеа бришење на '{record.FilePath}': {ex.Message}");
            }
        }

        await history.DeleteOlderThanAsync(cutoff, cancellationToken);

        return expired.Count;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _timer?.Dispose();
        _cts?.Dispose();
    }
}
