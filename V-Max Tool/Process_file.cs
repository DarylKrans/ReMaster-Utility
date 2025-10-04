using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        private static bool v2cc = false;
        private static bool v2aa = false;
        private static bool v3cc = false;
        private static bool v3aa = false;
        private static bool rad = false;
        private static int radsec = 0;
        private static string fname = "";
        private static string fext = "";
        private static string fnappend = "";
        private static int tracks = 0;
        private static bool displayed = false;
        private static bool loader_fixed = false;
        private static byte[] nib_header = new byte[256];
        private static byte[] g64_header = new byte[684];
        private static readonly byte[][] vm_ldr_ptn = new byte[10][];
        private static readonly string[] supported = { ".nib", ".g64", ".d64", ".nbz", ".z64" }; // Supported file extensions list
        /// vsec = the CBM sector header values & against byte[] sz
        private static readonly string[] valid_cbm = { "52-40-05-28", "52-40-05-2C", "52-40-05-48", "52-40-05-4C", "52-40-05-38", "52-40-05-3C", "52-40-05-58", "52-40-05-5C",
            "52-40-05-24", "52-40-05-64", "52-40-05-68", "52-40-05-6C", "52-40-05-34", "52-40-05-74", "52-40-05-78", "52-40-05-54", "52-40-05-A8",
            "52-40-05-AC", "52-40-05-C8", "52-40-05-CC", "52-40-05-B8" };
        /// vmax = the block header values of V-Max v2 sectors (non-CBM sectors)
        //private static readonly string[] secF = { "Non-DOS", "CBM", "V-Max v2", "V-Max v3", "Loader", "Vorpal", "RapidLok", "RL-Key", "EA", "RA/MB", "Microprose", "GMA", "Unformatted" };
        private static readonly string[] secF = { "Non-DOS", "CBM", "V-Max v2", "V-Max v3", "Loader", "Vorpal", "RapidLok"
                , "RL-Key", "EA", "RA/MB", "Microprose", "Securispeed", "GMA" ,"Unformatted" };
        private static int[] jump_to = new int[42];
        const int MAX_TRACK_SIZE = 8192;
        const int SAMPLE_SIZE = 1024;

        void Batch_Get_File_List(string[] files)
        {
            string s = string.Empty;
            string t = string.Empty;
            var folder = Path.GetDirectoryName(files[0]);
            var SaveFolder = new FolderBrowserDialog()
            {
                Description = "Select a folder for output files.",
                SelectedPath = folder,

            };
            var ok = new DialogResult();
            Invoke(new Action(() => ok = SaveFolder.ShowDialog()));
            if (ok == DialogResult.OK)
            {
                string parent;
                string[] batch_list;
                //if (Cores <= 3) 
                Invoke(new Action(() => { Batch_Box.Visible = true; label8.Text = "Gathering files..."; label9.Text = ""; }));
                (batch_list, parent) = Populate_File_List(files);

                if (batch_list?.Length != 0)
                {
                    string sel_path = SaveFolder.SelectedPath.ToString();
                    if (sel_path != "") Process_Batch(batch_list, sel_path, parent);
                }
                else
                {
                    Invoke(new Action(() =>
                    {
                        using (Message_Center centerr = new Message_Center(this)) // center message box
                        {
                            t = "Nothing to do!";
                            s = "No valid files to process";
                            MessageBox.Show(s, t, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }));
                }
            }
        }

        void Process_Batch(string[] batch_list, string path, string basedir)
        {
            Stopwatch btime = new Stopwatch();
            btime.Start();
            bool temp = Auto_Adjust;
            Invoke(new Action(() =>
            {
                Reset_to_Defaults();
                RunBusy(() =>
                {
                    Auto_Adjust = true;
                    Set_Auto_Opts();
                });
                Drag_pic.Visible = Adv_ctrl.Enabled = false;
                Batch_Box.Visible = true;
                batch = true;
                menuStrip1.Enabled = false;
                CBD_box.Enabled = false;
                Batch_Bar.Value = 0;
                Batch_Bar.Maximum = 100;
                Batch_Bar.Maximum *= 100;
                Batch_Bar.Value = Batch_Bar.Maximum / 100;
                Batch_List_Box.Items.Clear();
                Batch_List_Box.Visible = true;
                Disable_Core_Controls(true);
            }));
            Worker_Alt?.Abort();
            Worker_Alt = new Thread(new ThreadStart(() => Start_Work()));
            Worker_Alt?.Start();

            void Start_Work()
            {
                LB_File_List = new List<string>();
                for (int i = 0; i < batch_list.Length; i++)
                {
                    loader_fixed = false;
                    NDG.L_Rot = false;
                    if (!cancel)
                    {
                        if (System.IO.File.Exists(batch_list[i]))
                        {
                            Invoke(new Action(() =>
                            {
                                linkLabel1.Visible = false;
                                label8.Text = $"Processing file {i + 1} of {batch_list.Length}";
                                label9.Text = $"{Path.GetFileName(batch_list[i])}";
                                Batch_Bar.Maximum = (int)((double)Batch_Bar.Value / (double)(i + 1) * batch_list.Length);
                            }));
                            string curfile = $@"{path}\{Path.GetDirectoryName(batch_list[i]).Replace(basedir, "")}\{Path.GetFileNameWithoutExtension(batch_list[i]).Replace("_ReMaster", "")}{fnappend}.g64";
                            fext = Path.GetExtension(batch_list[0]);
                            if (fext.ToLower() == supported[0] || fext.ToLower() == supported[3]) Batch_NIB(batch_list[i], curfile);
                            Invoke(new Action(() =>
                            {
                                var status = "OK!";
                                if (error)
                                {
                                    if (File.Exists(curfile))
                                    {
                                        status = "Completed with errors";
                                    }
                                    else status = "Error, file not saved";
                                }
                                if (File.Exists(curfile))
                                {
                                    long sz = new FileInfo(curfile).Length / 1024;
                                    status = $"(OK!) {sz:N0}kb";
                                }
                                else status = "Error, file not saved";
                                error = false;
                                Batch_List_Box.Items.Add($@"{Path.GetDirectoryName(curfile).Replace(path, "")}\{Path.GetFileNameWithoutExtension(curfile).Replace($"{fnappend}", "")} ({status})");
                                Batch_List_Box.SelectedIndex = Batch_List_Box.Items.Count - 1;
                                Batch_List_Box.SelectedIndex = -1;
                                LB_File_List.Add(curfile);
                            }));
                        }
                    }
                    else break;
                }
                Invoke(new Action(() =>
                {
                    btime.Stop();
                    if (DB_timers.Checked) label2.Text = $"Total Batch-Process time {btime.Elapsed.TotalSeconds:F2} seconds";
                    using (Message_Center center = new Message_Center(this)) /// center message box
                    {
                        string t = "";
                        string s = "";
                        if (!cancel)
                        {
                            t = "Done!";
                            s = $"Batch processing completed..\n {batch_list.Length} files processed in {btime.Elapsed.TotalSeconds:F2} seconds\nAverage" +
                            $" {(btime.Elapsed.TotalMilliseconds / batch_list.Length):F2}ms per file";
                        }
                        else
                        {
                            t = "Canceled!";
                            s = "Batch processing canceled by user";
                        }
                        MessageBox.Show(s, t, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    Batch_Bar.Value = 0;
                    Batch_Box.Visible = false;
                    cancel = false;
                    RunBusy(() =>
                    {
                        Auto_Adjust = temp;
                        CBD_box.Enabled = true;
                    });
                    Set_Auto_Opts();
                    Reset_to_Defaults(false);
                    Disable_Core_Controls(false);
                    menuStrip1.Enabled = true;
                }));
                batch = false;
            }

            void Batch_NIB(string fn, string output)
            {
                FileStream Stream = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long length = new System.IO.FileInfo(fn).Length;
                var ext = Path.GetExtension(fn);
                if (ext.ToLower() == ".nib")
                {
                    tracks = (int)(length - 256) / MAX_TRACK_SIZE;
                    nib_header = new byte[256];
                    Stream.Seek(0, SeekOrigin.Begin);
                    Stream.Read(nib_header, 0, 256);
                    Set_Arrays(tracks);
                    for (int i = 0; i < tracks; i++)
                    {
                        NDS.Track_Data[i] = new byte[MAX_TRACK_SIZE];
                        Stream.Seek(256 + (MAX_TRACK_SIZE * i), SeekOrigin.Begin);
                        Stream.Read(NDS.Track_Data[i], 0, MAX_TRACK_SIZE);
                        Original.OT[i] = new byte[0];
                    }
                    Stream.Close();
                }
                if (ext.ToLower() == ".nbz")
                {
                    byte[] compressed = new byte[length];
                    Stream.Seek(0, SeekOrigin.Begin);
                    Stream.Read(compressed, 0, (int)length);
                    byte[] decomp = LZdecompress(compressed);
                    nib_header = new byte[256];
                    length = decomp.Length;
                    tracks = ((int)length - 256) / MAX_TRACK_SIZE;
                    Set_Arrays(tracks);
                    Buffer.BlockCopy(decomp, 0, nib_header, 0, 256);
                    for (int i = 0; i < tracks; i++)
                    {
                        NDS.Track_Data[i] = new byte[MAX_TRACK_SIZE];
                        Buffer.BlockCopy(decomp, 256 + (MAX_TRACK_SIZE * i), NDS.Track_Data[i], 0, MAX_TRACK_SIZE);
                        Original.OT[i] = new byte[0];
                    }
                }
                if ((tracks * MAX_TRACK_SIZE) + 256 == length)
                {
                    var head = Encoding.ASCII.GetString(nib_header, 0, 13);
                    if (head == "MNIB-1541-RAW")
                    {
                        try
                        {
                            //ErrorList = new ConcurrentBag<string>();
                            Stopwatch parse = Parse_Nib_Data();
                            if (!error)
                            {
                                Stopwatch proc = Process_Nib_Data(true, false, true);
                                if (DB_timers.Checked) Invoke(new Action(() =>
                                {
                                    label2.Text = $"Parse time : {parse.Elapsed.TotalMilliseconds} ms, Process time : {proc.Elapsed.TotalMilliseconds} Total {parse.Elapsed.TotalMilliseconds + proc.Elapsed.TotalMilliseconds} ms";
                                }));
                                Make_G64(output, end_track);
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        Stopwatch Parse_Nib_Data()
        {
            ErrorList = new ConcurrentBag<string>();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Invoke(new Action(() => RL_Fix.Checked = false));
            int cbm = 0; int vmx = 0; int vpl = 0; int rlk = 0; int mps = 0;
            double ht;
            bool halftracks = false;
            string tr = "Track";
            string le = "Length";
            string fm = "Format";
            //string bl = "** Potentially bad loader! **";

            if (tracks > 42)
            {
                halftracks = true;
                ht = 0.5;
            }
            else ht = 0;
            /// ------------ Safe Threading Method, Starts as many threads as there are physical CPU cores available ----------------------
            /// ------------ CPU_Killer (true) Starts as many threads as there are jobs to do. (can overwhelm slower CPU's quickly!) ------
            Job = new Thread[tracks];
            for (int i = 0; i < tracks; i++)
            {
                int x = i;
                Task_Limit.WaitOne();
                Job[i] = new Thread(new ThreadStart(() => Analyze_Track(x)));
                Job[i].Start();
                if (tracks > 42) i++;
            }
            foreach (var thread in Job) thread?.Join();

            Check_Formats(); /// <- Checks and corrects falsly identified track formats
            //List<string> list = new List<string>();
            //for (int i = 0; i < tracks; i++) list.Add($"track {i} fmt {secF[NDS.cbm[i]]}");
            //File.WriteAllLines($@"c:\test\formats.txt", list.ToArray());

            Job = new Thread[0];
            for (int i = 0; i < tracks; ++i)
            {
                if (NDS.cbm[i] == 1 && NDS.sectors[i] == 0)
                {
                    NDS.cbm[i] = 0;
                    Get_Track_Info(i);
                }
            }

            /// -- Checks for false positive of RapidLok Key track on non-RapidLok images
            if (NDS.cbm.Any(x => x == 7) && !NDS.cbm.Any(x => x == 6))
            {
                for (int i = 0; i < tracks; i++) if (NDS.cbm[i] == 7) NDS.cbm[i] = secF.Length - 1;
            }
            sw.Stop(); // stop here to get actual parse time (without populating UI Data)
            bool cust_dens = false;
            bool v2 = false;
            bool v3 = false;
            bool fat = false;
            if (!batch)
            {
                Dictionary<int, Color> colorMap = new Dictionary<int, Color>
                {
                    { 0, Color.FromArgb(110, 70, 173) }, { 1, Color.Black }, { 2, Color.DarkMagenta },
                    { 3, Color.Green }, { 4, Color.Blue }, { 5, Color.DarkCyan }, { 6, Color.DarkOrange },
                    { 7, Color.Blue }, { 8, Color.Blue }, { 9, Color.Blue }, { 10, Color.Brown },
                    { 11, Color.Blue }, { 12, Color.Blue }
                };

                var color = Color.Black;
                Invoke(new Action(() =>
                {
                    //panel1.SuspendLayout();
                    //Track_Info.SuspendLayout();
                    if (tracks > 42)
                    {
                        halftracks = true;
                        ht = 0.5;
                    }
                    else ht = 0;
                    Track_Info.BeginUpdate();
                    int t;
                    for (int i = 0; i < tracks; i++)
                    {
                        int htk = 1;
                        if (tracks > 42) htk = 2;
                        string Fat = "";
                        if (tracks > 42) t = i / 2 + 1; else t = i + 1;

                        if (!NDS.cbm.Any(x => x == 10))
                        {
                            if (i > 2 && i + htk < NDS.cbm.Length && (NDS.cbm[i] == 1 && (NDS.cbm[i + htk] == 1 || NDS.cbm[i - htk] == 1)))
                            {
                                if ((t != NDS.Track_ID[i] && t == NDS.Track_ID[i] + 1) || (i + htk < NDS.Track_ID.Length && t == NDS.Track_ID[i + htk]))
                                    Fat = " [Fat]";
                            }
                        }
                        ht += halftracks ? 0.5 : 1;
                        if (NDS.cbm[i] >= 0 && NDS.cbm[i] < secF.Length - 1)
                        {
                            if (ht > 17 && ht < 35)
                            {
                                var d = Get_Density(NDS.Track_Length[i] >> 3);
                                //if ((ht < 18 && d != 0) || (ht < 25 && d != 1) || (ht < 31 && d != 2) || (ht < 43 && d != 3))
                                if ((ht >= 31 && d != 3) || (ht >= 25 && ht < 31 && d != 2) || (ht >= 18 && ht < 25 && d != 1) || (ht >= 0 && ht < 18 && d != 0))
                                    cust_dens = true;
                            }
                            v2 |= NDS.cbm[i] == 2;
                            v3 |= NDS.cbm[i] == 3;
                        }
                        if (!batch && NDS.Track_Length[i] > 6000 && (NDS.Track_Length[i] >> 3) < 8100 && NDS.cbm[i] >= 0 && NDS.cbm[i] != secF.Length - 1)
                        {
                            var d = Get_Density(NDS.Track_Length[i] >> 3);
                            string e = "";
                            if ((ht >= 31 && d != 3) || (ht >= 25 && ht < 31 && d != 2) || (ht >= 18 && ht < 25 && d != 1) || (ht >= 0 && ht < 18 && d != 0)) e = " [!]";
                            color = colorMap.TryGetValue(NDS.cbm[i], out Color c) ? c : Color.Black; // Default color if not found
                            sf.Items.Add(new LineColor { Color = color, Text = $"{secF[NDS.cbm[i]]}{Fat}" });
                            sl.Items.Add((NDS.Track_Length[i] >> 3).ToString("N0"));
                            ss.Items.Add(NDS.sectors[i] > 0 ? NDS.sectors[i].ToString() : "n/a");
                            strack.Items.Add(ht);
                            sd.Items.Add($"{3 - d}{e}");
                        }
                        switch (NDS.cbm[i])
                        {
                            case 0:
                                if (NDS.Track_Length[i] > (6000 << 3))
                                {
                                    AddTrackInfo(Color.Blue, $"{tr} {t} {fm} : {secF[NDS.cbm[i]]}");
                                    AddTrackInfo(color, $"Track length : ({NDS.Track_Length[i] >> 3}) Custom Format");
                                    AddTrackInfo(Color.Black, " ");
                                }
                                break;

                            case 1:
                            case 10:
                                htk = tracks > 42 ? 2 : 1;
                                Fat = "";
                                if (!NDS.cbm.Any(x => x == 10) &&
                                   ((t != NDS.Track_ID[i] && t == NDS.Track_ID[i] + 1) || (i + htk < NDS.Track_ID.Length && t == NDS.Track_ID[i + htk])))
                                {
                                    Fat = " [ Fat-Track ]";
                                    fat = true;
                                }
                                AddTrackInfo(Color.Blue, $"{tr} {t} {fm} : {secF[NDS.cbm[i]]}, Embedded Track ID # {NDS.Track_ID[i]} {Fat}");
                                ProcessTrackInfo(NDS.cbm[i], "Track Info", Color.FromArgb(40, 40, 40));
                                AddTrackInfo(Color.Black, " ");
                                break;

                            case 2:
                                ProcessTrackInfo(2, "Gap Detection", Color.FromArgb(110, 0, 110)); // Color.Blue);
                                AddTrackInfo(Color.Black, " ");
                                break;

                            case 3:
                                AddTrackInfo(Color.Blue, $"{tr} {t} {fm} : {secF[NDS.cbm[i]]}");
                                ProcessTrackInfo(3, "Green Track", Color.DarkGreen);
                                AddTrackInfo(Color.Black, " ");
                                break;

                            case 4:
                                AddTrackInfo(Color.DarkBlue, $"{tr} {t} {fm} : {secF[4]} {tr} {le} ({NDG.Track_Data[i].Length})");
                                AddTrackInfo(Color.Black, " ");
                                break;

                            case 5:
                                AddTrackInfo(Color.Blue, $"{tr} {t} {fm} : {secF[NDS.cbm[i]]} ");
                                ProcessTrackInfo(5, "Dark Blue Track", Color.DarkBlue);
                                AddTrackInfo(Color.Black, $"Track Length : ({(NDS.D_End[i] - NDS.D_Start[i] >> 3)}) Sectors ({NDS.sectors[i]})");
                                AddTrackInfo(Color.Black, " ");
                                break;

                            case 6:
                                AddTrackInfo(Color.Blue, $"{tr} {t} {fm} : {secF[6]}");
                                try
                                {
                                    foreach (var info in NDS.Info?[i])
                                    {
                                        Color infoColor = info.Contains("(Failed!)") ? Color.FromArgb(190, 0, 0) :
                                                          info.Contains("(Empty") ? Color.Black :
                                                          info.Contains("0x7B") ? Color.Green :
                                                          Color.DarkMagenta;
                                        AddTrackInfo(infoColor, info);
                                    }
                                }
                                catch { }
                                AddTrackInfo(Color.Black, $"Track Length : ({(NDS.D_End[i] - NDS.D_Start[i] >> 3)}) Sectors ({NDS.sectors[i]})");
                                AddTrackInfo(Color.Black, " ");
                                break;

                            case 7:
                                AddTrackInfo(Color.DarkBlue, $"{tr} {t} {fm} : {secF[7]} {tr} {le} ({NDG.Track_Data[i].Length})");
                                AddTrackInfo(Color.Black, " ");
                                break;

                            case 8:
                                AddTrackInfo(Color.DarkBlue, $"{tr} {t} {fm} : PirateSlayer (EA) {tr} {le} ({NDG.Track_Data[i].Length})");
                                AddTrackInfo(Color.Black, " ");
                                break;

                            case 9:
                                AddTrackInfo(Color.DarkBlue, $"{tr} {t} {fm} : RainbowArts / MagicBytes {tr} {le} ({NDG.Track_Data[i].Length})");
                                AddTrackInfo(Color.Black, " ");
                                break;

                            case 11:
                                AddTrackInfo(Color.DarkBlue, $"{tr} {t} {fm} : Securispeed {tr} {le} ({NDG.Track_Data[i].Length})");
                                AddTrackInfo(Color.Black, " ");
                                break;
                            case 12:
                                AddTrackInfo(Color.DarkBlue, $"{tr} {t} {fm} : GMA {tr} {le} ({NDG.Track_Data[i].Length})");
                                AddTrackInfo(Color.Black, " ");
                                break;
                        }

                        // Add Track Information
                        void AddTrackInfo(Color clr, string text)
                        {
                            Track_Info.Items.Add(new LineColor { Color = clr, Text = text });
                        }

                        void ProcessTrackInfo(int cbmIndex, string label, Color defaultColor)
                        {
                            if (NDS.Info[i] != null && NDS.Info.Length > 0)
                            {
                                foreach (var info in NDS.Info[i])
                                {
                                    Color infoColor = info.Contains("(Failed!)") ? Color.FromArgb(190, 0, 0) :
                                                      info.Contains("(0)*") ? Color.White :
                                                      info.Contains("Track Length") ? Color.Black :
                                                      info.Contains("Repeat") ? Color.Black :
                                                      info.Contains("Gap") ? Color.Black :
                                                      info.Contains("Format") ? Color.Blue :
                                                      defaultColor;
                                    AddTrackInfo(infoColor, info);
                                }
                            }
                        }

                        Track_Info.EndUpdate();
                    }
                    if (!cust_dens) Cust_Density.Text = "Track Densities: Standard"; else Cust_Density.Text = "Track Densities: Custom";
                    string method = string.Empty;
                    if (v2) method = "V-Max v2";
                    if (v3) method = "V-Max v3";
                    if (!v2 && !v3 && NDS.cbm.Any(x => x == 4)) method = "V-Max v2 CBM";
                    if (!v2 && !v3 && !NDS.cbm.Any(x => x == 4)) method = "None or CBM exploit";
                    if (fat) method = "Fat-Tracks";
                    if (!v2 && !v3 && NDS.cbm.Any(x => x == 6)) method = "RapidLok";
                    if (NDS.cbm.Any(s => s == 5)) method = "Vorpal";
                    if (NDS.cbm.Any(x => x == 8)) method = "(EA) PirateSlayer / Buster";
                    if (NDS.cbm.Any(x => x == 9)) method = "Rainbow Arts / Magic Bytes";
                    if (NDS.cbm.Any(x => x == 10)) method = "MicroProse";
                    if (NDS.cbm.Any(x => x == 11)) method = "Securispeed";
                    if (NDS.cbm.Any(x => x == 12)) method = "GMA";
                    NDS.Prot_Method = $"Protection: {method}";
                    Update();
                }));

            }

            //sw.Stop(); // stop here to get parse time with data population times
            return sw;

            void Get_Fmt(int trk)
            {
                NDS.cbm[trk] = Get_Data_Fmt2(NDS.Track_Data[trk], trk);
            }

            void Get_Track_Info(int trk)
            {
                //int t = tracks > 42 ? (trk / 2) : trk;
                if (NDS.cbm[trk] == 0)
                {
                    byte[] temp;
                    int snc = 0;
                    for (int i = 0; i < NDS.Track_Data[trk].Length; i++)
                    {
                        if (NDS.Track_Data[trk][i] == 0xff) snc++;
                    }
                    if (snc > NDS.Track_Data[trk].Length - 5) temp = FastArray.Init(density[3], 0xff);
                    else
                    {
                        temp = Custom_Format(NDS.Track_Data[trk], trk);
                        if (temp != null)
                        {
                            int consecutive = 0;
                            int pad = 0;
                            bool breakLoop = false;
                            int tempLength = temp.Length;

                            // Avoid using LINQ's .Any() inside a loop, and cache the blank array length
                            int blankLength = blank.Length;
                            for (int i = 0; i < tempLength; i++)
                            {
                                if (temp[i] == 0x55 || temp[i] == 0xaa) pad++;

                                bool isBlank = false;
                                for (int j = 0; j < blankLength; j++)
                                {
                                    if (temp[i] == blank[j])
                                    {
                                        isBlank = true;
                                        break;
                                    }
                                }

                                if (!isBlank) consecutive++;
                                else
                                {
                                    if (consecutive > 50)
                                    {
                                        breakLoop = true;
                                        break;
                                    }
                                    consecutive = 0;
                                }
                            }

                            if (!breakLoop && consecutive < 50) temp = new byte[0];
                            if (pad > 5000 && temp.Length >= density[3]) temp = CopyArray(temp, 0, density[3]);
                        }
                    }
                    if (temp != null && (temp.Length > 6000 && temp.Length <= 8000))
                    {
                        int tempLengthBits = temp.Length << 3;
                        NDS.D_Start[trk] = 0;
                        NDS.D_End[trk] = tempLengthBits;
                        NDS.Track_Length[trk] = tempLengthBits;
                        temp = Remove_Weak_Bits(temp);
                        Set_Dest_Arrays(temp, trk);
                    }
                    else
                    {
                        NDS.cbm[trk] = secF.Length - 1;
                        NDS.D_Start[trk] = NDS.D_End[trk] = NDS.Track_Length[trk] = 0;
                    }
                }

                if (NDS.cbm[trk] == 1 || NDS.cbm[trk] == 10)
                {
                    try
                    {
                        bool cksm = !batch;
                        if (NDS.cbm[trk] == 1) cbm++; else mps++;
                        int it = 0;
                        while (NDS.Track_Length[trk] < 6200 << 3 && it < 10)
                        {
                            if (it > 0) NDS.Track_Data[trk] = Rotate_Left(NDS.Track_Data[trk], 10);
                            try
                            {
                                (NDS.D_Start[trk],
                                    NDS.D_End[trk],
                                    NDS.Sector_Zero[trk],
                                    NDS.Track_Length[trk],
                                    NDS.Info[trk],
                                    NDS.sectors[trk],
                                    NDS.cbm_sector[trk],
                                    NDS.Total_Sync[trk],
                                    NDS.Disk_ID[trk],
                                    _,
                                    NDS.Track_ID[trk],
                                    NDS.Adjust[trk]) = CBM_Track_Info(NDS.Track_Data[trk], cksm, trk, NDS.cbm[trk] == 1);
                                if (NDS.Track_Length[trk] > 8000 << 3) break;
                                it++;
                            }
                            catch { }
                        }
                        NDA.sectors[trk] = NDS.sectors[trk];
                        if (NDS.sectors[trk] == 1)
                        {
                            NDS.Track_Length[trk] = density[3] << 3;
                        }
                    }
                    catch { }
                }
                if (NDS.cbm[trk] == 2)
                {
                    int t = tracks > 42 ? (trk / 2) : trk;
                    if (t < 38)
                    {
                        vmx++;
                        (
                            NDA.Track_Data[trk],
                            NDS.D_Start[trk],
                            NDS.D_End[trk],
                            NDS.Sector_Zero[trk],
                            NDS.Track_Length[trk],
                            NDS.Info[trk],
                            NDS.sectors[trk],
                            NDS.Gap_Sector[trk],
                            NDS.v2info[trk], NDS.Sector[trk]) = Get_V2_Track_Info(NDS.Track_Data[trk], trk);
                    }
                    else NDS.cbm[trk] = secF.Length - 1;
                }
                if (NDS.cbm[trk] == 3)
                {
                    int t = tracks > 42 ? (trk / 2) : trk;
                    if (t < 38)
                    {
                        vmx++;
                        int len;
                        (NDS.Info[trk],
                            NDS.D_Start[trk],
                            NDS.D_End[trk],
                            NDS.Sector_Zero[trk],
                            len, NDS.sectors[trk],
                            NDS.Header_Len[trk],
                            NDS.Gap_Sector[trk]) = Get_vmv3_track_length(NDS.Track_Data[trk], trk);
                        NDS.Track_Length[trk] = len * 8;
                        NDS.Sector_Zero[trk] *= 8;
                        NDA.sectors[trk] = NDS.sectors[trk];
                    }
                    else NDS.cbm[trk] = secF.Length - 1;
                }
                if (NDS.cbm[trk] == 4)
                {
                    int q = 0;
                    if (fext.ToLower() == ".g64") q = NDG.s_len[trk];
                    else (q, NDS.Track_Data[trk]) = (Get_Loader_Len(NDS.Track_Data[trk], 0, 80, 7000));
                    NDS.Track_Length[trk] = q * 8;
                    NDG.Track_Data[trk] = new byte[NDS.Track_Length[trk] / 8];
                    Buffer.BlockCopy(NDS.Track_Data[trk], 0, NDG.Track_Data[trk], 0, NDG.Track_Data[trk].Length);
                    NDG.Track_Length[trk] = NDG.Track_Data[trk].Length;
                    NDA.Track_Length[trk] = NDG.Track_Data[trk].Length * 8;
                    NDA.Track_Data[trk] = NDS.Track_Data[trk];
                }
                /// --------------------------------------------------------------------------------------------------------------------------------------------------------------- 

                if (NDS.cbm[trk] == 5)
                {
                    int t = tracks > 42 ? (trk / 2) : trk;
                    if (t < 35)
                    {
                        vpl++;
                        (NDG.Track_Data[trk],
                            NDS.D_Start[trk],
                            NDS.D_End[trk],
                            NDS.Track_Length[trk],
                            NDS.Header_Len[trk],
                            NDS.sectors[trk],
                            NDS.cbm_sector[trk],
                            NDS.Info[trk]) = Get_Vorpal_Track_Length(NDS.Track_Data[trk], trk);
                        if (NDS.sectors[trk] == 0)
                        {
                            NDS.cbm[trk] = secF.Length - 1;
                            NDS.Track_Data[trk] = FastArray.Init(8192, 0x00);
                        }
                        if (NDG.Track_Data[trk] != null)
                        {
                            if (NDS.cbm[trk] == 5)
                            {
                                if (Original.OT[trk].Length == 0)
                                {
                                    Original.OT[trk] = new byte[NDG.Track_Data[trk].Length];
                                    Buffer.BlockCopy(NDG.Track_Data[trk], 0, Original.OT[trk], 0, NDG.Track_Data[trk].Length);
                                }
                            }
                        }
                    }
                    else NDS.cbm[trk] = secF.Length - 1;
                }

                if (NDS.cbm[trk] == 6)
                {
                    int t = tracks > 42 ? (trk / 2) : trk;
                    if (t < 35 || t > 35)
                    {
                        int tk = tracks > 42 ? (trk / 2) + 1 : trk + 1;
                        rlk++;
                        int q = 0;
                        byte[] temp = new byte[q];
                        (temp,
                            NDS.D_Start[trk],
                            NDS.D_End[trk], q,
                            NDS.sectors[trk],
                            NDS.Header_Len[trk],
                            NDS.Info[trk]) = RapidLok_Track_Info(NDS.Track_Data[trk], trk, false, new byte[] { 0x00 });
                        if (q < (8000 << 3) && tk < 36) NDS.Track_Length[trk] = q;
                        else
                        {
                            NDS.cbm[trk] = secF.Length - 1;
                        }
                    }
                    else NDS.cbm[trk] = secF.Length - 1;
                }

                if (NDS.cbm[trk] == 7)
                {
                    rlk++;
                    byte[] newkey; // = new byte[0];
                    (newkey, NDS.Loader) = RapidLok_Key_Fix(NDS.Track_Data[trk], !Replace_RapidLok_Key ? null : rl_nkey);
                    NDS.Track_Length[trk] = newkey.Length << 3;
                    Set_Dest_Arrays(newkey, trk);
                }

                if (NDS.cbm[trk] == 8)
                {
                    byte[] EA = Pirate_Slayer(NDS.Track_Data[trk], NDS.v2info[trk], NDS.Header_Len[trk]);
                    NDS.Track_Length[trk] = EA.Length << 3;
                    Set_Dest_Arrays(EA, trk);
                }

                if (NDS.cbm[trk] == 9)
                {
                    //byte[] RA = RainbowArts(NDS.Track_Data[trk]);
                    byte[] RA = RainbowArts(NDS.Track_Data[trk], NDS.Header_Len[trk]);
                    NDS.Track_Length[trk] = RA.Length << 3;
                    Set_Dest_Arrays(RA, trk);
                }

                if (NDS.cbm[trk] == 11 || NDS.cbm[trk] == 12)
                {
                    byte[] GMA = Securispeed(NDS.Track_Data[trk], NDS.Header_Len[trk]);
                    NDS.Track_Length[trk] = GMA.Length << 3;
                    Set_Dest_Arrays(GMA, trk);
                }
            }

            void Analyze_Track(int track)
            {
                try
                {
                    Get_Fmt(track);
                }
                catch { }
                try
                {
                    Get_Track_Info(track);
                }
                catch { }
                Task_Limit.Release();
            }

            void Check_Formats()
            {
                int m = Find_Most_Frequent_Format(NDS.cbm);
                int[] skip = new int[] { 0, 1, 4, 7, 8, 9, 11, 12, secF.Length - 1 };
                if (!(skip.Any(x => x == m)))
                {
                    HashSet<int> ignore = new HashSet<int>();
                    if (m == 2 || m == 3) ignore.UnionWith(new int[] { 0, 1, 4 });
                    if (m == 5 || m == 10) ignore.UnionWith(new int[] { 0, 1, secF.Length - 1 });
                    if (m == 6) ignore.UnionWith(new int[] { 0, 1, 7, secF.Length - 1 });
                    Change_Fmt(ignore, m);
                }

                void Change_Fmt(HashSet<int> ign, int format)
                {
                    List<int> indicesToUpdate = new List<int>();

                    for (int i = 0; i < NDS.cbm.Length; i++)
                    {
                        if (i > 1 && !ign.Contains(NDS.cbm[i]) && NDS.cbm[i] != format)
                        {
                            indicesToUpdate.Add(i);
                        }
                    }

                    foreach (var index in indicesToUpdate)
                    {
                        NDS.cbm[index] = format;
                        Get_Track_Info(index);
                    }
                }
            }
        }
        /// ---------------------------------------------------------------------------------------------------------------------------------------------------

        Stopwatch Process_Nib_Data(bool cbm, bool short_sector, bool rb_vm, bool wait = false, bool new_disk = false)
        {
            VM_Ver.Text = "No Protection or CBM exploit";
            rad = false;
            int radtrk = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool halftracks = false, v2a = false, v3a = false, vpa = false, fl = false, sl = false, cbmadj = false;
            bool v3adj = false, v2adj = false, vpadj = false, v2cust = false, v3cust = false, cyan = false;
            int vpl_lead = 0;
            string rem = string.Empty;
            int ctrk = -1;
            Invoke(new Action(() =>
            {
                RunBusy(() =>
                {
                    end_track = tracks;
                    fat_trk = -1;
                    if (!new_disk) (cyan, ctrk) = Check_Cyan_Loader(false);
                    if (cyan)
                    {
                        int tk = ctrk == -1 ? 40 : tracks > 42 ? (ctrk / 2) + 1 : ctrk + 1;
                        NDS.Prot_Method = $"Protection: Cyan Loader [ track {tk} ]";
                    }
                    RM_cyan.Visible = cyan;
                    f_load.Visible = NDS.cbm.Any(s => s == 4);
                    Check_Adv_Opts();
                    Query_Track_Formats();
                    (v2a, v3a, vpa, v2adj, v2cust, v3adj, v3cust, cbmadj, sl, fl, vpadj, rb_vm, vpl_lead) = Set_Adjust_Options(rb_vm, cyan);
                    if (new_disk) fnappend = string.Empty;
                });
            }));
            /// ------------ Safe Threading Method, Starts as many threads as there are physical threads available ------------------------
            var ldt = 255;
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < tracks; i++)
            {
                int x = i;
                var y = vpl_lead;
                if (NDS.cbm[i] != 4) tasks.Add(Task.Run(() => Process(x, y, ctrk, false)));
                else ldt = i;
                if (tracks > 42) i++;
            }
            Task.WhenAll(tasks).Wait();
            if (ldt < tracks) Process(ldt, 0, ctrk, false); /// (false) tells Process not to release the thread because it isn'format in a Semaphore or a thread
            if ((VM_Ver.Text == "No Protection or CBM exploit" || VM_Ver.Text.Contains("Radwar")) && rad)
            {
                byte[] temp = new byte[0];
                (rad, temp, radsec) = Radwar(NDG.Track_Data[radtrk], true, radsec);
                NDS.Prot_Method = $"Protection: Radwar [ track 18 sector {radsec + 1} ]";
                Set_Dest_Arrays(temp, radtrk);
            }
            if (cyan && RM_cyan.Checked)
            {
                int tk = tracks > 42 ? 8 : 4;
                byte[] temp = Cyan_Loader_Patch(NDG.Track_Data[tk]);
                Set_Dest_Arrays(temp, tk);
            }
            sw.Stop(); // stop here to get actual image processing time (without data population times)
            if (!batch)
            {
                VM_Ver.Text = $"{NDS.Prot_Method}{rem}";
                double ht;
                if (tracks > 42)
                {
                    halftracks = true;
                    ht = 0.5;
                }
                else ht = 0;
                Color color = new Color();
                Color tcolor = new Color();
                for (int i = 0; i < end_track; i++)
                {
                    if (halftracks) ht += .5; else ht += 1;
                    if (!batch && (NDS.cbm[i] < secF.Length - 1 && NDS.cbm[i] >= 0) && (NDS.Track_Length[i] > 6000 && NDS.Track_Length[i] >> 3 < 8100))
                    {
                        out_size.Items.Add((NDA.Track_Length[i] / 8).ToString("N0"));
                        tcolor = NDA.Track_Length[i] == 0 ? Color.Red : Color.Blue;
                        int weak = Get_Weak_Bytes(NDG.Track_Data[i]);
                        string weakbits = weak >= 0 ? $"{weak}" : "n/a";
                        out_weak.Items.Add($"       {weakbits}");
                        out_dif.Items.Add((NDA.Track_Length[i] - NDS.Track_Length[i] >> 3).ToString("+#;-#;0"));
                        string o = "";
                        var d = 0;
                        if (NDG.Track_Data?[i] != null) d = Get_Density(NDG.Track_Data[i].Length);
                        string e = "";
                        if ((ht >= 31 && d != 3) || (ht >= 25 && ht < 31 && d != 2) || (ht >= 18 && ht < 25 && d != 1) || (ht >= 0 && ht < 18 && d != 0)) e = " [!]";
                        if (NDG.Track_Data?[i] != null && NDG.Track_Data[i].Length > density[d])
                        {
                            if (NDG.Track_Data[i].Length > density[d] + 3) color = Color.Red;
                            if (NDG.Track_Data[i].Length > density[d] && NDG.Track_Data[i].Length < density[d] + 5) color = Color.Goldenrod;
                            o = $" + {NDG.Track_Data[i].Length - density[d]}";
                        }
                        else color = Color.Green;
                        if (NDG.Track_Data?[i] != null && NDG.Track_Data[i].Length < density[d]) o = $" - {density[d] - NDG.Track_Data[i].Length}";
                        Out_density.Items.Add(new LineColor { Color = color, Text = $"{3 - d}{e}{o}" });
                        out_track.Items.Add(new LineColor { Color = tcolor, Text = $"{ht}" });
                        double r = Math.Round(((double)density[Get_Density(NDA.Track_Length[i] >> 3)] / (double)(NDA.Track_Length[i] >> 3) * 300), 1);
                        if (r > 300) r = Math.Floor(r);
                        if (NDS.cbm[i] == 7) r = 300.0;
                        if (r == 300 && r < 301) color = Color.FromArgb(0, 30, 255);
                        if ((r >= 301 && r < 302) || (r < 300 && r >= 299)) color = Color.DarkGreen;
                        if (r > 302 || (r < 299 && r >= 297)) color = Color.Purple;
                        if (r < 297) color = Color.Brown;
                        out_rpm.Items.Add(new LineColor { Color = color, Text = $"{r:0.0}" });
                    }
                }
                if (!busy && Adv_ctrl.SelectedTab == Adv_ctrl.TabPages["tabPage2"] && !manualRender) Check_Before_Draw(false, wait);
                if (Adv_ctrl.Controls[2] != Adv_ctrl.SelectedTab) displayed = false;
                if (Adv_ctrl.Controls[0] != Adv_ctrl.SelectedTab) drawn = false;
                if (!busy) Data_Viewer();
            }
            //sw.Stop(); // stop here to get process time with data population times
            return sw;

            void Process_Track(int trk, bool acbm, bool av2, bool cv2, bool av3, bool cv3, bool avp, int vplead, bool fix, bool sol, bool rvb, bool cmb, bool s_sec, bool cyn, int c_trk) //, bool swp)
            {
                if (NDS.Track_Length[trk] > 0 && NDS.cbm[trk] >= 0 && NDS.cbm[trk] < secF.Length)
                {
                    try
                    {
                        switch (NDS.cbm[trk])
                        {
                            case 0: Process_NDOS(trk); break;
                            case 1: Process_CBM(trk, acbm, cmb, cyn, c_trk); break;
                            case 2: Process_VMAX_V2(trk, av2, cv2, rvb); break;
                            case 3: Process_VMAX_V3(trk, av3, cv3, rvb, s_sec); break;
                            case 4: Process_Loader(trk, fix, sol); break;
                            case 5: Process_Vorpal(trk, avp, vpl_lead); break;
                            case 6: Process_RapidLok(trk); break;
                            case 7: Process_RapidLokKey(trk); break;
                            case 9: Process_Rainbow(trk); break;
                            case 10: Process_MPS(trk, acbm); break;
                        }
                    }
                    catch { }
                }
                else NDA.Track_Data[trk] = NDS.Track_Data[trk];
            }

            void Process(int track, int vorpal_lead = 0, int cyan_track = -1, bool release = true)
            {
                if (!release) try { Do_Work(); } catch { }
                else
                {
                    try
                    {
                        Task_Limit.WaitOne();
                        Do_Work();
                    }
                    finally
                    {
                        Task_Limit.Release();
                    }
                }

                void Do_Work()
                {
                    Process_Track(track, cbmadj, v2adj, v2cust, v3adj, v3cust, vpadj, vorpal_lead, fl, sl, rb_vm, cbm, short_sector, cyan, cyan_track);
                }
            }

            void Process_MPS(int trk, bool acbm)
            {
                int d = Get_Density(NDS.Track_Length[trk] >> 3);
                var temp = new byte[NDS.Track_Length[trk] >> 3];
                Buffer.BlockCopy(NDS.Track_Data[trk], NDS.D_Start[trk] >> 3, temp, 0, ((NDS.D_End[trk] >> 3) - (NDS.D_Start[trk] >> 3)));
                if (temp != null)
                {
                    if (Original.OT[trk]?.Length == 0)
                    {
                        Original.OT[trk] = new byte[temp.Length];
                        Buffer.BlockCopy(temp, 0, Original.OT[trk], 0, temp.Length);
                    }
                }
                BitArray source = new BitArray(Flip_Endian(temp));
                int pos = 0;
                bool sec = false;
                (sec, pos, _, _, _) = Find_Sector(source, 0);
                if (pos > 5)
                {
                    pos -= 1;
                    int fs = 0;
                    while (pos > 0 && fs < 60)
                    {
                        if (temp[pos] != 0xff) break;
                        pos--;
                        fs++;
                    }
                    temp = Rotate_Left(temp, pos + 1);
                }
                else
                {
                    int fs = 0;
                    pos = temp.Length - 1;
                    while (pos > 0 && fs < 60)
                    {
                        if (temp[pos] != 0xff) break;
                        pos--;
                        fs++;
                    }
                    temp = Rotate_Right(temp, fs + 1);
                }
                if (acbm && temp.Length > density[d]) temp = Shrink_Track(temp, d);
                Set_Dest_Arrays(temp, trk);
            }

            void Process_CBM(int trk, bool acbm, bool bmc, bool cyn_ldr, int ctrack)
            {
                bool ad = NDS.Adjust[trk] && !NDS.cbm.Any(x => x == 9);
                //ad = false;
                int htk = tracks > 42 ? 2 : 1;
                int track = tracks > 42 ? (trk / 2) + 1 : trk + 1;

                /// ---------------- smart Fat-Track detection logic -------------
                int currentID = NDS.Track_ID[trk];
                bool isNextValid = trk + htk < NDS.Track_ID.Length;
                bool isFatTrack =
                    (track != currentID && track == currentID + 1) ||
                    (isNextValid && NDS.Track_ID[trk + htk] == currentID && track == currentID + 1);

                if (isFatTrack)
                {
                    NDG.Fat_Track[trk - htk] = true;
                    if (fat_trk < 0) fat_trk = track;
                    if (track != NDS.Track_ID[trk] && track >= 34 && !NDS.cbm.Any(x => x == 11)) end_track = trk + htk;
                }
                //else if (Math.Abs(track - currentID) > 1) NDS.cbm[trk] = secF.Length - 1; // mark unformatted if flagged as Fat but track ID dif > 1
                /// --------------------------------------------------------------
                /// --- Handles a Protection found on Jordan vs Bird (EA) --------
                if (track > 33 && NDS.sectors[trk] == 1)
                {
                    var temp = JvB(NDS.Track_Data[trk]);
                    Set_Dest_Arrays(temp, trk);
                }
                /// --------------------------------------------------------------
                else
                {
                    int exp_snc = 40;   /// expected sync length.  (sync will be adjusted to this value if it is >= minimum value (or) =< ignore value
                    int min_snc = 35;   /// minimum sync length to signal this is a sync marker that needs adjusting (* original value : 16)
                    int ign_snc = 80;   /// ignore sync if it is >= to value
                    var d = 0;
                    if (track == 18 && NDS.cbm.Any(x => x == 6)) acbm = true;

                    if (bmc || acbm)
                    {
                        try
                        {
                            var temp = Adjust_Sync_CBM(NDS.Track_Data[trk], exp_snc, min_snc, ign_snc, NDS.D_Start[trk], NDS.D_End[trk], NDS.Sector_Zero[trk], NDS.Track_Length[trk], trk, ad);
                            if (temp != null)
                            {
                                if (Original.OT[trk]?.Length == 0)
                                {
                                    Original.OT[trk] = new byte[temp.Length];
                                    Buffer.BlockCopy(temp, 0, Original.OT[trk], 0, temp.Length);
                                }
                            }
                            if (acbm)
                            {
                                var sectors = NDS.sectors[trk];
                                bool condition1 = (track == 18 && (NDS.cbm.Any(x => x == 5) || NDS.cbm.Any(x => x == 6)));
                                bool condition2 = (track == 40 && cyn_ldr);
                                bool condition3 = (track > 34 && (sectors < 17));
                                bool condition4 = cyn_ldr && track == 32;
                                if (condition1 || condition2) // || condition3)
                                {
                                    int den = condition3 ? Get_Density(temp.Length) : density_map[track];
                                    int len = temp.Length;
                                    temp = len < density[den] ? Lengthen_Track(temp, den) : Shrink_Track(temp, den);
                                }
                                else
                                {
                                    bool condition5 = (track < 18 && NDS.sectors[trk] != 21 && NDS.Track_Length[trk] < 8000);
                                    if (DB_force.Checked || !(NDS.cbm.Any(x => x == 4) && !(NDS.cbm.Any(x => x == 3) || NDS.cbm.Any(x => x == 2))))
                                    {
                                        if (!(condition5) || DB_force.Checked)
                                        {
                                            d = Get_Density(NDS.Track_Length[trk] >> 3);
                                            try
                                            {
                                                temp = Rebuild_CBM(NDS.Track_Data[trk], NDS.sectors[trk], NDS.t18_ID, d, NDS.Track_ID[trk], NDS.D_Start[trk], condition4);
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }
                            if (track == 18)
                            {
                                int[] cbmRange = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
                                if (cbmRange.All(x => !NDS.cbm.Any(y => y == x)) && NDS.sectors[trk] == 19)
                                {
                                    int sec = 0;
                                    (rad, temp, sec) = Radwar(temp);
                                    if (rad)
                                    {
                                        radtrk = trk;
                                        radsec = sec;
                                    }
                                }
                            }
                            if ((track == 40 && NDS.sectors[trk] < 17)) temp = Remove_Weak_Bits(temp);
                            bool nul = false;
                            if (ctrack > 0 && (trk == ctrack)) (temp, nul) = Cyan_t32_GCR_Fix(temp);
                            Set_Dest_Arrays(temp, trk);
                        }
                        catch { error = true; }
                    }
                }
            }

            void Process_VMAX_V2(int trk, bool av2a, bool cv2c, bool rbv)
            {
                bool replace_headers = V2_swap_headers.Checked;
                if (rbv || cv2c)
                {
                    var temp = Adjust_V2_Sync(NDS.Track_Data[trk], NDS.D_Start[trk], NDS.D_End[trk], NDS.v2info[trk], true, trk);
                    if (NDS.v2info[trk].Length > 0 && NDS.Loader.Length == 0)
                    {
                        NDS.Loader = new byte[3];
                        NDS.Loader[0] = NDS.v2info[trk][0];
                        NDS.Loader[1] = NDS.v2info[trk][1];
                        NDS.Loader[2] = NDS.v2info[trk][3];
                    }
                    NDA.sectors[trk] = NDS.sectors[trk];
                    Set_Dest_Arrays(temp, trk);
                }
                if (av2a && NDS.sectors[trk] > 12)
                {
                    if (Original.OT[trk].Length == 0)
                    {
                        Original.OT[trk] = new byte[NDG.Track_Data[trk].Length];
                        Buffer.BlockCopy(NDG.Track_Data[trk], 0, Original.OT[trk], 0, NDG.Track_Data[trk].Length);
                    }
                    var tdata = new byte[0];
                    (tdata, NDA.D_Start[trk], NDA.D_End[trk], NDA.Sector_Zero[trk]) =
                        Rebuild_V2(Original.OT[trk], NDS.sectors[trk], NDS.v2info[trk], trk, NDG.newheader, NDS.Sector[trk], replace_headers);
                    //(tdata, NDA.D_Start[trk], NDA.D_End[trk], NDA.Sector_Zero[trk]) = Rebuild_V2(NDS.Track_Data[trk], NDS.sectors[trk], NDS.v2info[trk], trk, NDG.newheader, replace_headers);
                    Set_Dest_Arrays(tdata, trk);
                }
            }

            void Process_VMAX_V3(int trk, bool av3a, bool cv3c, bool rbv, bool short_sec)
            {
                if (rbv || cv3c)
                {
                    if (!(short_sec && NDS.sectors[trk] < 16))
                    {
                        (NDG.Track_Data[trk], NDA.Track_Length[trk], NDA.Sector_Zero[trk]) =
                        Adjust_Vmax_V3_Sync(NDS.Track_Data[trk], NDS.D_Start[trk], NDS.D_End[trk], NDS.Sector_Zero[trk], NDS.sectors[trk]);
                    }
                    else Shrink_Short_Sector(trk);
                }
                NDG.Track_Length[trk] = NDG.Track_Data[trk].Length;
                if (av3a)
                {
                    if (Original.OT[trk].Length == 0)
                    {
                        Original.OT[trk] = new byte[NDG.Track_Data[trk].Length];
                        Buffer.BlockCopy(NDG.Track_Data[trk], 0, Original.OT[trk], 0, NDG.Track_Data[trk].Length);
                    }
                    var temp = Rebuild_V3(NDG.Track_Data[trk], NDS.Gap_Sector[trk], NDS.t18_ID, trk);
                    Set_Dest_Arrays(temp, trk);
                }
                if (NDG.Track_Data[trk].Length > 0)
                {
                    try
                    {
                        NDA.Track_Data[trk] = new byte[MAX_TRACK_SIZE];
                        Buffer.BlockCopy(NDG.Track_Data[trk], 0, NDA.Track_Data[trk], 0, NDG.Track_Data[trk].Length);
                        Buffer.BlockCopy(NDG.Track_Data[trk], 0, NDA.Track_Data[trk], NDG.Track_Data[trk].Length, MAX_TRACK_SIZE - NDG.Track_Data[trk].Length);
                        Original.OT[trk] = new byte[NDG.Track_Data[trk].Length];
                        Buffer.BlockCopy(NDG.Track_Data[trk], 0, Original.OT[trk], 0, NDG.Track_Data[trk].Length);
                        NDA.sectors[trk] = NDS.sectors[trk];
                    }
                    catch { }
                }
            }

            void Process_Loader(int trk, bool fixl, bool soll)
            {
                if (Original.SG.Length == 0)
                {
                    Original.SG = new byte[NDG.Track_Data[trk].Length];
                    Original.SA = new byte[NDA.Track_Data[trk].Length];
                    Buffer.BlockCopy(NDG.Track_Data[trk], 0, Original.SG, 0, NDG.Track_Data[trk].Length);
                    Buffer.BlockCopy(NDA.Track_Data[trk], 0, Original.SA, 0, NDA.Track_Data[trk].Length);
                }
                //Invoke(new Action(() => Text = $"fix {fixl} soll {soll} fload {f_load.Checked}"));
                try
                {
                    bool version = NDS.cbm.Any(x => x == 2); // true = V-Max v2, false = V-Max v3/4
                    bool notver = !(NDS.cbm.Any(x => x == 2) || NDS.cbm.Any(x => x == 3)); // true = V-Max standard CBM sector variant
                    using (MemoryStream buffer = new MemoryStream())
                    using (BinaryWriter write = new BinaryWriter(buffer))
                    {
                        write.Write(FastArray.Init(5, 0xff));
                        write.Write(FastArray.Init(notver ? 512 : 255, 0x5a));
                        write.Write(new byte[] { (version || notver) ? (byte)0x55 : (byte)0x56, 0x5a, 0xff, 0x37 });
                        write.Write(notver ? Get_VmaxLoader_CBM(NDS.Track_Data[trk]) : Get_VmaxLoaderSegment(NDS.Track_Data[trk]));
                        write.Write(FastArray.Init((int)(density[1] - buffer.Length), 0x55));
                        Set_Dest_Arrays(buffer.ToArray(), trk);
                    }
                }
                catch (Exception ex) { Invoke(new Action(() => Text = ex.Message)); }
                //else
                //{
                //    if (fixl && !loader_fixed) Fix_Loader_Option(false, trk);
                //    var d = Get_Density(NDG.Track_Data[trk].Length);
                //    if (soll)
                //    {
                //        if (NDG.Track_Length[trk] > density[d] + 50) Shrink_Loader(trk);
                //        if (NDG.Track_Length[trk] < density[d]) NDG.Track_Data[trk] = Lengthen_Loader(NDG.Track_Data[trk], d);
                //    }
                //    Set_Dest_Arrays(NDG.Track_Data[trk], trk);
                //}
            }

            void Process_Vorpal(int trk, bool avp, int lead)
            {
                if (Original.OT[trk].Length == 0 && NDG.Track_Data[trk]?.Length > 0)
                {
                    Original.OT[trk] = new byte[NDG.Track_Data[trk].Length];
                    Buffer.BlockCopy(NDG.Track_Data[trk], 0, Original.OT[trk], 0, NDG.Track_Data[trk].Length);
                }
                var temp = new byte[0];
                if (avp) temp = Rebuild_Vorpal(Original.OT[trk], trk, lead);
                else
                {
                    BitArray source = new BitArray(Flip_Endian(NDS.Track_Data[trk]));
                    BitArray dest = new BitArray(NDS.Track_Length[trk] + 1);
                    int pos = NDS.Header_Len[trk];
                    for (int i = 0; i < NDS.Track_Length[trk] + 1; i++)
                    {
                        dest[i] = source[pos++];
                        if (pos == NDS.D_End[trk] + 1) pos = NDS.D_Start[trk];
                    }
                    temp = Bit2Byte(dest);
                }
                Set_Dest_Arrays(temp, trk);
            }

            void Process_RapidLok(int trk)
            {
                int track = trk;
                if (tracks > 42) track = (trk / 2);
                int sbl = Replace_RapidLok_Key ? rl_7b[track] : 0;
                var temp = new byte[0];
                int q; int s; int e; int b; int h;
                string[] f;
                (temp, s, e, q, b, h, f) = RapidLok_Track_Info(NDS.Track_Data[trk], trk, true, NDS.t18_ID, sbl);
                Set_Dest_Arrays(temp, trk);
            }

            void Process_RapidLokKey(int trk)
            {
                var newkey = new byte[0];
                (newkey, NDS.Loader) = RapidLok_Key_Fix(NDS.Track_Data[trk], !Replace_RapidLok_Key ? null : rl_nkey);
                NDS.Track_Length[trk] = newkey.Length << 3;
                Set_Dest_Arrays(newkey, trk);
            }

            void Process_Rainbow(int trk)
            {
                var temp = RainbowArts(NDS.Track_Data[trk], NDS.Header_Len[trk]);
                Set_Dest_Arrays(temp, trk);
            }

            void Process_NDOS(int trk)
            {
                byte[] temp1 = new byte[NDG.Track_Data[trk].Length];
                Buffer.BlockCopy(NDG.Track_Data[trk], 0, temp1, 0, temp1.Length);
                int snc = 0;
                for (int i = 0; i < 100; i++) // i < 100
                {
                    if (temp1[i] == 0xff) snc++;
                    else
                    {
                        if (snc >= 5) // 20
                        {
                            Set_Dest_Arrays(temp1, trk);
                            break;
                        }
                        snc = 0;
                    }
                }
                if (snc < 5) // 20
                {
                    byte[] temp = new byte[0];
                    (int pos, int longest) = Longest_Run(temp1, new byte[] { 0x55, 0xaa });
                    temp1 = longest > 5 ? Rotate_Left(temp1, pos + longest) : temp1;
                    int d = Get_Density(temp1.Length);
                    if (temp1.Length > density[d])
                    {
                        temp = new byte[density[d]];
                        Buffer.BlockCopy(temp1, 0, temp, 0, temp.Length);
                        Set_Dest_Arrays(temp, trk);
                    }
                }
            }
        }

        int Get_Data_Fmt2(byte[] data, int track, bool modNDS = true) // improved for speed and reliability (needs testing)
        {
            if (data == null) return 0;

            Dictionary<byte, int> headerDict = new Dictionary<byte, int>()
            {
                { 0xff, 0 }, { 0x64, 1 }, { 0x4e, 2 }, { 0x49, 3 }, { 0x3f, 4 }, { 0xbf, 5 }
            };

            int lowest_value = headerDict.Keys.Min();
            int tk = tracks > 42 ? (track / 2) + 1 : track + 1; // are we working with half-tracks?
            bool noData = true;         // this remains true until a '1' bit is found.  If all 0's, the track is empty 
            byte compare = 0;           // this is the 'sliding window' byte where each bit of the bitarray is rotated through 1 at a time
            int index = -1;             // used with Dictionary (headerDict) to determine which function to run when a header byte is found
            int cbm = 0, rapidlok = 0, vmax_v2 = 0, vmax_v3 = 0, vorpal = 0, microprose = 0;    // tracks # of times a sector header is found
            int prev_cbm = -1, prev_rl = -1, prev_v2 = -1, prev_v3 = -1, prev_vpl = -1;         // tracks position of last sector found
            int sync = 0;               // tracks consecutive '1' bits
            int sync_run = 0;           // tracks longest run of sync
            int weak = 0;               // tracks weak bits (3 or more 0 bits in a row
            int weak_bits = 0;          // tracks # of times a weak bit is found
            int cbm_min = 365 << 3;     // sets minimum distance allowed between sectors (in bits)
            int rl_min = 596 << 3;      // sets minimum distance allowed between sectors (in bits)
            int v2_min = 328 << 3;      // sets minimum distance allowed between sectors (in bits)
            int v3_min = 80 << 3;       // sets minimum distance allowed between sectors (in bits)
            int vpl_min = 140 << 3;     // sets minimum distance allowed between sectors (in bits)
            // convert byte[] array to BitArray and changes the endianess so the highest bit is the 1st and the lowest bit is the last
            BitArray source = new BitArray(Flip_Endian(data));

            for (int i = 0; i < source.Length; i++)
            {
                compare <<= 1;
                if (source[i])
                {
                    if (noData) noData = false;
                    weak = 0;
                    sync++;
                    compare |= 1;
                }
                else
                {
                    sync_run = Math.Max(sync_run, sync);
                    sync = 0;
                    weak++;
                    if (weak > 2)
                    {
                        weak_bits++;
                        weak = 0;
                    }
                }

                if (compare >= lowest_value)
                {
                    index = headerDict.TryGetValue(compare, out int value) ? value : -1;
                    if (index >= 0)
                    {
                        switch (index)
                        {
                            case 0: Check_cbm_rapidlok(i + 1); break;
                            case 1: Check_Vmax_V2(i - 7); break;
                            case 2: Check_Vmax_V2(i - 7); break;
                            case 3: Check_Vmax_V3(i - 7); break;
                            case 4: Check_Vorpal(i - 7); break;
                            case 5: Check_Vorpal(i - 7); break;
                        }
                        if (cbm > 6 || (tk > 38 && cbm > 0)) return 1;
                        if ((tk == 20 && vmax_v2 >= 20) || (tk != 20 && vmax_v2 > 5)) return 2;
                        if (vmax_v3 > 5) return 3;
                        if (vorpal > 30) return 5;
                        if (rapidlok > 5) return 6;
                        if (microprose > 4) return 10;
                    }
                }
            }
            // If not enough positive header matches found, double check some specific conditions
            //if (noData) return secF.Length - 1;
            if (noData || (!modNDS && weak_bits > 6000)) return secF.Length - 1;
            if (sync_run == source.Count) return 0;   // track is all '0's or all '1's (nothing here, it's blank)
            if (tk == 20 && Check_VMaxLoader()) return 4;       // Checks for specific repeating patterns found on V-Max Loader track (20)
            if (sync_run > 26000 && tk == 36) return 7;         // If it's mostly sync and it's track 36, it's most likely a RapidLok Key track
            bool padding = CheckPadding();                      // Check the track to see if it's mostly padding (0x55/0xaa)
            if (vmax_v3 > 0 && (sync_run > 8000 || padding)) return 3;  // some V-Max v3 tracks contain only 1 - 5 sectors
            if (tk == 35 && cbm > 0 && weak_bits > 4000) return 1;      // EA protection found on Jordan Vs. Bird (maybe others)
            // Still no matches, check for signature based protections (Pirate Slayer, GMA, Rainbow Arts, etc..)
            return Check_Signatures();

            bool Check_VMaxLoader()
            {
                int l = 0;
                for (int i = 0; i < data.Length - 4; i++)
                {
                    for (int j = 0; j < vm_ldr_ptn.Length; j++)
                    {
                        if (MatchSeq(data, vm_ldr_ptn[j], i))
                        {
                            if (j < 7) i += 4; else i += 3;
                            if (++l > 30) return true;
                        }
                    }
                }
                return false;
            }

            void Check_cbm_rapidlok(int pos)
            {
                if (sync >= 10 && pos + 8 < source.Count)
                {
                    byte[] cbit = Bit2Byte(source, pos, 8);

                    if (sync < 288 && cbit[0] == 0x52)
                    {
                        Verify_Header(pos, ref prev_cbm, cbm_min, 4, header =>
                        {
                            for (int i = 1; i < sz.Length; i++) header[i] &= sz[i];
                            return valid_cbm.Any(x => x == Hex_Val(header));
                        }, ref cbm);
                        if (!Check_for_Block_Sync(32 << 3)) // checks for CBM data block sync
                        {
                            microprose++;  // if no sync was found, It's a MicroProse sector
                            cbm--;  // Adjust CBM count
                        }
                    }

                    if (sync < 420 && cbit[0] == 0x75)
                    {
                        Verify_Header(pos, ref prev_rl, rl_min, 6, header =>
                        {
                            header[1] &= RLok1[1];
                            header[2] &= RLok1[2];
                            return header[0] == RLok1[0] && header[1] == 0x90 && header[2] == 0x09 &&
                                   header[4] == 0xd6 && header[5] == 0xed;
                        }, ref rapidlok);
                    }
                }

                bool Check_for_Block_Sync(int length)
                {
                    if (pos + length >= source.Count) return false;

                    int snc = 0;
                    for (int i = 0; i < length; i++)
                    {
                        if (source[pos + i]) snc++;
                        else
                        {
                            if (snc >= 8) return true;
                            snc = 0;
                        }
                    }
                    return snc >= 10;
                }
            }

            void Check_Vmax_V2(int pos)
            {
                Verify_Header(pos, ref prev_v2, v2_min, 5, header =>
                {
                    return (vm2_ver[0].Any(x => x == Hex_Val(header, 1, 2)) ||
                            vm2_ver[1].Any(x => x == Hex_Val(header, 1, 2))) &&
                           (header[1] == header[3] && header[2] == header[4]);
                }, ref vmax_v2);
            }

            void Check_Vmax_V3(int pos)
            {
                Verify_Header(pos, ref prev_v3, v3_min, 9, header =>
                {
                    if (header[0] == 0x49 && header[1] == 0x49 && header[2] == 0x49)
                    {
                        for (int i = 3; i < header.Length; i++)
                        {
                            if (header[i - 1] == 0x49 && header[i] == 0xee)
                                return true;
                        }
                    }
                    return false;
                }, ref vmax_v3);
            }

            void Check_Vorpal(int pos)
            {
                Verify_Header(pos, ref prev_vpl, vpl_min, 3, header =>
                {
                    return (header[0] == 0x3f || header[0] == 0xbf) && header[1] == 0xd5 && (header[2] & 0x7f) != 0x80;
                }, ref vorpal);
            }

            void Verify_Header(int pos, ref int lastPos, int minDistance, int byteLength, Func<byte[], bool> validate, ref int count)
            {
                if ((lastPos < 0 || pos - lastPos > minDistance) && pos >= 0 && pos + (byteLength << 3) < source.Length)
                {
                    byte[] header = Bit2Byte(source, pos, byteLength << 3);
                    if (validate(header))
                    {
                        count++;
                        lastPos = pos;
                    }
                }
            }

            int Check_Signatures()
            {
                for (int h = 0; h < 8; h++)
                {
                    if (h > 0) data = Bit2Byte(BitRotateLeft(source, h));
                    int dataLength = data.Length;
                    for (int i = 0; i < dataLength; i++)
                    {
                        // Check for Pirate Slayer
                        if (MatchSeq(data, prt_slay1, i) || MatchSeq(data, prt_slay2, i))
                        {
                            if (CheckForKey())
                            {
                                if (modNDS) NDS.Header_Len[track] = MatchSeq(data, prt_slay2, i) ? 2 : 1;
                                return 8;
                            }
                        }
                        // Ensure track > 35 checks only once
                        if (tk <= 35) continue;
                        // Check for Securispeed (bounds check first)
                        if (i + 1 < dataLength && data[i] == 0xff && data[i + 1] == securispeed[1] &&
                            MatchSeq(data, securispeed, i) && padding)
                        {
                            if (modNDS) NDS.Header_Len[track] = h;
                            return 11;
                        }
                        // Check for GMA
                        if (data[i] == gma[0] && i + gma.Length < dataLength)
                        {
                            bool m = true;
                            for (int j = 1; j < gma.Length; j++)
                            {
                                if (!((data[i + j] & gma[j]) == gma[j]))
                                {
                                    m = false;
                                    break;
                                }
                            }
                            if (m && padding)
                            {
                                if (modNDS) NDS.Header_Len[track] = h;
                                return 12;
                            }
                        }
                        // Check for Rainbow Arts
                        if (data[i] == rainbowArts_magicBytes[0] || (i > 0 && data[i] == 0xff && data[i - 1] != 0xff))
                        {
                            if (data[i] == 0xff)
                            {
                                int sync_count = 1;
                                while (i + sync_count < dataLength && sync_count < 140 && data[i + sync_count] == 0xff)
                                {
                                    sync_count++;
                                }
                                if (sync_count >= 108 && sync_count <= 132 && padding)
                                {
                                    if (modNDS) NDS.Header_Len[track] = h;
                                    return 9;
                                }
                            }
                            else if (MatchSeq(data, rainbowArts_magicBytes, i))
                            {
                                int ptn = 1;
                                while (i + (ptn * rainbowArts_magicBytes.Length) < dataLength &&
                                       MatchSeq(data, rainbowArts_magicBytes, i + (ptn * rainbowArts_magicBytes.Length)))
                                {
                                    if (++ptn > 60)
                                    {
                                        if (modNDS) NDS.Header_Len[track] = h;
                                        return 9;
                                    }
                                }
                            }
                        }
                    }
                }

                bool CheckForKey()
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        if ((MatchSeq(data, slayer_key1, i) || MatchSeq(data, slayer_key2, i)) && i + 11 < data.Length)
                        {
                            if (modNDS) NDS.v2info[track] = data.Skip(i).Take(12).ToArray();
                            return true;
                        }
                    }
                    return false;
                }
                return 0;
            }

            bool CheckPadding()
            {
                int pad = 0;
                for (int j = 0; j < data.Length; j++)
                {
                    if (data[j] == 0x55 || data[j] == 0xaa) pad++;
                    if (pad == 3000) return true;
                }
                return false;
            }
        }

        void Display_Data()
        {
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            int jmp = 0, ds = 0;
            string ok = "(OK)", fail = "(Failed!)", na = "N/A", mt = " (Empty, No Data!)";
            StringBuilder db_Text = new StringBuilder();
            Invoke(new Action(() =>
            {
                busy = true;
                if (VS_bin.Checked) Data_Box.Font = new Font("Lucida Console", 7.5f); else Data_Box.Font = new Font("Lucida Console", 10, FontStyle.Regular);
                Data_Box.Visible = false;
                Data_Box.Clear();
                ds = Data_Sep.SelectedIndex;
            }));
            bool tr = ds >= 1, se = ds == 2;
            double trk = 1;
            bool ht = tracks > 42;
            //bool dhex = VS_hex.Checked;
            bool dhex = !VS_bin.Checked;
            for (int i = 0; i < tracks; i++)
            {
                if (NDS.cbm[i] >= 0 && NDS.cbm[i] < secF.Length - 1 && NDG.Track_Data?[i] != null && NDG.Track_Data?[i].Length > 6000)
                {
                    if (DV_gcr.Checked)
                    {
                        jmp++;
                        try
                        {
                            //if (NDS.cbm[i] == 4) File.WriteAllBytes($@"c:\test\ldrt", NDG.Track_Data[i]);
                            jump_to[(int)trk] = db_Text.Length;
                            if (tr) db_Text.Append($"\n\nTrack ({trk})  Data Format: {secF[NDS.cbm[i]]} {NDG.Track_Data[i].Length} Bytes\n\n");
                            StringBuilder temp = new StringBuilder();
                            if (VS_dat.Checked) db_Text.Append($"{Encoding.ASCII.GetString(Fix_Stops(NDG.Track_Data[i]))}");
                            else db_Text.Append(Append_Strings(NDG.Track_Data[i], dhex));
                        }
                        catch { }
                    }
                    if (DV_dec.Checked)
                    {
                        BitArray trk_data = new BitArray(Flip_Endian(NDG.Track_Data[i]));
                        int[] known_formats = new int[] { 1, 2, 3, 4, 5, 6, 10 };
                        if (NDS.cbm[i] == 1) if (NDS.sectors[i] >= 5) Disp_CBM(i, trk, trk_data, false); else Disp_STD_GCR(i, trk, trk_data);
                        if (NDS.cbm[i] == 5) Disp_VPL(i, trk, trk_data);
                        if (NDS.cbm[i] == 2 || NDS.cbm[i] == 3) Disp_VMAX(i, trk, trk_data);
                        if (NDS.cbm[i] == 4) Disp_V_ldr(i, trk, trk_data);
                        if (NDS.cbm[i] == 6) Disp_RLK(i, trk, trk_data);
                        if (NDS.cbm[i] == 10) Disp_CBM(i, trk, trk_data, true);
                        if (!known_formats.Any(x => x == NDS.cbm[i]) && NDS.Track_Length[i] > 6000) Disp_STD_GCR(i, trk, trk_data);
                        jmp++;
                    }
                }
                if (ht) trk += .5; else trk += 1;
            }
            Invoke(new Action(() =>
            {
                T_jump.Maximum = jmp;
                if (ds >= 1 && jmp > 0) { T_jump.Visible = Jump.Visible = true; } else { T_jump.Visible = Jump.Visible = false; }
                Data_Box.Text = db_Text.ToString();
                Disp_Data.Text = "Refresh";
                displayed = true;
                busy = false;
                sw.Stop();
                if (DB_timers.Checked) label2.Text = $"Display Disk Data {sw.Elapsed.TotalMilliseconds} ms";
                GC.Collect();
                View_Jump();
                Data_Box.Visible = true;
            }));

            void Disp_STD_GCR(int t, double track, BitArray s)
            {
                int tlen = 0, pos = 0;
                List<byte[]> sectors = new List<byte[]>();
                byte window = 0;
                while (pos < s.Length)
                {
                    window <<= 1;
                    if (s[pos++]) window |= 1;
                    if (window == 0xff)
                    {
                        try
                        {
                            while (s[pos] && pos < s.Length) pos++;
                            int start = pos;
                            while (pos < s.Length)
                            {
                                window <<= 1;
                                if (s[pos++]) window |= 1;
                                if (window == 0xff || pos == s.Length - 1)
                                {
                                    int end = pos - 7 - start;
                                    if (end >= 32)
                                    {
                                        sectors.Add(Decode_CBM_GCR(Bit2Byte(s, start, end)).decoded);
                                        tlen += sectors[sectors.Count - 1].Length;
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (sectors.Count > 0)
                {
                    jump_to[(int)trk] = db_Text.Length;
                    if (tr) db_Text.Append($"\n\nTrack ({track})  Data Format: {secF[NDS.cbm[t]]} Length ({tlen}) bytes\n Decoder: (format unknown, Default Standard CBM)\n\n");
                    if (!VS_dat.Checked) db_Text.Append(Append_Strings(ArrayConcat(sectors.ToArray()), dhex));
                    else db_Text.Append(Encoding.ASCII.GetString(Fix_Stops(ArrayConcat(sectors.ToArray()))));
                }
            }

            void Disp_VMAX(int t, double track, BitArray tdata = null)
            {
                //List<byte> uni = new List<byte>();
                //List<byte> list = new List<byte>();
                //string tbl = string.Empty;
                byte[] dec = new byte[0];
                int tlen = 0;
                string contents = string.Empty;
                byte[][] sectors = new byte[NDS.sectors[t]][];
                bool[] cksm = new bool[NDS.sectors[t]];
                for (int i = 0; i < sectors.Length; i++)
                {
                    (sectors[i], cksm[i]) = Find_VMax_Sector(NDG.Track_Data[t], tdata, i, NDS.cbm[t], true);
                    //byte[] tmmmp;
                    //(tmmmp, cksm[i]) = Find_VMax_Sector(NDG.Track_Data[t], tdata, i, NDS.cbm[t], false);
                    //foreach (byte b in tmmmp) if (!uni.Any(x => x == vmax_dec_table[b]) || uni.Count == 0) uni.Add(vmax_dec_table[b]);
                    //foreach (byte b in tmmmp)
                    //{
                    //    if (!uni.Any(x => x == b) || uni.Count == 0)
                    //    {
                    //        uni.Add(b);
                    //    }
                    //}
                    //uni.Sort();
                    //sectors[i] = Decode_VmaxGCR(tmmmp);
                    tlen += sectors[i].Length;
                }
                if (sectors.Length > 0)
                {
                    string decoder = "V-Max!";
                    jump_to[(int)trk] = db_Text.Length;
                    if (tr) db_Text.Append($"\n\nTrack ({track}) Format: {secF[NDS.cbm[t]]} Length ({tlen}) bytes, Sectors ({sectors.Length})\nDecoder: {decoder}\n\n");
                    if (!se && !VS_dat.Checked) db_Text.Append(Append_Strings(ArrayConcat(sectors), dhex));
                    else
                    {
                        for (int i = 0; i < sectors.Length; i++)
                        {
                            if (sectors?[i] != null && sectors?[i].Length > 0)
                            {
                                string checksumStatus = cksm[i] ? ok : fail;
                                if (se) db_Text.Append($"\n\nSector ({i + 1}) Length {sectors[i].Length}{contents} Checksum {checksumStatus}\n\n");
                                if (VS_dat.Checked) db_Text.Append(Encoding.ASCII.GetString(Fix_Stops(sectors[i])));
                                else db_Text.Append(Append_Strings(sectors[i], dhex));
                            }
                        }
                    }
                }
                //foreach (byte b in uni)
                //{
                //    list.Add(vmax_dec_table[b]);
                //}
                //File.WriteAllBytes($@"c:\test\track{t}.bin", uni.ToArray());
                //File.WriteAllBytes($@"c:\test\track{t}_1.bin", list.ToArray());
            }

            void Disp_V_ldr(int t, double track, BitArray tdata = null)
            {
                byte[] dec = new byte[0];
                int tlen = 0;
                string contents = string.Empty;
                int version = NDS.cbm.Any(x => x == 2) ? 2 : NDS.cbm.Any(x => x == 3) ? 3 : 1;
                byte[] sectors = new byte[0];
                switch (version)
                {
                    case 1: sectors = Decode_VM_Loader_CBM(Get_VmaxLoaderSegment(NDS.Track_Data[t])); break;
                    case 2: sectors = Decode_VM_Loader(Get_VmaxLoaderSegment(NDS.Track_Data[t])); break;
                    case 3: sectors = Decode_VM_Loader(Get_VmaxLoaderSegment(NDS.Track_Data[t])); break;
                }
                tlen += sectors.Length;
                if (sectors.Length > 0)
                {
                    string decoder = "V-Max! Loader";
                    jump_to[(int)trk] = db_Text.Length;
                    if (tr) db_Text.Append($"\n\nTrack ({track}) Format: {secF[NDS.cbm[t]]}, Length ({tlen}) bytes,\nDecoder: {decoder}\n\n");
                    if (!se && !VS_dat.Checked) db_Text.Append(Append_Strings(sectors, dhex));
                    else
                    {
                        if (sectors != null && sectors.Length > 0)
                        {
                            if (VS_dat.Checked) db_Text.Append(Encoding.ASCII.GetString(Fix_Stops(sectors)));
                            else db_Text.Append(Append_Strings(sectors, dhex));
                        }
                    }
                }
            }

            void Disp_RLK(int t, double track, BitArray s)
            {
                int tlen = 0;
                string contents = string.Empty;
                List<byte[]> sectors = new List<byte[]>();
                List<bool> cksm = new List<bool>();
                List<bool> version = new List<bool>();
                List<int> secnum = new List<int>();
                ushort window = 0, pos = 0;
                ushort[] hdr = new ushort[] { 0xff6b, 0xff55, 0xff75 };
                byte[] sec = new byte[0];
                while (pos < s.Length)
                {
                    window <<= 1;
                    if (s[pos++]) window |= 1;
                    if (window == hdr[2] && ((Bit2Byte(s, pos, 8)[0] & 0xF0) == 0x90)) sec = Decode_RL_Header(Bit2Byte(s, pos, 48)).header;
                    if (window == hdr[0] || window == hdr[1] && Bit2Byte(s, pos, 8)[0] != 0x7b)
                    {
                        if (window == hdr[0])
                        {
                            var (tmp, csm, vs) = Decode_Rapidlok_GCR(Bit2Byte(s, pos - 8, 583 << 3), true);
                            sectors.Add(tmp);
                            version.Add(vs);
                            cksm.Add(csm);
                        }
                        else
                        {
                            sectors.Add(FastArray.Init(376, 0x00));
                            version.Add(false);
                            cksm.Add(false);
                        }
                        secnum.Add((sec != null && sec.Length >= 1 && sec[0] <= 0x0b) ? Convert.ToInt32(sec[0]) : -1);
                        tlen += sectors[sectors.Count - 1].Length;
                    }
                }
                if (sectors.Count > 0)
                {
                    string dec = version.Any(x => x == true) ? " v2+" : " v1";
                    jump_to[(int)trk] = db_Text.Length;
                    if (tr) db_Text.Append($"\n\nTrack ({track}) Format: {secF[NDS.cbm[t]]} Length ({tlen}) bytes, Sectors ({sectors.Count})\nDecoder: RapidLok{dec}\n\n");
                    if (!se && !VS_dat.Checked) db_Text.Append(Append_Strings(ArrayConcat(sectors.ToArray()), dhex));
                    else
                    {
                        for (int i = 0; i < sectors.Count; i++)
                        {
                            try
                            {
                                if (sectors[i] != null && sectors?[i].Length >= 3)
                                {
                                    contents = sectors[i].All(x => x == 0x00) ? mt : string.Empty;
                                    string decoder = contents == mt ? string.Empty : version[i] ? " Decoder: RapidLok v2+" : " Decoder: RapidLok v1";
                                    string checksumStatus = contents != string.Empty ? na : cksm[i] ? ok : fail;
                                    if (se) db_Text.Append($"\n\nSector ({secnum[i]}) Length {sectors[i].Length}{contents} Checksum {checksumStatus}\n\n");
                                    if (VS_dat.Checked) db_Text.Append(Encoding.ASCII.GetString(Fix_Stops(sectors[i])));
                                    else db_Text.Append(Append_Strings(sectors[i], dhex));
                                }
                            }
                            catch { }
                        }
                    }
                }
            }

            void Disp_CBM(int t, double track, BitArray tdata, bool mps)
            {
                byte[][] temp = new byte[NDS.sectors[t]][];
                bool[] valid_checksum = new bool[NDS.sectors[t]];
                string ev = " Decoder: Early Vorpal (4:3)", std = " Decoder: Standard CBM (5:4)";
                if (DV_dec.Checked)
                {
                    jump_to[(int)trk] = db_Text.Length;
                    int total = 0;
                    for (int i = 0; i < NDS.sectors[t]; i++)
                    {
                        try
                        {
                            if (mps) (temp[i], valid_checksum[i], _) = Decode_MicroProse_Sector(tdata, i);
                            {
                                var tempdat = Decode_CBM_Sector(NDG.Track_Data[t], i, false, tdata).data;
                                (var cbmdat, var illcbm) = Decode_CBM_GCR(tempdat);
                                (var vpldat, var csm, var illvpl) = Decode_eVPL(CopyArray(tempdat, 3));
                                if (illcbm > 6 && illvpl < 10) { temp[i] = CopyArray(vpldat); valid_checksum[i] = csm; }
                                else { temp[i] = CopyArray(cbmdat, 1, 256); valid_checksum[i] = CBM_Checksum(cbmdat); }
                            }
                            if (temp[i] != null) total += temp[i].Length;
                        }
                        catch { }
                    }
                    if (tr) db_Text.Append($"\n\nTrack ({track})  Data Format: {secF[NDS.cbm[t]]} Length ({total}) bytes\n\n");
                    for (int i = 0; i < NDS.sectors[t]; i++)
                    {
                        try
                        {
                            if (temp[i]?.Length != null)
                            {
                                if (se) db_Text.Append($"\n\nSector ({i + 1}) Length {temp[i].Length} Checksum {(valid_checksum[i] ? ok : fail)}{(temp[i].Length > 240 ? std : ev)}\n\n");
                                if (VS_dat.Checked) db_Text.Append(Encoding.ASCII.GetString(Fix_Stops(temp[i])));
                                else db_Text.Append(Append_Strings(temp[i], dhex));
                            }
                        }
                        catch { }
                    }
                }
            }

            void Disp_VPL(int t, double track, BitArray tdata)
            {
                byte[][] temp = new byte[NDS.sectors[t]][];
                bool[] cksm = new bool[NDS.sectors[t]];
                byte[] ID = new byte[NDS.sectors[t]];
                int pos = 0;
                jump_to[(int)trk] = db_Text.Length;
                if (DV_dec.Checked)
                {
                    int interleave = 1; // Set to 3 to display sectors in read-order interleave
                    int current = 0, s = 0, total = 0;
                    for (int ii = 0; ii < NDS.sectors[t]; ii++)
                    {
                        (temp[ii], cksm[ii], _, pos) = Decode_Vorpal(tdata, ii);
                        total += temp[ii].Length;
                    }
                    if (tr) db_Text.Append($"\n\nTrack ({track}) {secF[NDS.cbm[t]]} Sectors ({NDS.sectors[t]}) Length ({total}) bytes\nDecoder: Vorpal Newer\n\n");
                    for (int ii = 0; ii < NDS.sectors[t]; ii++)
                    {
                        string ck = cksm[current] ? ok : fail;
                        if (se) db_Text.Append($"\n\nSector ({current}) Length ({temp[current].Length}) bytes. Checksum {ck}\n\n");
                        if (VS_dat.Checked) db_Text.Append(Encoding.ASCII.GetString(Fix_Stops(temp[current])));
                        else db_Text.Append(Append_Strings(temp[current], dhex));
                        current += interleave;
                        if (current > NDS.sectors[t] - 1)
                        {
                            s++;
                            current = 0 + s;
                        }
                    }
                }
            }
        }

        StringBuilder Append_Strings(byte[] secdat, bool dhex)
        {
            StringBuilder temp2 = new StringBuilder();
            if (secdat != null)
            {
                int div = dhex ? 16 : 8; // dhex = display as hex (16 bytes wide).  If false, Display as binary (8 bytes wide) 
                int remainder = secdat.Length % div;
                for (int j = 0; j < secdat.Length / div; j++) temp2.Append(Build_String(j * div, div));
                if (remainder > 0) temp2.Append(Build_String(secdat.Length - remainder, remainder, div));
            }
            return temp2;

            string Build_String(int pos, int length, int expected_length = 0)
            {
                string spc = new string(' ', Math.Max(0, (expected_length - length) * (dhex ? 3 : 9)));
                byte[] temp = CopyArray(secdat, pos, length);
                if (dhex) return ($"{Hex_Val(temp).Replace('-', ' ')}    {spc}{Encoding.ASCII.GetString(Fix_Stops(temp))}\n");
                else return ($"{Byte_to_Binary(temp)}     {spc}{Encoding.ASCII.GetString(Fix_Stops(temp))}\n");
            }
        }

        byte[] Fix_Stops(byte[] data)
        {
            if (data == null) return null;
            for (int i = 0; i < data.Length; i++) if ((data[i] >= 0 && data[i] <= 31) || data[i] == 95 || data[i] >= 128) data[i] = 0x2e;
            return data;
        }

        private void Fix_Errors()
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < tracks; i++)
            {
                switch (NDS.cbm[i])
                {
                    case 1: if (CBM_Fix.Checked) FixCBM(i); break;
                    case 5: FixVPL(i); break;
                    case 10: FixMPS(i); break;
                }
            }
            sw.Stop();

            void FixVPL(int track)
            {
                var source = new BitArray(Flip_Endian(NDG.Track_Data[track]));
                bool rewrite = false;
                for (int j = 0; j < NDS.sectors[track]; j++)
                {
                    (byte[] sector, bool cksm, bool isone, int pos) = Decode_Vorpal(source, j);
                    if (!cksm)
                    {
                        var newsec = Encode_Vorpal_GCR(sector, true, isone);
                        for (int k = 0; k < newsec.Length; k++)
                        {
                            source[pos + k] = newsec[k];
                        }
                        rewrite = true;
                    }
                }
                if (rewrite) Set_Dest_Arrays(Bit2Byte(source), track);
            }

            void FixMPS(int track)
            {
                var source = new BitArray(Flip_Endian(NDG.Track_Data[track]));
                bool rewrite = false;
                for (int j = 0; j < NDS.sectors[track]; j++)
                {
                    (byte[] sector, bool checksum, int pos) = Decode_MicroProse_Sector(source, j, false);
                    if (!checksum && (sector != null && pos >= 0))
                    {
                        int start = sector.Length == 335 ? 9 : sector.Length == 325 ? 1 : 0;
                        int end = sector.Length == 335 ? 266 : sector.Length == 325 ? 257 : 0;
                        byte[] dec = Decode_CBM_GCR(sector).decoded;
                        if (dec != null && (dec.Length == 268 || dec.Length == 260) && (start == 9 || start == 1) && (end == 257 || end == 266))
                        {
                            var newsec = Fix_Checksum(dec, start, end);
                            for (int k = 0; k < newsec.Length; k++) source[pos + k] = newsec[k];
                        }
                        rewrite = true;
                    }
                }
                if (rewrite) Set_Dest_Arrays(Bit2Byte(source), track);
            }

            void FixCBM(int track)
            {
                int tk = tracks > 42 ? (track / 2) + 1 : track + 1;
                var source = new BitArray(Flip_Endian(NDG.Track_Data[track]));
                bool rewrite = false;
                int avail = NDS.sectors[track] > Available_Sectors[tk] ? NDS.sectors[track] : Available_Sectors[tk];
                for (int j = 0; j < avail; j++)
                {
                    (bool found, int pos, int blk_pos, _, bool head_chksum) = Find_Sector(source, j, 0, true);
                    if (found && (pos >= 0 && pos + (10 << 3) < source.Length) && !head_chksum)
                    {
                        var header = Decode_CBM_GCR(Bit2Byte(source, pos, 10 << 3)).decoded;
                        if (header[2] == j && header[3] == tk)
                        {
                            int csm = 0;
                            for (int k = 2; k < 6; k++) csm ^= header[k];
                            header[1] = (byte)csm;
                            var newheader = new BitArray(Flip_Endian(Encode_CBM_GCR(header)));
                            for (int k = 0; k < newheader.Length; k++) source[pos + k] = newheader[k];
                            rewrite = true;

                        }
                    }

                    if (found && blk_pos >= 0)
                    {
                        bool checksum = Decode_CBM_Sector(null, j, true, source, pos).checksum;
                        if (!checksum)
                        {
                            try
                            {
                                var secdata = Decode_CBM_GCR(Bit2Byte(source, blk_pos, 325 << 3)).decoded;
                                if (secdata != null && secdata.Length == 260 && secdata[0] == 0x07)
                                {
                                    var newsec = Fix_Checksum(secdata, 1, 257);
                                    for (int k = 0; k < newsec.Length; k++) source[blk_pos + k] = newsec[k];
                                    rewrite = true;
                                }
                            }
                            catch { }
                        }
                    }
                }
                if (rewrite)
                {
                    Set_Dest_Arrays(Bit2Byte(source), track);
                    if (NDA.Track_Data[track] != null) Buffer.BlockCopy(NDA.Track_Data[track], 0, NDS.Track_Data[track], 0, 8192);
                }

            }

            BitArray Fix_Checksum(byte[] sec, int start, int end)
            {
                int chksm = 0;
                for (int k = start; k < end; k++) chksm ^= sec[k];
                sec[end] = (byte)chksm;
                return new BitArray(Flip_Endian(Encode_CBM_GCR(sec)));
            }
        }
    }
}