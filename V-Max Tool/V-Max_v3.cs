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
        /// V-Max v3 sync and header variables for "Rebuild tracks" options
        //private readonly byte[] v3_sector_sync = { 0x5b, 0xff };  // change the sync marker placed before sector headers (0x57, 0xff known working)
        private static readonly byte[] v3_sector_sync = { 0x7f, 0xff };  // change the sync marker placed before sector headers (0x57, 0xff known working)
        private static readonly int v3_min_header = 3;             // adjust the minimum length of the sector header (0x49) bytes
        private static readonly int v3_max_header = 8; //12;            // adjust the maximum length of the sector header (0x49) bytes
        private static readonly byte[] vm3_pos_sync = { 0x57, 0x5b, 0x5f, 0x7f, 0xff };
        private static readonly byte[] v3a = { 0x49, 0x49, 0x49, 0xee };

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
            bool e = busy;
            RunBusy(() => f_load.Checked = V3_Auto_Adj.Checked);
            busy = e;
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
            catch { };
            return pos;
        }

        byte[] Rebuild_V3(byte[] data, int gap_sector, byte[] Disk_ID, int trk)
        {
            trk = tracks > 42 ? (trk / 2) + 1 : trk + 1;
            int compare = 0;
            byte[] track_ID = trk % 2 == 1
                ? ArrayConcat(v3_sector_sync, new byte[] { 0xff, 0xff }, Build_BlockHeader(trk, 255, NDS.t18_ID))
                : new byte[] { 0x7f };
            int d = trk < 18 ? 0 : Get_Density(data.Length) < 1 ? 1 : Get_Density(data.Length);
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
                        if (!sector.ContainsKey(cursec) && cursec >= 0 && cursec < 30)
                        {
                            byte[] getsec = Bit2Byte(source, pos, Math.Min(340 << 3, source.Length - pos)); // take more than we need
                            int secsize = Get_vm3_sectorSize(getsec, 0);        // find exact size of sector
                            sector.Add(cursec, CopyFrom(getsec, 0, secsize));  // copy relevant sector data into the array
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

        (string[], int, int, int, int, int, int, int) Get_vmv3_track_length(byte[] data, int trk)
        {
            int data_start = 0;
            int data_end = 0;
            int sector_zero = 0;
            int header_total = 0;
            int header_avg = 0;
            int gap_sector = 0;
            int last_sector = 0;
            int sectors = 0;
            bool start_found = false;
            bool end_found = false;
            byte head_end = 0xee; /// V-Max v3 header end byte located directly following the 49-49-49 pattern
            byte[] comp = new byte[2];
            byte[] head = new byte[18];
            List<string> s = new List<string>();
            List<int> ss = new List<int>();
            var err = new List<int>();
            string stats = string.Empty;

            for (int i = 0; i < data.Length - comp.Length; i++)
            {
                if (data[i] == 0x49 && data[i + 1] == 0x49)
                {
                    var a = 0;
                    while (data[i + a] == 0x49) a++;
                    i += a;
                    if (data[i] == head_end)
                    {
                        if (i + head.Length < data.Length) Buffer.BlockCopy(data, i, head, 0, head.Length);
                        byte[] decgcr = Decode_VmaxGCR(CopyFrom(data, i + 1, 8));
                        int sec = (decgcr[0] & 0x1f);
                        if (!ss.Contains(sec))
                        {
                            int secsize = Get_vm3_sectorSize(data, i + 1);
                            int embsize = (decgcr[5] + 2 + (data[i + ((decgcr[5] + 2) << 2) + 2] == 0xf7 ? 1 : 0)) << 2;
                            string mismatch = embsize != secsize ? $" ! {embsize}" : string.Empty;
                            byte[] sdat = Decode_VmaxGCR(CopyFrom(data, i + 1, secsize));
                            int csm = 0;
                            foreach (byte b in sdat) csm ^= b;
                            if (csm != 0) err.Add(sec);
                            sectors++;
                            if ((decgcr[0] & 0x1f) == 0) sector_zero = i - a;
                            if (!start_found)
                            {
                                data_start = i - a;
                                start_found = true;
                                if (last_sector != 0) gap_sector = last_sector;
                            }
                            if (gap_sector == 0) gap_sector = last_sector;
                            last_sector = i;
                            ss.Add(sec);
                            var dhead = FastArray.Init(a + 1, 0x49);
                            dhead[dhead.Length - 1] = 0xee;
                            if (!batch) s.Add($"Sector ({sec}){(sec == 0 ? "*" : string.Empty)} Pos ({i - a}) Size ({secsize}{mismatch}) Header [ {Hex_Val(dhead)} ] Checksum ({(csm == 0 ? "OK" : "Failed!")})");
                            header_total += a;
                        }
                        else
                        {
                            end_found = true;
                            data_end = i - a;
                            if (!batch)
                            {
                                s.Add($"Pos {i - a} **Repeat** sector {sec}");
                                stats = $"Track Length ({data_end - data_start}) Sectors ({ss.Count})";
                            }
                            if (!batch)
                            {
                                stats += $" sector 0 ({sector_zero})  Header Length ({a + 1})";
                                s.Add(stats);
                            }
                        }
                    }
                }
                if (end_found) break;
            }
            if (header_avg > 0 && header_total > 0) header_avg = header_total / ss.Count;

            if (ss.Count < 16)
            {
                int de = density[Get_Density(data_end - data_start)];
                if ((tracks > 42 && trk == 36) || (tracks <= 42 && trk == 18)) de = density[1];
                if (start_found && !end_found)
                {
                    if (data_start > 500) data_start = 0;
                    //data_end = de + 200;
                    data_end = 7800;
                }
                if (start_found && end_found && (data_end - data_start) < 7000)
                {
                    var a = de - (data_end - data_start);
                    if (data_end + a < 8192) data_end += a;
                }
                //msg = $"Track Length [est] (7400) Sectors ({ss.Count})";
            }
            if (!batch && err.Count > 0)
            {
                var errtk = tracks > 42 ? (trk / 2) + 1 : trk + 1;
                foreach (var e in err) ErrorList.Add($"Checksum failed on track {errtk}, sector {e}");
            }
            return (s.ToArray(), data_start, data_end, sector_zero, (data_end - data_start), ss.Count, header_avg, gap_sector);
        }

        (byte[], int, int) Adjust_Vmax_V3_Sync(byte[] data, int data_start, int data_end, int sector_zero, int sectors = 0)
        {
            if (data == null) { return (null, 0, 0); }
            byte[] bdata = new byte[data_end - data_start];
            Buffer.BlockCopy(data, data_start, bdata, 0, data_end - data_start);
            bdata = Rotate_Left(bdata, ((sector_zero >> 3) - (data_start >> 3)) - 2);
            int spos = 0;
            int cust = (int)V3_hlen.Value;
            int cur_sec = 0;
            using (var buffer = new MemoryStream())
            using (var write = new BinaryWriter(buffer))
            {
                while (spos < bdata.Length)
                {
                    if (spos + 2 < bdata.Length && bdata[spos + 2] == 0x49)
                    {
                        try
                        {
                            if (MatchSeq(bdata, new byte[] { 0x49, 0x49 }, spos + 2))
                            {
                                var a = 0;
                                while (bdata[spos + a] != 0x49)
                                {
                                    if (!vm3_pos_sync.Any(s => s == bdata[spos + a])) write.Write(bdata[spos + a]);
                                    a++;
                                }
                                var b = 0;
                                while (spos + (a + b) < bdata.Length && bdata[spos + (a + b)] == 0x49) b++;
                                spos += (a + b);
                                if (b < 15 && V3_Custom.Checked) b = cust;
                                if (cur_sec < sectors) write.Write(v3_sector_sync);
                                cur_sec++;
                                for (int i = 0; i < b; i++) write.Write((byte)0x49);
                            }
                        }
                        catch { }
                    }
                    if (spos < bdata.Length) write.Write(bdata[spos]);
                    spos++;
                }
                var temp = buffer.ToArray();
                int pos = 0;
                while (pos < temp.Length - 1)
                {
                    if (temp[pos] == 0x49 && temp[pos + 1] == 0x49)
                    {
                        pos -= 2; break;
                    }
                    pos++;
                }
                temp = Rotate_Left(temp, pos);
                return (temp, (int)buffer.Length << 3, 0);
            }
        }
    }
}