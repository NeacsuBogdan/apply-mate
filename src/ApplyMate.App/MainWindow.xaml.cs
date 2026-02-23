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
    private bool _closeHookInitialized;

    public MainWindow()
    {
        InitializeComponent();

        _trayIconService = App.Services.GetRequiredService<ITrayIconService>();
        Activated += OnWindowActivated;
        Closed += OnClosed;
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_closeHookInitialized)
        {
            return;
        }

        var hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        if (_appWindow is null)
        {
            return;
        }

        _appWindow.Closing += OnAppWindowClosing;
        _closeHookInitialized = true;
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
        Activated -= OnWindowActivated;

        if (_appWindow is not null)
        {
            _appWindow.Closing -= OnAppWindowClosing;
            _appWindow = null;
        }

        _trayIconService.Dispose();
    }
}
