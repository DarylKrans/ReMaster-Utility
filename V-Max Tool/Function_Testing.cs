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

        void Bossdos(byte[] data)
        {
            if (data[0] == 0x52)
            {
                List<byte> list = new List<byte>();
                byte[] dec = Decode_CBM_GCR(CopyArray(data, 0, 10)).decoded;
                int pos = Decode_BDS_Pair(data[12], data[13]) < 3 ? 10 : 8;
                int ipos = pos;
                //int pos = 8; // skip past CBM sector header?  7 bytes wasn't enough
                //byte poo = Decode_BDS_Pair(data[12], data[13]);
                byte track = Decode_BDS_Pair(data[pos++], data[pos++]); // = track - 1
                byte sector = Decode_BDS_Pair(data[pos++], data[pos++]); // = sector
                int i;
                for (i = pos; i < 2048 + pos; i += 2) // decode 1024 pairs starting at (pos) -- sector payload
                {
                    list.Add(Decode_BDS_Pair(data[i], data[i + 1]));
                }
                byte parity = Decode_BDS_Pair(data[i++], data[i++]); // 1 pair at the end = sector parity
                byte checksum = 0;
                foreach (byte p in list) checksum ^= p; // calculate the parity
                // write in title bar (checksum, parity , checksum ^ parity)
                // checksum should = parity and checksum ^ parity shoule = 0
                Text = $"{ipos} {Hex_Val(new byte[] { track, sector, checksum, parity, (byte)(checksum ^ parity) })}, {Hex_Val(dec)}"; // verify csm and parity
                File.WriteAllBytes($@"c:\test\bd_t{track}_s{sector}_dec", list.ToArray());
            }

            //byte DecodePair(byte a, byte b)
            //{
            //    return (byte)((a | 0x55) & (b | 0xaa));
            //}
        }

        //byte Decode_BDS_Pair(byte a, byte b)
        //{
        //    return (byte)((a | 0x55) & (b | 0xaa));
        //}

        //void LDR_test(byte[] data)
        //{
        //    int pos = 1;
        //    int sl = 1538;
        //    List<string> list = new List<string>();
        //    for (int i = 0; i < 3; i++)
        //    {
        //        int pp = pos + (i * sl);
        //        (byte[] dec, bool par) = Decode_BDS_GCR(CopyArray(data, pp + i, sl), true, true);
        //        File.WriteAllBytes($@"c:\test\sec_{i}", dec);
        //        list.Add($"sector {i} : Pos : {pp} Parity ({par})");
        //    }
        //    File.WriteAllLines($@"c:\test\ldec.txt", list.ToArray());
        //}

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

        byte[] VM0_encode = new byte[16]
        {
            0x0F, 0x0A, 0x1E, 0x12,
            0x09, 0x17, 0x13, 0x1D,
            0x15, 0x19, 0x1A, 0x0D,
            0x1B, 0x16, 0x0E, 0x0B
        };

        byte[] VM0_highTable = new byte[32]
        {
            0xAE,0x00,0x02,0x02,0x2F,0x04,0x3A,0x03,
            0xFF,0x40,0x10,0xF0,0xFF,0xB0,0xE0,0x00,
            0xFF,0xFF,0x30,0x60,0xFF,0x80,0xD0,0x50,
            0xFF,0x90,0xA0,0xC0,0xFF,0x70,0x20,0xFF
        };

        byte[] VM0_lowTable = new byte[32]
        {
            0xFF,0x90,0xA0,0xC0,0xFF,0x70,0x20,0xFF,
            0xFF,0x04,0x01,0x0F,0xFF,0x0B,0x0E,0x00,
            0xFF,0xFF,0x03,0x06,0xFF,0x08,0x0D,0x05,
            0xFF,0x09,0x0A,0x0C,0xFF,0x07,0x02,0xFF
        };

        byte[] VM1_encode = new byte[16]
        {
            0x0E, 0x0A, 0x09, 0x1D,
            0x1B, 0x16, 0x1A, 0x19,
            0x13, 0x17, 0x0F, 0x1E,
            0x0D, 0x0B, 0x12, 0x15
        };

        byte[] VM1_highTable = new byte[32]
        {
            0x00,0x00,0x86,0x04,0xEE,0x03,0x3A,0x03,
            0x02,0x20,0x10,0xD0,0xFF,0xC0,0x00,0xA0,
            0xFF,0x50,0xE0,0x80,0xFF,0xF0,0x50,0x90,
            0x10,0x70,0x60,0x40,0xFF,0x30,0xB0,0xFF
        };

        byte[] VM1_lowTable = new byte[32]
        {
            0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
            0xFF,0x02,0x01,0x0D,0xFF,0x0C,0x00,0x0A,
            0xFF,0x05,0x0E,0x08,0xFF,0x0F,0x05,0x09,
            0x01,0x07,0x06,0x04,0xFF,0x03,0x0B,0xFF
        };

        byte[] Decode_VM1_GCR(byte[] gcr, bool decrypt = false)
        {
            byte[] plain = new byte[(gcr.Length / 5) << 2];
            int chunks = gcr.Length / 5;
            for (int i = 0; i < chunks; i++)
            {
                int baseIndex = i * 5;
                byte b1 = gcr[baseIndex];
                byte b2 = gcr[baseIndex + 1];
                plain[(i << 2) + 0] = CombineNibbles_PB(VM1_highTable[b1 >> 3], VM1_lowTable[((b1 << 2) | (b2 >> 6)) & 0x1f]);
                b1 = gcr[baseIndex + 1];
                b2 = gcr[baseIndex + 2];
                plain[(i << 2) + 1] = CombineNibbles_PB(VM1_highTable[(b1 >> 1) & 0x1f], VM1_lowTable[((b1 << 4) | (b2 >> 4)) & 0x1f]);
                b1 = gcr[baseIndex + 2];
                b2 = gcr[baseIndex + 3];
                plain[(i << 2) + 2] = CombineNibbles_PB(VM1_highTable[((b1 << 1) | (b2 >> 7)) & 0x1f], VM1_lowTable[(b2 >> 2) & 0x1f]);
                b1 = gcr[baseIndex + 3];
                b2 = gcr[baseIndex + 4];
                plain[(i << 2) + 3] = CombineNibbles_PB(VM1_highTable[((b1 << 3) | (b2 >> 5)) & 0x1f], VM1_lowTable[b2 & 0x1f]);
            }
            return decrypt ? Decrypt_VM0(plain) : plain;

            byte CombineNibbles_PB(byte highNibble, byte lowNibble)
            {
                if (highNibble == 0xff || lowNibble == 0xff) return 0x00;
                return (byte)(highNibble | lowNibble);
            }
        }

        byte[] Encode_VM1_GCR(byte[] plain, bool checksum, bool encrypt = false)
        {
            byte csm = 0;
            int l = plain.Length >> 2;
            if (encrypt) plain = Encrypt_VM0(plain);
            if (checksum && plain.Length >= 256)
            {
                for (int i = 1; i < 256; i++) csm ^= plain[i];
                plain[256] = csm;
            }
            byte[] gcr = new byte[l * 5];
            for (int i = 0; i < l; i++)
            {
                int baseIndex = i << 2;
                byte p1 = plain[baseIndex];
                byte p2 = plain[baseIndex + 1];
                byte p3 = plain[baseIndex + 2];
                byte p4 = plain[baseIndex + 3];
                gcr[0 + (i * 5)] = (byte)((VM1_encode[p1 >> 4] << 3) | (VM1_encode[p1 & 0x0f] >> 2));
                gcr[1 + (i * 5)] = (byte)((VM1_encode[p1 & 0x0f] << 6) | (VM1_encode[p2 >> 4] << 1) | (VM1_encode[p2 & 0x0f] >> 4));
                gcr[2 + (i * 5)] = (byte)((VM1_encode[p2 & 0x0f] << 4) | (VM1_encode[p3 >> 4] >> 1));
                gcr[3 + (i * 5)] = (byte)((VM1_encode[p3 >> 4] << 7) | (VM1_encode[p3 & 0x0f] << 2) | (VM1_encode[p4 >> 4] >> 3));
                gcr[4 + (i * 5)] = (byte)((VM1_encode[p4 >> 4] << 5) | VM1_encode[p4 & 0x0f]);
            }
            return gcr;
        }

        byte[] Decode_VM0_GCR(byte[] gcr, bool decrypt = false)
        {
            byte[] plain = new byte[(gcr.Length / 5) << 2];
            int chunks = gcr.Length / 5;
            for (int i = 0; i < chunks; i++)
            {
                int baseIndex = i * 5;
                byte b1 = gcr[baseIndex];
                byte b2 = gcr[baseIndex + 1];
                plain[(i << 2) + 0] = CombineNibbles_PB(VM0_highTable[b1 >> 3], VM0_lowTable[((b1 << 2) | (b2 >> 6)) & 0x1f]);
                b1 = gcr[baseIndex + 1];
                b2 = gcr[baseIndex + 2];
                plain[(i << 2) + 1] = CombineNibbles_PB(VM0_highTable[(b1 >> 1) & 0x1f], VM0_lowTable[((b1 << 4) | (b2 >> 4)) & 0x1f]);
                b1 = gcr[baseIndex + 2];
                b2 = gcr[baseIndex + 3];
                plain[(i << 2) + 2] = CombineNibbles_PB(VM0_highTable[((b1 << 1) | (b2 >> 7)) & 0x1f], VM0_lowTable[(b2 >> 2) & 0x1f]);
                b1 = gcr[baseIndex + 3];
                b2 = gcr[baseIndex + 4];
                plain[(i << 2) + 3] = CombineNibbles_PB(VM0_highTable[((b1 << 3) | (b2 >> 5)) & 0x1f], VM0_lowTable[b2 & 0x1f]);
            }
            return decrypt ? Decrypt_VM0(plain) : plain;

            byte CombineNibbles_PB(byte highNibble, byte lowNibble)
            {
                if (highNibble == 0xff || lowNibble == 0xff) return 0x00;
                return (byte)(highNibble | lowNibble);
            }
        }

        byte[] Encode_VM0_GCR(byte[] plain, bool checksum, bool encrypt = false)
        {
            byte csm = 0;
            int l = plain.Length >> 2;
            if (encrypt) plain = Encrypt_VM0(plain);
            if (checksum && plain.Length >= 256)
            {
                for (int i = 1; i < 256; i++) csm ^= plain[i];
                plain[256] = csm;
            }
            byte[] gcr = new byte[l * 5];
            for (int i = 0; i < l; i++)
            {
                int baseIndex = i << 2;
                byte p1 = plain[baseIndex];
                byte p2 = plain[baseIndex + 1];
                byte p3 = plain[baseIndex + 2];
                byte p4 = plain[baseIndex + 3];
                gcr[0 + (i * 5)] = (byte)((VM0_encode[p1 >> 4] << 3) | (VM0_encode[p1 & 0x0f] >> 2));
                gcr[1 + (i * 5)] = (byte)((VM0_encode[p1 & 0x0f] << 6) | (VM0_encode[p2 >> 4] << 1) | (VM0_encode[p2 & 0x0f] >> 4));
                gcr[2 + (i * 5)] = (byte)((VM0_encode[p2 & 0x0f] << 4) | (VM0_encode[p3 >> 4] >> 1));
                gcr[3 + (i * 5)] = (byte)((VM0_encode[p3 >> 4] << 7) | (VM0_encode[p3 & 0x0f] << 2) | (VM0_encode[p4 >> 4] >> 3));
                gcr[4 + (i * 5)] = (byte)((VM0_encode[p4 >> 4] << 5) | VM0_encode[p4 & 0x0f]);
            }
            return gcr;
        }

        byte[] Decrypt_VM0(byte[] data)
        {
            if (data == null) return null;
            for (int i = 4; i < data.Length; i++) data[i] = DecodePB_Data(data[i]);
            return data;
        }

        byte[] Encrypt_VM0(byte[] data)
        {
            if (data == null) return null;
            for (int i = 4; i < data.Length; i++) data[i] = EncodePB_Data(data[i]);
            return data;
        }


        static byte DecodePB_Data(byte input)
        {
            byte high = (byte)((input >> 4) & 0x0F);
            byte low = (byte)(input & 0x0F);

            byte result = 0;

            // Take high nibble bits
            result |= (byte)(((high >> 3) & 1) << 4); // bit 4
            result |= (byte)(((high >> 1) & 1) << 5); // bit 5
            result |= (byte)(((high >> 2) & 1) << 6); // bit 6
            result |= (byte)(((high >> 0) & 1) << 7); // bit 7

            // Take low nibble bits
            result |= (byte)(((low >> 3) & 1) << 0);  // bit 0
            result |= (byte)(((low >> 1) & 1) << 1);  // bit 1
            result |= (byte)(((low >> 2) & 1) << 2);  // bit 2
            result |= (byte)(((low >> 0) & 1) << 3);  // bit 3
            return result;
        }

        static byte EncodePB_Data(byte input)
        {
            byte highOut = (byte)((input >> 4) & 0x0F);
            byte lowOut = (byte)(input & 0x0F);

            byte high = 0;
            byte low = 0;

            // Rebuild high nibble
            high |= (byte)(((highOut >> 0) & 1) << 3); // bit 3
            high |= (byte)(((highOut >> 1) & 1) << 1); // bit 1
            high |= (byte)(((highOut >> 2) & 1) << 2); // bit 2
            high |= (byte)(((highOut >> 3) & 1) << 0); // bit 0

            // Rebuild low nibble
            low |= (byte)(((lowOut >> 0) & 1) << 3); // bit 3
            low |= (byte)(((lowOut >> 1) & 1) << 1); // bit 1
            low |= (byte)(((lowOut >> 2) & 1) << 2); // bit 2
            low |= (byte)(((lowOut >> 3) & 1) << 0); // bit 0

            return (byte)((high << 4) | low);
        }

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