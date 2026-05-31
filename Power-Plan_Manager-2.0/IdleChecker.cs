using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Power_Plan_Manager_Take_8
{
    public class IdleChecker : IDisposable
    {
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO usr);

        [DllImport("powrprof.dll")]
        static extern uint PowerSetActiveScheme(IntPtr RootPowerKey, ref Guid SchemeGuid);

        [DllImport("powrprof.dll")]
        static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("kernel32.dll")]
        static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("powrprof.dll")]
        static extern uint PowerDuplicateScheme(IntPtr RootPowerKey, ref Guid SourceSchemeGuid, out Guid DestinationSchemeGuid);

        [DllImport("powrprof.dll")]
        static extern uint PowerDeleteScheme(IntPtr RootPowerKey, ref Guid SchemeGuid);

        [DllImport("powrprof.dll")]
        static extern uint PowerWriteACValueIndex(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, uint AcValueIndex);

        [DllImport("powrprof.dll")]
        static extern uint PowerWriteDCValueIndex(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, uint DcValueIndex);

        [DllImport("powrprof.dll", CharSet = CharSet.Unicode)]
        static extern uint PowerWriteFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, string Buffer, uint BufferSize);

        // Processor Power Management subgroup GUID
        private static readonly Guid GUID_PROCESSOR_SUBGROUP = new Guid("54533251-82be-4824-96c1-47b60b740d00");
        // Maximum processor state setting GUID
        private static readonly Guid GUID_PROCESSOR_THROTTLE_MAXIMUM = new Guid("bc5038f7-23e0-4960-96da-33abaf5935ec");


        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        /// <summary>
        /// Default timeout (in seconds) before the system is considered idle.
        /// </summary>
        private const int DEFAULT_IDLE_TIMEOUT_SECONDS = 90;

        /// <summary>
        /// Default interval (in seconds) for checking user input when in idle mode.
        /// </summary>
        private const int DEFAULT_INPUT_CHECK_INTERVAL_SECONDS = 5;

        /// <summary>
        /// Gets the configured idle timeout in seconds from settings, or default if not set.
        /// </summary>
        private static int GetIdleTimeoutSeconds()
        {
            try
            {
                // Try to get from settings if property exists
                var idleTimeoutProp = Properties.Settings.Default.Properties["IdleTimeoutSeconds"];
                if (idleTimeoutProp != null)
                {
                    int value = (int)Properties.Settings.Default["IdleTimeoutSeconds"];
                    if (value > 0) return value;
                }
            }
            catch { /* Fall through to default */ }
            return DEFAULT_IDLE_TIMEOUT_SECONDS;
        }

        /// <summary>
        /// Gets the configured input check interval in seconds from settings, or default if not set.
        /// </summary>
        private static int GetInputCheckIntervalSeconds()
        {
            try
            {
                // Try to get from settings if property exists
                var inputCheckProp = Properties.Settings.Default.Properties["InputCheckIntervalSeconds"];
                if (inputCheckProp != null)
                {
                    int value = (int)Properties.Settings.Default["InputCheckIntervalSeconds"];
                    if (value > 0) return value;
                }
            }
            catch { /* Fall through to default */ }
            return DEFAULT_INPUT_CHECK_INTERVAL_SECONDS;
        }

        private bool disposed = false;
        private string activePowerPlan = "";
        private Guid? _idleThrottleSchemeGuid = null;
        private readonly object _throttleLock = new object();

        /// <summary>The system power plan that was active when the app started.</summary>
        public string SystemActivePlan => activePowerPlan;

        private System.Windows.Forms.Timer idleCheckTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer userInputCheckTimer = new System.Windows.Forms.Timer();



        public IdleChecker(string systemActivePlan)
        {
            // Get configured timeouts (or use defaults)
            int idleTimeoutSeconds = GetIdleTimeoutSeconds();
            int inputCheckIntervalSeconds = GetInputCheckIntervalSeconds();

            Logger.Log($"IdleChecker initialized with idle timeout: {idleTimeoutSeconds}s, input check interval: {inputCheckIntervalSeconds}s");

            // Store the system plan — do NOT switch the plan on startup
            activePowerPlan = systemActivePlan;
            Logger.Log($"System active power plan stored: {activePowerPlan}");
            idleCheckTimer.Interval = idleTimeoutSeconds * 1000;
            idleCheckTimer.Tick += IdleCheckTimer_Tick;
            idleCheckTimer.Start();

            userInputCheckTimer.Interval = inputCheckIntervalSeconds * 1000;
            userInputCheckTimer.Tick += UserInputCheckTimer_Tick;
        }

        private void IdleCheckTimer_Tick(object? sender, EventArgs e)
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);

            long idleTime = (Environment.TickCount64 - (long)lastInputInfo.dwTime) / 1000;

            if (idleTime >= idleCheckTimer.Interval / 1000 && Properties.Settings.Default.Enabled == true)
            {                
                ActivateIdleThrottlePlan(); // Energy Saver + 50% max CPU throttle
                idleCheckTimer.Stop();// Stop counting idle time and..
                userInputCheckTimer.Start(); // ..start checking for user input
            }            
        }

        private void UserInputCheckTimer_Tick(object? sender, EventArgs e)
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);

            long idleTime = (Environment.TickCount64 - (long)lastInputInfo.dwTime) / 1000;

            if (idleTime < userInputCheckTimer.Interval / 1000 && Properties.Settings.Default.Enabled == true)
            {
                CleanupIdleThrottlePlan(); // Delete throttle duplicate and restore active plan
                userInputCheckTimer.Stop(); // Stop checking for user input and..
                idleCheckTimer.Start(); // ..start counting idle time.
            }            
        }

        /// <summary>
        /// Returns the GUID string of the currently active Windows power scheme.
        /// Falls back to Ultimate Performance if the call fails.
        /// </summary>
        public static string GetSystemActivePlan()
        {
            try
            {
                uint result = PowerGetActiveScheme(IntPtr.Zero, out IntPtr guidPtr);
                if (result == 0 && guidPtr != IntPtr.Zero)
                {
                    try
                    {
                        Guid guid = (Guid)System.Runtime.InteropServices.Marshal.PtrToStructure(guidPtr, typeof(Guid))!;
                        return guid.ToString();
                    }
                    finally
                    {
                        LocalFree(guidPtr);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("IdleChecker.GetSystemActivePlan", ex);
            }

            // Fallback: use optimal high-performance plan
            return PowerPlanDetector.GetOptimalHighPerformancePlan();
        }

        /// <summary>
        /// Duplicates the Energy Saver scheme, caps max processor state to 50% on both
        /// AC and DC, names it "PPM-Idle-Throttle", and activates it.
        /// No elevation required — operates on a user-owned duplicate.
        /// </summary>
        private void ActivateIdleThrottlePlan()
        {
            if (disposed) return;
            Task.Run(() =>
            {
                try
                {
                    // Clean up any previous duplicate first
                    CleanupIdleThrottleDuplicate();

                    Guid sourcePlan = new Guid(Constants.EnergySaver);
                    uint result = PowerDuplicateScheme(IntPtr.Zero, ref sourcePlan, out Guid newScheme);
                    if (result != 0)
                    {
                        Logger.Log($"PowerDuplicateScheme failed: {result}. Falling back to plain Energy Saver.");
                        ChangePowerPlan(Constants.EnergySaver);
                        return;
                    }

                    Guid subgroup = GUID_PROCESSOR_SUBGROUP;
                    Guid setting  = GUID_PROCESSOR_THROTTLE_MAXIMUM;
                    const uint MAX_PROCESSOR_50 = 50;

                    PowerWriteACValueIndex(IntPtr.Zero, ref newScheme, ref subgroup, ref setting, MAX_PROCESSOR_50);
                    PowerWriteDCValueIndex(IntPtr.Zero, ref newScheme, ref subgroup, ref setting, MAX_PROCESSOR_50);

                    // Give it a recognizable name
                    PowerWriteFriendlyName(IntPtr.Zero, ref newScheme, IntPtr.Zero, IntPtr.Zero, "PPM-Idle-Throttle", (uint)("PPM-Idle-Throttle".Length + 1) * 2);

                    uint activateResult = PowerSetActiveScheme(IntPtr.Zero, ref newScheme);
                    if (activateResult == 0)
                    {
                        lock (_throttleLock)
                        {
                            _idleThrottleSchemeGuid = newScheme;
                        }
                        Logger.Log($"Idle throttle plan activated (50% max CPU): {newScheme}");
                    }
                    else
                    {
                        Logger.Log($"PowerSetActiveScheme failed for throttle plan: {activateResult}. Deleting duplicate.");
                        PowerDeleteScheme(IntPtr.Zero, ref newScheme);
                        ChangePowerPlan(Constants.EnergySaver);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException("ActivateIdleThrottlePlan", ex);
                    ChangePowerPlan(Constants.EnergySaver);
                }
            });
        }

        /// <summary>
        /// Deletes the temporary throttle duplicate (if any) and restores the stored active plan.
        /// </summary>
        private void CleanupIdleThrottlePlan()
        {
            if (disposed) return;
            Task.Run(() =>
            {
                try
                {
                    CleanupIdleThrottleDuplicate();

                    if (Guid.TryParse(activePowerPlan, out Guid restore))
                    {
                        uint result = PowerSetActiveScheme(IntPtr.Zero, ref restore);
                        if (result == 0)
                            Logger.Log($"Restored active plan: {activePowerPlan}");
                        else
                            Logger.Log($"PowerSetActiveScheme restore failed: {result}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException("CleanupIdleThrottlePlan", ex);
                }
            });
        }

        private void CleanupIdleThrottleDuplicate()
        {
            Guid? toDelete = null;
            lock (_throttleLock)
            {
                toDelete = _idleThrottleSchemeGuid;
                _idleThrottleSchemeGuid = null;
            }

            if (toDelete.HasValue)
            {
                Guid g = toDelete.Value;
                uint result = PowerDeleteScheme(IntPtr.Zero, ref g);
                Logger.Log($"Deleted idle throttle duplicate {g}: result={result}");
            }
        }

        public Task ChangePowerPlan(string powerPlanGuid)
        {
            if (disposed) return Task.CompletedTask;

            return Task.Run(() =>
            {
                try
                {
                    if (!Guid.TryParse(powerPlanGuid, out Guid guid))
                    {
                        Logger.Log($"Invalid power plan GUID: {powerPlanGuid}");
                        return;
                    }

                    uint result = PowerSetActiveScheme(IntPtr.Zero, ref guid);
                    if (result == 0)
                        Logger.Log($"Power plan changed to: {powerPlanGuid}");
                    else
                        Logger.Log($"PowerSetActiveScheme failed with error code: {result} for GUID: {powerPlanGuid}");
                }
                catch (Exception ex)
                {
                    Logger.LogException("ChangePowerPlan", ex);
                }
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                try { CleanupIdleThrottleDuplicate(); } catch (Exception ex) { Logger.LogException("Dispose.CleanupIdleThrottle", ex); }

                try
                {
                    if (idleCheckTimer != null)
                    {
                        idleCheckTimer.Stop();
                        idleCheckTimer.Tick -= IdleCheckTimer_Tick;
                        idleCheckTimer.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException("Dispose.idleCheckTimer", ex);
                }

                try
                {
                    if (userInputCheckTimer != null)
                    {
                        userInputCheckTimer.Stop();
                        userInputCheckTimer.Tick -= UserInputCheckTimer_Tick;
                        userInputCheckTimer.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException("Dispose.userInputCheckTimer", ex);
                }
            }
            disposed = true;
        }
    }
}
