using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        /// V-Max v3 sync and header variables for "Rebuild tracks" options
        //private readonly byte[] v3_sector_sync = { 0x5b, 0xff };  // change the sync marker placed before sector headers (0x57, 0xff known working)
        private static readonly byte[] v3_sector_sync = { 0x7f, 0xff };  // change the sync marker placed before sector headers (0x57, 0xff known working)
        private static readonly int v3_min_header = 3;             // adjust the minimum length of the sector header (0x49) bytes
        private static readonly int v3_max_header = 8; //12;            // adjust the maximum length of the sector header (0x49) bytes
        private static readonly byte[] vm3_pos_sync = { 0x57, 0x5b, 0x5f, 0x7f, 0xff };
        private static readonly byte[] v3a = { 0x49, 0x49, 0x49, 0xee };
        private static readonly byte[] cart_patch_v3 = { 0x82, 0xe0, 0x23, 0xff, 0x6c };

        void V3_Auto_Adjust()
        {
            bool p = true;
            bool v = false;
            if (V3_Auto_Adj.Checked || Adj_cbm.Checked)
            {
                for (int t = 0; t < tracks; t++)
                {
                    if (NDG.Track_Data[t] != null)
                    {
                        if (NDS.cbm[t] == 1 || NDS.cbm[t] == 3)
                        {
                            if (Original.OT[t]?.Length == 0)
                            {
                                Original.OT[t] = new byte[NDG.Track_Data[t].Length];
                                Buffer.BlockCopy(NDG.Track_Data[t], 0, Original.OT[t], 0, NDG.Track_Data[t].Length);
                            }
                        }
                        if (NDS.cbm[t] == 4) Shrink_Short_Sector(t);
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
                            Buffer.BlockCopy(Original.SG, 0, NDG.Track_Data[t], 0, Original.SG.Length);
                            Buffer.BlockCopy(Original.SA, 0, NDA.Track_Data[t], 0, Original.SA.Length);
                            NDG.Track_Length[t] = NDG.Track_Data[t].Length;
                            NDA.Track_Length[t] = NDG.Track_Length[t] * 8;
                            NDG.L_Rot = false;
                        }
                        if (NDS.cbm[t] == 1 || (NDS.cbm[t] == 3)) // && NDS.sectors[t] < 16))
                        {
                            if (Original.OT[t]?.Length != 0 || Original.OT[t] != null)
                            {
                                try
                                {
                                    NDG.Track_Data[t] = new byte[Original.OT[t].Length];
                                    Buffer.BlockCopy(Original.OT[t], 0, NDG.Track_Data[t], 0, Original.OT[t].Length);
                                    Buffer.BlockCopy(Original.OT[t], 0, NDA.Track_Data[t], 0, Original.OT[t].Length);
                                    Buffer.BlockCopy(Original.OT[t], 0, NDA.Track_Data[t], Original.OT[t].Length, NDA.Track_Data[t].Length - Original.OT[t].Length);
                                    p = false;
                                    v = true;
                                }
                                catch { }
                            }
                            NDG.Track_Length[t] = NDG.Track_Data[t].Length;
                            NDA.Track_Length[t] = NDG.Track_Length[t] * 8;
                        }
                    }
                }
            }
            Clear_Out_Items();
            if (Adj_cbm.Checked && !V3_Auto_Adj.Checked) p = false;
            Process_Nib_Data(true, p, v, true); /// false flag instructs the routine NOT to process CBM tracks again -- p (true/false) process v-max v3 short tracks
        }

        int Get_vm3_sectorSize(byte[] data, int curpos = 0)
        {
            int pos = 0;
            try
            {
                while (curpos + pos < data.Length)
                {
                    if (vm3_pos_sync.Any(x => x == data[curpos + pos]) || data[curpos + pos] == 0x49) break;
                    pos++;
                }
            }
            catch { }
            return pos;
        }

        byte[] Rebuild_V3(byte[] data, int gap_sector, byte[] Disk_ID, int trk, int trk_size, int tlength)
        {
            trk = tracks > 42 ? (trk / 2) + 1 : trk + 1;
            int compare = 0;
            byte[] track_ID = trk % 2 == 1
                ? ArrayConcat(v3_sector_sync, new byte[] { 0xff, 0xff }, Build_BlockHeader(trk, 255, NDS.t18_ID))
                : new byte[] { 0x7f };
            //int d = trk < 18 ? 0 : Get_Density(trk_size) < 1 ? 1 : Get_Density(trk_size);
            //version = true;
            int d = Get_Density(tlength >> 3);
            //int d = trk < 18 ? 0 : Get_Density(trk_size) < 1 ? 1 : 1;
            int sync = v3_sector_sync.Length;
            int tlen = track_ID.Length;
            Dictionary<int, byte[]> sector = new Dictionary<int, byte[]>();
            BitArray source = new BitArray(Flip_Endian(data));
            int i = 0;
            while (i < source.Length)
            {
                compare <<= 1;
                if (source[i]) compare |= 1;
                if ((compare & 0x00ffffff) == 0x4949ee)
                {
                    try
                    {
                        int pos = i + 1;
                        int cursec = Decode_VmaxGCR(Bit2Byte(source, pos, 32))[0] & 0x1f; // decodes the sector # from V-Max GCR
                        if (!sector.ContainsKey(cursec) && cursec >= 0 && cursec <= 32)
                        {
                            byte[] getsec = Bit2Byte(source, pos, Math.Min(340 << 3, source.Length - pos)); // take more than we need
                            int secsize = Get_vm3_sectorSize(getsec, 0);        // find exact size of sector
                            var embsize = Decode_VmaxGCR(CopyFrom(getsec, 0, 8))[5] << 2;
                            if (embsize <= secsize)
                            {
                                byte[] asec = CopyFrom(getsec, 0, secsize);
                                /// ------------------------------ Test Section ------------------------------------
                                (bool cart, byte[] newsec) = Find_Cart_Protection_v3(asec);
                                if (cart && ((P_Cart.Visible && P_Cart.Checked) || batch)) sector.Add(cursec, newsec);
                                else sector.Add(cursec, asec);  // copy relevant sector data into the array
                                /// --------------------------------------------------------------------------------
                            }
                            tlen += secsize + 1 + sync;
                            i += secsize << 3; // advance source pointer near the end of sector.
                        }
                    }
                    catch (Exception ex) { Console.WriteLine(ex); }
                }
                i++;
            }
            int header_len = (density[d] - (tlen + 10)) / sector.Count;
            header_len = header_len > v3_max_header ? v3_max_header : header_len < v3_min_header ? v3_min_header : header_len;
            byte[] sec_header = ArrayConcat(v3_sector_sync, (FastArray.Init(header_len, 0x49)), new byte[] { 0xee });
            var sec_seq = sector.Keys.OrderBy(k => k).ToList(); // sort sectors numerically
            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(buffer))
            {
                foreach (var sec in sec_seq)
                {
                    writer.Write(sec_header);
                    writer.Write(sector[sec]);
                }
                writer.Write(track_ID);
                int remaining = (density[d] - (int)buffer.Position);
                if (remaining > 0) writer.Write(FastArray.Init(remaining, sector.Count < 10 ? Get_Filler() : (byte)0x55));
                return buffer.ToArray();
            }

            byte Get_Filler()
            {
                byte[] possible_Filler = new byte[] { 0xaa, 0x55, 0xff };
                byte filler = 0;
                int count = 0, longest = 0;
                for (int f = 1; f < data.Length; f++)
                {
                    if (data[f] != data[f - 1] && (data[f] != 0xaa && data[f] != 0x55)) count = 0;
                    else if (++count > longest)
                    {
                        longest = count;
                        filler = data[f]; // track the most common filler byte
                    }
                }
                return possible_Filler.Any(x => x != filler) ? (byte)0xff : filler;
            }
        }

        (string[], int, int, int, int, int, int, int, bool) Get_vmv3_track_length(byte[] data, int trk, bool cartP)
        {
            int data_start = 0, data_end = 0, sector_zero = 0, header_total = 0, header_avg = 0, gap_sector = 0, last_sector = 0, sectors = 0;
            int track = tracks > 42 ? (trk / 2) : trk;
            int max_backtrack = 6 << 3;
            int curpos, secsize, embsize, sec, csm, secpos, pos = 0, fallback = 0, maxHlen = 0;
            uint compare = 0;
            bool start_found = false, end_found = false, v4 = false, sub_offset = false;
            byte[] v4id = FastArray.Init(4, 0xf7), rawsec, tsec, decgcr, sdat;
            string stats = string.Empty;
            List<string> s = new List<string>();
            List<int> ss = new List<int>();
            List<int> err = new List<int>();
            BitArray source = new BitArray(Flip_Endian(data));
            while (pos < source.Length)
            {
                compare <<= 1;
                if (source[pos]) compare |= 1;
                if ((compare & 0xffff) == 0x4949 && (((compare & 0xff0000) >> 16) != 0x49))
                {
                    try
                    {
                        curpos = pos - 15;
                        var header = Bit2Byte(source, curpos, (v3_max_header + 8) << 3);
                        int hlen = 0;
                        while (hlen < header.Length && header[hlen] == 0x49) hlen++;
                        if (header[hlen] == 0xee)
                        {
                            secpos = curpos + ((hlen + 1) << 3);
                            decgcr = Decode_VmaxGCR(Bit2Byte(source, secpos, 8 << 3));
                            sec = (decgcr[0] & 0x1f);
                            if (!ss.Contains(sec))
                            {
                                maxHlen = Math.Max(hlen, maxHlen);
                                tsec = Bit2Byte(source, secpos, Math.Min(285 << 3, source.Length - secpos));
                                secsize = Get_vm3_sectorSize(tsec); // find the true end of the V-Max sector
                                //embsize = decgcr[5] << 2;           // find the embeded size of the sector (GCR-decoded byte 5 (x4)
                                embsize = (decgcr[5] + 2 + (tsec[((decgcr[5] + 2) << 2) + 2] == 0xf7 ? 1 : 0)) << 2;
                                if (embsize <= secsize)             // if embeded sector size roughly the same size, continue.
                                {
                                    string mismatch = embsize != secsize ? $" ! {embsize}" : string.Empty;
                                    rawsec = CopyFrom(tsec, 0, secsize);
                                    v4 = MatchSeq(CopyFrom(rawsec, rawsec.Length - 4, 4), v4id);
                                    sdat = Decode_VmaxGCR(rawsec);
                                    if (!cartP) cartP = Find_Cart_Protection_v3(rawsec).Item1;
                                    csm = 0;
                                    foreach (byte b in sdat) csm ^= b;
                                    if (csm != 0) err.Add(sec);
                                    sectors++;
                                    if (sec == 0)
                                    {
                                        fallback = Math.Max(0, Math.Min(max_backtrack, curpos));
                                        if (fallback > max_backtrack) fallback = 0;
                                        sector_zero = curpos - fallback;
                                    }
                                    if (!start_found)
                                    {
                                        sub_offset = sec == 0;
                                        data_start = curpos;
                                        start_found = true;
                                        if (last_sector != 0) gap_sector = last_sector;
                                    }
                                    last_sector = curpos;
                                    ss.Add(sec);
                                    var dhead = FastArray.Init(hlen + 1, 0x49);
                                    dhead[dhead.Length - 1] = 0xee;
                                    if (!batch) s.Add($"Sector ({sec}){(sec == 0 ? "*" : string.Empty)} Pos ({curpos >> 3}) Size ({secsize}{mismatch}) Header [ {Hex_Val(dhead)} ] Checksum ({(csm == 0 ? "OK" : "Failed!")})");
                                    header_total += hlen;
                                    pos += secsize << 3;
                                }
                            }
                            else
                            {
                                end_found = true;
                                data_end = sub_offset ? curpos - fallback : curpos;
                                if (!batch)
                                {
                                    s.Add($"Pos {curpos >> 3} **Repeat** sector {sec}");
                                    stats = $"Track Length ({(data_end - data_start) >> 3}) Sectors ({ss.Count})";
                                }
                                if (!batch)
                                {
                                    stats += $" sector 0 ({sector_zero >> 3})  Header Length ({hlen + 1})";
                                    s.Add(stats);
                                }
                            }
                        }
                    }
                    catch { }
                }
                if (end_found) break;
                pos++;
            }
        
            if (header_avg > 0 && header_total > 0) header_avg = header_total / ss.Count;
        
            if (ss.Count < 16)
            {
                int d = (maxHlen <= 4 && v4) ? density[density_map[track]] : density[1];
                if ((start_found && !end_found) || end_found && data_end - data_start < (d << 3))
                {
                    if ((data_start >> 3) > (500)) data_start = 0; // (500 << 3)
                    data_end = data_start + (d << 3);
                }
            }
            if (!batch && err.Count > 0)
            {
                foreach (var e in err) ErrorList.Add($"Checksum failed on track {track + 1}, sector {e}");
            }
            //int st = sector_zero;
            //BitArray ff = new BitArray(data_end - data_start);
            //for (int i = 0; i < data_end -  data_start; i++)
            //{
            //    ff[i] = source[st++];
            //    if (st == data_end) st = data_start;
            //}
            ////string ddd = $"start : {data_start}, end : {data_end}, offset {fallback}";
            ////File.WriteAllText($@"c:\test\v3\track_{track}.txt", ddd);
            //File.WriteAllBytes($@"c:\test\v3\t_{track}_parse.bin", Bit2Byte(ff));
            return (s.ToArray(), data_start, data_end, sector_zero, (data_end - data_start), ss.Count, header_avg, gap_sector, cartP);
        }

        (byte[], int, int) Adjust_Vmax_V3_Sync(byte[] data, int data_start, int data_end, int sector_zero, int sectors = 0, bool fix = false, bool patch_cart = false, int trk = -1)
        {
            if (data == null) { return (null, 0, 0); }
            int track = tracks > 42 ? (trk >> 1) + 1 : trk + 1;
            //if (!patch_cart) File.WriteAllText($@"c:\test\v3\patch_{track}.txt", $"{patch_cart}");

            int st = sector_zero;
            BitArray ff = new BitArray(Flip_Endian(data));
            BitArray source = new BitArray(data_end - data_start);
            for (int i = 0; i < data_end - data_start; i++)
            {
                source[i] = ff[st++];
                if (st == data_end) st = data_start;
            }
            byte[] bdata = Bit2Byte(source);
            if (!fix) return (bdata, bdata.Length << 3, 0);

            int retdensity = density[Get_Density(bdata.Length)];
            //File.WriteAllBytes($@"c:\test\v3\pre_adj_{track}.bin", bdata);
            //BitArray sync = new BitArray(11);
            BitArray sync = new BitArray(V3_Cust_Sync.Checked ? (int)V3_syncLen.Value + 1 : 11);
            List<bool> dest = new List<bool>();
            List<VM> info = new List<VM>();
            for (int i = 1; i < sync.Length; i++) sync[i] = true;
            uint comp = 0;
            int pos = 0, curpos, sec, secpos, embsize, secsize, last = 0, fsec = 0;
            byte[] decgcr, tsec; //, pre = new byte[0], post = new byte[0];
            BitArray pre = new BitArray(0);
            BitArray post = new BitArray(0);
            while (pos < source.Length)
            {
                comp <<= 1;
                if (source[pos]) comp |= 1;
                if ((comp & 0xffff) == 0x4949 && ((comp & 0x00ff0000) >> 16) != 0x49)
                {
                    try {
                        curpos = pos - 15;
                        var header = Bit2Byte(source, curpos, (v3_max_header + 8) << 3);
                        int hlen = 0;
                        while (hlen < header.Length && header[hlen] == 0x49) hlen++;
                        if (header[hlen] == 0xee)
                        {
                            secpos = curpos + ((hlen + 1) << 3);
                            decgcr = Decode_VmaxGCR(Bit2Byte(source, secpos, 8 << 3));
                            sec = decgcr[0] & 0x1f;
                            tsec = Bit2Byte(source, secpos, Math.Min(285 << 3, source.Length - secpos));
                            secsize = Get_vm3_sectorSize(tsec); // find the true end of the V-Max sector
                            embsize = decgcr[5] << 2;           // find the embeded size of the sector (GCR-decoded byte 5 (x4)
                            if (embsize <= secsize)             // if embeded sector size roughly the same size, continue.
                            {

                                if (fsec < 1)
                                {
                                    pre = BitCopy(source, 0, Math.Max(0, curpos - 16));
                                    //File.WriteAllText($@"c:\test\v3\ttt_{track}.txt", $"pos {pos}, curpos {curpos - 16}");
                                }
                                var sdat = CopyArray(tsec, 0, secsize);
                                last = curpos + ((secsize + hlen + 1) << 3);
                                if (V3_Custom.Checked) hlen = (int)V3_hlen.Value;
                                if (patch_cart) sdat = Find_Cart_Protection_v3(sdat).Item2;
                                //File.WriteAllText($@"c:\test\v3\patch.txt", $"{patch_cart}");
                                //last = curpos + ((secsize + hlen) << 3);
                                info.Add(new VM
                                {
                                    Start = 0x49,
                                    End = 0xee,
                                    Sync = true,
                                    Len = hlen + 1,
                                    Data = sdat,
                                    Sector = sec,
                                });
                                fsec++;
                            }
                        }
                    }
                    catch { }
                    }
                pos++;

            }
            if (pos > last) post = BitCopy(source, last, pos - last - 1);
            //File.WriteAllText($@"c:\test\v3\pos_last_{track}.txt", $"pos {pos}, last {last}, l-p {pos - last - 1}");

            if (info.Count > 0)
            {
                if (pre.Length > 0) BitAppend(pre, dest);
                for (int i = 0; i < info.Count; i++)
                {
                    BitAppend(sync, dest);
                    BitAppend(BuildHeader(info[i].Len), dest);
                    BitAppend(new BitArray(Flip_Endian(info[i].Data)), dest);
                }
                if (V3_Trim.Enabled && V3_Trim.Checked && post.Count > 0)
                {
                    int psize = post.Count;
                    int d = (density[Get_Density(bdata.Length)]) << 3;
                    int sub = (dest.Count + post.Count) - d;
                    if (sub > 0 && post.Count - sub > 0)
                    {
                        psize = sub;
                        post = BitCopy(post, 0, post.Count - psize);
                    }
                }
                //BitAppend(new BitArray(Flip_Endian(new byte[] { 0x7f })), dest);
                if (post.Length > 0) BitAppend(post, dest);
                int fill = retdensity - (dest.Count >> 3);
                if (fill > 0)
                {
                    byte filler = post.Length > 0 ? Bit2Byte(post, post.Length - 8, 8)[0] : (byte)0x55;
                    BitAppend(new BitArray(Flip_Endian(FastArray.Init(fill, filler))), dest);
                }
                var final = Bit2Byte(new BitArray(dest.ToArray()));
                //File.WriteAllBytes($@"c:\test\v3\pre_{track}.bin", Bit2Byte(pre));
                //File.WriteAllBytes($@"c:\test\v3\post_{track}.bin", Bit2Byte(post));
                //File.WriteAllText($@"c:\test\v3\dsec_{track}.txt", $"{info.Count}");
                //File.WriteAllBytes($@"c:\test\v3\track_adj_{track}.bin", final);
                return (final, dest.Count, 0);
            }

            BitArray BuildHeader(int head_len)
            {
                byte[] h = FastArray.Init(head_len, 0x49);
                h[h.Length - 1] = 0xee;
                return new BitArray(Flip_Endian(h));
            }
            return (bdata, data.Length, 0);
        }

        //(byte[], int, int) Adjust_Vmax_V3_Sync(byte[] data, int data_start, int data_end, int sector_zero, int sectors = 0, int trk = -1)
        //{
        //    int track = tracks > 42 ? (trk / 2) + 1 : trk + 1;
        //    if (data == null) { return (null, 0, 0); }
        //    byte[] bdata = Rotate_Left(Bit2Byte(new BitArray(Flip_Endian(data)), data_start, data_end - data_start), (sector_zero >> 3) - (data_start >> 3) - 2);
        //    //byte[] bdata = Rotate_Left(Bit2Byte(new BitArray(Flip_Endian(data)), data_start, data_end - data_start), (sector_zero >> 3) - (data_start >> 3) - 2);
        //    int r = FindTrackGap(bdata, true, new byte[] { 0x49 });
        //    if (r > 0) bdata = Rotate_Left(bdata, r);
        //    File.WriteAllBytes($@"c:\test\v3\t_{track}", bdata);
        //
        //    int spos = 0;
        //    int cust = (int)V3_hlen.Value;
        //    int cur_sec = 0;
        //    using (var buffer = new MemoryStream())
        //    using (var write = new BinaryWriter(buffer))
        //    {
        //        while (spos < bdata.Length)
        //        {
        //            if (spos + 2 < bdata.Length && bdata[spos + 2] == 0x49)
        //            {
        //                try
        //                {
        //                    if (MatchSeq(bdata, new byte[] { 0x49, 0x49 }, spos + 2))
        //                    {
        //                        var a = 0;
        //                        while (bdata[spos + a] != 0x49)
        //                        {
        //                            if (!vm3_pos_sync.Any(s => s == bdata[spos + a])) write.Write(bdata[spos + a]);
        //                            a++;
        //                        }
        //                        var b = 0;
        //                        while (spos + (a + b) < bdata.Length && bdata[spos + (a + b)] == 0x49) b++;
        //                        spos += (a + b);
        //                        if (b < 15 && V3_Custom.Checked) b = cust;
        //                        if (cur_sec < sectors) write.Write(v3_sector_sync);
        //                        cur_sec++;
        //                        for (int i = 0; i < b; i++) write.Write((byte)0x49);
        //                    }
        //                }
        //                catch { }
        //            }
        //            if (spos < bdata.Length) write.Write(bdata[spos]);
        //            spos++;
        //        }
        //        var temp = buffer.ToArray();
        //        int pos = 0;
        //        while (pos < temp.Length - 1)
        //        {
        //            if (temp[pos] == 0x49 && temp[pos + 1] == 0x49)
        //            {
        //                pos -= 2; break;
        //            }
        //            pos++;
        //        }
        //        //temp = Rotate_Left(temp, pos);
        //        return (temp, (int)buffer.Length << 3, 0);
        //    }
        //}
    }
}