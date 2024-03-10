﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

/// CBM Block Header structure
/// 8 plain bytes converted to 10 GCR bytes
/// 
/// Byte 0          0x08  (always 08)
/// byte 1          EOR of next 4 bytes (sector, track, ID byte 2, ID byte 1)
/// byte 2          Sector #
/// byte 3          Track #
/// byte 4          Disk ID byte 2
/// byte 5          Disk ID byte 1
/// byte 6          0x0f (filler to make full GCR chunk)
/// byte 7          0x0f (filler to make full GCR chunk)
///
/// CBM Block Data structure
/// 
/// byte 0          0x07 (always 07) Sector marker?
/// byte 1-256      (sector data) 256 bytes
/// byte 257        Checksum (0 ^ bytes 1-257)
/// byte 258-260    0x00 (not used)
/// 
/// Standard CBM sector
/// 
/// byte 0          (track of next sector in file) 0x00 if end of file
/// byte 1          (sector # of next sector in fiel) 0xff if end of file
/// byte 2-256      file data


namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        readonly bool write_dir = false;
        private readonly byte[] sz = { 0x52, 0xc0, 0x0f, 0xfc };

        byte[] Rebuild_CBM(byte[] data, int sectors, byte[] Disk_ID, int t_density, int trk)
        {
            if (tracks > 42) trk = (trk / 2) + 1; else trk += 1;
            int sector_len = 10 + 325 + 10;
            int gap_len = 25;
            int gap_sync = 0;
            byte s = 0x00;
            byte[] sync = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff };
            //(s, gap_sync) = Find_Longest_Sync(data);  // <- uncomment to add sync to end of track if source has sync at the end
            if (s != 0xff || gap_sync <= 10) gap_sync = 0;
            int sector_gap = (density[t_density] - ((sector_len * sectors) + gap_len + gap_sync)) / (sectors * 2);
            gap_len = density[t_density] - gap_sync - ((sector_len + (sector_gap * 2)) * sectors);
            byte[] gap = new byte[sector_gap];
            byte[] tail_gap = new byte[sector_gap + gap_len];
            byte[] tail_sync = new byte[gap_sync];
            for (int i = 0; i < tail_gap.Length; i++)
            {
                if (i < sector_gap) gap[i] = 0x55;
                tail_gap[i] = 0x55;
            }
            for (int i = 0; i < gap_sync; i++) tail_sync[i] = 0xff;
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            bool c;
            byte[] current_sector;
            for (int i = 0; i < sectors; i++)
            {
                write.Write(sync);
                write.Write(Build_BlockHeader(trk, i, Disk_ID));
                write.Write(gap);
                write.Write(sync);
                (current_sector, c) = Decode_CBM_GCR(data, i, false);
                if (c || !c) write.Write(current_sector);
                if (i != sectors - 1) write.Write(gap);
            }
            if (gap_sync > 0) write.Write(tail_sync);
            if (gap_len > 0) write.Write(tail_gap);
            return buffer.ToArray();
        }

        
        (int, int, int, int, string[], int, int[], int, byte[]) Find_Sector_Zero(byte[] data, bool checksums)
        {
            string[] csm = new string[] { "OK", "Bad!" };
            string decoded_header;
            int sectors = 0;
            int[] s_st = new int[valid_cbm.Length];
            int pos = 0;
            int sync_count = 0;
            int data_start = 0;
            int sector_zero = 0;
            int data_end = 0;
            int total_sync = 0;
            int s_hed = 80;
            int comp = 32;
            bool sec_zero = false;
            bool sync = false;
            bool start_found = false;
            bool end_found = false;
            byte[] pdata = new byte[data.Length];
            byte[] dec_hdr;
            byte[] sec_hdr = new byte[10];
            byte[] Disk_ID = new byte[4];
            Array.Copy(data, 0, pdata, 0, data.Length);
            data = Flip_Endian(data);
            BitArray source = new BitArray(data);
            List<string> list = new List<string>();
            List<string> headers = new List<string>();
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
            return (data_start, data_end, sector_zero, len, headers.ToArray(), sectors, s_st, total_sync, Disk_ID);
        
            void add_total()
            {
                if (sync && sync_count < 80) total_sync += sync_count;
            }
        
            void Compare(int p)
            {
                d = Flip_Endian(Bit2Byte(source, pos, comp));
                if (d[0] == 0x52 && !(d[1] == 0x55 && d[2] == 0x55 && d[3] == 0x55))
                {
        
                    for (int i = 1; i < sz.Length; i++) d[i] &= sz[i];
                    h = Hex_Val(d);
                    if (valid_cbm.Any(s => s == h)) //.Contains(h))
                    {
                        if (!list.Any(s => s == h))
                        {
                            dec_hdr = Decode_GCR(Flip_Endian(Bit2Byte(source, pos, 80)));
                            decoded_header = Hex_Val(dec_hdr);
                            int chksum = 0;
                            string hdr_c = csm[0];
                            for (int i = 0; i < 4; i++) chksum ^= dec_hdr[i + 2];
                            if (chksum != dec_hdr[1]) hdr_c = csm[1];
                            string sz = "";
                            int a = Array.FindIndex(valid_cbm, se => se == h);
                            if (a == 0) sz = "*";
                            s_st[a] = pos;
                            if (!start_found) { data_start = pos; start_found = true; }
                            if (!sec_zero && h == valid_cbm[0])
                            {
                                Array.Copy(dec_hdr, 4, Disk_ID, 0, 4);
                                sector_zero = pos;
                                sec_zero = true;
                            }
                            string sec_c = csm[0];
                            if (checksums)
                            {
                                byte[] s_dat;
                                bool c;
                                (s_dat, c) = Decode_CBM_GCR(pdata, a, true);
                                if (!c) sec_c = csm[1];
                            }
                            headers.Add($"Sector ({a}){sz} Checksum ({sec_c}) pos ({p / 8}) Sync ({sync_count} bits) Header-ID [ {decoded_header.Substring(6, decoded_header.Length - 12)} ] Header ({hdr_c})");
                        }
                        else
                        {
                            if (list.Any(s => s == valid_cbm[0]))
                            {
                                string sz = "";
                                int a = Array.FindIndex(valid_cbm, se => se == h);
                                if (a == 0) sz = "*";
                                dec_hdr = Decode_GCR(Flip_Endian(Bit2Byte(source, data_start, s_hed)));
                                decoded_header = Hex_Val(dec_hdr);
                                int chksum = 0;
                                string hdr_c = csm[0];
                                for (int i = 0; i < 4; i++) chksum ^= dec_hdr[i + 2];
                                if (chksum != dec_hdr[1]) hdr_c = csm[1];
                                string sec_c = csm[0];
                                if (checksums)
                                {
                                    byte[] s_dat;
                                    bool c;
                                    (s_dat, c) = Decode_CBM_GCR(pdata, a, true);
                                    if (!c) sec_c = csm[1];
                                }
        
                                headers[0] = $"Sector ({a}){sz} Checksum ({sec_c}) pos ({data_start / 8}) Sync ({sync_count} bits) Header-ID [ {decoded_header.Substring(6, decoded_header.Length - 12)} ] Header ({hdr_c})";
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

                    byte[] dest = Bit2Byte(temp);
                    dest = Rotate_Right(Flip_Endian(dest), 6);
                    return dest;
                }
            }
            return data;

        }

        (byte[], bool) Decode_CBM_GCR(byte[] data, int sector, bool decode)
        {
            byte[] tmp = new byte[0];
            BitArray source = new BitArray(Flip_Endian(data));
            BitArray sector_data = new BitArray(325 * 8);
            byte[] sec;  // = new byte[325];
            bool sector_marker = false;
            bool sector_found = false;
            bool sync = false;
            int sync_count = 0;
            int pos = 0;
            Compare();
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
                            byte[] dec;
                            bool chksm;
                            (dec, chksm) = Decode_Sector();
                            if (!decode) return (dec, chksm);
                            tmp = new byte[dec.Length - 4];
                            Array.Copy(dec, 1, tmp, 0, tmp.Length);
                            return (tmp, chksm);
                        }
                    }
                    sync = false;
                    sync_count = 0;
                }
                pos++;
            }
            return (tmp, false);

            (byte[], bool) Decode_Sector()
            {
                sec = Flip_Endian(Bit2Byte(source, pos, sector_data.Count));
                if (!decode) return (sec, false);
                byte[] d_sec = Decode_GCR(sec);
                /// Calculate block checksum
                int cksm = 0;
                for (int i = 1; i < 257; i++) cksm ^= d_sec[i];
                return (d_sec, cksm == d_sec[257]);
            }

            bool Compare()
            {
                int cl = 5;
                if (!decode) cl = 10;
                byte[] d = Flip_Endian(Bit2Byte(source, pos, cl * 8));
                if (d[0] == 0x52 && !(d[1] == 0x55 && d[2] == 0x55 && d[3] == 0x55))
                {
                    byte[] g = Decode_GCR(d);
                    if (g[3] > 0 && g[3] < 43)
                    {
                        if ((g[2] == sector)) { sector_found = true; return true; }
                        pos += sector_data.Count;
                    }
                }
                return false;
            }
        }

        bool Find_Sector(byte[] data, int sector)
        {
            BitArray source = new BitArray(Flip_Endian(data));
            bool sector_found; // = false;
            bool sync = false;
            int sync_count = 0;
            int pos = 0;
            int cl = 5;
            //BitArray comp = new BitArray(40);
            sector_found = Compare();
            if (!sector_found)
            {
                while (pos < source.Length - 32)
                {
                    if (source[pos])
                    {
                        sync_count++;
                        if (sync_count == 15) sync = true;
                    }
                    if (!source[pos])
                    {
                        if (sync) sector_found = Compare();
                        if (sector_found) break; // return sector_marker;
                        sync = false;
                        sync_count = 0;
                    }
                    pos++;
                }
            }
            return sector_found;

            bool Compare()
            {
                byte[] d = Flip_Endian(Bit2Byte(source, pos, cl * 8));
                if (d[0] == 0x52 && !(d[1] == 0x55 && d[2] == 0x55 && d[3] == 0x55))
                {
                    byte[] g = Decode_GCR(d);
                    if (g[3] > 0 && g[3] < 43)
                    {
                        if ((g[2] == sector)) { return true; }
                        pos += (320 * 8);
                    }
                }
                return false;
            }
        }

        void Get_Disk_Directory()
        {
            int l = 0;
            string ret = "Disk Directory ID : n/a";
            var buff = new MemoryStream();
            var wrt = new BinaryWriter(buff);
            var halftrack = 0;
            int track = 0;
            int blocksFree = 0;
            bool c;
            if (tracks <= 42) { halftrack = 17; track = halftrack + 1; }
            if (tracks > 42) { halftrack = 34; track = (halftrack / 2) + 1; }
            byte[] temp;
            if (NDS.cbm[halftrack] == 1)
            {
                byte[] next_sector = new byte[] { (byte)track, 0x00 };
                byte[] last_sector = new byte[2];
                while (Convert.ToInt32(next_sector[0]) == track)
                {
                    Array.Copy(next_sector, 0, last_sector, 0, 2);
                    (temp, c) = Decode_CBM_GCR(NDA.Track_Data[halftrack], Convert.ToInt32(next_sector[1]), true);
                    if (temp.Length > 0 && (c || !c))
                    {
                        Array.Copy(temp, 0, next_sector, 0, next_sector.Length);
                        if (tracks <= 42) halftrack = Convert.ToInt32(next_sector[0]) - 1;
                        else
                        {
                            if ((next_sector[0] - 1) * 2 >= 0) halftrack = (Convert.ToInt32(next_sector[0]) - 1) * 2;
                        }
                        wrt.Write(temp);
                        if (BytesMatch(last_sector, next_sector)) break;
                    }
                    else { ret = "Error processing directory!"; break; }
                }
                if (buff.Length != 0)
                {
                    // Read track 18 sector 1 if sector 0 signals the end of the directory
                    if (buff.Length < 257)
                    {
                        (temp, c) = Decode_CBM_GCR(NDA.Track_Data[halftrack], 1, true);
                        if (c || !c) wrt.Write(temp);
                    }
                    byte[] directory = buff.ToArray();
                    if (write_dir) File.WriteAllBytes($@"hdr_c:\dir", directory);
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
                            l = ret.Length - 2;
                        }
                    }
                    if (directory.Length > 256)
                    {
                        string f_type;
                        byte[] file = new byte[32];
                        var blocks = (directory.Length / 256);
                        for (int i = 1; i < blocks; i++)
                        {
                            for (int j = 0; j < 8; j++)
                            {
                                Array.Copy(directory, 256 * i + (j * 32), file, 0, file.Length);
                                if (file[2] != 0x00)
                                {
                                    f_type = Get_DirectoryFileType(file[2]);
                                    bool eof = false;
                                    string f_name = "\"";
                                    for (int k = 5; k < 21; k++)
                                    {
                                        if (file[k] != 0xa0)
                                        {
                                            if (file[k] != 0x00) f_name += Encoding.ASCII.GetString(file, k, 1); else f_name += "@";
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
                            }
                        }
                        ret += $"\n{blocksFree} BLOCKS FREE.";
                    }
                }
            }
            if (ret.Length > 0)
            {
                Dir_screen.Text = ret;
                Dir_screen.Select(2, l);
                Dir_screen.SelectionBackColor = c64_text;
                Dir_screen.SelectionColor = C64_screen;
            }

            string Get_DirectoryFileType(byte b)
            {
                string fileType = " ";
                if ((b | 0x3f) == 0x3f || (b | 0x3f) == 0x7f) fileType = "*";
                switch (b | 0xf0)
                {
                    case 0xf0: fileType += "DEL"; break;
                    case 0xf1: fileType += "SEQ"; break;
                    case 0xf2: fileType += "PRG"; break;
                    case 0xf3: fileType += "USR"; break;
                    case 0xf4: fileType += "REL"; break;
                    case 0xf8: fileType += "DEL"; break;
                    default: fileType += "???"; break;
                }
                if ((b | 0x3f) == 0xff || (b | 0x3f) == 0x7f) fileType += "<";
                return fileType;
            }
        }

        void Create_Blank_Disk()
        {
            byte[] name = Encoding.ASCII.GetBytes("TEST DISK");
            byte[] id = Encoding.ASCII.GetBytes("00 2A");
            byte[] Disk_ID = new byte[] { id[1], id[0], 0x0f, 0x0f };
            byte[] sync = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff };
            byte[] dir_s0 = Encode_GCR(T18S0());
            byte[] dir_s1 = Encode_GCR(T18S1());
            byte[] blank = Encode_GCR(Empty_Sector());
            int ht;
            for (int i = 0; i < 40; i++)
            {
                if (tracks > 42) ht = i * 2; else ht = i;
                MemoryStream buffer = new MemoryStream();
                BinaryWriter write = new BinaryWriter(buffer); // = new BinaryWriter(buffer);
                for (int j = 0; j < Available_Sectors[i]; j++)
                {
                    bool w = true;
                    write.Write(sync);
                    write.Write(Build_BlockHeader(i + 1, j, Disk_ID));
                    for (int k = 0; k < sector_gap_length[i]; k++) write.Write((byte)0x55);
                    write.Write(sync);
                    if (i == 17 && j == 0) { write.Write(dir_s0); w = false; }
                    if (i == 17 && j == 1) { write.Write(dir_s1); w = false; }
                    if (w) write.Write(blank);
                    for (int k = 0; k < sector_gap_length[i]; k++) write.Write((byte)0x55);
                }
                while (buffer.Length < density[density_map[i]]) write.Write((byte)0x55);
                byte[] nt = buffer.ToArray();
                Set_Dest_Arrays(nt, ht);
            }

            byte[] T18S0()
            {
                int chksum = 0;
                byte[] title = new byte[27];
                byte[] ds0 = new byte[] { 0x12, 0x01, 0x41, 0x00 };
                for (int i = 0; i < title.Length; i++)
                {
                    if (i < 18) if (i < name.Length) title[i] = name[i]; else title[i] = 0xa0;
                    else if (i - 18 < id.Length) title[i] = id[i - 18]; else title[i] = 0xa0;
                }
                byte[] bam = Create_BAM();
                var buff = new MemoryStream();
                var wrt = new BinaryWriter(buff);
                wrt.Write((byte)0x07);
                wrt.Write(ds0);
                wrt.Write(bam);
                wrt.Write(title);
                while (buff.Length < 256) wrt.Write((byte)0x00);
                byte[] s = new byte[260];
                Array.Copy(buff.ToArray(), 0, s, 0, buff.Length);
                for (int i = 1; i < 257; i++) chksum ^= s[i];
                s[257] = (byte)chksum;
                return s;
            }

            byte[] T18S1()
            {
                int chksum = 0;
                var buff = new MemoryStream();
                var wrt = new BinaryWriter(buff);
                wrt.Write((byte)0x07);
                wrt.Write((byte)0x00);
                wrt.Write((byte)0xff);
                while (buff.Length < 260) wrt.Write((byte)0x00);
                byte[] t = buff.ToArray();
                for (int i = 1; i < 257; i++) chksum ^= t[i];
                t[257] = (byte)chksum;
                return t;
            }

            byte[] Create_BAM()
            {
                var buff = new MemoryStream();
                var wrt = new BinaryWriter(buff);
                byte[] bf = new byte[35];
                byte[][] used_sectors = new byte[35][];
                BitArray us = new BitArray(24);
                for (int i = 0; i < 35; i++)
                {
                    bf[i] = Available_Sectors[i];
                    used_sectors[i] = new byte[3];
                    for (int j = 0; j < Available_Sectors[i]; j++) us[j] = true;
                    us.CopyTo(used_sectors[i], 0);
                    used_sectors[i] = Flip_Endian(used_sectors[i]);
                    wrt.Write(bf[i]);
                    wrt.Write(used_sectors[i]);
                }
                return buff.ToArray();
            }

            byte[] Empty_Sector()
            {
                int chksum = 0;
                var buff = new MemoryStream();
                var wrt = new BinaryWriter(buff);
                wrt.Write((byte)0x07);
                wrt.Write((byte)0x4b);
                chksum ^= 0x4b;
                while (buff.Length < 260)
                {
                    if (buff.Length < 257)
                    {
                        wrt.Write((byte)0x01);
                        chksum ^= 1;
                    }
                    if (buff.Length == 257) wrt.Write((byte)chksum);
                    if (buff.Length > 257) wrt.Write(((byte)0x00));
                }
                return buff.ToArray();
            }
        }
    }
}
