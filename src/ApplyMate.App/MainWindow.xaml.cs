using ApplyMate.App.Services.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace ApplyMate.App;

public sealed partial class MainWindow : Window
{
    private readonly ITrayIconService _trayIconService;
    private AppWindow? _appWindow;

    public MainWindow()
    {
        InitializeComponent();

        _trayIconService = App.Services.GetRequiredService<ITrayIconService>();
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        if (_appWindow is not null)
        {
            _appWindow.Closing += OnAppWindowClosing;
        }

        Closed += OnClosed;
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_trayIconService.IsExitRequested)
        {
            return;
        }

        args.Cancel = true;
        _trayIconService.HideToTray();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_appWindow is not null)
        {
            _appWindow.Closing -= OnAppWindowClosing;
            _appWindow = null;
        }

        _trayIconService.Dispose();
    }
}
