# ARCHITECTURE.md — Technical Architecture & Design Document

## 1. Technology Stack & Justification

| Component | Choice | Justification |
| :--- | :--- | :--- |
| **Language** | C# 12 | Modern, type-safe, rich asynchronous primitives, zero-overhead P/Invoke interop with Win32 APIs. |
| **Runtime** | .NET 8 LTS (WindowsDesktop) | High performance, aggressive JIT optimisations, modern Garbage Collector with low-latency modes, native access to Windows APIs. |
| **UI Framework** | WPF (Windows Presentation Foundation) | Hardware-accelerated DirectX rendering, rich vector graphics, native support for Segoe UI Variable and OpenType tabular numbers, seamless Win32 window interop (`HwndSource`). |
| **Hardware APIs** | Win32 APIs, PDH, NVML, LibreHardwareMonitorLib | Low-overhead native data retrieval. Win32 `GetSystemTimes` for CPU; `GlobalMemoryStatusEx` for RAM; Windows Performance Counters (`GPU Engine(*)\Utilization Percentage`) & NVML for GPU; LibreHardwareMonitorLib/NVML for hardware temperatures. |
| **Persistence** | System.Text.Json | Built-in, high-performance, source-generated or zero-dependency JSON serialization. |

---

## 2. Overall Application Architecture

The application adopts a modular, decoupled architecture adhering to Clean Architecture principles:

```
+-------------------------------------------------------------------------------+
|                             Presentation Layer                                |
|   - TaskbarWidgetView (WPF Layered ToolWindow, WS_EX_NOACTIVATE, Segoe UI Var)|
|   - SettingsWindow (Fluent Design, Theme-aware, Tabbed configuration)         |
|   - SystemTrayIcon & ContextMenu (Direct access, refresh rates, theme switch) |
+---------------------------------------+---------------------------------------+
                                        | (Data Binding / INotifyPropertyChanged)
+---------------------------------------v---------------------------------------+
|                               View Models                                     |
|   - MainWidgetViewModel (Formatted metrics, threshold alerts, tooltips)       |
|   - SettingsViewModel (Preferences, GPU selector, metric toggles)             |
+---------------------------------------+---------------------------------------+
                                        |
+---------------------------------------v---------------------------------------+
|                              Core Services                                    |
|   - HardwareMonitorCoordinator (Schedules polling, merges provider outputs)   |
|   - TaskbarAnchorEngine (Win32 AppBar hooks, DPI scaling, position sync)      |
|   - SettingsService (Loads/saves %APPDATA%\WinTaskbarMonitor\settings.json)   |
|   - ThemeManager (Watches Windows dark/light mode registry & applies styles)  |
+---------------------------------------+---------------------------------------+
                                        |
+---------------------------------------v---------------------------------------+
|                       Hardware Monitoring Providers                           |
|   - CpuMonitorProvider (GetSystemTimes native calculation)                    |
|   - GpuMonitorProvider (PDH GPU Engine counter + NVML for NVIDIA)             |
|   - MemoryMonitorProvider (GlobalMemoryStatusEx native call)                  |
|   - TemperatureMonitorProvider (NVML GPU temp + LibreHardwareMonitor fallback)|
+-------------------------------------------------------------------------------+
```

---

## 3. Hardware Monitoring & Sensor Collection

### 3.1 CPU Utilisation
- **Mechanism**: Win32 native `GetSystemTimes` (P/Invoke to `kernel32.dll`).
- **Algorithm**:
  $$\text{TotalTime} = (\text{UserTime}_t - \text{UserTime}_{t-1}) + (\text{KernelTime}_t - \text{KernelTime}_{t-1})$$
  $$\text{IdleTime} = \text{IdleTime}_t - \text{IdleTime}_{t-1}$$
  $$\text{Usage\%} = \frac{\text{TotalTime} - \text{IdleTime}}{\text{TotalTime}} \times 100$$
- **Advantages**: Executes in microseconds, consumes ~0% CPU, requires zero elevation or external drivers, immune to per-core skewing.

### 3.2 RAM Utilisation
- **Mechanism**: Win32 native `GlobalMemoryStatusEx` (P/Invoke to `kernel32.dll`).
- **Telemetry Collected**:
  - `ullTotalPhys`: Total installed physical RAM (bytes).
  - `ullAvailPhys`: Currently available physical RAM (bytes).
  - Calculated: `ullTotalPhys - ullAvailPhys` = Used RAM (GB) and percentage.
- **Advantages**: Instantaneous, exact matching with Windows Task Manager "Memory" tab, zero allocation.

### 3.3 GPU Utilisation & Multi-GPU Handling
- **Mechanism 1 (NVIDIA)**: Direct P/Invoke to `nvml.dll` (`nvmlDeviceGetUtilizationRates`). If present on systems with NVIDIA GPUs, provides exact, zero-overhead core utilisation.
- **Mechanism 2 (Universal / AMD / Intel / Fallback)**: Windows Performance Counters via PDH:
  Querying `\GPU Engine(*)\Utilization Percentage` grouped by `luid` (Locally Unique Identifier).
- **Mechanism 3 (Direct3D/DXGI query)**: Fallback query via DXGI adapter performance monitoring.
- **Multi-GPU Logic**:
  Enumerates all active video controllers via Win32 DXGI / WMI `Win32_VideoController`. User can select "Auto (Prioritise Discrete)", "NVIDIA", "Intel iGPU", or "AMD".

### 3.4 Temperature Monitoring
- **GPU Temperature**:
  - NVIDIA: Direct `nvmlDeviceGetTemperature` with `NVML_TEMPERATURE_GPU`.
  - AMD / Intel: LibreHardwareMonitor sensor query.
- **CPU Temperature**:
  - Query hardware sensor nodes via `LibreHardwareMonitorLib` (Core temperatures / Package temperature).
  - Fallback: ACPI thermal zones (`MSAcpi_ThermalZoneTemperature` via WMI) if available.
- **Graceful Sensor Degradation**:
  - If a temperature sensor is protected by kernel ring-0 requirements and running in non-elevated mode, the provider emits `SensorValue.Unavailable()`.
  - The UI displays `N/A` cleanly without showing red alerts or throwing exceptions.

---

## 4. Windows 11 Taskbar Integration Approach

### 4.1 Why Not DeskBand or Explorer Injection?
- Windows 11 completely deprecated and removed the legacy DeskBand API (`ITaskbarList`, `CATID_DeskBand`).
- Injecting DLLs into `explorer.exe` or hooking internal Explorer threads causes instability, anticheat triggers in games, and breaks with every monthly Windows cumulative update.

### 4.2 The Solution: Taskbar-Anchored Floating Tool Window
The widget is implemented as a lightweight WPF `Window` with specific Win32 extended window styles:
- `WS_EX_TOOLWINDOW`: Hidden from the Alt+Tab switcher and Windows taskbar button list.
- `WS_EX_NOACTIVATE`: Clicking on the widget or interacting with it does not steal focus from active windows.
- `WS_EX_TOPMOST`: Remains visible above regular desktop windows.
- Window handles Win32 messages:
  - `WM_DISPLAYCHANGE`: Re-evaluates monitor resolutions.
  - `WM_DPICHANGED`: Scales typography and layout according to the target monitor's DPI.
  - `WM_SETTINGCHANGE`: Reacts to taskbar re-anchoring or shell alterations.

### 4.3 Taskbar Position Tracking
- Uses `SHAppBarMessage(ABM_GETTASKBARPOS, ...)` to locate the Windows 11 taskbar rectangle.
- Tracks `Shell_TrayWnd` and `TrayNotifyWnd` (the notification area / system tray).
- Calculates target coordinates:
  - Default: Positioned immediately to the left of the system tray / notification chevron, or aligned to the right-hand corner of the taskbar.
  - Automatically accounts for display DPI scaling factor via `GetDpiForWindow`.
  - Taskbar auto-hide: Listens to taskbar state (`ABM_GETSTATE`). When the taskbar hides, the anchor adjusts opacity or slides in tandem.
  - Explorer restart recovery: Subscribes to the registered window message `"TaskbarCreated"`. If Explorer crashes and restarts, the widget immediately re-anchors.

---

## 5. UI Rendering & Update Mechanism

- **Rendering Engine**: Hardware-accelerated WPF Direct3D composition pipeline.
- **Typography**: `Segoe UI Variable Text` with `Typography.NumeralAlignment="Tabular"`.
- **Throttled Dispatcher Updates**:
  - Hardware collection runs on a dedicated background thread pool timer (`System.Threading.PeriodicTimer`).
  - Readouts are pre-formatted into lightweight immutable structs (`HardwareMetricsSnapshot`).
  - Only when metric values change does the coordinator marshal the update to the UI thread via `Dispatcher.InvokeAsync(..., DispatcherPriority.Render)`.
- **Visual Stability**: Labels and value text blocks have fixed minimum widths and right/left alignment to prevent jitter as digits transition (e.g., from `9%` to `10%` to `100%`).

---

## 6. Configuration & Persistence

- **Path**: `%APPDATA%\WinTaskbarMonitor\settings.json`
- **Schema**:
  - `RefreshIntervalMs`: (500, 1000, 2000, 5000 - default 1000)
  - `SelectedGpuIndex`: Target GPU device index
  - `ShowCpu`: Boolean (default true)
  - `ShowGpu`: Boolean (default true)
  - `ShowCpuTemp`: Boolean (default true)
  - `ShowGpuTemp`: Boolean (default true)
  - `ShowRam`: Boolean (default true)
  - `TemperatureUnit`: "Celsius" or "Fahrenheit"
  - `Theme`: "System", "Dark", "Light"
  - `StartWithWindows`: Boolean (registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`)
  - `TaskbarAlignment`: "LeftOfTray", "RightEdge", "Manual"
  - `OffsetX`, `OffsetY`: Pixel offsets relative to taskbar anchor
  - `FontSize`: Double (default 11.5)

---

## 7. Performance & Resource Budgets

| Metric | Target | Verification Method |
| :--- | :--- | :--- |
| **Idle / Polling CPU** | < 0.5% average | Windows Performance Monitor / Task Manager |
| **Working Set Memory** | < 40 MB | Task Manager / Process Working Set 64-bit |
| **GDI / USER Handles** | < 150 handles | Sysinternals Process Explorer |
| **Garbage Collection** | Gen 0 < 1/min, Gen 1/2 = 0 | .NET dotnet-counters / Visual Studio Diagnostics |

---

## 8. Technical Decisions & Alternatives Considered

1. **WPF vs. WinForms vs. WinUI 3**:
   - *WinUI 3 (Windows App SDK)*: Requires heavy Windows App Runtime MSIX/WASDK dependencies (~150MB+ footprint, slow cold start, elevated background services).
   - *WinForms*: Lacks native DirectWrite tabular number rendering and smooth Fluent styling.
   - *WPF on .NET 8*: Best compromise — natively built into WindowsDesktop, DirectWrite typography, sub-second startup, zero external frameworks.
2. **DeskBand vs. Top-Level Layered Overlay**:
   - *DeskBand*: Deprecated and completely non-functional in Windows 11.
   - *Top-Level Layered Overlay*: 100% compatible with Windows 11, clean Win32 lifecycle, no Explorer crashing.
3. **NVML vs. WMI for GPU**:
   - WMI is notoriously slow (~100-300ms per call, high CPU spikes).
   - Direct NVML P/Invoke executes in < 0.1ms without CPU overhead.
   - Performance counters via PDH provide low-overhead metrics for non-NVIDIA GPUs.
