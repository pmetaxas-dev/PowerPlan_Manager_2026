namespace Power_Plan_Manager_Take_8
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            button1 = new Button();
            trayIcon = new NotifyIcon(components);
            contextMenuStrip1 = new ContextMenuStrip(components);
            showWindowToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            label2 = new Label();
            label3 = new Label();
            pictureBox1 = new PictureBox();
            checkBox1 = new CheckBox();
            checkBox2 = new CheckBox();
            contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Font = new Font("Segoe UI", 9F);
            button1.Location = new Point(12, 90);
            button1.Margin = new Padding(4, 5, 4, 5);
            button1.Name = "button1";
            button1.Size = new Size(70, 52);
            button1.TabIndex = 0;
            button1.Text = "About";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // trayIcon
            // 
            trayIcon.ContextMenuStrip = contextMenuStrip1;
            trayIcon.Icon = (Icon)resources.GetObject("trayIcon.Icon");
            trayIcon.Text = "Power-Plan Manager";
            trayIcon.Visible = true;
            trayIcon.MouseDoubleClick += trayIcon_MouseDoubleClick;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(28, 28);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { showWindowToolStripMenuItem, exitToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(200, 68);
            // 
            // showWindowToolStripMenuItem
            // 
            showWindowToolStripMenuItem.Name = "showWindowToolStripMenuItem";
            showWindowToolStripMenuItem.Size = new Size(199, 32);
            showWindowToolStripMenuItem.Text = "Show Window";
            showWindowToolStripMenuItem.Click += showWindowToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(199, 32);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 7F);
            label2.Location = new Point(10, 43);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(120, 19);
            label2.TabIndex = 3;
            label2.Text = "By Panos Metaxas";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 10F);
            label3.Location = new Point(145, 124);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(204, 28);
            label3.TabIndex = 4;
            label3.Text = "workersoft@gmx.com";
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.ErrorImage = (Image)resources.GetObject("pictureBox1.ErrorImage");
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(12, 10);
            pictureBox1.Margin = new Padding(2, 3, 2, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(332, 43);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 5;
            pictureBox1.TabStop = false;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Checked = true;
            checkBox1.CheckState = CheckState.Checked;
            checkBox1.Font = new Font("Segoe UI", 8F);
            checkBox1.Location = new Point(165, 60);
            checkBox1.Margin = new Padding(2, 3, 2, 3);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(171, 25);
            checkBox1.TabIndex = 6;
            checkBox1.Text = "System is managed";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Checked = true;
            checkBox2.CheckState = CheckState.Checked;
            checkBox2.Font = new Font("Segoe UI", 8F);
            checkBox2.Location = new Point(165, 90);
            checkBox2.Margin = new Padding(2, 3, 2, 3);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(145, 25);
            checkBox2.TabIndex = 7;
            checkBox2.Text = "Load on startup";
            checkBox2.UseVisualStyleBackColor = true;
            checkBox2.CheckedChanged += checkBox2_CheckedChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(354, 155);
            Controls.Add(checkBox2);
            Controls.Add(label2);
            Controls.Add(checkBox1);
            Controls.Add(pictureBox1);
            Controls.Add(label3);
            Controls.Add(button1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 5, 4, 5);
            Name = "Form1";
            ShowInTaskbar = false;
            Text = "Power-Plan Manager";
            WindowState = FormWindowState.Minimized;
            SizeChanged += Form1_SizeChanged;
            contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        public NotifyIcon trayIcon;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem showWindowToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private Label label2;
        private Label label3;
        private PictureBox pictureBox1;
        private CheckBox checkBox1;
        private CheckBox checkBox2;
    }
}