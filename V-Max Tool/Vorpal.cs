using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Drawing;
using static System.Windows.Forms.AxHost;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        // Vorpal stuff
        //private readonly byte[] t_start = new byte[] { 0x35, 0x4d, 0x53, 0x54, 0xd5 };
        //private readonly byte[] t_start = new byte[] { 0xa9, 0x33, 0x3f, 0xd5 };
        //private readonly byte[] t_start = new byte[] { 0xaa, 0x4c, 0xcf, 0xf5 };
        //private readonly int vpl_c = 24;

        //readonly byte[] vpl_s0 = new byte[] { 0xA9, 0x33, 0x3F, 0xD5 };
        
        
        readonly byte[] trk_start_reg = new byte[] { 0x35, 0x4d, 0x53, 0x54, 0xd5 };
        readonly byte[] trk_start_alt = new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55 };
        readonly byte[] vpl_s0 = new byte[] { 0x33, 0x3F, 0xD5 };
        readonly byte[] vpl_s1 = new byte[] { 0x35, 0x4d, 0x53 };
        readonly byte[] trk_end_0 = new byte[] { 0xb5, 0xb5, 0xbd };
        readonly BitArray lead_0 = new BitArray(10);
        readonly BitArray lead_1 = new BitArray(10);
        readonly int com = 4;

        

        (byte[], int, int, int, int, int, string[]) Get_Vorpal_Track_Length(byte[] data, int trk)
        {
            int data_start = 0;
            int data_end = 0;
            bool start_found = false;
            bool end_found = false;
            bool whole_track = false;
            bool lead_in_Found = false;
            int track_lead_in = 0;
            int sectors = 0;
            int snc_cnt = 0;
            List<string> hdr = new List<string>();
            List<string> sec_header = new List<string>();
            BitArray source = new BitArray(Flip_Endian(data));
            BitArray lead_in = new BitArray(lead_0.Length);
            for (int k = 0; k < source.Length; k++)
            {
                if (source[k]) snc_cnt++;
                if (!source[k])
                {
                    if (snc_cnt == 8)
                    {
                        var u = k;
                        if (k - ((com * 8) + 8) > 0)
                        {
                            if (!lead_in_Found && (source[u - 10] == false && source[u - 9] == false)) Get();
                            {
                                k -= ((com * 8) + 8);
                                byte[] sh = BitArray_to_ByteArray(source, true, k, com * 8);
                                if (sec_header.Any(ss => ss != Hex_Val(Flip_Endian(sh)))) hdr.Add($"sector {sectors} pos {k >> 3}");
                                if (!end_found && sec_header.Any(ss => ss == Hex_Val(Flip_Endian(sh)))) { end_found = true; data_end = u; }
                                if (!start_found) { data_start = u; sec_header.Add(Hex_Val(Flip_Endian(sh))); start_found = true; }
                                if (!end_found || (end_found && whole_track)) sectors++;
                            }
                            void Get()
                            {
                                byte[] pre_sync = BitArray_to_ByteArray(source, true, u - 18, 8);
                                if (pre_sync[0] == 0x33)
                                {
                                    int l = u - 18;
                                    bool equals = false;
                                    while (!equals)
                                    {
                                        l -= lead_0.Length;
                                        if (l < 0) { l = 0; break; }
                                        bool a = false; bool b = false;
                                        for (int i = 0; i < lead_in.Length; i++) lead_in[i] = source[l + i];
                                        a = ((BitArray)lead_0.Clone()).Xor(lead_in).OfType<bool>().All(e => !e);
                                        b = ((BitArray)lead_1.Clone()).Xor(lead_in).OfType<bool>().All(e => !e);
                                        equals = !a && !b;
                                    }
                                    track_lead_in = l + lead_0.Count - 1;
                                }
                                lead_in_Found = true;

                                if (track_lead_in + (7900 * 8) < source.Length)
                                {
                                    data_start = track_lead_in;
                                    start_found = true;
                                    int q = 6000 * 8;
                                    if (tracks > 42) q = (density[density_map[(trk / 2)]] * 8) - 550; else q = (density[density_map[trk]] * 8) - 550;
                                    byte[] lf_r = BitArray_to_ByteArray(source, true, track_lead_in, 16 * 8);
                                    while (q < source.Length - (16 * 8))
                                    {
                                        byte[] rcomp = BitArray_to_ByteArray(source, true, q, 16 * 8);
                                        if (Hex_Val(rcomp) == Hex_Val(lf_r))
                                        {
                                            end_found = true;
                                            data_end = q - 8;
                                            break;
                                        }
                                        q++;
                                    }
                                    whole_track = true;
                                }
                            }
                        }
                        k = u + 1200;
                    }
                    snc_cnt = 0;
                }
            }
            byte[] dtemp = new byte[0];
            int top = 0;
            if ((start_found && end_found) && data_start != track_lead_in)
            {
                int fs = track_lead_in - 16;
                while (fs < track_lead_in + 10)
                {
                    byte[] li = BitArray_to_ByteArray(source, true, fs, trk_start_reg.Length * 8);
                    if (Hex_Val(li) == Hex_Val(trk_start_reg) || Hex_Val(li) == Hex_Val(trk_start_alt)) break;
                    fs++;
                }
                int start = fs; //track_lead_in;
                int size = data_end - data_start;
                top = source.Length - size; //  (size + (16 * 8));
                BitArray temp = new BitArray(source.Length - top);
                for (int i = 0; i < source.Length - top; i++)
                {
                    temp[i] = source[start];
                    start++;
                    if (start == data_end) start = data_start;
                }
                dtemp = BitArray_to_ByteArray(temp, true, 0, temp.Length);
            }
            else
            {
                BitArray temp = new BitArray(data_end - data_start);
                for (int i = 0; i < data_end - data_start; i++) temp[i] = source[data_start + i];
                dtemp = BitArray_to_ByteArray(temp, true, 0, temp.Length);
            }
            return (dtemp, data_start, data_end, (data_end - data_start), track_lead_in, sectors, hdr.ToArray());
        }
    }
}