using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;

namespace KhTracker
{
    public partial class JoinMultiplayerForm : Form
    {
        public JoinMultiplayerForm()
        {
            InitializeComponent();
        }

        private void JoinMultiplayerForm_Load(object sender, EventArgs e)
        {

        }

        private bool ValidateIP()
        {
            IPAddress ip;
            return IPAddress.TryParse(IPEntry.Text, out ip);
        }

        private void Join_Click(object sender, EventArgs e)
        {
            if (ValidateIP())
            {
                global::KhTracker.Properties.Settings.Default.ServerIP = IPEntry.Text;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                //should display an error somehow
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}
