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

        void Test_GetFmt()
        {
            Stopwatch sw = Stopwatch.StartNew();

            sw.Stop();
            Text = sw.Elapsed.TotalMilliseconds.ToString();
        }

        void BinToByte_Table(string InputFile, string OutputFile, string TableName, int entriesPerLine)
        {
            byte[] tbl = File.ReadAllBytes(InputFile);
            entriesPerLine = entriesPerLine < 1 ? 1 : entriesPerLine;
            List<string> list = new List<string>
            {
                $"byte[] {TableName} = new byte[]",
                "{"
            };
            int i = 0;
            string d = "    ";
            foreach (byte b in tbl)
            {
                d += $"0x{Hex_Val(new byte[] { b })}, ";
                i++;
                if (i % entriesPerLine == 0)
                {
                    list.Add(d);
                    d = "    ";
                }
            }
            if (d != "    ") list.Add(d);
            list.Add("};");
            File.WriteAllLines(OutputFile, list.ToArray());
        }

        //void BinToDictionary(string InputFile, string OutputFile, string DictionaryName, string field1 = "int", string field2 = "byte")
        //{
        //    byte[] tbl = File.ReadAllBytes(InputFile);
        //    List<string> list = new List<string>
        //    {
        //        $"Dictionary<{field1}, {field2}> {DictionaryName} = new Dictionary<{field1}, {field2}>",
        //        "{"
        //    };
        //    int i = 0;
        //    int p = 0;
        //    string d = "    ";
        //    foreach (byte b in tbl)
        //    {
        //        if (b != 0xff)
        //        {
        //            //d += $"{{ 0x{Hex_Val(new byte[] { BitConverter.GetBytes((byte)i)[0] })}, 0x{Hex_Val(new byte[] { b })} }}, ";
        //            d += $"{{ {i} , 0x{Hex_Val(new byte[] { b })} }}, ";
        //            p++;
        //        }
        //        i++;
        //        if (p == 8)
        //        {
        //            list.Add(d);
        //            d = "    ";
        //            p = 0;
        //        }
        //    }
        //    if (p > 0) list.Add(d);
        //    list.Add("};");
        //    File.WriteAllLines(OutputFile, list.ToArray());
        //}

        void Rotate_andDump(byte[] data)
        {
            if (data == null) return;
            if (debug)
            {
                BitArray s = new BitArray(Flip_Endian(data));
                for (int i = 0; i < 8; i++)
                {
                    try
                    {
                        SaveBin(Bit2Byte(s, i), $"rotate_{i}");
                    }
                    catch { break; }
                }
            }
        }

        void BinToDictionary(string InputFile, string OutputFile, string DictionaryName, string field1 = "int", string field2 = "byte", int entriesPerLine = 8, byte[] Omit = null)
        {
            byte[] tbl = File.ReadAllBytes(InputFile);
            List<string> lines = new List<string>
            {
                $"Dictionary<{field1}, {field2}> {DictionaryName} = new Dictionary<{field1}, {field2}>",
                "{"
            };

            entriesPerLine = entriesPerLine < 1 ? 1 : entriesPerLine;
            int countInLine = 0;
            string line = "    ";

            for (int i = 0; i < tbl.Length; i++)
            {
                byte b = tbl[i];
                if (Omit == null || Omit.Length == 0 || b != Omit[0])
                {
                    string f1 = field1 == "byte" ? $"{{ 0x{Hex_Val(new byte[] { BitConverter.GetBytes((byte)i)[0] })}, " : $"{{ {i}, ";
                    string f2 = field2 == "byte" ? $"0x{Hex_Val(new byte[] { b })} }}, " : $"{b} }}, ";
                    line += f1 + f2;
                    countInLine++;
                }

                if (countInLine == entriesPerLine)
                {
                    lines.Add(line);
                    line = "    ";
                    countInLine = 0;
                }
            }

            // Add any remaining entries
            if (countInLine > 0)
                lines.Add(line.TrimEnd(' ', ','));

            lines.Add("};");
            File.WriteAllLines(OutputFile, lines);
        }

        void BinToDictionary2(string InputFile, string InputFile2, string OutputFile, string DictionaryName, string field1 = "int", string field2 = "byte", int entriesPerLine = 8, byte[] Omit = null)
        {
            byte[] tbl = File.ReadAllBytes(InputFile);
            byte[] tbl2 = File.ReadAllBytes(InputFile2);
            List<string> lines = new List<string>
            {
                $"Dictionary<{field1}, {field2}> {DictionaryName} = new Dictionary<{field1}, {field2}>",
                "{"
            };

            entriesPerLine = entriesPerLine < 1 ? 1 : entriesPerLine;
            int countInLine = 0;
            string line = "    ";

            for (int i = 0; i < tbl.Length; i++)
            {
                byte b = tbl[i];
                byte c = tbl2[i];
                if (Omit == null || Omit.Length == 0 || b != Omit[0])
                {
                    string f1 = field1 == "byte" ? $"{{ 0x{Hex_Val(new byte[] { BitConverter.GetBytes((byte)b)[0] })}, " : $"{{ {b}, ";
                    string f2 = field2 == "byte" ? $"0x{Hex_Val(new byte[] { c })} }}, " : $"{c} }}, ";
                    line += f1 + f2;
                    countInLine++;
                }

                if (countInLine == entriesPerLine)
                {
                    lines.Add(line);
                    line = "    ";
                    countInLine = 0;
                }
            }

            // Add any remaining entries
            if (countInLine > 0)
                lines.Add(line.TrimEnd(' ', ','));

            lines.Add("};");
            File.WriteAllLines(OutputFile, lines);
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