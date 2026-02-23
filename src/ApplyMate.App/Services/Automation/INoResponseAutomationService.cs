namespace ApplyMate.App.Services.Automation;

public interface INoResponseAutomationService
{
    Task StartAsync(CancellationToken ct);

    Task OnSettingsChangedAsync(CancellationToken ct);
}
