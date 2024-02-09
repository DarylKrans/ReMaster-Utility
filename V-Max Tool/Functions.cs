using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        Thread c;  // Thread for drawing circle disk image
        Thread f;  // Thread for drawing flat tracks image
        int pan_defw;
        int pan_defh;
        //int cx, cy, fx, fy = 0;

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

        void Check_Before_Draw()
        {
            if (Adv_ctrl.SelectedTab == Adv_ctrl.TabPages["tabPage2"])
            {
                opt = true;
                this.Update();
                c?.Abort();
                f?.Abort();
                f?.Join();
                circle?.Dispose();
                flat_large?.Dispose();
                //Img_Q.Visible = label4.Visible = false;
                try
                {
                    if (Out_view.Checked)
                    {
                        f = new Thread(new ThreadStart(() => Draw_Flat_Tracks(0, false)));
                        f.Start();
                    }
                    if (Src_view.Checked)
                    {
                        f = new Thread(new ThreadStart(() => Draw_Flat_Tracks(1, false)));
                        f.Start();
                    }
                }
                catch { }
                {
                    //Img_Q.Visible = label4.Visible = true;
                    try
                    {
                        c = new Thread(new ThreadStart(() => Draw_Circular_Tracks()));
                        c.Start();
                    }
                    catch { }
                }
                GC.Collect();
                opt = false;
            }
        }

        void Init()
        {
            opt = true;
            Height = PreferredSize.Height;
            pan_defw = panPic.Width;
            pan_defh = panPic.Height;
            panPic.Controls.Add(Disk_Image);
            Out_view.Select();
            Circle_View.Select();
            Disk_Image.Image = new Bitmap(8192, 42 * 15);
            //Disk_Image.Cursor = Cursors.Hand;
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
            //Adj_cbm.Visible = Adv_ctrl.Visible = false;
            Adj_cbm.Visible = false;
            Tabs.Visible = true;
            string[] o = { "G64", "NIB", "NIB & G64" };
            fnappend = fix;
            Out_Type.DataSource = o;
            label1.Text = label2.Text = label3.Text = "";
            Source.Visible = Output.Visible = label4.Visible = Img_Q.Visible = Save_Circle_btn.Visible = false;
            button1.Enabled = false;
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
            Draw_Init_Img();
            opt = false;
        }

        void Set_ListBox_Items(bool r)
        {
            if (r)
            {
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
            }
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
            outbox.Visible = inbox.Visible = !r;
            out_track.Height = Out_density.Height = out_size.Height = out_dif.Height = ss.Height = sf.Height = out_rpm.Height = out_size.PreferredHeight;
            sl.Height = strack.Height = sl.Height = sd.Height = sl.PreferredHeight; // (items * 12);
            outbox.Height = outbox.PreferredSize.Height;
            inbox.Height = inbox.PreferredSize.Height;
            Drag_pic.Visible = r;
            T_Info.Visible = !r;
        }
    }
}