using System.IO;
using System.Text.Json;
using WinTaskbarMonitor.Config;
using Xunit;

namespace WinTaskbarMonitor.Tests;

public class SettingsTests
{
    [Fact]
    public void AppSettings_DefaultValues_AreSensible()
    {
        var settings = new AppSettings();

        Assert.Equal(1000, settings.RefreshIntervalMs);
        Assert.True(settings.ShowCpu);
        Assert.True(settings.ShowGpu);
        Assert.True(settings.ShowCpuTemp);
        Assert.True(settings.ShowGpuTemp);
        Assert.True(settings.ShowRam);
        Assert.Equal(TemperatureUnit.Celsius, settings.TemperatureUnit);
        Assert.Equal(AppThemeMode.System, settings.ThemeMode);
        Assert.Equal(TaskbarAlignment.LeftOfTray, settings.Alignment);
    }

    [Fact]
    public void AppSettings_SerializationAndDeserialization_Matches()
    {
        var original = new AppSettings
        {
            RefreshIntervalMs = 2000,
            ShowGpuTemp = false,
            TemperatureUnit = TemperatureUnit.Fahrenheit,
            ThemeMode = AppThemeMode.Dark,
            Alignment = TaskbarAlignment.RightEdge,
            FontSize = 13.0
        };

        string json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(2000, deserialized!.RefreshIntervalMs);
        Assert.False(deserialized.ShowGpuTemp);
        Assert.Equal(TemperatureUnit.Fahrenheit, deserialized.TemperatureUnit);
        Assert.Equal(AppThemeMode.Dark, deserialized.ThemeMode);
        Assert.Equal(TaskbarAlignment.RightEdge, deserialized.Alignment);
        Assert.Equal(13.0, deserialized.FontSize);
    }

    [Fact]
    public void AppSettings_Clone_CreatesDeepCopy()
    {
        var original = new AppSettings
        {
            RefreshIntervalMs = 500,
            ShowCpu = false
        };

        var clone = original.Clone();
        clone.RefreshIntervalMs = 5000;

        Assert.Equal(500, original.RefreshIntervalMs);
        Assert.Equal(5000, clone.RefreshIntervalMs);
    }
}
