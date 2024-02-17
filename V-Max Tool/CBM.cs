using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        private readonly byte[] sz = { 0x52, 0xc0, 0x0f, 0xfc };
        private readonly byte[] GCR_decode_high =
            {
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0x80, 0x00, 0x10, 0xff, 0xc0, 0x40, 0x50,
                0xff, 0xff, 0x20, 0x30, 0xff, 0xf0, 0x60, 0x70,
                0xff, 0x90, 0xa0, 0xb0, 0xff, 0xd0, 0xe0, 0xff
            };

        private readonly byte[] GCR_decode_low =
            {
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0x08, 0x00, 0x01, 0xff, 0x0c, 0x04, 0x05,
                0xff, 0xff, 0x02, 0x03, 0xff, 0x0f, 0x06, 0x07,
                0xff, 0x09, 0x0a, 0x0b, 0xff, 0x0d, 0x0e, 0xff
            };

        (int, int, int, int, string[], int, int[], int) Find_Sector_Zero(byte[] data)
        {
            int sectors = 0;
            int[] s_st = new int[valid_cbm.Length];
            int pos = 0;
            int sync_count = 0;
            int data_start = 0;
            int sector_zero = 0;
            int data_end = 0;
            int total_sync = 0;
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
                if (end_found) { add_total(); break; }
                if (!source[pos])
                {
                    add_total();
                    sync = false;
                    sync_count = 0;
                }
                pos++;
            }
            var len = (data_end - data_start);
            list.Add($"{(len) / 8} {data_start} {data_end}");
            //this.Text = $"{data_start} {data_end} {sector_zero} {Math.Abs(len)} {start_found} {end_found}";  // for testing
            return (data_start, data_end, sector_zero, len, headers.ToArray(), sectors, s_st, total_sync);

            void add_total()
            {
                if (sync && sync_count < 80) total_sync += sync_count;
            }

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
                            //sector_marker = true;

                            string sz = "";
                            int a = Array.FindIndex(valid_cbm, se => se == h);
                            if (a == 0) sz = "*";
                            s_st[a] = pos;
                            if (!start_found) { data_start = pos; start_found = true; }
                            if (!sec_zero && h == valid_cbm[0])
                            {
                                //if ((tracks > 42 && ((trk / 2) + 1 == 18)) || (tracks <= 42 && trk + 1 == 18)) sector_marker = true;
                                sector_zero = pos;
                                sec_zero = true;
                            }
                            headers.Add($"Sector ({a}){sz} pos ({p / 8}) Sync Length ({sync_count} bits) Header-ID [ {h} ]");
                        }
                        else
                        {
                            if (list.Any(s => s == valid_cbm[0]))
                            {
                                string sz = "";
                                int a = Array.FindIndex(valid_cbm, se => se == h);
                                if (a == 0) sz = "*";
                                headers[0] = $"Sector ({a}){sz} pos ({data_start / 8}) Sync Length ({sync_count} bits) Header-ID [ {h} ]";
                                headers.Add($"pos {p / 8} ** repeat ** {h}");
                                if (data_start == 0) data_end = pos;
                                else data_end = pos;
                                end_found = true;
                                headers.Add($"Track length ({(data_end - data_start) >> 3}) Sectors ({list.Count}) Avg sync length ({(total_sync + sync_count) / (list.Count * 2)} bits)");
                                sectors = list.Count;
                            }
                        }
                        list.Add(h);
                        string q = $"sector_data {Array.FindIndex(valid_cbm, s => s == h)} Position {pos}";
                        //compare.Add($"{h} {q} {start_found} {end_found} {data_start} {data_end}");  // for testing
                    }
                }
            }
        }

        byte[] Adjust_Sync_CBM(byte[] data, int expected_sync, int minimum_sync, int exception, int Data_Start_Pos, int Data_End_Pos, int Sec_0, int Track_Len, int Track_Num)
        {
            if (Track_Num == Track_Num - 0) { };
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
                if (data.Length >= 5000)
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

                    if (dest_pos < d.Length) Pad_Bits(dest_pos, d.Length - dest_pos, d);
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
                    for (int i = 0; i < (bcnt << 3); i++) temp[i] = d[i];
                    if (sync)
                    {
                        var ver = 0;
                        for (int i = 0; i < expected_sync; i++) { if (temp[temp.Length - (i + 1)] == true) ver++; }
                        if (ver < sync_count && ver < expected_sync && ver >= minimum_sync)
                        {
                            for (int i = 0; i < expected_sync; i++) temp[(temp.Length - 1) - i] = true;
                        }
                        temp[(temp.Length - 1) - expected_sync] = false;
                        temp[(temp.Length - 2) - expected_sync] = true;
                    }

                    byte[] dest = new byte[bcnt];
                    temp.CopyTo(dest, 0);
                    dest = Flip_Endian(dest);
                    return dest;
                }
            }
            return data;

        }

        byte[] Decode_CBM_GCR(byte[] data, int sector)
        {
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            BitArray source = new BitArray(Flip_Endian(data));
            BitArray sector_data = new BitArray(325 * 8);
            BitArray comp = new BitArray(5 * 8);
            byte[] sec = new byte[325];
            bool sector_marker = false;
            bool sector_found = false;
            bool sync = false;
            int sync_count = 0;
            int pos = 0;
            while (pos < source.Length - 32)
            {
                if (source[pos])
                {
                    sync_count++;
                    if (sync_count == 15) sync = true;
                }
                if (!source[pos])
                {
                    if (sync) sector_marker = Compare();
                    if (pos + sector_data.Count < source.Length)
                    {
                        if (sync && sector_found && !sector_marker)
                        {
                            Decode_Sector();
                            return buffer.ToArray();
                        }
                    }
                    sync = false;
                    sync_count = 0;
                }
                pos++;
            }
            return buffer.ToArray();

            void Decode_Sector()
            {
                for (int i = 0; i < sector_data.Count; i++) sector_data[i] = source[pos + i];
                sector_data.CopyTo(sec, 0);
                sec = Flip_Endian(sec);
                byte[] gcr = new byte[5];
                byte[] dec;
                int a = 0;
                for (int i = 0; i < (sec.Length / 5); i++)
                {
                    Array.Copy(sec, i * 5, gcr, 0, gcr.Length);
                    dec = Convert_4bytes_from_GCR(gcr);
                    if (dec.Length > 0)
                    {
                        if (a == 0)
                        {
                            write.Write((byte)dec[1]);
                            write.Write((byte)dec[2]);
                            write.Write((byte)dec[3]);
                        }
                        if (a > 0 && a < 64) write.Write(dec);
                        if (a == 64) { write.Write((byte)dec[0]); }
                        a++;
                    }
                }
            }

            bool Compare()
            {
                byte[] d = new byte[5];
                for (int i = 0; i < comp.Length; i++)
                {
                    comp[i] = source[pos + i];
                }
                comp.CopyTo(d, 0);
                d = Flip_Endian(d);
                if (d[0] == 0x52)
                {
                    byte[] g = Convert_4bytes_from_GCR(d);
                    if ((g[2] == sector)) { sector_found = true; return true; }
                    pos += sector_data.Count;
                }
                return false;
            }
        }

        string Get_Disk_Directory()
        {
            string ret = "Disk Directory ID : n/a";
            var buff = new MemoryStream();
            var wrt = new BinaryWriter(buff);
            var halftrack = 0;
            int track = 0;
            int blocksFree = 0;
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
                }
                byte[] directory = buff.ToArray();
                if (directory.Length >= 256)
                {
                    for (int i = 0; i < 35; i++) if (i != 17) blocksFree += directory[4 + (i * 4)];
                    ret = $"0 \"";
                    for (int i = 0; i < 23; i++)
                    {
                        if (directory[144 + i] != 0x00)
                        {
                            if (i != 16) ret += Encoding.ASCII.GetString(directory, 144 + i, 1).Replace('?', ' ');
                            else ret += "\"";
                        }
                    }
                }
                if (directory.Length > 256)
                {
                    string f_type; // = "";
                    bool end_of_dir = false;
                    byte[] file = new byte[32];
                    var blocks = (directory.Length / 256);
                    for (int i = 1; i < blocks; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            Array.Copy(directory, 256 * i + (j * 32), file, 0, file.Length);
                            if (file[3] == 0x00 && file[4] == 0x00 && file[5] == 0x00 && file[6] == 0x00) { end_of_dir = true; break; }
                            f_type = Get_File_Type(file[2]);
                            bool eof = false;
                            string f_name = "\"";
                            for (int k = 5; k < 21; k++)
                            {
                                if (file[k] != 0xa0)
                                {
                                    if (file[k] != 0x00) f_name += Encoding.ASCII.GetString(file, k, 1); else f_name += " ";
                                }
                                else
                                {
                                    if (!eof) f_name += "\""; else f_name += " ";
                                    eof = true;
                                }
                            }
                            if (!eof) f_name += "\""; else f_name += " ";
                            f_name = f_name.Replace('?', '-');
                            string sz = $"{BitConverter.ToUInt16(file, 30)}".PadRight(4);
                            ret += $"\n{sz} {f_name}{f_type}";
                        }
                        if (end_of_dir) break;
                    }
                    ret += $"\n{blocksFree} BLOCKS FREE.";
                }
            }
            return ret;

            string Get_File_Type(byte b)
            {
                byte[] f = new byte[1]; f[0] = b; f = Flip_Endian(f);
                string g = "";
                BitArray ft = new BitArray(f);
                if (!ft[0]) g += "*"; else g += " "; // [ 1000 xxxx ] high-bit 0 set = open file (not closed properly)
                if (ft[4] && !ft[5] && !ft[6] && !ft[7]) g += "DEL"; // low bits [ xxxx 1000 ] 
                if (!ft[4] && !ft[5] && !ft[6] && ft[7]) g += "SEQ"; // low bits [ xxxx 0001 ]
                if (!ft[4] && !ft[5] && ft[6] && !ft[7]) g += "PRG"; // low bits [ xxxx 0010 ]
                if (!ft[4] && !ft[5] && ft[6] && ft[7]) g += "USR";  // low bits [ xxxx 0011 ]
                if (!ft[4] && ft[5] && !ft[6] && !ft[7]) g += "REL"; // low bits [ xxxx 0100 ]
                if (!ft[4] && !ft[5] && !ft[6] && !ft[7]) g += "???";// low bits [ xxxx 0000 ] <- file info exists, but no file-type bits set
                if (ft[1]) g += "<"; //  [ 0100 xxxx ] high-bit 1 set = locked file
                return g;
            }
        }

        byte[] Convert_4bytes_from_GCR(byte[] gcr)
        {
            byte hnib;
            byte lnib;
            byte[] plain = new byte[] { 0x00, 0x00, 0x00, 0x00 };

            hnib = GCR_decode_high[gcr[0] >> 3];
            lnib = GCR_decode_low[((gcr[0] << 2) | (gcr[1] >> 6)) & 0x1f];
            if (!(hnib == 0xff || lnib == 0xff)) plain[0] = hnib |= lnib;

            hnib = GCR_decode_high[(gcr[1] >> 1) & 0x1f];
            lnib = GCR_decode_low[((gcr[1] << 4) | (gcr[2] >> 4)) & 0x1f];
            if (!(hnib == 0xff || lnib == 0xff)) plain[1] = hnib |= lnib;

            hnib = GCR_decode_high[((gcr[2] << 1) | (gcr[3] >> 7)) & 0x1f];
            lnib = GCR_decode_low[(gcr[3] >> 2) & 0x1f];
            if (!(hnib == 0xff || lnib == 0xff)) plain[2] = hnib |= lnib;

            hnib = GCR_decode_high[((gcr[3] << 3) | (gcr[4] >> 5)) & 0x1f];
            lnib = GCR_decode_low[gcr[4] & 0x1f];
            if (!(hnib == 0xff || lnib == 0xff)) plain[3] = hnib |= lnib;

            return plain;
        }

    }
}