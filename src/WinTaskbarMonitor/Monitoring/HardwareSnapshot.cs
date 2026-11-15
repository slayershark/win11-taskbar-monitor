using System;

namespace WinTaskbarMonitor.Monitoring;

public sealed record HardwareSnapshot
{
    public double CpuUsage { get; init; }
    public string CpuName { get; init; } = "CPU";
    public double? CpuTempC { get; init; }

    public double GpuUsage { get; init; }
    public string GpuName { get; init; } = "GPU";
    public double? GpuTempC { get; init; }
    public int GpuIndex { get; init; }

    public double RamUsedGb { get; init; }
    public double RamTotalGb { get; init; }
    public double RamUsagePercent { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public sealed record GpuDeviceInfo(int Index, string Name, string Vendor, bool IsDiscrete);
