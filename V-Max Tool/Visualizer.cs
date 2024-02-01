using System;
using System.Drawing;
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
            Disk_Image.Image = new Bitmap(8192, 42 * 14);
            Disk_Image_Large.Image = new Bitmap(8192, 42 * 14);
            var d = 0;
            for (int i = 0; i < tracks; i++)
            {
                if (w == 0)
                {
                    if (NDG.Track_Length[i] > 0)
                    {
                        d = Get_Density(NDG.Track_Data[i].Length);
                        Draw_Track(NDG.Track_Data[i], (int)ht, 0, 0);
                    }
                }
                if (w == 1)
                {
                    var ds = NDS.D_Start[i];
                    var de = NDS.D_End[i];
                    if (NDS.cbm[i] == 1) { ds = ds >> 3; de = de >> 3; }
                    if (NDS.Track_Length[i] > 0) Draw_Track(NDS.Track_Data[i], (int)ht, ds, de);
                }
                if (halftracks) ht += .5; else ht += 1;
            }

            void Draw_Track(byte[] data, int trk, int s, int e)
            {
                byte[] tdata = new byte[data.Length];
                Array.Copy(data, 0, tdata, 0, data.Length);
                Pen pen = new Pen(Color.Green);
                for (int j = 0; j < tdata.Length; j++)
                {
                    if (w == 0)
                    {
                        pen = new Pen(Color.FromArgb(tdata[j] / 2, tdata[j] / 2, tdata[j] / 2));
                        if (j <= density[d]) pen = new Pen(Color.FromArgb(30, tdata[j], 30));
                        if (j > density[d] && j < density[d] + 5) pen = new Pen(Color.FromArgb(tdata[j], tdata[j], 30));
                    }
                    else
                    {
                        if (j < s || j > e) pen = new Pen(Color.FromArgb(tdata[j], tdata[j], 50));
                        else pen = new Pen(Color.FromArgb(30, tdata[j], 30));
                    }
                    int x1 = j;
                    int y1 = 0 + (trk * 14);
                    int x2 = j;
                    int y2 = 10 + (trk * 14);
                    using (var graphics = Graphics.FromImage(Disk_Image.Image))
                    {
                        graphics.DrawLine(pen, x1, y1, x2, y2);
                    }
                    using (var graphics = Graphics.FromImage(Disk_Image_Large.Image))
                    {
                        graphics.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
            }

        }

    }
}