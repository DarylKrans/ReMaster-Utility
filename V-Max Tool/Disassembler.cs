using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        public class Opcode
        {
            public string Mnemonic { get; set; }         // e.g., LDA, STA, RTS
            public string AddressingMode { get; set; }   // e.g., Immediate, Absolute
            public int Length { get; set; }              // Instruction length in bytes
            public bool Absolute { get; set; }
        }
        byte[] data = new byte[0];

        public static int HexStringToDecimal(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Input cannot be empty.");

            hex = hex.Trim();

            // Remove optional "0x" prefix or "h" suffix
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            else if (hex.EndsWith("h", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(0, hex.Length - 1);

            // Parse as hexadecimal
            return int.Parse(hex, NumberStyles.HexNumber);
        }

        public static string DecimalToHex(int value, bool uppercase = true, bool prefix0x = true)
        {
            return value.ToString(uppercase ? "X" : "x");
        }

        //public Form1()
        //{
        //    InitializeComponent();
        //    this.AllowDrop = true;
        //    this.DragEnter += new DragEventHandler(Form1_DragEnter);
        //    this.DragDrop += new DragEventHandler(Form1_DragDrop);
        //    textBox1.KeyPress += textBox1_KeyPress;
        //    textBox2.KeyPress += textBox1_KeyPress;
        //    textBox3.KeyPress += textBox1_KeyPress;
        //}

        //string[] Disassemble(byte[] memory, int startAddress = 0x0000, int endAddress = -1)
        StringBuilder Disassemble(byte[] memory, int startAddress = 0x0000, int endAddress = -1)
        {
            if (endAddress < 0) endAddress = memory.Length;
            if (startAddress < 0) startAddress = 0;
            //if (startAddress >= memory.Length || startAddress >= endAddress) return new string[0];
            if (startAddress >= memory.Length || startAddress >= endAddress) return new StringBuilder();
            int offset = 0;

            //try { offset = HexStringToDecimal(textBox3.Text); } catch { offset = 0; }
            string[] abs = new string[] { "BNE", "BEQ", "BMI", "BPL", "BCC", "BCS", "BVC", "BVS", };
            Dictionary<byte, Opcode> Opcodes = new Dictionary<byte, Opcode>
            {
                // --- Official opcodes ---
                {0x00, new Opcode{Mnemonic=" BRK", AddressingMode="Implied", Length=1}},
                {0x01, new Opcode{Mnemonic=" ORA", AddressingMode="Indirect,X", Length=2}},
                {0x05, new Opcode{Mnemonic=" ORA", AddressingMode="ZeroPage", Length=2}},
                {0x06, new Opcode{Mnemonic=" ASL", AddressingMode="ZeroPage", Length=2}},
                {0x08, new Opcode{Mnemonic=" PHP", AddressingMode="Implied", Length=1}},
                {0x09, new Opcode{Mnemonic=" ORA", AddressingMode="Immediate", Length=2}},
                {0x0A, new Opcode{Mnemonic=" ASL", AddressingMode="Accumulator", Length=1}},
                {0x0D, new Opcode{Mnemonic=" ORA", AddressingMode="Absolute", Length=3}},
                {0x0E, new Opcode{Mnemonic=" ASL", AddressingMode="Absolute", Length=3}},
                {0x10, new Opcode{Mnemonic=" BPL", AddressingMode="Relative", Length=2}},
                {0x11, new Opcode{Mnemonic=" ORA", AddressingMode="Indirect,Y", Length=2}},
                {0x15, new Opcode{Mnemonic=" ORA", AddressingMode="ZeroPage,X", Length=2}},
                {0x16, new Opcode{Mnemonic=" ASL", AddressingMode="ZeroPage,X", Length=2}},
                {0x18, new Opcode{Mnemonic=" CLC", AddressingMode="Implied", Length=1}},
                {0x19, new Opcode{Mnemonic=" ORA", AddressingMode="Absolute,Y", Length=3}},
                {0x1D, new Opcode{Mnemonic=" ORA", AddressingMode="Absolute,X", Length=3}},
                {0x1E, new Opcode{Mnemonic=" ASL", AddressingMode="Absolute,X", Length=3}},
                {0x20, new Opcode{Mnemonic=" JSR", AddressingMode="Absolute", Length=3}},
                {0x21, new Opcode{Mnemonic=" AND", AddressingMode="Indirect,X", Length=2}},
                {0x24, new Opcode{Mnemonic=" BIT", AddressingMode="ZeroPage", Length=2}},
                {0x25, new Opcode{Mnemonic=" AND", AddressingMode="ZeroPage", Length=2}},
                {0x26, new Opcode{Mnemonic=" ROL", AddressingMode="ZeroPage", Length=2}},
                {0x28, new Opcode{Mnemonic=" PLP", AddressingMode="Implied", Length=1}},
                {0x29, new Opcode{Mnemonic=" AND", AddressingMode="Immediate", Length=2}},
                {0x2A, new Opcode{Mnemonic=" ROL", AddressingMode="Accumulator", Length=1}},
                {0x2C, new Opcode{Mnemonic=" BIT", AddressingMode="Absolute", Length=3}},
                {0x2D, new Opcode{Mnemonic=" AND", AddressingMode="Absolute", Length=3}},
                {0x2E, new Opcode{Mnemonic=" ROL", AddressingMode="Absolute", Length=3}},
                {0x30, new Opcode{Mnemonic=" BMI", AddressingMode="Relative", Length=2}},
                {0x31, new Opcode{Mnemonic=" AND", AddressingMode="Indirect,Y", Length=2}},
                {0x35, new Opcode{Mnemonic=" AND", AddressingMode="ZeroPage,X", Length=2}},
                {0x36, new Opcode{Mnemonic=" ROL", AddressingMode="ZeroPage,X", Length=2}},
                {0x38, new Opcode{Mnemonic=" SEC", AddressingMode="Implied", Length=1}},
                {0x39, new Opcode{Mnemonic=" AND", AddressingMode="Absolute,Y", Length=3}},
                {0x3D, new Opcode{Mnemonic=" AND", AddressingMode="Absolute,X", Length=3}},
                {0x3E, new Opcode{Mnemonic=" ROL", AddressingMode="Absolute,X", Length=3}},
                {0x40, new Opcode{Mnemonic=" RTI", AddressingMode="Implied", Length=1}},
                {0x41, new Opcode{Mnemonic=" EOR", AddressingMode="Indirect,X", Length=2}},
                {0x45, new Opcode{Mnemonic=" EOR", AddressingMode="ZeroPage", Length=2}},
                {0x46, new Opcode{Mnemonic=" LSR", AddressingMode="ZeroPage", Length=2}},
                {0x48, new Opcode{Mnemonic=" PHA", AddressingMode="Implied", Length=1}},
                {0x49, new Opcode{Mnemonic=" EOR", AddressingMode="Immediate", Length=2}},
                {0x4A, new Opcode{Mnemonic=" LSR", AddressingMode="Accumulator", Length=1}},
                {0x4C, new Opcode{Mnemonic=" JMP", AddressingMode="Absolute", Length=3}},
                {0x4D, new Opcode{Mnemonic=" EOR", AddressingMode="Absolute", Length=3}},
                {0x4E, new Opcode{Mnemonic=" LSR", AddressingMode="Absolute", Length=3}},
                {0x50, new Opcode{Mnemonic=" BVC", AddressingMode="Relative", Length=2}},
                {0x51, new Opcode{Mnemonic=" EOR", AddressingMode="Indirect,Y", Length=2}},
                {0x55, new Opcode{Mnemonic=" EOR", AddressingMode="ZeroPage,X", Length=2}},
                {0x56, new Opcode{Mnemonic=" LSR", AddressingMode="ZeroPage,X", Length=2}},
                {0x58, new Opcode{Mnemonic=" CLI", AddressingMode="Implied", Length=1}},
                {0x59, new Opcode{Mnemonic=" EOR", AddressingMode="Absolute,Y", Length=3}},
                {0x5D, new Opcode{Mnemonic=" EOR", AddressingMode="Absolute,X", Length=3}},
                {0x5E, new Opcode{Mnemonic=" LSR", AddressingMode="Absolute,X", Length=3}},
                {0x60, new Opcode{Mnemonic=" RTS", AddressingMode="Implied", Length=1}},
                {0x61, new Opcode{Mnemonic=" ADC", AddressingMode="Indirect,X", Length=2}},
                {0x65, new Opcode{Mnemonic=" ADC", AddressingMode="ZeroPage", Length=2}},
                {0x66, new Opcode{Mnemonic=" ROR", AddressingMode="ZeroPage", Length=2}},
                {0x68, new Opcode{Mnemonic=" PLA", AddressingMode="Implied", Length=1}},
                {0x69, new Opcode{Mnemonic=" ADC", AddressingMode="Immediate", Length=2}},
                {0x6A, new Opcode{Mnemonic=" ROR", AddressingMode="Accumulator", Length=1}},
                {0x6C, new Opcode{Mnemonic=" JMP", AddressingMode="Indirect", Length=3}},
                {0x6D, new Opcode{Mnemonic=" ADC", AddressingMode="Absolute", Length=3}},
                {0x6E, new Opcode{Mnemonic=" ROR", AddressingMode="Absolute", Length=3}},
                {0x70, new Opcode{Mnemonic=" BVS", AddressingMode="Relative", Length=2}},
                {0x71, new Opcode{Mnemonic=" ADC", AddressingMode="Indirect,Y", Length=2}},
                {0x75, new Opcode{Mnemonic=" ADC", AddressingMode="ZeroPage,X", Length=2}},
                {0x76, new Opcode{Mnemonic=" ROR", AddressingMode="ZeroPage,X", Length=2}},
                {0x78, new Opcode{Mnemonic=" SEI", AddressingMode="Implied", Length=1}},
                {0x79, new Opcode{Mnemonic=" ADC", AddressingMode="Absolute,Y", Length=3}},
                {0x7D, new Opcode{Mnemonic=" ADC", AddressingMode="Absolute,X", Length=3}},
                {0x7E, new Opcode{Mnemonic=" ROR", AddressingMode="Absolute,X", Length=3}},
                {0x81, new Opcode{Mnemonic=" STA", AddressingMode="Indirect,X", Length=2}},
                {0x84, new Opcode{Mnemonic=" STY", AddressingMode="ZeroPage", Length=2}},
                {0x85, new Opcode{Mnemonic=" STA", AddressingMode="ZeroPage", Length=2}},
                {0x86, new Opcode{Mnemonic=" STX", AddressingMode="ZeroPage", Length=2}},
                {0x88, new Opcode{Mnemonic=" DEY", AddressingMode="Implied", Length=1}},
                {0x8A, new Opcode{Mnemonic=" TXA", AddressingMode="Implied", Length=1}},
                {0x8C, new Opcode{Mnemonic=" STY", AddressingMode="Absolute", Length=3}},
                {0x8D, new Opcode{Mnemonic=" STA", AddressingMode="Absolute", Length=3}},
                {0x8E, new Opcode{Mnemonic=" STX", AddressingMode="Absolute", Length=3}},
                {0x90, new Opcode{Mnemonic=" BCC", AddressingMode="Relative", Length=2}},
                {0x91, new Opcode{Mnemonic=" STA", AddressingMode="Indirect,Y", Length=2}},
                {0x94, new Opcode{Mnemonic=" STY", AddressingMode="ZeroPage,X", Length=2}},
                {0x95, new Opcode{Mnemonic=" STA", AddressingMode="ZeroPage,X", Length=2}},
                {0x96, new Opcode{Mnemonic=" STX", AddressingMode="ZeroPage,Y", Length=2}},
                {0x98, new Opcode{Mnemonic=" TYA", AddressingMode="Implied", Length=1}},
                {0x99, new Opcode{Mnemonic=" STA", AddressingMode="Absolute,Y", Length=3}},
                {0x9A, new Opcode{Mnemonic=" TXS", AddressingMode="Implied", Length=1}},
                {0x9D, new Opcode{Mnemonic=" STA", AddressingMode="Absolute,X", Length=3}},
                {0xA0, new Opcode{Mnemonic=" LDY", AddressingMode="Immediate", Length=2}},
                {0xA1, new Opcode{Mnemonic=" LDA", AddressingMode="Indirect,X", Length=2}},
                {0xA2, new Opcode{Mnemonic=" LDX", AddressingMode="Immediate", Length=2}},
                {0xA4, new Opcode{Mnemonic=" LDY", AddressingMode="ZeroPage", Length=2}},
                {0xA5, new Opcode{Mnemonic=" LDA", AddressingMode="ZeroPage", Length=2}},
                {0xA6, new Opcode{Mnemonic=" LDX", AddressingMode="ZeroPage", Length=2}},
                {0xA8, new Opcode{Mnemonic=" TAY", AddressingMode="Implied", Length=1}},
                {0xA9, new Opcode{Mnemonic=" LDA", AddressingMode="Immediate", Length=2}},
                {0xAA, new Opcode{Mnemonic=" TAX", AddressingMode="Implied", Length=1}},
                {0xAC, new Opcode{Mnemonic=" LDY", AddressingMode="Absolute", Length=3}},
                {0xAD, new Opcode{Mnemonic=" LDA", AddressingMode="Absolute", Length=3}},
                {0xAE, new Opcode{Mnemonic=" LDX", AddressingMode="Absolute", Length=3}},
                {0xB0, new Opcode{Mnemonic=" BCS", AddressingMode="Relative", Length=2}},
                {0xB1, new Opcode{Mnemonic=" LDA", AddressingMode="Indirect,Y", Length=2}},
                {0xB4, new Opcode{Mnemonic=" LDY", AddressingMode="ZeroPage,X", Length=2}},
                {0xB5, new Opcode{Mnemonic=" LDA", AddressingMode="ZeroPage,X", Length=2}},
                {0xB6, new Opcode{Mnemonic=" LDX", AddressingMode="ZeroPage,Y", Length=2}},
                {0xB8, new Opcode{Mnemonic=" CLV", AddressingMode="Implied", Length=1}},
                {0xB9, new Opcode{Mnemonic=" LDA", AddressingMode="Absolute,Y", Length=3}},
                {0xBA, new Opcode{Mnemonic=" TSX", AddressingMode="Implied", Length=1}},
                {0xBC, new Opcode{Mnemonic=" LDY", AddressingMode="Absolute,X", Length=3}},
                {0xBD, new Opcode{Mnemonic=" LDA", AddressingMode="Absolute,X", Length=3}},
                {0xBE, new Opcode{Mnemonic=" LDX", AddressingMode="Absolute,Y", Length=3}},
                {0xC0, new Opcode{Mnemonic=" CPY", AddressingMode="Immediate", Length=2}},
                {0xC1, new Opcode{Mnemonic=" CMP", AddressingMode="Indirect,X", Length=2}},
                {0xC4, new Opcode{Mnemonic=" CPY", AddressingMode="ZeroPage", Length=2}},
                {0xC5, new Opcode{Mnemonic=" CMP", AddressingMode="ZeroPage", Length=2}},
                {0xC6, new Opcode{Mnemonic=" DEC", AddressingMode="ZeroPage", Length=2}},
                {0xC8, new Opcode{Mnemonic=" INY", AddressingMode="Implied", Length=1}},
                {0xC9, new Opcode{Mnemonic=" CMP", AddressingMode="Immediate", Length=2}},
                {0xCA, new Opcode{Mnemonic=" DEX", AddressingMode="Implied", Length=1}},
                {0xCC, new Opcode{Mnemonic=" CPY", AddressingMode="Absolute", Length=3}},
                {0xCD, new Opcode{Mnemonic=" CMP", AddressingMode="Absolute", Length=3}},
                {0xCE, new Opcode{Mnemonic=" DEC", AddressingMode="Absolute", Length=3}},
                {0xD0, new Opcode{Mnemonic=" BNE", AddressingMode="Relative", Length=2}},
                {0xD1, new Opcode{Mnemonic=" CMP", AddressingMode="Indirect,Y", Length=2}},
                {0xD5, new Opcode{Mnemonic=" CMP", AddressingMode="ZeroPage,X", Length=2}},
                {0xD6, new Opcode{Mnemonic=" DEC", AddressingMode="ZeroPage,X", Length=2}},
                {0xD8, new Opcode{Mnemonic=" CLD", AddressingMode="Implied", Length=1}},
                {0xD9, new Opcode{Mnemonic=" CMP", AddressingMode="Absolute,Y", Length=3}},
                {0xDD, new Opcode{Mnemonic=" CMP", AddressingMode="Absolute,X", Length=3}},
                {0xDE, new Opcode{Mnemonic=" DEC", AddressingMode="Absolute,X", Length=3}},
                {0xE0, new Opcode{Mnemonic=" CPX", AddressingMode="Immediate", Length=2}},
                {0xE1, new Opcode{Mnemonic=" SBC", AddressingMode="Indirect,X", Length=2}},
                {0xE4, new Opcode{Mnemonic=" CPX", AddressingMode="ZeroPage", Length=2}},
                {0xE5, new Opcode{Mnemonic=" SBC", AddressingMode="ZeroPage", Length=2}},
                {0xE6, new Opcode{Mnemonic=" INC", AddressingMode="ZeroPage", Length=2}},
                {0xE8, new Opcode{Mnemonic=" INX", AddressingMode="Implied", Length=1}},
                {0xE9, new Opcode{Mnemonic=" SBC", AddressingMode="Immediate", Length=2}},
                {0xEA, new Opcode{Mnemonic=" NOP", AddressingMode="Implied", Length=1}},
                {0xEC, new Opcode{Mnemonic=" CPX", AddressingMode="Absolute", Length=3}},
                {0xED, new Opcode{Mnemonic=" SBC", AddressingMode="Absolute", Length=3}},
                {0xEE, new Opcode{Mnemonic=" INC", AddressingMode="Absolute", Length=3}},
                {0xF0, new Opcode{Mnemonic=" BEQ", AddressingMode="Relative", Length=2}},
                {0xF1, new Opcode{Mnemonic=" SBC", AddressingMode="Indirect,Y", Length=2}},
                {0xF5, new Opcode{Mnemonic=" SBC", AddressingMode="ZeroPage,X", Length=2}},
                {0xF6, new Opcode{Mnemonic=" INC", AddressingMode="ZeroPage,X", Length=2}},
                {0xF8, new Opcode{Mnemonic=" SED", AddressingMode="Implied", Length=1}},
                {0xF9, new Opcode{Mnemonic=" SBC", AddressingMode="Absolute,Y", Length=3}},
                {0xFD, new Opcode{Mnemonic=" SBC", AddressingMode="Absolute,X", Length=3}},
                {0xFE, new Opcode{Mnemonic=" INC", AddressingMode="Absolute,X", Length=3}},
            
                // --- Illegal Opcodes (most common ones) ---
                {0x0B, new Opcode{Mnemonic="*ANC", AddressingMode="Immediate", Length=2}},
                {0x2B, new Opcode{Mnemonic="*ANC", AddressingMode="Immediate", Length=2}},
                {0x6B, new Opcode{Mnemonic="*ARR", AddressingMode="Immediate", Length=2}},
                {0x4B, new Opcode{Mnemonic="*ALR", AddressingMode="Immediate", Length=2}},
                {0xAB, new Opcode{Mnemonic="*LAX", AddressingMode="Immediate", Length=2}},
                {0xA3, new Opcode{Mnemonic="*LAX", AddressingMode="Indirect,X", Length=2}},
                {0xB3, new Opcode{Mnemonic="*LAX", AddressingMode="Indirect,Y", Length=2}},
                {0xA7, new Opcode{Mnemonic="*LAX", AddressingMode="ZeroPage", Length=2}},
                {0xB7, new Opcode{Mnemonic="*LAX", AddressingMode="ZeroPage,Y", Length=2}},
                {0xAF, new Opcode{Mnemonic="*LAX", AddressingMode="Absolute", Length=3}},
                {0xBF, new Opcode{Mnemonic="*LAX", AddressingMode="Absolute,Y", Length=3}},
                {0x0F, new Opcode{Mnemonic="*SLO", AddressingMode="Absolute", Length=3}},
                {0x1F, new Opcode{Mnemonic="*SLO", AddressingMode="Absolute,X", Length=3}},
                {0x1B, new Opcode{Mnemonic="*SLO", AddressingMode="Absolute,Y", Length=3}},
                {0x03, new Opcode{Mnemonic="*SLO", AddressingMode="Indirect,X", Length=2}},
                {0x13, new Opcode{Mnemonic="*SLO", AddressingMode="Indirect,Y", Length=2}},
            };
            StringBuilder list = new StringBuilder();
            int pc = startAddress;
            string b = "  ";
            int target = -1;
            while (pc < endAddress)
            {
                int pcoffset = pc + offset;
                byte opcodeByte = memory[pc];
                if (!Opcodes.TryGetValue(opcodeByte, out var opcode))
                {
                    opcode = new Opcode
                    {
                        Mnemonic = $" ???",
                        AddressingMode = "Unknown",
                        Length = 1
                    };
                }
                if (abs.Any(x => x == opcode.Mnemonic.Substring(1, 3))) opcode.Absolute = true;
                var o = opcode.AddressingMode.Substring(opcode.AddressingMode.Length - 2, 2);
                string ext = o.ToLower() == ",x" || o.ToLower() == ",y" ? o.ToLower() : string.Empty;
                byte param1 = (pc + 1 < memory.Length) ? memory[pc + 1] : (byte)0;
                byte param2 = (pc + 2 < memory.Length) ? memory[pc + 2] : (byte)0;
                target = opcode.Absolute ? (pc + 2) + (sbyte)param1 : -1;
                string paramString = string.Empty;
                bool im = opcode.AddressingMode.ToLower().Contains("immediate");
                bool ind = opcode.AddressingMode.ToLower().Contains("indirect");
                string prefix = im ? "#" : "$";
                string openpar = ind && ext != string.Empty && prefix == "$" ? "(" : string.Empty;
                string clospar = ind && ext != string.Empty && prefix == "$" ? ")" : string.Empty;
                switch (opcode.Length)
                {
                    case 2: paramString = $"{openpar}{prefix}{param1:X2}{clospar}{ext}"; break;
                    case 3: paramString = $"{openpar}{prefix}{param2:X2}{param1:X2}{clospar}{ext}"; break;
                }
                list.Append($"{pcoffset:X6}  {opcodeByte:X2} {(opcode.Length >= 2 ? $"{param1:X2}" : b)} {(opcode.Length == 3 ? $"{param2:X2}" : b)} {opcode.Mnemonic} {(opcode.Absolute ? $"${target:X4}" : paramString)}\n");
                //list.Add($"{pcoffset:X4}  {opcodeByte:X2} {(opcode.Length >= 2 ? $"{param1:X2}" : b)} {(opcode.Length == 3 ? $"{param2:X2}" : b)} {opcode.Mnemonic} {(opcode.absolute ? $"${target:X4}" :paramString)}");
                //list[apos++] = $"{pcoffset:X4}  {opcodeByte:X2} {(opcode.Length >= 2 ? $"{param1:X2}" : b)} {(opcode.Length == 3 ? $"{param2:X2}" : b)} {opcode.Mnemonic} {(opcode.absolute ? $"${target:X4}" : paramString)}";
                pc += opcode.Length;
            }
            return list;
            //return list.Take(apos).ToArray();
            //return list.ToArray();
        }
    }
}