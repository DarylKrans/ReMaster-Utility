using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        private Thread circ;  // Thread for drawing circle disk image
        private Thread flat;  // Thread for drawing flat tracks image
        private Thread check_alive;
        private int pan_defw;
        private int pan_defh;
        private bool manualRender;
        private readonly Gbox outbox = new Gbox();
        private readonly Gbox inbox = new Gbox();
        private string def_bg_text;
        private bool Out_Type = true;

        void Reset_to_Defaults()
        {
            opt = true;
            Img_Q.SelectedIndex = 2;
            Set_ListBox_Items(true, true);
            Import_File.Visible = f_load.Visible = false;
            Tabs.Controls.Remove(Adv_V3_Opts);
            Tabs.Controls.Remove(Adv_V2_Opts);
            Img_style.Enabled = Img_View.Enabled = Img_opts.Enabled = Save_Circle_btn.Visible = M_render.Visible = Adv_ctrl.Enabled = false;
            VBS_info.Visible = Reg_info.Visible = false;
            Save_Disk.Visible = false;
            Adv_ctrl.SelectedIndex = 0;
            Draw_Init_Img(def_bg_text);
            opt = false;
        }

        void Out_Density_Color(object sender, DrawItemEventArgs e)
        {
            try
            {
                if (Out_density.Items[e.Index] is LineColor item)
                {
                    e.Graphics.DrawString(
                        item.Text,
                        e.Font,
                        new SolidBrush(item.Color),
                    e.Bounds);
                }
            }
            catch { }
        }

        void Source_Info_Color(object sender, DrawItemEventArgs e)
        {
            try
            {
                if (Track_Info.Items[e.Index] is LineColor item)
                {
                    e.Graphics.DrawString(
                        item.Text,
                        e.Font,
                        new SolidBrush(item.Color),
                    e.Bounds);
                }
            }
            catch { }
        }

        void Track_Format_Color(object sender, DrawItemEventArgs e)
        {
            try
            {
                if (sf.Items[e.Index] is LineColor item)
                {
                    e.Graphics.DrawString(
                        item.Text,
                        e.Font,
                        new SolidBrush(item.Color),
                    e.Bounds);
                }
            }
            catch { }
        }

        void RPM_Color(object sender, DrawItemEventArgs e)
        {
            try
            {
                if (out_rpm.Items[e.Index] is LineColor item)
                {
                    e.Graphics.DrawString(
                        item.Text,
                        e.Font,
                        new SolidBrush(item.Color),
                    e.Bounds);
                }
            }
            catch { }
        }

        byte[] Rotate_Left(byte[] data, int s)
        {
            s -= 1;
            data = data.Skip(s).Concat(data.Take(s)).ToArray();
            return data;
        }

        byte[] Rotate_Right(byte[] data, int s)
        {
            s -= 1;
            byte[] ret = data.Skip(data.Length - s).Concat(data.Take(data.Length - s)).ToArray();
            return ret;
        }

        string Hex_Val(byte[] data)
        {
            return BitConverter.ToString(data);
        }

        string Hex(byte[] data, int a, int b)
        {
            return BitConverter.ToString(data, a, b);
        }

        static String ToBinary(Byte[] data)
        {
            return string.Join("", data.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
        }

        void Pad_Bits(int position, int count, BitArray bitarray)
        {
            bool flip = !bitarray[position];
            for (int i = position; i < position + count; i++)
            {
                flip = !flip;
                bitarray[i] = flip;
            }
        }

        int Get_Density(int len)
        {
            int i = 0;
            if (len >= 7500) i = 0;
            if (len >= 6850 && len < 7500) i = 1;
            if (len >= 6400 && len < 6850) i = 2;
            if (len >= 6000 && len < 6400) i = 3;
            return i;
        }

        BitArray Flip_Bits(BitArray bits)
        {
            BitArray f = new BitArray(bits);
            for (int i = 0; i < bits.Count; i++)
            {
                f[i] = bits[7 - i];
            }
            return f;
        }

        byte[] Flip_Endian(byte[] data)
        {
            byte[] n = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                byte[] l = new byte[1];
                l[0] = data[i];
                BitArray t = Flip_Bits(new BitArray(l));
                t.CopyTo(n, i);
            }
            return n;
        }

        bool Check_Version(string find, byte[] sdat, int clen)
        {
            int i;
            byte[] comp = new byte[clen];
            for (i = 0; i < sdat.Length - find.Length; i++)
            {
                Array.Copy(sdat, i, comp, 0, comp.Length);
                if (Hex_Val(comp) == find) return (true);
            }
            return (false);
        }

        int Find_Data(string find, byte[] sdat, int clen)
        {
            int i;
            byte[] comp = new byte[clen];
            for (i = 0; i < sdat.Length - find.Length; i++)
            {
                Array.Copy(sdat, i, comp, 0, comp.Length);
                if (Hex_Val(comp) == find) break;
            }
            return i;
        }

        string Get_Disk_Directory()
        {
            string ret = "Disk Directory ID : n/a";
            var buff = new MemoryStream();
            var wrt = new BinaryWriter(buff);
            var halftrack = 0;
            int track = 0;
            if (tracks <= 42) { halftrack = 17; track = halftrack + 1; }
            if (tracks > 42) { halftrack = 34; track = (halftrack / 2) + 1; }
            if (NDS.cbm[halftrack] == 1)
            {
                byte[] next_sector = new byte[] { (byte)track, 0x00 };
                byte[] last_sector = new byte[2];
                while (Convert.ToInt32(next_sector[0]) == track)
                {
                    Array.Copy(next_sector, 0, last_sector, 0, 2);
                    byte[] temp = Decode_CBM_GCR(NDS.Track_Data[halftrack], Convert.ToInt32(next_sector[1]));
                    Array.Copy(temp, 0, next_sector, 0, next_sector.Length);
                    if (tracks <= 42) halftrack = Convert.ToInt32(next_sector[0]) - 1; else halftrack = (Convert.ToInt32(next_sector[0]) - 1) * 2;
                    wrt.Write(temp);
                    if (Hex_Val(last_sector) == Hex_Val(next_sector)) break;
                    //this.Text = Hex_Val(next_sector);
                }
                byte[] directory = buff.ToArray();
                //File.WriteAllBytes($@"c:\test.d64", buff.ToArray());
                ret = $"\"{Encoding.ASCII.GetString(directory, 144, 16).Replace('?', ' ')}\"{Encoding.ASCII.GetString(directory, 161, 6).Replace('?', ' ')}";
            }
            return ret;
        }

        void Set_Dest_Arrays(byte[] data, int trk)
        {
            NDG.Track_Data[trk] = new byte[data.Length];
            NDA.Track_Data[trk] = new byte[8192];
            Array.Copy(data, 0, NDG.Track_Data[trk], 0, data.Length);
            Array.Copy(data, 0, NDA.Track_Data[trk], 0, data.Length);
            Array.Copy(data, 0, NDA.Track_Data[trk], data.Length, 8192 - data.Length);
            NDA.Track_Length[trk] = data.Length << 3;
            NDG.Track_Length[trk] = data.Length;
        }

        void Export_File()
        {
            Save_Dialog.FileName = $"{fname}{fnappend}";
            if (Out_Type) Save_Dialog.Filter = "G64|*.g64|NIB|*.nib";
            else Save_Dialog.Filter = "G64|*.g64";
            Save_Dialog.Title = "Save File";
            if (Save_Dialog.ShowDialog() == DialogResult.OK)
            {
                string fs = Save_Dialog.FileName;
                if (Save_Dialog.FilterIndex == 1) Make_G64(fs);
                if (Save_Dialog.FilterIndex == 2) Make_NIB(fs);
                if (nib_error || g64_error)
                {
                    string s = "";
                    using (Message_Center center = new Message_Center(this)) // center message box
                    {
                        string t = "File Access Error!";
                        if (nib_error) s = $"{nib_err_msg}";
                        if (g64_error) s = $"{g64_err_msg}";
                        if (nib_error && g64_error) s = $"{nib_err_msg}\n\n{g64_err_msg}";
                        AutoClosingMessageBox.Show(s, t, 5000);
                        error = true;
                    }
                    nib_error = g64_error = false;
                }
            }
        }

        void Shrink_Short_Sector(int trk)
        {
            if (Original.OT[trk].Length == 0)
            {
                Original.OT[trk] = new byte[NDG.Track_Data[trk].Length];
                Array.Copy(NDG.Track_Data[trk], 0, Original.OT[trk], 0, NDG.Track_Data[trk].Length);
            }
            int d = Get_Density(NDG.Track_Data[trk].Length);
            byte[] temp = Shrink_Track(NDG.Track_Data[trk], d);
            if (temp.Length > density[d])
            {
                byte[] pattern = new byte[2];
                string current = "";
                int start = 0;
                int run = 0;
                int cur = 0;
                for (int i = 0; i < NDG.Track_Data[trk].Length - 1; i++)
                {
                    Array.Copy(NDG.Track_Data[trk], i, pattern, 0, pattern.Length);
                    if (Hex_Val(pattern) == current)
                    {
                        cur++;
                        if (cur > run)
                        {
                            run = cur;
                            start = (i - (run * 2));
                        }
                    }
                    else
                    {
                        cur = 0;
                        current = Hex_Val(pattern);
                    }
                    i++;
                }
                temp = new byte[density[d]];
                int skip = NDG.Track_Data[trk].Length - density[d];
                Array.Copy(NDG.Track_Data[trk], 0, temp, 0, start);
                Array.Copy(NDG.Track_Data[trk], start + skip, temp, start, (temp.Length - 1) - start);
            }
            NDG.Track_Data[trk] = new byte[temp.Length];
            Array.Copy(temp, 0, NDG.Track_Data[trk], 0, temp.Length);
            Array.Copy(temp, 0, NDA.Track_Data[trk], 0, temp.Length);
            Array.Copy(temp, 0, NDA.Track_Data[trk], temp.Length, NDA.Track_Data[trk].Length - temp.Length);
            NDG.Track_Length[trk] = NDG.Track_Data[trk].Length;
            NDA.Track_Length[trk] = NDG.Track_Length[trk] * 8;
        }

        byte[] Shrink_Track(byte[] data, int trk_density)
        {
            byte[] temp;

            if (data.Length > density[trk_density]) // - 2)
            {
                int start = 0;
                int longest = 0;
                int count = 0;
                for (int i = 1; i < data.Length; i++)
                {
                    if (data[i] != data[i - 1]) count = 0;
                    count++;
                    if (count > longest)
                    {
                        start = (i + 1) - count;
                        longest = count;
                    }
                }
                temp = new byte[density[trk_density]]; // - 2];
                int shrink = data.Length - temp.Length;
                try
                {
                    Array.Copy(data, 0, temp, 0, start);
                    Array.Copy(data, start + shrink, temp, start, temp.Length - start);
                }
                catch { return data; }
            }
            else temp = data;
            return temp;
        }

        void Check_Before_Draw(bool dontDrawFlat)
        {
            if (Adv_ctrl.SelectedTab == Adv_ctrl.TabPages["tabPage2"])
            {
                opt = true;
                this.Update();
                circ?.Abort();
                flat?.Abort();
                check_alive?.Abort();
                flat?.Join();
                try
                {
                    if (!dontDrawFlat)
                    {
                        flat_large?.Dispose();
                        flat = new Thread(new ThreadStart(() => Draw_Flat_Tracks(false)));
                        flat.Start();
                    }
                    circle?.Dispose();
                    circ = new Thread(new ThreadStart(() => Draw_Circular_Tracks()));
                    circ.Start();
                }
                catch { }

                GC.Collect();
                opt = false;
                Progress_Thread_Check();
            }
        }

        void Init()
        {
            opt = true;
            Set_Boxes();
            panel1.Controls.Add(outbox);
            panel1.Controls.Add(inbox);
            Height = PreferredSize.Height;
            pan_defw = panPic.Width;
            pan_defh = panPic.Height;
            panPic.Controls.Add(Disk_Image);
            Out_view.Select();
            Circle_View.Select();
            Disk_Image.Image = new Bitmap(8192, 42 * 15);
            panPic.AutoScroll = false;
            panPic.SetBounds(0, 0, Disk_Image.Width, Disk_Image.Height);
            Disk_Image.SizeMode = PictureBoxSizeMode.AutoSize;
            flat_large = new Bitmap(8192, panPic.Height - 16);
            Adv_ctrl.SelectedIndexChanged += new System.EventHandler(Adv_Ctrl_SelectedIndexChanged);
            Out_density.DrawItem += new DrawItemEventHandler(Out_Density_Color);
            Track_Info.DrawItem += new DrawItemEventHandler(Source_Info_Color);
            sf.DrawItem += new DrawItemEventHandler(Track_Format_Color);
            out_rpm.DrawItem += new DrawItemEventHandler(RPM_Color);
            Out_density.DrawMode = DrawMode.OwnerDrawFixed;
            Track_Info.DrawMode = DrawMode.OwnerDrawFixed;
            out_rpm.DrawMode = DrawMode.OwnerDrawFixed;
            sf.DrawMode = DrawMode.OwnerDrawFixed;
            Out_density.ItemHeight = out_rpm.ItemHeight = sf.ItemHeight = 13;
            Track_Info.ItemHeight = 15;
            Track_Info.HorizontalScrollbar = true;
            Adj_cbm.Visible = false;
            Tabs.Visible = true;
            string[] o = { "G64", "NIB", "NIB & G64" };
            fnappend = fix;
            label1.Text = label2.Text = coords.Text = "";
            Source.Visible = Output.Visible = label4.Visible = Img_Q.Visible = Save_Circle_btn.Visible = false;
            Save_Disk.Visible = false;
            AllowDrop = true;
            DragEnter += new DragEventHandler(Drag_Enter);
            DragDrop += new DragEventHandler(Drag_Drop);
            f_load.Visible = false;
            Tabs.Controls.Remove(Adv_V2_Opts);
            Tabs.Controls.Remove(Adv_V3_Opts);
            V3_hlen.Enabled = false;
            V2_hlen.Enabled = false;
            v2exp.Text = v3exp.Text = $"\u2190 Experimental";
            v2adv.Text = v3adv.Text = $"\u2193        Advanced users ONLY!        \u2193";
            vm2_ver[0] = new string[] { "A5-A5", "A4-A5", "A5-A7", "A5-A6", "A9-AD", "AC-A9", "AD-AB", "A9-AE", "A5-AD", "AC-A5", "AD-A7", "A5-AE", "A5-A9",
            "A4-A9", "A5-AB", "A5-AA", "A5-B5", "B4-A5", "A5-B7", "A5-B6", "A9-BD", "BC-A9" };
            vm2_ver[1] = new string[] { "A5-A5", "A4-A5", "A5-A7", "A5-A6", "A9-AD", "AC-A9", "A5-A3", "A9-AE", "A5-AD", "AC-A5", "A9-A3", "A5-AE", "A5-A9",
            "A4-A9", "A5-AB", "A5-AA", "A5-B5", "B4-A5", "A5-B7", "A5-B6", "A9-BD", "BC-A9" };
            Img_Q.DataSource = Img_Quality;
            Img_Q.SelectedIndex = 2;
            Width = PreferredSize.Width;
            Flat_Interp.Visible = Flat_View.Checked;
            label4.Visible = Img_Q.Visible = Circle_View.Checked;
            Circle_Render.Visible = Flat_Render.Visible = label3.Visible = false;
            Img_opts.Enabled = Img_style.Enabled = Img_View.Enabled = false;
            Import_File.Visible = false;
            for (int i = 0; i < 8000; i++) { def_bg_text += "10"; }
            Draw_Init_Img(def_bg_text);
            int cpu = 0;
            Thread perf = new Thread(new ThreadStart(() => Perf()));
            perf.Start();
            Thread.Sleep(100);
            perf.Abort();
            if (cpu < 300000000) Img_Q.SelectedIndex = 1;
            if (cpu < 200000000) Img_Q.SelectedIndex = 0;
            if (cpu < 150000000) M_render.Visible = manualRender = true;
            else M_render.Visible = manualRender = false;
            M_render.Enabled = false;
            Adv_ctrl.Enabled = false;
            VBS_info.Visible = Reg_info.Visible = false;
            opt = false;

            void Perf() { while (true) cpu++; }

            void Set_Boxes()
            {
                outbox.BackColor = Color.Gainsboro;
                panel1.Controls.Remove(this.Out_density);
                panel1.Controls.Remove(this.out_rpm);
                panel1.Controls.Remove(this.out_track);
                panel1.Controls.Remove(this.out_dif);
                panel1.Controls.Remove(this.out_size);
                outbox.Controls.Add(this.Out_density);
                outbox.Controls.Add(this.out_rpm);
                outbox.Controls.Add(this.out_track);
                outbox.Controls.Add(this.out_dif);
                outbox.Controls.Add(this.out_size);
                var w = 5;
                out_track.Location = new Point(w, 15); w += out_track.Width - 1;
                out_rpm.Location = new Point(w, 15); w += out_rpm.Width - 1;
                out_size.Location = new Point(w, 15); w += out_size.Width - 1;
                out_dif.Location = new Point(w, 15); w += out_dif.Width - 1;
                Out_density.Location = new Point(w, 15);
                outbox.FlatStyle = FlatStyle.Flat;
                outbox.ForeColor = Color.Indigo;
                outbox.Name = "outbox";
                outbox.Width = outbox.PreferredSize.Width;
                outbox.Height = outbox.PreferredSize.Height;
                outbox.Location = new Point(210, 24);
                outbox.TabIndex = 52;
                outbox.TabStop = false;
                outbox.Text = "Track/ RPM /    Size    /  Diff  / Density";
                inbox.BackColor = Color.Gainsboro;
                panel1.Controls.Remove(this.sd);
                panel1.Controls.Remove(this.strack);
                panel1.Controls.Remove(this.sf);
                panel1.Controls.Remove(this.ss);
                panel1.Controls.Remove(this.sl);
                inbox.Controls.Add(this.sd);
                inbox.Controls.Add(this.strack);
                inbox.Controls.Add(this.sf);
                inbox.Controls.Add(this.ss);
                inbox.Controls.Add(this.sl);
                w = 5;
                strack.Location = new Point(w, 15); w += strack.Width - 1;
                sl.Location = new Point(w, 15); w += sl.Width - 1;
                sf.Location = new Point(w, 15); w += sf.Width - 1;
                ss.Location = new Point(w, 15); w += ss.Width - 1;
                sd.Location = new Point(w, 15);
                inbox.FlatStyle = FlatStyle.Popup;
                inbox.ForeColor = Color.Indigo;
                inbox.Location = new Point(8, 24);
                inbox.Name = "inbox";
                inbox.Width = inbox.PreferredSize.Width;
                inbox.Height = inbox.PreferredSize.Height;
                inbox.TabIndex = 55;
                inbox.TabStop = false;
                inbox.Text = "Trk / Size / Format / Sectors / Dens";
            }
        }

        void Set_ListBox_Items(bool r, bool nofile)
        {
            strack.BeginUpdate();
            ss.BeginUpdate();
            sf.BeginUpdate();
            sl.BeginUpdate();
            sd.BeginUpdate();
            out_size.BeginUpdate();
            out_dif.BeginUpdate();
            out_rpm.BeginUpdate();
            out_track.BeginUpdate();
            Out_density.BeginUpdate();
            if (r)
            {
                Make_Visible();
                out_size.Items.Clear();
                out_dif.Items.Clear();
                ss.Items.Clear();
                sf.Items.Clear();
                sl.Items.Clear();
                sd.Items.Clear();
                strack.Items.Clear();
                out_rpm.Items.Clear();
                out_track.Items.Clear();
                Out_density.Items.Clear();

                out_track.Height = Out_density.Height = out_size.Height = out_dif.Height = ss.Height = sf.Height = out_rpm.Height = out_size.PreferredHeight;
                sl.Height = strack.Height = sl.Height = sd.Height = sl.PreferredHeight; // (items * 12);
            }
            Make_Visible();
            outbox.Visible = inbox.Visible = !r;
            out_track.Height = Out_density.Height = out_size.Height = out_dif.Height = ss.Height = sf.Height = out_rpm.Height = out_size.PreferredHeight;
            sl.Height = strack.Height = sl.Height = sd.Height = sl.PreferredHeight; // (items * 12);
            outbox.Height = outbox.PreferredSize.Height;
            inbox.Height = inbox.PreferredSize.Height;
            Drag_pic.Visible = (r && nofile);
            //T_Info.Visible = !def_bg_text;
            out_size.EndUpdate();
            out_dif.EndUpdate();
            Out_density.EndUpdate();
            ss.EndUpdate();
            sf.EndUpdate();
            out_rpm.EndUpdate();
            out_track.EndUpdate();
            strack.EndUpdate();
            sl.EndUpdate();
            sd.EndUpdate();

            void Make_Visible()
            {
                out_size.Visible = !r;
                out_dif.Visible = !r;
                ss.Visible = !r;
                sf.Visible = !r;
                sl.Visible = !r;
                sd.Visible = !r;
                strack.Visible = !r;
                out_rpm.Visible = !r;
                out_track.Visible = !r;
                Out_density.Visible = !r;
            }
        }
    }
}