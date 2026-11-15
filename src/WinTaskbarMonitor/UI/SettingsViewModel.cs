using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WinTaskbarMonitor.Config;
using WinTaskbarMonitor.Monitoring;

namespace WinTaskbarMonitor.UI;

public sealed class GpuDisplayItem
{
    public int Index { get; }
    public string DisplayName { get; }

    public GpuDisplayItem(int index, string displayName)
    {
        Index = index;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ISettingsService _settingsService;
    private readonly HardwareMonitorCoordinator _coordinator;

    public event PropertyChangedEventHandler? PropertyChanged;
    public Action? RequestClose { get; set; }

    public ObservableCollection<int> RefreshIntervals { get; } = new() { 500, 1000, 2000, 5000 };
    public ObservableCollection<GpuDisplayItem> GpuItems { get; } = new();
    public ObservableCollection<TemperatureUnit> TemperatureUnits { get; } = new() { TemperatureUnit.Celsius, TemperatureUnit.Fahrenheit };
    public ObservableCollection<AppThemeMode> ThemeModes { get; } = new() { AppThemeMode.System, AppThemeMode.Dark, AppThemeMode.Light };
    public ObservableCollection<TaskbarAlignment> Alignments { get; } = new() { TaskbarAlignment.LeftOfTray, TaskbarAlignment.RightEdge };

    private int _refreshIntervalMs;
    private bool _showCpu;
    private bool _showGpu;
    private bool _showCpuTemp;
    private bool _showGpuTemp;
    private bool _showRam;
    private int _selectedGpuIndex;
    private TemperatureUnit _temperatureUnit;
    private AppThemeMode _themeMode;
    private TaskbarAlignment _alignment;
    private bool _startWithWindows;
    private int _offsetX;
    private int _offsetY;
    private double _fontSize;
    private bool _compactMode;

    public int RefreshIntervalMs { get => _refreshIntervalMs; set => SetField(ref _refreshIntervalMs, value); }
    public bool ShowCpu { get => _showCpu; set => SetField(ref _showCpu, value); }
    public bool ShowGpu { get => _showGpu; set => SetField(ref _showGpu, value); }
    public bool ShowCpuTemp { get => _showCpuTemp; set => SetField(ref _showCpuTemp, value); }
    public bool ShowGpuTemp { get => _showGpuTemp; set => SetField(ref _showGpuTemp, value); }
    public bool ShowRam { get => _showRam; set => SetField(ref _showRam, value); }
    public int SelectedGpuIndex { get => _selectedGpuIndex; set => SetField(ref _selectedGpuIndex, value); }
    public TemperatureUnit SelectedTemperatureUnit { get => _temperatureUnit; set => SetField(ref _temperatureUnit, value); }
    public AppThemeMode SelectedThemeMode { get => _themeMode; set => SetField(ref _themeMode, value); }
    public TaskbarAlignment SelectedAlignment { get => _alignment; set => SetField(ref _alignment, value); }
    public bool StartWithWindows { get => _startWithWindows; set => SetField(ref _startWithWindows, value); }
    public int OffsetX { get => _offsetX; set => SetField(ref _offsetX, value); }
    public int OffsetY { get => _offsetY; set => SetField(ref _offsetY, value); }
    public double FontSize { get => _fontSize; set => SetField(ref _fontSize, value); }
    public bool CompactMode { get => _compactMode; set => SetField(ref _compactMode, value); }

    public ICommand SaveCommand { get; }
    public ICommand ResetDefaultsCommand { get; }
    public ICommand CloseCommand { get; }

    public SettingsViewModel(ISettingsService settingsService, HardwareMonitorCoordinator coordinator)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

        PopulateGpus();
        LoadCurrentValues();

        SaveCommand = new RelayCommand(_ => SaveSettings());
        ResetDefaultsCommand = new RelayCommand(_ => ResetDefaults());
        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
    }

    private void PopulateGpus()
    {
        GpuItems.Clear();
        GpuItems.Add(new GpuDisplayItem(-1, "Auto (Discrete preferred)"));
        foreach (var dev in _coordinator.DetectedGpus)
        {
            string desc = dev.IsDiscrete ? $"{dev.Name} ({dev.Vendor} dGPU)" : $"{dev.Name} ({dev.Vendor} iGPU)";
            GpuItems.Add(new GpuDisplayItem(dev.Index, desc));
        }
    }

    private void LoadCurrentValues()
    {
        var s = _settingsService.Current;
        _refreshIntervalMs = s.RefreshIntervalMs;
        _showCpu = s.ShowCpu;
        _showGpu = s.ShowGpu;
        _showCpuTemp = s.ShowCpuTemp;
        _showGpuTemp = s.ShowGpuTemp;
        _showRam = s.ShowRam;
        _selectedGpuIndex = s.SelectedGpuIndex;
        _temperatureUnit = s.TemperatureUnit;
        _themeMode = s.ThemeMode;
        _alignment = s.Alignment;
        _startWithWindows = s.StartWithWindows;
        _offsetX = s.OffsetX;
        _offsetY = s.OffsetY;
        _fontSize = s.FontSize;
        _compactMode = s.CompactMode;
    }

    private void SaveSettings()
    {
        var s = _settingsService.Current;
        s.RefreshIntervalMs = RefreshIntervalMs;
        s.ShowCpu = ShowCpu;
        s.ShowGpu = ShowGpu;
        s.ShowCpuTemp = ShowCpuTemp;
        s.ShowGpuTemp = ShowGpuTemp;
        s.ShowRam = ShowRam;
        s.SelectedGpuIndex = SelectedGpuIndex;
        s.TemperatureUnit = SelectedTemperatureUnit;
        s.ThemeMode = SelectedThemeMode;
        s.Alignment = SelectedAlignment;
        s.OffsetX = OffsetX;
        s.OffsetY = OffsetY;
        s.FontSize = FontSize;
        s.CompactMode = CompactMode;

        _coordinator.SetInterval(RefreshIntervalMs);
        _coordinator.SelectGpu(SelectedGpuIndex);

        if (s.StartWithWindows != StartWithWindows)
        {
            _settingsService.ApplyStartWithWindows(StartWithWindows);
        }
        else
        {
            _settingsService.Save();
        }

        RequestClose?.Invoke();
    }

    private void ResetDefaults()
    {
        _settingsService.ResetToDefaults();
        LoadCurrentValues();
        NotifyAllProperties();
    }

    private void NotifyAllProperties()
    {
        OnPropertyChanged(nameof(RefreshIntervalMs));
        OnPropertyChanged(nameof(ShowCpu));
        OnPropertyChanged(nameof(ShowGpu));
        OnPropertyChanged(nameof(ShowCpuTemp));
        OnPropertyChanged(nameof(ShowGpuTemp));
        OnPropertyChanged(nameof(ShowRam));
        OnPropertyChanged(nameof(SelectedGpuIndex));
        OnPropertyChanged(nameof(SelectedTemperatureUnit));
        OnPropertyChanged(nameof(SelectedThemeMode));
        OnPropertyChanged(nameof(SelectedAlignment));
        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(OffsetX));
        OnPropertyChanged(nameof(OffsetY));
        OnPropertyChanged(nameof(FontSize));
        OnPropertyChanged(nameof(CompactMode));
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
