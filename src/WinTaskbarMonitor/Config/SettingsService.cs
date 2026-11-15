using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace WinTaskbarMonitor.Config;

public sealed class SettingsService : ISettingsService
{
    private const string AppName = "WinTaskbarMonitor";
    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _settingsFilePath;
    private readonly object _lock = new();
    private AppSettings _current;

    public event EventHandler<AppSettings>? SettingsChanged;

    public AppSettings Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
        private set
        {
            lock (_lock)
            {
                _current = value;
            }
            SettingsChanged?.Invoke(this, _current);
        }
    }

    public SettingsService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appData, AppName);
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
        _current = new AppSettings();
        Load();
    }

    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loaded != null)
                    {
                        _current = loaded;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[SettingsService] Failed to load settings: {ex.Message}. Using defaults.");
            }

            _current = new AppSettings();
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonSerializer.Serialize(_current, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
            }
        }

        SettingsChanged?.Invoke(this, Current);
    }

    public void ResetToDefaults()
    {
        Current = new AppSettings();
        Save();
    }

    public void ApplyStartWithWindows(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;

            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            if (enable)
            {
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, false);
                }
            }

            Current.StartWithWindows = enable;
            Save();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[SettingsService] Failed to update startup registry: {ex.Message}");
        }
    }
}
