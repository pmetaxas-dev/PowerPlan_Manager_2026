# August Bug Fix

> Living planning and engineering record for the critical bug targeted by the next Microsoft Store update.

## Document Status

- Created: 2026-08-19
- Phase: Context collection and bug definition
- Target release version: To be determined
- Target Store submission date: To be determined
- Critical bug summary: Awaiting the detailed bug report

## Project Background

- PowerPlan Manager is a personal Microsoft Store project originally created in 2020.
- The application was recently updated and modernized with AI assistance.
- The current application is a .NET 8 WinForms system-tray utility that manages Windows power plans in response to user activity.
- The immediate goal is to resolve a critical bug and prepare a reliable update for Microsoft Store submission.

Because the codebase combines legacy behavior with recent AI-assisted changes, the investigation must establish the original intent, verify current behavior, and avoid unnecessary rewrites.

## Objective

1. Capture a precise and reproducible description of the critical bug.
2. Identify the root cause using code, logs, Windows behavior, and repeatable evidence.
3. Design the smallest complete fix that preserves intended behavior.
4. Add or update automated tests where practical.
5. Verify the fix manually on relevant Windows and power-plan scenarios.
6. Confirm packaging and Microsoft Store release readiness.

## Evidence Labels

Investigation notes will use these labels:

- **Confirmed:** Verified directly from code, tests, logs, or repeatable behavior.
- **Reported:** Observed or described but not yet independently reproduced.
- **Hypothesis:** A possible explanation that still requires evidence.
- **Decision:** An agreed technical or release choice and its rationale.

## Bug Report

- Summary: To be provided
- Expected behavior: To be provided
- Actual behavior: To be provided
- Reproduction steps: To be provided
- Frequency: To be provided
- Affected Windows versions and hardware: To be provided
- Affected application versions: To be provided
- Relevant logs or screenshots: To be provided
- Workarounds: To be provided
- User impact: To be provided

## Confirmed Baseline

- The documented automated suite contains 57 tests.
- On 2026-08-19, the full test project completed with 57 passed, 0 failed, and 0 skipped.
- The five reported MSTest analyzer warnings in `ConstantsTests.cs` were addressed while preserving the intent of the tests.
- The README character-encoding artifacts were corrected.
- The repository now ignores the local `.vscode/` directory.

Test command:

```powershell
dotnet test "Power-Plan_Manager-2.0.Tests\Power-Plan_Manager-Take_8.Tests.csproj"
```

## VS Code Development and Functional Testing

### Confirmed Capability

The application can be built, launched, and debugged from Visual Studio Code. The direct command is:

```powershell
dotnet run `
  --project "Power-Plan_Manager-2.0\Power-Plan_Manager-Take_8.csproj" `
  --configuration Debug
```

With the Microsoft C# extension and C# Dev Kit installed, the application can also be started under the debugger with F5. The current `.vscode/` directory does not contain a committed launch or build task configuration.

Reference: [Debugging C# in Visual Studio Code](https://code.visualstudio.com/docs/csharp/debugging)

### Testing Boundaries

An unpackaged launch can verify:

- Main-window and system-tray behavior
- Timer and user-activity behavior
- Logging
- Power-plan discovery, switching, restoration, and cleanup
- Enable and disable behavior

An unpackaged launch cannot fully verify:

- MSIX package identity
- Packaged installation and removal
- The packaged startup task
- Store-delivered update behavior
- Package architecture and dependency selection

Those behaviors require installing and running a packaged build.

### Safety Requirements for Manual Testing

The application changes real Windows power-plan state. Before functional testing:

1. Record the active power-plan GUID and the complete plan list.
2. Record whether a `PPM-Idle-Throttle` plan already exists.
3. Define a recovery command that restores the original active plan.
4. Monitor the application log during active-to-idle and idle-to-active transitions.
5. Confirm that temporary plans are deleted after normal exit, failure, and restart scenarios.
6. Stop the tray process explicitly after each test session.

## MSIX Store Packaging from VS Code

### Confirmed Local Tooling

The current machine has the components needed to attempt command-line Store packaging:

- Visual Studio 2022 Professional
- Full Visual Studio MSBuild
- Microsoft Desktop Bridge build targets
- Windows SDK 10.0.26100
- MakeAppx tools for x86 and x64
- An existing Windows Application Packaging Project
- An existing `Package.StoreAssociation.xml`

`MSBuild.exe` is installed but is not on the current shell `PATH`. It can be invoked directly from the VS Code terminal at:

```text
C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe
```

The packaging project currently:

- References `Power-Plan_Manager-Take_8.csproj` as its desktop application.
- Targets Windows SDK `10.0.26100.0` with minimum version `10.0.17763.0`.
- Configures an `x86|x64` bundle.
- Sets `AppxBundle` to `Always`.
- Disables package signing in the project.
- Contains a Store association whose main package identity matches the manifest identity.

Microsoft documents the Windows Application Packaging Project as the supported wrapper for producing an MSIX package for a WinForms application. The `StoreUpload` build mode generates an `.msixupload` or `.appxupload` artifact, which is the preferred upload format for Partner Center.

References:

- [Set up a desktop application for MSIX packaging](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net)
- [Command-line Store package build properties](https://learn.microsoft.com/en-us/windows/uwp/packaging/auto-build-package-uwp-apps)
- [Upload MSIX packages to Partner Center](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/upload-app-packages)

### Proposed Store-Upload Command

This command is based on the inspected project configuration and Microsoft build properties. It has not yet been executed for the August update.

```powershell
$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"

& $msbuild `
  "App_Packger_proj\App_Packger_proj.wapproj" `
  /restore `
  /t:Rebuild `
  /p:Configuration=Release `
  /p:Platform=x86 `
  '/p:AppxBundlePlatforms=x86|x64' `
  /p:AppxBundle=Always `
  /p:UapAppxPackageBuildMode=StoreUpload `
  /p:AppxPackageSigningEnabled=false
```

Microsoft re-signs MSIX packages during Store certification, so a Store submission does not require a CA-trusted signing certificate. A package used for local sideload installation must still be signed with a certificate trusted by the test machine.

Reference: [Publish a Windows application](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app)

### Release Metadata and Reproducibility Concerns

The following values are not currently aligned:

| Source | Current version |
|---|---|
| Existing generated package | `1.5.3.0` |
| README release badge/history | `2.0.0` |
| Application assembly/file version | `2.0.1.0` |
| MSIX package manifest | `2.1.0.0` |

Before creating the final artifact:

- Select one intended release version and apply it consistently where appropriate.
- Confirm that the package version is higher than the latest version already accepted by Partner Center.
- Confirm the package identity and publisher against the existing Partner Center product.
- Do not submit the existing `1.5.3.0` output found under `App_Packger_proj\AppPackages`.
- Generate and install a signed sideload package for package-specific functional testing.
- Generate a separate Store upload artifact using `StoreUpload` mode.
- Inspect the resulting bundle architectures, manifest, dependencies, symbols, and file names.
- Upload the candidate to Partner Center and address all validation errors or warnings.

The active x64 `dotnet` installation lists .NET SDK 9.0.314 and 9.0.315 but no .NET 8 SDK. The .NET 9 SDK has successfully built the application's .NET 8 target, so this is not an immediate compilation blocker. It is a release-reproducibility concern because the repository has no `global.json` and therefore uses the newest available SDK. Before the release build, either install and pin an approved .NET 8 SDK or deliberately document and validate the .NET 9 SDK toolchain.

## Investigation Plan

1. Record the full bug report and operating environment.
2. Reproduce the issue without changing the code.
3. Trace the relevant execution path, state transitions, timers, and Windows power-plan commands.
4. Compare the current implementation with the intended legacy behavior and recent changes.
5. Identify failure modes, cleanup requirements, race conditions, and recovery behavior.
6. Document solution options and tradeoffs before selecting a fix.
7. Implement only the approved scope.
8. Run focused regression tests, the full automated suite, and manual Windows validation.

## Proposed Fix

To be determined after reproduction and root-cause analysis.

## Verification Checklist

- [ ] Bug reproduced before implementation
- [ ] Root cause supported by evidence
- [ ] Regression test added or a reason documented if impractical
- [ ] Focused tests pass
- [ ] Full automated suite passes without warnings
- [ ] Active-to-idle transition verified manually
- [ ] Idle-to-active restoration verified manually
- [ ] Enable/disable behavior verified
- [ ] Temporary power-plan creation and cleanup verified
- [ ] Failure and restart recovery verified
- [ ] Logs checked for clear, actionable diagnostics
- [ ] Unpackaged Debug launch verified from VS Code
- [ ] Original power plan recorded before manual tests
- [ ] Recovery procedure tested
- [ ] Release version metadata reconciled
- [ ] Build SDK selected and made reproducible
- [ ] Partner Center identity and previous package version confirmed
- [ ] Signed sideload package generated and installed
- [ ] Packaged installation and startup behavior verified
- [ ] x86 and x64 package contents verified
- [ ] Store `.msixupload` generated using `StoreUpload` mode
- [ ] Upload artifact manifest, dependencies, and symbols inspected
- [ ] Package manifest, assets, identity, and version reviewed
- [ ] Store submission artifacts and certification checks completed

## Decisions

| Date | Decision | Rationale |
|---|---|---|
| 2026-08-19 | Use this file as the living record for the August critical-bug work. | Keeps evidence, decisions, verification, and release preparation in one reviewable place. |

## Progress Log

### 2026-08-19

- Created the August bug-fix record.
- Recorded the project's 2020 origin and recent AI-assisted modernization.
- Recorded the current automated-test baseline and recent repository housekeeping.
- Confirmed that the WinForms application can be run and debugged from VS Code.
- Confirmed that the installed Visual Studio and Windows SDK toolchain can attempt command-line MSIX Store packaging.
- Recorded the proposed Store-upload command and the distinction between unpackaged, sideloaded, and Store-package testing.
- Recorded release-version inconsistencies and the unpinned .NET SDK concern.
- No August Store package has been generated or validated yet.
- Awaiting the detailed critical-bug report before beginning investigation.
