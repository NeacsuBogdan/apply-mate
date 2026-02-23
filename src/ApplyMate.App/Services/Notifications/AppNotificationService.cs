using ApplyMate.App.Navigation;
using ApplyMate.App.Services.Settings;
using ApplyMate.Core.Abstractions;
using ApplyMate.Core.Domain;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace ApplyMate.App.Services.Notifications;

public sealed class AppNotificationService : IAppNotificationService, IDisposable
{
    private const string InterviewGroup = "applymate-interviews";
    private const string CheckEmailGroup = "applymate-check-email";
    private const string ActionKey = "action";
    private const string AppIdKey = "appId";
    private const string InterviewAction = "interview";
    private const string CheckEmailAction = "check-email";

    private readonly IJobApplicationRepository _repository;
    private readonly ISettingsStore _settingsStore;
    private readonly INavigationService _navigationService;
    private readonly IDateProvider _dateProvider;
    private readonly List<ScheduledNotification> _scheduled = [];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _pendingLock = new();

    private Timer? _timer;
    private NotificationRoute? _pendingRoute;
    private bool _isInitialized;
    private bool _disposed;

    public AppNotificationService(
        IJobApplicationRepository repository,
        ISettingsStore settingsStore,
        INavigationService navigationService,
        IDateProvider dateProvider)
    {
        _repository = repository;
        _settingsStore = settingsStore;
        _navigationService = navigationService;
        _dateProvider = dateProvider;
    }

    public void Initialize()
    {
        if (_isInitialized || !AppNotificationManager.IsSupported())
        {
            return;
        }

        try
        {
            var manager = AppNotificationManager.Default;
            manager.NotificationInvoked += OnNotificationInvoked;
            manager.Register();
            _isInitialized = true;
        }
        catch
        {
            _isInitialized = false;
        }
    }

    public async Task RescheduleAsync(CancellationToken ct)
    {
        Initialize();
        if (!_isInitialized)
        {
            return;
        }

        await _gate.WaitAsync(ct);

        try
        {
            _scheduled.Clear();
            _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            var manager = AppNotificationManager.Default;
            await manager.RemoveByGroupAsync(InterviewGroup);
            await manager.RemoveByGroupAsync(CheckEmailGroup);

            var settings = await _settingsStore.LoadAsync(ct);
            if (!settings.EnableNotifications)
            {
                return;
            }

            var notifications = await BuildNextSevenDayScheduleAsync(settings, ct);
            _scheduled.AddRange(notifications.OrderBy(x => x.DeliverAt));
            ScheduleNextTimerLocked();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void FlushPendingActivation()
    {
        NotificationRoute? route;
        lock (_pendingLock)
        {
            route = _pendingRoute;
            _pendingRoute = null;
        }

        if (route is null)
        {
            return;
        }

        NavigateOrQueue(route);
    }

    public Task SendTestNotificationNowAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Initialize();

        if (!_isInitialized)
        {
            return Task.CompletedTask;
        }

        var notification = new AppNotificationBuilder()
            .AddText("ApplyMate test notification")
            .AddText("Notification pipeline is active.")
            .AddArgument(ActionKey, CheckEmailAction)
            .SetGroup(CheckEmailGroup)
            .SetTag($"test-{Guid.NewGuid():N}")
            .BuildNotification();

        notification.ExpiresOnReboot = true;
        AppNotificationManager.Default.Show(notification);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_isInitialized)
        {
            var manager = AppNotificationManager.Default;
            manager.NotificationInvoked -= OnNotificationInvoked;
            manager.Unregister();
        }

        _timer?.Dispose();
        _gate.Dispose();
    }

    private async Task<List<ScheduledNotification>> BuildNextSevenDayScheduleAsync(
        AppSettings settings,
        CancellationToken ct)
    {
        var now = DateTimeOffset.Now;
        var scheduled = new List<ScheduledNotification>();

        var rangeStart = new DateTimeOffset(now.Date, now.Offset);
        var rangeEnd = rangeStart.AddDays(8).AddTicks(-1);
        var upcomingInterviews = await _repository.GetUpcomingInterviewsAsync(rangeStart, rangeEnd, ct);

        var staleDays = settings.CheckEmailStaleDays <= 0 ? 7 : settings.CheckEmailStaleDays;
        var allApps = await _repository.GetAllAsync(null, null, ct);
        var staleCount = allApps.Count(app =>
            app.Status is ApplicationStatus.Applied or ApplicationStatus.InProgress &&
            _dateProvider.Today.DayNumber - app.LastStatusChangedOn.DayNumber >= staleDays);

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = now.Date.AddDays(dayOffset);
            var localDateTime = date + settings.DailyNotificationTime.ToTimeSpan();
            var deliverAt = new DateTimeOffset(
                localDateTime,
                TimeZoneInfo.Local.GetUtcOffset(localDateTime));

            if (deliverAt <= now)
            {
                continue;
            }

            foreach (var interview in upcomingInterviews.Where(x =>
                         x.InterviewAt.HasValue &&
                         x.InterviewAt.Value.ToLocalTime().Date == date))
            {
                var interviewLocal = interview.InterviewAt!.Value.ToLocalTime();
                var appNotification = new AppNotificationBuilder()
                    .AddText("Don't forget interviews")
                    .AddText($"{interview.CompanyName} - {interview.JobName} at {interviewLocal:HH:mm}")
                    .AddArgument(ActionKey, InterviewAction)
                    .AddArgument(AppIdKey, interview.Id.ToString("D"))
                    .SetGroup(InterviewGroup)
                    .SetTag($"interview-{date:yyyyMMdd}-{interview.Id:N}")
                    .BuildNotification();

                appNotification.Expiration = deliverAt.AddDays(1);
                scheduled.Add(new ScheduledNotification(deliverAt, appNotification));
            }

            if (staleCount > 0)
            {
                var staleNotification = new AppNotificationBuilder()
                    .AddText("Check email for applications")
                    .AddText($"{staleCount} applications may need follow-up.")
                    .AddArgument(ActionKey, CheckEmailAction)
                    .SetGroup(CheckEmailGroup)
                    .SetTag($"check-email-{date:yyyyMMdd}")
                    .BuildNotification();

                staleNotification.Expiration = deliverAt.AddDays(1);
                scheduled.Add(new ScheduledNotification(deliverAt, staleNotification));
            }
        }

        return scheduled;
    }

    private void ScheduleNextTimerLocked()
    {
        if (_scheduled.Count == 0)
        {
            return;
        }

        var nextDue = _scheduled[0].DeliverAt;
        var dueTime = nextDue - DateTimeOffset.Now;
        if (dueTime < TimeSpan.Zero)
        {
            dueTime = TimeSpan.Zero;
        }

        _timer ??= new Timer(OnTimerElapsed);
        _timer.Change(dueTime, Timeout.InfiniteTimeSpan);
    }

    private void OnTimerElapsed(object? state)
    {
        _ = Task.Run(
            async () =>
            {
                await _gate.WaitAsync(CancellationToken.None);
                try
                {
                    if (_scheduled.Count == 0)
                    {
                        return;
                    }

                    var now = DateTimeOffset.Now;
                    var ready = _scheduled
                        .TakeWhile(x => x.DeliverAt <= now.AddSeconds(1))
                        .ToList();

                    if (ready.Count == 0)
                    {
                        ScheduleNextTimerLocked();
                        return;
                    }

                    foreach (var item in ready)
                    {
                        AppNotificationManager.Default.Show(item.Notification);
                    }

                    _scheduled.RemoveAll(x => x.DeliverAt <= now.AddSeconds(1));
                    ScheduleNextTimerLocked();
                }
                finally
                {
                    _gate.Release();
                }
            });
    }

    private void OnNotificationInvoked(
        AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
        if (!args.Arguments.TryGetValue(ActionKey, out var action))
        {
            return;
        }

        NotificationRoute? route = action switch
        {
            InterviewAction when args.Arguments.TryGetValue(AppIdKey, out var appIdText) &&
                                Guid.TryParse(appIdText, out var appId) =>
                new NotificationRoute(InterviewAction, appId),
            CheckEmailAction => new NotificationRoute(CheckEmailAction, null),
            _ => null
        };

        if (route is not null)
        {
            NavigateOrQueue(route);
        }
    }

    private void NavigateOrQueue(NotificationRoute route)
    {
        var window = App.CurrentWindow;
        var dispatcherQueue = window?.DispatcherQueue;
        if (dispatcherQueue is null)
        {
            QueueRoute(route);
            return;
        }

        var enqueued = dispatcherQueue.TryEnqueue(
            () =>
            {
                if (!TryNavigate(route))
                {
                    QueueRoute(route);
                }
            });

        if (!enqueued)
        {
            QueueRoute(route);
        }
    }

    private bool TryNavigate(NotificationRoute route)
    {
        return route.Action switch
        {
            InterviewAction when route.AppId.HasValue =>
                _navigationService.NavigateTo(PageKeys.ApplicationDetails, route.AppId.Value),
            CheckEmailAction =>
                _navigationService.NavigateTo(PageKeys.Applications, NotificationRouteConstants.CheckEmailFilter),
            _ => false
        };
    }

    private void QueueRoute(NotificationRoute route)
    {
        lock (_pendingLock)
        {
            _pendingRoute = route;
        }
    }

    private sealed record ScheduledNotification(DateTimeOffset DeliverAt, AppNotification Notification);

    private sealed record NotificationRoute(string Action, Guid? AppId);
}
