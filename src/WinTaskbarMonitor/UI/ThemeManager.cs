using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Microsoft.Win32;
using WinTaskbarMonitor.Config;

namespace WinTaskbarMonitor.UI;

public sealed class ThemeManager : INotifyPropertyChanged
{
    private readonly ISettingsService _settingsService;
    private bool _isWindowsDarkTheme = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsDarkTheme
    {
        get
        {
            var mode = _settingsService.Current.ThemeMode;
            return mode switch
            {
                AppThemeMode.Dark => true,
                AppThemeMode.Light => false,
                _ => _isWindowsDarkTheme
            };
        }
    }

    public Brush BackgroundBrush => IsDarkTheme
        ? new SolidColorBrush(Color.FromArgb(215, 32, 32, 32))
        : new SolidColorBrush(Color.FromArgb(225, 243, 243, 243));

    public Brush BorderBrush => IsDarkTheme
        ? new SolidColorBrush(Color.FromArgb(70, 255, 255, 255))
        : new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));

    public Brush TextPrimaryBrush => IsDarkTheme
        ? new SolidColorBrush(Color.FromRgb(245, 245, 245))
        : new SolidColorBrush(Color.FromRgb(25, 25, 25));

    public Brush TextSubtleBrush => IsDarkTheme
        ? new SolidColorBrush(Color.FromRgb(156, 163, 175))
        : new SolidColorBrush(Color.FromRgb(107, 114, 128));

    public Brush SeparatorBrush => IsDarkTheme
        ? new SolidColorBrush(Color.FromArgb(50, 255, 255, 255))
        : new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));

    public Brush AccentWarningBrush => new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
    public Brush AccentCriticalBrush => new SolidColorBrush(Color.FromRgb(239, 68, 68));  // Coral Red

    public ThemeManager(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        _isWindowsDarkTheme = QueryWindowsDarkTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            bool dark = QueryWindowsDarkTheme();
            if (_isWindowsDarkTheme != dark)
            {
                _isWindowsDarkTheme = dark;
                NotifyThemeProperties();
            }
        }
    }

    private void OnSettingsChanged(object? sender, AppSettings e)
    {
        NotifyThemeProperties();
    }

    private static bool QueryWindowsDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int val)
            {
                return val == 0; // 0 = Dark, 1 = Light
            }
        }
        catch
        {
            // Default to dark
        }
        return true;
    }

    private void NotifyThemeProperties()
    {
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(BackgroundBrush));
        OnPropertyChanged(nameof(BorderBrush));
        OnPropertyChanged(nameof(TextPrimaryBrush));
        OnPropertyChanged(nameof(TextSubtleBrush));
        OnPropertyChanged(nameof(SeparatorBrush));
        OnPropertyChanged(nameof(AccentWarningBrush));
        OnPropertyChanged(nameof(AccentCriticalBrush));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
