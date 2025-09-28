using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Instrumentation;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        /// <summary>
        ///  ------------------ CBM standard GCR Encode/Decode routines --------------------- 
        /// </summary>

        private static readonly byte[] GCR_encode =
        {
            0x0a, 0x0b, 0x12, 0x13,
            0x0e, 0x0f, 0x16, 0x17,
            0x09, 0x19, 0x1a, 0x1b,
            0x0d, 0x1d, 0x1e, 0x15
        };


        private static readonly byte[] GCR_decode_high =
        {
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0x80, 0x00, 0x10, 0xff, 0xc0, 0x40, 0x50,
            0xff, 0xff, 0x20, 0x30, 0xff, 0xf0, 0x60, 0x70,
            0xff, 0x90, 0xa0, 0xb0, 0xff, 0xd0, 0xe0, 0xff
        };

        private static readonly byte[] GCR_decode_low =
        {
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0x08, 0x00, 0x01, 0xff, 0x0c, 0x04, 0x05,
            0xff, 0xff, 0x02, 0x03, 0xff, 0x0f, 0x06, 0x07,
            0xff, 0x09, 0x0a, 0x0b, 0xff, 0x0d, 0x0e, 0xff
        };

        //(byte[] decoded, bool illegal) Decode_CBM_GCR(byte[] gcr)
        (byte[] decoded, int illegal) Decode_CBM_GCR(byte[] gcr)
        {
            if (gcr == null) return (null, -1);
            byte[] plain = new byte[(gcr.Length / 5) << 2];
            int illegal = 0;
            for (int i = 0; i < gcr.Length / 5; i++)
            {
                int baseIndex = i * 5;
                byte b1 = gcr[baseIndex];
                byte b2 = gcr[baseIndex + 1];
                plain[(i << 2) + 0] = CombineNibbles((byte)(b1 >> 3), (byte)(((b1 << 2) | (b2 >> 6)) & 0x1f));
                b1 = gcr[baseIndex + 1];
                b2 = gcr[baseIndex + 2];
                plain[(i << 2) + 1] = CombineNibbles((byte)((b1 >> 1) & 0x1f), (byte)(((b1 << 4) | (b2 >> 4)) & 0x1f));
                b1 = gcr[baseIndex + 2];
                b2 = gcr[baseIndex + 3];
                plain[(i << 2) + 2] = CombineNibbles((byte)(((b1 << 1) | (b2 >> 7)) & 0x1f), (byte)((b2 >> 2) & 0x1f));
                b1 = gcr[baseIndex + 3];
                b2 = gcr[baseIndex + 4];
                plain[(i << 2) + 3] = CombineNibbles((byte)(((b1 << 3) | (b2 >> 5)) & 0x1f), (byte)(b2 & 0x1f));
            }
            return (plain, illegal); // > 6);

            byte CombineNibbles(byte hnib, byte lnib)
            {
                hnib = GCR_decode_high[hnib];
                lnib = GCR_decode_low[lnib];
                if (hnib == 0xff || lnib == 0xff)
                {
                    illegal++;
                    return 0x00;
                }
                else return (byte)(hnib | lnib);
            }
        }

        byte[] Encode_CBM_GCR(byte[] plain)
        {
            int l = plain.Length >> 2;
            byte[] gcr = new byte[l * 5];
            for (int i = 0; i < l; i++)
            {
                int baseIndex = i << 2;
                byte p1 = plain[baseIndex];
                byte p2 = plain[baseIndex + 1];
                byte p3 = plain[baseIndex + 2];
                byte p4 = plain[baseIndex + 3];
                gcr[0 + (i * 5)] = (byte)((GCR_encode[p1 >> 4] << 3) | (GCR_encode[p1 & 0x0f] >> 2));
                gcr[1 + (i * 5)] = (byte)((GCR_encode[p1 & 0x0f] << 6) | (GCR_encode[p2 >> 4] << 1) | (GCR_encode[p2 & 0x0f] >> 4));
                gcr[2 + (i * 5)] = (byte)((GCR_encode[p2 & 0x0f] << 4) | (GCR_encode[p3 >> 4] >> 1));
                gcr[3 + (i * 5)] = (byte)((GCR_encode[p3 >> 4] << 7) | (GCR_encode[p3 & 0x0f] << 2) | (GCR_encode[p4 >> 4] >> 3));
                gcr[4 + (i * 5)] = (byte)((GCR_encode[p4 >> 4] << 5) | GCR_encode[p4 & 0x0f]);
            }
            return gcr;
        }

        /// <summary>
        ///  ------------------ Early Vorpal GCR Encode/Decode routines --------------- 
        /// </summary>
        /// 
        
        Dictionary<int, byte> eVorpal_LookupTable = new Dictionary<int, byte>
        {
            { 0 , 0x00 }, { 1 , 0x01 }, { 2 , 0x00 }, { 3 , 0x01 }, { 4 , 0x02 }, { 5 , 0x03 }, { 6 , 0x02 }, { 7 , 0x03 },
            { 8 , 0x00 }, { 9 , 0x01 }, { 10 , 0x00 }, { 11 , 0x01 }, { 12 , 0x02 }, { 13 , 0x03 }, { 14 , 0x02 }, { 15 , 0x03 },
            { 32 , 0x04 }, { 33 , 0x05 }, { 34 , 0x04 }, { 35 , 0x05 }, { 36 , 0x06 }, { 37 , 0x07 }, { 38 , 0x06 }, { 39 , 0x07 },
            { 40 , 0x04 }, { 41 , 0x05 }, { 42 , 0x04 }, { 43 , 0x05 }, { 44 , 0x06 }, { 45 , 0x07 }, { 46 , 0x06 }, { 47 , 0x07 },
            { 64 , 0x08 }, { 65 , 0x09 }, { 66 , 0x08 }, { 67 , 0x09 }, { 68 , 0x0A }, { 69 , 0x0B }, { 70 , 0x0A }, { 71 , 0x0B },
            { 72 , 0x08 }, { 73 , 0x09 }, { 74 , 0x08 }, { 75 , 0x09 }, { 76 , 0x0A }, { 77 , 0x0B }, { 78 , 0x0A }, { 79 , 0x0B },
            { 96 , 0x0C }, { 97 , 0x0D }, { 98 , 0x0C }, { 99 , 0x0D }, { 100 , 0x0E }, { 101 , 0x0F }, { 102 , 0x0E }, { 103 , 0x0F },
            { 104 , 0x0C }, { 105 , 0x0D }, { 106 , 0x0C }, { 107 , 0x0D }, { 108 , 0x0E }, { 109 , 0x0F }, { 110 , 0x0E }, { 111 , 0x0F },
            { 113 , 0x00 }, { 114 , 0x01 }, { 115 , 0x02 }, { 117 , 0x03 }, { 118 , 0x04 }, { 122 , 0x05 }, { 123 , 0x06 }, { 125 , 0x07 },
            { 126 , 0x08 }, { 129 , 0x09 }, { 130 , 0x0A }, { 131 , 0x0B }, { 133 , 0x0C }, { 134 , 0x0D }, { 141 , 0x0E }, { 142 , 0x0F },
            { 145 , 0x20 }, { 146 , 0x21 }, { 147 , 0x22 }, { 149 , 0x23 }, { 150 , 0x24 }, { 154 , 0x25 }, { 155 , 0x26 }, { 157 , 0x27 },
            { 158 , 0x28 }, { 161 , 0x29 }, { 162 , 0x2A }, { 163 , 0x2B }, { 186 , 0x2C }, { 187 , 0x2D }, { 189 , 0x2E }, { 190 , 0x2F },
            { 193 , 0x40 }, { 194 , 0x41 }, { 195 , 0x42 }, { 197 , 0x43 }, { 198 , 0x44 }, { 205 , 0x45 }, { 206 , 0x46 }, { 209 , 0x47 },
            { 210 , 0x48 }, { 211 , 0x49 }, { 213 , 0x4A }, { 214 , 0x4B }, { 218 , 0x4C }, { 219 , 0x4D }, { 221 , 0x4E }, { 222 , 0x4F },
            { 225 , 0x60 }, { 226 , 0x61 }, { 227 , 0x62 }, { 229 , 0x63 }, { 241 , 0x64 }, { 242 , 0x65 }, { 243 , 0x66 }, { 245 , 0x67 },
            { 246 , 0x68 }, { 250 , 0x69 }, { 251 , 0x6A }, { 253 , 0x6B }, { 254 , 0x6C }, { 257 , 0x6D }, { 258 , 0x6E }, { 259 , 0x6F },
        };

        (byte[] sector, bool checksum) Decode_eVPL(byte[] data)
        {
            if (data == null) return (new byte[0], false);
            byte a = 0, val;
            byte[] p1 = new byte[4]; // Decode through Lookup Table (pass 1)
            byte[] p2 = new byte[4]; // Decode through Lookup Table (pass 2)
            List<byte> output = new List<byte>();
            for (int i = 0; i < (data.Length >> 2); i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    p1[j] = a ^= (byte)(eVorpal_LookupTable.TryGetValue(data[(i << 2) + j] + 0x28, out val) ? val : 0xff);   // Pass 1
                    p2[j] = (byte)(eVorpal_LookupTable.TryGetValue(a, out val) ? val : 0xff);                                // Pass 2
                }
                output.AddRange(new byte[]  // Decode 4 GCR bytes to 3 Data bytes
                {
                    (byte)(GetBits(p1[0], 0) ^ GetBits(p2[0], 2) ^ GetBits((byte)(p2[0] << 1), 4) ^ GetBits(p1[1], 6)),
                    (byte)(GetBits(p2[1], 0) ^ GetBits((byte)(p2[1] << 1), 2) ^ GetBits(p1[2], 4) ^ GetBits(p2[2], 6)),
                    (byte)(GetBits((byte)(p2[2] << 1), 0) ^ GetBits(p1[3], 2) ^ GetBits(p2[3], 4) ^ GetBits((byte)(p2[3] << 1), 6))
                });
            }
            return (output.ToArray(), data.Length >= 321 && (a == (byte)(eVorpal_LookupTable.TryGetValue(data[320] + 0x28, out val) ? val : 255)));

            byte GetBits(byte b, int bitPosition)
            {
                return (byte)(((b & 0x02) ^ (byte)((b & 0x08) >> 3)) << bitPosition);
            }
        }


        /// <summary>
        ///  ------------------ Vorpal GCR Encode/Decode routines --------------------- 
        /// </summary>

        private static readonly byte[] VPL_encode = new byte[16]
        {
            0x09, 0x0A, 0x0B, 0x0D,
            0x0E, 0x0F, 0x12, 0x13,
            0x15, 0x16, 0x17, 0x19,
            0x1A, 0x1B, 0x1D, 0x1E
        };

        private static readonly byte[] VPL_decode_low =
        {
            0xff, 0xff, 0xff, 0xff, 0xff, 0x0e, 0x0f, 0xff,
            0xff, 0x00, 0x01, 0x02, 0x05, 0x03, 0x04, 0x05,
            0xff, 0xff, 0x06, 0x07, 0x0a, 0x08, 0x09, 0x0a,
            0xff, 0x0b, 0x0c, 0x0d, 0xff, 0x0e, 0x0f, 0xff,
        };

        private static readonly byte[] VPL_decode_high =
        {
            0xff, 0xff, 0xff, 0xff, 0xff, 0xe0, 0xf0, 0xff,
            0xff, 0x00, 0x10, 0x20, 0x50, 0x30, 0x40, 0x50,
            0xff, 0xff, 0x60, 0x70, 0xa0, 0x80, 0x90, 0xa0,
            0xff, 0xb0, 0xc0, 0xd0, 0xff, 0xe0, 0xf0, 0xff,
        };

        byte CombineNibbles_VPL(byte highNibble, byte lowNibble)
        {
            if (highNibble == 0xff || lowNibble == 0xff) return 0x00;
            else return (byte)(highNibble | lowNibble);
        }

        byte[] Decode_Vorpal_GCR(byte[] gcr)
        {
            byte[] plain = new byte[(gcr.Length / 5) << 2];
            for (int i = 0; i < gcr.Length / 5; i++)
            {
                int baseIndex = i * 5;
                byte b1 = gcr[baseIndex];
                byte b2 = gcr[baseIndex + 1];
                plain[(i << 2) + 0] = CombineNibbles_VPL(VPL_decode_high[b1 >> 3], VPL_decode_low[((b1 << 2) | (b2 >> 6)) & 0x1f]);
                b1 = gcr[baseIndex + 1];
                b2 = gcr[baseIndex + 2];
                plain[(i << 2) + 1] = CombineNibbles_VPL(VPL_decode_high[(b1 >> 1) & 0x1f], VPL_decode_low[((b1 << 4) | (b2 >> 4)) & 0x1f]);
                b1 = gcr[baseIndex + 2];
                b2 = gcr[baseIndex + 3];
                plain[(i << 2) + 2] = CombineNibbles_VPL(VPL_decode_high[((b1 << 1) | (b2 >> 7)) & 0x1f], VPL_decode_low[(b2 >> 2) & 0x1f]);
                b1 = gcr[baseIndex + 3];
                b2 = gcr[baseIndex + 4];
                plain[(i << 2) + 3] = CombineNibbles_VPL(VPL_decode_high[((b1 << 3) | (b2 >> 5)) & 0x1f], VPL_decode_low[b2 & 0x1f]);
            }
            return plain;
        }
        BitArray Encode_Vorpal_GCR(byte[] sector, bool Calculate_Checksum, bool nextBit)
        {
            if (sector == null) return null;
            int index = 0, checksum = 0;
            if (Calculate_Checksum)
            {
                foreach (byte b in sector) checksum ^= b;
                sector = ArrayConcat(sector, new byte[] { (byte)checksum });
            }
            byte[] nybl = new byte[sector.Length << 1];
            for (int i = 0; i < sector.Length; i++)
            {
                nybl[index++] = VPL_encode[(sector[i] >> 4) & 0x0F];
                nybl[index++] = VPL_encode[sector[i] & 0x0F];
            }
            BitArray encoded = new BitArray(sector.Length * 10);
            for (int i = 0; i < nybl.Length; i++)
            {
                index = i * 5;
                if (nybl[i] == 0x0f && (i < nybl.Length - 1 && (nybl[i + 1] & 0x10) != 0 || i == nybl.Length - 1 && nextBit)) nybl[i] = 0x0c;
                if (nybl[i] == 0x17 && (i < nybl.Length - 1 && (nybl[i + 1] & 0x10) != 0 || i == nybl.Length - 1 && nextBit)) nybl[i] = 0x14;
                if (nybl[i] == 0x1d && i > 0 && (nybl[i - 1] & 0x01) != 0) nybl[i] = 0x05;
                if (nybl[i] == 0x1e && i > 0 && (nybl[i - 1] & 0x01) != 0) nybl[i] = 0x06;
                for (int j = 0; j < 5; j++) encoded[index + (4 - j)] = (nybl[i] & (1 << j)) != 0;
            }
            return encoded;
        }

        /// <summary>
        ///  ------------------ RapidLok GCR Encode/Decode routines --------------------- 
        /// </summary>

        private static readonly byte[] RapidLok_Decode_Low =
        {
            0x0f, 0x07, 0x0d, 0x05,
            0x0b, 0x03, 0x09, 0x01,
            0x0e, 0x06, 0x0c, 0x04,
            0x0a, 0x02, 0x08, 0x00
        };

        private static readonly byte[] RapidLok_Decode_High =
        {
            0xf0, 0x70, 0xd0, 0x50,
            0xb0, 0x30, 0x90, 0x10,
            0xe0, 0x60, 0xc0, 0x40,
            0xa0, 0x20, 0x80, 0x00
        };

        (byte[] sector, bool checksum, bool version) Decode_RL_Data(byte[] sector)
        {
            if (sector == null) return (new byte[0], false, false);
            int pos = sector[0] == 0x6b ? 1 : 0;
            bool rl_v2_7 = (sector.Length == 583 && sector[195 + pos] == 0xa4);
            byte b1, b2, b3;
            byte dec0 = 0, dec1;

            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter write = new BinaryWriter(buffer))
            {
                while (pos < sector.Length)
                {
                    try
                    {
                        if (pos == 196 && rl_v2_7 && sector[pos] == 0xa4) pos++; // < 300
                        b1 = sector[pos++];
                        b2 = sector[pos++];
                        b3 = pos < sector.Length ? sector[pos++] : (byte)0;
                        dec0 = rl_v2_7
                            ? (byte)((b1 & 0x49) | (0xb6 & b2))         // rapidlok v2-7
                            : (byte)~(((b1 & 0x60) << 1) | (b1 & 0x0c) << 2 | (b1 & 0x01) << 3 | (b2 & 0x80) >> 5 | (b2 & 0x30) >> 4); // version 1
                        dec1 = rl_v2_7 && b3 != 0
                            ? dec1 = (byte)((0xdb & b3) | (b1 & 0x24))  // rapidlok v2-7
                            : (byte)~(((b2 & 0x06) << 5) | (b3 & 0xc0) >> 2 | (b3 & 0x18) >> 1 | (b3 & 0x03)); // version 1
                        if (b3 != 0)
                        {
                            write.Write(dec0);
                            write.Write(dec1);
                        }
                    }
                    catch { }
                }
                return (buffer.ToArray(), rl_v2_7 ? RL2_7_Checksum(buffer.ToArray(), dec0) : RL1_Checksum(sector), rl_v2_7);
            }
            bool RL2_7_Checksum(byte[] data, byte value)
            {
                int ck = 0;
                foreach (byte b in data) ck ^= b;
                ck ^= value;
                return value == ck;
            }

            bool RL1_Checksum(byte[] data)
            {
                int ck = 0;
                for (int i = 1; i < data.Length - 2; i++) ck ^= data[i];
                byte x = (byte)(data[data.Length - 2] << 3);
                // quite honestly, I don't know what I'm doing here, but it works
                byte value = (byte)(ck & 0x03 ^ ck & 0x0c ^ x & 0xc0 ^ (x & 0x18) << 1);
                return ck == value;
            }
        }

        byte[] Encode_RLK(byte[] data)
        {
            int cksm = 0;
            RL_Decrypt(data);
            foreach (byte d in data) cksm ^= d;
            MemoryStream buffer = new MemoryStream();
            BinaryWriter write = new BinaryWriter(buffer);
            int pos = 0;
            write.Write((byte)0x6b);
            while (pos < data.Length)
            {
                write.Write(Encode(data[pos++], data[pos++]));
                if (buffer.Length == 196) write.Write((byte)0xa4);
            }
            write.Write(CopyFrom(Encode((byte)cksm, 0), 0, 2));
            return buffer.ToArray();

            byte[] Encode(byte b1, byte b2)
            {
                byte GCR_a = (byte)(0x92 | ((byte)((b1 & 0x49) | (b2 & 0x24))));
                byte GCR_b = (byte)(0x49 | (b1 & 0xb6));
                byte GCR_c = (byte)(0x24 | (b2 & 0xdb));
                /// Check to make sure GCR is valid (can't have too many '1' bits in a row)
                if ((GCR_a & 0x03) == 0x03 && (GCR_b & 0xE0) == 0xE0) GCR_b &= 0xBF;
                if ((GCR_c & 0x80) == 0x80 && (GCR_b & 0x07) == 0x07) GCR_b &= 0xFE;
                if ((GCR_a & 0xF8) == 0xF8) GCR_a &= 0xEF;
                if ((GCR_a & 0x3F) == 0x3F && (GCR_b & 0xC0) == 0xC0) GCR_a &= 0xFE;
                if ((GCR_c & 0x3E) == 0x3E) GCR_c &= 0xFB;
                return new byte[] { GCR_a, GCR_b, GCR_c };
            }
        }

        /// <summary>
        ///  ------------------ V-Max GCR Decode routines --------------------- 
        /// </summary>
        /// 

        // This is the decoding table used by the GCR decoding routine for V-Max (credit to Lord Crass and Revolution-V)
        //byte[] vMax_decodeTable = new byte[] // Starting at $0f8e (256 bytes long)
        //{
        //    0x00, 0xA9, 0x01, 0x8D, 0xEF, 0x0F, 0xA9, 0x2B, 0x8D, 0xF0, 0x0F, 0xA0, 0x00, 0x84, 0xFE, 0x20,
        //    0xEE, 0x0F, 0x10, 0x3B, 0xBD, 0x8E, 0x0F, 0x0A, 0x0A, 0x85, 0xFF, 0x20, 0xEE, 0x0F, 0x5D, 0x8E,
        //    0x0F, 0x99, 0x00, 0x07, 0x45, 0xFE, 0x85, 0xFE, 0xA5, 0xFF, 0x0A, 0x0A, 0x85, 0xFF, 0x20, 0xEE,
        //    0x0F, 0x5D, 0x8E, 0x0F, 0x99, 0x50, 0x07, 0x45, 0xFE, 0x85, 0xFE, 0xA5, 0xFF, 0x0A, 0x0A, 0x20,
        //    0xEE, 0x0F, 0x5D, 0x8E, 0x0F, 0x99, 0xA0, 0x07, 0x45, 0xFE, 0x85, 0xFE, 0xC8, 0xD0, 0xC0, 0x84,
        //    0x9F, 0x98, 0x0A, 0x18, 0x65, 0x9F, 0x85, 0x9E, 0xA5, 0xFE, 0xF0, 0x02, 0x38, 0x60, 0x18, 0x60,
        //    0xAE, 0x01, 0x2B, 0x08, 0xEE, 0xEF, 0x0F, 0xD0, 0x0C, 0x48, 0xA9, 0xA7, 0x8D, 0xEF, 0x0F, 0xA9,
        //    0x02, 0x8D, 0xF0, 0x0F, 0x68, 0x28, 0x60, 0xE6, 0xFB, 0xD0, 0x02, 0xE6, 0xFC, 0x60, 0xA6, 0xB0,
        //    0xBD, 0x00, 0x01, 0x30, 0x09, 0xE4, 0xB2, 0xB0, 0x03, 0xE8, 0xD0, 0xF4, 0x38, 0x60, 0x18, 0x60,
        //    0x00, 0x00, 0x3B, 0x3A, 0x00, 0x00, 0x3C, 0x35, 0x00, 0x39, 0x00, 0x34, 0x38, 0x33, 0x32, 0x31,
        //    0x00, 0x00, 0x00, 0x2C, 0x3E, 0x3F, 0x3D, 0x30, 0x00, 0x37, 0x36, 0x2F, 0x2E, 0x2C, 0x2D, 0x2B,
        //    0x00, 0x00, 0x22, 0x00, 0x25, 0x2A, 0x28, 0x29, 0x00, 0x24, 0x27, 0x26, 0x21, 0x23, 0x20, 0x00,
        //    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F, 0x1E, 0x1D, 0x1B, 0x1C, 0x1A, 0x19,
        //    0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x17, 0x00, 0x14, 0x00, 0x15, 0x12, 0x13, 0x11, 0x18,
        //    0x00, 0x00, 0x0A, 0x00, 0x0F, 0x10, 0x0E, 0x0D, 0x00, 0x0C, 0x0A, 0x0B, 0x08, 0x09, 0x07, 0x06,
        //    0x00, 0x00, 0x05, 0x04, 0x02, 0x03, 0x01, 0x00, 0x20, 0x0F, 0x1C, 0xA5, 0xBF, 0x30, 0x0B, 0xC9, 0x61
        //};

        byte[] Decode_VmaxGCR(byte[] rawGcr)
        {
            if (rawGcr == null) return null;
            byte bitmask;
            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter write = new BinaryWriter(buffer))
            {
                for (int i = 0; i < rawGcr.Length; i += 4)
                {
                    try
                    {
                        bitmask = (byte)(vmax_dec_table[rawGcr[i]] << 2);
                        byte decoded = (byte)(bitmask ^ vmax_dec_table[rawGcr[i + 1]]);
                        write.Write(decoded);
                        bitmask <<= 2;

                        decoded = (byte)(bitmask ^ vmax_dec_table[rawGcr[i + 2]]);
                        write.Write(decoded);
                        decoded = (byte)(bitmask << 2);

                        decoded = (byte)(decoded ^ vmax_dec_table[rawGcr[i + 3]]);
                        write.Write(decoded);
                    }
                    catch { }
                }
                return buffer.ToArray();
            }
        }

        byte[] Decode_VM_Loader_CBM(byte[] gcrTrack)
        {
            List<byte> decoded = new List<byte>();
            int pos = 0;
            byte a = 0;
            while (pos < gcrTrack.Length)
            {
                try
                {
                    a ^= (byte)(gcrTrack[pos++] ^ gcrTrack[pos++]);
                    if (pos % 257 != 0) decoded.Add(a); // pos % 514 != 0
                }
                catch { }
            }
            return decoded.ToArray();
        }

        byte[] Decode_VM_Loader(byte[] data)
        {
            if (data == null || data.Length < 2) return null;
            List<byte> result = new List<byte>();
            int sec = 0;
            for (int i = 0; i < data.Length; i++)
            {
                result.Add(data[i]);
                sec++;
                if (sec % 387 == 0) i += 2;
            }
            data = result.ToArray();
            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter write = new BinaryWriter(buffer))
            {
                try
                {
                    int pos = 0;
                    while (pos < data.Length)
                    {
                        byte fa = (byte)(data[pos++] & 0xB6);
                        write.Write((byte)((data[pos++] & 0xDB) ^ fa));
                        write.Write((byte)((data[pos++] & 0x6D) ^ fa));
                    }
                }
                catch { }
                return buffer.ToArray();
            }
        }
    }
}