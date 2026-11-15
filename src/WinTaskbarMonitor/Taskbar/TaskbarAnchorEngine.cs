using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WinTaskbarMonitor.Config;

namespace WinTaskbarMonitor.Taskbar;

public sealed class TaskbarAnchorEngine : IDisposable
{
    #region Win32 Constants & Structs
    private const int ABM_GETTASKBARPOS = 0x00000005;
    private const int ABM_GETSTATE = 0x00000004;

    private const int ABE_LEFT = 0;
    private const int ABE_TOP = 1;
    private const int ABE_RIGHT = 2;
    private const int ABE_BOTTOM = 3;

    private const int ABS_AUTOHIDE = 0x0000001;

    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int GWL_EXSTYLE = -20;

    private const uint WM_DISPLAYCHANGE = 0x007E;
    private const uint WM_DPICHANGED = 0x02E0;
    private const uint WM_SETTINGCHANGE = 0x001A;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern uint RegisterWindowMessage(string lpString);
    #endregion

    private readonly Window _window;
    private readonly ISettingsService _settingsService;
    private readonly uint _taskbarCreatedMsg;
    private HwndSource? _hwndSource;
    private bool _isDisposed;

    public TaskbarAnchorEngine(Window window, ISettingsService settingsService)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _taskbarCreatedMsg = RegisterWindowMessage("TaskbarCreated");

        _window.SourceInitialized += OnWindowSourceInitialized;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnWindowSourceInitialized(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(_window);
        var hWnd = helper.Handle;

        // Apply WS_EX_TOOLWINDOW and WS_EX_NOACTIVATE styles
        int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        SetWindowLong(hWnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

        _hwndSource = HwndSource.FromHwnd(hWnd);
        _hwndSource?.AddHook(WndProc);

        Reposition();
    }

    private void OnSettingsChanged(object? sender, AppSettings e)
    {
        _window.Dispatcher.InvokeAsync(Reposition);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == (int)_taskbarCreatedMsg ||
            msg == (int)WM_DISPLAYCHANGE ||
            msg == (int)WM_DPICHANGED ||
            msg == (int)WM_SETTINGCHANGE)
        {
            Reposition();
        }

        return IntPtr.Zero;
    }

    public void Reposition()
    {
        try
        {
            var appbarData = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA))
            };

            uint state = SHAppBarMessage(ABM_GETSTATE, ref appbarData);
            bool isAutoHide = (state & ABS_AUTOHIDE) != 0;

            uint res = SHAppBarMessage(ABM_GETTASKBARPOS, ref appbarData);
            if (res == 0)
            {
                // Taskbar position not retrieved, fallback to default primary screen work area
                FallbackReposition();
                return;
            }

            var taskbarRect = appbarData.rc;
            var dpi = VisualTreeHelper.GetDpi(_window);
            double dpiX = dpi.DpiScaleX;
            double dpiY = dpi.DpiScaleY;

            double windowWidth = _window.ActualWidth > 0 ? _window.ActualWidth : _window.Width;
            double windowHeight = _window.ActualHeight > 0 ? _window.ActualHeight : _window.Height;

            if (double.IsNaN(windowWidth) || windowWidth <= 0) windowWidth = 380;
            if (double.IsNaN(windowHeight) || windowHeight <= 0) windowHeight = 36;

            var settings = _settingsService.Current;

            double targetScreenX;
            double targetScreenY;

            // Locate system tray
            IntPtr trayWnd = FindWindow("Shell_TrayWnd", null);
            IntPtr trayNotifyWnd = IntPtr.Zero;
            if (trayWnd != IntPtr.Zero)
            {
                trayNotifyWnd = FindWindowEx(trayWnd, IntPtr.Zero, "TrayNotifyWnd", null);
            }

            RECT trayRect = default;
            bool hasTrayRect = trayNotifyWnd != IntPtr.Zero && GetWindowRect(trayNotifyWnd, out trayRect);

            // Calculate X anchor
            if (settings.Alignment == TaskbarAlignment.RightEdge || !hasTrayRect)
            {
                // Right aligned before quick settings (reserve ~240 physical px for clock/wifi/battery)
                int reservedTrayWidth = (int)(240 * dpiX);
                targetScreenX = (taskbarRect.Right - reservedTrayWidth - (windowWidth * dpiX)) / dpiX;
            }
            else
            {
                // Anchored to the left of TrayNotifyWnd with a 12px margin
                int margin = (int)(12 * dpiX);
                targetScreenX = (trayRect.Left - margin - (windowWidth * dpiX)) / dpiX;
            }

            // Calculate Y anchor (vertically centered in taskbar)
            double taskbarHeightDpi = taskbarRect.Height / dpiY;
            double taskbarTopDpi = taskbarRect.Top / dpiY;
            targetScreenY = taskbarTopDpi + Math.Max(2, (taskbarHeightDpi - windowHeight) / 2.0);

            // Apply manual user offset from settings
            targetScreenX += settings.OffsetX;
            targetScreenY += settings.OffsetY;

            // If auto-hide is enabled and taskbar is currently hidden (small height), adjust visibility
            if (isAutoHide && taskbarRect.Height < 10)
            {
                _window.Opacity = 0.0;
            }
            else
            {
                _window.Opacity = 1.0;
            }

            _window.Left = targetScreenX;
            _window.Top = targetScreenY;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[TaskbarAnchorEngine] Reposition error: {ex.Message}");
            FallbackReposition();
        }
    }

    private void FallbackReposition()
    {
        var workArea = SystemParameters.WorkArea;
        _window.Left = workArea.Right - _window.Width - 180;
        _window.Top = workArea.Bottom - _window.Height - 6;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _settingsService.SettingsChanged -= OnSettingsChanged;
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
    }
}
