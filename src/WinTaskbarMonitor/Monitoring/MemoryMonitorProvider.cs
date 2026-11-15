using System;
using System.Runtime.InteropServices;

namespace WinTaskbarMonitor.Monitoring;

public sealed class MemoryMonitorProvider
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    private readonly MEMORYSTATUSEX _memStatus = new();

    public (double usedGb, double totalGb, double usagePercent) GetMemoryMetrics()
    {
        if (!GlobalMemoryStatusEx(_memStatus))
        {
            return (0.0, 0.0, 0.0);
        }

        const double bytesToGb = 1024.0 * 1024.0 * 1024.0;
        double totalGb = _memStatus.ullTotalPhys / bytesToGb;
        ulong usedBytes = _memStatus.ullTotalPhys - _memStatus.ullAvailPhys;
        double usedGb = usedBytes / bytesToGb;
        double percent = _memStatus.ullTotalPhys > 0
            ? (double)usedBytes / _memStatus.ullTotalPhys * 100.0
            : 0.0;

        return (usedGb, totalGb, Math.Clamp(percent, 0.0, 100.0));
    }
}
