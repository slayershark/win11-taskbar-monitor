using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinTaskbarMonitor.Monitoring;

public sealed class CpuMonitorProvider : IDisposable
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

    // PDH APIs for Windows Task Manager % Processor Utility
    [DllImport("pdh.dll", EntryPoint = "PdhOpenQueryW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int PdhOpenQuery(string? szDataSource, IntPtr dwUserData, out IntPtr phQuery);

    [DllImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int PdhAddEnglishCounter(IntPtr hQuery, string szFullCounterPath, IntPtr dwUserData, out IntPtr phCounter);

    [DllImport("pdh.dll", SetLastError = true)]
    private static extern int PdhCollectQueryData(IntPtr hQuery);

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct PDH_FMT_COUNTERVALUE
    {
        [FieldOffset(0)] public uint CStatus;
        [FieldOffset(8)] public double doubleValue;
    }

    [DllImport("pdh.dll", SetLastError = true)]
    private static extern int PdhGetFormattedCounterValue(IntPtr hCounter, uint dwFormat, IntPtr lpdwType, out PDH_FMT_COUNTERVALUE pValue);

    [DllImport("pdh.dll", SetLastError = true)]
    private static extern int PdhCloseQuery(IntPtr hQuery);

    private const uint PDH_FMT_DOUBLE = 0x00000200;

    private IntPtr _hPdhQuery = IntPtr.Zero;
    private IntPtr _hCpuCounter = IntPtr.Zero;
    private bool _pdhInitialized;

    private ulong _prevIdleTime;
    private ulong _prevKernelTime;
    private ulong _prevUserTime;
    private bool _isFirstCall = true;

    public string CpuName { get; }

    public CpuMonitorProvider()
    {
        CpuName = QueryCpuName();

        // 1. Initialize PDH % Processor Utility (Task Manager parity)
        try
        {
            if (PdhOpenQuery(null, IntPtr.Zero, out _hPdhQuery) == 0)
            {
                if (PdhAddEnglishCounter(_hPdhQuery, @"\Processor Information(_Total)\% Processor Time", IntPtr.Zero, out _hCpuCounter) == 0)
                {
                    PdhCollectQueryData(_hPdhQuery);
                    _pdhInitialized = true;
                }
            }
        }
        catch
        {
            _pdhInitialized = false;
        }

        // 2. Initialize fallback GetSystemTimes
        GetSystemTimes(out var idle, out var kernel, out var user);
        _prevIdleTime = idle.ToUInt64();
        _prevKernelTime = kernel.ToUInt64();
        _prevUserTime = user.ToUInt64();
    }

    public double GetCpuUsage()
    {
        // Primary: Query PDH for exact Task Manager % Processor Utility
        if (_pdhInitialized && _hPdhQuery != IntPtr.Zero && _hCpuCounter != IntPtr.Zero)
        {
            try
            {
                if (PdhCollectQueryData(_hPdhQuery) == 0)
                {
                    if (PdhGetFormattedCounterValue(_hCpuCounter, PDH_FMT_DOUBLE, IntPtr.Zero, out var pValue) == 0 && pValue.CStatus == 0)
                    {
                        return Math.Clamp(pValue.doubleValue, 0.0, 100.0);
                    }
                }
            }
            catch
            {
                // Fall back to GetSystemTimes below
            }
        }

        // Secondary fallback: GetSystemTimes
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

        ulong totalDiff = kernelDiff + userDiff;
        if (totalDiff == 0 || totalDiff < idleDiff) return 0.0;

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

    public void Dispose()
    {
        if (_hPdhQuery != IntPtr.Zero)
        {
            try
            {
                PdhCloseQuery(_hPdhQuery);
            }
            catch { }
            _hPdhQuery = IntPtr.Zero;
            _hCpuCounter = IntPtr.Zero;
            _pdhInitialized = false;
        }
    }
}
