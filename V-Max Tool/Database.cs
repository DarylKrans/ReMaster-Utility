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
        int currentEditIndex = -1;
        string lastNotes = string.Empty;
        private bool lastIconHoverState = false;
        private DateTime lastHoverUpdate = DateTime.MinValue;
        private readonly TimeSpan hoverDelay = TimeSpan.FromMilliseconds(10); // tweak as needed
        private DateTime ctrlHold;
        private bool ctrlHeld = false;
        private int bdbW = 0;
        private int regionSort = 0;
        private int lastSortedColumn = -1;
        //private int dbRemoved = 0;
        List<int> dbRemoved = new List<int>();
        private int RegionColumn;
        private int[] skipColumns;
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
        ToolStripMenuItem favItem = new ToolStripMenuItem("Add to Favorites");
        ToolStripMenuItem unfavItem = new ToolStripMenuItem("Unfavorite");
        ToolStripSeparator[] dbSep = new ToolStripSeparator[5];

        ContextMenuStrip udMenu = new ContextMenuStrip();
        ToolStripMenuItem recoverItem = new ToolStripMenuItem("Recover");
        ToolStripMenuItem purgeItem = new ToolStripMenuItem("Purge All Items");

        DoubleBufferedListView dbView = new DoubleBufferedListView();
        DoubleBufferedListView dbRemv = new DoubleBufferedListView();

        Panel editPan = new Panel();
        TextBox editTitle = new TextBox();
        NumericUpDown dSide = new NumericUpDown();
        NumericUpDown dYear = new NumericUpDown();
        ComboBox dRegn = new ComboBox();
        ComboBox dProt = new ComboBox();
        Button dOK = new Button();
        Button dCancel = new Button();
        Button dAddNotes = new Button();
        RichTextBox dNotes = new RichTextBox();
        Button nOK = new Button();
        Button nCancel = new Button();
        string NotesChange = string.Empty;
        bool changeNotes = false;

        private System.Windows.Forms.ToolTip dbTooltip = new System.Windows.Forms.ToolTip();


        // database icon placement

        int fav = 2 * 20;
        int note = 1 * 20;
        int stat = 1 * 20;

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

        void SetDBContextItems()
        {
            browseDBmenu.Enabled = disk.Length - dbRemoved.Count > 0;
            recoverDBmenu.Visible = rebuildDBmenu.Visible = toolStripSeparator5.Visible = dbRemoved.Count > 0;
        }

        void Setup_Database_Window()
        {
            ReadDB(false, false);
            dbRemoved = disk != null && disk.Length > 0 ? disk.Where(d => d.Marked).Select(d => d.Index).ToList() : new List<int>();
            SetDBContextItems();
            databaseToolStripMenuItem.Visible = disk.Length > 0;

            icons.ImageSize = new Size(20, 20);
            icons.Images.Add("lock", Resources._lock);      // lock icon
            icons.Images.Add("star", Resources.star);       // favorites icon
            icons.Images.Add("notes", Resources.notes);     // notes icon
            icons.Images.Add("edit", Resources.pencil);     // edit icon
            icons.Images.Add("notesH", Resources.notesH);   // notes highlighted icon
            icons.Images.Add("editH", Resources.pencilH);   // edit highlighted icon
            icons.Images.Add("editG", Resources.pencilG);   // edit grayed out
            icons.Images.Add("ok", Resources.OK);           // good image icon
            icons.Images.Add("bad", Resources.bad);         // bad image icon
            icons.Images.Add("wwe", Resources.wwe);         // works, with errors icon
            icons.Images.Add("recover", Resources.recover);         // recover
            icons.Images.Add("recoverH", Resources.recoverH);         // recover Hovered
            icons.Images.Add("redX", Resources.redX);         // recover
            icons.Images.Add("grnChk", Resources.greenChk);         // recover Hovered
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
            dRegn.DropDownStyle = ComboBoxStyle.DropDownList;
            dRegn.DataSource = new string[] { "( n/a )", "NTSC", "PAL" };
            dProt.Left = dRegn.Left + dRegn.Width;
            dProt.DropDownStyle = ComboBoxStyle.DropDownList;
            List<string> list = new List<string>();
            for (int p = 0; p < Prot.Count; p++) list.Add(Prot[p]);
            dProt.DataSource = list;
            editTitle.Width = dbView.Columns[1].Width - 70;
            editTitle.Top = 0;
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
            editPan.Height = 18;
            editPan.Width = dbView.Columns.Cast<ColumnHeader>().Sum(c => c.Width) - 24;
            editPan.Visible = editing;
            dCancel.Size = new Size(24, 24);
            dCancel.Left = GetColumnX(dbView, 2) - 24;
            dCancel.BackgroundImage = icons.Images["redX"];
            dOK.Left = dCancel.Left - 28;
            dOK.BackgroundImage = icons.Images["grnChk"];
            dOK.Size = new Size(24, 24);
            dAddNotes.Left = dProt.Left + dProt.Width + 2;
            dAddNotes.Width = editPan.Width - dAddNotes.Left;
            dAddNotes.Height = editPan.Height;
            dAddNotes.Text = "Edit Notes";
            dNotes.Width = EditNotes.Width - 20;
            dNotes.Height = EditNotes.Height - 90;
            dNotes.Location = new Point(2, 2);
            dNotes.MaxLength = DiskInfo.NOTES_SIZE;
            EditNotes.Controls.Add(dNotes);
            EditNotes.Controls.Add(nOK);
            EditNotes.Controls.Add(nCancel);
            nOK.Dock = DockStyle.Bottom;
            nOK.Text = "OK";
            nCancel.Text = "Cancel";
            nCancel.Dock = DockStyle.Bottom;
            nCancel.Click += (s, e) =>
            {
                dNotes.Clear();
                NotesChange = string.Empty;
                changeNotes = false;
                EditNotes.Close();
            };
            nOK.Click += (s, e) =>
            {
                NotesChange = dNotes.Text.Length > 0 ? SanitizeRichText(dNotes) : string.Empty;
                changeNotes = true;
                EditNotes.Close();
            };

            dAddNotes.BackColor = Color.LightGreen;
            BrowseDB.Controls.Add(editPan);
            dOK.Click += (s, e) =>
            {
                if (currentEditIndex >= 0 && currentEditIndex < disk.Length)
                {
                    int selectedIndex = dbView.SelectedItems[0].Index;
                    disk[currentEditIndex].Title = editTitle.Text.Length <= DiskInfo.NAME_SIZE ? editTitle.Text : disk[currentEditIndex].Title;
                    disk[currentEditIndex].Side = (int)dSide.Value - 1;
                    disk[currentEditIndex].Region = dRegn.SelectedIndex;
                    disk[currentEditIndex].Year = (int)dYear.Value;
                    disk[currentEditIndex].Protection = (byte)dProt.SelectedIndex;
                    if (changeNotes) disk[currentEditIndex].Notes = NotesChange;
                    UpdateDBDirectory(new int[] { currentEditIndex });
                    int lastidx = GetLastVisibleIndex(dbView);
                    ReadDB(true, false);
                    ScrollToIndex(dbView, lastidx);
                }
                editPan.Visible = false;
                dbView.Enabled = true;
                editing = false;
                currentEditIndex = -1;
            };
            dCancel.Click += (s, e) =>
            {
                editPan.Visible = false;
                dbView.Enabled = true;
                editing = false;
                currentEditIndex = -1;
            };
            dOK.MouseMove += (s, e) =>
            {
                var x = e.Location.X + 32; var y = e.Location.Y;
                dbTooltip.Show("Apply Changes", dOK, x, y);
            };
            dOK.MouseLeave += (s, e) => dbTooltip.Hide(dOK);
            dCancel.MouseMove += (s, e) =>
            {
                var x = e.Location.X + 32; var y = e.Location.Y;
                dbTooltip.Show("Cancel and discard changes", dCancel, x, y);
            };
            dCancel.MouseLeave += (s, e) => dbTooltip.Hide(dCancel);

            int GetColumnX(ListView listView, int columnIndex)
            {
                int x = 0;
                for (int i = 0; i < columnIndex; i++)
                {
                    x += listView.Columns[i].Width;
                }
                return x;
            }
            dAddNotes.Click += (s, e) =>
            {
                EditNotes.Location = new Point(
                    this.Location.X + ((this.Width - EditNotes.Width) / 2),
                    this.Location.Y + (this.Height - EditNotes.Height) / 2);
                dNotes.Text = disk[(int)dbView.SelectedItems[0].Tag].Notes ;
                dNotes.SelectionStart = dNotes.TextLength;
                dNotes.SelectionLength = 0;
                EditNotes.Text = $"Edit Notes ({dNotes.Text.Length}/{DiskInfo.NOTES_SIZE})";
                dNotes.ScrollToCaret();
                dNotes.Focus();
                changeNotes = false;
                EditNotes.ShowDialog(this);
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
            dbView.Location = new Point(0, 0);
            dbView.Width = totalWidth + 22;
            dbView.Height = BrowseDB.Height - 38 - dbView.Top; //BrowseDB.Height;
            dbView.ShowItemToolTips = true;

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
                browseDBmenu.Click += (s, e) => OpenDB_Windows();
                addFolderDBmenu.Click += (s, e) =>
                {
                    FolderBrowserDialog opn = new FolderBrowserDialog
                    {
                        ShowNewFolderButton = false,
                    };
                    opn.ShowDialog();
                    string path = opn.SelectedPath;
                    if (path != null && Directory.Exists(path)) BuildDB(path);
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

                dbView.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        ListView lv = (ListView)s;
                        ListViewHitTestInfo hit = lv.HitTest(e.Location);

                        if (hit.Item != null && hit.SubItem != null)
                        {
                            int index = (int)hit.Item.Tag;
                            int subItemIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
                            if (subItemIndex == 2 && !disk[index].Locked) EditItemHandler(index);
                        }
                    }
                };

                dbRemv.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        ListView lv = (ListView)s;
                        ListViewHitTestInfo hit = lv.HitTest(e.Location);

                        if (hit.Item != null && hit.SubItem != null)
                        {
                            int index = (int)hit.Item.Tag;
                            if (index >= 0 && index < disk.Length)
                            {
                                int subItemIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
                                if (subItemIndex == 2 && !disk[index].Locked)
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
                                    }
                                    dbRemoved = disk.Where(d => d.Marked).Select(d => d.Index).ToList();
                                    int lastidx = GetLastVisibleIndex(dbRemv);
                                    UpdateRemoveListView();
                                    if (dbRemoved.Count == 0) RecoverDB?.Close();
                                    else ScrollToIndex(dbRemv, lastidx);
                                }
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

                dbView.KeyUp += (s, e) =>
                {
                    var keys = new Keys[] { Keys.Up, Keys.Down, Keys.PageUp, Keys.PageDown };
                    if (e.KeyData == Keys.Enter && dbView.SelectedItems.Count == 1)
                    {
                        var idx = (int)dbView.SelectedItems[0].Tag;
                        if (idx >= 0) Import_Image_From_Database(idx);
                    }

                    if (keys.Contains(e.KeyData))
                    {
                        if (dbView.SelectedItems.Count > 0)
                        {
                            var selectedItem = dbView.SelectedItems[0];
                            if (selectedItem?.Tag != null) UpdatePreview((int)selectedItem.Tag);
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
                                if (!item.Selected)
                                {
                                    dbView.SelectedItems.Clear();
                                    item.Selected = true;
                                }
                                int idx = (int)item.Tag;
                                //bool Locked = disk[((Tag)item.Tag).Index].Locked;
                                bool Locked = disk[idx].Locked;
                                bool Favorite = disk[idx].Favorite;
                                dbMenu.Items.Add(Locked ? unlockItem : lockItem);
                                dbMenu.Items.Add(Favorite ? unfavItem : favItem);
                                if (!Locked)
                                {
                                    dbMenu.Items.Add(dbSep[separator++]);
                                    dbMenu.Items.Add(removeItem);
                                    dbMenu.Items.Add(dbSep[separator++]);
                                    dbMenu.Items.Add(editItem);
                                    if (disk.Length > 1) dbMenu.Items.Add(dupeItem);
                                }
                            }
                        }
                        dbMenu.Show(dbView, e.Location);
                    }
                };

                unlockItem.Click += (s, e) => LockFile(s);
                lockItem.Click += (s, e) => LockFile(s);
                favItem.Click += (s, e) => Favorite(s);
                unfavItem.Click += (s, e) => Favorite(s);
                editItem.Click += (s, e) =>
                {
                    if (dbView.SelectedItems.Count == 1)
                    {
                        int index = (int)dbView.SelectedItems[0].Tag;
                        if(index >=0 && index < disk.Length && !disk[index].Locked) EditItemHandler(index);
                    }
                    if (dbView.SelectedItems.Count > 1)
                    {
                        Text = "Fuck off!, too many items (translation : you still need to add this functionality)";
                    }
                };

                removeItem.Click += (s, e) =>
                {
                    int lastidx = GetLastVisibleIndex(dbView);
                    List<int> remove = new List<int>();
                    foreach (ListViewItem item in dbView.SelectedItems)
                    {
                        var idx = (int)item.Tag;
                        //Text = $"{idx} {disk[idx].Title} {disk[idx].Locked}";
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
                    }
                    else
                    {
                        using (Message_Center msgs = new Message_Center(this))
                            MessageBox.Show("No duplicates found!", "Congratulations!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };
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

            if (currentEditIndex >= 0 && currentEditIndex < lv.Items.Count)
            {
                lv.Items[currentEditIndex].Selected = true;
                lv.Items[currentEditIndex].Focused = true;
            }
            lv.EndUpdate();
            lv.Invalidate();
        }

        void EditItemHandler(int index)
        {
            if (index >= 0 && index < disk.Length)
            {
                editing = true;
                currentEditIndex = index;
                var itemBounds = dbView.SelectedItems[0].Bounds;
                int x = dbView.Left + itemBounds.Left + 24;
                int y = dbView.Top + itemBounds.Top + 3;
                editPan.Top = y;
                editPan.Left = x;
                editPan.BringToFront();
                editPan.Visible = true;
                var idx = (int)dbView.SelectedItems[0].Tag;
                editTitle.Text = disk[idx].Title;
                dSide.Value = disk[idx].Side + 1;
                dYear.Value = disk[idx].Year;
                dRegn.SelectedIndex = disk[idx].Region;
                dProt.SelectedIndex = disk[idx].Protection;
                dNotes.Text = disk[idx].Notes;
                dbTooltip.Hide(dbView);
                editTitle.Focus();
                dbView.Enabled = false;
            }
            else currentEditIndex = -1;
        }

        void UpdateRemoveListView()
        {
            dbRemv.BeginUpdate();
            dbRemv.Items.Clear();
            foreach (int i in dbRemoved)
            {
                ListViewItem item = new ListViewItem($"{i}");   // Locked
                item.SubItems.Add(disk[i].Title);                                  // Title
                item.SubItems.Add(string.Empty);                                  // Title
                item.SubItems.Add($"{disk[i].Side + 1}");                          // Side
                item.SubItems.Add(disk[i].Year > 1970 ? $"{disk[i].Year}" : string.Empty);    // Year
                item.SubItems.Add($"{getRegion[disk[i].Region]}");             // Region
                item.SubItems.Add($"{Prot[disk[i].Protection]}");                  // Protection
                item.SubItems.Add($"{disk[i].Timestamp}");                         // Timestamp
                                                                                   //item.Tag = new Tag { Index = disk.Index, Notes = disk.Notes };
                item.Tag = i;
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
            });
            Thread.Sleep(150);
            BrowseDB.ShowDialog(this);
        }

        void UpdatedbView()
        {
            if (disk?.Length > 0)
            {
                var sortedList = disk.OrderBy(d => d.Title).ToList();
                Invoke(new Action(() => UpdateList(RegionFilter(sortedList), true)));
                if (disk?.Length == dbRemoved.Count) ShowMessage();
            }
            else ShowMessage();
            if (dbSearch.Text.Length > 0)
            {
                var text = dbSearch.Text;
                Invoke(new Action(() =>
                {
                    dbSearch.Text = string.Empty;
                    dbSearch.Text = text;
                }));
            }

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

            foreach (int i in indexes)
            {
                if (!disk[i].Locked || ignoreLock)
                {
                    disk[i].Marked = true;
                    disk[i].Locked = false;
                    dbRemoved.Add(i); // ++;
                }
            }

            UpdateDBDirectory(indexes);

            if (dbRemoved.Count >= 100) PromptRebuildDatabase();
            ReadDB();
        }

        void LockFile(object s)
        {
            if (dbView.SelectedItems.Count > 0)
            {
                bool lockfile = s.Equals(lockItem);
                List<int> selected = new List<int>();
                dbView.BeginUpdate();
                foreach (ListViewItem currentItem in dbView.SelectedItems)
                {
                    var index = (int)currentItem.Tag;
                    disk[index].Locked = lockfile; // true;
                    selected.Add(index);
                }
                dbView.EndUpdate();
                UpdateDBDirectory(selected.ToArray());
            }
        }

        void Favorite(object s)
        {
            if (dbView.SelectedItems.Count > 0)
            {
                bool fav = s.Equals(favItem);
                List<int> selected = new List<int>();
                dbView.BeginUpdate();
                foreach (ListViewItem currentItem in dbView.SelectedItems)
                {
                    var index = (int)currentItem.Tag;
                    disk[index].Favorite = fav; // true;
                    selected.Add(index);
                }
                dbView.EndUpdate();
                UpdateDBDirectory(selected.ToArray());
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

        void ReadDB(bool UpdateList = true, bool showErrorMSG = true)
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
                        if (showErrorMSG)
                        {
                            error = true;
                            string title = "Error accessing database.";
                            string message = ex.Message;
                            MessageForYouSir(title, message);
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
            dbRemoved = new List<int>(); // 0;
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
                    ListViewItem item = new ListViewItem(string.Empty);   // Locked
                    item.SubItems.Add(disk.Title);                                  // Title
                    item.SubItems.Add(string.Empty);                                  // Title
                    item.SubItems.Add($"{disk.Side + 1}");                          // Side
                    item.SubItems.Add(disk.Year > 1970 ? $"{disk.Year}" : string.Empty);    // Year
                    item.SubItems.Add($"{getRegion[disk.Region]}");             // Region
                    item.SubItems.Add($"{Prot[disk.Protection]}");                  // Protection
                    item.SubItems.Add($"{disk.Timestamp}");                         // Timestamp
                    //item.Tag = new Tag { Index = disk.Index, Notes = disk.Notes };
                    item.Tag = disk.Index;
                    dbView.Items.Add(item);
                }
                else dbRemoved.Add(disk.Index); // ++;
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
                return null;
            }
        }

        bool Remove_Items_From_Database() //, bool IgnoreLockStatus = false)
        {
            string tempPath = $@"c:\test\temp.db";
            if (File.Exists(tempPath)) File.Delete(tempPath);
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

        void BuildDB(string rpath)
        {
            Dictionary<string, int> fileExt = new Dictionary<string, int>
            {
                { ".nib", 0 } ,{ ".nbz", 1 }, { ".g64", 2 }
            };
            Random rand = new Random();
            var ttl = Text;
            using (var dataBase = !File.Exists(TEMP.dbPath)
                ? new AccessDatabase().Create(TEMP.dbPath)
                : new AccessDatabase().ReadWrite(TEMP.dbPath))
            {
                if (dataBase.Valid)
                {
                    //string rpath = @"c:\test\test2";
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
                                var extension = Path.GetExtension(f).ToLower();
                                var dec = extension == ".nib" ? File.ReadAllBytes(f) : LZdecompress(File.ReadAllBytes(f));
                                int t = (dec.Length - 256) / 8192 > 42 ? 34 : 17;
                                var tk = new byte[8192];
                                var preview = new byte[0];
                                Buffer.BlockCopy(dec, 256 + (t * 8192), tk, 0, tk.Length);
                                string pv = Get_Disk_Directory(tk);
                                preview = Compress(Encoding.ASCII.GetBytes(pv));
                                var cmp = Compress(dec);
                                dataBase.Seek(dataBase.Offset);
                                dataBase.Write(ArrayConcat(cmp, preview));
                                var notes = rand.Next(3) == 0 ? Get_Name(Path.GetFileNameWithoutExtension(f.Replace("_", " "))) : string.Empty;
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
                                    Notes = notes, //Get_Name(Path.GetFileNameWithoutExtension(f.Replace("_", " "))),
                                    Extension = fileExt[extension],
                                    Favorite = rand.Next(5) == 0,
                                    Status = rand.Next(4),
                                };
                                write.Write(info.ToEntry());
                                dataBase.Offset += info.CompressedLength + info.PreviewLength;
                                Text = $"{ttl} ({dataBase.Entries++}/{file.Length})";
                                //string g = info.imgStat[info.Status];

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
                    if (ShowProgress) dbProg.BringToFront();
                    dbProg.Value = 0;
                    dbProg.Maximum = 100 * 100;
                    dbProg.Value = dbProg.Maximum / 100;
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
                }
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
            int editColumnIndex = 2;

            if (e.ColumnIndex == lockColumnIndex)
            {
                if (disk[(int)e.Item.Tag].Locked)
                {
                    int imgX = e.Bounds.X + (e.Bounds.Width - 20) / 2;
                    int imgY = e.Bounds.Y + (e.Bounds.Height - 20) / 2;
                    e.Graphics.DrawImage(icons.Images["lock"], new Rectangle(imgX, imgY, 20, 20));
                }
            }
            //
            if (e.ColumnIndex == editColumnIndex)
            {
                Image icon = null;
                var lv = (ListView)sender;
                Point cursorPos = lv.PointToClient(Cursor.Position);
                ListViewHitTestInfo hit = lv.HitTest(cursorPos);
                bool Hovered = hit.Item?.Index == e.ItemIndex &&
                     hit.Item.SubItems.IndexOf(hit.SubItem) == e.ColumnIndex;
                if (sender == dbView)
                {
                    if (!disk[(int)e.Item.Tag].Locked) icon = Hovered ? icons.Images["editH"] : icons.Images["edit"];
                    else icon = icons.Images["editG"];
                }
                if (sender == dbRemv)
                {
                    if (disk[(int)e.Item.Tag].Marked) icon = Hovered ? icons.Images["recover"] : icons.Images["recoverH"];
                }
                int imgX = e.Bounds.X + (e.Bounds.Width - 20) / 2;
                int imgY = e.Bounds.Y + (e.Bounds.Height - 20) / 2;
                e.Graphics.DrawImage(icon, new Rectangle(imgX, imgY, 16, 16));
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

            if (sender == dbView && e.ColumnIndex == titleColumnIndex)
            {
                int imgX;
                int imgY;
                var idx = (int)e.Item.Tag;
                if (disk[idx].Favorite)
                {
                    imgX = e.Bounds.X + (e.Bounds.Width - fav);
                    imgY = e.Bounds.Y + (e.Bounds.Height - 20);
                    e.Graphics.DrawImage(icons.Images["star"], new Rectangle(imgX, imgY, 16, 16));
                }

                if (!string.IsNullOrEmpty(disk[idx].Notes))
                {
                    var lv = (ListView)sender;
                    Point cursorPos = lv.PointToClient(Cursor.Position);
                    //ListViewHitTestInfo hit = lv.HitTest(cursorPos);
                    Rectangle subItemBounds = e.SubItem.Bounds;
                    // Define the icon's bounds: 16x16 size, positioned 20px from the right edge
                    Rectangle iconBounds = new Rectangle(subItemBounds.Right - note,
                        subItemBounds.Top + (subItemBounds.Height - 20) / 2, 20, 20);
                    Image icon = iconBounds.Contains(cursorPos) ? icons.Images["notesH"] : icons.Images["notes"];
                    imgX = e.Bounds.X + (e.Bounds.Width - note);
                    imgY = e.Bounds.Y + (e.Bounds.Height - 20);
                    e.Graphics.DrawImage(icon, new Rectangle(imgX, imgY, 20, 20));
                }

                if (disk[idx].Status > 0)
                {
                    //int size = disk[idx].Notes?.Length > 0 && disk[idx].Status != 2 ? 14 : 20;
                    int size = disk[idx].Notes?.Length > 0 ? 10 : 16;
                    int offset = 0; // size < 20 ? 3 : 0;
                    imgX = e.Bounds.X + (e.Bounds.Width - stat) + offset;
                    imgY = e.Bounds.Y + (e.Bounds.Height - 20) + offset;
                    //if (size < 20) { imgX += 3; imgY += 3; }
                    Image icon = null;
                    switch (disk[idx].Status)
                    {
                        case 1: icon = icons.Images["ok"]; break;
                        case 2: icon = icons.Images["wwe"]; break;
                        case 3: icon = icons.Images["bad"]; break;
                    }
                    e.Graphics.DrawImage(icon, new Rectangle(imgX, imgY, size, size));
                }
            }
        }

        private void DbView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = dbView.HitTest(e.Location);
            int subItemIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
            if (e.Button == MouseButtons.Left && hit.Item != null && subItemIndex != 2)
            {
                if (hit.Item?.Tag != null) Import_Image_From_Database((int)hit.Item.Tag);
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
                        var idx = (int)info.Item.Tag;
                        var x = e.Location.X + 32; var y = e.Location.Y;
                        int subItemIndex = info.Item.SubItems.IndexOf(info.SubItem);
                        var stats = disk[idx].Status > 0 ? $"{disk[idx].imgStat[disk[idx].Status]}" : string.Empty;
                        if (subItemIndex == 1) // && disk[idx].Notes != null)
                        {
                            Rectangle subItemBounds = dbView.GetSubItemBounds(dbView.Items[index], subItemIndex);

                            // Define the icon's bounds: 16x16 size, positioned 20px from the right edge
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
                                dbView.Invalidate(); // only redraw when state changes

                            }

                            var tip = string.Empty;

                            if (stats != string.Empty) tip += $"{stats}\n";
                            if (!string.IsNullOrEmpty(disk[idx].Notes)) tip += disk[idx].Notes;
                            //if (disk[idx].Notes.Length > 0) tip += disk[idx].Notes;
                            if (tip != string.Empty && (noteBounds.Contains(e.X, e.Y) || statBounds.Contains(e.X, e.Y)))
                                dbTooltip.Show(tip, dbView, x, y);
                            else if (favBounds.Contains(e.X, e.Y) && disk[idx].Favorite) dbTooltip.Show("Favorite", dbView, x, y);
                            else dbTooltip.Hide(dbView);
                        }
                        else if (subItemIndex == 0 && disk[idx].Locked) dbTooltip.Show("Locked", dbView, x, y);
                        else if (subItemIndex == 2) dbTooltip.Show(!disk[idx].Locked ? "Edit" : "Unlock to edit", dbView, x, y);
                        else dbTooltip.Hide(dbView); //dbTooltip.SetToolTip(dbView, null);

                        if (index != dbLastHoveredItem && DateTime.Now - lastHoverUpdate > hoverDelay)
                        {
                            dbLastHoveredItem = index;
                            dbView.Invalidate(); // Redraw to apply new hover effect
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
                ListViewHitTestInfo info = dbRemv.HitTest(e.Location);
                try
                {
                    int index = info.Item?.Index ?? -1;
                    if (index >= 0 && index < dbRemv.Items.Count)
                    {
                        var idx = (int)info.Item.Tag;
                        var x = e.Location.X + 32; var y = e.Location.Y;
                        int subItemIndex = info.Item.SubItems.IndexOf(info.SubItem);

                        if (subItemIndex == 2 && sender == dbRemv) dbTooltip.Show($"Recover {disk[idx].Title}", dbRemv, x, y);
                        else dbTooltip.Hide(dbRemv); //dbTooltip.SetToolTip(dbView, null);

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
                if (holdtime.TotalMilliseconds < 350)
                {
                    PVbox.Visible = !PVbox.Visible;
                    if (PVbox.Visible) BrowseDB.Width += PVbox.Width;
                    else BrowseDB.Width -= PVbox.Width;
                }
            }
            if (editing && e.KeyData == Keys.Escape)
            {
                editing = false;
                dbView.Enabled = true;
                editPan.Visible = false; // SendToBack();
                dbView.Focus();
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
                        dbProg.BringToFront();
                        dbProg.Value = 0;
                        dbProg.Maximum = 100 * 100;
                        dbProg.Value = dbProg.Maximum / 100;
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
                                dbProg.SendToBack();
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
