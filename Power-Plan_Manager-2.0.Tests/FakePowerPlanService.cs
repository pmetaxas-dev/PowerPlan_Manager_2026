using Power_Plan_Manager_Take_8;

namespace Power_Plan_Manager_Take_8.Tests;

internal sealed class FakePowerPlanService : IPowerPlanService
{
    private readonly object sync = new();
    private readonly Dictionary<Guid, PowerPlanInfo> plans;

    public FakePowerPlanService(IEnumerable<PowerPlanInfo> initialPlans, Guid? activePlan)
    {
        plans = initialPlans.ToDictionary(plan => plan.Id);
        ActivePlan = activePlan;
    }

    public Guid? ActivePlan { get; private set; }
    public int DuplicateCallCount { get; private set; }
    public List<Guid> DeletedPlans { get; } = new();
    public List<Guid> ActivationHistory { get; } = new();
    public HashSet<Guid> DeleteFailures { get; } = new();
    public bool AllowCreateScheme { get; set; } = true;
    public bool AllowWriteProcessorState { get; set; } = true;
    public uint ProcessorStateWriteResult { get; set; }
    public uint FriendlyNameWriteResult { get; set; }

    public static FakePowerPlanService CreateDefault()
    {
        Guid balanced = Guid.Parse(Constants.Balanced);
        return new FakePowerPlanService(
            new[]
            {
                Plan(Constants.EnergySaver, "Power saver", 100, 100),
                Plan(
                    Constants.Balanced,
                    "Balanced",
                    100,
                    100,
                    PowerPlanPersonality.Balanced),
                Plan(
                    Constants.HighPerformance,
                    "High performance",
                    100,
                    100,
                    PowerPlanPersonality.MaximumPerformance),
                Plan(
                    Constants.UltimatePerformance,
                    "Ultimate Performance",
                    100,
                    100,
                    PowerPlanPersonality.MaximumPerformance)
            },
            balanced);
    }

    public static PowerPlanInfo Plan(
        string id,
        string name,
        uint? acValue,
        uint? dcValue,
        PowerPlanPersonality personality = PowerPlanPersonality.MaximumPowerSavings)
    {
        return new PowerPlanInfo(Guid.Parse(id), name, acValue, dcValue, personality);
    }

    public static PowerPlanInfo Plan(
        Guid id,
        string name,
        uint? acValue,
        uint? dcValue,
        PowerPlanPersonality personality = PowerPlanPersonality.MaximumPowerSavings)
    {
        return new PowerPlanInfo(id, name, acValue, dcValue, personality);
    }

    public IReadOnlyList<PowerPlanInfo> GetPlans()
    {
        lock (sync)
        {
            return plans.Values.ToList();
        }
    }

    public Guid? GetActivePlan()
    {
        lock (sync)
        {
            return ActivePlan;
        }
    }

    public uint SetActivePlan(Guid planId)
    {
        lock (sync)
        {
            if (!plans.ContainsKey(planId))
            {
                return 2;
            }

            ActivePlan = planId;
            ActivationHistory.Add(planId);
            return 0;
        }
    }

    public uint DuplicatePlan(Guid sourcePlanId, out Guid newPlanId)
    {
        lock (sync)
        {
            if (!plans.TryGetValue(sourcePlanId, out PowerPlanInfo? source))
            {
                newPlanId = Guid.Empty;
                return 2;
            }

            DuplicateCallCount++;
            newPlanId = Guid.NewGuid();
            plans[newPlanId] = source with { Id = newPlanId };
            return 0;
        }
    }

    public uint DeletePlan(Guid planId)
    {
        lock (sync)
        {
            if (DeleteFailures.Contains(planId))
            {
                return 5;
            }

            if (!plans.Remove(planId))
            {
                return 2;
            }

            DeletedPlans.Add(planId);
            return 0;
        }
    }

    public uint SetMaxProcessorState(Guid planId, uint value)
    {
        lock (sync)
        {
            if (ProcessorStateWriteResult != 0)
            {
                return ProcessorStateWriteResult;
            }

            if (!plans.TryGetValue(planId, out PowerPlanInfo? plan))
            {
                return 2;
            }

            plans[planId] = plan with
            {
                MaxProcessorStateAc = value,
                MaxProcessorStateDc = value
            };
            return 0;
        }
    }

    public uint SetFriendlyName(Guid planId, string name)
    {
        lock (sync)
        {
            if (FriendlyNameWriteResult != 0)
            {
                return FriendlyNameWriteResult;
            }

            if (!plans.TryGetValue(planId, out PowerPlanInfo? plan))
            {
                return 2;
            }

            plans[planId] = plan with { Name = name };
            return 0;
        }
    }

    public bool CanCreateScheme() => AllowCreateScheme;

    public bool CanWriteProcessorState() => AllowWriteProcessorState;
}

internal sealed class FakePowerPlanStateStore : IPowerPlanStateStore
{
    public string IdleThrottleGuid { get; set; } = string.Empty;
    public string NormalPlanGuid { get; set; } = string.Empty;
    public int LegacyCleanupVersion { get; set; }
    public int SaveCount { get; private set; }

    public void Save()
    {
        SaveCount++;
    }
}
