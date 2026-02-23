namespace ApplyMate.App.Services.Settings;

public sealed class AppSettings
{
    public bool EnableNotifications { get; set; } = true;

    public TimeOnly DailyNotificationTime { get; set; } = new(9, 0);

    public int NoResponseDays { get; set; } = 30;

    public int CheckEmailStaleDays { get; set; } = 7;

    public bool RunOnStartup { get; set; }
}
