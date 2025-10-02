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
        byte[] Get_VmaxLoader_CBM(byte[] data)
        {
            byte[] possnc = new byte[] { 0xcb, 0xb3, 0xeb, 0xe3, 0x97, 0xa7, 0xbb, 0xd3, 0xd7 }; // these tend to have arbitrary sync following
            List<byte> filtered = new List<byte>();
            BitArray s = new BitArray(Flip_Endian(Get_VmaxLoaderSegment(data, true)));
            byte window = 0;
            int pos = 0, bytePos = 0;
            // filter through array on bit-level to find relevant loader code while removing arbitrary sync obfuscation
            while (pos < s.Length)
            {
                window <<= 1;
                if (s[pos]) window |= 1;
                if (++bytePos % 8 == 0)
                {
                    filtered.Add(window);
                    bytePos = 0;
                    if (possnc.Any(x => x == window))
                    {
                        // checking for 6+ '1' bits in a row (more than 5 is invalid GCR, signals arbitrary sync obfuscation)
                        byte wdw = (byte)(window & 0x07);
                        if ((wdw == 3 && s[pos + 1] && s[pos + 2] && s[pos + 3] && s[pos + 4]) ||
                            (wdw == 7 && s[pos + 1] && s[pos + 2] && s[pos + 3])) Reposition();
                    }
                }
                pos++;
            }
            return filtered.ToArray(); // return (what should be) the real loader GCR data for decoding

            void Reposition()
            {
                // arbitrary sync found, skipping forward to next '0' bit, this is the start of the next real GCR byte
                while (pos < s.Length && s[pos]) pos++;
                bytePos = 1; // First '0' bit found of next byte, just need the next 7
                window = 0; // clear window and set bytePos to 1, continue assembling the next byte
            }
        }

        //byte[] Get_VmaxLoader_CBM(byte[] data)
        //{
        //    byte[] valid = new byte[]
        //    {
        //        0x45, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x59, 0x5A, 0x5B, 0x5C,
        //        0x5D, 0x5E, 0x63, 0x64, 0x65, 0x66, 0x67, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x72, 0x73, 0x74, 0x75,
        //        0x76, 0x77, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x93, 0x94, 0x95, 0x96, 0x97, 0x99, 0x9A, 0x9B, 0x9C,
        //        0x9D, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA9, 0xAA, 0xAC, 0xAD, 0xAE, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB9,
        //        0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xC5, 0xC7, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xD2, 0xD3, 0xD4, 0xD5,
        //        0xD7, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE
        //    };
        //    byte[] possnc = new byte[] { 0xcb, 0xb3, 0xeb, 0xe3, 0x97, 0xa7, 0xbb, 0xd3, 0xd7 }; // these tend to have arbitrary sync following
        //    List<byte> fnl = new List<byte>();
        //    BitArray s = new BitArray(Flip_Endian(Get_VmaxLoaderSegment(data, true)));
        //    byte window = 0;
        //    int pos = 0, bytePos = 0;
        //    // filter through array on bit-level to find relevant loader code while removing arbitrary sync obfuscation
        //    while (pos < s.Length)
        //    {
        //        window <<= 1;
        //        if (s[pos]) window |= 1;
        //        if (++bytePos % 8 == 0)
        //        {
        //            if (valid.Any(x => x == window))
        //            {
        //                fnl.Add(window);
        //                bytePos = 0;
        //                if (possnc.Any(x => x == window))
        //                {
        //                    // checking for 6+ '1' bits in a row (more than 5 is invalid GCR, signals arbitrary sync obfuscation)
        //                    byte wdw = (byte)(window & 0x07);
        //                    if ((wdw == 3 && s[pos + 1] && s[pos + 2] && s[pos + 3] && s[pos + 4]) ||
        //                        (wdw == 7 && s[pos + 1] && s[pos + 2] && s[pos + 3])) Reposition();
        //                }
        //            }
        //            else Reposition();
        //        }
        //        pos++;
        //    }
        //    return fnl.ToArray(); // return (what should be) the real loader GCR data for decoding
        //
        //    void Reposition()
        //    {
        //        // arbitrary sync found, skipping forward to next '0' bit, this is the start of the next real GCR byte
        //        while (pos < s.Length && s[pos]) pos++;
        //        bytePos = 1; // First '0' bit found of next byte, just need the next 7
        //        window = 0; // clear window and set bytePos to 1, continue assembling the next byte
        //    }
        //}

        byte[] Get_VmaxLoaderSegment(byte[] data, bool cbm = false)
        {
            if (data == null || data.Length == 0) return null;
            BitArray s = new BitArray(0);
            if (!Padding_First())
            {
                byte[] comp = CopyArray(data, 1, 128);
                for (int i = 129; i < data.Length; i++)
                {
                    if (MatchSeq(data, comp, i)) s = new BitArray(Flip_Endian(Rotate_Loader(CopyArray(data, 1, i - 1))));
                }
            }
            else s = new BitArray(Flip_Endian(data));
            int compare = 0, pos = 0;
            int find = 0x005a0037;
            byte[] fiveAchunk = new byte[] { 0x5a, 0x55, 0x56, 0xff };
            while (pos < s.Length)
            {
                compare <<= 1;
                if (s[pos++]) compare |= 1;
                if ((compare & 0x00ff00ff) == find && (fiveAchunk.Any(x => x == (compare & 0xff000000) >> 24)))
                {
                    var temp = Bit2Byte(s, pos, Math.Min(5120 << 3, s.Length - pos));
                    int rep = 0;
                    for (int i = 1000; i < temp.Length; i++)
                    {
                        if (temp[i] == temp[i - 1]) rep++;
                        else rep = 0;
                        if (rep > 15)
                        {
                            if (cbm) return CopyArray(temp, 0, i - (rep - 1));
                            else return CopyArray(temp, 0, i - rep).Where(b => b != 0xff).ToArray();
                        }
                    }
                }
            }
            return null;

            bool Padding_First()
            {
                byte chk = data[0];
                for (int i = 1; i < 5; i++) if (data[i] != chk) return false;
                return true;
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
                        if (comp.Skip(p).Take(star.Length).SequenceEqual(star))
                        {
                            break;
                        }
                    }
                }
                return p + comp_length;
            }
        }

        /// ------------------------- Rotate Loader Track -------------------------------------------

        byte[] Rotate_Loader(byte[] temp)
        {
            ///------- Checks to see if Loader track contains V-Max Headers (found on Mindscape titles) -----------
            bool rotated = false;
            if (NDS.Loader.Length > 0)
            {
                byte[] sb = new byte[1]; byte[] eb = new byte[1];
                sb[0] = NDS.Loader[0];
                eb[0] = NDS.Loader[1];
                int vs = Convert.ToInt32(NDS.Loader[2]);
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
            if (!rotated)
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

        byte[] Pad_Loader(byte[] data, byte padding, int Density)
        {
            MemoryStream buffer = new MemoryStream();
            BinaryWriter write = new BinaryWriter(buffer);
            var pad_len = (density[Density] - data.Length) / 2;
            pad();
            write.Write(data);
            pad();
            return buffer.ToArray();

            void pad()
            {
                for (int i = 0; i < pad_len; i++) write.Write((byte)padding);
            }
        }

        /// ------------------------ Add Sync to Loader Track --------------------------------------------------


        void Fix_Loader_Option(bool draw, int i) //, bool swap = false)
        {
            var trk_num = 0;
            if (f_load.Checked)
            {
                var tt = 0;
                if (tracks > 42) trk_num = (i / 2) + 1; else trk_num = i;
                Original.G = new byte[NDG.Track_Data[i].Length];
                Original.A = new byte[NDA.Track_Data[i].Length];
                Buffer.BlockCopy(NDG.Track_Data[i], 0, Original.G, 0, NDG.Track_Data[i].Length);
                Buffer.BlockCopy(NDA.Track_Data[i], 0, Original.A, 0, NDA.Track_Data[i].Length);
                var d = Get_Density(NDG.Track_Data[i].Length);
                if (NDS.cbm.Any(x => x == 3))
                {
                    if (NDG.Track_Data[i].Length > density[d]) Shrink_Loader(i);
                    byte[] temp = Rotate_Loader(NDG.Track_Data[i]);
                    NDG.L_Rot = true;
                    Set_Dest_Arrays(Fix_Loader(temp), i);
                    FL();
                }
                if (!(NDS.cbm.Any(x => x == 2) || NDS.cbm.Any(x => x == 3)))
                {
                    Set_Dest_Arrays(Pad_Loader(v2ldrcbm, loader_padding, density_map[trk_num]), i);
                    FL();
                }
                if (NDS.cbm.Any(x => x == 2))
                {
                    for (int x = 0; i < tracks; x++) if (NDS.v2info[x]?.Length > 0) { tt = x; break; }
                    if (!V2_swap_headers.Checked)
                    {
                        if (Hex_Val(NDS.v2info[tt], 0, 2) == "4E-64") Set_Dest_Arrays(Pad_Loader(v24e64pal, loader_padding, density_map[trk_num]), i);
                        if (Hex_Val(NDS.v2info[tt], 0, 2) == "64-46") Set_Dest_Arrays(Pad_Loader(v26446ntsc, loader_padding, density_map[trk_num]), i);
                        if (Hex_Val(NDS.v2info[tt], 0, 2) == "64-4E") Set_Dest_Arrays(Pad_Loader(v2644entsc, loader_padding, density_map[trk_num]), i);
                    }
                    else
                    {
                        if (Hex_Val(NDG.newheader, 0, 2) == "4E-64") Set_Dest_Arrays(Pad_Loader(v24e64pal, loader_padding, density_map[trk_num]), i);
                        if (Hex_Val(NDG.newheader, 0, 2) == "64-46") Set_Dest_Arrays(Pad_Loader(v26446ntsc, loader_padding, density_map[trk_num]), i);
                        if (Hex_Val(NDG.newheader, 0, 2) == "64-4E") Set_Dest_Arrays(Pad_Loader(v2644entsc, loader_padding, density_map[trk_num]), i);
                    }
                    FL();
                }
                loader_fixed = true;
            }
            if (!f_load.Checked)
            {
                Invoke(new Action(() =>
                {
                    f_load.Text = "Fix Loader";
                    if (tracks > 0) i = Array.FindIndex(NDS.cbm, s => s == 4);
                    if (i > -1 && i < 100)
                    {
                        if (Original.A.Length > 0) { NDA.Track_Data[i] = Original.A; }
                        if (Original.G.Length > 0)
                        {
                            NDG.Track_Data[i] = Original.G; f_load.Text += " (Restored)";
                            NDG.L_Rot = false;
                        }
                        loader_fixed = false;
                    }
                }));
            }
            displayed = false;
            drawn = false;
            if (draw)
            {
                Check_Before_Draw(false);
                Data_Viewer();
            }

            void FL()
            {
                Invoke(new Action(() => f_load.Text = "Fix Loader (Fixed)"));
            }
        }

        byte[] Fix_Loader(byte[] data)
        {
            //byte[] tdata = data;
            byte[] v2 = new byte[] { 0x5b, 0x57, 0x52, 0x4d }; // Cinemaware and some other v2 variants
            byte[] v3 = new byte[] { 0xaa, 0xaf, 0xda, 0x5f }; // V3 Taito (arkanoid)
            byte[] v1 = new byte[] { 0xaa, 0xbf, 0xb4, 0xbf }; // v3 Taito (bubble bobble)
            byte[] v4 = new byte[] { 0x6b, 0xd9, 0xb6, 0xdd }; // Sega
            //byte[] comp = new byte[4];
            bool f = false;
            for (int i = 0; i < data.Length - 4; i++)
            {
                //Buffer.BlockCopy(tdata, i, comp, 0, comp.Length);
                if (MatchSeq(data, v1, i)) { Patch_V3(i - 4); f = true; }
                if (MatchSeq(data, v2, i)) { Patch_V2(i - 3); f = true; }
                if (MatchSeq(data, v3, i)) { Patch_V3(i - 4); f = true; }
                if (MatchSeq(data, v4, i)) { Patch_V2(i - 3); f = true; }
                if (f) break;
            }
            if (f) Invoke(new Action(() => f_load.Text = "Fix Loader (Fixed)"));
            return data;

            void Patch_V2(int pos)
            {
                if (pos > 0)
                {
                    data[pos] = 0xde;
                    data[pos + 1] = 0xff;
                    data[pos + 2] = 0xff;
                }
            }

            void Patch_V3(int pos)
            {
                if (pos > 0)
                {
                    data[pos] = 0x5f;
                    data[pos + 1] = 0xff;
                    data[pos + 2] = 0xff;
                }
            }
        }

        byte[] Lengthen_Loader(byte[] data, int Density)
        {
            if (data.Length > 0)
            {
                byte[] temp = new byte[density[Density]];
                int current = 0;
                int longest = 0;
                int pos = 0;
                byte fill = 0x00;
                byte cur = 0x00;
                int a = temp.Length - data.Length;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == cur) current++;
                    else
                    {
                        cur = data[i];
                        if (current > longest)
                        {
                            pos = i - 1;
                            longest = current;
                            fill = data[i - 1];
                        }
                        current = 0;
                    }
                }
                Buffer.BlockCopy(data, 0, temp, 0, pos);
                for (int i = pos; i < pos + a; i++) temp[i] = fill;
                Buffer.BlockCopy(data, pos, temp, pos + a, data.Length - pos);
                return temp;
            }
            else return data;
        }

        void Shrink_Loader(int trk)
        {
            byte[] temp = Shrink_Track(NDG.Track_Data[trk], 1);
            Set_Dest_Arrays(temp, trk);
        }
    }
}