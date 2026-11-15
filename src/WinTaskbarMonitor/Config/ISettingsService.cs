using System;

namespace WinTaskbarMonitor.Config;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
    void Load();
    void ResetToDefaults();
    void ApplyStartWithWindows(bool enable);
    event EventHandler<AppSettings>? SettingsChanged;
}
