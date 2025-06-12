using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace V_Max_Tool
{
    public static class TEMP
    {
        public static string path = $@"{Path.GetTempPath()}\remaster\".Replace(@"\\", @"\");
        public static string dll = "cpp_extf.dll";
        public static string Nibtools = "nibpath.txt";
        public static string recent = "recent.files";
        public static string settings = $"{path}setting.txt";
        public static string dbPath = $@"c:\test\img.db";

        public static string exedir = AssemblyDirectory;

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return $@"{Path.GetDirectoryName(path)}\".Replace(@"\\", @"\");
            }
        }
        //public static readonly string path = $@"{Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)}\cpp_extf.dll".Replace(@"\\", @"\");
    }

    public static class NDS  // Global variables for Nib file source data
    {
        public static byte[][] Track_Data = new byte[0][];
        public static int[] Track_Length = new int[0];
        public static int[] Sector_Zero = new int[0];
        public static int[] D_Start = new int[0];
        public static int[] D_End = new int[0];
        public static int[] cbm = new int[0];
        public static int[] sectors = new int[0];
        public static int[] Header_Len = new int[0];
        public static int[][] cbm_sector = new int[0][];
        public static byte[][] v2info = new byte[0][];
        public static byte[] Loader = new byte[0];
        public static int[] Total_Sync = new int[0];
        public static byte[][] Disk_ID = new byte[0][];
        public static byte[] t18_ID = new byte[0];
        public static int[] Gap_Sector = new int[0];
        public static int[] Track_ID = new int[0];
        public static bool[] Adjust = new bool[0];
        public static string Prot_Method = string.Empty;
        public static string[][] Info = new string[0][];
    }

    public static class NDA  // Global variables for adjusted-sync arrays
    {
        public static byte[][] Track_Data = new byte[0][];
        public static int[] Track_Length = new int[0];
        public static int[] Sector_Zero = new int[0];
        public static int[] D_Start = new int[0];
        public static int[] D_End = new int[0];
        public static int[] sectors = new int[0];
        public static int[] Total_Sync = new int[0];
    }

    public static class NDG  // Global variables for G64 array data
    {
        public static byte[][] Track_Data = new byte[0][];
        public static int[] Track_Length = new int[0];
        public static bool L_Rot = false;
        public static int[] s_len = new int[0];
        public static byte[] newheader = new byte[0];
        public static bool[] Fat_Track = new bool[0];
    }

    public static class Original  // Global variable for retaining original loader track data
    {
        public static byte[] G = new byte[0];
        public static byte[] A = new byte[0];
        public static byte[] SG = new byte[0];
        public static byte[] SA = new byte[0];
        public static byte[][] OT = new byte[0][];
    }

    public static class DiskDir
    {
        public static int Entries = 0;
        public static byte[][] Sectors = new byte[0][];
        public static byte[][] Entry = new byte[0][];
        public static string[] FileName = new string[0];
    }

    class LineColor
    {
        public string Text;
        public Color Color;
    };

    class CustomLabel : System.Windows.Forms.Label
    {

        private int m_RotateAngle = 0;
        private string m_NewText = string.Empty;

        public int RotateAngle { get { return m_RotateAngle; } set { m_RotateAngle = value; Invalidate(); } }
        public string NewText { get { return m_NewText; } set { m_NewText = value; Invalidate(); } }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            //Func<double, double> DegToRad = (angle) => Math.PI * angle / 180.0;
            double DegToRad(double angle) => Math.PI * angle / 180.0;

            Brush b = new SolidBrush(this.ForeColor);
            SizeF size = e.Graphics.MeasureString(this.NewText, this.Font, this.Parent.Width);

            int normalAngle = ((RotateAngle % 360) + 360) % 360;
            double normaleRads = DegToRad(normalAngle);

            int hSinTheta = (int)Math.Ceiling((size.Height * Math.Sin(normaleRads)));
            int wCosTheta = (int)Math.Ceiling((size.Width * Math.Cos(normaleRads)));
            int wSinTheta = (int)Math.Ceiling((size.Width * Math.Sin(normaleRads)));
            int hCosTheta = (int)Math.Ceiling((size.Height * Math.Cos(normaleRads)));

            int rotatedWidth = Math.Abs(hSinTheta) + Math.Abs(wCosTheta);
            int rotatedHeight = Math.Abs(wSinTheta) + Math.Abs(hCosTheta);

            this.Width = rotatedWidth;
            this.Height = rotatedHeight;

            int numQuadrants =
                (normalAngle >= 0 && normalAngle < 90) ? 1 :
                (normalAngle >= 90 && normalAngle < 180) ? 2 :
                (normalAngle >= 180 && normalAngle < 270) ? 3 :
                (normalAngle >= 270 && normalAngle < 360) ? 4 :
                0;

            int horizShift = 0;
            int vertShift = 0;

            if (numQuadrants == 1)
            {
                horizShift = Math.Abs(hSinTheta);
            }
            else if (numQuadrants == 2)
            {
                horizShift = rotatedWidth;
                vertShift = Math.Abs(hCosTheta);
            }
            else if (numQuadrants == 3)
            {
                horizShift = Math.Abs(wCosTheta);
                vertShift = rotatedHeight;
            }
            else if (numQuadrants == 4)
            {
                vertShift = Math.Abs(wSinTheta);
            }

            e.Graphics.TranslateTransform(horizShift, vertShift);
            e.Graphics.RotateTransform(this.RotateAngle);

            e.Graphics.DrawString(this.NewText, this.Font, b, 0f, 0f);
            base.OnPaint(e);
        }
    }

    //class FunctionLoader
    //{
    //    [DllImport("Kernel32.dll")]
    //    private static extern IntPtr LoadLibrary(string path);
    //
    //    [DllImport("Kernel32.dll")]
    //    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    //
    //    public static Delegate LoadFunction<T>(string dllPath, string functionName)
    //    {
    //        var hModule = LoadLibrary(dllPath);
    //        var functionAddress = GetProcAddress(hModule, functionName);
    //        return Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(T));
    //    }
    //}

    public class NativeMethods
    {
        [DllImport("cpp_extf.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Draw_Arc(IntPtr bitmap, int width, int height, int centerX, int centerY, int radius, int[] color, int colorLength, int track, int len, int trackWidth, double sub);
        [DllImport("cpp_extf.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LZ_CompressFast(IntPtr input, IntPtr output, uint size);
        [DllImport("cpp_extf.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LZ_Uncompress(IntPtr input, IntPtr output, uint size);
        [DllImport("cpp_extf.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LZ_GetUncompressedSize(IntPtr input, uint size);
        [DllImport("cpp_extf.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TestLoaded();
    }

    //public class Tag
    //{
    //    public int Index { get; set; } = -1;
    //    public string Notes { get; set; } = string.Empty;
    //}

    //public class ItemTag
    //{
    //    public int Index { get; set; }
    //    public string Notes { get; set; }
    //}

    public class DiskInfo
    {
        public Dictionary<int, string> imgStat = new Dictionary<int, string>
        {
            { 0, string.Empty }, { 1, "working" }, { 2, "works (with errors)" }, { 3, "not working" }
        };

        public Dictionary<int, string> imgRegion = new Dictionary<int, string>
        {
            { 0, string.Empty }, { 1, "NTSC" }, { 2, "PAL" }, { 3, "Any" }
        };

        public const int NAME_SIZE = 64;    // 64
        public const int NOTES_SIZE = 128;  // 128
        public const int ENTRY_SIZE = 256;

        public string Title;
        public bool Marked;
        public bool Locked;
        public bool Source;
        public int Extension;   // 0 = .nib, 1 = .nbz, 2 = .g64
        public int Status;      // 0 = (nothing), 1 = working, 2 = works with errors, 3 = not working
        public int Region;
        public bool Favorite;

        public int Side;
        public byte Protection;
        public int Year;

        public long Offset;
        public int CompressedLength;
        public int DecompressedLength;
        public short PreviewLength;

        public uint crc32;
        public ushort crc16;
        public byte[] rawHash = new byte[16];
        public byte[] secHash = new byte[16];
        public DateTime Timestamp;

        public string Notes; // padded with 0s
        public int Index; // Not stored in the meta!
        public string DirPreview;

        public static DiskInfo FromEntry(byte[] data)
        {
            if (data == null || data.Length != ENTRY_SIZE)
                throw new ArgumentException("Entry data is too short.");

            DiskInfo disk = new DiskInfo();
            int index = 0;
            disk.Title = Encoding.ASCII.GetString(data, index, 64).TrimEnd('\0'); index += 64;
            DecodeBits_1(disk, data[index++], data[index++], data[index++], data[index++]);
            disk.Offset = BitConverter.ToInt64(data, index); index += 8;
            disk.CompressedLength = BitConverter.ToInt32(data, index); index += 4;
            disk.DecompressedLength = BitConverter.ToInt32(data, index); index += 4;
            disk.PreviewLength = BitConverter.ToInt16(data, index); index += 2;
            disk.crc32 = BitConverter.ToUInt32(data, index); index += 4;
            disk.crc16 = BitConverter.ToUInt16(data, index); index += 2;
            disk.rawHash = new byte[16];
            Buffer.BlockCopy(data, index, disk.rawHash, 0, 16); index += 16;
            disk.secHash = new byte[16];
            Buffer.BlockCopy(data, index, disk.secHash, 0, 16); index += 16;
            disk.Notes = Encoding.ASCII.GetString(data, index, 128).TrimEnd('\0'); index += 128;
            disk.Timestamp = DecodeTimestamp(BitConverter.ToUInt32(data, index));
            return disk;
        }

        public byte[] ToEntry()
        {
            List<byte> bytes = new List<byte>();
            var titleBytes = Encoding.ASCII.GetBytes(Title ?? "");
            var notesBytes = Encoding.ASCII.GetBytes(Notes ?? "");
            Array.Resize(ref titleBytes, NAME_SIZE); // pad with 0s
            Array.Resize(ref notesBytes, NOTES_SIZE); // pad with 0s
            bytes.AddRange(titleBytes);
            //bytes.AddRange(new byte[] { EncodeBits_1(), (byte)Side, (byte)Protection, (byte)(Year - 1970) });
            (byte meta, byte side, byte year) = EncodeBits_1();
            bytes.AddRange(new byte[] { (byte)meta, (byte)side, (byte)Protection, (byte)year });
            bytes.AddRange(BitConverter.GetBytes(Offset));
            bytes.AddRange(BitConverter.GetBytes(CompressedLength));
            bytes.AddRange(BitConverter.GetBytes(DecompressedLength));
            bytes.AddRange(BitConverter.GetBytes(PreviewLength));
            bytes.AddRange(BitConverter.GetBytes(crc32));
            bytes.AddRange(BitConverter.GetBytes(crc16));
            bytes.AddRange(rawHash ?? FastArray.Init(16, 0));
            bytes.AddRange(secHash ?? FastArray.Init(16, 0));
            bytes.AddRange(notesBytes);
            bytes.AddRange(BitConverter.GetBytes(EncodeTimestamp(Timestamp)));
            return bytes.ToArray();
        }

        static void DecodeBits_1(DiskInfo disk, byte meta, byte side, byte protection, byte year)
        {
            disk.Marked = (meta & (1 << 7)) != 0;
            disk.Locked = (meta & (1 << 6)) != 0;
            disk.Source = (meta & (1 << 5)) != 0;
            disk.Extension = (meta >> 3) & 0b11;
            disk.Region = (meta >> 1) & 0b11;
            disk.Favorite = (meta & 1) != 0;

            disk.Side = side & 0b00011111;                // bits 0–4
            disk.Status = (side >> 5) & 0b11;             // bits 5–6

            disk.Protection = protection;
            disk.Year = year + 1970;
        }

        public (byte meta, byte side, byte year) EncodeBits_1()
        {
            byte meta = 0;
            byte side = (byte)(Side & 0b00011111);        // keep only lower 5 bits
            byte year = (byte)(Year - 1970);

            if (Marked) meta |= 1 << 7;
            if (Locked) meta |= 1 << 6;
            if (Source) meta |= 1 << 5;
            meta |= (byte)((Extension & 0b11) << 3);
            meta |= (byte)((Region & 0b11) << 1);
            if (Favorite) meta |= 1;

            side |= (byte)((Status & 0b11) << 5);         // set bits 5–6
            return (meta, side, year);
        }

        //public byte EncodeBits_1()
        //{
        //    byte meta = 0;
        //    if (Marked) meta |= 1 << 7;
        //    if (Locked) meta |= 1 << 6;
        //    if (Source) meta |= 1 << 5;
        //    meta |= (byte)((Extension & 0b11) << 3);
        //    meta |= (byte)((Region & 0b11) << 1);
        //    if (Favorite) meta |= 1;
        //    return meta;
        //}
        //
        //static void DecodeBits_1(DiskInfo disk, byte meta, byte side, byte protection, byte year)
        //{
        //    disk.Marked = (meta & (1 << 7)) != 0;
        //    disk.Locked = (meta & (1 << 6)) != 0;
        //    disk.Source = (meta & (1 << 5)) != 0;
        //    disk.Extension = (meta >> 3) & 0b11;
        //    disk.Region = (meta >> 1) & 0b11;
        //    disk.Favorite = (meta & 1) != 0;
        //    disk.Side = (side & 0x1f); // 0b00011111);
        //    disk.Protection = protection;
        //    disk.Year = year + 1970;
        //}

        public static uint EncodeTimestamp(DateTime dt)
        {
            uint year = (uint)(dt.Year - 1980);
            uint month = (uint)dt.Month;
            uint day = (uint)dt.Day;
            uint hour = (uint)dt.Hour;
            uint minute = (uint)dt.Minute;
            uint second = (uint)(dt.Second / 2); // stored in 2-second increments

            return (year << 25) | (month << 21) | (day << 16) |
                   (hour << 11) | (minute << 5) | second;
        }

        public static DateTime DecodeTimestamp(uint ts)
        {
            int year = 1980 + (int)((ts >> 25) & 0x7F);
            int month = (int)((ts >> 21) & 0x0F);
            int day = (int)((ts >> 16) & 0x1F);
            int hour = (int)((ts >> 11) & 0x1F);
            int minute = (int)((ts >> 5) & 0x3F);
            int second = (int)((ts & 0x1F) * 2);

            return new DateTime(year, month, day, hour, minute, second);
        }
    }

    public class AccessDatabase : IDisposable
    {
        //private readonly object streamLock = new object();
        public long Offset;
        public ushort Entries;
        public bool Valid;
        public FileStream Stream;
        private readonly int headerOffset = 6;
        private readonly string na = "Stream is not available";
        private readonly string ro = "Stream is ReadOnly";
        public enum Mode { Read, Write }

        public AccessDatabase ReadOnly(string path)
        {
            return Open(path, Mode.Read);
        }

        public AccessDatabase ReadWrite(string path)
        {
            return Open(path, Mode.Write);
        }

        private AccessDatabase Open(string path, Mode mode)
        {
            if (File.Exists(path) && new FileInfo(path).Length >= 16)
            {
                Stream = new FileStream(path, FileMode.Open, mode == Mode.Write
                    ? FileAccess.ReadWrite : FileAccess.Read, FileShare.Read);
                byte[] header = new byte[16];
                Stream.Seek(0, SeekOrigin.Begin);
                Stream.Read(header, 0, 16);
                if (Encoding.ASCII.GetString(header, 0, 6) == "SageDB")
                {
                    Entries = BitConverter.ToUInt16(header, 6);
                    Offset = BitConverter.ToInt64(header, 8);
                    Valid = Entries >= 0 && Offset <= Stream.Length && Offset + (Entries * DiskInfo.ENTRY_SIZE) <= Stream.Length;
                }
            }
            return this;
        }

        public AccessDatabase Create(string path)
        {
            if (File.Exists(path)) throw new Exception("File already exists");
            ushort e = 0; long o = 16;
            File.WriteAllBytes(path, ArrayConcat(Encoding.ASCII.GetBytes("SageDB")
                , BitConverter.GetBytes(e), BitConverter.GetBytes(o)));
            return Open(path, Mode.Write);
        }

        public void UpdateHeader()
        {
            Seek(headerOffset);
            Write(ArrayConcat(BitConverter.GetBytes(Entries), BitConverter.GetBytes(Offset)));
        }

        public void WriteDirectory(byte[] directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory), "Directory can't be null!");
            if (directory.Length % DiskInfo.ENTRY_SIZE != 0)
                throw new ArgumentException("Directory length must be divisible by entry size.", nameof(directory));
            if (Entries * DiskInfo.ENTRY_SIZE != directory.Length)
                throw new ArgumentException("Directory length does not match the expected number of entries.", nameof(directory));
            Seek(Offset);
            Write(directory);
        }

        public DiskInfo GetDirectoryEntry(int index)
        {
            if (index < 0 || index >= Entries) throw new Exception($"Index outside bounds of the array {index} of 0 - {Entries}");
            Seek(Offset + (index * DiskInfo.ENTRY_SIZE));
            return DiskInfo.FromEntry(Read(DiskInfo.ENTRY_SIZE));
        }

        public byte[] GetImageData(DiskInfo ent)
        {
            if (ent != null)
            {
                Seek(ent.Offset);
                byte[] data = Read(ent.CompressedLength);
                return data;
            }
            return null;
        }

        public byte[] GetPreviewData(DiskInfo ent)
        {
            if (ent != null)
            {
                Seek(ent.Offset + ent.CompressedLength);
                var preview = Read(ent.PreviewLength);
                return preview;
            }
            return null;
        }

        public void Seek(long offset)
        {
            if (!Valid || Stream == null) throw new Exception(na);
            if (Stream.CanSeek) Stream.Seek(offset, SeekOrigin.Begin); else throw new Exception("Stream can't locate position");
        }

        public void Write(byte[] data)
        {
            if (!Valid || Stream == null) throw new Exception(na);
            if (Stream.CanWrite) Stream.Write(data, 0, data.Length); else throw new Exception(ro);
        }

        public byte[] Read(long Length)
        {
            if (!Valid || Stream == null) throw new Exception(na);
            try
            {
                var data = new byte[Length];
                if (Stream.CanRead) Stream.Read(data, 0, data.Length); else throw new Exception("Error reading file.");
                return data;
            }
            catch { }
            return null;
        }

        static byte[] ArrayConcat(params byte[][] arrays)
        {
            var totalLength = arrays.Sum(a => a.Length);
            var result = new byte[totalLength];
            var offset = 0;
            foreach (var array in arrays)
            {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }
            return result;
        }

        public void Close()
        {
            Stream?.Close();
            Stream = null;
            Valid = false;
        }

        public void Dispose()
        {
            Stream?.Close();
            Stream = null;
            Valid = false;
        }
    }

    public static class Checksum
    {
        private static readonly uint[] Table;

        static Checksum()
        {
            Table = new uint[256];
            const uint Polynomial = 0xEDB88320;
            for (uint i = 0; i < Table.Length; ++i)
            {
                uint crc = i;
                for (int j = 0; j < 8; ++j)
                    crc = (crc & 1) != 0 ? (Polynomial ^ (crc >> 1)) : (crc >> 1);
                Table[i] = crc;
            }
        }

        public static ushort CRC16(Stream stream, long offset, int length)
        {
            const int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
            ushort crc = 0xFFFF;

            stream.Seek(offset, SeekOrigin.Begin);
            int remaining = length;

            while (remaining > 0)
            {
                int toRead = Math.Min(bufferSize, remaining);
                int bytesRead = stream.Read(buffer, 0, toRead);
                if (bytesRead == 0) break; // End of stream before expected length

                for (int i = 0; i < bytesRead; i++)
                {
                    crc ^= (ushort)(buffer[i] << 8);
                    for (int j = 0; j < 8; j++)
                    {
                        if ((crc & 0x8000) != 0)
                            crc = (ushort)((crc << 1) ^ 0x1021); // Standard CRC16-CCITT polynomial
                        else
                            crc <<= 1;
                    }
                }

                remaining -= bytesRead;
            }

            return crc;
        }

        public static uint CRC32(Stream stream, long offset, int length)
        {
            const int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
            uint crc = 0xFFFFFFFF;
            stream.Seek(offset, SeekOrigin.Begin);
            int remaining = length;

            while (remaining > 0)
            {
                int toRead = Math.Min(bufferSize, remaining);
                int bytesRead = stream.Read(buffer, 0, toRead);
                if (bytesRead == 0) break; // End of stream before expected length

                for (int i = 0; i < bytesRead; i++)
                {
                    byte index = (byte)((crc ^ buffer[i]) & 0xFF);
                    crc = (crc >> 8) ^ Table[index];
                }
                remaining -= bytesRead;
            }
            return ~crc;
        }

        public static ushort CRC16(byte[] data)
        {
            const ushort polynomial = 0x1021;
            ushort crc = 0xFFFF;
            if (data == null) return crc;

            foreach (byte b in data)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ polynomial) : (ushort)(crc << 1);
                }
            }
            return crc;
        }

        public static uint CRC32(byte[] bytes)
        {
            uint crc = 0xFFFFFFFF;
            foreach (byte b in bytes)
            {
                byte index = (byte)((crc ^ b) & 0xFF);
                crc = (crc >> 8) ^ Table[index];
            }
            return ~crc;
        }

        public static byte[] MD5(byte[] data)
        {
            using (MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }
    }

    //public static class BitmapExtensions
    //{
    //    public static Image SetOpacity(this Image image, float opacity)
    //    {
    //        var colorMatrix = new ColorMatrix();
    //        colorMatrix.Matrix33 = opacity;
    //        var imageAttributes = new ImageAttributes();
    //        imageAttributes.SetColorMatrix(
    //            colorMatrix,
    //            ColorMatrixFlag.Default,
    //            ColorAdjustType.Bitmap);
    //        var output = new Bitmap(image.Width, image.Height);
    //        using (var gfx = Graphics.FromImage(output))
    //        {
    //            gfx.SmoothingMode = SmoothingMode.AntiAlias;
    //            gfx.DrawImage(
    //                image,
    //                new Rectangle(0, 0, image.Width, image.Height),
    //                0,
    //                0,
    //                image.Width,
    //                image.Height,
    //                GraphicsUnit.Pixel,
    //                imageAttributes);
    //        }
    //        return output;
    //    }
    //}

    public class DoubleBufferedListView : ListView
    {
        public DoubleBufferedListView()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();
        }

        public Rectangle GetSubItemBounds(ListViewItem item, int subItemIndex)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (subItemIndex >= item.SubItems.Count)
                throw new ArgumentOutOfRangeException(nameof(subItemIndex));

            Rectangle itemBounds = item.GetBounds(ItemBoundsPortion.Entire);

            int left = itemBounds.Left;
            for (int i = 0; i < subItemIndex; i++)
            {
                left += this.Columns[i].Width;
            }

            int width = this.Columns[subItemIndex].Width;

            return new Rectangle(left, itemBounds.Top, width, itemBounds.Height);
        }
    }

    public class CustomCheckedListBox : CheckedListBox
    {
        public CustomCheckedListBox()
        {
            this.DoubleBuffered = true;
            this.DrawMode = DrawMode.OwnerDrawFixed;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            base.OnDrawItem(e);

            if (e.Index < 0) return;

            bool isDragging = e.Index == DraggingIndex;
            bool isChecked = GetItemChecked(e.Index);
            Brush brush = new SolidBrush(Color.FromArgb(135, 122, 237));
            Color fore = Color.FromArgb(69, 55, 176);

            // Set the background color based on whether the item is being dragged
            e.DrawBackground();
            if (isDragging)
            {
                //e.Graphics.FillRectangle(Brushes.White, e.Bounds);
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            else if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                //e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Define the rectangle for the checkbox
            Rectangle checkboxRect = new Rectangle(e.Bounds.Left + 2, e.Bounds.Top + 3, 16, 16);

            // Draw a checkbox
            CheckBoxRenderer.DrawCheckBox(e.Graphics, checkboxRect.Location,
                                          isChecked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal);

            // Define the rectangle for the text
            Rectangle textRect = new Rectangle(e.Bounds.Left + 20, e.Bounds.Top + 2, e.Bounds.Width - 20, e.Bounds.Height);

            // Get the item text and escape ampersands
            string itemText = Items[e.Index].ToString().Replace("&", "&&");

            // Define Text color for selected items that aren't being dragged
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color textColor = isSelected ? fore : SystemColors.ControlText;
            if (isSelected) TextRenderer.DrawText(e.Graphics, itemText, e.Font, textRect, textColor, TextFormatFlags.Left);

            // Draw the item text
            else TextRenderer.DrawText(e.Graphics, itemText, e.Font, textRect, isDragging ? Color.White : e.ForeColor, TextFormatFlags.Left);

            e.DrawFocusRectangle();
        }
        public int DraggingIndex { get; set; } = -1;
    }

    public class AutoClosingMessageBox
    {
        readonly System.Threading.Timer _timeoutTimer;
        readonly string _caption;
        AutoClosingMessageBox(string text, string caption, int timeout)
        {
            _caption = caption;
            _timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
                null, timeout, System.Threading.Timeout.Infinite);
            using (_timeoutTimer)
                MessageBox.Show(text, caption);
        }
        public static void Show(string text, string caption, int timeout)
        {
            new AutoClosingMessageBox(text, caption, timeout);
        }
        void OnTimerElapsed(object state)
        {
            IntPtr mbWnd = FindWindow("#32770", _caption); // lpClassName is #32770 for MessageBox
            if (mbWnd != IntPtr.Zero)
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _timeoutTimer.Dispose();
        }
        const int WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }

    public class Message_Center : IDisposable
    {
        private readonly IWin32Window owner;
        private readonly HookProc hookProc;
        private readonly IntPtr hHook = IntPtr.Zero;

        public Message_Center(IWin32Window owner)
        {
            this.owner = owner ?? throw new ArgumentNullException("owner");
            hookProc = DialogHookProc;

            hHook = SetWindowsHookEx(WH_CALLWNDPROCRET, hookProc, IntPtr.Zero, GetCurrentThreadId());
        }

        private IntPtr DialogHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                return CallNextHookEx(hHook, nCode, wParam, lParam);
            }

            CWPRETSTRUCT msg = (CWPRETSTRUCT)Marshal.PtrToStructure(lParam, typeof(CWPRETSTRUCT));
            IntPtr hook = hHook;

            if (msg.message == (int)CbtHookAction.HCBT_ACTIVATE)
            {
                try
                {
                    CenterWindow(msg.hwnd);
                }
                finally
                {
                    UnhookWindowsHookEx(hHook);
                }
            }
            return CallNextHookEx(hook, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(hHook);
        }

        private void CenterWindow(IntPtr hChildWnd)
        {
            Rectangle recChild = new Rectangle(0, 0, 0, 0);
            bool success = GetWindowRect(hChildWnd, ref recChild);
            if (!success)
            {
                return;
            }
            int width = recChild.Width - recChild.X;
            int height = recChild.Height - recChild.Y;
            Rectangle recParent = new Rectangle(0, 0, 0, 0);
            success = GetWindowRect(owner.Handle, ref recParent);
            if (!success)
            {
                return;
            }

            Point ptCenter = new Point(0, 0)
            {
                X = recParent.X + ((recParent.Width - recParent.X) / 2),
                Y = recParent.Y + ((recParent.Height - recParent.Y) / 2)
            };

            Point ptStart = new Point(0, 0)
            {
                X = (ptCenter.X - (width / 2)),
                Y = (ptCenter.Y - (height / 2))
            };

            Task.Factory.StartNew(() => SetWindowPos(hChildWnd, (IntPtr)0, ptStart.X, ptStart.Y, width, height, SetWindowPosFlags.SWP_ASYNCWINDOWPOS | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOOWNERZORDER | SetWindowPosFlags.SWP_NOZORDER));
        }

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        public delegate void TimerProc(IntPtr hWnd, uint uMsg, UIntPtr nIDEvent, uint dwTime);
        private const int WH_CALLWNDPROCRET = 12;
        private enum CbtHookAction : int
        {
            HCBT_MOVESIZE = 0,
            HCBT_MINMAX = 1,
            HCBT_QS = 2,
            HCBT_CREATEWND = 3,
            HCBT_DESTROYWND = 4,
            HCBT_ACTIVATE = 5,
            HCBT_CLICKSKIPPED = 6,
            HCBT_KEYSKIPPED = 7,
            HCBT_SYSCOMMAND = 8,
            HCBT_SETFOCUS = 9
        }

        [DllImport("kernel32.dll")]
        static extern int GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);

        [DllImport("user32.dll")]
        private static extern int MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("User32.dll")]
        public static extern UIntPtr SetTimer(IntPtr hWnd, UIntPtr nIDEvent, uint uElapse, TimerProc lpTimerFunc);

        [DllImport("User32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll")]
        public static extern int UnhookWindowsHookEx(IntPtr idHook);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxLength);

        [DllImport("user32.dll")]
        public static extern int EndDialog(IntPtr hDlg, IntPtr nResult);

        [StructLayout(LayoutKind.Sequential)]
        public struct CWPRETSTRUCT
        {
            public IntPtr lResult;
            public IntPtr lParam;
            public IntPtr wParam;
            public uint message;
            public IntPtr hwnd;
        };
    }

    [Flags]
    public enum SetWindowPosFlags : uint
    {
        SWP_ASYNCWINDOWPOS = 0x4000,
        SWP_DEFERERASE = 0x2000,
        SWP_DRAWFRAME = 0x0020,
        SWP_FRAMECHANGED = 0x0020,
        SWP_HIDEWINDOW = 0x0080,
        SWP_NOACTIVATE = 0x0010,
        SWP_NOCOPYBITS = 0x0100,
        SWP_NOMOVE = 0x0002,
        SWP_NOOWNERZORDER = 0x0200,
        SWP_NOREDRAW = 0x0008,
        SWP_NOREPOSITION = 0x0200,
        SWP_NOSENDCHANGING = 0x0400,
        SWP_NOSIZE = 0x0001,
        SWP_NOZORDER = 0x0004,
        SWP_SHOWWINDOW = 0x0040,
    }

    public class FastBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public FastBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new Int32[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }

        public IntPtr GetPixelPtr()
        {
            return BitsHandle.AddrOfPinnedObject();
        }

        public void SetPixel(int x, int y, Color color)
        {
            int index = x + (y * Width);
            int col = color.ToArgb();

            Bits[index] = col;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }
    }

    public class Gbox : GroupBox
    {
        private Color _borderColor = Color.Black;

        public Color BorderColor
        {
            get { return this._borderColor; }
            set { this._borderColor = value; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Size tSize = TextRenderer.MeasureText(this.Text, this.Font);
            Rectangle borderRect = e.ClipRectangle;
            borderRect.Y += (tSize.Height / 2);
            borderRect.Height -= (tSize.Height / 2);
            ControlPaint.DrawBorder(e.Graphics, borderRect, this._borderColor, ButtonBorderStyle.Solid);
            Rectangle textRect = e.ClipRectangle;
            textRect.X += 6;
            textRect.Width = tSize.Width;
            textRect.Height = tSize.Height;
            e.Graphics.FillRectangle(new SolidBrush(this.BackColor), textRect);
            e.Graphics.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), textRect);
        }
    }
    public static class FastArray
    {
        [DllImport("msvcrt.dll",
                  EntryPoint = "memset",
                  CallingConvention = CallingConvention.Cdecl,
                  SetLastError = false)]
        private static extern IntPtr MemSet(IntPtr dest, int c, int count);

        public static byte[] Init(int size, byte value)
        {
            if (size < 0) size = 0;
            var temp = new byte[size];
            GCHandle gch = GCHandle.Alloc(temp, GCHandleType.Pinned);
            MemSet(gch.AddrOfPinnedObject(), value, temp.Length);
            gch.Free();
            return temp;
        }
    }

    public class TaggedRectangle
    {
        public Rectangle Rect { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public (int Track, int Sector) Tag { get; set; }

        // Constructor to create TaggedRectangle with individual x, y, width, and height
        public TaggedRectangle(int x, int y, int width, int height, int track, int sector)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Rect = new Rectangle(x, y, width, height);
            Tag = (track, sector);
        }

        // Contains method to check if a point is within the Rect
        public bool Contains(Point point)
        {
            return Rect.Contains(point);
        }
    }

    class BlockMapInfo
    {
        public TaggedRectangle Rect { get; set; }
        public int Track { get; set; }
        public int Sector { get; set; }
        public Color Color { get; set; }
        public string Tip { get; set; }

        public BlockMapInfo(TaggedRectangle rect, int track, int sector, Color color, string tip)
        {
            Rect = rect;
            Track = track;
            Sector = sector;
            Color = color;
            Tip = tip;
        }

        public bool Contains(Point point)
        {
            return Rect.Contains(point);
        }
    }

    public class CustomBufferedPanel : Panel
    {
        public Color BorderColor { get; set; } = Color.Black;
        public override Color BackColor { get; set; } = Color.Black;
        public int BorderThickness { get; set; } = 2;

        public CustomBufferedPanel()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (Pen borderPen = new Pen(BorderColor, BorderThickness))
            {
                base.OnPaintBackground(e);
                Brush brush = new SolidBrush(BackColor);
                Rectangle rect = new Rectangle(
                    BorderThickness / 2,
                    BorderThickness / 2,
                    this.Width - BorderThickness,
                    this.Height - BorderThickness);
                Rectangle back = new Rectangle(0, 0, Width, Height);
                e.Graphics.DrawRectangle(borderPen, rect);
            }
        }
    }
}