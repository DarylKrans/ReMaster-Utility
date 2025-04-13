using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace V_Max_Tool
{


    public partial class Form1 : Form
    {
        readonly string n_read = "nibread.exe";
        readonly string n_writ = "nibwrite.exe";
        readonly string a_rpm = "rpm1541.exe";
        readonly string[] trk_aln = { "Automatic", "Longest run of unformatted data", "Longest gap", "Longest sync", "Sector 0", "Raw Data" };
        readonly string[] ProHand = { "V-Max! (v3)", "V-Max! (v2) Cinemaware", "GMA/Secruispeed (T38/T39)", "Rainbow Arts/Magic Bytes (T36)",
                                      "Rapidlok", "Vorpal (newer) EPYX", "Pirateslayer/Buster" };
        static bool zero = false;
        static bool rpm = false;

        void Init_Read_Options()
        {
            S_track.Enabled = E_track.Enabled = T_override.Checked;
            R_adv.Enabled = R_advanced.Checked;
            R_Retry.Enabled = Retry.Checked;
            U_test.Enabled = U_sensor.Enabled = U_bitrate.Enabled = U_alignment.Enabled = IHS.Checked;
            R_tgap.Enabled = Read_tgap.Checked;
            R_tgap.Value = 7;
            S_track.Value = 1;
            E_track.Value = 41;

        }

        void Init_Write_Options()
        {
            WriteNib.Controls.Add(Write_GBox);
            Write_GBox.Location = new System.Drawing.Point(0, 0);
            NibWriteImage.Enabled = false;
            rpm = File.Exists($@"{NibPath}\{a_rpm}".Replace(@"\\", @"\"));
            Drv_status.Visible = File.Exists($@"c:\program files\opencbm\cbmctrl.exe");

            W_prot.DataSource = ProHand;
            W_prot.Enabled = WP.Checked;
            W_align.DataSource = trk_aln;
            W_advopts.Enabled = WAdv.Checked;
            W_start.Enabled = W_end.Enabled = W_override.Checked;
            W_align.Enabled = WTA.Checked;
            W_rpm.Enabled = Wrpm.Checked;
            W_tskew.Enabled = W_skew.Checked;
            W_agg.Enabled = W_aggcr.Checked;
            W_tgap.Enabled = W_gapmatch.Checked;
            W_cap.Enabled = W_capmar.Checked;
            W_agg.Value = 1;
            W_end.Value = 41;
            W_rpm.Value = 300;
            W_tskew.Value = 0;
            W_tgap.Value = 7;
            ADJ_RPM.Visible = rpm;
        }

        void Read_Disk()
        {
            string t = "Something (possibly) went wrong!";
            string m = $"Either nibread.exe failed to start\ror something is wrong with your settings\rcheck your settings and try again";
            string args = " ";
            Get_ReadArgs();

            var exe = "\"" + $@"{NibPath}\{n_read}".Replace(@"\\", @"\") + "\"";
            var f = $"{TEMP.path}temp_read.nib";
            if (File.Exists(f))
            {
                File.Delete(f);
            }
            var outfile = args + " \"" + f + "\"";
            //ProcessStartInfo procStartInfo = new ProcessStartInfo($"cmd.exe", $"/c \"{exe} {outfile} & pause\"")
            ProcessStartInfo procStartInfo = new ProcessStartInfo($"cmd.exe", $"/c \"{exe} {outfile}")
            {
                RedirectStandardError = false,
                RedirectStandardOutput = false,
                UseShellExecute = true,
                CreateNoWindow = false,
            };
            Process process = new Process
            {
                StartInfo = procStartInfo
            };

            try
            {
                process.Start();
                process.WaitForExit();
                if (System.IO.File.Exists(f))
                {
                    DateTime n = DateTime.Now;                       // <-- Current date/time
                    DateTime s = System.IO.File.GetCreationTime(f);  // <-- file creation date/time
                    TimeSpan d = (n - s);                            // <-- time elapsed since the file was created
                    if (d.TotalSeconds <= 60)                        // <-- (set tollerance value in seconds) 
                    {
                        if (File.Exists(f) && new FileInfo(f).Length >= 8192 + 256)
                        {
                            fname = Path.GetFileNameWithoutExtension(f).Replace("_ReMaster", "");
                            fext = Path.GetExtension(f);
                            ClearInfo();
                            Process_New_Image(f);
                        }
                    }
                    else MessageForYouSir(t, m);
                }
                else MessageForYouSir(t, m);
            }
            catch { }
            ReadNib.Close();

            void Get_ReadArgs()
            {
                if (ET_matching.Checked) args += "-v ";
                if (T_override.Checked) args += $"-S{S_track.Value} -E{E_track.Value} ";
                if (Dev_num.Value != 8) args += $"-D{Dev_num.Value} ";
                if (Parallel.Checked) args += "-P ";
                if (R_verb.Checked) args += "-V ";
                if (Retry.Checked) args += $"-e{R_Retry.Value}";
                if (R_limit.Checked && !T_override.Checked) args += "-E40 ";
                if (R_advanced.Checked)
                {
                    if (DR_killer.Checked) args += "-k ";
                    if (FD_density.Checked) args += "-d ";
                    if (II_mode.Checked) args += "-I ";
                    if (R_halftracks.Checked) args += "-h ";
                    if (EP_tests.Checked) args += "-t ";
                    if (U_sensor.Checked) args += "-j ";
                    if (U_alignment.Checked) args += "-x ";
                    if (U_bitrate.Checked) args += "-y ";
                    if (U_test.Checked) args += "-z ";
                    if (Read_tgap.Checked) args += $"-G{R_tgap.Value} ";
                }
            }
        }

        private void Read_Start_Click(object sender, EventArgs e)
        {
            if (!No_Warn.Checked)
            {
                using (Message_Center center = new Message_Center(this))
                {
                    DialogResult ays = MessageBox.Show("Please make sure your disk is write-protected!\n\nProceed?", "Caution!", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                    if (ays == DialogResult.OK)
                    {
                        Read_Disk();
                    }
                }
            }
            else Read_Disk();
        }

        void Write_DiskImage(bool erase = false)
        {
            string args = " ";
            string f;
            BuildArgs();
            var exe = "\"" + $@"{NibPath}\{n_writ}".Replace(@"\\", @"\") + "\"";
            if (!zero) f = args + "\"" + $"{TEMP.path}temp_write.g64" + "\""; else f = args + "-u ";
            zero = false; // Zero_Disk.Enabled = true;
            Zero_Disk.Enabled = Write_Start.Enabled = !zero;
            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd.exe", $"/c \"{exe} {f}" + "\"")
            {
                RedirectStandardError = false,
                RedirectStandardOutput = false,
                UseShellExecute = true,
                CreateNoWindow = false,
            };

            Process process = new Process
            {
                StartInfo = procStartInfo
            };
            try
            {
                process.Start();
                process.WaitForExit();
            }
            catch { }
            if (!erase) WriteNib.Close();

            void BuildArgs()
            {
                if (W_num.Value != 8) args += $"-D{W_num.Value} ";
                if (W_override.Checked) args += $"-S{W_start.Value} -E{W_end.Value} ";
                if (WParallel.Checked) args += "-P ";
                if (W_verb.Checked) args += "-v ";
                if (W_limit.Checked && !W_override.Checked) args += "-E40 ";
                if (WAdv.Checked && !zero)
                {
                    if (WP.Checked)
                    {
                        var se = W_prot.SelectedIndex;
                        switch (se)
                        {
                            case 0: args += "-px "; break;
                            case 1: args += "-pc "; break;
                            case 2: args += "-pg "; break;
                            case 3: args += "-pm "; break;
                            case 4: args += "-pr "; break;
                            case 5: args += "-pv "; break;
                            case 6: args += "-pp "; break;
                        }
                    }
                    if (WTA.Checked)
                    {
                        var se = W_align.SelectedIndex;
                        switch (se)
                        {
                            case 0: args += "-aa "; break;
                            case 1: args += "-aw "; break;
                            case 2: args += "-ag "; break;
                            case 3: args += "-as "; break;
                            case 4: args += "-a0 "; break;
                            case 5: args += "-an "; break;
                        }
                    }
                    if (W_skew.Checked) args += $"-T{W_tskew.Value} ";
                    if (W_aggcr.Checked) args += $"-f{W_agg.Value} ";
                    if (W_autobad.Checked) args += $"-f ";
                    if (W_gapmatch.Checked) args += $"-G{W_tgap.Value} ";
                    if (Wrpm.Checked) args += $"-C{W_rpm.Value} ";
                    if (W_capacity.Checked) args += $"-c ";
                    if (W_gapreduce.Checked) args += $"-g ";
                    if (W_gcrrunred.Checked) args += $"-0 ";
                    if (W_syncred.Checked) args += $"-r ";
                    if (W_timedalign.Checked) args += $"-t ";
                    if (W_capmar.Checked) args += $"-m{W_cap.Value} ";
                }
            }
        }

        private void Check_RPM(string dev_num)
        {
            var exe = $@"{NibPath}\{a_rpm}".Replace(@"\\", @"\");
            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd.exe", $"/c \"{exe}\" {dev_num}")
            {
                RedirectStandardError = false,
                RedirectStandardOutput = false,
                UseShellExecute = true,
                CreateNoWindow = false,

            };
            if (System.Environment.OSVersion.Version.Major >= 6)
            {
                procStartInfo.Verb = "runas";
            }
            Process process = new Process
            {
                StartInfo = procStartInfo
            };
            try
            {
                process.Start();
                process.WaitForExit();
            }
            catch { }
        }

        void Reset_Zoom()
        {
            if (File.Exists($@"c:\program files\opencbm\cbmctrl.exe"))
            {
                var exe = $@"c:\program files\opencbm\cbmctrl.exe";
                var args = $"status {W_num.Value}";
                RunCommand(exe, args, string.Empty, false, true);
            }
        }

        private void RunCommand(string exe, string args, string text, bool erase = false, bool usetitle = false)
        {

            ProcessStartInfo procStartInfo = new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = procStartInfo };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Invoke(new Action(() => groupBox4.Text = $" [Drive Status]  {e.Data}"));
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Invoke(new Action(() => groupBox4.Text = $" [Drive Status]  {e.Data}"));
                }
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                Task.Run(delegate
                {
                    process.WaitForExit();
                });
            }
            catch (Exception ex)
            {
                using (Message_Center center = new Message_Center(this))
                {
                    MessageBox.Show($"An error occurred: {ex.Message}");
                }
            }
        }

        void MessageForYouSir(string t, string m)
        {
            MessageBoxButtons b = MessageBoxButtons.OK;
            using (Message_Center center = new Message_Center(this))
            {
                if (InvokeRequired) Invoke(new Action(() => MessageBox.Show(m, t, b, MessageBoxIcon.Warning)));
                else MessageBox.Show(m, t, b, MessageBoxIcon.Warning);
            }
        }

        private void R_advanced_CheckedChanged(object sender, EventArgs e)
        {
            R_adv.Enabled = R_advanced.Checked;
        }

        private void Override_CheckedChanged(object sender, EventArgs e)
        {
            S_track.Enabled = E_track.Enabled = T_override.Checked;
        }

        private void Retry_CheckedChanged(object sender, EventArgs e)
        {
            R_Retry.Enabled = Retry.Checked;
        }

        private void IHS_CheckedChanged(object sender, EventArgs e)
        {
            U_test.Enabled = U_sensor.Enabled = U_bitrate.Enabled = U_alignment.Enabled = IHS.Checked;
        }

        private void Read_tgap_CheckedChanged(object sender, EventArgs e)
        {
            R_tgap.Enabled = Read_tgap.Checked;
        }

        private void Track_ValueChanged(object sender, EventArgs e)
        {
            if (S_track.Value > E_track.Value) { E_track.Value = S_track.Value; }
        }

        private void E_track_ValueChanged(object sender, EventArgs e)
        {
            if (E_track.Value < S_track.Value) { S_track.Value = E_track.Value; }
        }

        private void W_override_CheckedChanged(object sender, EventArgs e)
        {
            W_start.Enabled = W_end.Enabled = W_override.Checked;
        }

        private void W_start_ValueChanged(object sender, EventArgs e)
        {
            if (W_start.Value > W_end.Value) { W_end.Value = W_start.Value; }

        }

        private void W_end_ValueChanged(object sender, EventArgs e)
        {
            if (W_end.Value < W_start.Value) { W_start.Value = W_end.Value; }
        }

        private void WAdv_CheckedChanged(object sender, EventArgs e)
        {
            W_advopts.Enabled = WAdv.Checked;
            W_align.Enabled = WTA.Checked;
            W_tgap.Enabled = W_gapmatch.Checked;
            W_prot.Enabled = WP.Checked;
            W_tskew.Enabled = W_skew.Checked;
            W_rpm.Enabled = Wrpm.Checked;
            W_cap.Enabled = W_capmar.Checked;
        }

        private void W_aggcr_CheckedChanged(object sender, EventArgs e)
        {
            W_agg.Enabled = W_aggcr.Checked;
            if (W_aggcr.Checked && W_autobad.Checked) { W_autobad.Checked = false; }
        }

        private void Zero_Disk_Click(object sender, EventArgs e)
        {
            using (Message_Center center = new Message_Center(this))
            {
                DialogResult ays = MessageBox.Show("You are about to ERASE the disk in the drive!\n\nAre you sure?", "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (ays == DialogResult.Yes)
                {
                    zero = true;
                    Zero_Disk.Enabled = Write_Start.Enabled = !zero;
                    Write_DiskImage(true);
                }
            }
        }

        private void Write_Start_Click(object sender, EventArgs e)
        {
            if (!No_Warn.Checked)
            {
                using (Message_Center center = new Message_Center(this))
                {
                    DialogResult ays = MessageBox.Show("All existing data on the disk will be overwritten!!\n\nAre you sure?", "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (ays == DialogResult.Yes)
                    {
                        Write_DiskImage();
                    }
                }
            }
            else Write_DiskImage();
        }

        private void ADJ_RPM_Click(object sender, EventArgs e)
        {
            Check_RPM(W_num.Value.ToString());
        }

        private void Drv_status_Click(object sender, EventArgs e)
        {
            Reset_Zoom();
        }

        private void W_autobad_CheckedChanged(object sender, EventArgs e)
        {
            if (W_autobad.Checked && W_aggcr.Checked) { W_aggcr.Checked = false; }
        }

    }
}
