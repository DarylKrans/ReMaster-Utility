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
        bool editing = false;
        bool favToggle = false;
        bool dbCorrupt = false;
        //int currentEditIndex = -1;
        string lastNotes = string.Empty;
        private bool lastIconHoverState = false;
        private DateTime lastHoverUpdate = DateTime.MinValue;
        private readonly TimeSpan hoverDelay = TimeSpan.FromMilliseconds(10); // tweak as needed
        private DateTime ctrlHold;
        private bool ctrlHeld = false;
        private int bdbW = 0;
        private int regionSort = 0;
        private int lastSortedColumn = -1;
        List<int> dbRemoved = new List<int>();
        private const int MAX_UNDO = 32;
        List<UndoState> undoStack = new List<UndoState>();
        List<UndoState> redoStack = new List<UndoState>();
        //List<RedoState> redoStack = new List<RedoState>();
        private int RegionColumn;
        private int[] skipColumns;
        private string lastColumnName = string.Empty;
        private readonly string[] statuses = new string[] { "( n/a )", "Good", "Works (with errors)", "Not working" };
        private ListViewItem rightClickedItem = null;
        //private int[] colWidths;
        DiskInfo[] disk;
        ImageList icons = new ImageList();
        RichTextBox PVbox = new RichTextBox
        {
            Font = new Font("Courier New", 12)
        };
        Thread sort;
        ContextMenuStrip dbMenu = new ContextMenuStrip();
        //TopmostTooltip ContextTip = new TopmostTooltip();
        ToolTip ContextTip = new ToolTip();
        ToolStripMenuItem lockItem = new ToolStripMenuItem("Lock Image");
        ToolStripMenuItem unlockItem = new ToolStripMenuItem("Unlock Image");
        ToolStripMenuItem removeItem = new ToolStripMenuItem("Remove");
        ToolStripMenuItem editItem = new ToolStripMenuItem("Edit");
        ToolStripMenuItem dupeItem = new ToolStripMenuItem("Check for Duplicates");
        ToolStripMenuItem favItem = new ToolStripMenuItem("Add to Favorites");
        ToolStripMenuItem unfavItem = new ToolStripMenuItem("Unfavorite");
        ToolStripMenuItem sideItem = new ToolStripMenuItem("Disk Side #");
        ToolStripMenuItem regionItem = new ToolStripMenuItem("Disk Region");
        ToolStripMenuItem yearItem = new ToolStripMenuItem("Release Year");
        ToolStripMenuItem statusItem = new ToolStripMenuItem("Disk Status");
        ToolStripMenuItem protectionItem = new ToolStripMenuItem("Disk Protection Type");
        ToolStripMenuItem importItem = new ToolStripMenuItem("Import for Processing");
        ToolStripMenuItem exportItem = new ToolStripMenuItem("Export to File");
        ToolStripMenuItem markItem = new ToolStripMenuItem("Mark image for merging");
        ToolStripMenuItem mergeItem = new ToolStripMenuItem("Merge marked images");
        ToolStripMenuItem unmarkAllItem = new ToolStripMenuItem("Clear all marks");
        ToolStripSeparator[] dbSep = new ToolStripSeparator[5];

        ContextMenuStrip udMenu = new ContextMenuStrip();
        ToolStripMenuItem recoverItem = new ToolStripMenuItem("Recover");
        ToolStripMenuItem purgeItem = new ToolStripMenuItem("Purge All Items");

        DoubleBufferedListView dbView = new DoubleBufferedListView();
        DoubleBufferedListView dbRemv = new DoubleBufferedListView();

        Panel BuildDatabase = new Panel { Size = new Size(350, 80) };
        Label BuildStatus = new Label();

        Panel editPan = new Panel();
        Panel opts = new Panel();
        TextBox editTitle = new TextBox();
        NumericUpDown dSide = new NumericUpDown();
        NumericUpDown dYear = new NumericUpDown();
        ComboBox dRegn = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        ComboBox dProt = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        Button dOK = new Button();
        Button dCancel = new Button();
        Button dAddNotes = new Button();
        RichTextBox dNotes = new RichTextBox();
        Button nOK = new Button();
        Button nCancel = new Button();
        ComboBox dStat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        Button dFav = new Button();
        System.Windows.Forms.Timer tooltipCheckTimer = new System.Windows.Forms.Timer();
        string NotesChange = string.Empty;
        bool changeNotes = false;
        // database icon placement
        int fav = 2 * 20;
        int note = 1 * 20;
        int stat = 1 * 20;

        private System.Windows.Forms.ToolTip dbTooltip = new System.Windows.Forms.ToolTip();

        private readonly Dictionary<string, int> setRegion = new Dictionary<string, int>()
        {
            { "", 0 }, { "ntsc", 1 }, { "pal", 2 }
        };

        private readonly Dictionary<int, string> getRegion = new Dictionary<int, string>()
        {
            { 0, string.Empty }, { 1, "NTSC" }, { 2, "PAL" }
        };


        TextBox dbSearch = new TextBox
        {
            Width = 350,
            Height = 18,
            Top = 2, // 4,
            Left = 10, // 100,
            BackColor = Color.LightGray,
            ForeColor = Color.Black,
        };
        ProgressBar dbProg = new ProgressBar
        {
            Width = 340,
            Height = 10,
            Top = 7, // 9,
            Left = 105,
            BackColor = Color.White,
            ForeColor = Color.Black,
        };
        Label DBsearch = new Label
        {
            Top = 5, // 7,
            Left = 15, //105,
            Height = 15,
            Width = 55,
            Cursor = Cursors.IBeam,
            Text = "( Search )",
            ForeColor = Color.Gray,
            BackColor = Color.LightGray,
        };

        Button cancl = new Button
        {
            Text = "Cancel",
            Height = 20,
        };

        Button Undo = new Button
        {
            Top = 4,
            Height = 16,
            Width = 16,
            ImageAlign = ContentAlignment.BottomCenter,
            FlatStyle = FlatStyle.Flat,
        };
        Button Redo = new Button
        {
            Top = 4,
            Height = 16,
            Width = 16,
            ImageAlign = ContentAlignment.BottomCenter,
            FlatStyle = FlatStyle.Flat,
        };

        Button Prvw = new Button
        {
            Top = 1,
            Height = 22,
            Width = 22,
            ImageAlign = ContentAlignment.BottomCenter,
            FlatStyle = FlatStyle.Flat,
        };

        void SetDBContextItems()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    browseDBmenu.Enabled = disk?.Length - dbRemoved?.Count > 0;
                    recoverDBmenu.Visible = rebuildDBmenu.Visible = toolStripSeparator5.Visible = dbRemoved?.Count > 0;
                }));
            }
            else
            {
                browseDBmenu.Enabled = disk?.Length - dbRemoved?.Count > 0;
                recoverDBmenu.Visible = rebuildDBmenu.Visible = toolStripSeparator5.Visible = dbRemoved?.Count > 0;
            }
        }

        void Setup_Database_Window()
        {
            databaseToolStripMenuItem.Visible = EnableDBMenu.Checked;
            if (EnableDBMenu.Checked)
            {
                try
                {
                    ReadDB(false, false);
                    if (disk != null && disk?.Length > 0)
                    {
                        dbRemoved = disk != null && disk?.Length > 0 ? disk.Where(d => d.Marked).Select(d => d.Index).ToList() : new List<int>();
                    }
                }
                catch { }
                SetDBContextItems();
            }

            List<string> protList = new List<string> { "Auto-Detect" };
            for (int p = 0; p < DiskInfo.Prot.Count; p++) protList.Add(DiskInfo.Prot[p] == string.Empty ? "None" : DiskInfo.Prot[p]);
            bool mClick = false;
            editPan.MouseMove += (s, e) => EditPan_MouseMove(s, e);
            ProtDetectMethod.Enabled = EnableDBMenu.Checked;
            icons.ImageSize = new Size(20, 20);
            icons.Images.Add("lock", Resources._lock);      // lock icon
            icons.Images.Add("star", Resources.star);       // favorites icon
            icons.Images.Add("starU", Resources.starU);     // unfavorite icon
            icons.Images.Add("notes", Resources.notes);     // notes icon
            icons.Images.Add("edit", Resources.pencil);     // edit icon
            icons.Images.Add("notesH", Resources.notesH);   // notes highlighted icon
            icons.Images.Add("editH", Resources.pencilH);   // edit highlighted icon
            icons.Images.Add("editG", Resources.pencilG);   // edit grayed out
            icons.Images.Add("ok", Resources.OK);           // good image icon
            icons.Images.Add("bad", Resources.bad);         // bad image icon
            icons.Images.Add("wwe", Resources.wwe);         // works, with errors icon
            icons.Images.Add("recover", Resources.recover); // recover
            icons.Images.Add("recoverH", Resources.recoverH); // recover Hovered
            icons.Images.Add("redX", Resources.redX);       // recover
            icons.Images.Add("grnChk", Resources.greenChk); // recover Hovered
            icons.Images.Add("!undo", Resources.recoverH); // undo disabled
            icons.Images.Add("!redo", Resources.redoG); // redo disabled
            icons.Images.Add("undo", Resources.recover); // undo enabled
            icons.Images.Add("redo", Resources.redoGn); // redo enabled
            icons.Images.Add("preview", Resources.diskPreview); // redo enabled
            PopulateEditItems();

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
            dbView.Columns.Add("  ", 24, HorizontalAlignment.Center);
            dbView.Columns.Add("Side", 35, HorizontalAlignment.Center);
            dbView.Columns.Add("Year", 45, HorizontalAlignment.Center);
            dbView.Columns.Add("Region", 80, HorizontalAlignment.Center);
            dbView.Columns.Add("Protection", 100, HorizontalAlignment.Center);
            dbView.Columns.Add("Date Added", 130, HorizontalAlignment.Center);

            Controls.Add(BuildDatabase);
            BuildDatabase.Controls.Add(cancl);
            BuildDatabase.Controls.Add(BuildStatus);
            BuildStatus.Location = new Point(5, 5);
            BuildStatus.AutoSize = true;
            cancl.Top = 45;
            cancl.Left = (BuildDatabase.Width - cancl.Width) / 2;
            cancl.Click += (ss, ee) => cancel = true;

            dSide.Width = dbView.Columns[3].Width;
            dSide.Left = 450 + 24;
            dSide.Maximum = 32;
            dSide.Minimum = 0;
            dYear.Width = dbView.Columns[4].Width;
            dYear.Left = 450 + 24 + 35;
            dYear.Minimum = 1970;
            dYear.Maximum = dYear.Minimum + 127;
            dRegn.Width = dbView.Columns[5].Width;
            dRegn.Left = dYear.Left + dYear.Width;
            dRegn.DataSource = new string[] { "( n/a )", "NTSC", "PAL" };
            dProt.Left = dRegn.Left + dRegn.Width;
            dProt.Width = dbView.Columns[6].Width;
            dProt.Height = dRegn.Height = dStat.Height = 18;
            dStat.Width = dbView.Columns[7].Width;// - 40;
            dStat.Left = dProt.Left + dProt.Width;
            dStat.DataSource = statuses;
            dProt.DataSource = protList;
            editTitle.Width = dbView.Columns[1].Width - 70;
            editTitle.Top = 1;
            editTitle.Left = 2;
            editTitle.MaxLength = DiskInfo.NAME_SIZE;
            editPan.Controls.Add(editTitle);
            editPan.Controls.Add(dSide);
            editPan.Controls.Add(dYear);
            editPan.Controls.Add(dRegn);
            editPan.Controls.Add(dProt);
            editPan.Controls.Add(dOK);
            editPan.Controls.Add(dCancel);
            editPan.Controls.Add(dAddNotes);
            editPan.Controls.Add(dStat);
            editPan.Controls.Add(dFav);
            editPan.Height = 20;
            editPan.Width = dbView.Columns.Cast<ColumnHeader>().Sum(c => c.Width) - 24;
            editPan.Visible = editing;
            Size size = new Size(22, 20);
            int icX = 18;
            int icY = 14;
            dCancel.Size = size;
            dOK.Size = size;
            dAddNotes.Size = size;
            dFav.Size = size;
            dCancel.Left = GetColumnX(dbView, 2) - 24;
            dCancel.BackgroundImageLayout = dOK.BackgroundImageLayout = ImageLayout.Center;
            dFav.BackgroundImageLayout = dAddNotes.BackgroundImageLayout = ImageLayout.Center;
            dOK.Left = dCancel.Left - dCancel.Width;
            dCancel.BackgroundImage = ResizeIcon("redX", icX, icY);
            dOK.BackgroundImage = ResizeIcon("grnChk", icX, icY);
            dAddNotes.BackgroundImage = ResizeIcon("notes", icX, icY);
            dAddNotes.Top = 0;
            dAddNotes.Left = dOK.Left - 20;
            dAddNotes.Height = editPan.Height;
            dNotes.Width = EditNotes.Width - 20;
            dNotes.Height = EditNotes.Height - 90;
            dNotes.Location = new Point(2, 2);
            dNotes.MaxLength = DiskInfo.NOTES_SIZE;
            EditNotes.Controls.Add(dNotes);
            EditNotes.Controls.Add(nOK);
            EditNotes.Controls.Add(nCancel);
            dFav.Location = new Point(dAddNotes.Left - 20, 0); // + dStat.Width, 0);
            nOK.Dock = DockStyle.Bottom;
            nOK.Text = "OK";
            nCancel.Text = "Cancel";
            nCancel.Dock = DockStyle.Bottom;
            nCancel.Click += (s, e) =>
            {
                if (mClick)
                {
                    dNotes.Clear();
                    NotesChange = string.Empty;
                    changeNotes = false;
                    EditNotes.Close();
                    mClick = false;
                }
            };
            nOK.Click += (s, e) =>
            {
                NotesChange = dNotes.Text.Length > 0 ? SanitizeRichText(dNotes) : string.Empty;
                changeNotes = true;
                EditNotes.Close();
            };

            dAddNotes.BackColor = Color.LightGreen;
            BrowseDB.Controls.Add(editPan);
            dOK.MouseDown += (s, e) => CheckMouseClick(s, e);
            dFav.MouseDown += (s, e) => CheckMouseClick(s, e);
            dCancel.MouseDown += (s, e) => CheckMouseClick(s, e);
            dAddNotes.MouseDown += (s, e) => CheckMouseClick(s, e);
            dOK.Click += (s, e) =>
            {
                if (mClick)
                {
                    UpdateEditedItem();
                    mClick = false;
                }
            };

            dCancel.Click += (s, e) =>
            {
                if (mClick)
                {
                    editPan.Visible = false;
                    dbView.Enabled = dbSearch.Enabled = true;
                    editing = false;
                    BrowseDB.ControlBox = true;
                    mClick = false;
                }
            };
            dOK.MouseMove += (s, e) => ShowTooltipOnHover(s, e, "Apply Changes");
            dCancel.MouseMove += (s, e) => ShowTooltipOnHover(s, e, "Cancel and discard changes");
            dAddNotes.MouseMove += (s, e) => ShowTooltipOnHover(s, e, "Add/Edit notes");
            dStat.MouseMove += (s, e) => ShowTooltipOnHover(s, e, "Disk health status");
            dSide.MouseMove += (s, e) => ShowTooltipOnHover(s, e, "Disk Side");
            dYear.MouseMove += (s, e) => ShowTooltipOnHover(s, e, "Release Year\n1970 = No year displayed");
            dRegn.MouseMove += (s, e) => ShowTooltipOnHover(s, e, "Disk Region");
            dProt.MouseMove += (s, e) => ShowTooltipOnHover(s, e, "Copy Protection Type");
            dFav.MouseMove += (s, e) => ShowTooltipOnHover(s, e, favToggle ? "Remove from Favorites" : "Add to Favorites");
            tooltipCheckTimer.Interval = 100;
            tooltipCheckTimer.Tick += (s, e) =>
            {
                Point screenPos = Cursor.Position;
                Control hovered = editPan.GetChildAtPoint(editPan.PointToClient(screenPos), GetChildAtPointSkip.None);

                if (!editPan.Bounds.Contains(editPan.PointToClient(screenPos)) || hovered == null)
                {
                    dbTooltip.Hide(editPan); // or dbTooltip.RemoveAll() to be aggressive
                    tooltipCheckTimer.Stop();
                    tooltipCheckTimer.Dispose();
                }
            };

            dFav.Click += (s, e) =>
            {
                if (mClick)
                {
                    favToggle = !favToggle;
                    dFav.BackgroundImage = favToggle ? ResizeIcon("star", 18, 14) : ResizeIcon("starU", 18, 14);
                    mClick = false;
                }
            };
            dAddNotes.Click += (s, e) =>
            {
                if (mClick)
                {
                    EditNotes.Location = new Point(
                    this.Location.X + ((this.Width - EditNotes.Width) / 2),
                    this.Location.Y + (this.Height - EditNotes.Height) / 2);
                    dNotes.Text = disk[(int)dbView.SelectedItems[0].Tag].Notes;
                    dNotes.SelectionStart = dNotes.TextLength;
                    dNotes.SelectionLength = 0;
                    EditNotes.Text = $"Edit Notes ({dNotes.Text.Length}/{DiskInfo.NOTES_SIZE})";
                    dNotes.ScrollToCaret();
                    dNotes.Focus();
                    changeNotes = false;
                    EditNotes.ShowDialog(this);
                    mClick = false;
                }
            };
            dNotes.TextChanged += new System.EventHandler(this.Dnotes_TextChanged);

            dbRemv.OwnerDraw = true;
            dbRemv.View = View.Details; // Enables column mode
            dbRemv.FullRowSelect = true;
            dbRemv.GridLines = false; // Adds line separators
            dbRemv.MultiSelect = true;
            dbRemv.Scrollable = true;
            dbRemv.HideSelection = false;
            dbRemv.CheckBoxes = false;
            dbRemv.SmallImageList = icons;
            dbRemv.Columns.Add("Entry# ", 50, HorizontalAlignment.Center); // Lock Column
            dbRemv.Columns.Add("Title", 450, HorizontalAlignment.Left);
            dbRemv.Columns.Add("  ", 24, HorizontalAlignment.Center);
            dbRemv.Columns.Add("Side", 35, HorizontalAlignment.Center);
            dbRemv.Columns.Add("Year", 45, HorizontalAlignment.Center);
            dbRemv.Columns.Add("Region", 80, HorizontalAlignment.Center);
            dbRemv.Columns.Add("Protection", 100, HorizontalAlignment.Center);
            dbRemv.Columns.Add("Date Added", 130, HorizontalAlignment.Center);
            dbRemv.ContextMenuStrip = udMenu;  //Controls.Add(udMenu);
            udMenu.Items.Add(recoverItem);
            udMenu.Items.Add(purgeItem);
            recoverItem.Click += (s, e) =>
            {
                if (dbRemv != null && dbRemv.SelectedItems.Count > 0)
                {
                    List<int> update = new List<int>();
                    foreach (ListViewItem i in dbRemv.SelectedItems)
                    {
                        int index = (int)i.Tag;
                        disk[index].Marked = false;
                        update.Add(index);
                    }
                    if (update.Count > 0)
                    {
                        UpdateDBDirectory(update.ToArray());
                        using (var dataBase = new AccessDatabase().ReadOnly(TEMP.dbPath))
                        {
                            if (dataBase.Valid)
                            {
                                foreach (int index in update)
                                    try
                                    {
                                        disk[index] = dataBase.GetDirectoryEntry(index);
                                        disk[index].Index = index;
                                    }
                                    catch { }
                            }
                            else dataBase.Dispose();
                        }
                        dbRemoved = disk.Where(d => d.Marked).Select(d => d.Index).ToList();
                        int lastidx = GetLastVisibleIndex(dbRemv);
                        UpdateRemoveListView();
                        if (dbRemoved.Count == 0) RecoverDB?.Close();
                        else ScrollToIndex(dbRemv, lastidx);
                    }
                }
            };
            purgeItem.Click += (s, e) => PurgeItems_Click(s);

            int tWidth = dbRemv.Columns.Cast<ColumnHeader>().Sum(c => c.Width);
            RecoverDB.Controls.Add(dbRemv);
            dbRemv.Scrollable = true;
            dbRemv.Location = new Point(0, 0);
            dbRemv.Width = tWidth + 22;
            RecoverDB.Controls.Add(dbRemv);
            RecoverDB.Width = dbRemv.Width + 17; // RecoverDB.Width + 22;
            dbRemv.Height = RecoverDB.Height - 38 - dbRemv.Top; //BrowseDB.Height;
            dbRemv.DrawColumnHeader += DbView_DrawColumnHeader;
            dbRemv.DrawItem += DbView_DrawItem;
            dbRemv.DrawSubItem += DbView_DrawSubItem;
            dbRemv.MouseMove += DbView_MouseMove;

            List<int> skip = new List<int>();
            for (int i = 0; i < dbView.Columns.Count; i++)
            {
                if (dbView.Columns[i].Text.ToLower() == "region") RegionColumn = i;
                if (dbView.Columns[i].Text == " "
                    || dbView.Columns[i].Text == "  "
                    || dbView.Columns[i].Text.ToString().ToLower() == "region"
                    || dbView.Columns[i].Text.ToString().ToLower() == "side") skip.Add(i);
            }
            skipColumns = skip.ToArray();
            int totalWidth = dbView.Columns.Cast<ColumnHeader>().Sum(c => c.Width);
            BrowseDB.Controls.Add(dbView);
            dbView.Scrollable = true;
            dbView.Width = totalWidth + 22;
            BrowseDB.Width = dbView.Width + 17;
            CreateOptionsPanel();
            dbView.Height = BrowseDB.Height - 38 - opts.Height - 5; //BrowseDB.Height;
            dbView.ShowItemToolTips = true;
            dbView.Location = new Point(0, opts.Height);
            
            BrowseDB.Controls.Add(opts);
            opts.BringToFront();
            bdbW = BrowseDB.Width;
            BrowseDB.Controls.Add(PVbox);
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
            dbProg.Visible = false;
            Set_Behaviors();

            void Set_Behaviors()
            {
                browseDBmenu.Click += (s, e) => OpenDB_Windows();
                addFolderDBmenu.Click += (s, e) =>
                {
                    FolderBrowserDialog opn = new FolderBrowserDialog { ShowNewFolderButton = false };
                    opn.ShowDialog();
                    string path = opn.SelectedPath;
                    Task.Run(() =>
                    {
                        if (path != null && Directory.Exists(path))
                        {
                            var top = dbProg.Top;
                            var left = dbProg.Left;
                            Invoke(new Action(() =>
                            {
                                menuStrip1.Enabled = false;

                                BuildDatabase.Location = new Point(
                                    (this.ClientSize.Width - BuildDatabase.Width) / 2,
                                    (this.ClientSize.Height - BuildDatabase.Height) / 2);
                                BuildDatabase.Controls.Add(dbProg);
                                dbProg.Visible = true;
                                dbProg.Location = new Point(5, 30);
                                dbProg.Value = 0;
                                dbProg.Maximum = 100 * 100;
                                dbProg.Value = dbProg.Maximum / 100;
                                BuildDatabase.BringToFront();
                                BuildDatabase.Visible = true;
                            }));
                            Invoke(new Action(() => Worker_Main = new Thread(new ThreadStart(() => BuildDB(path)))));
                            Worker_Main.Start();
                            Worker_Main.Join();
                            ReadDB(true, false);
                            Invoke(new Action(() =>
                            {
                                dbProg.Location = new Point(left, top);
                                //BrowseDB.Controls.Add(dbProg);
                                dbProg.Visible = false;
                                opts.Controls.Add(dbProg);
                                BuildDatabase.Visible = false; ;
                                SetDBContextItems();
                                menuStrip1.Enabled = true;
                            }));
                        }
                    });
                };

                recoverDBmenu.Click += (s, e) =>
                {
                    if (disk == null || disk.Length < 1) ReadDB(true, false);
                    if (disk != null && dbRemoved.Count > 0 && disk?.Length > 0)
                    {
                        RecoverDB.Location = new Point(
                        this.Location.X + ((this.Width - RecoverDB.Width) / 2),
                        this.Location.Y + (this.Height - RecoverDB.Height) / 2);
                        UpdateRemoveListView();
                        RecoverDB.ShowDialog(this);
                    }
                };
                rebuildDBmenu.Click += (s, e) => PurgeItems_Click(s);

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
                dbView.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        rightClickedItem = dbView.GetItemAt(e.X, e.Y);
                    }
                };

                dbView.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        ListView lv = (ListView)s;
                        ListViewHitTestInfo hit = lv.HitTest(e.Location);

                        if (hit.Item != null)
                        {
                            int index = (int)hit.Item.Tag;

                            // Determine virtual sub-item index using vColumns
                            int subItemIndex = -1;
                            int left = hit.Item.Bounds.Left;

                            for (int i = 0; i < dbView.vColumns.Length; i++)
                            {
                                var colRect = new Rectangle(left, hit.Item.Bounds.Top, dbView.vColumns[i], hit.Item.Bounds.Height);
                                if (colRect.Contains(e.Location))
                                {
                                    subItemIndex = i;
                                    break;
                                }
                                left += dbView.vColumns[i];
                            }

                            // Handle only virtual column 2 ("edit") clicks
                            if (subItemIndex == 2) EditItemHandler(index);
                        }
                    }
                };

                dbRemv.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        var view = (DoubleBufferedListView)s;
                        ListViewHitTestInfo hit = view.HitTest(e.Location);

                        if (hit.Item != null && hit.Item.Tag is int index && index >= 0 && index < disk.Length)
                        {
                            // Compute virtual column rectangles for the clicked item
                            Rectangle itemBounds = hit.Item.GetBounds(ItemBoundsPortion.Entire);
                            int xPos = itemBounds.Left;
                            Rectangle vCol2 = Rectangle.Empty;

                            for (int i = 0; i < view.vColumns.Length; i++)
                            {
                                Rectangle colRect = new Rectangle(xPos, itemBounds.Top, view.vColumns[i], itemBounds.Height);
                                if (i == 2)
                                {
                                    vCol2 = colRect;
                                    break;
                                }
                                xPos += view.vColumns[i];
                            }

                            if (vCol2.Contains(e.Location) && !disk[index].Locked)
                            {
                                disk[index].Marked = false;
                                UpdateDBDirectory(new int[] { index });

                                using (var dataBase = new AccessDatabase().ReadOnly(TEMP.dbPath))
                                {
                                    if (dataBase.Valid)
                                    {
                                        try
                                        {
                                            disk[index] = dataBase.GetDirectoryEntry(index);
                                            disk[index].Index = index;
                                        }
                                        catch { }
                                    }
                                    else dataBase.Dispose();
                                }

                                dbRemoved = disk.Where(d => d.Marked).Select(d => d.Index).ToList();
                                int lastidx = GetLastVisibleIndex(dbRemv);
                                UpdateRemoveListView();

                                if (dbRemoved.Count == 0) RecoverDB?.Close();
                                else ScrollToIndex(dbRemv, lastidx);
                            }
                        }
                    }
                };

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

                dbView.MouseUp += (s, e) =>
                {
                    dbMenu.Items.Clear();
                    if (e.Button == MouseButtons.Right)
                    {
                        var separator = 0;
                        ListViewItem item = dbView.GetItemAt(e.X, e.Y);
                        if (item != null)
                        {
                            if (!item.Selected)
                            {
                                dbView.SelectedItems.Clear();
                                item.Selected = true;
                            }
                            int idx = (int)item.Tag;
                            dbMenu.Items.AddRange(new ToolStripDropDownItem[] { lockItem, unlockItem, favItem, unfavItem });
                            dbMenu.Items.Add(dbSep[separator++]);
                            dbMenu.Items.Add(editItem);
                            dbMenu.Items.Add(dbSep[separator++]);
                            dbMenu.Items.Add(importItem);
                            dbMenu.Items.Add(exportItem);
                            if (disk.Length > 1)
                            {
                                dbMenu.Items.Add(dupeItem);
                                dbMenu.Items.Add(dbSep[separator++]);
                                dbMenu.Items.Add(markItem);
                                dbMenu.Items.Add(mergeItem);
                                dbMenu.Items.Add(unmarkAllItem);
                                markItem.Text = item.Checked ? "Remove from merge list" : "Mark image for merging";
                            }
                            dbMenu.Items.Add(dbSep[separator++]);
                            dbMenu.Items.Add(removeItem);
                            SetEnabled(dbView.SelectedItems.Count, item.Checked);
                        }

                        dbMenu.Show(dbView, e.Location);
                    }

                    void SetEnabled(int items, bool chk)
                    {
                        bool allLocked = true;
                        bool anyLocked = false;
                        bool anyUnlocked = false;
                        bool allUnlocked = true;
                        bool anyFav = false;
                        bool anyUnfav = false;
                        int checkedItems = 0;

                        foreach (ListViewItem chked in dbView.Items) if (chked.Checked) checkedItems++;
                        foreach (ListViewItem item in dbView.SelectedItems)
                        {
                            
                            var d = disk[(int)item.Tag];
                            if (d.Locked)
                            {
                                anyLocked = true;
                                allUnlocked = false;
                            }
                            else anyUnlocked = true;
                            allLocked &= d.Locked;

                            if (d.Favorite) anyFav = true;
                            else anyUnfav = true;
                        }
                        importItem.Enabled = items == 1;

                        // Fields with bulk-edit actions
                        sideItem.Enabled = !allLocked;
                        regionItem.Enabled = !allLocked;
                        yearItem.Enabled = !allLocked;
                        protectionItem.Enabled = !allLocked;
                        if (checkedItems > 2 && chk)
                        {
                            markItem.Text = "Remove from merge list";
                            markItem.Enabled = true;
                        }
                        else markItem.Enabled = (checkedItems < 3 && !chk && dbView.SelectedItems.Count == 1);
                        unmarkAllItem.Enabled = checkedItems > 0;
                        mergeItem.Enabled = checkedItems > 1;
                        // Handle menu text decoration for locked items
                        SetMenuItemText(sideItem, "Disk Side #");
                        SetMenuItemText(regionItem, "Disk Region");
                        SetMenuItemText(yearItem, "Release Year");
                        SetMenuItemText(protectionItem, "Disk Protection Type");
                        SetMenuItemText(removeItem, "Remove");

                        removeItem.Enabled = anyUnlocked;
                        lockItem.Visible = anyUnlocked;  // Only show Lock if there's something to lock
                        unlockItem.Visible = anyLocked;  // Only show Unlock if there's something to unlock
                        favItem.Visible = anyUnfav;      // Show if any are not favorited
                        unfavItem.Visible = anyFav;      // Show if any are already favorited

                        void SetMenuItemText(ToolStripMenuItem item, string baseText)
                        {
                            if (!allUnlocked && anyUnlocked) item.Text = anyUnlocked ? $"* {baseText}" : baseText;
                            else item.Text = baseText;
                        }

                        AddTooltipHandlers(dbMenu.Items);

                        void AddTooltipHandlers(ToolStripItemCollection itemss)
                        {
                            foreach (ToolStripItem item in itemss)
                            {
                                SetTip(item);
                                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                                {
                                    AddTooltipHandlers(menuItem.DropDownItems); // recurse
                                }
                            }
                        }

                        void SetTip(ToolStripItem item)
                        {
                            item.MouseEnter += (ss, ee) =>
                            {
                                if (ss is ToolStripItem itemss && itemss.Text.StartsWith("*"))
                                {
                                    Point screenLocation = dbMenu.Bounds.Location;
                                    screenLocation.Offset(30, dbMenu.Height + 10);
                                    Point clientLocation = dbMenu.SourceControl.PointToClient(screenLocation);
                                    ContextTip.Show("* Locked items in selection will be ignored", dbMenu.SourceControl, clientLocation.X, clientLocation.Y);
                                }
                            };
                            item.MouseLeave += (ss, eee) => ContextTip.Hide(dbMenu);
                        }
                    }

                };
            }

            void PopulateEditItems()
            {
                editItem.DropDownItems.AddRange(new ToolStripMenuItem[] { statusItem, sideItem, regionItem, yearItem, protectionItem });

                // Disk Sides context menu items
                int totalSides = 32;
                int groupSize = 10;
                ToolStripMenuItem[] groupHeaders = Enumerable.Range(0, (totalSides + groupSize - 1) / groupSize)
                    .Select(i => new ToolStripMenuItem($"{i * groupSize + 1} - {Math.Min((i + 1) * groupSize, totalSides)}")).ToArray();
                foreach (var header in groupHeaders) sideItem.DropDownItems.Add(header);
                for (int i = 0; i < totalSides; i++)
                {
                    int sideValue = i;
                    ToolStripMenuItem item = new ToolStripMenuItem($"{sideValue + 1}");
                    groupHeaders[i / groupSize].DropDownItems.Add(item);
                    item.Click += (s, e) => EditDiskField(s, sideValue, d => d.Side, (d, val) => d.Side = val, true);
                }

                // Disk Year context menu items
                totalSides = 128;
                groupSize = 20;
                ToolStripMenuItem[] yearHeaders = Enumerable.Range(0, (totalSides + groupSize - 1) / groupSize)
                    .Select(i => new ToolStripMenuItem($"{(i * groupSize == 0 ? "none" : $"{i * groupSize + 1970}")} - {Math.Min((i + 1) * groupSize, totalSides) + 1969}")).ToArray();
                foreach (var header in yearHeaders) yearItem.DropDownItems.Add(header);
                for (int i = 0; i < totalSides; i++)
                {
                    int yearValue = i;
                    ToolStripMenuItem item = new ToolStripMenuItem(yearValue == 0 ? "none" : $"{yearValue + 1970}");
                    yearHeaders[i / groupSize].DropDownItems.Add(item);
                    item.Click += (s, e) => EditDiskField(s, yearValue + 1970, d => d.Year, (d, val) => d.Year = val, true);
                }
                ToolStripMenuItem[] chgRegion = new ToolStripMenuItem[3];

                // Disk Region context menu Items
                for (int i = 0; i < getRegion.Count; i++)
                {
                    int x = i;
                    chgRegion[i] = new ToolStripMenuItem(i == 0 ? "n/a [ Unknown ]" : getRegion[i]);
                    regionItem.DropDownItems.Add(chgRegion[i]);
                    chgRegion[i].Click += (s, e) => EditDiskField(s, x, d => d.Region, (d, val) => d.Region = val, true);
                }
                ToolStripMenuItem[] chgProt = new ToolStripMenuItem[protList.Count];

                // Disk Protection context menu items
                for (int i = 0; i < protList.Count; i++)
                {
                    byte x = (byte)i;
                    chgProt[i] = new ToolStripMenuItem(protList[i]);
                    protectionItem.DropDownItems.Add(chgProt[i]);
                    chgProt[i].Click += (s, e) => EditDiskField(s, x, d => d.Protection, (d, val) => d.Protection = val, true);
                }
                ToolStripMenuItem[] chgStat = new ToolStripMenuItem[statuses.Length];

                // Disk Status context menu Items
                for (int i = 0; i < statuses.Length; i++)
                {
                    int x = i;
                    chgStat[i] = new ToolStripMenuItem(statuses[i]);
                    statusItem.DropDownItems.Add(chgStat[i]);
                    chgStat[i].Click += (s, e) =>
                    {
                        int status = x;
                        EditDiskField(s, status, d => d.Status, (d, val) => d.Status = val, false);
                    };
                }

                unlockItem.Click += (s, e) => EditDiskField(s, s.Equals(lockItem), d => d.Locked, (d, val) => d.Locked = val, false);
                lockItem.Click += (s, e) => EditDiskField(s, s.Equals(lockItem), d => d.Locked, (d, val) => d.Locked = val, false);
                favItem.Click += (s, e) => EditDiskField(s, s.Equals(favItem), d => d.Favorite, (d, val) => d.Favorite = val, false);
                unfavItem.Click += (s, e) => EditDiskField(s, s.Equals(favItem), d => d.Favorite, (d, val) => d.Favorite = val, false);

                removeItem.Click += (s, e) =>
                {
                    int lastidx = GetLastVisibleIndex(dbView);
                    List<int> remove = new List<int>();
                    foreach (ListViewItem item in dbView.SelectedItems)
                    {
                        var idx = (int)item.Tag;
                        remove.Add((int)item.Tag);
                    }
                    if (remove.Count > 1)
                    {
                        string message = $"Proceed with removal of ({remove.Count}) files?";
                        string title = "Multiple files selected!";
                        RemoveEntries(remove.ToArray(), title, message);
                    }
                    else RemoveEntries(remove.ToArray());
                    if (disk.Length != 0) ScrollToIndex(dbView, lastidx);
                    SetDBContextItems();
                };

                dupeItem.Click += (s, e) =>
                {
                    int[] dupes = CheckForDuplicates(true, true);
                    if (dupes.Length > 0)
                    {
                        string message = $"({dupes.Length}) Duplicates found!\nWould you like to remove them?";
                        string title = "Duplicates found!";
                        int lastidx = GetLastVisibleIndex(dbView);
                        RemoveEntries(dupes, title, message);
                        if (disk.Length != 0) ScrollToIndex(dbView, lastidx);
                        SetDBContextItems();
                    }
                    else
                    {
                        using (Message_Center msgs = new Message_Center(this))
                            MessageBox.Show("No duplicates found!", "Congratulations!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                importItem.Click += (s, e) =>
                {
                    if (dbView.SelectedItems.Count == 1 && disk?.Length > 0)
                    {
                        int index = (int)dbView.SelectedItems[0].Tag;
                        if (index >= 0 && index < disk?.Length) Import_Image_From_Database(index);
                    }
                };

                exportItem.Click += (s, e) =>
                {
                    if (dbView.SelectedItems.Count == 0 || disk?.Length == 0) return;

                    List<int> images = new List<int>();
                    List<string> fnames = new List<string>();
                    Dictionary<string, int> nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    Dictionary<int, string> extension = new Dictionary<int, string>() { { 0, ".nib" }, { 1, ".nbz" } };

                    foreach (ListViewItem item in dbView.SelectedItems)
                    {
                        int idx = (int)item.Tag;
                        images.Add(idx);

                        string baseName = disk[idx].Title.Replace(" ", "_").ToLower();
                        string ext = extension[disk[idx].Extension];
                        string fullName = baseName + ext;

                        if (nameCounts.TryGetValue(fullName, out int count))
                        {
                            nameCounts[fullName] = ++count;
                            fullName = $"{baseName}_{count}{ext}";
                        }
                        else nameCounts[fullName] = 0;

                        fnames.Add(fullName);
                    }

                    if (images.Count == 0) return;

                    bool showProgress = images.Count > 10;
                    if (showProgress) InitProgressBar();

                    using (FolderBrowserDialog opn = new FolderBrowserDialog())
                    {
                        opn.ShowNewFolderButton = true;
                        if (opn.ShowDialog() != DialogResult.OK) return;

                        string path = opn.SelectedPath;
                        if (!Directory.Exists(path))
                        {
                            MessageForYouSir("Error", $"Error accessing selected path\n{path}");
                            return;
                        }

                        bool errors = false, overwrite = false, prompt = false;

                        for (int i = 0; i < images.Count; i++)
                        {
                            try
                            {
                                int idx = images[i];
                                byte[] data = GetNIBData(idx);
                                if (data == null) continue;

                                string writefile = Path.Combine(path, fnames[i]);
                                if (!prompt && File.Exists(writefile))
                                {
                                    DialogResult result = MessageBox.Show(
                                        "File already exists! Overwrite?\n(This choice will apply to all)",
                                        "Overwrite Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                    overwrite = (result == DialogResult.Yes);
                                    prompt = true;
                                }

                                bool compress = extension[disk[idx].Extension] == ".nbz";
                                if (!File.Exists(writefile) || overwrite)
                                {
                                    File.WriteAllBytes(writefile, compress ? LZcompress(data) : data);
                                }
                            }
                            catch { errors = true; }
                            UpdateProgressBar(i, images.Count);
                        }


                        MessageForYouSir(errors ? "Export Completed with Errors" : "Export Complete",
                                         errors ? "Some files may not have been exported." : "File(s) successfully exported.");
                    };
                    dbProg.Visible = false;
                };

                markItem.Click += (s, e) =>
                {
                    if (dbView.SelectedItems.Count == 1)
                    {
                        Text = s.ToString();
                        bool chk = !s.ToString().ToLower().Contains("remove");
                        foreach (ListViewItem item in dbView.SelectedItems) item.Checked = chk;
                        dbView.Invalidate();
                    }
                };

                mergeItem.Click += (s, e) =>
                {
                    string n = "";
                    foreach (ListViewItem item in dbView.Items)
                    {
                        if (item.Checked)
                        {
                            int index = (int)item.Tag;
                            n += $"{disk[index].Title} ";
                        }
                    }
                    Text = n;
                };

                unmarkAllItem.Click += (s, e) =>
                {
                    foreach (ListViewItem item in dbView.Items) if (item.Checked) item.Checked = false;
                };
            }

            void CreateOptionsPanel()
            {
                Undo.Left = dbSearch.Width + dbSearch.Left + 5;
                Undo.ImageAlign = ContentAlignment.BottomCenter;
                Redo.Left = Undo.Width + Undo.Left + 5;
                Redo.ImageAlign = ContentAlignment.BottomCenter;
                Prvw.Left = Redo.Width + Redo.Left + 5;
                Prvw.ImageAlign = ContentAlignment.BottomCenter;
                Undo.FlatAppearance.BorderSize = 0;
                Redo.FlatAppearance.BorderSize = 0;
                Prvw.FlatAppearance.BorderSize = 0;
                SetButtonImages();
                ToolTip dbTip = new ToolTip();
                dbTip.SetToolTip(Undo, "Undo");
                dbTip.SetToolTip(Redo, "Redo");
                dbTip.SetToolTip(Prvw, "Toggle Directory Preview *'CTRL' key");
                Undo.Click += (s, e) => UndoClick();
                Redo.Click += (s, e) => RedoClick();
                Prvw.Click += (s, e) => ToggleDirPreview();
                opts.Width = BrowseDB.Width - 17;
                opts.Height = 24;
                opts.Location = new Point(0, 0);
                opts.Controls.AddRange(new Control[] { dbSearch, DBsearch, dbProg, Undo, Redo, Prvw });
                dbProg.Location = new Point(BrowseDB.Width - (dbProg.Width + 35), 7);
                dbProg.Visible = false;
            }

            void CheckMouseClick(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) mClick = true;
            }

            void ShowTooltipOnHover(object sender, MouseEventArgs e, string message)
            {
                if (sender is Control ctrl)
                {
                    tooltipCheckTimer.Start();
                    Point screenPos = Cursor.Position;
                    screenPos.X += 32;
                    Point clientPos = editPan.PointToClient(screenPos);
                    dbTooltip.Show(message, editPan, clientPos);
                    dAddNotes.BackgroundImage = ctrl == dAddNotes ? ResizeIcon("notesH", icX, icY) : ResizeIcon("notes", icX, icY);
                }
            }

            int GetColumnX(ListView listView, int columnIndex)
            {
                int x = 0;
                for (int i = 0; i < columnIndex; i++)
                {
                    x += listView.Columns[i].Width;
                }
                return x;
            }
        }

        int GetLastVisibleIndex(ListView lv)
        {
            int lastVisibleIndex = -1;
            for (int i = lv.TopItem.Index; i < lv.Items.Count; i++)
            {
                var rect = lv.GetItemRect(i);
                if (rect.Bottom <= lv.ClientSize.Height)
                {
                    lastVisibleIndex = i;
                }
                else
                {
                    break;
                }
            }
            return lastVisibleIndex;
        }

        void ScrollToIndex(ListView lv, int lastVisibleIndex)
        {
            if (lv.Items.Count == 0) return;
            lv.BeginUpdate();
            if (lastVisibleIndex >= 0 && lastVisibleIndex < lv.Items.Count)
            {
                int itemHeight = lv.GetItemRect(0).Height;
                if (itemHeight > 0)
                {
                    int visibleCount = lv.ClientSize.Height / itemHeight;
                    int topIndex = lastVisibleIndex - visibleCount + 2;
                    if (topIndex < 0) topIndex = 0;
                    if (topIndex < lv.Items.Count)
                    {
                        lv.TopItem = lv.Items[topIndex];
                    }
                }
            }
            lv.EndUpdate();
            lv.Invalidate();
        }

        bool AnySelectedItemsLocked()
        {
            foreach (ListViewItem lv in dbView.SelectedItems)
            {
                int index = (int)lv.Tag;
                if (index >= 0 && index < disk.Length && disk[index].Locked)
                    return true;
            }
            return false;
        }

        void EditItemHandler(int index, bool multiple = false, bool AnyLockedImages = false)
        {
            if (index >= 0 && index < disk.Length)
            {
                editing = true;
                BrowseDB.ControlBox = false;
                var itemBounds = dbView.SelectedItems[0].Bounds;
                int x = dbView.Left + itemBounds.Left + 24;
                int y = dbView.Top + itemBounds.Top + 2;
                if (multiple && rightClickedItem != null)
                {
                    itemBounds = rightClickedItem.Bounds;
                    x = dbView.Left + itemBounds.Left + 24;
                    y = dbView.Top + itemBounds.Top + 2;
                    editPan.Location = new Point(x, y);
                }
                editPan.Top = y;
                editPan.Left = x;
                editPan.BringToFront();
                editPan.Visible = true;
                var idx = !multiple ? (int)dbView.SelectedItems[0].Tag : (int)rightClickedItem.Tag;
                bool locked = AnyLockedImages || disk[idx].Locked;
                editTitle.Text = multiple ? "Multi-select-edit mode, Cannot change name" : disk[idx].Title;
                dSide.Value = disk[idx].Side + 1;
                dYear.Value = disk[idx].Year;
                dRegn.SelectedIndex = disk[idx].Region;
                dProt.SelectedIndex = disk[idx].Protection + 1;
                dStat.SelectedIndex = disk[idx].Status;
                favToggle = disk[idx].Favorite;
                dFav.BackgroundImage = favToggle ? ResizeIcon("star", 18, 14) : ResizeIcon("starU", 18, 14);
                editTitle.Enabled = dAddNotes.Visible = !locked && !multiple && !AnyLockedImages;
                dSide.Enabled = dYear.Enabled = dRegn.Enabled = dProt.Enabled = !locked && (!multiple || !AnyLockedImages);
                dNotes.Text = disk[idx].Notes;
                dbTooltip.Hide(dbView);
                editTitle.Focus();
                dbView.Enabled = dbSearch.Enabled = false;
            }
        }

        Image ResizeIcon(string iconName, int width, int height)
        {
            if (icons.Images.ContainsKey(iconName))
            {
                using (var original = icons.Images[iconName])
                {
                    return new Bitmap(original, new Size(width, height));
                }
            }
            return null;
        }

        void UpdateRemoveListView()
        {
            dbRemv.BeginUpdate();
            dbRemv.Items.Clear();
            foreach (int i in dbRemoved)
            {
                ListViewItem item = new ListViewItem($"{i}") { Tag = i };   // Locked
                dbRemv.Items.Add(item);
            }
            dbRemv.EndUpdate();
            SetDBContextItems();
            RecoverDB.Text = $"Browse images deleted from database ({dbRemv.Items.Count})";
        }

        void CloseRecoverWindow()
        {
            dbRemv.Visible = false;
            this.Controls.Add(dbRemv);
            RecoverDB?.Close();
        }

        void OpenDB_Windows()
        {
            BrowseDB.Location = new Point(
                    this.Location.X + ((this.Width - BrowseDB.Width) / 2),
                    this.Location.Y + (this.Height - BrowseDB.Height) / 2);
            dbView.Items.Clear();
            Task.Run(delegate
            {
                if (disk == null || disk.Length == 0 || dbView.Items.Count == 0) ReadDB();
                SetDBContextItems();
            });
            Thread.Sleep(150);
            BrowseDB.ShowDialog(this);
        }

        void UpdatedbView()
        {
            if (disk?.Length > 0)
            {
                if (dbSearch.Text.Length > 0)
                {
                    var text = dbSearch.Text;
                    Invoke(new Action(() =>
                    {
                        dbSearch.Text = string.Empty;
                        dbSearch.Text = text;
                    }));
                }
                else
                {
                    var sortedList = disk.OrderBy(d => d.Title).ToList();
                    Invoke(new Action(() => UpdateList(RegionFilter(sortedList), true)));
                }
                if (disk?.Length == dbRemoved.Count) ShowMessage();
            }
            else ShowMessage();

            void ShowMessage()
            {
                Invoke(new Action(() =>
                {
                    using (Message_Center center = new Message_Center(this))
                    {
                        MessageBox.Show("No images in the database", "Database is empty!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    BrowseDB?.Close();
                }));
            }
        }

        void UpdateDBDirectory(int[] index)
        {
            if (index.Length > 0)
            {
                int items = index.Length;
                bool update = items > 100;
                if (update) InitProgressBar();
                int current = 0;
                using (var dataBase = new AccessDatabase().ReadWrite(TEMP.dbPath))
                {
                    if (dataBase.Valid)
                    {
                        if (File.Exists(TEMP.dbTempDir))
                        {
                            using (FileStream tdir = new FileStream(TEMP.dbTempDir, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                            {
                                foreach (int i in index)
                                {
                                    var entry = disk[i].ToEntry();
                                    UpdateTempDir(tdir, i, entry);
                                }
                            }
                        }
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
                            if (update && current++ > 1) UpdateProgressBar(current, items);
                        }
                    }
                    else dataBase.Dispose();
                }
            }
            dbProg.Visible = false;
        }

        void UpdateTempDir(FileStream tdir, int index, byte[] entry)
        {
            long length = tdir.Length;
            if (length % DiskInfo.ENTRY_SIZE == 0 && index * DiskInfo.ENTRY_SIZE < length)
            {
                tdir.Seek(index * DiskInfo.ENTRY_SIZE, SeekOrigin.Begin);
                tdir.Write(entry, 0, entry.Length);
                tdir.Flush();
            }
        }

        void UndoClick()
        {
            if (undoStack.Count > 0)
            {
                var undo = undoStack.Count - 1;
                var entrylist = Decompress(undoStack[undo].Data);
                var indexes = undoStack[undo].Indexes;

                bool update = indexes.Length > 100;
                int currentItem = 0;
                if (update) InitProgressBar();

                Dictionary<int, ListViewItem> itemMap = dbView.Items
                    .Cast<ListViewItem>()
                    .ToDictionary(item => (int)item.Tag);
                // Create a Redo option
                using (MemoryStream buffer = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(buffer))
                {
                    foreach (int index in indexes) writer.Write(disk[index].ToEntry());
                    redoStack.Add(new UndoState(indexes, Compress(buffer.ToArray())));
                }
                if (entrylist.Length % DiskInfo.ENTRY_SIZE == 0 && (entrylist.Length / DiskInfo.ENTRY_SIZE) == indexes.Length)
                {
                    using (AccessDatabase db = new AccessDatabase().ReadWrite(TEMP.dbPath))
                    using (MemoryStream buffer = new MemoryStream(entrylist))
                    {
                        if (File.Exists(TEMP.dbTempDir))
                        {
                            using (FileStream tdir = new FileStream(TEMP.dbTempDir, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                            {
                                foreach (int i in indexes)
                                {
                                    var entry = disk[i].ToEntry();
                                    UpdateTempDir(tdir, i, entry);
                                }
                            }
                        }
                        dbView.SelectedItems.Clear();
                        for (int i = 0; i < indexes.Length; i++)
                        {
                            if (indexes[i] < 0 || indexes[i] >= disk.Length || indexes[i] >= dbView.Items.Count)
                                continue;
                            var temp = new byte[DiskInfo.ENTRY_SIZE];
                            buffer.Seek(i * DiskInfo.ENTRY_SIZE, SeekOrigin.Begin);
                            buffer.Read(temp, 0, temp.Length);
                            CopyDiskInfoData(DiskInfo.FromEntry(temp), indexes[i]);
                            if (itemMap.TryGetValue(indexes[i], out var item)) item.Selected = true;
                            var entry = disk[indexes[i]].ToEntry();
                            if (entry.Length == DiskInfo.ENTRY_SIZE)
                            {
                                long seekidx = db.Offset + (indexes[i] * DiskInfo.ENTRY_SIZE);
                                db.Seek(seekidx);
                                db.Write(entry);
                            }
                            if (update && currentItem++ > 0) UpdateProgressBar(currentItem, indexes.Length);
                        }
                    }
                }
                undoStack.RemoveAt(undo);
            }
            dbProg.Visible = false;
            dbView.Invalidate();
            SetButtonImages();
        }

        void UpdateProgressBar(int currentItem, int totalItems)
        {
            if (InvokeRequired) Invoke(new Action(() => dbProg.Maximum = (int)((double)dbProg.Value / (double)(currentItem + 1) * totalItems)));
            else dbProg.Maximum = (int)((double)dbProg.Value / (double)(currentItem + 1) * totalItems);
        }

        void InitProgressBar()
        {
            dbProg.Maximum = 100 * 100;
            dbProg.Value = 100;
            dbProg.Visible = true;
        }

        void RedoClick()
        {
            if (redoStack.Count > 0)
            {
                var redo = redoStack.Count - 1;
                var entrylist = Decompress(redoStack[redo].Data);
                var indexes = redoStack[redo].Indexes;

                Dictionary<int, ListViewItem> itemMap = dbView.Items
                    .Cast<ListViewItem>()
                    .ToDictionary(item => (int)item.Tag);
                // Create an Undo state before redoing
                using (MemoryStream buffer = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(buffer))
                {
                    foreach (int index in indexes)
                        writer.Write(disk[index].ToEntry());

                    undoStack.Add(new UndoState(indexes, Compress(buffer.ToArray())));
                    if (undoStack.Count > MAX_UNDO)
                        undoStack.RemoveAt(0);
                }

                bool update = indexes.Length > 100;
                //int currentItem = 0;
                if (update) InitProgressBar();

                if (entrylist.Length % DiskInfo.ENTRY_SIZE == 0 &&
                    (entrylist.Length / DiskInfo.ENTRY_SIZE) == indexes.Length)
                {
                    using (AccessDatabase db = new AccessDatabase().ReadWrite(TEMP.dbPath))
                    using (MemoryStream buffer = new MemoryStream(entrylist))
                    {
                        dbView.SelectedItems.Clear();
                        for (int i = 0; i < indexes.Length; i++)
                        {
                            if (indexes[i] < 0 || indexes[i] >= disk.Length || indexes[i] >= dbView.Items.Count)
                                continue;

                            var temp = new byte[DiskInfo.ENTRY_SIZE];
                            buffer.Seek(i * DiskInfo.ENTRY_SIZE, SeekOrigin.Begin);
                            buffer.Read(temp, 0, temp.Length);
                            CopyDiskInfoData(DiskInfo.FromEntry(temp), indexes[i]);
                            if (itemMap.TryGetValue(indexes[i], out var item))
                            {
                                item.Selected = true;
                                if (temp.Length == DiskInfo.ENTRY_SIZE)
                                {
                                    long seekidx = db.Offset + (indexes[i] * DiskInfo.ENTRY_SIZE);
                                    db.Seek(seekidx);
                                    db.Write(temp);
                                }
                                if (update) UpdateProgressBar(i, indexes.Length);
                            }
                        }
                    }
                }
                redoStack.RemoveAt(redo);
            }
            dbProg.Visible = false;
            dbView.Invalidate();
            SetButtonImages();

        }

        void SetButtonImages()
        {
            Image uicon;
            Image ricon;
            bool u = undoStack.Count > 0;
            bool r = redoStack.Count > 0;
            uicon = u ? ResizeIcon("undo", 16, 16) : ResizeIcon("!undo", 16, 16);
            ricon = r ? ResizeIcon("redo", 16, 16) : ResizeIcon("!redo", 16, 16);
            Image preview = ResizeIcon("preview", 22, 22);
            if (!u) uicon = GetTransparentImage(uicon, 50);
            if (!r) ricon = GetTransparentImage(ricon, 50);
            
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    Undo.BackgroundImage = uicon;
                    Redo.BackgroundImage = ricon;
                    Undo.Enabled = u;
                    Redo.Enabled = r;
                    Prvw.BackgroundImage = preview;
                }));
            }
            else
            {
                Undo.BackgroundImage = uicon;
                Redo.BackgroundImage = ricon;
                Prvw.BackgroundImage = preview;
                Undo.Enabled = u;
                Redo.Enabled = r;
            }
        }

        void CopyDiskInfoData(DiskInfo source, int index)
        {
            if (source == null || disk == null || index < 0 || index >= disk.Length) return;
            disk[index].Locked = source.Locked;
            disk[index].Title = source.Title;
            disk[index].Side = source.Side;
            disk[index].Region = source.Region;
            disk[index].Year = source.Year;
            disk[index].Protection = source.Protection;
            disk[index].Notes = source.Notes;
            disk[index].Status = source.Status;
            disk[index].Favorite = source.Favorite;
        }

        void AddUndoState(Dictionary<int, DiskInfo> diskCompare)
        {
            List<int> idx = new List<int>();
            List<byte[]> entries = new List<byte[]>();
            
            foreach (var pair in diskCompare)
            {
                int index = pair.Key;
                DiskInfo oldState = pair.Value;
                DiskInfo currentState = disk[index];

                if (!DiskInfoEquals(oldState, currentState))
                {
                    idx.Add(index);
                    entries.Add(oldState.ToEntry());
                }
            }

            if (idx.Count > 0)
            {
                using (MemoryStream buffer = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(buffer))
                {
                    foreach (var entry in entries)
                        writer.Write(entry);

                    undoStack.Add(new UndoState(idx.ToArray(), Compress(buffer.ToArray())));
                    if (undoStack.Count > MAX_UNDO) undoStack.RemoveAt(0);
                }
                redoStack.Clear();
            }

            bool DiskInfoEquals(DiskInfo a, DiskInfo b)
            {
                return a.Locked == b.Locked &&
                       a.Title == b.Title &&
                       a.Side == b.Side &&
                       a.Region == b.Region &&
                       a.Year == b.Year &&
                       a.Protection == b.Protection &&
                       a.Notes == b.Notes &&
                       a.Status == b.Status &&
                       a.Favorite == b.Favorite &&
                       a.Index == b.Index;
            }
            SetButtonImages();
        }

        void UpdateEditedItem(bool addUndo = true)
        {
            int totalItems = dbView.SelectedItems.Count;
            int currentItem = 0;
            bool updateTitle = totalItems == 1;
            bool DetectProt = dProt.SelectedIndex == 0;
            // Cache control values first
            string newTitle = updateTitle ? editTitle.Text : null;
            string newNotes = updateTitle && changeNotes ? NotesChange : null;
            int newSide = (int)dSide.Value - 1;
            int newRegion = dRegn.SelectedIndex;
            int newYear = (int)dYear.Value;
            byte newProtection = !DetectProt ? (byte)(dProt.SelectedIndex - 1) : (byte)0;
            int newStatus = dStat.SelectedIndex;
            bool newFavorite = favToggle;
            bool progressUpdates = (DetectProt && totalItems > 4) || totalItems > 100;
            if (progressUpdates) InitProgressBar();
            List<int> updatedItems = new List<int>();
            List<ListViewItem> listViewItems = new List<ListViewItem>();
            Dictionary<int, DiskInfo> diskCompare = addUndo ? GetPreEditedDisks() : null;
            foreach (ListViewItem lv in dbView.SelectedItems)
            {
                int index = (int)lv.Tag;
                if (index < 0 || index >= disk.Length)
                    continue;

                if (!disk[index].Locked)
                {
                    if (updateTitle)
                        disk[index].Title = newTitle.Length <= DiskInfo.NAME_SIZE ? newTitle : disk[index].Title;

                    disk[index].Side = newSide;
                    disk[index].Region = newRegion;
                    disk[index].Year = newYear;
                    disk[index].Protection = !DetectProt ? newProtection : GetProtectionType(GetNIBData(index));

                    if (updateTitle && changeNotes) disk[index].Notes = newNotes;
                }
                if (progressUpdates && currentItem++ > 0) UpdateProgressBar(currentItem, totalItems);
                disk[index].Status = newStatus;
                disk[index].Favorite = newFavorite;

                updatedItems.Add(index);
                listViewItems.Add(lv);
            }
            if (addUndo && diskCompare != null) AddUndoState(diskCompare);
            dbProg.Visible = false;
            var indexes = updatedItems.ToArray();
            UpdateDBDirectory(indexes);
            dbView.BeginUpdate();
            UpdateDiskInfoAndListView(indexes, listViewItems.ToArray());
            dbView.EndUpdate();

            BrowseDB.ControlBox = true;
            editing = editPan.Visible = false;
            dbView.Enabled = dbSearch.Enabled = true;
            dbView.Focus();
        }

        Dictionary<int, DiskInfo> GetPreEditedDisks()
        {
            if (dbView.SelectedItems.Count == 0) return null;

            Dictionary<int, DiskInfo> diskCompare = new Dictionary<int, DiskInfo>();
            foreach (ListViewItem lv in dbView.SelectedItems)
            {
                int index = (int)lv.Tag;
                if (index >= 0 && index < disk.Length)
                {
                    diskCompare[index] = disk[index].Clone(); // Ensure Clone makes a deep copy
                }
            }
            return diskCompare;
        }

        void UpdateDiskInfoAndListView(int[] DiskIndex, ListViewItem[] lv)
        {
            if (lv == null || DiskIndex == null) return;
            try
            {
                bool showProg = DiskIndex.Length > 100;
                if (showProg) InitProgressBar();
                using (AccessDatabase getent = new AccessDatabase().ReadOnly(TEMP.dbPath))
                {
                    if (getent.Valid)
                    {
                        for (int i = 0; i < DiskIndex.Length; i++)
                        {
                            var temp = getent.GetDirectoryEntry(DiskIndex[i]);
                            if (temp != null) CopyDiskInfoData(temp, DiskIndex[i]);
                            if (showProg && i > 1) UpdateProgressBar(i, DiskIndex.Length);
                        }
                    }
                    else getent.Dispose();
                }
            }
            catch { }
            dbView.Invalidate();
            dbProg.Visible = false;
        }

        void RemoveEntries(int[] indexes, string promptTitle = null, string promptMessage = null)
        {
            if (indexes == null || indexes.Length == 0) return;

            bool ignoreLock = false;
            int locked = indexes.Count(i => disk[i].Locked);

            if (!string.IsNullOrEmpty(promptMessage))
            {
                DialogResult result = MessageBox.Show(promptMessage, promptTitle ?? "Confirm Removal",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.No) return;
            }

            if (locked > 0)
            {
                ignoreLock = PromptRemoveLockedItems(locked);
                if (!ignoreLock && locked == indexes.Length)
                    return; // All locked, user didn't confirm override
            }
            dbView.BeginUpdate();
            foreach (int i in indexes)
            {
                if (!disk[i].Locked || ignoreLock)
                {
                    disk[i].Marked = true;
                    disk[i].Locked = false;
                    dbRemoved.Add(i); // ++;
                    int itemIndex = dbView.Items.Cast<ListViewItem>().ToList()
                    .FindIndex(item => (int)item.Tag == i);

                    if (itemIndex != -1)
                        dbView.Items.RemoveAt(itemIndex);
                }
            }
            dbView.EndUpdate();
            BrowseDB.Text = $"Browse ReMaster Image Database ({dbView.Items.Count}/{disk?.Length - dbRemoved.Count})";
            UpdateDBDirectory(indexes);

            if (dbRemoved.Count >= 100) PromptRebuildDatabase();
        }

        void EditDiskField<T>(object sender, T newValue, Func<DiskInfo, T> getter, Action<DiskInfo, T> setter
            , bool checkLockedStatus = true)
            where T : struct, IEquatable<T>
        {
            int totalItems = dbView.SelectedItems.Count;

            if (totalItems > 0)
            {
                Dictionary<int, DiskInfo> compare = GetPreEditedDisks();
                List<int> selected = new List<int>();
                bool protV = typeof(T) == typeof(byte);
                bool showUpdates = false;
                int pval = protV ? Convert.ToInt32(newValue) : 0;
                if (totalItems > 100 || (protV && totalItems > 4))
                {
                    InitProgressBar();
                    showUpdates = true;
                }
                int current = 0;
                dbView.BeginUpdate();
                foreach (ListViewItem currentItem in dbView.SelectedItems)
                {
                    int index = (int)currentItem.Tag;
                    if (checkLockedStatus && disk[index].Locked) continue;
                    if (!getter(disk[index]).Equals(newValue) || !(protV && (byte)(disk[index].Protection) + 1 == pval))
                    {
                        if (!protV) setter(disk[index], newValue);
                        else disk[index].Protection = (byte)(pval == 0 ? GetProtectionType(GetNIBData(index)) : pval - 1);
                        selected.Add(index);
                    }
                    if (showUpdates && current++ > 1) UpdateProgressBar(current, totalItems);
                }
                UpdateDBDirectory(selected.ToArray());
                AddUndoState(compare);
                dbView.EndUpdate();
                dbProg.Visible = false;
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

        void ReadDB(bool UpdateList = true, bool showErrorMSG = true)
        {
            using (var dataBase = new AccessDatabase().ReadOnly(TEMP.dbPath))
            {
                if ((!dataBase.Valid || dbCorrupt) && showErrorMSG)
                {
                    dataBase.Close();
                    Invoke(new Action(() =>
                    {
                        using (Message_Center msg = new Message_Center(this))
                        {
                            DialogResult result = MessageBox.Show("Would you like to attempt recovering?",
                                "Database has been corrupted", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (result == DialogResult.Yes)
                            {
                                RecoverDatabase();
                                ReadDB(false, false);
                            }
                        }
                    }));
                }
                else
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
                        if (showErrorMSG)
                        {
                            dbCorrupt = true;
                            string title = "Error accessing database.";
                            string message = ex.Message;
                            MessageForYouSir(title, message);
                        }
                    }
                    if (disk.Length > 0 && !File.Exists(TEMP.dbTempDir))
                    {
                        using (MemoryStream buffer = new MemoryStream())
                        using (BinaryWriter write = new BinaryWriter(buffer))
                        {
                            foreach (DiskInfo d in disk) write.Write(d.ToEntry());
                            File.WriteAllBytes(TEMP.dbTempDir, buffer.ToArray());
                        }
                    }
                }
            }

            if (UpdateList) UpdatedbView();
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
            dbRemoved = disk.Where(d => d.Marked).Select(d => d.Index).ToList();
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
                if (!disk.Marked)
                {
                    ListViewItem item = new ListViewItem { Tag = disk.Index };
                    dbView.Items.Add(item);
                }
            }
            dbView.EndUpdate();
            BrowseDB.Text = $"Browse ReMaster Image Database ({dbView.Items.Count}/{disk?.Length - dbRemoved.Count})";
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
                if (dataBase.Valid && (entry >= 0 && entry <= disk.Length))
                {
                    var cmp = dataBase.GetImageData(disk[entry]);
                    if (Checksum.CRC32(cmp) == disk[entry].crc32)
                    {
                        byte[] dec = Decompress(cmp);
                        return (Match(Checksum.MD5(dec), disk[entry].rawHash)) ? dec : null;
                    }
                }
                else dataBase.Dispose();
                return null;
            }
        }

        bool Remove_Items_From_Database() //, bool IgnoreLockStatus = false)
        {
            string tempPath = TEMP.dbTemp;
            if (File.Exists(tempPath)) File.Delete(tempPath);
            byte[] newDir;
            using (var current = new AccessDatabase().ReadOnly(TEMP.dbPath))
            using (var temp = new AccessDatabase().Create(tempPath))
            {
                ClearStacks();
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
                        if (!ent.Marked)
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
                    newDir = buffer.ToArray();
                    temp.WriteDirectory(newDir);
                }
            }
            if (VerifyDBintegrity(tempPath) > 0)
            {
                File.Delete(tempPath);
                return false;
            }
            else
            {
                try
                {
                    File.Replace(tempPath, TEMP.dbPath, destinationBackupFileName: null);
                    if (newDir != null) File.WriteAllBytes(TEMP.dbTempDir, newDir);
                    return true;
                }
                catch { return false; }
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
                    dbCorrupt = failed > 0;
                    return failed;
                }
                else
                {
                    verify.Dispose();
                    return 65536;
                }
            }
        }

        void ClearStacks()
        {
            undoStack.Clear();
            redoStack.Clear();
            SetButtonImages();
        }

        void BuildDB(string rpath = null, string[] file = null, bool getNameFromDirectory = false)
        {
            if (rpath == null && file == null) return;
            Dictionary<string, int> fileExt = new Dictionary<string, int>
            {
                { ".nib", 0 } ,{ ".nbz", 1 }, { ".g64", 2 }
            };

            int detect = ProtDetectMethod.SelectedIndex;
            var ttl = Text;
            using (var dataBase = !File.Exists(TEMP.dbPath)
                ? new AccessDatabase().Create(TEMP.dbPath)
                : new AccessDatabase().ReadWrite(TEMP.dbPath))
            {
                if (!dataBase.Valid) dataBase.Dispose();
                else
                {
                    ClearStacks();
                    //string[] file = Directory.EnumerateFiles(rpath, "*.nib", SearchOption.AllDirectories)
                    if (rpath != null)
                    {
                        file = Directory.EnumerateFiles(rpath, "*.nib", SearchOption.AllDirectories)
                            .Concat(Directory.EnumerateFiles(rpath, "*.nbz", SearchOption.AllDirectories))
                            .ToArray();
                    }
                    var processed = 0;
                    using (MemoryStream buffer = new MemoryStream())
                    using (BinaryWriter write = new BinaryWriter(buffer))
                    {
                        if (dataBase.Entries > 0)
                        {
                            dataBase.Seek(dataBase.Offset);
                            var old = dataBase.Read(dataBase.Entries * DiskInfo.ENTRY_SIZE);
                            write.Write(old);
                        }
                        if (buffer.Length > 0) WriteTempDir(buffer.ToArray(), true);
                        if (dataBase.Entries < 65535)
                        {
                            foreach (var f in file)
                            {
                                try
                                {
                                    byte protection = 0;
                                    var extension = Path.GetExtension(f).ToLower();
                                    var dec = extension == ".nib" ? File.ReadAllBytes(f) : LZdecompress(File.ReadAllBytes(f));
                                    string lf = f.Replace(extension, ".log");
                                    
                                    switch (detect)
                                    {
                                        case 0: protection = GetProtectionType(dec); break;
                                        case 1: protection = Get_ProtectionFromFileName(Path.GetFileNameWithoutExtension(f)); break;
                                    }
                                    var title = getNameFromDirectory
                                        ? GetTitleFromDirectory(dec)
                                        : Get_Name(Path.GetFileNameWithoutExtension(f.Replace("_", " ")));
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
                                        DecompressedLength = dec.Length,
                                        CompressedLength = cmp.Length,
                                        PreviewLength = (short)preview.Length,
                                        crc16 = Checksum.CRC16(preview),
                                        Timestamp = DateTime.Now,
                                        Year = Get_Year(f),
                                        Title = title,
                                        Region = Get_Region(f, pv),
                                        Side = (byte)Get_DiskSide(f),
                                        Extension = fileExt[extension],
                                        Protection = protection,
                                    };
                                    if (ParseLog.Checked && File.Exists(lf) && new FileInfo(lf).Length < 1024 * 1024)
                                    {
                                        (bool errors, string tracks) = ParseLogFile(File.ReadAllLines(lf));
                                        info.Status = errors ? 2 : 0;
                                        info.Notes = tracks;
                                    }
                                    var newent = info.ToEntry();
                                    WriteTempDir(newent);
                                    write.Write(newent);
                                    dataBase.Offset += info.CompressedLength + info.PreviewLength;
                                    Invoke(new Action(() =>
                                    {
                                        if (processed > 1) UpdateProgressBar(processed, file.Length);
                                        BuildStatus.Text = $"Building Database ({processed++}/{file.Length}) Total Entries {dataBase.Entries++}";
                                    }));
                                }
                                catch { }
                                dataBase.UpdateHeader();
                                if (cancel || dataBase.Entries == 65535) break;
                            }
                            cancel = false;
                            dataBase.WriteDirectory(buffer.ToArray());
                        }
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

                    byte Get_ProtectionFromFileName(string f)
                    {
                        int prt = -1;
                        if (f.Contains("vmax2".ToLower())) prt = 12;
                        if (f.Contains("vmax3".ToLower()) || f.Contains("vmax4".ToLower())) prt = 13;
                        if (f.Contains("(rl".ToLower())) prt = 9;
                        if (f.Contains("[ea_".ToLower())) prt = 6;
                        if (f.Contains("(cyan".ToLower())) prt = 1;
                        if (f.Contains("(radwar".ToLower())) prt = 7;
                        if (f.Contains("[rainbow_arts".ToLower()) || f.Contains("[magic_bytes".ToLower())) prt = 8;
                        if (f.Contains("(gma".ToLower())) prt = 4;

                        if (prt < 0)
                        {
                            var p = Path.GetDirectoryName(f).ToLower();
                            if (p.Contains("vmax")) prt = 11;
                            if (p.Contains("vorpal")) prt = 14;
                            if (p.Contains("rapidlok")) prt = 9;
                            if (p.Contains("cyan")) prt = 1;
                            if (p.Contains("securispeed")) prt = 10;
                            if (p.Contains("rainbow")) prt = 8;
                            if (p.Contains("radwar")) prt = 7;
                            if (p.Contains("fat")) prt = 3;
                            if (p.Contains("microprose")) prt = 5;
                        }
                        return (byte)(prt < 0 ? 0 : prt);
                    }
                }
            }

            void WriteTempDir(byte[] entry, bool overwrite = false)
            {
                if (overwrite && File.Exists(TEMP.dbTempDir)) File.Delete(TEMP.dbTempDir);
                using (FileStream tdir = new FileStream(TEMP.dbTempDir, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    tdir.Seek(0, SeekOrigin.End); // Move to the end for appending
                    tdir.Write(entry, 0, entry.Length);
                    tdir.Flush();
                }
            }
        }

        byte GetProtectionType(byte[] nibFile)
        {
            if (nibFile == null) return 0;
            try
            {
                int tracks = (nibFile.Length - 256) >> 13;
                bool ht = tracks > 42;
                var trkData = new byte[tracks][];
                int[] fmt = new int[tracks];
                int[] trkID = new int[tracks];
                Job = new Thread[tracks];
                for (int i = 0; i < tracks; i++)
                {
                    try
                    {
                        trkData[i] = new byte[8192];
                        Buffer.BlockCopy(nibFile, 256 + (i * 8192), trkData[i], 0, 8192);
                    }
                    catch { }
                }
                for (int i = 0; i < tracks; i++)
                {
                    int x = i;
                    Task_Limit.WaitOne();
                    Job[i] = new Thread(new ThreadStart(() =>
                    {
                        try
                        {
                            fmt[x] = Get_Data_Fmt2(trkData[x], x, false);
                            if (fmt[x] == 1 || fmt[x] == 10) trkID[x] = CBM_Track_Info(trkData[x], false, x, fmt[x] == 1).Item11;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Get Format failed during nib-parse: {ex.Message}");
                        }
                        Task_Limit.Release();
                    }));
                    Job[i].Start();
                    if (ht) fmt[i++] = secF.Length - 1;
                }
                foreach (Thread t in Job) t?.Join();

                CheckFormats();
                int protection = 0;
                int maxtrack = ht ? 67 : 34;
                bool vorpal = fmt.Select((val, idx) => new { val, idx }).Count(x => x.val == 5 && x.idx <= maxtrack) > 3;
                bool rapidlok = fmt.Select((val, idx) => new { val, idx }).Count(x => x.val == 6 && x.idx <= maxtrack) > 5;
                bool microprose = fmt.Select((val, idx) => new { val, idx }).Count(x => x.val == 10 && x.idx <= maxtrack) > 3;
                bool custom = (fmt.Select((val, idx) => new { val, idx })
                    .Count(x => x.val == 0 && x.idx <= maxtrack) - (ht ? maxtrack >> 1 : 0)) > 5;
                bool vmaxCBM = false;
                bool vmaxV2 = false;
                bool vmaxV3 = false;
                bool fatTracks = false;
                bool radwar = false;
                bool deepcheck = !vorpal && !rapidlok && !microprose;
                if (deepcheck)
                {
                    bool halftracks = tracks > 42;
                    for (int i = 0; i < tracks; i++)
                    {
                        if (fmt[i] > 0 && fmt[i] < secF.Length - 1) // && tlen[i] >> 3 > 6000)
                        {
                            int halfTrack = ht ? 2 : 1;
                            int track = ht ? (i / 2) + 1 : i + 1;
                            if (fmt[i] >= 0 && fmt[i] < secF.Length - 1)
                            {
                                vmaxV2 |= fmt[i] == 2;
                                vmaxV3 |= fmt[i] == 3;
                            }
                            if (fmt[i] == 1)
                            {
                                halfTrack = ht ? 2 : 1;
                                try
                                {
                                    if (track < 37 && ((track != trkID[i] && track == trkID[i] + 1)
                                       || (i + halfTrack < trkID.Length && track == trkID[i + halfTrack]))) fatTracks = true;
                                }
                                catch { }
                                if (track == 18)
                                {
                                    int[] cbmRange = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
                                    if (cbmRange.All(x => !fmt.Any(y => y == x))) radwar = Radwar(trkData[i]).Item1;
                                }
                            }
                        }
                    }
                    var dirTrack = ht ? 34 : 17;
                    var cyanTrack = ht ? 8 : 4;
                    var vmloaderTrack = ht ? 38 : 19;
                    if (vmaxV2) protection = 12;
                    if (vmaxV3) protection = 13;
                    if (!(vmaxV2 || vmaxV3) && fmt[dirTrack] == 1)
                    {
                        if (fmt[vmloaderTrack] == 4) vmaxCBM = true;
                        else
                        {
                            string s0 = Encoding.ASCII.GetString(Decode_CBM_Sector(trkData[dirTrack], 0, true).data).ToLower();
                            string s1 = Encoding.ASCII.GetString(Decode_CBM_Sector(trkData[dirTrack], 1, true).data).ToLower();
                            if ((s0 != null && s0.Contains("v-max")) || (s1 != null && s1.Contains("v-max"))) vmaxCBM = true;
                        }
                        protection = vmaxCBM ? 11 : 0;              // V-Max CBM / None
                    }
                    if (protection == 0)
                    {
                        if (fatTracks) protection = 3;              // Fat Tracks
                        if (fmt.Any(x => x == 11)) protection = 10; // Securispeed
                        if (fmt.Any(x => x == 12)) protection = 4;  // GMA
                        if (fmt.Any(x => x == 9)) protection = 8;   // Rainbow Arts / Magic Bytes
                        if (fmt[cyanTrack] == 1 && Check_Cyan_Loader(trkData[cyanTrack])) protection = 1;    // Cyan
                        if (radwar) protection = 7;                 // Radwar
                    }
                }

                if (!vmaxCBM && !vmaxV2 && !vmaxV3)
                {
                    if (rapidlok) protection = 9;
                    if (vorpal) protection = 14;
                    if (microprose) protection = 5;
                    if (custom) protection = 2;
                    if (fmt.Any(x => x == 8)) protection = 6;       // EA
                }

                return (byte)protection;
                bool Check_Cyan_Loader(byte[] trackData)
                {
                    byte[] cmp;
                    cmp = Decode_CBM_Sector(trackData, 5, true).data;
                    if (cmp == null) return false;
                    int match = 0;
                    for (int i = 0; i < cmp.Length; i++) if (cmp[i] == cldr_id[i]) match++;
                    return match > 240;
                }

                void CheckFormats()
                {
                    int m = Find_Most_Frequent_Format(NDS.cbm);
                    int[] skip = new int[] { 0, 1, 4, 7, 8, 9, 11, secF.Length - 1 };
                    if (!(skip.Any(x => x == m)))
                    {
                        HashSet<int> ignore = new HashSet<int>();
                        if (m == 2 || m == 3) ignore.UnionWith(new int[] { 0, 1, 4 });
                        if (m == 5 || m == 10) ignore.UnionWith(new int[] { 0, 1, secF.Length - 1 });
                        if (m == 6) ignore.UnionWith(new int[] { 0, 1, 7, secF.Length - 1 });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get Format failed : {ex.Message}");
                return 0;
            }
        }

        string GetTitleFromDirectory(byte[] nibFile)
        {
            if (nibFile == null) return string.Empty;
            string ret = string.Empty;
            try
            {
                int tracks = (nibFile.Length - 256) >> 13;
                bool ht = tracks > 42;
                var dir = ht ? 34 : 17;
                byte[] dirtrk = new byte[8192];
                Buffer.BlockCopy(nibFile, 256 + (dir * 8192), dirtrk, 0, 8192);
                int fmt = Get_Data_Fmt2(dirtrk, dir, false);
                if (fmt == 1)
                {
                    const int TitleOffset = 144;
                    const int TitleLength = 23;
                    var cmp = Decode_CBM_Sector(dirtrk, 0, true).data;
                    if (cmp != null)
                    {
                        ret = $"0 \"";
                        for (int i = 0; i < TitleLength; i++)
                        {
                            if (cmp[TitleOffset + i] != 0x00)
                            {
                                if (i != 16) ret += Encoding.ASCII.GetString(cmp, 144 + i, 1).Replace('?', ' ');
                                else ret += "\"";
                            }
                        }
                        return ret;
                    }
                }
                ret = $"nib_read_{DateTime.Now:yyyyMMdd_HHmmss}";
                //ret = $"nib_read_{DateTime.Now}";
            }
            catch { }
            return ret;
        }

        void RecoverDatabase()
        {
            if (File.Exists(TEMP.dbPath) && File.Exists(TEMP.dbTempDir))
            {
                var tempPath = TEMP.dbTemp;
                using (FileStream tempdir = new FileStream(TEMP.dbTempDir, FileMode.Open, FileAccess.Read))
                using (AccessDatabase recover = new AccessDatabase().ReadWrite(TEMP.dbPath))
                {
                    int dbEnt = recover.Entries;
                    long dbOffset = recover.Offset;
                    long length = new System.IO.FileInfo(TEMP.dbTempDir).Length;
                    int entries = (length % DiskInfo.ENTRY_SIZE == 0) ? (int)(length / DiskInfo.ENTRY_SIZE) : -1;
                    if (entries > 0)
                    {
                        List<DiskInfo> entList = new List<DiskInfo>();
                        for (int i = 0; i < entries; i++)
                        {
                            tempdir.Seek(i * DiskInfo.ENTRY_SIZE, SeekOrigin.Begin);
                            byte[] ent = new byte[DiskInfo.ENTRY_SIZE];
                            tempdir.Read(ent, 0, ent.Length);
                            if (ent != null) entList.Add(DiskInfo.FromEntry(ent));
                        }
                        using (AccessDatabase temp = new AccessDatabase().Create(tempPath))
                        {
                            using (MemoryStream buffer = new MemoryStream())
                            using (BinaryWriter write = new BinaryWriter(buffer))
                            {
                                foreach (DiskInfo ent in entList)
                                {
                                    var data = recover.GetImageData(ent);
                                    var preview = recover.GetPreviewData(ent);
                                    if (data != null && preview != null)
                                    {
                                        if (Checksum.CRC32(data) == ent.crc32 && Checksum.CRC16(preview) == ent.crc16)
                                        {
                                            ent.Offset = temp.Offset;
                                            write.Write(ent.ToEntry());
                                            temp.Seek(temp.Offset);
                                            temp.Write(ArrayConcat(data, preview));
                                            temp.Offset += data.Length + preview.Length;
                                            temp.Entries++;
                                        }
                                        else Text = $"Failed Entry {entries}";
                                    }
                                    temp.UpdateHeader();
                                    temp.WriteDirectory(buffer.ToArray());
                                }
                            }
                        }
                    }
                }
                if (VerifyDBintegrity(tempPath) > 0) File.Delete(tempPath);
                else
                {
                    try
                    {
                        File.Replace(tempPath, TEMP.dbPath, destinationBackupFileName: null);
                    }
                    catch { }
                }
            }
        }

        void PromptRebuildDatabase() //int[] dupes, bool ignoreLock)
        {
            using (Message_Center del = new Message_Center(this))
            {
                DialogResult rem = MessageBox.Show("Would you like to permenantly delete entires\nto reclaim space?"
                    , $"({dbRemoved.Count} Items can be purged)", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (rem == DialogResult.Yes)
                {
                    List<int> remove = new List<int>();
                    foreach (var ent in disk) if (ent.Marked) remove.Add(ent.Index);
                    dbView.Enabled = dbSearch.Enabled = false;
                    ShowProgress = disk.Length > 250;
                    if (ShowProgress) InitProgressBar();
                    //dbProg.BringToFront();
                    //dbProg.Value = 0;
                    //dbProg.Maximum = 100 * 100;
                    //dbProg.Value = dbProg.Maximum / 100;
                    Task.Run(delegate
                    {
                        if (!Remove_Items_From_Database()) //, ignoreLock))
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
                            if (!dbCorrupt)
                            {
                                var sortedList = disk.OrderBy(d => d.Title).ToList();
                                Invoke(new Action(() =>
                                {
                                    UpdateList(RegionFilter(sortedList), true);
                                    SetDBContextItems();
                                }));
                            }
                            else RecoverDatabase();
                        }
                        Invoke(new Action(() =>
                        {
                            dbProg.Visible = false;
                            dbView.Enabled = dbSearch.Enabled = true;
                            ShowProgress = false;
                        }));
                    });
                }
                else ReadDB();
            }
        }

        bool PromptRemoveLockedItems(int locked)
        {
            var title = locked == 1 ? "Image is locked!" : $"({locked}) Images are locked!";
            using (Message_Center ignore = new Message_Center(this))
            {
                DialogResult res = MessageBox.Show("Ignore locked status and remove anyway?",
                    title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes) return true;
            }
            return false;
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
                dbView.Columns[5].Text = $"Region {getRegion[regionSort]}";
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

        void ToggleDirPreview()
        {
            PVbox.Visible = !PVbox.Visible;
            if (PVbox.Visible) BrowseDB.Width += PVbox.Width;
            else BrowseDB.Width -= PVbox.Width;
        }

        string SanitizeRichText(RichTextBox rtb)
        {
            var sb = new StringBuilder();
            foreach (char c in rtb.Text)
            {
                if (char.IsLetterOrDigit(c) || @" ,.?<>-=!@#$%^&*()':;+`~\".Contains(c))
                    sb.Append(c);
                else if (c == '\n' || c == '\r')
                    sb.Append('\n'); // Normalize all line breaks to \n
                                     // else skip the character
            }
            return sb.ToString();
        }

        int[] CheckForDuplicates(bool show = false, bool select = false)
        {
            List<int> dupeIdx = new List<int>();
            if (disk.Length > 1)
            {
                int totalItems = disk.Length;
                bool showUpdates = totalItems > 200;
                if (showUpdates) InitProgressBar();
                int currentItem = 0;
                Stopwatch sw = Stopwatch.StartNew();
                Dictionary<string, int> hashMap = new Dictionary<string, int>(); // md5 hash -> index of first occurrence
                HashSet<string> seenHashes = new HashSet<string>(); // For quick lookup
                HashSet<int> indicesToShow = new HashSet<int>(); // Final list of all indexes to display
                foreach (DiskInfo d in disk)
                {
                    if (!d.Marked)
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
                    if (showUpdates && currentItem++ > 1) UpdateProgressBar(currentItem, totalItems);
                }
                dbProg.Visible = false;
                List<DiskInfo> list = new List<DiskInfo>();
                foreach (int i in indicesToShow) list.Add(disk[i]);
                if (show && dupeIdx.Count > 0)
                {
                    var sortedList = sortAscending
                        ? list.OrderBy(d => Encoding.ASCII.GetString(d.rawHash)).ThenBy(d => d.Title).ToList()
                        : list.OrderByDescending(d => Encoding.ASCII.GetString(d.rawHash)).ThenByDescending(d => d.Title).ToList();
                    UpdateList(sortedList);
                    if (select) foreach (ListViewItem l in dbView.Items) if (dupeIdx.Contains((int)l.Tag)) l.Selected = true;
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

        private void EditPan_MouseMove(object sender, MouseEventArgs e)
        {
            Control hovered = editPan.GetChildAtPoint(e.Location);
            if (hovered != null && !hovered.Enabled)
            {
                string tip;
                bool isMulti = dbView.SelectedItems.Count > 1;
                bool anyLocked = AnySelectedItemsLocked();

                // Only these fields are disabled *purely* due to multi-edit mode
                bool multiEditOnlyField = hovered == editTitle || hovered == dAddNotes;
                if (isMulti)
                {
                    if (anyLocked && !multiEditOnlyField) tip = "Multi-edit mode, field disabled\ndue to locked image(s) in selection";
                    else tip = "Multi-edit mode, field cannot be changed";
                }
                else tip = "Image locked, field cannot be changed";
                dbTooltip.Show(tip, editPan, e.Location.X + 32, e.Location.Y);
            }
            else dbTooltip.Hide(editPan);
        }

        private void DbView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            DiskInfo diskInfo = disk[(int)e.Item.Tag];
            int index = e.ItemIndex;
            int transparency = 80;
            bool isSelected = e.Item.Selected;
            bool isHovered = (dbLastHoveredItem == index);
            bool editing = this.editing; // Use your existing flag
            string search = dbSearch.Text.Trim();
            bool hasSearch = !string.IsNullOrEmpty(search);
            DoubleBufferedListView view = sender as DoubleBufferedListView;
            string[] values =
            {
                view == dbView ? string.Empty : $"{e.Item.Text}",
                diskInfo.Title,
                string.Empty,
                $"{diskInfo.Side + 1}",
                diskInfo.Year > 1970 ? $"{diskInfo.Year}" : string.Empty,
                $"{getRegion[diskInfo.Region]}",
                $"{DiskInfo.Prot[diskInfo.Protection]}",
                $"{diskInfo.Timestamp}",
            };
            bool chked = e.Item.Checked;
            // Compute column rectangles
            Rectangle[] cols = new Rectangle[view.vColumns.Length];
            int x = e.Bounds.Left;
            for (int i = 0; i < view.vColumns.Length; i++)
            {
                cols[i] = new Rectangle(x, e.Bounds.Top, view.vColumns[i], e.Bounds.Height);
                x += view.vColumns[i];
            }

            // Background color
            Color backColor = isSelected
                ? (index % 2 == 0 ? Color.FromArgb(105, 123, 223) : Color.FromArgb(75, 96, 216))
                : (index % 2 == 0 ? Color.FromArgb(230, 230, 230) : Color.FromArgb(200, 200, 200));
            if (editing) backColor = ApplyMultiplyModifier(backColor, 1.1f);

            using (SolidBrush backBrush = new SolidBrush(backColor))
                e.Graphics.FillRectangle(backBrush, e.Bounds);

            // Text settings
            Font font = dbView.Font;
            Color normalColor = isSelected ? SystemColors.HighlightText : Color.Black;
            Color hoverColor = isSelected ? Color.Yellow : Color.Blue;
            Color textColor;
            //Color textColor = editing ? Color.Gray : isHovered ? hoverColor : normalColor;
            if (chked) textColor = isSelected ? isHovered ? Color.LightGreen : Color.Orange : isHovered ? Color.Red : Color.BlueViolet;
            else textColor = editing ? Color.Gray : isHovered ? hoverColor : normalColor;

            Image icon;

            // Column 0: Lock Icon
            if (diskInfo.Locked)
            {
                icon = editing ? GetTransparentImage(icons.Images["lock"], transparency) : icons.Images["lock"];
                Rectangle imgRect = new Rectangle(
                    cols[0].X + (cols[0].Width - 16) / 2,
                    cols[0].Y + (cols[0].Height - 16) / 2,
                    16, 16);
                e.Graphics.DrawImage(icon, imgRect);
            }

            // Column 1: Title + Highlight Search
            if (hasSearch && values[1].IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string lower = values[1].ToLower();
                string lowerSearch = search.ToLower();
                int matchIndex = lower.IndexOf(lowerSearch);
                if (matchIndex >= 0)
                {
                    string before = values[1].Substring(0, matchIndex);
                    string match = values[1].Substring(matchIndex, search.Length);
                    string after = values[1].Substring(matchIndex + search.Length);

                    float y = cols[1].Y + (cols[1].Height - TextRenderer.MeasureText("A", font).Height) / 2 + 7;
                    float drawX = cols[1].X;

                    Size beforeSize = TextRenderer.MeasureText(e.Graphics, before, font);
                    Size matchSize = TextRenderer.MeasureText(e.Graphics, match, font);

                    drawX += beforeSize.Width - (matchIndex == 0 ? 0 : 6);

                    Rectangle matchRect = new Rectangle((int)drawX + 2, cols[1].Y, matchSize.Width - 6, cols[1].Height);
                    using (SolidBrush hi = new SolidBrush(Color.FromArgb(188, 128, 0)))
                        e.Graphics.FillRectangle(hi, matchRect);

                    drawX += matchSize.Width - 6;
                }
            }

            for (int i = 0; i < cols.Length; i++)
            {
                HorizontalAlignment align = view.Columns[i].TextAlign;
                TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis;
                switch (align)
                {
                    case HorizontalAlignment.Center: flags |= TextFormatFlags.HorizontalCenter; break;
                    case HorizontalAlignment.Right: flags |= TextFormatFlags.Right; break;
                    default: flags |= TextFormatFlags.Left; break;
                }
                TextRenderer.DrawText(e.Graphics, values[i], font, cols[i], textColor, flags);
            }

            icon = null;
            var lv = (ListView)sender;
            Point cursorPos = lv.PointToClient(Cursor.Position);
            bool Hovered = cols[2].Contains(cursorPos);
            if (sender == dbView)
            {
                if (!diskInfo.Locked) icon = Hovered ? icons.Images["editH"] : icons.Images["edit"];
                else icon = icons.Images["editG"];
            }
            if (editing) icon = GetTransparentImage(icon, transparency);
            if (sender == dbRemv)
            {
                if (disk[(int)e.Item.Tag].Marked) icon = Hovered ? icons.Images["recover"] : icons.Images["recoverH"];
            }
            int imgX = cols[2].Left;
            int imgY = cols[2].Y + 2;
            e.Graphics.DrawImage(icon, new Rectangle(imgX, imgY, 16, 16));

            // Icons on title: Star, Notes, Status
            if (diskInfo.Favorite)
            {
                icon = editing ? GetTransparentImage(icons.Images["star"], transparency) : icons.Images["star"];
                Rectangle iconRect = new Rectangle(cols[1].Right - 40, cols[1].Y + 2, 16, 16);
                e.Graphics.DrawImage(icon, iconRect);
            }

            if (!string.IsNullOrEmpty(diskInfo.Notes))
            {
                Rectangle iconRect = new Rectangle(cols[1].Right - 20, cols[1].Y + 2, 20, 20);
                icon = iconRect.Contains(cursorPos) ? icons.Images["notesH"] : icons.Images["notes"];
                if (editing) icon = GetTransparentImage(icon, transparency);
                e.Graphics.DrawImage(icon, iconRect);
            }

            if (diskInfo.Status > 0)
            {
                int size = diskInfo.Notes?.Length > 0 ? 10 : 16;
                string[] statusIcons = { "", "ok", "wwe", "bad" };
                string iconKey = statusIcons[diskInfo.Status];
                icon = editing ? GetTransparentImage(icons.Images[iconKey], transparency) : icons.Images[iconKey];
                e.Graphics.DrawImage(icon, new Rectangle(cols[1].Right - 20, cols[1].Y + 2, size, size));
            }
        }

        private Image GetTransparentImage(Image image, int alpha)
        {
            Bitmap output = new Bitmap(image);

            for (int x = 0; x < output.Width; x++)
            {
                for (int y = 0; y < output.Height; y++)
                {
                    Color color = output.GetPixel(x, y);
                    output.SetPixel(x, y, Color.FromArgb(color.A == 0 ? 0 : alpha, color.R, color.G, color.B));
                }
            }

            return output;
        }

        private void DbView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = dbView.HitTest(e.Location);
            if (e.Button == MouseButtons.Left && hit.Item != null)
            {
                // Determine which virtual column was clicked
                int subItemIndex = -1;
                int left = hit.Item.Bounds.Left;

                for (int i = 0; i < dbView.vColumns.Length; i++)
                {
                    var colRect = new Rectangle(left, hit.Item.Bounds.Top, dbView.vColumns[i], hit.Item.Bounds.Height);
                    if (colRect.Contains(e.Location))
                    {
                        subItemIndex = i;
                        break;
                    }
                    left += dbView.vColumns[i];
                }

                // Only proceed if the double-click wasn't on the "edit" (column 2) area
                if (subItemIndex != 2 && hit.Item?.Tag != null)
                {
                    Import_Image_From_Database((int)hit.Item.Tag);
                }
            }
        }

        private void DbView_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender == dbView)
            {
                ListViewHitTestInfo info = dbView.HitTest(e.Location);
                try
                {
                    int index = info.Item?.Index ?? -1;
                    if (index >= 0 && index < dbView.Items.Count)
                    {
                        var item = dbView.Items[index];
                        var idx = (int)item.Tag;
                        var x = e.Location.X + 32;
                        var y = e.Location.Y;
                        var stats = disk[idx].Status > 0 ? $"{disk[idx].imgStat[disk[idx].Status]}" : string.Empty;

                        // Instead of subItemIndex from SubItems, we calculate it from mouse X using vColumns
                        int subItemIndex = -1;
                        int left = item.Bounds.Left;

                        for (int i = 0; i < dbView.vColumns.Length; i++)
                        {
                            var colRect = new Rectangle(left, item.Bounds.Top, dbView.vColumns[i], item.Bounds.Height);
                            if (colRect.Contains(e.Location))
                            {
                                subItemIndex = i;
                                break;
                            }
                            left += dbView.vColumns[i];
                        }

                        // Proceed with logic
                        if (subItemIndex == 1) // Title column (Notes, Status, Favorite icons)
                        {
                            Rectangle subItemBounds = dbView.GetSubItemBounds(item, subItemIndex);

                            Rectangle noteBounds = new Rectangle(subItemBounds.Right - note,
                                subItemBounds.Top + (subItemBounds.Height - 20) / 2, 20, 20);
                            Rectangle statBounds = new Rectangle(subItemBounds.Right - stat,
                                subItemBounds.Top + (subItemBounds.Height - 20) / 2, 20, 20);
                            Rectangle favBounds = new Rectangle(subItemBounds.Right - fav,
                                subItemBounds.Top + (subItemBounds.Height - 20) / 2, 20, 20);

                            bool isCurrentlyHovered = noteBounds.Contains(e.X, e.Y);
                            if (isCurrentlyHovered != lastIconHoverState)
                            {
                                lastIconHoverState = isCurrentlyHovered;
                                dbView.Invalidate();
                            }

                            string tip = "";
                            if (!string.IsNullOrEmpty(stats)) tip += stats + "\n";
                            if (!string.IsNullOrEmpty(disk[idx].Notes)) tip += disk[idx].Notes;

                            if (tip != string.Empty && (noteBounds.Contains(e.X, e.Y) || statBounds.Contains(e.X, e.Y)))
                                dbTooltip.Show(tip, dbView, x, y);
                            else if (favBounds.Contains(e.X, e.Y) && disk[idx].Favorite) dbTooltip.Show("Favorite", dbView, x, y);
                            else dbTooltip.Hide(dbView);
                        }
                        else if (subItemIndex == 0 && disk[idx].Locked) dbTooltip.Show("Locked", dbView, x, y);
                        else if (subItemIndex == 2) dbTooltip.Show(!disk[idx].Locked ? "Edit" : "Unlock to edit", dbView, x, y);
                        else dbTooltip.Hide(dbView);

                        // Handle hover changes
                        if (index != dbLastHoveredItem && DateTime.Now - lastHoverUpdate > hoverDelay)
                        {
                            dbLastHoveredItem = index;
                            dbView.Invalidate();
                            dbRemv.Invalidate();
                            lastHoverUpdate = DateTime.Now;
                            UpdatePreview(idx);
                        }
                    }
                }
                catch { }
            }

            if (sender == dbRemv)
            {
                var view = (DoubleBufferedListView)sender;
                ListViewHitTestInfo info = view.HitTest(e.Location);
                try
                {
                    int index = info.Item?.Index ?? -1;
                    if (index >= 0 && index < view.Items.Count)
                    {
                        var idx = (int)info.Item.Tag;
                        var x = e.Location.X + 32;
                        var y = e.Location.Y;

                        // Build the virtual column rectangles for this item
                        Rectangle itemBounds = info.Item.GetBounds(ItemBoundsPortion.Entire);
                        int colX = itemBounds.Left;
                        Rectangle vCol2 = Rectangle.Empty;

                        for (int i = 0; i < view.vColumns.Length; i++)
                        {
                            Rectangle colRect = new Rectangle(colX, itemBounds.Top, view.vColumns[i], itemBounds.Height);
                            if (i == 2) // Edit/Recover icon column
                            {
                                vCol2 = colRect;
                                break;
                            }
                            colX += view.vColumns[i];
                        }

                        if (vCol2.Contains(e.Location))
                            dbTooltip.Show($"Recover {disk[idx].Title}", dbRemv, x, y);
                        else
                            dbTooltip.Hide(dbRemv);

                        if (index != dbLastHoveredItem && DateTime.Now - lastHoverUpdate > hoverDelay)
                        {
                            dbLastHoveredItem = index;
                            dbRemv.Invalidate();
                            lastHoverUpdate = DateTime.Now;
                        }
                    }
                }
                catch { }
            }
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
                if (holdtime.TotalMilliseconds < 350) ToggleDirPreview();
            }
            if (e.KeyData == Keys.Escape)
            {
                if (editing)
                {
                    editing = false;
                    dbView.Enabled = dbSearch.Enabled = true;
                    editPan.Visible = false; // SendToBack();
                    dbView.Focus();
                    BrowseDB.ControlBox = true;
                }
                else BrowseDB.Close();
            }

            if (e.KeyData == Keys.Enter && !editing)
            {
                if (dbView.SelectedItems.Count > 0)
                {
                    var idx = (int)dbView.SelectedItems[0].Tag;
                    if (idx >= 0) Import_Image_From_Database(idx);
                }
            }
            if (e.KeyData == Keys.Enter && editing)
            {
                UpdateEditedItem();
            }
            var keys = new Keys[] { Keys.Up, Keys.Down, Keys.PageUp, Keys.PageDown };

            if (keys.Contains(e.KeyData))
            {
                if (dbView.SelectedItems.Count > 0)
                {
                    var selectedItem = dbView.SelectedItems[0];
                    if (selectedItem?.Tag != null) UpdatePreview((int)selectedItem.Tag);
                }
            }

        }

        private void Dnotes_TextChanged(object sender, EventArgs e)
        {
            if (!busy)
            {
                string f = SanitizeRichText(dNotes);
                byte[] t = Encoding.ASCII.GetBytes(f);
                if (t.Length > 128)
                {
                    busy = true;
                    dNotes.Text = lastNotes;
                    dNotes.SelectionStart = dNotes.TextLength;
                    dNotes.SelectionLength = 0;
                    dNotes.ScrollToCaret();
                    busy = false;
                    f = SanitizeRichText(dNotes);
                    t = Encoding.ASCII.GetBytes(f);
                }
                else lastNotes = f;
                EditNotes.Text = $"Edit Notes ({t.Length}/{DiskInfo.NOTES_SIZE})";
            }
        }

        private void DbView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column != RegionColumn) sortAscending = lastSortedColumn == e.Column ? !sortAscending : sortAscending;
            lastSortedColumn = e.Column;
            string column = dbView.Columns[e.Column].Text.ToLower().ToString();
            lastColumnName = column;
            var searchText = dbSearch.Text.ToLower();
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
            if (column != "  ")
            {
                sort?.Abort();
                sort = new Thread(() => SortList(column, searchText));
                sort.Start();
            }
        }

        private void PurgeItems_Click(object sender)
        {
            try
            {
                if (ShowPurgeConfirmation("make it so!"))
                {
                    dbRemv.Enabled = false;
                    ShowProgress = disk.Length > 250;
                    if (ShowProgress)
                    {
                        if (sender == purgeItem) RecoverDB.Controls.Add(dbProg);
                        if (sender == rebuildDBmenu) this.Controls.Add(dbProg);
                        InitProgressBar();
                    }
                    Task.Run(delegate
                    {
                        if (!Remove_Items_From_Database())
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
                        Invoke(new Action(() =>
                        {
                            if (ShowProgress)
                            {
                                dbProg.Visible = false;
                                ShowProgress = false;
                                BrowseDB.Controls.Add(dbProg);
                            }
                            dbRemv.Enabled = true;
                            ReadDB(true, false);
                            dbRemoved = disk.Where(d => d.Marked).Select(d => d.Index).ToList();
                            UpdateRemoveListView();
                            if (sender == purgeItem && dbRemoved.Count == 0) RecoverDB?.Close();
                        }));
                    });
                }
            }
            catch { }
            bool ShowPurgeConfirmation(string requiredWord)
            {
                Form confirmForm = new Form()
                {
                    Width = 400,
                    Height = 180,
                    Text = "Confirm Purge",
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false,
                };

                Label label = new Label()
                {
                    Text = $"This will permanently remove *ALL* items in this list.\nType \"{requiredWord}\" to confirm:",
                    Dock = DockStyle.Top,
                    Height = 60,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
                };

                TextBox inputBox = new TextBox()
                {
                    //Dock = DockStyle.Fill,
                    Top = (confirmForm.Height - 70) / 2,
                    Left = (confirmForm.Width - 200) / 2,
                    Margin = new Padding(10),
                    Width = 200,
                };

                Button confirmBtn = new Button()
                {
                    Text = "Confirm",
                    Dock = DockStyle.Bottom,
                    Enabled = false,
                };

                Button cancelBtn = new Button()
                {
                    Text = "Cancel",
                    Dock = DockStyle.Bottom,
                };

                confirmBtn.Click += (ss, ee) => confirmForm.DialogResult = DialogResult.OK;
                cancelBtn.Click += (ss, ee) => confirmForm.DialogResult = DialogResult.Cancel;

                inputBox.TextChanged += (ss, ee) =>
                {
                    confirmBtn.Enabled = inputBox.Text.Equals(requiredWord, StringComparison.OrdinalIgnoreCase);
                };

                confirmForm.Controls.Add(cancelBtn);
                confirmForm.Controls.Add(confirmBtn);
                confirmForm.Controls.Add(inputBox);
                confirmForm.Controls.Add(label);
                return confirmForm.ShowDialog() == DialogResult.OK;
            }
        }
    }
}
