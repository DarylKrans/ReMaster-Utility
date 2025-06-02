using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ReMaster_Utility.Properties;

namespace V_Max_Tool
{
    /*
    --------------------
     SageFS file header (16 bytes)
    --------------------

     0-5    first 6 bytes "SageDB"
     6-7    Ushort (2 bytes) # of disk-image entries
     8-15   Long (8 bytes) directory offset

    ---------------------
     SageFS Entry layout (98 bytes)
    ---------------------

     0      byte (Region 2 bits, Disk Side 5 bits, Lock/Protected status 1 bit)
            first two bits - 00 (None) 01 (NTSC) 10 (PAL) 11 (Any) :(reserved for Region coding [PAL/NTSC])
            next 5 bits (Disk Side) 0 - 31
            last bit (Lock status) 1: File is locked, 0: File unlocked (can be removed from the database)
     1      byte (reserved for protection type used on the image)
     2-9    Long Offset to first byte of the image
     10-13  Uint32 Size of the compressed image
     14-17  Uint32 Size of decompressed image
     18-21  Uint32 TimeStamp of when the entry was added to the database
     22-37  MD5 Hash of decompressed image (used to check repeats and to ensure match when decompressed)
     38-53  MD5 Hash of compressed image (used to verify integrity of compressed data before decompression)
     54-117 string (Name of disk image from Track 18, sector 0) -- if it exists, otherwise blank
     */

    public partial class Form1 : Form
    {
        private int dbLastHoveredItem = -1;
        bool sortAscending = true;
        bool ShowProgress = false;
        private DateTime lastHoverUpdate = DateTime.MinValue;
        private readonly TimeSpan hoverDelay = TimeSpan.FromMilliseconds(10); // tweak as needed
        private DateTime ctrlHold;
        private bool ctrlHeld = false;
        private int bdbW = 0;
        private int regionSort = 0;
        private int lastSortedColumn = -1;
        private string lastColumnName = string.Empty;
        DiskInfo[] disk;
        ImageList icons = new ImageList();
        Thread sort;
        ContextMenuStrip dbMenu = new ContextMenuStrip();
        ToolStripMenuItem lockItem = new ToolStripMenuItem("Lock Image");
        ToolStripMenuItem unlockItem = new ToolStripMenuItem("Unlock Image");
        ToolStripMenuItem removeItem = new ToolStripMenuItem("Remove");
        ToolStripMenuItem editItem = new ToolStripMenuItem("Edit");
        ToolStripMenuItem dupeItem = new ToolStripMenuItem("Check for Duplicates");
        ToolStripSeparator[] dbSep = new ToolStripSeparator[5];

        private readonly Dictionary<int, string> Prot = new Dictionary<int, string> {
            { 0, "None" }, { 1, "V-Max" }, { 2, "Vorpal" }, {3, "RapidLok" } , { 4, "Cyan" }, { 5, "GMA/Secruispeed" } , { 6, "RainbowArts" },
            { 7, "Radwar" }, { 8, "PirateSlayer" }, { 9, "Fat Tracks" }, { 10, "MicroProse" }, { 11, "Custom" }
        };

        private readonly Dictionary<string, int> setRegion = new Dictionary<string, int>()
        {
            { "", 0 }, { "ntsc", 1 }, { "pal", 2 }
        };

        private readonly Dictionary<int, string> getRegion = new Dictionary<int, string>()
        {
            { 0, "" }, { 1, "NTSC" }, { 2, "PAL" }
        };


        TextBox dbSearch = new TextBox
        {
            Width = 350,
            Height = 18,
            Top = 4,
            Left = 100,
            BackColor = Color.LightGray,
            ForeColor = Color.Black,
        };
        ProgressBar dbProg = new ProgressBar
        {
            Width = 340,
            Height = 10,
            Top = 9,
            Left = 105,
            BackColor = Color.White,
            ForeColor = Color.Black,
        };
        Label DBsearch = new Label
        {
            Top = 7,
            Left = 105,
            Height = 15,
            Width = 55,
            Cursor = Cursors.IBeam,
            Text = "( Search )",
            ForeColor = Color.Gray,
            BackColor = Color.LightGray,
        };

        void Setup_Database_Window()
        {
            icons.ImageSize = new Size(20, 20);

            icons.Images.Add("lock", Resources._lock);
            dbView.OwnerDraw = true;
            dbView.View = View.Details; // Enables column mode
            dbView.FullRowSelect = true;
            dbView.GridLines = false; // Adds line separators
            dbView.MultiSelect = true;
            dbView.Scrollable = true;
            dbView.HideSelection = false;
            dbView.CheckBoxes = false;
            dbView.SmallImageList = icons;
            dbView.Columns.Add(" ", 24, HorizontalAlignment.Center); // Lock Column
            dbView.Columns.Add("Title", 450, HorizontalAlignment.Left);
            dbView.Columns.Add("Side", 35, HorizontalAlignment.Center);
            dbView.Columns.Add("Year", 45, HorizontalAlignment.Center);
            dbView.Columns.Add("Region", 80, HorizontalAlignment.Center);
            dbView.Columns.Add("Protection", 100, HorizontalAlignment.Center);
            //dbView.Columns.Add("Compressed", 80, HorizontalAlignment.Center);
            //dbView.Columns.Add("Decompressed", 80, HorizontalAlignment.Center);
            dbView.Columns.Add("Date Added", 130, HorizontalAlignment.Center);

            int totalWidth = dbView.Columns.Cast<ColumnHeader>().Sum(c => c.Width);
            BrowseDB.Controls.Add(dbView);
            dbView.Scrollable = true;
            dbView.Location = new Point(0, 0);
            dbView.Width = totalWidth + 22;
            dbView.Height = BrowseDB.Height - 38 - dbView.Top; //BrowseDB.Height;
            BrowseDB.Width = dbView.Width + 17;
            bdbW = BrowseDB.Width;
            BrowseDB.Controls.Add(PVbox);
            BrowseDB.Controls.Add(dbSearch);
            BrowseDB.Controls.Add(DBsearch);
            BrowseDB.Controls.Add(dbProg);
            dbProg.Value = 0;
            PVbox.Width = 310;
            PVbox.BackColor = C64_screen;
            PVbox.ForeColor = c64_text;
            PVbox.Location = new Point(BrowseDB.Width - 17, 0);
            PVbox.Height = BrowseDB.Height - 38;
            PVbox.Font = new Font(DirFont.Families[0], 7.875f, FontStyle.Regular);
            PVbox.BringToFront();
            PVbox.Visible = false;
            dbSearch.BringToFront();
            DBsearch.BringToFront();
            dbProg.SendToBack();
            Set_Behaviors();

            void Set_Behaviors()
            {
                for (int i = 0; i < dbSep.Length; i++)
                {
                    dbSep[i] = new ToolStripSeparator();
                }
                BrowseDB.KeyDown += BrowseDB_KeyDown;
                BrowseDB.KeyUp += BrowseDB_KeyUp;

                dbView.ContextMenuStrip = dbMenu;
                dbView.DrawColumnHeader += DbView_DrawColumnHeader;
                dbView.DrawItem += DbView_DrawItem;
                dbView.DrawSubItem += DbView_DrawSubItem;
                dbView.MouseDoubleClick += DbView_MouseDoubleClick;
                dbView.MouseMove += DbView_MouseMove;
                dbView.ColumnClick += DbView_ColumnClick;

                DBsearch.Click += (s, e) =>
                {
                    dbSearch.Focus();
                };

                dbSearch.TextChanged += (s, e) =>
                {
                    DBsearch.Visible = dbSearch.Text.Length == 0;
                    var searchText = dbSearch.Text.ToLower();
                    var filteredList = disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderBy(d => d.Title).ToList();
                    UpdateList(RegionFilter(filteredList), true);
                };

                //dbView.KeyDown += (s, e) =>
                dbView.KeyUp += (s, e) =>
                {
                    var keys = new Keys[] { Keys.Up, Keys.Down, Keys.PageUp, Keys.PageDown };
                    if (e.KeyData == Keys.Enter && dbView.SelectedItems.Count == 1)
                    {
                        var idx = ((Tag)dbView.SelectedItems[0].Tag).Index;// (int)dbView.SelectedItems[0].Tag;
                        if (idx >= 0) Import_Image_From_Database(idx);
                    }

                    if (keys.Contains(e.KeyData))
                    {
                        if (dbView.SelectedItems.Count > 0)
                        {
                            var selectedItem = dbView.SelectedItems[0];
                            if (selectedItem?.Tag != null) UpdatePreview(((Tag)selectedItem.Tag).Index);
                        }
                    }
                };

                dbView.MouseUp += (s, e) =>
                {
                    dbMenu.Items.Clear();
                    if (e.Button == MouseButtons.Right)
                    {
                        //if (dbView.SelectedItems.Count == 1)
                        {
                            var separator = 0;
                            ListViewItem item = dbView.GetItemAt(e.X, e.Y);
                            if (item != null)
                            {
                                // Optionally select the item if not already selected
                                if (!item.Selected)
                                {
                                    dbView.SelectedItems.Clear();
                                    item.Selected = true;
                                }
                                bool Locked = disk[((Tag)item.Tag).Index].Locked;
                                dbMenu.Items.Add(Locked ? unlockItem : lockItem);
                                if (!Locked)
                                {
                                    dbMenu.Items.Add(dbSep[separator++]);
                                    dbMenu.Items.Add(removeItem);
                                    dbMenu.Items.Add(dbSep[separator++]);
                                    dbMenu.Items.Add(editItem);
                                    if (disk.Length > 1) dbMenu.Items.Add(dupeItem);
                                }
                            }
                            // Show the context menu at cursor position
                        }
                        dbMenu.Show(dbView, e.Location);
                    }
                };

                unlockItem.Click += (s, e) => LockFile(s);
                lockItem.Click += (s, e) => LockFile(s);
                dupeItem.Click += (s, e) =>
                {
                    int[] dupes = CheckForDuplicates(true, true);
                    string message = dupes.Length > 0
                        ? $"({dupes.Length}) Duplicates found!\nWould you like to remove selected them?"
                        : "No duplicates found!";
                    string title = dupes.Length > 0 ? "Duplicates found!" : "Congratulations!";
                    MessageBoxButtons buttons = dupes.Length > 0 ? MessageBoxButtons.YesNo : MessageBoxButtons.OK;
                    using (Message_Center center = new Message_Center(this))
                    {
                        bool ignoreLock = false;
                        DialogResult result = MessageBox.Show(message, title, buttons, MessageBoxIcon.Information);
                        if (result == DialogResult.Yes)
                        {
                            int locked = 0;
                            foreach (int i in dupes) if (disk[i].Locked) locked++;
                            if (locked > 0)
                            {
                                title = locked == 1 ? "Image is locked!" : $"({locked}) Images are locked!";
                                using (Message_Center ignore = new Message_Center(this))
                                {
                                    DialogResult res = MessageBox.Show("Ignore locked status and remove anyway?",
                                        title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                    if (res == DialogResult.Yes) ignoreLock = true;
                                }
                            }
                            if (locked != dupes.Length || ignoreLock)
                            {
                                Task.Run(delegate
                                {
                                    Invoke(new Action(() =>
                                    {
                                        dbView.Enabled = dbSearch.Enabled = false;
                                        ShowProgress = disk.Length > 250;
                                        if (ShowProgress) dbProg.BringToFront();
                                        dbProg.Value = 0;
                                        dbProg.Maximum = 100 * 100;
                                        dbProg.Value = dbProg.Maximum / 100;
                                    }));
                                    if (!Remove_Items_From_Database(dupes, ignoreLock))
                                    {

                                        Invoke(new Action(() =>
                                        {
                                            using (Message_Center ignore = new Message_Center(this))
                                            {
                                                MessageBox.Show("No changes were made.", "An error occured!"
                                                    , MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                            }
                                        }));
                                    }
                                    else
                                    {
                                        ReadDB();
                                        var sortedList = disk.OrderBy(d => d.Title).ToList();
                                        Invoke(new Action(() => UpdateList(RegionFilter(sortedList), true)));
                                    }
                                    Invoke(new Action(() =>
                                    {
                                        dbProg.SendToBack();
                                        dbView.Enabled = dbSearch.Enabled = true;
                                        ShowProgress = false;
                                    }));
                                });
                            }
                        }
                    }
                };

                void LockFile(object s)
                {
                    if (dbView.SelectedItems.Count > 0)
                    {
                        bool lockfile = s.Equals(lockItem);
                        List<int> selected = new List<int>();
                        dbView.BeginUpdate();
                        foreach (ListViewItem currentItem in dbView.SelectedItems)
                        {
                            var index = ((Tag)currentItem.Tag).Index;
                            disk[index].Locked = lockfile; // true;
                            selected.Add(index);
                            currentItem.SubItems[0].Text = lockfile ? " " : "";
                        }
                        dbView.EndUpdate();
                        UpdateDBDirectory(selected.ToArray());
                    }
                }
            }
        }

        void OpenDB_Windows()
        {
            BrowseDB.Location = new Point(
                    this.Location.X + ((this.Width - BrowseDB.Width) / 2),
                    this.Location.Y + (this.Height - BrowseDB.Height) / 2);
            Task.Run(delegate
            {
                ReadDB();
                if (disk?.Length > 0)
                {
                    if (dbView.Items.Count == 0 || dbView.Items.Count != disk.Length)
                    {
                        var sortedList = disk.OrderBy(d => d.Title).ToList();
                        Invoke(new Action(() => UpdateList(RegionFilter(sortedList), true)));
                    }
                    Invoke(new Action(() => BrowseDB.ShowDialog(this)));
                }
                else
                {
                    Invoke(new Action(() =>
                    {
                        using (Message_Center center = new Message_Center(this))
                        {
                            MessageBox.Show("No images in the database", "Database is empty!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }));
                }
            });
            //BrowseDB.ShowDialog(this);
        }

        void UpdateDBDirectory(int[] index)
        {
            if (index.Length > 0)
            {
                using (var dataBase = new AccessDatabase().ReadWrite(TEMP.dbPath))
                {
                    if (dataBase.Valid)
                    {
                        foreach (int i in index)
                        {
                            try
                            {
                                var entry = disk[i].ToEntry();
                                if (entry.Length == DiskInfo.ENTRY_SIZE)
                                {
                                    long seekidx = dataBase.Offset + (i * DiskInfo.ENTRY_SIZE);
                                    dataBase.Seek(seekidx);
                                    dataBase.Write(entry);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        void Import_Image_From_Database(int idx)
        {
            BrowseDB?.Close();
            byte[] data = GetNIBData(idx);
            if (data != null)
            {
                fname = disk[idx].Title.Replace(" ", "_").ToLower();
                fext = ".nib";
                var l = "";
                Data_Box.Clear();
                tracks = (int)(data.Length - 256) >> 13; // 8192;
                if ((tracks << 13) + 256 == data.Length)
                {
                    l = "File Size OK!";
                    Track_Info.Items.Clear();
                    Set_ListBox_Items(true, false);
                    Buffer.BlockCopy(data, 0, nib_header, 0, 256);
                    Set_Arrays(tracks);
                    for (int i = 0; i < tracks; i++)
                    {
                        NDS.Track_Data[i] = new byte[MAX_TRACK_SIZE];
                        Buffer.BlockCopy(data, 256 + (i * MAX_TRACK_SIZE), NDS.Track_Data[i], 0, MAX_TRACK_SIZE);
                        Original.OT[i] = new byte[0];
                    }
                    var head = Encoding.ASCII.GetString(nib_header, 0, 13);
                    var hm = "Bad Header";
                    if (head == "MNIB-1541-RAW")
                    {
                        hm = "Header Match!";
                        var lab = $"Total Tracks ({tracks}), {l}, {hm}";
                        Process(true, lab);
                    }
                    else
                    {
                        label1.Text = $"{hm}";
                        label2.Text = "";
                    }
                    if (hm == "Bad Header")
                    {
                        using (Message_Center center = new Message_Center(this)) // center message box
                        {
                            string t = "Bad Header!";
                            string s = "Image is corrupt and cannot be opened";
                            MessageBox.Show(s, t, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            error = true;
                        }
                    }
                }
                void Process(bool get, string l2)
                {
                    Batch_List_Box.Visible = false;
                    Dir_screen.Clear();
                    Dir_screen.Text = "LOAD\"$\",8\nSEARCHING FOR $\nLOADING";
                    loader_fixed = false;
                    Worker_Main?.Abort();
                    Worker_Main = new Thread(new ThreadStart(() => Do_work("", false)));
                    Worker_Main.Start();
                }
            }
        }

        void ReadDB()
        {
            using (var dataBase = new AccessDatabase().ReadOnly(TEMP.dbPath))
            {
                if (dataBase.Valid)
                {
                    try
                    {
                        disk = new DiskInfo[dataBase.Entries];
                        for (int i = 0; i < dataBase.Entries; i++)
                        {
                            disk[i] = dataBase.GetDirectoryEntry(i);
                            if (disk[i].PreviewLength > 0)
                            {
                                var preview = dataBase.GetPreviewData(disk[i]);
                                if (disk[i].crc16 == Checksum.CRC16(preview)) disk[i].DirPreview = Encoding.ASCII.GetString(Decompress(preview));
                            }
                            disk[i].Index = i;
                        }
                    }
                    catch (Exception ex)
                    {
                        error = true;
                        string title = "Error accessing database.";
                        string message = ex.Message;
                        MessageForYouSir(title, message);
                    }
                }
            }
        }

        List<DiskInfo> RegionFilter(List<DiskInfo> sortedList)
        {
            if (regionSort > 0)
            {
                var filtered = sortedList.Where(d => d.Region == regionSort);
                return filtered.ToList();
            }
            return sortedList;
        }

        void UpdateList(List<DiskInfo> sorted, bool removeSortFlag = false)
        {
            dbView.BeginUpdate();
            dbView.Items.Clear();
            if (removeSortFlag)
            {
                for (int i = 1; i < dbView.Columns.Count; i++)
                {
                    var col = dbView.Columns[i];
                    col.Text = col.Text.Replace(" ↑", "").Replace(" ↓", "");
                }
            }

            foreach (var disk in sorted)
            {
                ListViewItem item = new ListViewItem(disk.Locked ? " " : "");   // Locked
                item.SubItems.Add(disk.Title);                                  // Title
                item.SubItems.Add($"{disk.Side + 1}");                          // Side
                item.SubItems.Add(disk.Year > 1970 ? $"{disk.Year}" : "");    // Year
                item.SubItems.Add($"{getRegion[disk.Region]}");             // Region
                //item.SubItems.Add($"{secF[disk.Protection]}");                  // Protection
                item.SubItems.Add($"{Prot[disk.Protection]}");                  // Protection
                //item.SubItems.Add($"{disk.cSize:N0}");                          // Compressed size
                //item.SubItems.Add($"{disk.dSize:N0}");                          // Decompressed size
                item.SubItems.Add($"{disk.Timestamp}");                         // Timestamp
                dbView.Items.Add(item);
                item.Tag = new Tag { Index = disk.Index, Notes = disk.Notes };
            }
            dbView.EndUpdate();
            BrowseDB.Text = $"Browse ReMaster Image Database ({dbView.Items.Count}/{disk?.Length})";
        }

        string Get_Name(string input)
        {
            string[] exceptions = new[] { "and", "or", "the", "a", "an", "of", "in", "on", "at", "to", "for", "by", "with" };
            string[] words = input.ToLower().Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                // Capitalize the first word no matter what
                if (i == 0 || !exceptions.Contains(words[i]))
                {
                    if (words[i].Length > 0)
                    {
                        words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                    }
                }
            }
            return string.Join(" ", words);
        }

        byte[] GetNIBData(int entry)
        {
            using (var dataBase = new AccessDatabase().ReadOnly(TEMP.dbPath))
            {
                if (dataBase.Valid)
                {
                    dataBase.Seek(disk[entry].Offset);
                    var cmp = dataBase.Read(disk[entry].CompressedLength);
                    if (Checksum.CRC32(cmp) == disk[entry].crc32)
                    {
                        byte[] dec = Decompress(cmp);
                        if (Match(Checksum.MD5(dec), disk[entry].rawHash)) return dec;
                    }
                }
            }
            return null;
        }

        bool Remove_Items_From_Database(int[] ItemList, bool IgnoreLockStatus = false)
        {
            if (ItemList == null || ItemList.Length == 0) return true;
            string tempPath = $@"c:\test\temp.db";
            var skip = new HashSet<int>(ItemList);
            using (var current = new AccessDatabase().ReadOnly(TEMP.dbPath))
            using (var temp = new AccessDatabase().Create(tempPath))
            {
                using (MemoryStream buffer = new MemoryStream())
                using (BinaryWriter write = new BinaryWriter(buffer))
                {
                    for (int i = 0; i < current.Entries; i++)
                    {
                        if (ShowProgress && i - 1 > 0)
                        {
                            Invoke(new Action(() =>
                            {
                                dbProg.Maximum = (int)((double)dbProg.Value / (double)(i + 1) * (current.Entries << 1));
                            }));
                        }
                        var ent = current.GetDirectoryEntry(i);
                        bool remove = IgnoreLockStatus || !ent.Locked;
                        if (!skip.Contains(i) || (skip.Contains(i) && !remove))
                        {
                            var data = current.GetImageData(ent);
                            var preview = current.GetPreviewData(ent);
                            ent.Offset = temp.Offset;
                            write.Write(ent.ToEntry());
                            temp.Seek(temp.Offset);
                            temp.Write(ArrayConcat(data, preview));
                            temp.Offset += data.Length + preview.Length;
                            temp.Entries++;
                        }
                    }
                    temp.UpdateHeader();
                    temp.WriteDirectory(buffer.ToArray());
                }
            }
            if (VerifyDBintegrity(tempPath) > 0)
            {
                File.Delete(tempPath);
                return false;
            }
            else
            {
                File.Replace(tempPath, TEMP.dbPath, destinationBackupFileName: null);
                return true;
            }
        }

        int VerifyDBintegrity(string path)
        {

            int failed = 0;
            using (var verify = new AccessDatabase().ReadOnly(path))
            {
                if (verify.Valid)
                {
                    int sub = (verify.Entries / 200) << 1;
                    for (int i = 0; i < verify.Entries; i++)
                    {
                        if (sub > 0 && ShowProgress && i % sub == 0) Invoke(new Action(() => dbProg.Maximum -= 1));
                        try
                        {
                            var ent = verify.GetDirectoryEntry(i);
                            var data = verify.GetImageData(ent);
                            var preview = verify.GetPreviewData(ent);
                            if (Checksum.CRC32(data) != ent.crc32 || Checksum.CRC16(preview) != ent.crc16) failed++;
                        }
                        catch { failed++; }
                    }
                    return failed;
                }
                else return 65536;
            }
        }

        void BuildDB()
        {
            var ttl = Text;
            using (var dataBase = !File.Exists(TEMP.dbPath)
                ? new AccessDatabase().Create(TEMP.dbPath)
                : new AccessDatabase().ReadWrite(TEMP.dbPath))
            {
                if (dataBase.Valid)
                {
                    string rpath = @"c:\test\test2";
                    string[] file = Directory.EnumerateFiles(rpath, "*.nib", SearchOption.AllDirectories)
                        .Concat(Directory.EnumerateFiles(rpath, "*.nbz", SearchOption.AllDirectories))
                        .ToArray();

                    using (MemoryStream buffer = new MemoryStream())
                    using (BinaryWriter write = new BinaryWriter(buffer))
                    {
                        if (dataBase.Entries > 0)
                        {
                            dataBase.Seek(dataBase.Offset);
                            var old = dataBase.Read(dataBase.Entries * DiskInfo.ENTRY_SIZE);
                            write.Write(old);
                        }
                        foreach (var f in file)
                        {
                            try
                            {
                                var dec = Path.GetExtension(f).ToLower() == ".nib" ? File.ReadAllBytes(f) : LZdecompress(File.ReadAllBytes(f));
                                int t = (dec.Length - 256) / 8192 > 42 ? 34 : 17;
                                var tk = new byte[8192];
                                var preview = new byte[0];
                                Buffer.BlockCopy(dec, 256 + (t * 8192), tk, 0, tk.Length);
                                string pv = Get_Disk_Directory(tk);
                                preview = Compress(Encoding.ASCII.GetBytes(pv));
                                var cmp = Compress(dec);
                                dataBase.Seek(dataBase.Offset);
                                dataBase.Write(ArrayConcat(cmp, preview));
                                DiskInfo info = new DiskInfo
                                {
                                    Offset = dataBase.Offset,
                                    rawHash = Checksum.MD5(dec),
                                    crc32 = Checksum.CRC32(cmp),
                                    Locked = dataBase.Offset % 2 == 0,
                                    DecompressedLength = dec.Length,
                                    CompressedLength = cmp.Length,
                                    PreviewLength = (short)preview.Length,
                                    crc16 = Checksum.CRC16(preview),
                                    Timestamp = DateTime.Now,
                                    Year = Get_Year(f),
                                    Title = Get_Name(Path.GetFileNameWithoutExtension(f.Replace("_", " "))),
                                    Protection = Get_Protection(f),
                                    Region = Get_Region(f, pv),
                                    Side = (byte)Get_DiskSide(f),
                                };
                                write.Write(info.ToEntry());
                                dataBase.Offset += info.CompressedLength + info.PreviewLength;
                                Text = $"{ttl} ({dataBase.Entries++}/{file.Length})";

                            }
                            catch { }
                            dataBase.UpdateHeader();
                        }
                        Text = ttl;
                        dataBase.WriteDirectory(buffer.ToArray());
                    }

                    int Get_Region(string f, string Preview)
                    {
                        string lowerF = f.ToLower();
                        int r = lowerF.Contains("(pal") ? 2 :
                                lowerF.Contains("(ntsc") ? 1 : 0;

                        try
                        {
                            if (r == 0 && !string.IsNullOrEmpty(Preview))
                            {
                                string firstLine = Preview.Split('\n')[0].ToLower();
                                r = firstLine.Contains("pal") ? 2 :
                                    firstLine.Contains("ntsc") ? 1 : 0;
                            }
                        }
                        catch { }
                        return r;
                    }

                    int Get_DiskSide(string input)
                    {
                        var match = Regex.Match(input.ToLower(), @"_s(\d{1,2})");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int side))
                        {
                            return side - 1 < 32 ? side - 1 : 0;
                        }
                        return 0;
                    }

                    int Get_Year(string input)
                    {
                        var match = Regex.Match(input.ToLower(), @"_(\d{1,4})]");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int year))
                        {
                            return year - 1970 > 0 ? year : 1970;
                        }
                        return 1970;
                    }

                    byte Get_Protection(string f)
                    {
                        int prt = -1;
                        if (f.Contains("vmax2".ToLower())) prt = 1;
                        if (f.Contains("vmax3".ToLower()) || f.Contains("vmax4".ToLower())) prt = 1;
                        if (f.Contains("(rl".ToLower())) prt = 6;
                        if (f.Contains("[ea_".ToLower())) prt = 8;

                        if (prt < 0)
                        {
                            var p = Path.GetDirectoryName(f).ToLower();
                            if (p.Contains("vmax")) prt = 1;
                            if (p.Contains("vorpal")) prt = 2;
                            if (p.Contains("rapidlok")) prt = 3;
                            if (p.Contains("cyan")) prt = 4;
                            if (p.Contains("securispeed")) prt = 5;
                            if (p.Contains("rainbow")) prt = 6;
                            if (p.Contains("radwar")) prt = 7;
                            if (p.Contains("fat")) prt = 9;
                            if (p.Contains("microprose")) prt = 10;
                        }
                        return (byte)(prt < 0 ? 0 : prt);
                    }
                }
            }
        }

        void SortList(string column, string searchText)
        {
            var sortedList = new List<DiskInfo>();
            if (column == " ") // Index #
            {
                if (searchText.Length > 0)
                {
                    sortedList = sortAscending
                        ? disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderBy(d => d.Locked).ToList()
                        : disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderByDescending(d => d.Locked).ToList();
                }
                else
                {
                    sortedList = sortAscending
                        ? disk.OrderBy(d => d.Locked).ToList()
                        : disk.OrderByDescending(d => d.Locked).ToList();
                }
            }

            if (column.Contains("title")) // Title
            {
                if (searchText.Length > 0)
                {
                    sortedList = sortAscending
                        ? disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderBy(d => d.Title).ToList()
                        : disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderByDescending(d => d.Title).ToList();
                }
                else
                {
                    sortedList = sortAscending
                        ? disk.OrderBy(d => d.Title).ToList()
                        : disk.OrderByDescending(d => d.Title).ToList();
                }
            }

            if (column.Contains("side")) // Disk Side
            {
                if (searchText.Length > 0)
                {
                    sortedList = sortAscending
                        ? disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderBy(d => d.Side).ToList()
                        : disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderByDescending(d => d.Side).ToList();
                }
                else
                {
                    sortedList = sortAscending
                        ? disk.OrderBy(d => d.Side).ToList()
                        : disk.OrderByDescending(d => d.Side).ToList();
                }
            }

            if (column.Contains("year")) // Disk Side
            {
                if (searchText.Length > 0)
                {
                    sortedList = sortAscending
                        ? disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderBy(d => d.Year).ToList()
                        : disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderByDescending(d => d.Year).ToList();
                }
                else
                {
                    sortedList = sortAscending
                        ? disk.OrderBy(d => d.Year).ToList()
                        : disk.OrderByDescending(d => d.Year).ToList();
                }
            }

            if (column.Contains("region")) // Region column
            {
                regionSort++;
                if (regionSort == 3) regionSort = 0; // Wrap around

                // Start with the base filtered list by search text
                var filtered = searchText.Length > 0
                    ? disk.Where(d => d.Title.ToLower().Contains(searchText))
                    : disk;

                // Apply sorting
                sortedList = sortAscending
                    ? filtered.OrderBy(d => d.Title).ToList()
                    : filtered.OrderByDescending(d => d.Title).ToList();
            }

            if (column.Contains("protection")) // Protection
            {
                if (searchText.Length > 0)
                {
                    sortedList = sortAscending
                        ? disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderBy(d => d.Protection).ToList()
                        : disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderByDescending(d => d.Protection).ToList();
                }
                else
                {
                    sortedList = sortAscending
                        ? disk.OrderBy(d => d.Protection).ToList()
                        : disk.OrderByDescending(d => d.Protection).ToList();
                }
            }

            if (column.Contains("compressed")) // Compressed Size
            {
                if (searchText.Length > 0)
                {
                    sortedList = sortAscending
                        ? disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderBy(d => d.CompressedLength).ToList()
                        : disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderByDescending(d => d.CompressedLength).ToList();
                }
                else
                {
                    sortedList = sortAscending
                        ? disk.OrderBy(d => d.CompressedLength).ToList()
                        : disk.OrderByDescending(d => d.CompressedLength).ToList();
                }
            }

            if (column.Contains("date added")) // TimeStamp
            {
                if (searchText.Length > 0)
                {
                    sortedList = sortAscending
                        ? disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderBy(d => d.Timestamp.Date).ThenBy(d => d.Timestamp.TimeOfDay).ToList()
                        : disk.Where(d => d.Title.ToLower().Contains(searchText)).OrderByDescending(d => d.Timestamp.Date).ThenByDescending(d => d.Timestamp.TimeOfDay).ToList();
                }
                else
                {
                    sortedList = sortAscending
                    ? disk.OrderBy(d => d.Timestamp.Date).ThenBy(d => d.Timestamp.TimeOfDay).ToList()
                    : disk.OrderByDescending(d => d.Timestamp.Date).ThenByDescending(d => d.Timestamp.TimeOfDay).ToList();
                }
            }
            Invoke(new Action(() =>
            {
                UpdateList(RegionFilter(sortedList));
                dbView.Columns[4].Text = $"Region {getRegion[regionSort]}";
            }));
        }

        void UpdatePreview(int index)
        {
            PVbox.Clear();
            PVbox.Text = disk[index].DirPreview;
            int firstLineIndex = 0;
            int start = PVbox.GetFirstCharIndexFromLine(firstLineIndex);
            int nextLineStart = PVbox.GetFirstCharIndexFromLine(firstLineIndex + 1);
            int length;
            if (nextLineStart == -1) length = PVbox.Text.Length - start;
            else length = nextLineStart - start - 3;
            PVbox.Select(disk[index].DirPreview.Substring(0, 1) == "0" ? 2 : 0, length);
            PVbox.SelectionBackColor = c64_text;
            PVbox.SelectionColor = C64_screen;
        }

        int[] CheckForDuplicates(bool show = false, bool select = false)
        {
            List<int> dupeIdx = new List<int>();
            if (disk.Length > 1)
            {
                Stopwatch sw = Stopwatch.StartNew();
                Dictionary<string, int> hashMap = new Dictionary<string, int>(); // md5 hash -> index of first occurrence
                HashSet<string> seenHashes = new HashSet<string>(); // For quick lookup
                HashSet<int> indicesToShow = new HashSet<int>(); // Final list of all indexes to display
                foreach (DiskInfo d in disk)
                {
                    string hash = Encoding.ASCII.GetString(d.rawHash);
                    if (!seenHashes.Add(hash))
                    {
                        // This is a duplicate
                        if (hashMap.TryGetValue(hash, out int originalIndex))
                        {
                            indicesToShow.Add(originalIndex); // Add the original
                        }
                        indicesToShow.Add(d.Index); // Add the duplicate
                        dupeIdx.Add(d.Index);   // Add directory index of file to remove
                    }
                    else hashMap[hash] = d.Index; // First time seeing this hash
                }
                List<DiskInfo> list = new List<DiskInfo>();
                foreach (int i in indicesToShow) list.Add(disk[i]);
                if (show && dupeIdx.Count > 0)
                {
                    var sortedList = sortAscending
                        ? list.OrderBy(d => Encoding.ASCII.GetString(d.rawHash)).ThenBy(d => d.Title).ToList()
                        : list.OrderByDescending(d => Encoding.ASCII.GetString(d.rawHash)).ThenByDescending(d => d.Title).ToList();
                    UpdateList(sortedList);
                    if (select) foreach (ListViewItem l in dbView.Items) if (dupeIdx.Contains(((Tag)l.Tag).Index)) l.Selected = true;
                }
                sw.Stop();
            }
            return dupeIdx.ToArray(); // return list of file indexes to remove
        }

        private void DbView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true; // Use system default for column headers
        }

        private void DbView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // If View is Details, we handle drawing in DrawSubItem instead
            if (dbView.View != View.Details)
            {
                Color backColor = (e.ItemIndex % 2 == 0) ? Color.White : Color.LightGray;
                e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);
                e.DrawText();
            }
        }

        private void DbView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            bool isSelected = e.Item.Selected;
            bool isHovered = (dbLastHoveredItem == e.ItemIndex);
            string search = dbSearch.Text.Trim();
            bool hasSearch = !string.IsNullOrEmpty(search);
            string text = e.SubItem.Text;

            // Background color logic
            Color backColor = isSelected ? (e.ItemIndex % 2 == 0 ? Color.FromArgb(150, 0, 30, 200) : Color.FromArgb(180, 0, 30, 200))
                : (e.ItemIndex % 2 == 0 ? Color.FromArgb(230, 230, 230) : Color.FromArgb(200, 200, 200));
            Color HiBack = Color.FromArgb(150, 188, 128, 0);

            // Text color logic
            Color normalColor = isSelected ? SystemColors.HighlightText : Color.Black;
            Color hoverColor = isSelected ? Color.Yellow : Color.Blue;
            Color textColor = isHovered ? hoverColor : normalColor;

            // Fill background
            using (SolidBrush backBrush = new SolidBrush(backColor))
                e.Graphics.FillRectangle(backBrush, e.Bounds);

            // Alignment logic
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            switch (dbView.Columns[e.ColumnIndex].TextAlign)
            {
                case HorizontalAlignment.Center:
                    flags |= TextFormatFlags.HorizontalCenter;
                    break;
                case HorizontalAlignment.Right:
                    flags |= TextFormatFlags.Right;
                    break;
                default:
                    flags |= TextFormatFlags.Left;
                    break;
            }

            int lockColumnIndex = 0;
            int titleColumnIndex = 1;

            if (e.ColumnIndex == lockColumnIndex)
            {
                bool locked = e.SubItem.Text == " ";
                if (locked)
                {
                    int imgX = e.Bounds.X + (e.Bounds.Width - 20) / 2;
                    int imgY = e.Bounds.Y + (e.Bounds.Height - 20) / 2;
                    e.Graphics.DrawImage(icons.Images["lock"], new Rectangle(imgX, imgY, 20, 20));
                }
            }

            Rectangle bounds = e.Bounds;
            if (e.ColumnIndex == titleColumnIndex && !string.IsNullOrEmpty(dbSearch.Text))
            {
                // Offset for checkbox in first column
                if (e.ColumnIndex == 0 && dbView.CheckBoxes) bounds.X += 20;

                if (hasSearch)
                {
                    Font font = dbView.Font;
                    Color highlightColor = isHovered && isSelected ? Color.Yellow : isSelected ? Color.White : Color.Black;// Color.LightGreen;
                    string lowerText = text.ToLower();
                    string lowerSearch = search.ToLower();

                    int start = 0;
                    float x = bounds.X;
                    float y = bounds.Y + (bounds.Height - TextRenderer.MeasureText("A", font).Height) / 2 + 7;

                    while (true)
                    {
                        int matchIndex = lowerText.IndexOf(lowerSearch, start);
                        if (matchIndex < 0) break;
                        string before = text.Substring(start, matchIndex - start);
                        string match = text.Substring(matchIndex, search.Length);

                        // Draw 'before' text
                        Size beforeSize = TextRenderer.MeasureText(e.Graphics, before, font, bounds.Size, flags);
                        if (before.Length > 0)
                        {
                            TextRenderer.DrawText(e.Graphics, before, font, new Point((int)x, (int)y), textColor, flags);
                            x += beforeSize.Width - 7;
                        }

                        // Draw highlight background for 'match'
                        Size matchSize = TextRenderer.MeasureText(e.Graphics, match, font, bounds.Size, flags);
                        Rectangle rect = new Rectangle((int)x + 2, bounds.Y, matchSize.Width - 6, bounds.Size.Height);
                        using (SolidBrush backBrush = new SolidBrush(HiBack))
                            e.Graphics.FillRectangle(backBrush, rect);

                        // Draw match text
                        TextRenderer.DrawText(e.Graphics, match, font, new Point((int)x, (int)y), highlightColor, flags);
                        x += matchSize.Width - 7;

                        start = matchIndex + search.Length;
                    }

                    // Draw any remaining text after the last match
                    if (start < text.Length)
                    {
                        string after = text.Substring(start);
                        TextRenderer.DrawText(e.Graphics, after, font, new Point((int)x, (int)y), textColor, flags);
                    }
                    return;
                }
            }

            // Default draw if no match or no search
            TextRenderer.DrawText(e.Graphics, text, dbView.Font, bounds, textColor, flags);
        }

        private void DbView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = dbView.HitTest(e.Location);
            if (e.Button == MouseButtons.Left)
            {
                if (hit.Item != null)
                {
                    if (hit.Item?.Tag != null) Import_Image_From_Database(((Tag)hit.Item.Tag).Index);
                }
            }
        }

        private void DbView_MouseMove(object sender, MouseEventArgs e)
        {

            ListViewHitTestInfo info = dbView.HitTest(e.Location);
            int index = info.Item?.Index ?? -1;
            try
            {
                if (index != dbLastHoveredItem && DateTime.Now - lastHoverUpdate > hoverDelay)
                {
                    dbLastHoveredItem = index;
                    dbView.Invalidate(); // Redraw to apply new hover effect
                    lastHoverUpdate = DateTime.Now;
                    if (info.Item?.Tag != null) UpdatePreview(((Tag)info.Item.Tag).Index);
                }
            }
            catch { }
        }

        private void BrowseDB_KeyDown(object sender, KeyEventArgs e)
        {
            if (!ctrlHeld && e.Control)
            {
                ctrlHeld = true;
                ctrlHold = DateTime.Now;
            }
        }

        private void BrowseDB_KeyUp(object sender, KeyEventArgs e)
        {
            if (!e.Control && ctrlHeld)
            {
                ctrlHeld = false;
                var holdtime = DateTime.Now - ctrlHold;
                if (holdtime.TotalMilliseconds < 350)
                {
                    PVbox.Visible = !PVbox.Visible;
                    if (PVbox.Visible) BrowseDB.Width += PVbox.Width;
                    else BrowseDB.Width -= PVbox.Width;
                }
            }
        }

        private void DbView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column != 4) sortAscending = lastSortedColumn == e.Column ? !sortAscending : sortAscending;
            lastSortedColumn = e.Column;
            string column = dbView.Columns[e.Column].Text.ToLower().ToString();
            lastColumnName = column;
            var searchText = dbSearch.Text.ToLower();
            int[] skipColumns = new int[] { 0, 2, 4 };
            var appendAscend = " ↑";
            var appendDescend = " ↓";
            for (int i = 1; i < dbView.Columns.Count; i++)
            {
                var col = dbView.Columns[i];
                col.Text = col.Text.Replace(appendAscend, "").Replace(appendDescend, "");
            }
            // Add sort indicator to current column
            if (!skipColumns.Any(x => x == e.Column))
            {
                var sortSymbol = sortAscending ? appendAscend : appendDescend;
                dbView.Columns[e.Column].Text += sortSymbol;
            }
            sort?.Abort();
            sort = new Thread(() => SortList(column, searchText));
            sort.Start();
        }
    }
}
