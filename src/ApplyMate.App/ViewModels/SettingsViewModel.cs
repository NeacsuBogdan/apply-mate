using ApplyMate.App.Services.Automation;
using ApplyMate.App.Services.Notifications;
using ApplyMate.App.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace ApplyMate.App.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private const string StartupTaskId = "ApplyMateStartupTask";

    private readonly ISettingsStore _settingsStore;
    private readonly INoResponseAutomationService _noResponseAutomationService;
    private readonly IAppNotificationService _notificationService;

    public SettingsViewModel(
        ISettingsStore settingsStore,
        INoResponseAutomationService noResponseAutomationService,
        IAppNotificationService notificationService)
    {
        _settingsStore = settingsStore;
        _noResponseAutomationService = noResponseAutomationService;
        _notificationService = notificationService;
        DailyNotificationTime = TimeSpan.FromHours(9);
        NoResponseDaysText = "30";
        CheckEmailStaleDaysText = "7";
    }

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private TimeSpan _dailyNotificationTime;

    [ObservableProperty]
    private string _noResponseDaysText;

    [ObservableProperty]
    private string _checkEmailStaleDaysText;

    [ObservableProperty]
    private bool _runOnStartup;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    public Visibility DebugNotificationVisibility =>
#if DEBUG
        Visibility.Visible;
#else
        Visibility.Collapsed;
#endif

    public async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            StatusMessage = null;

            var settings = await _settingsStore.LoadAsync(ct);
            EnableNotifications = settings.EnableNotifications;
            DailyNotificationTime = settings.DailyNotificationTime.ToTimeSpan();
            NoResponseDaysText = settings.NoResponseDays.ToString();
            CheckEmailStaleDaysText = settings.CheckEmailStaleDays.ToString();
            RunOnStartup = await ResolveRunOnStartupAsync(settings.RunOnStartup);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            StatusMessage = null;

            var noResponseDays = ParsePositiveInt(NoResponseDaysText, "No Response Days");
            var checkEmailStaleDays = ParsePositiveInt(CheckEmailStaleDaysText, "Check Email Stale Days");

            var settings = new AppSettings
            {
                EnableNotifications = EnableNotifications,
                DailyNotificationTime = TimeOnly.FromTimeSpan(NormalizeTime(DailyNotificationTime)),
                NoResponseDays = noResponseDays,
                CheckEmailStaleDays = checkEmailStaleDays,
                RunOnStartup = await ApplyRunOnStartupPreferenceAsync(RunOnStartup)
            };

            RunOnStartup = settings.RunOnStartup;
            await _settingsStore.SaveAsync(settings, CancellationToken.None);
            await _noResponseAutomationService.OnSettingsChangedAsync(CancellationToken.None);
            await _notificationService.RescheduleAsync(CancellationToken.None);
            StatusMessage = "Settings saved.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static int ParsePositiveInt(string value, string fieldName)
    {
        if (!int.TryParse(value?.Trim(), out var parsed) || parsed <= 0)
        {
            throw new InvalidOperationException($"{fieldName} must be a number greater than zero.");
        }

        return parsed;
    }

    private static TimeSpan NormalizeTime(TimeSpan time)
    {
        var ticksPerDay = TimeSpan.TicksPerDay;
        var normalizedTicks = time.Ticks % ticksPerDay;
        if (normalizedTicks < 0)
        {
            normalizedTicks += ticksPerDay;
        }

        return TimeSpan.FromTicks(normalizedTicks);
    }

#if DEBUG
    [RelayCommand]
    private async Task SendTestNotificationAsync()
    {
        ErrorMessage = null;
        StatusMessage = null;

        try
        {
            await _notificationService.SendTestNotificationNowAsync(CancellationToken.None);
            StatusMessage = "Test notification sent.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
#endif

    private static async Task<bool> ResolveRunOnStartupAsync(bool fallback)
    {
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            return task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        }
        catch
        {
            return fallback;
        }
    }

    private static async Task<bool> ApplyRunOnStartupPreferenceAsync(bool requested)
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            if (requested)
            {
                if (startupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy)
                {
                    return true;
                }

                var state = await startupTask.RequestEnableAsync();
                return state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            }

            if (startupTask.State == StartupTaskState.Enabled)
            {
                startupTask.Disable();
            }

            return startupTask.State == StartupTaskState.EnabledByPolicy;
        }
        catch
        {
            return false;
        }
    }
}
