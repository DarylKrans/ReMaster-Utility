using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        private static readonly Label[] BlkMap_track = new Label[41];
        private static readonly Label[] BlkMap_sector = new Label[21];
        private static readonly TaggedRectangle[][] BlkMap_bam = new TaggedRectangle[41][];
        private List<BlockMapInfo> blockMap = new List<BlockMapInfo>();
        private int hoveredIndex = -1;
        private int clickedIndex = -1;
        private int lastHovered = -1; //0;
        private Color lastHoveredColor = Color.Black; // = 0;
        private Color CurrentViewColor = Color.HotPink;
        private static readonly Color hiLight = Color.White; // Color.Yellow; 
        private bool dispEncoding = false;
        private bool lastDispEnc = false;
        //private bool secDispWasShown = false;
        private int viewedIndex = -1;
        private Color viewedColor = Color.Black;
        private Color currentHoverColor = Color.Black;

        public static readonly string[] CBM_Fmt = new string[]
        {
            "Standard CBM",
            "Vorpal v1",
            "MicroProse",
            "Vorpal Loader",
        };

        private static readonly Dictionary<int, Color> ErrorMap = new Dictionary<int, Color>
        {
            {2, Color.Red }, {4, Color.Salmon }, {5, Color.Orange },
            {6, Color.Pink }, {9, Color.DeepPink }, {11, Color.Yellow },
        };

        readonly Dictionary<int, Color> colorMap = new Dictionary<int, Color>
        {
            { 0, Color.FromArgb(110, 70, 173) }, { 1, Color.Black }, { 2, Color.DarkMagenta },
            { 3, Color.Green }, { 4, Color.Blue }, { 15, Color.DarkCyan }, { 6, Color.FromArgb(128, 64, 0) },
            { 7, Color.Blue }, { 8, Color.Blue }, { 9, Color.Blue }, { 10, Color.FromArgb(180, 100, 100) },
            { 11, Color.Blue }, { 12, Color.Blue }, { 13, Color.FromArgb(84, 128, 255) }, { 14, Color.Blue },
            { 5, Color.FromArgb(0, 80, 80) }
        };

        void SetBlockMapFonts()
        {
            BamDB2.Font = BamDB3.Font = FontCbox.Checked ? GetFont(6.2f)
            : BamDB2.Font = BamDB3.Font = new Font("Courier New", 9.4f, FontStyle.Bold); //GetFont(7);
            Dir_Box.Font = Dir_screen.Font = FontCbox.Checked? GetFont(12)
                : new Font("Courier New", 18.4f, FontStyle.Bold);
        }

        void BlockMap_Setup()
        {
            this.Controls.Add(BamDispPan);
            BamDB1.Height = 280;
            BamDB1.Font = new Font("Courier New", 9f, FontStyle.Regular); //GetFont(7);
            SetBlockMapFonts();
            BamDB2.Size = new Size(530, 500);
            BamDB1.ScrollBars = BamDB2.ScrollBars = BamDB3.ScrollBars = RichTextBoxScrollBars.None;
            BamDB2.BackColor = BamDB3.BackColor = C64_screen;
            BamDB2.ForeColor = BamDB3.ForeColor = c64_text;
            BamDispPan.Location = new Point(0, 0);
            BamDispPan.Location = new Point(0, 50);
            int _width = BamDB2.Font.Name == _C64ProMono.Families[0].Name ? 530 : 555;
            BamDispPan.Size = new Size(_width, 610);
            BamDispPan.BringToFront();
            BamSplit.Location = new Point(0, 0);
            BamSplit.Width = BamSplitPan1.Width;
            BamSplit.Height = BamSplitPan1.Height;
            BamSplitPan1.AutoScroll = false;
            BamSplitPan1.HorizontalScroll.Maximum = 0;
            BamSplitPan1.HorizontalScroll.Visible = false;
            BamSplitPan1.AutoScroll = true;
            BamSplit.SplitterDistance = 385;
            BamDispPan.Visible = false;

            Blk_pan.Paint += PaintBAM;
            Blk_pan.MouseMove += Bam_Layout_MouseMove;
            Blk_pan.MouseEnter += Blk_pan_MouseEnter;
            Blk_pan.MouseLeave += Button_MouseLeave;
            //Blk_pan.MouseDown += Blk_pan_MouseDown;
            //Blk_pan.MouseUp += Blk_pan_MouseUp;
            Blk_pan.MouseClick += Blk_pan_Click;
            track_label.Text = "";
            track_label.AutoSize = false;
            track_label.NewText = "Track";
            track_label.ForeColor = Color.White;
            track_label.RotateAngle = -90;

            FreeBlk.Text = "";
            FreeBlk.AutoSize = false;
            FreeBlk.NewText = "Free Block";
            FreeBlk.ForeColor = Color.FromArgb(30, 125, 30);
            FreeBlk.RotateAngle = -90;

            AllocBlk.Text = "";
            AllocBlk.AutoSize = false;
            AllocBlk.NewText = "Allocated Block";
            AllocBlk.ForeColor = Color.FromArgb(30, 200, 30);
            AllocBlk.RotateAngle = -90;

            CSTfmt.Text = "";
            CSTfmt.AutoSize = false;
            CSTfmt.NewText = "Unknown Format";
            CSTfmt.ForeColor = Color.MediumOrchid;
            CSTfmt.RotateAngle = -90;

            BME_2.ForeColor = ErrorMap[2];
            BME_4.ForeColor = ErrorMap[4];
            BME_5.ForeColor = ErrorMap[5];
            BME_6.ForeColor = ErrorMap[6];
            BME_9.ForeColor = ErrorMap[9];
            BME_11.ForeColor = ErrorMap[11];

            // set Track # labels in Block Map
            int left = 25;
            int top = 27;
            int inc = 14; // 15
            for (int i = 0; i < 41; i++)
            {
                string spc = i < 9 ? " " : string.Empty;
                BlkMap_track[i] = new Label();
                BlkMap_Panel.Controls.Add(BlkMap_track[i]);
                BlkMap_track[i].AutoSize = true;
                BlkMap_track[i].Font = new Font("Courier New", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
                BlkMap_track[i].ForeColor = Color.DarkGray;
                BlkMap_track[i].Location = new Point(left, top + (inc * i) - 2);
                BlkMap_track[i].Size = new Size(26, 27);
                BlkMap_track[i].TabIndex = 4;
                BlkMap_track[i].Text = $"{spc}{i + 1}";
                BlkMap_track[i].Visible = true;
                BlkMap_track[i].BringToFront();
                Blk_pan.Height = 3 + inc + (inc * i);
            }
            // set Sector # labels in Block Map
            left = 50;
            top = 6;
            int spacing = 24;
            for (int i = 0; i < 21; i++)
            {
                string spc = i < 9 ? " " : string.Empty;
                BlkMap_sector[i] = new Label();
                this.BlkMap_Panel.Controls.Add(BlkMap_sector[i]);
                BlkMap_sector[i].AutoSize = true;
                BlkMap_sector[i].Font = new Font("Courier New", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
                BlkMap_sector[i].ForeColor = Color.DarkGray;
                BlkMap_sector[i].Location = new Point(left - (i < 9 ? 5 : 0) + (spacing * i), top);
                BlkMap_sector[i].Size = new Size(26, 27);
                BlkMap_sector[i].TabIndex = 4;
                BlkMap_sector[i].Text = $"{spc}{i}";
                BlkMap_sector[i].BringToFront();
                Blk_pan.Width = spacing + (spacing * i) + 2;
            }
        }

        private void Blk_pan_MouseUp(object sender, MouseEventArgs e)
        {
            BamDispPan.Visible = false;
            Refresh();
        }

        // set BAM buttons in Block Map
        void Set_BlockMap_Blocks()
        {
            blockMap = new List<BlockMapInfo>();
            var spacing = 2;
            var sectors = 21;
            for (int i = 0; i < 41; i++)
            {
                var _track = Disk.Source.Track.FirstOrDefault(x => (int)x.TrackNumber == i + 1);
                if (_track != null)
                {
                    switch (_track.Format)
                    {
                        case 1: sectors = 21; break;
                        case 2: sectors = 22; break;
                        case 3: sectors = 32; break;
                        case 4: sectors = 8; break;
                        case 5: sectors = 47; break;
                        case 6: sectors = 12; break;
                        case 13: sectors = 3; break;
                        default: sectors = 21; break;
                    }
                }
                else sectors = 21;
                var ht = (Blk_pan.Height / 41) - 2;
                var wt = (Blk_pan.Width / sectors) - spacing;
                BlkMap_bam[i] = new TaggedRectangle[sectors];
                for (int j = 0; j < sectors; j++)
                {
                    var x = spacing + (j * (wt + spacing));
                    var y = 2 + (i * (ht + 2));
                    var track = i + 1;
                    var sector = j + 1;
                    var color = Color.FromArgb(30, 100, 100, 100);
                    var tip = $"track {track} sector {sector}";
                    BlkMap_bam[i][j] = new TaggedRectangle(x, y, wt, ht, track, j);
                    blockMap.Add(new BlockMapInfo(BlkMap_bam[i][j], track, sector, color, tip));
                    Update_BlockMap(track, sector, color, tip);
                    Blk_pan.Invalidate();
                    Blk_pan.Visible = true;
                }
            }
        }

        void Set_BlockMap()
        {
            Stopwatch sw = Stopwatch.StartNew();
            Blk_pan.Visible = false;
            Set_BlockMap_Blocks();
            ResetAllBlocks();
            byte[] bam = GetBam();
            string usedsec = string.Empty;
            int trk = 0, validSectors = 0, sectors = 0, index = -1;
            for (int i = 0; i < tracks; i++)
            {
                int _fmt = Disk.Source.Track[i].Format;
                //if (Disk.Source.Track[i].Format == 1 || Disk.Source.Track[i].Format == 10)
                if (_fmt == 1 || _fmt == 10 || _fmt == 15)
                {
                    trk = (int)Disk.G64.Track[i].TrackNumber - 1;
                    validSectors = Available_Sectors[trk];
                    sectors = Disk.G64.Track[i].Sectors < validSectors ? validSectors : Disk.G64.Track[i].Sectors;
                    index = -1;
                    for (int j = 0; j < 21; j++)
                    {
                        try { index = blockMap.FindIndex(b => b.Track == trk + 1 && b.Sector == j + 1); }
                        catch { index = -1; }
                        var _s = Disk.G64.Track[i].GetSector(j);
                        if (_s != null && j < sectors && index >= 0)
                        {
                            bool valid = j < Available_Sectors[trk];
                            int errorCode = _s.ErrorCode;
                            bool error = errorCode > 1;
                            bool mp = false, vp = false;
                            string fmt = string.Empty;
                            switch (_s.Format)
                            {
                                case 0: fmt = CBM_Fmt[0]; break;
                                case 1: fmt = CBM_Fmt[1]; vp = true; break;
                                case 2: fmt = CBM_Fmt[2]; mp = true; break;
                                case 3: fmt = CBM_Fmt[3]; vp = true; break;
                                default: fmt = string.Empty; break;
                            }
                            bool available = BlockAllocStatus(bam, trk, j);
                            bool inRange = valid && trk < 35;

                            usedsec = trk > 34 || !valid ? "* outside BAM range" : !available ? "Block Allocated (Used)" : "Block Available (Free)";
                            usedsec += (error ? $"\nError {c1541error[errorCode]}" : string.Empty);

                            Color color;
                            int alpha = inRange ? 255 : 130;
                            if (error) color = ErrorMap.TryGetValue(errorCode, out var _c) ? _c : Color.Black;
                            else
                            {
                                int red = mp ? 180 : vp ? 0 : 30;
                                int green = !available ? 200 : mp ? 100 : vp ? 150 : 75;
                                int blue = mp ? 100 : vp ? 150 : 30;
                                color = Color.FromArgb(alpha, red, green, blue);
                            }

                            blockMap[index].Color = color;
                            blockMap[index].Tip = $"Track {trk + 1} Sector {j} {fmt}"
                                + (usedsec != "" ? $"\n{usedsec}" : "")
                                + (errorCode == 1 ? $"\n{ErrorCodes[errorCode]}" : "");
                        }
                        else
                        {
                            if (index >= 0)
                            {
                                blockMap[index].Color = Color.FromArgb(30, 100, 100, 100);
                                blockMap[index].Tip = string.Empty;
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        var fmt = Disk.Source.Track[i].Format;
                        trk = (int)Disk.G64.Track[i].TrackNumber - 1;
                        if (fmt < secF.Length - 1 && Disk.G64.Track[i].Data != null)
                        {
                            int sec = Disk.Source.Track[i].Sectors;
                            for (int j = 0; j < sec; j++)
                            {
                                index = blockMap.FindIndex(b => b.Track == trk + 1 && b.Sector == j + 1);
                                Color color = colorMap.TryGetValue(fmt, out var color2) ? color2 : Color.FromArgb(200, 100, 30, 100);
                                blockMap[index].Color = color;
                                blockMap[index].Tip = (fmt > 0 && fmt < secF.Length - 1)
                                    ? j < sec ? $"Track {trk + 1} Sector {j} {secF[Disk.Source.Track[i].Format]}" :
                                    string.Empty : string.Empty;
                            }
                        }
                    }
                    catch { }
                }
                if (tracks > 42) i++;
            }
            Blk_pan.Visible = true;
            sw.Stop();
            Console.WriteLine($"BlockMap Time : {sw.Elapsed.TotalMilliseconds}");
        }

        public void Update_BlockMap(int track, int sector, Color newColor, string tip = null)
        {
            var rectangleToUpdate = blockMap.FirstOrDefault(r => r.Track == track && r.Sector == sector);
            if (rectangleToUpdate != null)
            {
                // Update the color
                rectangleToUpdate.Color = newColor;

                // Redraw the panel to show the updated color
                Rectangle tempRect = new Rectangle(rectangleToUpdate.Rect.X, rectangleToUpdate.Rect.Y, rectangleToUpdate.Rect.Width, rectangleToUpdate.Rect.Height);
                if (tip != null) rectangleToUpdate.Tip = tip;
                Blk_pan.Invalidate(tempRect);
            }
        }

        void Blk_pan_MouseEnter(object sender, EventArgs e)
        {
            lastHoveredButton = sender as System.Windows.Forms.Panel;
            tips.Show(tips.GetToolTip(lastHoveredButton), lastHoveredButton, lastHoveredButton.Width, lastHoveredButton.Height);
        }

        void Button_MouseLeave(object sender, EventArgs e)
        {
            if (lastHoveredButton == sender)
            {
                tips.Hide(lastHoveredButton);
                lastHoveredButton = null;
                //if (lastHovered >= 0 && lastHoveredColor != Color.Black)
                if (lastHovered >= 0 && lastHovered != viewedIndex && lastHoveredColor != Color.Black)
                    Update_BlockMap(blockMap[lastHovered].Track, blockMap[lastHovered].Sector, lastHoveredColor);
                if (lastHovered == viewedIndex) Update_BlockMap(blockMap[lastHovered].Track, blockMap[lastHovered].Sector, CurrentViewColor);
            }
        }

        void DisplaySector(int index, bool GCR)
        {
            var block = blockMap[index];
            if (block.Tip == string.Empty) return;
            var _track = Disk.G64.Track
                .FirstOrDefault(t => t.TrackNumber == block.Track);
            var sector = _track.GetSector(block.Sector - 1);

            if (viewedIndex >= 0 && viewedIndex != index)
                Update_BlockMap(blockMap[viewedIndex].Track, blockMap[viewedIndex].Sector, viewedColor);

            viewedColor = currentHoverColor;
            viewedIndex = index;
            Update_BlockMap(blockMap[index].Track, blockMap[index].Sector, CurrentViewColor);
            //Text = $"currentHoverClr {currentHoverColor} viewedClr {viewedColor} viewedIDX {viewedIndex}";

            string content = $"Track {block.Track} Sector {block.Sector - 1}";
            string fmt;
            if (sector == null)
            {
                BamDB1.Text = content;
                BamDB2.Text = BamDB3.Text = string.Empty;
                return;
            }
            switch (sector.Format)
            {
                case 0: fmt = CBM_Fmt[0]; break;
                case 1: fmt = CBM_Fmt[1]; break;
                case 2: fmt = CBM_Fmt[2]; break;
                case 3: fmt = CBM_Fmt[3]; break;
                default: fmt = "n/a"; break; // string.Empty; break;
            }
            string status = sector.Format >= 0 ? $" / Status: {c1541error[sector.ErrorCode]}\n" : "\n";
            string _scksm = sector.Format == 3
                ? $"Sector : Hash {(sector.Data.Checksum ? "OK!" : "Failed!")}\n"
                : $"Sector : Checksum ({Hex_Val(new byte[] { sector.Data.ChecksumActual })}), Expected ({Hex_Val(new byte[] { sector.Data.ChecksumExpected })})\n";
            try
            {
                content += status
                + $"Header : Checksum ({Hex_Val(new byte[] { sector.Header.ChecksumActual })}), Expected ({Hex_Val(new byte[] { sector.Header.ChecksumExpected })})\n"
                + $"         Sync Length       {sector.Header.SyncLen} bits\n"
                + $"         Track Position    {sector.Header.Pos >> 3}\n"
                + $"         Tail Gap  Length  {sector.Header.TailGap.Length}\n"
                + $"         Illegal GCR Count {sector.Header.Illegal}\n"
                + _scksm
                + $"         Sync Length       {sector.Data.SyncLen} bits\n"
                + $"         Track Position    {sector.Data.Pos >> 3}\n"
                + $"         Tail Gap  Length  {sector.Data.TailGap.Length}\n"
                + $"         Illegal GCR Count {sector.Data.Illegal}\n"
                + $"\nDisplay Format {(GCR ? "GCR" : "Decoded")}, Sector Format ({fmt})\n";
            }
            catch (Exception ex) { Console.WriteLine($"error {ex.Message}"); }
            int _case = (BamDB2.Font.Name != _C64ProMono.Families[0].Name) ? 2 : 1;
            Console.WriteLine($"name equal? {BamDB2.Font.Name != _C64ProMono.Families[0].Name} {BamDB2.Font.Name} cbox {FontCbox.Checked}");
            byte[] sec = GCR ? sector.Data.GCR : sector.Data.Decoded;
            byte[] hdr = GCR ? sector.Header.GCR : sector.Header.Decoded;
            StringBuilder dContent = new StringBuilder();
            StringBuilder _dContent = new StringBuilder();
            if (hdr.Length > 0) content += $"\nHeader : {Hex_Val(hdr).Replace('-', ' ')}\n";
            if (sec.Length > 0)
            {
                content += $"Sector Length : {sec.Length}\n";
                int it = sec.Length >> 4;
                int rem = sec.Length - (it << 4);
                int lines = 0;
                for (int i = 0; i < it; i++)
                {
                    var pos = i << 4;
                    Hex_Val(ref dContent, sec, pos, 16);
                    dContent.Append('\n');
                    BytesToC64Glyph(ref _dContent, sec, pos, 16, _case);
                    if (i < it || (i == it - 1 && rem != 0)) _dContent.Append("\n");
                    lines++;
                }
                if (rem > 0)
                {
                    var pos = it << 4;
                    Hex_Val(ref dContent, sec, pos, rem);
                    dContent.Append('\n');
                    BytesToC64Glyph(ref _dContent, sec, pos, rem, _case);
                }

            }
            int previousHeight = BamDispPan.Height;
            BamDB1.Text = content;
            BamDB1.Height = TextRenderer.MeasureText(content, BamDB1.Font).Height;
            BamDB2.Text = dContent.ToString();
            BamDB3.Text = _dContent.ToString();
            BamDB2.Height = BamDB3.Height = BamSplit.Height =
                TextRenderer.MeasureText(BamDB2.Text, BamDB2.Font).Height;
            BamDispPan.Height = Math.Min(610, (BamSplit.Height + BamDB1.Height));
            BamSplitPan1.Height = BamDispPan.Height - BamDB1.Height; // 400;
            if (BamDispPan.Height != previousHeight) Refresh();
        }

        private void Blk_pan_MouseDown(object sender, MouseEventArgs e)
        {

            dispEncoding = (e.Button == MouseButtons.Left);
            BamDispPan.Visible = true;
            int index = hoveredIndex;
            if (index < 0) return;
            DisplaySector(index, dispEncoding);
        }

        private void Blk_pan_Click(object sender, MouseEventArgs e)
        {

            int index = hoveredIndex;
            if (index < 0 || blockMap[index].Tip == string.Empty) return;
            dispEncoding = (e.Button == MouseButtons.Left);
            BamDispPan.Visible = true;
            if (dispEncoding == lastDispEnc && clickedIndex == index)
            {
                BamDispPan.Visible = false;
                if (viewedIndex >= 0) Update_BlockMap(blockMap[viewedIndex].Track, blockMap[viewedIndex].Sector, viewedColor);
                viewedIndex = -1;
                clickedIndex = -1;
                Refresh();
                return;
            }

            lastDispEnc = dispEncoding;
            clickedIndex = index;
            DisplaySector(index, dispEncoding);
        }

        void ResetAllBlocks()
        {
            foreach (var sec in blockMap)
            {
                sec.Tip = string.Empty;
                sec.Color = Color.FromArgb(30, 100, 100, 100);
            }
        }

        private void Bam_Layout_MouseMove(object sender, MouseEventArgs e)
        {
            int index = blockMap.FindIndex(r => r.Contains(e.Location));
            if (index == hoveredIndex) return; // nothing changed, bail early

            if (index >= 0 && index != viewedIndex)
                currentHoverColor = blockMap[index].Color;

            if (index >= 0 && lastHovered >= 0 && lastHovered != index)
            {
                if (lastHoveredColor != Color.Black)
                {
                    Color _c = lastHovered == viewedIndex ? CurrentViewColor : lastHoveredColor;
                    Update_BlockMap(blockMap[lastHovered].Track, blockMap[lastHovered].Sector, _c);
                    //Update_BlockMap(blockMap[lastHovered].Track, blockMap[lastHovered].Sector, lastHoveredColor);
                }
                tips.Hide(Blk_pan);
                //lastHoveredColor = blockMap[index].Color;
                Color _h = blockMap[index].Color;
                lastHoveredColor = _h != CurrentViewColor && _h != Color.White ? blockMap[index].Color : viewedColor;
                lastHovered = index;
            }
            else if (lastHoveredColor == Color.Black && index >= 0)
                lastHoveredColor = blockMap[index].Color;

            hoveredIndex = index;
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)// Text = $"{e.Button}";
            {
                if (BamDispPan.Visible && index >= 0 && blockMap[index].Tip != string.Empty)
                {
                    dispEncoding = e.Button == MouseButtons.Left;
                    DisplaySector(index, dispEncoding);
                }
            }
            if (index >= 0 && blockMap[index].Tip != string.Empty)
            {
                try
                {
                    Update_BlockMap(blockMap[index].Track, blockMap[index].Sector, hiLight);
                    Point localPos = Blk_pan.PointToClient(Cursor.Position);
                    string _tip = blockMap[index].Tip + (index == viewedIndex ? "\n*Currently Viewing" : string.Empty);
                    tips.Show(_tip, lastHoveredButton, localPos.X + 25, localPos.Y + 25);
                    //tips.Show(blockMap[index].Tip, lastHoveredButton, localPos.X + 25, localPos.Y + 25);
                }
                catch { }
            }
        }

        private void PaintBAM(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (var rectInfo in blockMap)
            {
                if (rectInfo != null)
                {
                    Rectangle tempRect = new Rectangle(rectInfo.Rect.X, rectInfo.Rect.Y, rectInfo.Rect.Width - 1, rectInfo.Rect.Height - 1);
                    if (e.ClipRectangle.IntersectsWith(tempRect)) // Only draw if within the invalidated area
                    {
                        using (Brush brush = new SolidBrush(rectInfo.Color))
                        {
                            g.FillRectangle(brush, tempRect);
                        }
                    }
                }
            }
        }
    }
}