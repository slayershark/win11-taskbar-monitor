using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace WinTaskbarMonitor;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "WinTaskbarMonitor_SingleInstance_Mutex";

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (s, args) =>
        {
            Trace.WriteLine($"[App] Unhandled UI Exception: {args.Exception.Message}");
            args.Handled = true; // Prevent crash
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Trace.WriteLine($"[App] Unhandled Domain Exception: {args.ExceptionObject}");
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // Ignore
            }
            _mutex.Dispose();
        }
        base.OnExit(e);
    }
}
