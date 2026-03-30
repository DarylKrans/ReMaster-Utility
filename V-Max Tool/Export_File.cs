using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        void Export_File(int last_track = -1)
        {
            Save_Dialog.FileName = $"{fname}{fnappend}";
            //Save_Dialog.Filter = "G64|*.g64|NIB|*.nib|D64|*.d64|NBZ|*.nbz|Compressed G64 [only supported by ReMaster]|*.z64";
            Save_Dialog.Filter = "G64|*.g64|NIB|*.nib|D64|*.d64|NBZ|*.nbz";
            Save_Dialog.Title = "Save File";
            if (Save_Dialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = Save_Dialog.FileName;
                switch (Save_Dialog.FilterIndex)
                {
                    case 1: Make_G64(fileName, last_track); break;
                    case 2: Make_NIB(fileName); break;
                    case 3: Make_D64(fileName, last_track); break;
                    case 4: Make_NIB(fileName, true); break;
                }
                if (nib_error || g64_error)
                {
                    using (Message_Center center = new Message_Center(this)) // center message box
                    {
                        string t = "File Access Error!";
                        string s = nib_error ? $"{nib_err_msg}" : g64_error ? $"{g64_err_msg}" : "Undefined error, What did you do?!";
                        AutoClosingMessageBox.Show(s, t, 5000);
                        error = true;
                    }
                    nib_error = g64_error = false;
                }
            }
        }

        void Make_NIB(string fname, bool compress = false)
        {
            bool halfTracks = tracks > 42;
            using (var buffer = new MemoryStream())
            using (var write = new BinaryWriter(buffer))
            {
                write.Write(Encoding.ASCII.GetBytes("MNIB-1541-RAW"));
                write.Write(new byte[] { 0x03, 0x00, (byte)(halfTracks ? 0x01 : 0x00) });
                byte[] empty = FastArray.Init(8192, 0);
                for (int i = 0; i < tracks; i++)
                {
                    write.Write((byte)(halfTracks ? i + 2 : (i + 2) << 1));
                    write.Write((byte)(Disk.Source.Track[i].Format < secF.Length - 1 ? (3 - Get_Density(Disk.G64.Track[i].Length)) : 0x00));
                }
                write.Write(FastArray.Init(256 - (int)buffer.Length, 0x00));
                for (int i = 0; i < tracks; i++)
                {
                    write.Write((i < Disk.Adjusted.Track[i].Data.Length && Disk.Adjusted.Track[i].Data != null) ? Disk.Adjusted.Track[i].Data : empty);
                }
                try
                {
                    File.WriteAllBytes(fname, compress ? LZcompress(buffer.ToArray()) : buffer.ToArray());
                }
                catch (Exception ex)
                {
                    nib_error = true;
                    nib_err_msg = ex.Message;
                }
            }
        }

        void Make_G64(string fname, int last_track, bool compress = false)
        {
            fname = fname.Replace($"\\\\", $"\\");
            if (last_track < 0) last_track = tracks;
            if (!Directory.Exists(Path.GetDirectoryName(fname))) Directory.CreateDirectory(Path.GetDirectoryName(fname));
            List<int> WritableTracks = new List<int>();
            int defaultTracks = 84; // tracks > 42 ? last_track : last_track << 1;
            //short MaxLen = (short)Math.Max(NDS.cbm.Select((val, i) => (val >= 0 && val < secF.Length - 1)
            //? NDG.Track_Length[i] : 0).Take(last_track).Max(), 7928);
            short MaxLen = (short)Math.Max(Disk.Source.Track.Take(last_track).Select((t, i)
               => (t != null && t.Format >= 0 && t.Format < secF.Length - 1) ? Disk.G64.Track[i].Length : 0).Max(), 7928);

            byte[] header = Encoding.ASCII.GetBytes("GCR-1541");
            byte[] TotalTracks = BitConverter.GetBytes((short)defaultTracks).Reverse().ToArray();
            byte[] MaxTrkLen = BitConverter.GetBytes(MaxLen);
            byte[] TrackPointers = FastArray.Init(defaultTracks << 2, 0);
            byte[] TrackDensities = FastArray.Init(defaultTracks << 2, 0);
            byte[] watermark = Encoding.ASCII.GetBytes($"    ReMaster Utility{ver} https://github.com/DarylKrans/ReMaster-Utility                  ")
                .Select(b => b == 0x20 ? (byte)0x00 : b).ToArray();
            for (int i = 0; i < watermark.Length; i++) if (watermark[i] == 0x20) watermark[i] = 0x00;
            int dataOffset = new[] { header, TotalTracks, MaxTrkLen, TrackPointers, TrackDensities, watermark }.Sum(arr => arr.Length);
            int skip = tracks > 42 ? 1 : 0;
            for (int i = 0; i < last_track; i++)
            {
                if (Disk.G64.Track[i].Length > 6000 && Disk.Source.Track[i].Format >= 0 && Disk.Source.Track[i].Format < secF.Length - 1)
                {
                    int pointerOffset = WritableTracks.Count << 1;
                    int pairedTrackOffset = skip == 1 ? 2 : 1;
                    int trackSkip = skip == 0 ? 2 : 1;
                    int density = 3 - Get_Density(Disk.G64.Track[i].Length);
                    SetPointer(TrackPointers, i * trackSkip, dataOffset + pointerOffset);
                    SetPointer(TrackDensities, i * trackSkip, density);
                    if (i + pairedTrackOffset < tracks && Disk.G64.Track[i].FatTrack)
                    {
                        SetPointer(TrackPointers, (i * trackSkip) + 1, dataOffset + pointerOffset);
                        SetPointer(TrackDensities, (i * trackSkip) + 1, density);
                    }
                    dataOffset += Pad_Tracks.Checked ? MaxLen : Disk.G64.Track[i].Length;
                    WritableTracks.Add(i);
                }
            }
            if (WritableTracks.Count > 0)
            {
                using (var buffer = new MemoryStream())
                using (var write = new BinaryWriter(buffer))
                {
                    write.Write(ArrayConcat(header, TotalTracks, MaxTrkLen, TrackPointers, TrackDensities, watermark));
                    for (int i = 0; i < WritableTracks.Count; i++)
                    {
                        short trk_len = (short)Disk.G64.Track[WritableTracks[i]].Data.Length;
                        int fillLength = MaxLen - trk_len;
                        write.Write(trk_len);
                        write.Write(Disk.G64.Track[WritableTracks[i]].Data);
                        if (Pad_Tracks.Checked && fillLength > 0) write.Write(FastArray.Init(fillLength, 0));
                    }
                    try
                    {
                        File.WriteAllBytes(fname, compress ? LZcompress(buffer.ToArray()) : buffer.ToArray());
                    }
                    catch (Exception ex)
                    {
                        g64_error = true;
                        g64_err_msg = ex.Message;
                    }
                }
            }

            void SetPointer(byte[] array, int trackNumber, int value)
            {
                if ((array != null && array.Length == defaultTracks << 2) && (trackNumber >= 0 && trackNumber < defaultTracks))
                {
                    int index = trackNumber << 2;
                    byte[] offsetBytes = BitConverter.GetBytes(value);
                    Buffer.BlockCopy(offsetBytes, 0, array, index, 4);
                }
            }
        }

        void Make_D64(string path, int endTrack)
        {
            int halfTrack = tracks > 42 ? 2 : 1;
            int stop = Array.FindLastIndex(Disk.Source.Track, x => x.Format == 1);

            //int stop = Array.FindLastIndex(NDS.cbm, x => x == 1);
            int adjustedEndTrack = Math.Min(stop, endTrack + 2);
            int lastTrack = Math.Max(Math.Min(stop, adjustedEndTrack), 34);
            stop = halfTrack == 2 ? stop / halfTrack : stop;
            endTrack = Math.Max(adjustedEndTrack, (35 * halfTrack) - 1);
            int sectors = Available_Sectors.Take(lastTrack + 1).Sum(b => b);
            byte[] ID = GetDiskID(true);
            byte[] errorMap = FastArray.Init(sectors, 0x01);
            byte[] empty_sector = FastArray.Init(256, 0x01);
            empty_sector[0] = 0x4b;
            int currentSector = 0;
            int currentTrack = 0;
            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter write = new BinaryWriter(buffer))
            {
                for (int i = 0; i <= endTrack; i += halfTrack, currentTrack++)
                {
                    if (Disk.Source.Track[i].Format == 1 && Disk.G64.Track?[i].Data != null)
                    {
                        var source = new BitArray(Flip_Endian(Disk.G64.Track[i].Data));
                        for (int j = 0; j < Available_Sectors[currentTrack]; j++)
                        {
                            (byte[] sector, int errorCode, _) = GetSectorWithErrorCode(Disk.G64.Track[i].Data, j, true, ID, source);
                            errorMap[currentSector++] = (byte)errorCode;
                            write.Write((sector != null && !(errorCode == 2 || errorCode == 4)) ? sector : empty_sector);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < Available_Sectors[currentTrack]; j++)
                        {
                            write.Write(empty_sector);
                            errorMap[currentSector++] = 2;
                        }
                    }
                }
                if (errorMap.Any(e => e != 1)) write.Write(errorMap);
                try
                {
                    File.WriteAllBytes(path, buffer.ToArray());
                }
                catch { }
            }
        }
    }
}