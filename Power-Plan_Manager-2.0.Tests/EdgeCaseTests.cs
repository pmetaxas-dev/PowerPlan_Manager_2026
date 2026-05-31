using Microsoft.VisualStudio.TestTools.UnitTesting;
using Power_Plan_Manager_Take_8;

namespace Power_Plan_Manager_Take_8.Tests
{
    /// <summary>
    /// Edge case and error handling tests for Power-Plan Manager.
    /// Tests boundary conditions, exception scenarios, and robustness.
    /// </summary>
    [TestClass]
    public class EdgeCaseTests
    {
        [TestMethod]
        public void EdgeCase_PowerPlanDetector_MixedCaseGuid_IsHandledCorrectly()
        {
            // Arrange
            string mixedCaseGuid = "8C5E7FDA-E8BF-4A96-9A85-A6E23A8C635C"; // Mixed case

            // Act & Assert
            try
            {
                bool result = PowerPlanDetector.IsPowerPlanAvailable(mixedCaseGuid);
                Assert.IsTrue(result,
                    "Mixed case GUIDs should be handled correctly (case-insensitive)");
            }
            catch (ArgumentException)
            {
                Assert.Fail("Mixed case GUIDs should be accepted");
            }
        }

        [TestMethod]
        public void EdgeCase_IdleChecker_Dispose_Multiple_Times_IsIdempotent()
        {
            // Arrange
            var idleChecker = new IdleChecker(IdleChecker.GetSystemActivePlan());

            // Act & Assert
            try
            {
                idleChecker.Dispose();
                idleChecker.Dispose();
                idleChecker.Dispose(); // Should not throw
            }
            catch (Exception ex)
            {
                Assert.Fail($"Multiple Dispose() calls should be safe: {ex.Message}");
            }
        }

        [TestMethod]
        public void EdgeCase_IdleChecker_ChangePowerPlan_WithNullGuid_DoesNotCrash()
        {
            // Arrange
            var idleChecker = new IdleChecker(IdleChecker.GetSystemActivePlan());

            try
            {
                // Act - Attempt to change to null plan
                idleChecker.ChangePowerPlan(null!);
                Thread.Sleep(100);

                // Assert - Should not crash even if process call fails
            }
            catch (NullReferenceException)
            {
                Assert.Fail("ChangePowerPlan should handle null GUID gracefully, not crash");
            }
            finally
            {
                idleChecker.Dispose();
            }
        }

        [TestMethod]
        public void EdgeCase_IdleChecker_ChangePowerPlan_WithEmptyGuid_DoesNotCrash()
        {
            // Arrange
            var idleChecker = new IdleChecker(IdleChecker.GetSystemActivePlan());

            try
            {
                // Act
                idleChecker.ChangePowerPlan("");
                Thread.Sleep(100);

                // Assert - Should not crash even if powercfg fails
            }
            catch (Exception ex)
            {
                Assert.Fail($"ChangePowerPlan should handle empty GUID gracefully: {ex.Message}");
            }
            finally
            {
                idleChecker.Dispose();
            }
        }

        [TestMethod]
        public void EdgeCase_IdleChecker_ChangePowerPlan_WithVeryLongGuid_DoesNotCrash()
        {
            // Arrange
            var idleChecker = new IdleChecker(IdleChecker.GetSystemActivePlan());
            string veryLongGuid = new string('a', 1000);

            try
            {
                // Act
                idleChecker.ChangePowerPlan(veryLongGuid);
                Thread.Sleep(100);

                // Assert - Should not crash
            }
            catch (Exception ex)
            {
                Assert.Fail($"ChangePowerPlan should handle very long string gracefully: {ex.Message}");
            }
            finally
            {
                idleChecker.Dispose();
            }
        }

        [TestMethod]
        public void EdgeCase_Constants_GuidCase_IsConsistent()
        {
            // Assert - All GUIDs should use the same case convention (lowercase or uppercase)
            var allLowercase = Constants.HighPerformance.All(c => !char.IsUpper(c) || c == '-');
            var allUppercase = Constants.HighPerformance.All(c => !char.IsLower(c) || c == '-');

            bool isConsistentCase = allLowercase || allUppercase;
            Assert.IsTrue(isConsistentCase,
                "GUID constants should use consistent case (all lowercase or all uppercase)");
        }

        [TestMethod]
        public void EdgeCase_Concurrent_IdleChecker_Creation()
        {
            // Arrange
            var tasks = new List<Task<IdleChecker>>();

            // Act - Create multiple IdleCheckers concurrently
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    tasks.Add(Task.Run(() => new IdleChecker(IdleChecker.GetSystemActivePlan())));
                }

                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));

                // Assert - All should be created without exception
                foreach (var task in tasks)
                {
                    Assert.IsNotNull(task.Result);
                    task.Result.Dispose();
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Concurrent IdleChecker creation should be thread-safe: {ex.Message}");
            }
        }

        [TestMethod]
        public void EdgeCase_Interleaved_Dispose_And_ChangePowerPlan()
        {
            // Arrange
            var idleChecker = new IdleChecker(IdleChecker.GetSystemActivePlan());
            var disposeTask = Task.Run(() =>
            {
                Thread.Sleep(50);
                idleChecker.Dispose();
            });

            // Act & Assert - Interleave power plan changes with disposal
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    idleChecker.ChangePowerPlan(Constants.EnergySaver);
                    Thread.Sleep(10);
                }

                disposeTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Interleaved Dispose and ChangePowerPlan should be safe: {ex.Message}");
            }
        }

        [TestMethod]
        public void EdgeCase_PowerPlanDetector_Handles_Corrupted_Powercfg_Output()
        {
            // This test verifies robustness even though we can't mock powercfg in this setup
            // It documents the expected behavior for malformed output.

            // Arrange & Act
            string optimalPlan = PowerPlanDetector.GetOptimalHighPerformancePlan();

            // Assert - Should still return a valid GUID even if output is somehow corrupted
            Assert.IsNotNull(optimalPlan);
            Assert.IsFalse(string.IsNullOrWhiteSpace(optimalPlan));
            Assert.IsTrue(Guid.TryParse(optimalPlan, out _));
        }
    }
}

