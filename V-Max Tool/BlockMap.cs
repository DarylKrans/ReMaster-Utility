using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        //private readonly Color vmaxv2 = Color.DarkMagenta;
        //private readonly Color vmaxv3 = Color.Green;
        //private readonly Color vmloader = Color.Blue;
        //private readonly Color vorpalnew = Color.DarkCyan;
        //private readonly Color rapidlok = Color.DarkOrange;

        Dictionary<int, Color> colorMap = new Dictionary<int, Color>
        {
            { 0, Color.FromArgb(110, 70, 173) }, { 1, Color.Black }, { 2, Color.DarkMagenta },
            { 3, Color.Green }, { 4, Color.Blue }, { 5, Color.DarkCyan }, { 6, Color.DarkOrange },
            { 7, Color.Blue }, { 8, Color.Blue }, { 9, Color.Blue }, { 10, Color.Brown },
            { 11, Color.Blue }, { 12, Color.Blue }
        };

        //Dictionary<int, Color> colorMap = new Dictionary<int, Color>
        //        {
        //            { 0, Color.FromArgb(110, 70, 173) }, { 1, Color.Black }, { 2, vmaxv2 },
        //            { 3, vmaxv3 }, { 4, vmloader }, { 5, vorpalnew }, { 6, rapidlok },
        //            { 7, Color.Blue }, { 8, Color.Blue }, { 9, Color.Blue }, { 10, Color.Brown },
        //            { 11, Color.Blue }, { 12, Color.Blue }
        //        };

        void BlockMap_Setup()
        {
            Blk_pan.Paint += PaintBAM;
            Blk_pan.MouseMove += Bam_Layout_MouseMove;
            Blk_pan.MouseEnter += Blk_pan_MouseEnter;
            Blk_pan.MouseLeave += Button_MouseLeave;
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

            ErrorBlk.Text = "";
            ErrorBlk.AutoSize = false;
            ErrorBlk.NewText = "Block Error";
            ErrorBlk.ForeColor = Color.Red;
            ErrorBlk.RotateAngle = -90;

            CSTfmt.Text = "";
            CSTfmt.AutoSize = false;
            CSTfmt.NewText = "Custom Format";
            CSTfmt.ForeColor = Color.MediumOrchid;
            CSTfmt.RotateAngle = -90;

            // set Track # labels in Block Map
            int left = 25;
            int top = 27;
            int inc = 15;
            for (int i = 0; i < 41; i++)
            {
                string spc = i < 9 ? " " : string.Empty;
                BlkMap_track[i] = new Label();
                BlkMap_Panel.Controls.Add(BlkMap_track[i]);
                BlkMap_track[i].AutoSize = true;
                BlkMap_track[i].Font = new Font("Courier New", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
                BlkMap_track[i].ForeColor = Color.DarkGray;
                BlkMap_track[i].Location = new Point(left, top + (inc * i));
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
                //BlkMap_sector[i].Text = $"{spc}{i + 1}";
                BlkMap_sector[i].Text = $"{spc}{i}";
                BlkMap_sector[i].BringToFront();
                Blk_pan.Width = spacing + (spacing * i) + 2;
            }
            // set BAM buttons in Block Map

            var spcacing = 2;
            var ht = (Blk_pan.Height / 41) - spcacing;
            var wt = (Blk_pan.Width / 21) - spcacing;
            top = 3;
            left = 2;
            for (int i = 0; i < 41; i++)
            {
                BlkMap_bam[i] = new TaggedRectangle[21];
                for (int j = 0; j < 21; j++)
                {
                    var x = spcacing + (j * (wt + spcacing));
                    var y = spcacing + (i * (ht + spcacing));
                    var track = i + 1;
                    var sector = j + 1;
                    var color = Color.FromArgb(30, 30, 30);
                    var tip = $"track {track} sector {sector}";
                    BlkMap_bam[i][j] = new TaggedRectangle(x, y, wt, ht, i + 1, j);
                    blockMap.Add(new BlockMapInfo(BlkMap_bam[i][j], track, sector, color, tip));
                    Update_BlockMap(track, sector, color, tip);
                    Blk_pan.Invalidate();
                    Blk_pan.Visible = true;
                }
            }
        }

        void Set_BlockMap()
        {
            Blk_pan.Visible = false;
            ResetAllBlocks();
            byte[] bam = GetBam();
            bool vbam = bam != null;
            string usedsec = string.Empty;
            //int max_track = tracks > 42 ? 82 : 41;
            //for (int i = 0; i < Math.Min(tracks, max_track); i++)
            for (int i = 0; i < tracks; i++)
            {
                int trk = tracks > 42 ? (i / 2) : i;
                if (NDS.cbm[i] == 1)
                {
                    int validSectors = Available_Sectors[trk];
                    int sectors = NDS.sectors[i] < validSectors ? validSectors : NDS.sectors[i];

                    int[] c = new int[] { 2, 3, 4, 5, 6 };
                    bool alt = (NDS.cbm.Any(x => c.Any()));
                    int start = trk == 17 || alt ? 0 : NDS.D_Start[i];
                    int index = -1;
                    BitArray tk = new BitArray(Flip_Endian(trk == 17 || alt ? NDG.Track_Data[i] : NDS.Track_Data[i]));
                    for (int j = 0; j < 21; j++)
                    {
                        try { index = blockMap.FindIndex(b => b.Track == trk + 1 && b.Sector == j + 1); }
                        catch { index = -1; }
                        if (j < sectors && index >= 0)
                        {
                            bool valid = j < Available_Sectors[trk];
                            (_, int errorCode, _) = GetSectorWithErrorCode(null, j, true, null, tk, start);
                            //if (trk == 17 && NDS.cbm.Any(x => x == 5) && j > 12)
                            if (trk == 17 && NDS.cbm.Any(x => x == 5) && errorCode != 1)
                            {
                                blockMap[index].Color = Color.FromArgb(100, 200, 200);
                                blockMap[index].Tip = "Vorpal Loader";
                            }
                            else
                            {

                                bool error = errorCode > 1;
                                bool available = BlockAllocStatus(bam, trk, j);
                                usedsec = trk > 34 || !valid ? "* outside BAM range" : !available ? "Block Allocated (Used)" : "Block Available (Free)";
                                usedsec += (error ? $"\nError {c1541error[errorCode]}" : string.Empty);
                                Color color = Color.FromArgb(valid && trk < 35 ? 255 : 130, error ? 200 : 30, error ? 30 : !available ? 200 : 75, 30);
                                blockMap[index].Color = color;
                                blockMap[index].Tip = $"Track {trk + 1} Sector {j}" + (usedsec != "" ? $"\n{usedsec}" : "")
                                    + (errorCode == 1 ? $"\n{ErrorCodes[errorCode]}" : "");
                            }
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
                    //File.WriteAllLines($@"c:\test\track{trk}_errors.txt", e.ToArray());
                }
                else
                {
                    try
                    {
                        var fmt = NDS.cbm[i];
                        if (fmt < secF.Length - 1 && NDG.Track_Data[i] != null)
                        {
                            int sec = Sectors_by_density[Get_Density(NDG.Track_Data[i].Length)];
                            for (int j = 0; j < 21; j++)
                            {
                                int index = blockMap.FindIndex(b => b.Track == trk + 1 && b.Sector == j + 1);
                                Color color = fmt < 2 || fmt == secF.Length - 1 || j >= sec ? Color.FromArgb(30, 100, 100, 100) : Color.FromArgb(200, 100, 30, 100);
                                //Color color = fmt < 2 || fmt == secF.Length - 1 || j >= sec
                                //    ? Color.FromArgb(30, 100, 100, 100) 
                                //    : colorMap.TryGetValue(NDS.cbm[i], out Color clr) ? clr : Color.Black;
                                blockMap[index].Color = color;
                                blockMap[index].Tip = (fmt > 0 && fmt < secF.Length - 1)
                                    ? j < sec ? $"Track {trk + 1} {secF[NDS.cbm[i]]}" :
                                    string.Empty : string.Empty;
                            }
                        }
                    }
                    catch { }
                }
                if (tracks > 42) i++;
            }
            Blk_pan.Visible = true;
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
            }
        }

        private void Panel_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (sender is Panel clickedPanel && clickedPanel.Tag != null)
                {
                    // Retrieve the track and sector from the Tag property
                    var tag = (dynamic)clickedPanel.Tag;
                    int track = tag.Track;
                    int sector = tag.Sector;
                    int index = blockMap.FindIndex(r => r.Contains(e.Location));
                    int actualTrack = tracks > 42 ? track << 1 : track;
                    if (NDS.cbm[actualTrack] == 1 && (track < 35 && sector < Available_Sectors[track]))
                    {
                        Color used = Color.FromArgb(255, 30, 200, 30);
                        Color avail = Color.FromArgb(255, 30, 75, 30);
                        byte[] bam = GetBam();
                        if (bam != null)
                        {
                            bool status = !BlockAllocStatus(bam, track, sector);
                            AllocBlock(bam, track, sector, !BlockAllocStatus(bam, track, sector));
                            blockMap[index].Color = status ? avail : used;
                            string text = blockMap[index].Tip;
                            if (text.Contains("Block Allocated (Used)"))
                            {
                                blockMap[index].Tip = text.Replace("Block Allocated (Used)", "Block Available (Free)");
                            }
                            else if (text.Contains("Block Available (Free)"))
                            {
                                blockMap[index].Tip = text.Replace("Block Available (Free)", "Block Allocated (Used)");
                            }
                            UpdateBam(bam);
                            Default_Dir_Screen();
                            Set_Dir(Get_Disk_Directory());
                        }
                    }
                }
            }
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
            if (index != hoveredIndex) // New rectangle hovered
            {
                hoveredIndex = index;
                if (index >= 0 && blockMap[index].Tip != string.Empty)
                {
                    try
                    {
                        Point pos = Cursor.Position;
                        Point localPos = Blk_pan.PointToClient(pos);
                        tips.Show(blockMap[index].Tip, lastHoveredButton, localPos.X + 25, localPos.Y + 25);
                    }
                    catch { }
                }
                else tips.Hide(Blk_pan);
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