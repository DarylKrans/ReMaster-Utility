using System;
using System.Collections;
using System.Linq;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        /// ---------------------------------- Get Length of Loader Track ---------------------------------------------
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
                    Array.Copy(dataa, start_pos, star, 0, comp_length);
                    byte[] comp = new byte[8192 - (comp_length + start_pos)];
                    Array.Copy(dataa, comp_length, comp, 0, 8192 - (comp_length + start_pos));

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
            //------- Checks to see if Loader track contains V-Max Headers (found on Mindscape titles) -----------
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
                    Array.Copy(temp, 0, tmp, 0, tmp.Length);
                    BitArray s_bArray = new BitArray(Flip_Endian(tmp));
                    BitArray d_bArray = new BitArray(s_bArray.Count);
                    int dp = 0;
                    for (int h = j; h < s_bArray.Length; h++)
                    {
                        d_bArray[dp] = s_bArray[h];
                        dp++;
                        if (dp == d_bArray.Length) dp = 0;
                    }
                    byte[] cc = new byte[d_bArray.Length / 8];
                    d_bArray.CopyTo(cc, 0);
                    cc = Flip_Endian(cc);
                    int sec = 0;
                    for (int i = 0; i < cc.Length - 5; i++)
                    {

                        if (cc[i] == sb[0]) Array.Copy(cc, i + 1, comp, 0, comp.Length);
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
        //----------------------------------------------------------------------------------------------------------
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

        /// ------------------------ Add Sync to Loader Track --------------------------------------------------

        byte[] Fix_Loader(byte[] data)
        {
            byte[] tdata = data;
            byte[] v2 = new byte[] { 0x5b, 0x57, 0x52, 0x4d }; // Cinemaware and some other v2 variants
            byte[] v3 = new byte[] { 0xaa, 0xaf, 0xda, 0x5f }; // V3 Taito (arkanoid)
            byte[] v1 = new byte[] { 0xaa, 0xbf, 0xb4, 0xbf }; // v3 Taito (bubble bobble)
            byte[] v4 = new byte[] { 0x6b, 0xd9, 0xb6, 0xdd }; // Sega
            byte[] comp = new byte[4];
            bool f = false;
            for (int i = 0; i < tdata.Length - 4; i++)
            {
                Array.Copy(tdata, i, comp, 0, comp.Length);
                if (Hex_Val(comp) == Hex_Val(v1)) { Patch_V3(i - 4); f = true; }
                if (Hex_Val(comp) == Hex_Val(v2)) { Patch_V2(i - 3); f = true; }
                if (Hex_Val(comp) == Hex_Val(v3)) { Patch_V3(i - 4); f = true; }
                if (Hex_Val(comp) == Hex_Val(v4)) { Patch_V2(i - 3); f = true; }
                if (f) break;
            }
            if (f) f_load.Text = "Fix Loader Sync ( Fixed )";
            return tdata;

            void Patch_V2(int pos)
            {
                if (pos > 0)
                {
                    tdata[pos] = 0xde;
                    tdata[pos + 1] = 0xff;
                    tdata[pos + 2] = 0xff;
                }
            }

            void Patch_V3(int pos)
            {
                if (pos > 0)
                {
                    tdata[pos] = 0x5f;
                    tdata[pos + 1] = 0xff;
                    tdata[pos + 2] = 0xff;
                }
            }
        }
    }
}