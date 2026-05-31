using Microsoft.VisualStudio.TestTools.UnitTesting;
using Power_Plan_Manager_Take_8;
using System.Diagnostics;

namespace Power_Plan_Manager_Take_8.Tests
{
    /// <summary>
    /// Unit tests for PowerPlanDetector utility class.
    /// Tests power plan detection logic and GUID validation.
    /// </summary>
    [TestClass]
    public class PowerPlanDetectorTests
    {
        [TestMethod]
        public void GetOptimalHighPerformancePlan_ReturnsValidGuid()
        {
            // Arrange & Act
            string result = PowerPlanDetector.GetOptimalHighPerformancePlan();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
            Assert.IsTrue(Guid.TryParse(result, out _), 
                "GetOptimalHighPerformancePlan should return a valid GUID");
        }

        [TestMethod]
        public void GetOptimalHighPerformancePlan_ReturnEitherRyzenOrUltimate()
        {
            // Arrange & Act
            string result = PowerPlanDetector.GetOptimalHighPerformancePlan();

            // Assert
            bool isRyzen = result.Equals(Constants.RyzenUniversal, StringComparison.OrdinalIgnoreCase);
            bool isUltimate = result.Equals(Constants.UltimatePerformance, StringComparison.OrdinalIgnoreCase);

            Assert.IsTrue(isRyzen || isUltimate,
                "GetOptimalHighPerformancePlan should return either RyzenUniversal or UltimatePerformance");
        }

        [TestMethod]
        public void IsPowerPlanAvailable_EnergySaver_ShouldBeAvailable()
        {
            // Arrange & Act
            bool result = PowerPlanDetector.IsPowerPlanAvailable(Constants.EnergySaver);

            // Assert
            Assert.IsTrue(result,
                "Energy Saver power plan should be available on all Windows systems");
        }

        [TestMethod]
        public void IsPowerPlanAvailable_InvalidGuid_ReturnsFalse()
        {
            // Arrange
            string invalidGuid = "99999999-9999-9999-9999-999999999999";

            // Act
            bool result = PowerPlanDetector.IsPowerPlanAvailable(invalidGuid);

            // Assert
            Assert.IsFalse(result,
                "IsPowerPlanAvailable should return false for non-existent plan GUIDs");
        }

        [TestMethod]
        public void ConstantsGuids_AreValidGuids()
        {
            // Assert
            Assert.IsTrue(Guid.TryParse(Constants.HighPerformance, out _));
            Assert.IsTrue(Guid.TryParse(Constants.UltimatePerformance, out _));
            Assert.IsTrue(Guid.TryParse(Constants.EnergySaver, out _));
            Assert.IsTrue(Guid.TryParse(Constants.RyzenUniversal, out _));
        }

        [TestMethod]
        public void ConstantsGuids_AreNotEmpty()
        {
            // Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.HighPerformance));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.UltimatePerformance));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.EnergySaver));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.RyzenUniversal));
        }

        [TestMethod]
        public void ConstantsGuids_AreUnique()
        {
            // Arrange
            var guids = new[] 
            { 
                Constants.HighPerformance, 
                Constants.UltimatePerformance, 
                Constants.EnergySaver, 
                Constants.RyzenUniversal 
            };

            // Assert
            var uniqueCount = guids.Distinct().Count();
            Assert.AreEqual(guids.Length, uniqueCount,
                "All power plan GUIDs should be unique");
        }
    }
}
