using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

/// CBM Block Header structure
/// 8 plain bytes converted to 10 GCR bytes
/// 
/// Byte 0          0x08  (always 08)
/// byte 1          EOR of next 4 bytes (sector, track, ID byte 2, ID byte 1)
/// byte 2          Sector #
/// byte 3          Track #
/// byte 4          Disk ID byte 2
/// byte 5          Disk ID byte 1
/// byte 6          0x0f (filler to make full GCR chunk) not used
/// byte 7          0x0f (filler to make full GCR chunk) not used
///
/// CBM Block Data structure
/// 
/// byte 0          0x07 (always 07) Sector marker?
/// byte 1-256      (sector data) 256 bytes
/// byte 257        Parity (0 ^ bytes 1-257)
/// byte 258-260    0x00 (not used)
/// 
/// Standard CBM sector
/// 
/// byte 0          (track of next sector in file) 0x00 if end of file
/// byte 1          (sector # of next sector in fiel) 0xff if end of file
/// byte 2-256      file data

/// CBM File Type bit settings
///     BIT: 7  6  5  4 | 3  2  1  0
/// DEL      -  -  -  - | 0  0  0  0
/// SEQ      -  -  -  - | 0  0  0  1
/// PRG      -  -  -  - | 0  0  1  0
/// USR      -  -  -  - | 0  0  1  1
/// REL      -  -  -  - | 0  1  0  0
/// OK       1  -  -  - | -  -  -  -
/// SPLAT    0  -  -  - | -  -  -  -
/// LOCK     -  1  -  - | -  -  -  -

/// BAM Layout (Block Allocation Map)
/// t18 s0
/// byte 0  Track#                      hex 0x12
/// byte 1  Next sector in directory    hex 0x01
/// byte 2  hex 0x41
/// byte 3  hex 0x00
/// byte 4 - 143 (map of free and allocated blocks) in sets of 4 bytes
///              byte 0 # of available sectors in track (starting at track 1)
///              bytes 1 - 3 set each bit to '1' for block available and '0' for used blocks up to the # of sectors in that track
///              repeat for each track up to 35
/// 




namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        //readonly bool write_dir = false;
        private static readonly byte[] sz = { 0x52, 0xc0, 0x0f, 0xfc };
        private static readonly byte cbm_gap = 0x55;
        int SelectionLength = 0;


        //private static HashSet<byte[]> vldHashes = new HashSet<byte[]>();
        private static HashSet<byte[]> _vHash = new HashSet<byte[]>();

        byte[] Rebuild_CBM(byte[] data, int sectors, byte[] Disk_ID, int t_density, int trk, int start, bool cyan = false)
        {
            if (!(data?.Length > 0)) return null;
            BitArray tk = new BitArray(Flip_Endian(data));
            sectors = sectors < Available_Sectors[trk] ? Available_Sectors[trk] : sectors;

            int dif = cyan ? 3 : 0;
            int errorCode = 1;
            int pos;
            int[] c = new int[] { 2, 3, 4, 5, 6 };
            bool alt = (Disk.Source.Track.Any(x => c.Contains(x.Format)));

            byte[] nosync = FastArray.Init(5, cbm_gap);
            byte[] noheader = FastArray.Init(10, cbm_gap);
            byte[] emptySector = Encode_CBM_GCR(Create_Empty_Sector());
            byte[] sync = FastArray.Init(5, 0xff);
            byte[] head_gap = SetSectorGap(sector_gap_density[t_density] - dif);
            byte[] tail_gap = SetSectorGap(sector_gap_density[t_density] + dif);
            byte[] current_sector;
            byte[] block_header = new byte[10];
            start = !alt ? 0 : start;
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            for (int i = 0; i < sectors; i++)
            {
                (current_sector, errorCode, pos) = GetSectorWithErrorCode(data, i, false, null, tk, start);
                if (pos >= 0) block_header = Bit2Byte(tk, pos, 80);
                block_header = (alt && pos >= 0) ? Bit2Byte(tk, pos, 80) : Build_BlockHeader(trk, i, Disk_ID);
                write.Write((errorCode == 2 || errorCode == 3) ? nosync : sync);
                write.Write(errorCode == 2 ? noheader : block_header);
                write.Write(head_gap);
                write.Write(errorCode == 3 ? nosync : sync);
                write.Write((errorCode == 2 || errorCode == 4 || current_sector == null) ? emptySector : current_sector);
                if (i != sectors - 1) write.Write(tail_gap);
            }
            //write.Write(ArrayConcat(FastArray.Init(15, 0x55), FastArray.Init(3, 0x00)));
            if (buffer.Length > density[t_density] && t_density > 0)
            {
                while (buffer.Length > density[t_density] && t_density > 0) t_density -= 1;
            }
            int rem = (int)(density[t_density] - buffer.Length);
            if (rem > 0)
            {
                write.Write(FastArray.Init(rem, cbm_gap));
            }

            return buffer.ToArray();
        }

        (bool cart, bool manual) CBM_Track_Info(ref Disk_Track T, bool countErrors = true)
        {
            int track = (int)T.TrackNumber;
            if (T.Bits == null)
            {
                if (T.Data == null || T.Data.Length == 0) return (false, false);
                T.Bits = new BitArray(Flip_Endian(T.Data));
            }
            BitArray source = T.Bits;
            byte[] _psec = track == 18 ? new byte[] { 0x55, 0x5b, 0x5d } : new byte[] { 0x55 };
            int pos = 0, trackID = 0, sncCnt = 0, tSync = 0, minSnc = 10, blkLen = 325 << 3, vplLen = 513 << 3;
            bool startFound = false, endFound = false, secZero = false, vpLDR = false;
            bool manP = false, cartP = false, mps = false, vpl = false;  // tracks manual lookup / cartridge check bools
            string repeatSector = string.Empty;
            byte[] _trackID = new byte[0];
            byte _fbyte = 0;
            List<string> err = new List<string>();
            List<string> headers = new List<string>();
            List<int> sectors = new List<int>();
            List<Sector> _sectors = new List<Sector>();
            List<uint> _vldPos = new List<uint>();
            // - Main loop to find sectors
            Compare(ref T, pos);
            while (pos < source.Length - 32)
            {
                if (source[pos]) sncCnt++;
                else
                {
                    if (sncCnt >= minSnc) Compare(ref T, pos);
                    sncCnt = 0;
                }
                if (endFound) break;
                pos++;
            }
            // - Main loop end. All unique sectors found (doesn't account for duplicates -some protections may fail)
            if (!endFound)
            {
                if (startFound && !endFound && T.Start == 0)
                {
                    T.End = (density[density_map[track - 1]] + 80) << 3;
                    T.Adjust = false;
                }
                else T.End = source.Length - 1;
            }
            // Find highest sector ID found
            int _maxID = _sectors.Max(s => s.ID);
            int maxSector = Math.Max(_maxID, Available_Sectors[(int)T.TrackNumber] - 1);
            int _firstNotFound = -1;
            // Add missing sectors with ErrorCode 2 (header not found)
            for (int i = 0; i <= maxSector; i++)
            {
                if (!_sectors.Any(s => s.ID == i))
                {
                    if (debug) Console.WriteLine($"Track {track} Sector {i} : Not Found!");
                    if (_firstNotFound == -1) _firstNotFound = i;
                    _sectors.Add(new Sector
                    {
                        ID = i,
                        ErrorCode = 2,
                        //Format = 0 // Only set to 0 if Sector class 'Format' default is -1
                    });
                }
            }
            if (_vldPos.Count == 4) GetVorpalLoaderSectors(ref T, _firstNotFound, _vldPos.ToArray());

            if (debug)
            {
                if (track == 18 && vpLDR) Console.WriteLine($"track {track} max Sector ID {_maxID}, First not found {_firstNotFound} has Vorpal Loader? ({_vldPos.Count == 4 || vpLDR}) Uses Standard headers? ({T.Spec.VL.Sectors < 0})");
                if (T.Spec.VL.Sectors > 0)
                {

                    Console.WriteLine("---------- Vorpal Loader Injection (no sector headers) -----------");
                    Console.WriteLine($"Inject loader at sector {T.Spec.VL.InjectAt} Total Sectors {T.Spec.VL.Sectors}");
                    foreach (var s in T.Spec.VL.Sector)
                    {
                        bool cksm = s.Data.Checksum; // VerifyHash(s.Data.Hash, _vHash);
                        string info = $"Track Pos: {s.Data.Pos >> 3}, Sync Length: {s.Data.SyncLen} (bits), Sector Pos: {s.ID}, GCR Length: {s.Data.GCR.Length} (bytes), Loader ID byte: ${Hex_Val(s.Data.GCR, 0, 1)} Valid? {cksm}";
                        Console.WriteLine(info);
                    }
                    Console.WriteLine("------------------------------------------------------------------");
                }
                else if (vpLDR)
                {
                    foreach (var s in _sectors.Where(x => x.Format == 3))
                    {
                        bool cksm = s.Data.Checksum; // VerifyHash(s.Data.Hash, _vHash);
                        foreach (var h in _vHash) if (MatchSeq(h, s.Data.Hash)) { cksm = true; break; }
                        Console.WriteLine($"{Hex_Val(s.Data.Hash)} {cksm}");
                    }
                }
            }

            if (T.CBMTrack == 0 && trackID != 0) T.CBMTrack = trackID;
            if (!secZero && _trackID.Length == 4) T.TrackID = _trackID;
            if (mps) T.Format = 10;
            if (vpl) T.Format = 15;
            T.Length = T.End - T.Start;
            T.Sectors = sectors.Count;
            T.Sector = _sectors;
            FindGaps(ref T);
            AddInfo(ref T);
            if (!batch && countErrors && err.Count > 0)
            {
                int errtk = track;
                foreach (var s in err) ErrorList.Add($"Parity failed on track {errtk} sector {s}");
            }
            return (cartP, manP);

            void Compare(ref Disk_Track _T, int p)
            {
                if (p + 80 < source.Length)
                {
                    var _gcr = Bit2Byte(source, p, 80);
                    var dec = Decode_CBM_GCR(_gcr).decoded;
                    if (_gcr[0] == 0x52 && dec[2] < 21 && dec[3] > 0 && dec[3] <= 42)
                    {
                        int secID = dec[2];
                        trackID = dec[3]; // save for later incase needed
                        if (!sectors.Any(x => x == secID))
                        {
                            sectors.Add(secID);
                            if (!startFound)
                            {
                                if (_trackID.Length == 0) _trackID = CopyArray(dec, 4, 4);
                                startFound = true;
                                _T.Start = p - sncCnt;
                            }
                            if (!secZero && secID == 0)
                            {
                                _T.TrackID = CopyArray(dec, 4, 4);
                                _T.CBMTrack = dec[3];
                                _T.SectorZero = pos - sncCnt;
                                secZero = true;
                            }
                            (bool blkSync, int sncLen, int blkPos) = CheckforBlockSync(p + 80);
                            Sector sector = new Sector { ID = secID };
                            (bool hdrCksum, byte actual, byte compare) = GetCBMChecksum(dec, 2, 4, 1);
                            sector.Header = new Sector.Info
                            {
                                Pos = p,
                                SyncLen = sncCnt,
                                GCR = _gcr,
                                Decoded = dec,
                                Checksum = hdrCksum,
                                ChecksumActual = actual,
                                ChecksumExpected = compare
                            };
                            if (blkPos != -1)
                            {
                                _fbyte = Bit2Byte(source, blkPos == -2 ? p + 80 : blkPos, 8)[0];
                                if (_psec.Contains(_fbyte))
                                {
                                    if (_fbyte == 0x55)
                                    {
                                        if (blkSync && sncLen != -1 && blkPos != -1) GetCBMSector(ref sector, sncLen, blkPos);
                                        if (!blkSync && sncLen == -2 && blkPos == -2) GetMicroProseSector(ref sector, track, p + 80);
                                    }
                                    else if (_fbyte == 0x5b || _fbyte == 0x5d)
                                        GetVorpalLoader(ref sector, sncLen, blkPos);
                                }
                                if (sector.Data.Checksum) pos += _fbyte == 0x55 ? blkLen : vplLen;
                            }
                            else pos += 80;
                            _sectors.Add(sector);
                        }
                        else
                        {
                            repeatSector = ($"pos {p >> 3} ** repeat ** Sector ({secID})");
                            if (_sectors[0].Header.SyncLen <= 0)
                            {
                                _sectors[0].Header.SyncLen = sncCnt;
                                sncCnt = 0;
                            }
                            endFound = true;
                            _T.End = p - sncCnt;
                        }
                    }
                    else if (sncCnt >= 10 && track == 18 && (_gcr[0] == 0x5b || _gcr[0] == 0x5d))
                    {
                        uint _p = ((uint)p << 16) | (uint)sncCnt;
                        _vldPos.Add(_p);
                    }
                }
            }


            (bool hasSync, int length, int position) CheckforBlockSync(int p)
            {
                if (p + blkLen < source.Length)
                {
                    int _tsnc = 0;
                    for (int j = 0; j < blkLen; j++)
                    {
                        if (source[p + j]) _tsnc++;
                        else
                        {
                            if (_tsnc >= 10 && p + j + blkLen < source.Length) return (true, _tsnc, p + j);
                            else _tsnc = 0;
                        }
                    }
                    return (false, -2, -2);
                }
                return (false, -1, -1);
            }

            void GetCBMSector(ref Sector _sector, int _sncLen, int _blkPos)
            {
                if (_blkPos + blkLen >= source.Length) return;
                var _gcr = Bit2Byte(source, _blkPos, blkLen);
                (var _dec, var _illegal) = Decode_CBM_GCR(_gcr);
                (bool checksum, byte actual, byte compare) = GetCBMChecksum(_dec, 1, 256, 257);
                _sector.Format = 0;
                if (_illegal > 30)
                {
                    (var _decv, var _checksumv, var _illegalv, var _expected, var _actual) = Decode_eVPL(CopyArray(_gcr, 3));
                    if (_illegalv < 10)
                    {
                        _sector.Format = 1;
                        checksum = _checksumv;
                        _dec = _decv;
                        _illegal = _illegalv;
                        actual = _actual;
                        compare = _expected;
                        if (!vpl) vpl = true;
                    }
                }
                if (!checksum && _sector.Format > 0) err.Add($"{_sector.ID}");
                byte[] gap = new byte[0];
                if (_sector.Format < 1 && !cartP)
                {
                    cartP = Find_VMax_Cart_CBM(_dec, track, _sector.ID).has_cart;
                }
                AddSector(ref _sector, _blkPos, _sncLen, _gcr, _dec, checksum, actual, compare, _illegal);
            }

            void GetVorpalLoader(ref Sector _sector, int _sncLen, int _blkPos)
            {
                if (_blkPos + vplLen >= source.Length) return;
                var _gcr = Bit2Byte(source, _blkPos, vplLen);
                var _dec = Decode_VorpalLoader(_gcr);
                _sector.Format = 3;
                AddSector(ref _sector, _blkPos, _sncLen, _gcr, _dec, true, 0x00, 0x00);
                _sector.Data.GetHash();
                _sector.Data.Checksum = VerifyHash(_sector.Data.Hash, _vHash);
                vpLDR = true;

            }

            void GetMicroProseSector(ref Sector _sector, int trackNum, int _pos)
            {
                if (_pos + blkLen >= source.Length) return;
                _sector.Format = 2;
                var _gcr = Bit2Byte(source, _pos, blkLen);
                var _dec = Decode_CBM_GCR(_gcr).decoded;
                (bool checksum, byte actual, byte compare) = GetCBMChecksum(_dec, 1, 256, 257);
                if (!checksum) err.Add($"{_sector.ID}");
                if (!manP) manP = Find_MPS_Manual(ArrayConcat(_sector.Header.Decoded, _dec), trackNum, _sector.ID).has_manual;
                AddSector(ref _sector, _pos, 0, _gcr, _dec, checksum, actual, compare);
                if (!mps) mps = true;
            }

            (bool, byte actual, byte expected) GetCBMChecksum(byte[] data, int start, int length, int parity)
            {
                if (data == null || data.Length < Math.Max(start + length, parity)) return (false, 0, 0);
                byte compare = data[parity];
                byte checksum = 0;
                for (int i = start; i < (start + length); i++) checksum ^= data[i];
                return (checksum == compare, checksum, compare);
            }

            void AddSector(ref Sector _sector, int position, int _snc, byte[] _gcr
                , byte[] _dec, bool _checksum, byte actual, byte compare, int _illegal = 0)
            {
                _sector.Data = new Sector.Info
                {
                    Pos = position,
                    GCR = _gcr,
                    Decoded = _dec,
                    Checksum = _checksum,
                    SyncLen = _snc,
                    Illegal = _illegal,
                    ChecksumActual = actual,
                    ChecksumExpected = compare
                };
            }

            void FindGaps(ref Disk_Track _T)
            {
                for (int i = 0; i < _T.Sector.Count; i++)
                {
                    byte[] hGap = new byte[0], sGap = new byte[0];
                    int hGlen = 0, sGlen = 0;
                    var _h = _T.Sector[i].Header;
                    var _s = _T.Sector[i].Data;
                    var _h1 = i + 1 < _T.Sector.Count ? _T.Sector[i + 1].Header : null;

                    if (_h.GCR.Length == 10)
                    {
                        var _hpos = _h.Pos + (_h.GCR.Length << 3);
                        hGlen = _s.Pos - _s.SyncLen - _hpos;
                        if (hGlen < 0) hGlen = 0;
                        else hGap = Bit2Byte(source, _hpos, hGlen);
                    }

                    if (_h1 != null)
                    {
                        var startPos = _s.Pos + (_s.GCR.Length << 3);
                        sGlen = _h1.Pos - _h1.SyncLen - 1 - startPos;

                        if (sGlen < 0) sGlen = 0;
                        else sGap = Bit2Byte(source, startPos, sGlen);
                    }
                    _s.TailGap = sGap;
                    _h.TailLength = hGlen;
                    _h.TailGap = hGap;
                    _s.TailLength = sGlen;
                }
            }

            void GetVorpalLoaderSectors(ref Disk_Track _T, int _first, uint[] sec_pos)
            {
                List<Sector> _vLoader = new List<Sector>();
                for (int i = 0; i < sec_pos.Length; i++)
                {
                    int snc = (int)sec_pos[i] & 0xffff;
                    int _pos = (int)(sec_pos[i] >> 16) & 0xffff;
                    Sector s = new Sector { ID = _first };
                    GetVorpalLoader(ref s, snc, _pos);
                    var h = _sectors.FirstOrDefault(x => x.ID == _first);
                    if (h != null)
                    {
                        s.Header = h.Header;
                        h.Format = s.Format;
                    }
                    _vLoader.Add(s);
                    _first++;
                }
                if (_vLoader.Count == 4)
                {
                    _T.Spec.VL.InjectAt = _first - _vLoader.Count;
                    _T.Spec.VL.Sector = _vLoader;
                }
            }

            void AddInfo(ref Disk_Track _T)
            {
                if (!batch)
                {
                    tSync = 0;
                    for (int i = 0; i < _T.Sector.Count; i++)
                    {
                        var _sector = _T.GetSector(i);
                        if (_sector.ErrorCode == 2) headers.Add($"Sector ({_sector.ID}) (Failed!) Header not found!");
                        else
                        {
                            var f = _sector.Format;
                            bool hasData = _sector.Header.GCR.Length > 4;
                            var sz = _sector.ID == 0 ? "*" : string.Empty;
                            var dec_hdr = hasData ? Hex_Val(_sector.Header.Decoded, 2, 4) : "not found!";
                            var hcsm = (f == 3 && _sector.Header.GCR.Length == 0) ? "N/A" : _sector.Header.Checksum ? "OK" : "Failed!";
                            var scsm = _sector.Data.GCR.Length > 0 ? _sector.Data.Checksum ? "OK" : "Failed!" : "No Block Data";
                            var hID = f != 3 ? $"Header-ID [ {dec_hdr} ] Header ({hcsm})" : "Vorpal Loader";
                            tSync += (_sector.Header.SyncLen + _sector.Data.SyncLen) / (_sector.Format == 2 ? 1 : 2);
                            headers.Add($"Sector ({_sector.ID}){sz} {hID} Sector ({scsm}) Track Position ({(f != 3 ? _sector.Header.Pos >> 3 : _sector.Data.Pos >> 3)})");
                        }
                    }
                    if (repeatSector.Length > 0) headers.Add(repeatSector);
                    headers.Add($"Track length ({(_T.End - _T.Start) >> 3}) Sectors ({sectors.Count}) Avg sync length ({tSync / sectors.Count} bits)");
                    _T.Info = headers.ToArray();
                }
            }

            bool VerifyHash(byte[] sector, HashSet<byte[]> hash)
            {
                foreach (var h in hash) if (MatchSeq(h, sector)) return true;
                return false;
            }
        }

        bool Check_Header(byte[] data)
        {
            byte checksum = 0;
            for (int i = 2; i < 6; i++) checksum ^= data[i];
            return checksum == data[1];
        }

        byte[] Adjust_Sync_CBM(byte[] data, int expected_sync, int minimum_sync, int exception, int Data_Start_Pos, int Data_End_Pos, int Sec_0, int Track_Len, int Track_Num, bool adjust = true)
        {
            if (Track_Num == Track_Num - 0) { }
            if (exception > expected_sync && expected_sync > minimum_sync)
            {
                byte[] tempp = Flip_Endian(data);
                BitArray s = new BitArray(Track_Len);
                BitArray z = new BitArray(tempp);
                var r = Sec_0;
                for (int i = 0; i < Track_Len; i++)
                {
                    s[i] = z[r];
                    r++;
                    if (r == Data_End_Pos) r = Data_Start_Pos;
                }

                BitArray d = new BitArray(s.Length + 4096);
                if (data.Length >= 5000)
                {
                    int sync_count = 0;
                    bool sync = false;
                    int dest_pos = 0;
                    for (int i = 0; i < s.Count; i++)
                    {
                        if (s[i])
                        {
                            sync_count++;
                            d[dest_pos] = true;
                            if (sync_count == minimum_sync && adjust) sync = true;
                        }
                        if (!s[i] && sync)
                        {
                            if (sync_count < expected_sync)
                            {
                                var m = expected_sync - sync_count;
                                for (int j = 0; j < m; j++) d[dest_pos + j] = true;
                                dest_pos += m;
                            }
                            if (expected_sync < sync_count && sync_count < exception)
                            {
                                dest_pos += (expected_sync - sync_count);
                                for (int j = dest_pos; j < dest_pos + (sync_count - expected_sync); j++) d[j] = false;
                            }
                        }
                        if (!s[i])
                        {
                            sync_count = 0;
                            sync = false;
                        }
                        dest_pos++;
                        if (dest_pos == d.Length) break;
                    }
                    int bcnt;
                    var a = Math.Abs(((dest_pos >> 3) << 3) - dest_pos);
                    if (a != 0) bcnt = (dest_pos >> 3) + 1;
                    else bcnt = dest_pos >> 3;
                    var y = (bcnt * 8) - dest_pos;
                    if (y != 0)
                    {
                        for (int i = 0; i < ((expected_sync + 8) + 1); i++)
                        {
                            d[dest_pos + (y - i)] = d[dest_pos - (y + i)];
                        }
                        Pad_Bits(dest_pos - (y + expected_sync + 8), (8 - y) + 1, d);
                    }
                    //return Rotate_Right(Bit2Byte(d, 0, bcnt << 3), 6);
                    return Bit2Byte(d, 0, bcnt << 3);
                }
            }
            return data;
        }

        (byte[] data, bool checksum) Decode_CBM_Sector(byte[] data, int sector, bool decode, BitArray source = null, int pos = 0)
        {
            if (source == null) source = new BitArray(Flip_Endian(data));

            const int sectorDataLength = 325 << 3;
            byte[] tmp = null;
            bool sectorFound = false;
            bool sectorMarker = false;
            bool sync = false;
            int syncCount = 0;
            /// --- next line commented out to skip first sector found if it resides at position 0.  Uncomment to check position 0 for sector
            CompareSectorMarker();
            while (pos < source.Length - 32)
            {
                if (source[pos])
                {
                    syncCount++;
                    //if (syncCount == 12) sync = true;
                    if (syncCount == 10) sync = true; // 12
                }
                else
                {
                    if (sync) sectorMarker = CompareSectorMarker();
                    if (pos + sectorDataLength < source.Length)
                    {
                        if (sync && sectorFound && !sectorMarker)
                        {
                            var (decodedSector, checksum) = DecodeSector();
                            if (!decode) return (decodedSector, checksum);

                            tmp = new byte[decodedSector.Length - 4];
                            Buffer.BlockCopy(decodedSector, 1, tmp, 0, tmp.Length);
                            return (tmp, checksum);
                        }
                    }

                    sync = false;
                    syncCount = 0;
                }
                pos++;
            }
            return (tmp ?? Array.Empty<byte>(), false);
            //return (tmp ?? new byte[0], false); /// <- For .Net 3.5 

            (byte[], bool) DecodeSector()
            {
                byte[] sectorBytes = Bit2Byte(source, pos, sectorDataLength);
                if (!decode) return (sectorBytes, false);
                byte[] decodedSector = Decode_CBM_GCR(sectorBytes).decoded;
                return (decodedSector, CBM_Checksum(decodedSector));
            }

            bool CompareSectorMarker(bool skip = true)
            {
                int checkLength = decode ? 5 : 10;
                byte[] header = Bit2Byte(source, pos, checkLength * 8);

                if (header[0] == 0x52)
                {
                    byte[] decodedHeader = Decode_CBM_GCR(header).decoded;
                    if (decodedHeader[3] > 0 && decodedHeader[3] < 43 && decodedHeader[2] == sector)
                    {
                        sectorFound = true;
                        return true;
                    }
                    //pos += sectorDataLength;
                }
                return false;
            }
        }

        bool CBM_Checksum(byte[] data)
        {
            if (data == null || data.Length < 258) return false;
            int checksum = 0;
            for (int i = 1; i < 257; i++) checksum ^= data[i];
            return checksum == data[257];
        }

        byte[] Replace_CBM_Sector(byte[] data, int sector, byte[] new_sector, byte[] padding = null, int pos = 0)
        {
            if (new_sector.Length == 256) new_sector = Build_Sector(new_sector);

            BitArray source = new BitArray(Flip_Endian(data));
            BitArray sec = new BitArray(Flip_Endian(new_sector));
            BitArray pad = padding != null ? new BitArray(Flip_Endian(padding)) : new BitArray(0);

            const int sectorDataLength = 325 << 3;
            bool sector_found = false;
            bool sync = false;
            bool sector_marker;// = false;
            int sync_count = 0;
            sector_marker = Compare();
            while (pos < source.Length - 32)
            {
                if (source[pos])
                {
                    sync_count++;
                    if (sync_count == 15)
                        sync = true;
                }
                else
                {
                    if (sync) sector_marker = Compare();
                    if (pos + sectorDataLength < source.Length)
                    {
                        if (sync && sector_found && !sector_marker)
                        {
                            ReplaceSector(pos, sec, pad, source);
                            return Bit2Byte(source);
                        }
                    }
                    sync = false;
                    sync_count = 0;
                }
                pos++;
            }

            return data;

            bool Compare()
            {
                const int checkLength = 5;
                byte[] d = Bit2Byte(source, pos, checkLength * 8);

                if (d[0] == 0x52)
                {
                    byte[] g = Decode_CBM_GCR(d).decoded;
                    if (g[3] > 0 && g[3] < 43 && g[2] == sector)
                    {
                        sector_found = true;
                        return true;
                    }
                    pos += sectorDataLength;
                }
                return false;
            }

            void ReplaceSector(int startPos, BitArray sectorBits, BitArray paddingBits, BitArray sourceBits)
            {
                for (int i = 0; i < sectorBits.Count; i++) sourceBits[startPos + i] = sectorBits[i];
                if (paddingBits.Length > 0)
                {
                    startPos += sectorBits.Length;
                    for (int i = 0; i < paddingBits.Count; i++) sourceBits[startPos + i] = paddingBits[i];
                }
            }
        }

        BitArray Replace_MPS_Sector(BitArray source, int sector, byte[] new_sector)
        {
            if (source == null) return null;
            if (new_sector == null || sector < 0) return source;
            //BitArray source = new BitArray(Flip_Endian(data));
            (var fsec, _, var pos) = Decode_MicroProse_Sector(source, sector, false);
            if (pos > -1 && fsec != null)
            {
                if (new_sector.Length == 256) new_sector = Build_MPS_Sector(Decode_CBM_GCR(fsec).decoded, new_sector);
                if (new_sector != null)
                {
                    BitArray nsec = new BitArray(Flip_Endian(new_sector));
                    for (int i = 0; i < nsec.Length; i++) source[pos + i] = nsec[i];
                }
            }
            return source;

            byte[] Build_MPS_Sector(byte[] old_sec, byte[] sect)
            {
                if (sect == null || old_sec == null) return null;
                byte checksum = 0;
                for (int i = 0; i < sect.Length; i++) checksum ^= sect[i];
                Buffer.BlockCopy(sect, 0, old_sec, 9, sect.Length);
                old_sec[265] = checksum;
                return Encode_CBM_GCR(old_sec);
            }
        }

        //byte[] Replace_MPS_Sector(byte[] data, int sector, byte[] new_sector)
        //{
        //    if (data == null) return null;
        //    if (new_sector == null || sector < 0) return data;
        //    BitArray source = new BitArray(Flip_Endian(data));
        //    (var fsec, _, var pos) = Decode_MicroProse_Sector(source, sector, false);
        //    if (pos > -1 && fsec != null)
        //    {
        //        if (new_sector.Length == 256) new_sector = Build_MPS_Sector(Decode_CBM_GCR(fsec).decoded, new_sector);
        //        if (new_sector != null)
        //        {
        //            BitArray nsec = new BitArray(Flip_Endian(new_sector));
        //            for (int i = 0; i < nsec.Length; i++) source[pos + i] = nsec[i];
        //            return Bit2Byte(source);
        //        }
        //    }
        //    return data;
        //
        //    byte[] Build_MPS_Sector(byte[] old_sec, byte[] sect)
        //    {
        //        if (sect == null || old_sec == null) return null;
        //        byte checksum = 0;
        //        for (int i = 0; i < sect.Length; i++) checksum ^= sect[i];
        //        Buffer.BlockCopy(sect, 0, old_sec, 9, sect.Length);
        //        old_sec[265] = checksum;
        //        return Encode_CBM_GCR(old_sec);
        //    }
        //}

        byte[] Build_Sector(byte[] sect, bool badChecksum = false)
        {
            if (sect == null) return null;
            int checksum = 0;
            for (int i = 0; i < sect.Length; i++) checksum ^= sect[i];
            if (badChecksum) checksum = Flip_Endian(new byte[] { (byte)checksum })[0];
            return Encode_CBM_GCR(ArrayConcat(new byte[] { 0x07 }, sect, new byte[] { (byte)checksum, 0x00, 0x00 }));
        }



        (bool found, int header_pos, int block_pos, byte[] ID, bool checksum) Find_Sector(BitArray source, int sector, int pos = -1, bool bit_pos = false)
        {
            if (pos < 0) pos = 0;
            byte[] dID;
            bool sector_found;
            bool cksm = false;
            int block_pos;// = 0;
            (sector_found, dID, cksm, block_pos) = Compare();
            if (!sector_found)
            {
                bool sync = false;
                int sync_count = 0;

                while (pos < source.Length - 32)
                {
                    if (source[pos])
                    {
                        sync_count++;
                        if (sync_count == 10) sync = true;
                    }
                    else
                    {
                        if (sync)
                        {
                            (sector_found, dID, cksm, block_pos) = Compare();
                            if (sector_found) break;
                        }
                        sync = false;
                        sync_count = 0;
                    }
                    pos++;
                }
            }
            if (bit_pos) return sector_found ? (true, pos, block_pos, dID, cksm) : (false, -1, -1, null, cksm);
            return sector_found ? (true, pos >> 3, block_pos >> 3, dID, cksm) : (false, -1, -1, null, cksm);

            (bool, byte[], bool, int) Compare()
            {
                int blk_pos = -1;
                int cl = 10;
                if (pos + (cl << 3) < source.Length)
                {
                    byte[] d = Bit2Byte(source, pos, cl << 3);
                    if (d[0] == 0x52)
                    {
                        byte[] g = Decode_CBM_GCR(d).decoded;
                        byte[] ID = new byte[2];
                        byte csm = 0x00;
                        for (int i = 2; i < 6; i++) csm ^= g[i];
                        cksm = (g[1] == csm);
                        Buffer.BlockCopy(g, 4, ID, 0, 2);
                        if (g[3] > 0 && g[3] < 43 && g[2] == sector)
                        {
                            try
                            {
                                int snc2 = 0;
                                for (int k = pos; k < pos + (80 << 3); k++)
                                {
                                    if (source[k]) snc2++;
                                    else
                                    {
                                        if (snc2 > 10 && Bit2Byte(source, k, 8)[0] == 0x55)
                                        {
                                            var blkdata = Decode_CBM_GCR(Bit2Byte(source, k, 5 << 3)).decoded;
                                            if (blkdata[0] == 0x07) blk_pos = k;
                                        }
                                        snc2 = 0;
                                    }
                                }
                            }
                            catch { }
                            return (true, ID, cksm, blk_pos);
                        }
                        //pos += (320 << 3);
                        pos += (1 << 3);
                    }
                }
                return (false, null, cksm, blk_pos);
            }
        }

        //void Get_Disk_Directory()
        string Get_Disk_Directory(byte[] t18 = null)
        {
            bool keepgoing = t18 == null;
            string ret = "Disk Directory ID : n/a";
            var buff = new MemoryStream();
            var wrt = new BinaryWriter(buff);
            List<string> d_files = new List<string>();
            List<string> d_sec = new List<string>();
            List<string> filename = new List<string>();
            int halftrack;
            int track;
            int blocksFree = 0;
            SelectionLength = 0;

            if (tracks <= 42)
            {
                halftrack = 17;
                track = halftrack + 1;
            }
            else
            {
                halftrack = 34;
                track = (halftrack / 2) + 1;
            }

            //if (NDS.cbm[halftrack] == 1)
            if (t18 == null && Disk.Source.Track[halftrack].Format == 1)
            {
                t18 = new byte[Disk.G64.Track[halftrack].Data.Length];
                Buffer.BlockCopy(Disk.G64.Track[halftrack].Data, 0, t18, 0, t18.Length);
            }

            List<string> list = new List<string>();
            byte[] nextSector = new byte[] { (byte)track, 0x00 };
            byte[] lastSector = new byte[2];
            int tnum = Convert.ToInt32(nextSector[0]);
            int snum = Convert.ToInt32(nextSector[1]);
            try
            {
                while ((tnum != 0 && tnum < 42) && !list.Any(x => x == Hex_Val(nextSector)))
                {
                    if (tnum != 18)
                    {
                        if (keepgoing)
                        {
                            t18 = new byte[Disk.G64.Track[halftrack].Data.Length];
                            Buffer.BlockCopy(Disk.G64.Track[halftrack].Data, 0, t18, 0, t18.Length);
                        }
                        else break;
                    }
                    list.Add(Hex_Val(nextSector));
                    if (snum < 22 && !(tnum == 18 && snum == 0)) d_sec.Add(Hex_Val(nextSector).Replace("-", ""));
                    Buffer.BlockCopy(nextSector, 0, lastSector, 0, 2);
                    byte[] temp = new byte[0];
                    try
                    {
                        //(temp, _) = Decode_CBM_Sector(NDG.Track_Data[halftrack], Convert.ToInt32(nextSector[1]), true);
                        (temp, _) = Decode_CBM_Sector(t18, Convert.ToInt32(nextSector[1]), true);
                        if (temp.Length > 0)
                        {
                            Buffer.BlockCopy(temp, 0, nextSector, 0, nextSector.Length);
                            tnum = Convert.ToInt32(nextSector[0]);
                            snum = Convert.ToInt32(nextSector[1]);

                            if (tracks <= 42) halftrack = tnum - 1;
                            else halftrack = (tnum - 1) * 2;
                            wrt.Write(temp);

                        }
                        else
                        {
                            ret = "Error processing directory!";
                            break;
                        }
                    }
                    catch { }

                }
            }
            catch { }

            if (buff.Length != 0)
            {
                try
                {
                    if (buff.Length < 257)
                    {
                        byte[] temp;
                        //(temp, _) = Decode_CBM_Sector(NDG.Track_Data[halftrack], 1, true);
                        (temp, _) = Decode_CBM_Sector(t18, 1, true);
                        wrt.Write(temp);
                    }
                }
                catch { }

                byte[] directory = buff.ToArray();

                if (directory.Length >= 256)
                {
                    for (int i = 0; i < 35; i++)
                    {
                        if (i != 17)
                            blocksFree += directory[4 + (i * 4)];
                    }

                    ret = $"0 \"";
                    SelectionLength = 0;
                    for (int i = 0; i < 23; i++)
                    {
                        if (directory[144 + i] != 0x00)
                        {
                            if (i != 16) ret += Encoding.ASCII.GetString(directory, 144 + i, 1).Replace('?', ' ');
                            else ret += "\"";
                            SelectionLength = ret.Length - 2;
                        }
                    }
                }

                if (directory.Length > 256)
                {
                    for (int i = 1; i < directory.Length / 256; i++)
                    {
                        byte[] file = new byte[32];
                        for (int j = 0; j < 8; j++)
                        {
                            Buffer.BlockCopy(directory, 256 * i + (j * 32), file, 0, file.Length);
                            if (file[2] != 0x00)
                            {
                                Disk.Directory.Entries++;
                                file[0] = 0x00; file[1] = 0x00;
                                d_files.Add(Hex_Val(file).Replace("-", ""));
                                string sz = Get_FileName(file);
                                filename.Add($"{sz}");
                                ret += $"\n{sz}";
                            }
                        }
                    }
                    ret += $"\n{blocksFree} BLOCKS FREE.";
                    if (keepgoing)
                    {
                        Disk.Directory.Entry = new byte[Disk.Directory.Entries][];
                        d_temp = new byte[Disk.Directory.Entries][];
                        Disk.Directory.Sectors = new byte[d_sec.Count][];
                        for (int i = 0; i < Disk.Directory.Entries; i++)
                        {
                            Disk.Directory.Entry[i] = Hex2Byte(d_files[i]);
                            d_temp[i] = Hex2Byte(d_files[i]);
                        }
                        for (int i = 0; i < d_sec.Count; i++)
                        {
                            Disk.Directory.Sectors[i] = Hex2Byte(d_sec[i]);
                        }
                        f_temp = filename.ToArray();
                        Disk.Directory.FileName = filename.ToArray();
                        Dir_Box.Items.Clear();
                        for (int k = 0; k < filename.Count; k++) Dir_Box.Items.Add(filename[k]);
                    }
                }
            }
            return ret;
        }

        void Set_Dir(string ret)
        {
            if (ret.Length > 0)
            {
                Dir_screen.Text = ret;
                Dir_screen.Select(2, SelectionLength);
                Dir_screen.SelectionBackColor = c64_text;
                Dir_screen.SelectionColor = C64_screen;
            }
        }

        void Create_Blank_Disk()
        {
            Invoke(new Action(() => Disable_Core_Controls(true)));
            if (BD_id.Text == "") BD_id.Text = "  ";
            var name = AsciiToPetscii(BD_name.Text, true);
            fname = BD_name.Text;
            tracks = Convert.ToInt32(BD_tracks.Value);
            sl.DataSource = null;
            out_size.DataSource = null;
            Data_Box.Clear();
            Track_Info.Items.Clear();
            Set_Arrays(tracks);
            Set_ListBox_Items(true, false);
            var id = AsciiToPetscii(BD_id.Text, true);
            var Disk_ID = new byte[] { id[1], id[0], 0x0f, 0x0f };
            var sync = FastArray.Init(5, 0xff);
            var dir_s0 = Encode_CBM_GCR(T18S0());
            var dir_s1 = Encode_CBM_GCR(T18S1());
            var blank = Encode_CBM_GCR(Create_Empty_Sector());

            for (int i = 0; i < Convert.ToInt32(BD_tracks.Value); i++)
            {
                var gap = SetSectorGap(sector_gap_length[i]);
                MemoryStream buffer = new MemoryStream();
                BinaryWriter write = new BinaryWriter(buffer);
                for (int j = 0; j < Available_Sectors[i]; j++)
                {
                    var block_header = Build_BlockHeader(i + 1, j, Disk_ID);
                    write.Write(ArrayConcat(sync, block_header, gap, sync, (i == 17 && j < 2) ? j == 1 ? dir_s1 : dir_s0 : blank, gap));
                }
                int rem = (int)(density[density_map[i]] - buffer.Length);
                if (rem > 0) write.Write(FastArray.Init(rem, cbm_gap));
                var nt = buffer.ToArray();
                Set_Dest_Arrays(nt, i);
                Disk.Source.Track[i].Data = new byte[8192];
                Buffer.BlockCopy(Disk.Adjusted.Track[i].Data, 0, Disk.Source.Track[i].Data, 0, 8192);
            }
            if (!DontThread)
            {
                if (Worker_Alt != null) Worker_Alt?.Abort();
                Worker_Alt = new Thread(new ThreadStart(() => Parse_Disk()));
                Worker_Alt.Start();
            }
            else Parse_Disk();

            void Parse_Disk()
            {
                Stopwatch pn = Parse_Nib_Data();
                Invoke(new Action(() =>
                {
                    Stopwatch po = Process_Nib_Data(true, false, false, false, true);
                    Set_Dir(Get_Disk_Directory());
                    //Set_BlockMap();
                    Set_ListBox_Items(false, false);
                    Set_Buttons_Active();
                    Batch_List_Box.Visible = false;
                    if (DB_timers.Checked) label2.Text = $"New Disk Time - Parse : {pn.Elapsed.TotalMilliseconds} Process: {po.Elapsed.TotalMilliseconds} Total : {pn.Elapsed.TotalMilliseconds + po.Elapsed.TotalMilliseconds}";
                }));
            }

            byte[] T18S0()
            {
                int chksum = 0;
                var title = new byte[27];
                var ds0 = new byte[] { 0x12, 0x01, 0x41, 0x00 };
                for (int i = 0; i < title.Length; i++)
                {
                    if (i < 18) if (i < name.Length) title[i] = name[i]; else title[i] = 0xa0;
                    else if (i - 18 < id.Length) title[i] = id[i - 18]; else title[i] = 0xa0;
                }
                var bam = Create_BAM();
                AllocBlock(bam, 17, 0, Set);
                AllocBlock(bam, 17, 1, Set);
                using (MemoryStream buff = new MemoryStream())
                using (BinaryWriter wrt = new BinaryWriter(buff))
                {
                    wrt.Write(ArrayConcat(new byte[] { 0x07 }, ds0, bam, title));
                    while (buff.Length < 256) wrt.Write((byte)0x00);
                    var s = new byte[260];
                    Buffer.BlockCopy(buff.ToArray(), 0, s, 0, (int)buff.Length);
                    for (int i = 1; i < 257; i++) chksum ^= s[i];
                    s[257] = (byte)chksum;
                    return s;
                }
            }

            byte[] T18S1()
            {
                int chksum = 0;
                using (MemoryStream buff = new MemoryStream())
                using (BinaryWriter wrt = new BinaryWriter(buff))
                {
                    wrt.Write(new byte[] { 0x07, 0x00, 0xff });
                    while (buff.Length < 260) wrt.Write((byte)0x00);
                    var t = buff.ToArray();
                    for (int i = 1; i < 257; i++) chksum ^= t[i];
                    t[257] = (byte)chksum;
                    return t;
                }
            }

            byte[] Create_BAM()
            {
                using (MemoryStream buff = new MemoryStream())
                using (BinaryWriter wrt = new BinaryWriter(buff))
                {
                    var bf = new byte[35];
                    var used_sectors = new byte[35][];
                    BitArray us = new BitArray(24);
                    for (int i = 0; i < 35; i++)
                    {
                        bf[i] = Available_Sectors[i];
                        used_sectors[i] = new byte[3];
                        for (int j = 0; j < Available_Sectors[i]; j++) us[j] = true;
                        used_sectors[i] = Flip_Endian(Bit2Byte(us));
                        wrt.Write(bf[i]);
                        wrt.Write(used_sectors[i]);
                    }
                    return buff.ToArray();
                }
            }
        }

        void AllocBlock(byte[] bam, int track, int sector, bool set)
        {
            if (track < 35 && sector < Available_Sectors[track])
            {
                int getbyte = (sector / 8) + 1;
                int getbit = sector % 8;
                int pos = (track << 2) + getbyte;
                if (BlockAllocStatus(bam, track, sector) != set)
                {
                    // Use the 'set' flag to either allocate (clear bit) or free (set bit)
                    bam[pos] = set ? SetBit(bam[pos], getbit) : ClearBit(bam[pos], getbit);

                    // Adjust the block count
                    bam[track << 2] = (byte)(bam[track << 2] + (set ? 1 : -1));
                }
            }
        }

        bool BlockAllocStatus(byte[] bam, int track, int sector)
        {
            if (bam != null && track < 35 && sector < Available_Sectors[track])
            {
                int getbyte = (sector >> 3) + 1;
                int getbit = sector % 8;
                int pos = (track << 2) + getbyte;
                return GetBitStatus(bam[pos], getbit);
            }
            return true;
        }

        byte[] GetBam()
        {
            byte[] data = Disk.Source.Track
                .FirstOrDefault(t => t.TrackNumber == 18 && t.Format == 1)
                ?.Sector.FirstOrDefault(s => s.ID == 0)
                ?.Data.Decoded ?? null;
            return data != null ? CopyArray(data, 5, 140) : null;
        }

        void UpdateBam(byte[] bam)
        {
            int dirtrack = tracks > 42 ? 34 : 17;
            if (Disk.Source.Track[dirtrack].Format == 1)
            {
                (byte[] data, _) = Decode_CBM_Sector(Disk.G64.Track[dirtrack].Data, 0, true);
                Buffer.BlockCopy(bam, 0, data, 4, bam.Length);
                byte[] temp = Replace_CBM_Sector(Disk.G64.Track[dirtrack].Data, 0, data);
                Set_Dest_Arrays(temp, dirtrack);
                Buffer.BlockCopy(Disk.Adjusted.Track[dirtrack].Data, 0, Disk.Source.Track[dirtrack].Data, 0, NIB_TRACK_LEN);
            }
        }

        void AddFileToDisk(byte[] prg, string filename, byte[] bam, byte[][] freesec)
        {
            int blocks = prg.Length % 254 == 0 ? prg.Length / 254 : prg.Length / 254 + 1;
            if (freesec != null)
            {
                blocks = blocks > freesec.Length ? freesec.Length : blocks;

                if (blocks <= freesec.Length)
                {
                    List<int> ttrks = new List<int>();
                    int prevTrack = -1;
                    int curtrack = -1;
                    byte[] temp = new byte[0];
                    for (int i = 0; i < blocks; i++)
                    {
                        curtrack = tracks > 42 ? (freesec[i][0]) << 1 : freesec[i][0];
                        if (prevTrack != curtrack)
                        {
                            if (!(prevTrack < 0)) Set_Dest_Arrays(temp, prevTrack);
                            temp = new byte[Disk.G64.Track[curtrack].Data.Length];
                            Buffer.BlockCopy(Disk.G64.Track[curtrack].Data, 0, temp, 0, temp.Length);
                            prevTrack = curtrack;
                        }
                        if (!ttrks.Contains(curtrack)) ttrks.Add(curtrack);
                        int track = freesec[i][0];
                        int cursec = freesec[i][1];

                        try
                        {
                            byte[] secData = FastArray.Init(256, 00);
                            int len = (i + 1) * 254 > prg.Length ? prg.Length - (i * 254) : 254;
                            Buffer.BlockCopy(prg, i * 254, secData, 2, len);
                            secData[0] = i == blocks - 1 ? (byte)0x00 : (byte)(freesec[i + 1][0] + 1);
                            secData[1] = i == blocks - 1 ? (byte)0xff : (byte)freesec[i + 1][1];
                            temp = Replace_CBM_Sector(temp, cursec, secData);
                            AllocBlock(bam, track, cursec, Set);
                        }
                        catch { }
                    }
                    Set_Dest_Arrays(temp, curtrack);
                    foreach (int a in ttrks)
                    {
                        Buffer.BlockCopy(Disk.Adjusted.Track[a].Data, 0, Disk.Source.Track[a].Data, 0, NIB_TRACK_LEN);
                    }
                    UpdateBam(bam);
                }
            }
        }

        void AddEntryToDirectory(byte[] newfile)
        {
            int ht = tracks > 42 ? 2 : 1;
            int dirtrack = 17 * ht;
            int atrack = 17;
            bool newsector = false;
            bool added = false;
            if (Disk.Source.Track[dirtrack].Format == 1)
            {
                int nexttrack = dirtrack;
                int nextsector = 1;
                int prevsector = 0;
                int i = nextsector;
                while (!newsector)
                {
                    int curtrack = nexttrack;
                    int cursector = nextsector;
                    (byte[] cursec, _) = Decode_CBM_Sector(Disk.G64.Track[curtrack].Data, nextsector, true);
                    if (cursec != null && cursec.Length == 256)
                    {
                        nexttrack = Convert.ToInt32(cursec[0] - 1);
                        nextsector = cursec[1];
                        if (nextsector == prevsector) newsector = true;
                        else prevsector = nextsector;
                        if (nexttrack == 0 || nexttrack > 35 || nextsector > Available_Sectors[atrack]) newsector = true;
                        else
                        {
                            nexttrack *= ht;
                            atrack = nexttrack + 1;
                        }
                        if (i > 0)
                        {
                            int pos = 2;
                            for (int j = 0; j < 8; j++)
                            {
                                int tpos = pos + (j * 32);
                                if (cursec[tpos] == 0x00 && !added)
                                {
                                    Buffer.BlockCopy(newfile, 0, cursec, tpos, 30);
                                    byte[] temp = Replace_CBM_Sector(Disk.G64.Track[curtrack].Data, cursector, cursec);
                                    Set_Dest_Arrays(temp, curtrack);
                                    Buffer.BlockCopy(Disk.Adjusted.Track[curtrack].Data, 0, Disk.Source.Track[curtrack].Data, 0, NIB_TRACK_LEN);
                                    added = true;
                                }
                                if (added) break;
                            }

                        }
                    }
                    if (added) break;
                    if (newsector)
                    {
                        int intlv = Sec_Interleave.SelectedIndex;
                        byte[] tbam = GetBam();
                        HashSet<int> processedSectors = new HashSet<int>();
                        int tsec = Available_Sectors[17];
                        int newsec = (cursector * sectorInterleave[intlv]) % tsec;
                        for (int k = 1; k < tsec; k++)
                        {
                            while (processedSectors.Contains(newsec))
                            {
                                newsec = (newsec + 1) % tsec; // Increment sec and wrap around if needed
                            }
                            if (BlockAllocStatus(tbam, 17, newsec))
                            {
                                cursec[0] = (byte)(18);
                                cursec[1] = (byte)(newsec);
                                byte[] stemp = Replace_CBM_Sector(Disk.G64.Track[dirtrack].Data, cursector, cursec);
                                byte[] nsector = FastArray.Init(256, 0x00);
                                nsector[1] = 0xff;
                                Buffer.BlockCopy(newfile, 0, nsector, 2, 30);
                                stemp = Replace_CBM_Sector(stemp, newsec, nsector);
                                Set_Dest_Arrays(stemp, dirtrack);
                                Buffer.BlockCopy(Disk.Adjusted.Track[dirtrack].Data, 0, Disk.Source.Track[dirtrack].Data, 0, NIB_TRACK_LEN);
                                AllocBlock(tbam, 17, newsec, Set);
                                UpdateBam(tbam);
                                added = true;
                            }
                            if (added) break;
                            processedSectors.Add(newsec);
                        }
                    }
                    i++;
                }
            }
        }

        List<byte[]> Get_Directory_Entries()
        {
            int ht = tracks > 42 ? 2 : 1;
            int dirtrack = 17 * ht;
            int atrack = 17;
            bool stop = false;
            List<byte[]> entries = new List<byte[]>();
            if (Disk.Source.Track[dirtrack].Format == 1)
            {
                int nexttrack = dirtrack;
                int nextsector = 0;
                int prevsector = 0;
                int i = 0;
                while (!stop)
                {
                    (byte[] cursec, _) = Decode_CBM_Sector(Disk.G64.Track[nexttrack].Data, nextsector, true);
                    if (cursec != null && cursec.Length == 256)
                    {
                        nexttrack = Convert.ToInt32(cursec[0] - 1);
                        nextsector = cursec[1];
                        if (nextsector == prevsector) stop = true;
                        else prevsector = nextsector;
                        if (nexttrack == 0 || nexttrack > 35 || nextsector > Available_Sectors[atrack]) stop = true;
                        else
                        {
                            nexttrack *= ht;
                            atrack = nexttrack + 1;
                        }
                        if (i > 0)
                        {
                            int pos = 2;
                            for (int j = 0; j < 8; j++)
                            {
                                int tpos = pos + (j * 32);
                                if (cursec[tpos] != 0x00)
                                {
                                    byte[] entry = new byte[30];
                                    Buffer.BlockCopy(cursec, tpos, entry, 0, 30);
                                    entries.Add(entry);
                                }
                            }

                        }
                    }
                    if (stop) break;
                    i++;
                }
                if (entries.Count > 0)
                {
                    return entries;
                }
            }
            return entries;
        }

        (byte[], byte[][]) GetAvailableSectors(bool extra_sectors = false)
        {
            byte[] bam = GetBam();
            int ht = tracks > 42 ? 2 : 1;
            int intlv = Sec_Interleave.SelectedIndex;
            bool rev = false;
            int strk = rev ? 18 : 16;
            int lastSector = 0; // Tracks the last sector processed
            //int strk = 16;
            List<byte[]> available = new List<byte[]>();
            if (bam != null)
            {
                for (int i = 0; i < 35; i++)
                {
                    if (Disk.Source.Track[i * ht].Format == 1)
                    {
                        HashSet<int> processedSectors = new HashSet<int>();
                        int max = Available_Sectors[strk];
                        int sec = (lastSector + sectorInterleave[intlv]) % max; // Start from the interleave of the last sector

                        for (int j = 0; j < Available_Sectors[strk]; j++)
                        {
                            while (processedSectors.Contains(sec))
                            {
                                sec = (sec + 1) % max; // Increment sec and wrap around if needed
                            }

                            if (BlockAllocStatus(bam, strk, sec))
                            {
                                available.Add(new byte[] { (byte)strk, (byte)sec });
                            }

                            processedSectors.Add(sec);
                            lastSector = (sec - 1) % max; // Update the last processed sector
                            sec = (sec + sectorInterleave[intlv]) % max; // Continue with the interleave for the next sector
                        }
                    }

                    strk += rev ? 1 : -1;

                    if (strk < 0)
                    {
                        strk = 18;
                        rev = true;
                    }

                    if (strk > 34 && extra_sectors)
                    {
                        strk = 17;
                    }
                }
            }
            if (available.Count > 0)
            {
                byte[][] freeSectors = new byte[available.Count][];
                for (int i = 0; i < available.Count; i++)
                {
                    freeSectors[i] = new byte[2];
                    freeSectors[i] = available[i];
                }
                return (bam, freeSectors);
            }
            return (bam, new byte[0][]);
        }

        void ProcessNewFiletoImage(string[] files)
        {
            bool fastload = true;
            DontThread = true;
            if (tracks == 0) RunBusy(Create_Blank_Disk);
            DontThread = false;
            bool refresh = false;
            foreach (string file in files)
            {
                byte[] data = File.ReadAllBytes(file);
                string filename = Path.GetFileName(file);
                long length = new FileInfo(file).Length;
                (byte[] bam, byte[][] freesec) = GetAvailableSectors(true);
                List<string> frsec = new List<string>();
                for (int i = 0; i < freesec.Length; i++)
                {
                    frsec.Add($"{Convert.ToInt32(freesec[i][0])}, {Convert.ToInt32(freesec[i][1])}");
                }
                //File.WriteAllLines($@"c:\test\bamtest", frsec.ToArray());
                length = length % 254 == 0 ? length / 254 : length / 254 + 1;
                if (length <= freesec.Length)
                {
                    List<byte[]> entries = Get_Directory_Entries();
                    if (entries == null || entries.Count < 144)
                    {
                        List<string> entNames = new List<string>();
                        foreach (var entry in entries)
                        {
                            entNames.Add(ExtractFileName(entry));
                        }
                        int blocks = data.Length % 254 == 0 ? data.Length / 254 : data.Length / 254 + 1;
                        byte[] newfile = CreateFileEntry(filename.ToUpper(), blocks, freesec[0][0] + 1, freesec[0][1]);
                        string newfilename = ExtractFileName(newfile);

                        if (!entNames.Contains(newfilename) && newfile != null)
                        {
                            if (fastload)
                            {
                                AddFastLoad(newfilename);
                                (bam, freesec) = GetAvailableSectors(true);
                                newfile = CreateFileEntry(filename.ToUpper(), blocks, freesec[0][0] + 1, freesec[0][1]);
                            }
                            AddFileToDisk(data, filename, bam, freesec);
                            AddEntryToDirectory(newfile);

                            refresh = true;
                        }
                        else
                        {
                            //MessageBox.Show($"File already exists in directory");
                            using (Message_Center center = new Message_Center(this)) // center message box
                            {
                                string s = $"File {newfilename} already exists in directory";
                                string t = "Error!";
                                MessageBox.Show(s, t, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    else
                    {
                        if (entries.Count >= 144)
                        {
                            using (Message_Center center = new Message_Center(this)) // center message box
                            {
                                string t = "Error!";
                                string s = $"Directory is FULL!";
                                MessageBox.Show(s, t, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            break;
                        }
                    }

                    void AddFastLoad(string newfilename)
                    {
                        byte[] fl = new byte[fastloader.Length];
                        fastloader.CopyTo(fl, 0);
                        for (int i = 0; i < newfilename.Length; i++) fl[fldOffset + i] = (byte)newfilename[i];
                        fl[fldOffset + 16] = (byte)(newfilename.Length);
                        string flname = $"BOOT.{newfilename}";
                        if (newfilename.Length > 16) flname = flname.Substring(0, 16);
                        byte[] fld = CreateFileEntry(flname, 5, freesec[0][0] + 1, freesec[0][1]);
                        AddFileToDisk(fl, flname, bam, freesec);
                        AddEntryToDirectory(fld);
                    }
                }
                else
                {
                    using (Message_Center center = new Message_Center(this)) // center message box
                    {
                        string s = $"Not enough free space\nBlocks needed {length}\nBlocks free {freesec.Length}";
                        string t = "Error!";
                        MessageBox.Show(s, t, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            if (refresh)
            {
                Clear_Out_Items();
                Process_Nib_Data(true, false, false, true);
                Default_Dir_Screen();
                Set_Dir(Get_Disk_Directory());
                Set_BlockMap();
                linkLabel1.Visible = false;
                Save_Disk.Visible = true;
                Source.Visible = Output.Visible = true;
                label1.Text = $"{fname}{fext}";
                M_render.Enabled = true;
                Adv_ctrl.Enabled = true;
                Blk_pan.Enabled = true;
                Disable_Core_Controls(false);
            }
        }

        string ExtractFileName(byte[] file)
        {
            string fName = "";
            for (int k = 3; k < 19; k++)
            {
                if (file[k] != 0xa0)
                {
                    if (file[k] != 0x00) fName += Encoding.ASCII.GetString(file, k, 1);
                    else fName += "@";
                }
            }
            return fName;
        }

        (byte[] data, bool checksum, int position) Decode_MicroProse_Sector(BitArray source, int sect, bool decode = true)
        {
            if (source == null) return (null, false, -1);// source = new BitArray(Flip_Endian(data));
            int pos = 0;
            int mps = 335 << 3;
            int cbm = 325 << 3;
            int blk_snc = 80 << 3;
            const int sectorDataLength = 300 << 3;

            bool sync = false;
            int syncCount = 0;
            /// --- next line commented out to skip first sector found if it resides at position 0.  Uncomment to check position 0 for sector
            if (CompareSectorMarker()) return Decode();

            while (pos < source.Length - 32)
            {
                if (source[pos])
                {
                    syncCount++;
                    if (syncCount == 10) sync = true;
                }
                else
                {
                    if (sync && CompareSectorMarker()) return Decode();
                    sync = false;
                    syncCount = 0;
                }
                pos++;
            }
            return (null, false, -1);

            (byte[], bool, int) Decode()
            {
                if (pos + mps >= source.Length) return (null, false, -1);
                byte[] dec = Bit2Byte(source, pos, mps);

                // Determine if it's a CBM sector by detecting multiple consecutive 0xFF bytes
                (bool isCBMSector, int location) = Check_BlockSync_BitLevel();

                if (!isCBMSector)
                {
                    byte[] decoded = Decode_CBM_GCR(dec).decoded;
                    if (decoded != null && decoded.Length == 268)
                    {
                        int checksum = 0;
                        for (int i = 9; i < 266; i++) checksum ^= decoded[i];
                        return (decode ? CopyFrom(decoded, 9, 256) : dec, checksum == decoded[266], pos);
                    }
                }
                else
                {
                    // Look for standard CBM sector data since sync was found shortly after the header
                    if (pos + location + (cbm) < source.Length)
                    {
                        byte[] decoded = Decode_CBM_GCR(Bit2Byte(source, pos + location, cbm)).decoded;
                        if (decoded != null && decoded.Length == 260 && decoded[0] == 0x07)
                        {
                            int checksum = 0;
                            for (int i = 1; i < 257; i++) checksum ^= decoded[i];
                            return (decode ? CopyFrom(decoded, 1, 256) : dec, checksum == decoded[257], pos + location);
                        }
                    }

                }
                return (null, false, -1);
            }

            (bool, int) Check_BlockSync_BitLevel()
            {
                if (pos + (blk_snc) < source.Length)
                {
                    int tsnc = 0;
                    int cpos = pos + (80 << 3);
                    int location = 0;
                    while (pos + location < cpos)
                    {
                        if (source[pos + location]) tsnc++;
                        else
                        {
                            if (tsnc > 24)
                            {
                                if (Bit2Byte(source, pos + location, 8)[0] == 0x55 && pos + location + cbm < source.Length) return (true, location);
                            }
                            tsnc = 0;
                        }
                        location++;
                    }
                }
                return (false, -1);
            }

            bool CompareSectorMarker()
            {
                int checkLength = 10;
                byte[] header = Bit2Byte(source, pos, checkLength << 3);

                if (header[0] == 0x52)
                {
                    byte[] decodedHeader = Decode_CBM_GCR(header).decoded;
                    if (decodedHeader[3] > 0 && decodedHeader[3] < 42 && decodedHeader[2] == sect)
                    {
                        return true;
                    }
                    pos += sectorDataLength;
                }
                return false;
            }
        }

        void Repair_CBM_Checksums()
        {
            int errors; // = 0;
            ErrorList = new ConcurrentBag<string>();
            ScanForErrors();
            if (ErrorList.Count > 0)
            {
                errors = ErrorList.Count;
                Attempt_Repair();
            }
            else
            {
                var t = "Image is Clean!";
                var s = "No errors found!";
                using (Message_Center center = new Message_Center(this)) // center message box
                {
                    DialogResult result = MessageBox.Show(s, t, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            void ScanForErrors()
            {
                bool head_cksm; // = false;
                bool sec_cksm; // = false;
                bool found; // = false;
                int pos; // = 0
                for (int track = 0; track < tracks; track++)
                {
                    if (Disk.Source.Track[track].Format == 1)
                    {
                        var source = new BitArray(Flip_Endian(Disk.G64.Track[track].Data));
                        int tk = tracks > 42 ? (track >> 1) + 1 : track + 1;
                        //int avail = NDS.sectors[track] > Available_Sectors[tk] ? NDS.sectors[track] : Available_Sectors[tk];
                        int avail = Disk.Source.Track[track].Sectors != Available_Sectors[tk]
                            ? Math.Max(Disk.Source.Track[track].Sectors, Available_Sectors[tk]) : Available_Sectors[tk];
                        if (Disk.Source.Track.Any(x => x.Format == 5) && tk == 18) avail = 13;
                        for (int j = 0; j < avail; j++)
                        {
                            (found, pos, _, _, head_cksm) = Find_Sector(source, j, 0, true);
                            if (found)
                            {
                                (sec_cksm) = Decode_CBM_Sector(null, j, true, source, pos).checksum;
                                if (!head_cksm || !sec_cksm) ErrorList.Add($"Checksum failed on track {tk} sector {j}");
                            }
                        }
                    }
                }
            }

            void Attempt_Repair()
            {
                //bool fix = false;
                List<string> list = new List<string>(ErrorList);
                var s = Sort_Errors(list);
                s += "\n Would you like to (attempt) repairing?";

                using (Message_Center center = new Message_Center(this)) // center message box
                {
                    var t = "Errors found!";
                    DialogResult result = MessageBox.Show(s, t, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        //fix = true;
                        Fix_Errors();
                    }
                }
            }
        }
    }
}