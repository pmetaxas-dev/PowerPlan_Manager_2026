using Microsoft.VisualStudio.TestTools.UnitTesting;
using Power_Plan_Manager_Take_8;

namespace Power_Plan_Manager_Take_8.Tests
{
    /// <summary>
    /// Integration tests for Power-Plan Manager application.
    /// Tests end-to-end scenarios combining multiple components.
    /// </summary>
    [TestClass]
    public class IntegrationTests
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
        public void Integration_Switch_To_EnergySaver_And_Back()
        {
            // Arrange
            string highPerfPlan = PowerPlanDetector.GetOptimalHighPerformancePlan();

            // Act & Assert - Switch to Energy Saver
            try
            {
                idleChecker?.ChangePowerPlan(Constants.EnergySaver);
                Thread.Sleep(500); // Wait for process to complete
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to switch to Energy Saver: {ex.Message}");
            }

            // Act & Assert - Switch back to high-performance
            try
            {
                idleChecker?.ChangePowerPlan(highPerfPlan);
                Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to switch back to high-performance plan: {ex.Message}");
            }
        }

        [TestMethod]
        public void Integration_Form1_And_IdleChecker_Initialize_Together()
        {
            // Arrange & Act
            Form1? form = null;

            try
            {
                form = new Form1();

                // Assert
                Assert.IsNotNull(form,
                    "Form1 should initialize successfully alongside IdleChecker");
            }
            finally
            {
                form?.Dispose();
            }
        }

        [TestMethod]
        public void Integration_Rapid_Power_Plan_Switches_DoNotCrash()
        {
            // Arrange
            var plans = new[] 
            { 
                Constants.EnergySaver, 
                PowerPlanDetector.GetOptimalHighPerformancePlan(),
                Constants.EnergySaver 
            };

            // Act & Assert - Rapidly switch plans
            try
            {
                foreach (string plan in plans)
                {
                    idleChecker?.ChangePowerPlan(plan);
                    Thread.Sleep(200); // Brief delay between switches
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Rapid power plan switches should not crash: {ex.Message}");
            }
        }

        [TestMethod]
        public void Integration_IdleChecker_Handles_Disposal_During_Operation()
        {
            // Arrange
            var disposeTask = Task.Run(() =>
            {
                Thread.Sleep(100); // Let idle checker run for 100ms
                idleChecker?.Dispose();
            });

            // Act & Assert - Trigger operations while disposal is pending
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    idleChecker?.ChangePowerPlan(Constants.EnergySaver);
                    Thread.Sleep(50);
                }

                disposeTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                Assert.Fail($"IdleChecker should handle disposal gracefully: {ex.Message}");
            }
        }

        [TestMethod]
        public void Integration_All_Components_Can_Be_Created_Destroyed_Multiple_Times()
        {
            // Act & Assert - Create and destroy multiple times
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    Form1? form = new Form1();
                    IdleChecker? checker = new IdleChecker(IdleChecker.GetSystemActivePlan());

                    Thread.Sleep(100); // Let them run briefly

                    checker?.Dispose();
                    form?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Components should handle multiple create/destroy cycles: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task Integration_ChangePowerPlan_EnergySaver_IsVerified()
        {
            // Arrange
            string originalPlan = IdleChecker.GetSystemActivePlan();

            try
            {
                // Act
                await idleChecker!.ChangePowerPlan(Constants.EnergySaver);

                // Assert — read back the active plan and confirm it changed
                string activePlan = IdleChecker.GetSystemActivePlan();
                Assert.AreEqual(
                    Constants.EnergySaver.ToLowerInvariant(),
                    activePlan.ToLowerInvariant(),
                    $"Active plan should be Energy Saver after switch. Got: {activePlan}");
            }
            finally
            {
                // Restore original plan
                await idleChecker!.ChangePowerPlan(originalPlan);
            }
        }

        [TestMethod]
        public async Task Integration_ChangePowerPlan_HighPerformance_IsVerified()
        {
            // Arrange
            string originalPlan = IdleChecker.GetSystemActivePlan();
            string highPerfPlan = PowerPlanDetector.GetOptimalHighPerformancePlan();

            // Skip if the optimal plan is not installed on this machine
            if (!PowerPlanDetector.IsPowerPlanAvailable(highPerfPlan))
            {
                Assert.Inconclusive($"High-performance plan {highPerfPlan} is not installed on this machine — skipping.");
                return;
            }

            try
            {
                // Act
                await idleChecker!.ChangePowerPlan(highPerfPlan);

                // Assert
                string activePlan = IdleChecker.GetSystemActivePlan();
                Assert.AreEqual(
                    highPerfPlan.ToLowerInvariant(),
                    activePlan.ToLowerInvariant(),
                    $"Active plan should be high-performance after switch. Got: {activePlan}");
            }
            finally
            {
                await idleChecker!.ChangePowerPlan(originalPlan);
            }
        }

        [TestMethod]
        public async Task Integration_ChangePowerPlan_RestoresOriginalPlan_IsVerified()
        {
            // Arrange
            string originalPlan = IdleChecker.GetSystemActivePlan();

            // Act — switch away then restore
            await idleChecker!.ChangePowerPlan(Constants.EnergySaver);
            await idleChecker!.ChangePowerPlan(originalPlan);

            // Assert — should be back to original
            string activePlan = IdleChecker.GetSystemActivePlan();
            Assert.AreEqual(
                originalPlan.ToLowerInvariant(),
                activePlan.ToLowerInvariant(),
                $"Active plan should be restored to original. Got: {activePlan}");
        }
    }
}
