using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        bool interp = false;

        void Draw_Flat_Tracks(int w, bool chg_itrp)
        {
            var d = 0;
            if (!chg_itrp)
            {
                double ht;
                bool halftracks = false;
                if (tracks > 42)
                {
                    ht = 0.5;
                    halftracks = true;
                }
                else ht = 0;
                Disk_Image_Large.Image = new Bitmap(8192, panPic2.Height - 16);
                for (int i = 0; i < tracks; i++)
                {
                    if (w == 0)
                    {
                        if (NDG.Track_Length[i] > 0)
                        {
                            d = Get_Density(NDG.Track_Data[i].Length);
                            Draw_Track(NDG.Track_Data[i], (int)ht, 0, 0, NDS.cbm[i], NDS.v2info[i]);
                            Disk_Image.Image = Resize_Image(Disk_Image_Large.Image, panPic.Width, panPic.Height - 16, false);
                        }
                    }
                    if (w == 1)
                    {
                        var ds = NDS.D_Start[i];
                        var de = NDS.D_End[i];
                        if (NDS.cbm[i] == 1) { ds >>= 3; de >>= 3; }
                        if (NDS.Track_Length[i] > 0)
                        {
                            Draw_Track(NDS.Track_Data[i], (int)ht, ds, de, NDS.cbm[i], NDS.v2info[i]);
                            Disk_Image.Image = Resize_Image(Disk_Image_Large.Image, panPic.Width, panPic.Height - 16, false);
                        }
                    }
                    if (halftracks) ht += .5; else ht += 1;
                }
            }
            else Disk_Image.Image = Resize_Image(Disk_Image_Large.Image, panPic.Width, panPic.Height - 16, false);

            void Draw_Track(byte[] data, int trk, int s, int e, int tf, byte[] v2i)
            {
                byte[] tdata = new byte[data.Length];
                Array.Copy(data, 0, tdata, 0, data.Length);
                Pen pen; // = new Pen (Color.Green);
                bool v2 = false;
                int t_height = (panPic2.Height / 42) - 4;
                for (int j = 0; j < tdata.Length; j++)
                {
                    if (w == 0)
                    {
                        if (Cap_margins.Checked)
                        {
                            pen = new Pen(Color.FromArgb(tdata[j] / 2, tdata[j] / 2, tdata[j] / 2), 1);
                            if (j <= density[d]) pen = new Pen(Color.FromArgb(30, tdata[j], 30));
                            if (j > density[d] && j < density[d] + 5) pen = new Pen(Color.FromArgb(tdata[j], tdata[j], 30));
                        }
                        else pen = new Pen(Color.FromArgb(30, tdata[j], 30));
                        if (tf == 2 && tdata[j] == v2i[0]) v2 = true;
                        if (v2 && tdata[j] == v2i[1]) v2 = false;
                        if (Show_sec.Checked && ((tf == 3 && tdata[j] == 0x49) || v2)) pen = new Pen(Color.FromArgb(30, 30, 255));
                    }
                    else
                    {
                        if (Cap_margins.Checked)
                        {
                            if (j < s || j > e) pen = new Pen(Color.FromArgb(tdata[j], tdata[j], 50));
                            else pen = new Pen(Color.FromArgb(30, tdata[j], 30));
                        }
                        else pen = new Pen(Color.FromArgb(30, tdata[j], 30));
                        if (tf == 2 && tdata[j] == v2i[0]) v2 = true;
                        if (v2 && tdata[j] == v2i[1]) v2 = false;
                        if (Show_sec.Checked && ((tf == 3 && tdata[j] == 0x49) || v2)) pen = new Pen(Color.FromArgb(30, 30, 255));
                    }
                    int x1 = j;
                    int y1 = 0 + (trk * ((panPic.Height - 16) / 42));
                    int x2 = j;
                    int y2 = t_height + (trk * ((panPic.Height - 16) / 42));
                    using (var graphics = Graphics.FromImage(Disk_Image_Large.Image))
                    {
                        graphics.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
            }
        }

        Image Resize_Image(Image temp, int width, int height, bool preserveAspectRatio)
        {
            int newWidth;
            int newHeight;
            if (preserveAspectRatio)
            {
                int originalWidth = temp.Width;
                int originalHeight = temp.Height;
                float percentWidth = (float)width / (float)originalWidth;
                float percentHeight = (float)height / (float)originalHeight;
                float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
                newWidth = (int)(originalWidth * percent);
                newHeight = (int)(originalHeight * percent);
            }
            else
            {
                newWidth = width;
                newHeight = height;
            }
            Image newImage = new Bitmap(newWidth, newHeight);
            using (Graphics graphicsHandle = Graphics.FromImage(newImage))
            {
                if (!interp) graphicsHandle.InterpolationMode = InterpolationMode.NearestNeighbor;
                else graphicsHandle.InterpolationMode = InterpolationMode.High;
                graphicsHandle.DrawImage(temp, 0, 0, newWidth, newHeight);
            }
            return newImage;
        }

        void Draw_Disk()
        {
            int width = 1000;
            int height = 1000;
            int track = 0;
            int i = 0;
            int j = 0;
            int k = 0;
            int x = width / 2;
            int y = height / 2;
            int r = 490;
            int len;
            Color col;
            Bitmap disk = new Bitmap(width, height);
            Make_Disk(disk);

            while (r > 80 && track < tracks)
            {
                if (NDG.Track_Length[track] > 0)
                {
                    len = NDG.Track_Length[track];
                    for (i = 0; i < NDG.Track_Data[track].Length; i++)
                    {
                        col = Color.FromArgb(0, NDG.Track_Data[track][i], 0);
                        Draw_Arc(disk, track, x, y, r + j, i, col);
                    }

                }
                r -= 5;
                track += 1;
            }
            Disk_Image.Image = Resize_Image(disk, panPic.Width, panPic.Height - 16, false);

            void Draw_Arc(Bitmap d, int trk, int cx, int cy, int radius, int startAngle, Color color)
            {
                int segments = 22 - track / 4;
                float tempang = (float)(len) / 359.1f;
                float angle = (startAngle / tempang) * 3.14159265f / 180; // initial angle in radians
                float angleInc = 3.14159265f / (180 * segments); // angle increment
                float c, s;
                for (k = 1; k < segments; k++, angle += angleInc)
                {
                    c = (float)Math.Cos(angle);
                    s = (float)Math.Sin(angle);

                    for (j = 0; j < 5; j++) d.SetPixel((int)(cx + (radius + j) * c), (int)(cy + (radius + j) * s), color);
                }



            }

            void Make_Disk(Bitmap d)
            {
                Brush b = new SolidBrush(Color.FromArgb(50, 40, 20));
                Pen p = new Pen(Color.Black, 2);
                using (var g = Graphics.FromImage(d))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.FillEllipse(b, 2.5f, 8, d.Width - 10, d.Height - 10);
                    g.DrawEllipse(p, 2.5f, 8, d.Width - 10, d.Height - 10);
                    b = new SolidBrush(Color.FromArgb(60, 44, 24));
                    g.FillEllipse(b, 275, 275, 450, 450);
                    g.FillEllipse(Brushes.Black, 350, 350, 300, 300);
                    g.FillEllipse(b, 367.5f, 367.5f, 265, 265);
                    b = new SolidBrush(Color.FromArgb(30, 30, 30));
                    g.FillEllipse(b, 380, 380, 240, 240);
                    g.DrawEllipse(p, 380, 380, 240, 240);
                    g.FillEllipse(b, 487.5f, 302.5f, 25, 25);
                    g.DrawEllipse(p, 487.5f, 302.5f, 25, 25);
                    g.DrawLine(p, 499.5f, 10, 499.5f, 275);
                }

            }
        }
    }
}