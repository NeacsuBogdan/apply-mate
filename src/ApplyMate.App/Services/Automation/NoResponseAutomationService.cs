using ApplyMate.App.Messaging;
using ApplyMate.App.Services.Settings;
using ApplyMate.Core.Abstractions;
using CommunityToolkit.Mvvm.Messaging;

namespace ApplyMate.App.Services.Automation;

public sealed class NoResponseAutomationService : INoResponseAutomationService, IDisposable
{
    private readonly IJobApplicationRepository _repository;
    private readonly ISettingsStore _settingsStore;
    private readonly IDateProvider _dateProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private Timer? _timer;
    private bool _started;

    public NoResponseAutomationService(
        IJobApplicationRepository repository,
        ISettingsStore settingsStore,
        IDateProvider dateProvider)
    {
        _repository = repository;
        _settingsStore = settingsStore;
        _dateProvider = dateProvider;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (_started)
        {
            return;
        }

        _started = true;
        await ApplyNowAndRescheduleAsync(ct);
    }

    public Task OnSettingsChangedAsync(CancellationToken ct)
    {
        return ApplyNowAndRescheduleAsync(ct);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _gate.Dispose();
    }

    private async Task ApplyNowAndRescheduleAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var settings = await _settingsStore.LoadAsync(ct);
            var thresholdDays = settings.NoResponseDays <= 0 ? 30 : settings.NoResponseDays;
            var affectedRows = await _repository.ApplyNoResponseRuleAsync(
                _dateProvider.Today,
                thresholdDays,
                ct);

            if (affectedRows > 0)
            {
                WeakReferenceMessenger.Default.Send(new NoResponseRuleAppliedMessage(affectedRows));
            }

            ScheduleNextDailyRun();
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ScheduleNextDailyRun()
    {
        var now = DateTimeOffset.Now;
        var tomorrow = now.Date.AddDays(1).AddMinutes(5);
        var dueTime = tomorrow - now;

        if (dueTime < TimeSpan.FromMinutes(1))
        {
            dueTime = TimeSpan.FromMinutes(1);
        }

        _timer ??= new Timer(OnTimerElapsed);
        _timer.Change(dueTime, Timeout.InfiniteTimeSpan);
    }

    private void OnTimerElapsed(object? state)
    {
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await ApplyNowAndRescheduleAsync(CancellationToken.None);
                }
                catch
                {
                }
            });
    }
}
