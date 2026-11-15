using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LibreHardwareMonitor.Hardware;

namespace WinTaskbarMonitor.Monitoring;

public sealed class GpuMonitorProvider : IDisposable
{
    private readonly List<GpuDeviceInfo> _devices = new();
    private IntPtr _nvmlDeviceHandle = IntPtr.Zero;
    private bool _nvmlInitialized;
    private int _selectedGpuIndex = -1; // -1 for Auto

    private readonly Computer _computer;
    private IHardware? _lhmSelectedGpu;

    public IReadOnlyList<GpuDeviceInfo> DetectedDevices => _devices;

    public GpuMonitorProvider()
    {
        _computer = new Computer
        {
            IsGpuEnabled = true,
            IsCpuEnabled = false
        };

        Initialize();
    }

    private void Initialize()
    {
        // 1. Try Initialize NVML
        if (NvmlInterop.IsNvmlAvailable())
        {
            try
            {
                int res = NvmlInterop.nvmlInit_v2();
                if (res == NvmlInterop.NVML_SUCCESS)
                {
                    _nvmlInitialized = true;
                    if (NvmlInterop.nvmlDeviceGetCount_v2(out uint count) == NvmlInterop.NVML_SUCCESS)
                    {
                        for (uint i = 0; i < count; i++)
                        {
                            if (NvmlInterop.nvmlDeviceGetHandleByIndex_v2(i, out var handle) == NvmlInterop.NVML_SUCCESS)
                            {
                                byte[] nameBuf = new byte[64];
                                string devName = "NVIDIA GPU";
                                if (NvmlInterop.nvmlDeviceGetName(handle, nameBuf, (uint)nameBuf.Length) == NvmlInterop.NVML_SUCCESS)
                                {
                                    devName = Encoding.ASCII.GetString(nameBuf).TrimEnd('\0').Trim();
                                }

                                _devices.Add(new GpuDeviceInfo(_devices.Count, devName, "NVIDIA", true));
                                if (_nvmlDeviceHandle == IntPtr.Zero)
                                {
                                    _nvmlDeviceHandle = handle;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[GpuMonitorProvider] NVML init failed: {ex.Message}");
            }
        }

        // 2. Open LibreHardwareMonitor for Intel/AMD/Other GPUs
        try
        {
            _computer.Open();
            foreach (var hw in _computer.Hardware)
            {
                if (hw.HardwareType is HardwareType.GpuIntel or HardwareType.GpuAmd or HardwareType.GpuNvidia)
                {
                    bool isDiscrete = hw.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd;
                    string vendor = hw.HardwareType switch
                    {
                        HardwareType.GpuIntel => "Intel",
                        HardwareType.GpuAmd => "AMD",
                        HardwareType.GpuNvidia => "NVIDIA",
                        _ => "Unknown"
                    };

                    // Don't add duplicate if NVML already detected NVIDIA GPU
                    bool alreadyAdded = _devices.Exists(d => d.Vendor == vendor && d.Name.Equals(hw.Name, StringComparison.OrdinalIgnoreCase));
                    if (!alreadyAdded)
                    {
                        _devices.Add(new GpuDeviceInfo(_devices.Count, hw.Name, vendor, isDiscrete));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[GpuMonitorProvider] LHM init failed: {ex.Message}");
        }

        // Fallback default if no GPU found
        if (_devices.Count == 0)
        {
            _devices.Add(new GpuDeviceInfo(0, "GPU", "Generic", false));
        }

        SelectDevice(-1);
    }

    public void SelectDevice(int index)
    {
        _selectedGpuIndex = index;

        // Auto mode (-1): Select discrete GPU if present, else first GPU
        int targetIndex = index;
        if (targetIndex < 0 || targetIndex >= _devices.Count)
        {
            int discreteIdx = _devices.FindIndex(d => d.IsDiscrete);
            targetIndex = discreteIdx >= 0 ? discreteIdx : 0;
        }

        var activeDev = _devices[targetIndex];

        // Match LHM hardware
        _lhmSelectedGpu = null;
        try
        {
            foreach (var hw in _computer.Hardware)
            {
                if (hw.Name.Contains(activeDev.Name, StringComparison.OrdinalIgnoreCase) ||
                    activeDev.Name.Contains(hw.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _lhmSelectedGpu = hw;
                    break;
                }
            }
        }
        catch
        {
            // Ignore
        }
    }

    public (double usage, double? tempC, string name) QueryMetrics()
    {
        int targetIndex = _selectedGpuIndex;
        if (targetIndex < 0 || targetIndex >= _devices.Count)
        {
            int discreteIdx = _devices.FindIndex(d => d.IsDiscrete);
            targetIndex = discreteIdx >= 0 ? discreteIdx : 0;
        }

        if (targetIndex >= _devices.Count)
        {
            return (0.0, null, "GPU");
        }

        var dev = _devices[targetIndex];
        double usage = 0.0;
        double? tempC = null;

        // Try NVML first if NVIDIA
        if (_nvmlInitialized && dev.Vendor == "NVIDIA" && _nvmlDeviceHandle != IntPtr.Zero)
        {
            try
            {
                if (NvmlInterop.nvmlDeviceGetUtilizationRates(_nvmlDeviceHandle, out var util) == NvmlInterop.NVML_SUCCESS)
                {
                    usage = util.gpu;
                }

                if (NvmlInterop.nvmlDeviceGetTemperature(_nvmlDeviceHandle, NvmlInterop.NVML_TEMPERATURE_GPU, out uint t) == NvmlInterop.NVML_SUCCESS)
                {
                    // Filter out 0 or unreasonable temperature readings
                    if (t > 0 && t < 125)
                    {
                        tempC = t;
                    }
                }

                return (Math.Clamp(usage, 0.0, 100.0), tempC, dev.Name);
            }
            catch
            {
                // Fallback to LHM below
            }
        }

        // LibreHardwareMonitor query
        if (_lhmSelectedGpu != null)
        {
            try
            {
                _lhmSelectedGpu.Update();
                foreach (var sensor in _lhmSelectedGpu.Sensors)
                {
                    if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                    {
                        if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                            sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) ||
                            sensor.Name.Contains("D3D 3D", StringComparison.OrdinalIgnoreCase))
                        {
                            usage = Math.Max(usage, sensor.Value.Value);
                        }
                    }
                    else if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue && tempC == null)
                    {
                        if (sensor.Value.Value > 0 && sensor.Value.Value < 125)
                        {
                            tempC = sensor.Value.Value;
                        }
                    }
                }
            }
            catch
            {
                // Graceful fallback
            }
        }

        return (Math.Clamp(usage, 0.0, 100.0), tempC, dev.Name);
    }

    public void Dispose()
    {
        if (_nvmlInitialized)
        {
            try
            {
                NvmlInterop.nvmlShutdown();
            }
            catch
            {
                // Ignore
            }
            _nvmlInitialized = false;
        }

        try
        {
            _computer.Close();
        }
        catch
        {
            // Ignore
        }
    }
}
