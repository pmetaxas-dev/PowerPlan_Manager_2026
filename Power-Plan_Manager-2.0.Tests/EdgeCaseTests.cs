using Power_Plan_Manager_Take_8;

namespace Power_Plan_Manager_Take_8.Tests;

[TestClass]
public class EdgeCaseTests
{
    private static readonly Guid NormalPlan = Guid.Parse(Constants.HighPerformance);

    [TestMethod]
    public async Task NullEmptyAndLongGuids_DoNotChangePlan()
    {
        var service = FakePowerPlanService.CreateDefault();
        var state = new FakePowerPlanStateStore();
        using var checker = new IdleChecker(NormalPlan, service, state, startTimers: false);
        Guid? expected = service.ActivePlan;

        await checker.ChangePowerPlan(null!);
        await checker.ChangePowerPlan(string.Empty);
        await checker.ChangePowerPlan(new string('a', 1000));

        Assert.AreEqual(expected, service.ActivePlan);
    }

    [TestMethod]
    public void DisposeMultipleTimes_IsIdempotent()
    {
        var service = FakePowerPlanService.CreateDefault();
        var checker = new IdleChecker(
            NormalPlan,
            service,
            new FakePowerPlanStateStore(),
            startTimers: false);

        checker.Dispose();
        checker.Dispose();
        checker.Dispose();

        Assert.AreEqual(NormalPlan, service.ActivePlan);
    }

    [TestMethod]
    public async Task ConcurrentCheckerCreation_DoesNotAccumulateWithinEachStore()
    {
        Task<(FakePowerPlanService Service, IdleChecker Checker)>[] tasks = Enumerable
            .Range(0, 8)
            .Select(_ => Task.Run(() =>
            {
                var service = FakePowerPlanService.CreateDefault();
                var checker = new IdleChecker(
                    NormalPlan,
                    service,
                    new FakePowerPlanStateStore(),
                    startTimers: false);
                return (service, checker);
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            Assert.AreEqual(1, result.Service.DuplicateCallCount);
            result.Checker.Dispose();
        }
    }

    [TestMethod]
    public async Task InterleavedDisposeAndTransitions_DoNotThrowOrDuplicate()
    {
        var service = FakePowerPlanService.CreateDefault();
        var checker = new IdleChecker(
            NormalPlan,
            service,
            new FakePowerPlanStateStore(),
            startTimers: false);

        Task[] transitions = Enumerable.Range(0, 20)
            .Select(index => index % 2 == 0
                ? checker.EnterIdleModeAsync()
                : checker.ExitIdleModeAsync())
            .ToArray();

        checker.Dispose();
        await Task.WhenAll(transitions);

        Assert.AreEqual(1, service.DuplicateCallCount);
        Assert.AreEqual(NormalPlan, service.ActivePlan);
    }

    [TestMethod]
    public void MixedCaseGuid_IsParsedCaseInsensitively()
    {
        Assert.IsTrue(Guid.TryParse(
            "8C5E7FDA-E8BF-4A96-9A85-A6E23A8C635C",
            out Guid parsed));
        Assert.AreEqual(Guid.Parse(Constants.HighPerformance), parsed);
    }
}
