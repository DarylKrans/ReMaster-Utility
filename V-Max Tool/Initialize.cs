using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ReMaster_Utility.Properties;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        private static Thread Draw;
        private static Thread circ;  // Thread for drawing circle disk image
        private static Thread flat;  // Thread for drawing flat tracks image
        private static Thread check_alive;
        private static Thread Worker_Main;
        private static Thread Worker_Alt;
        private static Thread[] Job;
        private static Semaphore Task_Limit = new Semaphore(3, 3);
        private static readonly System.Windows.Forms.ToolTip tips = new System.Windows.Forms.ToolTip();
        private static List<string> LB_File_List = new List<string>();
        private static List<string> RM_Recent = new List<string>();
        private static List<Keys> keyBuffer = new List<Keys>();
        private static Keys[] obj_temp = new Keys[4];
        private static Keys[] debuging = new Keys[] { Keys.D, Keys.B, Keys.U, Keys.G };
        private static readonly byte[] keyset = new byte[] { 0x06, 0x14, 0x12, 0x10 };
        private static string NibPath = string.Empty;
        private static string recentPath = Path.Combine(TEMP.path, TEMP.recent);
        private static int Cores;
        private static int Default_Cores;
        private static int pan_defw;
        private static int pan_defh;
        private static bool manualRender;
        private static bool DontThread = false;
        private static readonly Gbox outbox = new Gbox();
        private static readonly Gbox inbox = new Gbox();
        private static readonly Color C64_screen = Color.FromArgb(69, 55, 176);
        private static readonly Color c64_text = Color.FromArgb(135, 122, 237);
        private static bool usecpp = true;
        private static bool CPP_LZ = true;
        private static bool debug = false;
        private static string def_bg_text;
        private static readonly PrivateFontCollection DirFont = new PrivateFontCollection();
        private static ConcurrentBag<string> ErrorList = new ConcurrentBag<string>();
        private static FontFamily customFontFamily;
        //private static Font customFont; // = GetCustomFont(12.0f, FontStyle.Regular);
        private const bool Set = false;
        private const bool Free = true;

        private static readonly byte[] sector_gap_length =
        {
            10, 10, 10, 10, 10, 10, 10, 10, 10, 10,	/*  1 - 10 */
        	10, 10, 10, 10, 10, 10, 10, 14, 14, 14,	/* 11 - 20 */
        	14, 14, 14, 14, 11, 11, 11, 11, 11, 11,	/* 21 - 30 */
        	8, 8, 8, 8, 8,					    	/* 31 - 35 */
        	8, 8, 8, 8, 8, 8, 8		        		/* 36 - 42 (non-standard) */
        };

        private static readonly byte[] sector_gap_density =
        {
            10, 14, 11, 8
        };

        private static readonly byte[] Available_Sectors =
        {
            21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21,
            19, 19, 19, 19, 19, 19, 19,
            18, 18, 18, 18, 18, 18,
            17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17,

        };

        private static readonly byte[] density_map =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,	/*  1 - 10 */
        	0, 0, 0, 0, 0, 0, 0, 1, 1, 1,	/* 11 - 20 */
        	1, 1, 1, 1, 2, 2, 2, 2, 2, 2,	/* 21 - 30 */
        	3, 3, 3, 3, 3,					/* 31 - 35 */
        	3, 3, 3, 3, 3, 3, 3				/* 36 - 42 (non-standard) */
        };

        private static readonly byte[] vm2_density_map =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,	/*  1 - 10 */
        	0, 0, 0, 0, 0, 0, 0, 1, 1, 1,	/* 11 - 20 */
        	1, 1, 1, 1, 1, 1, 1, 1, 1, 1,	/* 21 - 30 */
        	1, 1, 1, 1, 1,					/* 31 - 35 */
        	1, 1, 1, 1, 1, 1, 1				/* 36 - 42 (non-standard) */
        };

        private static readonly int[] VPL_density_map =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,	/*  1 - 10 */
        	0, 0, 0, 0, 0, 0, 0, 1, 1, 1,	/* 11 - 20 */
        	1, 1, 1, 1, 2, 2, 2, 2, 2, 2,	/* 21 - 30 */
        	3, 3, 3, 3, 3					/* 31 - 35 */
        };

        private static readonly int[] RLK_density_map =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,	/*  1 - 10 */
        	0, 0, 0, 0, 0, 0, 0, 1, 1, 1,	/* 11 - 20 */
        	1, 1, 1, 1, 1, 1, 1, 1, 1, 1,	/* 21 - 30 */
        	1, 1, 1, 1, 1                   /* 31 - 35 */
        };

        private static readonly int[] vm2_Sectors_by_density = { 22, 20 };
        private static readonly int[] Sectors_by_density = { 21, 19, 18, 17 };
        private static readonly string[] ErrorCodes =
        {
            "null",
            "Sector OK",            // 01
            "Header not Found",     // 02
            "Sync not found",       // 03
            "Data not found",       // 04
            "Bad data checksum",    // 05
            "Bad GCR",              // 06
            "Bad header checksum",  // 09
            "ID mismatch"           // 0b (11)
        };

        private static readonly string[] c1541error =
        {
            "",
            "0, Sector OK",
            "20, Block header not found",
            "21, Sync not found",
            "22, Data block not found",
            "23, Checksum error in data",
            "24, Byte decoding error",
            "",
            "",
            "27, Checksum error in header",
            "",
            "29, Disk ID mismatch"
        };

        private static readonly int[] sectorInterleave =
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
        };

        private static readonly Dictionary<byte, char> petsciiToAscii = new Dictionary<byte, char>()
        {
            { 0x41, 'a' }, { 0x42, 'b' }, { 0x43, 'c' }, { 0x44, 'd' }, { 0x45, 'e' },
            { 0x46, 'f' }, { 0x47, 'g' }, { 0x48, 'h' }, { 0x49, 'i' }, { 0x4A, 'j' },
            { 0x4B, 'k' }, { 0x4C, 'l' }, { 0x4D, 'm' }, { 0x4E, 'n' }, { 0x4F, 'o' },
            { 0x50, 'p' }, { 0x51, 'q' }, { 0x52, 'r' }, { 0x53, 's' }, { 0x54, 't' },
            { 0x55, 'u' }, { 0x56, 'v' }, { 0x57, 'w' }, { 0x58, 'x' }, { 0x59, 'y' },
            { 0x5A, 'z' }, { 0x20, ' ' }, { 0x0D, '\n' }, { 0x5B, '[' }, { 0x5C, '\\' },
            { 0x5D, ']' }, { 0xC1, 'A' }, { 0xC2, 'B' }, { 0xC3, 'C' }, { 0xC4, 'D' },
            { 0xC5, 'E' }, { 0xC6, 'F' }, { 0xC7, 'G' }, { 0xC8, 'H' }, { 0xC9, 'I' },
            { 0xCA, 'J' }, { 0xCB, 'K' }, { 0xCC, 'L' }, { 0xCD, 'M' }, { 0xCE, 'N' },
            { 0xCF, 'O' }, { 0xD0, 'P' }, { 0xD1, 'Q' }, { 0xD2, 'R' }, { 0xD3, 'S' },
            { 0xD4, 'T' }, { 0xD5, 'U' }, { 0xD6, 'V' }, { 0xD7, 'W' }, { 0xD8, 'X' },
            { 0xD9, 'Y' }, { 0xDA, 'Z' }, { 0x93, '?' },  // Control codes or unmapped chars
        };

        private static readonly Dictionary<char, byte> asciiToPetscii = new Dictionary<char, byte>()
        {
            { 'a', 0x41 }, { 'b', 0x42 }, { 'c', 0x43 }, { 'd', 0x44 }, { 'e', 0x45 },
            { 'f', 0x46 }, { 'g', 0x47 }, { 'h', 0x48 }, { 'i', 0x49 }, { 'j', 0x4A },
            { 'k', 0x4B }, { 'l', 0x4C }, { 'm', 0x4D }, { 'n', 0x4E }, { 'o', 0x4F },
            { 'p', 0x50 }, { 'q', 0x51 }, { 'r', 0x52 }, { 's', 0x53 }, { 't', 0x54 },
            { 'u', 0x55 }, { 'v', 0x56 }, { 'w', 0x57 }, { 'x', 0x58 }, { 'y', 0x59 },
            { 'z', 0x5A }, { 'A', 0xC1 }, { 'B', 0xC2 }, { 'C', 0xC3 }, { 'D', 0xC4 },
            { 'E', 0xC5 }, { 'F', 0xC6 }, { 'G', 0xC7 }, { 'H', 0xC8 }, { 'I', 0xC9 },
            { 'J', 0xCA }, { 'K', 0xCB }, { 'L', 0xCC }, { 'M', 0xCD }, { 'N', 0xCE },
            { 'O', 0xCF }, { 'P', 0xD0 }, { 'Q', 0xD1 }, { 'R', 0xD2 }, { 'S', 0xD3 },
            { 'T', 0xD4 }, { 'U', 0xD5 }, { 'V', 0xD6 }, { 'W', 0xD7 }, { 'X', 0xD8 },
            { 'Y', 0xD9 }, { 'Z', 0xDA }, { ' ', 0x20 }, { '\n', 0x0D }, { '[', 0x5B },
            { '\\', 0x5C }, { ']', 0x5D }
        };

        private static readonly Dictionary<char, byte> asciiToPetsciiReversed = new Dictionary<char, byte>()
        {
            { 'A', 0x61 }, { 'B', 0x62 }, { 'C', 0x63 }, { 'D', 0x64 }, { 'E', 0x65 },
            { 'F', 0x66 }, { 'G', 0x67 }, { 'H', 0x68 }, { 'I', 0x69 }, { 'J', 0x6A },
            { 'K', 0x6B }, { 'L', 0x6C }, { 'M', 0x6D }, { 'N', 0x6E }, { 'O', 0x6F },
            { 'P', 0x70 }, { 'Q', 0x71 }, { 'R', 0x72 }, { 'S', 0x73 }, { 'T', 0x74 },
            { 'U', 0x75 }, { 'V', 0x76 }, { 'W', 0x77 }, { 'X', 0x78 }, { 'Y', 0x79 },
            { 'Z', 0x7A }, { '0', 0x30 }, { '1', 0x31 }, { '2', 0x32 }, { '3', 0x33 },
            { '4', 0x34 }, { '5', 0x35 }, { '6', 0x36 }, { '7', 0x37 }, { '8', 0x38 },
            { '9', 0x39 }, { 'a', 0x41 }, { 'b', 0x42 }, { 'c', 0x43 }, { 'd', 0x44 },
            { 'e', 0x45 }, { 'f', 0x46 }, { 'g', 0x47 }, { 'h', 0x48 }, { 'i', 0x49 },
            { 'j', 0x4A }, { 'k', 0x4B }, { 'l', 0x4C }, { 'm', 0x4D }, { 'n', 0x4E },
            { 'o', 0x4F }, { 'p', 0x50 }, { 'q', 0x51 }, { 'r', 0x52 }, { 's', 0x53 },
            { 't', 0x54 }, { 'u', 0x55 }, { 'v', 0x56 }, { 'w', 0x57 }, { 'x', 0x58 },
            { 'y', 0x59 }, { 'z', 0x5A }, { ' ', 0xA0 }, { '-', 0xAB }, { '=', 0xBC }
        };

        private static readonly Dictionary<byte, char> petsciiToAsciiReversed = new Dictionary<byte, char>()
        {
            { 0x61, 'A' }, { 0x62, 'B' }, { 0x63, 'C' }, { 0x64, 'D' }, { 0x65, 'E' },
            { 0x66, 'F' }, { 0x67, 'G' }, { 0x68, 'H' }, { 0x69, 'I' }, { 0x6A, 'J' },
            { 0x6B, 'K' }, { 0x6C, 'L' }, { 0x6D, 'M' }, { 0x6E, 'N' }, { 0x6F, 'O' },
            { 0x70, 'P' }, { 0x71, 'Q' }, { 0x72, 'R' }, { 0x73, 'S' }, { 0x74, 'T' },
            { 0x75, 'U' }, { 0x76, 'V' }, { 0x77, 'W' }, { 0x78, 'X' }, { 0x79, 'Y' },
            { 0x7A, 'Z' }, { 0x30, '0' }, { 0x31, '1' }, { 0x32, '2' }, { 0x33, '3' },
            { 0x34, '4' }, { 0x35, '5' }, { 0x36, '6' }, { 0x37, '7' }, { 0x38, '8' },
            { 0x39, '9' }, { 0x41, 'a' }, { 0x42, 'b' }, { 0x43, 'c' }, { 0x44, 'd' },
            { 0x45, 'e' }, { 0x46, 'f' }, { 0x47, 'g' }, { 0x48, 'h' }, { 0x49, 'i' },
            { 0x4A, 'j' }, { 0x4B, 'k' }, { 0x4C, 'l' }, { 0x4D, 'm' }, { 0x4E, 'n' },
            { 0x4F, 'o' }, { 0x50, 'p' }, { 0x51, 'q' }, { 0x52, 'r' }, { 0x53, 's' },
            { 0x54, 't' }, { 0x55, 'u' }, { 0x56, 'v' }, { 0x57, 'w' }, { 0x58, 'x' },
            { 0x59, 'y' }, { 0x5A, 'z' }, { 0xA0, ' ' }, { 0xAB, '-' }, { 0xBC, '=' }
        };

        private static readonly byte[] weakBytes =  // All bytes containing 3 or more '0' bits in a row
        {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21,
            0x22, 0x23, 0x28, 0x30, 0x31, 0x38, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x50, 0x51,
            0x58, 0x60, 0x61, 0x62, 0x63, 0x68, 0x70, 0x71, 0x78, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
            0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x91, 0x98, 0xA0, 0xA1, 0xA2, 0xA3, 0xA8, 0xB0,
            0xB1, 0xB8, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xD0, 0xD1, 0xD8, 0xE0, 0xE1, 0xE2,
            0xE3, 0xE8, 0xF0, 0xF1, 0xF8
        };

        private static bool[] weakTable = new bool[256];
        private static byte[] nonWeak = new byte[0];

        void Reset_to_Defaults(bool clear_batch_list = true)
        {
            busy = true;
            Img_Q.SelectedIndex = 2;
            Set_ListBox_Items(true, true, clear_batch_list);
            Tabs.Controls.Remove(Advanced_Opts);
            if (clear_batch_list)
            {
                Batch_List_Box.Visible = false;
                Batch_List_Box.Items.Clear();
            }
            end_track = -1;
            fat_trk = -1;
            tracks = -1;
            Img_style.Enabled = Img_View.Enabled = Img_opts.Enabled = Save_Circle_btn.Visible = M_render.Visible = Adv_ctrl.Enabled = false;
            VBS_info.Visible = Reg_info.Visible = false;
            Other_opts.Visible = false;
            Save_Disk.Visible = false;
            Adv_ctrl.SelectedIndex = 0;
            linkLabel1.Visible = true;
            Draw_Init_Img(def_bg_text);
            Data_Box.Clear();
            Default_Dir_Screen();
            label2.Text = string.Empty;
            Dir_Box.Items.Clear();
            saveAsToolStripMenuItem.Enabled = false;
            NibWriteImage.Enabled = false;
            busy = false;
        }

        void Set_Arrays(int len)
        {
            /// NDS is the input or source array
            NDS.Track_Data = new byte[len][];
            NDS.Sector_Zero = new int[len];
            NDS.Track_Length = new int[len];
            NDS.D_Start = new int[len];
            NDS.D_End = new int[len];
            NDS.cbm = new int[len];
            NDS.sectors = new int[len];
            NDS.Header_Len = new int[len];
            NDS.cbm_sector = new int[len][];
            NDS.v2info = new byte[len][];
            NDS.Loader = new byte[0];
            NDS.Total_Sync = new int[len];
            NDS.Disk_ID = new byte[len][];
            NDS.Gap_Sector = new int[len];
            NDS.Track_ID = new int[len];
            NDS.Prot_Method = string.Empty;
            NDS.t18_ID = new byte[4];
            NDS.Adjust = new bool[len];
            NDS.Info = new string[len][];

            NDS.Sector = new byte[len][][];
            /// NDA is the destination or output array
            NDA.Track_Data = new byte[len][];
            NDA.Sector_Zero = new int[len];
            NDA.Track_Length = new int[len];
            NDA.D_Start = new int[len];
            NDA.D_End = new int[len];
            NDA.sectors = new int[len];
            NDA.Total_Sync = new int[len];
            /// NDG is the G64 arrays
            NDG.Track_Length = new int[len];
            NDG.Track_Data = new byte[len][];
            NDG.L_Rot = false;
            NDG.s_len = new int[len];
            NDG.newheader = new byte[2];
            NDG.Fat_Track = new bool[len];
            /// Original is the arrays that keep the original track data for the Auto Adjust feature
            Original.A = new byte[0];
            Original.G = new byte[0];
            Original.SA = new byte[0];
            Original.SG = new byte[0];
            Original.OT = new byte[len][];
            /// DiskDir is the arrays that handle directoy entries
            DiskDir.Entries = 0;
            DiskDir.Sectors = new byte[0][];
            DiskDir.Entry = new byte[0][];
            DiskDir.FileName = new string[0];
            Dir_Box.Items.Clear();
        }

        void AddRecentFile(string filePath)
        {
            RM_Recent.Remove(filePath);
            RM_Recent.Insert(0, filePath);
            if (RM_Recent.Count > 10) RM_Recent.RemoveRange(10, RM_Recent.Count - 10);
            SaveRecentList();
        }

        void SaveRecentList()
        {
            File.WriteAllLines(recentPath, RM_Recent);
            LoadRecentList();
        }

        void LoadRecentList()
        {
            if (!File.Exists(recentPath))
            {
                File.Create(recentPath).Close(); // Important: Close the stream immediately
                recentMenu.Enabled = false;
                return;
            }

            if (new FileInfo(recentPath).Length > 0)
            {
                RM_Recent = File.ReadAllLines(recentPath).ToList();
                recentMenu.Enabled = RM_Recent.Count > 0;
            }
            else
            {
                recentMenu.Enabled = false;
            }

            PopulateRecentFilesMenu();
        }

        void PopulateRecentFilesMenu()
        {
            recentMenu.DropDownItems.Clear();

            foreach (string file in RM_Recent)
            {
                if (File.Exists(file))
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(Path.GetFileName(file))
                    {
                        ToolTipText = file
                    };

                    item.Click += (sender, e) =>
                    {
                        string selectedFile = ((ToolStripMenuItem)sender).ToolTipText;
                        OpenFileFromRecent(selectedFile); // Replace with your actual file-opening method
                    };

                    recentMenu.DropDownItems.Add(item);
                }
            }

            // Optional: Add a separator and a "Clear List" option
            if (RM_Recent.Count > 0)
            {
                recentMenu.DropDownItems.Add(new ToolStripSeparator());

                ToolStripMenuItem clearItem = new ToolStripMenuItem("Clear Recent Files");
                clearItem.Click += (s, e) =>
                {
                    RM_Recent.Clear();
                    SaveRecentList();
                    recentMenu.Enabled = false;
                    PopulateRecentFilesMenu();
                };
                recentMenu.DropDownItems.Add(clearItem);
            }
            else
            {
                ToolStripMenuItem empty = new ToolStripMenuItem("(No recent files)")
                {
                    Enabled = false
                };
                recentMenu.DropDownItems.Add(empty);
            }
        }

        void SaveSettings()
        {
            var lines = new List<string>
            {
                $"No_Warn={No_Warn.Checked}",
                $"Parallel={Parallel.Checked}",
                $"WParallel={WParallel.Checked}",
                $"Dev_num={Dev_num.Value}",
                $"W_num={W_num.Value}",
                $"CPP_tog={CPP_tog.Checked}",
                $"Pad_Tracks={Pad_Tracks.Checked}",
                $"R_limit={R_limit.Checked}",
                $"W_limit={W_limit.Checked}",
                $"R_verb={R_verb.Checked}",
                $"W_verb={W_verb.Checked}",
                $"Enable_DB={EnableDBMenu.Checked}",
                $"Prot_DetectMethod={ProtDetectMethod.SelectedIndex}",
                $"Parse_Log={ParseLog.Checked}"
            };
            File.WriteAllLines(TEMP.settings, lines);
        }

        void LoadSettings()
        {
            if (!File.Exists(TEMP.settings)) SaveSettings();

            var lines = File.ReadAllLines(TEMP.settings);
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length != 2) continue;

                string key = parts[0];
                string value = parts[1];

                switch (key)
                {
                    case "No_Warn": No_Warn.Checked = bool.Parse(value); break;
                    case "Parallel": Parallel.Checked = bool.Parse(value); break;
                    case "WParallel": WParallel.Checked = bool.Parse(value); break;
                    case "Dev_num": Dev_num.Value = int.Parse(value); break;
                    case "W_num": W_num.Value = int.Parse(value); break;
                    case "CPP_tog": CPP_tog.Checked = bool.Parse(value); break;
                    case "Pad_Tracks": Pad_Tracks.Checked = bool.Parse(value); break;
                    case "R_limit": R_limit.Checked = bool.Parse(value); break;
                    case "W_limit": W_limit.Checked = bool.Parse(value); break;
                    case "R_verb": R_verb.Checked = bool.Parse(value); break;
                    case "W_verb": W_verb.Checked = bool.Parse(value); break;
                    case "Enable_DB": EnableDBMenu.Checked = bool.Parse(value); break;
                    case "Prot_DetectMethod": ProtDetectMethod.SelectedIndex = int.Parse(value); break;
                    case "Parse_Log": ParseLog.Checked = bool.Parse(value); break;
                }
            }
        }

        void FindNibtools()
        {
            if (File.Exists(TEMP.path + TEMP.Nibtools) && new System.IO.FileInfo(TEMP.path + TEMP.Nibtools).Length > 0)
            {
                NibPath = File.ReadAllText(TEMP.path + TEMP.Nibtools);
            }
            else
            {
                NibPath = TEMP.exedir;
            }
            bool Nwrite = File.Exists(NibPath + "nibwrite.exe");
            bool Nread = File.Exists(NibPath + "nibread.exe");

            if (Nwrite || Nread)
            {
                NibReadImage.Enabled = NibReadImage.Visible = Nread;
                NibWriteImage.Enabled = NibWriteImage.Visible = Nwrite;
                NibSeparator.Visible = true;
            }
            else NibReadImage.Visible = NibWriteImage.Visible = NibSeparator.Visible = false;
        }

        public static Font GetCustomFont(float fontSize, FontStyle fontStyle)
        {
            return new Font(DirFont.Families[0], fontSize, fontStyle);
        }
        int Get_Cores()
        {
            foreach (var item in new System.Management.ManagementObjectSearcher("Select NumberOfCores from Win32_Processor").Get())
            {
                int coreCount = int.Parse(item["NumberOfCores"].ToString());
                Cores += coreCount;
            }
            if (Cores == 1) manualRender = M_render.Visible = true;
            Default_Cores = Cores;
            return Cores;
        }

        void Set_Cores(bool Min_Cores_3 = true)
        {
            if (Min_Cores_3 && Cores < 3) Cores = 3;
            else DB_cores.Value = Cores;
            Task_Limit = new Semaphore(Cores, Cores);
        }

        void Set_Auto_Opts()
        {
            if (Auto_Adjust) V3_Auto_Adj.Checked = V2_Auto_Adj.Checked = VPL_auto_adj.Checked = true;
            else V3_Auto_Adj.Checked = V2_Auto_Adj.Checked = VPL_auto_adj.Checked = false;
            V2_Add_Sync.Checked = V2_Auto_Adj.Checked;
        }



        void Default_Dir_Screen()
        {
            Dir_screen.Clear();
            Dir_screen.Text = dir_def;
            Dir_screen.Select(2, 23);
            Dir_screen.SelectionBackColor = c64_text;
            Dir_screen.SelectionColor = C64_screen;
            DiskDir.Entries = 0;
            DiskDir.Sectors = new byte[0][];
            DiskDir.Entry = new byte[0][];
            DiskDir.FileName = new string[0];
            Dir_Box.Items.Clear();
        }

        void Init()
        {
            /// Remove Create Blank Disk and Directory Editor functions
            CBD_box.Visible = false;
            Dir_Edit.Visible = false;
            label19.Visible = label18.Visible = Sec_Interleave.Visible = CBD_box.Visible;
            Dir_screen.Top = CBD_box.Visible ? Dir_screen.Top : 0;
            Dir_screen.Height = tabPage3.Height;
            /// Don't allow files to be added to disk images (comment out the following lines)
            //Dir_screen.AllowDrop = true;
            //Dir_screen.DragEnter += new DragEventHandler(Dir_Screen_DragEnter);
            //Dir_screen.DragDrop += new DragEventHandler(Dir_Screen_DragDrop);
            //menuStrip1.Items.Remove("Database");
            databaseToolStripMenuItem.Visible = databaseToolStripMenuItem.Enabled = false;

            ReadNib.Controls.Add(Read_GBox);
            Read_GBox.Location = new Point(0, 0);
            Options.Controls.Add(Options_Box);
            Options_Box.Location = new Point(0, 0);
            customFontFamily = LoadFontFromResource(Resources.C64_Pro_Mono_STYLE);
            Font customFont = GetCustomFont(12.0f, FontStyle.Regular);
            usecpp = Load_Dll();
            FindNibtools();
            Init_Read_Options();
            Init_Write_Options();
            LoadRecentList();
            //Setup_Database_Window();

            if (!usecpp) CPP_tog.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            Batch_List_Box.Visible = false; // set to true for debugging that requires a listbox
            Batch_List_Box.HorizontalScrollbar = true;
            Other_opts.Visible = false;
            Advanced_Opts.Controls.Add(V2_Advanced);
            Advanced_Opts.Controls.Add(V3_Advanced);
            Advanced_Opts.Controls.Add(VPL_Advanced);
            Advanced_Opts.Controls.Add(RPL_Advanced);
            V2_Advanced.Top = V3_Advanced.Top = VPL_Advanced.Top = RPL_Advanced.Top = 0;
            V2_Advanced.Left = V3_Advanced.Left = VPL_Advanced.Left = RPL_Advanced.Left = 0;
            V2_Advanced.Visible = V3_Advanced.Visible = VPL_Advanced.Visible = RPL_Advanced.Visible = false;
            /// Directory editing box values
            groupBox3.Controls.Add(Dir_Box);
            Dir_Box.DrawMode = DrawMode.OwnerDrawFixed;
            Dir_Box.CheckOnClick = true;
            Dir_Box.UseCompatibleTextRendering = true;
            Dir_Box.Font = Dir_screen.Font = customFont;
            Dir_Box.BackColor = C64_screen;
            Dir_Box.ForeColor = c64_text;
            Dir_Box.Location = new System.Drawing.Point(1, 12);
            Dir_Box.Size = new System.Drawing.Size(groupBox3.Width - 12, 31 * 19);
            Dir_Box.FormattingEnabled = true;
            Dir_Box.MouseDown += new MouseEventHandler(Dir_Box_MouseDown);
            Dir_Box.MouseMove += new MouseEventHandler(Dir_Box_MouseMove);
            Dir_Box.MouseUp += new MouseEventHandler(Dir_Box_MouseUp);
            Dir_Box.DragOver += new DragEventHandler(Dir_Box_DragOver);
            Dir_Box.DragDrop += new DragEventHandler(Dir_Box_DragDrop);
            Dir_Box.AllowDrop = true;
            Dir_All.LinkBehavior = LinkBehavior.NeverUnderline;
            Dir_None.LinkBehavior = LinkBehavior.NeverUnderline;
            Dir_Rev.LinkBehavior = LinkBehavior.NeverUnderline;
            Dir_Edit.LinkBehavior = LinkBehavior.NeverUnderline;
            scrollTimer = new System.Windows.Forms.Timer { Interval = 20 };
            scrollTimer.Tick += new EventHandler(ScrollTimer_Tick);
            groupBox3.BringToFront();
            groupBox3.Visible = false;
            GB_NewDisk.BringToFront();
            GB_NewDisk.Visible = false;
            GB_NewDisk.Top = ((this.Height - GB_NewDisk.Height) / 2) - 100;
            GB_NewDisk.Left = ((this.Width - GB_NewDisk.Width) / 2) - 100;
            Dir_Ftype.Enabled = Dir_ChgType.Checked;
            Dir_Ftype.DataSource = new string[] { "PRG", "SEQ", "USR", "REL", "DEL" };
            /// ----------------------------
            linkLabel1.Visible = false;
            panel1.Controls.Add(outbox);
            panel1.Controls.Add(inbox);
            Height = PreferredSize.Height;
            pan_defw = panPic.Width;
            pan_defh = panPic.Height;
            panPic.Controls.Add(Disk_Image);
            Out_view.Select();
            Circle_View.Select();
            Disk_Image.Image = new Bitmap(8192, 42 * 15);
            panPic.AutoScroll = false;
            panPic.SetBounds(0, 0, Disk_Image.Width, Disk_Image.Height);
            Disk_Image.SizeMode = PictureBoxSizeMode.AutoSize;
            flat_large = new Bitmap(8192, panPic.Height - 16);
            out_weak.Width = 64;
            Adv_ctrl.SelectedIndexChanged += new System.EventHandler(Adv_Ctrl_SelectedIndexChanged);
            Out_density.DrawItem += new DrawItemEventHandler(Out_Density_Color);
            out_track.DrawItem += new DrawItemEventHandler(Out_Track_Color);
            Track_Info.DrawItem += new DrawItemEventHandler(Source_Info_Color);
            sf.DrawItem += new DrawItemEventHandler(Track_Format_Color);
            out_rpm.DrawItem += new DrawItemEventHandler(RPM_Color);
            Out_density.DrawMode = DrawMode.OwnerDrawFixed;
            out_track.DrawMode = DrawMode.OwnerDrawFixed;
            Track_Info.DrawMode = DrawMode.OwnerDrawFixed;
            out_rpm.DrawMode = DrawMode.OwnerDrawFixed;
            sf.DrawMode = DrawMode.OwnerDrawFixed;
            Out_density.ItemHeight = out_rpm.ItemHeight = sf.ItemHeight = 13;
            out_track.ItemHeight = out_rpm.ItemHeight = sf.ItemHeight = 13;
            Track_Info.ItemHeight = 15;
            Track_Info.HorizontalScrollbar = true;
            Adj_cbm.Visible = false;
            Tabs.Visible = true;
            Data_Box.DetectUrls = false;
            Data_Sep.DataSource = new string[] { "None", "Tracks", "Sectors" }; //d;
            Data_Sep.SelectedIndex = 1;
            VS_hex.Checked = true;
            T_jump.Visible = Jump.Visible = false;
            DV_gcr.Checked = true;
            fnappend = fix;
            label1.Text = label2.Text = coords.Text = "";
            Source.Visible = Output.Visible = label4.Visible = Img_Q.Visible = Save_Circle_btn.Visible = false;
            Save_Disk.Visible = false;
            AllowDrop = true;
            DragEnter += new DragEventHandler(Drag_Enter);
            DragDrop += new DragEventHandler(Drag_Drop);
            /// ------------------ Vorpal Config --------------
            Lead_In.Enabled = VPL_lead.Checked;
            Lead_In.Value = 50;
            Lead_ptn.DataSource = new string[] { "Default", "0x55", "0xAA" };//pt;
            Lead_ptn.SelectedIndex = 0;
            Lead_ptn.Enabled = VPL_rb.Checked;
            VD0.Enabled = VD1.Enabled = VD2.Enabled = VD3.Enabled = VPL_density_reset.Visible = false;
            VD0.Value = vpl_density[0];
            VD1.Value = vpl_density[1];
            VD2.Value = vpl_density[2];
            VD3.Value = vpl_density[3];
            Lead_ptn.BringToFront();
            /// ----------------- V-Max v3 Config -------------
            V3_hlen.Enabled = false;
            /// ----------------- V-Max v2 Config -------------
            Tabs.Controls.Remove(Advanced_Opts);
            V2_hlen.Enabled = false;
            v2adv.Text = v3adv.Text = $"\u2193        Advanced users ONLY!        \u2193";
            vm2_ver[0] = new string[] { "A5-A5", "A4-A5", "A5-A7", "A5-A6", "A9-AD", "AC-A9", "AD-AB", "A9-AE", "A5-AD", "AC-A5", "AD-A7", "A5-AE", "A5-A9",
            "A4-A9", "A5-AB", "A5-AA", "A5-B5", "B4-A5", "A5-B7", "A5-B6", "A9-BD", "BC-A9" };
            vm2_ver[1] = new string[vm2_ver[0].Length];
            Array.Copy(vm2_ver[0], 0, vm2_ver[1], 0, vm2_ver[0].Length);
            vm2_ver[1][6] = "A5-A3"; vm2_ver[1][10] = "A9-A3";
            V2_swap_headers.Visible = false;
            string[] interleave_select = new string[] { "1", "2", "3", "4", "5", "6", "7 JiffyDos 1571", "8 Fastloader", "9", "10 Standard", "11", "12" };
            Sec_Interleave.DataSource = interleave_select; // new string[] { "Standard (10)", "JiffyDos 1571 (7)", "Custom (5)" };
            S_Interleave.DataSource = interleave_select; //new string[] { "Standard (10)", "JiffyDos 1571 (7)", "Custom (5)" };
            /// Loads V-Max Loader track replacements into byte[] arrays
            v2ldrcbm = Decompress(XOR(Resources.v2cbmla, 0xcb)); // V-Max CBM sectors (DotC, Into the Eagles Nest, Paperboy, etc..)
            v24e64pal = Decompress(XOR(Resources.v24e64p, 0x64)); // V-Max Custom sectors (PAL Loader)
            v26446ntsc = Decompress(XOR(Resources.v26446n, 0x46)); // V-Max Custom sectors (NTSC Loader) Older version, headers have weak bits and may be incompatible with some 1541's
            v2644entsc = Decompress(XOR(Resources.v2644En, 0x4e)); // V-Max Custom sectors (NTSC Loader) Newer version, headers are compatible with all 1541 versions.
            v2stub = Decompress(XOR(Resources.v2stub, 0x5a));
            //vmax_dec_table = Decompress(XOR(Resources.vmctbl, 0x4e));
            /// these loaders are guaranteed to work and the loader code has not been modified from original. (these are not "cracked" loaders)
            rak1 = Decompress(XOR(Resources.rak1, 0xab));
            cldr_id = Decompress(XOR(Resources.cyan, 0xc1));
            fastloader = Decompress(XOR(Resources.fload, 0xf1));
            byte[] rlnk = Decompress(XOR(Resources.rlnk, 0x7b));
            Buffer.BlockCopy(rlnk, 0, rl_nkey, 0, rl_nkey.Length);
            for (int i = 54; i < rlnk.Length; i++) rl_7b[i - 54] = Convert.ToInt32(rlnk[i]);
            /// RapidLok Patches
            rl6_t18s3[0, 0] = new byte[] { 0x60, 0x9b, 0x2a, 0x7d };
            rl6_t18s3[1, 0] = new byte[] { 0xea, 0xf4, 0xb7, 0xb3, 0xba };
            rl6_t18s3[2, 0] = new byte[] { 0x75, 0x73, 0xdc, 0x3d, 0x27, 0xd6 };
            rl6_t18s3[3, 0] = new byte[] { 0x18, 0x28, 0x9b, 0x95, 0x64, 0x00, 0xa5, 0xc7, 0xc2, 0x27 };
            rl6_t18s3[4, 0] = new byte[] { 0xf9, 0xeb, 0x63 };
            rl6_t18s3[0, 1] = new byte[] { 0x68, 0x95, 0x56, 0xcb };
            rl6_t18s3[1, 1] = new byte[] { 0x82, 0xae, 0xeb, 0x93, 0x46 };
            rl6_t18s3[2, 1] = new byte[] { 0xa5, 0xf3, 0xb3, 0x8a, 0xc3, 0xd6 };
            rl6_t18s3[3, 1] = new byte[] { 0x1b, 0x12, 0x68, 0xb5, 0xb4, 0x01, 0xa5, 0x83, 0x83, 0xdc };
            rl6_t18s3[4, 1] = new byte[] { 0xf9, 0x3c, 0x63 };
            rl6_t18s6[0, 0] = new byte[] { 0x91, 0xb3, 0x7f, 0x92, 0xbe, 0xe9, 0x0c, 0x92, 0xfd, 0xcc, 0x24, 0x38, 0x02, 0x5a, 0xf1, 0x3e, 0x27, 0x51,
                                           0x52, 0x43, 0x9c, 0xd3, 0x93, 0x23, 0xca, 0x5d, 0x24, 0x7d, 0x31};
            rl6_t18s6[0, 1] = new byte[] { 0x32, 0x36, 0xe8, 0x0e, 0x33, 0x71, 0x70, 0x3a, 0xe0, 0xdf, 0x3d, 0x57, 0xd7, 0xb3, 0xcc, 0x2d, 0x0a, 0x4d,
                                           0x87, 0x06, 0x97, 0x74, 0xb2, 0x7a, 0x75, 0x83, 0x56, 0x9f, 0x33};
            rl2_t18s9[0, 0] = new byte[] { 0x75, 0xf3, 0x7f, 0x9b, 0xb9 };
            rl2_t18s9[1, 0] = new byte[] { 0xa9, 0xd5, 0x95, 0xfa, 0x9a };
            rl2_t18s9[0, 1] = new byte[] { 0x75, 0xf3, 0xef, 0x5b, 0xa9 };
            rl2_t18s9[1, 1] = new byte[] { 0xa9, 0xd5, 0xdc, 0xdb, 0xea };
            rl1_t18s9[0, 0] = new byte[] { 0xd5, 0x5e, 0xeb, 0xb7, 0xdb };
            rl1_t18s9[1, 0] = new byte[] { 0x3c, 0xcf, 0x3e, 0xd6, 0x96 };
            rl1_t18s9[0, 1] = new byte[] { 0xd5, 0x5e, 0x7b, 0xb6, 0xdb };
            rl1_t18s9[1, 1] = new byte[] { 0x3c, 0xcd, 0x5a, 0xdd, 0x56 };
            RL_Fix.Visible = false;
            RL_success.Text = string.Empty;
            RM_cyan.Visible = false;
            RM_cyan.Left = 8;
            Img_Q.DataSource = Img_Quality;
            Img_Q.SelectedIndex = 2;
            Width = PreferredSize.Width;
            Flat_Interp.Visible = Flat_View.Checked;
            Circle_View.Checked = Out_view.Checked = true;
            label4.Visible = Img_Q.Visible = Circle_View.Checked;
            Circle_Render.Visible = Flat_Render.Visible = label3.Visible = false;
            Img_opts.Enabled = Img_style.Enabled = Img_View.Enabled = false;
            Batch_Box.Visible = false;
            for (int i = 0; i < 8000; i++) { def_bg_text += "10"; if (i < 4) obj_temp[i] = (Keys)(keyset[i] ^ 0x55); }
            M_render.Enabled = false;
            Adv_ctrl.Enabled = false;
            VBS_info.Visible = Reg_info.Visible = false;
            Dir_screen.BackColor = C64_screen;
            Dir_screen.ForeColor = c64_text;
            Dir_screen.ReadOnly = true;
            DB_cores.Enabled = DB_core_override.Checked;
            SwapDensities();
            Set_Boxes();
            Draw_Init_Img(def_bg_text);
            Default_Dir_Screen();
            Set_Auto_Opts();
            BlockMap_Setup();
            Cores = Get_Cores();
            Set_Cores();
            Set_Tool_Tips();
            tips.ShowAlways = true;
            manualRender = M_render.Visible = Cores <= 3;
            if (Cores < 2) Img_Q.SelectedIndex = 0;
            /// ------------------------------------------------------------------------------------------
            Build_BitReverseTable();
            Build_WeakTable();
            ProtDetectMethod.DataSource = new string[] { "Scan source on add (slower)", "Parse file-name for protection type", "Don't detect" };
            RunBusy(() => LoadSettings());

            //Setup_Database_Window();
            //RecoverDatabase();

            try
            {
                //File.WriteAllBytes($@"c:\test\v2stub.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\v2stub")), 0x5a));
                //File.WriteAllBytes($@"c:\test\compressed\fload.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\fload")), 0xf1));
                //File.WriteAllBytes($@"c:\test\compressed\cpp_extf.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\DrawArc.dll")), 0xda));
                //File.WriteAllBytes($@"c:\test\compressed\msvcrt.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\msvcrt")), 0x24));
                //File.WriteAllBytes($@"c:\test\compressed\rlnk.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\rlnk")), 0x7b));
                //File.WriteAllBytes($@"c:\test\compressed\cyan.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\cyan")), 0xc1));
                //File.WriteAllBytes($@"c:\test\compressed\rak1.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\rak2")), 0xab));
                //File.WriteAllBytes($@"c:\test\compressed\v2cbmla.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\cbm")), 0xcb));
                //File.WriteAllBytes($@"c:\test\compressed\v24e64p.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\4e64")), 0x64));
                //File.WriteAllBytes($@"c:\test\compressed\v26446n.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\6446")), 0x46));
                //File.WriteAllBytes($@"c:\test\compressed\v2644en.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\644e")), 0x4e));
                //File.WriteAllBytes($@"c:\test\compressed\vmv2dt1.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\loaders\vmv2dt1")), 0x4e));
                //File.WriteAllBytes($@"c:\test\assets\vmctbl.bin", XOR(Compress(File.ReadAllBytes($@"c:\test\assets\vmctable.bin")), 0x4e));
            }
            catch { }
            Pad_Tracks.Checked = true;

            void Set_Boxes()
            {
                outbox.BackColor = Color.Gainsboro;
                outbox.BringToFront();
                panel1.Controls.Remove(this.out_weak);
                panel1.Controls.Remove(this.Out_density);
                panel1.Controls.Remove(this.out_rpm);
                panel1.Controls.Remove(this.out_track);
                panel1.Controls.Remove(this.out_dif);
                panel1.Controls.Remove(this.out_size);
                outbox.Controls.Add(this.Out_density);
                outbox.Controls.Add(this.out_rpm);
                outbox.Controls.Add(this.out_track);
                outbox.Controls.Add(this.out_dif);
                outbox.Controls.Add(this.out_size);
                outbox.Controls.Add(this.out_weak);
                var w = 5;
                out_track.Location = new Point(w, 15); w += out_track.Width - 1;
                out_rpm.Location = new Point(w, 15); w += out_rpm.Width - 1;
                out_size.Location = new Point(w, 15); w += out_size.Width - 1;
                out_dif.Location = new Point(w, 15); w += out_dif.Width - 1;
                Out_density.Location = new Point(w, 15); w += out_dif.Width - 1;
                out_weak.Location = new Point(w, 15); //w += out_dif.Width - 1;
                outbox.FlatStyle = FlatStyle.Flat;
                outbox.ForeColor = Color.Indigo;
                outbox.Name = "outbox";
                outbox.Width = outbox.PreferredSize.Width;
                outbox.Height = outbox.PreferredSize.Height;
                outbox.Location = new Point(225, 13);
                outbox.TabIndex = 52;
                outbox.TabStop = false;
                outbox.Text = "Track/ RPM /     Size     / Diff     / Density  / Weak";
                inbox.BackColor = Color.Gainsboro;
                panel1.Controls.Remove(this.sd);
                panel1.Controls.Remove(this.strack);
                panel1.Controls.Remove(this.sf);
                panel1.Controls.Remove(this.ss);
                panel1.Controls.Remove(this.sl);
                inbox.Controls.Add(this.sd);
                inbox.Controls.Add(this.strack);
                inbox.Controls.Add(this.sf);
                inbox.Controls.Add(this.ss);
                inbox.Controls.Add(this.sl);
                inbox.BringToFront();
                w = 5;
                strack.Location = new Point(w, 15); w += strack.Width - 1;
                sl.Location = new Point(w, 15); w += sl.Width - 1;
                sf.Location = new Point(w, 15); w += sf.Width - 1;
                ss.Location = new Point(w, 15); w += ss.Width - 1;
                sd.Location = new Point(w, 15);
                inbox.FlatStyle = FlatStyle.Popup;
                inbox.ForeColor = Color.Indigo;
                inbox.Location = new Point(8, 13);
                inbox.Name = "inbox";
                inbox.Width = inbox.PreferredSize.Width;
                inbox.Height = inbox.PreferredSize.Height;
                inbox.TabIndex = 55;
                inbox.TabStop = false;
                inbox.Text = "Trk / Size / Format / Sectors / Dens";
            }

            FontFamily LoadFontFromResource(byte[] fontdata)
            {
                // Pin the font data array in memory
                IntPtr fontPtr = Marshal.AllocCoTaskMem(fontdata.Length);
                Marshal.Copy(fontdata, 0, fontPtr, fontdata.Length);

                // Add the font to the PrivateFontCollection
                DirFont.AddMemoryFont(fontPtr, fontdata.Length);

                // Free the memory
                Marshal.FreeCoTaskMem(fontPtr);

                // Return the first font family in the collection
                return DirFont.Families[0];
            }

            void Set_Tool_Tips()
            {
                tips.SetToolTip(Adj_cbm, "Adjust standard tracks to fit a 300rpm rotation cycle\n" +
                    "Allows for writing images without slowing down the disk drive\n\n" +
                    "Option may not be available on certain Protection types that rely on the extra data");
                tips.SetToolTip(Save_Disk, "Export ReMastered file as G64 or NIB");
                tips.SetToolTip(Circle_View, "Show image of track data representation as it would be on a disk");
                tips.SetToolTip(Flat_View, "Show image of track data representation in a linear view");
                tips.SetToolTip(Out_view, "Show the processed (output) image data representation");
                tips.SetToolTip(Src_view, "Show the source file's (input) image data representation");
                tips.SetToolTip(Show_sec, "Highlight areas where a new sector starts (V-Max/Vorpal)");
                tips.SetToolTip(Cap_margins, "Show's where the data exceeds the track's capacity limit at 300rpm");
                tips.SetToolTip(Flat_Interp, "Blurs the image a little (can help better define where the sectors are)");
                tips.SetToolTip(Rev_View, "Shows the tracks in different colors to differentiate between formats");
                tips.SetToolTip(Save_Circle_btn, "Save currently displayed image as BMP or JPG");
                tips.SetToolTip(label4, "Change Disk-View image resolution\nLow = 1000 x 1000, Insanity = 7000 x 7000");
                tips.SetToolTip(Img_Q, "Change Disk-View image resolution\nLow = 1000 x 1000, Insanity = 7000 x 7000");
                tips.SetToolTip(Re_Align, "Attempt to center the V-Max loader data in the track to prevent the track gap from being placed within the data");
                tips.SetToolTip(V2_Auto_Adj, "Adjust all tracks to fit on a disk without slowing down the drive motor");
                tips.SetToolTip(V2_Custom, "Manually set the sector header length (applies to all tracks)\n" +
                    "this isn't very useful, but it could be fun! or dangerous. Who knows?");
                tips.SetToolTip(V3_Auto_Adj, "Adjust all tracks to fit on a disk without slowing down the drive motor");
                tips.SetToolTip(V3_Custom, "Manually set the sector header length (applies to all tracks)\n" +
                    "this isn't very useful, but it could be fun! or dangerous. Who knows?");
                tips.SetToolTip(V2_swap_headers, "Changes the sector headers (must use the same headers on all sides)\n" +
                    "64-46 contains weak-bits that might not work on older 1541 drives.\n" +
                    "Change headers to 64-4E if your drive has any issues with loading\n" +
                    "*4E-64 is only found on European versions of V-Max, but they also work");
                tips.SetToolTip(V2_Add_Sync, "Adds 10 bits of sync before each sector on syncless tracks\n" +
                    "This doesn't have any affect on loading and the protection doesn't check for it");
                tips.SetToolTip(VPL_auto_adj, "Adjust all tracks for best success on write");
                tips.SetToolTip(VPL_rb, "Adjust all (Vorpal) tracks for best success on write, leaves standard tracks un-altered");
                tips.SetToolTip(VPL_lead, "Adjust sector data placement (in bytes) from the start of the track");
                tips.SetToolTip(VPL_only_sectors, "Vorpal tracks will ONLY contain the sector data, no lead-in or lead-out\n" +
                    "this is for educational purposes only and is unlikely to produce a working image");
                tips.SetToolTip(VPL_presync, "Adds 16 bits of sync to the start of the track (in the lead-in)\n" +
                    "this is for experimentation and may help or hinder successful disk-writes");
                tips.SetToolTip(label7, "Change the Lead-in/out sequence of Vorpal tracks\n" +
                    "0x55 and 0xAA are essentially the same (01010101 or 10101010");
                tips.SetToolTip(RL_ChangeKey, "Check this option if output image fails the Track-36 key check");
                tips.SetToolTip(RL_Fix, "Remove all advanced RapidLok checks.\nDisables the following items...\n\nTrack 36 key-check\nHeader integrity checks\n" +
                    "Sync checks\nSpecial sector checks\nTrack alignment checks\n\nCurrently only works on RapidLok versions 1,2, and 4-7");
                tips.SetToolTip(DB_force, "Will perform auto-adjust on standard CBM-Formatted tracks, ignoring special\n"
                    + "conditions that some copy protections rely on to pass the protection.\n\n Sometimes these conditions are falsely identified."
                    + " select this option to FORCE adjusting of these tracks");
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            keyBuffer.Add(keyData);
            if (keyBuffer.Count > obj_temp.Length) keyBuffer.RemoveAt(0);
            if (keyBuffer.SequenceEqual(obj_temp))
            {
                keyBuffer.Clear();
                Object_Imager();
            }
            if (keyBuffer.SequenceEqual(debuging))
            {
                keyBuffer.Clear();
                Object_Imager(true);
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Object_Imager(bool dbg = false)
        {
            if (dbg)
            {
                debug = !debug;
                button1.Visible = button2.Visible = CBM_Fix.Visible = debug;
            }
            else
            {
                byte[] obj = Decompress(XOR(Resources.objects, 0xbd));
                using (MemoryStream ms = new MemoryStream(obj))
                {
                    Image img = Image.FromStream(ms);
                    Disk_Image.Image = img;
                }
                Disk_Image.SizeMode = PictureBoxSizeMode.Zoom;
            }
        }

        void Set_ListBox_Items(bool r, bool nofile, bool clear_batch_list = true)
        {
            strack.BeginUpdate();
            ss.BeginUpdate();
            sf.BeginUpdate();
            sl.BeginUpdate();
            sd.BeginUpdate();
            out_size.BeginUpdate();
            out_dif.BeginUpdate();
            out_rpm.BeginUpdate();
            out_track.BeginUpdate();
            Out_density.BeginUpdate();
            out_weak.BeginUpdate();
            if (r)
            {
                Make_Visible();
                Clear_Out_Items();
                Clear_In_Items();

                out_track.Height = Out_density.Height = out_size.Height = out_dif.Height = out_rpm.Height = out_weak.Height = out_size.PreferredHeight;
                sl.Height = strack.Height = sl.Height = sd.Height = ss.Height = sf.Height = sl.PreferredHeight; // (items * 12);
            }
            Make_Visible();
            outbox.Visible = inbox.Visible = !r;
            out_track.Height = Out_density.Height = out_size.Height = out_dif.Height = out_rpm.Height = out_weak.Height = out_size.PreferredHeight;
            sl.Height = strack.Height = sl.Height = sd.Height = ss.Height = sf.Height = sl.PreferredHeight; // (items * 12);
            outbox.Height = outbox.PreferredSize.Height;
            inbox.Height = inbox.PreferredSize.Height;
            if (clear_batch_list) Drag_pic.Visible = (r && nofile);
            out_size.EndUpdate();
            out_dif.EndUpdate();
            Out_density.EndUpdate();
            ss.EndUpdate();
            sf.EndUpdate();
            out_rpm.EndUpdate();
            out_track.EndUpdate();
            out_weak.EndUpdate();
            strack.EndUpdate();
            sl.EndUpdate();
            sd.EndUpdate();

            void Make_Visible()
            {
                out_size.Visible = !r;
                out_dif.Visible = !r;
                ss.Visible = !r;
                sf.Visible = !r;
                sl.Visible = !r;
                sd.Visible = !r;
                strack.Visible = !r;
                out_rpm.Visible = !r;
                out_track.Visible = !r;
                Out_density.Visible = !r;
                out_weak.Visible = !r;
            }
        }

        void Clear_Out_Items()
        {
            out_track.Items.Clear();
            out_size.Items.Clear();
            out_dif.Items.Clear();
            Out_density.Items.Clear();
            out_rpm.Items.Clear();
            out_weak.Items.Clear();
        }

        void Clear_In_Items()
        {
            ss.Items.Clear();
            sf.Items.Clear();
            sl.Items.Clear();
            sd.Items.Clear();
            strack.Items.Clear();
        }

        bool Load_Dll()
        {
            try
            {
                byte[] cpp = Decompress(XOR(Resources.cpp_extf, 0xda));

                if (!Directory.Exists(TEMP.path)) Directory.CreateDirectory(TEMP.path);
                // Check if the file exists and needs to be overwritten
                string dfl = Path.Combine(TEMP.path, TEMP.dll);
                if (File.Exists(dfl))
                {
                    try
                    {
                        var verify = File.ReadAllBytes(dfl);
                        // If the file contents differ, overwrite it
                        if (cpp.Length != verify.Length || !cpp.SequenceEqual(verify))
                        {
                            try
                            {
                                OverwriteFile(dfl, cpp);
                            }
                            catch { return false; }
                        }
                    }
                    catch { return false; }
                }
                else
                {
                    WriteNewFile(dfl, cpp);
                }

                // Attempt to load and test the DLL
                int test = 0;
                try
                {
                    string originalDir = Environment.CurrentDirectory;
                    Environment.CurrentDirectory = TEMP.path;
                    test = NativeMethods.TestLoaded();
                    Environment.CurrentDirectory = originalDir;
                }
                catch { }
                return (test == 6);

                void OverwriteFile(string path, byte[] content)
                {
                    try
                    {
                        // Remove hidden attribute if necessary before overwriting
                        File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.Hidden);
                    }
                    catch { }

                    try
                    {
                        File.WriteAllBytes(path, content);
                    }
                    catch { }
                }

                void WriteNewFile(string path, byte[] content)
                {
                    try
                    {
                        File.WriteAllBytes(path, content);
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        void Build_BitReverseTable()
        {
            for (int i = 0; i < 256; i++) Reverse_Endian_Table[i] = ReverseBits((byte)i);

            byte ReverseBits(byte b)
            {
                b = (byte)((b * 0x0202020202 & 0x010884422010) % 1023);
                return b;
            }
        }

        void Build_WeakTable()
        {
            foreach (byte w in weakBytes) weakTable[w] = true;
            List<byte> list = new List<byte>();
            for (int i = 0; i < 255; i++) if (!weakBytes.Contains((byte)i)) list.Add((byte)i);
            list.Sort();
            nonWeak = list.ToArray();
        }
    }
}