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
        
        readonly byte[] trk_start_reg = new byte[] { 0x35, 0x4d, 0x53, 0x54, 0xd5 };
        readonly byte[] trk_start_alt = new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55 };
        readonly byte[] vpl_s0 = new byte[] { 0x33, 0x3F, 0xD5 };
        readonly byte[] vpl_s1 = new byte[] { 0x35, 0x4d, 0x53 };
        readonly byte[] trk_end_0 = new byte[] { 0xb5, 0xb5, 0xbd };
        readonly BitArray leadIn_std = new BitArray(10);
        readonly BitArray leadIn_alt = new BitArray(10);
        readonly int com = 16;

        (byte[], int, int, int, int, int, string[] headers) Get_Vorpal_Track_Length(byte[] data, int trk)
        {
            int sub = 1; // <- # of bits to subtract from 'data_end' position marker
            int compare_len = 16; // <- sets the number of bytes to compare with for finding the end of the track
            int min_skip_len = 6000; // <- sets the # of bytes to skip when searching for the repeat of data
            int max_track_size = 7900;
            int data_start = 0;
            int data_end = 0;
            int track_len = 0;
            bool single_rotation = false;
            bool start_found = false;
            bool end_found = false;
            bool lead_in_Found = false;
            int track_lead_in = 0;
            int sectors = 0;
            int snc_cnt = 0;
            byte[] tdata = new byte[0];
            List<string> hdr = new List<string>();
            List<string> sec_header = new List<string>();
            BitArray source = new BitArray(Flip_Endian(data));
            BitArray lead_in = new BitArray(leadIn_std.Length);
            for (int k = 0; k < source.Length; k++)
            {
                if (source[k]) snc_cnt++;
                if (!source[k])
                {
                    if (snc_cnt == 8)
                    {
                        if (k - ((com * 8) + 8) > 0)
                        {
                            // checking for [00110011 00 11111111] sector 0 marker
                            if (!lead_in_Found && (source[k - 10] == false && source[k - 9] == false)) (lead_in_Found, track_lead_in) = Get_LeadIn_Position(k);
                            byte[] sec_ID = BitArray_to_ByteArray(source, true, k - ((com * 8)/ 2), com * 8);
                            if (!sec_header.Any(x => x == Hex_Val(sec_ID)))
                            {
                                sec_header.Add(Hex_Val(sec_ID));
                                if (!start_found)
                                {
                                    data_start = k;
                                    start_found = true;
                                }
                            }
                            else
                            {
                                if (!end_found)
                                {
                                    data_end = k - sub;
                                    end_found = true;
                                }
                            }
                            sectors = sec_header.Count;
                            if (!single_rotation && end_found) break;
                        }
                        k += 1200;
                    }
                    snc_cnt = 0;
                }
            }
            track_len = (data_end - data_start) + 1;
            if (single_rotation) tdata = BitArray_to_ByteArray(source, true, data_start, track_len);
            else
            {
                BitArray temp = new BitArray(track_len);
                int pos = track_lead_in;
                for (int i = 0; i < track_len; i++)
                {
                    temp[i] = source[pos];
                    pos++;
                    if (pos > data_end) pos = data_start;
                }
                tdata = BitArray_to_ByteArray(temp, true);
            }
            return (tdata, data_start, data_end, track_len, track_lead_in, sectors, sec_header.ToArray());



            (bool, int) Get_LeadIn_Position(int position)
            {
                bool leadF = false;
                int leadin = 0;
                int l = position - 18;
                byte[] pre_sync = BitArray_to_ByteArray(source, true, position - 18, 8);
                if (pre_sync[0] == 0x33)
                {
                    bool equal = false;
                    while (!equal)
                    {
                        l -= leadIn_std.Length;
                        if (l < 0) { l = 0; break; }
                        bool a = false; bool b = false;
                        for (int i = 0; i < lead_in.Length; i++) lead_in[i] = source[l + i];
                        a = ((BitArray)leadIn_std.Clone()).Xor(lead_in).OfType<bool>().All(e => !e);
                        b = ((BitArray)leadIn_alt.Clone()).Xor(lead_in).OfType<bool>().All(e => !e);
                        equal = !a && !b;
                    }
                    leadin = l + leadIn_std.Count - 1;
                }
                
                if (leadin + (max_track_size * 8) < source.Length)
                {
                    data_start = leadin;
                    start_found = true;
                    int q = min_skip_len * 8;
                    byte[] lf_r = BitArray_to_ByteArray(source, true, leadin, compare_len * 8);
                    while (q < source.Length - (compare_len * 8))
                    {
                        byte[] rcomp = BitArray_to_ByteArray(source, true, q, compare_len * 8);
                        if (Hex_Val(rcomp) == Hex_Val(lf_r))
                        {
                            end_found = true;
                            data_end = q - sub;
                            single_rotation = true; // <- entire track contained within the start and end point of the source array
                            break;
                        }
                        q++;
                    }
                }
                leadF = true;
                return (leadF, leadin);
            }
        }
    }
}