# PowerPlan Manager

> Automatically switches your Windows power plan based on user activity � saving energy when you're away and restoring performance the moment you return.

[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](https://apps.microsoft.com/detail/9p87s5vf93vq?hl=en-US&gl=US)
[![Framework](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com)
[![Version](https://img.shields.io/badge/version-2.1.0-green)](https://github.com/pmetaxas-dev/PowerPlan_Manager_2026/releases)
[![Tests](https://img.shields.io/badge/tests-57%20passing-brightgreen)](#testing)
[![Store](https://img.shields.io/badge/Microsoft%20Store-Available-blue)](https://apps.microsoft.com/detail/9p87s5vf93vq?hl=en-US&gl=US)

### [Download on the Microsoft Store](https://apps.microsoft.com/detail/9p87s5vf93vq?hl=en-US&gl=US)

---

## Overview

**PowerPlan Manager** is a lightweight Windows system-tray application built with .NET 8 and WinForms. It monitors user input activity and automatically switches Windows power plans:

- **When idle** ? activates a custom throttled energy-saving plan (Energy Saver + 50% max CPU)
- **When active again** ? instantly restores your original power plan

No manual intervention needed. It runs silently in the background and starts automatically with Windows.

---

## Features

- ? **Automatic idle detection** � detects inactivity via last user input timestamp
- ? **CPU throttling on idle** � reuses one energy-saving plan with a 50% max processor state
- ? **Instant restore on activity** � detects input within 5 seconds and switches back
- ? **System tray operation** � minimizes to tray, never gets in your way
- ? **Vendor-plan support** � preserves a suitable active AMD, Intel, OEM, or user performance plan
- ? **Enable / Disable toggle** � checkbox to pause management without closing the app
- ? **Auto-start with Windows** � registered as a startup task via MSIX packaging
- ? **File-based logging** � timestamped log at `%APPDATA%\PowerPlanManager\log.txt`
- ? **Thread-safe async operations** � all power plan switches run off the UI thread

---

## How It Works

```
User is active
     ?
     ?
[idleCheckTimer] � fires after 90 seconds of inactivity
     ?
     ?
Find or create one reusable idle scheme ? cap CPU to 50% ? activate it
     ?
     ?
[userInputCheckTimer] � polls every 5 seconds for new input
     ?
     ?
Input detected ? restore the persisted normal power plan
     ?
     ?
[idleCheckTimer] restarts
```

---

## Architecture

```
PowerPlan_Manager_2026/
??? Power-Plan_Manager-2.0/          # Main WinForms application
?   ??? Form1.cs                     # Main window & tray icon logic
?   ??? IdleChecker.cs               # Core idle detection & power plan switching
?   ??? CpuDetector.cs               # Power plan availability detection
?   ??? Constants.cs                 # Power plan GUIDs (validated at startup)
?   ??? Logger.cs                    # Thread-safe file logging
?   ??? About_Window.cs              # About dialog
?   ??? Program.cs                   # Entry point
?   ??? Properties/
?       ??? Settings.Designer.cs     # Persistent user settings
?
??? Power-Plan_Manager-2.0.Tests/    # MSTest unit & integration tests (57 tests)
?   ??? ConstantsTests.cs
?   ??? IdleCheckerTests.cs
?   ??? PowerPlanDetectorTests.cs
?   ??? Form1Tests.cs
?   ??? EdgeCaseTests.cs
?   ??? IntegrationTests.cs
?
??? App_Packger_proj/                # MSIX packaging project
    ??? Package.appxmanifest         # App identity, capabilities, startup task
    ??? Images/                      # All required Store image assets
```

---

## Power Plan GUIDs

| Plan | GUID |
|------|------|
| High Performance | `8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c` |
| Ultimate Performance | `e9a42b02-d5df-448d-aa00-03f14749e6c0` |
| Energy Saver | `a1841308-3541-4fab-bc81-f71556f20b4a` |
| Ryzen Universal (1usmus) | `fcaac3f2-997a-4fdb-8e30-c4fb6df29398` |

> All GUIDs are validated at application startup via `Constants.cs` static constructor.

---

## Requirements

| Requirement | Minimum |
|-------------|---------|
| OS | Windows 10 (build 17763+) |
| Runtime | .NET 8.0 |
| Architecture | x86 or x64 |
| Windows SDK | 10.0.26100.0 (for building) |

---

## Installation

### Via Microsoft Store *(recommended)*
Search for **PowerPlan Manager** in the Microsoft Store, or click the link below:

**?? [https://apps.microsoft.com/detail/9p87s5vf93vq?hl=en-US&gl=US](https://apps.microsoft.com/detail/9p87s5vf93vq?hl=en-US&gl=US)**

### Via MSIX Package
1. Obtain the signed `App_Packger_proj_2.1.0.0_x86_x64.msixbundle` sideload build
2. Double-click to install
3. The app will appear in Start Menu and auto-start with Windows

---

## Building from Source

### Prerequisites
- Visual Studio 2022
- .NET 8 SDK
- Windows SDK 10.0.26100.0
- Windows Application Packaging Project workload

### Build (Debug)
```bash
dotnet build "Power-Plan_Manager-2.0\Power-Plan_Manager-Take_8.csproj"
```

### Build (Release)
```bash
dotnet build "Power-Plan_Manager-2.0\Power-Plan_Manager-Take_8.csproj" --configuration Release
```

### Build MSIX Store Package
```bash
msbuild App_Packger_proj\App_Packger_proj.wapproj ^
  /p:Configuration=Release ^
  /p:AppxBundle=Always ^
  /p:UapAppxPackageBuildMode=StoreUpload ^
  /p:AppxPackageSigningEnabled=false ^
  /p:Platform=x86
```
Output: `App_Packger_proj\AppPackages\App_Packger_proj_2.1.0.0_x86_x64_bundle.msixupload`

---

## Logging

*    Logs are saved inside this folder:
     %APPDATA%\PowerPlanManager\log.txt

## Testing

```bash
dotnet test "Power-Plan_Manager-2.0.Tests\Power-Plan_Manager-Take_8.Tests.csproj"
```

| Test Suite | Tests | Status |
|---|---|---|
| `ConstantsTests` | 13 | Pass |
| `PowerPlanDetectorTests` | 8 | Pass |
| `IdleCheckerTests` | 14 | Pass |
| `Form1Tests` | 12 | Pass |
| `EdgeCaseTests` | 5 | Pass |
| `PowerPlanLifecycleIntegrationTests` | 5 | Pass |
| **Total** | **57** | **All pass** |

---

## Configuration

Default values (defined in `IdleChecker.cs`):

| Setting | Default | Description |
|---------|---------|-------------|
| Idle timeout | 90 seconds | Time before switching to energy-saving plan |
| Input check interval | 5 seconds | How often to poll for user input when idle |

To override, add `IdleTimeoutSeconds` or `InputCheckIntervalSeconds` to the app's user settings.

---

## Logging

Logs are written to:
```
%APPDATA%\PowerPlanManager\log.txt
```

To view recent entries:
```powershell
Get-Content "$env:APPDATA\PowerPlanManager\log.txt" -Tail 20
```

To clear the log:
```powershell
Clear-Content "$env:APPDATA\PowerPlanManager\log.txt"
```

---

## Troubleshooting

### Power plans not switching
- Open `powercfg /list` in a terminal and verify the Energy Saver GUID `a1841308-3541-4fab-bc81-f71556f20b4a` is present
- Check the log file for errors
- Make sure the **Enable** checkbox in the main window is checked

### App not starting with Windows
- Reinstall via MSIX � the startup task is registered automatically by the package
- Check Task Manager ? Startup Apps for "Power-Plan Manager"

### App not visible
- Look for the icon in the **system tray** (bottom-right of taskbar, may be hidden under the arrow)
- Double-click the tray icon to show the main window

---

## Version History

| Version | Date | Notes |
|---------|------|-------|
| 2.1.0 | 2026 | Critical duplicate-plan cleanup, stable idle-plan reuse, locale-independent plan selection |
| 2.0.0 | 2026 | x86+x64 bundle, CPU throttle plan, .NET 8 |
| 1.5.3 | 2024 | Previous Store release |

---

## Author

**Panos Metaxas**
- Microsoft Store: [PowerPlan Manager](https://apps.microsoft.com/detail/9p87s5vf93vq?hl=en-US&gl=US)
- GitHub: [pmetaxas-dev](https://github.com/pmetaxas-dev)

---

## License

MIT License � see [LICENSE](LICENSE) for details.
