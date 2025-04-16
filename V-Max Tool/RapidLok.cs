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
        private readonly byte[] RLok = new byte[] { 0xff, 0xff, 0x55, 0x7b }; /// Pattern used to detect if a track is RapidLok protected
        private readonly byte[] RLok1 = new byte[] { 0x75, 0x90, 0x09 }; /// Pattern used to detect if a track is RapidLok protected
        private readonly byte[] RLok_7b = new byte[] { 0x55, 0x7b, 0x7b, 0x7b, 0x7b, 0x7b, 0x7b, 0x7b, 0x7b, 0x7b };
        private readonly byte[,][] rl6_t18s3 = new byte[5, 2][];
        private readonly byte[,][] rl6_t18s6 = new byte[1, 2][];
        private readonly byte[,][] rl2_t18s9 = new byte[2, 2][];
        private readonly byte[,][] rl1_t18s9 = new byte[2, 2][];
        private readonly byte[] rl_nkey = new byte[54];
        private readonly int[] rl_7b = new int[35];
        private bool Replace_RapidLok_Key = false;

        string RL_Remove_Protection()
        {
            byte[] f = new byte[0];
            if (NDS.cbm.Any(x => x == 7) && RL_Fix.Checked)
            {
                int track = 17;
                if (tracks > 43) track = 34;
                (byte[] temp, int rem) = Patch_RapidLok(NDG.Track_Data[track], 0);
                Set_Dest_Arrays(temp, track);
                switch (rem)
                {
                    case 0: return "Failed! Protection still intact";
                    case 1: return "Success! Removed all protecion checks!";
                    case 2: return "Protection already removed.";
                }
            }
            return null;
        }

        (byte[], int) Patch_RapidLok(byte[] data, int start)
        {
            byte[] sector = new byte[0];
            BitArray source = new BitArray(Flip_Endian(data));

            bool[] rl6 = new bool[5];
            bool[] rl2 = new bool[2];
            bool[] rl1 = new bool[2];

            (bool chk, bool already) = TryPatchSector(3, rl6, true, rl6_t18s3, Patch_v6);
            if (chk) return (data, 1);
            if (already) return (data, 2);

            (chk, already) = TryPatchSector(9, rl2, false, rl2_t18s9, Patch_v2);
            if (chk)
            {
                data = Replace_CBM_Sector(data, 9, sector);
                return (data, 1);
            }
            else if (already) return (data, 2);

            (chk, already) = TryPatchSector(9, rl1, false, rl1_t18s9, Patch_v1);
            if (chk)
            {
                data = Replace_CBM_Sector(data, 9, sector);
                return (data, 1);
            }
            else if (already) return (data, 2);

            LogSectorStatus(rl6, rl2, rl1);
            return (data, 0);

            void Patch_v2()
            {
                PatchSector(rl2_t18s9);
            }

            void Patch_v1()
            {
                PatchSector(rl1_t18s9);
            }

            void Patch_v6()
            {
                PatchSector(rl6_t18s3);
                data = Replace_CBM_Sector(data, 3, sector);

                (bool exist, int pos, _, _, bool hdr_cksm) = Find_Sector(source, 6);
                if (exist)
                {
                    (sector, _) = Decode_CBM_Sector(data, 6, true, source, pos);
                    PatchSector(rl6_t18s6);
                    data = Replace_CBM_Sector(data, 6, sector, null, start);
                }
            }

            (bool, bool) TryPatchSector(int sectorIndex, bool[] flags, bool decode, byte[,][] patterns, Action patchAction)
            {
                bool[] patched = new bool[flags.Length];
                (bool exist, int pos, _, _, _) = Find_Sector(source, sectorIndex, start);
                if (!exist) return (false, false);

                (sector, _) = Decode_CBM_Sector(data, sectorIndex, decode, source, pos);
                for (int i = 0; i < sector.Length; i++)
                {
                    for (int j = 0; j < patterns.GetLength(0); j++)
                    {
                        if (patterns[j, 0].SequenceEqual(sector.Skip(i).Take(patterns[j, 0].Length)))
                        {
                            flags[j] = true;
                        }
                        if (patterns[j, 1].SequenceEqual(sector.Skip(i).Take(patterns[j, 1].Length)))
                        {
                            patched[j] = true;
                        }
                    }
                }

                if (flags.All(x => x))
                {
                    patchAction();
                    return (true, false);
                }
                if (patched.All(x => x))
                {
                    patchAction();
                    return (false, true);
                }

                return (false, false);
            }

            void PatchSector(byte[,][] patterns)
            {
                for (int i = 0; i < sector.Length; i++)
                {
                    for (int j = 0; j < patterns.GetLength(0); j++)
                    {
                        if (patterns[j, 0].SequenceEqual(sector.Skip(i).Take(patterns[j, 0].Length)))
                        {
                            Buffer.BlockCopy(patterns[j, 1], 0, sector, i, patterns[j, 1].Length);
                        }
                    }
                }
            }

            void LogSectorStatus(bool[] rl_6, bool[] rl_2, bool[] rl_1)
            {
                int r6 = rl_6.Count(x => x);
                int r2 = rl_2.Count(x => x);
                int r1 = rl_1.Count(x => x);
            }
        }

        (byte[], byte[]) RapidLok_Key_Fix(byte[] data, byte[] new_key = null)
        {
            byte[] newkey = FastArray.Init(7153, 0xff);
            byte[] key = new byte[256];
            byte[] _key = new byte[54];
            int s = 0;
            if (data[s] == 0x6b) s += 200;
            for (int i = s; i < data.Length; i++)
            {
                if ((data[i] == 0x6b) && (i + 256) < data.Length)
                {
                    Buffer.BlockCopy(data, i, key, 0, 256);
                    break;
                }
            }
            if (new_key != null) Buffer.BlockCopy(new_key, 0, key, 0, new_key.Length);
            for (int i = 54; i < key.Length; i++)
            {
                if (key[i] != 0xff) key[i] = 0x00;
                if (i >= 250) key[i] = 0xff;
                if (i < 250) key[i] = 0x00;
            }
            Buffer.BlockCopy(key, 0, newkey, newkey.Length - 286, 256);
            Buffer.BlockCopy(key, 0, _key, 0, _key.Length);
            return (newkey, _key);
        }

        (byte[], int, int, int, int, int, string[]) RapidLok_Track_Info(byte[] data, int trk, bool build, byte[] track_ID, int rl_7b_len = 0)
        {
            bool sevenb = false;
            bool trk_id = false;
            bool sync = false;
            bool start_found = false;
            bool end_found = false;
            var track = tracks > 42 ? (trk / 2) + 1 : trk + 1;
            var errors = 0;
            var rl_seclen = 583;
            var d_start = 0;
            var d_end = 0;
            var sectors = 0;
            var pos = 0;
            var snc_cnt = 0;
            var sevenb_pos = 0;
            var header = new string[0];
            var pre_head = new byte[32];
            var nsb = rl_7b_len;
            var sb_sec = rl_7b_len;
            var tid = new byte[0];
            var first_sector = -1;
            var headers = new List<string>();
            var a_headers = new List<string>();
            var sec_pos = new List<int>();
            var secds_pos = new List<int>();
            var secde_pos = new List<int>();
            var sec_data = new List<byte[]>();
            var sec_head = new List<byte[]>();
            var source = new BitArray(Flip_Endian(data));
            var adata = new byte[0];
            Compare();
            while (pos < source.Length)
            {
                if (source[pos]) sync = snc_cnt++ >= 24;
                else
                {
                    if (sync) Compare();
                    snc_cnt = 0;
                    sync = false;
                }
                pos++;
            }
            int diff = 0;
            try
            {
                if (pos >= source.Length - 1 && !end_found)
                {
                    if (pos >= source.Length) pos = source.Length - 1;
                    d_end = pos;
                    int c_len = pos - secds_pos[secds_pos.Count - 1] - 1;
                    diff = ((583 + 21) * 8) - c_len;
                    d_start -= diff;
                }
            }
            catch { }
            if (build && sectors > 10) Rebuild_RapidLok_Track();
            if ((track < 18 && sectors < 12) || track > 18 && sectors < 11)
            {
                int needed = track < 18 ? 12 : 11;
                string msg = $"Track ({track}) is missing {needed - sectors} sector(s)";
                if (!ErrorList.Contains(msg)) ErrorList.Add(msg);
            }
            if (!batch && errors > 0)
            {
                string msg = $"Track ({track}) checksum failed on {errors} sector(s)";
                if (!ErrorList.Contains(msg)) ErrorList.Add(msg);
            }
            return (adata, d_start, d_end, d_end - d_start, sectors, rl_7b_len, a_headers.ToArray());

            void Rebuild_RapidLok_Track()
            {
                var den = (track >= 18) ? 1 : 0; // Determine density index based on track number
                var snc = 20;   // (20) Sync length before the 0x7b sector (40) before Track ID (60) Before first RapidLok sector
                var os_sync = FastArray.Init(5, 0xff); // normal sector sync (every sector after the first sector)
                using (MemoryStream buffer = new MemoryStream())
                using (BinaryWriter write = new BinaryWriter(buffer))
                {
                    var cursec = (first_sector == sectors || first_sector == -1) ? 0 : first_sector;
                    if (sb_sec > 0)
                    {
                        write.Write(ArrayConcat(FastArray.Init(snc, 0xff), new byte[] { 0x55 }, (nsb == 0)
                            ? FastArray.Init(sb_sec - 1, 0x7b) : FastArray.Init(nsb, 0x7b)));
                    }
                    write.Write(ArrayConcat(FastArray.Init(snc << 1, 0xff), Verify_Track_ID(tid), FastArray.Init((snc * 3) - os_sync.Length, 0xff)));
                    var rem = Math.Max((density[den] - ((int)buffer.Length + (sectors * (583 + 7 + (os_sync.Length << 1))))) / (sectors << 1), 5);
                    var sector_gap = FastArray.Init(rem, 0x00);
                    for (int i = 0; i < sectors; i++, cursec = (cursec + 1) % sectors)
                    {
                        if (sec_data?[cursec]?.Length == 583)
                            write.Write(ArrayConcat(os_sync, sec_head[cursec], sector_gap, os_sync, sec_data[cursec], sector_gap));
                    }
                    if (buffer.Length < density[den]) write.Write((FastArray.Init(density[den] - (int)buffer.Length, 0x55)));
                    adata = buffer.ToArray();
                }
            }

            byte[] Verify_Track_ID(byte[] tk_id)
            {
                if (track_ID.Length == 4)
                {
                    var temp = Decode_CBM_GCR(tk_id);
                    if (temp[3] != track || (temp[4] != track_ID[0] && temp[5] != track_ID[1]))
                    {
                        byte[] ID = FastArray.Init(12, 0x55);
                        Buffer.BlockCopy(Build_BlockHeader(track, 0, track_ID), 0, ID, 0, 10);
                        return ID;
                    }
                }
                return tk_id;
            }

            void Compare()
            {
                const int bitBlockSize = 10 << 3;
                if (pos + bitBlockSize >= source.Length) return;
                var c = Bit2Byte(source, pos, bitBlockSize);
                if (MatchSeq(RLok_7b, c)) HandleRLok7bMatch();
                else if (trk_id) HandleTrackId(c);
                else if (c[0] == 0x52 && tid.Length == 0) tid = Bit2Byte(source, pos, 12 << 3);
                if (c[0] == 0x75 && (c[4] == 0xd6 || c[5] == 0xed)) HandleC0x75(c);
            }

            void HandleRLok7bMatch()
            {
                if (sevenb)
                {
                    end_found = true;
                    d_end = pos;
                }
                else
                {
                    sevenb = true;
                    sevenb_pos = pos;
                    if (sb_sec == 0) FindSbSec();
                    first_sector = sectors;
                }

                if (!start_found)
                {
                    start_found = true;
                    d_start = pos;
                }
            }

            void HandleTrackId(byte[] d)
            {
                if (d[0] == 0x52)
                {
                    end_found = true;
                    d_end = pos;
                }
            }

            void FindSbSec()
            {
                int sl = 0;
                int tsnc = 0;
                for (int i = pos; i < source.Length; i++)
                {
                    if (source[i]) tsnc++;
                    else
                    {
                        if (tsnc >= 16)
                        {
                            sb_sec = (sl - tsnc - 8) >> 3;
                            a_headers.Add($"Security sector (0x7B) Length {sb_sec}");
                            break;
                        }
                        tsnc = 0;
                    }
                    sl++;
                }
            }

            void HandleC0x75(byte[] d)
            {
                if (!start_found)
                {
                    start_found = true;
                    d_start = pos;
                }
                string hdr = string.Empty;
                try { hdr = Hex_Val(Decode_RL_Data(CopyFrom(d, 1)).Item1); } catch { }
                string head = Hex_Val(d, 0, 7); //6
                if (!headers.Any(x => x == head))
                {
                    if (!batch)
                    {
                        int ckm = VerifySectorData();
                        string cksm = string.Empty;
                        switch (ckm)
                        {
                            case 0: cksm = "Failed!"; break;
                            case 1: cksm = "OK"; break;
                            case 2: cksm = "Empty Sector, No Data"; break;
                        }
                        a_headers.Add($"sector ({headers.Count}) Header ID [ {hdr} ] Checksum ({cksm})");
                        if (ckm < 1) errors++;
                    }
                    sectors++;
                    if (build) BuildSectorData();
                }
                else
                {
                    if (!end_found)
                    {
                        end_found = true;
                        d_end = pos;
                        a_headers.Add($"pos ({pos / 8}) {hdr} ** Repeat **");
                    }
                }
                headers.Add(Hex_Val(d, 0, 7)); //6
                sec_head.Add(d.Take(7).ToArray());
                sec_pos.Add(pos);
            }

            int VerifySectorData()
            {
                int tpos = pos;
                int tsnc = 0;
                while (tpos < source.Length - 16)
                {
                    if (source[tpos]) tsnc++;
                    else
                    {
                        if (tsnc > 24)
                        {
                            byte[] cc = Bit2Byte(source, tpos, 16);
                            byte[] sdt = DetermineSectorData(cc, tpos);
                            if (sdt != null)
                            {
                                if (sdt[0] == 0x55 && sdt[1] == 0x55) return 2;
                                bool valid_Checksum = Decode_RL_Data(sdt).Item2;
                                return valid_Checksum ? 1 : 0;
                            }
                        }
                        tsnc = 0;
                    }
                    tpos++;
                }
                return 0;
            }

            void BuildSectorData()
            {
                int tpos = pos;
                int tsnc = 0;
                while (tpos < source.Length - 16)
                {
                    if (source[tpos]) tsnc++;
                    else
                    {
                        if (tsnc > 24)
                        {
                            var cc = Bit2Byte(source, tpos, 16);
                            var sdt = DetermineSectorData(cc, tpos);
                            if (sdt != null)
                            {
                                sec_data.Add(sdt);
                                secds_pos.Add(tpos);
                                secde_pos.Add(tpos + (rl_seclen * 8));
                                break;
                            }
                        }
                        tsnc = 0;
                    }
                    tpos++;
                }
            }

            byte[] DetermineSectorData(byte[] cc, int tpos)
            {
                if (cc[0] == 0x6b || (cc[0] == 0x55 && cc[1] == 0x55))
                {
                    if (pos + (rl_seclen << 3) < source.Length)
                    {
                        if (cc[0] == 0x6b)
                        {
                            var sdt = FastArray.Init(rl_seclen, 0x00);
                            sdt = Bit2Byte(source, tpos, sdt.Length << 3);
                            return sdt;
                        }
                    }
                    else if (cc[0] == 0x6b)
                    {
                        var sdt = HandleIncompleteSector(cc, tpos);
                        return sdt;
                    }
                    if (cc[0] == 0x55) return FastArray.Init(rl_seclen, 0x55);
                }
                return null;
            }

            byte[] HandleIncompleteSector(byte[] cc, int tpos)
            {
                var sdt = Bit2Byte(source, tpos);
                var dif = rl_seclen - sdt.Length;
                var comp = new byte[16];
                Buffer.BlockCopy(sdt, sdt.Length - 16, comp, 0, 16);
                for (int i = 0; i < 8000; i++)
                {
                    var find = Bit2Byte(source, i, comp.Length << 3);
                    if (MatchSeq(find, comp))
                    {
                        var rpos = i + (comp.Length << 3);
                        var rem = Bit2Byte(source, rpos, dif << 3);
                        using (MemoryStream buffer = new MemoryStream())
                        using (BinaryWriter write = new BinaryWriter(buffer))
                        {
                            write.Write(sdt);
                            write.Write(rem);
                            sdt = buffer.ToArray();
                        }
                        break;
                    }
                }
                return sdt;
            }
        }

        (byte[] data, bool checksum) Decode_Rapidlok_GCR(byte[] sector, bool just_the_sector = false)
        {
            if (sector == null) return (new byte[0], false);
            (byte[] decoded, bool cksm) = Decode_RL_Data(sector);
            RL_Decrypt(decoded);
            if (!just_the_sector) return (decoded, cksm);
            return (decoded.Length > 10 ? CopyFrom(decoded, 10) : decoded, cksm);
        }

        byte[] RL_Decrypt(byte[] data)
        {
            for (int i = 10; i < data.Length; i++)
            {
                data[i] = (byte)(RapidLok_Decode_High[(byte)(data[i] >> 4)] | RapidLok_Decode_Low[(byte)(data[i] & 0x0f)]);
            }
            return data;
        }
    }
}