using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Power_Plan_Manager_Take_8
{
    public partial class About_Window : Form
    {
        public About_Window()
        {
            InitializeComponent();

            // Make window unresizable and centered
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
        }
    }
}
