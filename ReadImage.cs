using System;
using System.Windows.Forms;


namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        private void T_override_CheckedChanged(object sender, EventArgs e)
        {
            S_track.Enabled = E_track.Enabled = T_override.Checked;
        }
    }
}