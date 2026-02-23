namespace ApplyMate.App.Services.Settings;

public sealed class InMemorySettingsStore : ISettingsStore
{
    private AppSettings _cachedSettings = new();

    public Task<AppSettings> LoadAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var copy = new AppSettings
        {
            EnableNotifications = _cachedSettings.EnableNotifications,
            DailyNotificationTime = _cachedSettings.DailyNotificationTime,
            NoResponseDays = _cachedSettings.NoResponseDays,
            CheckEmailStaleDays = _cachedSettings.CheckEmailStaleDays,
            RunOnStartup = _cachedSettings.RunOnStartup
        };

        return Task.FromResult(copy);
    }

    public Task SaveAsync(AppSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ct.ThrowIfCancellationRequested();

        _cachedSettings = new AppSettings
        {
            EnableNotifications = settings.EnableNotifications,
            DailyNotificationTime = settings.DailyNotificationTime,
            NoResponseDays = settings.NoResponseDays,
            CheckEmailStaleDays = settings.CheckEmailStaleDays,
            RunOnStartup = settings.RunOnStartup
        };

        return Task.CompletedTask;
    }
}
