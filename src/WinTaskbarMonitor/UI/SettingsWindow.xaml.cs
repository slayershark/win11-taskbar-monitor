using System;
using System.Windows;
using System.Windows.Media;

namespace WinTaskbarMonitor.UI;

public partial class SettingsWindow : Window
{
    private readonly ThemeManager _themeManager;

    public SettingsWindow(SettingsViewModel viewModel, ThemeManager themeManager)
    {
        InitializeComponent();
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        DataContext = viewModel;

        ApplyTheme();
        _themeManager.PropertyChanged += (_, _) => ApplyTheme();
    }

    private void ApplyTheme()
    {
        bool isDark = _themeManager.IsDarkTheme;
        Background = isDark ? new SolidColorBrush(Color.FromRgb(32, 32, 32)) : new SolidColorBrush(Color.FromRgb(248, 248, 248));
        Foreground = isDark ? new SolidColorBrush(Color.FromRgb(240, 240, 240)) : new SolidColorBrush(Color.FromRgb(25, 25, 25));
    }
}
