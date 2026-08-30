using Power_Plan_Manager_Take_8;

namespace Power_Plan_Manager_Take_8.Tests;

[TestClass]
public class PowerPlanLifecycleIntegrationTests
{
    private static readonly Guid NormalPlan = Guid.Parse(Constants.HighPerformance);

    [TestMethod]
    public async Task IdleAndActiveCycle_UsesManagedPlanThenRestoresNormalPlan()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();
        using var checker = new IdleChecker(NormalPlan, service, state, startTimers: false);
        Guid managedPlan = checker.IdleThrottlePlanId!.Value;

        await checker.EnterIdleModeAsync();
        Assert.AreEqual(managedPlan, service.ActivePlan);

        await checker.ExitIdleModeAsync();
        Assert.AreEqual(NormalPlan, service.ActivePlan);
    }

    [TestMethod]
    public async Task RestartWhileIdle_RestoresNormalAndReusesManagedPlan()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();
        var firstChecker = new IdleChecker(NormalPlan, service, state, startTimers: false);
        Guid managedPlan = firstChecker.IdleThrottlePlanId!.Value;

        await firstChecker.EnterIdleModeAsync();
        Assert.AreEqual(managedPlan, service.ActivePlan);

        using var restartedChecker = new IdleChecker(
            NormalPlan,
            service,
            state,
            startTimers: false);

        Assert.AreEqual(NormalPlan, service.ActivePlan);
        Assert.AreEqual(managedPlan, restartedChecker.IdleThrottlePlanId);
        Assert.AreEqual(1, service.DuplicateCallCount);

        firstChecker.Dispose();
    }

    [TestMethod]
    public void FormAndIdleChecker_InitializeWithoutNativePowerCalls()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();

        using var form = new Form1(service, state);

        Assert.IsNotNull(form);
        Assert.AreEqual(Guid.Parse(Constants.UltimatePerformance), service.ActivePlan);
        Assert.AreEqual(1, service.DuplicateCallCount);
    }

    [TestMethod]
    public async Task RapidTransitions_DoNotAccumulatePlans()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();
        using var checker = new IdleChecker(NormalPlan, service, state, startTimers: false);

        for (int index = 0; index < 25; index++)
        {
            await checker.EnterIdleModeAsync();
            await checker.ExitIdleModeAsync();
        }

        Assert.AreEqual(1, service.DuplicateCallCount);
        Assert.AreEqual(1, service.GetPlans().Count(
            plan => plan.Name == Constants.IdleThrottleName));
    }

    [TestMethod]
    public void CleanupAndProvisioning_CompleteBeforeIdlePlanIsUsed()
    {
        var plans = FakePowerPlanService.CreateDefault().GetPlans().ToList();
        Guid retainedDuplicate = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid redundantDuplicate = Guid.Parse("22222222-2222-2222-2222-222222222222");
        plans.Add(FakePowerPlanService.Plan(
            retainedDuplicate,
            "Εξοικονόμηση ενέργειας",
            64,
            54,
            PowerPlanPersonality.MaximumPowerSavings));
        plans.Add(FakePowerPlanService.Plan(
            redundantDuplicate,
            "Εξοικονόμηση ενέργειας",
            64,
            54,
            PowerPlanPersonality.MaximumPowerSavings));

        var service = new FakePowerPlanService(plans, retainedDuplicate);
        var state = new FakePowerPlanStateStore();
        using var checker = new IdleChecker(NormalPlan, service, state, startTimers: false);

        Assert.AreEqual(NormalPlan, service.ActivationHistory.First());
        Assert.AreEqual(retainedDuplicate, checker.IdleThrottlePlanId);
        Assert.Contains(redundantDuplicate, service.DeletedPlans);
        Assert.AreEqual(Constants.LegacyCleanupVersion, state.LegacyCleanupVersion);
    }
}
