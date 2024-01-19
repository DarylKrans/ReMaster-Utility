using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

/// -- Currently not being used, but may be handy later
//private readonly byte[] vvm3l = { 0xf6, 0xf4, 0xf5, 0xe9, 0xe7, 0xe6, 0xe4, 0xec, 0xed, 0xea, 0xeb, 0xd9, 0xdb, 0xd3, 0xd7, 0xe5, 0xde, 0xdc, 0xdd, 0xcd,
//    0xcb, 0xca, 0xf3, 0xf2, 0xef, 0xee, 0xf7 };
//private readonly byte vvm3l = 0xf6;
//private readonly byte vvm3s = 0xf3;
//private readonly byte[] vvm3s = { 0xf6, 0xf4, 0xf5, 0xe9, 0xe7, 0xe6, 0xe4, 0xec, 0xed, 0xea, 0xeb, 0xd9, 0xdb, 0xd3, 0xd7, 0xe5, 0xde, 0xdc, 0xdd, 0xcd, 0xf3,
//    0xf2, 0xef, 0xee, 0xf7 };
//private readonly byte[] vvm3ls = { 0xf6, 0xf3, 0xf2, 0xef, 0xa9, 0xf7 };
//private readonly byte[] vvm3ls = { 0xf3, 0xf2, 0xef, 0xa9, 0xf7, 0xf6 };
/// ------------------------------------------------------

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        private readonly string ver = " v0.8 (beta)";
        private readonly string fix = "(sync_fixed)";
        private readonly string mod = "(modified)";
        private string fname = "";
        private string fext = "";
        private string dirname = "";
        private string fnappend = "";
        private int tracks = 0;
        private byte[] nib_header = new byte[256];
        private readonly byte[] g64_header = new byte[684];
        private readonly byte[] sz = { 0x52, 0xc0, 0x0f, 0xfc };
        //private readonly int[] density = { 7692, 7142, 6666, 6250 }; // <- Actual capacity as defined by the manual
        private readonly int[] density = { 7682, 7132, 6656, 6240 }; // <- adjusted capacity to account for minor RPM variation higher than 300
        private readonly string[] supported = { ".nib", ".g64" }; // Supported file extensions list
        // vsec = the block header values & against byte[] sz
        private readonly string[] valid_cbm = { "52-40-05-28", "52-40-05-2C", "52-40-05-48", "52-40-05-4C", "52-40-05-38", "52-40-05-3C", "52-40-05-58", "52-40-05-5C",
            "52-40-05-24", "52-40-05-64", "52-40-05-68", "52-40-05-6C", "52-40-05-34", "52-40-05-74", "52-40-05-78", "52-40-05-54", "52-40-05-A8",
            "52-40-05-AC", "52-40-05-C8", "52-40-05-CC", "52-40-05-B8" };
        // vmax = the block header values of V-Max v2 sectors (non-CBM sectors)
        private string[][] vm2_ver = new string[2][];
        private readonly string[] v_check = { "A5-A3", "A9-A3", "AD-AB", "AD-A7" };
        private readonly byte[] VM2_Valid = { 0xa5, 0xa4, 0xa9, 0xaC, 0xad, 0xb4, 0xbc };
        private readonly string v2 = "A5-A5-A5-A5"; // V-MAX v2 sector 0 header (cinemaware)
        private readonly string v3 = "49-49-49"; // V-MAX v3 sector header
        private readonly string[] secF = { "NDOS", "CBM", "V-Max v2", "V-Max v3", "Loader", "tbd", "Unformatted" };
        private bool error = false;

        public static class NDS  // Global variables for Nib file source data
        {
            public static byte[][] Track_Data = new byte[0][];
            public static int[] Track_Length = new int[0];
            public static int[] Sector_Zero = new int[0];
            public static int[] D_Start = new int[0];
            public static int[] D_End = new int[0];
            public static int[] cbm = new int[0];
            public static int[] sectors = new int[0];
            public static int[] Header_Len = new int[0];
            public static int[][] cbm_sector = new int[0][];
            public static byte[][] v2info = new byte[0][];
        }

        public static class NDA  // Global variables for adjusted-sync arrays
        {
            public static byte[][] Track_Data = new byte[0][];
            public static int[] Track_Length = new int[0];
            public static int[] Sector_Zero = new int[0];
            public static int[] D_Start = new int[0];
            public static int[] D_End = new int[0];
            public static int[] sectors = new int[0];
        }

        public static class NDG  // Global variables for G64 array data
        {
            public static byte[][] Track_Data = new byte[0][];
            public static int[] Track_Length = new int[0];
        }

        public static class Original  // Global variable for retaining original loader track data
        {
            public static byte[] G = new byte[0];
            public static byte[] A = new byte[0];
            public static byte[] SG = new byte[0];
            public static byte[] SA = new byte[0];
            public static byte[][] OT = new byte[0][];
        }

        public Form1()
        {
            InitializeComponent();
            this.Text += ver;
            Init();
            Set_ListBox_Items(true);
        }

        void Set_ListBox_Items(bool r)
        {
            if (r)
            {
                out_size.Items.Clear();
                out_dif.Items.Clear();
                out_sec.Items.Clear();
                out_fmt.Items.Clear();
                out_rpm.Items.Clear();
                out_track.Items.Clear();
                strack.Items.Clear();
                ss.Items.Clear();
                sl.Items.Clear();
                sd.Items.Clear();
                out_track.Height = out_size.Height = out_dif.Height = out_sec.Height = out_fmt.Height = out_rpm.Height = out_size.PreferredHeight;
                ss.Height = strack.Height = sl.Height = sd.Height = ss.PreferredHeight; // (items * 12);
                out_size.BeginUpdate();
                out_dif.BeginUpdate();
                out_sec.BeginUpdate();
                out_fmt.BeginUpdate();
                out_rpm.BeginUpdate();
                out_track.BeginUpdate();
                strack.BeginUpdate();
                ss.BeginUpdate();
                sl.BeginUpdate();
                sd.BeginUpdate();
            }
            out_size.EndUpdate();
            out_dif.EndUpdate();
            out_sec.EndUpdate();
            out_fmt.EndUpdate();
            out_rpm.EndUpdate();
            out_track.EndUpdate();
            strack.EndUpdate();
            ss.EndUpdate();
            sl.EndUpdate();
            sd.EndUpdate();
            outbox.Visible = inbox.Visible = !r;
            out_track.Height = out_size.Height = out_dif.Height = out_sec.Height = out_fmt.Height = out_rpm.Height = out_size.PreferredHeight;
            ss.Height = strack.Height = sl.Height = sd.Height = ss.PreferredHeight; // (items * 12);
            outbox.Height = outbox.PreferredSize.Height;
            inbox.Height = inbox.PreferredSize.Height;
            Drag_pic.Visible = r;
            T_Info.Visible = !r;
        }

        void Init()
        {
            Tabs.Visible = true;
            string[] o = { "G64", "NIB", "NIB & G64" };
            fnappend = fix;
            Out_Type.DataSource = o;
            label1.Text = label2.Text = "";
            listBox3.Visible = label7.Visible = false;
            Source.Visible = Output.Visible = false;
            button1.Enabled = false;
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Drag_Enter);
            this.DragDrop += new DragEventHandler(Drag_Drop);
            listBox3.HorizontalScrollbar = true;
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
        }

        void Make_NIB()
        {
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            write.Write(nib_header);
            for (int i = 0; i < tracks; i++) write.Write(NDA.Track_Data[i]);
            File.WriteAllBytes($@"{dirname}\{fname}{fnappend}{fext}", buffer.ToArray());
            buffer.Close();
            write.Close();
        }

        void Make_G64()
        {
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            byte[] head = Encoding.ASCII.GetBytes("GCR-1541");
            byte z = 0;
            short m = Convert.ToInt16(NDG.Track_Length.Max());
            if (m < 7928) m = 7928;
            write.Write(head);
            write.Write(z);
            write.Write((byte)84);
            write.Write(m);
            int offset = 684;
            int th = 0;
            int[] td = new int[84];
            if (tracks > 42) Big(); else Small();
            for (int i = 0; i < 84; i++) write.Write(td[i]);
            for (int i = 0; i < tracks; i++)
            {
                if (NDG.Track_Length[i] > 6000)
                {
                    write.Write((short)NDG.Track_Length[i]);
                    if (NDG.Track_Data[i].Length < m) write.Write(NDG.Track_Data[i]);
                    else
                    {
                        byte[] t = new byte[m];
                        Array.Copy(NDG.Track_Data[i], 0, t, 0, m);
                        write.Write(t);
                    }
                    var o = m - NDG.Track_Length[i];
                    if (o > 0) for (int j = 0; j < o; j++) write.Write((byte)0);
                }
            }
            File.WriteAllBytes($@"{dirname}\{fname}{fnappend}.g64", buffer.ToArray());
            void Big() // 84 track nib file
            {
                for (int i = 0; i < 84; i++)
                {
                    if (i < NDG.Track_Data.Length && NDG.Track_Length[i] > 6000)
                    {
                        write.Write((int)offset + th);
                        th += 2;
                        offset += m;
                        td[i] = 3 - Get_Density(NDG.Track_Data[i].Length);
                    }
                    else write.Write((int)0);
                }
            }

            void Small() // 42 track nib file
            {
                int r = 0;
                for (int i = 0; i < 42; i++)
                {
                    if (i < NDG.Track_Data.Length && NDG.Track_Length[i] > 0)
                    {
                        write.Write((int)offset + th);
                        th += 2;
                        offset += m;
                        td[r] = 3 - Get_Density(NDG.Track_Data[i].Length);
                        r++; td[r] = 0; r++;
                    }
                    else write.Write((int)0);
                    write.Write((int)0);
                }
            }
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
            byte[] ret = new byte[data.Length];
            ret = data.Skip(data.Length - s).Concat(data.Take(data.Length - s)).ToArray();
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

            if (data.Length > density[trk_density] - 2)
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
                temp = new byte[density[trk_density] - 2];
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

        byte[] Shrink_Gap_CBM(byte[] data, int d, int last, int trk) // <-- int trk only needed for debugging
        {
            if (trk == 0) trk += 0; // <-- aboslutely useless but gets rid of a dumb in the editor message
            byte[] temp = new byte[density[d] - 2];
            int skip = data.Length - (density[d] - 2);
            byte[] comp = new byte[4];
            byte[] ignore = { 0x00, 0x11, 0x22, 0x44, 0x88, 0x55, 0xaa, 0xff };
            int cur = 0;
            int gap = 0;
            try
            {
                for (int i = last; i < data.Length; i++)
                {
                    if (ignore.Any(s => s == data[i])) cur++;
                    else cur = 0;
                    if (cur > 20)
                    {
                        gap = i - (cur - 1);
                    }
                    if (gap > 0) break;
                }
                Array.Copy(data, 0, temp, 0, gap);
                Array.Copy(data, gap + skip, temp, gap, (temp.Length - gap));
            }
            catch { return data; }
            return temp;
        }

        (int, int, int, int, string[], int, int[]) Find_Sector_Zero(byte[] data)
        {
            int sectors = 0;
            int[] s_st = new int[valid_cbm.Length];
            int pos = 0;
            int sync_count = 0;
            int data_start = 0;
            int sector_zero = 0;
            int data_end = 0;
            bool sec_zero = false;
            bool sync = false;
            bool start_found = false;
            bool end_found = false;
            data = Flip_Endian(data);
            BitArray source = new BitArray(data);
            BitArray comp = new BitArray(32);
            List<string> list = new List<string>();
            List<string> headers = new List<string>();
            //List<string> compare = new List<string>(); // for testing
            byte[] d = new byte[4];
            var h = "";
            Compare(pos);
            while (pos < source.Length - 32)
            {
                if (source[pos])
                {
                    sync_count++;
                    if (sync_count == 15) sync = true;
                }
                if (sync) Compare(pos);
                if (end_found) break;
                if (!source[pos]) { sync = false; sync_count = 0; }
                pos++;
            }
            var len = (data_end - data_start);
            list.Add($"{(len) / 8} {data_start} {data_end}");
            //this.Text = $"{data_start} {data_end} {sector_zero} {Math.Abs(len)} {start_found} {end_found}";  // for testing
            return (data_start, data_end, sector_zero, len, headers.ToArray(), sectors, s_st);

            void Compare(int p)
            {
                for (int i = 0; i < comp.Length; i++)
                {
                    comp[i] = source[pos + i];
                }
                comp.CopyTo(d, 0);
                d = Flip_Endian(d);
                if (d[0] == 0x52)
                {
                    for (int i = 1; i < sz.Length; i++) d[i] &= sz[i];
                    h = Hex_Val(d);
                    if (valid_cbm.Contains(h))
                    {
                        if (!list.Any(s => s == h))
                        {
                            string sz = "";
                            int a = Array.FindIndex(valid_cbm, se => se == h);
                            if (a == 0) sz = "*";
                            s_st[a] = pos;
                            if (!start_found) { data_start = pos; start_found = true; }
                            if (!sec_zero && h == valid_cbm[0])
                            {
                                sector_zero = pos;
                                sec_zero = true;
                            }
                            headers.Add($"Sector ({a}){sz} pos ({p / 8}) {h}");
                        }
                        else
                        {
                            if (list.Any(s => s == valid_cbm[0]))
                            {
                                headers.Add($"pos {p / 8} ** repeat ** {h}");
                                if (data_start == 0) data_end = pos;
                                else data_end = pos;
                                end_found = true;
                                headers.Add($"Track length ({(data_end - data_start) >> 3}) Sectors ({list.Count}) Sector 0 ({sector_zero >> 3})");
                                sectors = list.Count;
                                //Array.Resize(ref s_st, sectors);
                            }
                        }
                        list.Add(h);
                        string q = $"sector {Array.FindIndex(valid_cbm, s => s == h)} Position {pos}";
                        //compare.Add($"{h} {q} {start_found} {end_found} {data_start} {data_end}");  // for testing
                    }
                }
            }
        }

        (int, byte[]) Get_Loader_Len(byte[] data, int start_pos, int comp_length, int skip_length)
        {
            int q = 8192;
            int qq = 0;
            byte[] dataa = data;
            while (q == 8192 && qq < 32)
            {
                q = find();
                if (q == 8192)
                {
                    dataa = Rotate_Left(data, qq);
                    qq++;
                }
            }
            return (q, dataa);
            int find()
            {
                int p = 0;
                if (dataa != null)
                {
                    byte[] star = new byte[comp_length];
                    Array.Copy(dataa, start_pos, star, 0, comp_length);
                    byte[] comp = new byte[8192 - (comp_length + start_pos)];
                    Array.Copy(dataa, comp_length, comp, 0, 8192 - (comp_length + start_pos));

                    for (p = skip_length; p < comp.Length; p++)
                    {
                        if (comp.Skip(p).Take(star.Length).SequenceEqual(star))
                        {
                            break;
                        }
                    }
                }
                return p + comp_length;
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

        byte[] Adjust_Sync_CBM(byte[] data, int expected_sync, int minimum_sync, int exception, int Data_Start_Pos, int Data_End_Pos, int Sec_0, int Track_Len, int Track_Num)
        {
            if (exception > expected_sync && expected_sync > minimum_sync)
            {
                byte[] tempp = Flip_Endian(data);
                BitArray s = new BitArray(Track_Len);
                BitArray z = new BitArray(tempp);
                var r = Sec_0;
                for (int i = 0; i < Track_Len; i++)
                {
                    s[i] = z[r];
                    r++;
                    if (r == Data_End_Pos) r = Data_Start_Pos;
                }

                BitArray d = new BitArray(s.Length + 4096);
                if (data.Length >= 6000)
                {
                    int sync_count = 0;
                    bool sync = false;
                    int dest_pos = 0;
                    for (int i = 0; i < s.Count; i++)
                    {
                        if (s[i])
                        {
                            sync_count++;
                            d[dest_pos] = true;
                            if (sync_count == minimum_sync) sync = true;
                        }
                        if (!s[i] && sync)
                        {
                            if (sync_count < expected_sync)
                            {
                                var m = expected_sync - sync_count;
                                for (int j = 0; j < m; j++) d[dest_pos + j] = true;
                                dest_pos += m;
                            }
                            if (expected_sync < sync_count && sync_count < exception)
                            {
                                dest_pos += (expected_sync - sync_count);
                                for (int j = dest_pos; j < dest_pos + (sync_count - expected_sync); j++) d[j] = false;
                            }
                        }
                        if (!s[i])
                        {
                            sync_count = 0;
                            sync = false;
                        }
                        dest_pos++;
                        if (dest_pos == d.Length) break;
                    }

                    if (dest_pos < d.Length)
                    {
                        Pad_Bits(dest_pos, d.Length - dest_pos, d);
                    }
                    int bcnt;
                    var a = Math.Abs(((dest_pos >> 3) << 3) - dest_pos);
                    if (a != 0) bcnt = (dest_pos >> 3) + 1;
                    else bcnt = dest_pos >> 3;
                    var y = (bcnt * 8) - dest_pos;
                    if (y != 0)
                    {
                        for (int i = 0; i < ((expected_sync + 8) + 1); i++)
                        {
                            d[dest_pos + (y - i)] = d[dest_pos - (y + i)];
                        }
                        Pad_Bits(dest_pos - (y + expected_sync + 8), (8 - y) + 1, d);
                    }

                    BitArray temp = new BitArray(bcnt << 3);
                    for (int i = 0; i < (bcnt << 3); i++)
                    {
                        temp[i] = d[i];
                    }
                    if (sync)
                    {
                        var ver = 0;
                        for (int i = 0; i < expected_sync; i++) { if (temp[temp.Length - (i + 1)] == true) ver++; }
                        if (ver < sync_count && ver < expected_sync && ver >= minimum_sync)
                        {
                            for (int i = 0; i < expected_sync; i++) temp[(temp.Length - 1) - i] = true;
                        }
                    }
                    byte[] dest = new byte[bcnt];
                    temp.CopyTo(dest, 0);
                    dest = Flip_Endian(dest);
                    NDG.Track_Data[Track_Num] = dest;
                    NDG.Track_Length[Track_Num] = dest.Length;
                    var g = 8192 - dest.Length;
                    byte[] t = new byte[8192];
                    var pp = 0;
                    var dd = 0;

                    if (g > 0 && dest.Length >= 4096)
                    {
                        try
                        {
                            Array.Copy(dest, 0, t, 0, dest.Length);
                            Array.Copy(dest, 0, t, dest.Length, g);
                        }
                        catch { }
                    }
                    else
                    {
                        while (pp < t.Length)
                        {
                            t[pp] = dest[dd];
                            pp++; dd++;
                            if (dd == dest.Length) dd = 0;
                        }
                    }
                    return t;
                }
            }
            return data;
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

        /// --------------------------------  Rebuild V-Max v2 Track  ---------------------------------------


        (byte[], int, int, int) Rebuild_V2(byte[] data, int sectors, byte[] t_info, int trk)
        {
            // t_info[0] = start byte, t_info[1] = end byte, t_info[2] = header length, t_info[3] = v-max version (for sector headers)
            bool error = false;
            byte end_byte;
            int error_sec = 0;
            int trk_len = 0;
            int gap_len;
            int pos = 0;
            int Sector_len = 320;
            byte gab_byte = 0x55;
            int trk_density = density[Get_Density(data.Length)] - 2;
            byte[] start_byte = new byte[1];
            byte[] sec_sync = { 0x57, 0xff };
            //byte[] trk_sync = { 0x57, 0xff };
            byte se_byte = 0x7f;
            byte[] tail = new byte[0];
            byte[][] hdr_dat = new byte[23][];
            byte[][] sec_dat = new byte[23][];
            byte[][] header = new byte[23][];
            byte[] t_gap = new byte[0]; // { 0xff, 0x52 };
            int[] sector_pos = new int[23];
            start_byte[0] = t_info[0];
            end_byte = t_info[1];
            int g_sec = Convert.ToInt32(t_info[5]);
            int vs = Convert.ToInt32(t_info[3]);
            int gap_pos = 0;
            byte[] compare = new byte[2];
            bool gap_found = false;
            for (int i = 0; i < sectors; i++)
            {
                gap_pos = 0;
                pos = Find_Data($"{Hex_Val(start_byte)}-{vm2_ver[vs][i]}", data, 3);
                while (data[pos] != end_byte && (pos < data.Length - 1)) pos++;
                pos += 320;
                while (pos < data.Length)
                {
                    if (data[pos] == start_byte[0] && VM2_Valid.Any(s => s == data[pos + 1])) break;
                    pos++;
                    if (pos > 5 && data[pos] != 0xf7) gap_pos++;
                    if (gap_pos > 20) { gap_found = true; break; }
                }
                if (gap_found)
                {
                    if (data[pos - 20] == 0x52)
                    {
                        t_gap = new byte[8];
                        Array.Copy(data, pos - 20, t_gap, 0, t_gap.Length);
                    }
                    if (i == vm2_ver[vs].Length - 1) gap_pos = 0; else gap_pos += pos;
                    break;
                }
            }
            if (gap_pos > 0)
            {
                byte[] t = Rotate_Left(data, gap_pos);
                Array.Copy(t, 0, data, 0, t.Length);
            }
            //File.WriteAllBytes($@"c:\source{trk}.bin", data); // <- for debugging
            int slen;
            var sec = 1000;
            for (int i = 0; i < data.Length; i++)
            {
                while (data[i] != start_byte[0]) i++;
                Array.Copy(data, i + 1, compare, 0, compare.Length);
                if (vm2_ver[vs].Any(s => s == Hex_Val(compare)))
                {
                    sec = Array.IndexOf(vm2_ver[vs], Hex_Val(compare));
                }
                if (sec > 0 && sec < 1000) break;
            }
            int ssec = sec;
            for (int i = 0; i < sectors; i++)
            {
                //if (i == 0) slen = trk_sync.Length; else slen = sec_sync.Length;
                slen = sec_sync.Length;
                header[sec] = new byte[2];
                sec_dat[sec] = new byte[Sector_len];
                pos = Find_Data($"{Hex_Val(start_byte)}-{vm2_ver[vs][sec]}", data, 3);
                Array.Copy(data, pos + 1, header[sec], 0, header[sec].Length);
                while (data[pos] != end_byte && (pos + Sector_len + 2) < data.Length) pos++;
                try
                {
                    Array.Copy(data, pos + 1, sec_dat[sec], 0, Sector_len);
                }
                catch
                {
                    error = true;
                    error_sec = sec;
                }
                trk_len += (sec_dat[sec].Length + (slen + 1));
                sec++;
                if (sec == sectors) sec = 0;
            }
            int left = trk_density - trk_len - 115;
            int hlen = ((left / sectors) / 2) * 2;
            gap_len = trk_density - ((hlen * sectors) + trk_len) - t_gap.Length;
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            for (int i = 0; i < sectors; i++)
            {
                if (sec_dat[i].Length > 0)
                {
                    if (i == g_sec)
                    {
                        if (t_gap.Length > 0) write.Write(t_gap);
                        for (int j = 0; j < gap_len - 1; j++) write.Write((byte)gab_byte);
                    }
                    write.Write(sec_sync);
                    write.Write(Build_Header(header[i], hlen));
                    write.Write(sec_dat[i]);
                    write.Write((byte)se_byte);
                }
            }
            //File.WriteAllBytes($@"c:\track{trk}.bin", buffer.ToArray()); // <- for debugging
            if (error)
            {
                var tk = 0;
                if (tracks > 42) tk = (trk / 2) + 1; else tk = trk + 1;
                error = false;
                string m = $"Possible corrupt data on track {tk} sector {error_sec}";
                string t = "Error processing image!";
                MessageBox.Show(m, t, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return (buffer.ToArray(), 0, (int)buffer.Length, sectors);

            byte[] Build_Header(byte[] ID, int len)
            {
                var buff = new MemoryStream();
                var wrt = new BinaryWriter(buff);
                wrt.Write(start_byte);
                for (int i = 0; i < (len - 2) / 2; i++) wrt.Write(ID);
                wrt.Write((byte)end_byte);
                buff.Close();
                wrt.Close();
                return buff.ToArray();
            }
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
        /// -----------------------------------------------------------------------------------------------------

        (byte[], int, int, int, int, string[], int, byte[]) Get_V2_Track_Info(byte[] data, int trk)
        {
            int data_start = 0;
            int data_end = 0;
            int sec_zero = 0;
            int sectors = 0;
            bool start_found = false;
            bool end_found = false;
            bool found = false;
            byte[] start_byte = new byte[1];
            byte[] end_byte = new byte[1];
            byte[] pattern = new byte[] { 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5 };
            byte[] ignore = new byte[] { 0x7e, 0x7f, 0xff, 0x5f, 0xbf, 0x57 };
            byte[] compare = new byte[6];
            byte[] m = new byte[6];
            List<string> all_headers = new List<string>();
            List<string> headers = new List<string>();
            for (int i = 0; i < data.Length - 4; i++)
            {
                Array.Copy(data, i, compare, 0, compare.Length);
                if (Hex_Val(compare) == Hex_Val(pattern))
                {
                    start_byte[0] = data[i - 1];
                    m[0] = data[i - 1];
                    for (int j = 1; j < data.Length; j++)
                    {
                        if (data[j + i] != 0xa5)
                        {
                            end_byte[0] = data[j + i];
                            m[1] = end_byte[0];
                            found = true;
                            break;
                        }
                    }
                }
                if (found) break;
            }
            int tr;
            if (tracks > 42) tr = (trk / 2) + 1; else tr = (trk + 1);
            bool v = false;
            string ver = "";
            int vs = 0;
            for (int i = 0; i < v_check.Length; i++)
            {
                v = Check_Version($"{Hex_Val(start_byte)}-{v_check[i]}", data, 3);
                if (v)
                {
                    if (i < 2) { ver = "(older)"; vs = 1; } else { ver = "(newer)"; v = false; vs = 0; }
                    break;
                }
            }
            m[3] = (byte)vs;
            var co = 0;
            string snc = " *(Syncless)";
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == start_byte[0] && i > 0)
                {
                    if (ignore.Any(s => s == data[i - 1])) co++;
                }
                if (co > 10) { m[4] = 1; snc = ""; break; }
            }

            all_headers.Add($"track {tr} Format : {secF[NDS.cbm[trk]]} {ver}");
            byte[] comp = new byte[2];
            byte[] rep = new byte[0];
            int dif = 0;
            for (int i = 0; i < data.Length - 5; i++)
            {
                List<byte> hd = new List<byte>();
                if (data[i] == start_byte[0])
                {
                    Array.Copy(data, i + 1, comp, 0, comp.Length);
                    if (vm2_ver[vs].Any(s => s == Hex_Val(comp)))
                    {
                        var pos = i + 1;
                        while (data[pos] != end_byte[0] && data[pos] < data.Length)
                        {
                            hd.Add(data[pos]); pos++;
                        }
                        if (Hex_Val(comp) != Hex_Val(rep))
                        {
                            var a = Array.FindIndex(vm2_ver[vs], s => s == Hex_Val(comp));
                            if (pos - dif > 370)
                            {
                                m[5] = (byte)a;
                                all_headers.Add($"<------------------- (Gap) ------------------->");
                            }

                            m[2] = (byte)hd.Count;
                            string sz = "";
                            if (a == 0) { sz = "*"; sec_zero = i; }
                            all_headers.Add($"Sector ({a}){sz} pos ({i}) {Hex(start_byte, 0, 1)}-{Hex_Val(hd.ToArray())}-{Hex(end_byte, 0, 1)}");
                            if (!start_found) { data_start = i; start_found = true; }
                            sectors++;
                            dif = pos;
                        }
                        else
                        {
                            data_end = i;
                            end_found = true;
                            all_headers.Add($"pos {i} ** Repeat ** {Hex(start_byte, 0, 1)}-{Hex_Val(hd.ToArray())}-{Hex(end_byte, 0, 1)}");
                            all_headers.Add($"Track length ({data_end - data_start}){snc} Sectors ({sectors}) Sector 0 ({sec_zero}) Header length ({hd.Count + 2})");
                            all_headers.Add(" ");
                            break;
                        }
                        if (rep.Length == 0)
                        {
                            rep = new byte[2];
                            Array.Copy(comp, 0, rep, 0, comp.Length);
                        }
                        if (!start_found) data_start = i;
                    }
                    if (end_found) break;
                }
            }
            if (data_end < data_start) data_end = data.Length;
            byte[] tdata = new byte[8192];
            Array.Copy(data, data_start, tdata, 0, data_end - data_start);
            Array.Copy(data, data_start, tdata, (data_end - data_start), 8192 - (data_end - data_start));
            return (tdata, data_start, data_end, sec_zero, (data_end - data_start) << 3, all_headers.ToArray(), sectors, m);
        }

        byte[] Adjust_V2_Sync(byte[] data, int data_start, int data_end, int sectors, byte[] t_info, bool Fix_Sync, int trk)
        {
            byte[] temp_data = new byte[data_end - data_start];
            byte[] start_byte = { t_info[0] };
            byte[] end_byte = { t_info[1] };
            byte[] compare = new byte[4];
            byte[] pattern = { 0xa5, 0xa5, 0xa5 };
            byte[] ignore = new byte[] { 0x7e, 0x7f, 0xff, 0x5f, 0xbf, 0x57, 0x5b }; // possible sync markers to ignore when building track
            //byte[] sync_marker = new byte[] { 0x7f, 0xff };  // set the sync bytes (5b-ff) = 10 sync bits (7f-ff) = 15 sync bits
            byte[] sync_marker = new byte[] { 0x57, 0xff };  // set the sync bytes (5b-ff) = 10 sync bits (7f-ff) = 15 sync bits
            bool st = (t_info[4] == 0);   //false;
            int head_len = Convert.ToInt32(t_info[2]);
            int sec_zero;
            int vs = Convert.ToInt32(t_info[3]);
            Array.Copy(data, data_start, temp_data, 0, data_end - data_start);
            for (int i = 0; i < temp_data.Length - 5; i++)
            {
                Array.Copy(temp_data, i, compare, 0, compare.Length);
                if (Hex_Val(compare) == $"{Hex_Val(start_byte)}-{Hex_Val(pattern)}")
                {
                    if (i > 5)
                    {
                        sec_zero = i - 5;
                        temp_data = Rotate_Left(temp_data, i - 5);
                    }
                    else
                    {
                        temp_data = Rotate_Right(temp_data, i + 5);
                        sec_zero = i + 5;
                    }
                    break;
                }
            }
            if (V2_Auto_Adj.Checked)
            {
                int dd = 0;
                int d = 0;
                int k = temp_data.Length;
                if (k >= density[1] && k >= density[0]) { d = density[0]; dd = 0; } else { d = density[1]; dd = 1; }
                if (k >= density[2] && k < density[1]) d = density[2];
                if (k >= density[3] && k < density[2]) d = density[3];
                if (k > density[dd])
                {
                    int sdat = (sectors * 320) + ((int)t_info[2] * sectors);
                    int sync = sectors * 2;
                    if (!st && !V2_Add_Sync.Checked) sync = 2;
                    int rem = (temp_data.Length) - sdat - (sync);
                    int sub = Math.Abs(d - (rem + sdat + sync));
                    int hdd = (((sub / sectors)) / 2) * 2;
                    //if (hdd > 10) hdd = 0;
                    if (hdd > head_len) head_len += hdd; else head_len -= hdd;
                }
                if (!st && V2_Add_Sync.Checked) head_len -= 2;
            }
            if (Fix_Sync) // <- if the "Fix_Sync" bool is true, otherwise just return track info without any adjustments
            {
                // ---------------------- Build new track with adjusted sync markers -------------------------------------- //
                var buffer = new MemoryStream();
                var write = new BinaryWriter(buffer);
                var s_pos = 0;
                // Set the length of the sector header in multiples of 2 including the start and end marker.  Minimum = 6
                var sector_header = Convert.ToInt32((V2_hlen.Value - 2) * 2) / 2;
                if (V2_Auto_Adj.Checked) sector_header = head_len;
                byte[] sec_header = new byte[0];
                byte[] secz = { 0xa5, 0xa5 };
                bool no_sync = false;
                compare = new byte[2];
                // begin processing the track
                while (s_pos < temp_data.Length)
                {
                    try
                    {
                        if (s_pos + 2 < temp_data.Length && temp_data[s_pos] == start_byte[0] && VM2_Valid.Any(s => s == temp_data[s_pos + 1]))  // s_pos + 2 
                        {
                            var m = 0;
                            byte[] header_ID = new byte[2];
                            if (s_pos + 3 < temp_data.Length - 1) Array.Copy(temp_data, s_pos + 2, header_ID, 0, 2); // s_pos + 4, s_pos + 3
                            while (temp_data[s_pos] != start_byte[0]) m++; // s_pos++;
                            s_pos += m + 1; // sets source position 1 byte after the header start byte to get the header pattern data
                            Array.Copy(temp_data, s_pos, compare, 0, compare.Length);

                            if (vm2_ver[vs].Any(s => s == Hex_Val(compare))) // <- checks to verify header pattern is in the list of valid headers
                            {
                                // check that it's not sector 0 which needs sync, then check if a sync marker is before the header start byte.  If not, its a syncless track
                                if (!V2_Add_Sync.Checked)
                                {
                                    //if (Hex_Val(compare) != "A5-A5" && (!sync_marker.Any(s => s == temp_data[s_pos - 2])) && temp_data[s_pos - 1] == start_byte[0])
                                    if (compare != secz && (!sync_marker.Any(s => s == temp_data[s_pos - 2])) && temp_data[s_pos - 1] == start_byte[0]) no_sync = true;
                                    else no_sync = false;
                                }
                                var header_length = 0;
                                while (s_pos < temp_data.Length && temp_data[s_pos] != end_byte[0]) // <- getting the length of the header pattern
                                {
                                    s_pos++; header_length++;
                                }
                                s_pos++;
                                if (V2_Custom.Checked || V2_Auto_Adj.Checked) header_length = sector_header;
                                if (!no_sync) write.Write(sync_marker); // <- Here's where we add the sync (unless its a syncless track)
                                write.Write(Build_Header(start_byte, end_byte, compare, ((header_length) / 2) * 2)); // building new header and writing to buffer
                            }
                        }
                    }
                    catch { }
                    if (s_pos < temp_data.Length && !ignore.Any(s => s == temp_data[s_pos])) write.Write(temp_data[s_pos]); // <- loop writes sector data to the buffer until it hits another header
                    s_pos++;
                }
                sec_zero = Find_Data($"{Hex_Val(start_byte)}-{vm2_ver[vs][0]}", data, 3);
                return buffer.ToArray(); // <- Return new array with sync markers adjusted

                byte[] Build_Header(byte[] s, byte[] e, byte[] f, int len)
                {
                    var buff = new MemoryStream();
                    var wrt = new BinaryWriter(buff);
                    wrt.Write((byte)s[0]);
                    for (int i = 1; i < (len / 2); i++) wrt.Write(f);
                    wrt.Write((byte)e[0]);
                    return buff.ToArray();
                }
            }
            else return temp_data; // <- Return array without any adjustments to sync
        }

        /// ------------------------------------------------------------------------------------------------------------------------

        (string[], int, int, int, int, int, int) Get_vmv3_track_length(byte[] data, int trk)
        {
            int data_start = 0;
            int data_end = 0;
            int sector_zero = 0;
            int header_total = 0;
            int header_avg = 0;
            bool start_found = false;
            bool end_found = false;
            bool s_zero = false;
            byte[] sec_0_ID = { 0xf6, 0xf3 }; // V-Max v3 sector 0 ID marker (f6 found on tracks > 15 sectors, f3 found on short tracks < 15 sectors)
            byte[] header = new byte[] { 0x49, 0x49 }; // V-Max v3 header pattern
            byte head_end = 0xee; // V-Max v3 common sector ID start byte located directly following the 49-49-49 pattern
            byte[] comp = new byte[2];
            byte[] head = new byte[18];
            List<string> s = new List<string>();
            List<string> ss = new List<string>();
            List<int> spos = new List<int>();
            List<byte> hb = new List<byte>();
            List<int> hl = new List<int>();
            for (int i = 0; i < data.Length - comp.Length; i++)
            {
                Array.Copy(data, i, comp, 0, comp.Length);
                if (Hex_Val(comp) == Hex_Val(header))
                {
                    var a = 0;
                    while (data[i + a] == header[0]) a++;
                    i += a;
                    if (data[i] == head_end)
                    {
                        if (i + head.Length < data.Length) Array.Copy(data, i, head, 0, head.Length);
                        if (!ss.Any(b => b == Hex_Val(head)))
                        {
                            if (head[2] == sec_0_ID[0]) { sector_zero = i - a; s_zero = true; }
                            if (!start_found) { data_start = i - a; start_found = true; }
                            ss.Add(Hex_Val(head));
                            hb.Add(head[2]);
                            spos.Add(i - a);
                            hl.Add(a);
                            header_total += a;
                        }
                        else
                        {
                            end_found = true;
                            data_end = i - a;
                            build_list();
                            s.Add($"Pos {i - a} **repeat** {Hex_Val(head).Remove(8, Hex_Val(head).Length - 8)}");
                            string stats = $"Track Length ({data_end - data_start}) Sectors ({ss.Count})";
                            if (!s_zero)
                            {
                                if (hb.Count < 10 && !s_zero)
                                {
                                    int p = Array.FindIndex(hb.ToArray(), se => se == sec_0_ID[1]);
                                    if (p != -1)
                                    {
                                        s_zero = true;
                                        sector_zero = spos[p];
                                    }
                                }
                            }
                            stats += $" sector 0 ({sector_zero})  Header Length ({a + 3})";
                            s.Add(stats);
                        }
                    }
                }
                if (end_found) break;
            }
            try
            {
                if (end_found && hb.Count < 10) sector_zero = spos[Array.FindIndex(hb.ToArray(), se => se == sec_0_ID[1])];
            }
            catch
            {
                string m = "Output image may not work!";
                string t = $"Error processing track {(trk / 2) + 1}";
                MessageBox.Show(m, t, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            header_avg = header_total / ss.Count;

            void build_list()
            {
                byte vvm3l = 0xf6;
                int h;
                if (hb.Count > 10) vvm3l = 0xf6; else vvm3l = sec_0_ID[1];
                int p = Array.FindIndex(hb.ToArray(), se => se == vvm3l);
                for (int j = 0; j < ss.Count; j++)
                {
                    string hdr = "";
                    string sz = "";
                    if (j - p >= 0) h = j - p; else h = (hb.Count - p) + j;
                    if (h == 0) sz = "*";
                    for (int u = 0; u < hl[j]; u++) hdr += "49-";
                    s.Add($"Sector ({h}){sz} Pos ({spos[j]}) {hdr}{ss[j].Remove(8, ss[j].Length - 8)}");
                }
                if (!end_found) s.Add($"Track Length [est] (7400) Sectors ({hb.Count})");
            }
            if (ss.Count < 10)
            {
                int de = density[Get_Density(data_end - data_start)];
                if ((tracks > 42 && trk == 36) || (tracks <= 42 && trk == 19)) de = density[1];
                if (start_found && !end_found)
                {
                    if (data_start > 3000) data_start = 0;
                    data_end = de;
                }
                if (start_found && end_found && (data_end - data_start) < 7000)
                {
                    var a = de - (data_end - data_start);
                    if (data_end + a < 8192) data_end += a;
                }
                if (!end_found)
                {
                    build_list();
                }
            }
            //
            //
            return (s.ToArray(), data_start, data_end, sector_zero, (data_end - data_start), ss.Count, header_avg);
        }

        (byte[], int, int) Adjust_Vmax_V3_Sync(byte[] data, int sds, int sde, int ssz, int sectors, int hlen)
        {
            int d = 0;
            byte[] bdata = new byte[sde - sds];
            Array.Copy(data, sds, bdata, 0, sde - sds);
            byte[] tdata = Rotate_Left(bdata, ((ssz >> 3) - (sds >> 3)));
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            int spos = 0;
            byte[] sync = new byte[] { 0x7f, 0xff };
            byte[] hd = new byte[] { 0x49, 0x49 };
            byte[] comp = new byte[2];
            int cust = (int)V3_hlen.Value;
            int tot_head = sectors * hlen;
            int ched = hlen;
            int tsize = (bdata.Length - tot_head);
            if (V3_Auto_Adj.Checked)
            {
                var se = 0;
                for (int i = 0; i < bdata.Length - 2; i++)
                {
                    Array.Copy(bdata, i, comp, 0, comp.Length);
                    if (Hex_Val(comp) == Hex_Val(sync)) se++;
                }
                int sws = (sectors - se) * 2; // - (se * 2);
                cust = hlen;
                d = Get_Density(bdata.Length);
                if (bdata.Length + (sws) > density[d])
                {
                    var a = 0;
                    while ((bdata.Length + sws) - (sectors * a) > density[d] - 2) a++;
                    if (ched - a < 3) cust = 3; else cust = ched - a;
                }
            }

            while (spos < tdata.Length)
            {
                if (spos + 2 < tdata.Length && tdata[spos + 2] == hd[0])
                {
                    try
                    {
                        Array.Copy(tdata, spos + 2, comp, 0, comp.Length);
                        if (Hex_Val(comp) == Hex_Val(hd))
                        {
                            var a = 0;
                            while (tdata[spos + a] != hd[0])
                            {
                                if (!sync.Any(s => s == tdata[spos + a])) write.Write(tdata[spos + a]);
                                a++;
                            }
                            var b = 0;
                            while (spos + (a + b) < tdata.Length && tdata[spos + (a + b)] == hd[0]) b++;
                            spos += (a + b);
                            if (b < 15 && (V3_Custom.Checked || V3_Auto_Adj.Checked)) b = cust;
                            write.Write(sync);
                            for (int i = 0; i < b; i++) write.Write((byte)hd[0]);
                        }
                    }
                    catch { }
                }
                if (spos < tdata.Length) write.Write(tdata[spos]);
                spos++;
            }
            if (V3_Auto_Adj.Checked && buffer.Length > density[d] - 2)
            {
                byte[] too_long = buffer.ToArray();
                byte[] just_right = new byte[density[d] - 2];
                int gap_count = 0;
                int gap_start = 0;
                int skip = too_long.Length - just_right.Length;
                byte[] gap = { 0x00, 0x11, 0x22, 0x44, 0x88, 0x55, 0xaa };
                for (int i = 0; i < too_long.Length; i++)
                {
                    if (gap.Any(s => s == too_long[i])) gap_count++; else gap_count = 0;
                    if (gap_count > 10) { gap_start = i - (gap_count - 1); break; }
                }
                Array.Copy(too_long, 0, just_right, 0, gap_start);
                Array.Copy(too_long, gap_start + skip, just_right, gap_start, just_right.Length - gap_start);
                return (just_right, just_right.Length << 3, 0);
            }

            return (buffer.ToArray(), (int)buffer.Length << 3, 0);
        }

        byte[] Fix_Loader(byte[] data)
        {
            byte[] tdata = data;
            byte[] v2 = new byte[] { 0x5b, 0x57, 0x52, 0x4d }; // Cinemaware and some other v2 variants
            byte[] v3 = new byte[] { 0xaa, 0xaf, 0xda, 0x5f }; // V3 Taito (arkanoid)
            byte[] v1 = new byte[] { 0xaa, 0xbf, 0xb4, 0xbf }; // v3 Taito (bubble bobble)
            byte[] v4 = new byte[] { 0x6b, 0xd9, 0xb6, 0xdd }; // Sega
            byte[] comp = new byte[4];
            bool f = false;
            for (int i = 0; i < tdata.Length - 4; i++)
            {
                Array.Copy(tdata, i, comp, 0, comp.Length);
                if (Hex_Val(comp) == Hex_Val(v1)) { Patch_V3(i - 4); f = true; }
                if (Hex_Val(comp) == Hex_Val(v2)) { Patch_V2(i - 3); f = true; }
                if (Hex_Val(comp) == Hex_Val(v3)) { Patch_V3(i - 4); f = true; }
                if (Hex_Val(comp) == Hex_Val(v4)) { Patch_V2(i - 3); f = true; }
                if (f) break;
            }
            if (f) f_load.Text = "Fix Loader Track (Sync added to header)";
            return tdata;

            void Patch_V2(int pos)
            {
                if (pos > 0)
                {
                    tdata[pos] = 0xde;
                    tdata[pos + 1] = 0xff;
                    tdata[pos + 2] = 0xff;
                }
            }

            void Patch_V3(int pos)
            {
                if (pos > 0)
                {
                    tdata[pos] = 0x5f;
                    tdata[pos + 1] = 0xff;
                    tdata[pos + 2] = 0xff;
                }
            }
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

        void V3_Auto_Adjust()
        {
            bool p = true;
            if (V3_Auto_Adj.Checked || Adj_cbm.Checked)
            {
                for (int t = 0; t < tracks; t++)
                {
                    if (NDG.Track_Data[t] != null)
                    {
                        if (NDS.cbm[t] == 1 || NDS.cbm[t] == 3)
                        {
                            if (Original.OT[t].Length == 0)
                            {
                                Original.OT[t] = new byte[NDG.Track_Data[t].Length];
                                Array.Copy(NDG.Track_Data[t], 0, Original.OT[t], 0, NDG.Track_Data[t].Length);
                            }
                        }
                        if (NDS.cbm[t] == 4 || (NDS.cbm[t] == 3 && NDS.sectors[t] < 16)) Shrink_Short_Sector(t);
                        if (NDS.cbm[t] == 1)
                        {
                            int d = Get_Density(NDG.Track_Data[t].Length);
                            if (!(NDG.Track_Data[t].Length > (density[d] + 100)) && !(NDG.Track_Data[t].Length < (density[d] - 90)))
                            {
                                byte[] temp = Shrink_Gap_CBM(NDG.Track_Data[t], d, NDS.cbm_sector[t][NDS.cbm_sector[t].Length - 1] >> 3, t);
                                Set_Dest_Arrays(temp, t);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int t = 0; t < tracks; t++)
                {
                    if (NDG.Track_Data[t] != null)
                    {
                        if (NDS.cbm[t] == 4)
                        {
                            NDG.Track_Data[t] = new byte[Original.SG.Length];
                            NDA.Track_Data[t] = new byte[Original.SA.Length];
                            Array.Copy(Original.SG, 0, NDG.Track_Data[t], 0, Original.SG.Length);
                            Array.Copy(Original.SA, 0, NDA.Track_Data[t], 0, Original.SA.Length);
                            NDG.Track_Length[t] = NDG.Track_Data[t].Length;
                            NDA.Track_Length[t] = NDG.Track_Length[t] * 8;
                        }
                        if (NDS.cbm[t] == 1 || (NDS.cbm[t] == 3 && NDS.sectors[t] < 16))
                        {
                            if (Original.OT[t].Length != 0)
                            {
                                NDG.Track_Data[t] = new byte[Original.OT[t].Length];
                                Array.Copy(Original.OT[t], 0, NDG.Track_Data[t], 0, Original.OT[t].Length);
                                Array.Copy(Original.OT[t], 0, NDA.Track_Data[t], 0, Original.OT[t].Length);
                                Array.Copy(Original.OT[t], 0, NDA.Track_Data[t], Original.OT[t].Length, NDA.Track_Data[t].Length - Original.OT[t].Length);
                                p = false;
                            }
                            NDG.Track_Length[t] = NDG.Track_Data[t].Length;
                            NDA.Track_Length[t] = NDG.Track_Length[t] * 8;
                        }
                    }
                }
            }

            out_track.Items.Clear();
            out_size.Items.Clear();
            out_dif.Items.Clear();
            out_sec.Items.Clear();
            out_fmt.Items.Clear();
            out_rpm.Items.Clear();
            if (Adj_cbm.Checked && !V3_Auto_Adj.Checked) p = false;
            Process_Nib_Data(false, p); // false flag instructs the routine NOT to process CBM tracks again -- p (true/false) process v-max v3 short tracks
        }
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
                    (NDS.D_Start[i], NDS.D_End[i], NDS.Sector_Zero[i], NDS.Track_Length[i], f, NDS.sectors[i], NDS.cbm_sector[i]) = Find_Sector_Zero(NDS.Track_Data[i]);
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
                    int q;
                    (q, NDS.Track_Data[i]) = (Get_Loader_Len(NDS.Track_Data[i], 0, 80, 7000));
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
                if (NDS.Track_Length[i] > 0 && NDS.cbm[i] != 6)
                {
                    sl.Items.Add((NDS.Track_Length[i] >> 3).ToString("N0"));
                    ss.Items.Add((NDS.Sector_Zero[i] >> 3).ToString("N0"));
                    strack.Items.Add(ht);
                    sd.Items.Add(3 - Get_Density(NDS.Track_Length[i] >> 3));
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

        void Process_Nib_Data(bool cbm, bool short_sector)
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
                        out_sec.Items.Add(NDA.sectors[i]);
                        out_fmt.Items.Add(secF[NDS.cbm[i]]);
                        out_track.Items.Add(ht);
                        double r = Math.Round(((double)density[Get_Density(NDA.Track_Length[i] >> 3)] / (double)(NDA.Track_Length[i] >> 3) * 300), 1);
                        if (r > 300) r = Math.Floor(r);
                        out_rpm.Items.Add($"{r:0.0}");
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
                if (cbm)
                {
                    try
                    {
                        NDA.Track_Data[trk] = Adjust_Sync_CBM(NDS.Track_Data[trk], 40, 15, 55, NDS.D_Start[trk], NDS.D_End[trk], NDS.Sector_Zero[trk], NDS.Track_Length[trk], trk);
                        (NDA.D_Start[trk], NDS.D_End[trk], NDA.Sector_Zero[trk], NDA.Track_Length[trk], f, NDA.sectors[trk], NDS.cbm_sector[trk]) = Find_Sector_Zero(NDA.Track_Data[trk]);
                        f[0] = "";
                    }
                    catch
                    {
                        if (!error)
                        {
                            string m = "This image is not compatible with this program!";
                            string t = "This is not a CBM or (known) V-Max variant";
                            MessageBox.Show(m, t, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            error = true;
                        }
                    }
                }
                if (V3_Auto_Adj.Checked || V2_Auto_Adj.Checked)
                {
                    if (Original.OT[trk].Length == 0)
                    {
                        Original.OT[trk] = new byte[NDG.Track_Data[trk].Length];
                        Array.Copy(NDG.Track_Data[trk], 0, Original.OT[trk], 0, NDG.Track_Data[trk].Length);
                    }
                    int d = Get_Density(NDG.Track_Data[trk].Length);
                    if (NDG.Track_Data[trk].Length > density[d] - 2)
                    {
                        byte[] temp = Shrink_Gap_CBM(NDG.Track_Data[trk], d, NDS.cbm_sector[trk][NDS.cbm_sector[trk].Length - 1] >> 3, trk);
                        Set_Dest_Arrays(temp, trk);
                    }
                }
            }
            void Process_VMAX_V2(int trk)
            {
                V3_Auto_Adj.Checked = V3_Custom.Checked = false;
                byte[] temp = Adjust_V2_Sync(NDS.Track_Data[trk], NDS.D_Start[trk], NDS.D_End[trk], NDS.sectors[trk], NDS.v2info[trk], true, trk);
                NDA.sectors[trk] = NDS.sectors[trk];
                Set_Dest_Arrays(temp, trk);
                if (V2_rb.Checked && NDS.sectors[trk] > 12)
                {
                    byte[] tdata;
                    (tdata, NDA.D_Start[trk], NDA.D_End[trk], NDA.Sector_Zero[trk]) = Rebuild_V2(NDG.Track_Data[trk], NDS.sectors[trk], NDS.v2info[trk], trk);
                    Set_Dest_Arrays(tdata, trk);
                }
                if (V2_Auto_Adj.Checked || V2_Custom.Checked || V2_Add_Sync.Checked || V2_rb.Checked) fnappend = mod; // "(sync_fixed)(modified)";
                else fnappend = fix;
            }
            void Process_VMAX_V3(int trk)
            {
                V2_Auto_Adj.Checked = V2_Custom.Checked = V2_Add_Sync.Checked = false;
                if (V3_Auto_Adj.Checked || V3_Custom.Checked) fnappend = mod;
                else fnappend = fix;
                if (!(short_sector && NDS.sectors[trk] < 16))
                {
                    (NDG.Track_Data[trk], NDA.Track_Length[trk], NDA.Sector_Zero[trk]) =
                        Adjust_Vmax_V3_Sync(NDS.Track_Data[trk], NDS.D_Start[trk], NDS.D_End[trk], NDS.Sector_Zero[trk], NDS.sectors[trk], NDS.Header_Len[trk]);
                }
                else Shrink_Short_Sector(trk);
                NDG.Track_Length[trk] = NDG.Track_Data[trk].Length;
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
            }

            void Shrink_Loader(int trk)
            {
                byte[] temp = Shrink_Track(NDG.Track_Data[trk], 1);
                Set_Dest_Arrays(temp, trk);
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
        }

        private void Drag_Enter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Drag_Drop(object sender, DragEventArgs e)
        {
            bool adv = false;
            if (V3_Auto_Adj.Checked)
            {
                adv = true;
                V3_Auto_Adj.Checked = false;
            }
            Source.Visible = Output.Visible = false;
            f_load.Text = "Fix Loader Track";
            button1.Enabled = false;
            sl.DataSource = null;
            out_size.DataSource = null;
            string[] File_List = (string[])e.Data.GetData(DataFormats.FileDrop);
            string l = "Not ok";
            if (File.Exists(File_List[0]))
            {
                dirname = Path.GetDirectoryName(File_List[0]);
                fname = Path.GetFileNameWithoutExtension(File_List[0]);
                fext = Path.GetExtension(File_List[0]);
            }
            FileStream Stream = new FileStream(File_List[0], FileMode.Open, FileAccess.Read);
            if (fext.ToLower() == supported[0])
            {
                long length = new System.IO.FileInfo(File_List[0]).Length;
                tracks = (int)(length - 256) / 8192;
                if ((tracks * 8192) + 256 == length) l = "File Size OK!";
                listBox3.Items.Clear();
                Set_ListBox_Items(true);
                nib_header = new byte[256];
                Stream.Seek(0, SeekOrigin.Begin);
                Stream.Read(nib_header, 0, 256);
                Set_Arrays(tracks);
                for (int i = 0; i < tracks; i++)
                {
                    NDS.Track_Data[i] = new byte[8192];
                    Stream.Seek(256 + (8192 * i), SeekOrigin.Begin);
                    Stream.Read(NDS.Track_Data[i], 0, 8192);
                    Original.OT[i] = new byte[0];
                }
                Stream.Close();
                var head = Encoding.ASCII.GetString(nib_header, 0, 13);
                var hm = "Bad Header";
                if (head == "MNIB-1541-RAW") hm = "Header Match!";
                var lab = $"Total Track ({tracks}), {l}, {hm}";
                Process(true, lab);
            }
            if (fext.ToLower() == supported[1])
            {
                listBox3.Items.Clear();
                Set_ListBox_Items(true);
                Stream.Seek(0, SeekOrigin.Begin);
                Stream.Read(g64_header, 0, 684);
                var head = Encoding.ASCII.GetString(g64_header, 0, 8);
                tracks = Convert.ToInt32(g64_header[9]);
                Set_Arrays(tracks);
                int tr_size = BitConverter.ToInt16(g64_header, 10);
                var hm = "Bad Header";
                if (head == "GCR-1541")
                {
                    hm = "Header Match!";
                    byte[] temp = new byte[2];
                    for (int i = 0; i < tracks; i++)
                    {
                        Original.OT[i] = new byte[0];
                        int pos = BitConverter.ToInt32(g64_header, 12 + (i * 4));
                        if (pos != 0)
                        {
                            Stream.Seek(pos, SeekOrigin.Begin);
                            Stream.Read(temp, 0, 2);
                            short ts = BitConverter.ToInt16(temp, 0);
                            NDS.Track_Data[i] = new byte[8192];
                            byte[] tdata = new byte[ts];
                            Stream.Seek(pos + 2, SeekOrigin.Begin);
                            Stream.Read(tdata, 0, ts);
                            Array.Copy(tdata, 0, NDS.Track_Data[i], 0, ts);
                            Array.Copy(tdata, 0, NDS.Track_Data[i], ts, 8192 - ts);
                        }
                        else
                        {
                            NDS.Track_Data[i] = new byte[8192];
                            for (int j = 0; j < NDS.Track_Data[i].Length; j++)
                            {
                                NDS.Track_Data[i][j] = 0;
                            }
                        }
                    }
                    Stream.Close();
                    var lab = $"Total Track ({tracks}), G64 Track Size ({tr_size:N0})";
                    Out_Type.SelectedIndex = 0;
                    Process(false, lab);
                }
                else
                {
                    label1.Text = $"{hm}"; //Invalid Header!";
                    label2.Text = "";
                }
            }
            if (!supported.Any(s => s == fext.ToLower()))
            {
                Set_ListBox_Items(true);
                label1.Text = "File not Valid!";
                label2.Text = string.Empty;
            }
            if (adv)
            {
                adv = false;
                if (NDS.cbm.Any(s => s == 3))
                {
                    V3_Auto_Adj.Checked = true;
                    out_track.Items.Clear();
                    out_size.Items.Clear();
                    out_dif.Items.Clear();
                    out_sec.Items.Clear();
                    out_fmt.Items.Clear();
                    out_rpm.Items.Clear();
                    Process_Nib_Data(false, true);
                }
            }
            if (error)
            {
                Set_ListBox_Items(true);
                label1.Text = "File not Valid!";
                label2.Text = string.Empty;
                error = false;
            }

            void Process(bool get, string l2)
            {
                Parse_Nib_Data();
                if (!error)
                {
                    Process_Nib_Data(true, false);
                    Set_ListBox_Items(false);
                    Out_Type.Enabled = get;
                    button1.Enabled = true;
                    Source.Visible = Output.Visible = true;
                    label1.Text = $"{fname}{fext}";
                    label2.Text = l2;
                    label1.Update();
                    label2.Update();
                }
            }

            void Set_Arrays(int len)
            {
                // NDS is the input or source array
                NDS.Track_Data = new byte[len][];
                NDS.Sector_Zero = new int[len];
                NDS.Track_Length = new int[len];
                NDS.D_Start = new int[len];
                NDS.D_End = new int[len];
                NDS.Sector_Zero = new int[len];
                NDS.cbm = new int[len];
                NDS.sectors = new int[len];
                NDS.Header_Len = new int[len];
                NDS.cbm_sector = new int[len][];
                NDS.v2info = new byte[len][];
                // NDA is the destination or output array
                NDA.Track_Data = new byte[len][];
                NDA.Sector_Zero = new int[len];
                NDA.Track_Length = new int[len];
                NDA.D_Start = new int[len];
                NDA.D_End = new int[len];
                NDA.Sector_Zero = new int[len];
                NDA.sectors = new int[len];
                // NDG is the G64 arrays
                NDG.Track_Length = new int[len];
                NDG.Track_Data = new byte[len][];
                // Original is the arrays that keep the original track data for the Auto Adjust feature
                Original.A = new byte[0];
                Original.G = new byte[0];
                Original.SA = new byte[0];
                Original.SG = new byte[0];
                Original.OT = new byte[len][];
                //vm2h = new string[tracks][];
            }
        }

        private void Make(object sender, EventArgs e)
        {
            switch (Out_Type.SelectedIndex)
            {
                case 0: Make_G64(); break;
                case 1: Make_NIB(); break;
                case 2: { Make_G64(); Make_NIB(); } break;
            }
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            listBox3.Visible = label7.Visible = !listBox3.Visible;
            listBox3.Width = listBox3.PreferredSize.Width;
            Width = PreferredSize.Width;
        }

        private void F_load_CheckedChanged(object sender, EventArgs e)
        {
            int i = 100;
            if (f_load.Checked)
            {
                if (tracks > 0 && NDS.Track_Data.Length > 0)
                {
                    i = Array.FindIndex(NDS.cbm, s => s == 4);
                    if (i < 100 && i > -1)
                    {
                        Original.G = new byte[NDG.Track_Data[i].Length];
                        Original.A = new byte[NDA.Track_Data[i].Length];
                        Array.Copy(NDG.Track_Data[i], 0, Original.G, 0, NDG.Track_Data[i].Length);
                        Array.Copy(NDA.Track_Data[i], 0, Original.A, 0, NDA.Track_Data[i].Length);
                        NDG.Track_Data[i] = Fix_Loader(NDG.Track_Data[i]);
                        NDA.Track_Data[i] = new byte[8192];
                        if (NDG.Track_Data[i].Length < 8192)
                        {
                            try
                            {
                                Array.Copy(NDG.Track_Data[i], 0, NDA.Track_Data[i], 0, NDG.Track_Data[i].Length);
                                Array.Copy(NDG.Track_Data[i], 0, NDA.Track_Data[i], NDG.Track_Data[i].Length, 8192 - NDG.Track_Data[i].Length);
                            }
                            catch { }
                        }
                    }
                }
            }
            if (!f_load.Checked)
            {
                f_load.Text = "Fix Loader Track";
                if (tracks > 0) i = Array.FindIndex(NDS.cbm, s => s == 4);
                if (i > -1 && i < 100)
                {
                    if (Original.A.Length > 0) { NDA.Track_Data[i] = Original.A; }
                    if (Original.G.Length > 0) { NDG.Track_Data[i] = Original.G; f_load.Text += " (original data restored)"; }
                }
            }
        }

        private void V2_Custom_CheckedChanged(object sender, EventArgs e)
        {
            V2_hlen.Enabled = V2_Custom.Checked;
            if (V2_Custom.Checked) V2_Auto_Adj.Checked = V2_rb.Checked = false;
        }

        private void V2_Apply_Click(object sender, EventArgs e)
        {
            if (V2_Auto_Adj.Checked || V2_rb.Checked)
            {
                for (int t = 0; t < tracks; t++)
                {
                    //if (NDS.cbm[t] == 1 || NDS.cbm[t] == 4)
                    if (NDS.cbm[t] == 4)
                    {
                        if (Original.OT[t].Length == 0)
                        {
                            Original.OT[t] = new byte[NDG.Track_Data[t].Length];
                            Array.Copy(NDG.Track_Data[t], 0, Original.OT[t], 0, NDG.Track_Data[t].Length);
                        }
                        int d = Get_Density(NDG.Track_Data[t].Length);
                        byte[] temp = new byte[0];
                        if (NDS.cbm[t] == 4) temp = Shrink_Track(NDG.Track_Data[t], d);
                        if (NDS.cbm[t] == 1) temp = Shrink_Gap_CBM(NDG.Track_Data[t], d, NDS.cbm_sector[t][NDS.cbm_sector[t].Length - 1] >> 3, t);
                        Set_Dest_Arrays(temp, t);
                    }
                }
            }
            else
            {
                for (int t = 0; t < tracks; t++)
                {
                    //if (NDS.cbm[t] == 1 || NDS.cbm[t] == 4)
                    if (NDS.cbm[t] == 4)
                    {
                        if (Original.OT[t].Length != 0)
                        {
                            NDG.Track_Data[t] = new byte[Original.OT[t].Length];
                            Array.Copy(Original.OT[t], 0, NDG.Track_Data[t], 0, Original.OT[t].Length);
                            Array.Copy(Original.OT[t], 0, NDA.Track_Data[t], 0, Original.OT[t].Length);
                            Array.Copy(Original.OT[t], 0, NDA.Track_Data[t], Original.OT[t].Length, 8192 - Original.OT[t].Length);
                        }
                        NDG.Track_Length[t] = NDG.Track_Data[t].Length;
                        NDA.Track_Length[t] = NDG.Track_Length[t] * 8;

                    }
                }
            }

            if (V2_Apply.Enabled)
            {
                int i = Convert.ToInt32(V2_hlen.Value);
                if (i >= V2_hlen.Minimum && i <= V2_hlen.Maximum)
                {
                    out_track.Items.Clear();
                    out_size.Items.Clear();
                    out_dif.Items.Clear();
                    out_sec.Items.Clear();
                    out_fmt.Items.Clear();
                    out_rpm.Items.Clear();
                    Process_Nib_Data(false, false); // false flag instructs the routine NOT to process CBM tracks again
                }
            }
        }

        private void AutoAdj_CheckedChanged(object sender, EventArgs e)
        {
            if (V2_Auto_Adj.Checked) { V2_Custom.Checked = false; V2_hlen.Enabled = false; V2_rb.Checked = false; }
        }

        private void V3_Auto_Adj_CheckedChanged(object sender, EventArgs e)
        {
            if (V3_Auto_Adj.Checked)
            {
                V3_Custom.Checked = V3_hlen.Enabled = false;
            }
        }

        private void V3_Custom_CheckedChanged(object sender, EventArgs e)
        {
            if (V3_Custom.Checked)
            {
                V3_Auto_Adj.Checked = false;
                V3_hlen.Enabled = true;
            }
            else V3_hlen.Enabled = false;
        }

        private void V3_Apply_Click(object sender, EventArgs e)
        {
            V3_Auto_Adjust();
        }

        private void RebuildV2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < tracks; i++)
            {
                if (NDS.cbm[i] == 2) Rebuild_V2(NDG.Track_Data[i], NDS.sectors[i], NDS.v2info[i], i);
            }
        }

        private void V2_rb_CheckedChanged(object sender, EventArgs e)
        {
            if (V2_rb.Checked)
            {
                V2_Auto_Adj.Checked = V2_Custom.Checked = V2_Add_Sync.Checked = false;
            }
        }

        private void Adj_cbm_CheckedChanged(object sender, EventArgs e)
        {
            V3_Auto_Adjust();
        }
    }
}