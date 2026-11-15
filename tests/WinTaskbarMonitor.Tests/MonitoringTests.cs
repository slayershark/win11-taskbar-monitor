using System;
using System.Threading;
using WinTaskbarMonitor.Config;
using WinTaskbarMonitor.Monitoring;
using Xunit;

namespace WinTaskbarMonitor.Tests;

public class MonitoringTests
{
    [Fact]
    public void CpuMonitorProvider_ReturnsValidMetrics()
    {
        var provider = new CpuMonitorProvider();
        Assert.False(string.IsNullOrWhiteSpace(provider.CpuName));

        // Sleep briefly to accumulate CPU cycles
        Thread.Sleep(200);
        double usage = provider.GetCpuUsage();

        Assert.InRange(usage, 0.0, 100.0);
    }

    [Fact]
    public void MemoryMonitorProvider_ReturnsValidMetrics()
    {
        var provider = new MemoryMonitorProvider();
        var (usedGb, totalGb, percent) = provider.GetMemoryMetrics();

        Assert.True(totalGb > 0.0, "Total RAM should be greater than 0 GB");
        Assert.True(usedGb > 0.0, "Used RAM should be greater than 0 GB");
        Assert.True(usedGb <= totalGb, "Used RAM should not exceed total RAM");
        Assert.InRange(percent, 0.0, 100.0);
    }

    [Fact]
    public void GpuMonitorProvider_InitializesAndQueriesWithoutCrashing()
    {
        using var provider = new GpuMonitorProvider();
        var devices = provider.DetectedDevices;

        Assert.NotNull(devices);
        Assert.NotEmpty(devices);

        var (usage, temp, name) = provider.QueryMetrics();
        Assert.InRange(usage, 0.0, 100.0);
        Assert.False(string.IsNullOrWhiteSpace(name));

        if (temp.HasValue)
        {
            Assert.InRange(temp.Value, 15.0, 120.0);
        }
    }

    [Fact]
    public void CpuTemperatureProvider_QueriesWithoutCrashing()
    {
        using var provider = new CpuTemperatureProvider();
        double? temp = provider.QueryTemperatureC();

        if (temp.HasValue)
        {
            Assert.InRange(temp.Value, 15.0, 120.0);
        }
        else
        {
            // Null is an expected graceful fallback when running in non-elevated user mode
            Assert.Null(temp);
        }
    }

    [Fact]
    public void HardwareMonitorCoordinator_EmitsSnapshot()
    {
        using var coordinator = new HardwareMonitorCoordinator(500);
        HardwareSnapshot? received = null;
        var signal = new ManualResetEventSlim(false);

        coordinator.SnapshotUpdated += (s, snap) =>
        {
            received = snap;
            signal.Set();
        };

        coordinator.Start();
        bool signaled = signal.Wait(TimeSpan.FromSeconds(3));
        coordinator.Stop();

        Assert.True(signaled, "Coordinator should emit a snapshot within timeout");
        Assert.NotNull(received);
        Assert.InRange(received!.CpuUsage, 0.0, 100.0);
        Assert.InRange(received.RamUsagePercent, 0.0, 100.0);
        Assert.InRange(received.GpuUsage, 0.0, 100.0);
        Assert.True(received.RamTotalGb > 0);
    }
}
