using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Power_Plan_Manager_Take_8
{
    internal class IdleChecker
    {
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO usr);


        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        System.Windows.Forms.Timer idleCheckTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer userInputCheckTimer = new System.Windows.Forms.Timer();



        public IdleChecker()
        {
            ChangePowerPlan("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"); // (High Performance)
            idleCheckTimer.Interval = 90 * 1000; //  3 minutes of user inactivity          
            idleCheckTimer.Tick += IdleCheckTimer_Tick;
            idleCheckTimer.Start();

            userInputCheckTimer.Interval = 5 * 1000; // 5 seconds to check for user activity
            userInputCheckTimer.Tick += UserInputCheckTimer_Tick;
        }

        private void IdleCheckTimer_Tick(object sender, EventArgs e)
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);

            uint idleTime = ((uint)Environment.TickCount - lastInputInfo.dwTime) / 1000;

            if (idleTime >= idleCheckTimer.Interval / 1000 && Properties.Settings.Default.Enabled == true)
            {                
                ChangePowerPlan("a1841308-3541-4fab-bc81-f71556f20b4a"); // Energy Saver
                idleCheckTimer.Stop();// Stop counting idle time and..
                userInputCheckTimer.Start(); // ..start checking for user input
            }            
        }

        private void UserInputCheckTimer_Tick(object sender, EventArgs e)
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);

            uint idleTime = ((uint)Environment.TickCount - lastInputInfo.dwTime) / 1000;

            if (idleTime < userInputCheckTimer.Interval / 1000 && Properties.Settings.Default.Enabled == true)
            {                         
                //ChangePowerPlan("fcaac3f2-997a-4fdb-8e30-c4fb6df29398"); // (1usmus Ryzen Universal)
                ChangePowerPlan("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"); // (High Performance)                
                userInputCheckTimer.Stop(); // Stop checking for user input and..
                idleCheckTimer.Start(); // ..start counting idle time.
            }            
        }

        public void ChangePowerPlan(string powerPlanGuid)
        {
            try
            {
                var psi = new ProcessStartInfo("powercfg", $"/setactive {powerPlanGuid}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to change power plan: {ex.Message} - " +
                    $"Please contact me for a solution: workersoft@gmx.com");
            }
        }
    }
}
