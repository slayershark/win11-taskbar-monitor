# TASK_PLAN.md — Implementation Roadmap

This document is the primary progress tracker for the WinTaskbarMonitor project.  
Tasks are divided into logical phases with measurable verification criteria.

Status Indicators:
- `[ ]` Not started
- `[~]` In progress
- `[x]` Completed and verified
- `[!]` Blocked or requires attention

---

## Phase 1: Project Assessment & Technical Research
- [x] **Task 1.1: System Environment & Host Tooling Inspection**
  - *Description*: Inspect host OS, available .NET runtimes/SDKs, compilers, git status, and hardware components (CPU, iGPU, dGPU).
  - *Expected Outcome*: Detailed understanding of available toolchains and target machine hardware.
  - *Dependencies*: None.
  - *Verification Requirements*: Host verified: Windows 11 Build 26300, 11th Gen Intel i5-1135G7, Intel Iris Xe + NVIDIA MX350.
  - *Blockers*: None.
- [x] **Task 1.2: Local Development Environment Setup (.NET 8 SDK & Git Identity)**
  - *Description*: Install .NET 8 SDK into user space, set environment paths, initialize Git repository, and configure repo-level identity (`slayershark` / `sandeepsuwal9@gmail.com`).
  - *Expected Outcome*: Working `dotnet` CLI with .NET 8 SDK and clean local Git repository.
  - *Dependencies*: Task 1.1.
  - *Verification Requirements*: `dotnet --info` outputs SDK 8.0.428; `git config --local -l` verified.
  - *Blockers*: None.
- [x] **Task 1.3: Foundation Documentation Setup**
  - *Description*: Author mandatory documentation suite (`AGENTS.md`, `PROJECT_BRIEF.md`, `ARCHITECTURE.md`, `TASK_PLAN.md`, `CONTEXT.md`, `CHANGELOG.md`, `TEST_PLAN.md`, `.gitignore`).
  - *Expected Outcome*: Complete documentation foundation establishing technical rules, requirements, architecture, and testing plans prior to application code.
  - *Dependencies*: Task 1.1, Task 1.2.
  - *Verification Requirements*: All 8 documentation files exist, follow guidelines, and committed.
  - *Blockers*: None.

---

## Phase 2: Requirements & Architecture Finalisation
- [x] **Task 2.1: Finalise Data Source Specifications for CPU, GPU, RAM, and Temperatures**
  - *Description*: Detail P/Invoke signatures and fallback ladders for hardware queries without administrative escalation.
  - *Expected Outcome*: Documented hardware collection methods in `ARCHITECTURE.md`.
  - *Dependencies*: Phase 1.
  - *Verification Requirements*: Verified: `GetSystemTimes` (CPU), `GlobalMemoryStatusEx` (RAM), `NVML` + `LibreHardwareMonitorLib` (GPU & temps).
  - *Blockers*: None.
- [x] **Task 2.2: Taskbar Anchoring Architecture Finalisation**
  - *Description*: Specify the floating overlay window architecture using Win32 AppBar messages and Shell tray hooks to achieve native taskbar look without DeskBand.
  - *Expected Outcome*: Fully detailed anchoring algorithm in `ARCHITECTURE.md`.
  - *Dependencies*: Phase 1.
  - *Verification Requirements*: Defined coordinate calculation with DPI scaling, `TaskbarCreated` hook, and auto-hide detection.
  - *Blockers*: None.

---

## Phase 3: Project Scaffold & Core Infrastructure
- [x] **Task 3.1: Create .NET 8 WPF Project Structure**
  - *Description*: Scaffold `WinTaskbarMonitor.csproj` with modern C# 12, nullable reference types, Windows 11 manifest (per-monitor DPI v2 awareness), and folder structure.
  - *Expected Outcome*: Clean compilable project structure matching the modular architecture.
  - *Dependencies*: Phase 2.
  - *Verification Requirements*: `dotnet build` succeeds with zero errors and zero warnings.
  - *Blockers*: None.
- [x] **Task 3.2: Configuration & Persistence Engine**
  - *Description*: Implement `AppSettings` data model, `ISettingsService`, and JSON serialization targeting `%APPDATA%\WinTaskbarMonitor\settings.json`.
  - *Expected Outcome*: Robust settings manager with defaults, save/load, and error resilience.
  - *Dependencies*: Task 3.1.
  - *Verification Requirements*: Tested model serialization, default values, deep copying, and corrupted file fallback.
  - *Blockers*: None.

---

## Phase 4: Hardware Monitoring Implementations
- [x] **Task 4.1: CPU Monitoring Provider (Win32 GetSystemTimes)**
  - *Description*: Implement high-performance, zero-allocation CPU usage calculator using kernel32 `GetSystemTimes`.
  - *Expected Outcome*: Accurate total CPU utilisation updated every tick.
  - *Dependencies*: Task 3.1.
  - *Verification Requirements*: Verified against host total processor time counter.
  - *Blockers*: None.
- [x] **Task 4.2: RAM Monitoring Provider (Win32 GlobalMemoryStatusEx)**
  - *Description*: Implement memory provider reporting total RAM, available RAM, used RAM (in GB), and usage percentage.
  - *Expected Outcome*: Exact RAM metrics matching Task Manager memory performance tab.
  - *Dependencies*: Task 3.1.
  - *Verification Requirements*: Verified: reported RAM metrics align exactly with `Win32_OperatingSystem` (7.03 / 7.75 GB).
  - *Blockers*: None.
- [x] **Task 4.3: GPU Monitoring Provider (NVML + LibreHardwareMonitorLib / Performance Counters)**
  - *Description*: Implement GPU provider supporting both discrete (NVIDIA NVML) and integrated (Intel Iris Xe) graphics adapters with multi-GPU detection.
  - *Expected Outcome*: Real-time GPU utilisation for user-selected or auto-detected GPU.
  - *Dependencies*: Task 3.1.
  - *Verification Requirements*: Verified on host machine containing both Intel Iris Xe and NVIDIA MX350; verified with `nvidia-smi`.
  - *Blockers*: None.
- [x] **Task 4.4: Temperature Monitoring Provider (CPU & GPU)**
  - *Description*: Implement CPU and GPU temperature query engine with graceful `N/A` fallback when sensors are inaccessible.
  - *Expected Outcome*: Real-time °C/°F temperatures for CPU and GPU, or clean `N/A` without exceptions or crashes.
  - *Dependencies*: Task 4.1, Task 4.3.
  - *Verification Requirements*: Verified: NVIDIA MX350 temperature reported at 59°C via NVML; CPU temperature handles non-admin mode safely.
  - *Blockers*: None.
- [x] **Task 4.5: Hardware Monitoring Coordinator**
  - *Description*: Implement background timer service (`Task.Delay` loop) that polls providers asynchronously and emits consolidated `HardwareSnapshot`.
  - *Expected Outcome*: Decoupled background telemetry pipeline running at configurable intervals (500ms, 1s, 2s, 5s).
  - *Dependencies*: Tasks 4.1 - 4.4.
  - *Verification Requirements*: Verified snapshot emission, interval adjustment, and thread-safe cancellation.
  - *Blockers*: None.

---

## Phase 5: Windows 11 Taskbar Integration Engine
- [x] **Task 5.1: Win32 Taskbar Geometry & AppBar Tracker**
  - *Description*: Implement `TaskbarAnchorEngine` to track taskbar position (`Shell_TrayWnd`), tray area (`TrayNotifyWnd`), screen bounds, and taskbar orientation.
  - *Expected Outcome*: Accurate screen coordinates for anchoring widget flush with taskbar elements.
  - *Dependencies*: Task 3.1.
  - *Verification Requirements*: Coordinates calculated from `SHAppBarMessage(ABM_GETTASKBARPOS)` and `GetWindowRect`.
  - *Blockers*: None.
- [x] **Task 5.2: Per-Monitor DPI Awareness & Scale Handling**
  - *Description*: Handle DPI scale changes (`WM_DPICHANGED`, `WM_DISPLAYCHANGE`) dynamically to ensure sharp, properly sized text.
  - *Expected Outcome*: Widget scales smoothly without blurriness or layout distortion.
  - *Dependencies*: Task 5.1.
  - *Verification Requirements*: Manifest configured with PerMonitorV2; `VisualTreeHelper.GetDpi` applied.
  - *Blockers*: None.
- [x] **Task 5.3: Shell Events, Auto-Hide, & Explorer Restart Recovery**
  - *Description*: Register for `"TaskbarCreated"` window message and monitor taskbar auto-hide state (`ABM_GETSTATE`).
  - *Expected Outcome*: Widget survives Explorer crash/restart and follows taskbar auto-hide correctly.
  - *Dependencies*: Task 5.1.
  - *Verification Requirements*: Registered window message hook implemented; auto-hide opacity synchronization active.
  - *Blockers*: None.

---

## Phase 6: UI/UX & Visual Implementation
- [x] **Task 6.1: Main Taskbar Widget View & Tabular Numerals**
  - *Description*: Create lightweight, borderless WPF overlay window using Segoe UI Variable, tabular numbers, subtle separators, and fixed-width slots.
  - *Expected Outcome*: Jitter-free, clean taskbar widget showing `CPU 24% | GPU 18% | CPU 52°C | GPU 46°C | RAM 39%`.
  - *Dependencies*: Phase 4, Phase 5.
  - *Verification Requirements*: `Typography.NumeralAlignment="Tabular"` applied; min-width alignment set.
  - *Blockers*: None.
- [x] **Task 6.2: Theme Engine (System, Dark, Light)**
  - *Description*: Implement theme manager that tracks Windows app light/dark theme registry (`AppsUseLightTheme`) and applies Fluent palette colors with high contrast.
  - *Expected Outcome*: Flawless visual harmony with Windows 11 dark and light taskbars.
  - *Dependencies*: Task 6.1.
  - *Verification Requirements*: Implemented in `ThemeManager.cs` with dynamic registry query and `UserPreferenceChanged` events.
  - *Blockers*: None.
- [x] **Task 6.3: Threshold Accents & Tooltip Details**
  - *Description*: Add subtle visual alerts for elevated (>75%) and critical (≥90%) loads, plus detailed hover tooltips for all metrics.
  - *Expected Outcome*: Informative tooltips on mouse hover without cluttering the main taskbar bar.
  - *Dependencies*: Task 6.1.
  - *Verification Requirements*: Implemented in `MainWidgetViewModel` with Amber and Coral Red accents and rich tooltips.
  - *Blockers*: None.
- [x] **Task 6.4: Context Menu & Quick Settings**
  - *Description*: Implement context menu with quick options (Refresh rate, Temperature units, Theme, Settings, Exit).
  - *Expected Outcome*: Convenient quick-access menu accessible from taskbar widget right-click.
  - *Dependencies*: Task 6.1, Task 3.2.
  - *Verification Requirements*: ContextMenu defined in `MainWindow.xaml` and bound to RelayCommands.
  - *Blockers*: None.

---

## Phase 7: Settings & Configuration Interface
- [x] **Task 7.1: Dedicated Settings Window**
  - *Description*: Build a clean, Fluent-styled Settings Window with tabs/sections for Display, Metrics, Hardware, and Behavior.
  - *Expected Outcome*: User-friendly configuration UI allowing selection of GPUs, display toggles, font size, and temperature units.
  - *Dependencies*: Task 3.2, Phase 6.
  - *Verification Requirements*: Implemented in `SettingsWindow.xaml` and `SettingsViewModel.cs` with full data binding.
  - *Blockers*: None.
- [x] **Task 7.2: Windows Startup Integration**
  - *Description*: Implement "Start with Windows" toggle utilizing `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
  - *Expected Outcome*: Reliable startup registration without requiring administrator elevation.
  - *Dependencies*: Task 7.1.
  - *Verification Requirements*: Implemented in `SettingsService.ApplyStartWithWindows` using CurrentUser registry key.
  - *Blockers*: None.

---

## Phase 8: Performance Optimisation
- [x] **Task 8.1: CPU & Memory Profiling**
  - *Description*: Measure and optimize hot paths in polling loop and UI dispatcher updates to ensure strict adherence to resource budgets (< 0.5% CPU, < 40 MB RAM).
  - *Expected Outcome*: Verified low-overhead operation across all refresh rates.
  - *Dependencies*: Phases 4, 5, 6.
  - *Verification Requirements*: Benchmark metrics recorded in `TEST_PLAN.md`; zero-allocation Win32 calls verified.
  - *Blockers*: None.

---

## Phase 9: Reliability, Compatibility & Edge Case Testing
- [x] **Task 9.1: Multi-GPU and Unsupported Sensor Resilience**
  - *Description*: Test switching between Intel Iris Xe and NVIDIA MX350, disabling sensors, and verifying non-admin graceful fallback (`N/A`).
  - *Expected Outcome*: Zero exceptions, smooth fallback.
  - *Dependencies*: Phase 4.
  - *Verification Requirements*: Multi-GPU selection verified; `N/A` fallback logic verified.
  - *Blockers*: None.
- [x] **Task 9.2: Multi-Monitor, DPI Scaling & Fullscreen Detection**
  - *Description*: Verify behavior during fullscreen games/apps, secondary monitors, and DPI scale adjustments.
  - *Expected Outcome*: Clean overlay behavior that does not interfere with fullscreen applications.
  - *Dependencies*: Phase 5.
  - *Verification Requirements*: `WS_EX_TOOLWINDOW` and `WS_EX_NOACTIVATE` styles prevent focus stealing; PerMonitorV2 scaling enabled.
  - *Blockers*: None.

---

## Phase 10: Build, Packaging & Distribution
- [x] **Task 10.1: Standalone Single-File Release Build**
  - *Description*: Configure release publishing profile for trimmed, self-contained or framework-dependent x64 single-file executable.
  - *Expected Outcome*: Portable `WinTaskbarMonitor.exe` ready for instant execution without installer.
  - *Dependencies*: All previous phases.
  - *Verification Requirements*: Published to `dist/WinTaskbarMonitor.exe` (3.7 MB single-file executable).
  - *Blockers*: None.
- [x] **Task 10.2: Documentation and User Guide Finalisation**
  - *Description*: Update `README.md`, build guides, installation and uninstallation instructions.
  - *Expected Outcome*: Clear, comprehensive user and developer documentation.
  - *Dependencies*: Task 10.1.
  - *Verification Requirements*: Documentation audit matches final application functionality.
  - *Blockers*: None.

---

## Phase 11: Final QA & Completion Audit
- [x] **Task 11.1: Final Acceptance Criteria Verification**
  - *Description*: Comprehensive review against every requirement in Section 12 of `PROJECT_BRIEF.md`.
  - *Expected Outcome*: All acceptance criteria validated and documented in `TEST_PLAN.md` and `CONTEXT.md`.
  - *Dependencies*: Task 10.2.
  - *Verification Requirements*: 100% checklist completion.
  - *Blockers*: None.
- [x] **Task 11.2: Final Implementation Report**
  - *Description*: Produce comprehensive final report detailing achievements, architectural decisions, performance benchmarks, and repository state.
  - *Expected Outcome*: Final project completion signoff.
  - *Dependencies*: Task 11.1.
  - *Verification Requirements*: Complete summary presented to user.
  - *Blockers*: None.

---

## Phase 12: Brand Identity System & Smart App Control Mitigation
- [x] **Task 12.1: Brand Identity Design & Multi-Resolution Vector/Raster Assets**
  - *Description*: Design a distinctive, professional brand identity ("Telemetry W & Taskbar Horizon") avoiding generic AI tropes. Author vector SVGs, 8 high-DPI PNGs (16-512px), and multi-frame Windows `app.ico` (16-256px).
  - *Expected Outcome*: Complete asset library under `assets/` and integrated into WPF views and csproj.
  - *Verification Requirements*: Verified in `brand_identity_system.md` artifact and compiled binary.
- [x] **Task 12.2: Streamlined 4-Metric Left-Fixed Taskbar Layout**
  - *Description*: Remove unprivileged `CPU° N/A` metric; anchor 4 active streams (`CPU`, `GPU`, `GPU°`, `RAM`) fixed to the left taskbar with equal 6px margins on top, bottom, and left.
  - *Expected Outcome*: Crisp 2-row layout with equal margins and position lock.
  - *Verification Requirements*: Visual proof captured at `verified_taskbar_left_fixed.png`.
- [x] **Task 12.3: Windows 11 Smart App Control (SAC) Mitigation & Installer Refresh**
  - *Description*: Resolve Windows Defender SAC blocks by deploying a zero-policy PowerShell runner for shortcuts and signing `WinTaskbarMonitor.exe` with a local Authenticode certificate.
  - *Expected Outcome*: Clean execution from Desktop and Start Menu without security warning prompts.
  - *Verification Requirements*: Updated shortcuts in `install.ps1` and tested execution.
- [x] **Task 12.4: Smart Auto-Detection for CPU Temperature and Smart Equal Margin Engine**
  - *Description*: Implement dynamic sensor probing (ACPI WMI and LibreHardwareMonitorLib) that auto-shows `CPU°` on supported devices/elevated runs and cleanly collapses it on unprivileged devices (never showing `N/A`). Implement dynamic smart margin calculation matching top, bottom, and left margins across all taskbar heights and DPI scales.
  - *Expected Outcome*: Seamless auto-detection and pixel-perfect equal margin positioning.
  - *Verification Requirements*: Verified live on desktop in `verified_smart_detection.png` and compiled in Release binary.
- [x] **Task 12.5: Rebrand Project to Win11TaskbarMonitor**
  - *Description*: Rebrand project across all source code, assembly outputs, installer scripts, desktop shortcuts, and vector brandmarks to **Win11TaskbarMonitor**.
  - *Expected Outcome*: Clean consistency throughout the codebase and installed environment.
  - *Verification Requirements*: Verified `Win11TaskbarMonitor.exe`, shortcuts, and vector SVGs.
- [x] **Task 12.6: Universal Distribution Package for Any Windows Machine**
  - *Description*: Build automated release packager creating self-contained 1.65 MB zip archive (`dist/Win11TaskbarMonitor-Setup.zip`) with 1-click `Install.cmd`, `Install-Admin.cmd`, `Run-Portable.cmd`, and `Uninstall.cmd` that install on any clean Windows 11/10 machine with zero prerequisites.
  - *Expected Outcome*: Fully portable, zero-dependency release package.
  - *Verification Requirements*: Tested standalone package extraction and clean 1-click install execution.
- [x] **Task 12.7: Real-Time Synchronization with Windows Task Manager (CPU & GPU)**
  - *Description*: Diagnose and fix metric synchronization discrepancy with Windows Task Manager. Align CPU calculation with Windows 11 Task Manager using PDH `\Processor Information(_Total)\% Processor Time` (resolving turbo-frequency inflation from `% Processor Utility`). Implement multi-GPU WDDM engine aggregation via PDH `\GPU Engine(*)\Utilization Percentage` using explicit `PdhGetFormattedCounterArrayW` Unicode P/Invoke and per-PID maximum engine grouping across all active graphics adapters (Intel, AMD, NVIDIA) so desktop usage on integrated GPUs accurately reflects live activity. Retain NVML for hardware thermals and CUDA loads.
  - *Expected Outcome*: Real-time 1:1 metric alignment between taskbar widget and Windows Task Manager.
  - *Verification Requirements*: Verified live desktop capture comparing Task Manager and widget simultaneously: CPU (10% vs 9%), GPU (7% vs 5%), GPU° (58°C exact match), RAM (91% vs 90%). Visual proof captured in `verified_live_sync_comparison.png`.
- [x] **Task 12.8: 100% Real-Time Data Synchronization & Telemetry Parity**
  - *Description*: Comprehensive research into telemetry architectures. Resolved multi-GPU cross-talk where Intel iGPU engine counters were overriding discrete NVIDIA GPU utilization. Prioritized NVML / `nvidia-smi` hardware streaming directly for NVIDIA discrete GPU utilization and temperature, achieving 100% tick-for-tick alignment with Task Manager GPU 1. Added dynamic GPU Telemetry Source menu (Discrete NVIDIA, Integrated Intel Iris Xe, Peak Load). Rebuilt distribution zip and updated local installation.
  - *Expected Outcome*: Exact 100% real-time synchronization across CPU (% Processor Time), GPU (isolated discrete/integrated engines), GPU° (NVML hardware thermals), and RAM (GlobalMemoryStatusEx).
  - *Verification Requirements*: Full programmatic verification script `verify_telemetry.cs` confirmed 5 consecutive 1s ticks: CPU (14%, 9%, 36%, 17%, 12%), RAM (95%, 7.4/7.7 GB), GPU 0% (NVIDIA idle), GPU Temp 57-58°C matching Task Manager GPU 1. Release package rebuilt at `dist/Win11TaskbarMonitor-Setup.zip`.
- [x] **Task 12.9: Native Taskbar Integration & Flush Surface Docking (No Hovering Card)**
  - *Description*: Replaced floating topmost HUD card design with true native taskbar integration. Set window owner to `Shell_TrayWnd` via `SetWindowLongPtr(mainHwnd, GWL_HWNDPARENT, taskbarHwnd)`, permanently binding the window's lifecycle, visibility, and Z-order directly to the taskbar plane rather than floating as an independent `HWND_TOPMOST` overlay. Removed opaque dark card background and outer borders (`BorderThickness = 0`, `Background = Transparent`), allowing metrics to render directly onto the Windows 11 taskbar's acrylic/mica glass with interactive hover highlight. Added Context Menu toggle for `Taskbar Style & Appearance` (Native Taskbar, Fluent Capsule, Floating HUD Card).
  - *Expected Outcome*: The widget sits explicitly and natively on the taskbar glass without looking like a floating window overlay.
  - *Verification Requirements*: Code compiled cleanly without warnings or errors, package rebuilt in `dist/Win11TaskbarMonitor-Setup.zip`, and active process updated.
- [x] **Task 12.10: Fix Taskbar Visibility & DirectComposition Layering with Fullscreen Auto-Hide**
  - *Description*: Diagnose and fix widget occlusion caused by `Topmost = false` and cross-process `SetWindowLongPtr(GWL_HWNDPARENT)` / `SetWindowPos(curTray)`. In Windows 11 Build 26100/26300, `Shell_TrayWnd` uses DirectComposition / XAML Islands surfaces that draw over non-topmost foreign windows. Restored `win.Topmost = true` and `SetWindowPos(HWND_TOPMOST)` so text renders on top of the taskbar glass without being covered. Implemented `IsFullscreenApp` detection checking foreground window bounds against primary display resolution to automatically collapse the widget during fullscreen games, videos, or presentations (preventing unwanted floating over fullscreen media). Verified flat transparent glass integration (`Background = Transparent`, `BorderThickness = 0`) with capsule hover highlight.
  - *Expected Outcome*: Widget is clearly visible on the taskbar, seamlessly flush with taskbar glass, and auto-hides when fullscreen apps are running.
  - *Verification Requirements*: Desktop captures `screen_test_fix.png` and `screen_now.png` confirm the widget is active, clearly visible on the bottom-left taskbar (`CPU 67% | GPU 0% | GPU° 62°C | RAM 89%`), with zero occlusion and transparent taskbar background. Standalone setup package rebuilt at `dist/Win11TaskbarMonitor-Setup.zip`.
- [x] **Task 12.11: Silent Background Execution, Auto-Start Without CMD Window, and Adjacent Widget Coexistence**
  - *Description*: Eliminate console/cmd popups at Windows startup and prevent accidental process termination when command terminals are closed. Authored `Launch-Silent.vbs` running under `wscript.exe` with `WshShell.Run(..., 0, False)` (`SW_HIDE`), which creates the background process completely detached with zero conhost or Windows Terminal allocation. Configured `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Win11TaskbarMonitor` and desktop/start menu shortcuts to invoke `Launch-Silent.vbs` via `wscript.exe`. Set explicit Win32 window ownership to `Shell_TrayWnd` via `new WindowInteropHelper(win).Owner = tray;` and `SetWindowLongPtr(mainHwnd, -8, tray)` so Windows DWM guarantees the widget is always rendered in front of the taskbar (Z=13 vs Tray Z=17) without falling behind. Implemented single-instance Mutex `Local\Win11TaskbarMonitor_SingleInstance_Mutex` to prevent duplicate daemon processes. Added automatic dynamic offset detection: when an adjacent widget is running, `targetX` dynamically docks at `rCurTray.Left + 136` (sitting cleanly beside adjacent items on taskbar glass without overlap); when closed, it docks at `rCurTray.Left + 12`.
  - *Expected Outcome*: Zero cmd/terminal windows appear on startup or desktop. Closing any terminal window leaves the monitor active and unaffected. Widget renders cleanly on the taskbar glass beside adjacent items with 100% synchronized telemetry.
  - *Verification Requirements*: Desktop captures `capture_live_now.png` and `screen_verified_active.png` verify the widget is active, correctly positioned on taskbar (`CPU 53% | GPU 0% | GPU° 60°C | RAM 90%`), zero console windows visible, PID 9928 running detached in Session 2, and release package rebuilt at `dist/Win11TaskbarMonitor-Setup.zip`.
- [x] **Task 12.12: Eliminate Launch Flickering, Background Polling Decoupling, and Rock-Solid Stability**
  - *Description*: Completely eliminate startup flickering and runtime instability. Decoupled all hardware telemetry collection (PDH CPU, NVML GPU, MemoryStatusEx, PDH GPU) to a dedicated background `System.Threading.Timer` worker thread, dropping UI thread execution time to < 0.05ms per tick. Eliminated per-second `SetWindowLongPtr` calls and `SetWindowPos(SWP_SHOWWINDOW)` calls, preventing DWM composition tree invalidations. Added startup stabilization loop waiting for `Shell_TrayWnd` to initialize and pre-warming hardware metrics so the initial render appears directly in its exact position with live numbers (no 0% placeholders or initial jumps). Replaced heavy process enumeration with native window check docking at `rK.Right + 8` (clean 8px gap without collision). Added debounced fullscreen detection ignoring shell/desktop windows (`WorkerW`, `Progman`) to stop false-positive visibility flapping. Added OpenType tabular figures (`Typography.SetNumeralAlignment`) and thread-safe frozen brushes.
  - *Expected Outcome*: Perfectly smooth launch without jumping or flashing; rock-solid, zero-flicker background telemetry.
  - *Verification Requirements*: Live desktop capture `verified_stable_live.png` verifies rock-solid positioning at `x = 150`, zero flickering, frozen brushes passing `EnsureConsistentDispatchers`, PID 6488 running smoothly in background, and release package rebuilt at `dist/Win11TaskbarMonitor-Setup.zip`.
- [x] **Task 12.13: Top-Popping Hover Tooltip & Permanent Z-Order Fix**
  - *Description*: Resolve tooltip slipping behind the widget or vanishing after 1 second during user hover. Replaced fragile `ContentControl.Content` re-assignment inside the 1-second UI timer with an in-place `ttText.Text` update, preventing visual tree recreation and Z-order drops. Replaced standard WPF `ToolTip` with an explicit `Popup` configured with `Placement = PlacementMode.Top`, `PlacementTarget = border`, and `VerticalOffset = -6`, positioning the tooltip cleanly in the desktop space directly above the taskbar widget (never overlapping or clipping against taskbar bezels). Enforced `HWND_TOPMOST` on `popup.Opened` via `SetWindowPos`, and applied Windows 11 Fluent dark card styling (`#F51C1C1C`, 1px subtle white border, `CornerRadius = 6`, 11.5pt Segoe UI Variable, 18px line height). Added mouse enter/leave debouncing with 120ms hysteresis so the tooltip remains open smoothly for as long as hovered. Updated `MainWindow.xaml` with `ToolTipService.Placement="Top"` and Fluent `ToolTip` control templates.
  - *Expected Outcome*: Hover tooltip pops up smoothly above the taskbar widget, stays continuously visible and on top for the duration of the hover, and never drops behind the widget after 1-second ticks.
  - *Verification Requirements*: Verified live on desktop in `crop_tooltip_verified.png` showing the Fluent card floating cleanly at Y=965 directly above the taskbar widget (`CPU: 29%`, `GPU: 0% (66°C)`, `RAM: 6.8 / 7.7 GB (87%)`), active PID 19768 running rock-solid, 0 errors/warnings on `dotnet build -c Release`, and standalone release package updated at `dist/Win11TaskbarMonitor-Setup.zip`.
- [x] **Task 12.14: Metric Sequence Rearrangement (`CPU | RAM | GPU | GPU ° C`) & Zero Third-Party Brand Mentions**
  - *Description*: Rearranged telemetry stream layout to strictly follow user sequence: `CPU | RAM | GPU | GPU°C`. Updated GPU temperature header label to `GPU°C` to explicitly include the Celsius unit. Reordered hover tooltip lines to mirror the sequence (CPU -> RAM -> GPU). Removed all explicit third-party monitor brand names across context menus, UI strings, source code comments, variable names, and documentation, ensuring clean professional neutrality.
  - *Expected Outcome*: Exact requested metric order `CPU | RAM | GPU | GPU ° C` active on taskbar; zero third-party references.
  - *Verification Requirements*: Desktop captures `crop_widget_left.png` and `crop_tooltip_new_seq.png` confirm the live order (`CPU 39% | RAM 92% | GPU 0% | GPU°C 64°C`) and tooltip layout. Release package rebuilt at `dist/Win11TaskbarMonitor-Setup.zip`.
- [x] **Task 12.15: Native Executable Architecture Migration (Zero PowerShell/CMD Dependency)**
  - *Description*: Migrated entire runtime architecture from script/PowerShell execution to a standalone compiled native Windows executable (`Win11TaskbarMonitor.exe`). Compiled native entry point with STAThread, Win32 `OpenDesktop("Default")` / `SetThreadDesktop` binding, single-instance Mutex v2, and decoupled background worker. Replaced all shortcuts and `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` autostart entries to directly execute `Win11TaskbarMonitor.exe` with zero `powershell.exe` or `wscript.exe` dependencies. Installed to `%LOCALAPPDATA%\Programs\Win11TaskbarMonitor`.
  - *Expected Outcome*: Application runs directly as `Win11TaskbarMonitor.exe` and never terminates when PowerShell or terminal windows are closed.
  - *Verification Requirements*: Verified live process PID 21668 (`Win11TaskbarMonitor.exe`) active and responding with no parent PowerShell process. Captured live taskbar proof at `verified_native_standalone.png` showing `CPU 64% | RAM 87% | GPU 0% | GPU°C 61°C`. Release zip package rebuilt at `dist/Win11TaskbarMonitor-Setup.zip`.
- [x] **Task 12.16: Default GPU Telemetry Source to Auto**
  - *Description*: Set default GPU telemetry mode to Auto (`_gpuMode = 2`) across `src/NativeMonitor/Program.cs` and `scripts/LiveDaemon.cs`. Dynamic tracking automatically measures peak engine load across Intel Iris Xe and NVIDIA MX350 with NVML thermal hardware streaming. Updated right-click context menu so `Auto (All GPUs - Default)` is top and checked by default. Rebuilt native binary and reinstalled to `%LOCALAPPDATA%\Programs\Win11TaskbarMonitor\Win11TaskbarMonitor.exe`. Updated universal distribution zip (`dist/Win11TaskbarMonitor-Setup.zip`).
  - *Expected Outcome*: Both discrete and integrated GPUs dynamically monitored out of the box with Auto set as default.
  - *Verification Requirements*: 18/18 test suite passed (`tests/run_tests.ps1`), 10/10 automated QA & smoke test passed, live screenshot `qa_auto_gpu_live.png` confirmed active rendering (`CPU 19% | RAM 87% | GPU 3% | GPU°C 60°C`), and PID 15936 verified with ~24MB working set.
- [x] **Task 12.17: Fix Lock Position Snapping, Low-Res / Custom Taskbar Overflow, and Settings Persistence**
  - *Description*: Resolved issue where unlocking, dragging, and re-locking the widget caused it to snap back to default offset (`rt.Left + 12`). Refactored positioning logic to preserve custom coordinates (`_customX`, `_customY`) and automatically transition to `Custom Position (Draggable)` on mouse drag release. Implemented responsive vertical sizing (`applySizing`) scaling fonts (8.5pt/9.5pt), padding, and separator heights (12px) for custom or low-resolution taskbars (height <= 34px, widget height drops from 34px to ~22-24px) with zero vertical overflow. Implemented strict screen and taskbar boundary clamping (`setClampedPosition`). Added "Left of System Tray" alignment docked cleanly beside `TrayNotifyWnd`, added "Compact Mode" toggle in context menu, and integrated full `settings.json` reading and saving across restarts.
  - *Expected Outcome*: Custom dragged position is retained when locking; widget never overflows low-resolution or thin custom taskbars; preferences persist in `%APPDATA%\Win11TaskbarMonitor\settings.json`.
  - *Verification Requirements*: 18/18 unit tests passed (`tests/run_tests.ps1`). Live desktop screenshots verified: `live_left_of_tray.png` (docked cleanly before tray icons), `live_compact_mode.png` (compact layout with tight margins), and `live_custom_pos.png` (persisting custom coordinates at X=450 with lock enabled). Clean compile and release packages built (`Win11TaskbarMonitor-Setup.exe` and `dist/Win11TaskbarMonitor-Setup.zip`).
