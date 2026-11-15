using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WinTaskbarMonitor.Monitoring;

public static class NvmlInterop
{
    private const string NvmlDll = "nvml.dll";

    public const int NVML_SUCCESS = 0;
    public const int NVML_TEMPERATURE_GPU = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct NvmlUtilization
    {
        public uint gpu;
        public uint memory;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NvmlMemory
    {
        public ulong total;
        public ulong free;
        public ulong used;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport(NvmlDll, EntryPoint = "nvmlInit_v2")]
    public static extern int nvmlInit_v2();

    [DllImport(NvmlDll, EntryPoint = "nvmlShutdown")]
    public static extern int nvmlShutdown();

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetCount_v2")]
    public static extern int nvmlDeviceGetCount_v2(out uint deviceCount);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
    public static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetName")]
    public static extern int nvmlDeviceGetName(IntPtr device, byte[] name, uint length);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetUtilizationRates")]
    public static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization utilization);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetTemperature")]
    public static extern int nvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temp);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetMemoryInfo")]
    public static extern int nvmlDeviceGetMemoryInfo(IntPtr device, out NvmlMemory memory);

    public static bool IsNvmlAvailable()
    {
        try
        {
            var hModule = LoadLibrary(NvmlDll);
            return hModule != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }
}
