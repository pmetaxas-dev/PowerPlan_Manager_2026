# Windows Autostart Implementation Plan (Manifest-Based Only)

**Project:** Power-Plan_Manager-2.0  
**Feature:** Application starts automatically when Windows starts  
**Type:** Manifest-based approach (MSIX/Store only)  
**Status:** Final Planning Document  
**Target Framework:** .NET 8 Windows Forms  

---

## ? Quick Start

Add this to Package.appxmanifest:

\\\xml
<Extensions>
  <desktop:Extension Category="windows.startupTask">
    <desktop:StartupTask Task="StartPowerPlanManager" Enabled="true" DisplayName="Power-Plan Manager" />
  </desktop:Extension>
</Extensions>
\\\

**Done!** No C# code changes. ~15 minutes.

---

## Why Manifest-Based Only?

Registry methods **FAIL** with MSIX:
- MSIX virtualizes registry - entries invisible to external processes
- App path becomes invalid in sandbox
- Windows can't launch from virtualized registry
- Task Manager can't manage MSIX autostart

Manifest approach:
- ? Bypasses registry
- ? Works in MSIX sandbox
- ? Integrates with Windows startup
- ? Users manage from Settings ? Apps ? Startup apps
- ? No C# code changes

**ONLY reliable method for Store.**

---

## How It Works

1. **Installation**: Windows reads manifest, registers StartupTask
2. **User Login**: Windows launches app automatically
3. **User Control**: Toggle on/off in Settings ? Apps ? Startup apps

---

## Implementation

### Step 1: Add Namespace

\\\xml
<?xml version="1.0" encoding="utf-8"?>
<Package 
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:desktop="http://schemas.microsoft.com/appx/manifest/desktop/windows10">
\\\

### Step 2: Add Extension

\\\xml
<Extensions>
  <desktop:Extension Category="windows.startupTask">
    <desktop:StartupTask Task="StartPowerPlanManager" Enabled="true" DisplayName="Power-Plan Manager" />
  </desktop:Extension>
</Extensions>
\\\

### Step 3: Build & Test

\\\powershell
# Build MSIX
Build ? Package App

# Install
Add-AppxPackage -Path "Power-Plan_Manager-2.0_2.0.0.0_x64.msix"

# Verify: Settings ? Apps ? Startup apps
\\\

### Step 4: Test

1. Close app
2. Restart Windows
3. App launches automatically
4. Check system tray

---

## Files to Modify

| File | Action |
|------|--------|
| \Package.appxmanifest\ | Add StartupTask extension |

**No C# code changes!**

---

## Testing Checklist

- [ ] Desktop namespace in manifest
- [ ] StartupTask extension added
- [ ] Manifest validates
- [ ] MSIX builds
- [ ] App in Settings ? Apps ? Startup apps
- [ ] Autostart works
- [ ] Settings toggle works
- [ ] Manual launch works

---

## Complete Example

\\\xml
<?xml version="1.0" encoding="utf-8"?>
<Package 
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:desktop="http://schemas.microsoft.com/appx/manifest/desktop/windows10">

  <Identity
    Name="YourPublisherId.PowerPlanManager"
    Publisher="CN=Your Company"
    Version="2.0.0.0" />

  <Properties>
    <DisplayName>Power-Plan Manager</DisplayName>
    <PublisherDisplayName>Your Company</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-us" />
  </Resources>

  <Applications>
    <Application Id="App" StartPage="Power-Plan_Manager-2.0.exe">
      <uap:VisualElements DisplayName="Power-Plan Manager" />
    </Application>
  </Applications>

  <Extensions>
    <desktop:Extension Category="windows.startupTask">
      <desktop:StartupTask Task="StartPowerPlanManager" Enabled="true" DisplayName="Power-Plan Manager" />
    </desktop:Extension>
  </Extensions>

  <Capabilities>
    <Capability Name="internetClient" />
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>

</Package>
\\\

---

## Attributes

| Attribute | Value | Description |
|-----------|-------|-------------|
| \Task\ | \StartPowerPlanManager\ | Unique ID |
| \Enabled\ | \	rue\ | Auto-enabled on install |
| \DisplayName\ | \Power-Plan Manager\ | Settings display name |

---

## User Experience

**Install**: Autostart enabled by default

**Disable**: Settings ? Apps ? Startup apps ? Toggle OFF

**Re-enable**: Settings ? Apps ? Startup apps ? Toggle ON

---

## Troubleshooting

**Not in Startup apps:**
- Verify namespace: \xmlns:desktop="..."\
- StartupTask in \<Extensions>\ section
- Rebuild and reinstall

**Won't autostart:**
- Check Settings (may be OFF)
- Verify \Enabled="true"\
- Restart system

---

## No Code Changes

\\\csharp
// Program.cs - UNCHANGED
static void Main()
{
    ApplicationConfiguration.Initialize();
    Application.Run(new Form1());
}
\\\

Manifest handles everything!

---

## Performance & Security

? Minimal overhead  
? No memory leaks  
? No elevated privileges  
? No registry mods  
? User control  

---

## Success Criteria

? StartupTask in manifest  
? MSIX builds  
? App in Settings ? Startup apps  
? Autostart works  
? Settings toggle works  
? No code changes  
? Manual launch works  

---

## Timeline

- Manifest edit: 5 min
- Build: 5 min
- Test: 10 min
- **Total: ~20 min**

---

**Status:** ? Ready  
**Target:** Microsoft Store via MSIX  
**Next:** Add extension ? Build ? Test ? Submit
