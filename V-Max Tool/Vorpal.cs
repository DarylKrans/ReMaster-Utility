using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {

        //readonly static byte[] vpl_s0 = new byte[] { 0x33, 0x3F, 0xD5 };
        //readonly static byte[] vpl_s1 = new byte[] { 0x35, 0x4d, 0x53 };
        readonly static BitArray leadIn_std = new BitArray(10);
        readonly static BitArray leadIn_alt = new BitArray(10);
        readonly static int com = 20;

        void Vorpal_Rebuild()
        {
            bool p = false;
            if (VPL_rb.Checked || VPL_auto_adj.Checked)
            {
                for (int t = 0; t < tracks; t++)
                {
                    if (NDG.Track_Data[t] != null)
                    {
                        if (NDS.cbm[t] == 1)
                        {
                            if (Original.OT[t].Length == 0)
                            {
                                Original.OT[t] = new byte[NDG.Track_Data[t].Length];
                                Buffer.BlockCopy(NDG.Track_Data[t], 0, Original.OT[t], 0, NDG.Track_Data[t].Length);
                            }
                        }
                    }
                }
            }
            if (VPL_auto_adj.Checked) p = true;
            for (int t = 0; t < tracks; t++)
            {
                if (NDG.Track_Data[t] != null)
                {
                    if (NDS.cbm[t] == 5 || NDS.cbm[t] == 1)
                    {
                        if (Original.OT[t].Length > 6000)
                        {
                            try
                            {
                                NDG.Track_Data[t] = new byte[Original.OT[t].Length];
                                Buffer.BlockCopy(Original.OT[t], 0, NDG.Track_Data[t], 0, Original.OT[t].Length);
                                Buffer.BlockCopy(Original.OT[t], 0, NDA.Track_Data[t], 0, Original.OT[t].Length);
                                Buffer.BlockCopy(Original.OT[t], 0, NDA.Track_Data[t], Original.OT[t].Length, NDA.Track_Data[t].Length - Original.OT[t].Length);
                            }
                            catch { }
                        }
                        NDG.Track_Length[t] = NDG.Track_Data[t].Length;
                        NDA.Track_Length[t] = NDG.Track_Length[t] * 8;
                    }
                }
            }
            Clear_Out_Items();
            Process_Nib_Data(p, false, false, true); /// false flag instructs the routine NOT to process CBM tracks again
        }

        (int sectors, int first, int last_pos) Get_VPL_Sectors(BitArray source)
        {
            var sectors = 0;
            var first = 0;
            var last = 0;
            int compare = 0;
            int pos = 0;
            int s0 = 0x3fd500;
            int s1 = 0xbfd500;
            while (pos < source.Length)
            {
                compare <<= 1;
                if (source[pos]) compare |= 1;
                if ((compare & 0x00ffff80) == s0 || (compare & 0x00ffff80) == s1)
                {
                    if (sectors < 1) first = pos - 31;
                    sectors++;
                    last = pos - 7;
                }
                pos++;
            }
            return (sectors, first, last);
        }

        byte[] Rebuild_Vorpal(byte[] data, int trk, int leadptn = 0)
        {
            int offset;
            int last_sector = 1312; // 1312; /// # of bytes to read when last sector found
            byte[] output = new byte[0];
            int d = 0;
            int tlen = data.Length;
            if (VPL_auto_adj.Checked || VPL_rb.Checked) d = VPL_Density(tlen);
            var source = new BitArray(Flip_Endian(data));
            (int sectors, int tstart, int tend) = Get_VPL_Sectors(source);
            tend += last_sector;
            byte[] lead_in = new byte[0];
            byte endbyte = leadptn == 1 ? (byte)0x55 : (leadptn == 2 ? (byte)0xAA : (byte)0xb5);
            switch (leadptn)
            {
                case 0: lead_in = new byte[] { 0xd5, 0x35, 0x4d, 0x53, 0x54 }; break;
                case 1: lead_in = FastArray.Init(5, 0x55); break;
                case 2: lead_in = FastArray.Init(5, 0xaa); break;
            }
            output = new byte[tlen];
            if (!VPL_only_sectors.Checked) Write_Lead(100);
            var output_bits = new BitArray(Flip_Endian(output));
            offset = (((output_bits.Length - (tend - tstart)) >> 1) >> 3) << 3;
            var len = (tend - tstart) >> 3;
            if (VPL_lead.Checked) offset = Convert.ToInt32(Lead_In.Value) << 3;
            if (VPL_only_sectors.Checked)
            {
                var asize = (tend - tstart + 7) >> 3;
                var size = sectors > 44 ? 7728 : density[d];
                var tsize = size > density[d] ? size : density[d];
                var offset_adj = (tsize - asize) / 2;
                offset = offset_adj >= 60 ? 60 << 3 : offset_adj << 3;
                output = new byte[tsize];
                Write_Lead(offset + 5);
                if (VPL_presync.Checked) Add_Pre_Sync();
                output[output.Length - 1] = 0x55;
                output_bits = new BitArray(Flip_Endian(output));
            }
            if ((VPL_auto_adj.Checked || VPL_rb.Checked) && !(VPL_only_sectors.Checked || VPL_lead.Checked))
            {
                offset = ((((vpl_density[d] << 3) - (tend - tstart)) >> 1) >> 3) << 3;
                offset = (offset > 60 << 3) ? 60 << 3 : (offset < 15 << 3) ? 15 << 3 : offset;
            }
            if (VPL_auto_adj.Checked)
            {
                var r = offset >> 3;
                var e = len + (r << 1);
                if (e < vpl_density[d])
                {
                    if (vpl_density[d] - e > 125) len = vpl_density[d] - 70;
                    else len = vpl_density[d] - ((r << 1) + 1);
                }
                output = new byte[vpl_density[d]];
                Write_Lead(offset + 5);
                if (VPL_presync.Checked) Add_Pre_Sync();
                output_bits = new BitArray(Flip_Endian(output));
                var os = 0;
                var ts = 0;
                if (!output_bits[offset - 1] && !output_bits[offset - 2]) ts += 2;
                offset += os; tstart += ts;
            }
            try
            {
                for (int i = 0; i < tend - tstart; i++) output_bits[offset + i] = source[tstart + i];
            }
            catch { }
            int total_size = offset + (tend - tstart);
            int leadout_len = (((output.Length << 3) - total_size) >> 3);
            var leadout = FastArray.Init(leadout_len, endbyte);
            if (leadptn == 0) leadout[leadout.Length - 1] = 0xbd;
            var leadout_bits = new BitArray(Flip_Endian(leadout));
            for (int i = 0; i < leadout_bits.Length; i++)
            {
                output_bits[total_size + i] = leadout_bits[i];
            }
            output = Bit2Byte(output_bits);
            Check_Sync();
            return output;
        
            void Write_Lead(int li)
            {
                try
                {
                    for (int i = 0; i < li; i++) Buffer.BlockCopy(lead_in, 0, output, 0 + (i * lead_in.Length), lead_in.Length);
                }
                catch { }
            }
        
            void Check_Sync()
            {
                if (leadptn > 0)
                {
                    output[(offset >> 3) - 4] = 0xff;
                    output[(offset >> 3) - 3] = 0xff;
                    output[(offset >> 3) - 2] = 0x55;
                    output[(offset >> 3) - 1] = 0x55;
                }
            }
        
            void Add_Pre_Sync()
            {
                output[(offset / 8) - 3] = 0xff;
                output[(offset / 8) - 2] = 0xff;
                output[(offset / 8) - 1] = 0x55;
            }
        }

        (byte[] data, bool checksum, bool isone, int Position) Decode_Vorpal(BitArray source, int sector = -1, bool dec = true)
        {
            int snc_cnt = 0, psec = 0, sub = dec ? 0 : 8 * 5;
            var inc = 162 << 3;
            for (int k = 0; k < source.Length; k++)
            {
                if (source[k]) snc_cnt++;
                else
                {
                    if (snc_cnt == 8)
                    {
                        int dep = k + 7;
                        if (psec++ == sector && dep + 1290 < source.Length)
                        {
                            var sec_data = Bit2Byte(source, dep, inc);
                            var decoded = Decode_Vorpal_GCR(sec_data);
                            bool isone = source[dep + 1290];
                            bool pass = GetVorpal_Checksum(decoded, CopyFrom(sec_data, 160, 2));
                            return dec ? (decoded, pass, isone, dep) : (sec_data, pass, isone, dep);
                        }
                        k += inc - sub;
                    }
                    snc_cnt = 0;
                }
            }
            return (new byte[0], false, false, 0);
        }

        bool GetVorpal_Checksum(byte[] data, byte[] GCR_value)
        {
            var cksm = 0;
            var ck = CombineNibbles_VPL(VPL_decode_high[GCR_value[0] >> 3], VPL_decode_low[((GCR_value[0] << 2) | (GCR_value[1] >> 6)) & 0x1f]);
            for (int i = 0; i < 128; i++) cksm ^= data[i];
            return ck == cksm;
        }

        (byte[] data, int start, int end, int len, int lead, int sectors, int[] sec_pos, string[] headers) Get_Vorpal_Track_Length(byte[] data, int trk = -1)
        {
            var err = new List<int>();
            bool first_sec_start = false;
            int numbering = 0;
            int track = (tracks > 42) ? (trk / 2) + 1 : trk;
            int secLen = 160 << 3;
            int min_skip_len = vpl_density[density_map[track]] - 100;
            int data_start = 0, data_end = 0, track_len = 0, sectors = 0;
            var sid = string.Empty;
            bool start_found = false, end_found = false;
            var sec_header = new List<string>();
            var sec_hdr = new List<string>();
            var sec_pos = new List<int>();
            var source = new BitArray(Flip_Endian(data));
            var vcksm = string.Empty;
            int sec_zero_pos = 0, longest_gap = 0, last_sec = 0;
            byte[] header = new byte[] { 0x3f, 0xbf };

            int pos = 300;
            byte compare = 0;
            while (pos < source.Length)
            {
                compare <<= 1;
                if (source[pos]) compare |= 1;
                if (header.Any(x => x == compare))
                {
                    var comp = Bit2Byte(source, pos + 1, 16);
                    if (comp[0] == 0xd5 && (comp[1] & 0x80) == 0)
                    {
                        var cur_pos = pos - 7;
                        sid = Hex_Val(Bit2Byte(source, pos - ((com << 3) >> 1), com << 3));
                        if (!sec_header.Any(x => x == sid))
                        {
                            int distance = Math.Abs(cur_pos - last_sec);
                            if (!start_found) { start_found = true; data_start = cur_pos; }
                            if (cur_pos + (164 << 3) < source.Length)
                            {
                                try
                                {
                                    sec_header.Add(sid);
                                    sec_pos.Add(cur_pos);
                                    sectors++;
                                    if (compare == 0x3f)
                                    {
                                        if (sectors == 1) first_sec_start = true; 
                                        sec_zero_pos = cur_pos; numbering = sec_hdr.Count;
                                        longest_gap = Math.Min(160 << 3, Math.Max(cur_pos - last_sec, longest_gap));
                                    }
                                    last_sec = cur_pos + secLen;
                                    if (!batch)
                                    {
                                        byte[] secdata = Decode_Vorpal_GCR(Bit2Byte(source, cur_pos + 17, secLen));
                                        var ckm = GetVorpal_Checksum(secdata, Bit2Byte(source, cur_pos + 17 + secLen, 16));
                                        vcksm = ckm ? "(OK)" : "(Failed!)";
                                        if (!ckm) err.Add(sectors);
                                    }
                                    var sid2 = Byte_to_Binary(Bit2Byte(source, cur_pos, 16), true);
                                    sec_hdr.Add($"pos ({pos >> 3}) Header [{sid2}] Checksum {vcksm}");
                                }
                                catch { }
                            }
                            pos += 1180; // Skip over the next (x) bits after finding a sector
                        }
                        else
                        {
                            end_found = true; data_end = cur_pos - 1;
                            sec_hdr.Add($"* Repeat * pos {pos >> 3} sector {sectors - numbering}");
                        }
                    }
                }
                if (end_found) break;
                pos++;
            }
            //if (first_sec_start && !end_found) data_start = sec_zero_pos;
            //if (start_found && !end_found)
            //{
            //    sub = Find_LeadIn(BitCopy(source, last_sec), true);
            //    data_end = last_sec + sub;
            //}
            
            int sub = Find_LeadIn(BitCopy(source, sec_zero_pos - longest_gap, longest_gap));
            if (sub > 0) sec_zero_pos -= sub;
            if (!end_found)
            {
                if (first_sec_start) data_start = sec_zero_pos;
                sub = Find_LeadIn(BitCopy(source, last_sec), true);
                data_end = last_sec + sub;
            }
            track_len = (data_end - data_start);
            var temp = new BitArray(track_len);
            pos = sec_zero_pos;
            for (int i = 0; i < track_len; i++)
            {
                try
                {
                    temp[i] = source[pos++];
                    if (pos > data_end) pos = data_start;
                }
                catch { }
            }
            var tdata = Bit2Byte(temp);
            var headers = new string[sec_hdr.Count];
            var strt = numbering > 0 ? sectors - numbering : 0;
            for (int i = 0; i < sectors; i++)
            {
                var z = strt == 0 ? "*" : string.Empty;
                headers[i] = $"Sector ({strt++}){z} {sec_hdr[i]}";
                if (strt == sectors) strt = 0;
            }
            if (headers.Length > sectors) headers[headers.Length - 1] = sec_hdr[sec_hdr.Count - 1];
            if (!batch && err.Count > 0) foreach (var s in err) ErrorList.Add($"Checksum failed on track {track}");
            return (tdata, data_start, data_end, track_len, sec_zero_pos, sectors, sec_pos.ToArray(), headers);

            int Find_LeadIn(BitArray gap, bool leadout = false)
            {
                byte[] d = Bit2Byte(gap);
                int count = 0;
                foreach (byte b in d)
                {
                    if (b == 0xaa || b == 0x55) count++;
                    else count = 0;
                    if (count > 8) return 32;
                }
                pos = 0;
                int window = 0;
                for (int i = 0; i < gap.Length; i++)
                {
                    window <<= 1;
                    if (gap[i]) window |= 1;
                    if ((window & 0x00ffff00) == 0xb5b500 && (window & 0x000000ff) != 0xb5) return !leadout ? gap.Length - (i + 1) : i + 1;
                }
                return sec_zero_pos;
            }
        }

        void Density_Reset()
        {
            VPL_density_reset.Visible = false;
            RunBusy(() =>
            {
                for (int i = 0; i < vpl_density.Length; i++)
                {
                    vpl_density[i] = vpl_defaults[i];
                    switch (i)
                    {
                        case 0: VD0.Value = vpl_density[i]; break;
                        case 1: VD1.Value = vpl_density[i]; break;
                        case 2: VD2.Value = vpl_density[i]; break;
                        case 3: VD3.Value = vpl_density[i]; break;
                    }
                }
            });
            Vorpal_Rebuild();
        }
    }
}