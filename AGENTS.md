# AGENTS.md — Project Development Rules and Guidelines

## 1. Project Objectives and Scope

**Project Name**: WinTaskbarMonitor  
**Objective**: Build a lightweight, native, and highly reliable Windows 11 system-monitoring utility that displays essential hardware metrics directly on or anchored alongside the Windows 11 taskbar without requiring users to open Task Manager or a separate resource-heavy dashboard.

### Core Metrics:
1. **CPU Utilisation**: Accurate real-time total CPU percentage.
2. **GPU Utilisation**: Accurate real-time overall GPU percentage (supporting NVIDIA, AMD, Intel iGPU/dGPU).
3. **CPU Temperature**: Real-time reading in °C (with optional °F), or graceful `N/A` fallback if inaccessible without kernel elevation.
4. **GPU Temperature**: Real-time reading in °C (with optional °F) via vendor APIs (NVML, ADL/DXGI, Intel), or graceful `N/A`.
5. **RAM Utilisation**: Real-time physical memory percentage, used GB, and total GB.

### Core Principles:
- **Lightweight & Low Overhead**: Near-zero idle CPU footprint (< 0.5% CPU during 1s polling), minimal RAM footprint (< 30-40 MB).
- **Native Taskbar Integration**: Taskbar-anchored overlay that adheres to Windows 11 taskbar positioning, multi-monitor setups, DPI scaling, and auto-hide behavior without invasive Explorer code injection or unstable hooks.
- **Visual Harmony with Windows 11**: Adheres strictly to the Windows 11 Fluent design language (Segoe UI Variable, tabular numbers, light/dark/system themes, subtle borders and tooltips).
- **Resilience and Graceful Degradation**: Sensor failures or missing hardware must never crash the application or prevent other metrics from displaying.
- **Zero Bloat**: No background telemetry, no web runtimes (Electron/Chromium), no ads, no cloud dependencies.

---

## 2. Technology Stack & Architectural Principles

- **Platform**: Windows 11 (x64)
- **Language & Runtime**: C# 12 / .NET 8 LTS (WindowsDesktop / WPF or Win32 lightweight UI window)
- **UI Framework**: WPF with hardware acceleration, composition layering (`WS_EX_TOOLWINDOW`, `WS_EX_NOACTIVATE`, `WS_EX_TOPMOST`), Segoe UI Variable font, and Fluent theme styling.
- **Hardware Monitoring Stack**:
  - CPU & Memory: High-performance Win32 native APIs (`GetSystemTimes`, `GlobalMemoryStatusEx`, PDH / Windows Performance Counters).
  - GPU Metrics: Direct3D/DXGI engine counters, Windows Performance Counters (`GPU Engine(*)\Utilization Percentage`), NVML (`nvml.dll` P/Invoke for NVIDIA cards), and LibreHardwareMonitorLib / sensor providers where suitable.
  - Temperature: Native hardware monitor abstractions with fallback mechanisms (NVML for NVIDIA GPU temp, vendor WMI/ACPI, or LibreHardwareMonitor hardware providers). Non-admin friendly with explicit graceful degradation to `N/A`.
- **Architectural Principles**:
  - **Decoupled Architecture**: Strict separation between Hardware Polling Services, State Stores, Window Management (Taskbar Anchor Engine), and the UI Presentation layer.
  - **Asynchronous & Non-Blocking**: Polling runs on dedicated background timer threads; UI thread is never blocked.
  - **Zero-Allocation Hot Paths**: Reuse buffers, pre-allocated structs, and fixed string formatters to prevent GC churn.

---

## 3. Coding Conventions

- **Language Standard**: C# 12 with nullable reference types enabled (`<Nullable>enable</Nullable>`).
- **Naming Conventions**:
  - `PascalCase` for types, namespaces, methods, public properties, events.
  - `camelCase` for local variables and parameters.
  - `_camelCase` for private fields.
  - Interfaces prefixed with `I` (e.g., `IHardwareMonitorService`, `ITaskbarAnchorService`).
- **File Organization**:
  - One primary class/interface per file.
  - Group files logically into namespaces:
    - `WinTaskbarMonitor.Core`
    - `WinTaskbarMonitor.Monitoring` (Hardware providers: CPU, GPU, RAM, Temperatures)
    - `WinTaskbarMonitor.Taskbar` (Win32 AppBar, placement, DPI, multi-monitor hooks)
    - `WinTaskbarMonitor.UI` (Views, controls, converters, themes)
    - `WinTaskbarMonitor.Config` (Settings model and persistence)
- **Error Handling**:
  - Never swallow exceptions silently in the monitoring layer without logging or setting state to `Unavailable` / `N/A`.
  - Defensive sensor polling: wrap hardware driver/API calls in try-catch to guarantee that any single sensor failure does not bring down the entire application.

---

## 4. UI/UX and Design-System Requirements

- **Typography**:
  - Font Family: `Segoe UI Variable Text` / `Segoe UI Variable Display` (fallback to `Segoe UI`).
  - OpenType Feature: `FontNumeralAlignment="Tabular"` on numerical readouts to prevent horizontal jitter as numbers change.
  - Font Sizes: Metric values 11-12pt, labels 9-10pt, subtle contrast.
- **Layout**:
  - Ultra-compact horizontal bar designed to fit neatly in empty taskbar space (e.g. to the left of the system notification area or beside centered taskbar buttons).
  - Configurable positioning (Left of Tray, Screen Edge, or Custom taskbar anchor).
- **Themes**:
  - System Theme (auto-detects Windows dark/light app theme via registry `AppsUseLightTheme`), Manual Dark, Manual Light.
  - Status Indicators: Subtle accents for normal levels, amber for elevated (>80%), soft red for critical (>90%).
- **Interactive Features**:
  - Rich hover tooltips for detailed breakdowns (e.g., core counts, GPU name, exact memory used/free).
  - Right-click context menu for quick settings, theme switching, refresh interval, and exit.
  - Minimalist Settings Window for in-depth configuration.

---

## 5. Dependency Management Rules

- **Strict Dependency Discipline**: Avoid heavy external frameworks or unnecessary third-party packages.
- **Allowed Dependencies**:
  - Standard .NET 8 BCL (`System.Text.Json`, `System.Diagnostics`, etc.).
  - Native P/Invoke libraries (`PInvoke.User32` or direct well-documented `DllImport` definitions).
  - Highly targeted, transparent hardware access libraries (e.g., `LibreHardwareMonitorLib` trimmed or direct NVML/PDH bindings).
- **Forbidden Dependencies**:
  - Electron, Chromium Embedded Framework, WebView2 dashboards.
  - Heavy ORMs or external database engines.
  - Telemetry or tracking SDKs.

---

## 6. Performance and Security Requirements

- **CPU Overhead Target**: < 0.5% average CPU consumption on modern quad-core processors at default 1-second refresh rate.
- **Memory Overhead Target**: < 45 MB working set memory.
- **Security & Privileges**:
  - Runs in standard user mode without requiring Administrator / UAC elevation for standard metrics.
  - Gracefully displays `N/A` for protected sensor registers that require ring-0 drivers, instead of forcing elevation or failing to launch.
  - No network socket binding, no external internet traffic, no telemetric communication.

---

## 7. Testing and Quality Assurance Expectations

- **Test Plan Compliance**: Every release must be validated against `TEST_PLAN.md`.
- **Accuracy Verification**: Cross-verify CPU, GPU, and RAM readouts against Windows Task Manager and GPU vendor software.
- **Taskbar Stability Verification**:
  - DPI changes (100%, 125%, 150%, 200%).
  - Multi-monitor placement and resolution changes.
  - Windows Explorer restart (`taskkill /f /im explorer.exe & start explorer.exe`).
  - Taskbar auto-hide toggling.
- **Long-Running Stress Test**: Verify zero memory leak over continuous 1-hour and 24-hour monitoring runs.

---

## 8. Documentation Maintenance Requirements

- All development activities must adhere to the **Documentation-First Workflow**.
- At each milestone:
  - Update `TASK_PLAN.md` with verifiable task statuses (`[ ]`, `[~]`, `[x]`, `[!]`).
  - Update `CONTEXT.md` with active state, decisions, branch, commit, and next steps.
  - Update `CHANGELOG.md` with chronological changes under Keep a Changelog conventions.
  - Maintain `ARCHITECTURE.md` and `PROJECT_BRIEF.md`.
- No task shall be marked complete without verified proof of functionality.

---

## 9. Definition of Completion

A task or feature is only complete when:
1. Code is cleanly written, formatted, and strictly follows architectural rules.
2. The project compiles without warnings or errors.
3. Tests and manual validations against acceptance criteria pass.
4. Git commit is created with descriptive conventional commit messages.
5. All tracking documentation (`TASK_PLAN.md`, `CONTEXT.md`, `CHANGELOG.md`) is updated.
