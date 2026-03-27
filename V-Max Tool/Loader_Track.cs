using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        //byte[] Get_VmaxLoaderSegment(byte[] data, bool cbm = false)
        (byte[] segment, bool obfuscated) Get_VmaxLoaderSegment(byte[] data, bool cbm = false)
        {
            if (data == null || data.Length == 0) return (null, false);

            byte[] custom = new byte[] // Bytes of loader possibly followed with arbitrary sync on later V-Max v2+ versions
            {
                0x27, 0x2B, 0x37, 0x3B, 0x4B, 0x4F, 0x53, 0x57, 0x5B, 0x67, 0x6B, 0x6F, 0x73, 0x77, 0x7B, 0x93,
                0x9B, 0xA7, 0xAB, 0xAF, 0xB3, 0xB7, 0xBB, 0xCB, 0xCF, 0xD3, 0xD7, 0xDB, 0xE7, 0xEB, 0xF3,
            };

            byte[] std = new byte[] // early V-Max loader version bytes followed by arbitrary sync
            {
                0xcb, 0xb3, 0xeb, 0xe3, 0x97, 0xa7, 0xbb, 0xd3, 0xd7
            };

            int async = 0;
            BitArray s = new BitArray(0);
            if (!Padding_First())
            {
                byte[] comp = CopyArray(data, 1, 256); // 128
                for (int i = 257; i < data.Length; i++)
                {
                    if (MatchSeq(data, comp, i)) s = new BitArray(Flip_Endian(Rotate_Loader(CopyArray(data, 1, i - 1), true)));
                }
            }
            else s = new BitArray(Flip_Endian(data));
            //SaveBin(Bit2Byte(s), "v3contra");
            int compare = 0, pos = 0;
            int find = 0x005a0037;
            byte[] fiveAchunk = new byte[] { 0x5a, 0x55, 0x56, 0xff };
            while (pos < s.Length)
            {
                compare <<= 1;
                if (s[pos++]) compare |= 1;
                if ((compare & 0x00ff00ff) == find && fiveAchunk.Any(x => x == (compare & 0xff000000) >> 24))
                {
                    int rep = 0, len = Math.Min(5120 << 3, s.Length - pos);
                    var temp = Bit2Byte(s, pos, len);
                    //var tmppp = Bit2Byte(s, pos - (512 << 3), len);
                    //File.WriteAllBytes($@"c:\test\v3l.bin", tmppp);
                    for (int i = 1000; i < temp.Length; i++)
                    {
                        if (temp[i] == temp[i - 1]) rep++;
                        else rep = 0;
                        if (rep > 15)
                        {
                            /// ------------------------  Test Code ---------------------------
                            // ---- Check if this is a loader produced by Revolution V ------------------------------------------
                            var ldr_seg = Bit2Byte(s, pos, (i - (rep - (cbm ? 1 : 0))) << 3);
                            for (int j = 0; j < ldr_seg.Length; j++)
                            {   // Revolution V adds $55 gap and $4fffff sync before each loader sector
                                if (MatchSeq(ldr_seg, new byte[] { 0x55, 0x55, 0x4f, 0xff }, j)) return (ldr_seg, false);
                            }
                            // ---- Return un-processed loader if it is. Routines need more work to handle Rev-V loaders --------
                            /// ------------------ Remove if causes issues --------------------
                            // This next line filters arbitrary sync from V-Max loader segment to yield the true GCR data
                            var tmp = Filter_Sync(BitCopy(s, pos, (i - (rep - (cbm ? 1 : 0))) << 3), cbm ? std : custom)
                                .Where(b => b != 0xff).ToArray();
                            len = tmp.Length;
                            // Trim length to remove trailing garbage data or weak-bits
                            if (tmp.Length > 2056 && tmp.Length < 2200) len = 2056; // v-max v0-1 loader length
                            if (tmp.Length > 2701 && tmp.Length < 3000) len = 2701; // v-max v2   loader length
                            if (tmp.Length > 3089) len = 3089;                      // v-max v3-4 loader length
                            //SaveBin(CopyArray(tmp, 0, len), "contraseg");
                            return (CopyArray(tmp, 0, len), async > 1);
                        }
                    }
                }
            }
            return (null, false);

            bool Padding_First()
            {
                byte chk = data[0];
                for (int i = 1; i < 5; i++) if (data[i] != chk) return false;
                return true;
            }

            byte[] Filter_Sync(BitArray d, byte[] PossibleSync)
            {
                if (d == null || d.Count == 0) return new byte[0];
                byte[] garbage = new byte[] { 0x00, 0x11, 0x22, 0x44, 0x88 };
                List<byte> filtered = new List<byte>();
                byte window = 0;
                int bytePos = 0, posi = 0;
                while (posi < d.Length)
                {
                    window <<= 1;
                    if (d[posi]) window |= 1;
                    if (++bytePos % 8 == 0)
                    {
                        if (!garbage.Contains(window)) filtered.Add(window);
                        bytePos = 0;
                        if (PossibleSync.Any(x => x == window))
                        {
                            try
                            {
                                // checking for 6+ '1' bits in a row (more than 5 is invalid GCR, signals arbitrary sync obfuscation)
                                byte wdw = (byte)(window & 0x07);
                                if ((wdw == 3 && d[posi + 1] && d[posi + 2] && d[posi + 3] && d[posi + 4]) ||
                                    (wdw == 7 && d[posi + 1] && d[posi + 2] && d[posi + 3])) Reposition();
                            }
                            catch { }
                        }
                    }
                    posi++;
                }
                return filtered.ToArray();


                void Reposition()
                {
                    // arbitrary sync found, skipping forward to next '0' bit, this is the start of the next real GCR byte
                    while (posi < d.Length && d[posi]) posi++;
                    bytePos = 1; // First '0' bit found of next byte, just need the next 7
                    window = 0; // clear window and set bytePos to 1, continue assembling the next byte
                    async++;
                }
            }
        }

        /// ---------------------------------- Get_LeadIn_Position Length of Loader Track ---------------------------------------------
        (int, byte[]) Get_Loader_Len(byte[] data, int start_pos, int comp_length, int skip_length)
        {
            int q = 8192;
            int qq = 0;
            byte[] dataa = data;
            while (q == 8192 && qq < 32)
            {
                q = find();
                if (q == 8192)
                {
                    dataa = Rotate_Left(data, qq);
                    qq++;
                }
            }
            return (q, dataa);

            int find()
            {
                int p = 0;
                if (dataa != null)
                {
                    byte[] star = new byte[comp_length];
                    Buffer.BlockCopy(dataa, start_pos, star, 0, comp_length);
                    byte[] comp = new byte[8192 - (comp_length + start_pos)];
                    Buffer.BlockCopy(dataa, comp_length, comp, 0, 8192 - (comp_length + start_pos));

                    for (p = skip_length; p < comp.Length; p++)
                    {
                        if (comp.Skip(p).Take(star.Length).SequenceEqual(star)) break;
                    }
                }
                return p + comp_length;
            }
        }

        /// ------------------------- Rotate Loader Track -------------------------------------------

        byte[] Rotate_Loader(byte[] temp, bool force = false)
        {
            ///------- Checks to see if Loader track contains V-Max Headers (found on Mindscape titles) -----------
            bool rotated = false;
            if (Disk.Loader.Length > 0)
            {
                byte[] sb = new byte[1]; byte[] eb = new byte[1];
                sb[0] = Disk.Loader[0];
                eb[0] = Disk.Loader[1];
                int vs = Convert.ToInt32(Disk.Loader[2]);
                byte[] comp = new byte[2];
                for (int j = 0; j < 8; j++)
                {
                    byte[] tmp = new byte[temp.Length];
                    Buffer.BlockCopy(temp, 0, tmp, 0, tmp.Length);
                    BitArray s_bArray = new BitArray(Flip_Endian(tmp));
                    BitArray d_bArray = new BitArray(s_bArray.Count);
                    int dp = 0;
                    for (int h = j; h < s_bArray.Length; h++)
                    {
                        d_bArray[dp] = s_bArray[h];
                        dp++;
                        if (dp == d_bArray.Length) dp = 0;
                    }
                    byte[] cc = Bit2Byte(d_bArray);
                    int sec = 0;
                    for (int i = 0; i < cc.Length - 5; i++)
                    {

                        if (cc[i] == sb[0]) Buffer.BlockCopy(cc, i + 1, comp, 0, comp.Length);
                        if (vm2_ver[vs].Any(s => s == Hex_Val(comp)))
                        {
                            for (int g = (i + 2); g < cc.Length; g++)
                            {
                                if (cc[g] == eb[0] && g < (i + 40) && g > (i + 5))
                                {
                                    sec++;
                                    i += 340;
                                    if (sec > 1)
                                    {
                                        rotated = true;
                                        temp = Rotate_Left(temp, i + 1);
                                        goto End_rotate;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        ///----------------------------------------------------------------------------------------------------------
        End_rotate:
            if (!rotated || force)
            {
                int start = 0;
                int longest = 0;
                int count = 0;
                for (int i = 1; i < temp.Length; i++)
                {
                    if (temp[i] != temp[i - 1]) count = 0;
                    count++;
                    if (count > longest)
                    {
                        start = i - count;
                        longest = count;
                    }
                }
                if (longest > 2)
                {
                    temp = Rotate_Left(temp, start + (longest / 2));
                }
            }
            return temp;
        }

        byte[] Mod_Loader_Mod(byte[] data, byte[] headers) // Used when Swap Headers option is checked
        {
            if (data == null) return new byte[0];
            byte[] hdr = new byte[] { 0x64, 0x4e, 0x46 };
            if (headers == null || headers.Length > 2 || headers[0] == headers[1]) return data;
            if (hdr.Any(x => x == headers[0]) && hdr.Any(x => x == headers[1]))
            {
                (byte[][] temp, bool[] cksm) = Decode_VM_Loader(data);
                // Make sure checksums are all OK, and make sure this is a V-Max v2 Loader (7 sectors)
                if (cksm.Any(x => x == false) || temp.Length != 7) return data;
                // Swap header values according to selected values from Advanced Options
                if (temp[3][86] == 0x64 || temp[3][86] == 0x4e) temp[3][86] = headers[0];
                if (temp[3][124] == 0x46 || temp[3][124] == 0x4e || temp[3][124] == 0x64) temp[3][124] = headers[1];
                // Add non-weak GCR conversion references to tables missing these references. ($AD/EA)
                if (temp[5][173] == 0x00) temp[5][173] = 0x2c;
                if (temp[5][234] != 0x0a) temp[5][234] = 0x0a;
                // Re-encode the loader and send it back to the calling function
                return Encode_VM_Loader(temp);
            }
            return data;
        }
    }
}