using System;

namespace Power_Plan_Manager_Take_8
{
    /// <summary>
    /// Central repository for application-wide constants.
    /// Includes GUID validation to catch configuration errors early.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Validates that a string is a properly formatted GUID.
        /// Throws FormatException if the GUID is invalid.
        /// </summary>
        private static Guid ValidateGuid(string guidString, string name)
        {
            if (string.IsNullOrWhiteSpace(guidString))
            {
                throw new ArgumentException($"GUID for {name} cannot be null or empty");
            }

            if (!Guid.TryParse(guidString, out var guid))
            {
                throw new FormatException($"Invalid GUID format for {name}: {guidString}");
            }

            return guid;
        }

        // Static initialization: validate all GUIDs on first use
        static Constants()
        {
            try
            {
                ValidateGuid(HighPerformance, nameof(HighPerformance));
                ValidateGuid(UltimatePerformance, nameof(UltimatePerformance));
                ValidateGuid(EnergySaver, nameof(EnergySaver));
                ValidateGuid(RyzenUniversal, nameof(RyzenUniversal));
                // Additional known 1usmus Ryzen power plan GUID (some users may have this variant)
                ValidateGuid(RyzenPowerPlan, nameof(RyzenPowerPlan));
                Logger.Log("All power plan GUIDs validated successfully");
            }
            catch (Exception ex)
            {
                Logger.LogException("Constants.StaticConstructor", ex);
                throw;
            }
        }

        /// <summary>
        /// Power plan GUID for High Performance mode.
        /// </summary>
        public const string HighPerformance = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

        /// <summary>
        /// Power plan GUID for Ultimate Performance mode (Windows 10+).
        /// </summary>
        public const string UltimatePerformance = "e9a42b02-d5df-448d-aa00-03f14749e6c0";

        /// <summary>
        /// Power plan GUID for Energy Saver mode.
        /// </summary>
        public const string EnergySaver = "a1841308-3541-4fab-bc81-f71556f20b4a";

        /// <summary>
        /// Power plan GUID for 1usmus Ryzen Universal profile.
        /// </summary>
        public const string RyzenUniversal = "fcaac3f2-997a-4fdb-8e30-c4fb6df29398";

        /// <summary>
        /// Additional known 1usmus Ryzen power plan GUID found on some systems.
        /// Treated as an acceptable high-performance plan for AMD machines.
        /// </summary>
        public const string RyzenPowerPlan = "d8ba2c0c-8978-4d80-b094-71dd1850c424";
    }
}

