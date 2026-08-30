using System.Runtime.InteropServices;

namespace Power_Plan_Manager_Take_8;

public class IdleChecker : IDisposable
{
    private const int DefaultIdleTimeoutSeconds = 90;
    private const int DefaultInputCheckIntervalSeconds = 5;

    private readonly IPowerPlanService powerPlans;
    private readonly IPowerPlanStateStore stateStore;
    private readonly object powerOperationLock = new();
    private readonly Guid normalPlanId;
    private readonly System.Windows.Forms.Timer idleCheckTimer = new();
    private readonly System.Windows.Forms.Timer userInputCheckTimer = new();

    private Guid? idleThrottlePlanId;
    private volatile bool disposed;

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    public IdleChecker(string systemActivePlan)
        : this(
            ParseNormalPlan(systemActivePlan),
            new WindowsPowerPlanService(),
            new SettingsPowerPlanStateStore(),
            startTimers: true)
    {
    }

    internal IdleChecker(
        Guid normalPlan,
        IPowerPlanService powerPlanService,
        IPowerPlanStateStore powerPlanStateStore,
        bool startTimers)
    {
        normalPlanId = normalPlan;
        powerPlans = powerPlanService ?? throw new ArgumentNullException(nameof(powerPlanService));
        stateStore = powerPlanStateStore ?? throw new ArgumentNullException(nameof(powerPlanStateStore));

        int idleTimeoutSeconds = GetIdleTimeoutSeconds();
        int inputCheckIntervalSeconds = GetInputCheckIntervalSeconds();

        Logger.Log(
            $"IdleChecker initialized with idle timeout: {idleTimeoutSeconds}s, "
            + $"input check interval: {inputCheckIntervalSeconds}s");
        Logger.Log($"Normal power plan stored: {normalPlanId}");

        InitializePowerPlanLifecycle();

        idleCheckTimer.Interval = idleTimeoutSeconds * 1000;
        idleCheckTimer.Tick += IdleCheckTimer_Tick;

        userInputCheckTimer.Interval = inputCheckIntervalSeconds * 1000;
        userInputCheckTimer.Tick += UserInputCheckTimer_Tick;

        if (startTimers)
        {
            idleCheckTimer.Start();
        }
    }

    public string SystemActivePlan => normalPlanId.ToString();

    internal Guid? IdleThrottlePlanId => idleThrottlePlanId;

    public static string GetSystemActivePlan()
    {
        try
        {
            var service = new WindowsPowerPlanService();
            Guid? activePlan = service.GetActivePlan();
            if (activePlan.HasValue)
            {
                return activePlan.Value.ToString();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException("IdleChecker.GetSystemActivePlan", ex);
        }

        return PowerPlanDetector.GetOptimalHighPerformancePlan();
    }

    public Task ChangePowerPlan(string powerPlanGuid)
    {
        if (!Guid.TryParse(powerPlanGuid, out Guid planId))
        {
            Logger.Log($"Invalid power plan GUID: {powerPlanGuid}");
            return Task.CompletedTask;
        }

        return RunPowerOperationAsync(
            "ChangePowerPlan",
            () => ActivatePlan(planId, "Power plan changed"));
    }

    internal Task EnterIdleModeAsync()
    {
        return RunPowerOperationAsync("EnterIdleMode", ActivateIdleThrottlePlan);
    }

    internal Task ExitIdleModeAsync()
    {
        return RunPowerOperationAsync(
            "ExitIdleMode",
            () => ActivatePlan(normalPlanId, "Normal power plan restored"));
    }

    private static Guid ParseNormalPlan(string systemActivePlan)
    {
        if (Guid.TryParse(systemActivePlan, out Guid normalPlan))
        {
            return normalPlan;
        }

        return Guid.Parse(PowerPlanDetector.GetOptimalHighPerformancePlan());
    }

    private void InitializePowerPlanLifecycle()
    {
        lock (powerOperationLock)
        {
            uint activateResult = powerPlans.SetActivePlan(normalPlanId);
            if (activateResult != 0)
            {
                Logger.Log(
                    $"Unable to activate normal plan {normalPlanId} during startup: "
                    + $"result={activateResult}. Cleanup and provisioning skipped.");
                return;
            }

            PersistNormalPlan(normalPlanId);
            idleThrottlePlanId = FindOrCreateIdleThrottlePlan();
            if (idleThrottlePlanId.HasValue)
            {
                RunLegacyCleanupOnce(idleThrottlePlanId.Value);
            }
            else
            {
                Logger.Log(
                    "Legacy cleanup skipped because no verified idle plan is available to retain.");
            }
        }
    }

    private void RunLegacyCleanupOnce(Guid retainedIdlePlanId)
    {
        if (stateStore.LegacyCleanupVersion >= Constants.LegacyCleanupVersion)
        {
            return;
        }

        try
        {
            IReadOnlyList<PowerPlanInfo> plans = powerPlans.GetPlans();
            List<PowerPlanInfo> candidates = plans
                .Where(plan => IsCleanupCandidate(
                    plan,
                    retainedIdlePlanId,
                    normalPlanId))
                .ToList();

            bool allDeletesSucceeded = true;
            foreach (PowerPlanInfo candidate in candidates)
            {
                uint result = powerPlans.DeletePlan(candidate.Id);
                Logger.Log(
                    $"Legacy cleanup delete {candidate.Id} ('{candidate.Name}'): "
                    + $"result={result}");
                allDeletesSucceeded &= result == 0;
            }

            IReadOnlyList<PowerPlanInfo> remainingPlans = powerPlans.GetPlans();
            bool candidatesRemain = remainingPlans.Any(
                plan => IsCleanupCandidate(
                    plan,
                    retainedIdlePlanId,
                    normalPlanId));

            if (allDeletesSucceeded && !candidatesRemain)
            {
                stateStore.LegacyCleanupVersion = Constants.LegacyCleanupVersion;
                stateStore.Save();
                Logger.Log(
                    $"Legacy power-plan cleanup version {Constants.LegacyCleanupVersion} "
                    + "completed and verified.");
            }
            else
            {
                Logger.Log(
                    "Legacy power-plan cleanup was not verified; it will retry on next startup.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogException("IdleChecker.RunLegacyCleanupOnce", ex);
        }
    }

    internal static bool IsCleanupCandidate(
        PowerPlanInfo plan,
        Guid retainedIdlePlanId,
        Guid normalPlanId)
    {
        if (plan.Id == Guid.Parse(Constants.EnergySaver)
            || plan.Id == retainedIdlePlanId
            || plan.Id == normalPlanId)
        {
            return false;
        }

        return plan.Personality == PowerPlanPersonality.MaximumPowerSavings
            && plan.MaxProcessorStateAc.HasValue
            && plan.MaxProcessorStateAc.Value < 100;
    }

    private Guid? FindOrCreateIdleThrottlePlan()
    {
        try
        {
            IReadOnlyList<PowerPlanInfo> plans = powerPlans.GetPlans();

            if (Guid.TryParse(stateStore.IdleThrottleGuid, out Guid persistedPlanId))
            {
                PowerPlanInfo? persistedPlan = plans.FirstOrDefault(
                    plan => plan.Id == persistedPlanId);

                if (persistedPlan is not null && IsValidIdleThrottlePlan(persistedPlan))
                {
                    RemoveExtraManagedPlans(plans, persistedPlan.Id);
                    return persistedPlan.Id;
                }
            }

            List<PowerPlanInfo> managedPlans = plans
                .Where(plan => string.Equals(
                    plan.Name,
                    Constants.IdleThrottleName,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            PowerPlanInfo? reusablePlan = managedPlans.FirstOrDefault(IsValidIdleThrottlePlan);
            if (reusablePlan is not null)
            {
                RemoveExtraManagedPlans(plans, reusablePlan.Id);
                PersistIdleThrottlePlan(reusablePlan.Id);
                return reusablePlan.Id;
            }

            if (managedPlans.Count > 0)
            {
                PowerPlanInfo repairCandidate = managedPlans[0];
                uint repairStateResult = powerPlans.SetMaxProcessorState(
                    repairCandidate.Id,
                    Constants.IdleMaxProcessorState);
                uint repairNameResult = powerPlans.SetFriendlyName(
                    repairCandidate.Id,
                    Constants.IdleThrottleName);

                PowerPlanInfo? repairedPlan = powerPlans.GetPlans()
                    .FirstOrDefault(plan => plan.Id == repairCandidate.Id);
                if (repairStateResult == 0
                    && repairNameResult == 0
                    && repairedPlan is not null
                    && IsValidIdleThrottlePlan(repairedPlan))
                {
                    RemoveExtraManagedPlans(plans, repairedPlan.Id);
                    PersistIdleThrottlePlan(repairedPlan.Id);
                    Logger.Log($"Managed idle plan repaired and reused: {repairedPlan.Id}");
                    return repairedPlan.Id;
                }
            }

            bool invalidManagedPlanRemains = false;
            foreach (PowerPlanInfo invalidManagedPlan in managedPlans)
            {
                if (!IsCleanupCandidate(
                    invalidManagedPlan,
                    retainedIdlePlanId: Guid.Empty,
                    normalPlanId))
                {
                    Logger.Log(
                        $"Preserved managed-name plan {invalidManagedPlan.Id} because "
                        + "it does not match the legacy cleanup signature.");
                    invalidManagedPlanRemains = true;
                    continue;
                }

                uint deleteResult = powerPlans.DeletePlan(invalidManagedPlan.Id);
                Logger.Log(
                    $"Removed invalid managed idle plan {invalidManagedPlan.Id}: "
                    + $"result={deleteResult}");
                invalidManagedPlanRemains |= deleteResult != 0;
            }

            if (invalidManagedPlanRemains)
            {
                Logger.Log(
                    "An invalid managed-name plan was preserved or could not be removed; "
                    + "new plan creation was skipped to prevent accumulation.");
                return null;
            }

            IReadOnlyList<PowerPlanInfo> adoptionPlans = powerPlans.GetPlans();
            PowerPlanInfo? reusableSavingsPlan = adoptionPlans
                .Where(IsReusableSavingsPlan)
                .OrderByDescending(IsValidIdleThrottlePlan)
                .ThenBy(plan => plan.Id)
                .FirstOrDefault();

            if (reusableSavingsPlan is not null)
            {
                if (!IsValidIdleThrottlePlan(reusableSavingsPlan))
                {
                    if (!powerPlans.CanWriteProcessorState())
                    {
                        Logger.Log(
                            $"Cannot configure retained savings plan {reusableSavingsPlan.Id}; "
                            + "idle-plan adoption and cleanup skipped.");
                        return null;
                    }

                    uint configureResult = powerPlans.SetMaxProcessorState(
                        reusableSavingsPlan.Id,
                        Constants.IdleMaxProcessorState);
                    if (configureResult != 0)
                    {
                        Logger.Log(
                            $"Failed to configure retained savings plan {reusableSavingsPlan.Id}: "
                            + $"result={configureResult}.");
                        return null;
                    }

                    reusableSavingsPlan = powerPlans.GetPlans().FirstOrDefault(
                        plan => plan.Id == reusableSavingsPlan.Id);
                }

                if (reusableSavingsPlan is not null
                    && IsValidIdleThrottlePlan(reusableSavingsPlan))
                {
                    PersistIdleThrottlePlan(reusableSavingsPlan.Id);
                    Logger.Log(
                        $"Existing savings plan retained for idle use: {reusableSavingsPlan.Id}");
                    return reusableSavingsPlan.Id;
                }

                Logger.Log("Retained savings plan failed verification; cleanup skipped.");
                return null;
            }

            if (!powerPlans.CanCreateScheme() || !powerPlans.CanWriteProcessorState())
            {
                Logger.Log(
                    "Current user or policy does not allow creation/configuration "
                    + "of the managed idle plan.");
                return null;
            }

            Guid energySaver = Guid.Parse(Constants.EnergySaver);
            Guid balanced = Guid.Parse(Constants.Balanced);
            Guid sourcePlan = plans.Any(plan => plan.Id == energySaver)
                ? energySaver
                : balanced;

            if (!plans.Any(plan => plan.Id == sourcePlan))
            {
                Logger.Log("No suitable source plan exists for idle-plan provisioning.");
                return null;
            }

            uint duplicateResult = powerPlans.DuplicatePlan(sourcePlan, out Guid newPlanId);
            if (duplicateResult != 0 || newPlanId == Guid.Empty)
            {
                Logger.Log($"Managed idle-plan duplication failed: result={duplicateResult}");
                return null;
            }

            uint maxStateResult = powerPlans.SetMaxProcessorState(
                newPlanId,
                Constants.IdleMaxProcessorState);
            uint nameResult = powerPlans.SetFriendlyName(
                newPlanId,
                Constants.IdleThrottleName);

            if (maxStateResult != 0 || nameResult != 0)
            {
                Logger.Log(
                    $"Managed idle-plan configuration failed: max={maxStateResult}, "
                    + $"name={nameResult}. Deleting {newPlanId}.");
                powerPlans.DeletePlan(newPlanId);
                return null;
            }

            PowerPlanInfo? verifiedPlan = powerPlans.GetPlans()
                .FirstOrDefault(plan => plan.Id == newPlanId);
            if (verifiedPlan is null || !IsValidIdleThrottlePlan(verifiedPlan))
            {
                Logger.Log($"Managed idle plan {newPlanId} failed verification; deleting it.");
                powerPlans.DeletePlan(newPlanId);
                return null;
            }

            PersistIdleThrottlePlan(newPlanId);
            Logger.Log($"Managed idle plan provisioned once: {newPlanId}");
            return newPlanId;
        }
        catch (Exception ex)
        {
            Logger.LogException("IdleChecker.FindOrCreateIdleThrottlePlan", ex);
            return null;
        }
    }

    private static bool IsValidIdleThrottlePlan(PowerPlanInfo plan)
    {
        return plan.MaxProcessorStateAc == Constants.IdleMaxProcessorState
            && plan.MaxProcessorStateDc == Constants.IdleMaxProcessorState;
    }

    private bool IsReusableSavingsPlan(PowerPlanInfo plan)
    {
        return plan.Id != Guid.Parse(Constants.EnergySaver)
            && plan.Id != normalPlanId
            && plan.Personality == PowerPlanPersonality.MaximumPowerSavings
            && plan.MaxProcessorStateAc.HasValue
            && plan.MaxProcessorStateAc.Value < 100;
    }

    private void RemoveExtraManagedPlans(
        IReadOnlyList<PowerPlanInfo> plans,
        Guid planToKeep)
    {
        foreach (PowerPlanInfo extraPlan in plans.Where(
                     plan => plan.Id != planToKeep
                         && IsCleanupCandidate(plan, planToKeep, normalPlanId)
                         && string.Equals(
                             plan.Name,
                             Constants.IdleThrottleName,
                             StringComparison.OrdinalIgnoreCase)))
        {
            uint result = powerPlans.DeletePlan(extraPlan.Id);
            Logger.Log($"Removed extra managed idle plan {extraPlan.Id}: result={result}");
        }
    }

    private void PersistIdleThrottlePlan(Guid planId)
    {
        stateStore.IdleThrottleGuid = planId.ToString();
        stateStore.Save();
    }

    private void PersistNormalPlan(Guid planId)
    {
        string value = planId.ToString();
        if (string.Equals(
            stateStore.NormalPlanGuid,
            value,
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        stateStore.NormalPlanGuid = value;
        stateStore.Save();
    }

    private void ActivateIdleThrottlePlan()
    {
        IReadOnlyList<PowerPlanInfo> plans = powerPlans.GetPlans();
        if (!idleThrottlePlanId.HasValue
            || !plans.Any(plan => plan.Id == idleThrottlePlanId.Value
                && IsValidIdleThrottlePlan(plan)))
        {
            idleThrottlePlanId = FindOrCreateIdleThrottlePlan();
        }

        if (idleThrottlePlanId.HasValue)
        {
            ActivatePlan(idleThrottlePlanId.Value, "Idle throttle plan activated");
            return;
        }

        Guid energySaver = Guid.Parse(Constants.EnergySaver);
        if (plans.Any(plan => plan.Id == energySaver))
        {
            ActivatePlan(
                energySaver,
                "Managed idle plan unavailable; built-in Power Saver activated");
        }
    }

    private void ActivatePlan(Guid planId, string successMessage)
    {
        uint result = powerPlans.SetActivePlan(planId);
        if (result == 0)
        {
            Logger.Log($"{successMessage}: {planId}");
        }
        else
        {
            Logger.Log($"PowerSetActiveScheme failed for {planId}: result={result}");
        }
    }

    private Task RunPowerOperationAsync(string operationName, Action operation)
    {
        if (disposed)
        {
            return Task.CompletedTask;
        }

        return Task.Run(() =>
        {
            lock (powerOperationLock)
            {
                if (disposed)
                {
                    return;
                }

                try
                {
                    operation();
                }
                catch (Exception ex)
                {
                    Logger.LogException($"IdleChecker.{operationName}", ex);
                }
            }
        });
    }

    private async void IdleCheckTimer_Tick(object? sender, EventArgs e)
    {
        long idleTimeSeconds = GetIdleTimeSeconds();
        if (idleTimeSeconds >= idleCheckTimer.Interval / 1000
            && Properties.Settings.Default.Enabled)
        {
            idleCheckTimer.Stop();
            await EnterIdleModeAsync();
            if (!disposed)
            {
                userInputCheckTimer.Start();
            }
        }
    }

    private async void UserInputCheckTimer_Tick(object? sender, EventArgs e)
    {
        long idleTimeSeconds = GetIdleTimeSeconds();
        if (idleTimeSeconds < userInputCheckTimer.Interval / 1000
            && Properties.Settings.Default.Enabled)
        {
            userInputCheckTimer.Stop();
            await ExitIdleModeAsync();
            if (!disposed)
            {
                idleCheckTimer.Start();
            }
        }
    }

    private static long GetIdleTimeSeconds()
    {
        var lastInputInfo = new LastInputInfo
        {
            Size = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        if (!GetLastInputInfo(ref lastInputInfo))
        {
            return 0;
        }

        uint elapsedMilliseconds = unchecked((uint)Environment.TickCount - lastInputInfo.Time);
        return elapsedMilliseconds / 1000;
    }

    private static int GetIdleTimeoutSeconds()
    {
        try
        {
            var property = Properties.Settings.Default.Properties["IdleTimeoutSeconds"];
            if (property is not null)
            {
                int value = (int)Properties.Settings.Default["IdleTimeoutSeconds"];
                if (value > 0)
                {
                    return value;
                }
            }
        }
        catch
        {
            // Use the default.
        }

        return DefaultIdleTimeoutSeconds;
    }

    private static int GetInputCheckIntervalSeconds()
    {
        try
        {
            var property = Properties.Settings.Default.Properties["InputCheckIntervalSeconds"];
            if (property is not null)
            {
                int value = (int)Properties.Settings.Default["InputCheckIntervalSeconds"];
                if (value > 0)
                {
                    return value;
                }
            }
        }
        catch
        {
            // Use the default.
        }

        return DefaultInputCheckIntervalSeconds;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            idleCheckTimer.Stop();
            idleCheckTimer.Tick -= IdleCheckTimer_Tick;
            idleCheckTimer.Dispose();

            userInputCheckTimer.Stop();
            userInputCheckTimer.Tick -= UserInputCheckTimer_Tick;
            userInputCheckTimer.Dispose();

            lock (powerOperationLock)
            {
                ActivatePlan(normalPlanId, "Normal power plan restored during shutdown");
                disposed = true;
            }
        }
        else
        {
            disposed = true;
        }
    }
}
