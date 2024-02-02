using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {

        void Visualize_Flat(int w)
        {
            double ht;
            bool halftracks = false;
            if (tracks > 42)
            {
                ht = 0.5;
                halftracks = true;
            }
            else ht = 0;
            Disk_Image_Large.Image = new Bitmap(8192, 42 * 14);
            //Disk_Image_Large.Image = new Bitmap(8192, out_track.Items.Count * 14);
            var d = 0;
            for (int i = 0; i < tracks; i++)
            {
                if (w == 0)
                {
                    if (NDG.Track_Length[i] > 0)
                    {
                        d = Get_Density(NDG.Track_Data[i].Length);
                        Draw_Track(NDG.Track_Data[i], (int)ht, 0, 0, NDS.cbm[i], NDS.v2info[i]);
                        Image orig = Disk_Image_Large.Image;
                        Disk_Image.Image = ResizeImage(orig, new Size(panPic.Width, panPic.Height));
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
                        Image orig = Disk_Image_Large.Image;
                        Disk_Image.Image = ResizeImage(orig, new Size(panPic.Width, panPic.Height));
                    }
                }
                if (halftracks) ht += .5; else ht += 1;
            }

            Image ResizeImage(Image image, Size size, bool preserveAspectRatio = false)
            {
                int newWidth;
                int newHeight;
                if (preserveAspectRatio)
                {
                    int originalWidth = image.Width;
                    int originalHeight = image.Height;
                    float percentWidth = (float)size.Width / (float)originalWidth;
                    float percentHeight = (float)size.Height / (float)originalHeight;
                    float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
                    newWidth = (int)(originalWidth * percent);
                    newHeight = (int)(originalHeight * percent);
                }
                else
                {
                    newWidth = size.Width;
                    newHeight = size.Height;
                }
                Image newImage = new Bitmap(newWidth, newHeight);
                using (Graphics graphicsHandle = Graphics.FromImage(newImage))
                {
                    graphicsHandle.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
                }
                return newImage;
            }



            void Draw_Track(byte[] data, int trk, int s, int e, int tf, byte[] v2i)
            {
                byte[] tdata = new byte[data.Length];
                Array.Copy(data, 0, tdata, 0, data.Length);
                Pen pen; // = new Pen (Color.Green);
                bool v2 = false;
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
                    int y1 = 0 + (trk * 14);
                    int x2 = j;
                    int y2 = 10 + (trk * 14);
                    using (var graphics = Graphics.FromImage(Disk_Image_Large.Image))
                    {
                        graphics.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
            }
        }
    }
}