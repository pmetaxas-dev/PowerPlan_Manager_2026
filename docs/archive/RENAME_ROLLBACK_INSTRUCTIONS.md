# Project Rename Rollback Instructions

**Date:** May 16, 2026  
**Rename Operation:** Power-Plan_Manager-Take_8 → Power-Plan_Manager-2.0  
**Status:** ✅ SUCCESSFUL

## Quick Reference

If you need to rollback this rename operation, follow these steps:

---

## Backup Location

**Backup Directory:**
```
C:\Users\Dev_Panos\Desktop\Software Development\C# - Visual Studio\
  └─ Power-Plan_Manager-Sentinel(UPGRADE)_BACKUP_20260516_182910
```

This is a complete copy of the solution before any renaming occurred.

---

## Rollback Procedure (Option 1: Copy Backup)

### Fastest & Safest Method

1. **Stop Visual Studio** (if running)
   ```powershell
   # Close any open instances of Visual Studio
   ```

2. **Delete Current Solution** (optional - creates space)
   ```powershell
   cd "C:\Users\Dev_Panos\Desktop\Software Development\C# - Visual Studio\"
   Remove-Item -Path "Power-Plan_Manager-Sentinel(UPGRADE)" -Recurse -Force
   ```

3. **Restore from Backup**
   ```powershell
   cd "C:\Users\Dev_Panos\Desktop\Software Development\C# - Visual Studio\"
   Copy-Item -Path "Power-Plan_Manager-Sentinel(UPGRADE)_BACKUP_20260516_182910" `
			 -Destination "Power-Plan_Manager-Sentinel(UPGRADE)" -Recurse -Force
   ```

4. **Verify Rollback**
   ```powershell
   cd "Power-Plan_Manager-Sentinel(UPGRADE)"
   dir  # Should show: Power-Plan_Manager-Take_8, Power-Plan_Manager-Take_8.Tests
   dotnet build  # Verify build still works
   ```

5. **Cleanup Backup** (optional - after verification)
   ```powershell
   Remove-Item -Path "Power-Plan_Manager-Sentinel(UPGRADE)_BACKUP_20260516_182910" -Recurse -Force
   ```

---

## Rollback Procedure (Option 2: Manual Rename Back)

### If You Prefer to Manually Undo Changes

1. **Rename Folders Back**
   ```powershell
   cd "C:\Users\Dev_Panos\Desktop\Software Development\C# - Visual Studio\Power-Plan_Manager-Sentinel(UPGRADE)\"

   # Rename main project
   Rename-Item -Path "Power-Plan_Manager-2.0" -NewName "Power-Plan_Manager-Take_8"

   # Rename test project
   Rename-Item -Path "Power-Plan_Manager-2.0.Tests" -NewName "Power-Plan_Manager-Take_8.Tests"
   ```

2. **Update Solution File**
   ```powershell
   # Edit Power-Plan_Manager-Take_8.sln
   # Change this line:
   #   Project(...) = "Power-Plan_Manager-2.0", "Power-Plan_Manager-2.0\...
   # Back to:
   #   Project(...) = "Power-Plan_Manager-Take_8", "Power-Plan_Manager-Take_8\...
   ```

3. **Update .csproj Files**

   In `Power-Plan_Manager-Take_8\Power-Plan_Manager-Take_8.csproj`:
   ```xml
   <!-- Remove or change this line: -->
   <AssemblyName>Power-Plan_Manager-2.0</AssemblyName>
   <!-- Or revert to original if it had a different name -->
   ```

   In `Power-Plan_Manager-Take_8.Tests\Power-Plan_Manager-Take_8.Tests.csproj`:
   ```xml
   <!-- Change this line: -->
   <ProjectReference Include="..\Power-Plan_Manager-2.0\Power-Plan_Manager-Take_8.csproj" />
   <!-- Back to: -->
   <ProjectReference Include="..\Power-Plan_Manager-Take_8\Power-Plan_Manager-Take_8.csproj" />
   ```

4. **Verify and Test**
   ```powershell
   dotnet clean
   dotnet build
   dotnet test Power-Plan_Manager-Take_8.Tests
   ```

---

## What Was Changed

### Files/Folders Modified

1. **Folder Rename**
   - `Power-Plan_Manager-Take_8/` → `Power-Plan_Manager-2.0/`
   - `Power-Plan_Manager-Take_8.Tests/` → `Power-Plan_Manager-2.0.Tests/`

2. **Solution File** (Power-Plan_Manager-Take_8.sln)
   - Updated project display name
   - Updated project path reference

3. **.csproj Files**
   - `Power-Plan_Manager-2.0/Power-Plan_Manager-Take_8.csproj`:
	 - Added: `<AssemblyName>Power-Plan_Manager-2.0</AssemblyName>`
   - `Power-Plan_Manager-2.0.Tests/Power-Plan_Manager-Take_8.Tests.csproj`:
	 - Updated project reference path

4. **InternalsVisibleTo.cs**
   - No changes needed (assembly name reference unchanged)

### Code Changes

- **No C# code was modified**
- **Namespace unchanged** (still `Power_Plan_Manager_Take_8`)
- **All functionality identical**
- **All tests still pass**

---

## Important Notes

1. **Assembly Name vs Project Folder Name**
   - Project Folder: `Power-Plan_Manager-2.0`
   - Assembly Name (DLL): `Power-Plan_Manager-2.0.dll`
   - .csproj File Name: Unchanged (`Power-Plan_Manager-Take_8.csproj`)
   - Namespace: Unchanged (`Power_Plan_Manager_Take_8`)

2. **Why Keep .csproj File Name?**
   - Avoids extra confusion
   - Project references still work
   - Solution file specifies correct path

3. **Build Output**
   - Output DLL is now: `Power-Plan_Manager-2.0.dll`
   - Location: `Power-Plan_Manager-2.0\bin\Debug\net8.0-windows7.0\`

---

## Verification Checklist

After rollback, verify:

- ✅ Solution file loads in Visual Studio
- ✅ Both projects visible in Solution Explorer
- ✅ Build succeeds with 0 errors
- ✅ All tests pass (13 passing, 0 failing)
- ✅ No broken references

---

## Support

If rollback fails:

1. Check file permissions (may need admin)
2. Ensure Visual Studio is closed
3. Verify backup exists: `dir "Power-Plan_Manager-Sentinel(UPGRADE)_BACKUP_20260516_182910"`
4. Try Option 1 (copy from backup) rather than manual undo

---

## Delete Backup (After Confirmation)

Once you've verified the rename is working correctly and no longer need the backup:

```powershell
cd "C:\Users\Dev_Panos\Desktop\Software Development\C# - Visual Studio\"
Remove-Item -Path "Power-Plan_Manager-Sentinel(UPGRADE)_BACKUP_20260516_182910" -Recurse -Force
Write-Host "✅ Backup deleted successfully"
```

---

**Backup Size:** ~100+ MB (includes build artifacts, .vs folder, etc.)  
**Keep Backup For:** Until you're completely confident the rename is permanent  
**Backup Path:** `C:\Users\Dev_Panos\Desktop\Software Development\C# - Visual Studio\Power-Plan_Manager-Sentinel(UPGRADE)_BACKUP_20260516_182910`

---

**Generated:** May 16, 2026  
**Operation:** Non-destructive Project Rename  
**Status:** ✅ SUCCESSFUL
