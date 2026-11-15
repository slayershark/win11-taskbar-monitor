# TEST_PLAN.md — Testing and Quality Assurance Plan

This document governs the verification and validation procedures for **WinTaskbarMonitor**.  
Every major feature must satisfy its documented acceptance criteria before being marked complete.

---

## 1. Functional Testing & Hardware Accuracy

| Test ID | Metric | Test Procedure | Expected Outcome | Status |
| :--- | :--- | :--- | :--- | :--- |
| **FT-01** | **CPU Utilisation** | Run `GetSystemTimes` monitoring and compare against Windows Task Manager "CPU" tab under idle and synthetic stress (`stress` or CPU-intensive task). | Readout matches Task Manager within ±2% margin. | `[ ]` |
| **FT-02** | **RAM Utilisation** | Compare reported physical memory used, total, and percentage against Windows Task Manager "Memory" tab. | Total RAM matches physical capacity (GB). Used RAM matches Task Manager within ±0.2 GB. | `[ ]` |
| **FT-03** | **GPU Utilisation (NVIDIA)** | Monitor NVIDIA GeForce MX350 utilisation using NVML under idle and during 3D/video workload. Compare with Task Manager "GPU 1". | Accurate real-time GPU load matching active engine load. | `[ ]` |
| **FT-04** | **GPU Utilisation (Intel iGPU)** | Monitor Intel Iris Xe Graphics utilisation via PDH counter under desktop activity and video playback. | Accurate real-time iGPU load matching Task Manager "GPU 0". | `[ ]` |
| **FT-05** | **GPU Selection Toggle** | Switch preferred GPU in Settings between Intel iGPU and NVIDIA MX350. | Widget instantly reflects the selected GPU's active metrics. | `[ ]` |
| **FT-06** | **CPU Temperature** | Query CPU temperature via sensor provider. If hardware sensor is queryable, verify realistic range (30°C - 95°C). | Valid reading or clean `N/A` fallback if unprivileged. No exception thrown. | `[ ]` |
| **FT-07** | **GPU Temperature** | Query NVIDIA MX350 temperature via NVML (`nvmlDeviceGetTemperature`). | Valid Celsius reading (e.g., 40°C - 75°C) matching vendor tools. | `[ ]` |
| **FT-08** | **Temperature Unit Toggle** | Switch temperature unit from Celsius to Fahrenheit in settings. | Readings convert correctly ($F = C \times \frac{9}{5} + 32$) with °F suffix. | `[ ]` |
| **FT-09** | **Metric Visibility Toggles** | Toggle visibility of individual metrics (e.g., disable GPU Temp, disable RAM). | Widget layout contracts neatly without gaps; remaining metrics remain aligned. | `[ ]` |

---

## 2. Windows 11 Taskbar Integration & Display

| Test ID | Feature | Test Procedure | Expected Outcome | Status |
| :--- | :--- | :--- | :--- | :--- |
| **WI-01** | **Taskbar Anchoring** | Observe position relative to Windows 11 taskbar (Left of Tray / notification chevron). | Perfectly aligned vertically within taskbar bounds; doesn't overlap icons. | `[ ]` |
| **WI-02** | **DPI Scale Change** | Change Windows display scaling (100% -> 125% -> 150%). | Widget scales crisply; fonts remain sharp; no clipped text or misaligned elements. | `[ ]` |
| **WI-03** | **Resolution Change** | Modify display resolution in Windows Settings. | Widget recalculates AppBar coordinates and re-anchors to taskbar within 500ms. | `[ ]` |
| **WI-04** | **Taskbar Auto-Hide** | Enable "Automatically hide the taskbar" in Windows Taskbar settings. | Widget moves/hides in sync with the taskbar without flickering. | `[ ]` |
| **WI-05** | **Explorer Restart Recovery**| Kill `explorer.exe` via Task Manager or PowerShell (`taskkill /f /im explorer.exe & start explorer.exe`). | Widget detects `"TaskbarCreated"` and snaps back into position within 2 seconds. | `[ ]` |
| **WI-06** | **Window Focus & Z-Order** | Click on desktop windows, fullscreen apps, and the widget itself. | Clicking widget does not steal keyboard focus (`WS_EX_NOACTIVATE`); doesn't appear in Alt+Tab (`WS_EX_TOOLWINDOW`). | `[ ]` |

---

## 3. UI/UX, Themes & Typography

| Test ID | Feature | Test Procedure | Expected Outcome | Status |
| :--- | :--- | :--- | :--- | :--- |
| **UX-01** | **Tabular Numerals** | Observe changing numbers (e.g. CPU bouncing between 9% and 10% and 100%). | Zero horizontal jitter; label and value bounds remain rock-solid. | `[ ]` |
| **UX-02** | **System Theme Adapt** | Switch Windows between Dark mode and Light mode in Personalisation Settings. | Widget instantly updates background tint, text colors, and separator lines to match. | `[ ]` |
| **UX-03** | **Manual Dark / Light** | Override theme in Settings to Dark or Light explicitly. | Forces selected theme regardless of Windows system theme. | `[ ]` |
| **UX-04** | **Threshold Alert Colors**| Stress CPU/GPU above 75% and 90%. | Values change to amber (>75%) and soft red (≥90%) clearly indicating elevated load. | `[ ]` |
| **UX-05** | **Hover Tooltips** | Hover mouse cursor over each metric item. | Displays rich tooltip with model names, exact memory usage, and hardware specs. | `[ ]` |
| **UX-06** | **Context Menu** | Right-click anywhere on the taskbar widget or system tray icon. | Fluent context menu opens with working options (Refresh rate, Theme, Settings, Exit). | `[ ]` |

---

## 4. Settings Persistence & Lifecycle

| Test ID | Feature | Test Procedure | Expected Outcome | Status |
| :--- | :--- | :--- | :--- | :--- |
| **SP-01** | **Settings Persistence** | Change refresh interval to 2s, toggle °F, hide RAM, exit application, and restart. | All configuration restored exactly as configured from `settings.json`. | `[ ]` |
| **SP-02** | **Start with Windows** | Enable "Start with Windows" in Settings, check `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. | Registry value created pointing to executable; cleanly removed when disabled. | `[ ]` |
| **SP-03** | **Corrupted Settings Recovery** | Intentionally write invalid JSON into `settings.json` and launch app. | App falls back gracefully to default settings without crashing and repairs file. | `[ ]` |

---

## 5. Performance & Resource Consumption Benchmarks

| Benchmark ID | Condition | Target Metric | Measured Value | Result |
| :--- | :--- | :--- | :--- | :--- |
| **PB-01** | **CPU Footprint (1s interval)** | < 0.5% average CPU usage over 5 minutes | *Pending measurement* | `[ ]` |
| **PB-02** | **CPU Footprint (500ms interval)**| < 0.8% average CPU usage | *Pending measurement* | `[ ]` |
| **PB-03** | **Memory Working Set** | < 40 MB private working set | *Pending measurement* | `[ ]` |
| **PB-04** | **Cold Startup Time** | < 1.0 second from launch to visible taskbar widget | *Pending measurement* | `[ ]` |
| **PB-05** | **Memory Leak Test** | 1-hour continuous run: Working set growth < 3 MB, Gen 2 GC = 0 | *Pending measurement* | `[ ]` |

---

## 6. Build, Standalone Packaging & Distribution

| Test ID | Feature | Test Procedure | Expected Outcome | Status |
| :--- | :--- | :--- | :--- | :--- |
| **BD-01** | **Release Publish** | Run `dotnet publish -c Release -r win-x64 --self-contained false` (and single-file). | Clean build output with zero errors or warnings. | `[ ]` |
| **BD-02** | **Clean Environment Execution** | Run compiled executable on a machine without dev tools. | Application launches smoothly without requiring external runtime installers. | `[ ]` |
| **BD-03** | **Clean Removal** | Delete executable and verify no persistent services or locked files remain. | Application uninstalls cleanly by simple folder deletion. | `[ ]` |
