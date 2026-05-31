using System;
using System.Diagnostics;

namespace Power_Plan_Manager_Take_8
{
    /// <summary>
    /// Utility class to detect available power plans on the system.
    /// </summary>
    public static class PowerPlanDetector
    {
        /// <summary>
        /// Checks if a power plan with the given GUID is available on the system.
        /// </summary>
        /// <param name="powerPlanGuid">The GUID of the power plan to check.</param>
        /// <returns>True if the power plan is available, false otherwise.</returns>
        public static bool IsPowerPlanAvailable(string powerPlanGuid)
        {
            try
            {
                var psi = new ProcessStartInfo("powercfg", "/list")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return false;

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Check if the GUID appears in the output (case-insensitive)
                    return output.Contains(powerPlanGuid, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("PowerPlanDetector.IsPowerPlanAvailable", ex);
            }

            return false;
        }

        /// <summary>
        /// Gets the best available high-performance power plan:
        /// Returns Ryzen Universal if available, otherwise Ultimate Performance.
        /// </summary>
        /// <returns>The GUID of the selected power plan.</returns>
        public static string GetOptimalHighPerformancePlan()
        {
            // Check if Ryzen Universal is available
            if (IsPowerPlanAvailable(Constants.RyzenUniversal))
            {
                return Constants.RyzenUniversal;
            }

            // Fall back to Ultimate Performance
            return Constants.UltimatePerformance;
        }
    }
}

