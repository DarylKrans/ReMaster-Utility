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
        private static readonly byte[] v2_sync_marker = { 0x7f, 0xff }; /// 0x5b, 0xff (known working)
        private static readonly string[][] vm2_ver = new string[2][];
        private static readonly string[] v_check = { "A5-A3", "A9-A3", "AD-AB", "AD-A7" };
        private static readonly byte[] VM2_Valid = { 0xa5, 0xa4, 0xa9, 0xaC, 0xad, 0xb4, 0xbc };
        private static byte[] v2stub = new byte[0];
        private static readonly byte[] cart_patch_v2 = { 0x39, 0x00, 0xcd, 0xf1, 0xd7 };

        void GetNewHeaders()
        {
            if (V2_Fix_Weak.Checked) Disk.G64.NewHeader = new byte[] { 0x64, 0x4e };
            else Disk.G64.NewHeader = new byte[] { 0x64, 0x46 };
        }

        void Find_VMax_Sector_New(ref Disk_Track T, int version)
        {
            bool checksum = false;
            if (T.Bits == null) return; // (new byte[0], false, -1);
            BitArray source = T.Bits;
            if (version == 2)
            {
                byte sb = T.Spec.VMax.V2.HeaderStart; // new byte[] { 0x64, 0x4e };
                byte eb = T.Spec.VMax.V2.HeaderEnd; // new byte[] { 0x46, 0x4e, 0x64 };
                int pos = 0;
                byte compare = 0;
                while (pos < source.Length)
                {
                    compare <<= 1;
                    if (source[pos]) compare |= 1;
                    if (compare == sb)
                    {
                        try
                        {
                            bool t = false;
                            int dpos = 0;
                            var a = Bit2Byte(source, pos + 1, 5 << 3);
                            if (VM2_Valid.Any(x => x == a[0]) && a[2] == a[0] && a[3] == a[1]) t = true;
                            int sec = a[0] ^ a[1];
                            if (t && sec < 22 && (T.Sector.Count == 0 || !T.Sector.Any(x => x.ID == sec)))
                            {
                                var getsec = Bit2Byte(source, pos + 1, Math.Min(380 << 3, source.Length - pos));
                                while (getsec[dpos] != eb) dpos++;
                                T.Spec.VMax.V2.Length = (byte)dpos;
                                var secdata = CopyFrom(getsec, dpos + 1, 320);
                                var dec = Decode_VmaxGCR(secdata);
                                if (dec != null) checksum = Get_Checksum(dec);
                                T.Sector.Add(new Sector
                                {
                                    ID = a[0] ^ a[1],
                                    Data = new Sector.Info
                                    {
                                        GCR = secdata,
                                        Decoded = dec,
                                        Pos = dpos + 1,
                                        Checksum = checksum
                                    },
                                    Header = new Sector.Info
                                    {
                                        Pos = pos + 1
                                    }
                                });
                            }
                            else if (t && sec < 22) pos += (300 + dpos - 1) << 3;
                            else pos += 8;
                        }
                        catch { }
                    }
                    pos++;
                }
            }
            if (version == 3)
            {
                int pos = 0;
                uint compare = 0;
                while (pos < source.Length)
                {
                    compare <<= 1;
                    if (source[pos]) compare |= 1;
                    if ((((compare & 0xff0000) >> 16) != 0x49) && (compare & 0xffff) == 0x4949)
                    {
                        try
                        {
                            var header = Bit2Byte(source, pos - 15, (v3_max_header + 8) << 3);
                            int hlen = 0;
                            while (hlen < header.Length && header[hlen] == 0x49) hlen++;
                            if (header[hlen] == 0xee)
                            {
                                T.Spec.VMax.V3.HeaderLength = hlen;
                                int secpos = pos - 15 + ((hlen + 1) << 3);
                                byte[] cmp = Decode_VmaxGCR(Bit2Byte(source, secpos, 8 << 3));
                                int sec = cmp[0] & 0x1f;
                                if (T.Sector.Count == 0 || !T.Sector.Any(x => x.ID == sec))
                                {
                                    var tmp = Bit2Byte(source, secpos, Math.Min(285 << 3, source.Length - secpos));
                                    var secsize = Get_vm3_sectorSize(tmp);
                                    var secdata = CopyFrom(tmp, 0, secsize);
                                    var dec = Decode_VmaxGCR(secdata);
                                    var embsize = dec[5] << 2;
                                    if (embsize <= secsize)
                                    {
                                        if (dec != null) checksum = Get_Checksum(dec);
                                        T.Sector.Add(new Sector
                                        {
                                            ID = cmp[0] & 0x1f,
                                            Header = new Sector.Info
                                            {
                                                Pos = pos - 15
                                            },
                                            Data = new Sector.Info
                                            {
                                                Pos = secpos,
                                                GCR = secdata,
                                                Decoded = dec,
                                                Checksum = checksum
                                            }
                                        });
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    pos++;
                }
            }

            bool Get_Checksum(byte[] d)
            {
                int csm = 0;
                for (int j = 0; j < d.Length; j++) csm ^= d[j];
                return csm == 0;
            }
        }

        (byte[] sector, bool checksum, int pos) Find_VMax_Sector(byte[] data, BitArray source, int sector, int version, bool decode = false, int trk = -1)
        {
            bool checksum = false;
            if ((data == null && source == null) || sector < 0) return (new byte[0], false, -1);
            if (source == null || source.Count < 1) source = new BitArray(Flip_Endian(data));
            int track = tracks > 42 ? (trk / 2) + 1 : trk + 1;
            if (version == 2)
            {
                byte[] sb = new byte[] { 0x64, 0x4e };
                byte[] eb = new byte[] { 0x46, 0x4e, 0x64 };
                int pos = 0;
                byte compare = 0;
                // process as bitarray //
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
                                var secdata = CopyFrom(getsec, dpos + 1, 320);
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
                int pos = 0;
                uint compare = 0;
                while (pos < source.Length)
                {
                    compare <<= 1;
                    if (source[pos]) compare |= 1;
                    if ((((compare & 0xff0000) >> 16) != 0x49) && (compare & 0xffff) == 0x4949)
                    {
                        try
                        {
                            var header = Bit2Byte(source, pos - 15, (v3_max_header + 8) << 3);
                            int hlen = 0;
                            while (hlen < header.Length && header[hlen] == 0x49) hlen++;
                            if (header[hlen] == 0xee)
                            {
                                int secpos = pos - 15 + ((hlen + 1) << 3);
                                byte[] cmp = Decode_VmaxGCR(Bit2Byte(source, secpos, 8 << 3));
                                if ((cmp[0] & 0x1f) == sector)
                                {
                                    var tmp = Bit2Byte(source, secpos, Math.Min(285 << 3, source.Length - secpos));
                                    var secsize = Get_vm3_sectorSize(tmp);
                                    var embsize = (Decode_VmaxGCR(CopyFrom(tmp, 0, 8))[5] << 2);
                                    if (embsize <= secsize)
                                    {
                                        var secdata = CopyFrom(tmp, 0, secsize);
                                        var dec = Decode_VmaxGCR(secdata);
                                        if (dec != null) checksum = Get_Checksum(dec);
                                        return (decode ? dec : secdata, checksum, secpos);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    pos++;
                }
            }
            return (new byte[0], false, -1);

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
                    if (Disk.Source.Track[t].Format == 4) if (Disk.Original.TrackData[t].Length == 0) Disk.Original.TrackData[t] = CopyFrom(Disk.G64.Track[t].Data);
                }
            }
            else
            {
                for (int t = 0; t < tracks; t++)
                {
                    if (Disk.Source.Track[t].Format == 4 || Disk.Source.Track[t].Format == 1)
                    {
                        if (Disk.Original.TrackData[t].Length != 0)
                        {
                            Disk.G64.Track[t].Data = CopyFrom(Disk.Original.TrackData[t]);
                            Disk.Adjusted.Track[t].Data = FillArray(Disk.Original.TrackData[t], 8192);
                        }
                        Disk.G64.Track[t].Length = Disk.G64.Track[t].Data.Length;
                        Disk.Adjusted.Track[t].Length = Disk.G64.Track[t].Length << 3;
                    }
                }
            }
            int i = Convert.ToInt32(V2_hlenD0.Value);
            if (i >= V2_hlenD0.Minimum && i <= V2_hlenD0.Maximum)
            {
                Clear_Out_Items();
                Process_Nib_Data(true, false, !V2_Auto_Adj.Checked, true);
            }
        }

        //byte[] Rebuild_V2(ref Disk_Track T, byte[] data, ref Disk_Track A, byte[] new_header, bool use_new_Headers = false)
        //{
        //    int sectors = T.Sectors;
        //    byte[] t_info = T.Spec.VMax.V2.GetV2Info();
        //    /// t_info[0] = start byte, t_info[1] = end byte, t_info[2] = header length, t_info[3] = v-max version (for sector headers)
        //    int track_num = (int)T.TrackNumber; // tracks > 42 ? (trk / 2) + 1 : trk + 1;
        //    (var found, var pos) = Find_Data(new byte[] { t_info[0], 0xa5, 0xa5 }, data);
        //    if (found) data = Rotate_Left(data, pos - 1);
        //    else
        //    {
        //        int gap = FindTrackGap(data);
        //        if (gap > 0) data = Rotate_Left(data, gap);
        //    }
        //    bool syncless = t_info[4] == 0x00;
        //    bool addSync = V2_Add_Sync.Checked;
        //    int t_dens = density[vm2_density_map[track_num - 1]];
        //    int t_sync = syncless && !addSync ? v2_sync_marker.Length : v2_sync_marker.Length * sectors;
        //    byte[][] sec_dat = new byte[sectors][];
        //    byte[][] header = new byte[sectors][];
        //    byte[] t_ID = track_num % 2 == 1 ? ArrayConcat(v2_sync_marker, new byte[] { 0xff, 0xff },
        //        Build_BlockHeader(track_num, 255, Disk.DiskID)) : new byte[] { 0x7f };
        //    byte header1 = use_new_Headers ? new_header[0] : t_info[0];
        //    byte header2 = use_new_Headers ? new_header[1] : t_info[1];
        //    List<string> sf = new List<string>();
        //    for (int i = 0; i < sectors; i++)
        //    {
        //        if (!use_new_Headers)
        //        {
        //            header[i] = Hex2Byte(vm2_ver[t_info[3]][i]);
        //            sec_dat[i] = T.Sector[i].Data.GCR;
        //        }
        //        else
        //        {
        //            bool older = header2 == 0x46;
        //            header[i] = Hex2Byte(vm2_ver[0][i]);
        //            sec_dat[i] = CopyArray(T.Sector[i].Data.GCR);
        //            for (int j = 0; j < sec_dat[i].Length; j++)
        //            {
        //                if (sec_dat[i][j] == 0xa3) sec_dat[i][j] = 0xad;
        //                if (sec_dat[i][j] == 0xe2) sec_dat[i][j] = 0xea;
        //            }
        //        }
        //        //if (track_num == 19 && i == 14 && P_Cart.Checked) sec_dat[i] = Find_Cart_Protection_v2_Compressed(sector_data[i], true, use_new_Headers).Item2;
        //        if (P_Cart.Checked || batch) sec_dat[i] = Find_Cart_Protection_v2(sec_dat[i], track_num == 19, use_new_Headers).Item2;
        //    }
        //    int hlen = (((t_dens - (sec_dat.Where(x => x != null).Sum(x => x.Length) + t_sync + 15)) / sectors) >> 1) - 1;
        //    using (var buffer = new MemoryStream())
        //    using (var write = new BinaryWriter(buffer))
        //    {
        //        for (int i = 0; i < sectors; i++)
        //        {
        //            if (sec_dat[i]?.Length > 0)
        //            {
        //                if ((i == 0 && syncless && !addSync) || !syncless || addSync) write.Write(v2_sync_marker);
        //                write.Write(ArrayConcat(Build_Header(header[i], hlen), sec_dat[i]));
        //            }
        //        }
        //        write.Write(t_ID);
        //        if (t_dens - (int)buffer.Position > 0) write.Write(FastArray.Init(t_dens - (int)buffer.Position, 0x55));
        //        //return (buffer.ToArray(), 0, (int)buffer.Length, Sectors);
        //        A.Start = 0; A.End = (int)buffer.Length; A.SectorZero = sectors;
        //        return buffer.ToArray();
        //    }
        //
        //    byte[] Build_Header(byte[] ID, int len)
        //    {
        //        var secn = FastArray.Init(len << 1, ID[0]);
        //        for (int i = 0; i < len << 1; i++) if (i % 2 == 1) secn[i] = ID[1];
        //        return ArrayConcat(new byte[] { header1 }, secn, new byte[] { header2 });
        //    }
        //}

        (byte[], int, int, int) Rebuild_V2(byte[] data, int sectors, byte[] t_info, int trk, byte[] new_header, byte[][] sector_data, bool use_new_Headers = false)
        {
            /// t_info[0] = start byte, t_info[1] = end byte, t_info[2] = header length, t_info[3] = v-max version (for sector headers)
            int track_num = tracks > 42 ? (trk / 2) + 1 : trk + 1;
            //File.WriteAllText($@"c:\test\trk{trk}.txt", $"tracks {tracks} tnum {track_num} len, {data.Length}");
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
                Build_BlockHeader(track_num, 255, Disk.DiskID)) : new byte[] { 0x7f };
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
                if (P_Cart.Checked || batch) sec_dat[i] = Find_Cart_Protection_v2(sec_dat[i], track_num == 19, use_new_Headers).Item2;
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

        //void Get_V2_Track_Info(ref Disk_Track T, ref bool cartP)
        //{
        //    int tr = (int)T.TrackNumber; // (tracks > 42) ? (trk / 2) + 1 : trk + 1;
        //    int pos = 0, vs = 0, syncs_found = 0, ds = 0;
        //    bool start_found = false, end_found = false, found = false;
        //    byte start_byte = 0, end_byte = 0;
        //    List<string> all_headers = new List<string>();
        //    List<string> headers = new List<string>();
        //    var err = new List<int>();
        //    int secsize = 320 << 3;
        //    string ver = string.Empty;
        //    string snc = " *(Single Sync)";
        //    uint comp = 0;
        //    byte[] sb = new byte[] { 0x64, 0x4e };
        //    byte[] eb = new byte[] { 0x46, 0x64, 0x4e };
        //    BitArray source = T.Bits;// new BitArray(Flip_Endian(data));
        //    all_headers.Add($"Track {tr} Format : {secF[T.Format]} {ver}");
        //    while (pos < source.Length)
        //    {
        //        comp <<= 1;
        //        if (source[pos]) comp |= 1;
        //        if (sb.Any(x => x == (byte)(comp & 0xff)))
        //        {
        //            try
        //            {
        //                var a = Bit2Byte(source, pos - 7, Math.Min(60 << 3, source.Length - pos));
        //                int sec = a[1] ^ a[2];
        //                string hd = Hex_Val(new byte[] { (byte)(comp & 0xff), a[1], a[2] });
        //                if (!headers.Contains(hd) && sec < 22 && VM2_Valid.Any(x => x == a[1]) && a[3] == a[1] && a[4] == a[2])
        //                {
        //                    Sector s = new Sector();
        //                    s.Header.Pos = pos - 7;
        //                    s.ID = sec;
        //
        //                    headers.Add(hd);
        //                    int hlen = 0;
        //                    if (!found) Get_Header_Bytes(ref T, a);
        //                    if (found && !start_found)
        //                    {
        //                        start_found = true;
        //                        T.Start = pos - 7;
        //                    }
        //                    if (ver == string.Empty) Check_Ver(ref T, a);
        //                    // ------ check for sync pattern ---------
        //                    byte sb0 = (byte)((comp >> 16) & 0xff);
        //                    byte sb1 = (byte)((comp >> 8) & 0xff);
        //                    if (((sb0 & 0x07) == 0x03 && sb1 == 0xff)
        //                        || ((sb0 == 0x5b || sb0 == 0x7f || sb0 == 0xff) && (sb1 == 0xff || sb1 == 0x7f))) syncs_found++;
        //                    if (syncs_found > 10)
        //                    {
        //                        T.Spec.VMax.V2.MultiSync = 0x01;
        //                        snc = string.Empty;
        //                    }
        //                    // ---------------------------------------
        //                    while (a[hlen] != end_byte) hlen++;
        //                    s.Data.Pos = pos + 1 + (hlen << 3); // newpos;
        //                    s.Data.GCR = Bit2Byte(source, s.Data.Pos, 320 << 3);
        //                    byte[] f = new byte[0];
        //                    bool t19s14 = (tr == 19 && (a[1] ^ a[2]) == 14);
        //                    if (!cartP) (cartP, f) = Find_Cart_Protection_v2(s.Data.GCR, t19s14);
        //                    string sz = sec == 0 ? "*" : string.Empty;
        //                    if (sec == 0)
        //                    {
        //                        T.SectorZero = (pos - 7) >> 3;
        //                        ds = pos - Math.Min(7 + (8 << 3), pos - 7);
        //                    }
        //                    if (s.Data.Pos + secsize < source.Length)
        //                    {
        //                        s.Data.Decoded = Decode_VmaxGCR(s.Data.GCR);
        //                        s.Data.Checksum = Get_Checksum(s.Data.Decoded);
        //                        if (!s.Data.Checksum) err.Add(sec);
        //                        var dhead = new byte[] { start_byte, a[1], a[2], end_byte };
        //                        all_headers.Add($"Sector ({sec}){sz} pos ({pos >> 3}) Header [ {Hex_Val(dhead)} ] Checksum ({(s.Data.Checksum ? "OK" : "Failed!")})");
        //                    }
        //                    pos += secsize + ((hlen - 1) << 3); // secsize;
        //                }
        //                else if (headers.Contains(hd))
        //                {
        //                    T.End = pos - 7;
        //                    end_found = true;
        //                    if (!batch)
        //                    {
        //                        all_headers.Add($"pos {(pos - 7) >> 3} ** Repeat ** sector {sec}");
        //                        all_headers.Add($"Track Length ({(T.End - T.Start) >> 3}) Sectors ({headers.Count}) Sector 0 ({T.SectorZero}) Header length ({T.Spec.VMax.V2.Length + 2})");
        //                        all_headers.Add(" ");
        //                    }
        //                }
        //                if (end_found) break;
        //            }
        //            catch { }
        //        }
        //        pos++;
        //    }
        //    all_headers[0] += $" {snc}";
        //
        //    if (T.End < T.Start) T.End = source.Length;
        //
        //    byte[] tmpdata = Bit2Byte(source, T.Start, T.End - T.Start);
        //    int rotate = FindTrackGap(tmpdata, true, new byte[] { 0x64 });
        //    if (rotate > 0) tmpdata = Rotate_Left(tmpdata, rotate);
        //    byte[] tdata = FillArray(tmpdata ?? (new byte[0]), 8192);
        //    if (!batch && err.Count > 0) foreach (var e in err) ErrorList.Add($"Checksum failed on track {tr}, sector {e}");
        //    T.Info = headers.ToArray();
        //    //return (tdata, data_start >> 3, data_end >> 3, sec_zero >> 3, tmpdata.Length << 3, all_headers.ToArray(), headers.Count, 0, track_info, sec_data.ToArray(), cartP);
        //
        //    void Get_Header_Bytes(ref Disk_Track _T, byte[] hdr)
        //    {
        //        start_byte = hdr[0];
        //        _T.Spec.VMax.V2.HeaderStart = hdr[0];
        //        sb = new byte[] { hdr[0] };
        //        for (int i = 1; i < hdr.Length; i++)
        //        {
        //            if (eb.Any(x => x == hdr[i]))
        //            {
        //                end_byte = hdr[i];
        //                eb = new byte[] { hdr[i] };
        //                _T.Spec.VMax.V2.HeaderEnd = hdr[i];
        //                _T.Spec.VMax.V2.Length = (byte)(i - 1);
        //                found = true;
        //            }
        //        }
        //    }
        //
        //    void Check_Ver(ref Disk_Track _T, byte[] hdr)
        //    {
        //        for (int i = 0; i < v_check.Length; i++)
        //        {
        //            if (Check_Version($"{Hex_Val(new byte[] { start_byte })}-{v_check[i]}", hdr, 3))
        //            {
        //                if (i < 2) { ver = "(older)"; vs = 1; } else { ver = "(newer)"; vs = 0; }
        //                break;
        //            }
        //        }
        //        _T.Spec.VMax.V2.Version = (byte)vs;
        //    }
        //
        //    bool Get_Checksum(byte[] d)
        //    {
        //        if (d == null) return false;
        //        int csm = 0;
        //        foreach (byte b in d) csm ^= b;
        //        return csm == 0;
        //    }
        //}

        (byte[], int, int, int, int, string[], int, int, byte[], byte[][], bool) Get_V2_Track_Info(byte[] data, int trk, bool cartP)
        {
            int tr = (tracks > 42) ? (trk / 2) + 1 : trk + 1;
            int data_start = 0, data_end = 0, sec_zero = 0, pos = 0, vs = 0, syncs_found = 0, ds = 0;
            bool start_found = false, end_found = false, found = false;
            byte start_byte = 0, end_byte = 0;
            byte[] track_info = new byte[6];
            List<string> all_headers = new List<string>();
            List<string> headers = new List<string>();
            var err = new List<int>();
            int secsize = 320 << 3;
            string ver = string.Empty;
            string snc = " *(Single Sync)";
            uint comp = 0;
            byte[] sb = new byte[] { 0x64, 0x4e };
            byte[] eb = new byte[] { 0x46, 0x64, 0x4e };
            byte[][] sec_data = new byte[22][];
            BitArray source = new BitArray(Flip_Endian(data));
            all_headers.Add($"Track {tr} Format : {secF[Disk.Source.Track[trk].Format]} {ver}");
            while (pos < source.Length)
            {
                comp <<= 1;
                if (source[pos]) comp |= 1;
                if (sb.Any(x => x == (byte)(comp & 0xff)))
                {
                    try
                    {
                        var a = Bit2Byte(source, pos - 7, Math.Min(60 << 3, source.Length - pos));
                        int sec = a[1] ^ a[2];
                        string hd = Hex_Val(new byte[] { (byte)(comp & 0xff), a[1], a[2] });
                        if (!headers.Contains(hd) && sec < 22 && VM2_Valid.Any(x => x == a[1]) && a[3] == a[1] && a[4] == a[2])
                        {
                            headers.Add(hd);
                            int hlen = 0;
                            if (!found) Get_Header_Bytes(a);
                            if (found && !start_found)
                            {
                                start_found = true;
                                data_start = pos - 7;
                            }
                            if (ver == string.Empty) Check_Ver(a);
                            // ------ check for sync pattern ---------
                            byte sb0 = (byte)((comp >> 16) & 0xff);
                            byte sb1 = (byte)((comp >> 8) & 0xff);
                            if (((sb0 & 0x07) == 0x03 && sb1 == 0xff)
                                || ((sb0 == 0x5b || sb0 == 0x7f || sb0 == 0xff) && (sb1 == 0xff || sb1 == 0x7f))) syncs_found++;
                            if (syncs_found > 10)
                            {
                                track_info[4] = 0x01;           // value of 1 means track contains sync before each sector
                                snc = string.Empty;
                            }
                            // ---------------------------------------
                            while (a[hlen] != end_byte) hlen++;
                            var newpos = pos + 1 + (hlen << 3);
                            sec_data[sec] = Bit2Byte(source, newpos, 320 << 3);
                            byte[] f = new byte[0];
                            bool t19s14 = (tr == 19 && (a[1] ^ a[2]) == 14);
                            if (!cartP) (cartP, f) = Find_Cart_Protection_v2(sec_data[sec], t19s14);
                            string sz = sec == 0 ? "*" : string.Empty;
                            if (sec == 0)
                            {
                                sec_zero = (pos - 7) >> 3;
                                ds = pos - Math.Min(7 + (8 << 3), pos - 7);
                            }
                            if (newpos + secsize < source.Length)
                            {
                                bool cksm = Get_Checksum(Decode_VmaxGCR(sec_data[sec]));
                                if (!cksm) err.Add(sec);
                                var dhead = new byte[] { start_byte, a[1], a[2], end_byte };
                                all_headers.Add($"Sector ({sec}){sz} pos ({pos >> 3}) Header [ {Hex_Val(dhead)} ] Checksum ({(cksm ? "OK" : "Failed!")})");
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
                                all_headers.Add($"Track Length ({(data_end - data_start) >> 3}) Sectors ({headers.Count}) Sector 0 ({sec_zero}) Header length ({track_info[2] + 2})");
                                all_headers.Add(" ");
                            }
                        }
                        if (end_found) break;
                    }
                    catch { }
                }
                pos++;
            }
            all_headers[0] += $" {snc}";

            if (data_end < data_start) data_end = source.Length;

            byte[] tmpdata = Bit2Byte(source, data_start, data_end - data_start);
            int rotate = FindTrackGap(tmpdata, true, new byte[] { 0x64 });
            if (rotate > 0) tmpdata = Rotate_Left(tmpdata, rotate);
            byte[] tdata = FillArray(tmpdata ?? (new byte[0]), 8192);
            if (!batch && err.Count > 0) foreach (var e in err) ErrorList.Add($"Checksum failed on track {tr}, sector {e}");
            return (tdata, data_start >> 3, data_end >> 3, sec_zero >> 3, tmpdata.Length << 3, all_headers.ToArray(), headers.Count, 0, track_info, sec_data.ToArray(), cartP);

            void Get_Header_Bytes(byte[] hdr)
            {
                start_byte = hdr[0];
                track_info[0] = hdr[0];
                sb = new byte[] { hdr[0] };
                for (int i = 1; i < hdr.Length; i++)
                {
                    if (eb.Any(x => x == hdr[i]))
                    {
                        end_byte = hdr[i];
                        eb = new byte[] { hdr[i] };
                        track_info[1] = hdr[i];
                        found = true;
                        track_info[2] = (byte)(i - 1);
                    }
                }
            }

            void Check_Ver(byte[] hdr)
            {
                for (int i = 0; i < v_check.Length; i++)
                {
                    if (Check_Version($"{Hex_Val(new byte[] { start_byte })}-{v_check[i]}", hdr, 3))
                    {
                        if (i < 2) { ver = "(older)"; vs = 1; } else { ver = "(newer)"; vs = 0; }
                        break;
                    }
                }
                track_info[3] = (byte)vs;
            }

            bool Get_Checksum(byte[] d)
            {
                if (d == null) return false;
                int csm = 0;
                foreach (byte b in d) csm ^= b;
                return csm == 0;
            }
        }

        //byte[] Adjust_V2_Sync(ref Disk_Track T, bool Fix_Sync, bool fix_weak = false, bool patch_Cart = false, int trk = -1)
        //{
        //    if (T.Data == null || T.Data.Length == 0) return null;
        //    int track = (int)T.TrackNumber;
        //    //int track = tracks > 42 ? (trk >> 1) + 1 : trk + 1;
        //    int dens = track > 17 ? density[1] : density[0];
        //    byte start_byte = T.Spec.VMax.V2.HeaderStart;
        //    byte end_byte = T.Spec.VMax.V2.HeaderEnd;
        //    int head_len = T.Spec.VMax.V2.Length;
        //    int vs = Convert.ToInt32(T.Spec.VMax.V2.Version);
        //    bool st = T.Spec.VMax.V2.MultiSync == 0;
        //    BitArray source = new BitArray(T.Bits);
        //    int r = 0;
        //    source = new BitArray(Flip_Endian(Rotate_Left(Bit2Byte(source), r)));
        //    byte[] temp_data = Bit2Byte(source);
        //
        //    if (Fix_Sync) /// <- if the "Fix_Sync" bool is true, otherwise just return track info without any adjustments
        //    {
        //        byte[] ignore = new byte[] { 0x7e, 0x7f, 0xff, 0x5f, 0xbf, 0x57, 0x5b }; /// possible sync markers to ignore when building track
        //        byte[] compare = new byte[2];
        //        /// begin processing the track
        //        byte[] chk = new byte[1];
        //        byte[] secz = { 0xa5, 0xa5 };
        //        var sector_header = track < 18 ? Convert.ToInt32((V2_hlenD0.Value - 2) * 2) / 2 : Convert.ToInt32((V2_hlenD1.Value - 2) * 2) / 2;
        //        byte[] pre = new byte[0];  // Capture data pre-first sector
        //        byte[] post = new byte[0]; // Capture data post-last sector
        //        byte newendbyte = end_byte;
        //        List<VM> info = new List<VM>();
        //        uint cmp = 0;
        //        int posi = 0;
        //        int snc = 0, times = 0;
        //        bool dbl = false, addsnc = V2_Add_Sync.Checked;
        //        byte[] curhead = new byte[0];
        //        bool cursnc = false;
        //        while (posi < source.Length)
        //        {
        //            cmp <<= 1;
        //            if (source[posi])
        //            {
        //                cmp |= 1;
        //                snc++;
        //                if (snc == 10)
        //                {
        //                    snc = 0; times++;
        //                    if (times > 2) times = 0;
        //                }
        //            }
        //            else snc = 0;
        //            if ((cmp & 0xff) == start_byte)
        //            {
        //
        //                try
        //                {
        //                    var a = Bit2Byte(source, posi - 7, Math.Min(60 << 3, source.Length - posi));
        //                    int sec = a[1] ^ a[2];
        //                    if (sec < 22 && VM2_Valid.Any(x => x == a[1]) && a[3] == a[1] && a[4] == a[2])
        //                    {
        //                        if (info.Count < 1)
        //                        {
        //                            try
        //                            {
        //                                pre = Bit2Byte(source, 0, posi - (3 << 3));
        //                                if ((pre[pre.Length - 1] & 0x01) != 1) pre[pre.Length - 1] |= 1;
        //                            }
        //                            catch { }
        //                        }
        //                        int hlen = 0;
        //                        curhead = new byte[] { a[1], a[2] };
        //                        int num = (a[1] ^ a[2]);
        //                        byte sb0 = (byte)((cmp >> 16) & 0xff);
        //                        byte sb1 = (byte)((cmp >> 8) & 0xff);
        //
        //                        cursnc = sec == 0 ? true : ((sb0 & 0x07) == 0x03 && sb1 == 0xff)
        //                            || (ignore.Any(x => x == sb0) || ignore.Any(x => x == sb1));
        //                        if ((times == 0 && sb0 == 0x7f && sb1 == 0x7f) || times == 2) dbl = true;
        //
        //                        while (a[hlen] != end_byte) hlen++;
        //                        var headlen = V2_Custom.Checked ? sector_header : hlen - 1;
        //                        var newpos = posi + 1 + (hlen << 3);
        //                        if (addsnc && !cursnc) headlen -= 2;
        //                        var sec_data = T.Sector[sec].Data.GCR; // sectors[sec];
        //                        bool t19s14 = track == 19; // && sec == 14;
        //                        if (patch_Cart) sec_data = Find_Cart_Protection_v2(sec_data, t19s14, fix_weak).Item2;
        //                        if (fix_weak)
        //                        {
        //                            newendbyte = end_byte == 0x46 ? (byte)0x4e : end_byte;
        //                            for (int i = 0; i < sec_data.Length; i++)
        //                            {
        //                                if (sec_data[i] == 0xe2) sec_data[i] = 0xea;
        //                                if (sec_data[i] == 0xa3) sec_data[i] = 0xad;
        //                            }
        //                            curhead = Hex2Byte(vm2_ver[0][sec]);
        //                        }
        //                        info.Add(new VM
        //                        {
        //                            Start = start_byte,
        //                            End = newendbyte,
        //                            Sync = cursnc,
        //                            Len = headlen,
        //                            Header = curhead,
        //                            Data = sec_data,
        //                            Sector = num,
        //                            Double = dbl
        //                        });
        //                        times = 0;
        //                        if (info.Count == T.Sectors)
        //                        {
        //                            int start = newpos + (sec_data.Length << 3);
        //                            post = Bit2Byte(source, start, source.Length - start);
        //                            break;
        //                        }
        //                    }
        //                }
        //                catch { } // Console.WriteLine(ex); }
        //            }
        //            posi++;
        //        }
        //        if (V2_pad55.Checked)
        //        {
        //            if (pre.Length > 0)
        //            {
        //                for (int i = 0; i < pre.Length; i++) if (weakBytes.Any(x => x == pre[i])) pre[i] = 0x55;
        //                for (int i = 0; i < post.Length; i++) if (weakBytes.Any(x => x == post[i])) post[i] = 0x55;
        //            }
        //        }
        //        if (info.Count > 0)
        //        {
        //            List<bool> dest = new List<bool>();
        //            BitArray sync = new BitArray(V2_cust_snc.Checked ? (int)V2_sync_len.Value + 1 : 11);
        //            for (int i = 1; i < sync.Count; i++) sync[i] = true;
        //            if (pre.Length > 0) BitAppend(new BitArray(Flip_Endian(pre)), dest);
        //            for (int i = 0; i < info.Count; i++)
        //            {
        //                if (info[i].Sync || addsnc)
        //                {
        //                    if (info[i].Double) BitAppend(sync, dest);
        //                    BitAppend(sync, dest);
        //                }
        //                BitAppend(new BitArray(Flip_Endian(Build_Header(info[i].Start, info[i].End, info[i].Header, info[i].Len))), dest);
        //                BitAppend(new BitArray(Flip_Endian(info[i].Data)), dest);
        //            }
        //            if (post.Length > 0) BitAppend(new BitArray(Flip_Endian(post)), dest);
        //            return Bit2Byte(new BitArray(dest.ToArray()));
        //        }
        //
        //        byte[] Build_Header(byte s, byte e, byte[] f, int len)
        //        {
        //            using (var buff = new MemoryStream())
        //            using (var wrt = new BinaryWriter(buff))
        //            {
        //                wrt.Write((byte)s);
        //                for (int i = 0; i < (len / 2); i++) wrt.Write(f);
        //                wrt.Write((byte)e);
        //                return buff.ToArray();
        //            }
        //        }
        //        return temp_data;
        //    }
        //    return T.Data; /// <- Return array without any adjustments to sync
        //}


        byte[] Adjust_V2_Sync(byte[] data, int track_len, byte[] t_info, bool Fix_Sync, byte[][] sectors, int secs, bool fix_weak = false, bool patch_Cart = false, int trk = -1)
        {
            if (data == null) return null;
            int track = tracks > 42 ? (trk >> 1) + 1 : trk + 1;
            int dens = track > 17 ? density[1] : density[0];
            byte start_byte = t_info[0];
            byte end_byte = t_info[1];
            int head_len = Convert.ToInt32(t_info[2]);
            int vs = Convert.ToInt32(t_info[3]);
            bool st = t_info[4] == 0;
            BitArray source = new BitArray(Flip_Endian(CopyArray(data, 0, track_len >> 3)));
            int r = 0;
            source = new BitArray(Flip_Endian(Rotate_Left(Bit2Byte(source), r)));
            byte[] temp_data = Bit2Byte(source);

            if (Fix_Sync) /// <- if the "Fix_Sync" bool is true, otherwise just return track info without any adjustments
            {
                byte[] ignore = new byte[] { 0x7e, 0x7f, 0xff, 0x5f, 0xbf, 0x57, 0x5b }; /// possible sync markers to ignore when building track
                byte[] compare = new byte[2];
                /// begin processing the track
                byte[] chk = new byte[1];
                byte[] secz = { 0xa5, 0xa5 };
                var sector_header = track < 18 ? Convert.ToInt32((V2_hlenD0.Value - 2) * 2) / 2 : Convert.ToInt32((V2_hlenD1.Value - 2) * 2) / 2;
                byte[] pre = new byte[0];  // Capture data pre-first sector
                byte[] post = new byte[0]; // Capture data post-last sector
                byte newendbyte = end_byte;
                List<VM> info = new List<VM>();
                uint cmp = 0;
                int posi = 0;
                int snc = 0, times = 0;
                bool dbl = false, addsnc = V2_Add_Sync.Checked;
                byte[] curhead = new byte[0];
                bool cursnc = false;
                while (posi < source.Length)
                {
                    cmp <<= 1;
                    if (source[posi])
                    {
                        cmp |= 1;
                        snc++;
                        if (snc == 10)
                        {
                            snc = 0; times++;
                            if (times > 2) times = 0;
                        }
                    }
                    else snc = 0;
                    if ((cmp & 0xff) == start_byte)
                    {

                        try
                        {
                            var a = Bit2Byte(source, posi - 7, Math.Min(60 << 3, source.Length - posi));
                            int sec = a[1] ^ a[2];
                            if (sec < 22 && VM2_Valid.Any(x => x == a[1]) && a[3] == a[1] && a[4] == a[2])
                            {
                                if (info.Count < 1)
                                {
                                    try
                                    {
                                        pre = Bit2Byte(source, 0, posi - (3 << 3));
                                        if ((pre[pre.Length - 1] & 0x01) != 1) pre[pre.Length - 1] |= 1;
                                    }
                                    catch { }
                                }
                                int hlen = 0;
                                curhead = new byte[] { a[1], a[2] };
                                int num = (a[1] ^ a[2]);
                                byte sb0 = (byte)((cmp >> 16) & 0xff);
                                byte sb1 = (byte)((cmp >> 8) & 0xff);

                                cursnc = sec == 0 ? true : ((sb0 & 0x07) == 0x03 && sb1 == 0xff)
                                    || (ignore.Any(x => x == sb0) || ignore.Any(x => x == sb1));
                                if ((times == 0 && sb0 == 0x7f && sb1 == 0x7f) || times == 2) dbl = true;

                                while (a[hlen] != end_byte) hlen++;
                                var headlen = V2_Custom.Checked ? sector_header : hlen - 1;
                                var newpos = posi + 1 + (hlen << 3);
                                if (addsnc && !cursnc) headlen -= 2;
                                var sec_data = sectors[sec];
                                bool t19s14 = track == 19; // && sec == 14;
                                if (patch_Cart) sec_data = Find_Cart_Protection_v2(sec_data, t19s14, fix_weak).Item2;
                                if (fix_weak)
                                {
                                    newendbyte = end_byte == 0x46 ? (byte)0x4e : end_byte;
                                    for (int i = 0; i < sec_data.Length; i++)
                                    {
                                        if (sec_data[i] == 0xe2) sec_data[i] = 0xea;
                                        if (sec_data[i] == 0xa3) sec_data[i] = 0xad;
                                    }
                                    curhead = Hex2Byte(vm2_ver[0][sec]);
                                }
                                info.Add(new VM
                                {
                                    Start = start_byte,
                                    End = newendbyte,
                                    Sync = cursnc,
                                    Len = headlen,
                                    Header = curhead,
                                    Data = sec_data,
                                    Sector = num,
                                    Double = dbl
                                });
                                times = 0;
                                if (info.Count == secs)
                                {
                                    int start = newpos + (sec_data.Length << 3);
                                    post = Bit2Byte(source, start, source.Length - start);
                                    break;
                                }
                            }
                        }
                        catch { } // Console.WriteLine(ex); }
                    }
                    posi++;
                }
                if (V2_pad55.Checked)
                {
                    if (pre.Length > 0)
                    {
                        for (int i = 0; i < pre.Length; i++) if (weakBytes.Any(x => x == pre[i])) pre[i] = 0x55;
                        for (int i = 0; i < post.Length; i++) if (weakBytes.Any(x => x == post[i])) post[i] = 0x55;
                    }
                }
                if (info.Count > 0)
                {
                    List<bool> dest = new List<bool>();
                    BitArray sync = new BitArray(V2_cust_snc.Checked ? (int)V2_sync_len.Value + 1 : 11);
                    for (int i = 1; i < sync.Count; i++) sync[i] = true;
                    if (pre.Length > 0) BitAppend(new BitArray(Flip_Endian(pre)), dest);
                    for (int i = 0; i < info.Count; i++)
                    {
                        if (info[i].Sync || addsnc)
                        {
                            if (info[i].Double) BitAppend(sync, dest);
                            BitAppend(sync, dest);
                        }
                        BitAppend(new BitArray(Flip_Endian(Build_Header(info[i].Start, info[i].End, info[i].Header, info[i].Len))), dest);
                        BitAppend(new BitArray(Flip_Endian(info[i].Data)), dest);
                    }
                    if (post.Length > 0) BitAppend(new BitArray(Flip_Endian(post)), dest);
                    return Bit2Byte(new BitArray(dest.ToArray()));
                }

                byte[] Build_Header(byte s, byte e, byte[] f, int len)
                {
                    using (var buff = new MemoryStream())
                    using (var wrt = new BinaryWriter(buff))
                    {
                        wrt.Write((byte)s);
                        for (int i = 0; i < (len / 2); i++) wrt.Write(f);
                        wrt.Write((byte)e);
                        return buff.ToArray();
                    }
                }
                return temp_data;
            }
            return data; /// <- Return array without any adjustments to sync
        }
    }
}