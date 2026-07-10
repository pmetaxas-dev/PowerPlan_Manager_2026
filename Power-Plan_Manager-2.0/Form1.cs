

using System.Diagnostics;

namespace Power_Plan_Manager_Take_8
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Horizontal offset from screen right edge (pixels).
        /// Positions the window slightly inset from the right edge of the screen.
        /// </summary>
        private const int WINDOW_OFFSET_X = 192;

        /// <summary>
        /// Vertical offset from screen bottom edge (pixels).
        /// Positions the window slightly inset from the bottom edge of the screen,
        /// accounting for taskbar height on most Windows systems (~40-50 pixels).
        /// </summary>
        private const int WINDOW_OFFSET_Y = 100;

        IdleChecker? idleChecker;        


        public Form1()
        {
            InitializeComponent();



            // Make window unresizable
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Position window at bottom-right of screen, with offsets to avoid overlap with taskbar
            this.StartPosition = FormStartPosition.Manual;
            Rectangle resolution = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            this.Location = new Point(resolution.Width - this.Width - WINDOW_OFFSET_X, 
                                     resolution.Height - this.Height - WINDOW_OFFSET_Y);
            Logger.Log($"Form1 initialized at position: {this.Location}");

            // Reset the app state to 'Enabled'
            // In case it was disabled when the app closed..
            // ..or the system restarted.            
            Properties.Settings.Default.Enabled = true;
            Properties.Settings.Default.Save();
            //

            // Determine and enforce the preferred high-performance plan BEFORE creating IdleChecker
            // so the app does not capture Energy Saver (or other low-power plan) as the "system" plan.
            string currentActivePlan = IdleChecker.GetSystemActivePlan();
            Logger.Log($"Captured system active plan before enforcement: {currentActivePlan}");

            // Determine the preferred candidate (Ryzen variants -> Ultimate)
            string preferred = PowerPlanDetector.GetOptimalHighPerformancePlan();

            string chosenPlan;
            if (PowerPlanDetector.IsPowerPlanAvailable(preferred))
            {
                chosenPlan = preferred;
            }
            else if (PowerPlanDetector.IsPowerPlanAvailable(Constants.HighPerformance))
            {
                chosenPlan = Constants.HighPerformance;
            }
            else
            {
                chosenPlan = currentActivePlan; // fall back to current active
            }

            // If the chosen plan differs from current active, force it using powercfg /setactive
            if (!string.Equals(chosenPlan, currentActivePlan, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Logger.Log($"Attempting to set active power plan to {chosenPlan} on startup");
                    var psi = new ProcessStartInfo("powercfg", $"/setactive {chosenPlan}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var p = Process.Start(psi))
                    {
                        if (p != null)
                        {
                            string outText = p.StandardOutput.ReadToEnd();
                            string errText = p.StandardError.ReadToEnd();
                            p.WaitForExit();
                            Logger.Log($"powercfg /setactive output: {outText.Trim()} {errText.Trim()}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException("Form1.SetActivePowerPlan", ex);
                }
            }

            // Create IdleChecker with the chosen plan (this is the plan the app will restore after idle)
            idleChecker = new IdleChecker(chosenPlan);

            // Run redundant-powerplan cleanup once on startup (background)
            try { idleChecker.RemoveRedundantEnergySaverPlans(); } catch (Exception ex) { Logger.LogException("Form1.RemoveRedundantEnergySaverPlans", ex); }

            // Check if it's the first run
            if (Properties.Settings.Default.FirstRun == 1)
            {
                MessageBox.Show("Hi! Power-Plan Manager is minimized to tray. Enjoy!");

                // Mark as not the first run
                Properties.Settings.Default.FirstRun = 0;
                Properties.Settings.Default.Save();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Create a new instance of the form you want to open
            About_Window about_Form = new About_Window();
            // Show the new form
            about_Form.Show();

            // Perform actions specific to the first run
            //MessageBox.Show("This app changes the Windows power-plan to 'Power Saving', after user inactivity. " +
            //    "A mouse movement or a button press and power=plan returns to balanced. The app is always minimized to tray." +
            //    " \t\tIf you want it to autostart with Windows add a shortcut to 'shell:startup'.");
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {

            //trayIcon.BalloonTipText = "Power-Plan Manager is minimized to system tray.";
            //trayIcon.ShowBalloonTip(500);
            trayIcon.Visible = true;

            //if (WindowState == FormWindowState.Minimized && Properties.Settings.UserMinimized = 1)
            //{
            //    Hide(); // Hide the form
            //}
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                Hide(); // Hide the form
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                base.OnFormClosing(e);
                try { idleChecker?.Dispose(); } catch (Exception ex) { Logger.LogException("Form1.OnFormClosing.Dispose", ex); }
            }
        }


        private void showWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show(); // Show the form
            this.WindowState = FormWindowState.Normal; // Return to normal!
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try { idleChecker?.Dispose(); } catch (Exception ex) { Logger.LogException("Form1.ExitToolStripMenuItem_Click.Dispose", ex); }
            Application.Exit();
        }

        private void trayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show(); // Show the form
            this.WindowState = FormWindowState.Normal; // Return to normal!
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                Properties.Settings.Default.Enabled = true;
                Properties.Settings.Default.Save();
                checkBox1.Text = "System is managed";
                // Restore the system default plan
                if (idleChecker != null)
                    _ = idleChecker.ChangePowerPlan(idleChecker.SystemActivePlan);
            }
            else
            {
                Properties.Settings.Default.Enabled = false;
                Properties.Settings.Default.Save();
                checkBox1.Text = "System is unmanaged";
                // Restore the system default plan
                if (idleChecker != null)
                    _ = idleChecker.ChangePowerPlan(idleChecker.SystemActivePlan);
            }
        }
    }
}
