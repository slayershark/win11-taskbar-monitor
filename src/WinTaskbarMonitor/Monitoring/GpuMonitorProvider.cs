using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using LibreHardwareMonitor.Hardware;

namespace WinTaskbarMonitor.Monitoring;

public sealed class GpuMonitorProvider : IDisposable
{
    // PDH APIs for Windows Task Manager GPU engine metrics
    [DllImport("pdh.dll", EntryPoint = "PdhOpenQueryW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int PdhOpenQuery(string? szDataSource, IntPtr dwUserData, out IntPtr phQuery);

    [DllImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int PdhAddEnglishCounter(IntPtr hQuery, string szFullCounterPath, IntPtr dwUserData, out IntPtr phCounter);

    [DllImport("pdh.dll", SetLastError = true)]
    private static extern int PdhCollectQueryData(IntPtr hQuery);

    [DllImport("pdh.dll", EntryPoint = "PdhGetFormattedCounterArrayW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int PdhGetFormattedCounterArray(IntPtr hCounter, uint dwFormat, ref uint lpdwBufferSize, ref uint lpdwItemCount, IntPtr ItemBuffer);

    [DllImport("pdh.dll", SetLastError = true)]
    private static extern int PdhCloseQuery(IntPtr hQuery);

    private const uint PDH_FMT_DOUBLE = 0x00000200;

    private IntPtr _hPdhQuery = IntPtr.Zero;
    private IntPtr _hGpuCounter = IntPtr.Zero;
    private bool _pdhGpuInitialized;

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
        // 1. Try Initialize PDH GPU Engine (matches Task Manager across all GPUs)
        try
        {
            if (PdhOpenQuery(null, IntPtr.Zero, out _hPdhQuery) == 0)
            {
                if (PdhAddEnglishCounter(_hPdhQuery, @"\GPU Engine(*)\Utilization Percentage", IntPtr.Zero, out _hGpuCounter) == 0)
                {
                    PdhCollectQueryData(_hPdhQuery);
                    _pdhGpuInitialized = true;
                }
            }
        }
        catch
        {
            _pdhGpuInitialized = false;
        }

        // 2. Try Initialize NVML
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

        // 3. Open LibreHardwareMonitor for Intel/AMD/Other GPUs
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

        // 1. Direct NVML for NVIDIA GPUs (100% Task Manager GPU 1 parity)
        if (dev.Vendor == "NVIDIA" && _nvmlInitialized && _nvmlDeviceHandle != IntPtr.Zero)
        {
            try
            {
                if (NvmlInterop.nvmlDeviceGetUtilizationRates(_nvmlDeviceHandle, out var util) == NvmlInterop.NVML_SUCCESS)
                {
                    usage = (double)util.gpu;
                }

                if (NvmlInterop.nvmlDeviceGetTemperature(_nvmlDeviceHandle, NvmlInterop.NVML_TEMPERATURE_GPU, out uint t) == NvmlInterop.NVML_SUCCESS)
                {
                    if (t > 0 && t < 125)
                    {
                        tempC = t;
                    }
                }

                return (Math.Clamp(usage, 0.0, 100.0), tempC, dev.Name);
            }
            catch
            {
                // Fallback to PDH below
            }
        }

        // 2. Query PDH for system-wide / iGPU Task Manager GPU utilization
        if (_pdhGpuInitialized && _hPdhQuery != IntPtr.Zero && _hGpuCounter != IntPtr.Zero)
        {
            try
            {
                if (PdhCollectQueryData(_hPdhQuery) == 0)
                {
                    uint bufSize = 0, count = 0;
                    PdhGetFormattedCounterArray(_hGpuCounter, PDH_FMT_DOUBLE, ref bufSize, ref count, IntPtr.Zero);
                    if (bufSize > 0)
                    {
                        IntPtr pBuf = Marshal.AllocHGlobal((int)bufSize);
                        try
                        {
                            if (PdhGetFormattedCounterArray(_hGpuCounter, PDH_FMT_DOUBLE, ref bufSize, ref count, pBuf) == 0)
                            {
                                double max3d = 0.0;
                                double maxAny = 0.0;
                                for (int idx = 0; idx < count; idx++)
                                {
                                    long baseOff = pBuf.ToInt64() + idx * 24;
                                    uint status = (uint)Marshal.ReadInt32(new IntPtr(baseOff + 8));
                                    if (status == 0)
                                    {
                                        double val = BitConverter.ToDouble(BitConverter.GetBytes(Marshal.ReadInt64(new IntPtr(baseOff + 16))), 0);
                                        if (!double.IsNaN(val) && !double.IsInfinity(val) && val > 0.01)
                                        {
                                            IntPtr szName = Marshal.ReadIntPtr(new IntPtr(baseOff), 0);
                                            string? name = Marshal.PtrToStringUni(szName);
                                            if (name != null)
                                            {
                                                if (val > maxAny) maxAny = val;
                                                if (name.Contains("engtype_3D") && val > max3d) max3d = val;
                                            }
                                        }
                                    }
                                }
                                usage = max3d > 0 ? max3d : maxAny;
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pBuf);
                        }
                    }
                }
            }
            catch
            {
                // Fallback below
            }
        }

        // 3. LibreHardwareMonitor query
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
        if (_hPdhQuery != IntPtr.Zero)
        {
            try
            {
                PdhCloseQuery(_hPdhQuery);
            }
            catch { }
            _hPdhQuery = IntPtr.Zero;
            _hGpuCounter = IntPtr.Zero;
            _pdhGpuInitialized = false;
        }

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
