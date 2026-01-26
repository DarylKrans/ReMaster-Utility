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
        (byte[] data, int start, int end, int length, int sec_zero, string[] info, int sectors) Get_BD_Track_Info(byte[] data, int trk)
        {
            if (data == null) return (new byte[0], 0, 0, 0, 0, new string[1], 0);
            int track = tracks > 42 ? (trk / 2) : trk, header_len = 10 << 3, sec_len = 2065 << 3;
            int d = density[density_map[track]];
            int data_start = 0, data_end = 0, sectors = 0, pos = 0, curpos = 0, secz = 0, snc = 0, offset = 10, ostart = 0;
            bool start_found = false, end_found = false, weak = false, csm = false;
            byte[] vhead = new byte[4];
            byte sc = 0, tk = 0;
            ushort comp = 0;
            string bad = "Failed!";
            string ok = "OK";
            string wk = "N/A";
            List<string> info = new List<string>();
            List<string> headers = new List<string>();
            List<int> err = new List<int>();
            byte[][] dsec = new byte[3][];
            byte[] tdata = CopyArray(data);
            BitArray source = new BitArray(Flip_Endian(tdata));
            while (pos < source.Length)
            {
                comp <<= 1;
                if (source[pos])
                {
                    comp |= 1;
                    snc++;
                }
                else snc = 0;
                if (comp == 0xffff && Bit2Byte(source, pos + 1, 8)[0] == 0x52 && pos + header_len < source.Length)
                {
                    curpos = pos + 1;
                    var hdr = Bit2Byte(source, curpos, header_len);
                    vhead[0] = 0x52;
                    for (int i = 1; i < sz.Length; i++) vhead[i] = (byte)(hdr[i] & sz[i]);
                    if (valid_cbm.Any(x => x == Hex_Val(vhead)))
                    {
                        csm = false;
                        {
                            byte[] sec_data = new byte[0];
                            if (curpos + sec_len < source.Length) sec_data = Bit2Byte(source, curpos, sec_len);
                            else
                            {
                                int rpos = 0;
                                (sec_data, rpos) = HandleIncompleteSector(source, curpos, sec_len);
                                if (start_found && !end_found && sectors >= 2)
                                {
                                    ostart = data_start;
                                    data_start = rpos;
                                    data_end = (source.Length >> 3) << 3;
                                    end_found = true;
                                }
                            }

                            int sec_pos = offset;
                            (tk, sc) = Get_Current_Sector(sec_data, sec_pos);
                            if (tk != track || sc > 2)
                            {
                                sec_pos -= 2;
                                sec_len = 2063 << 3;
                                (tk, sc) = Get_Current_Sector(sec_data, sec_pos);
                            }
                            sec_pos += 4;
                            string sec_header = Hex_Val(new byte[] { tk, sc });
                            if (tk == track && sc < 3 && !headers.Contains(sec_header))
                            {
                                string szf = string.Empty;
                                weak = false;
                                headers.Add(sec_header);
                                if (!start_found)
                                {
                                    start_found = true;
                                    data_start = curpos;
                                }
                                if (sc == 2)
                                {
                                    secz = pos - snc;
                                    szf = "*";
                                }
                                byte[] dec_sec = new byte[1024];
                                sectors++;
                                for (int i = 0; i < dec_sec.Length; i++)
                                {
                                    dec_sec[i] = Decode_BDS_Pair(sec_data[sec_pos++], sec_data[sec_pos++]);
                                }
                                byte parity = Decode_BDS_Pair(sec_data[sec_pos++], sec_data[sec_pos++]);
                                byte checksum = 0;
                                foreach (byte p in dec_sec) checksum ^= p;
                                csm = checksum == parity;
                                dsec[sc] = CopyArray(sec_data, 0, sec_len >> 3);

                                if (tk == 0 && sc == 0)
                                {
                                    for (int i = 0; i < dsec[sc].Length - 1; i++)
                                    {
                                        if (weakTable[dsec[sc][i]])
                                        {
                                            dsec[sc][i] = 0x00;
                                            weak = true;
                                        }
                                    }
                                }
                                if (!csm && !weak) err.Add(sectors);
                                info.Add($"sector ({sc}){szf} pos ({pos >> 3}), Length {sec_len >> 3}, Sector ({(weak ? wk : csm ? ok : bad)}) {(weak ? "*Weak-Bits" : "")}");
                            }
                            else
                            {
                                end_found = true;
                                data_start = ostart;
                                data_end = curpos; // - 1;
                                info.Add($"** Repeat Sector ({sc}) pos ({pos >> 3})");
                                break;
                            }
                        }
                    }
                }
                pos++;
            }
            if (!end_found) data_end = (source.Count >> 3) << 3;

            byte[] adata = new byte[0];
            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter write = new BinaryWriter(buffer))
            {
                for (int i = 2; i >= 0; i--)
                {
                    write.Write(FastArray.Init(i == 2 ? 9 : 5, 0xff));
                    write.Write(dsec[i]);
                }
                int rem = d - (int)buffer.Length;
                if (rem > 0) write.Write(FastArray.Init(rem, 0x55));
                adata = buffer.ToArray();
            }
            Set_Dest_Arrays(adata, trk);
            if (!batch && err.Count > 0) foreach (var s in err) ErrorList.Add($"Sector {sc} failed on track {track + 1}");
            return (adata, data_start, data_end, data_end - data_start, secz, info.ToArray(), sectors);
        }

        (byte[] data, int start, int end, int length, int sec_zero, string[] info, int sectors, byte[][] sector) Get_BD_Loader_Info(byte[] data, int trk)
        {
            if (data == null) return (new byte[0], 0, 0, 0, 0, new string[1], 0, new byte[0][]);
            int track = tracks > 42 ? (trk / 2) : trk, header_len = 10 << 3, sec_len = 4618 << 3;
            int d = density[density_map[track]];
            int data_start = 0, data_end = 0, sectors = 0, pos = 0, curpos = 0, secz = -1, snc = 0, offset = 10, ostart = 0;
            bool start_found = false, end_found = false, weak = false, csm = false;
            ushort comp = 0;
            string bad = "Failed!";
            string ok = "OK";
            List<string> info = new List<string>();
            List<string> headers = new List<string>();
            List<int> err = new List<int>();
            byte[] tdata = CopyArray(data);
            BitArray source = new BitArray(Flip_Endian(tdata));
            List<byte[]> decsec = new List<byte[]>();
            //File.WriteAllBytes($@"c:\test\loader", Bit2Byte(source));
            while (pos < source.Length)
            {
                comp <<= 1;
                if (source[pos])
                {
                    comp |= 1;
                    snc++;
                }
                else snc = 0;
                if (!start_found && comp == 0xffff && Bit2Byte(source, pos + 1, 8)[0] == 0x52)
                {
                    start_found = true;
                    data_start = pos - snc;
                    secz = data_start;
                    curpos = pos + 1;
                    BitArray nsnc = BitCopy(source, curpos, 32 << 3);
                    snc = 0;
                    for (int i = 0; i < nsnc.Length; i++)
                    {
                        if (nsnc[i]) snc++;
                        else
                        {
                            if (snc >= 10)
                            {
                                curpos += i;
                                break;
                            }
                            snc = 0;
                        }
                    }

                    byte[] sec_data = new byte[0];
                    if (curpos + sec_len < source.Length) sec_data = Bit2Byte(source, curpos, sec_len);
                    else
                    {
                        int rpos = 0;
                        (sec_data, rpos) = HandleIncompleteSector(source, curpos, sec_len);
                        if (start_found && !end_found) // && sectors >= 2)
                        {
                            ostart = data_start;
                            data_start = rpos;
                            data_end = (source.Length >> 3) << 3;
                            end_found = true;
                        }
                    }
                    sectors = 3;
                    int spos = 1;
                    int sl = 1538;
                    for (int i = 0; i < 3; i++)
                    {
                        int pp = spos + (i * sl);
                        byte[] csec = CopyArray(sec_data, pp + i, sl);
                        //(byte[] dec, bool par) = Decode_BDS_GCR(CopyArray(sec_data, pp + i, sl), true, true);
                        (byte[] dec, bool par) = Decode_BDS_GCR(csec, true, true);
                        decsec.Add(csec);
                        info.Add($"sector ({i}) pos ({(pos >> 3) + pp}), Length {dec.Length}, Sector ({(par ? ok : bad)})");

                        if (!par) err.Add(i);
                    }
                    //dsec = CopyArray(sec_data);
                }
                else
                {
                    if (start_found)
                    {
                        data_end = pos;
                        end_found = true;
                        break;
                    }
                }
                if (end_found) break;
                    pos++;
            }
            if (!end_found) data_end = (source.Count >> 3) << 3;

            //BitArray dest = new BitArray(data_end - data_start);
            BitArray dest = new BitArray(d << 3);
            pos = secz;
            //for (int i = 0; i < dest.Length; i++)
            for (int i = 0; i < data_end - data_start; i++)
            {
                dest[i] = source[pos++];
                if (pos == data_end) pos = data_start;
                if (i >= dest.Length - 1) break;
            }
            byte[] adata = Bit2Byte(dest);
            Set_Dest_Arrays(adata, trk);
            info.Add($"start {data_start >> 3} end {data_end >> 3} len {(data_end - data_start) >> 3} sz {secz >> 3}");
            if (!batch && err.Count > 0) foreach (var s in err) ErrorList.Add($"Sector {s} failed on track {track + 1}");
            return (adata, data_start, data_end, data_end - data_start, secz, info.ToArray(), sectors, decsec.ToArray());
        }

        //(byte[] data, int start, int end, int length, int sec_zero, string[] info, int sectors) Get_BD_Loader_Info(byte[] data, int trk)
        //{
        //    if (data == null) return (new byte[0], 0, 0, 0, 0, new string[1], 0);
        //    int track = tracks > 42 ? (trk / 2) : trk, header_len = 10 << 3, sec_len = 4618 << 3;
        //    int d = density[density_map[track]];
        //    int data_start = 0, data_end = 0, sectors = 0, pos = 0, curpos = 0, secz = -1, snc = 0, offset = 10, ostart = 0;
        //    bool start_found = false, end_found = false, weak = false, csm = false;
        //    ushort comp = 0;
        //    string bad = "Failed!";
        //    string ok = "OK";
        //    List<string> info = new List<string>();
        //    List<string> headers = new List<string>();
        //    List<int> err = new List<int>();
        //    byte[] dsec = new byte[0];
        //    byte[] tdata = CopyArray(data);
        //    BitArray source = new BitArray(Flip_Endian(tdata));
        //    while (pos < source.Length)
        //    {
        //        comp <<= 1;
        //        if (source[pos])
        //        {
        //            comp |= 1;
        //            snc++;
        //        }
        //        else snc = 0;
        //        if (comp == 0xffff && Bit2Byte(source, pos + 1, 8)[0] == 0x52)
        //        {
        //            if (start_found)
        //            {
        //                end_found = true;
        //                data_end = pos - snc;
        //                break;
        //            }
        //            if (!start_found)
        //            {
        //                secz = pos - snc;
        //                start_found = true;
        //                data_start = secz;
        //            }
        //        }
        //        if (!end_found && comp == 0xffff && (MatchSeq(Bit2Byte(source, pos + 1, 16), new byte[] { 0x55, 0x55 })))
        //        {
        //            curpos = pos + 1;
        //            byte[] sec_data = new byte[0];
        //            if (curpos + sec_len < source.Length) sec_data = Bit2Byte(source, curpos, sec_len);
        //            else
        //            {
        //                int rpos = 0;
        //                (sec_data, rpos) = HandleIncompleteSector(source, curpos, sec_len);
        //            }
        //            sectors = 3;
        //            int spos = 1;
        //            int sl = 1538;
        //            for (int i = 0; i < 3; i++)
        //            {
        //                int pp = spos + (i * sl);
        //                (byte[] dec, bool par) = Decode_BDS_GCR(CopyArray(sec_data, pp + i, sl), true, true);
        //                info.Add($"sector ({i}) pos ({(pos >> 3) + pp}), Length {dec.Length}, Sector ({(par ? ok : bad)})");
        //                if (!par) err.Add(i);
        //            }
        //            dsec = CopyArray(sec_data);
        //        }
        //        pos++;
        //    }
        //    if (!end_found) data_end = (source.Count >> 3) << 3;
        //
        //    byte[] adata = new byte[0];
        //    //using (MemoryStream buffer = new MemoryStream())
        //    //using (BinaryWriter write = new BinaryWriter(buffer))
        //    //{
        //    //    for (int i = 2; i >= 0; i--)
        //    //    {
        //    //        write.Write(FastArray.Init(i == 2 ? 9 : 5, 0xff));
        //    //        write.Write(dsec[i]);
        //    //    }
        //    //    int rem = d - (int)buffer.Length;
        //    //    if (rem > 0) write.Write(FastArray.Init(rem, 0x55));
        //    //    adata = buffer.ToArray();
        //    //}
        //    
        //    Set_Dest_Arrays(adata, trk);
        //    info.Add($"start {data_start >> 3} end {data_end >> 3} len {(data_end - data_start) >> 3} sz {secz >> 3}");
        //    if (!batch && err.Count > 0) foreach (var s in err) ErrorList.Add($"Sector {s} failed on track {track + 1}");
        //    return (adata, data_start, data_end, data_end - data_start, secz, info.ToArray(), sectors);
        //}

        (byte, byte) Get_Current_Sector(byte[] sec, int spos)
        {
            byte tck = Decode_BDS_Pair(sec[spos++], sec[spos++]);
            byte sct = Decode_BDS_Pair(sec[spos++], sec[spos++]);
            return (tck, sct);
        }

        (byte[] data, int position) HandleIncompleteSector(BitArray source, int tpos, int sec_len)
        {
            var sdt = Bit2Byte(source, tpos, ((source.Length - tpos) >> 3) << 3);
            var dif = (sec_len >> 3) - sdt.Length;
            var len = Math.Min(sdt.Length, 2065);
            var cmp = new byte[len];
            int rpos = 0;
            Buffer.BlockCopy(sdt, sdt.Length - len, cmp, 0, len);
            for (int i = 0; i < source.Length; i++)
            {
                var find = Bit2Byte(source, i, cmp.Length << 3);
                if (MatchSeq(find, cmp))
                {
                    rpos = i + (cmp.Length << 3);
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
            return (sdt, rpos);
        }

        (byte[] sec, bool checksum, int position) Find_BDS_Sector(byte[] data, int trk, int sector, bool decode = true)
        {
            if (data == null) return (new byte[0], false, -1);
            int track = tracks > 42 ? (trk / 2) : trk;
            BitArray source = new BitArray(Flip_Endian(data));
            int pos = 0, sec_len = 2065 << 3, curpos;
            byte comp = 0;
            while (pos < source.Length)
            {
                comp <<= 1;
                if (source[pos]) comp |= 1;
                if (comp == 0xff && Bit2Byte(source, pos + 1, 8)[0] == 0x52) // && pos + header_len < source.Length)
                {
                    curpos = pos + 1;
                    byte[] sec_data = new byte[0];
                    if (pos + sec_len < source.Length) sec_data = Bit2Byte(source, curpos, sec_len);
                    else sec_data = HandleIncompleteSector(source, curpos, sec_len).data;

                    int sec_pos = 10;
                    (byte tk, byte sc) = Get_Current_Sector(sec_data, sec_pos);
                    if (sc > 2)
                    {
                        sec_pos -= 2;
                        sec_len = 2063 << 3;
                        (tk, sc) = Get_Current_Sector(sec_data, sec_pos);
                    }
                    if (sc == sector)
                    {
                        sec_pos += 4;
                        byte[] rawsec = CopyArray(sec_data, sec_pos, 2050);
                        (byte[] dec_sec, bool csm) = Decode_BDS_GCR(rawsec, true, tk == 0);
                        return (decode ? dec_sec : rawsec, csm, curpos + (sec_pos << 3));
                    }
                }
                pos++;
            }
            return (new byte[0], false, -1);
        }

        byte[] Replace_BDS_Sector(byte[] data, int trk, int sector, byte[] newsec)
        {
            if (data == null || newsec == null || tracks < 0 || sector < 0 || sector > 2) return data;
            int track = tracks > 42 ? (trk / 2) : trk;
            BitArray source = new BitArray(Flip_Endian(data));
            if (newsec.Length == 1024) newsec = Encode_BDS_GCR(newsec, track == 0, true);
            BitArray nsec = new BitArray(Flip_Endian(newsec));
            int pos = 0, sec_len = 2065 << 3, curpos;
            byte comp = 0;
            while (pos < source.Length)
            {
                comp <<= 1;
                if (source[pos]) comp |= 1;
                if (comp == 0xff && Bit2Byte(source, pos + 1, 8)[0] == 0x52) // && pos + header_len < source.Length)
                {
                    curpos = pos + 1;
                    byte[] sec_data = new byte[0];
                    if (pos + sec_len < source.Length) sec_data = Bit2Byte(source, curpos, sec_len);
                    else sec_data = HandleIncompleteSector(source, curpos, sec_len).data;

                    int sec_pos = 10;
                    (byte tk, byte sc) = Get_Current_Sector(sec_data, sec_pos);
                    if (sc > 2)
                    {
                        sec_pos -= 2;
                        sec_len = 2063 << 3;
                        (tk, sc) = Get_Current_Sector(sec_data, sec_pos);
                    }
                    if (sc == sector)
                    {
                        sec_pos += 4;
                        curpos += (sec_pos << 3);
                        for (int i = 0; i < nsec.Length; i++) source[curpos + i] = nsec[i];
                        return Bit2Byte(source);
                    }
                }
                pos++;
            }
            return data;
        }
    }
}
