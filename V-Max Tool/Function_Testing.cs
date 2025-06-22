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

        //void New_Task(int x)
        //{
        //    Get_Data_Fmt2(NDS.Track_Data[x], x);
        //    Task_Limit.Release();
        //}

        void Test_RLD()
        {
            byte[] key = File.ReadAllBytes($@"c:\test\pirates_key.bin");
            Text = key.Length.ToString();
            //byte[] gcr = CopyFrom(key, 3, 3);
            //byte a = GCRDecoder.DecodeGCR(gcr);
            //Text = $"{Hex_Val(new byte[] { a })}";


            //byte x = 0, y = 0, a = 0;
            byte a;
            byte b1, b2, b3;
            int pos = 0;
            List<byte> dec = new List<byte>();
            while (pos < key.Length)
            {
                b1 = key[pos++];
                b2 = key[pos++];
                b3 = key[pos++];
                a = b3;
                for (int i = 0; i < 4; i++)
                {
                    ROLL(b1, 2);
                    ROLL(b2, 2);
                    ROLL(b3, 2);
                    ROLL(a, 1);
                    ROLL(b1, 1);
                    ROLL(b2, 1);
                    ROLL(b3, 1);
                    ROLL(a, 1);
                }
                dec.Add(a);

            }
            Text = $"{dec.Count} [ {Hex_Val(dec.ToArray())} ]";
            Text = dec.Count.ToString();


            byte ROLL(byte value, int count)
            {
                //byte carry = 0;
                return (byte)((value << count) | (value >> (8 - count)));
            }

            //byte[] sector = File.ReadAllBytes($@"c:\test\rlsec11.bin");
            //int ck = 0;
            //for (int i = 1; i < sector.Length; i++) ck ^= sector[i];
            //ck ^= sector[sector.Length - 1];
            //ck ^= sector[sector.Length - 2];
            //
            //byte x = (byte)(sector[sector.Length - 2] << 3);
            //byte b24 = (byte)(ck & 0x03); // a;
            //b24 ^= (byte)(ck & 0x0c); // a;
            //b24 ^= (byte)(x & 0xc0);
            //b24 ^= (byte)((x & 0x18) << 1);
            //Text = $"{Hex_Val(new byte[] { (byte)ck, b24})}";
        }

        //void Test_RL1()
        //{
        //    byte[] sector = File.ReadAllBytes($@"c:\test\rlsec1.bin");
        //    
        //    byte and1;
        //    byte and2;
        //    byte a;
        //    byte stk1;
        //    byte g1, g2, g3;
        //    //byte d1 = 0, d2 = 0;
        //    //byte b0 = 0, b1 = 0;
        //    int pos = 1;
        //    //
        //    while (pos < sector.Length)
        //    {
        //        g1 = sector[pos++];
        //        g2 = sector[pos++];
        //        g3 = sector[pos++];
        //        and1 = (byte)(g1 << 1);
        //        and2 = ROR(g1, 3);
        //        stk1 = ROR(g2, 1);
        //        a = (byte)(stk1 >> 3);
        //        //byte x = 0x02;
        //    
        //    }
        //}

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