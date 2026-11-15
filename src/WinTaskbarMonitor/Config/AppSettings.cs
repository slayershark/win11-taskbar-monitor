using System;
using System.Text.Json.Serialization;

namespace WinTaskbarMonitor.Config;

public enum TemperatureUnit
{
    Celsius,
    Fahrenheit
}

public enum AppThemeMode
{
    System,
    Dark,
    Light
}

public enum TaskbarAlignment
{
    LeftEdge,
    LeftOfTray,
    RightEdge,
    Custom
}

public sealed class AppSettings
{
    public int RefreshIntervalMs { get; set; } = 1000;

    public bool ShowCpu { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowCpuTemp { get; set; } = false;
    public bool ShowGpuTemp { get; set; } = true;
    public bool ShowRam { get; set; } = true;

    /// <summary>
    /// -1 represents Auto (Discrete preferred), 0..N represents specific GPU adapter index.
    /// </summary>
    public int SelectedGpuIndex { get; set; } = -1;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TaskbarAlignment Alignment { get; set; } = TaskbarAlignment.LeftEdge;

    public bool StartWithWindows { get; set; } = false;

    public int OffsetX { get; set; } = 0;
    public int OffsetY { get; set; } = 0;

    public double FontSize { get; set; } = 11.5;

    public bool CompactMode { get; set; } = false;

    public AppSettings Clone()
    {
        return (AppSettings)MemberwiseClone();
    }
}
