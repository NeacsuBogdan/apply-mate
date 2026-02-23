namespace ApplyMate.App.Services.Notifications;

public interface IAppNotificationService
{
    void Initialize();

    Task RescheduleAsync(CancellationToken ct);

    void FlushPendingActivation();

    Task SendTestNotificationNowAsync(CancellationToken ct);
}
