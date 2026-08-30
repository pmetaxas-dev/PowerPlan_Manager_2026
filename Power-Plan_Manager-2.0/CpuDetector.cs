namespace Power_Plan_Manager_Take_8;

/// <summary>
/// Selects a stable Active User plan without relying on localized names or
/// machine-specific scheme GUIDs.
/// </summary>
public static class PowerPlanDetector
{
    public static bool IsPowerPlanAvailable(string powerPlanGuid)
    {
        if (!Guid.TryParse(powerPlanGuid, out Guid requestedPlan))
        {
            return false;
        }

        try
        {
            var service = new WindowsPowerPlanService();
            return service.GetPlans().Any(plan => plan.Id == requestedPlan);
        }
        catch (Exception ex)
        {
            Logger.LogException("PowerPlanDetector.IsPowerPlanAvailable", ex);
            return false;
        }
    }

    public static string GetOptimalHighPerformancePlan()
    {
        try
        {
            var service = new WindowsPowerPlanService();
            var state = new SettingsPowerPlanStateStore();
            IReadOnlyList<PowerPlanInfo> plans = service.GetPlans();

            return SelectActiveUserPlan(
                    plans,
                    service.GetActivePlan(),
                    ParseGuid(state.NormalPlanGuid),
                    ParseGuid(state.IdleThrottleGuid))
                .ToString();
        }
        catch (Exception ex)
        {
            Logger.LogException("PowerPlanDetector.GetOptimalHighPerformancePlan", ex);
            return Constants.Balanced;
        }
    }

    internal static Guid SelectActiveUserPlan(
        IReadOnlyList<PowerPlanInfo> installedPlans,
        Guid? currentPlanId,
        Guid? savedPlanId,
        Guid? idlePlanId)
    {
        if (installedPlans.Count == 0)
        {
            throw new InvalidOperationException("No Windows power plans are installed.");
        }

        PowerPlanInfo? savedPlan = FindEligibleActiveUserPlan(
            installedPlans,
            savedPlanId,
            idlePlanId);
        if (savedPlan is not null)
        {
            return savedPlan.Id;
        }

        PowerPlanInfo? currentPlan = FindEligibleActiveUserPlan(
            installedPlans,
            currentPlanId,
            idlePlanId);

        bool currentIsPreferred = currentPlan is not null
            && (currentPlan.Personality == PowerPlanPersonality.MaximumPerformance
                || currentPlan.Id != Guid.Parse(Constants.Balanced));
        if (currentIsPreferred)
        {
            return currentPlan!.Id;
        }

        List<PowerPlanInfo> maximumPerformancePlans = installedPlans
            .Where(plan => IsEligibleActiveUserPlan(plan, idlePlanId)
                && plan.Personality == PowerPlanPersonality.MaximumPerformance)
            .OrderBy(plan => plan.Id)
            .ToList();

        Guid ultimateTemplate = Guid.Parse(Constants.UltimatePerformance);
        PowerPlanInfo? canonicalUltimate = maximumPerformancePlans.FirstOrDefault(
            plan => plan.Id == ultimateTemplate);
        if (canonicalUltimate is not null)
        {
            return canonicalUltimate.Id;
        }

        if (maximumPerformancePlans.Count > 0)
        {
            return maximumPerformancePlans[0].Id;
        }

        if (currentPlan is not null)
        {
            return currentPlan.Id;
        }

        Guid balanced = Guid.Parse(Constants.Balanced);
        PowerPlanInfo? canonicalBalanced = installedPlans.FirstOrDefault(
            plan => plan.Id == balanced && IsEligibleActiveUserPlan(plan, idlePlanId));
        if (canonicalBalanced is not null)
        {
            return canonicalBalanced.Id;
        }

        PowerPlanInfo? fallback = installedPlans
            .Where(plan => IsEligibleActiveUserPlan(plan, idlePlanId))
            .OrderBy(plan => plan.Id)
            .FirstOrDefault();

        return fallback?.Id
            ?? throw new InvalidOperationException(
                "No suitable Active User power plan is installed.");
    }

    internal static bool IsEligibleActiveUserPlan(
        PowerPlanInfo plan,
        Guid? idlePlanId)
    {
        if (plan.Id == Guid.Parse(Constants.EnergySaver)
            || plan.Id == idlePlanId
            || plan.MaxProcessorStateAc != 100
            || plan.Personality == PowerPlanPersonality.MaximumPowerSavings)
        {
            return false;
        }

        return !string.Equals(
            plan.Name,
            Constants.IdleThrottleName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static PowerPlanInfo? FindEligibleActiveUserPlan(
        IReadOnlyList<PowerPlanInfo> plans,
        Guid? planId,
        Guid? idlePlanId)
    {
        if (!planId.HasValue)
        {
            return null;
        }

        PowerPlanInfo? plan = plans.FirstOrDefault(candidate => candidate.Id == planId.Value);
        return plan is not null && IsEligibleActiveUserPlan(plan, idlePlanId)
            ? plan
            : null;
    }

    internal static Guid? ParseGuid(string value)
    {
        return Guid.TryParse(value, out Guid parsed) ? parsed : null;
    }
}
