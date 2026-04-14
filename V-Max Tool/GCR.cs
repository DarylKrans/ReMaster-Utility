using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        private static readonly byte[] CBM_encode =
        {
            0x0a, 0x0b, 0x12, 0x13,
            0x0e, 0x0f, 0x16, 0x17,
            0x09, 0x19, 0x1a, 0x1b,
            0x0d, 0x1d, 0x1e, 0x15
        };

        private static readonly byte[] CBM_Decode_High =
        {
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0x80, 0x00, 0x10, 0xff, 0xc0, 0x40, 0x50,
            0xff, 0xff, 0x20, 0x30, 0xff, 0xf0, 0x60, 0x70,
            0xff, 0x90, 0xa0, 0xb0, 0xff, 0xd0, 0xe0, 0xff
        };

        private static readonly byte[] CBM_Decode_Low =
        {
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0x08, 0x00, 0x01, 0xff, 0x0c, 0x04, 0x05,
            0xff, 0xff, 0x02, 0x03, 0xff, 0x0f, 0x06, 0x07,
            0xff, 0x09, 0x0a, 0x0b, 0xff, 0x0d, 0x0e, 0xff
        };

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

        Dictionary<byte, byte> eVPL_gcrTable = new Dictionary<byte, byte> // GCR byte in, 6-bit nybble out
        {
            { 0x49, 0x00 }, { 0x56, 0x01 }, { 0x4B, 0x02 }, { 0x5A, 0x03 },
            { 0x99, 0x04 }, { 0xAA, 0x05 }, { 0x9B, 0x06 }, { 0xAD, 0x07 },
            { 0x4E, 0x08 }, { 0x5D, 0x09 }, { 0x53, 0x0A }, { 0x65, 0x0B },
            { 0x9E, 0x0C }, { 0xB2, 0x0D }, { 0xA6, 0x0E }, { 0xB5, 0x0F },
            { 0x69, 0x10 }, { 0x76, 0x11 }, { 0x6B, 0x12 }, { 0x7A, 0x13 },
            { 0xB9, 0x14 }, { 0xCE, 0x15 }, { 0xBB, 0x16 }, { 0xD3, 0x17 },
            { 0x6E, 0x18 }, { 0x92, 0x19 }, { 0x73, 0x1A }, { 0x95, 0x1B },
            { 0xC9, 0x1C }, { 0xD6, 0x1D }, { 0xCB, 0x1E }, { 0xDA, 0x1F },
            { 0x4A, 0x20 }, { 0x59, 0x21 }, { 0x4D, 0x22 }, { 0x5B, 0x23 },
            { 0x9A, 0x24 }, { 0xAB, 0x25 }, { 0x9D, 0x26 }, { 0xAE, 0x27 },
            { 0x52, 0x28 }, { 0x5E, 0x29 }, { 0x55, 0x2A }, { 0x66, 0x2B },
            { 0xA5, 0x2C }, { 0xB3, 0x2D }, { 0xA9, 0x2E }, { 0xB6, 0x2F },
            { 0x6A, 0x30 }, { 0x79, 0x31 }, { 0x6D, 0x32 }, { 0x7B, 0x33 },
            { 0xBA, 0x34 }, { 0xD2, 0x35 }, { 0xBD, 0x36 }, { 0xD5, 0x37 },
            { 0x72, 0x38 }, { 0x93, 0x39 }, { 0x75, 0x3A }, { 0x96, 0x3B },
            { 0xCA, 0x3C }, { 0xD9, 0x3D }, { 0xCD, 0x3E }, { 0xDB, 0x3F },
        };

        Dictionary<byte, byte> VMax_gcrTable = new Dictionary<byte, byte> // converts raw GCR (key) into 6-bit nybbles (value)
        {
            { 0x92, 0x3B }, { 0x93, 0x3A }, { 0x96, 0x3C }, { 0x97, 0x35 },
            { 0x99, 0x39 }, { 0x9B, 0x34 }, { 0x9C, 0x38 }, { 0x9D, 0x33 },
            { 0x9E, 0x32 }, { 0x9F, 0x31 }, { 0xA4, 0x3E }, { 0xA5, 0x3F },
            { 0xA6, 0x3D }, { 0xA7, 0x30 }, { 0xA9, 0x37 }, { 0xAA, 0x36 },
            { 0xAB, 0x2F }, { 0xAC, 0x2E }, { 0xAD, 0x2C }, { 0xAE, 0x2D },
            { 0xAF, 0x2B }, { 0xB2, 0x22 }, { 0xB4, 0x25 }, { 0xB5, 0x2A },
            { 0xB6, 0x28 }, { 0xB7, 0x29 }, { 0xB9, 0x24 }, { 0xBA, 0x27 },
            { 0xBB, 0x26 }, { 0xBC, 0x21 }, { 0xBD, 0x23 }, { 0xBE, 0x20 },
            { 0xC9, 0x1F }, { 0xCA, 0x1E }, { 0xCB, 0x1D }, { 0xCC, 0x1B },
            { 0xCD, 0x1C }, { 0xCE, 0x1A }, { 0xCF, 0x19 }, { 0xD3, 0x16 },
            { 0xD7, 0x17 }, { 0xD9, 0x14 }, { 0xDB, 0x15 }, { 0xDC, 0x12 },
            { 0xDD, 0x13 }, { 0xDE, 0x11 }, { 0xDF, 0x18 }, { 0xE4, 0x0F },
            { 0xE5, 0x10 }, { 0xE6, 0x0E }, { 0xE7, 0x0D }, { 0xE9, 0x0C },
            { 0xEA, 0x0A }, { 0xEB, 0x0B }, { 0xEC, 0x08 }, { 0xED, 0x09 },
            { 0xEE, 0x07 }, { 0xEF, 0x06 }, { 0xF2, 0x05 }, { 0xF3, 0x04 },
            { 0xF4, 0x02 }, { 0xF5, 0x03 }, { 0xF6, 0x01 }, { 0xF7, 0x00 },
            // Bytes used by older V-Max v2 (containing weak bits)
            { 0xA3, 0x2c }, { 0xE2, 0x0A}
        };

        byte[] BDS_LookUp_Table = new byte[] // Used on sector data (don't use for Loader data)
        {
             0xFF, 0xF7, 0xFD, 0xF5, 0xFB, 0xF3, 0xF9, 0xF1, 0xFE, 0xF6, 0xFC, 0xF4, 0xFA, 0xF2, 0xF8, 0xF0,
             0xEF, 0xE7, 0xED, 0xE5, 0xEB, 0xE3, 0xE9, 0xE1, 0xEE, 0xE6, 0xEC, 0xE4, 0xEA, 0xE2, 0xE8, 0xE0,
             0xDF, 0xD7, 0xDD, 0xD5, 0xDB, 0xD3, 0xD9, 0xD1, 0xDE, 0xD6, 0xDC, 0xD4, 0xDA, 0xD2, 0xD8, 0xD0,
             0xCF, 0xC7, 0xCD, 0xC5, 0xCB, 0xC3, 0xC9, 0xC1, 0xCE, 0xC6, 0xCC, 0xC4, 0xCA, 0xC2, 0xC8, 0xC0,
             0xBF, 0xB7, 0xBD, 0xB5, 0xBB, 0xB3, 0xB9, 0xB1, 0xBE, 0xB6, 0xBC, 0xB4, 0xBA, 0xB2, 0xB8, 0xB0,
             0xAF, 0xA7, 0xAD, 0xA5, 0xAB, 0xA3, 0xA9, 0xA1, 0xAE, 0xA6, 0xAC, 0xA4, 0xAA, 0xA2, 0xA8, 0xA0,
             0x9F, 0x97, 0x9D, 0x95, 0x9B, 0x93, 0x99, 0x91, 0x9E, 0x96, 0x9C, 0x94, 0x9A, 0x92, 0x98, 0x90,
             0x8F, 0x87, 0x8D, 0x85, 0x8B, 0x83, 0x89, 0x81, 0x8E, 0x86, 0x8C, 0x84, 0x8A, 0x82, 0x88, 0x80,
             0x7F, 0x77, 0x7D, 0x75, 0x7B, 0x73, 0x79, 0x71, 0x7E, 0x76, 0x7C, 0x74, 0x7A, 0x72, 0x78, 0x70,
             0x6F, 0x67, 0x6D, 0x65, 0x6B, 0x63, 0x69, 0x61, 0x6E, 0x66, 0x6C, 0x64, 0x6A, 0x62, 0x68, 0x60,
             0x5F, 0x57, 0x5D, 0x55, 0x5B, 0x53, 0x59, 0x51, 0x5E, 0x56, 0x5C, 0x54, 0x5A, 0x52, 0x58, 0x50,
             0x4F, 0x47, 0x4D, 0x45, 0x4B, 0x43, 0x49, 0x41, 0x4E, 0x46, 0x4C, 0x44, 0x4A, 0x42, 0x48, 0x40,
             0x3F, 0x37, 0x3D, 0x35, 0x3B, 0x33, 0x39, 0x31, 0x3E, 0x36, 0x3C, 0x34, 0x3A, 0x32, 0x38, 0x30,
             0x2F, 0x27, 0x2D, 0x25, 0x2B, 0x23, 0x29, 0x21, 0x2E, 0x26, 0x2C, 0x24, 0x2A, 0x22, 0x28, 0x20,
             0x1F, 0x17, 0x1D, 0x15, 0x1B, 0x13, 0x19, 0x11, 0x1E, 0x16, 0x1C, 0x14, 0x1A, 0x12, 0x18, 0x10,
             0x0F, 0x07, 0x0D, 0x05, 0x0B, 0x03, 0x09, 0x01, 0x0E, 0x06, 0x0C, 0x04, 0x0A, 0x02, 0x08, 0x00,
        };

        byte[] BDS_55 = new byte[] // First GCR pair for BossDos
        {
            0x55, 0x55, 0x57, 0x57, 0x55, 0x55, 0x57, 0x57, 0x5D, 0x5D, 0x5F, 0x5F, 0x5D, 0x5D, 0x5F, 0x5F,
            0x55, 0x55, 0x57, 0x57, 0x55, 0x55, 0x57, 0x57, 0x5D, 0x5D, 0x5F, 0x5F, 0x5D, 0x5D, 0x5F, 0x5F,
            0x75, 0x75, 0x77, 0x77, 0x75, 0x75, 0x77, 0x77, 0x7D, 0x7D, 0x7F, 0x7F, 0x7D, 0x7D, 0x7F, 0x7F,
            0x75, 0x75, 0x77, 0x77, 0x75, 0x75, 0x77, 0x77, 0x7D, 0x7D, 0x7F, 0x7F, 0x7D, 0x7D, 0x7F, 0x7F,
            0x55, 0x55, 0x57, 0x57, 0x55, 0x55, 0x57, 0x57, 0x5D, 0x5D, 0x5F, 0x5F, 0x5D, 0x5D, 0x5F, 0x5F,
            0x55, 0x55, 0x57, 0x57, 0x55, 0x55, 0x57, 0x57, 0x5D, 0x5D, 0x5F, 0x5F, 0x5D, 0x5D, 0x5F, 0x5F,
            0x75, 0x75, 0x77, 0x77, 0x75, 0x75, 0x77, 0x77, 0x7D, 0x7D, 0x7F, 0x7F, 0x7D, 0x7D, 0x7F, 0x7F,
            0x75, 0x75, 0x77, 0x77, 0x75, 0x75, 0x77, 0x77, 0x7D, 0x7D, 0x7F, 0x7F, 0x7D, 0x7D, 0x7F, 0x7F,
            0x95, 0x95, 0x97, 0x97, 0x95, 0x95, 0x97, 0x97, 0x9D, 0x9D, 0x9F, 0x9F, 0x9D, 0x9D, 0x9F, 0x9F,
            0x95, 0x95, 0x97, 0x97, 0x95, 0x95, 0x97, 0x97, 0x9D, 0x9D, 0x9F, 0x9F, 0x9D, 0x9D, 0x9F, 0x9F,
            0xB5, 0xB5, 0xB7, 0xB7, 0xB5, 0xB5, 0xB7, 0xB7, 0xBD, 0xBD, 0xBF, 0xBF, 0xBD, 0xBD, 0xBF, 0xBF,
            0xB5, 0xB5, 0xB7, 0xB7, 0xB5, 0xB5, 0xB7, 0xB7, 0xBD, 0xBD, 0xBF, 0xBF, 0xBD, 0xBD, 0xBF, 0xBF,
            0x95, 0x95, 0x97, 0x97, 0x95, 0x95, 0x97, 0x97, 0x9D, 0x9D, 0x9F, 0x9F, 0x9D, 0x9D, 0x9F, 0x9F,
            0x95, 0x95, 0x97, 0x97, 0x95, 0x95, 0x97, 0x97, 0x9D, 0x9D, 0x9F, 0x9F, 0x9D, 0x9D, 0x9F, 0x9F,
            0xB5, 0xB5, 0xB7, 0xB7, 0xB5, 0xB5, 0xB7, 0xB7, 0xBD, 0xBD, 0xBF, 0xBF, 0xBD, 0xBD, 0xBF, 0xBF,
            0xB5, 0xB5, 0xB7, 0xB7, 0xB5, 0xB5, 0xB7, 0xB7, 0xBD, 0xBD, 0xBF, 0xBF, 0xBD, 0xBD, 0xBF, 0xBF
        };

        byte[] BDS_AA = new byte[] // Second GCR pair for BossDos
        {
            0xAA, 0xAB, 0xAA, 0xAB, 0xAE, 0xAF, 0xAE, 0xAF, 0xAA, 0xAB, 0xAA, 0xAB, 0xAE, 0xAF, 0xAE, 0xAF,
            0xBA, 0xBB, 0xBA, 0xBB, 0xBE, 0xBF, 0xBE, 0xBF, 0xBA, 0xBB, 0xBA, 0xBB, 0xBE, 0xBF, 0xBE, 0xBF,
            0xAA, 0xAB, 0xAA, 0xAB, 0xAE, 0xAF, 0xAE, 0xAF, 0xAA, 0xAB, 0xAA, 0xAB, 0xAE, 0xAF, 0xAE, 0xAF,
            0xBA, 0xBB, 0xBA, 0xBB, 0xBE, 0xBF, 0xBE, 0xBF, 0xBA, 0xBB, 0xBA, 0xBB, 0xBE, 0xBF, 0xBE, 0xBF,
            0x6A, 0x6B, 0x6A, 0x6B, 0x6E, 0x6F, 0x6E, 0x6F, 0x6A, 0x6B, 0x6A, 0x6B, 0x6E, 0x6F, 0x6E, 0x6F,
            0x7A, 0x7B, 0x7A, 0x7B, 0x7E, 0x7F, 0x7E, 0x7F, 0x7A, 0x7B, 0x7A, 0x7B, 0x7E, 0x7F, 0x7E, 0x7F,
            0x6A, 0x6B, 0x6A, 0x6B, 0x6E, 0x6F, 0x6E, 0x6F, 0x6A, 0x6B, 0x6A, 0x6B, 0x6E, 0x6F, 0x6E, 0x6F,
            0x7A, 0x7B, 0x7A, 0x7B, 0x7E, 0x7F, 0x7E, 0x7F, 0x7A, 0x7B, 0x7A, 0x7B, 0x7E, 0x7F, 0x7E, 0x7F,
            0xAA, 0xAB, 0xAA, 0xAB, 0xAE, 0xAF, 0xAE, 0xAF, 0xAA, 0xAB, 0xAA, 0xAB, 0xAE, 0xAF, 0xAE, 0xAF,
            0xBA, 0xBB, 0xBA, 0xBB, 0xBE, 0xBF, 0xBE, 0xBF, 0xBA, 0xBB, 0xBA, 0xBB, 0xBE, 0xBF, 0xBE, 0xBF,
            0xAA, 0xAB, 0xAA, 0xAB, 0xAE, 0xAF, 0xAE, 0xAF, 0xAA, 0xAB, 0xAA, 0xAB, 0xAE, 0xAF, 0xAE, 0xAF,
            0xBA, 0xBB, 0xBA, 0xBB, 0xBE, 0xBF, 0xBE, 0xBF, 0xBA, 0xBB, 0xBA, 0xBB, 0xBE, 0xBF, 0xBE, 0xBF,
            0x6A, 0x6B, 0x6A, 0x6B, 0x6E, 0x6F, 0x6E, 0x6F, 0x6A, 0x6B, 0x6A, 0x6B, 0x6E, 0x6F, 0x6E, 0x6F,
            0x7A, 0x7B, 0x7A, 0x7B, 0x7E, 0x7F, 0x7E, 0x7F, 0x7A, 0x7B, 0x7A, 0x7B, 0x7E, 0x7F, 0x7E, 0x7F,
            0x6A, 0x6B, 0x6A, 0x6B, 0x6E, 0x6F, 0x6E, 0x6F, 0x6A, 0x6B, 0x6A, 0x6B, 0x6E, 0x6F, 0x6E, 0x6F,
            0x7A, 0x7B, 0x7A, 0x7B, 0x7E, 0x7F, 0x7E, 0x7F, 0x7A, 0x7B, 0x7A, 0x7B, 0x7E, 0x7F, 0x7E, 0x7F
        };

        byte[] BDS_Valid_55 = new byte[]
        {
            0x55, 0x57, 0x5D, 0x5F,
            0x75, 0x77, 0x7D, 0x7F,
            0x95, 0x97, 0x9D, 0x9F,
            0xB5, 0xB7, 0xBD, 0xBF,
        };

        byte[] BDS_Valid_AA = new byte[]
        {
            0x6A, 0x6B, 0x6E, 0x6F,
            0x7A, 0x7B, 0x7E, 0x7F,
            0xAA, 0xAB, 0xAE, 0xAF,
            0xBA, 0xBB, 0xBE, 0xBF,
        };

        /// <summary>
        ///  ------------------ CBM standard GCR Encode/Decode routines --------------------- 
        /// </summary>

        (byte[] decoded, int illegal) Decode_CBM_GCR(byte[] gcr, int type = 0)
        {
            if (gcr == null) return (null, -1);
            byte[] high, low;
            switch (type)
            {
                case 5: high = VPL_decode_high; low = VPL_decode_low; break;
                default: high = CBM_Decode_High; low = CBM_Decode_Low; break;
            }
            byte[] plain = new byte[(gcr.Length / 5) << 2];
            int illegal = 0;
            for (int i = 0; i < gcr.Length / 5; i++)
            {
                int baseIndex = i * 5;
                byte b1 = gcr[baseIndex];
                byte b2 = gcr[baseIndex + 1];
                plain[(i << 2) + 0] = CombineNibbles(ref illegal,
                    (byte)(b1 >> 3), (byte)(((b1 << 2) | (b2 >> 6)) & 0x1f), high, low);
                b1 = gcr[baseIndex + 1];
                b2 = gcr[baseIndex + 2];
                plain[(i << 2) + 1] = CombineNibbles(ref illegal,
                    (byte)((b1 >> 1) & 0x1f), (byte)(((b1 << 4) | (b2 >> 4)) & 0x1f), high, low);
                b1 = gcr[baseIndex + 2];
                b2 = gcr[baseIndex + 3];
                plain[(i << 2) + 2] = CombineNibbles(ref illegal,
                    (byte)(((b1 << 1) | (b2 >> 7)) & 0x1f), (byte)((b2 >> 2) & 0x1f), high, low);
                b1 = gcr[baseIndex + 3];
                b2 = gcr[baseIndex + 4];
                plain[(i << 2) + 3] = CombineNibbles(ref illegal,
                    (byte)(((b1 << 3) | (b2 >> 5)) & 0x1f), (byte)(b2 & 0x1f), high, low);
            }
            return (plain, illegal);
        }

        byte CombineNibbles(ref int illegal, byte hnib, byte lnib, byte[] high, byte[] low)
        {
            hnib = high[hnib];
            lnib = low[lnib];
            if (hnib == 0xff || lnib == 0xff)
            {
                illegal++;
                return 0x00;
            }
            else return (byte)(hnib | lnib);
        }

        byte[] Encode_CBM_GCR(byte[] plain) //, bool checksum = false)
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
                gcr[0 + (i * 5)] = (byte)((CBM_encode[p1 >> 4] << 3) | (CBM_encode[p1 & 0x0f] >> 2));
                gcr[1 + (i * 5)] = (byte)((CBM_encode[p1 & 0x0f] << 6) | (CBM_encode[p2 >> 4] << 1) | (CBM_encode[p2 & 0x0f] >> 4));
                gcr[2 + (i * 5)] = (byte)((CBM_encode[p2 & 0x0f] << 4) | (CBM_encode[p3 >> 4] >> 1));
                gcr[3 + (i * 5)] = (byte)((CBM_encode[p3 >> 4] << 7) | (CBM_encode[p3 & 0x0f] << 2) | (CBM_encode[p4 >> 4] >> 3));
                gcr[4 + (i * 5)] = (byte)((CBM_encode[p4 >> 4] << 5) | CBM_encode[p4 & 0x0f]);
            }
            return gcr;
        }

        /// <summary>
        ///  ------------------ Vorpal (early) GCR Encode/Decode routines --------------- 
        /// </summary>
        /// 

        (byte[] sector, bool checksum, int illegal, byte expected, byte actual) Decode_eVPL(byte[] data)
        {
            if (data == null || data.Length < 4) return (new byte[0], false, 240, 0, 0);
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
            byte actual = eVPL_gcrTable.FirstOrDefault(x => x.Value == parity).Key;
            byte expected = data.Length >= ppos ? data[ppos] : (byte)0;
            bool checksumValid = data.Length >= ppos && actual == expected;
            return (output.ToArray(), checksumValid, illegal, expected, actual);
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

        byte[] Decode_VorpalLoader(byte[] data)
        {
            if (data == null || data.Length != 513) return null;
            int pos = 1;
            List<byte> dec = new List<byte>();
            while (pos < data.Length - 1)
                dec.Add((byte)(data[pos++] ^ data[pos++]));
            return dec.ToArray();
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

        ///
        /// ------------------- BossDos GCR Encode/Decode routines ----------------------
        ///


        byte Decode_BDS_Pair(byte a, byte b)
        {
            return (byte)((a | 0x55) & (b | 0xaa));
        }

        (byte[] decoded, bool parity) Decode_BDS_GCR(byte[] data, bool sector_decode = false, bool loader = false)
        {
            if (data == null) return (null, false);
            int pairs = data.Length >> 1;
            byte parity = 0, d;
            int pos = 0;
            byte[] dec = new byte[pairs - 1];
            for (int i = 0; i < pairs - 1; i++)
            {
                parity ^= d = Decode_BDS_Pair(data[pos++], data[pos++]);
                dec[i] = sector_decode ? loader ? parity : BDS_LookUp_Table[parity] : d;
            }
            return (dec, parity == Decode_BDS_Pair(data[pos++], data[pos++]));
        }

        byte[] Encode_BDS_GCR(byte[] data, bool loader = false, bool parity = true)
        {
            List<byte> sec = new List<byte>();
            byte pty = 0;
            for (int i = 0; i < data.Length; i++)
            {
                byte b = loader ? (byte)(pty ^ data[i]) : (byte)(pty ^ BDS_LookUp_Table[data[i]]);
                pty ^= b; // loader ? data[i] : BDS_LookUp_Table[data[i]];
                sec.Add(BDS_55[b]);
                sec.Add(BDS_AA[b]);
            }
            if (parity)
            {
                sec.Add(BDS_55[pty]);
                sec.Add(BDS_AA[pty]);
            }
            return sec.ToArray();
        }


        /// <summary>
        ///  ------------------ RapidLok GCR Encode/Decode routines --------------------- 
        /// </summary>

        (byte[] sector, bool checksum, bool version) Decode_RL_Data(byte[] sector)
        {
            if (sector == null || sector.Length < 1) return (new byte[0], false, false);
            int pos = sector[0] == 0x6b ? 1 : 0;
            bool rl_v2_7 = (sector.Length == 583 && sector[195 + pos] == 0xa4);
            byte b1, b2, b3;
            byte dec0 = 0, dec1;
            byte[] sec_data = rl_v2_7 ? DecodeV2_7() : DecodeV1();
            return (sec_data, rl_v2_7 ? RL2_7_Checksum(sec_data, dec0) : RL1_Checksum(sector), rl_v2_7);

            byte[] DecodeV1()
            {
                using (MemoryStream buffer = new MemoryStream())
                using (BinaryWriter write = new BinaryWriter(buffer))
                {
                    while (pos < sector.Length - 6)
                    {
                        try
                        {
                            b1 = sector[pos++];
                            b2 = sector[pos++];
                            b3 = sector[pos++];
                            write.Write((byte)~(((b1 & 0x60) << 1) | (b1 & 0x0c) << 2 | (b1 & 0x01) << 3 | (b2 & 0x80) >> 5 | (b2 & 0x30) >> 4));
                            write.Write((byte)~(((b2 & 0x06) << 5) | (b3 & 0xc0) >> 2 | (b3 & 0x18) >> 1 | (b3 & 0x03)));
                        }
                        catch { }
                    }
                    while (pos < sector.Length - 2)
                    {
                        try
                        {
                            b1 = sector[pos++];
                            b2 = sector[pos++];
                            write.Write((byte)((b2 & 0x03) | ((b2 & 0x18) >> 1) | ((b1 & 0x18) << 3) | (b1 & 0x03) << 4));
                        }
                        catch { }
                    }
                    return buffer.ToArray();
                }
            }

            byte[] DecodeV2_7()
            {
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
                            dec0 = (byte)((b1 & 0x49) | (0xb6 & b2));         // rapidlok v2-7
                            dec1 = b3 != 0 ? (byte)((0xdb & b3) | (b1 & 0x24)) : (byte)0;  // rapidlok v2-7
                            if (b3 != 0)
                            {
                                write.Write(dec0);
                                write.Write(dec1);
                            }
                        }
                        catch { }
                    }
                    return buffer.ToArray();
                }
            }
        }

        bool RL1_Checksum(byte[] data)
        {
            if (data == null || data.Length < 2) return false;
            byte checksum = 0, parity;
            byte b = data[data.Length - 2];
            byte a = data[data.Length - 1];
            for (int i = 1; i < data.Length - 2; i++) checksum ^= data[i];
            // bits from (a) ---43-10 bits from (b) ---43-10 (bits 7,6,5 and 2 from both bytes are discarded)
            // parity byte becomes b1 b0 b4 b3 a4 a3 a1 a0
            // if 'checksum' and 'parity' are equal, the sector is valid
            parity = (byte)((a & 0x03) | ((a & 0x18) >> 1) | ((b & 0x18) << 3) | (b & 0x03) << 4);
            return checksum == parity;
        }

        bool RL2_7_Checksum(byte[] data, byte parity)
        {
            if (data == null || data.Length == 0) return false;
            byte checksum = 0;
            foreach (byte b in data) checksum ^= b;
            return (checksum ^ parity) == parity;
        }

        byte[] Encode_RLKv1(byte[] data)
        {
            byte cksm = 0, GCR_a, GCR_b, GCR_c;
            int pos = 0;
            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter write = new BinaryWriter(buffer))
            {
                write.Write((byte)0x6b);
                while (pos < data.Length - 2)
                {
                    write.Write(Encode((byte)~data[pos++], (byte)~data[pos++])); // length - 4
                }
                write.Write(encode_tail(data[pos++])); // I'm not sure what these 2 bytes are for, but are encoded
                write.Write(encode_tail(data[pos++])); // the same way as the parity. These 2 bytes are not the parity
                var temp = buffer.ToArray();
                for (int i = 1; i < temp.Length; i++) cksm ^= temp[i];
                write.Write(encode_tail(cksm)); // this is the parity byte (checksum)
                return buffer.ToArray();
            }

            byte[] Encode(byte b1, byte b2)
            {
                GCR_a = (byte)(0x92 ^ ((b1 & 0xc0) >> 1) ^ ((b1 & 0x30) >> 2) ^ (b1 & 0x08) >> 3);
                GCR_b = (byte)(0x49 ^ ((b1 & 0x04) << 5) ^ ((b1 & 0x03) << 4) ^ (b2 & 0xc0) >> 5);
                GCR_c = (byte)(0x24 ^ ((b2 & 0x30) << 2) ^ ((b2 & 0x0c) << 1) ^ (b2 & 0x03));
                return Validate_GCR(GCR_a, GCR_b, GCR_c);
            }

            byte[] encode_tail(byte t)
            {
                byte[] tail = new byte[2];
                tail[0] = (byte)(0xa4 ^ ((t & 0xc0) >> 3) | ((t & 0x30) >> 4));
                tail[1] = (byte)(0xa4 ^ ((t & 0x03) | ((t & 0x0c) << 1)));
                if ((tail[0] & 0x1e) == 0x1e) tail[0] &= 0xfb;
                if ((tail[1] & 0x1e) == 0x1e) tail[1] &= 0xfb;
                return tail;
            }
        }

        byte[] Encode_RLKv2(byte[] data, bool fully_decoded = true)
        {
            byte cksm = 0;
            int pos = 0;
            if (fully_decoded) RL_Decrypt(data);
            for (int i = 0; i < data.Length - 1; i++) cksm ^= data[i];
            data[data.Length - 1] = cksm;
            using (MemoryStream buffer = new MemoryStream())
            using (BinaryWriter write = new BinaryWriter(buffer))
            {
                write.Write((byte)0x6b);
                while (pos < data.Length)
                {
                    write.Write(Encode(data[pos++], data[pos++]));
                    if (buffer.Length == 196) write.Write((byte)0xa4);
                }
                write.Write(new byte[] { 0x55, 0x55 });
                return buffer.ToArray();
            }

            byte[] Encode(byte b1, byte b2)
            {
                byte GCR_a = (byte)(0x92 | ((byte)((b1 & 0x49) | (b2 & 0x24))));
                byte GCR_b = (byte)(0x49 | (b1 & 0xb6));
                byte GCR_c = (byte)(0x24 | (b2 & 0xdb));
                return Validate_GCR(GCR_a, GCR_b, GCR_c);
            }
        }

        byte[] Validate_GCR(byte g1, byte g2, byte g3)
        {
            /// Check to make sure GCR is valid (can't have too many '1' bits in a row)
            if ((g1 & 0x03) == 0x03 && (g2 & 0xE0) == 0xE0) g2 &= 0xBF;
            if ((g3 & 0x80) == 0x80 && (g2 & 0x07) == 0x07) g2 &= 0xFE;
            if ((g1 & 0xF8) == 0xF8) g1 &= 0xEF;
            if ((g3 & 0x3E) == 0x3E) g3 &= 0xFB;
            return new byte[] { g1, g2, g3 };
        }

        /// <summary>
        ///  ------------------ V-Max GCR Encode/Decode routines --------------------- 
        /// </summary>

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

        byte[] Decode_VmaxGCR_Linear(byte[] rawGcr)    // Decode V-Max (custom) Sectors
        {
            if (rawGcr == null) return null;
            byte aa, bb, cc;
            List<byte> output = new List<byte>();
            for (int i = 0; i < rawGcr.Length; i += 4)
            {
                try
                {
                    byte mask = (byte)(VMax_gcrTable.TryGetValue(rawGcr[i], out var val) ? val : 0xff);
                    aa = ((byte)(mask << 2 ^ (VMax_gcrTable.TryGetValue(rawGcr[i + 1], out val) ? val : 0xff)));
                    bb = ((byte)(mask << 4 ^ (VMax_gcrTable.TryGetValue(rawGcr[i + 2], out val) ? val : 0xff)));
                    cc = ((byte)(mask << 6 ^ (VMax_gcrTable.TryGetValue(rawGcr[i + 3], out val) ? val : 0xff)));
                    output.Add(cc);
                    output.Add(bb);
                    output.Add(aa);
                }
                catch { }
            }
            return output.ToArray();
        }

        byte[] Encode_VmaxGCR(byte[] data, bool Calculate_Checksum = false, bool older = false)
        {
            if (data == null || data.Length < 3) return null;
            List<byte> output = new List<byte>();
            //if (data.Length >= 240 && Calculate_Checksum) Checksum();
            if (data.Length >= 3 && Calculate_Checksum) Checksum();
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
                for (int i = 0; i < data.Length - 1; i++) checksum ^= data[i];
                data[data.Length - 1] = checksum;
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
            //for (int i = 0; i < sectors.Count; i++) File.WriteAllBytes($@"c:\test\rltest\v0l_s{i}", sectors[i].ToArray());
            return (sectors.ToArray(), Checksums.ToArray()); // return decoded sectors and if they passed parity check
        }


        byte[] Encode_VM_Loader_CBM(byte[][] data)
        {
            if (data == null || data.Length == 0) return null;

            byte[] allowedGCR = new byte[]
            {
                0xEE, 0xED, 0xEC, 0xEB, 0xEA, 0xE9, 0xE7, 0xE6, 0xE5, 0xE4, 0xDD, 0xDC, 0xDB, 0xDA, 0xD9, 0xD7,
                0xD5, 0xD4, 0xD3, 0xD2, 0xCE, 0xCD, 0xCC, 0xCB, 0xCA, 0xC9, 0xBE, 0xBD, 0xBC, 0xBB, 0xBA, 0xB9,
                0xB7, 0xB6, 0xB5, 0xB4, 0xB3, 0xAE, 0xAD, 0xAC, 0xAA, 0xA9, 0xA7, 0xA6, 0xA5, 0xA4, 0x9D, 0x9C,
                0x9B, 0x9A, 0x99, 0x97, 0x96, 0x95, 0x94, 0x93, 0x7E, 0x7D, 0x7C, 0x7B, 0x7A, 0x79, 0x77, 0x76,
                0x75, 0x74, 0x73, 0x72, 0x6E, 0x6D, 0x6C, 0x6B, 0x6A, 0x69, 0x67, 0x66, 0x65, 0x64, 0x5E, 0x5D,
                0x5C, 0x5B, 0x5A, 0x59, 0x57, 0x56, 0x55, 0x54, 0x53, 0x52, 0x4E, 0x4D, 0x4C, 0x4B, 0x4A, 0x49,
            };

            List<byte> enc = new List<byte>();
            byte parity = 0, lastGCR = 0, xor_value;

            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < data[i].Length; j++)
                {
                    xor_value = (byte)(data[i][j] ^ parity);
                    byte[] e = EncodeGCR(xor_value, allowedGCR, lastGCR);
                    enc.AddRange(e);
                    parity ^= xor_value;
                    lastGCR = e[1];
                }
                enc.AddRange(EncodeGCR(parity, allowedGCR, lastGCR));
                parity = 0;
            }
            return enc.ToArray();

            byte[] EncodeGCR(byte p, byte[] alwd, byte last)
            {
                for (int f = 0; f < alwd.Length; f++)
                {
                    byte GCR_a = alwd[f];
                    if ((last & 0x01) == 0 && (GCR_a & 0x80) == 0) continue;
                    for (int h = 0; h < alwd.Length; h++)
                    {
                        byte GCR_b = alwd[h];
                        byte lowBits = (byte)(GCR_b & 0x03);
                        if ((GCR_a & 0x01) == 0 && (GCR_b & 0x80) == 0) continue;
                        if (lowBits != 0x01 && lowBits != 0x02) continue;
                        if ((GCR_a ^ GCR_b) == p) return new byte[] { GCR_a, GCR_b };
                    }
                }
                return new byte[2];
            }
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
            //for (int i = 0; i < sectors.Count; i++) File.WriteAllBytes($@"c:\test\rltest\v3l_s{i}", sectors[i].ToArray());
            return (sectors.ToArray(), checksums.ToArray()); // return decoded sectors and if they passed parity check
        }


        byte[] Encode_VM_Loader(byte[][] data)
        {
            byte g0, g1, g2, b0, b1;
            List<byte> gcr = new List<byte>();
            for (int i = 0; i < data.Length; i++)
            {
                byte parity = 0;
                int pos = 0;
                while (pos < data[i].Length)
                {
                    parity ^= b0 = data[i][pos++];
                    parity ^= b1 = data[i][pos++];
                    g0 = (byte)(0x49 | ((b0 & 0x24) ^ (b1 & 0x92)));
                    g1 = (byte)(0x24 | ((b0 & 0xDB) ^ (g0 & 0x92)));
                    g2 = (byte)(0x92 | ((b1 & 0x6D) ^ (g0 & 0x24)));
                    gcr.AddRange(Validate(g0, g1, g2));
                }
                gcr.AddRange(EncodeParity(parity, gcr[gcr.Count - 1])); // send the last encoded GCR byte to determine the first parity byte
            }
            return gcr.ToArray();

            byte[] Validate(byte GCR_a, byte GCR_b, byte GCR_c)
            {
                if ((GCR_a & 0xe0) == 0xe0) GCR_a &= 0xbf;
                if ((GCR_a & 0x1c) == 0x1c) GCR_a &= 0xf7;
                if (((GCR_a & 0x03) == 0x03) && ((GCR_b & 0x80) == 0x80)) GCR_a &= 0xfe;
                if ((GCR_b & 0x70) == 0x70) GCR_b &= 0xdf;
                if ((GCR_b & 0x0e) == 0x0e) GCR_b &= 0xfb;
                if (((GCR_c & 0xc0) == 0xc0) && ((GCR_b & 0x01) == 0x01)) GCR_c &= 0x7f;
                if ((GCR_c & 0x07) == 0x07) GCR_c &= 0xfd;
                if ((GCR_c & 0x38) == 0x38) GCR_c &= 0xef;
                return new byte[] { GCR_a, GCR_b, GCR_c };
            }
        }

        byte[] EncodeParity(byte parity, byte lastGcr)
        {
            HashSet<byte> allowedGCR = new HashSet<byte>
            {
                0x24, 0x25, 0x26, 0x27, 0x2A, 0x2B, 0x2C, 0x2D, 0x34, 0x35, 0x36, 0x37, 0x3A, 0x3B, 0x3C, 0x3D,
                0x49, 0x4A, 0x4B, 0x4D, 0x4E, 0x4F, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x59, 0x5A, 0x5B, 0x5C,
                0x5D, 0x5E, 0x64, 0x65, 0x66, 0x67, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x72, 0x73, 0x74,
                0x75, 0x76, 0x77, 0x79, 0x7A, 0x7B, 0x92, 0x93, 0x95, 0x96, 0x99, 0x9A, 0x9B, 0x9D, 0x9E, 0xA4,
                0xA5, 0xA6, 0xA7, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7,
                0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6,
                0xD7, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xE4, 0xE5, 0xE6, 0xE7, 0xEA, 0xEB, 0xEC, 0xED, 0xEE,
                0xF2, 0xF3, 0xF5, 0xF6,
            };

            // if last encoded GCR byte ends in 0, first parity byte must start with 1; GCR ends in 1, parity must start with 0
            bool lastGcrLsb = (lastGcr & 0x01) != 0;
            Func<byte, bool> aConstraint = lastGcrLsb
                ? (Func<byte, bool>)(a => (a & 0x80) == 0)  // MSB == 0
                : (a => (a & 0x80) != 0);                   // MSB == 1

            foreach (var GCR_a in allowedGCR)
            {
                // Check a start bit
                if (!aConstraint(GCR_a) || GCR_a == parity) continue;
                bool aLsb = (GCR_a & 0x01) != 0;
                Func<byte, bool> bConstraint = aLsb ? (Func<byte, bool>)(b => (b & 0x80) == 0) : (b => (b & 0x80) != 0);
                byte GCR_b = (byte)(GCR_a ^ parity);

                // Return only if both bytes are GCR compliant and (GCR_a ^ GCR_b) = parity
                //----------------- Line commented out because it prevented Loaders with $64/46 headers from encoding ----------------
                //if (GCR_b != GCR_a && GCR_b != parity && allowedGCR.Contains(GCR_b) && bConstraint(GCR_b) && bEndConstraint(GCR_b))
                //--------------------------------------------------------------------------------------------------------------------
                if (allowedGCR.Contains(GCR_b) && bConstraint(GCR_b) && bEndConstraint(GCR_b))
                {
                    return new byte[] { GCR_a, GCR_b };
                }
            }
            return new byte[2]; // Array.Empty<byte>();

            bool bEndConstraint(byte b)
            {
                int last2 = b & 0x03;
                return last2 == 0x01 || last2 == 0x02;
            }
        }
    }
}