using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace ApplyMate.App.Services.Tray;

public sealed class TrayIconService : ITrayIconService
{
    private const int GwlpWndProc = -4;
    private const int WmClose = 0x0010;
    private const int WmNull = 0x0000;
    private const int WmApp = 0x8000;
    private const int WmTrayIcon = WmApp + 11;
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonUp = 0x0205;

    private const uint NimAdd = 0x00000000;
    private const uint NimDelete = 0x00000002;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;

    private const uint MfString = 0x00000000;
    private const uint TpmRightButton = 0x0002;
    private const uint TpmReturnCmd = 0x0100;
    private const int CommandOpen = 1001;
    private const int CommandExit = 1002;

    private const int SwHide = 0;
    private const int SwShow = 5;
    private const int SwRestore = 9;
    private static readonly nint IdiApplication = 32512;

    private readonly WndProc _wndProcDelegate;
    private readonly object _lock = new();

    private Window? _window;
    private DispatcherQueue? _dispatcherQueue;
    private IntPtr _hwnd;
    private IntPtr _originalWndProc;
    private NotifyIconData _notifyIconData;
    private bool _isInitialized;
    private bool _isExitRequested;
    private bool _disposed;

    public TrayIconService()
    {
        _wndProcDelegate = WindowProcedure;
    }

    public bool IsExitRequested => _isExitRequested;

    public void Initialize(Window window)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(window);

        lock (_lock)
        {
            if (_isInitialized)
            {
                return;
            }

            _window = window;
            _dispatcherQueue = window.DispatcherQueue;
            _hwnd = WindowNative.GetWindowHandle(window);

            var wndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            _originalWndProc = SetWindowLongPtr(_hwnd, GwlpWndProc, wndProcPtr);

            _notifyIconData = new NotifyIconData
            {
                cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NifIcon | NifMessage | NifTip,
                uCallbackMessage = WmTrayIcon,
                hIcon = LoadIcon(IntPtr.Zero, IdiApplication),
                szTip = "ApplyMate"
            };

            _ = Shell_NotifyIcon(NimAdd, ref _notifyIconData);
            _isInitialized = true;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_isInitialized)
            {
                _ = Shell_NotifyIcon(NimDelete, ref _notifyIconData);
                SetWindowLongPtr(_hwnd, GwlpWndProc, _originalWndProc);
                _isInitialized = false;
            }
        }
    }

    public void HideToTray()
    {
        HideWindow();
    }

    public void ShowFromTray()
    {
        ShowWindow();
    }

    private IntPtr WindowProcedure(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (msg == WmClose && !_isExitRequested)
        {
            HideWindow();
            return IntPtr.Zero;
        }

        if (msg == WmTrayIcon)
        {
            var eventType = unchecked((int)lParam.ToInt64());
            if (eventType == WmLButtonUp)
            {
                ShowWindow();
                return IntPtr.Zero;
            }

            if (eventType == WmRButtonUp)
            {
                ShowContextMenu();
                return IntPtr.Zero;
            }
        }

        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        var menuHandle = CreatePopupMenu();
        if (menuHandle == IntPtr.Zero)
        {
            return;
        }

        _ = AppendMenu(menuHandle, MfString, CommandOpen, "Open ApplyMate");
        _ = AppendMenu(menuHandle, MfString, CommandExit, "Exit");

        _ = SetForegroundWindow(_hwnd);

        if (!GetCursorPos(out var point))
        {
            _ = DestroyMenu(menuHandle);
            return;
        }

        var command = TrackPopupMenu(
            menuHandle,
            TpmRightButton | TpmReturnCmd,
            point.X,
            point.Y,
            0,
            _hwnd,
            IntPtr.Zero);

        _ = PostMessage(_hwnd, WmNull, IntPtr.Zero, IntPtr.Zero);
        _ = DestroyMenu(menuHandle);

        switch (command)
        {
            case CommandOpen:
                ShowWindow();
                break;
            case CommandExit:
                ExitApplication();
                break;
        }
    }

    private void ExitApplication()
    {
        if (_dispatcherQueue is null || _window is null)
        {
            return;
        }

        _ = _dispatcherQueue.TryEnqueue(
            () =>
            {
                _isExitRequested = true;
                _window.Close();
            });
    }

    private void HideWindow()
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        _ = ShowWindowNative(_hwnd, SwHide);
    }

    private void ShowWindow()
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        var isVisible = IsWindowVisible(_hwnd);
        _ = ShowWindowNative(_hwnd, isVisible ? SwShow : SwRestore);
        _ = SetForegroundWindow(_hwnd);
    }

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
        {
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }

        return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uTimeoutOrVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpData);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(
        IntPtr lpPrevWndFunc,
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool AppendMenu(
        IntPtr hMenu,
        uint uFlags,
        uint uIDNewItem,
        string lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint TrackPopupMenu(
        IntPtr hMenu,
        uint uFlags,
        int x,
        int y,
        int nReserved,
        IntPtr hWnd,
        IntPtr prcRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindowNative(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindowVisible(IntPtr hWnd);
}
