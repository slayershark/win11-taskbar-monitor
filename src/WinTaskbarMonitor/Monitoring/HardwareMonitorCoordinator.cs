using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WinTaskbarMonitor.Monitoring;

public sealed class HardwareMonitorCoordinator : IDisposable
{
    private readonly CpuMonitorProvider _cpuProvider;
    private readonly MemoryMonitorProvider _memoryProvider;
    private readonly GpuMonitorProvider _gpuProvider;
    private readonly CpuTemperatureProvider _cpuTempProvider;

    private CancellationTokenSource? _cts;
    private Task? _monitorLoopTask;
    private int _intervalMs = 1000;
    private readonly object _timerLock = new();

    public event EventHandler<HardwareSnapshot>? SnapshotUpdated;

    public IReadOnlyList<GpuDeviceInfo> DetectedGpus => _gpuProvider.DetectedDevices;

    public HardwareSnapshot? LatestSnapshot { get; private set; }

    public HardwareMonitorCoordinator(int initialIntervalMs = 1000, int selectedGpuIndex = -1)
    {
        _intervalMs = Math.Max(250, initialIntervalMs);

        _cpuProvider = new CpuMonitorProvider();
        _memoryProvider = new MemoryMonitorProvider();
        _gpuProvider = new GpuMonitorProvider();
        _cpuTempProvider = new CpuTemperatureProvider();

        _gpuProvider.SelectDevice(selectedGpuIndex);
    }

    public void SetInterval(int intervalMs)
    {
        lock (_timerLock)
        {
            _intervalMs = Math.Clamp(intervalMs, 250, 10000);
        }
    }

    public void SelectGpu(int gpuIndex)
    {
        _gpuProvider.SelectDevice(gpuIndex);
    }

    public void Start()
    {
        lock (_timerLock)
        {
            if (_cts != null) return; // Already running

            _cts = new CancellationTokenSource();
            _monitorLoopTask = Task.Run(() => MonitorLoopAsync(_cts.Token));
        }
    }

    public void Stop()
    {
        lock (_timerLock)
        {
            if (_cts == null) return;

            try
            {
                _cts.Cancel();
                _monitorLoopTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[HardwareMonitorCoordinator] Stop exception: {ex.Message}");
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _monitorLoopTask = null;
            }
        }
    }

    private async Task MonitorLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var snapshot = CollectSnapshot();
                LatestSnapshot = snapshot;
                SnapshotUpdated?.Invoke(this, snapshot);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[HardwareMonitorCoordinator] Error during metric poll: {ex.Message}");
            }

            int delay;
            lock (_timerLock)
            {
                delay = _intervalMs;
            }

            try
            {
                await Task.Delay(delay, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public HardwareSnapshot CollectSnapshot()
    {
        double cpuUsage = _cpuProvider.GetCpuUsage();
        double? cpuTemp = _cpuTempProvider.QueryTemperatureC();

        var (ramUsed, ramTotal, ramPercent) = _memoryProvider.GetMemoryMetrics();
        var (gpuUsage, gpuTemp, gpuName) = _gpuProvider.QueryMetrics();

        return new HardwareSnapshot
        {
            CpuUsage = Math.Round(cpuUsage, 1),
            CpuName = _cpuProvider.CpuName,
            CpuTempC = cpuTemp.HasValue ? Math.Round(cpuTemp.Value, 1) : null,

            GpuUsage = Math.Round(gpuUsage, 1),
            GpuName = gpuName,
            GpuTempC = gpuTemp.HasValue ? Math.Round(gpuTemp.Value, 1) : null,

            RamUsedGb = Math.Round(ramUsed, 1),
            RamTotalGb = Math.Round(ramTotal, 1),
            RamUsagePercent = Math.Round(ramPercent, 1),
            Timestamp = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        Stop();
        _cpuProvider.Dispose();
        _gpuProvider.Dispose();
        _cpuTempProvider.Dispose();
    }
}
