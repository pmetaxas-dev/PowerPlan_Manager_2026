using Power_Plan_Manager_Take_8;

namespace Power_Plan_Manager_Take_8.Tests;

[TestClass]
public class PowerPlanDetectorTests
{
    [TestMethod]
    public void Selection_RestoresSavedPlanAfterRestartWhileIdle()
    {
        Guid savedPlan = Guid.NewGuid();
        Guid idlePlan = Guid.NewGuid();
        var plans = new[]
        {
            Plan(savedPlan, "Vendor performance", PowerPlanPersonality.Balanced, 100),
            Plan(idlePlan, Constants.IdleThrottleName, PowerPlanPersonality.MaximumPowerSavings, 50)
        };

        Guid selected = PowerPlanDetector.SelectActiveUserPlan(
            plans,
            idlePlan,
            savedPlan,
            idlePlan);

        Assert.AreEqual(savedPlan, selected);
    }

    [TestMethod]
    public void Selection_PreservesCurrentGeneratedMaximumPerformancePlan()
    {
        Guid generatedUltimate = Guid.NewGuid();
        var plans = new[]
        {
            Plan(Constants.Balanced, "Εξισορρόπηση", PowerPlanPersonality.Balanced, 100),
            Plan(generatedUltimate, "Κορυφαίες επιδόσεις", PowerPlanPersonality.MaximumPerformance, 100)
        };

        Guid selected = PowerPlanDetector.SelectActiveUserPlan(
            plans,
            generatedUltimate,
            savedPlanId: null,
            idlePlanId: null);

        Assert.AreEqual(generatedUltimate, selected);
    }

    [TestMethod]
    public void Selection_PreservesCurrentNoncanonicalVendorPlanAtFullCpu()
    {
        Guid vendorPlan = Guid.NewGuid();
        var plans = new[]
        {
            Plan(Constants.Balanced, "Balanced", PowerPlanPersonality.Balanced, 100),
            Plan(Constants.HighPerformance, "High performance", PowerPlanPersonality.MaximumPerformance, 100),
            Plan(vendorPlan, "Vendor optimized", PowerPlanPersonality.Balanced, 100)
        };

        Guid selected = PowerPlanDetector.SelectActiveUserPlan(
            plans,
            vendorPlan,
            savedPlanId: null,
            idlePlanId: null);

        Assert.AreEqual(vendorPlan, selected);
    }

    [TestMethod]
    public void Selection_DoesNotPreferCanonicalBalancedOverMaximumPerformance()
    {
        Guid generatedPerformance = Guid.NewGuid();
        var plans = new[]
        {
            Plan(Constants.Balanced, "Balanced", PowerPlanPersonality.Balanced, 100),
            Plan(generatedPerformance, "Performance", PowerPlanPersonality.MaximumPerformance, 100)
        };

        Guid selected = PowerPlanDetector.SelectActiveUserPlan(
            plans,
            Guid.Parse(Constants.Balanced),
            savedPlanId: null,
            idlePlanId: null);

        Assert.AreEqual(generatedPerformance, selected);
    }

    [TestMethod]
    public void Selection_UsesBalancedWhenNoPerformancePlanExists()
    {
        var plans = new[]
        {
            Plan(Constants.Balanced, "Balanced", PowerPlanPersonality.Balanced, 100)
        };

        Guid selected = PowerPlanDetector.SelectActiveUserPlan(
            plans,
            currentPlanId: null,
            savedPlanId: null,
            idlePlanId: null);

        Assert.AreEqual(Guid.Parse(Constants.Balanced), selected);
    }

    [TestMethod]
    public void Eligibility_RejectsSavingsThrottledAndManagedIdlePlans()
    {
        Guid savings = Guid.NewGuid();
        Guid throttledPerformance = Guid.NewGuid();
        Guid managed = Guid.NewGuid();

        Assert.IsFalse(PowerPlanDetector.IsEligibleActiveUserPlan(
            Plan(savings, "Savings", PowerPlanPersonality.MaximumPowerSavings, 64),
            idlePlanId: null));
        Assert.IsFalse(PowerPlanDetector.IsEligibleActiveUserPlan(
            Plan(throttledPerformance, "Performance", PowerPlanPersonality.MaximumPerformance, 99),
            idlePlanId: null));
        Assert.IsFalse(PowerPlanDetector.IsEligibleActiveUserPlan(
            Plan(managed, Constants.IdleThrottleName, PowerPlanPersonality.Balanced, 100),
            managed));
    }

    [TestMethod]
    public void IsPowerPlanAvailable_InvalidGuid_ReturnsFalse()
    {
        Assert.IsFalse(PowerPlanDetector.IsPowerPlanAvailable("not-a-guid"));
    }

    [TestMethod]
    public void ConstantsGuids_AreValidAndUnique()
    {
        string[] values =
        {
            Constants.Balanced,
            Constants.HighPerformance,
            Constants.UltimatePerformance,
            Constants.EnergySaver,
            Constants.RyzenUniversal,
            Constants.RyzenPowerPlan
        };

        Assert.IsTrue(values.All(value => Guid.TryParse(value, out _)));
        Assert.AreEqual(values.Length, values.Distinct().Count());
    }

    private static PowerPlanInfo Plan(
        string id,
        string name,
        PowerPlanPersonality personality,
        uint acValue)
    {
        return Plan(Guid.Parse(id), name, personality, acValue);
    }

    private static PowerPlanInfo Plan(
        Guid id,
        string name,
        PowerPlanPersonality personality,
        uint acValue)
    {
        return new PowerPlanInfo(id, name, acValue, acValue, personality);
    }
}
