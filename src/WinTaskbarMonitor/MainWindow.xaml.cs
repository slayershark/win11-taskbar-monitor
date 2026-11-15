using System;
using System.Windows;
using System.Windows.Input;
using WinTaskbarMonitor.Config;
using WinTaskbarMonitor.Monitoring;
using WinTaskbarMonitor.Taskbar;
using WinTaskbarMonitor.UI;

namespace WinTaskbarMonitor;

public partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly HardwareMonitorCoordinator _coordinator;
    private readonly ThemeManager _themeManager;
    private readonly TaskbarAnchorEngine _anchorEngine;
    private readonly MainWidgetViewModel _viewModel;

    private SettingsWindow? _settingsWindow;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService = new SettingsService();
        _themeManager = new ThemeManager(_settingsService);
        _coordinator = new HardwareMonitorCoordinator(
            _settingsService.Current.RefreshIntervalMs,
            _settingsService.Current.SelectedGpuIndex);

        _viewModel = new MainWidgetViewModel(_settingsService, _coordinator, _themeManager);
        _viewModel.RequestOpenSettings = OpenSettings;
        DataContext = _viewModel;

        _anchorEngine = new TaskbarAnchorEngine(this, _settingsService);

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _coordinator.Start();
        _anchorEngine.Reposition();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _coordinator.Dispose();
        _anchorEngine.Dispose();
    }

    private void OpenSettings()
    {
        if (_settingsWindow != null && _settingsWindow.IsVisible)
        {
            _settingsWindow.Activate();
            return;
        }

        var settingsVm = new SettingsViewModel(_settingsService, _coordinator);
        _settingsWindow = new SettingsWindow(settingsVm, _themeManager);
        settingsVm.RequestClose = () =>
        {
            _settingsWindow.Close();
            _anchorEngine.Reposition();
        };

        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        // If Custom alignment is enabled, allow dragging to place anywhere on taskbar
        if (_settingsService.Current.Alignment == TaskbarAlignment.Custom && e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
            _settingsService.Current.OffsetX = (int)Left;
            _settingsService.Current.OffsetY = (int)Top;
            _settingsService.Save();
        }
    }
}
