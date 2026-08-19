using Microsoft.VisualStudio.TestTools.UnitTesting;
using Power_Plan_Manager_Take_8;

namespace Power_Plan_Manager_Take_8.Tests
{
    /// <summary>
    /// Unit tests for Constants class.
    /// Validates that all power plan GUIDs are properly defined and formatted.
    /// </summary>
    [TestClass]
    public class ConstantsTests
    {
        [TestMethod]
        public void Constants_HighPerformance_IsValidGuid()
        {
            // Act & Assert
            Assert.IsTrue(Guid.TryParse(Constants.HighPerformance, out _),
                "HighPerformance GUID should be a valid GUID format");
        }

        [TestMethod]
        public void Constants_UltimatePerformance_IsValidGuid()
        {
            // Act & Assert
            Assert.IsTrue(Guid.TryParse(Constants.UltimatePerformance, out _),
                "UltimatePerformance GUID should be a valid GUID format");
        }

        [TestMethod]
        public void Constants_EnergySaver_IsValidGuid()
        {
            // Act & Assert
            Assert.IsTrue(Guid.TryParse(Constants.EnergySaver, out _),
                "EnergySaver GUID should be a valid GUID format");
        }

        [TestMethod]
        public void Constants_RyzenUniversal_IsValidGuid()
        {
            // Act & Assert
            Assert.IsTrue(Guid.TryParse(Constants.RyzenUniversal, out _),
                "RyzenUniversal GUID should be a valid GUID format");
        }

        [TestMethod]
        public void Constants_AllGuids_AreNotNull()
        {
            // Assert
            Assert.IsNotNull(Constants.HighPerformance);
            Assert.IsNotNull(Constants.UltimatePerformance);
            Assert.IsNotNull(Constants.EnergySaver);
            Assert.IsNotNull(Constants.RyzenUniversal);
        }

        [TestMethod]
        public void Constants_AllGuids_AreNotEmpty()
        {
            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(Constants.HighPerformance));
            Assert.IsFalse(string.IsNullOrEmpty(Constants.UltimatePerformance));
            Assert.IsFalse(string.IsNullOrEmpty(Constants.EnergySaver));
            Assert.IsFalse(string.IsNullOrEmpty(Constants.RyzenUniversal));
        }

        [TestMethod]
        public void Constants_AllGuids_AreNotWhiteSpace()
        {
            // Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.HighPerformance));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.UltimatePerformance));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.EnergySaver));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.RyzenUniversal));
        }

        [TestMethod]
        public void Constants_HighPerformance_MatchesKnownGuid()
        {
            // Assert - High Performance GUID is a Windows standard
            Assert.AreEqual(Guid.Parse("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"), Guid.Parse(Constants.HighPerformance),
                "HighPerformance GUID should match the Windows standard GUID");
        }

        [TestMethod]
        public void Constants_UltimatePerformance_MatchesKnownGuid()
        {
            // Assert - Ultimate Performance GUID is a Windows standard (Windows 10+)
            Assert.AreEqual(Guid.Parse("e9a42b02-d5df-448d-aa00-03f14749e6c0"), Guid.Parse(Constants.UltimatePerformance),
                "UltimatePerformance GUID should match the Windows standard GUID");
        }

        [TestMethod]
        public void Constants_EnergySaver_MatchesKnownGuid()
        {
            // Assert - Energy Saver GUID is a Windows standard
            Assert.AreEqual(Guid.Parse("a1841308-3541-4fab-bc81-f71556f20b4a"), Guid.Parse(Constants.EnergySaver),
                "EnergySaver GUID should match the Windows standard GUID");
        }

        [TestMethod]
        public void Constants_RyzenUniversal_MatchesKnownGuid()
        {
            // Assert - Ryzen Universal GUID is a 1usmus profile
            Assert.AreEqual(Guid.Parse("fcaac3f2-997a-4fdb-8e30-c4fb6df29398"), Guid.Parse(Constants.RyzenUniversal),
                "RyzenUniversal GUID should match the known 1usmus profile GUID");
        }

        [TestMethod]
        public void Constants_AllGuids_AreUnique()
        {
            // Arrange
            var guids = new[] 
            { 
                Constants.HighPerformance, 
                Constants.UltimatePerformance, 
                Constants.EnergySaver, 
                Constants.RyzenUniversal 
            };

            var uniqueGuids = guids.Distinct().ToList();

            // Assert
            Assert.HasCount(guids.Length, uniqueGuids,
                "All power plan GUIDs must be unique (no duplicates)");
        }

        [TestMethod]
        public void Constants_GuidFormat_IsSingleDashFormat()
        {
            // Test that GUIDs are in the standard format: 8-4-4-4-12 with lowercase or uppercase
            var guidRegex = new System.Text.RegularExpressions.Regex(
                @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Assert
            Assert.IsTrue(guidRegex.IsMatch(Constants.HighPerformance));
            Assert.IsTrue(guidRegex.IsMatch(Constants.UltimatePerformance));
            Assert.IsTrue(guidRegex.IsMatch(Constants.EnergySaver));
            Assert.IsTrue(guidRegex.IsMatch(Constants.RyzenUniversal));
        }
    }
}
