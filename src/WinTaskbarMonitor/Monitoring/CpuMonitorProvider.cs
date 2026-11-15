using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinTaskbarMonitor.Monitoring;

public sealed class CpuMonitorProvider
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        public ulong ToUInt64() => ((ulong)dwHighDateTime << 32) | dwLowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    private ulong _prevIdleTime;
    private ulong _prevKernelTime;
    private ulong _prevUserTime;
    private bool _isFirstCall = true;

    public string CpuName { get; }

    public CpuMonitorProvider()
    {
        CpuName = QueryCpuName();
        // Warm up initial sample
        GetSystemTimes(out var idle, out var kernel, out var user);
        _prevIdleTime = idle.ToUInt64();
        _prevKernelTime = kernel.ToUInt64();
        _prevUserTime = user.ToUInt64();
    }

    public double GetCpuUsage()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            return 0.0;
        }

        ulong idle = idleTime.ToUInt64();
        ulong kernel = kernelTime.ToUInt64();
        ulong user = userTime.ToUInt64();

        if (_isFirstCall)
        {
            _prevIdleTime = idle;
            _prevKernelTime = kernel;
            _prevUserTime = user;
            _isFirstCall = false;
            return 0.0;
        }

        ulong idleDiff = idle - _prevIdleTime;
        ulong kernelDiff = kernel - _prevKernelTime;
        ulong userDiff = user - _prevUserTime;

        _prevIdleTime = idle;
        _prevKernelTime = kernel;
        _prevUserTime = user;

        // Total system time is (kernel + user) because kernelTime in GetSystemTimes already includes idle time.
        ulong totalDiff = kernelDiff + userDiff;
        if (totalDiff == 0) return 0.0;

        if (totalDiff < idleDiff) return 0.0;

        double usage = (double)(totalDiff - idleDiff) / totalDiff * 100.0;
        return Math.Clamp(usage, 0.0, 100.0);
    }

    private static string QueryCpuName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            string? name = key?.GetValue("ProcessorNameString") as string;
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }
        }
        catch
        {
            // Fallback
        }
        return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "CPU";
    }
}
