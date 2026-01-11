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
        private static readonly byte[] prt_slay1 = { 0xeb, 0xd7, 0xaa, 0x55, 0xaa }; // <- Piraye Slayer v1 check
        private static readonly byte[] prt_slay2 = { 0xd7, 0xd7, 0xeb, 0xcc, 0xad }; // <- Pirate Slayer v2 check
        private static readonly byte[] slayer_key1 = new byte[] { 0x55, 0xae, 0x9b, 0x55, 0xad, 0x55, 0xcb, 0xae, 0x6b, 0xab, 0xad, 0xaf };
        private static readonly byte[] slayer_key2 = new byte[] { 0x55, 0xae, 0x9b, 0x55, 0xad, 0x55, 0x2b, 0xae, 0x2b, 0xab, 0xad, 0xaf };
        private static readonly byte[] rainbowArts_magicBytes = new byte[] { 0xbe, 0x55, 0x5b, 0xe5, 0x55 }; // <- RainbowArts / MagicBytes key signature found on t36
        private static readonly byte[] gma = new byte[] { 0x69, 0x50, 0x50, 0xa0, 0xa0 };
        private static readonly byte[] securispeed = new byte[] { 0xff, 0x56, 0x56, 0xa3, 0xa3 };
        //private static readonly byte[] blank = new byte[] { 0x00, 0x11, 0x22, 0x44, 0x45, 0x14, 0x12, 0x51, 0x88, 0x18, 0x31, 0x23 }; // weak GCR

        //(bool has_prot, byte[] patched) Find_VMax_Cart_CBM(byte[] data, int track, int sec)
        //{
        //    if (data == null && data.Length != 256) return (false, null);
        //    byte[] offsets = new byte[] { 0xa2, 0x5a, 0x55, 0xe1, 0xb3, 0x80, 0xa3, 0x71 };
        //    byte[] checksums = new byte[] { 0x37, 0x85, 0x34, 0x5e, 0x2a, 0xab, 0xab, 0xa5 };
        //    byte[] newsums = new byte[] { 0xf7, 0x6c, 0x54, 0xbc, 0x9a, 0xd4, 0xbb, 0xcf };
        //
        //    byte[][] original = new byte[][]
        //    {
        //        new byte[] { 0xec, 0x8e, 0xb7 },
        //        new byte[] { 0xd9, 0xb2, 0xfe },
        //        new byte[] { 0xb5, 0x54, 0x08 },
        //        new byte[] { 0xdc, 0xa0, 0xbe },
        //        new byte[] { 0xe0, 0x5a, 0x61 },
        //        new byte[] { 0xd1, 0x47, 0x22 },
        //        new byte[] { 0xb5, 0x54, 0x2b },
        //        new byte[] { 0x42, 0x10, 0xad }
        //    };
        //
        //    byte[][] replace = new byte[][]
        //    {
        //        new byte[] { 0xf0, 0x5f },
        //        new byte[] { 0xd0, 0x75 },
        //        new byte[] { 0xa5 },
        //        new byte[] { 0xe5, 0x44 },
        //        new byte[] { 0xf0 },
        //        new byte[] { 0xa4, 0x44 },
        //        new byte[] { 0xa5 },
        //        new byte[] { 0x48 }
        //    };
        //
        //    for (int i = 0; i < offsets.Length; i++)
        //    {
        //        (bool success, byte[] patched) = patch(offsets[i], original[i], replace[i], checksums[i], newsums[i]);
        //        if (success && patched != null)
        //        {
        //            //File.WriteAllBytes($@"c:\test\t{track}_s{sec}_{i}", data);
        //            return (true, patched);
        //        }
        //    }
        //    return (false, data);
        //
        //    (bool, byte[]) patch(byte ofst, byte[] srch, byte[] repl, byte cksm, byte newsum)
        //    {
        //        try
        //        {
        //            if (MatchSeq(data, srch, ofst) && data[255] == cksm)
        //            {
        //                byte[] temp = CopyArray(data);
        //                Buffer.BlockCopy(repl, 0, temp, ofst, repl.Length);
        //                temp[255] = newsum;
        //                return (true, temp);
        //            }
        //        }
        //        catch { }
        //        return (false, null);
        //    }
        //
        //}

        (bool has_prot, byte[] patched) Find_VMax_Cart_CBM(byte[] data, int track, int sec)
        {
            if (data == null && data.Length != 256) return (false, null);
            byte[] offset = new byte[0];
            byte[] offset2 = new byte[0];
            byte[][] search = new byte[0][];
            byte[][] search2 = new byte[0][];
            byte[][] replace = new byte[0][];
            byte[][] replace2 = new byte[0][];
            //int matches = 0;

            if (track == 5)
            {
                if (sec == 0)
                {
                    // Into the Eagles Nest
                    offset = new byte[] { 0xa2, 0xff };
                    replace = new byte[2][];
                    search = new byte[2][];
                    search[0] = new byte[] { 0xec, 0x8e, 0xb7 };
                    replace[0] = new byte[] { 0xf0, 0x5f };
                    search[1] = new byte[] { 0x37 };
                    replace[1] = new byte[] { 0xf7 };

                    // Ms. Pac Man
                    offset2 = new byte[] { 0x5a, 0xff };
                    replace2 = new byte[2][];
                    search2 = new byte[2][];
                    search2[0] = new byte[] { 0xd9, 0xb2, 0xfe };
                    replace2[0] = new byte[] { 0xd0, 0x75 };
                    search2[1] = new byte[] { 0x85 };
                    replace2[1] = new byte[] { 0x6c };
                }

                if (sec == 6)
                {
                    // Dig Dug - Pole Position
                    offset = new byte[] { 0x55, 0xff };
                    replace = new byte[2][];
                    search = new byte[2][];
                    search[0] = new byte[] { 0xb5, 0x54, 0x08 };
                    replace[0] = new byte[] { 0xa5 };
                    search[1] = new byte[] { 0x34 };
                    replace[1] = new byte[] { 0x54 };

                    // Xevious
                    offset2 = new byte[] { 0xe1, 0xff };
                    replace2 = new byte[2][];
                    search2 = new byte[2][];
                    search2[0] = new byte[] { 0xdc, 0xa0, 0xbe };
                    replace2[0] = new byte[] { 0xe5, 0x44 };
                    search2[1] = new byte[] { 0x5e };
                    replace2[1] = new byte[] { 0xbc };
                }

                if (sec == 8)   // Paperboy
                {
                    offset = new byte[] { 0xb3, 0xff };
                    replace = new byte[2][];
                    search = new byte[2][];
                    search[0] = new byte[] { 0xe0, 0x5a, 0x61 };
                    replace[0] = new byte[] { 0xf0 };
                    search[1] = new byte[] { 0x2a };
                    replace[1] = new byte[] { 0x9a };
                }
            }

            if (track == 10 && sec == 0) // Deja Vu
            {
                offset = new byte[] { 0x80, 0xff };
                replace = new byte[2][];
                search = new byte[2][];
                search[0] = new byte[] { 0xd1, 0x47, 0x22 };
                replace[0] = new byte[] { 0xa4, 0x44 };
                search[1] = new byte[] { 0xab };
                replace[1] = new byte[] { 0xd4 };
            }

            if (track == 19 && sec == 0) // Bop n Rumble
            {
                offset = new byte[] { 0xa3, 0xff };
                replace = new byte[2][];
                search = new byte[2][];
                search[0] = new byte[] { 0xb5, 0x54, 0x2b };
                replace[0] = new byte[] { 0xa5 };
                search[1] = new byte[] { 0xab };
                replace[1] = new byte[] { 0xbb };
            }

            if (track == 39 && sec == 13) // Gauntlet
            {
                offset = new byte[] { 0x71, 0xff };
                search = new byte[2][];
                replace = new byte[2][];
                search[0] = new byte[] { 0x42, 0x10, 0xad };
                replace[0] = new byte[] { 0x48 };
                search[1] = new byte[] { 0xc5 };
                replace[1] = new byte[] { 0xcf };
            }

            if (offset.Length > 0)
            {
                (bool success, byte[] patched) = patch(offset, search, replace);
                if (!success && offset2.Length > 0) (success, patched) = patch(offset2, search2, replace2);
                if (success) return (true, patched);
            }
            // No matches found, returning (failure, original sector)
            return (false, data);

            (bool, byte[]) patch(byte[] ofst, byte[][] srch, byte[][] repl)
            {
                int matches = 0;
                try
                {
                    byte[] temp = CopyArray(data);
                    for (int i = 0; i < ofst.Length; i++)
                    {
                        if (MatchSeq(temp, srch[i], ofst[i]))
                        {
                            Buffer.BlockCopy(repl[i], 0, temp, ofst[i], repl[i].Length);
                            matches++;
                        }
                    }
                    if (matches == ofst.Length) return (true, temp);
                }
                catch { }
                return (false, null);
            }
        }

        (bool, byte[]) Find_Cart_Protection_v2(byte[] data, bool t19s14, bool use_newer_GCR = false)
        {
            if (data == null) return (false, null);
            bool older = !use_newer_GCR;
            if (!use_newer_GCR)
            {
                // if 'use_newer_GCR is false, check sector for existence of weak-bits to determine which encoding method to use
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0xe2 || data[i] == 0xa3) { older = true; break; }
                }
            }
            byte[] temp = Decode_VmaxGCR(data);
            if (t19s14) // Do Compressed Search & Replace (always resides on Track 19, Sector 14)
            {
                byte[] offset = new byte[] { 0x88, 0xd7, 0x76 };   // sector offsets for HCS, BSB, G / GDD
                byte[][] search = new byte[3][];
                byte[][] replace = new byte[3][];
                /// Patch bytes (Search for / Repplace with)
                search[0] = new byte[] { 0x9c, 0x38 };  // Harrier Combat Simulator
                replace[0] = new byte[] { 0x15, 0x08 };
                search[1] = new byte[] { 0x63, 0xd0 };  // Bad Street Brawler
                replace[1] = new byte[] { 0x66, 0x00 };
                search[2] = new byte[] { 0xd2, 0xb4 };  // Gauntlet / Gauntlet Deeper Dungeons
                replace[2] = new byte[] { 0xd8, 0x10 };
                for (int i = 0; i < search.Length; i++)
                {
                    if (MatchSeq(temp, search[i], offset[i]))
                    {
                        Buffer.BlockCopy(replace[i], 0, temp, offset[i], 2);
                        // match found, return true, and encoded sector with checksum
                        return (true, Encode_VmaxGCR(temp, true, older)); // 'older' specifies which encoding method, true = weak false = non-weak
                    }
                }
            }
            // Uncompressed Search : only runs if Compressed Search failed.
            for (int i = 0; i < temp.Length; i++)
            {
                if (MatchSeq(temp, cart_patch_v2, i) && i > 2)
                {
                    int pos = i + cart_patch_v2.Length;
                    Buffer.BlockCopy(temp, pos + 1, temp, pos - 2, 2);
                    return (true, Encode_VmaxGCR(temp, true, older));
                }
            }
            // no match found (return false, original encoded sector)
            return (false, data);
        }

        (bool, byte[]) Find_Cart_Protection_v3(byte[] data)
        {
            if (data == null) return (false, null);
            byte[] temp = Decode_VmaxGCR_Linear(data);
            for (int i = 0; i < temp.Length - cart_patch_v3.Length; i++)
            {
                if (MatchSeq(temp, cart_patch_v3, i) && i > 2)
                {
                    Buffer.BlockCopy(temp, i - 3, temp, i, 2);
                    int chunks = temp.Length / 3;
                    byte[] output = new byte[temp.Length];
                    for (int j = 0; j < chunks; j++)
                    {
                        int src = j * 3;
                        output[j] = temp[src + 2];
                        output[j + chunks] = temp[src + 1];
                        output[j + (chunks * 2)] = temp[src];
                    }
                    return (true, Encode_VmaxGCR(output, true));
                }
            }
            return (false, data);
        }

        byte[] Pirate_Slayer(byte[] data, byte[] key, int version)
        {
            if (key == null || version == 0) return data;
            int track_density = density[3];
            MemoryStream buffer = new MemoryStream();
            BinaryWriter write = new BinaryWriter(buffer);
            if (version == 1) Slayer1();
            if (version == 2) Slayer2();
            return buffer.Length == track_density ? buffer.ToArray() : data.Length >= track_density ? data.Take(track_density).ToArray() : data;

            void Slayer1()
            {
                // I'm not entirely sure the correct byte sequence, but this works. Fast Hackem's method didn't work well in Vice
                var head = FastArray.Init(184, 0xeb);
                var aa55 = PaddingFill(2049, 0x55, 0xaa); aa55[0] = 0xd7;
                var fill = FastArray.Init(track_density - new[] { head, aa55, key }.Sum(arr => arr.Length), 0xeb);
                write.Write(ArrayConcat(head, aa55, key, fill));
            }

            void Slayer2()
            {
                // Out of my code Hacker!  Hi Kris, Interesting, yet simple and effective protection. It got me as a kid,
                // and now I'm here helping preserving your work.  E-mail me wadzinsky@hotmail.com :)
                var d7 = FastArray.Init(1001, 0xd7);
                var eb = new byte[] { 0xeb };
                var ccad = PaddingFill(1024, 0xcc, 0xad);
                for (int i = 0; i < 3; i++) write.Write(ArrayConcat(d7, eb, ccad, key, new byte[] { 0x57 }));
                write.Write(FastArray.Init(track_density - (int)buffer.Length, 0xeb));
            }

            byte[] PaddingFill(int length, byte even, byte odd)
            {
                var arr = new byte[length];
                for (int i = 0; i < length; i++) arr[i] = (i % 2 == 0) ? even : odd;
                return arr;
            }
        }

        (bool, byte[], int) Radwar(byte[] data, bool fix = false, int sector = -1)
        {
            int rw = 0;
            bool rad = false;
            BitArray source = new BitArray(Flip_Endian(data));
            byte[] temp = new byte[0];
            bool f = false;
            int pos;
            if (!fix && sector == -1)
            {
                for (sector = 2; sector < 19; sector++)
                {
                    (f, pos, _, _, _) = Find_Sector(source, sector);
                    if (f)
                    {
                        rw = 0;
                        (temp, f) = Decode_CBM_Sector(data, sector, false, source);
                        for (int j = 0; j < temp.Length - 1; j++)
                        {
                            //if (blank.Any(x => x == temp[j]))
                            if (weakTable[temp[j]])
                            {
                                rw++;
                                if (rw > 4) { rad = true; break; }
                            }
                        }
                    }
                    if (rad) break;
                }
            }
            else if (sector >= 0) Fix(sector);
            return (rad, data, sector);

            void Fix(int sec)
            {
                data = Replace_CBM_Sector(data, sec, Remove_Weak_Bits(Decode_CBM_Sector(data, sec, false, source).data));
            }
        }

        byte[] JvB(byte[] data)
        {
            byte[] temp = FastArray.Init(density[3], 0x00);
            int run = 0;
            int longest = 0;
            int pos = 0;
            for (int i = 0; i < data.Length; i++)
            {
                //if (!blank.Any((x) => x == data[i])) run++;
                if (!weakTable[data[i]]) run++;
                else
                {
                    if (run > longest)
                    {
                        longest = run;
                        pos = i - run;
                    }
                    run = 0;
                }
            }
            Buffer.BlockCopy(data, pos, temp, 0, longest);
            return temp;
        }

        (bool, int) Check_Cyan_Loader(bool patch = false)
        {
            bool cyan = false;
            int c_cyn = 8;
            int c_gcr = 62;
            int c_v1 = 78;
            int w_trk = 76;
            bool cpt = false;
            try
            {
                if (tracks < 43) { c_cyn = 4; c_gcr = 31; c_v1 = 39; w_trk = 38; }
                if (NDS.cbm[c_cyn] == 1) cyan = Find_Cyan_Sector(NDS.Track_Data[c_cyn]);
                if (cyan && !patch)
                {
                    NDS.Prot_Method = "Protection: Cyan Loader";
                    if (NDS.cbm[c_v1] != 1) (NDS.Track_Data[c_gcr], cpt) = Cyan_t32_GCR_Fix(NDS.Track_Data[c_gcr]);
                    if (NDS.cbm[w_trk] == 1 && NDS.Track_ID[w_trk] == 40)
                    {
                        NDS.cbm[c_v1] = 1;
                        NDS.Track_Data[c_v1] = NDS.Track_Data[w_trk];
                        NDS.Track_ID[c_v1] = NDS.Track_ID[w_trk];
                        NDS.Track_Length[c_v1] = NDS.Track_Length[w_trk];
                        NDS.Track_Data[w_trk] = FastArray.Init(8192, 0x00);
                        NDS.Track_ID[w_trk] = 0;
                        NDS.cbm[w_trk] = 0;
                        NDS.Track_Length[w_trk] = 0;
                    }
                    else if ((((NDS.cbm[c_v1] == 1 && NDS.Track_ID[c_v1] != 40) || NDS.cbm[c_v1] != 1) && !cpt) || (NDS.cbm[c_v1] == 1 && NDS.sectors[c_v1] < 16))
                    {
                        if (!batch)
                        {
                            using (Message_Center center = new Message_Center(this))
                            {
                                string t = "Cyan Loader detected!";
                                string s = "Cyan Loader detected!\n\nProtected track is missing or corrupt\n\nWould you like to patch out the protection check?";
                                DialogResult result = MessageBox.Show(s, t, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                                if (result == DialogResult.Yes) Remove_Protection();
                            }
                        }
                        if (batch) Remove_Protection();
                    }
                }
                if (cyan && patch) Remove_Protection();
                if (NDS.cbm[c_v1] == 1) c_gcr = -1;
            }
            catch { }
            return (cyan, c_gcr);

            void Remove_Protection()
            {
                NDS.Track_Data[c_cyn] = Cyan_Loader_Patch(NDS.Track_Data[c_cyn]);
                if (NDS.cbm[c_v1] == 1)
                {
                    NDS.Track_Length[c_v1] = 0;
                    NDS.cbm[c_v1] = 0;
                }
            }

            bool Find_Cyan_Sector(byte[] data)
            {
                byte[] cmp;
                bool pass;
                (cmp, pass) = Decode_CBM_Sector(data, 5, true);
                if (pass || !pass)
                {
                    int match = 0;
                    for (int i = 0; i < cmp.Length; i++) if (cmp[i] == cldr_id[i]) match++;
                    if (match > 240) return true;
                }
                return false;
            }
        }

        byte[] Cyan_Loader_Patch(byte[] data) /// <- removes track 32 bad GCR check (cracks the proteciton)
        {
            byte[] cmp;
            (cmp, _) = Decode_CBM_Sector(data, 5, true);
            int match = 0;
            for (int i = 0; i < cmp.Length; i++) if (cmp[i] == cldr_id[i]) match++;
            if (match > 240)
            {
                if (cmp[45] == 0x2f) cmp[45] = 0xa2;
                if (cmp[53] == 0x18) cmp[53] = 0x0f;
                data = Replace_CBM_Sector(data, 5, cmp);
            }
            return data;
        }

        (byte[], bool) Cyan_t32_GCR_Fix(byte[] data)
        {
            BitArray s = new BitArray(Flip_Endian(data));
            (bool exists, _, _, _, _) = Find_Sector(s, 1, 0, true);
            if (exists)
            {
                (byte[] new_sec, _) = Decode_CBM_Sector(data, 1, false, s);
                byte[] padding = FastArray.Init(4, 0x00);
                data = Replace_CBM_Sector(data, 1, new_sec, padding);
            }
            return (data, exists);
        }

        byte[] Securispeed(byte[] data, int shifted)
        {
            if (data == null) return null;
            if (shifted > 0) data = Bit2Byte(BitRotateLeft(new BitArray(Flip_Endian(data)), shifted));
            byte[] key = new byte[density[3]];
            byte[] sync = FastArray.Init(5, 0xff);
            int start = 0;
            int pos = 0;
            bool weak = false;
            bool end = false;
            for (int i = 0; i < data.Length - gma.Length; i++)
            {
                if (data[i] == gma[0] || (data[i] == securispeed[0] && data[i + 1] == securispeed[1]))
                {
                    if (MatchSeq(data, securispeed, i))
                    {
                        pos = i;
                        if (pos == 0) start = 5;
                        break;
                    }
                    bool m = true;

                    if (m)
                    {
                        for (int k = i; k < i + 60; k++)
                        {
                            if (k < data.Length && data[k] == 0xff)
                            {
                                weak = true;
                                pos = i;
                                end = true;
                                break;
                            }
                        }
                        if (end) break;
                    }
                }
            }

            if (pos >= 5) data = Rotate_Left(data, pos - 5);
            if (weak) data = Remove_Weak_Bits(data, true);
            if (start == 5) Buffer.BlockCopy(sync, 0, key, 0, sync.Length);
            Buffer.BlockCopy(data, 0, key, start, key.Length - start);
            if (key[0] == gma[0])
            {
                key = Rotate_Right(key, 6);
                Buffer.BlockCopy(sync, 0, key, 0, sync.Length);
            }
            return key;
        }

        byte[] RainbowArts(byte[] data, int shifted)
        {
            if (data == null) return null;
            if (shifted > 0) data = Bit2Byte(BitRotateLeft(new BitArray(Flip_Endian(data)), shifted));
            int pos = 0;
            int start;
            int end = 0;
            int sync;
            byte[] temp = new byte[0];
            int v = 0;
            for (int i = 0; i < data.Length - rainbowArts_magicBytes.Length; i++)
            {
                if (MatchSeq(data, rainbowArts_magicBytes, i))
                {
                    pos = i; v = 1;
                    break;
                }
                if (data[i] == 0xff && (i > 0 && data[i - 1] != 0xff)) { v = 255; break; }
            }
            if (pos < data.Length)
            {
                if (v == 1) Key_Exists();
                if (v == 255) Key_Missing();
            }
            return temp;

            void Key_Exists()
            {
                temp = FastArray.Init(density[1], 0x55);
                start = pos;
                while (pos < data.Length)
                {
                    if (data[pos] == 0xff) { end = pos; break; }
                    pos++;
                }
                if (end - start > 0)
                {
                    Buffer.BlockCopy(data, start, temp, 0, end - start);
                    sync = Get_Sync_Length();
                    for (int i = 0; i < sync; i++) temp[(end - start) + i] = 0xff;
                    temp[end - start + sync] = 0x52;
                }
            }

            void Key_Missing()
            {
                temp = FastArray.Init(density[1], 0x55);
                sync = Get_Sync_Length();
                Buffer.BlockCopy(rak1, 0, temp, 0, rak1.Length);
                for (int i = 0; i < sync; i++) temp[rak1.Length + i] = 0xff;
                temp[rak1.Length + sync] = 0x52;
            }

            int Get_Sync_Length()
            {
                int snc = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0xff) snc++;
                    if (snc > 108 && data[i] != 0xff) break;
                }
                if (snc > 108 && snc < 123) return 116;
                return 128;
            }
        }

        byte[] Custom_Format(byte[] data, int trk = -1)
        {
            if (data == null || data.Length < 6000) return data;
            //int track = tracks > 42 ? (trk / 2) + 1 : trk + 1;
            byte[] skip = new byte[] { 0x55, 0xaa };
            //HashSet<byte> BlankSet = new HashSet<byte>(blank);
            HashSet<byte> Padding = new HashSet<byte>(skip);
            int nb = 0;
            int pad = 0;
            int dataLength = data.Length;
            int sncpos = -1;

            for (int i = 0; i < dataLength; i++)
            {
                //if (!BlankSet.Contains(data[i])) nb++;
                if (!weakTable[data[i]]) nb++;
                if (data[i] == 0x55 || data[i] == 0xaa) pad++;
                if (data[i] == 0xff && sncpos != -1) sncpos = i;
            }
            if (pad > density[0] - 100) return new byte[0];
            int sp = 0;
            if (nb > 1000) // 6200
            {
                while (sp < 256)
                {
                    if (sp++ > 0) data = Rotate_Left(data, 1);
                    const int clen = 192;
                    int start_pos = sncpos >= 0 && sncpos < 256 ? sncpos : 2;
                    byte[] compare = CopyArray(data, start_pos, clen);
                    for (int i = clen + 1000; i < dataLength - clen; i++)
                    {
                        if (MatchSeq(data, compare, i))
                        {
                            int newLength = i - start_pos; // - 10;
                            if (newLength > 6000)
                            {
                                byte[] temp = CopyArray(data, start_pos, newLength);
                                if (pad < 1000)
                                {
                                    int gap = FindLongestRun_Specific(temp, 0x55);
                                    if (gap > 0) temp = Rotate_Left(temp, gap);
                                }
                                int d = density[Get_Density(temp.Length)];
                                if (temp.Length > d)
                                {
                                    int trim = temp.Length - d;
                                    (int pos, int len) = FindLongestRun_General(temp);
                                    if (len > 0 && pos >= 0 && pos + len < temp.Length)
                                    {
                                        if (trim > len) trim = len - 2;
                                        temp = ArrayConcat(CopyArray(temp, 0, pos), CopyArray(temp, pos + trim));
                                    }
                                }
                                int snc = FindLongestRun_Specific(temp, 0xff);
                                if (snc > 0) temp = Rotate_Left(temp, snc);
                                if (temp.Length > d) temp = CopyArray(temp, 0, d);
                                return temp;
                            }
                            else
                            {
                                int p = density[2] >> 1;
                                return ArrayConcat(FastArray.Init(p - 1, 0xac), new byte[] { 0xa0 }, FastArray.Init(density[2] - p, 0xca));
                            }
                        }
                    }
                }
            }

            if (nb > 500)
            {
                int snc = 0;
                int spos = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0xff) snc++;
                    else
                    {
                        if (snc > 4)
                        {
                            spos = i - snc;
                            break;
                        }
                        snc = 0;
                    }
                }
                if (spos > 0) data = Rotate_Left(data, spos);

                int actual_data = Check_Valid_Data(data, true);
                byte[] temp = new byte[Check_Valid_Data(data, false, true) < 1000 ? density[2] : density[3]];
                Buffer.BlockCopy(data, 0, temp, 0, temp.Length);
                if (actual_data > 500 && snc < 1000) temp = Remove_Weak_Bits(temp, true);
                //temp = Add_Weak_Bit(temp);
                return temp;
            }
            return data;

            int Check_Valid_Data(byte[] array, bool include_Padding = false, bool only_blank = false)
            {
                int ad = 0;
                int bd = 0;
                for (int j = 0; j < array.Length; j++)
                {
                    //if (BlankSet.Contains(array[j])) bd++;
                    if (weakTable[array[j]]) bd++;
                    else ad++;
                    if (include_Padding)
                    {
                        if (Padding.Contains(array[j])) bd++;
                        else ad++;
                    }
                }
                if (only_blank) return bd;
                return ad;
            }

            //byte[] Add_Weak_Bit(byte[] input)
            //{
            //    int run = 0;
            //    byte g = 0x00;
            //    for (int j = 0; j < input.Length; j++)
            //    {
            //        if (input[j] == g) run++;
            //        else
            //        {
            //            g = input[j];
            //            if (run > 300)
            //            {
            //                input[j] = 0x00;
            //                if (j + 1 < input.Length) input[j + 1] = 0x00;
            //                byte[] output = new byte[density[3]];
            //                int start = (input.Length - density[3]) / 2;
            //                Buffer.BlockCopy(input, start, output, 0, density[3]);
            //                return output;
            //            }
            //            run = 0;
            //        }
            //    }
            //    return input;
            //}
        }
    }
}