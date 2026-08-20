**Start-up Power Plan Bug Solution Strategy**

To address the bug where the application defaults to "Energy Saver" mode upon restart, you should leverage the existing **PowerPlanDetector** logic to explicitly select a high-performance profile rather than capturing whatever plan happens to be active at boot.  
The following strategy is based on the application's current architecture and identified prioritized power plans:

### 1\. Shift from "Active Capture" to "Explicit Selection"

Currently, the application captures the "current active plan" at startup 1\. If the system shut down while idle, it restarts in **Energy Saver** mode, causing the app to incorrectly treat it as the "Power Mode" default.  
**Proposed Fix:** Upon application initialization in Form1.cs or Program.cs, the app must ignore the currently active system plan and instead call PowerPlanDetector.GetOptimalHighPerformancePlan(). This method is already designed to scan the system via powercfg /list and return the best available performance profile.

### 2\. Implement the High-Performance Priority Chain

The strategy should strictly follow the priority logic already defined in the technical documentation to ensure the most powerful plan is chosen:

1. **Priority 1: 1usmus Ryzen Universal** (fcaac3f2-997a-4fdb-8e30-c4fb6df29398) — Preferred for AMD Ryzen systems.  
2. **Priority 2: Ultimate Performance** (e9a42b02-d5df-448d-aa00-03f14749e6c0) — Built-in Windows high-end fallback.  
3. **Priority 3: High Performance** (8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c) — Standard Windows performance plan.

### 3\. Forced Restoration on Startup

To ensure the system does not remain in a throttled state if the app was closed/crashed during an idle period, the following steps should be added to the startup sequence:

* **Immediate Restore:** As soon as the app starts, it should use powercfg /setactive to force the system into the plan discovered by the GetOptimalHighPerformancePlan() method.  
* **Settings Integration:** Since the application is already configured to reset the "System is managed" state to **Enabled** (true) on every launch, this is the ideal trigger point to verify and set the high-performance plan.

### 4\. Technical Implementation Steps

* **Modify Form1.cs Constructor:** Instead of just initializing the IdleChecker, add a call to PowerPlanDetector to identify the correct performance GUID and set it as the active plan before the idle detection timers begin.  
* **Validation:** Use the existing IsPowerPlanAvailable logic to ensure the chosen GUID actually exists on the specific Windows installation before attempting to switch.  
* **Logging:** Ensure this startup plan selection is logged to %APPDATA%\\PowerPlanManager\\log.txt so that if the selection fails, there is a clear audit trail of what plan the app *attempted* to set.

By implementing this, the app will no longer "inherit" the Power Saving state from a previous session and will instead proactively enforce the user's desired high-performance state at every boot.  
