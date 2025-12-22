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
        /*
        v2_info[0] = v2 header start byte
        v2_info[1] = v2 header end byte
        v2_info[2] = header length
        v2_info[3] = v2 version
        v2_info[4] = 0 (syncless track) 1 (track has sync)
        v2_info[5] = gap located before this sector
        */
        private static readonly byte[] vm2_pos_sync = { 0x57, 0x5b, 0x5f, 0x7f, 0xff };
        //private readonly byte[] v2_sync_marker = { 0x5b, 0xff }; /// 0x5b, 0xff (known working)
        private static readonly byte[] v2_sync_marker = { 0x7f, 0xff, 0xff }; /// 0x5b, 0xff (known working)
        private static readonly string[][] vm2_ver = new string[2][];
        private static readonly string[] v_check = { "A5-A3", "A9-A3", "AD-AB", "AD-A7" };
        private static readonly byte[] VM2_Valid = { 0xa5, 0xa4, 0xa9, 0xaC, 0xad, 0xb4, 0xbc };
        private static readonly byte[] vv2n = { 0x64, 0xa5, 0xa5, 0xa5 };
        private static readonly byte[] vv2p = { 0x4e, 0xa5, 0xa5, 0xa5 };
        private static byte[] vmax_dec_table = new byte[0];
        private static byte[] v2stub = new byte[0];
        private static readonly byte[] cart_patch_v2 = { 0x39, 0x00, 0xcd, 0xf1, 0xd7 };

        void GetNewHeaders()
        {
            if (V2_swap_headers.Checked) NDG.newheader = new byte[] { 0x64, 0x4e };
            else NDG.newheader = new byte[] { 0x64, 0x46 };
        }

        (byte[] sector, bool checksum, int pos) Find_VMax_Sector(byte[] data, BitArray source, int sector, int version, bool decode = false, int trk = -1)
        {
            byte[] secdata = new byte[0];
            bool checksum = false;
            if ((data == null && source == null) || sector < 0) return (secdata, false, -1);
            if (version == 2)
            {
                byte[] sb = new byte[] { 0x64, 0x4e };
                byte[] eb = new byte[] { 0x46, 0x4e, 0x64 };
                // process as bitarray //
                //BitArray source = new BitArray(Flip_Endian(data));
                if (source == null || source.Count < 1) source = new BitArray(Flip_Endian(data));
                int pos = 0;
                byte compare = 0;
                while (pos < source.Length)
                {
                    compare <<= 1;
                    if (source[pos]) compare |= 1;
                    if (sb.Any(x => x == compare))
                    {
                        try
                        {
                            bool t = false;
                            int dpos = 0;
                            var a = Bit2Byte(source, pos + 1, 5 << 3);
                            if (VM2_Valid.Any(x => x == a[0]) && a[2] == a[0] && a[3] == a[1]) t = true;
                            if (t && (a[0] ^ a[1]) == sector)
                            {
                                sb = new byte[] { compare };
                                byte[] getsec = Bit2Byte(source, pos + 1, Math.Min(380 << 3, source.Length - pos));
                                while (!eb.Any(x => x == getsec[dpos])) dpos++;
                                eb = new byte[] { getsec[dpos] };
                                secdata = CopyFrom(getsec, dpos + 1, 320);
                                byte[] dec = Decode_VmaxGCR(secdata);
                                if (dec != null) checksum = Get_Checksum(dec);
                                return (decode ? dec : secdata, checksum, pos + ((dpos + 1) << 3) + 1);
                            }
                            else if (t && (a[0] ^ a[1]) < 22) pos += (300 + dpos - 1) << 3;
                            else pos += 8;
                        }
                        catch { }
                    }
                    pos++;
                }
            }
            if (version == 3)
            {
                if (data == null && source.Count > 0) data = Bit2Byte(source);
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0x49)
                    {
                        while (data[i] != 0xee) i++;
                        byte[] cmp = Decode_VmaxGCR(CopyFrom(data, i + 1, 8));
                        if ((cmp[0] & 0x1f) == sector)
                        {
                            i++;
                            try
                            {
                                secdata = CopyFrom(data, i, Get_vm3_sectorSize(data, i));
                                byte[] dec = Decode_VmaxGCR(secdata);
                                if (dec != null) checksum = Get_Checksum(dec);
                                return (decode ? dec : secdata, checksum, i);
                            }
                            catch { }
                        }
                    }
                }
            }
            return (secdata, false, -1);

            bool Get_Checksum(byte[] d)
            {
                int csm = 0;
                for (int j = 0; j < d.Length; j++) csm ^= d[j];
                return csm == 0;
            }
        }

        void V2_Adv_Opts()
        {
            if (V2_Auto_Adj.Checked)
            {
                for (int t = 0; t < tracks; t++)
                {
                    if (NDS.cbm[t] == 4) if (Original.OT[t].Length == 0) Original.OT[t] = CopyFrom(NDG.Track_Data[t]);
                }
            }
            else
            {
                for (int t = 0; t < tracks; t++)
                {
                    if (NDS.cbm[t] == 4 || NDS.cbm[t] == 1)
                    {
                        if (Original.OT[t].Length != 0)
                        {
                            NDG.Track_Data[t] = CopyFrom(Original.OT[t]);
                            NDA.Track_Data[t] = FillArray(Original.OT[t], 8192);
                        }
                        NDG.Track_Length[t] = NDG.Track_Data[t].Length;
                        NDA.Track_Length[t] = NDG.Track_Length[t] << 3;
                    }
                }
            }
            int i = Convert.ToInt32(V2_hlen.Value);
            if (i >= V2_hlen.Minimum && i <= V2_hlen.Maximum)
            {
                Clear_Out_Items();
                Process_Nib_Data(true, false, !V2_Auto_Adj.Checked, true);
            }
        }

        (byte[], int, int, int) Rebuild_V2(byte[] data, int sectors, byte[] t_info, int trk, byte[] new_header, byte[][] sector_data, bool use_new_Headers = false)
        {
            /// t_info[0] = start byte, t_info[1] = end byte, t_info[2] = header length, t_info[3] = v-max version (for sector headers)
            int track_num = tracks > 42 ? (trk / 2) + 1 : trk + 1;
            (var found, var pos) = Find_Data(new byte[] { t_info[0], 0xa5, 0xa5 }, data);
            if (found) data = Rotate_Left(data, pos - 1);
            else
            {
                int gap = FindTrackGap(data);
                if (gap > 0) data = Rotate_Left(data, gap);
            }
            bool syncless = t_info[4] == 0x00;
            bool addSync = V2_Add_Sync.Checked;
            int t_dens = density[vm2_density_map[track_num - 1]];
            int t_sync = syncless && !addSync ? v2_sync_marker.Length : v2_sync_marker.Length * sectors;
            byte[][] sec_dat = new byte[sectors][];
            byte[][] header = new byte[sectors][];
            byte[] t_ID = track_num % 2 == 1 ? ArrayConcat(v2_sync_marker, new byte[] { 0xff, 0xff },
                Build_BlockHeader(track_num, 255, NDS.t18_ID)) : new byte[] { 0x7f };
            byte header1 = use_new_Headers ? new_header[0] : t_info[0];
            byte header2 = use_new_Headers ? new_header[1] : t_info[1];
            List<string> sf = new List<string>();
            for (int i = 0; i < sectors; i++)
            {
                if (!use_new_Headers)
                {
                    header[i] = Hex2Byte(vm2_ver[t_info[3]][i]);
                    sec_dat[i] = sector_data[i];
                }
                else
                {
                    bool older = header2 == 0x46;
                    header[i] = Hex2Byte(vm2_ver[0][i]);
                    sec_dat[i] = CopyArray(sector_data[i]);
                    for (int j = 0; j < sec_dat[i].Length; j++)
                    {
                        if (sec_dat[i][j] == 0xa3) sec_dat[i][j] = 0xad;
                        if (sec_dat[i][j] == 0xe2) sec_dat[i][j] = 0xea;
                    }
                }
                //if (track_num == 19 && i == 14 && P_Cart.Checked) sec_dat[i] = Find_Cart_Protection_v2_Compressed(sector_data[i], true, use_new_Headers).Item2;
                if (P_Cart.Checked) sec_dat[i] = Find_Cart_Protection_v2(sec_dat[i], track_num == 19, use_new_Headers).Item2;
            }
            int hlen = (((t_dens - (sec_dat.Where(x => x != null).Sum(x => x.Length) + t_sync + 15)) / sectors) >> 1) - 1;
            using (var buffer = new MemoryStream())
            using (var write = new BinaryWriter(buffer))
            {
                for (int i = 0; i < sectors; i++)
                {
                    if (sec_dat[i]?.Length > 0)
                    {
                        if ((i == 0 && syncless && !addSync) || !syncless || addSync) write.Write(v2_sync_marker);
                        write.Write(ArrayConcat(Build_Header(header[i], hlen), sec_dat[i]));
                    }
                }
                write.Write(t_ID);
                if (t_dens - (int)buffer.Position > 0) write.Write(FastArray.Init(t_dens - (int)buffer.Position, 0x55));
                return (buffer.ToArray(), 0, (int)buffer.Length, sectors);
            }

            byte[] Build_Header(byte[] ID, int len)
            {
                var secn = FastArray.Init(len << 1, ID[0]);
                for (int i = 0; i < len << 1; i++) if (i % 2 == 1) secn[i] = ID[1];
                return ArrayConcat(new byte[] { header1 }, secn, new byte[] { header2 });
            }
        }

        (bool, byte[]) Find_Cart_Protection_v2(byte[] data, bool t19s14, bool use_newer_GCR = false)
        {
            if (data == null) return (false, null);
            //bool older = !use_newer_GCR;
            bool older = use_newer_GCR ? false : true;
            //if (!(V2_swap_headers.Visible && V2_swap_headers.Checked))
            if (!use_newer_GCR)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0xe2 || data[i] == 0xa3)
                    {
                        older = true;
                        break;
                    }
                }
            }
            byte[] temp = Decode_VmaxGCR(data);
            if (t19s14) // Do Compressed Search & Replace
            {
                byte[][] search = new byte[3][];
                byte[][] replace = new byte[3][];
                /// Patch bytes (Search for / Repplace with)
                byte[] offsets = new byte[] { 0x88, 0xd7, 0x76 };   // sector offsets for HCS, BSB, G / GDD
                search[0] = new byte[] { 0x9c, 0x38 };  // Harrier Combat Simulator
                replace[0] = new byte[] { 0x15, 0x0b };
                search[1] = new byte[] { 0x63, 0xd0 };  // Bad Street Brawler
                replace[1] = new byte[] { 0x66, 0x00 };
                search[2] = new byte[] { 0xd2, 0xb4 };  // Gauntlet / Gauntlet Deeper Dungeons
                replace[2] = new byte[] { 0xd8, 0x10 };
                for (int i = 0; i < search.Length; i++)
                {
                    var srch = CopyArray(temp, offsets[i], 2);
                    if (MatchSeq(search[i], srch))
                    {
                        Buffer.BlockCopy(replace[i], 0, temp, offsets[i], 2);
                        return (true, Encode_VmaxGCR(temp, true, older));
                    }
                }
            }
            // Code only reachable if Compressed Search failed.  Now doing Uncompressed Search
            for (int i = 0; i < temp.Length; i++)
            {
                if (MatchSeq(temp, cart_patch_v2, i) && i > 2)
                {
                    int pos = i + cart_patch_v2.Length;
                    Buffer.BlockCopy(temp, pos + 1, temp, pos - 2, 2);
                    return (true, Encode_VmaxGCR(temp, true, older));
                }
            }

            return (false, data);
        }

        (byte[], int, int, int, int, string[], int, int, byte[], byte[][], bool) Get_V2_Track_Info(byte[] data, int trk, bool cartP)
        {
            int tr = (tracks > 42) ? (trk / 2) + 1 : trk + 1;
            int data_start = 0, data_end = 0, sec_zero = 0, pos = 0, vs = 0, co = 0;
            bool start_found = false, end_found = false, found = false;
            byte[] start_byte = new byte[1];
            byte[] end_byte = new byte[1];
            byte[] ignore = new byte[] { 0x7e, 0x7f, 0xff, 0x5f, 0xbf, 0x57 };
            byte[] m = new byte[6];
            List<string> all_headers = new List<string>();
            List<string> headers = new List<string>();
            var err = new List<int>();
            int secsize = 320 << 3;
            string ver = string.Empty;
            string snc = " *(Syncless)";
            byte comp = 0;
            byte[] sb = new byte[] { 0x64, 0x4e };
            byte[] eb = new byte[] { 0x46, 0x64, 0x4e };

            byte[][] sec_data = new byte[22][];
            //List<byte[]> sec_data = new List<byte[]>();

            BitArray source = new BitArray(Flip_Endian(data));
            if (!batch) all_headers.Add($"Track {tr} Format : {secF[NDS.cbm[trk]]} {ver} {snc}");
            while (pos < source.Length)
            {
                comp <<= 1;
                if (source[pos]) comp |= 1;
                if (sb.Any(x => x == comp))
                {
                    try
                    {
                        var a = Bit2Byte(source, pos - 7, Math.Min(60 << 3, source.Length - pos));
                        int sec = a[1] ^ a[2];
                        string hd = Hex_Val(new byte[] { comp, a[1], a[2] });
                        if (!headers.Contains(hd) && sec < 22 && VM2_Valid.Any(x => x == a[1]) && a[3] == a[1] && a[4] == a[2])
                        {
                            headers.Add(hd);
                            int hlen = 0;
                            if (!found) Get_Header_Bytes(a);
                            if (found && !start_found) start_found = true;
                            if (ver == string.Empty) Check_Ver(a);
                            if (co <= 11 && pos >= 15)
                            {
                                if (ignore.Any(s => s == Bit2Byte(source, pos - 15, 8)[0])) co++;
                                if (co > 10) { m[4] = 1; snc = string.Empty; }
                            }
                            while (a[hlen] != end_byte[0]) hlen++;
                            var newpos = pos + 1 + (hlen << 3);
                            sec_data[sec] = Bit2Byte(source, newpos, 320 << 3); ;
                            byte[] f = new byte[0];
                            bool t19s14 = (tr == 19 && (a[1] ^ a[2]) == 14);
                            if (!cartP) (cartP, f) = Find_Cart_Protection_v2(sec_data[sec], t19s14);
                            if (!batch)
                            {
                                string sz = sec == 0 ? "*" : string.Empty;
                                if (sec == 0) sec_zero = (pos - 7) >> 3;
                                if (newpos + secsize < source.Length)
                                {
                                    bool cksm = Get_Checksum(Decode_VmaxGCR(sec_data[sec]));
                                    if (!cksm) err.Add(sec);
                                    var dhead = new byte[] { start_byte[0], a[1], a[2], end_byte[0] };
                                    all_headers.Add($"Sector ({sec}){sz} pos ({pos >> 3}) Header [ {Hex_Val(dhead)} ] Checksum ({(cksm ? "OK" : "Failed!")})");
                                }
                            }
                            pos += secsize + ((hlen - 1) << 3); // secsize;
                        }
                        else if (headers.Contains(hd))
                        {
                            data_end = pos - 7;
                            end_found = true;
                            if (!batch)
                            {
                                all_headers.Add($"pos {(pos - 7) >> 3} ** Repeat ** sector {sec}");
                                all_headers.Add($"Track Length ({(data_end - data_start) >> 3}) Sectors ({headers.Count}) Sector 0 ({sec_zero}) Header length ({m[2] + 2})");
                                all_headers.Add(" ");
                            }
                        }
                        if (end_found) break;
                    }
                    catch { }
                }
                pos++;
            }

            if (data_end < data_start) data_end = source.Length;
            byte[] tmpdata = Bit2Byte(source, data_start, data_end - data_start);

            byte[] tdata = new byte[8192];
            try
            {
                Buffer.BlockCopy(tmpdata, 0, tdata, 0, tmpdata.Length);
                Buffer.BlockCopy(tmpdata, 0, tdata, tmpdata.Length, 8192 - tmpdata.Length);
            }
            catch { }
            if (!batch && err.Count > 0) foreach (var e in err) ErrorList.Add($"Checksum failed on track {tr}, sector {e}");
            return (tdata, data_start >> 3, data_end >> 3, sec_zero >> 3, tmpdata.Length << 3, all_headers.ToArray(), headers.Count, 0, m, sec_data.ToArray(), cartP);

            void Get_Header_Bytes(byte[] hdr)
            {
                start_byte[0] = hdr[0];
                m[0] = hdr[0];
                sb = new byte[] { hdr[0] };
                for (int i = 1; i < hdr.Length; i++)
                {
                    if (eb.Any(x => x == hdr[i]))
                    {
                        end_byte[0] = hdr[i];
                        eb = new byte[] { hdr[i] };
                        m[1] = hdr[i];
                        found = true;
                        m[2] = (byte)(i - 1);
                    }
                }
            }

            void Check_Ver(byte[] hdr)
            {
                for (int i = 0; i < v_check.Length; i++)
                {
                    if (Check_Version($"{Hex_Val(start_byte)}-{v_check[i]}", hdr, 3))
                    {
                        if (i < 2) { ver = "(older)"; vs = 1; } else { ver = "(newer)"; vs = 0; }
                        break;
                    }
                }
                m[3] = (byte)vs;
            }

            bool Get_Checksum(byte[] d)
            {
                if (d == null) return false;
                int csm = 0;
                foreach (byte b in d) csm ^= b;
                return csm == 0;
            }
        }

        byte[] Adjust_V2_Sync(byte[] data, int data_start, int data_end, byte[] t_info, bool Fix_Sync, int trk = -1)
        {
            if (trk < 0) trk = 0;
            byte[] temp_data = new byte[data_end - data_start];
            byte[] start_byte = { t_info[0] };
            byte[] end_byte = { t_info[1] };
            byte[] compare = new byte[4];
            byte[] pattern = FastArray.Init(3, 0xa5);
            byte[] ignore = new byte[] { 0x7e, 0x7f, 0xff, 0x5f, 0xbf, 0x57, 0x5b }; /// possible sync markers to ignore when building track
            bool st = (t_info[4] == 0);
            int head_len = Convert.ToInt32(t_info[2]);
            int sec_zero;
            byte[] find = FastArray.Init(4, 0xa5);
            find[0] = start_byte[0];
            int vs = Convert.ToInt32(t_info[3]);
            try { Buffer.BlockCopy(data, data_start, temp_data, 0, data_end - data_start); } catch { }
            for (int i = 0; i < temp_data.Length - 5; i++)
            {
                if (temp_data[i] == find[0])
                {
                    if (MatchSeq(temp_data, find, i))
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
            }
            if (Fix_Sync) /// <- if the "Fix_Sync" bool is true, otherwise just return track info without any adjustments
            {
                /// ---------------------- Build new track with adjusted sync markers -------------------------------------- //
                var s_pos = 0;
                /// Set the length of the sector header in multiples of 2 including the start and end marker.  Minimum = 6
                var sector_header = Convert.ToInt32((V2_hlen.Value - 2) * 2) / 2;
                if (V2_Auto_Adj.Checked) sector_header = head_len;
                byte[] sec_header = new byte[0];
                byte[] secz = { 0xa5, 0xa5 };
                bool no_sync = false;
                compare = new byte[2];
                /// begin processing the track
                bool sf = false;
                byte[] chk = new byte[1];
                using (var buffer = new MemoryStream())
                using (var write = new BinaryWriter(buffer))
                {
                    while (s_pos < temp_data.Length)
                    {
                        try
                        {
                            if (s_pos + 2 < temp_data.Length && temp_data[s_pos] == start_byte[0] && VM2_Valid.Any(s => s == temp_data[s_pos + 1]))  // s_pos + 2 
                            {
                                sf = false;
                                var m = 0;
                                byte[] header_ID = new byte[2];
                                if (s_pos + 3 < temp_data.Length - 1) Buffer.BlockCopy(temp_data, s_pos + 2, header_ID, 0, 2); // s_pos + 4, s_pos + 3
                                while (temp_data[s_pos] != start_byte[0]) m++; // s_pos++;
                                s_pos += m + 1; /// sets source position 1 byte after the header start byte to get the header pattern data
                                Buffer.BlockCopy(temp_data, s_pos, compare, 0, compare.Length);

                                if (vm2_ver[vs].Any(s => s == Hex_Val(compare))) // <- checks to verify header pattern is in the list of valid headers
                                {
                                    /// check that it's not sector 0 which needs sync, then check if a sync marker is before the header start byte.  If not, its a syncless track
                                    if (!V2_Add_Sync.Checked)
                                    {
                                        if (compare != secz && (!vm2_pos_sync.Any(s => s == temp_data[s_pos - 2])) && temp_data[s_pos - 1] == start_byte[0]) no_sync = true;
                                        else no_sync = false;
                                    }
                                    var header_length = 0;
                                    while (s_pos < temp_data.Length && temp_data[s_pos] != end_byte[0]) /// <- getting the length of the header pattern
                                    {
                                        s_pos++; header_length++;
                                    }
                                    s_pos++;
                                    if (V2_Custom.Checked) header_length = sector_header;

                                    if (!no_sync)
                                    {
                                        buffer.Seek(buffer.Length, SeekOrigin.Begin);
                                        buffer.Read(chk, 0, 1);
                                        write.Write(v2_sync_marker); /// <- Here's where we add the sync (unless its a syncless track)
                                    }
                                    write.Write(Build_Header(start_byte, end_byte, compare, ((header_length) << 1) >> 1)); /// building new header and writing to buffer
                                }
                            }
                        }
                        catch { }
                        if (s_pos < temp_data.Length && !ignore.Any(s => s == temp_data[s_pos]))
                        {
                            if (!sf) write.Write(temp_data[s_pos]); /// <- loop writes sector data to the buffer until it hits another header
                            if (temp_data[s_pos] == 0x7f) sf = true;
                        }
                        s_pos++;
                    }
                    //bool found = false;
                    //(found, sec_zero) = Find_Data(ArrayConcat(start_byte, Hex2Byte(vm2_ver[vs][0])), data, 3);
                    sec_zero = Find_Data(ArrayConcat(start_byte, Hex2Byte(vm2_ver[vs][0])), data, 3).Item2;
                    return buffer.ToArray(); /// <- Return new array with sync markers adjusted
                }

                byte[] Build_Header(byte[] s, byte[] e, byte[] f, int len)
                {
                    using (var buff = new MemoryStream())
                    using (var wrt = new BinaryWriter(buff))
                    {
                        wrt.Write((byte)s[0]);
                        for (int i = 0; i < (len / 2); i++) wrt.Write(f);
                        wrt.Write((byte)e[0]);
                        return buff.ToArray();
                    }
                }
            }
            else return temp_data; /// <- Return array without any adjustments to sync
        }
    }
}