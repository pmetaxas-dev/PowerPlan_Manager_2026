
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using IWshRuntimeLibrary;


namespace Power_Plan_Manager_Take_8
{
    public partial class Form1 : Form
    {
        IdleChecker? idleChecker;


        public Form1()
        {
            InitializeComponent();



            // Make window unresizable
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Open main window in a specified position
            // Set the start position to manual
            this.StartPosition = FormStartPosition.Manual;
            // Get the screen resolution
            Rectangle resolution = Screen.PrimaryScreen.Bounds;
            // Set the desired offset
            int offsetX = 192;
            int offsetY = 100;
            // Set the location of the form
            this.Location = new Point(resolution.Width - this.Width - offsetX, resolution.Height - this.Height - offsetY);
            //

            // Reset the app state to 'Enabled'
            // In case it was disabled when the app closed..
            // ..or the system restarted.            
            Properties.Settings.Default.Enabled = true;
            Properties.Settings.Default.Save();
            //

            idleChecker = new IdleChecker();

            // Check if it's the first run
            if (Properties.Settings.Default.FirstRun == 1)
            {
                // Perform actions specific to the first run
                MessageBox.Show("Hi! Power-Plan Manager is minimized to tray. By default it auto-starts with Windows. Enjoy!");

                if (Properties.Settings.Default.AutoStart = true)
                {
                    // Create Desktop Shortcut **(Future shell:Startup Folder)**
                    CreateShortcut();
                }
                else if (Properties.Settings.Default.AutoStart = false)
                {
                    DeleteShortcut();
                }

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
            }
        }


        private void showWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show(); // Show the form
            this.WindowState = FormWindowState.Normal; // Return to normal!
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
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
                idleChecker.ChangePowerPlan("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"); // (High Performance)
            }
            else
            {
                Properties.Settings.Default.Enabled = false;
                Properties.Settings.Default.Save();
                checkBox1.Text = "System is unmanaged";
                idleChecker.ChangePowerPlan("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"); // (High Performance)
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                // Create the shortcut on the desktop                
                CreateShortcut();
            }
            else
            {                
                DeleteShortcut();
            }
        }

        public void CreateShortcut()
        {
            // Create the shortcut on the desktop
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutLocation = Path.Combine(desktopPath, "Desktop Web Tiles" + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = "New shortcut for a Notepad";
            shortcut.IconLocation = @"C:\Program Files\Desktop Web Tiles\Desktop Web Tiles.exe";
            shortcut.TargetPath = @"C:\Program Files\Desktop Web Tiles\Desktop Web Tiles.exe";
            shortcut.Save();

            // Create persistence
            Properties.Settings.Default.AutoStart = true;
            Properties.Settings.Default.Save();
            checkBox2.Checked = true;
        }

        public void DeleteShortcut()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutLocation = Path.Combine(desktopPath, "Desktop Web Tiles" + ".lnk");

            // Create persistence
            Properties.Settings.Default.AutoStart = false;
            Properties.Settings.Default.Save();
            checkBox2.Checked = false;

            // Check if the shortcut exists
            if (System.IO.File.Exists(shortcutLocation))
            {
                // Delete the shortcut
                System.IO.File.Delete(shortcutLocation);
            }
            else
            {                
                // MessageBox.Show("Shortcut not found.");
            }
        }
    }
}