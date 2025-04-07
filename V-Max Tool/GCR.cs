using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        /// <summary>
        ///  ------------------ CBM standard GCR Encode/Decode routines --------------------- 
        /// </summary>

        private readonly byte[] GCR_encode =
        {
            0x0a, 0x0b, 0x12, 0x13,
            0x0e, 0x0f, 0x16, 0x17,
            0x09, 0x19, 0x1a, 0x1b,
            0x0d, 0x1d, 0x1e, 0x15
        };


        private readonly byte[] GCR_decode_high =
        {
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0x80, 0x00, 0x10, 0xff, 0xc0, 0x40, 0x50,
            0xff, 0xff, 0x20, 0x30, 0xff, 0xf0, 0x60, 0x70,
            0xff, 0x90, 0xa0, 0xb0, 0xff, 0xd0, 0xe0, 0xff
        };

        private readonly byte[] GCR_decode_low =
        {
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0x08, 0x00, 0x01, 0xff, 0x0c, 0x04, 0x05,
            0xff, 0xff, 0x02, 0x03, 0xff, 0x0f, 0x06, 0x07,
            0xff, 0x09, 0x0a, 0x0b, 0xff, 0x0d, 0x0e, 0xff
        };

        byte[] Decode_CBM_GCR(byte[] gcr)
        {
            byte[] plain = new byte[(gcr.Length / 5) << 2];
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
            return plain;

            byte CombineNibbles(byte hnib, byte lnib)
            {
                hnib = GCR_decode_high[hnib];
                lnib = GCR_decode_low[lnib];
                if (hnib == 0xff || lnib == 0xff) return 0x00;
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
        ///  ------------------ Vorpal GCR Encode/Decode routines --------------------- 
        /// </summary>

        private static readonly byte[] VPL_encode = new byte[16]
        {
            0x09, 0x0A, 0x0B, 0x0D,
            0x0E, 0x0F, 0x12, 0x13,
            0x15, 0x16, 0x17, 0x19,
            0x1A, 0x1B, 0x1D, 0x1E
        };

        private readonly byte[] VPL_decode_low =
        {
            0xff, 0xff, 0xff, 0xff, 0xff, 0x0e, 0x0f, 0xff,
            0xff, 0x00, 0x01, 0x02, 0x05, 0x03, 0x04, 0x05,
            0xff, 0xff, 0x06, 0x07, 0x0a, 0x08, 0x09, 0x0a,
            0xff, 0x0b, 0x0c, 0x0d, 0xff, 0x0e, 0x0f, 0xff,
        };

        private readonly byte[] VPL_decode_high =
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

        private readonly byte[] RapidLok_Decode_Low =
        {
            0x0f, 0x07, 0x0d, 0x05,
            0x0b, 0x03, 0x09, 0x01,
            0x0e, 0x06, 0x0c, 0x04,
            0x0a, 0x02, 0x08, 0x00
        };

        private readonly byte[] RapidLok_Decode_High =
        {
            0xf0, 0x70, 0xd0, 0x50,
            0xb0, 0x30, 0x90, 0x10,
            0xe0, 0x60, 0xc0, 0x40,
            0xa0, 0x20, 0x80, 0x00
        };

        (byte[], bool) Decode_RL_Data(byte[] sector)
        {
            if (sector == null) return (new byte[0], false);
            int pos = sector[0] == 0x6b ? 1 : 0;
            bool rl_ver = (sector.Length == 583 && sector[195 + pos] == 0xa4);
            byte GCR_a, GCR_b, GCR_c;
            byte dec0 = 0, dec1;
            List<byte> output = new List<byte>();
            while (pos < sector.Length)
            {
                if (pos < 300 && sector[pos] == 0xa4) pos++;
                GCR_a = sector[pos++];
                GCR_b = sector[pos++];
                GCR_c = pos < sector.Length ? sector[pos++] : (byte)0;
                dec0 = (byte)((0xb6 & GCR_b) + (GCR_a & 0x49));
                if (GCR_c != 0)
                {
                    dec1 = (byte)((0xdb & GCR_c) + (GCR_a & 0x24));
                    output.Add(dec0);
                    output.Add(dec1);
                }
            }
            return (output.ToArray(), rl_ver ? RL2_7_Checksum(output.ToArray(), dec0) : RL1_Checksum(sector));

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
    }
}