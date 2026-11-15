using System;
using System.Diagnostics;
using System.Management;
using LibreHardwareMonitor.Hardware;

namespace WinTaskbarMonitor.Monitoring;

public sealed class CpuTemperatureProvider : IDisposable
{
    private readonly Computer _computer;
    private IHardware? _cpuHardware;
    private bool _lhmInitialized;
    private bool _wmiTested;
    private bool _wmiAvailable;

    public CpuTemperatureProvider()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = false
        };

        try
        {
            _computer.Open();
            _lhmInitialized = true;
            foreach (var hw in _computer.Hardware)
            {
                if (hw.HardwareType == HardwareType.Cpu)
                {
                    _cpuHardware = hw;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[CpuTemperatureProvider] LHM CPU init failed: {ex.Message}");
        }
    }

    public double? QueryTemperatureC()
    {
        // 1. Try LibreHardwareMonitor
        if (_lhmInitialized && _cpuHardware != null)
        {
            try
            {
                _cpuHardware.Update();
                double? bestCandidate = null;

                foreach (var sensor in _cpuHardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                    {
                        float val = sensor.Value.Value;
                        if (val > 15 && val < 115)
                        {
                            // Prioritize "CPU Package"
                            if (sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                            {
                                return val;
                            }
                            bestCandidate ??= val;
                        }
                    }
                }

                if (bestCandidate.HasValue)
                {
                    return bestCandidate.Value;
                }
            }
            catch
            {
                // Graceful fallback
            }
        }

        // 2. Fallback: WMI MSAcpi_ThermalZoneTemperature
        return QueryWmiAcpiTemperature();
    }

    private double? QueryWmiAcpiTemperature()
    {
        if (_wmiTested && !_wmiAvailable) return null;

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            using var collection = searcher.Get();
            _wmiTested = true;

            foreach (var item in collection)
            {
                if (item["CurrentTemperature"] is uint rawKelvinTenths && rawKelvinTenths > 0)
                {
                    double celsius = (rawKelvinTenths / 10.0) - 273.15;
                    if (celsius > 15 && celsius < 115)
                    {
                        _wmiAvailable = true;
                        return Math.Round(celsius, 1);
                    }
                }
            }
        }
        catch
        {
            _wmiTested = true;
            _wmiAvailable = false;
        }

        return null;
    }

    public void Dispose()
    {
        if (_lhmInitialized)
        {
            try
            {
                _computer.Close();
            }
            catch
            {
                // Ignore
            }
            _lhmInitialized = false;
        }
    }
}
