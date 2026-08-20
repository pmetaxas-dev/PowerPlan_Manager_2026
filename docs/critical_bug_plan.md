# Critical Bug Plan: Duplicate Power-Saving Plans

## Status

- Created: 2026-08-20
- Severity: Critical
- Target: Microsoft Store update
- Phase: Bug definition and planning
- Implementation: Not started

## Reported Bug

On every app startup, the app appears to create a new custom Windows
power-saving plan. Over time, hundreds of custom plans accumulate and clutter
Windows Power Options.

## Expected Behavior

Starting the app or repeatedly entering and leaving the idle state must not
increase the number of power plans managed by the app.

## Functional Goal

- While idle, the app must reduce CPU energy use by an amount clearly equivalent
  to the original approximately 50% throttle.
- The exact Windows setting or technique is flexible; a fixed 50% Maximum
  processor state is not mandatory.
- Built-in Power Saver alone is insufficient unless it achieves equivalent CPU
  savings.
- Additional system-wide energy savings are welcome and highly desirable.
- Any solution must avoid administrator elevation and remain Store-compatible.

## Background and Constraints

- The app was originally created in 2020 and is now being modernized with AI
  assistance.
- As a Microsoft Store app, it must avoid elevated privileges because elevation
  could prevent successful Store submission.
- The intended idle behavior requires the power-saving plan to limit the CPU to
  50%.
- A custom power plan was introduced because modifying the built-in Power Saver
  plan without elevation was not considered feasible at the time.
- The custom-plan lifecycle was implemented incorrectly, resulting in the
  accumulation of duplicate plans.

## Secondary Remediation Requirement

- The app is sold through Microsoft Store and has been installed on more than
  300 machines.
- Existing users may already have many custom power plans created by the app.
- Preventing future duplicates is therefore not sufficient.
- The update must also erase legacy custom plans previously created by the app.
- Cleanup must avoid deleting legitimate user- or vendor-created plans.
- Cleanup must run automatically because affected users may not know about the
  problem; it must not require prompts or user action.
- Cleanup should run once, verify that it succeeded, and only then record
  completion so it is not executed again.
- Failure and retry behavior will depend on the selected remediation mechanism.

## Related Startup Recovery Bug

- If Windows shuts down, hangs, or restarts while the app's idle plan is active,
  the app can capture that plan at startup as its normal performance plan.
- The system may then remain permanently in power-saving mode.
- A mitigation was attempted in an earlier AI-assisted session, but its status in
  the current version is unknown.
- Startup must identify the fastest suitable installed plan instead of blindly
  trusting the currently active plan.
- The best plan depends on CPU vendor and installed plans; an AMD system may have
  a plan such as the 1usmus Ryzen Power Plan that is unavailable on Intel systems.
- Users can override the active plan, but the current idle/active cycle takes
  control again: idle selects power saving, then activity selects the fastest
  available plan.
- Changing this user-override behavior is not urgent and is outside the current
  critical-fix scope.

## Preliminary Code Analysis

The following findings were recorded before investigation was paused:

- The app calls `PowerDuplicateScheme` during idle activation. Windows assigns
  each duplicate a new GUID.
- Creation is not called directly by the startup constructor. The default
  90-second idle timer may make creation appear startup-related, especially when
  the Windows session is already idle.
- The duplicate is configured with a 50% maximum processor state, named
  `PPM-Idle-Throttle`, activated, and its GUID is saved.
- When activity resumes, cleanup attempts to delete the duplicate before
  restoring the previous plan. Deletion may fail while the duplicate is active.
- Some cleanup paths clear the saved or in-memory GUID without first confirming
  that deletion succeeded.
- Activation, restoration, deletion, and startup cleanup run asynchronously
  without one serialized lifecycle, creating possible ordering races.
- An earlier fix persists the latest GUID and searches for old duplicates by
  name or a 50% CPU setting. This does not remove the underlying repeated-create
  design and could mistake an unrelated user plan for an app-owned plan.

## Preliminary Root Cause

The current lifecycle is not idempotent. It creates a temporary Windows power
plan and relies on later deletion. Any failed, interrupted, or misordered cleanup
leaves a plan behind, allowing duplicates to accumulate over repeated starts or
idle cycles.

The exact sequence in the published Store build has not yet been reproduced.

## Proposed Direction

Do not assume that the existing custom-plan approach must be retained. First
search for alternative ways to apply the required 50% CPU limit without
administrator elevation and while remaining compatible with Microsoft Store
submission.

If no suitable alternative exists, keep the custom-plan approach but implement
it with one idempotent lifecycle:

1. Clearly identify one app-owned throttle plan.
2. Find and validate that plan at startup.
3. Create it only when it does not already exist.
4. Reuse it for every idle transition.
5. Restore the normal plan when activity resumes without deleting and recreating
   the throttle plan.
6. Serialize all power-plan operations.
7. Preserve recovery state until native operations are confirmed successful.
8. Never delete unrelated plans based only on their name or CPU setting.

The alternative is to use Windows' built-in Energy Saver plan and create no
custom plan. The intended product behavior must be confirmed before choosing.

## Test Systems

Three systems are available:

- **System 1:** AMD, Windows build `10.0.26200.8655`, affected, and the primary
  machine for repeated remedy tests.
- **System 2:** Intel, affected, and reserved for secondary cleanup and
  validation tests. Windows version pending.
- **System 3:** Intel, completely unaffected, and reserved for fresh reproduction
  and regression testing. Windows version pending.

This setup provides affected AMD and Intel environments plus a clean Intel
baseline.

### System 1 Evidence

`powercfg /list` shows 30 plans:

- 25 plans named `Power saver`.
- 1 canonical Windows Power Saver plan.
- 24 app-created Power Saver duplicates.
- 5 legitimate non-Power-Saver plans.
- `1usmus Ryzen Universal` is active.

The canonical Windows Power Saver GUID is
`a1841308-3541-4fab-bc81-f71556f20b4a` and must always be preserved.

Every buggy app version created duplicates consistently by copying the canonical
Power Saver plan, assigning a new GUID, and changing only Maximum processor
state to 50%. The duplicates retained the name `Power saver`.

## Engineering and Release Baseline

- The app is a .NET 8 WinForms system-tray utility.
- On 2026-08-19, the documented test suite completed with 57 passed, 0 failed,
  and 0 skipped.
- Baseline test command:

  ```powershell
  dotnet test "Power-Plan_Manager-2.0.Tests\Power-Plan_Manager-Take_8.Tests.csproj"
  ```

- Local Store-packaging tooling is available: Visual Studio 2022 Professional,
  Desktop Bridge targets, Windows SDK 10.0.26100, MakeAppx x86/x64 tools, an
  existing packaging project, and Store association metadata.
- No Store package for this critical update has yet been generated or validated.

### Test Boundaries and Safety

- An unpackaged run can verify power-plan discovery, switching, restoration,
  cleanup, timers, logging, tray behavior, and enable/disable behavior.
- A packaged build is required to verify MSIX identity, installation, removal,
  startup tasks, architecture, dependencies, and Store-update behavior.
- Before manual tests, record the active GUID and complete plan list, define a
  recovery command, monitor logs, and stop the tray process after each session.
- Test normal exit, forced termination, failure, and restart recovery while
  confirming that the original normal plan remains recoverable.

### Release Concerns

- Recorded versions are inconsistent: generated package `1.5.3.0`, README
  `2.0.0`, assembly/file `2.0.1.0`, and MSIX manifest `2.1.0.0`.
- Select a consistent release version higher than the latest Partner Center
  submission; do not reuse the existing `1.5.3.0` package output.
- Confirm package identity and publisher against the existing Store product.
- Generate a signed sideload package for packaged functional testing and a
  separate Store upload artifact for Partner Center.
- Inspect bundle architectures, manifest, dependencies, symbols, and filenames.
- The repository has no `global.json`; the .NET 8 target currently builds with
  installed .NET 9 SDKs. Pin an approved SDK or deliberately document and verify
  the selected release toolchain.

## Information Still Needed

- Names shown for the duplicates: `PPM-Idle-Throttle`, `Power saver`, blank, or
  something else.
- Whether a new plan appears immediately at launch or after inactivity.
- The affected published app version.
- Whether the app should keep one reusable custom plan with a 50% CPU cap or use
  the built-in Energy Saver plan.
- The performance-plan preference order for AMD, Intel, and unknown CPUs.
- The fallback when the preferred performance plan is unavailable.
- The power-plan inventory and duplicate count on the second test system.
- The initial power-plan inventory on the clean third system.
- Any available app logs from an affected installation.

## Acceptance Criteria

- Repeated startups do not increase the number of app-owned plans.
- Repeated idle/active cycles do not increase the plan count.
- At most one app-owned custom plan exists, or none if the built-in plan is used.
- The intended normal plan is restored after activity and shutdown.
- An idle plan is never captured as the normal performance plan after restart.
- The fastest suitable plan is selected correctly for the CPU vendor and
  installed-plan set.
- Failed operations retain enough state for safe recovery.
- Power-plan operations cannot race.
- Unrelated user or vendor plans are never deleted.
- Restarting after a crash does not create another duplicate.
- Legacy cleanup runs automatically without user action.
- Cleanup is marked complete only after its result is verified.
- Successfully completed cleanup is not repeated on later startups.
- Duplicate prevention and cleanup are verified on both AMD and Intel systems.
- The final behavior is verified in a packaged Microsoft Store candidate.

## Planning Rule

From this point forward, this file will record only bug information supplied by
the user. No further code, log, test, or repository investigation will be
performed unless the user explicitly changes that instruction.
