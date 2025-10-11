using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        ///  ------------------ Vorpal (early) GCR Encode/Decode routines --------------- 
        /// </summary>
        /// 

        Dictionary<byte, byte> eVPL_gcrTable = new Dictionary<byte, byte> // GCR byte in, 6-bit nybble out
        {
            { 0x49, 0x00 }, { 0x56, 0x01 }, { 0x4B, 0x02 }, { 0x5A, 0x03 }, { 0x99, 0x04 }, { 0xAA, 0x05 }, { 0x9B, 0x06 }, { 0xAD, 0x07 },
            { 0x4E, 0x08 }, { 0x5D, 0x09 }, { 0x53, 0x0A }, { 0x65, 0x0B }, { 0x9E, 0x0C }, { 0xB2, 0x0D }, { 0xA6, 0x0E }, { 0xB5, 0x0F },
            { 0x69, 0x10 }, { 0x76, 0x11 }, { 0x6B, 0x12 }, { 0x7A, 0x13 }, { 0xB9, 0x14 }, { 0xCE, 0x15 }, { 0xBB, 0x16 }, { 0xD3, 0x17 },
            { 0x6E, 0x18 }, { 0x92, 0x19 }, { 0x73, 0x1A }, { 0x95, 0x1B }, { 0xC9, 0x1C }, { 0xD6, 0x1D }, { 0xCB, 0x1E }, { 0xDA, 0x1F },
            { 0x4A, 0x20 }, { 0x59, 0x21 }, { 0x4D, 0x22 }, { 0x5B, 0x23 }, { 0x9A, 0x24 }, { 0xAB, 0x25 }, { 0x9D, 0x26 }, { 0xAE, 0x27 },
            { 0x52, 0x28 }, { 0x5E, 0x29 }, { 0x55, 0x2A }, { 0x66, 0x2B }, { 0xA5, 0x2C }, { 0xB3, 0x2D }, { 0xA9, 0x2E }, { 0xB6, 0x2F },
            { 0x6A, 0x30 }, { 0x79, 0x31 }, { 0x6D, 0x32 }, { 0x7B, 0x33 }, { 0xBA, 0x34 }, { 0xD2, 0x35 }, { 0xBD, 0x36 }, { 0xD5, 0x37 },
            { 0x72, 0x38 }, { 0x93, 0x39 }, { 0x75, 0x3A }, { 0x96, 0x3B }, { 0xCA, 0x3C }, { 0xD9, 0x3D }, { 0xCD, 0x3E }, { 0xDB, 0x3F },
        };

        (byte[] sector, bool checksum, int illegal) Decode_eVPL(byte[] data)
        {
            if (data == null || data.Length < 4) return (new byte[0], false, 240);
            byte[] gcr = new byte[4];
            byte parity = 0;
            int illegal = 0, chunks = data.Length >> 2, ppos = chunks << 2;
            List<byte> output = new List<byte>();
            for (int i = 0; i < chunks; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    gcr[j] = parity = (byte)(eVPL_gcrTable.TryGetValue(data[(i << 2) + j], out byte val) ? val ^ parity : 0xff);
                    if (gcr[j] == 0xff) illegal++;
                }
                output.AddRange(new byte[]
                {
                    (byte)(gcr[0] | ((gcr[1] & 0x03) << 6)),
                    (byte)(((gcr[1] >> 2) & 0x0F) | ((gcr[2] & 0x0F) << 4)),
                    (byte)(((gcr[2] >> 4) & 0x03) | (gcr[3] << 2))
                });
            }
            return (output.ToArray(), data.Length >= ppos && eVPL_gcrTable.FirstOrDefault(x => x.Value == parity).Key == data[ppos], illegal);
        }

        byte[] Encode_eVpl(byte[] data, bool full_325 = false)
        {
            if (data == null || data.Length < 3) return new byte[0];
            byte parity = 0; int EncodeLen = (data.Length / 3) * 3;
            List<byte> output = new List<byte>();
            if (full_325) output.AddRange(new byte[] { 0x55, 0xd4, 0xad });
            for (int i = 0; i < EncodeLen; i += 3)
            {
                AddOutput((byte)(data[i] & 0x3f));
                AddOutput((byte)(((data[i + 1] << 2) | (data[i] >> 6)) & 0x3f));
                AddOutput((byte)((((data[i + 1] >> 4) & 0x0f) | ((data[i + 2] & 0x03) << 4)) & 0x3f));
                AddOutput((byte)((data[i + 2] >> 2) & 0x3f));
            }
            output.Add(eVPL_gcrTable.FirstOrDefault(x => x.Value == parity).Key);
            if (full_325) output.Add(0x55);
            return output.ToArray();

            void AddOutput(byte gcr)
            {
                output.Add(eVPL_gcrTable.FirstOrDefault(x => x.Value == (byte)(gcr ^ parity)).Key);
                parity = gcr;
            }
        }

        /// <summary>
        ///  ------------------ Vorpal (newer) GCR Encode/Decode routines --------------------- 
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
        }

        bool RL2_7_Checksum(byte[] data, byte parity)
        {
            if (data == null || data.Length == 0) return false;
            byte checksum = 0;
            foreach (byte b in data) checksum ^= b;
            return (checksum ^ parity) == parity;
        }

        bool RL1_Checksum(byte[] data)
        {
            if (data == null || data.Length == 0) return false;
            byte checksum = 0, parity, a = data[data.Length - 1], b = data[data.Length - 2];
            for (int i = 1; i < data.Length - 2; i++) checksum ^= data[i];
            // bits from (a) ---43-10 bits from (b) ---43-10 (bits 7,6,5 and 2 from both bytes are discarded)
            // parity byte becomes x1 x0 x4 x3 a4 a3 a1 a0
            // if 'checksum' and 'parity' are equal, the sector is valid
            parity = (byte)((a & 0x03) | ((a & 0x18) >> 1) | ((b & 0x18) << 3) | (b & 0x03) << 4);
            return checksum == parity;
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
        ///  ------------------ V-Max GCR Encode/Decode routines --------------------- 
        /// </summary>
        /// 

        Dictionary<byte, byte> VMax_gcrTable = new Dictionary<byte, byte> // converts raw GCR (key) into 6-bit nybbles (value)
        {
            { 0x92, 0x3B }, { 0x93, 0x3A }, { 0x96, 0x3C }, { 0x97, 0x35 }, { 0x99, 0x39 }, { 0x9B, 0x34 }, { 0x9C, 0x38 }, { 0x9D, 0x33 },
            { 0x9E, 0x32 }, { 0x9F, 0x31 }, { 0xA4, 0x3E }, { 0xA5, 0x3F }, { 0xA6, 0x3D }, { 0xA7, 0x30 }, { 0xA9, 0x37 }, { 0xAA, 0x36 },
            { 0xAB, 0x2F }, { 0xAC, 0x2E }, { 0xAD, 0x2C }, { 0xAE, 0x2D }, { 0xAF, 0x2B }, { 0xB2, 0x22 }, { 0xB4, 0x25 }, { 0xB5, 0x2A },
            { 0xB6, 0x28 }, { 0xB7, 0x29 }, { 0xB9, 0x24 }, { 0xBA, 0x27 }, { 0xBB, 0x26 }, { 0xBC, 0x21 }, { 0xBD, 0x23 }, { 0xBE, 0x20 },
            { 0xC9, 0x1F }, { 0xCA, 0x1E }, { 0xCB, 0x1D }, { 0xCC, 0x1B }, { 0xCD, 0x1C }, { 0xCE, 0x1A }, { 0xCF, 0x19 }, { 0xD3, 0x16 },
            { 0xD7, 0x17 }, { 0xD9, 0x14 }, { 0xDB, 0x15 }, { 0xDC, 0x12 }, { 0xDD, 0x13 }, { 0xDE, 0x11 }, { 0xDF, 0x18 }, { 0xE4, 0x0F },
            { 0xE5, 0x10 }, { 0xE6, 0x0E }, { 0xE7, 0x0D }, { 0xE9, 0x0C }, { 0xEA, 0x0A }, { 0xEB, 0x0B }, { 0xEC, 0x08 }, { 0xED, 0x09 },
            { 0xEE, 0x07 }, { 0xEF, 0x06 }, { 0xF2, 0x05 }, { 0xF3, 0x04 }, { 0xF4, 0x02 }, { 0xF5, 0x03 }, { 0xF6, 0x01 }, { 0xF7, 0x00 },
            // Bytes used by older V-Max v2 (containing weak bits)
            { 0xA3, 0x2c }, { 0xE2, 0x0A}
        };

        byte[] Decode_VmaxGCR(byte[] rawGcr)    // Decode V-Max (custom) Sectors
        {
            if (rawGcr == null) return null;
            int chunks = rawGcr.Length >> 2, b = 0;
            byte[] output = new byte[chunks * 3];
            for (int i = 0; i < rawGcr.Length; i += 4)
            {
                try
                {
                    byte mask = (byte)(VMax_gcrTable.TryGetValue(rawGcr[i], out var val) ? val : 0xff);
                    output[b] = (byte)(mask << 2 ^ (VMax_gcrTable.TryGetValue(rawGcr[i + 1], out val) ? val : 0xff));
                    output[b + chunks] = (byte)(mask << 4 ^ (VMax_gcrTable.TryGetValue(rawGcr[i + 2], out val) ? val : 0xff));
                    output[b++ + (chunks << 1)] = (byte)(mask << 6 ^ (VMax_gcrTable.TryGetValue(rawGcr[i + 3], out val) ? val : 0xff));
                }
                catch { }
            }
            return output;
        }

        byte[] Encode_VmaxGCR(byte[] data, bool Calculate_Checksum = false, bool older = false)
        {
            if (data == null || data.Length < 3) return null;
            List<byte> output = new List<byte>();
            if (data.Length >= 240 && Calculate_Checksum) Checksum();
            int offset = (data.Length / 3), len = offset * 3;
            for (int i = 0; i < offset; i++)
            {
                byte g0 = (byte)((data[i] & 0xc0) ^ ((data[i + offset] & 0xc0) >> 2) ^ ((data[i + (offset << 1)] & 0xc0) >> 4));
                byte g1 = (byte)((data[i] ^ g0) & 0x3f);
                byte g2 = (byte)((data[i + offset] ^ (g0 << 2)) & 0x3f);
                byte g3 = (byte)((data[i + (offset << 1)] ^ (g0 << 4)) & 0x3f);
                output.AddRange(new byte[] { Encode((byte)(g0 >> 2)), Encode(g1), Encode(g2), Encode(g3) });
            }
            return output.ToArray();

            byte Encode(byte b)
            {
                // nybbles 0x2c and 0x0a use alternate GCR encoding (0xa3 and 0xe2) for older V-Max version (has weak bits) 
                if (older && b == 0x2C) return 0xA3;
                if (older && b == 0x0A) return 0xE2;
                // All custom sector versions of V-Max use the same table for encoding for all other bytes
                return VMax_gcrTable.FirstOrDefault(x => x.Value == b).Key; 
            }

            void Checksum()
            {
                byte checksum = 0;
                for (int i = 0; i < 239; i++) checksum ^= data[i];
                data[239] = checksum;
            }
        }

        /*
                      In memory of Sage. (7/2/14 - 9/15/25)  Rest in peace.

                $$$$$$$$$$$$$$$$x;;X$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                $$$$$$$$$$$$$$$;::::;$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                $$$$$$$$$$$$$$:::::::;X$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                $$$$$$$$$$$$$;:::::::::X$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                $$$$$$$$$$$X;::::::;:::;$$$$$$$$$$$$$$$$$$$$$$$$$$$Xx;:;X$$$$$$$
                $$$$$$$$$$X:::::::::::::+$$$$$$$$$$$$$$$$$$$$$$$$x;::::::$$$$$$$
                $$$$$$$$$X:::::.:::::::::X$$$$$$$$$$$$$$$$$$$$$$;::::::::x$$$$$$
                $$$$$$$$$;::::::;:;::::::;$$$$$$$$$$$$$$$$$$$$x::::::::::;$$$$$$
                $$$$$$$$$;:::::::;x;;:::::;X$$$$$$$$$$$$$$$$x:::::::::::::$$$$$$
                $$$$$$$$X;::::::::;x+;:::::;x$$$$$$$$$$$$$x:::::::::::::::x$$$$$
                $$$$$$$$+;;;::::;:+xx;;::::::+$$$$$$$$$$X;::::::::::::.::::X$$$$
                $$$$$$$x+::;::::;+;;:::.:..::x$$$$$$$x;;:::::::::;+;:::::::x$$$$
                $$$$$Xx+;;;;;;::;;x:::::::::;+xxxxX$$x::::::;;;+xx;:::.:;::x$$$$
                $$$$$x;::;;::;;;::::::::::::;;+;;+x+x+;:::::;;xx+;;:::::::;$$$$$
                $$$$$X;:::::::;;::::::::::;;xxX$XX+;;;;:::::.::xx;;:::.:::;X$$$$
                $$$$$x:::::::::::;;;;:::;;;xx$&&&x;;;;;;;:::::;+x::::::;+;;x$$$$
                $$$$X+;::::::::;;++;;;::;xxx$$&$x;;;;;;;;;;::::::;:;;;;;;:;X$$$$
                $$$$x;:;;;:::;;;+x+;;;:;xx+xX$$+;;:;:;;;;;;:::::::;;;;:;;;;xX$$$
                $$$$x::;:::;;;;;xx;;;;:;xxx++x;;:;;;;;;;;::::::::::::::;;;:x$$$$
                $$$Xx;;;;;;;;;;;;;;;;;;;;xx+x+:;:::::;;:;:::::::::::::::::;x$$$$
                $$$X+::::::::;;;xxxXxxx$xxxx+::::::::;;::::::::::::::::::;x$$$$$
                $$$X+::.:.::::;;xxx+++x$xxxx;:::::;;;:;::::::::::::::::::;x$$$$$
                $$$Xx:::::++++;+;;;x:;xXx+Xx:::::;XXxxx+;:::;:::::::::::::xX$$$$
                $$$Xx:;;xx$x++Xx;;:::;xx+Xx+:;:;;x$x++;+++:::::::::::::::+$$$$$$
                X$$$+;x$&$x;+xxx+;;;;xx+x$x;;;;:;xx;:::::;;::::::::::::::;x$$$$$
                $$Xx;+X$$x;;xxxxxxxxxx+X$xx+;;;;;;;;:::;;;;::::::::::::::;x$$$$$
                $$X;;;+xXxx+;+xxxxxxxx$$$Xx+;;+;;;;;:;;;;xx;:;+xx;:::::::;x$$$X$
                $$x;;;++x$&$$&$$$xxx$$$$Xxxx+++;;:;+;;;;;+;;;:;xXxx;:::::+xX$$$$
                $$$x+;xxX&&&$$$$xxX&&$$XXXXxxxxx;;:;;;;;++;:::;x$$$x;:::;xX$$X$$
                $$$x;;+xX$$$XxX$X&&&&$XXXxxxxxxx+;:;+xxx;;:;;:;+xXXx;:::;xx$$$X$
                $$$X+;xxXX$$XXXX$&$$$$XxxxXxx$$X+;;;+xx$xx+;++xxxxx;::::+xX$$XX$
                $&$X+;;xxx$$$$$$&&$Xxxxxx$$X$$X$Xx+;xxXXxxxx$Xxxx;;::::;+xX$$XXX
                $$$Xxx+xX$&&&&$&&$+xxxx+++++x$$$xXX$XXXXXxxXXXXxx;;::::+xXX$$XX$
                $$$$XxxX$$&&&&&&$X;::;;;;;;;;xx$X$$x$x;+Xxxx$Xxx+;;;;;+x$$$$$$XX
                $$$$$X$$$$&&&&$$$$;;;;:;..::;xXXxX$Xxxx$$$$$$$$$Xxx;;+xxXX$$XXXX
                $$$XXxxXX$&&&&$$$Xx;:::::;:;xxXXx$+;xX$$$XX$$$$$$xxxxxxx$$$$XXXX
                $$XX$$$$XX$$&&&&xxX+;;::;+++xxXXx;;x$$$XXX$$$$$$Xx+;;x+x$XXXXXXX
                $$$Xx$$X$XXX$&&&$xXXxx+xxxxxxx;:;x$X$$X$$X$$$$$Xxxx;;xxxX$XXXXXX
                $$$$XxX$XXXX$$$$&$$xxx+xxxxxxx;xXX$$$X$$$X$$$$Xxx+++xx$XX$xxxXXX
                $$$$X$Xx$$$$X$$$$x$xxxxxxxx$x+X$$$$X$$$$$$$$$XXx;+++xxxxXxxxxxXX
                $$$$$XxxxxX$$$$$$+x$Xxxxxx$x;x$$$$$$$$$$$$$$$Xxx;;xxXXxXXxXxxxXX
                $$XXXxXXXxxxXXXX$X;xxxXxx+;;X$$$$$$$$$$$$$XXX$xxx+xXXxxXXxxxxxXX
                $$xxxxx$XxxxxxX$X$$+:::::;X$$$$$$$$$$$$$$xxxX$x+xxXXxX$$XxxxxxxX
                $$xX$$X$$XXXXxxxXxxX$XX$$$X$$$$$$$$$$$$$xx+x+xxxxXxxxX$$XxxxxxXX
                          
                        Chase all those squirrels in heaven.  Go get 'em!
        */

        (byte[][] sectors, bool[] checksums) Decode_VM_Loader_CBM(byte[] data)    // Decode V-Max v0/1 (standard sectors) Loader track
        {
            if (data == null) return (new byte[0][], new bool[0]);
            int pos = 0, bpos = 0;
            byte parity = 0;
            List<bool> Checksums = new List<bool>();
            List<byte> sec_data = new List<byte>();
            List<byte[]> sectors = new List<byte[]>();
            while (pos < data.Length)
            {
                if (pos + 2 > data.Length) break;            // make sure we have 2 more bytes to process
                parity ^= (byte)(data[pos++] ^ data[pos++]); // set value of rolling parity byte (which is also the decoded byte) 
                if (bpos++ == 256)                           // 1 byte decoded per iteration from every 2 bytes GCR
                {
                    Checksums.Add(parity == 0);         // adds bool to checksums. (parity = 0, passed; parity != 0, failed) 
                    sectors.Add(sec_data.ToArray());    // add decoded sector to list of 'sectors'
                    sec_data = new List<byte>();        // clear sec_data and start over for next sector
                    bpos = 0;                           // reset byte counter to 0
                }
                else sec_data.Add(parity);  // write decoded value to the sector. (again, the rolling parity is also the decoded byte)
            }
            return (sectors.ToArray(), Checksums.ToArray()); // return decoded sectors and if they passed parity check
        }

        (byte[][] sectors, bool[] checksums) Decode_VM_Loader(byte[] data)    // Decode V-Max v2+ (custom sectors) Loader track
        {
            if (data == null || data.Length < 2) return (new byte[0][], new bool[0]);
            int pos = 0, bpos = 0;
            byte b0, b1, b2, parity = 0;
            List<bool> checksums = new List<bool>();
            List<byte> sec_data = new List<byte>();
            List<byte[]> sectors = new List<byte[]>();
            while (pos < data.Length)
            {
                if (bpos++ == 128) // 2 bytes decoded per 3 bytes GCR processed, counter of 128 = 256 decoded bytes
                {
                    sectors.Add(sec_data.ToArray()); // add completed decode of 256 byte sector to list of 'sectors'
                    sec_data = new List<byte>();     // clear list for new sector
                    if (pos + 1 < data.Length) checksums.Add((parity ^ (byte)(data[pos++] ^ data[pos++])) == 0); // verify sector parity
                    bpos = 0; parity = 0; // clear parity (which should = 0 anyway) and reset bpos (tracks length of newly decoded sector)
                }
                else
                {
                    if (pos + 2 >= data.Length) break; // make sure we have at least 3 more bytes of data to decode
                    b0 = (byte)(data[pos++] & 0xB6);
                    parity ^= b1 = (byte)((data[pos++] & 0xDB) ^ b0);
                    parity ^= b2 = (byte)((data[pos++] & 0x6D) ^ b0);
                    sec_data.AddRange(new byte[] { b1, b2 });
                }
            }
            return (sectors.ToArray(), checksums.ToArray()); // return decoded sectors and if they passed parity check
        }
    }
}