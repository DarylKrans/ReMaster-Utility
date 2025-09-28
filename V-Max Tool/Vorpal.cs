using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
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

        (int sectors, int first, int last_pos, byte[] ID) Get_VPL_Sectors(BitArray source, int trk = -1)
        {
            var sectors = 0;
            var first = 0;
            var last = 0;
            uint compare = 0;
            int pos = 0;
            int s0 = 0x3fd500;
            int s1 = 0xbfd500;
            uint s3 = 0xfffff525;
            byte[] id = null;
            List<string> list = new List<string>();
            while (pos < source.Length)
            {
                compare <<= 1;
                if (source[pos++]) compare |= 1;
                if ((compare & 0xffff80) == s0 || (compare & 0xffff80) == s1)
                {
                    if (sectors < 1) first = pos - 32;
                    sectors++;
                    last = pos - 8;
                }
                if (compare == s3) id = Bit2Byte(source, pos - 12, 80);
            }
            return (sectors, first, last, id);
        }

        byte[] Rebuild_Vorpal(byte[] data, int trk, int leadptn = 0)
        {
            int offset;
            int last_sector = 1300; // Sector length (160 * 3 bits) + checksum (10 bits) + 10 bit sector identifier
                                    // Previously set to 1312 before I figured out how to read the sector ID
            int d = 0;
            trk = tracks > 42 ? (trk >> 1) + 1 : trk + 1;
            int tlen = data.Length;
            if (VPL_auto_adj.Checked || VPL_rb.Checked) d = VPL_Density(tlen);
            //if (VPL_auto_adj.Checked || VPL_rb.Checked) d = density[Get_Density(tlen)];
            var source = new BitArray(Flip_Endian(data));
            (int sectors, int tstart, int tend, _) = Get_VPL_Sectors(source, trk > 42 ? (trk >> 1) + 1 : trk + 1);
            tend += last_sector;
            byte[] lead_in = new byte[0];
            byte endbyte = leadptn == 1 ? (byte)0x55 : (leadptn == 2 ? (byte)0xAA : (byte)0xb5);
            switch (leadptn)
            {
                case 0: lead_in = new byte[] { 0xd5, 0x35, 0x4d, 0x53, 0x54 }; break;
                case 1: lead_in = FastArray.Init(5, 0x55); break;
                case 2: lead_in = FastArray.Init(5, 0xaa); break;
            }
            byte[] output = new byte[tlen];
            if (!VPL_only_sectors.Checked) Write_Lead(100);
            var output_bits = new BitArray(Flip_Endian(output));
            offset = (((output_bits.Length - (tend - tstart)) >> 1) >> 3) << 3;
            if (VPL_lead.Checked) offset = Convert.ToInt32(Lead_In.Value) << 3;
            if (VPL_only_sectors.Checked)
            {
                var asize = (tend - tstart + 7) >> 3;
                // Minimum track length for Vorpal (when "Fastest Writing Speed Possible" is enabled).
                // Uses 7726 bytes for >44 sectors (297.9 RPM) instead of 7728 (297.8 RPM).
                // On 47-sector tracks this leaves only 1 spare byte — slight risk of truncation on odd-ball images.
                var size = sectors > 41 ? 7726 : density[d];
                var tsize = size > density[d] ? size : density[d];
                var offset_adj = (tsize - asize) >> 1 < 3 ? 3 : (tsize - asize) >> 1;
                offset = offset_adj >= 60 ? 60 << 3 : offset_adj << 3;
                output = new byte[tsize];
                Write_Lead(offset + 5);
                if (VPL_presync.Checked) Add_Pre_Sync();
                output[output.Length - 1] = 0x55;
                output_bits = new BitArray(Flip_Endian(output));
            }
            if ((VPL_auto_adj.Checked || VPL_rb.Checked) && !(VPL_only_sectors.Checked || VPL_lead.Checked))
            {
                offset = ((vpl_density[d] << 3) - (tend - tstart)) >> 1 >> 3 << 3;
                offset = (offset > 60 << 3) ? 60 << 3 : (offset < 15 << 3) ? 15 << 3 : offset;
            }
            if (endbyte != 0xb5) offset = 24;

            if (VPL_auto_adj.Checked)
            {
                var asize = (tend - tstart + 7) >> 3;

                output = new byte[trk < 18 ? vpl_density[d] : density[density_map[trk - 1]]];
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
            int leadout_len = ((output.Length << 3) - total_size) >> 3;
            var leadout = FastArray.Init(leadout_len, endbyte);
            if (leadptn == 0) leadout[leadout.Length - 1] = 0xbd;
            var leadout_bits = new BitArray(Flip_Endian(leadout));
            for (int i = 0; i < leadout_bits.Length; i++) output_bits[total_size + i] = leadout_bits[i];
            output = Bit2Byte(output_bits);
            if (leadptn > 0) Add_Pre_Sync();
            return output;

            void Write_Lead(int li)
            {
                try
                {
                    for (int i = 0; i < li; i++) Buffer.BlockCopy(lead_in, 0, output, 0 + (i * lead_in.Length), lead_in.Length);
                }
                catch { }
            }

            void Add_Pre_Sync()
            {
                output[(offset >> 3) - 3] = 0xff;
                output[(offset >> 3) - 2] = 0xff;
                output[(offset >> 3) - 1] = 0x55;
            }
        }

        (byte[] data, bool checksum, bool isone, int Position) Decode_Vorpal(BitArray source, int sector = -1, bool dec = true)
        {
            byte[] chk = new byte[] { 0x7f, 0xaa };
            int inc = (162 << 3) - 6;
            int pos = 0;
            ushort window = 0;
            while (pos < source.Length)
            {
                window <<= 1;
                if (source[pos]) window |= 1;
                if ((window >> 8) == chk[0] && (window & 0xff) == chk[1])
                {
                    var p = pos + 1;
                    var secnum = p + 1299 < source.Length ? Get_VPL_SecNum(Bit2Byte(source, p + inc, 9)) : -1;
                    if (secnum == sector)
                    {
                        var sec_data = Bit2Byte(source, p, inc);
                        var decoded = Decode_Vorpal_GCR(sec_data);
                        bool isone = source[p + 1290];
                        bool pass = GetVorpal_Checksum(decoded, CopyFrom(sec_data, 160, 2));
                        return dec ? (decoded, pass, isone, p) : (sec_data, pass, isone, p);
                    }
                    pos += inc - 20;
                }
                pos++;
            }
            return (new byte[0], false, false, 0);
        }

        int Get_VPL_SecNum(byte[] input)
        {
            // Deconstruct Vorpal 6-bit sector ID# (0-46) from 9-bit encoding { 0x00x00x0 } where 'x' is ignored
            int secnum = 0;
            secnum |= ((input[0] >> 7) & 1) << 5; // keep bit7 of input[0]
            secnum |= ((input[0] >> 5) & 1) << 4; // keep bit5 of input[0]
            secnum |= ((input[0] >> 4) & 1) << 3; // keep bit4 of input[0]
            secnum |= ((input[0] >> 2) & 1) << 2; // keep bit2 of input[0]
            secnum |= ((input[0] >> 1) & 1) << 1; // keep bit1 of input[0]
            secnum |= ((input[1]) >> 7) << 0;     // keep bit7 of input[1]
            return secnum;
        }

        BitArray Make_VPL_SecNum(int secnum)
        {
            // Reconstruct Vorpal 9-bit encoded sector ID from 6-bit integer { 000000 = 0x00x00x0 } 
            byte[] b = new byte[] { 0x49, 0x7f }; // Blank sector ID with filler bits { [01001001-0]1111111 }
            if ((secnum & (1 << 5)) != 0) b[0] |= 1 << 7; // secnum[5] = bit7
            if ((secnum & (1 << 4)) != 0) b[0] |= 1 << 5; // secnum[4] = bit5
            if ((secnum & (1 << 3)) != 0) b[0] |= 1 << 4; // secnum[3] = bit4
            if ((secnum & (1 << 2)) != 0) b[0] |= 1 << 2; // secnum[2] = bit2
            if ((secnum & (1 << 1)) != 0) b[0] |= 1 << 1; // secnum[1] = bit1
            if ((secnum & (1 << 0)) != 0) b[1] |= 1 << 7; // secnum[0] = bit9 (highes bit of b[1])
            // The following lines change the filler bit from 1 to 0 if the bit before and after are 1's -- maintains Vorpal GCR rules
            if ((secnum & (1 << 0)) != 0 && (secnum & (1 << 1)) != 0) b[0] = (byte)(b[0] & ~1);
            if ((secnum & (1 << 2)) != 0 && (secnum & (1 << 3)) != 0) b[0] = (byte)(b[0] & ~(1 << 3));
            // Next line isn't necessary since Vorpal sectors never exceed a value of 46
            if ((secnum & (1 << 4)) != 0 && (secnum & (1 << 5)) != 0) b[0] = (byte)(b[0] & ~(1 << 5));
            // return the new 9-bit sector ID
            return BitCopy(new BitArray(Flip_Endian(b)), 0, 9);
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
            bool first_sec_start = false, start_found = false, end_found = false;
            int track = (tracks > 42) ? (trk / 2) + 1 : trk;
            int secLen = 160 << 3;
            int comp_len = 20;
            int min_skip_len = vpl_density[density_map[track]] - 100;
            int data_start = 0, data_end = 0, track_len = 0, sectors = 0, sec_zero_pos = 0, longest_gap = 0, last_sec = 0, pos = 0;
            byte compare = 0;
            byte[] header = new byte[] { 0x3f, 0xbf };
            string sid = string.Empty;
            string vcksm = string.Empty;
            List<string> sec_header = new List<string>();
            List<string> sec_hdr = new List<string>();
            List<int> sec_pos = new List<int>();
            List<int> err = new List<int>();
            BitArray source = new BitArray(Flip_Endian(data));
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
                        var secnum = cur_pos + (163 << 3) < source.Length
                            ? Get_VPL_SecNum(Bit2Byte(source, cur_pos + 17 + (161 << 3) + 2, 9)) : -1;
                        sid = Hex_Val(Bit2Byte(source, pos - ((comp_len << 3) >> 1), comp_len << 3));
                        // skip first sector found if it resides within the 1st 38 bytes of the NIB track to avoid possible header corruption
                        // also sets variable 'last_sec' to the end of the found sector to calculate Lead-In length if next sector is '0'
                        if (pos < 300 && secnum < 47) last_sec = cur_pos + 17 + secLen + 20;
                        else if (!sec_header.Any(x => x == sid))
                        {
                            int distance = Math.Abs(cur_pos - last_sec);
                            if (!start_found) { start_found = true; data_start = cur_pos; }
                            if (cur_pos + (164 << 3) < source.Length)
                            {
                                try
                                {
                                    sec_header.Add(sid);
                                    sec_pos.Add(cur_pos >> 3);
                                    sectors++;
                                    if (compare == 0x3f)
                                    {
                                        if (sectors == 1) first_sec_start = true;
                                        sec_zero_pos = cur_pos;
                                        longest_gap = Math.Min(160 << 3, Math.Max(cur_pos - last_sec, longest_gap));
                                    }
                                    last_sec = cur_pos + 17 + secLen + 20;
                                    if (!batch)
                                    {
                                        byte[] secdata = Decode_Vorpal_GCR(Bit2Byte(source, cur_pos + 17, secLen));
                                        var ckm = GetVorpal_Checksum(secdata, Bit2Byte(source, cur_pos + 17 + secLen, 10));
                                        vcksm = ckm ? "(OK)" : "(Failed!)";
                                        if (!ckm) err.Add(sectors);
                                    }
                                    var sid2 = Byte_to_Binary(Bit2Byte(source, cur_pos, 16), true);
                                    sec_hdr.Add($"pos ({pos >> 3}) Sector ({secnum}){(secnum == 0 ? "*" : string.Empty)} Header [{sid2}] Checksum {vcksm}");
                                }
                                catch { }
                            }
                            pos += 1180; // Skip over the next (x) bits after finding a sector
                        }
                        else
                        {
                            end_found = true; data_end = cur_pos - 1;
                            sec_hdr.Add($"* Repeat * pos {pos >> 3} sector {secnum}");
                        }
                    }
                }
                if (end_found) break;
                pos++;
            }

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
            if (!batch && err.Count > 0) foreach (var s in err) ErrorList.Add($"Checksum failed on track {track}");
            return (tdata, data_start, data_end, track_len, sec_zero_pos, sectors, sec_pos.ToArray(), sec_hdr.ToArray());

            int Find_LeadIn(BitArray gap, bool leadout = false)
            {
                if (gap == null || gap.Length < 1) return sec_zero_pos;
                // Check for authentic track lead-out (0xb5b5bx?)
                pos = 0;
                int window = 0;
                for (int i = 0; i < gap.Length; i++)
                {
                    window <<= 1;
                    if (gap[i]) window |= 1;
                    if ((window & 0x00ffff00) == 0xb5b500 && (window & 0x000000ff) != 0xb5) return !leadout ? gap.Length - (i + 1) : i + 1;
                }
                // If authentic lead-out not found, check for inert padding (0x55/0xaa)
                byte[] d = Bit2Byte(gap);
                int count = 0;
                foreach (byte b in d)
                {
                    if (b == 0xaa || b == 0x55) count++;
                    else count = 0;
                    if (count > 8) return 32;
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