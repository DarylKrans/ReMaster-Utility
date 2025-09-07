using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        private static readonly object fileLock = new object();

        private void Button2_Click(object sender, EventArgs e)
        {
            //Test_RLD();
            //Test_GetFmt();
            //Make_G64("file", tracks);

            //BuildDB();
            //OpenDB_Windows();

            databaseToolStripMenuItem.Enabled = true;

            //byte[] sector = File.ReadAllBytes($@"c:\test\vmtest\sector.bin");
            //byte[] dec = DecodeVmaxV2(sector);
            //File.WriteAllBytes($@"c:\test\vmtest\decoded.bin", dec);

            //Thread tempthread = new Thread(new ThreadStart(() =>
            //{
            //    while (true)
            //    {
            //        Thread.Sleep(20);
            //        Invoke(new Action(() =>
            //        {
            //            Text = $"undo's {undoStack.Count} redo's {redoStack.Count} {DateTime.Now}";
            //            this.Update();
            //        }));
            //    }
            //}));
            //tempthread.Start();

        }




        //byte[] DecodeVmaxV2(byte[] rawGcr)
        //{
        //    byte[] decodeTable = new byte[] // Starting at $0f8e (256 bytes long)
        //    {
        //        0x00, 0xA9, 0x01, 0x8D, 0xEF, 0x0F, 0xA9, 0x2B, 0x8D,
        //        0xF0, 0x0F, 0xA0, 0x00, 0x84, 0xFE, 0x20, 0xEE,
        //        0x0F, 0x10, 0x3B, 0xBD, 0x8E, 0x0F, 0x0A, 0x0A,
        //        0x85, 0xFF, 0x20, 0xEE, 0x0F, 0x5D, 0x8E, 0x0F,
        //        0x99, 0x00, 0x07, 0x45, 0xFE, 0x85, 0xFE, 0xA5,
        //        0xFF, 0x0A, 0x0A, 0x85, 0xFF, 0x20, 0xEE, 0x0F,
        //        0x5D, 0x8E, 0x0F, 0x99, 0x50, 0x07, 0x45, 0xFE,
        //        0x85, 0xFE, 0xA5, 0xFF, 0x0A, 0x0A, 0x20, 0xEE,
        //        0x0F, 0x5D, 0x8E, 0x0F, 0x99, 0xA0, 0x07, 0x45,
        //        0xFE, 0x85, 0xFE, 0xC8, 0xD0, 0xC0, 0x84, 0x9F,
        //        0x98, 0x0A, 0x18, 0x65, 0x9F, 0x85, 0x9E, 0xA5,
        //        0xFE, 0xF0, 0x02, 0x38, 0x60, 0x18, 0x60, 0xAE,
        //        0x01, 0x2B, 0x08, 0xEE, 0xEF, 0x0F, 0xD0, 0x0C,
        //        0x48, 0xA9, 0xA7, 0x8D, 0xEF, 0x0F, 0xA9, 0x02,
        //        0x8D, 0xF0, 0x0F, 0x68, 0x28, 0x60, 0xE6, 0xFB,
        //        0xD0, 0x02, 0xE6, 0xFC, 0x60, 0xA6, 0xB0, 0xBD,
        //        0x00, 0x01, 0x30, 0x09, 0xE4, 0xB2, 0xB0, 0x03,
        //        0xE8, 0xD0, 0xF4, 0x38, 0x60, 0x18, 0x60, 0x00,
        //        0x00, 0x3B, 0x3A, 0x00, 0x00, 0x3C, 0x35, 0x00,
        //        0x39, 0x00, 0x34, 0x38, 0x33, 0x32, 0x31, 0x00,
        //        0x00, 0x00, 0x2C, 0x3E, 0x3F, 0x3D, 0x30, 0x00,
        //        0x37, 0x36, 0x2F, 0x2E, 0x2C, 0x2D, 0x2B, 0x00,
        //        0x00, 0x22, 0x00, 0x25, 0x2A, 0x28, 0x29, 0x00,
        //        0x24, 0x27, 0x26, 0x21, 0x23, 0x20, 0x00, 0x00,
        //        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //        0x1F, 0x1E, 0x1D, 0x1B, 0x1C, 0x1A, 0x19, 0x00,
        //        0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x17, 0x00,
        //        0x14, 0x00, 0x15, 0x12, 0x13, 0x11, 0x18, 0x00,
        //        0x00, 0x0A, 0x00, 0x0F, 0x10, 0x0E, 0x0D, 0x00,
        //        0x0C, 0x0A, 0x0B, 0x08, 0x09, 0x07, 0x06, 0x00,
        //        0x00, 0x05, 0x04, 0x02, 0x03, 0x01, 0x00, 0x20,
        //        0x0F, 0x1C, 0xA5, 0xBF, 0x30, 0x0B, 0xC9, 0x61
        //    };
        //
        //    byte fe = 0;
        //    byte ff = 0;
        //    byte a = 0;
        //    byte x = 0;
        //    byte y = 0;
        //    int ptr = 0;
        //    byte[] output = new byte[240];
        //    int op1 = 0;
        //    int op2 = 80;
        //    int op3 = 160;
        //
        //    byte b1 = 0;
        //    byte b2 = 0;
        //    byte b3 = 0;
        //    byte b4 = 0;
        //
        //    while (ptr < rawGcr.Length)
        //    {
        //        b1 = rawGcr[ptr++];
        //        b2 = rawGcr[ptr++];
        //        b3 = rawGcr[ptr++];
        //        b4 = rawGcr[ptr++];
        //
        //        //// byte 1
        //        x = b1;
        //        a = (byte)((decodeTable[x]) << 2);
        //        ff = a;
        //        //showregs(decodeTable[x]);
        //
        //        // byte 2
        //        x = b2;
        //        a ^= decodeTable[x];
        //        output[op1 + y] = a;
        //        a ^= fe;
        //        fe = a;
        //        a = (byte)(ff << 2);
        //        ff = a;
        //        //showregs(decodeTable[x]);
        //
        //        // byte 3
        //        x = b3;
        //        a ^= decodeTable[x];
        //        output[op2 + y] = a;
        //        a ^= fe;
        //        fe = a;
        //        a = ff;
        //        a <<= 2;
        //        //showregs(decodeTable[x]);
        //
        //        // byte 4
        //        x = b4;
        //        a ^= decodeTable[x];
        //        output[op3 + y] = a;
        //        a ^= fe;
        //        fe = a;
        //        y++;
        //        //showregs(decodeTable[x]);
        //
        //    }
        //    return output;
        //
        //    //void showregs(byte t)
        //    //{
        //    //    Text = $"{Hex_Val(new byte[] {a, x, y, fe, ff, t})}";
        //    //    Thread.Sleep(100);
        //    //}
        //}


        /// ------------------------------------------------------------------------------------------------ ///



        void Test_GetFmt()
        {
            Stopwatch sw = Stopwatch.StartNew();
            /// Run Process on single thread
            //for (int j = 0; j < 10; j++)
            //{
            //    for (int i = 0; i < tracks; i++)
            //    {
            //        int tk = tracks > 42 ? (i / 2) + 1 : i + 1;
            //        int fmt = Get_Data_Fmt2(NDS.Track_Data[i], i);
            //        //int fmt = Get_Data_Format(NDS.Track_Data[i], i);
            //        if (fmt > 0)
            //        {
            //            Text = $"track {tk} {secF[fmt]}"; // fmt.ToString();
            //            Thread.Sleep(200);
            //        }
            //    }
            //}
            ///// Run Process Treaded
            //Job = new Thread[tracks];
            //for (int j = 0; j < 1; j++)
            //{
            //    for (int i = 0; i < tracks; i++)
            //    {
            //        int x = i;
            //        Task_Limit.WaitOne();
            //        Job[i] = new Thread(new ThreadStart(() => New_Task(x)));
            //        Job[i].Start();
            //    }
            //    foreach (var thread in Job) thread?.Join();
            //}

            //var topPatterns = GetTopPatterns(NDG.Track_Data[19], 8, 80);
            //
            //foreach (var pattern in topPatterns)
            //{
            //    Console.WriteLine($"{Hex_Val(pattern.pattern)} {pattern.count}");
            //}
            sw.Stop();
            Text = sw.Elapsed.TotalMilliseconds.ToString();
        }
        
        byte[] rl1_test2(byte[] data)  // Test RapidLok v1 GCR decoding
        {
            if (data == null) return null;
            int chunks = data.Length / 3;
            int pos = 0;
            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter write = new BinaryWriter(buffer))
            {
                while (pos < chunks)
                {
                    byte b1 = data[pos * 3];
                    byte b2 = data[(pos * 3) + 1];
                    byte b3 = data[(pos++ * 3) + 2];
                    write.Write((byte)~(((b1 & 0x60) << 1) | (b1 & 0x0c) << 2 | (b1 & 0x01) << 3 | (b2 & 0x80) >> 5 | (b2 & 0x30) >> 4));
                    write.Write((byte)~(((b2 & 0x06) << 5) | (b3 & 0xc0) >> 2 | (b3 & 0x18) >> 1 | (b3 & 0x03)));
                }
                return buffer.ToArray();
            }
        }

        /// Vorpal sector modifications code
        private void Button1_Click(object sender, EventArgs e)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < tracks; i++)
            {
                if (NDS.cbm[i] == 5)
                {
                    BitArray source = new BitArray(Flip_Endian(NDG.Track_Data[i]));
                    for (int j = 0; j < NDS.sectors[i]; j++)
                    {
                        (byte[] sector, _, bool isone, int pos) = Decode_Vorpal(source, j);

                        (byte[] rawsector, _, _, _) = Decode_Vorpal(source, j, false);
                        BitArray newsec = Encode_Vorpal_GCR(sector, true, isone);
                        for (int k = 0; k < newsec.Length; k++)
                        {
                            source[pos + k] = newsec[k];
                        }
                        byte[] temp = Bit2Byte(source);

                        byte[] newsector = Bit2Byte(newsec, 0, 1290);
                        rawsector = Bit2Byte(NewBit(rawsector, 1290));
                        for (int k = 0; k < rawsector.Length; k++)
                        {
                            if (rawsector[k] != newsector[k])
                            {
                                byte g = 0;
                                for (int k2 = 0; k2 < sector.Length; k2++) g ^= sector[k2];
                                File.WriteAllBytes($@"c:\test\cg\Raw_Cg_t{i + 1}-s{j}.bin", rawsector);
                                File.WriteAllBytes($@"c:\test\cg\New_Cg_t{i + 1}-s{j}.bin", newsector);
                                break;
                            }
                        }
                        Set_Dest_Arrays(temp, i);
                    }
                }
            }
            sw.Stop();
            Text = $"{sw.Elapsed.TotalMilliseconds}";

            //var trk = tracks > 42 ? 64 : 32;
            //var sec = 14;
            //if (NDS.cbm[trk] == 5)
            //{
            //    BitArray source = new BitArray(Flip_Endian(NDG.Track_Data[trk]));
            //
            //    //void Dump()
            //    //{
            //    (byte[] sector, _, bool isone, int pos) = Decode_Vorpal(source, sec);
            //    if (sector != null)
            //    {
            //        File.WriteAllBytes($@"c:\test\cgintro_t33-s14", sector);
            //        if (File.Exists($@"c:\test\cgintro_t33-s14.bin"))
            //        {
            //            byte[] newsector = File.ReadAllBytes($@"c:\test\cgintro_t33-s14.bin");
            //
            //            BitArray newsec = Encode_Vorpal_GCR(newsector, true, isone);
            //
            //            for (int i = 0; i < newsec.Length; i++)
            //            {
            //                source[pos + i] = newsec[i];
            //            }
            //            byte[] temp = Bit2Byte(source);
            //            Set_Dest_Arrays(temp, trk);
            //        }
            //    }
            //    sec = 17;
            //    (sector, _, isone, pos) = Decode_Vorpal(source, sec);
            //    if (sector != null)
            //    {
            //        File.WriteAllBytes($@"c:\test\cgintro_t33-s17", sector);
            //        if (File.Exists($@"c:\test\cgintro_t33-s17.bin"))
            //        {
            //            byte[] newsector = File.ReadAllBytes($@"c:\test\cgintro_t33-s17.bin");
            //
            //            BitArray newsec = Encode_Vorpal_GCR(newsector, true, isone);
            //
            //            for (int i = 0; i < newsec.Length; i++)
            //            {
            //                source[pos + i] = newsec[i];
            //            }
            //            byte[] temp = Bit2Byte(source);
            //            Set_Dest_Arrays(temp, trk);
            //        }
            //    }
            //    //}
            //}
        }
    }
}