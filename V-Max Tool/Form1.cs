using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ReMaster_Utility.Properties;



namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        //private readonly int[] vpl_density = { 7750, 7106, 6635, 6230 }; // <- original values used by ReMaster for faster writing RPM
        private static bool Auto_Adjust = true; // <- Sets the Auto Adjust feature for V-Max and Vorpal images (for best remastering results)
        private static readonly string ver = " v1.3 beta 01252026 (bossdos test)";
        private static readonly string fix = "_ReMaster";
        private static readonly string mod = "_ReMaster"; // _(modified)";
        private static readonly string vorp = "_ReMaster"; //(aligned)";
        //private static readonly byte loader_padding = 0x55;
        private static readonly int[] CBM_Standard_Density = { 7692, 7142, 6666, 6250 }; // <- density zone capacity accoriding to CBM specifications
        private static readonly int[] ReMaster_Adjusted_Density = { 7672, 7122, 6646, 6230 }; // <- adjusted capacity to account for minor RPM variation higher than 300
        private static readonly int[] vpl_density = { 7750, 6950, 6585, 6255 }; // <- Vorpal densities used to be more accurate to original disk-reads
        private static readonly int[] vpl_defaults = { 7750, 6950, 6585, 6255 };
        private readonly int[] density = new int[4];
        private static bool busy = false;
        private static bool cancel = false;
        private static bool error = false;
        private static bool batch = false;
        private static bool nib_error = false;
        private static bool g64_error = false;
        private static bool VM_c_fix = false;
        private static bool exitConfirmed = false;
        private static string nib_err_msg;
        private static string g64_err_msg;
        private static byte[] rak1 = new byte[0];
        private static byte[] cldr_id = new byte[0];
        private static byte[] v2ldrcbm = new byte[0];
        private static byte[] v24e64pal = new byte[0];
        private static byte[] v26446ntsc = new byte[0];
        private static byte[] v2644entsc = new byte[0];
        private static byte[] fastloader = new byte[0];
        private static readonly int fldOffset = 184;
        private static readonly int min_t_len = 3000; // was 6000
        private static int end_track = -1;
        private static int fat_trk = -1;
        System.Windows.Forms.Panel lastHoveredButton = null;

        readonly Form Blank_Disk = new Form
        {
            Text = "Create Blank Disk",
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Size = new Size(250, 150),
            StartPosition = FormStartPosition.Manual
        };

        readonly Form Options = new Form
        {
            Text = "Options",
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Size = new Size(350, 200),
            StartPosition = FormStartPosition.Manual,
            AutoScaleMode = AutoScaleMode.Font
        };

        readonly Form ReadNib = new Form
        {
            Text = "Read Image From Disk",
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Size = new Size(350, 200),
            StartPosition = FormStartPosition.Manual,
            AutoScaleMode = AutoScaleMode.Font
        };

        readonly Form WriteNib = new Form
        {
            Text = "<- Use RPM guide and adjust your drive accordingly (if possible) ** Write Image To Disk",
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Size = new Size(350, 200),
            StartPosition = FormStartPosition.Manual,
            AutoScaleMode = AutoScaleMode.Font
        };

        Form BrowseDB = new Form
        {
            Text = "Browse ReMaster Image Database",
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Size = new Size(900, 600), // 800
            StartPosition = FormStartPosition.Manual,
            AutoScaleMode = AutoScaleMode.Font,
            KeyPreview = true
        };

        Form RecoverDB = new Form
        {
            Text = "Browse images deleted from database",
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Size = new Size(900, 600), // 800
            StartPosition = FormStartPosition.Manual,
            AutoScaleMode = AutoScaleMode.Font,
            KeyPreview = true
        };

        Form EditNotes = new Form
        {
            Text = "Edit Notes",
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Size = new Size(350, 200), // 800
            StartPosition = FormStartPosition.Manual,
            AutoScaleMode = AutoScaleMode.Font,
            KeyPreview = true
        };

        public Form1()
        {
            InitializeComponent();
            this.Text = $"ReMaster {ver}";
            RunBusy(Init);
            Set_ListBox_Items(true, true);
            LDR_test(File.ReadAllBytes($@"c:\test\ldr_test.bin"));
            //byte[] a = new byte[256];
            //List<string> poo = new List<string>();
            //for (int i = 0; i < a.Length; i++)
            //{
            //    a[i] = (byte)i;
            //    byte b = BDS_AA[i];
            //    poo.Add($"{Hex_Val(new byte[] {b})} = {i}");
            //}
            //poo.Sort();
            //File.WriteAllLines($@"c:\test\AA.txt", poo.ToArray());
            //string a = "conan";
            //byte[] b = Encoding.ASCII.GetBytes(a);
            //byte[] c = Encode_BDS_GCR(b, false, false);
            //byte[] d = Decode_BDS_GCR(c, true, false).decoded;
            //string e = Encoding.ASCII.GetString(d);
            //Text = e;

            //byte[] o = File.ReadAllBytes($@"c:\test\c1.bin");
            //byte[] dec = Decode_BDS_GCR(o, true, false).decoded;
            //byte[] dec = File.ReadAllBytes($@"c:\test\c1_dec.bin");
            ////File.WriteAllBytes($@"c:\test\c1_dec.bin", dec);
            //byte[] renc = Encode_BDS_GCR(dec, false, true);
            //File.WriteAllBytes($@"c:\test\c1_enc.bin", renc);


            //byte[] e = File.ReadAllBytes($@"c:\test\tsecmod.bin");
            //int pos = 0;
            //List<byte> dec = new List<byte>();
            //byte c = 0;
            //while (pos < e.Length)
            //{
            //    byte a = (byte)(c ^= Decode_BDS_Pair(e[pos++], e[pos++]));
            //    dec.Add(a);
            //}
            //File.WriteAllBytes($@"c:\test\tsec_dec1.bin", dec.ToArray());

            ///---------- Cart-Patch sector processing helpers
            //Bossdos(File.ReadAllBytes($@"c:\test\bd_raw4.bin"));
            //secF
            //byte[] f = File.ReadAllBytes($@"c:\test\xev2secmod.bin");
            //
            ///// encrypted sector
            //byte[] g = Encode_VM1_GCR(f, true, true);
            //File.WriteAllBytes($@"c:\test\xev2Changes.bin", CopyArray(Decode_CBM_GCR(g).decoded, 1, 256));
            //File.WriteAllBytes($@"c:\test\xev2renc.bin", g);

            /// plain sector
            //byte c = 0;
            //for (int i = 1; i < 256; i++) c ^= f[i];
            //f[256] = c;
            //File.WriteAllBytes($@"c:\test\gauntChanges.bin", CopyArray(f, 1, 256));
            //File.WriteAllBytes($@"c:\test\gauntrenc.bin", Build_Sector(CopyArray(f, 1, 256))); //, true));
            /// ----------------------------------------------

            //BinToByte_Table($@"c:\test\bds_dec.bin", $@"c:\test\bds_tbl.txt", "BDS_LUT", 16);
            //BinToDictionary2($@"c:\test\track1.bin", $@"c:\test\track1_1.bin", $@"c:\test\vm_table.txt", "VMax_DecodeTable", "byte", "byte", 8);

            button2.Visible = EnableDBMenu.Checked = false;
            //button1.Visible = button2.Visible = false;
        }

        private void Drag_Drop(object sender, DragEventArgs e)
        {
            string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop);
            if ((fileList.Length > 1) || Directory.Exists(fileList[0]))
            {
                if (ShowConfirmation("Multiple Files Selected", "Only .NIB/NBZ files will be processed\nand exported as .G64 with the\nAuto-Adjust options\n\nStart Batch-Processing?"))
                {
                    ClearInfo();
                    Worker_Main = new Thread(new ThreadStart(() => Batch_Get_File_List(fileList)));
                    Worker_Main.Start();
                }
            }
            else ProcessSingleFile(fileList[0]);

            bool ShowConfirmation(string title, string message)
            {
                using (Message_Center center = new Message_Center(this))
                {
                    return MessageBox.Show(message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK;
                }
            }

            // Main (single file) drag/drop handler
            void ProcessSingleFile(string filePath)
            {
                if (System.IO.File.Exists(filePath) && supported.Any(s => s == Path.GetExtension(filePath).ToLower()))
                {
                    fname = Path.GetFileNameWithoutExtension(filePath).Replace("_ReMaster", "");
                    fext = Path.GetExtension(filePath);
                    ClearInfo();
                    Process_New_Image(filePath);
                }
            }
        }

        void Process_New_Image(string file)
        {
            Disable_Core_Controls(true);
            Blk_pan.Enabled = false;
            try
            {
                Data_Box.Clear();
                Track_Info.Items.Clear();
                bool process = false;
                var ext = fext.ToLower();
                bool get = ext != ".g64" && ext != ".z64";
                if (ext == ".nib" || ext == ".nbz") process = Import_NIB(file, ext == ".nbz");
                if (ext == ".g64" || ext == ".z64") process = Import_G64(file, ext == ".z64");
                if (ext == ".d64") process = Import_D64(file);
                if (process) Process(get, "");
            }
            catch (Exception ex)
            {
                using (Message_Center center = new Message_Center(this)) // center message box
                {
                    string t = "Something went wrong!";

                    string s = ex.Message;
                    if (s.ToLower().Contains("source array")) s = "Image is corrupt and cannot be opened";
                    MessageBox.Show(s, t, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    error = true;
                }
            }
            GC.Collect();
            if (!error && !supported.Any(s => s == fext.ToLower()))
            {
                Reset_to_Defaults();
                label1.Text = "File not Valid!";
                label2.Text = string.Empty;
            }
            if (error)
            {
                Reset_to_Defaults();
                label1.Text = "";
                label2.Text = string.Empty;
                error = false;
            }

            void Process(bool get, string l2)
            {
                Batch_List_Box.Visible = false;
                Dir_screen.Clear();
                Dir_screen.Text = "LOAD\"$\",8\nSEARCHING FOR $\nLOADING";
                //loader_fixed = false;
                Worker_Main?.Abort();
                Worker_Main = new Thread(new ThreadStart(() => Do_work(file)));
                Worker_Main.Start();
            }
        }

        void Do_work(string file, bool recent = true)
        {
            Stopwatch parse = new Stopwatch();
            Stopwatch proc = new Stopwatch();
            try
            {
                parse = Parse_Nib_Data();
            }
            catch { }
            if (!error)
            {
                Invoke(new Action(() =>
                {
                    try
                    {
                        proc = Process_Nib_Data(true, false, true);
                        if (DB_timers.Checked) Invoke(new Action(() => label2.Text = $"Parse time : {parse.Elapsed.TotalMilliseconds} ms, Process time : {proc.Elapsed.TotalMilliseconds} ms, Total {parse.Elapsed.TotalMilliseconds + proc.Elapsed.TotalMilliseconds} ms"));
                        Set_ListBox_Items(false, false);
                        Set_Dir(Get_Disk_Directory());
                        Set_BlockMap();
                        Source.Visible = Output.Visible = true;
                        label1.Text = $"{fname}{fext}";
                        M_render.Enabled = true;
                        Set_Buttons_Active();
                        Blk_pan.Enabled = true;
                        if (recent) AddRecentFile(file);
                        P_Cart.Visible = NDS.Cart_Protection || (RemMan && NDS.External_Protection);
                    }
                    catch (Exception ex)
                    {
                        if (!batch)
                        {
                            using (Message_Center center = new Message_Center(this)) // center message box
                            {
                                string t = "Something went wrong!";
                                string s = ex.Message;
                                MessageBox.Show(s, t, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            Reset_to_Defaults();
                        }
                    }
                    if (!batch && ErrorList.Count > 0)
                    {
                        int[] norep = new int[] { };
                        bool norepair = NDS.cbm.Any(x => norep.Contains(x));
                        List<string> list = new List<string>(ErrorList);
                        var s = Sort_Errors(list);
                        s += norepair ? "\nThis image cannot be repaired (yet)\nOutput image may not work" : "\n Would you like to (attempt) repairing?";
                        using (Message_Center center = new Message_Center(this)) // center message box
                        {
                            string t = "File Integrity Warning!";
                            DialogResult result = MessageBox.Show(s, t, norepair ? MessageBoxButtons.OK : MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (result == DialogResult.Yes) Fix_Errors();
                        }
                    }
                }));
            }

        }

        void Set_Buttons_Active()
        {
            Adv_ctrl.Enabled = true;
            Save_Disk.Visible = true;
            linkLabel1.Visible = false;
            Disable_Core_Controls(false);
            saveAsToolStripMenuItem.Enabled = true;
            NibWriteImage.Enabled = true;
        }

        private void Drag_Enter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Make(object sender, EventArgs e)
        {
            Export_File(end_track);
        }

        private void V2_Custom_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                RunBusy(() =>
                {
                    V2_hlenD0.Enabled = V2_hlenD1.Enabled = v2cc = V2_Custom.Checked;
                    if (V2_Custom.Checked) V2_Auto_Adj.Checked = v2aa = false;
                    V2_pad55.Enabled = !V2_Auto_Adj.Checked;
                });
                V2_Adv_Opts();
            }
        }

        private void AutoAdj_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                RunBusy(() =>
                {
                    //if (V2_Auto_Adj.Checked) V2_Custom.Checked = V2_hlen.Enabled = V2_Add_Sync.Checked = false;
                    if (V2_Auto_Adj.Checked)
                    {
                        V2_Custom.Checked = v2cc = V2_hlenD0.Enabled = V2_hlenD1.Enabled = V2_cust_snc.Checked =
                        V2_pad55.Enabled = V2_cust_snc.Checked = V2_sync_len.Enabled = false;
                        V2_Add_Sync.Checked = true;
                    }
                    if (!V2_Auto_Adj.Checked)
                    {
                        v2aa = V2_Add_Sync.Checked = false;
                        V2_pad55.Enabled = true;
                        V2_sync_len.Enabled = V2_cust_snc.Checked;
                    }
                });
                V2_Adv_Opts();
            }
        }

        private void V3_Auto_Adj_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                RunBusy(() =>
                {
                    if (V3_Auto_Adj.Checked) V3_Custom.Checked = v3cc = V3_hlen.Enabled = V3_Trim.Enabled =
                        V3_Cust_Sync.Checked = V3_syncLen.Enabled = false;
                    if (!V3_Auto_Adj.Checked) { v3aa = false; V3_Trim.Enabled = true; }
                });
                V3_Auto_Adjust();
            }
        }

        private void V3_Custom_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                RunBusy(() =>
                {
                    if (V3_Custom.Checked)
                    {
                        V3_Auto_Adj.Checked = v3aa = false;
                        V3_hlen.Enabled = true;
                    }
                    else
                    {
                        V3_hlen.Enabled = false;
                        v3cc = false;
                    }
                });
                V3_hlen.Enabled = V3_Custom.Checked;
                V3_Trim.Enabled = !V3_Auto_Adj.Checked;
                V3_Auto_Adjust();
            }
        }

        private void Adj_cbm_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy) RunBusy(V3_Auto_Adjust);
        }

        private void V2_Add_Sync_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                //if (V2_Add_Sync.Checked) RunBusy(() => V2_Auto_Adj.Checked = false);
                V2_Adv_Opts();
            }
        }

        private void V2_hlen_ValueChanged(object sender, EventArgs e)
        {
            V2_Adv_Opts();
        }

        private void V3_hlen_ValueChanged(object sender, EventArgs e)
        {
            V3_Auto_Adjust();
        }

        private void Manual_render_Click(object sender, EventArgs e)
        {
            drawn = false;
            Check_Before_Draw(false);
        }

        private void LinkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel1.LinkVisited = true;
            Process.Start("https://github.com/DarylKrans/V-Max-Sync-Tool");
        }

        private void VPL_lead_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                RunBusy(() =>
                {
                    Lead_In.Enabled = VPL_lead.Checked;
                    if (VPL_lead.Checked)
                    {
                        VPL_rb.Checked = Lead_ptn.Enabled = true;
                        VPL_only_sectors.Checked = VPL_auto_adj.Checked = false;
                        VPL_presync.Enabled = VPL_auto_adj.Checked;
                    }
                });
                Vorpal_Rebuild();
            }
        }

        private void VPL_rb_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                RunBusy(() =>
                {
                    if (!VPL_rb.Checked) VPL_lead.Checked = Lead_In.Enabled = VPL_only_sectors.Checked = VPL_auto_adj.Checked = Adj_cbm.Checked = false;
                    VPL_presync.Enabled = VPL_auto_adj.Checked;
                    Lead_ptn.Enabled = VPL_rb.Checked;
                    if (VPL_rb.Checked) VPL_auto_adj.Checked = false;
                    VPL_presync.Enabled = VPL_auto_adj.Checked;
                });
                Vorpal_Rebuild();
            }
        }

        private void Lead_In_ValueChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                Vorpal_Rebuild();
            }
        }

        private void VPL_only_sectors_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                RunBusy(() =>
                {
                    if (VPL_only_sectors.Checked)
                    {
                        Lead_In.Enabled = VPL_lead.Checked = VPL_auto_adj.Checked = false;
                        VPL_rb.Checked = true;
                    }
                });
                Vorpal_Rebuild();
            }
        }

        private void VPL_Auto_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                RunBusy(() =>
                {
                    Lead_In.Enabled = VPL_lead.Checked = VPL_only_sectors.Checked = VPL_rb.Checked = false;
                    VPL_presync.Enabled = VPL_auto_adj.Checked;
                });
                Vorpal_Rebuild();
            }
        }

        private void Disp_Data_Click(object sender, EventArgs e)
        {
            if (busy) Data_Viewer(true);
            else Data_Viewer();
        }

        //private void Jump_ValueChanged(object sender, EventArgs e)
        //{
        //    View_Jump();
        //}

        private void DV_gcr_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                if ((RadioButton)sender == DV_gcr) DV_Disassemble.Checked = false;
                Data_Viewer();
            }
        }

        private void Data_Sep_SelectedIndexChanged(object sender, EventArgs e)
        {
            Data_Viewer();
        }

        private void Show_sec_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) drawn = false;
            if (!busy)
            {
                Check_Before_Draw(false);
            }
        }

        private void Img_Q_SelectedIndexChanged(object sender, EventArgs e)
        {
            drawn = false;
            if (!busy)
            {
                Check_Before_Draw(false);
            }
        }

        private void VD0_ValueChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                vpl_density[0] = Convert.ToInt32(VD0.Value);
                vpl_density[1] = Convert.ToInt32(VD1.Value);
                vpl_density[2] = Convert.ToInt32(VD2.Value);
                vpl_density[3] = Convert.ToInt32(VD3.Value);
                for (int i = 0; i < vpl_density.Length; i++)
                {
                    if (vpl_density[i] != vpl_defaults[i]) { VPL_density_reset.Visible = true; break; }
                }
                Vorpal_Rebuild();
            }
        }

        private void B_cancel_Click(object sender, EventArgs e)
        {
            cancel = true;
        }

        private void Re_Align_CheckedChanged(object sender, EventArgs e)
        {
            V2_Adv_Opts();
        }

        private void DB_vpl_CheckedChanged(object sender, EventArgs e)
        {
            usecpp = !CPP_tog.Checked;
            this.Text = $"Re-Master {ver}";
        }

        private void Debug_Button_Click(object sender, EventArgs e)
        {
            RunBusy(Create_Blank_Disk);
            Blank_Disk.Close();
        }

        private void V2_Swap_Headers_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy) V2_Adv_Opts();
        }

        private void V2_swap_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                RunBusy(() => V2_Auto_Adj.Checked = true);
                RunBusy(() => V2_Custom.Checked = V2_Add_Sync.Checked = false);
                GetNewHeaders();
                V2_Adv_Opts();
            }
        }

        private void DB_cores_ValueChanged(object sender, EventArgs e)
        {
            Cores = Convert.ToInt32(DB_cores.Value);
            Set_Cores(false);
        }

        private void DB_core_override_CheckedChanged(object sender, EventArgs e)
        {
            DB_cores.Enabled = DB_core_override.Checked;
            if (DB_core_override.Checked)
            {
                Cores = Convert.ToInt32(DB_cores.Value);
            }
            else
            {
                Cores = Default_Cores;
            }
            Set_Cores();
        }

        private void ListBox1_DoubleClick(object sender, EventArgs e)
        {
            var a = Batch_List_Box.SelectedIndex;
            if (a >= 0 && a < Batch_List_Box.Items.Count)
            {
                string argument = "/select, \"" + LB_File_List[a].Replace(@"\\", @"\") + "\"";
                if (System.IO.File.Exists(LB_File_List[a])) Process.Start("explorer.exe", argument);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!exitConfirmed)
            {
                bool shouldExit = Exit(batch);
                if (!shouldExit)
                {
                    e.Cancel = true; // User cancelled exit
                }
                else
                {
                    exitConfirmed = true; // Mark it confirmed to avoid prompt loop
                    Terminate();
                }
            }
        }

        private bool Exit(bool busy)
        {
            if (busy)
            {
                using (Message_Center center = new Message_Center(this))
                {
                    string message = "Cancel operations and exit?";
                    string title = "Operations in progress!";
                    DialogResult exit = MessageBox.Show(message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                    return exit == DialogResult.OK;
                }
            }
            return true;
        }

        private void Terminate()
        {
            try
            {
                cancel = true;
                this.Text = "Closing..";
                SaveSettings();
                Application.Exit();
                Environment.Exit(0);
            }
            catch { }
            this.Close();
        }

        private void RL_Fix_CheckedChanged(object sender, EventArgs e)
        {
            RL_success.Visible = RL_Fix.Checked;
            if (!busy && RL_Fix.Checked)
            {
                if (NDS.cbm.Any(x => x == 6)) RL_success.Text = RL_Remove_Protection();
            }
        }

        private void SaveDataBox_TextOutput(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string Style;
            if (DV_gcr.Checked) Style = "(GCR"; else Style = "(GCR-Decoded";
            if (VS_hex.Checked) Style += "_HEX)";
            if (VS_bin.Checked) Style += "_Binary)";
            if (VS_dat.Checked) Style += "_Data)";

            Save_Dialog.Filter = "Text File|*.txt";
            Save_Dialog.Title = "Save Image File";
            Save_Dialog.FileName = $"{fname}{Style}{fext.ToLower().Replace('.', '(')})";
            Save_Dialog.ShowDialog();
            string fs = Save_Dialog.FileName;
            if (fs != "" || fs != null) System.IO.File.WriteAllText(fs, Data_Box.Text);
        }

        private void RL_ChangeKey_CheckedChanged(object sender, EventArgs e)
        {
            if (RL_ChangeKey.Checked) Replace_RapidLok_Key = true; else Replace_RapidLok_Key = false;
            Clear_Out_Items();
            Process_Nib_Data(true, false, false, true);
        }

        private void VPL_mod_CheckedChanged(object sender, EventArgs e)
        {
            VD0.Enabled = VD1.Enabled = VD2.Enabled = VD3.Enabled = VPL_mod.Checked;
            if (!VPL_mod.Checked)
            {
                Density_Reset();
            }
        }

        private void VPL_density_reset_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Density_Reset();
        }

        private void RM_cyan_CheckedChanged(object sender, EventArgs e)
        {
            int tk = tracks > 42 ? 8 : 4;
            if (NDG.Track_Data?[tk] != null && RM_cyan.Checked)
            {
                byte[] temp = Cyan_Loader_Patch(NDG.Track_Data[tk]);
                Set_Dest_Arrays(temp, tk);
            }
        }

        private void Density_Range_CheckedChanged(object sender, EventArgs e)
        {
            SwapDensities(tracks > 0 && !NDS.cbm.Any(x => x == 5));
            if (!busy)
            {
                Clear_Out_Items();
                bool p = true;
                if (Adj_cbm.Checked && !V3_Auto_Adj.Checked) p = false;
                Process_Nib_Data(true, p, false, true);
            }
        }

        private void Dir_Screen_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;  // Allow the file to be dropped
            }
            else
            {
                e.Effect = DragDropEffects.None;  // Disallow other types of drops
            }
        }

        private void Dir_Screen_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Array.Sort(files, (x, y) => new FileInfo(x).Length.CompareTo(new FileInfo(y).Length));
            ProcessNewFiletoImage(files);
            //ProcessNewFiletoImage((string[])e.Data.GetData(DataFormats.FileDrop));
        }

        private void NewDiskBtn_Click(object sender, EventArgs e, string[] File_List)
        {
            BD_name.Text = ND_name.Text;
            BD_id.Text = ND_id.Text;
            Sec_Interleave.SelectedIndex = S_Interleave.SelectedIndex;
            GB_NewDisk.Visible = false;
            if (SortBySize.Checked) Array.Sort(File_List, (x, y) => new FileInfo(x).Length.CompareTo(new FileInfo(y).Length));
            ProcessNewFiletoImage(File_List);
        }



        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close(); // triggers OnFormClosing, which handles confirmation and cleanup
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form AboutForm = new Form
            {
                Text = "About ReMaster Utilities",
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                // Center relative to the main form
                Size = new Size(600, 650),
                StartPosition = FormStartPosition.Manual
            };
            AboutForm.Location = new Point(
                this.Location.X + (this.Width - AboutForm.Width) / 2,
                this.Location.Y + (this.Height - AboutForm.Height) / 2);

            RichTextBox richTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = this.BackColor,
                Font = new Font("Segoe UI", 10),
                Enabled = false,
                ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None,
                Rtf = Resources.about1
            };
            AboutForm.Controls.Add(linkLabel1);
            AboutForm.Controls.Add(richTextBox);
            linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel1_LinkClicked);
            linkLabel1.Location = new Point((AboutForm.Width - linkLabel1.Width) - 60, 2);
            linkLabel1.Visible = true;
            linkLabel1.BringToFront();
            AboutForm.ShowDialog(this);
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Open Disk Image";
            openFileDialog1.Filter = "Disk Images (*.nib;*.nbz;*.g64;*.d64)|*.nib;*.nbz;*.g64;*.d64";
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.FileName = "";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = openFileDialog1.FileName;
                if (File.Exists(selectedFile))
                {
                    fname = Path.GetFileNameWithoutExtension(selectedFile).Replace("_ReMaster", "");
                    fext = Path.GetExtension(selectedFile);
                    ClearInfo();
                    Process_New_Image(selectedFile);
                }
            }
        }

        private void OpenFileFromRecent(string filename)
        {
            if (File.Exists(filename))
            {
                fname = Path.GetFileNameWithoutExtension(filename).Replace("_ReMaster", "");
                fext = Path.GetExtension(filename);
                ClearInfo();
                Process_New_Image(filename);
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Export_File(end_track);
        }

        private void CreateBlankDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Blank_Disk.Location = new Point(
                this.Location.X + (this.Width - Blank_Disk.Width) / 2,
                this.Location.Y + (this.Height - Blank_Disk.Height) / 2);

            Blank_Disk.Controls.Add(CBD_box);
            CBD_box.Location = new Point(0, 0);
            CBD_box.Visible = true;
            CBD_box.Size = PreferredSize;
            Blank_Disk.ShowDialog(this);
        }

        private void CBD_Cancel_Click(object sender, EventArgs e)
        {
            Blank_Disk.Close();
        }

        private void OptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Options.Location = new Point(
               this.Location.X + (this.Width - Options.Width) / 2,
               this.Location.Y + (this.Height - Options.Height) / 2);

            //Options_Box.Size = PreferredSize;
            Options.Size = new Size(Options_Box.Width + 10, Options_Box.Height + 30);
            Options.ShowDialog(this);
        }

        private void ConfigurePathToNibtools_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowNewFolderButton = false;
            DialogResult ok = folderBrowserDialog1.ShowDialog(this);
            if (ok == DialogResult.OK)
            {
                string fldr = $@"{folderBrowserDialog1.SelectedPath}\".Replace(@"\\", @"\");
                if (!(File.Exists(fldr + "nibread.exe") || File.Exists(fldr + "nibwrite.exe")))
                {
                    using (Message_Center center = new Message_Center(this))
                    {
                        var message = "Nibread/Nibwrite doesn't exist here";
                        var title = "Nibtools not found!";
                        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    File.WriteAllText(TEMP.path + TEMP.Nibtools, fldr);
                    FindNibtools();
                }
            }
        }

        private void NibReadImage_Click(object sender, EventArgs e)
        {
            Read_GBox.Size = Read_GBox.PreferredSize;
            ReadNib.Size = new Size(Read_GBox.Width, Read_GBox.Height + 40);
            ReadNib.Location = new Point(
                this.Location.X + (this.Width - ReadNib.Width) / 2,
                this.Location.Y + (this.Height - ReadNib.Height) / 2);
            ReadNib.ShowDialog(this);
        }

        private void NibWriteImage_Click(object sender, EventArgs e)
        {
            Make_G64($"{TEMP.path}temp_write.g64", end_track);
            Write_GBox.Size = Write_GBox.PreferredSize;
            WriteNib.Size = new Size(Write_GBox.Width + 10, Write_GBox.Height + 30);
            WriteNib.Location = new Point(
                this.Location.X + ((this.Width - WriteNib.Width) / 2) + 120,
                this.Location.Y + (this.Height - WriteNib.Height) / 2);
            WriteNib.ShowDialog(this);
        }

        private void No_Warn_CheckedChanged(object sender, EventArgs e)
        {
            if (No_Warn.Checked && !busy)
            {
                using (Message_Center center = new Message_Center(this))
                {
                    string message = "With this enabled, you will not be prompted before\nreading or writing a disk image!\n\n" +
                                     "I am not responsible for any lost data\nplease make sure your important disks (originals)\n" +
                                     "or ANYTHING you don't want destroyed is write-protected.";
                    string title = "Proceed with caution!";
                    MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void CBM_Fix_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy && !batch && CBM_Fix.Checked)
            {
                Fix_Errors();
                //ErrorList = new ConcurrentBag<string>();
                //Repair_CBM_Checksums();
                CBM_Fix.Checked = false;
            }
        }

        private void EnableDBMenu_CheckedChanged(object sender, EventArgs e)
        {
            ProtDetectMethod.Enabled = ParseLog.Enabled = databaseToolStripMenuItem.Visible = EnableDBMenu.Checked;
            if (databaseToolStripMenuItem.Visible)
            {
                ReadDB(false, true);
                dbRemoved = disk != null && disk?.Length > 0 ? disk.Where(d => d.Marked).Select(d => d.Index).ToList() : new List<int>();
                SetDBContextItems();
            }
        }

        private void DV_Disassemble_CheckedChanged(object sender, EventArgs e)
        {
            if (DV_Disassemble.Checked)
            {
                DV_dec.Checked = true;
                groupBox1.Enabled = false;
            }
            else groupBox1.Enabled = true;
            Data_Viewer();
        }

        private void P_Cart_CheckedChanged(object sender, EventArgs e)
        {
            VM_c_fix = P_Cart.Visible && P_Cart.Checked;
            //NDS.Cart_Fix = P_Cart.Visible && P_Cart.Checked;
            //if (P_Cart.Checked) V3_Auto_Adjust();
            V3_Auto_Adjust();
        }

        private void DB_force_CheckedChanged(object sender, EventArgs e)
        {
            if (DB_force.Checked) Adj_cbm.Enabled = true;
            else
            {
                if (tracks > 0)
                {
                    int[] fmts = new int[] { 5, 6, 8, 10, 11, 12 };
                    if (!NDS.cbm.Any(x => fmts.Contains(x))) Adj_cbm.Enabled = (!NDS.cbm.Any(x => x == 4) && !NDS.Prot_Method.ToLower().Contains("(cbm)"));
                    else Adj_cbm.Enabled = true;
                    if (!Adj_cbm.Enabled && Adj_cbm.Checked)
                    {
                        Adj_cbm.Checked = false;
                        Clear_Out_Items();
                        Process_Nib_Data(true, false, false, true);
                    }
                }
            }
        }

        private void V2_cust_snc_CheckedChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                V2_sync_len.Enabled = V2_cust_snc.Checked;
                if (V2_cust_snc.Checked) V2_Auto_Adj.Checked = false;
                V2_Adv_Opts();
            }
        }

        private void V2_sync_len_ValueChanged(object sender, EventArgs e)
        {
            if (V2_cust_snc.Checked) V2_Adv_Opts();
        }

        private void V2_pad55_CheckedChanged(object sender, EventArgs e)
        {
            if (!V2_Auto_Adj.Checked) V2_Adv_Opts();
        }

        private void T_jump_SelectedItemChanged(object sender, EventArgs e)
        {
            View_Jump();
        }

        private void T_jump_SelectedIndexChanged(object sender, EventArgs e)
        {
            View_Jump();
        }

        private void V3_Trim_CheckedChanged(object sender, EventArgs e)
        {
            V3_Auto_Adjust();
        }

        private void V3_Cust_Sync_CheckedChanged(object sender, EventArgs e)
        {
            RunBusy(() =>
            {
                if (V3_Cust_Sync.Checked) V3_Auto_Adj.Checked = v3aa = false;
                V3_syncLen.Enabled = V3_Cust_Sync.Checked;
            });
            V3_Auto_Adjust();
        }

        private void V3_syncLen_ValueChanged(object sender, EventArgs e)
        {
            V3_Auto_Adjust();
        }

        private void ReAlign_v3_CheckedChanged(object sender, EventArgs e)
        {
            V3_Auto_Adjust();
        }
    }
}
