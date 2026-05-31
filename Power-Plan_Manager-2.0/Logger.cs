using System;
using System.IO;

namespace Power_Plan_Manager_Take_8
{
    /// <summary>
    /// Provides simple file-based logging for the Power Plan Manager application.
    /// Logs are written to AppData/PowerPlanManager/log.txt with timestamps.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory = 
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PowerPlanManager"
            );

        private static readonly string LogFile = Path.Combine(LogDirectory, "log.txt");
        private static readonly object LockObject = new object();

        /// <summary>
        /// Logs a message to the log file with timestamp.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void Log(string message)
        {
            try
            {
                lock (LockObject)
                {
                    // Ensure directory exists
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    // Write log with timestamp
                    string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                    File.AppendAllText(LogFile, timestampedMessage + Environment.NewLine);
                }
            }
            catch
            {
                // Prevent logging errors from crashing the application
                // Silently ignore logging failures
            }
        }

        /// <summary>
        /// Logs an exception with context information.
        /// </summary>
        /// <param name="context">The context where the exception occurred (e.g., "ChangePowerPlan")</param>
        /// <param name="ex">The exception to log</param>
        public static void LogException(string context, Exception ex)
        {
            try
            {
                string message = $"ERROR [{context}]: {ex.GetType().Name}: {ex.Message}";
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    message += $"\n  Stack Trace: {ex.StackTrace}";
                }

                if (ex.InnerException != null)
                {
                    message += $"\n  Inner Exception: {ex.InnerException.Message}";
                }

                Log(message);
            }
            catch
            {
                // Prevent logging errors from crashing the application
            }
        }

        /// <summary>
        /// Gets the path to the log file.
        /// </summary>
        /// <returns>The full path to log.txt</returns>
        public static string GetLogFilePath()
        {
            return LogFile;
        }

        /// <summary>
        /// Clears the log file.
        /// </summary>
        public static void ClearLog()
        {
            try
            {
                lock (LockObject)
                {
                    if (File.Exists(LogFile))
                    {
                        File.Delete(LogFile);
                    }
                }
            }
            catch
            {
                // Prevent logging errors from crashing the application
            }
        }
    }
}
