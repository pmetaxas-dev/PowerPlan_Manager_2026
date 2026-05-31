# Product Requirements Document (PRD)

## Power-Plan Manager — Sentinel Edition

**Document Version:** 1.0  
**Last Updated:** 2026-05-16  
**Status:** Active Development  
**Platform:** Windows (Windows 7+)  
**Framework:** .NET 8 / Windows Forms  

---

## Table of Contents

1. [Overview](#1-overview)
2. [Goals & Non-Goals](#2-goals--non-goals)
3. [Target Users](#3-target-users)
4. [System Requirements](#4-system-requirements)
5. [Architecture Overview](#5-architecture-overview)
6. [Features & Functional Requirements](#6-features--functional-requirements)
7. [Non-Functional Requirements](#7-non-functional-requirements)
8. [UI / UX Requirements](#8-ui--ux-requirements)
9. [Power Plans Reference](#9-power-plans-reference)
10. [Settings & Persistence](#10-settings--persistence)
11. [Known Limitations & Risks](#11-known-limitations--risks)
12. [Future Enhancements (Backlog)](#12-future-enhancements-backlog)
13. [Glossary](#13-glossary)

---

## 1. Overview

**Power-Plan Manager** is a lightweight Windows system-tray utility that automatically switches the active Windows power plan based on user activity. When the user is active, it enables a high-performance power plan to maximise responsiveness. After a configurable period of inactivity it switches to an energy-saving plan to reduce power consumption and heat. As soon as activity is detected again, the high-performance plan is restored transparently.

The **Sentinel** edition introduces dynamic detection of AMD Ryzen-optimised power profiles, adaptive plan selection, and reliable resource management for always-on background operation.

---

## 2. Goals & Non-Goals

### Goals

- **Automate power plan switching** between a high-performance and an energy-saver profile based on idle/active state.
- **Support AMD Ryzen systems** by preferring the 1usmus Ryzen Universal power profile when available.
- **Run silently in the system tray** with minimal user interaction required.
- **Start automatically with Windows** so the user never needs to think about it.
- **Enforce correct Energy Saver CPU cap** (50 % maximum processor state) on startup to prevent wasted cycles.
- **Give the user manual control** via a single checkbox to enable or disable automatic management.

### Non-Goals

- Does **not** manage GPU, display, sleep, or hibernate settings.
- Does **not** replace a full power management suite (e.g., Windows Settings, AMD Ryzen Master).
- Does **not** provide scheduled (time-of-day) power plan changes.
- Is **not** a cross-platform utility — targets Windows only.

---

## 3. Target Users

| Persona | Description |
|---|---|
| **AMD Ryzen desktop/laptop user** | Wants maximum performance when active and lower power draw when away, without manual switching. |
| **General Windows power user** | Comfortable with system-tray utilities; wants automated power management without bloat. |
| **Energy-conscious user** | Wants to reduce idle power consumption automatically. |

---

## 4. System Requirements

| Requirement | Minimum | Recommended |
|---|---|---|
| **OS** | Windows 7 SP1 | Windows 10 / 11 |
| **Architecture** | x64 | x64 |
| **.NET Runtime** | .NET 8 (Windows) | .NET 8 (Windows) |
| **Permissions** | Standard user (some features require elevation) | Administrator |
| **Power Plans** | Ultimate Performance built-in | 1usmus Ryzen Universal profile installed |

> **Note:** Setting the Energy Saver processor throttle cap requires a one-time UAC elevation prompt. All other operations run without elevation.

---

## 5. Architecture Overview

```
+-----------------------------------------------------------+
|                    Power-Plan Manager                     |
|                                                           |
|  +----------------+   +--------------------------------+  |
|  |    Form1       |   |         IdleChecker            |  |
|  |  (Main UI)     |-->|  - idleCheckTimer (90 s)       |  |
|  |  System Tray   |   |  - userInputCheckTimer (5 s)   |  |
|  |  About Window  |   |  - GetLastInputInfo (Win32)    |  |
|  +-------+--------+   +-------------+------------------+  |
|          |                          |                     |
|          |          +--------------v------------------+  |
|          +--------->|      PowerPlanDetector          |  |
|                     |  - IsPowerPlanAvailable()        |  |
|                     |  - GetOptimalHighPerformancePlan |  |
|                     +--------------+------------------+  |
|                                    |                     |
|                     +--------------v------------------+  |
|                     |         Constants               |  |
|                     |  - HighPerformance  GUID        |  |
|                     |  - UltimatePerformance GUID     |  |
|                     |  - EnergySaver GUID             |  |
|                     |  - RyzenUniversal GUID          |  |
|                     +---------------------------------+  |
+-----------------------------------------------------------+
          |
          v  powercfg.exe (Windows built-in CLI)
     Windows Power Management API
```

### Key Components

| File | Class | Responsibility |
|---|---|---|
| `Form1.cs` | `Form1` | Main window, system tray, checkbox toggle, UAC-protected processor state enforcement |
| `IdleChecker.cs` | `IdleChecker` | Idle detection via Win32 `GetLastInputInfo`, dual-timer state machine, power plan switching |
| `CpuDetector.cs` | `PowerPlanDetector` | Runtime detection of installed power plans via `powercfg /list` |
| `Constants.cs` | `Constants` | Centralised repository of Windows power plan GUIDs |
| `About_Window.cs` | `About_Window` | Informational modal dialog |
| `Program.cs` | `Program` | Application entry point (`STAThread`) |

---

## 6. Features & Functional Requirements

### FR-01 — Automatic Idle Detection

- The application monitors user input using the Win32 `GetLastInputInfo` API.
- After **90 seconds** of no keyboard or mouse activity, the system is considered idle.
- On idle detection: switch the active power plan to **Energy Saver**.
- The idle timer stops; the input-monitoring timer starts.

### FR-02 — Automatic Activity Restore

- When the input-monitoring timer fires (every **5 seconds**) and detects new input:
  - Switch the power plan back to the optimal high-performance plan.
  - Stop the input-monitoring timer.
  - Restart the idle detection timer.

### FR-03 — Adaptive High-Performance Plan Selection

- At startup and at each plan-switch decision point, `PowerPlanDetector.GetOptimalHighPerformancePlan()` is called.
- **Priority 1:** 1usmus Ryzen Universal profile (`fcaac3f2-997a-4fdb-8e30-c4fb6df29398`) — used if installed.
- **Priority 2:** Ultimate Performance (`e9a42b02-d5df-448d-aa00-03f14749e6c0`) — used as fallback.
- Detection is performed by scanning the output of `powercfg /list`.

### FR-04 — Energy Saver CPU Cap Enforcement

- On application startup and when management is re-enabled:
  - Read current AC and DC maximum processor state for the Energy Saver plan.
  - If either value differs from **50 %**, update via `powercfg /setacvalueindex` and `/setdcvalueindex`.
  - UAC elevation is requested automatically via `Verb = "runas"` if the process is not already elevated.
  - If values are already at 50 %, no action is taken (no unnecessary UAC prompt).

### FR-05 — Enable / Disable Management Toggle

- The main window exposes a **checkbox** (label: "System is managed" / "System is unmanaged").
- **Checked:** Idle detection is active; power plans switch automatically; Energy Saver CPU cap is enforced.
- **Unchecked:** Auto-switching is paused; the high-performance plan is activated immediately.
- The state is persisted via `Properties.Settings` across restarts.
- On application start, the state is always reset to **Enabled** (`true`).

### FR-06 — System Tray Integration

- The application always shows a tray icon while running.
- **Minimise / Close:** The window hides to tray (does not exit).
- **Double-click tray icon:** Restores the main window.
- **Right-click context menu:**
  - "Show Window" — restores the main window.
  - "Exit" — cleanly disposes `IdleChecker`, stops all timers, and exits.

### FR-07 — First-Run Welcome Message

- On the very first launch (`Settings.FirstRun == 1`), a `MessageBox` informs the user that the app is minimised to tray and auto-starts with Windows.
- The flag is set to `0` and saved immediately so the message only ever appears once.

### FR-08 — Resource Management & Cleanup

- `IdleChecker` implements `IDisposable`.
- On disposal: both timers are stopped, event handlers detached, and timers disposed.
- `Form1` calls `idleChecker.Dispose()` in both `OnFormClosing` (non-user-close path) and `ExitToolStripMenuItem_Click`.

---

## 7. Non-Functional Requirements

| ID | Category | Requirement |
|---|---|---|
| NFR-01 | Performance | Negligible CPU usage at idle — timer-driven, no busy-wait polling. |
| NFR-02 | Memory | Resident memory footprint must remain below 50 MB at all times. |
| NFR-03 | Reliability | Timer resources must be correctly disposed on exit to prevent handle leaks. |
| NFR-04 | Startup Time | Application must reach tray-visible state within 3 seconds on modern hardware. |
| NFR-05 | Compatibility | Must function correctly on Windows 7 SP1 through Windows 11. |
| NFR-06 | Security | UAC elevation only requested for Energy Saver CPU cap write. All other operations are unprivileged. |
| NFR-07 | Maintainability | All power plan GUIDs must be defined exclusively in `Constants.cs` — no magic strings elsewhere. |

---

## 8. UI / UX Requirements

### Main Window

- **Fixed size** — not resizable (`FormBorderStyle.FixedSingle`, no Maximise box).
- **Positioned** at bottom-right of the primary screen (offset: 192 px from right, 100 px from bottom) on launch.
- Opens briefly on launch then hides to tray; on subsequent launches it remains hidden.

### System Tray

- Tray icon is always visible while the application is running.
- Balloon tip or tooltip may optionally describe the current state.

### About Window

- Modal dialog, fixed size, centred on screen.
- Displays application information and usage notes.

### Checkbox States

| State | Label |
|---|---|
| Checked | "System is managed" |
| Unchecked | "System is unmanaged" |

---

## 9. Power Plans Reference

| Constant | GUID | Description |
|---|---|---|
| `HighPerformance` | `8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c` | Windows built-in High Performance |
| `UltimatePerformance` | `e9a42b02-d5df-448d-aa00-03f14749e6c0` | Windows built-in Ultimate Performance |
| `EnergySaver` | `a1841308-3541-4fab-bc81-f71556f20b4a` | Windows built-in Energy Saver |
| `RyzenUniversal` | `fcaac3f2-997a-4fdb-8e30-c4fb6df29398` | 1usmus Ryzen Universal (third-party, optional) |

**Selection Logic:**

```
if RyzenUniversal is installed  =>  activePerformancePlan = RyzenUniversal
else                            =>  activePerformancePlan = UltimatePerformance
```

---

## 10. Settings & Persistence

Managed via `Properties.Settings` (user-scoped):

| Key | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Whether automatic power plan management is active. Always reset to `true` on launch. |
| `FirstRun` | `int` | `1` | Set to `0` after the first-launch welcome message is shown. |

---

## 11. Known Limitations & Risks

| # | Risk / Limitation | Mitigation |
|---|---|---|
| 1 | **UAC prompt on first run** — Energy Saver CPU cap enforcement requires elevation. | Cap only set if value differs from 50 %; prompt is skipped when already aligned. |
| 2 | **RyzenUniversal not installed** — App falls back to Ultimate Performance, which may not exist on all Windows SKUs. | Fallback chain can be extended; user can install the profile manually. |
| 3 | **`Environment.TickCount` wrap-around** — On systems running for ~49.7 days, signed `TickCount` rolls over. | Idle time calculation may briefly misfire; mitigated by migrating to `TickCount64`. |
| 4 | **No logging** — Errors in power plan switching are surfaced only via `MessageBox`. | Optional structured logging is listed in the backlog. |
| 5 | **Single-monitor positioning** — Window placement uses `Screen.PrimaryScreen` only. | Acceptable for current scope; multi-monitor support is a backlog item. |

---

## 12. Future Enhancements (Backlog)

| Priority | Feature |
|---|---|
| High | Replace `Environment.TickCount` with `Environment.TickCount64` to prevent 49-day wrap-around |
| High | Make idle timeout (90 s) and input-check interval (5 s) user-configurable via Settings UI |
| Medium | Add structured file logging for power plan change events |
| Medium | Show tray tooltip / balloon tip reflecting current plan name and managed state |
| Medium | Add unit tests for `PowerPlanDetector` and idle calculation logic |
| Low | Multi-monitor window positioning awareness |
| Low | Microsoft Store packaging validation (`PPM_Setup_Project` assets & paths) |
| Low | UI feedback (status label or tray icon change) when Energy Saver CPU cap is adjusted |

---

## 13. Glossary

| Term | Definition |
|---|---|
| **Power Plan** | A Windows configuration profile controlling the balance between performance and energy consumption. |
| **GUID** | Globally Unique Identifier — used by Windows to reference specific power plans via `powercfg`. |
| **Idle** | State in which no keyboard or mouse input has been recorded for >= 90 seconds. |
| **UAC** | User Account Control — Windows security feature that prompts for elevation before privileged operations. |
| **1usmus Ryzen Universal** | A third-party optimised power plan for AMD Ryzen processors, authored by 1usmus. |
| **`powercfg`** | Windows built-in command-line utility for managing power configurations. |
| **`GetLastInputInfo`** | Win32 API that returns the tick count of the last keyboard or mouse input event. |
| **STAThread** | Single-Threaded Apartment — required COM threading model for Windows Forms applications. |

---

*End of Document*
