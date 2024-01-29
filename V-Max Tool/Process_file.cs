﻿using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        private string fname = "";
        private string fext = "";
        private string dirname = "";
        private string fnappend = "";
        private int tracks = 0;
        private byte[] nib_header = new byte[256];
        private readonly byte[] g64_header = new byte[684];
        private readonly string[] supported = { ".nib", ".g64" }; // Supported file extensions list
        // vsec = the CBM sector header values & against byte[] sz
        private readonly string[] valid_cbm = { "52-40-05-28", "52-40-05-2C", "52-40-05-48", "52-40-05-4C", "52-40-05-38", "52-40-05-3C", "52-40-05-58", "52-40-05-5C",
            "52-40-05-24", "52-40-05-64", "52-40-05-68", "52-40-05-6C", "52-40-05-34", "52-40-05-74", "52-40-05-78", "52-40-05-54", "52-40-05-A8",
            "52-40-05-AC", "52-40-05-C8", "52-40-05-CC", "52-40-05-B8" };
        // vmax = the block header values of V-Max v2 sectors (non-CBM sectors)
        private readonly string[] secF = { "NDOS", "CBM", "V-Max v2", "V-Max v3", "Loader", "tbd", "Unformatted" };

        void Parse_Nib_Data()
        {
            double ht;
            bool halftracks = false;
            string[] f;
            string[] headers;
            listBox3.BeginUpdate();
            string tr = "Track";
            string le = "Length";
            string fm = "Format";
            string bl = "** Potentially bad loader! **";
            if (tracks > 42)
            {
                halftracks = true;
                ht = 0.5;
            }
            else ht = 0;
            int t;
            for (int i = 0; i < tracks; i++)
            {
                NDS.cbm[i] = Get_Data_Format(NDS.Track_Data[i]);
                if (NDS.cbm[i] == 0)
                {
                    int l = Get_Track_Len(NDS.Track_Data[i]);
                    if (l > 6000 && l < 8192)
                    {
                        if (tracks > 42) t = i / 2 + 1; else t = i + 1;
                        NDS.Track_Length[i] = l << 3;
                        NDS.D_Start[i] = 0;
                        NDS.D_End[i] = l;
                        NDS.sectors[i] = 0;
                        NDS.Sector_Zero[i] = 0;
                        listBox3.Items.Add($"{tr} {t} {fm} : {secF[NDS.cbm[i]]}");
                        listBox3.Items.Add($"Track Length {l}");
                    }
                }
                if (NDS.cbm[i] == 1)
                {
                    if (tracks > 42) t = i / 2 + 1; else t = i + 1;
                    listBox3.Items.Add($"{tr} {t} {fm} : {secF[NDS.cbm[i]]}");
                    (NDS.D_Start[i], NDS.D_End[i], NDS.Sector_Zero[i], NDS.Track_Length[i], f, NDS.sectors[i], NDS.cbm_sector[i], NDS.Total_Sync[i]) = Find_Sector_Zero(NDS.Track_Data[i]);
                    for (int j = 0; j < f.Length; j++)
                    {
                        listBox3.Items.Add($"{f[j]}");
                    }
                    listBox3.Items.Add(" ");
                    NDA.sectors[i] = NDS.sectors[i];
                }
                if (NDS.cbm[i] == 2)
                {
                    (NDA.Track_Data[i], NDS.D_Start[i], NDS.D_End[i], NDS.Sector_Zero[i], NDS.Track_Length[i], headers, NDS.sectors[i], NDS.v2info[i]) = Get_V2_Track_Info(NDS.Track_Data[i], i);
                    for (int j = 0; j < headers.Length; j++)
                    {
                        listBox3.Items.Add(headers[j]);
                        listBox3.Update();
                    }
                }
                if (NDS.cbm[i] == 3)
                {
                    if (tracks > 42) t = i / 2 + 1; else t = i + 1;
                    listBox3.Items.Add($"{tr} {t} {fm} : {secF[NDS.cbm[i]]}");
                    int len;
                    (f, NDS.D_Start[i], NDS.D_End[i], NDS.Sector_Zero[i], len, NDS.sectors[i], NDS.Header_Len[i]) = Get_vmv3_track_length(NDS.Track_Data[i], i);
                    NDS.Track_Length[i] = len * 8;
                    NDS.Sector_Zero[i] *= 8;
                    NDA.sectors[i] = NDS.sectors[i];
                    for (int j = 0; j < f.Length; j++)
                    {
                        listBox3.Items.Add($"{f[j]}");
                    }
                    listBox3.Items.Add(" ");
                }
                if (NDS.cbm[i] == 4)
                {
                    if (tracks > 42) t = i / 2 + 1; else t = i + 1;
                    int q = 0;
                    if (fext.ToLower() == ".g64") q = NDG.s_len[i];
                    else (q, NDS.Track_Data[i]) = (Get_Loader_Len(NDS.Track_Data[i], 0, 80, 7000));
                    NDS.Track_Length[i] = q * 8;
                    NDG.Track_Data[i] = new byte[NDS.Track_Length[i] / 8];
                    Array.Copy(NDS.Track_Data[i], 0, NDG.Track_Data[i], 0, NDG.Track_Data[i].Length);
                    NDG.Track_Length[i] = NDG.Track_Data[i].Length;
                    NDA.Track_Length[i] = NDG.Track_Data[i].Length * 8;
                    NDA.Track_Data[i] = NDS.Track_Data[i];
                    listBox3.Items.Add($"{tr} {t} {fm} : {secF[NDS.cbm[i]]} {tr} {le} ({NDG.Track_Data[i].Length})");
                    if (NDG.Track_Data[i].Length > 7400) listBox3.Items.Add(bl);
                    listBox3.Items.Add(" ");
                }
                if (NDS.D_Start[i] == 0 && NDS.D_End[i] == 0 && NDS.Track_Length[i] == 0)
                {
                    NDS.Track_Length[i] = Get_Track_Len(NDS.Track_Data[i]);
                    if (NDS.Track_Length[i] > 32 && NDS.Track_Length[i] < 8192)
                    {
                        NDA.Track_Data[i] = new byte[8192];
                        NDG.Track_Data[i] = new byte[NDS.Track_Length[i]];
                        Array.Copy(NDS.Track_Data[i], NDG.Track_Data[i], NDS.Track_Length[i]);
                        Array.Copy(NDS.Track_Data[i], NDA.Track_Data[i], NDS.Track_Data[i].Length);
                        NDA.Track_Length[i] = NDG.Track_Length[i] = NDG.Track_Data[i].Length;
                        NDS.Track_Length[i] *= 8; NDS.D_Start[i] = 0; NDS.D_End[i] = NDS.Track_Length[i];
                    }
                    else { NDS.Track_Length[i] = 0; }
                }
                if (halftracks) ht += .5; else ht += 1;
                Color color = Color.Black;
                if (NDS.Track_Length[i] > 0 && NDS.cbm[i] != 6 && NDS.cbm[i] != 0)
                {
                    var d = Get_Density(NDS.Track_Length[i] >> 3);
                    string e = "";
                    if ((ht >= 31 && d != 3) || (ht >= 25 && ht < 31 && d != 2) || (ht >= 18 && ht < 25 && d != 1) || (ht >= 0 && ht < 18 && d != 0)) e = " [!]";
                    if (NDS.cbm[i] == 1) color = Color.Black;
                    if (NDS.cbm[i] == 2) color = Color.DarkMagenta;
                    if (NDS.cbm[i] == 3) color = Color.Green;
                    if (NDS.cbm[i] == 4) color = Color.Blue;
                    sf.Items.Add(new LineColor { Color = color, Text = $"{secF[NDS.cbm[i]]}" });
                    sl.Items.Add((NDS.Track_Length[i] >> 3).ToString("N0"));
                    ss.Items.Add(NDS.sectors[i]);
                    strack.Items.Add(ht);
                    sd.Items.Add($"{3 - d}{e}");
                }
            }
            listBox3.EndUpdate();
            if (NDS.cbm.Any(s => s == 4)) f_load.Visible = true; else f_load.Visible = false;
            if (NDS.cbm.Any(s => s == 2))
            {
                if (!Tabs.TabPages.Contains(Adv_V2_Opts)) Tabs.Controls.Add(Adv_V2_Opts);
            }
            else Tabs.Controls.Remove(Adv_V2_Opts);
            if (NDS.cbm.Any(s => s == 3))
            {
                if (!Tabs.TabPages.Contains(Adv_V3_Opts)) Tabs.Controls.Add(Adv_V3_Opts);
            }
            else Tabs.Controls.Remove(Adv_V3_Opts);
        }

        void Process_Nib_Data(bool cbm, bool short_sector, bool rb_vm)
        {
            double ht;
            bool halftracks = false;
            string[] f;
            if (tracks > 42)
            {
                halftracks = true;
                ht = 0.5;
            }
            else ht = 0;
            Color color = new Color(); // = Color.Green;
            for (int i = 0; i < tracks; i++)
            {
                if (halftracks) ht += .5; else ht += 1;
                if (NDS.Track_Length[i] > 0 && NDS.cbm[i] != 0)
                {
                    if (NDS.cbm[i] == 0) Process_Ndos(i);
                    if (NDS.cbm[i] == 1) Process_CBM(i);
                    if (NDS.cbm[i] == 2) Process_VMAX_V2(i);
                    if (NDS.cbm[i] == 3) Process_VMAX_V3(i);
                    if (NDS.cbm[i] == 4) Process_Loader(i);
                    if (NDA.Track_Length[i] > 0 && NDS.cbm[i] != 6)
                    {
                        out_size.Items.Add((NDA.Track_Length[i] / 8).ToString("N0"));
                        out_dif.Items.Add((NDA.Track_Length[i] - NDS.Track_Length[i] >> 3).ToString("+#;-#;0"));
                        string o = "";
                        var d = Get_Density(NDG.Track_Data[i].Length);
                        string e = "";
                        if ((ht >= 31 && d != 3) || (ht >= 25 && ht < 31 && d != 2) || (ht >= 18 && ht < 25 && d != 1) || (ht >= 0 && ht < 18 && d != 0)) e = " [!]";
                        if (NDG.Track_Data[i].Length > density[d])
                        {
                            if (NDG.Track_Data[i].Length > density[d] + 3) color = Color.Red;
                            if (NDG.Track_Data[i].Length > density[d] && NDG.Track_Data[i].Length < density[d] + 5) color = Color.Goldenrod;
                            o = $" + {NDG.Track_Data[i].Length - density[d]}";
                        }
                        else color = Color.Green;
                        if (NDG.Track_Data[i].Length < density[d]) o = $" - {density[d] - NDG.Track_Data[i].Length}";
                        Out_density.Items.Add(new LineColor { Color = color, Text = $"{3 - d}{e}{o}" });
                        out_track.Items.Add(ht);
                        double r = Math.Round(((double)density[Get_Density(NDA.Track_Length[i] >> 3)] / (double)(NDA.Track_Length[i] >> 3) * 300), 1);
                        if (r > 300) r = Math.Floor(r);
                        if (r == 300 && r < 301) color = Color.FromArgb(0, 30, 255);
                        if ((r >= 301 && r < 302) || (r < 300 && r >= 299)) color = Color.DarkGreen;
                        if (r > 302 || (r < 299 && r >= 297)) color = Color.Purple;
                        if (r < 297) color = Color.Brown;
                        out_rpm.Items.Add(new LineColor { Color = color, Text = $"{r:0.0}" });
                    }
                }
                else { NDA.Track_Data[i] = NDS.Track_Data[i]; }
            }

            void Process_Ndos(int trk)
            {
                NDA.Track_Data[trk] = NDS.Track_Data[trk];
                NDG.Track_Data[trk] = new byte[NDS.Track_Length[trk >> 3]];
                Array.Copy(NDS.Track_Data[trk], 0, NDG.Track_Data[trk], 0, NDS.D_End[trk] - NDS.D_Start[trk]);
                NDA.Track_Length[trk] = NDG.Track_Length[trk];
            }

            void Process_CBM(int trk)
            {
                int exp_snc = 40;   // expected sync length.  (sync will be adjusted to this value if it is >= minimum value (or) =< ignore value
                int min_snc = 16;   // minimum sync length to signal this is a sync marker that needs adjusting
                int ign_snc = 80;   // ignore sync if it is >= to value
                bool adj = false;
                var d = 0;
                if ((V2_Auto_Adj.Checked || V3_Auto_Adj.Checked || Adj_cbm.Checked)) // && (NDS.cbm.Any(s => s == 2) || NDS.cbm.Any(s => s == 3)))
                {
                    if (NDS.cbm.Any(s => s == 2) || NDS.cbm.Any(s => s == 3))
                    {
                        int tot_snc;
                        adj = true;
                        d = Get_Density(NDS.Track_Length[trk] >> 3);
                        if (NDS.Track_Length[trk] >> 3 > density[d])
                        {
                            tot_snc = NDS.Total_Sync[trk] / (NDS.sectors[trk] * 2);
                            var t_len = NDS.Track_Length[trk] - NDS.Total_Sync[trk];
                            while (t_len + (tot_snc * (NDS.sectors[trk] * 2)) > density[d] << 3)
                            {
                                tot_snc -= 1;
                                //if (tot_snc < 33) break;
                                if (tot_snc < 25) break; // <-- reduces the sector/data sync to a minimum of 24.  Very short for standard CBM tracks, but it works
                            }
                            exp_snc = tot_snc;
                        }
                    }
                }

                if (cbm)
                {
                    try
                    {
                        byte[] temp = Adjust_Sync_CBM(NDS.Track_Data[trk], exp_snc, min_snc, ign_snc, NDS.D_Start[trk], NDS.D_End[trk], NDS.Sector_Zero[trk], NDS.Track_Length[trk], trk);
                        if (adj && temp.Length > density[d])
                        {
                            // ---------- Get rid of this section if images error on CBM tracks --------------------------
                            byte[] p_gap = { 0x55, 0xaa, 0x00, 0x11, 0x22, 0x44, 0x88, 0x45, 0x12, 0x15, 0x51 };
                            int gap = 0;
                            var tb = temp.Length - density[d];
                            byte[] nt = new byte[temp.Length - tb];
                            for (int i = 1; i < 200; i++)
                            {
                                if (p_gap.Any(s => s == temp[temp.Length - i])) gap++; else gap = 0;
                                if (gap == tb + 2)
                                {
                                    Array.Copy(temp, 0, nt, 0, temp.Length - i);
                                    Array.Copy(temp, (temp.Length - i) + tb, nt, temp.Length - i, nt.Length - (temp.Length - i));
                                    temp = new byte[nt.Length];
                                    Array.Copy(nt, 0, temp, 0, temp.Length);
                                    break;
                                }
                            }
                            // -------------------------------------------------------------------------------------------
                            if (temp.Length > density[d]) temp = Shrink_Track(temp, d); // this can cause corrupted tracks if sectors contain single byte repeats
                        }
                        Set_Dest_Arrays(temp, trk);
                        (NDA.D_Start[trk], NDA.D_End[trk], NDA.Sector_Zero[trk], NDA.Track_Length[trk], f, NDA.sectors[trk], NDS.cbm_sector[trk], NDA.Total_Sync[trk]) = Find_Sector_Zero(NDA.Track_Data[trk]);
                        f[0] = "";
                    }
                    catch
                    {
                        if (!error)
                        {
                            using (Message_Center center = new Message_Center(this)) // center message box
                            {
                                string m = "This image is not compatible with this program!";
                                string t = "This is not a CBM or (known) V-Max variant";
                                MessageBox.Show(m, t, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }

            }

            void Process_VMAX_V2(int trk)
            {
                opt = true;
                V3_Auto_Adj.Checked = V3_Custom.Checked = false;
                if (rb_vm)
                {

                    byte[] temp = Adjust_V2_Sync(NDS.Track_Data[trk], NDS.D_Start[trk], NDS.D_End[trk], NDS.v2info[trk], true, trk);
                    if (NDS.v2info[trk].Length > 0 && NDS.Loader.Length == 0)
                    {
                        NDS.Loader = new byte[3];
                        NDS.Loader[0] = NDS.v2info[trk][0];
                        NDS.Loader[1] = NDS.v2info[trk][1];
                        NDS.Loader[2] = NDS.v2info[trk][3];
                    }
                    NDA.sectors[trk] = NDS.sectors[trk];
                    Set_Dest_Arrays(temp, trk);
                }
                if (V2_Auto_Adj.Checked && NDS.sectors[trk] > 12)
                {
                    if (Original.OT[trk].Length == 0)
                    {
                        Original.OT[trk] = new byte[NDG.Track_Data[trk].Length];
                        Array.Copy(NDG.Track_Data[trk], 0, Original.OT[trk], 0, NDG.Track_Data[trk].Length);
                    }
                    byte[] tdata;
                    (tdata, NDA.D_Start[trk], NDA.D_End[trk], NDA.Sector_Zero[trk]) = Rebuild_V2(NDG.Track_Data[trk], NDS.sectors[trk], NDS.v2info[trk], trk);
                    Set_Dest_Arrays(tdata, trk);
                }
                if (V2_Auto_Adj.Checked || V2_Custom.Checked || V2_Add_Sync.Checked) fnappend = mod; // "(sync_fixed)(modified)";
                else fnappend = fix;
                opt = false;
            }

            void Process_VMAX_V3(int trk)
            {
                opt = true;
                V2_Auto_Adj.Checked = V2_Custom.Checked = V2_Add_Sync.Checked = false;
                if (V3_Auto_Adj.Checked || V3_Custom.Checked) fnappend = mod;
                else fnappend = fix;
                if (rb_vm)
                {
                    if (!(short_sector && NDS.sectors[trk] < 16))
                    {
                        (NDG.Track_Data[trk], NDA.Track_Length[trk], NDA.Sector_Zero[trk]) =
                            Adjust_Vmax_V3_Sync(NDS.Track_Data[trk], NDS.D_Start[trk], NDS.D_End[trk], NDS.Sector_Zero[trk]);
                    }
                    else Shrink_Short_Sector(trk);
                }
                NDG.Track_Length[trk] = NDG.Track_Data[trk].Length;
                if (V3_Auto_Adj.Checked)
                {
                    if (Original.OT[trk].Length == 0)
                    {
                        Original.OT[trk] = new byte[NDG.Track_Data[trk].Length];
                        Array.Copy(NDG.Track_Data[trk], 0, Original.OT[trk], 0, NDG.Track_Data[trk].Length);
                    }
                    byte[] temp = Rebuild_V3(NDG.Track_Data[trk], trk);
                    Set_Dest_Arrays(temp, trk);
                }
                if (NDG.Track_Data[trk].Length > 0)
                {
                    try
                    {
                        NDA.Track_Data[trk] = new byte[8192];
                        Array.Copy(NDG.Track_Data[trk], 0, NDA.Track_Data[trk], 0, NDG.Track_Data[trk].Length);
                        Array.Copy(NDG.Track_Data[trk], 0, NDA.Track_Data[trk], NDG.Track_Data[trk].Length, 8192 - NDG.Track_Data[trk].Length);
                        Original.OT[trk] = new byte[NDG.Track_Data[trk].Length];
                        Array.Copy(NDG.Track_Data[trk], 0, Original.OT[trk], 0, NDG.Track_Data[trk].Length);
                        NDA.sectors[trk] = NDS.sectors[trk];
                    }
                    catch { }
                }
                opt = false;
            }

            void Process_Loader(int trk)
            {
                if (Original.SG.Length == 0)
                {
                    Original.SG = new byte[NDG.Track_Data[trk].Length];
                    Original.SA = new byte[NDA.Track_Data[trk].Length];
                    Array.Copy(NDG.Track_Data[trk], 0, Original.SG, 0, NDG.Track_Data[trk].Length);
                    Array.Copy(NDA.Track_Data[trk], 0, Original.SA, 0, NDA.Track_Data[trk].Length);
                }
                if (NDG.Track_Length[trk] > 7600) Shrink_Loader(trk);
                if (V2_Auto_Adj.Checked || V3_Auto_Adj.Checked) Shrink_Loader(trk);
                if (f_load.Checked) (NDG.Track_Data[trk]) = Fix_Loader(NDG.Track_Data[trk]);
                NDA.Track_Data[trk] = new byte[8192];
                if (Re_Align.Checked || V3_Auto_Adj.Checked || V2_Auto_Adj.Checked)
                {
                    if (!NDG.L_Rot) NDG.Track_Data[trk] = Rotate_Loader(NDG.Track_Data[trk]);
                    NDG.L_Rot = true;
                }

                if (NDG.Track_Data[trk].Length < 8192)
                {
                    try
                    {
                        Array.Copy(NDG.Track_Data[trk], 0, NDA.Track_Data[trk], 0, NDG.Track_Data[trk].Length);
                        Array.Copy(NDG.Track_Data[trk], 0, NDA.Track_Data[trk], NDG.Track_Data[trk].Length, 8192 - NDG.Track_Data[trk].Length);
                    }
                    catch { }
                }
            }

            void Shrink_Loader(int trk)
            {
                byte[] temp = Shrink_Track(NDG.Track_Data[trk], 1);
                Set_Dest_Arrays(temp, trk);
            }
        }

        int Get_Track_Len(byte[] data)
        {
            int p = 0;
            if (data != null)
            {
                byte[] star = new byte[16];
                Array.Copy(data, 0, star, 0, 16);
                byte[] comp = new byte[8176];
                Array.Copy(data, 16, comp, 0, 8176);

                for (p = 16; p < comp.Length; p++)
                {
                    if (comp.Skip(p).Take(star.Length).SequenceEqual(star))
                    {
                        break;
                    }
                }
            }
            return p + 16;
        }

        int Get_Data_Format(byte[] data)
        {
            int t = 0;
            int csec = 0;
            byte[] comp = new byte[4];
            if (Check_Loader(data)) return 4;
            for (int i = 0; i < data.Length - comp.Length; i++)
            {
                Array.Copy(data, i, comp, 0, comp.Length);
                t = Compare(comp);
                if (t == 3 && i + 20 < data.Length)
                {
                    for (int j = 0; j < 20; j++) if (data[i + j] == 0xee) return 3;
                    t = 0;
                }
                if (t != 0) break;
            }
            if (t == 0) t = Check_Blank(data);
            return t;

            int Compare(byte[] d)
            {
                if (Hex_Val(d) == v2) return 2;
                if ((Hex_Val(d)).Contains(v3)) return 3; // { vm3s++; if (vm3s > 1) return 3; }  //return 3;
                if (d[0] == sz[0])
                {
                    d[1] &= sz[1]; d[2] &= sz[2]; d[3] &= sz[3];
                    if (valid_cbm.Contains(Hex_Val(d))) { csec++; if (csec > 1) return 1; } // change csec > 6 if there are issues
                }
                return 0;
            }

            int Check_Blank(byte[] d)
            {
                int b = 0;
                int snc = 0;
                byte[] blank = new byte[] { 0x00, 0x11, 0x22, 0x44, 0x45, 0x14, 0x12, 0x51, 0x88 };
                for (int i = 0; i < d.Length; i++)
                {
                    if (blank.Any(s => s == d[i])) b++;
                    if (d[i] == 0xff) snc++;
                }
                if (snc > 10) return 0;
                if (b > 4000) return 6;
                else return 0;
            }

            bool Check_Loader(byte[] d)
            {
                byte[][] p = new byte[10][];
                // byte[] p contains a list of commonly repeating patters in the V-Max track 20 loader
                // the following (for) statement checks the track for these patters, if 30 matches are found, we assume its a loader track
                p[0] = new byte[] { 0xd2, 0x4b, 0xff, 0x64 };
                p[1] = new byte[] { 0x4d, 0x6d, 0x5b, 0xff };
                p[2] = new byte[] { 0x92, 0x49, 0x24, 0x92 };
                p[3] = new byte[] { 0x6b, 0xff, 0x65, 0x53 };
                p[4] = new byte[] { 0x93, 0xff, 0x69, 0x25 };
                p[5] = new byte[] { 0x33, 0x33, 0x33, 0x33 };
                p[6] = new byte[] { 0x52, 0x52, 0x52, 0x52 };
                p[7] = new byte[] { 0x5a, 0x5a, 0x5a, 0x5a };
                p[8] = new byte[] { 0x69, 0x69, 0x69, 0x69 };
                p[9] = new byte[] { 0x4b, 0x4b, 0x4b, 0x4b };
                int l = 0;
                byte[] cmp = new byte[4];
                for (int i = 0; i < d.Length - cmp.Length; i++)
                {
                    Array.Copy(d, i, cmp, 0, cmp.Length);
                    for (int j = 0; j < p.Length; j++)
                    {
                        if (Hex_Val(cmp) == Hex_Val(p[j]))
                        {
                            if (j < 7) i += 4; else i += 3;
                            l++;
                        }
                    }
                }
                if (l > 30) return true; else return false;
            }
        }
    }
}