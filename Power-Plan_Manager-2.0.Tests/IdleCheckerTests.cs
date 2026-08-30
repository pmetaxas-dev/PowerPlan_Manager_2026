using Power_Plan_Manager_Take_8;

namespace Power_Plan_Manager_Take_8.Tests;

[TestClass]
public class IdleCheckerTests
{
    private static readonly Guid NormalPlan = Guid.Parse(Constants.HighPerformance);

    [TestMethod]
    public void Constructor_ProvisionsOneReusableIdlePlan()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();

        using var checker = CreateChecker(service, state);

        Assert.AreEqual(1, service.DuplicateCallCount);
        Assert.IsTrue(checker.IdleThrottlePlanId.HasValue);

        PowerPlanInfo idlePlan = service.GetPlans().Single(
            plan => plan.Id == checker.IdleThrottlePlanId);
        Assert.AreEqual(Constants.IdleThrottleName, idlePlan.Name);
        Assert.AreEqual(Constants.IdleMaxProcessorState, idlePlan.MaxProcessorStateAc);
        Assert.AreEqual(Constants.IdleMaxProcessorState, idlePlan.MaxProcessorStateDc);
    }

    [TestMethod]
    public async Task RepeatedIdleCycles_ReuseTheSamePlan()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();
        using var checker = CreateChecker(service, state);
        Guid idlePlan = checker.IdleThrottlePlanId!.Value;

        for (int cycle = 0; cycle < 5; cycle++)
        {
            await checker.EnterIdleModeAsync();
            Assert.AreEqual(idlePlan, service.ActivePlan);

            await checker.ExitIdleModeAsync();
            Assert.AreEqual(NormalPlan, service.ActivePlan);
        }

        Assert.AreEqual(1, service.DuplicateCallCount);
        Assert.AreEqual(1, service.GetPlans().Count(
            plan => plan.Name == Constants.IdleThrottleName));
    }

    [TestMethod]
    public void Restart_ReusesPersistedIdlePlan()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();
        Guid firstPlan;

        using (var firstChecker = CreateChecker(service, state))
        {
            firstPlan = firstChecker.IdleThrottlePlanId!.Value;
        }

        using var secondChecker = CreateChecker(service, state);

        Assert.AreEqual(firstPlan, secondChecker.IdleThrottlePlanId);
        Assert.AreEqual(1, service.DuplicateCallCount);
    }

    [TestMethod]
    public void LegacyCleanup_RetainsOneLocalizedSavingsPlanAndDeletesTheRest()
    {
        var plans = FakePowerPlanService.CreateDefault().GetPlans().ToList();
        Guid firstSavingsPlan = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid secondSavingsPlan = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid balancedBelowHundred = Guid.NewGuid();

        plans.Add(FakePowerPlanService.Plan(
            firstSavingsPlan,
            "Εξοικονόμηση ενέργειας",
            64,
            54,
            PowerPlanPersonality.MaximumPowerSavings));
        plans.Add(FakePowerPlanService.Plan(
            secondSavingsPlan,
            "Εξοικονόμηση ενέργειας",
            64,
            54,
            PowerPlanPersonality.MaximumPowerSavings));
        plans.Add(FakePowerPlanService.Plan(
            balancedBelowHundred,
            "OEM Balanced",
            80,
            80,
            PowerPlanPersonality.Balanced));

        var service = new FakePowerPlanService(plans, NormalPlan);
        var state = new FakePowerPlanStateStore();
        using var checker = CreateChecker(service, state);

        Assert.AreEqual(firstSavingsPlan, checker.IdleThrottlePlanId);
        Assert.DoesNotContain(firstSavingsPlan, service.DeletedPlans);
        Assert.Contains(secondSavingsPlan, service.DeletedPlans);
        Assert.DoesNotContain(balancedBelowHundred, service.DeletedPlans);
        PowerPlanInfo retainedPlan = service.GetPlans().Single(
            plan => plan.Id == firstSavingsPlan);
        Assert.AreEqual(Constants.IdleMaxProcessorState, retainedPlan.MaxProcessorStateAc);
        Assert.AreEqual(Constants.IdleMaxProcessorState, retainedPlan.MaxProcessorStateDc);
        Assert.AreEqual(Constants.LegacyCleanupVersion, state.LegacyCleanupVersion);
    }

    [TestMethod]
    public void LegacyCleanup_FailureDoesNotSetCompletionMarker()
    {
        var plans = FakePowerPlanService.CreateDefault().GetPlans().ToList();
        Guid retainedPlan = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid duplicate = Guid.Parse("22222222-2222-2222-2222-222222222222");
        plans.Add(FakePowerPlanService.Plan(
            retainedPlan,
            "Savings",
            Constants.IdleMaxProcessorState,
            Constants.IdleMaxProcessorState,
            PowerPlanPersonality.MaximumPowerSavings));
        plans.Add(FakePowerPlanService.Plan(
            duplicate,
            "Savings",
            Constants.IdleMaxProcessorState,
            Constants.IdleMaxProcessorState,
            PowerPlanPersonality.MaximumPowerSavings));

        var service = new FakePowerPlanService(plans, NormalPlan);
        service.DeleteFailures.Add(duplicate);
        var state = new FakePowerPlanStateStore();

        using (var checker = CreateChecker(service, state))
        {
            Assert.AreEqual(0, state.LegacyCleanupVersion);
        }

        service.DeleteFailures.Remove(duplicate);
        using var retryChecker = CreateChecker(service, state);

        Assert.AreEqual(Constants.LegacyCleanupVersion, state.LegacyCleanupVersion);
        Assert.IsFalse(service.GetPlans().Any(plan => plan.Id == duplicate));
    }

    [TestMethod]
    public void LegacyCleanup_NeverTargetsCanonicalPowerSaver()
    {
        PowerPlanInfo canonical = FakePowerPlanService.Plan(
            Constants.EnergySaver,
            "Power saver",
            Constants.IdleMaxProcessorState,
            Constants.IdleMaxProcessorState,
            PowerPlanPersonality.MaximumPowerSavings);

        Assert.IsFalse(IdleChecker.IsCleanupCandidate(
            canonical,
            Guid.NewGuid(),
            NormalPlan));
    }

    [TestMethod]
    public void CleanupCandidate_RequiresMaximumSavingsPersonalityAndAcBelowHundred()
    {
        Guid candidate = Guid.NewGuid();
        Guid retained = Guid.NewGuid();

        Assert.IsTrue(IdleChecker.IsCleanupCandidate(
            FakePowerPlanService.Plan(
                candidate,
                "Οποιοδήποτε όνομα",
                64,
                54,
                PowerPlanPersonality.MaximumPowerSavings),
            retained,
            NormalPlan));
        Assert.IsFalse(IdleChecker.IsCleanupCandidate(
            FakePowerPlanService.Plan(
                candidate,
                "OEM Balanced",
                64,
                54,
                PowerPlanPersonality.Balanced),
            retained,
            NormalPlan));
        Assert.IsFalse(IdleChecker.IsCleanupCandidate(
            FakePowerPlanService.Plan(
                candidate,
                "Savings at full CPU",
                100,
                80,
                PowerPlanPersonality.MaximumPowerSavings),
            retained,
            NormalPlan));
    }

    [TestMethod]
    public void FailedPc2BuildState_RetainsManagedIdleAndDeletesLegacySavingsPlans()
    {
        var plans = FakePowerPlanService.CreateDefault().GetPlans().ToList();
        Guid managedIdle = Guid.NewGuid();
        Guid firstLegacy = Guid.NewGuid();
        Guid secondLegacy = Guid.NewGuid();
        plans.Add(FakePowerPlanService.Plan(
            managedIdle,
            Constants.IdleThrottleName,
            50,
            50,
            PowerPlanPersonality.Balanced));
        plans.Add(FakePowerPlanService.Plan(
            firstLegacy,
            "Εξοικονόμηση ενέργειας",
            64,
            54,
            PowerPlanPersonality.MaximumPowerSavings));
        plans.Add(FakePowerPlanService.Plan(
            secondLegacy,
            "Εξοικονόμηση ενέργειας",
            64,
            54,
            PowerPlanPersonality.MaximumPowerSavings));

        var service = new FakePowerPlanService(plans, NormalPlan);
        var state = new FakePowerPlanStateStore
        {
            IdleThrottleGuid = managedIdle.ToString()
        };

        using var checker = CreateChecker(service, state);

        Assert.AreEqual(managedIdle, checker.IdleThrottlePlanId);
        Assert.DoesNotContain(managedIdle, service.DeletedPlans);
        Assert.Contains(firstLegacy, service.DeletedPlans);
        Assert.Contains(secondLegacy, service.DeletedPlans);
        Assert.AreEqual(0, service.DuplicateCallCount);
    }

    [TestMethod]
    public async Task ProvisioningDenied_DoesNotCreateDuplicates()
    {
        var service = FakePowerPlanService.CreateDefault();
        service.AllowCreateScheme = false;
        var state = new FakePowerPlanStateStore();
        using var checker = CreateChecker(service, state);

        await checker.EnterIdleModeAsync();

        Assert.AreEqual(0, service.DuplicateCallCount);
        Assert.IsFalse(checker.IdleThrottlePlanId.HasValue);
        Assert.AreEqual(Guid.Parse(Constants.EnergySaver), service.ActivePlan);
    }

    [TestMethod]
    public void InvalidManagedPlanThatCannotBeRemoved_DoesNotCreateAnotherPlan()
    {
        var plans = FakePowerPlanService.CreateDefault().GetPlans().ToList();
        Guid invalidManagedPlan = Guid.NewGuid();
        plans.Add(FakePowerPlanService.Plan(
            invalidManagedPlan,
            Constants.IdleThrottleName,
            75,
            75));

        var service = new FakePowerPlanService(plans, NormalPlan)
        {
            ProcessorStateWriteResult = 5
        };
        service.DeleteFailures.Add(invalidManagedPlan);

        using var checker = CreateChecker(service, new FakePowerPlanStateStore());

        Assert.AreEqual(0, service.DuplicateCallCount);
        Assert.IsFalse(checker.IdleThrottlePlanId.HasValue);
    }

    [TestMethod]
    public void ManagedNameAlone_NeverAuthorizesDeletion()
    {
        var plans = FakePowerPlanService.CreateDefault().GetPlans().ToList();
        Guid unrelatedBalancedPlan = Guid.NewGuid();
        plans.Add(FakePowerPlanService.Plan(
            unrelatedBalancedPlan,
            Constants.IdleThrottleName,
            75,
            75,
            PowerPlanPersonality.Balanced));

        var service = new FakePowerPlanService(plans, NormalPlan)
        {
            ProcessorStateWriteResult = 5
        };

        using var checker = CreateChecker(service, new FakePowerPlanStateStore());

        Assert.DoesNotContain(unrelatedBalancedPlan, service.DeletedPlans);
        Assert.AreEqual(0, service.DuplicateCallCount);
        Assert.IsFalse(checker.IdleThrottlePlanId.HasValue);
    }

    [TestMethod]
    public async Task Dispose_RestoresNormalPlanAndIsIdempotent()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();
        var checker = CreateChecker(service, state);

        await checker.EnterIdleModeAsync();
        checker.Dispose();
        checker.Dispose();

        Assert.AreEqual(NormalPlan, service.ActivePlan);
    }

    [TestMethod]
    public async Task InvalidGuid_DoesNotChangeTheActivePlan()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();
        using var checker = CreateChecker(service, state);
        Guid? before = service.ActivePlan;

        await checker.ChangePowerPlan("not-a-guid");

        Assert.AreEqual(before, service.ActivePlan);
    }

    [TestMethod]
    public async Task ConcurrentTransitions_DoNotCreateAdditionalPlans()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();
        using var checker = CreateChecker(service, state);

        var transitions = Enumerable.Range(0, 20)
            .Select(index => index % 2 == 0
                ? checker.EnterIdleModeAsync()
                : checker.ExitIdleModeAsync());

        await Task.WhenAll(transitions);

        Assert.AreEqual(1, service.DuplicateCallCount);
        Assert.AreEqual(1, service.GetPlans().Count(
            plan => plan.Name == Constants.IdleThrottleName));
    }

    private static IdleChecker CreateChecker(
        IPowerPlanService service,
        IPowerPlanStateStore state)
    {
        return new IdleChecker(NormalPlan, service, state, startTimers: false);
    }
}
