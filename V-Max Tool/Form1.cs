using System;
using System.Collections;
using System.Collections.Generic;
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
        private string fname = "";
        private string fext = ".g64";
        private string dirname = "";
        private int tracks = 0;
        private byte[] nib_header = new byte[256];
        private readonly string fnappend = "(sync_fixed)";
        private readonly byte[] sz = { 0x52, 0xc0, 0x0f, 0xfc };
        // vsec = the block header values & against byte[] sz
        private readonly string[] vsec = { "52-40-05-28", "52-40-05-2C", "52-40-05-48", "52-40-05-4C", "52-40-05-38", "52-40-05-3C", "52-40-05-58", "52-40-05-5C",
            "52-40-05-24", "52-40-05-64", "52-40-05-68", "52-40-05-6C", "52-40-05-34", "52-40-05-74", "52-40-05-78", "52-40-05-54", "52-40-05-A8",
            "52-40-05-AC", "52-40-05-C8", "52-40-05-CC", "52-40-05-B8" };
        // vmax = the block header values of V-Max v2 sectors (Cineamware variant non-CBM sectors)
        private readonly string[] Vmax_CW = { "A5", "A7", "A6", "AD", "AE", "A9", "AB", "AA", "B5", "B7", "B6", "A3" };
        private readonly byte[] VM2_Valid = { 0xa5, 0xa4, 0xa9, 0xaC, 0xad, 0xb4, 0xbc };
        private readonly string v2 = "A5-A5-A5-A5"; // V-MAX v2 sector 0 header (cinemaware)
        private readonly string v3 = "49-49-49-49"; // V-MAX v3 sector header
        private readonly string[] secF = { "[ NDOS ]", "[ CBM ]", "[ V-Max v2 ]", "[ V-Max v3 ]", "[ Loader ]", "[ tbd ]", "[ Unformatted ]" };

        public static class NDS  // Global variables for Nib file source data
        {
            public static byte[][] Track_Data = new byte[0][];
            public static int[] Track_Length = new int[0];
            public static int[] Sector_Zero = new int[0];
            public static int[] D_Start = new int[0];
            public static int[] D_End = new int[0];
            public static int[] cbm = new int[0];
        }

        public static class NDA  // Global variables for adjusted-sync arrays
        {
            public static byte[][] Track_Data = new byte[0][];
            public static int[] Track_Length = new int[0];
            public static int[] Sector_Zero = new int[0];
            public static int[] D_Start = new int[0];
            public static int[] D_End = new int[0];
        }

        public static class NDG  // Global variables for G64 array data
        {
            public static byte[][] Track_Data = new byte[0][];
            public static int[] Track_Length = new int[0];
        }

        public Form1()
        {
            InitializeComponent();
            label1.Text = label2.Text = "";
            button1.Enabled = button2.Enabled = button3.Enabled = false;
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Drag_Enter);
            this.DragDrop += new DragEventHandler(Drag_Drop);
            listBox3.HorizontalScrollbar = true;

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
        //
        //byte[] Shift_Right(byte[] data, int s, int pos)
        //{
        //    pos -= 1;
        //    if (pos + s < data.Length)
        //    {
        //        Array.Copy(data, pos, data, pos + s, data.Length - pos - s);
        //        for (int i = 0; i < s; i++) data[pos + i] = 0x00;
        //    }
        //    return data;
        //}

        //string Bin_Val(BitArray bits)
        //{
        //    string bin = "";
        //    for (int bi = 0; bi < bits.Count; bi++)
        //    {
        //        bin += Convert.ToInt32(bits[bi]);
        //    }
        //    return bin;
        //}

        string Hex_Val(byte[] data)
        {
            string hex = BitConverter.ToString(data);
            return hex;
        }
        string Hex(byte[] data, int a, int b)
        {
            string hex = BitConverter.ToString(data, a, b);
            return hex;
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

        (int, int, int, int) Find_Sector_Zero(byte[] data)
        {
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
            //List<string> compare = new List<string>(); // for testing
            byte[] d = new byte[4];
            var h = "";
            Compare();
            while (pos < source.Length - 32)
            {
                if (source[pos])
                {
                    sync_count++;
                    if (sync_count == 15) sync = true;
                }
                if (sync) Compare();
                if (end_found) break;
                if (!source[pos]) { sync = false; sync_count = 0; }
                pos++;
            }

            var len = (data_end - data_start);
            list.Add($"{(len) / 8} {data_start} {data_end}");
            //File.WriteAllLines($@"c:\test.txt", compare);
            //this.Text = $"{data_start} {data_end} {sector_zero} {Math.Abs(len)} {start_found} {end_found}";  // for testing
            return (data_start, data_end, sector_zero, len);

            void Compare()
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
                    if (vsec.Contains(h))
                    {
                        if (!list.Any(s => s == h))
                        {
                            if (!start_found) { data_start = pos; start_found = true; } // data_start = pos - sync_count;
                            if (!sec_zero && h == vsec[0]) //sec_z)
                            {
                                sector_zero = pos;
                                sec_zero = true;
                            }
                        }
                        else
                        {
                            if (list.Any(s => s == vsec[0]))
                            {
                                if (data_start == 0) data_end = pos;
                                else data_end = pos;
                                end_found = true;
                            }

                        }
                        list.Add(h);
                        string q = $"sector {Array.FindIndex(vsec, s => s == h)} Position {pos}";
                        //compare.Add($"{h} {q} {start_found} {end_found} {data_start} {data_end}");  // for testing
                    }
                }
            }
        }
        int Get_Loader_Len(byte[] data)
        {
            int p = 0;
            if (data != null)
            {
                byte[] star = new byte[16];
                Array.Copy(data, 0, star, 0, 16);
                byte[] comp = new byte[8176];
                Array.Copy(data, 16, comp, 0, 8176);

                for (p = 7100; p < comp.Length; p++)
                {
                    if (comp.Skip(p).Take(star.Length).SequenceEqual(star))
                    {
                        break;
                    }
                }
            }
            return p + 16;
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
                        Pad_Bits(dest_pos, d.Length - dest_pos, d); //- dest_pos, d);
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
                            for (int i = 0; i < expected_sync; i++) temp[(temp.Length - 1) - i] = true;   // d[(dest_pos - (sync_count + add) + i)] = true;
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
                        Array.Copy(dest, 0, t, 0, dest.Length);
                        Array.Copy(dest, 0, t, dest.Length, g);
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

        int Get_Data_Format(byte[] data, int trk)
        {
            //byte[] v = { 0x28, 0x2c, 0x48, 0x4c, 0x38, 0x3c, 0x58, 0x5c, 0x24, 0x64, 0x68, 0x6c, 0x34, 0x74, 0x78, 0x54, 0xa8, 0xac, 0xc8, 0xcc, 0xbb };
            int t = 0;
            int csec = 0;
            byte[] comp = new byte[4];
            if (Check_Loader(data)) return 4;
            for (int i = 0; i < data.Length - comp.Length; i++)
            {
                Array.Copy(data, i, comp, 0, comp.Length);
                t = Compare(comp);
                if (t != 0) break;
            }
            if (t == 0) t = Check_Blank(data);
            return t;

            int Compare(byte[] d)
            {
                if (Hex_Val(d) == v2) return 2;
                if (Hex_Val(d) == v3) return 3;
                if (d[0] == sz[0])
                {
                    d[1] &= sz[1]; d[2] &= sz[2]; d[3] &= sz[3];
                    if (vsec.Contains(Hex_Val(d))) { csec++; if (csec > 5) return 1; }
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
                byte[][] p = new byte[7][];
                p[0] = new byte[] { 0xd2, 0x4b, 0xff, 0x64 };
                p[1] = new byte[] { 0x4d, 0x6d, 0x5b, 0xff };
                p[2] = new byte[] { 0x92, 0x49, 0x24, 0x92 };
                p[3] = new byte[] { 0x6b, 0xff, 0x65, 0x53 };
                p[4] = new byte[] { 0x93, 0xff, 0x69, 0x25 };
                p[5] = new byte[] { 0x5a, 0x5a, 0x5a, 0x5a };
                p[6] = new byte[] { 0x69, 0x69, 0x69, 0x69 };

                int l = 0;
                byte[] cmp = new byte[4];
                ////byte[] ldr = new byte[] { 0x69, 0x5a, 0x4b, 0x92, 0x24, 0x49, 0x33, 0xb4, 0x2d, 0xd2, 0x96, 0xb4 };
                //byte[] ldr = new byte[] { 0x69, 0x5a, 0x4b, 0x92, 0x24, 0x33, 0xb4, 0x2d, 0xd2, 0x96, 0xb4 };
                for (int i = 0; i < d.Length - cmp.Length; i++)
                {
                    Array.Copy(d, i, cmp, 0, cmp.Length);
                    for (int j = 0; j < p.Length; j++)
                    {
                        if (Hex_Val(cmp) == Hex_Val(p[j]))
                        {
                            if (j < 5) i += 4; else i += 3;
                            l++;
                        }
                    }
                }
                if (l > 30) return true; else return false;
            }
        }

        (byte[], int, int, int, int, string[]) Adjust_Vmax_V2_Sync(byte[] data, int trk, bool process_Data)
        {
            int data_start = 0;
            int data_end = 0;
            int sec_zero = 0;
            bool start_found = false;
            bool end_found = false;
            byte[] start_byte = new byte[1];
            byte[] end_byte = new byte[1];
            byte[] pattern = new byte[] { 0xa5, 0xa5, 0xa5, 0xa5, 0xa5 };
            byte[] compare = new byte[5];
            bool found = false;
            List<string> all_headers = new List<string>();
            List<string> headers = new List<string>();
            for (int i = 0; i < data.Length - 4; i++)
            {
                Array.Copy(data, i, compare, 0, compare.Length);
                if (Hex_Val(compare) == Hex_Val(pattern))
                {
                    start_byte[0] = data[i - 1];
                    for (int j = 1; j < data.Length; j++)
                    {
                        if (data[j + i] != 0xa5)
                        {
                            end_byte[0] = data[j + i];
                            found = true;
                            pattern[0] = start_byte[0];
                            break;
                        }
                    }
                }
                if (found) break;
            }

            all_headers.Add($"track {trk}");

            byte[] track_data = Get_Track_Data(process_Data);
            byte[] t = new byte[8192];
            if (process_Data)
            {
                try
                {
                    NDG.Track_Data[trk] = track_data;
                    NDG.Track_Length[trk] = track_data.Length;
                    Array.Copy(track_data, 0, t, 0, track_data.Length);
                    Array.Copy(track_data, 0, t, track_data.Length, 8192 - track_data.Length);
                    //File.WriteAllBytes($@"C:\{trk}.bin", track_data);  // <- for debugging 
                    NDA.Track_Data[trk] = t;
                    data_start = 0;
                    data_end = track_data.Length << 3;
                    //sec_zero = 0;
                }
                catch { }
            }
            return (t, data_start, data_end, sec_zero << 3, track_data.Length << 3, all_headers.ToArray());

            byte[] Get_Track_Data(bool Fix_Sync)
            {
                List<byte> hd = new List<byte>();
                headers = new List<string>();
                var a = 0;
                for (int i = 0; i < data.Length - 1; i++)
                {
                    if (data[i] == start_byte[0] && VM2_Valid.Any(s => s == data[i + 1]))
                    {
                        hd = new List<byte>();
                        for (int j = 1; j + i < data.Length - 1; j++)
                        {
                            if (data[i + j] != end_byte[0])
                            {
                                hd.Add(data[i + j]);
                            }
                            else if (data[i + j] == end_byte[0])
                            {
                                i += j;
                                break;
                            }
                        }
                        byte[] hdr_ID = new byte[2];
                        Array.Copy(hd.ToArray(), 0, hdr_ID, 0, hdr_ID.Length);
                        string cheese = $"{Hex_Val(hdr_ID)}";
                        if (!headers.Any(g => g == cheese))
                        {
                            headers.Add(cheese);
                            all_headers.Add($"{a} pos {i} {Hex(start_byte, 0, 1)}-{cheese}-{Hex(end_byte, 0, 1)}");
                            if (!start_found) { data_start = i; start_found = true; }
                            a++;
                        }
                        else
                        {
                            //all_headers.Add($"{a} pos {i} {Hex(start_byte, 0, 1)}-{cheese}-{Hex(end_byte, 0, 1)}");
                            if (!end_found) { data_end = i; end_found = true; }
                            break;
                        }
                    }
                    if (end_found) break;
                }

                // Create array containing non-repeating track data and rotate so track 0 is at the start of the array
                    byte[] temp_data = new byte[data_end - data_start];
                    Array.Copy(data, data_start, temp_data, 0, data_end - data_start);
                    for (int i = 0; i < temp_data.Length - 5; i++)
                    {
                        Array.Copy(temp_data, i, compare, 0, compare.Length);
                        if (Hex_Val(compare) == Hex_Val(pattern))
                        {
                            sec_zero = i - 1;
                            temp_data = Rotate_Left(temp_data, i - 5);
                            break;
                        }
                    }
                if (Fix_Sync) // <- if the "Fix_Sync" bool is true, otherwise just return track info without any adjustments
                {
                    // ---------------------- Build new track with adjusted sync markers -------------------------------------- //
                    var buffer = new MemoryStream();
                    var write = new BinaryWriter(buffer);
                    var s_pos = 0;
                    //var sector_header = 18;  // Set the length of the sector header in multiples of 2 including the start and end marker.  Minimum = 6
                    byte[] sync_marker = new byte[] { 0x7f, 0xff };  // set the sync bytes (5b-ff) = 10 sync bits (7f-ff) = 15 sync bits
                    byte[] sec_header = new byte[0];
                    bool no_sync = false;
                    compare = new byte[2];
                    // begin processing the track
                    while (s_pos < temp_data.Length)
                    {
                        try
                        {
                            if (s_pos + 2 < temp_data.Length && temp_data[s_pos + 2] == start_byte[0])
                            {
                                byte[] header_ID = new byte[2];
                                if (s_pos + 4 < temp_data.Length - 1) Array.Copy(temp_data, s_pos + 3, header_ID, 0, 2);
                                while (temp_data[s_pos] != start_byte[0])
                                {
                                    // We're copying remaing sector data and ignoring potential sync (0x7f/0xff) in the source because we're going to add our own sync later.
                                    if (temp_data[s_pos] != 0x7f && temp_data[s_pos] != 0xff) write.Write(temp_data[s_pos]);
                                    s_pos++;
                                }
                                s_pos++; // sets source position 1 byte after the header start byte to get the header pattern data
                                Array.Copy(temp_data, s_pos, compare, 0, compare.Length);
                                if (headers.Any(s => s == Hex_Val(compare))) // <- checks to verify header pattern is in the list of valid headers
                                {
                                    // check that it's not sector 0 which needs sync, then check if a sync marker is before the header start byte.  If not, its a syncless track
                                    if (Hex_Val(compare) != "A5-A5" && (!sync_marker.Any(s => s == temp_data[s_pos - 2])) && temp_data[s_pos - 1] == start_byte[0]) no_sync = true;
                                    else no_sync = false;
                                    var header_length = 0;
                                    while (s_pos < temp_data.Length && temp_data[s_pos] != end_byte[0]) // <- getting the length of the header pattern
                                    {
                                        s_pos++; header_length++;
                                    }
                                    s_pos++;
                                    if (!no_sync) write.Write(sync_marker); // <- Here's where we add the sync (unless its a syncless track)
                                    write.Write(Build_Header(start_byte, end_byte, compare, (header_length / 2) * 2)); // building new header and writing to buffer
                                }
                            }
                        }
                        catch { }
                        if (s_pos < temp_data.Length) write.Write(temp_data[s_pos]); // <- loop writes sector data to the buffer until it hits another header
                        s_pos++;
                    }
                    compare = new byte[5];
                    for (int i = 0; i < temp_data.Length - 5; i++)
                    {
                        //Array.Copy(temp_data, i, compare, 0, compare.Length);
                        Array.Copy(buffer.ToArray(), i, compare, 0, compare.Length);
                        if (Hex_Val(compare) == Hex_Val(pattern))
                        {
                            sec_zero = i - 1;
                            break;
                        }
                    }
                    return buffer.ToArray(); // <- Return new array with sync markers adjusted

                    byte[] Build_Header(byte[] s, byte[] e, byte[] f, int len)
                    {
                        byte[] h = new byte[len + 2];
                        h[0] = s[0];
                        for (int i = 1; i < h.Length - 1; i++)
                        {
                            h[i] = f[0];
                            i++;
                            h[i] = f[1];
                        }
                        h[h.Length - 1] = e[0];
                        return h;
                    }
                }
                else return temp_data; // <- Return array without any adjustments to sync
            }
        }

        (string[], int, int, int, int) Get_vmv3_track_length(byte[] data, int trk)
        {
            int data_start = 0;
            int data_end = 0;
            int sector_zero = 0;
            bool start_found = false;
            bool end_found = false;
            bool szero = false;
            byte[] sze = new byte[] { 0xee, 0xf6, 0xf3 };
            byte[] szo = new byte[] { 0xee, 0xbc, 0xf3 };

            byte[] hd = new byte[] { 0x49, 0x49 };
            byte[] comp = new byte[2];
            byte[] head = new byte[18];
            //var ls = 0;
            List<string> s = new List<string>();
            List<string> ss = new List<string>();
            s.Add($"Track {trk}");
            for (int i = 0; i < data.Length - comp.Length; i++)
            {

                Array.Copy(data, i, comp, 0, comp.Length);
                if (Hex_Val(comp) == Hex_Val(hd))
                {
                    var a = 0;
                    while (data[i + a] == 0x49) a++;
                    i += a;
                    if (data[i] == 0xee)
                    {
                        if (i + head.Length < data.Length) Array.Copy(data, i, head, 0, head.Length);
                        if (!ss.Any(b => b == Hex_Val(head)))
                        {
                            if (Hex_Val(head).Contains(Hex_Val(sze)) || Hex_Val(head).Contains(Hex_Val(szo))) { sector_zero = i - a; szero = true; }
                            if (!start_found) { data_start = i - a; start_found = true; }
                            ss.Add(Hex_Val(head));
                            s.Add($"Pos {i - a} {Hex_Val(head)}");
                        }
                        else
                        {
                            end_found = true;
                            data_end = i - a;
                            s.Add($"Pos {i - a} repeat {Hex_Val(head)}");
                            string stats = $"Track Length ({data_end - data_start}) Sectors {ss.Count} ";
                            if (szero) stats += $" sector 0 {sector_zero}"; else stats += "Sector 0 not found";
                            s.Add(stats);
                        }
                    }
                }
                if (end_found) break;
            }
            return (s.ToArray(), data_start, data_end, sector_zero, (data_end - data_start) );
        }

        (byte[], int, int) Adjust_Vmax_V3_Sync(byte[] data, int sds, int sde, int ssz, int trk)
        {
            byte[] tdata = new byte[sde - sds];
            Array.Copy(data, sds, tdata, 0, sde - sds);
            if (ssz - sds - 5 >= 0) tdata = Rotate_Left(tdata, ssz - sds - 5);
            else tdata = Rotate_Right(tdata, 5);
            //File.WriteAllBytes($@"c:\{trk}.bin", tdata);
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            int spos = 0;
            byte[] sync = new byte[] { 0x7f, 0xff };
            byte[] hd = new byte[] { 0x49, 0x49 };
            byte[] comp = new byte[2];

            while (spos < tdata.Length)
            {
                if (spos + 2 < tdata.Length && tdata[spos + 2] == 0x49)
                {
                    Array.Copy(tdata, spos + 2, comp, 0, comp.Length);
                    if (Hex_Val(comp) == Hex_Val(hd))
                    {
                        var a = 0;
                        while (tdata[spos + a] != 0x49)
                        {
                            if (tdata[spos + a] != 0x7f && tdata[spos + a] != 0xff) write.Write(tdata[spos + a]);
                            a++;
                            
                        }
                        var b = 0;
                        while (spos + (a + b) < tdata.Length && tdata[spos + (a + b)] == 0x49) b++;
                        if (b > 20) for (int i = 0; i < (b + a); i++) write.Write((byte)0x49);
                        spos += (a + b);
                        write.Write(sync);
                        for (int i = 0; i < 3; i++) write.Write(hd);
                    }
                }
                if (spos < tdata.Length) write.Write(tdata[spos]);
                spos++;
            }
            //File.WriteAllBytes($@"c:\test-{trk}.bin", buffer.ToArray());
            return (buffer.ToArray(), (int)buffer.Length << 3, 0);


        }
        (byte[], bool) Fix_Loader(byte[] data)
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
            if (f) this.Text = " Found";
            Thread.Sleep(1000);
            this.Text = "";
            return (tdata, f);

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
        

        private void Drag_Enter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
        private void Drag_Drop(object sender, DragEventArgs e)
        {
            button1.Enabled = button2.Enabled = button3.Enabled = false;
            string[] headers = new string[0];
            listBox1.DataSource = null;
            listBox2.DataSource = null;
            listBox3.Items.Clear();
            List<string> gaps = new List<string>();
            List<string> haps = new List<string>();
            string[] File_List = (string[])e.Data.GetData(DataFormats.FileDrop);
            string l = "Not ok";
            bool halftracks = false;
            Double ht;
            if (File.Exists(File_List[0]))
            {
                dirname = Path.GetDirectoryName(File_List[0]);
                fname = Path.GetFileNameWithoutExtension(File_List[0]);
                fext = Path.GetExtension(File_List[0]);
                long length = new System.IO.FileInfo(File_List[0]).Length;
                tracks = (int)(length - 256) / 8192;
                if ((tracks * 8192) + 256 == length) l = "File Size OK!";
            }
            FileStream Stream = new FileStream(File_List[0], FileMode.Open, FileAccess.Read);
            if (fext.ToLower() == ".nib")
            {
                nib_header = new byte[256];
                Stream.Seek(0, SeekOrigin.Begin);
                Stream.Read(nib_header, 0, 256);
                var head = Encoding.ASCII.GetString(nib_header, 0, 13);
                var hm = "Bad Header";
                if (head == "MNIB-1541-RAW") hm = "Header Match!";
                label1.Text = $"{fname}{fext}";
                label2.Text = $"Total Track ({tracks}), {l}, {hm}";
                label1.Update();
                label2.Update();
            }
            if (fext.ToLower() != ".g64")
            {
                Set_Arrays(tracks);
                Get_Nib_Data();
                listBox1.DataSource = gaps;
                listBox1.Update();
                Process_Nib_Data();
                listBox2.DataSource = haps;
                button1.Enabled = button2.Enabled = button3.Enabled = true;
            }
            else
            {
                label1.Text = "Not a NIB file!";
                label2.Text = "";
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
                // NDA is the destination or output array
                NDA.Track_Data = new byte[len][];
                NDA.Sector_Zero = new int[len];
                NDA.Track_Length = new int[len];
                NDA.D_Start = new int[len];
                NDA.D_End = new int[len];
                NDA.Sector_Zero = new int[len];
                // NDG is the G64 arrays
                NDG.Track_Length = new int[len];
                NDG.Track_Data = new byte[len][];
            }

            void Get_Nib_Data()
            {
                if (tracks > 42)
                {
                    halftracks = true;
                    ht = 0.5;
                }
                else ht = 0;
                for (int i = 0; i < tracks; i++) // tracks
                {
                    NDS.Track_Data[i] = new byte[8192];
                    Stream.Seek(256 + (8192 * i), SeekOrigin.Begin);
                    Stream.Read(NDS.Track_Data[i], 0, 8192);
                    NDS.cbm[i] = Get_Data_Format(NDS.Track_Data[i], i);
                    if (NDS.cbm[i] == 1)
                    {
                        (NDS.D_Start[i], NDS.D_End[i], NDS.Sector_Zero[i], NDS.Track_Length[i]) = Find_Sector_Zero(NDS.Track_Data[i]);
                    }
                    if (NDS.cbm[i] == 2)
                    {
                        //Get V-Max v2 variant and data for processing
                        (NDA.Track_Data[i], NDS.D_Start[i], NDS.D_End[i], NDS.Sector_Zero[i], NDS.Track_Length[i], headers) = Adjust_Vmax_V2_Sync(NDS.Track_Data[i], i, false);
                        for (int j = 0; j < headers.Length; j++)
                        {
                            listBox3.Items.Add(headers[j]);
                            listBox3.Update();
                        }
                    }
                    if (NDS.cbm[i] == 3)
                    {
                        var len = 0;
                        string[] f = new string[0];
                        (f, NDS.D_Start[i], NDS.D_End[i], NDS.Sector_Zero[i], len) = Get_vmv3_track_length(NDS.Track_Data[i], i);
                        NDS.Track_Length[i] = len * 8;
                        for (int j = 0; j < f.Length; j++)
                        {
                            if (j < f.Length - 1) listBox3.Items.Add($"{j} {f[j]}"); else listBox3.Items.Add($"{f[j]}");
                        }

                    }
                    if (NDS.cbm[i] == 4)
                    {
                        NDS.Track_Length[i] = (Get_Loader_Len(NDS.Track_Data[i])) * 8;
                        NDG.Track_Data[i] = new byte[NDS.Track_Length[i] / 8];
                        Array.Copy(NDS.Track_Data[i], 0, NDG.Track_Data[i], 0, NDG.Track_Data[i].Length);
                        NDG.Track_Length[i] = NDG.Track_Data[i].Length;
                        NDA.Track_Length[i] = NDG.Track_Data[i].Length * 8;
                        NDA.Track_Data[i] = NDS.Track_Data[i];
                    }
                    //
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
                    if (NDS.Track_Length[i] > 0)
                    {
                        gaps.Add($"Track {ht:F1}: {NDS.Track_Length[i] / 8} / {NDS.Sector_Zero[i] / 8}"); // / {a} ");
                    }
                }
            }

            void Process_Nib_Data()
            {
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
                        if (NDS.cbm[i] == 1) Process_CBM(i);
                        if (NDS.cbm[i] == 2) Process_VMAX_V2(i);
                        if (NDS.cbm[i] == 3) Process_VMAX_V3(i);
                        if (NDS.cbm[i] == 4) Process_Loader(i);
                        var a = NDA.Track_Length[i];
                        if (NDA.Track_Length[i] > 0)
                        {
                            haps.Add($"{ht:F1}: {a / 8} / {NDA.Sector_Zero[i] / 8} : {secF[NDS.cbm[i]]}");
                        }
                    }
                    else { NDA.Track_Data[i] = NDS.Track_Data[i]; }
                }
                void Process_CBM(int trk)
                {
                    NDA.Track_Data[trk] = Adjust_Sync_CBM(NDS.Track_Data[trk], 40, 15, 55, NDS.D_Start[trk], NDS.D_End[trk], NDS.Sector_Zero[trk], NDS.Track_Length[trk], trk);
                    (NDA.D_Start[trk], NDS.D_End[trk], NDA.Sector_Zero[trk], NDA.Track_Length[trk]) = Find_Sector_Zero(NDA.Track_Data[trk]);
                }
                void Process_VMAX_V2(int trk)
                {
                    (NDA.Track_Data[trk], NDA.D_Start[trk], NDA.D_End[trk], NDA.Sector_Zero[trk], NDA.Track_Length[trk], headers) = Adjust_Vmax_V2_Sync(NDS.Track_Data[trk], trk, true);
                }
                void Process_VMAX_V3(int trk)
                {
                    (NDG.Track_Data[trk], NDA.Track_Length[trk], NDA.Sector_Zero[trk]) = 
                        Adjust_Vmax_V3_Sync(NDS.Track_Data[trk], NDS.D_Start[trk], NDS.D_End[trk], NDS.Sector_Zero[trk], trk);
                    NDG.Track_Length[trk] = NDG.Track_Data[trk].Length;
                    if (NDG.Track_Data[trk].Length > 0)
                    {
                        try
                        {
                            NDA.Track_Data[trk] = new byte[8192];
                            Array.Copy(NDG.Track_Data[trk], 0, NDA.Track_Data[trk], 0, NDG.Track_Data[trk].Length);
                            Array.Copy(NDG.Track_Data[trk], 0, NDA.Track_Data[trk], NDG.Track_Data[trk].Length, 8192 - NDG.Track_Data[trk].Length);
                        }
                        catch { }
                    }
                }
                void Process_Loader(int trk)
                {
                    bool f = false;
                    if(f_load.Checked) (NDG.Track_Data[trk], f) = Fix_Loader(NDG.Track_Data[trk]);
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
        }

        private void Make_Nib(object sender, EventArgs e)
        {
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            write.Write(nib_header);
            for (int i = 0; i < tracks; i++) write.Write(NDA.Track_Data[i]);
            File.WriteAllBytes($@"{dirname}\{fname}{fnappend}{fext}", buffer.ToArray());
            buffer.Close();
            write.Close();
        }

        private void Make_G64(object sender, EventArgs e)
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
                if (NDG.Track_Length[i] > 0)
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
                    if (i < NDG.Track_Data.Length && NDG.Track_Length[i] > 0)
                    {
                        write.Write((int)offset + th);
                        th += 2;
                        offset += m;
                        Track_Density(i, i);
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
                        Track_Density(i, r);
                        r++; td[r] = 0; r++;
                    }
                    else write.Write((int)0);
                    write.Write((int)0);
                }
            }

            void Track_Density(int i, int r)
            {
                if (NDG.Track_Length[i] < 6400) td[r] = 0;
                if (NDG.Track_Length[i] >= 6400) td[r] = 1;
                if (NDG.Track_Length[i] >= 6700) td[r] = 2;
                if (NDG.Track_Length[i] >= 7300) td[r] = 3;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < tracks; i++)
            {
                if (NDS.cbm[i] == 3)
                {
                    string[] f = (Get(NDS.Track_Data[i], i));
                    if (f.Length > 0)
                    {
                        listBox3.Items.Add($"track {i}");
                        for (int j = 0; j < f.Length; j++)
                        {
                            if (j < f.Length - 1) listBox3.Items.Add($"{j} {f[j]}"); else listBox3.Items.Add($"{f[j]}");
                        }
                    }
                }
            }
            //listBox3.DataSource = list;

            string[] Get(byte[] data, int trk)
            {
                int data_start = 0;
                int data_end = 0;
                int sector_zero = 0;
                bool start_found = false;
                bool end_found = false;
                bool szero = false;
                byte[] sz = new byte[] { 0xee, 0xf6, 0xf3 };
                byte[] hd = new byte[] { 0x49, 0x49 };
                byte[] comp = new byte[2];
                byte[] head = new byte[18];
                //var ls = 0;
                List<string> s = new List<string>();
                List<string> ss = new List<string>();
                for (int i = 0; i < data.Length - comp.Length; i++)
                {

                    Array.Copy(data, i, comp, 0, comp.Length);
                    if (Hex_Val(comp) == Hex_Val(hd))
                    {
                        var a = 0;
                        while (data[i + a] == 0x49) a++;
                        i += a;
                        if (data[i] == 0xee)
                        {
                            if (i + head.Length < data.Length) Array.Copy(data, i, head, 0, head.Length);
                            if (!ss.Any(b => b == Hex_Val(head)))
                            {
                                if (Hex_Val(head).Contains(Hex_Val(sz))) { sector_zero = i - a; szero = true; }
                                if (!start_found) { data_start = i - a; start_found = true; }
                                ss.Add(Hex_Val(head));
                                s.Add($"Pos {i - a} {Hex_Val(head)}");
                            }
                            else 
                            { 
                                end_found = true;
                                data_end = i - a;
                                s.Add($"Pos {i - a} repeat {Hex_Val(head)}");
                                string stats = $"Track Length ({data_end - data_start}) Sectors {ss.Count} ";
                                if (szero) stats += $" sector 0 {sector_zero}"; else stats += "Sector 0 not found";
                                s.Add (stats);
                            }
                        }
                    }
                    if (end_found) break;
                }
                return s.ToArray();
            }
        }
    }
}