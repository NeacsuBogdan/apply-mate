using System.Text.Json;
using ApplyMate.Infrastructure.Persistence.Pathing;

namespace ApplyMate.App.Services.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppSettings? _cached;

    public JsonSettingsStore(ILocalStoragePathProvider localStoragePathProvider)
    {
        ArgumentNullException.ThrowIfNull(localStoragePathProvider);

        var localRoot = localStoragePathProvider.GetApplyMateLocalFolderPath();
        _settingsPath = Path.Combine(localRoot, "settings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_cached is not null)
            {
                return Clone(_cached);
            }

            if (!File.Exists(_settingsPath))
            {
                _cached = Normalize(new AppSettings());
                await PersistAsync(_cached, ct);
                return Clone(_cached);
            }

            await using var stream = new FileStream(
                _settingsPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, ct)
                         ?? new AppSettings();

            _cached = Normalize(loaded);
            return Clone(_cached);
        }
        catch (JsonException)
        {
            _cached = Normalize(new AppSettings());
            await PersistAsync(_cached, ct);
            return Clone(_cached);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _gate.WaitAsync(ct);
        try
        {
            var normalized = Normalize(Clone(settings));
            await PersistAsync(normalized, ct);
            _cached = normalized;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task PersistAsync(AppSettings settings, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(
            _settingsPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);

        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, ct);
    }

    private static AppSettings Clone(AppSettings source)
    {
        return new AppSettings
        {
            EnableNotifications = source.EnableNotifications,
            DailyNotificationTime = source.DailyNotificationTime,
            NoResponseDays = source.NoResponseDays,
            CheckEmailStaleDays = source.CheckEmailStaleDays,
            RunOnStartup = source.RunOnStartup
        };
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        if (settings.NoResponseDays <= 0)
        {
            settings.NoResponseDays = 30;
        }

        if (settings.CheckEmailStaleDays <= 0)
        {
            settings.CheckEmailStaleDays = 7;
        }

        if (settings.DailyNotificationTime == default)
        {
            settings.DailyNotificationTime = new TimeOnly(9, 0);
        }

        return settings;
    }
}
