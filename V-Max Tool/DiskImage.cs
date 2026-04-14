using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace V_Max_Tool
{
    //int[] positions = Disk.{Source/Adjusted/G64}.track[x].GetSectorValues(s => s.Data.Pos);
    //int[] headers = Disk.{Source/Adjusted/G64}.track[x].GetSectorValues(s => s.Header.Pos);
    //int[] formats = Disk.{Source/Adjusted/G64}.track[x].GetSectorValues(s => s.Format);
    //bool[] dataCksm = Disk.{Source/Adjusted/G64}.track[x].GetSectorBools(s => s.Data.Checksum);
    //bool[] headCksm = Disk.{Source/Adjusted/G64}.track[x].GetSectorBools(s => s.Header.Checksum);

    public class Disk_Track
    {
        public int Format { get; set; } = 0;
        public double TrackNumber { get; set; }
        public int CBMTrack { get; set; } = 0;
        public int Start { get; set; } = 0;
        public int End { get; set; } = 0;
        public int Length { get; set; } = 0;
        public int TotalSync { get; set; } = 0;
        public int SectorZero { get; set; } = 0;
        public int GapSector { get; set; } = 0;
        public bool Adjust { get; set; } = true;
        public byte[] Data { get; set; } = new byte[0];
        public byte[] TrackID { get; set; } = new byte[0];
        public string[] Info { get; set; } = new string[0];
        public int Sectors { get; set; } = 0;
        public int GLength { get; set; } = 0;   // Used when importing G64 to set 'Length'
        public int Density { get; set; } = 0;
        public BitArray Bits { get; set; } = null;
        public List<Sector> Sector = new List<Sector>();
        public int[] IDList => Sector != null ? Sector.Where(x => x != null).Select(x => x.ID).ToArray() : new int[0];

        // Various Protection Modifiers/Info
        public bool FatTrack { get; set; } = false;
        public int BitShift { get; set; } = 0;
        public ProtectionSpecifics Spec = new ProtectionSpecifics();

        public Disk_Track(double trackNumber = 0)
        {
            TrackNumber = trackNumber;
        }

        public void SetBits()
        {
            Bits = new BitArray(Form1.Flip_Endian(Data));
        }

        public void SetData(int start = 0, int length = -1)
        {
            Data = Form1.Bit2Byte(Bits, start, length);
        }

        public void ClearBits()
        {
            Bits = null;
        }

        public int[] GetIntValues(Func<Sector, int> selector)
        {
            return Sector.Select(selector).ToArray();
        }

        public bool[] GetBoolValues(Func<Sector, bool> selector)
        {
            return Sector.Select(selector).ToArray();
        }

        public Sector GetSector(int _ID = 0)
        {
            bool _vl = Spec.VL.IDList.Length == 4;
            var sector = _vl && Spec.VL.IDList.Any(x => x == _ID)
                ? Spec.VL.Sector.FirstOrDefault(x => x.ID == _ID)
                : Sector.FirstOrDefault(x => x.ID == _ID);
            return sector;
        }

    }

    public class Sector
    {
        // int Format
        // CBM Sectors   : 0 = Standard, 1 = Early Vorpal, 2 = Microprose, 3 = Vorpal Loader
        /// *** Not implemented yet! ***
        // V-Max Sectors : 0 = older GCR (weak-bits), 1 = newer GCR
        // RapidLok Sectors : 0 = Version 1, 1 = Version 2-7
        /// ----------------------------
        public int Format { get; set; } = 0; // 0
        public int ID { get; set; } = -1;
        public int ErrorCode { get; set; } = 0;
        public Info Header = new Info();
        public Info Data = new Info();

        public class Info
        {
            public int Pos { get; set; } = -1;
            public int SyncLen { get; set; } = 0;
            public byte[] GCR { get; set; } = new byte[0];
            public byte[] Decoded { get; set; } = new byte[0];
            public byte[] TailGap { get; set; } = new byte[0];
            public bool Checksum { get; set; } = false;
            public byte ChecksumExpected { get; set; } = 0;  // what it should be
            public byte ChecksumActual { get; set; } = 0;    // what was calculated
            public int Illegal { get; set; } = 0;
            public int SyncPos => Pos - SyncLen;
            public int TailLength { get; set; } = 0;
            public byte[] Hash { get; set; } = new byte[0];

            public void GetHash()
            {
                if (Decoded == null || Decoded.Length == 0) Hash = new byte[0];
                else using (var sha256 = SHA256.Create()) Hash = sha256.ComputeHash(Decoded);
            }
        }
    }

    public class ProtectionSpecifics
    {
        public PirateSlayer Slayer = new PirateSlayer();
        public VMAX VMax = new VMAX();
        public RapidLok RL = new RapidLok();
        public VorpalLoader VL = new VorpalLoader();

        public class VMAX
        {
            public Version2 V2 = new Version2();
            public Version3 V3 = new Version3();

            public class Version2
            {
                public byte HeaderStart { get; set; } = 0;
                public byte HeaderEnd { get; set; } = 0;
                public byte Length { get; set; } = 0;
                public byte Version { get; set; } = 0;
                public byte MultiSync { get; set; } = 0;
                public byte GapSector { get; set; } = 0;
                public int Sectors { get; set; } = 0;

                public byte[] GetV2Info()
                {
                    return new byte[]
                    {
                        HeaderStart,    // Header start byte ($64, $4E)
                        HeaderEnd,      // Header end byte ($46, $4E)
                        Length,         // Number of sector ID pairs
                        Version,        // Same as _Sector.Format (0 or 1)
                        MultiSync,      // each sector has sync or only a couple
                        GapSector       // track gap before this sector
                    };
                }

                public void SetV2Info(byte[] info)
                {
                    if (info.Length != 6) return;
                    HeaderStart = info[0];
                    HeaderEnd = info[1];
                    Length = info[2];
                    Version = info[3];
                    MultiSync = info[4];
                    GapSector = info[5];
                }
            }

            public class Version3
            {
                public byte[] SectorCount { get; set; } = new byte[0]; // Track 18's list of v-max sectors for each track
                public int HeaderLength { get; set; } = 0;
            }
        }

        public class PirateSlayer
        {
            public byte[] Key { get; set; } = new byte[0];
            public int Version { get; set; } = 0;
        }

        public class RapidLok
        {
            public byte[] Key { get; set; } = new byte[0];
            public bool GCRVersion { get; set; } = false;  // False = RL v1, True = RL v2-7
            public int SpecialSector { get; set; } = 0;
        }

        public class VorpalLoader
        {
            public int InjectAt { get; set; } = -1; // Loader injected at this sector position
            public int Sectors => Sector != null ? Sector.Count : -1;
            public int[] IDList => Sector != null ? Sector.Where(x => x != null).Select(x => x.ID).ToArray() : new int[0];
            public List<Sector> Sector { get; set; }
        }
    }

    public class ImportedDisk
    {
        public string FileName { get; set; } = string.Empty;
        public Image_Source Source { get; set; }
        public Image_Adjusted Adjusted { get; set; }
        public Image_G64 G64 { get; set; }
        public Image_Backup Original { get; set; }
        public DiskDir Directory { get; set; }
        public int Tracks { get; set; } = 0;
        public bool Cart_Protection { get; set; } = false;
        public bool External_Protection { get; set; } = false;
        public string ProtectionType { get; set; } = string.Empty;
        public byte[] DiskID { get; set; } = new byte[] { 0x30, 0x30, 0x0f, 0x0f };
        public byte[] Loader { get; set; } = new byte[0];

        public ImportedDisk(int len = 0)
        {
            Source = new Image_Source(len);
            Adjusted = new Image_Adjusted(len);
            G64 = new Image_G64(len);
            Original = new Image_Backup(len);
            Tracks = len;
            Directory = new DiskDir(0);

            bool halfTracks = len > 42;
            for (int i = 0; i < len; i++)
            {
                double num = 1 + ((halfTracks ? 0.5 : 1.0) * i);
                Source.Track[i] = new Disk_Track(num);
                Adjusted.Track[i] = new Disk_Track(num);
                G64.Track[i] = new Disk_Track(num);
            }
        }

        public int[] GetFormats()
        {
            List<int> formats = new List<int>();
            foreach (var fmt in Source.Track) formats.Add(fmt.Format);
            return formats.ToArray();
        }

        public byte[] GetSectorData(double trackNum, int sectorID, bool decoded = false)
        {
            var sector = Source.Track
                .FirstOrDefault(t => t?.TrackNumber == trackNum)
                ?.Sector.FirstOrDefault(s => s.ID == sectorID);
            return (decoded ? sector?.Data.Decoded : sector?.Data.GCR) ?? new byte[0];
        }

        public void SetErrorCodes(bool source = true)
        {
            List<string> l = new List<string>();
            List<string> g = new List<string>();
            var cbmSectors = (source ? Source.Track : G64.Track)
                .SelectMany(t => t.Sector
                .Select(s => (t, s)));

            foreach (var (_t, _s) in cbmSectors)
            {
                try
                {
                    int errCode = 1;
                    if (_s.Format >= 0)
                    {
                        int expLen = _s.Format == 3 ? 256 : _s.Format == 1 ? 240 : 260;
                        if (_s.Header.GCR == null || _s.Header.GCR.Length < 10) errCode = 2;
                        else if (_s.Header.Decoded == null || _s.Header.Decoded.Length == 0) errCode = 2;
                        else if (!_s.Header.Checksum) errCode = 9;
                        else if (_t.TrackID != null && !ID(DiskID, _t.TrackID)) errCode = 11;
                        else if (_s.Data.Decoded == null || _s.Data.Decoded.Length < expLen) errCode = 4;
                        else if (!_s.Data.Checksum) errCode = 5;
                        else if (_s.Data.GCR == null || _s.Data.GCR.Length < 325) errCode = 6;
                        _s.ErrorCode = errCode;
                        if (Form1.debug)
                        {
                            g.Add($"t {_t.TrackNumber} s {_s.ID} header gap len ({_s.Header.TailLength}) {Form1.Hex_Val(_s.Header.TailGap)} sector gap len  ({_s.Data.TailLength}) {Form1.Hex_Val(_s.Data.TailGap)} {_s.Data.Decoded.Length} cksm {Form1.Hex_Val(new byte[] { _s.Header.ChecksumActual, _s.Header.ChecksumExpected, _s.Data.ChecksumActual, _s.Data.ChecksumExpected })}");
                            l.Add($"track {_t.TrackNumber} sector {_s.ID} Errorcode : {Form1.c1541error[_s.ErrorCode]} - Format {Form1.CBM_Fmt[_s.Format]} {_s.Data.Decoded.Length} {Form1.Hex_Val(DiskID)} {Form1.Hex_Val(_t.TrackID)}");
                        }
                    }
                    else Console.WriteLine($"Track {_t.TrackNumber}, Sector {_s.ID}, Unknown format : {_s.Format}");
                }
                catch (Exception e) { Console.WriteLine($"{e.Message}"); }
            }
            if (Form1.debug)
            {
                if (l.Count > 0) File.WriteAllLines($@"c:\test\errorcodes_{source}.txt", l.ToArray());
                if (g.Count > 0) File.WriteAllLines($@"c:\test\gaps_{source}.txt", g.ToArray());
            }

            bool ID(byte[] d, byte[] t)
            {
                if (d.Length < 2 || t.Length < 2) return false;
                return d[0] == t[0] && d[1] == t[1];
            }
        }

        public class Image_Source  // Global variables for Nib file source data
        {
            public Disk_Track[] Track { get; set; }

            public Image_Source(int len = 0)
            {
                Track = new Disk_Track[len];
            }
        }

        public class Image_Adjusted  // Global variables for adjusted-sync arrays
        {
            public Disk_Track[] Track { get; set; }

            public Image_Adjusted(int len = 0)
            {
                Track = new Disk_Track[len];
            }
        }

        public class Image_G64  // Global variables for G64 array data
        {
            public Disk_Track[] Track { get; set; }
            public bool LoaderRotated { get; set; } = false;
            public byte[] NewHeader { get; set; } = new byte[0];

            public Image_G64(int len = 0)
            {
                Track = new Disk_Track[len];
            }
        }

        public class Image_Backup  // Global variable for retaining original track data
        {
            public byte[] LoaderG64 { get; set; }
            public byte[] LoaderAdjusted { get; set; }
            public byte[][] TrackData { get; set; }

            public Image_Backup(int len = 0)
            {
                LoaderG64 = new byte[0];
                LoaderAdjusted = new byte[0];
                TrackData = new byte[len][];
            }
        }

        public class DiskDir
        {
            public int Entries { get; set; }
            public byte[][] Sectors { get; set; }
            public byte[][] Entry { get; set; }
            public string[] FileName { get; set; }
            public DiskDir(int len)
            {
                Entries = len;
                Sectors = new byte[len][];
                Entry = new byte[len][];
                FileName = new string[len];
            }

            public void Reset()
            {
                Entries = 0;
                Sectors = new byte[0][];
                Entry = new byte[0][];
                FileName = new string[0];
            }
        }
    }
}
