using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ReMaster_Utility
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
        public BitArray Bits { get; set; } = null;
        public List<Disk_Sector> Sector = new List<Disk_Sector>();
        public ProtectionSpecifics Spec = new ProtectionSpecifics();

        public Disk_Track(double trackNumber = 0)
        {
            TrackNumber = trackNumber;
        }

        public int[] GetIntValues(Func<Disk_Sector, int> selector)
        {
            return Sector.Select(selector).ToArray();
        }

        public bool[] GetBoolValues(Func<Disk_Sector, bool> selector)
        {
            return Sector.Select(selector).ToArray();
        }
    }

    public class Disk_Sector
    {
        /// *** Not implemented yet! ***
        // int Format
        // CBM Sectors   : 0 = Standard, 1 = Early Vorpal, 2 = Microprose
        // V-Max Sectors : 0 = older GCR (weak-bits), 1 = newer GCR
        // RapidLok Sectors : 0 = Version 1, 1 = Version 2-7
        /// ----------------------------
        public int Format { get; set; } = 0;
        public int ID { get; set; } = -1;
        public Sector_HeaderData Header = new Sector_HeaderData();
        public Sector_BlockData Data = new Sector_BlockData();
    }

    public class Sector_HeaderData
    {
        public int Pos { get; set; } = -1;
        public int SyncLen { get; set; } = 0;
        public byte[] GCR { get; set; } = new byte[0];
        public byte[] Decoded { get; set; } = new byte[0];
        public bool Checksum { get; set; } = false;
        public int SyncPos => Pos - SyncLen;
    }

    public class Sector_BlockData
    {
        public int Pos { get; set; } = -1;
        public int SyncLen { get; set; } = 0;
        public byte[] GCR { get; set; } = new byte[0];
        public byte[] Decoded { get; set; } = new byte[0];
        public bool Checksum { get; set; } = false;
        public int SyncPos => Pos - SyncLen;
    }

    public class ProtectionSpecifics
    {
        public bool FatTrack { get; set; } = false;
        public int PreDeterminedLength {  get; set; } = 0;
        public PirateSlayer Slayer = new PirateSlayer();
        public VMAX VMax = new VMAX();
        public RapidLok RL = new RapidLok();

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
        }
    }

    public class ImportedDisk
    {
        public Image_Source Source { get; set; }
        public Image_Adjusted Adjusted { get; set; }
        public Image_G64 G64 { get; set; }
        public Image_Backup Original { get; set; }
        public DiskDir Directory { get; set; }
        public int Tracks { get; set; } = 0;
        public bool Cart_Protection { get; set; } = false;
        public bool External_Protection { get; set; } = false;
        public string ProtectionType { get; set; } = string.Empty;
        public byte[] DiskID { get; set; } = new byte[0];
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
