# Windows Store Deployment Plan

**Project:** Power-Plan_Manager-2.0  
**Target:** Microsoft Windows App Store (formerly Microsoft Store)  
**Framework:** .NET 8 Windows Forms  
**Status:** Planning Document  
**Estimated Timeline:** 2-3 weeks

---

## Overview

This document outlines the complete roadmap to prepare Power-Plan_Manager-2.0 for deployment on the Microsoft Windows App Store (also known as Microsoft Store or Windows Store). The Windows App Store provides:

- ✅ Automated updates via Microsoft Store
- ✅ Large potential user base
- ✅ Simplified installation (single click)
- ✅ Trusted distribution channel
- ✅ Monetization options (if desired)
- ✅ Analytics and crash reporting

---

## Current State Assessment

### Project Readiness

| Aspect | Status | Notes |
|--------|--------|-------|
| **Framework** | ✅ Ready | .NET 8, WinExe target |
| **UI Polish** | ⚠️ Partial | Tray app works well, could use About window polish |
| **Icons** | ⚠️ Partial | Current icon exists, may need 200x200 store asset |
| **Installer** | ❌ Missing | Need MSIX package |
| **Certificate** | ❌ Missing | Need code signing certificate |
| **Telemetry** | ⚠️ Partial | Basic logging, no crash reporting |
| **Settings** | ✅ Ready | Registry-based settings work |
| **Permissions** | ⚠️ Partial | May need manifest review |
| **Documentation** | ⚠️ Partial | README exists, Store description needed |
| **Testing** | ✅ Ready | Test project exists with 13 tests |

---

## Store Requirements Overview

### Microsoft Store Requirements for Desktop Apps

#### 1. **Code Signing**
- ✅ Required for all executables
- ✅ Required for MSIX package
- ✅ Must be Authenticode signed
- ✅ Certificate from approved CA (Sectigo, DigiCert, etc.)

#### 2. **Package Format (MSIX)**
- ✅ All desktop apps must use MSIX or Appx format
- ✅ Cannot submit raw .exe files
- ✅ Visual Studio can create MSIX packages
- ✅ Support for .NET 8 included

#### 3. **Store Listing Assets**
- ✅ 300x300 icon (PNG, transparent preferred)
- ✅ 1920x1080 feature graphic (promo image)
- ✅ Screenshots (min 320x480, recommended multiple angles)
- ✅ Description (up to 10,000 characters)
- ✅ Keywords (for search optimization)

#### 4. **System Requirements Declaration**
- ✅ Minimum Windows version (likely Windows 10 2004+)
- ✅ RAM requirements
- ✅ Disk space requirements
- ✅ GPU requirements (if any)

#### 5. **App Manifest**
- ✅ Package identity, version, publisher
- ✅ Capabilities and permissions
- ✅ File associations (if any)
- ✅ Extensions (if any)

#### 6. **Privacy Policy**
- ❌ **REQUIRED** - Must have online privacy policy URL
- This is a **blocker** for Store submission

#### 7. **Content Restrictions**
- ✅ No deceptive practices
- ✅ No malware or viruses
- ✅ No illegal functionality
- ✅ No cryptocurrency mining
- ✅ No rootkits or kernel-mode code

---

## Implementation Roadmap

### Phase 1: Prepare Application (Week 1)

#### 1.1 Code Cleanup
- [ ] Review all warnings
- [ ] Remove debug logging (or make optional)
- [ ] Ensure no hardcoded paths
- [ ] Validate all exception handling
- [ ] Test on clean Windows system

#### 1.2 Create Legal Documents
- [ ] **Privacy Policy** (CRITICAL - Store requirement)
  - Address data collection (if any)
  - Explain logging practices
  - Host on public website
  - Include URL in app manifest

- [ ] **Terms of Service** (Recommended)
  - Usage terms
  - Warranty disclaimers
  - License information

- [ ] **License File**
  - Include LICENSE.txt in package
  - MIT, GPL, Apache 2.0, or custom

#### 1.3 Polish Application
- [ ] Update About window with Store-ready branding
- [ ] Ensure tray icon is high quality
- [ ] Test first-run experience
- [ ] Verify all UI text for typos
- [ ] Update version number (use semantic versioning)
- [ ] Create CHANGELOG.md

#### 1.4 Update Manifest & Metadata
```xml
<!-- Package.appxmanifest -->
<Identity 
  Name="YourPublisherId.PowerPlanManager"
  Publisher="CN=Your Name, O=Your Organization"
  Version="2.0.0.0" />

<Properties>
  <DisplayName>Power-Plan Manager</DisplayName>
  <PublisherDisplayName>Your Name</PublisherDisplayName>
  <Logo>Assets\StoreLogo.png</Logo>
</Properties>
```

---

### Phase 2: Create Assets & Graphics (Week 1-2)

#### 2.1 Required Store Assets

| Asset | Dimensions | Format | Notes |
|-------|-----------|--------|-------|
| **Logo** | 300x300 | PNG | App icon, transparent background |
| **Small Tile** | 71x71 | PNG | Home screen small tile |
| **Medium Tile** | 150x150 | PNG | Home screen medium tile |
| **Large Tile** | 310x310 | PNG | Home screen large tile |
| **Wide Tile** | 310x150 | PNG | Wide home screen tile |
| **Store Logo** | 120x120 | PNG | Store listing |
| **Feature Graphic** | 1920x1080 | PNG | Store hero image (optional) |
| **Screenshots** | Min 320x480 | PNG | At least 1, up to 5 recommended |

#### 2.2 Create Assets (Tools)
- **Option A:** Use existing icon and scale up
  - Current: `Papirus-Team-Papirus-Apps-Preferences-system-login.ico`
  - Tool: Adobe XD, Figma, Canva Pro

- **Option B:** Hire designer
  - Budget: $100-300
  - Timeline: 3-5 days

- **Option C:** Use free tools
  - Canva (free tier)
  - GIMP (free, open-source)
  - Photopea (free web editor)

#### 2.3 Store Description Template
```
Title (50 chars max):
Power-Plan Manager: Smart Power Saving

Short Description (120 chars max):
Automatically switches Windows to Power Saver mode during inactivity, balancing performance and battery life.

Full Description (10,000 chars available):
Power-Plan Manager automatically manages your Windows power plan to balance 
performance and battery life. When you stop using your computer, it automatically 
switches to Power Saver mode. As soon as you resume activity, it switches back 
to Balanced mode.

Perfect for:
• Laptop users wanting extended battery life
• Desktop users wanting to reduce power consumption
• Anyone wanting "set and forget" power management
• Organizations wanting automated energy savings

Features:
• Automatic idle detection
• Instant response to user activity
• System tray integration (minimal visual footprint)
• No configuration required
• Lightweight and efficient
• Free and open-source

How it works:
1. Application starts automatically with Windows
2. Runs silently in system tray
3. Monitors for user inactivity
4. Switches to Power Saver when idle
5. Instantly returns to Balanced when active

System Requirements:
• Windows 10 or later
• .NET 8 Runtime (or bundled)
• Minimal CPU/RAM impact
```

#### 2.4 Keywords for Store Search
- Power plan
- Power saving
- Battery life
- Energy saving
- Laptop
- Idle
- Power management
- System tray
- Windows
- Performance

---

### Phase 3: Set Up Developer Account (Week 1)

#### 3.1 Microsoft Developer Account
- [ ] Register at [Microsoft Partner Center](https://partner.microsoft.com)
- [ ] Complete identity verification (5-10 days)
- [ ] Pay registration fee (~$19 one-time)
- [ ] Set up developer profile
- [ ] Add payment method

#### 3.2 Publisher Identity
- [ ] Choose publisher name (e.g., "Your Name")
- [ ] Generate publisher ID from Partner Center
- [ ] Use in Package.appxmanifest

#### 3.3 Code Signing Certificate
- [ ] Option A: Buy Authenticode certificate (~$200-400/year)
  - From: Sectigo, DigiCert, Comodo, GlobalSign
  - Import into Visual Studio
  - Use for signing .exe and MSIX

- [ ] Option B: Use test certificate
  - For local testing only
  - Cannot submit to Store
  - Generated by Visual Studio

---

### Phase 4: Build MSIX Package (Week 2)

#### 4.1 Create MSIX Project in Visual Studio

**Step-by-Step:**

1. **Add Packaging Project**
   ```
   File → New → Project → Windows Application Packaging Project
   ```

2. **Configure Package**
   ```
   Right-click Package → Set as Startup Project
   Right-click Package → Properties → Package
   ```

3. **Update Package.appxmanifest**
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
			xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">
	 <Identity
	   Name="YourPublisherID.PowerPlanManager"
	   Publisher="CN=Your Name, O=Your Organization, C=US"
	   Version="2.0.0.0" />

	 <Properties>
	   <DisplayName>Power-Plan Manager</DisplayName>
	   <PublisherDisplayName>Your Name</PublisherDisplayName>
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
		 <uap:VisualElements DisplayName="Power-Plan Manager" Square150x150Logo="Assets\Square150x150Logo.png" Square44x44Logo="Assets\Square44x44Logo.png" Description="Smart power saving for Windows" BackgroundColor="transparent" />
	   </Application>
	 </Applications>

	 <Capabilities>
	   <Capability Name="internetClient" />
	   <rescap:Capability Name="runFullTrust" />
	 </Capabilities>
   </Package>
   ```

4. **Capabilities Explanation**
   - `internetClient`: For crash reporting/telemetry
   - `runFullTrust`: Required for Win32 desktop apps (MSIX bridge)

#### 4.2 Code Signing

**Option A: With Certificate**
```csharp
// In Project file:
<PropertyGroup>
  <PackageCertificateKeyFile>certificate.pfx</PackageCertificateKeyFile>
  <PackageCertificatePassword>password_here</PackageCertificatePassword>
</PropertyGroup>
```

**Option B: Self-Signed (Testing)**
```powershell
# Visual Studio generates automatically for testing
# Project → Package → Create App Packages → Self-signed
```

#### 4.3 Build MSIX
```powershell
# Via Visual Studio
# Build → Package App

# Or via command line:
msbuild PackageProject.csproj /p:Configuration=Release /p:Platform=x64
```

**Output:** `Power-Plan_Manager-2.0_2.0.0.0_x64.msix`

#### 4.4 Test MSIX Package Locally
```powershell
# Install locally (may require test certificate trusted)
Add-AppxPackage -Path "Power-Plan_Manager-2.0_2.0.0.0_x64.msix"

# Verify installation
Get-AppxPackage | Select Name, Version

# Test the app functionality
# - Launch from Start menu
# - Verify tray icon appears
# - Test about window
# - Verify settings save
```

---

### Phase 5: Prepare Store Submission (Week 2)

#### 5.1 Create Store Listing

**In Partner Center:**

1. Create new app submission
2. Fill out store listing:
   - [ ] English title (50 chars max)
   - [ ] English description
   - [ ] Keywords (7 max)
   - [ ] Category selection
   - [ ] Age rating completion
   - [ ] Upload logo/assets
   - [ ] Add screenshots (at least 1)

3. System requirements:
   - [ ] Windows 10 version 17763+
   - [ ] 64-bit processor
   - [ ] 2GB RAM recommended
   - [ ] 50MB disk space

4. Pricing:
   - [ ] Free (recommended for first launch)
   - [ ] Trial available?
   - [ ] In-app purchases?

#### 5.2 Submit for Certification

1. [ ] Complete all required fields
2. [ ] Review Store policies compliance
3. [ ] Upload signed MSIX package
4. [ ] Upload screenshots
5. [ ] Add privacy policy URL
6. [ ] Submit for certification

**Certification typically takes:**
- 24-48 hours for first submission
- 6-24 hours for updates
- May request additional info

---

### Phase 6: Maintenance & Updates (Ongoing)

#### 6.1 Monitor Feedback
- [ ] Check Store reviews regularly
- [ ] Respond to user feedback
- [ ] Track crash reports

#### 6.2 Create Updates
```powershell
# Update version in .csproj and manifest
# Build new MSIX
# Submit new package to Partner Center
# Users auto-update via Store
```

#### 6.3 Version Strategy
- Use semantic versioning: `MAJOR.MINOR.PATCH.BUILD`
- Example: `2.0.0.0` = v2.0, build 0
- Update before each Store submission

---

## Detailed Technical Requirements

### Architecture Changes

#### 1. **MSIX Compatibility**

Current: Standalone .exe  
Target: MSIX container + runtime

| Aspect | Current | Store |
|--------|---------|-------|
| Installation | Manual | Windows Store |
| Updates | Manual | Automatic (Store) |
| Registry Access | Full HKCU | Virtualized* |
| File Access | Full user | App data folders |
| Uninstall | Manual | Windows + Store |

*MSIX uses registry virtualization; may need adjustments.

#### 2. **Registry Virtualization Workaround**

If registry autostart doesn't work in MSIX:

```csharp
// Option A: Use VisualStudioUriScheme (APPX manifest)
// Option B: Use scheduled task via COM
// Option C: Use Task Scheduler programmatically

public static bool CreateScheduledTaskAutostart()
{
	try
	{
		TaskSchedulerClass scheduler = new TaskSchedulerClass();
		scheduler.Connect();

		ITaskFolder rootFolder = scheduler.GetFolder("\\");
		ITaskDefinition taskDef = scheduler.NewTask(0);

		// Set trigger (at user login)
		ILogonTrigger logonTrigger = 
			(ILogonTrigger)taskDef.Triggers.Create(_TASK_TRIGGER_TYPE.TASK_TRIGGER_LOGON);

		// Set action (run app)
		IExecAction action = 
			(IExecAction)taskDef.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
		action.Path = Application.ExecutablePath;

		// Register task
		rootFolder.RegisterTaskDefinition(
			"PowerPlanManager",
			taskDef,
			(int)_TASK_CREATION.TASK_CREATE,
			null,
			null,
			_TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN
		);

		return true;
	}
	catch (Exception ex)
	{
		Logger.LogException("CreateScheduledTaskAutostart", ex);
		return false;
	}
}
```

#### 3. **Runtime Bundling**

Options:
- [ ] **A:** User installs .NET 8 runtime separately
  - Smaller MSIX size (~20MB)
  - Requires runtime installation

- [ ] **B:** Bundle .NET 8 runtime in MSIX (recommended)
  - Larger MSIX size (~150-200MB)
  - Works out-of-box
  - Recommended for Store apps

**Enable runtime bundling in .csproj:**
```xml
<PropertyGroup>
  <SelfContained>true</SelfContained>
  <PublishTrimmed>true</PublishTrimmed>
  <PublishReadyToRun>true</PublishReadyToRun>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
</PropertyGroup>
```

---

## Required Legal Documents

### 1. **Privacy Policy** (CRITICAL - BLOCKER)

**Location:** Must be hosted on public website  
**Format:** Web page URL or PDF  
**Length:** 1-2 pages minimum  

**Must Address:**
- [ ] Data collection practices
- [ ] Logging details (local/cloud)
- [ ] Third-party services used
- [ ] User rights and consent
- [ ] Data deletion/retention
- [ ] Contact information

**Template:**
```markdown
# Privacy Policy - Power-Plan Manager

## Overview
Power-Plan Manager is a simple power management utility that:
- Monitors system idle time (local only)
- Switches Windows power plans (local action)
- Stores settings in Windows Registry (local only)
- Does NOT collect or transmit personal data
- Does NOT require internet connection
- Does NOT track usage

## Data Collection
None. This app runs entirely locally.

## Third-Party Services
None. The app is completely offline.

## Logging
- Optional debug logs stored locally in AppData
- No transmission to external servers
- Completely optional

## User Rights
Users can:
- Disable autostart in Task Manager
- Uninstall from Settings → Apps
- Delete app folder to remove all data
- Opt-out of any telemetry

## Questions?
Contact: [your email]
Last Updated: [date]
```

### 2. **License File**
```
LICENSE.txt (in package)

MIT License OR Custom License
Include in root of MSIX
```

### 3. **Terms of Service** (Recommended)
```
- Usage restrictions
- Warranty disclaimers
- Limitation of liability
- Governing law
```

---

## Submission Checklist

### Before Submitting to Store

#### Code Quality
- [ ] No compiler warnings
- [ ] No runtime errors
- [ ] All tests passing
- [ ] Code reviewed
- [ ] Signed executable

#### Assets & Graphics
- [ ] 300x300 logo in PNG format
- [ ] All required tiles (71x71, 150x150, etc.)
- [ ] Store icon/hero image
- [ ] Screenshots (at least 1)
- [ ] All assets are original/licensed

#### Store Listing
- [ ] Title (50 chars or less)
- [ ] Short description (120 chars or less)
- [ ] Full description (compelling, no typos)
- [ ] Keywords (7 max, relevant)
- [ ] Category selected
- [ ] Age rating completed
- [ ] Keywords in description

#### Legal & Policy
- [ ] Privacy policy URL (live, accessible)
- [ ] Terms accepted
- [ ] No policy violations
- [ ] No malware/viruses
- [ ] No deceptive practices

#### Technical
- [ ] MSIX package signed with real certificate
- [ ] Manifest includes all capabilities needed
- [ ] Version number incremented
- [ ] System requirements specified
- [ ] Dependencies documented

#### Testing
- [ ] Installed from MSIX successfully
- [ ] App launches correctly
- [ ] All features work (power plans, tray, settings)
- [ ] Crash-free operation for 30+ minutes
- [ ] Uninstall removes all traces
- [ ] Can reinstall without issues

#### Documentation
- [ ] README updated for Store
- [ ] Help/Support information ready
- [ ] Known issues documented
- [ ] System requirements clear

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Certification rejection | Medium | High | Review all policy requirements |
| Registry access fails in MSIX | Medium | High | Use scheduled tasks fallback |
| Certificate expires | Low | High | Set renewal reminders |
| User data collection concerns | Low | Medium | Clear privacy policy |
| App crashes on startup | Low | High | Extensive testing before submit |
| MSIX signing issues | Low | Medium | Test signing process locally first |
| Missing legal documents | Medium | High | Complete all docs before submit |
| Store account issues | Low | Medium | Complete identity verification early |

---

## Timeline & Dependencies

```
Week 1:
- Mon: Code cleanup, legal docs, design assets
- Tue: Create privacy policy (CRITICAL)
- Wed: Polish app, update manifests
- Thu: Developer account registration
- Fri: Assets finalized

Week 2:
- Mon: MSIX project setup, testing
- Tue: Code signing certificate (if needed)
- Wed: Build and test MSIX locally
- Thu: Create store listing in Partner Center
- Fri: Final submission

Week 3:
- Mon-Fri: Certification review (24-48 hours typically)
- Final approval & publication
```

---

## Cost Breakdown

| Item | Cost | Notes |
|------|------|-------|
| Dev Account Registration | $19 | One-time |
| Code Signing Certificate | $200-400 | Annual (if needed) |
| Designer/Assets | $0-300 | Optional (DIY possible) |
| Legal Review | $0-500 | Optional |
| **Total** | **$200-1200** | Minimum $19 |

---

## Post-Launch Monitoring

### Store Metrics to Track
- [ ] Install count
- [ ] User ratings
- [ ] Review sentiment
- [ ] Crash reports
- [ ] Feature requests

### Feedback Loop
1. Monitor Store reviews weekly
2. Respond to user feedback
3. Track crash reports in telemetry
4. Plan updates based on user requests
5. Release updates monthly or as needed

---

## Future Store Enhancements

1. **In-App Purchases**
   - Premium features (scheduling, profiles)
   - Donations/tips

2. **Crash Reporting**
   - Integrate AppCenter or similar
   - Monitor stability

3. **A/B Testing**
   - Test different UI configurations
   - Measure user preference

4. **Localization**
   - Translate to multiple languages
   - Expand market reach

5. **Monetization**
   - Ads (non-intrusive)
   - Premium version
   - Donations

---

## Resources & References

### Microsoft Documentation
- [MSIX Documentation](https://docs.microsoft.com/en-us/windows/msix/)
- [App Store Policies](https://docs.microsoft.com/en-us/windows/apps/publish/store-policies)
- [Partner Center Guide](https://docs.microsoft.com/en-us/windows/apps/publish/)
- [Package.appxmanifest Schema](https://docs.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/schema-root)

### Tools
- [MSIX Packaging Tool](https://www.microsoft.com/en-us/p/msix-packaging-tool/9n5lw3jbcxkf)
- [Makecert.exe](https://docs.microsoft.com/en-us/windows/win32/seccrypto/makecert) (testing)
- [SignTool.exe](https://docs.microsoft.com/en-us/windows/win32/seccrypto/signtool) (signing)

### Design Tools
- [Figma](https://www.figma.com) (free tier)
- [Canva](https://www.canva.com) (free tier)
- [Adobe XD](https://www.adobe.com/products/xd) (free tier)

---

## Success Criteria

✅ App successfully published on Microsoft Store  
✅ Achieves 4+ star rating  
✅ Zero critical bugs in first month  
✅ Clear privacy policy in place  
✅ Regular updates released  
✅ User support system in place  
✅ Analytics showing positive engagement  
✅ Positive user feedback and reviews  

---

## Support & Escalation

### If Certification Fails
1. Review rejection reason from Microsoft
2. Address specific policy violation
3. Resubmit within guidelines
4. Contact Microsoft support if unclear

### If Technical Issues Arise
1. Review error logs in Partner Center
2. Test locally with matching configuration
3. Update MSIX and resubmit
4. Post on Microsoft forums if needed

---

**Document Status:** Ready for Implementation  
**Last Updated:** May 16, 2026  
**Next Action:** Review requirements → Create legal docs → Register developer account → Build MSIX
