using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        /// These functions return 'True' if no errors were found.  If true, this signals to the caller that the image should be processed

        bool Import_NIB(string file, bool isNBZ = false)
        {
            byte[] data;
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                data = isNBZ ? LZdecompress(ms.ToArray()) : ms.ToArray();
            }
            if (data != null && data.Length >= NIB_HEADER_LEN + NIB_TRACK_LEN)
            {
                int length = data.Length;
                tracks = (length - NIB_HEADER_LEN) / NIB_TRACK_LEN;
                if ((tracks * NIB_TRACK_LEN) + NIB_HEADER_LEN == length)
                {
                    tracks = tracks > 42 ? Math.Min(tracks, 82) : Math.Min(tracks, 41);
                    if (!batch) Set_ListBox_Items(true, false);
                    nib_header = CopyArray(data, 0, NIB_HEADER_LEN);
                    if (Encoding.ASCII.GetString(nib_header, 0, 13) == "MNIB-1541-RAW")
                    {
                        var lab = $"Total Tracks ({tracks})";
                        Set_Arrays(tracks);
                        for (int i = 0; i < tracks; i++)
                        {
                            NDS.Track_Data[i] = CopyArray(data, NIB_HEADER_LEN + (NIB_TRACK_LEN * i), NIB_TRACK_LEN);
                            Original.OT[i] = new byte[0];
                        }
                        return true;
                    }
                    else
                    {
                        if (!batch) Display_Error();
                    }
                }
            }
            return false;
        }

        bool Import_G64(string file, bool isZ64 = false)
        {
            byte[] decomp;
            g64_header = new byte[684];
            using (FileStream Stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                long length = new FileInfo(file).Length;
                Set_ListBox_Items(true, false);
                Stream.Seek(0, SeekOrigin.Begin);
                if (isZ64)
                {
                    byte[] compressed = new byte[length];
                    Stream.Read(compressed, 0, (int)length);
                    decomp = LZdecompress(compressed);
                }
                else
                {
                    decomp = new byte[length];
                    Stream.Read(decomp, 0, (int)length);
                }
            }
            if (decomp != null && decomp.Length >= 684)
            {
                g64_header = CopyArray(decomp, 0, 684);
                Buffer.BlockCopy(decomp, 0, g64_header, 0, 684);
                if (Encoding.ASCII.GetString(g64_header, 0, 8) == "GCR-1541")
                {
                    tracks = Math.Min(Convert.ToInt32(g64_header[9]), 82);
                    Set_Arrays(tracks);
                    for (int i = 0; i < tracks; i++)
                    {
                        Original.OT[i] = new byte[0];
                        int pos = BitConverter.ToInt32(g64_header, 12 + (i * 4));
                        if (pos != 0)
                        {
                            try
                            {
                                short ts = BitConverter.ToInt16(decomp, pos);
                                var tdata = CopyArray(decomp, pos + 2, ts);
                                (int r, int ln) = FindLongestRun_General(tdata);
                                if (r > 0 && ln > 0) tdata = Rotate_Left(tdata, r + ln);
                                NDG.s_len[i] = tdata.Length;
                                NDS.Track_Data[i] = FillArray(tdata, NIB_TRACK_LEN);
                            }
                            catch { }
                        }
                        else NDS.Track_Data[i] = FastArray.Init(NIB_TRACK_LEN, 0x00);
                    }
                    return true;
                }
                else Display_Error();
            }
            return false;
        }

        bool Import_D64(string file)
        {
            byte[] data;
            using (FileStream Stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (MemoryStream ms = new MemoryStream())
            {
                Stream.CopyTo(ms);
                data = ms.ToArray();
            }
            if (data != null && data.Length >= 256)
            {
                int length = data.Length;
                bool errorcodes = length % 257 == 0;
                int sectors = sectors = length / (errorcodes ? 257 : 256);
                byte[] codes = new byte[sectors];
                byte[][] secdata = new byte[sectors][];
                int sector_counter = 0;
                tracks = 0;
                for (int i = 0; i <= sectors; i++)
                {
                    try
                    {
                        if (i < sectors)
                        {
                            secdata[i] = CopyArray(data, i << 8, 256);
                            sector_counter++;
                            if (sector_counter == Available_Sectors[tracks])
                            {
                                sector_counter = 0;
                                tracks++;
                            }
                        }
                        if (i == sectors && errorcodes) codes = CopyArray(data, i << 8, sectors);
                    }
                    catch { }
                }
                if (tracks > 0)
                {
                    Set_ListBox_Items(true, false);
                    Set_Arrays(tracks);
                    byte[] ID = FastArray.Init(4, 0x0f);
                    byte[] ID_MisMatch = FastArray.Init(4, 0x0f);
                    ID[1] = secdata[357][163];
                    ID[0] = secdata[357][162];
                    for (int i = 0; i < 8; i++)
                    {
                        ID_MisMatch[0] = ToggleBit((byte)ID[0], i);
                        ID_MisMatch[1] = ToggleBit((byte)ID[1], i);
                    }
                    int psec = 0;
                    byte[] sync = FastArray.Init(5, 0xff);
                    byte[] nosync = FastArray.Init(5, cbm_gap);
                    byte[] noheader = FastArray.Init(10, cbm_gap);
                    byte[] nodata = FastArray.Init(325, cbm_gap);
                    for (int i = 0; i < tracks; i++)
                    {

                        NDS.Track_Data[i] = FastArray.Init(NIB_TRACK_LEN, 0x00);
                        int tsec = Available_Sectors[i];
                        int len = density[density_map[i]];
                        byte[] gap = SetSectorGap(sector_gap_length[i]);
                        using (MemoryStream buffer = new MemoryStream())
                        using (BinaryWriter write = new BinaryWriter(buffer))
                        {
                            for (int j = 0; j < tsec; j++)
                            {
                                bool isHeaderMissing = codes[psec] == 2;
                                bool isDataMissing = codes[psec] == 4;
                                bool badDataChecksum = codes[psec] == 5;
                                bool badHeaderChecksum = codes[psec] == 9;
                                bool idMismatch = codes[psec] == 11;
                                write.Write(isHeaderMissing ? nosync : sync);
                                write.Write(isHeaderMissing ? noheader : Build_BlockHeader(i + 1, j, idMismatch ? ID_MisMatch : ID, badHeaderChecksum));
                                write.Write(gap);
                                write.Write(isDataMissing ? nosync : sync);
                                write.Write(isDataMissing ? nodata : Build_Sector(secdata[psec], badDataChecksum));
                                write.Write(gap);
                                psec++;
                            }
                            int dif = len - (int)buffer.Length;
                            if (dif > 0) write.Write(FastArray.Init(dif, 0x55));
                            byte[] temp = buffer.ToArray();
                            NDS.Track_Data[i] = FillArray(temp, NIB_TRACK_LEN);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        void Display_Error()
        {
            label1.Text = $"Bad Header";
            label2.Text = "";
            using (Message_Center center = new Message_Center(this)) // center message box
            {
                string t = "Bad Header!";
                string s = "Image is corrupt and cannot be opened";
                MessageBox.Show(s, t, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                error = true;
            }
        }
    }
}
