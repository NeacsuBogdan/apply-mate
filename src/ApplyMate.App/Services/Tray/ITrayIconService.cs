using Microsoft.UI.Xaml;

namespace ApplyMate.App.Services.Tray;

public interface ITrayIconService : IDisposable
{
    void Initialize(Window window);
    bool IsExitRequested { get; }
    void HideToTray();
    void ShowFromTray();
}
