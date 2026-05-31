# Autostart Plan Revision - MSIX Awareness

**Date:** May 16, 2026 (Afternoon Revision)  
**Status:** ✅ CRITICAL CORRECTION APPLIED  
**Impact:** High - Affects Store deployment strategy

---

## What Changed

The `/docs/autostart.md` plan has been **completely revised** to reflect the reality of MSIX app deployment.

### Original Approach (OUTDATED)
❌ Registry-based autostart (`HKEY_CURRENT_USER\...\Run`)  
❌ Startup folder shortcuts  
❌ Would NOT work with Microsoft Store deployment  

### Revised Approach (CORRECT)
✅ **Manifest-based autostart** using `Package.appxmanifest`  
✅ **Only method that works** with MSIX sandbox  
✅ **Required for Store deployment**  

---

## Why This Matters

### MSIX Registry Virtualization Problem
When your app is packaged as MSIX:
- The registry is virtualized/isolated
- Registry entries are invisible to external processes
- Windows can't launch apps from virtualized registry entries
- Task Manager can't see MSIX apps in the Startup tab

**Result:** Registry autostart simply doesn't work.

### The Manifest Solution
Adding a `StartupTask` extension to `Package.appxmanifest`:
- ✅ Bypasses registry entirely
- ✅ Works inside MSIX sandbox
- ✅ Integrates with Windows Startup system
- ✅ Appears in Settings → Apps → Startup apps
- ✅ Users can toggle on/off from Settings

**Result:** Autostart works perfectly!

---

## Implementation for Store Apps

### Quick Start (Copy-Paste)

**Add this to `Package.appxmanifest`:**

```xml
<Extensions>
  <desktop:Extension Category="windows.startupTask">
	<desktop:StartupTask Task="StartPowerPlanManager" Enabled="true" DisplayName="Power-Plan Manager" />
  </desktop:Extension>
</Extensions>
```

**That's it!**

- No C# code needed
- No registry modifications
- No AutostartManager class
- Just edit the manifest and build

### Time Investment
- ⏱️ **15 minutes total** (vs 90 minutes for registry approach)
- Edit manifest: 5 minutes
- Rebuild MSIX: 5 minutes
- Test: 5 minutes

---

## Key Takeaways

### ✅ For Microsoft Store
1. Use manifest-based `StartupTask`
2. Add extension to `Package.appxmanifest`
3. No code changes required
4. Much faster and cleaner

### ⚠️ DO NOT
- ❌ Do NOT use registry method with MSIX
- ❌ Do NOT implement AutostartManager for Store version
- ❌ Do NOT try UI checkboxes for manifest autostart
- ❌ Do NOT expect Task Manager control in Store version

### ✅ DO DO
- ✅ Do use manifest extension
- ✅ Do test with local MSIX install
- ✅ Do verify in Settings → Apps → Startup apps
- ✅ Do include proper namespaces in manifest

---

## Testing Manifest Autostart

After building MSIX:

```powershell
# 1. Install locally
Add-AppxPackage -Path "Power-Plan_Manager-2.0_2.0.0.0_x64.msix"

# 2. Verify in Settings
# Settings → Apps → Startup apps
# Look for "Power-Plan Manager"

# 3. Test autostart
# Close app completely
# Restart Windows
# App should launch automatically

# 4. Test disable
# Settings → Apps → Startup apps
# Toggle off "Power-Plan Manager"
# Restart - should NOT launch
# Toggle on - should launch again
```

---

## Updated Document Structure

The `/docs/autostart.md` now has:

1. **Overview** - MSIX-aware
2. **Critical Finding** - Why registry doesn't work
3. **⭐ KEY TAKEAWAY** - The manifest solution (read this first!)
4. **Current State** - Honest assessment
5. **Two Paths**
   - Path 1: MSIX/Store (recommended, 15 min)
   - Path 2: Standalone (optional, 90 min)
6. **Technical Details**
   - Method 1: Manifest approach (complete XML)
   - Method 2: Registry approach (for non-Store)
7. **Implementation Steps** - Separate for each path
8. **Comparison Table** - Manifest vs Registry
9. **MSIX-Specific Notes** - Why this approach works
10. **Testing Guide** - MSIX-specific procedures
11. **Risks & Mitigation** - Path-specific risks

---

## Files Modified

- ✅ `/docs/autostart.md` (775 lines, completely revised)
- ⏸️ `/docs/Store-Deployment.md` (references registry issue, should be noted)
- ⏸️ `/docs/PLANS_SUMMARY.md` (may want to update overview)

---

## Next Action

### For Store Deployment
1. Read the **KEY TAKEAWAY** section in updated `/docs/autostart.md`
2. Add the manifest extension to `Package.appxmanifest`
3. Build MSIX package
4. Test locally per the procedure
5. Submit to Store

### For Standalone Version (Optional)
1. Implement registry method if supporting non-Store distribution
2. Use conditional compilation (`#if !MSIX`) to separate code paths
3. Test both approaches thoroughly

---

## Before vs After

### Before (Outdated)
```csharp
// ❌ This won't work in MSIX
public class AutostartManager
{
	private const string AutostartKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
	// ... registry code that fails in sandbox
}
```

### After (Correct)
```xml
<!-- ✅ This works in MSIX -->
<Extensions>
  <desktop:Extension Category="windows.startupTask">
	<desktop:StartupTask Task="StartPowerPlanManager" Enabled="true" DisplayName="Power-Plan Manager" />
  </desktop:Extension>
</Extensions>
```

---

## Credit

This critical revision was based on your research findings:
- You discovered registry methods fail with MSIX after multiple attempts
- You found that manifest editing is the only reliable approach
- This insight was crucial to correcting the plan

**Thank you for catching this - it would have been a significant issue during Store submission!**

---

## Questions?

Refer to the updated `/docs/autostart.md`:
- Section: **"CRITICAL: MSIX-Specific Implementation Notes"**
- Section: **"Testing MSIX Autostart"**
- Section: **"Comparison: Manifest vs Registry Methods"**

All now include complete, accurate guidance for MSIX applications.

---

**Revision Status:** ✅ Complete  
**Plan Status:** Ready for implementation (use manifest approach for Store)  
**Next Step:** Add manifest extension to Package.appxmanifest
