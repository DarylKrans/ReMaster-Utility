using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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