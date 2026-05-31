# Refactoring Report

## Power-Plan Manager — Code Quality & Technical Debt Analysis

**Generated:** 2026-05-16  
**Scope:** Power-Plan_Manager-Take_8.csproj  
**Target Framework:** .NET 8 / Windows Forms  
**Status:** Identifies issues across code quality, performance, security, and maintainability.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Critical Issues (Must Fix)](#critical-issues-must-fix)
3. [High Priority (Should Fix)](#high-priority-should-fix)
4. [Medium Priority (Nice to Fix)](#medium-priority-nice-to-fix)
5. [Low Priority (Consider)](#low-priority-consider)
6. [Technical Debt Summary](#technical-debt-summary)

---

## Executive Summary

The codebase is **functionally sound** and implements the core idle-detection and power-plan-switching logic correctly. However, several code-quality, testability, and maintainability issues have been identified. This report prioritises them for future refactoring cycles.

**Severity Breakdown:**
- **Critical:** 2 issues
- **High:** 5 issues
- **Medium:** 6 issues
- **Low:** 4 issues

---

## Critical Issues (Must Fix)

### CRIT-01: `Environment.TickCount` Wrap-Around Bug

**File(s):** `IdleChecker.cs`  
**Severity:** CRITICAL  
**Impact:** On systems running continuously for ~49.7 days, the signed 32-bit `TickCount` overflows, causing idle-time calculation to become incorrect.

**Current Code:**
```csharp
uint idleTime = ((uint)Environment.TickCount - lastInputInfo.dwTime) / 1000;
```

**Problem:**
- `Environment.TickCount` is a signed `int` (32-bit).
- It wraps from `Int32.MaxValue` (2,147,483,647 ms ≈ 24.8 days) back to `Int32.MinValue`.
- Casting to `uint` does not prevent the wrap-around; the underlying tick count still rolls.
- Long-running systems will experience spurious idle/activity transitions after 49.7 days of uptime.

**Recommended Fix:**
Replace with `Environment.TickCount64` (introduced in .NET Standard 2.0, available in .NET 8):

```csharp
long idleTime = (Environment.TickCount64 - (long)lastInputInfo.dwTime) / 1000;
```

**Testing:**
- Unit test: Simulate tick counts near `Int32.MaxValue` and verify the calculation remains correct after wrap.

**Effort:** 15 minutes  
**Risk:** Minimal — direct replacement, no logic change.

---

### CRIT-02: Bare `catch` Blocks Swallowing Exceptions

**File(s):** `Form1.cs`, `IdleChecker.cs`  
**Severity:** CRITICAL  
**Impact:** Silent failures make debugging and long-term maintenance difficult. Users receive no feedback when power plan switching fails.

**Instances:**

1. **Form1.cs** — Constructor and `EnsurePowerSaverProcessorState`:
```csharp
try { EnsurePowerSaverProcessorState(50); } catch { }
```

2. **IdleChecker.cs** — Dispose method:
```csharp
catch { }
```

3. **Form1.cs** — `OnFormClosing`:
```csharp
try { idleChecker?.Dispose(); } catch { }
```

**Problem:**
- Exceptions are silently ignored without logging, re-throwing, or user notification.
- No distinction between expected (e.g., process timeout) and unexpected errors (e.g., disk full, out of memory).
- Reduces observability; users may be unaware the application is not functioning as intended.

**Recommended Fix:**

Option A (Minimal): Log to a file or event log:
```csharp
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"Power Saver state enforcement failed: {ex.Message}");
    // Optionally: log to event log or file
}
```

Option B (Best Practice): Use a dedicated exception handler:
```csharp
private void SafeEnsurePowerSaverState(int targetPercent)
{
    try
    {
        EnsurePowerSaverProcessorState(targetPercent);
    }
    catch (Exception ex)
    {
        LogException("EnsurePowerSaverProcessorState", ex);
    }
}
```

**Effort:** 30 minutes (add logging infrastructure, update all catch blocks)  
**Risk:** Low — improves observability without changing logic.

---

## High Priority (Should Fix)

### HIGH-01: Hardcoded Idle & Input-Check Timeouts

**File(s):** `IdleChecker.cs`  
**Severity:** HIGH  
**Impact:** Users cannot adjust idle detection sensitivity without recompiling. Useful for testing and personalisation.

**Current Code:**
```csharp
idleCheckTimer.Interval = 90 * 1000;        // 90 seconds
userInputCheckTimer.Interval = 5 * 1000;    // 5 seconds
```

**Problem:**
- Hard-coded to 90 seconds idle + 5 seconds input check.
- No Settings UI or configuration mechanism to adjust these.
- Different users may want different thresholds (e.g., idle-out after 2 minutes vs. 30 seconds).

**Recommended Fix:**

1. Add settings keys to `Properties.Settings`:
   ```
   IdleTimeoutSeconds (int, default: 90)
   InputCheckIntervalSeconds (int, default: 5)
   ```

2. Update `IdleChecker` constructor:
```csharp
public IdleChecker()
{
    int idleTimeout = Properties.Settings.Default.IdleTimeoutSeconds ?? 90;
    int inputCheck = Properties.Settings.Default.InputCheckIntervalSeconds ?? 5;
    
    idleCheckTimer.Interval = idleTimeout * 1000;
    userInputCheckTimer.Interval = inputCheck * 1000;
    // ...
}
```

3. Optionally: Add Settings UI tab to `Form1` for adjustment.

**Effort:** 45 minutes (add settings, update IdleChecker, optional UI)  
**Risk:** Low — backward-compatible with sensible defaults.

---

### HIGH-02: No Input Validation for Power Plan GUIDs

**File(s):** `Constants.cs`, `PowerPlanDetector.cs`, `IdleChecker.cs`  
**Severity:** HIGH  
**Impact:** If a GUID is malformed, errors occur at runtime without clear feedback. Hard to trace to the source.

**Current Code:**
```csharp
public const string RyzenUniversal = "fcaac3f2-997a-4fdb-8e30-c4fb6df29398";
// No validation that this is a valid GUID format
```

**Problem:**
- GUIDs are passed as strings directly to `powercfg` without validation.
- A typo in `Constants.cs` would only surface when the plan is not found.
- No early-error detection; failure is silent until the user notices the power plan doesn't switch.

**Recommended Fix:**

1. Validate GUIDs in `Constants` or at app startup:
```csharp
public static class Constants
{
    private static Guid ValidateGuid(string guidString, string name)
    {
        if (!Guid.TryParse(guidString, out var guid))
            throw new FormatException($"Invalid GUID for {name}: {guidString}");
        return guid;
    }
    
    public static readonly Guid HighPerformance = 
        ValidateGuid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "HighPerformance");
    
    // ...
}
```

2. Or, validate during `IsPowerPlanAvailable`:
```csharp
public static bool IsPowerPlanAvailable(string powerPlanGuid)
{
    if (!Guid.TryParse(powerPlanGuid, out _))
        throw new ArgumentException($"Invalid GUID format: {powerPlanGuid}");
    // ...
}
```

**Effort:** 20 minutes  
**Risk:** Low — raises early errors if GUIDs are malformed.

---

### HIGH-03: Synchronous Process Calls Blocking UI Thread

**File(s):** `Form1.cs`, `IdleChecker.cs`, `PowerPlanDetector.cs`  
**Severity:** HIGH  
**Impact:** Process calls (especially with UAC prompts) can freeze the UI for several seconds. Poor user experience.

**Current Code:**
```csharp
var psi = new ProcessStartInfo("powercfg", $"/setactive {powerPlanGuid}")
{
    CreateNoWindow = true,
    UseShellExecute = false
};
Process.Start(psi);  // No wait, no timeout — fire and forget
```

Also:
```csharp
Process.Start(psi)?.WaitForExit(3000);  // Blocking wait
```

**Problem:**
- Power plan switches happen on the idle timer (UI thread for `System.Windows.Forms.Timer`).
- `powercfg` commands can take 1–5 seconds, especially with UAC.
- `WaitForExit(3000)` blocks the timer thread, causing stutters and unresponsive UI.
- No timeout handling — if a process hangs, the application hangs.

**Recommended Fix:**

1. Move power plan switching to a background thread:
```csharp
public void ChangePowerPlan(string powerPlanGuid)
{
    if (disposed) return;
    
    Task.Run(async () =>
    {
        try
        {
            await ChangePowerPlanAsync(powerPlanGuid);
        }
        catch (Exception ex)
        {
            LogException("ChangePowerPlan", ex);
        }
    });
}

private async Task ChangePowerPlanAsync(string powerPlanGuid)
{
    var psi = new ProcessStartInfo("powercfg", $"/setactive {powerPlanGuid}")
    {
        CreateNoWindow = true,
        UseShellExecute = false
    };
    
    using (var process = Process.Start(psi))
    {
        if (process == null) return;
        await Task.Run(() => process.WaitForExit(3000));
    }
}
```

2. Use `ProcessStartInfo` with proper timeout handling:
```csharp
using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
{
    var task = Task.Run(() => process.WaitForExit());
    if (!await task.ConfigureAwait(false)) 
        process.Kill();  // Force-terminate if timeout
}
```

**Effort:** 1–2 hours (refactor to async, add CancellationToken support)  
**Risk:** Moderate — introduces async/await; requires careful thread-safety review.

---

### HIGH-04: No Structured Logging or Event Log Integration

**File(s):** All  
**Severity:** HIGH  
**Impact:** No audit trail of power plan changes. Difficult to troubleshoot user issues or verify the app is working.

**Current Code:**
```csharp
catch (Exception ex)
{
    MessageBox.Show($"Failed to change power plan: {ex.Message} - " +
        $"Please contact me for a solution: workersoft@gmx.com");
}
```

**Problem:**
- Errors are surfaced only via `MessageBox` — no permanent record.
- No way to audit power plan changes (when they occurred, which plan, why).
- For a background utility, logging to Windows Event Log is a standard practice.
- No diagnostic data for troubleshooting long-running stability issues.

**Recommended Fix:**

1. Add a simple file-based logger (or use `System.Diagnostics.EventLog`):
```csharp
public static class Logger
{
    private static readonly string LogFile = 
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PowerPlanManager",
            "log.txt"
        );
    
    public static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogFile);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            
            File.AppendAllText(LogFile, 
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { /* Prevent logging errors from crashing the app */ }
    }
    
    public static void LogException(string context, Exception ex)
    {
        Log($"ERROR [{context}]: {ex.Message}\n  Stack: {ex.StackTrace}");
    }
}
```

2. Call in all exception handlers:
```csharp
catch (Exception ex)
{
    Logger.LogException("ChangePowerPlan", ex);
}
```

3. Optional: Create a "View Log" button in the About window.

**Effort:** 45 minutes  
**Risk:** Low — logging is non-critical; failures in logging should not break the app.

---

### HIGH-05: No Unit Tests

**File(s):** All  
**Severity:** HIGH  
**Impact:** Changes to idle detection or power plan selection logic are not validated. Risk of regressions.

**Current Code:**
- No test project exists.

**Problem:**
- `PowerPlanDetector.IsPowerPlanAvailable()` parses `powercfg /list` output; format changes would break silently.
- Idle-time calculations (especially with the TickCount64 migration) should be unit-tested.
- No integration tests for power plan switching.

**Recommended Fix:**

1. Create a new test project:
```bash
dotnet new mstest -n Power-Plan_Manager-Take_8.Tests
```

2. Add tests for `PowerPlanDetector`:
```csharp
[TestClass]
public class PowerPlanDetectorTests
{
    [TestMethod]
    public void GetOptimalHighPerformancePlan_ReturnRyzenIfAvailable()
    {
        // Mock powercfg /list output
        // Assert: returns RyzenUniversal GUID
    }
    
    [TestMethod]
    public void GetOptimalHighPerformancePlan_ReturnUltimateIfRyzenNotAvailable()
    {
        // Mock powercfg /list output (without Ryzen)
        // Assert: returns UltimatePerformance GUID
    }
}
```

3. Add tests for idle-time logic:
```csharp
[TestClass]
public class IdleCheckerTests
{
    [TestMethod]
    public void IdleTime_AfterTickCountWrap_CalculatedCorrectly()
    {
        // Simulate dwTime near Int32.MaxValue
        // Assert: idle calculation still correct after wrap
    }
}
```

**Effort:** 2–3 hours (create test project, write initial test suite)  
**Risk:** Low — tests are additive, no code changes required initially.

---

## Medium Priority (Nice to Fix)

### MED-01: Regex-Based Parsing of `powercfg` Output

**File(s):** `Form1.cs` — `GetPowerSettingValue`  
**Severity:** MEDIUM  
**Impact:** Fragile parsing; changes to `powercfg` output format could break the feature silently.

**Current Code:**
```csharp
var match = Regex.Match(output, @":\s*(\d+)");
if (match.Success && int.TryParse(match.Groups[1].Value, out int val))
    return val;
```

**Problem:**
- Regex is very broad (`:\s*(\d+)`), which could match unintended lines if `powercfg` output format changes.
- No fallback or error message if parsing fails; returns `-1` silently.
- Brittle approach; more robust parsing would be beneficial.

**Recommended Fix:**

1. Improve regex to match the expected line pattern more precisely:
```csharp
var match = Regex.Match(output, @"Current\s+(?:AC|DC)\s+Power\s+Setting\s+Index:\s*(\d+)", 
    RegexOptions.IgnoreCase);
```

2. Add logging if parsing fails:
```csharp
if (!match.Success)
{
    Logger.Log($"Failed to parse powercfg output for AC={ac}. Raw output:\n{output}");
    return -1;
}
```

3. Consider a dedicated parser class if logic becomes more complex.

**Effort:** 20 minutes  
**Risk:** Low — improves robustness without changing behavior.

---

### MED-02: Screen Positioning Magic Numbers

**File(s):** `Form1.cs` — Constructor  
**Severity:** MEDIUM  
**Impact:** Hard-coded offsets are inflexible. Multi-monitor support is impossible.

**Current Code:**
```csharp
int offsetX = 192;
int offsetY = 100;
this.Location = new Point(resolution.Width - this.Width - offsetX, 
                          resolution.Height - this.Height - offsetY);
```

**Problem:**
- Offsets are magic numbers; no explanation for why 192 & 100 were chosen.
- Doesn't account for taskbar height, DPI scaling, or multi-monitor setups.
- Future developers won't know how to adjust these sensibly.

**Recommended Fix:**

1. Move to configurable settings:
```csharp
public enum WindowPositionMode
{
    BottomRight,   // Bottom-right corner (default)
    TopRight,
    Center,
    SystemDefault  // Let Windows decide
}
```

2. Add settings:
```csharp
WindowPositionMode (default: BottomRight)
OffsetX (default: 192)
OffsetY (default: 100)
```

3. Improve positioning logic:
```csharp
private void PositionWindow()
{
    var screen = Screen.FromPoint(Cursor.Position) ?? Screen.PrimaryScreen;
    var workingArea = screen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
    
    int x = workingArea.Right - this.Width - Properties.Settings.Default.OffsetX;
    int y = workingArea.Bottom - this.Height - Properties.Settings.Default.OffsetY;
    
    this.Location = new Point(
        Math.Max(workingArea.Left, x),
        Math.Max(workingArea.Top, y)
    );
}
```

**Effort:** 30 minutes  
**Risk:** Low — improves positioning logic without breaking existing behavior.

---

### MED-03: Settings Always Reset to Enabled on Startup

**File(s):** `Form1.cs` — Constructor  
**Severity:** MEDIUM  
**Impact:** User's preference to disable management is lost on every restart. Counter-intuitive behaviour.

**Current Code:**
```csharp
// Reset the app state to 'Enabled'
// In case it was disabled when the app closed..
// ..or the system restarted.            
Properties.Settings.Default.Enabled = true;
Properties.Settings.Default.Save();
```

**Problem:**
- Comment explains the intention (restore state after a crash), but implementation is too broad.
- If a user explicitly disables management and restarts, it silently re-enables.
- This is likely unintended behaviour.

**Recommended Fix:**

Option A: Only reset if the app detects a crash or abnormal shutdown:
```csharp
bool wasAbnormalShutdown = DetectAbnormalShutdown();
if (wasAbnormalShutdown)
{
    Properties.Settings.Default.Enabled = true;
    Properties.Settings.Default.Save();
}
```

Option B: Remove the auto-reset entirely and preserve user preference:
```csharp
// Load the saved state; if this is the first run, set to enabled
if (Properties.Settings.Default.FirstRun == 1)
{
    Properties.Settings.Default.Enabled = true;
}
// Otherwise, use the saved state
```

Option C (Recommended): Add a "last known good" checkpoint:
```csharp
public static class AppLifecycle
{
    private static readonly string StateFile = 
        Path.Combine(AppDataPath, "state.txt");
    
    public static void RecordState()
    {
        File.WriteAllText(StateFile, Environment.TickCount64.ToString());
    }
    
    public static bool IsUncleanShutdown()
    {
        if (!File.Exists(StateFile)) return false;
        
        // If app wrote state file, it exited cleanly
        // If state file is older than 10 seconds, app crashed
        var fileTime = File.GetLastWriteTime(StateFile);
        return DateTime.Now.Subtract(fileTime).TotalSeconds > 10;
    }
}
```

**Effort:** 30 minutes  
**Risk:** Medium — changes user-facing behaviour; needs clear documentation.

---

### MED-04: Error Messages Lack Actionable Context

**File(s):** `IdleChecker.cs`, `Form1.cs`  
**Severity:** MEDIUM  
**Impact:** Users cannot self-troubleshoot. Generic error messages are not helpful.

**Current Code:**
```csharp
MessageBox.Show($"Failed to change power plan: {ex.Message} - " +
    $"Please contact me for a solution: workersoft@gmx.com");
```

**Problem:**
- Message asks user to contact developer rather than providing actionable steps.
- No information about which plan was being switched to.
- No context about the underlying error (permissions, plan not found, etc.).

**Recommended Fix:**

```csharp
private string GetErrorMessage(Exception ex, string powerPlanGuid)
{
    if (ex is Win32Exception we)
    {
        // Access denied — likely permissions issue
        if (we.NativeErrorCode == 5)
            return "Access denied. Please run as Administrator or grant the app " +
                   "elevated privileges via Group Policy.";
    }
    
    if (ex is FileNotFoundException)
        return "The 'powercfg' command was not found. This may indicate a corrupted " +
               "Windows installation. Please repair Windows or reinstall .NET.";
    
    if (ex is TimeoutException)
        return $"The power plan switch timed out. The plan '{powerPlanGuid}' may not exist " +
               "on this system. Run 'powercfg /list' in Command Prompt to check available plans.";
    
    return $"An unexpected error occurred: {ex.Message}. Please check the application log " +
           $"(in %APPDATA%\\PowerPlanManager\\log.txt) and contact support.";
}
```

**Effort:** 45 minutes  
**Risk:** Low — improves UX without changing logic.

---

### MED-05: No Input Handling During Power Plan Switch

**File(s):** `IdleChecker.cs`  
**Severity:** MEDIUM  
**Impact:** If a user provides input during the few seconds while `powercfg` is executing, the behaviour is undefined.

**Current Code:**
```csharp
public void ChangePowerPlan(string powerPlanGuid)
{
    try
    {
        var psi = new ProcessStartInfo("powercfg", $"/setactive {powerPlanGuid}")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        Process.Start(psi);  // Fire and forget; timers keep running
    }
    catch { }
}
```

**Problem:**
- While `powercfg` is executing, the idle timers are still running.
- User input could trigger a second power plan switch before the first completes.
- No synchronisation; both switches could execute concurrently, causing race conditions.

**Recommended Fix:**

Add a "switch in progress" flag:
```csharp
private bool isPlanSwitchInProgress = false;

public void ChangePowerPlan(string powerPlanGuid)
{
    if (disposed || isPlanSwitchInProgress) return;
    
    isPlanSwitchInProgress = true;
    Task.Run(async () =>
    {
        try
        {
            var psi = new ProcessStartInfo("powercfg", $"/setactive {powerPlanGuid}")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using (var process = Process.Start(psi))
            {
                process?.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            Logger.LogException("ChangePowerPlan", ex);
        }
        finally
        {
            isPlanSwitchInProgress = false;
        }
    });
}
```

**Effort:** 20 minutes  
**Risk:** Low — adds a simple guard; prevents race conditions.

---

### MED-06: Limited Documentation of State Machine

**File(s):** `IdleChecker.cs`  
**Severity:** MEDIUM  
**Impact:** The two-timer state machine is not immediately obvious to new developers.

**Current Code:**
```csharp
private void IdleCheckTimer_Tick(object? sender, EventArgs e)
{
    // ... calculation ...
    if (idleTime >= idleCheckTimer.Interval / 1000 && Properties.Settings.Default.Enabled == true)
    {                
        ChangePowerPlan(Constants.EnergySaver);
        idleCheckTimer.Stop();
        userInputCheckTimer.Start();
    }            
}
```

**Problem:**
- The two-timer design (idle detection vs. input monitoring) is not documented.
- New developers might not understand why both timers exist.
- State transitions are implicit and scattered across two methods.

**Recommended Fix:**

Add class-level documentation:
```csharp
/// <summary>
/// Monitors system idle state and automatically switches Windows power plans.
/// 
/// STATE MACHINE:
///   [IDLE_DETECTION] --idle--> [INPUT_MONITORING] --activity--> [IDLE_DETECTION]
///   
/// When idle (90 s no input):
///   1. Stop idleCheckTimer
///   2. Switch power plan to EnergySaver
///   3. Start userInputCheckTimer (checks every 5 s for input)
/// 
/// When activity detected during INPUT_MONITORING:
///   1. Stop userInputCheckTimer
///   2. Switch power plan to high-performance (Ryzen or Ultimate)
///   3. Start idleCheckTimer
/// </summary>
public class IdleChecker : IDisposable
{
    // ...
}
```

**Effort:** 15 minutes  
**Risk:** None — documentation only.

---

## Low Priority (Consider)

### LOW-01: Enum for Application Permissions State

**File(s):** `Form1.cs`  
**Severity:** LOW  
**Impact:** Slight readability improvement. Minimal functional impact.

**Recommendation:**
```csharp
public enum ElevationState
{
    NotElevated,
    Elevated,
    Unknown
}

private static ElevationState GetElevationState()
{
    using (var identity = WindowsIdentity.GetCurrent())
    {
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator) 
            ? ElevationState.Elevated 
            : ElevationState.NotElevated;
    }
}
```

**Effort:** 15 minutes  
**Risk:** None — improves code clarity.

---

### LOW-02: Constants.cs Could Validate Itself at Load

**File(s):** `Constants.cs`  
**Severity:** LOW  
**Impact:** Early error detection. Helps catch typos during startup.

**Recommendation:**
```csharp
public static class Constants
{
    static Constants()
    {
        // Validate all GUIDs at runtime
        var guids = new[] { HighPerformance, UltimatePerformance, EnergySaver, RyzenUniversal };
        foreach (var guid in guids)
        {
            if (!Guid.TryParse(guid, out _))
                throw new InvalidOperationException($"Invalid GUID in Constants: {guid}");
        }
    }
}
```

**Effort:** 10 minutes  
**Risk:** None — static constructor provides early validation.

---

### LOW-03: Nullable Reference Type Strictness

**File(s):** All  
**Severity:** LOW  
**Impact:** Prevents null-reference exceptions at runtime.

**Current Code:**
```csharp
idleChecker?.Dispose(); // Defensive null-coalescing
```

**Recommendation:**
- Enable `<Nullable>enable</Nullable>` in `.csproj` (already enabled).
- Run the compiler in strict mode to catch all potential null issues:
  ```csharp
  #pragma warning disable CS8602 // Dereference of a possibly null reference
  ```
- Resolve all warnings in future refactoring cycles.

**Effort:** 1–2 hours (across multiple files)  
**Risk:** Low — improves null-safety without changing logic.

---

### LOW-04: Configurable Power Plan Names Display in UI

**File(s):** `Form1.cs`  
**Severity:** LOW  
**Impact:** UX improvement; users can see which plan is active.

**Recommendation:**
```csharp
private string GetPowerPlanName(string guid)
{
    return guid switch
    {
        Constants.EnergySaver => "Energy Saver",
        Constants.UltimatePerformance => "Ultimate Performance",
        Constants.RyzenUniversal => "Ryzen Universal",
        _ => "Unknown Plan"
    };
}

private void UpdateStatusLabel(string activeGuid)
{
    statusLabel.Text = $"Active Plan: {GetPowerPlanName(activeGuid)}";
}
```

**Effort:** 20 minutes  
**Risk:** Low — adds informational UI element.

---

## Technical Debt Summary

| Category | Count | Estimated Effort | Business Impact |
|---|---|---|---|
| **Critical** | 2 | 1–2 hours | High — wrap-around bug, silent failures |
| **High** | 5 | 4–6 hours | Medium–High — testability, UX, reliability |
| **Medium** | 6 | 2.5–3 hours | Medium — code quality, robustness |
| **Low** | 4 | 1–1.5 hours | Low — nice-to-have improvements |
| **TOTAL** | **17** | **8.5–12.5 hours** | **Medium overall** |

---

## Recommended Refactoring Roadmap

### Phase 1 — Critical Issues (Required)
1. **CRIT-01:** Replace `TickCount` with `TickCount64`
2. **CRIT-02:** Add basic logging; remove bare `catch` blocks

**Timeline:** 1–2 sprints  
**Impact:** Fixes wrap-around bug; improves observability.

---

### Phase 2 — High Priority (Next Sprint)
3. **HIGH-01:** Make idle/input intervals configurable
4. **HIGH-02:** Add GUID validation
5. **HIGH-04:** Add structured logging

**Timeline:** 2–3 sprints  
**Impact:** Improves user experience, maintainability, and testability.

---

### Phase 3 — Medium Priority (Backlog)
6. **MED-01** through **MED-06:** Code quality improvements

**Timeline:** 3–4 sprints  
**Impact:** Improves robustness and developer experience.

---

### Phase 4 — Low Priority (Polish)
7. **LOW-01** through **LOW-04:** Minor enhancements

**Timeline:** As time permits  
**Impact:** Code polish; minimal functional change.

---

## Conclusion

The codebase is **production-ready** for its current scope but would benefit from addressing the **critical and high-priority issues** before any major feature additions. The most impactful improvements are:

1. Migrating to `TickCount64` to prevent the 49-day wrap-around bug.
2. Adding structured logging to improve observability.
3. Creating a test suite to validate power plan detection logic.

All other items are enhancements that can be prioritised based on team bandwidth and user feedback.

---

*End of Refactoring Report*
