# Critical Bug Fix — Duplicate Energy Saver Power Plans

Date: 2026-07-03

Summary

- A critical bug caused the application to create a new duplicate "Energy Saver" (Power Saving) power plan on each idle activation and across restarts. Over time many redundant power-saving schemes accumulated on affected Windows systems.

Symptoms

- Hundreds of "Power Saver"/unnamed plans visible in `powercfg /list`.
- App created a duplicate scheme each time it triggered idle throttle.
- No UAC prompt required on deletion in most cases, but earlier attempts using `powercfg /delete` required elevation and were not reliable for unattended removal.

Root cause

- The app duplicated the system `Energy Saver` plan and modified processor throttle (50%). Duplicates were created and activated but not reliably deleted across app restarts and system crashes.
- Detection and cleanup formerly relied on friendly names. Many duplicate schemes had empty friendly names or were created without consistent names, so they were not detected or deleted.
- A persisted GUID value was not used initially, so duplicates from previous runs could remain.

Fix implemented (summary)

- Automatic redundant-scheme cleanup on startup (background): the app now enumerates all power schemes, probes unnamed schemes for the specific changes the app makes (50% processor max on AC/DC), and deletes matching duplicates using the `PowerDeleteScheme` API (no elevation required in typical cases).
- Persist created duplicate GUID to user settings when creating the throttle scheme, and attempt to remove that persisted duplicate on subsequent runs.
- Robust logging of detection, probe results, and deletion outcomes to `%APPDATA%/PowerPlanManager/log.txt` for diagnostics.
- Removed automatic elevation/UAC prompts. If API deletion fails the app logs GUIDs that could not be removed; no user prompts or elevation attempts occur.

Files changed

- `Power-Plan_Manager-2.0/IdleChecker.cs`
  - Added robust cleanup logic:
    - `RemoveRedundantEnergySaverPlans()` — enumerates schemes (`powercfg /list`), matches canonical names and unnamed candidates, probes unnamed schemes using `PowerReadACValueIndex` and `PowerReadDCValueIndex` for the 50% throttle signature, deletes duplicates with `PowerDeleteScheme`.
    - Persist/delete of temporary duplicate GUID in settings.
    - Extensive logging to help validate behavior and troubleshoot different system formats/localizations.
  - Added P/Invoke signatures: `PowerReadACValueIndex`, `PowerReadDCValueIndex`.
  - Removed elevated deletion flow (no `cmd.exe /runas` prompts). Elevated delete logic existed during debugging but was removed per product constraints.
  - Automatically invoked `RemoveRedundantEnergySaverPlans()` on startup (from `Form1`) so cleanup runs without user interaction.

- `Power-Plan_Manager-2.0/Properties/Settings.Designer.cs`
  - Added new user-scoped setting `IdleThrottleGuid` (string) to persist the GUID of the temporary throttle scheme created by the app.

- `Power-Plan_Manager-2.0/Form1.cs`
  - Called cleanup on startup after creating `IdleChecker`.

- `docs/critical-bug-fix.md` (this document)

How the cleanup works (high-level)

1. On startup the app captures the current active plan and instantiates `IdleChecker`.
2. `RemoveRedundantEnergySaverPlans()` runs in the background and calls `powercfg /list` to enumerate all schemes.
3. The method builds a candidate list by:
   - Selecting schemes whose friendly name matches "Power Saver"/"Energy Saver"/"PPM-Idle-Throttle".
   - Including unnamed schemes (except known canonical GUIDs) for probing.
4. For unnamed candidates the app reads the AC/DC processor maximum setting for the scheme using `PowerReadACValueIndex` and `PowerReadDCValueIndex`.
   - If either AC or DC value equals 50 (the throttle value used by the app), the scheme is considered a duplicate.
5. The app deletes duplicates using the `PowerDeleteScheme` API. Successfully deleted GUIDs return result 0 and are logged.
6. If API deletion fails for any GUIDs, those GUIDs are logged and the app does not prompt or elevate; they are skipped.

Why this approach

- Using the native `Power*` APIs (duplicate/delete/read) lets the app operate without elevation in the common case and is appropriate for Microsoft Store app constraints.
- Probing the processor-throttle setting identifies duplicates even when the friendly name is absent or localized.
- Persisting the created GUID allows quick cleanup of the single duplicate the app itself created if needed.

Logs and diagnostics

- All cleanup actions, probes, and results are logged to:
  - `%APPDATA%\PowerPlanManager\log.txt`
- Key log messages to look for:
  - `RemoveRedundantEnergySaverPlans: powercfg /list output` — raw enumeration.
  - `RemoveRedundantEnergySaverPlans: found entry guid='...' name='...'` — entry discovery.
  - `ReadAC/DC for {guid} -> r1=... ac=..., r2=... dc=...` — probe results.
  - `{guid} identified as throttle duplicate (50%).` — candidate marked for deletion.
  - `PowerDeleteScheme result for {guid}: 0` — successful deletion.
  - `Could not delete ... via API; skipping elevation` — indicates schemes that remain.

Observed behavior

- In testing and on affected systems, the API deletion removed many duplicate schemes without any UAC prompt.
- Example logs (abridged):
  - `fb188c23... identified as throttle duplicate (50%).`
  - `Attempting to delete redundant power scheme fb188c23... via API`
  - `PowerDeleteScheme result for fb188c23...: 0`

Verification steps

1. Run the app; it will perform cleanup in background on startup.
2. Check `%APPDATA%\PowerPlanManager\log.txt` for the cleanup lines described above.
3. Confirm the number of redundant power schemes by running in a Command Prompt:
   - `powercfg /list`
   - Re-run after the app startup to observe decreased count.

Rollback

- Revert the changes in `IdleChecker.cs` and `Form1.cs` and remove the `IdleThrottleGuid` setting from `Settings.Designer.cs`.
- Rebuild the project.

Caveats and future improvements

- Localization: friendly-name matching currently uses English substrings. If you need robust detection for localized friendly names, extend name matching using localized resources or always rely on the probe (more expensive).
- Additional integrity checks: the probe checks for the exact 50% throttle value. If future app versions change the throttle value, update the probe accordingly.
- Elevated fallback: currently omitted to avoid UAC prompts for Microsoft Store deployments. If a silent elevated fallback becomes acceptable for some distribution channels (non-Store), an elevated deletion path can be provided behind an advanced setting.
- UI visibility: the cleanup runs silently. If you later want a non-interactive telemetry or notification to inform users, add a one-time notification controlled by a setting.

Contact and notes

- Logs and diagnostic output are essential if some schemes still persist. If that occurs, collect `%APPDATA%\PowerPlanManager\log.txt` and a `powercfg /list` output and attach them for further analysis.


---

Generated by: GitHub Copilot

