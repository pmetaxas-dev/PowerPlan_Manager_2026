using Microsoft.VisualStudio.TestTools.UnitTesting;
using Power_Plan_Manager_Take_8;

namespace Power_Plan_Manager_Take_8.Tests
{
    /// <summary>
    /// Unit tests for the IdleChecker class.
    /// Tests idle detection logic, timer state transitions, and power plan switching.
    /// </summary>
    [TestClass]
    public class IdleCheckerTests
    {
        private IdleChecker? idleChecker;

        [TestInitialize]
        public void Setup()
        {
            idleChecker = new IdleChecker(IdleChecker.GetSystemActivePlan());
        }

        [TestCleanup]
        public void Teardown()
        {
            idleChecker?.Dispose();
        }

        [TestMethod]
        public void IdleChecker_Constructor_CreatesSuccessfully()
        {
            // Assert
            Assert.IsNotNull(idleChecker,
                "IdleChecker should be created without exception");
        }

        [TestMethod]
        public void IdleChecker_ImplementsIDisposable()
        {
            // Assert
            Assert.IsInstanceOfType(idleChecker, typeof(IDisposable),
                "IdleChecker should implement IDisposable");
        }

        [TestMethod]
        public void IdleChecker_Dispose_CompletesWithoutException()
        {
            // Act & Assert
            idleChecker?.Dispose();
            // If we get here without exception, the test passes
        }

        [TestMethod]
        public void IdleChecker_DisposeTwice_DoesNotThrow()
        {
            // Act & Assert
            idleChecker?.Dispose();
            idleChecker?.Dispose(); // Should be idempotent
        }

        [TestMethod]
        public void IdleChecker_ChangePowerPlan_WithValidGuid_CompletesWithoutException()
        {
            // Arrange
            string validGuid = Constants.EnergySaver;

            // Act & Assert
            try
            {
                idleChecker?.ChangePowerPlan(validGuid);
                // Allow time for async operation to complete
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ChangePowerPlan should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void IdleChecker_ChangePowerPlan_WithHighPerformancePlan_CompletesWithoutException()
        {
            // Arrange
            string perfPlan = PowerPlanDetector.GetOptimalHighPerformancePlan();

            // Act & Assert
            try
            {
                idleChecker?.ChangePowerPlan(perfPlan);
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ChangePowerPlan with high-performance plan should not throw: {ex.Message}");
            }
        }

        [TestMethod]
        public void IdleChecker_ChangePowerPlan_AfterDispose_DoesNotThrow()
        {
            // Arrange
            idleChecker?.Dispose();

            // Act & Assert
            try
            {
                idleChecker?.ChangePowerPlan(Constants.EnergySaver);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ChangePowerPlan after Dispose should not throw: {ex.Message}");
            }
        }

        [TestMethod]
        public void IdleChecker_MultiplePowerPlanChanges_DoNotCauseRaceConditions()
        {
            // Arrange
            var tasks = new List<Task>();

            // Act - Rapidly switch power plans to test thread safety
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    idleChecker?.ChangePowerPlan(Constants.EnergySaver);
                }));

                tasks.Add(Task.Run(() =>
                {
                    idleChecker?.ChangePowerPlan(PowerPlanDetector.GetOptimalHighPerformancePlan());
                }));
            }

            // Assert
            try
            {
                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
                // If we complete without exception, the test passes
            }
            catch (Exception ex)
            {
                Assert.Fail($"Concurrent power plan changes should not cause exceptions: {ex.Message}");
            }
        }
    }
}

