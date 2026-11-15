using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WinTaskbarMonitor.Config;
using WinTaskbarMonitor.Monitoring;

namespace WinTaskbarMonitor.UI;

public sealed class MainWidgetViewModel : INotifyPropertyChanged
{
    private readonly ISettingsService _settingsService;
    private readonly HardwareMonitorCoordinator _coordinator;
    private readonly ThemeManager _themeManager;

    public ThemeManager Theme => _themeManager;

    private string _cpuValue = "0%";
    private string _cpuTooltip = "CPU: Loading...";
    private Brush _cpuBrush;

    private string _gpuValue = "0%";
    private string _gpuTooltip = "GPU: Loading...";
    private Brush _gpuBrush;

    private string _cpuTempValue = "N/A";
    private string _cpuTempTooltip = "CPU Temperature: N/A";
    private Brush _cpuTempBrush;

    private string _gpuTempValue = "N/A";
    private string _gpuTempTooltip = "GPU Temperature: N/A";
    private Brush _gpuTempBrush;

    private string _ramValue = "0%";
    private string _ramTooltip = "RAM: Loading...";
    private Brush _ramBrush;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CpuValue { get => _cpuValue; private set => SetField(ref _cpuValue, value); }
    public string CpuTooltip { get => _cpuTooltip; private set => SetField(ref _cpuTooltip, value); }
    public Brush CpuBrush { get => _cpuBrush; private set => SetField(ref _cpuBrush, value); }

    public string GpuValue { get => _gpuValue; private set => SetField(ref _gpuValue, value); }
    public string GpuTooltip { get => _gpuTooltip; private set => SetField(ref _gpuTooltip, value); }
    public Brush GpuBrush { get => _gpuBrush; private set => SetField(ref _gpuBrush, value); }

    public string CpuTempValue { get => _cpuTempValue; private set => SetField(ref _cpuTempValue, value); }
    public string CpuTempTooltip { get => _cpuTempTooltip; private set => SetField(ref _cpuTempTooltip, value); }
    public Brush CpuTempBrush { get => _cpuTempBrush; private set => SetField(ref _cpuTempBrush, value); }

    public string GpuTempValue { get => _gpuTempValue; private set => SetField(ref _gpuTempValue, value); }
    public string GpuTempTooltip { get => _gpuTempTooltip; private set => SetField(ref _gpuTempTooltip, value); }
    public Brush GpuTempBrush { get => _gpuTempBrush; private set => SetField(ref _gpuTempBrush, value); }

    public string RamValue { get => _ramValue; private set => SetField(ref _ramValue, value); }
    public string RamTooltip { get => _ramTooltip; private set => SetField(ref _ramTooltip, value); }
    public Brush RamBrush { get => _ramBrush; private set => SetField(ref _ramBrush, value); }

    private bool _hasValidCpuTemp = false;

    // Visibility toggles
    public bool ShowCpu => _settingsService.Current.ShowCpu;
    public bool ShowGpu => _settingsService.Current.ShowGpu;
    public bool ShowCpuTemp => _settingsService.Current.ShowCpuTemp && _hasValidCpuTemp;
    public bool ShowGpuTemp => _settingsService.Current.ShowGpuTemp;
    public bool ShowRam => _settingsService.Current.ShowRam;

    public double FontSize => _settingsService.Current.FontSize;
    public bool CompactMode => _settingsService.Current.CompactMode;

    public Action? RequestOpenSettings { get; set; }

    public ICommand OpenSettingsCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand SetRefreshIntervalCommand { get; }
    public ICommand SetTemperatureUnitCommand { get; }
    public ICommand SetThemeCommand { get; }

    public MainWidgetViewModel(
        ISettingsService settingsService,
        HardwareMonitorCoordinator coordinator,
        ThemeManager themeManager)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));

        _cpuBrush = _themeManager.TextPrimaryBrush;
        _gpuBrush = _themeManager.TextPrimaryBrush;
        _cpuTempBrush = _themeManager.TextPrimaryBrush;
        _gpuTempBrush = _themeManager.TextPrimaryBrush;
        _ramBrush = _themeManager.TextPrimaryBrush;

        _coordinator.SnapshotUpdated += OnSnapshotUpdated;
        _settingsService.SettingsChanged += OnSettingsChanged;
        _themeManager.PropertyChanged += OnThemeChanged;

        OpenSettingsCommand = new RelayCommand(_ => RequestOpenSettings?.Invoke());
        ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

        SetRefreshIntervalCommand = new RelayCommand(p =>
        {
            if (p is string s && int.TryParse(s, out int interval))
            {
                _settingsService.Current.RefreshIntervalMs = interval;
                _coordinator.SetInterval(interval);
                _settingsService.Save();
            }
        });

        SetTemperatureUnitCommand = new RelayCommand(p =>
        {
            if (p is string u && Enum.TryParse<TemperatureUnit>(u, out var unit))
            {
                _settingsService.Current.TemperatureUnit = unit;
                _settingsService.Save();
            }
        });

        SetThemeCommand = new RelayCommand(p =>
        {
            if (p is string t && Enum.TryParse<AppThemeMode>(t, out var mode))
            {
                _settingsService.Current.ThemeMode = mode;
                _settingsService.Save();
            }
        });
    }

    private void OnThemeChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateBrushes();
    }

    private void OnSettingsChanged(object? sender, AppSettings e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            OnPropertyChanged(nameof(ShowCpu));
            OnPropertyChanged(nameof(ShowGpu));
            OnPropertyChanged(nameof(ShowCpuTemp));
            OnPropertyChanged(nameof(ShowGpuTemp));
            OnPropertyChanged(nameof(ShowRam));
            OnPropertyChanged(nameof(FontSize));
            OnPropertyChanged(nameof(CompactMode));

            if (_coordinator.LatestSnapshot != null)
            {
                ApplySnapshot(_coordinator.LatestSnapshot);
            }
        });
    }

    private void OnSnapshotUpdated(object? sender, HardwareSnapshot snapshot)
    {
        Application.Current.Dispatcher.InvokeAsync(() => ApplySnapshot(snapshot));
    }

    private void ApplySnapshot(HardwareSnapshot snap)
    {
        var settings = _settingsService.Current;
        bool isFahrenheit = settings.TemperatureUnit == TemperatureUnit.Fahrenheit;

        // CPU
        CpuValue = $"{Math.Round(snap.CpuUsage)}%";
        CpuTooltip = $"{snap.CpuName}\nUsage: {snap.CpuUsage:F1}%\nStatus: {(snap.CpuUsage >= 90 ? "Critical" : snap.CpuUsage >= 75 ? "Elevated" : "Normal")}";
        CpuBrush = GetStatusBrush(snap.CpuUsage, 75, 90);

        // GPU
        GpuValue = $"{Math.Round(snap.GpuUsage)}%";
        GpuTooltip = $"{snap.GpuName}\nUsage: {snap.GpuUsage:F1}%\nStatus: {(snap.GpuUsage >= 90 ? "Critical" : snap.GpuUsage >= 75 ? "Elevated" : "Normal")}";
        GpuBrush = GetStatusBrush(snap.GpuUsage, 75, 90);

        // CPU Temp (Smart Auto-Detection)
        bool hasValidCpu = snap.CpuTempC.HasValue && snap.CpuTempC.Value > 15 && snap.CpuTempC.Value < 115;
        if (_hasValidCpuTemp != hasValidCpu)
        {
            _hasValidCpuTemp = hasValidCpu;
            OnPropertyChanged(nameof(ShowCpuTemp));
        }

        if (hasValidCpu)
        {
            double temp = isFahrenheit ? (snap.CpuTempC!.Value * 1.8 + 32) : snap.CpuTempC!.Value;
            string unit = isFahrenheit ? "°F" : "°C";
            CpuTempValue = $"{Math.Round(temp)}{unit}";
            CpuTempTooltip = $"CPU Temperature: {temp:F1}{unit}\nThreshold: Warning 75°C, Critical 85°C";
            CpuTempBrush = GetStatusBrush(snap.CpuTempC!.Value, 75, 85);
        }
        else
        {
            CpuTempValue = "N/A";
            CpuTempTooltip = "CPU Temperature: Unavailable in standard user mode without kernel driver";
            CpuTempBrush = _themeManager.TextSubtleBrush;
        }

        // GPU Temp
        if (snap.GpuTempC.HasValue)
        {
            double temp = isFahrenheit ? (snap.GpuTempC.Value * 1.8 + 32) : snap.GpuTempC.Value;
            string unit = isFahrenheit ? "°F" : "°C";
            GpuTempValue = $"{Math.Round(temp)}{unit}";
            GpuTempTooltip = $"GPU Temperature ({snap.GpuName}): {temp:F1}{unit}\nThreshold: Warning 75°C, Critical 85°C";
            GpuTempBrush = GetStatusBrush(snap.GpuTempC.Value, 75, 85);
        }
        else
        {
            GpuTempValue = "N/A";
            GpuTempTooltip = "GPU Temperature: Inactive or sensor unavailable";
            GpuTempBrush = _themeManager.TextSubtleBrush;
        }

        // RAM
        if (settings.CompactMode)
        {
            RamValue = $"{Math.Round(snap.RamUsagePercent)}%";
        }
        else
        {
            RamValue = $"{snap.RamUsedGb:F1}/{snap.RamTotalGb:F0}G · {Math.Round(snap.RamUsagePercent)}%";
        }

        double freeGb = Math.Max(0, snap.RamTotalGb - snap.RamUsedGb);
        RamTooltip = $"Physical Memory:\nUsed: {snap.RamUsedGb:F2} GB ({snap.RamUsagePercent:F1}%)\nFree: {freeGb:F2} GB\nTotal: {snap.RamTotalGb:F2} GB";
        RamBrush = GetStatusBrush(snap.RamUsagePercent, 75, 90);
    }

    private void UpdateBrushes()
    {
        if (_coordinator.LatestSnapshot != null)
        {
            ApplySnapshot(_coordinator.LatestSnapshot);
        }
        else
        {
            CpuBrush = _themeManager.TextPrimaryBrush;
            GpuBrush = _themeManager.TextPrimaryBrush;
            CpuTempBrush = _themeManager.TextSubtleBrush;
            GpuTempBrush = _themeManager.TextSubtleBrush;
            RamBrush = _themeManager.TextPrimaryBrush;
        }
    }

    private Brush GetStatusBrush(double val, double warnThreshold, double critThreshold)
    {
        if (val >= critThreshold) return _themeManager.AccentCriticalBrush;
        if (val >= warnThreshold) return _themeManager.AccentWarningBrush;
        return _themeManager.TextPrimaryBrush;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propName = null)
    {
        if (!object.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propName);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
}
