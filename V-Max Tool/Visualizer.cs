using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        string def_text = "";
        bool interp = false;
        bool Dragging = false;
        bool vm_reverse = false;
        int xPos;
        int yPos;
        private readonly string[] styles = { "Flat Tracks", "Circular Tracks" };
        private readonly string[] Img_Quality = { "Low", "Medium", "High", "Ultra", "Insane!", "Atomic" };
        FastBitmap flat;
        FastBitmap disk;
        private readonly Brush cbm_brush = new SolidBrush(Color.FromArgb(200, 67, 200));
        private readonly Brush ldr_brush = new SolidBrush(Color.FromArgb(133, 133, 200));
        private readonly Brush vmx_brush = new SolidBrush(Color.FromArgb(30, 200, 30));
        private readonly Color Write_face = Color.FromArgb(41, 40, 36);
        private readonly Color Inner_face = Color.FromArgb(50, 49, 44);


        void Draw_Flat_Tracks(int w, bool chg_itrp)
        {
            string ext = "";
            var d = 0;
            Font font = new Font("Ariel", 11);
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
                //Disk_Image_Large.Image = new Bitmap(8192, panPic2.Height - 16);
                flat = new FastBitmap(8192, panPic2.Height - 16);
                FastBitmap t = new FastBitmap(flat.Width, flat.Height);
                panPic2.Size = new Size(8192, p2_def);
                for (int i = 0; i < tracks; i++)
                {
                    if (w == 0)
                    {
                        if (NDG.Track_Length[i] > min_t_len)
                        {
                            d = Get_Density(NDG.Track_Data[i].Length);
                            t = Draw_Track(flat, NDG.Track_Data[i], (int)ht, 0, 0, NDS.cbm[i], NDS.v2info[i]);
                            ext = "(flat_tracks).g64";
                        }
                    }
                    if (w == 1)
                    {
                        var ds = NDS.D_Start[i];
                        var de = NDS.D_End[i];
                        if (NDS.cbm[i] == 1) { ds >>= 3; de >>= 3; }
                        if (NDS.Track_Length[i] > min_t_len)
                        {
                            t = Draw_Track(flat, NDS.Track_Data[i], (int)ht, ds, de, NDS.cbm[i], NDS.v2info[i]);
                            ext = $"(flat_tracks){fext}";
                        }
                    }
                    if (halftracks) ht += .5; else ht += 1;
                }
                Disk_Image_Large.Image = Resize_Image(t.Bitmap, flat.Width, flat.Height, false);
                Disk_Image.Image = Resize_Image(Disk_Image_Large.Image, panPic.Width, panPic.Height - 16, false);

                Add_Text(Disk_Image_Large.Image, $"{fname}{fnappend}{ext}", Color.FromArgb(40, 40, 40),
                    Brushes.White, font, 20, Disk_Image_Large.Height - 20, 600, Disk_Image_Large.Height);
                Add_Text(Disk_Image.Image, $"{fname}{fnappend}{ext}", Color.FromArgb(40, 40, 40),
                    Brushes.White, font, 20, Disk_Image.Height - 40, 600, Disk_Image.Height);
                def_text = $"{fname}{fnappend}{ext}";
                t.Dispose();
            }
            else
            {
                Disk_Image.Image = Resize_Image(Disk_Image_Large.Image, panPic.Width, panPic.Height - 16, false);
                Add_Text(Disk_Image.Image, def_text, Color.FromArgb(40, 40, 40),
                    Brushes.White, font, 20, Disk_Image.Height - 40, 600, Disk_Image.Height);
            }
            GC.Collect();
            flat.Dispose();

            FastBitmap Draw_Track(FastBitmap bmp, byte[] data, int trk, int s, int e, int tf, byte[] v2i)
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
                    using (var graphics = Graphics.FromImage(bmp.Bitmap))
                    {
                        graphics.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
                return bmp;
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
            try
            {
                using (Graphics graphicsHandle = Graphics.FromImage(newImage))
                {
                    if (!interp) graphicsHandle.InterpolationMode = InterpolationMode.NearestNeighbor;
                    else graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphicsHandle.DrawImage(temp, 0, 0, newWidth, newHeight);
                }
            }
            catch { }
            return newImage;
        }

        void Draw_Circular_Tracks()
        {
            int q = 1000;
            Invoke(new Action(() =>
            {
                Save_Image.Visible = false;
                Img_zoom.Enabled = false;
                Img_style.Enabled = false;
                Disk_Image_Large.Enabled = false;
                Reset_img_pos.Visible = false;
                q = (Img_Q.SelectedIndex + 1) * 1000;
            }));
            interp = true;
            int skp = 1;
            if (tracks < 43) skp = 2;
            string fi_ext = ".g64";
            string fi_nam = $"{fname}{fnappend}";
            byte[] t_data = new byte[0];
            bool v2 = false;
            int width = q;
            int height = q;
            int track = 0;
            int i;
            int de;
            int j = 0;
            int k;
            int x = width / 2;
            int y = height / 2;
            float m = (width / 1000);
            int r = (int)((width / 2) / 1.0316368638f);
            r = (width / 2) - (int)(20 * m);
            int len;
            int t_width = (int)(3f * m);
            Color col;
            disk = new FastBitmap(width, height);
            if (Src_view.Checked) { fi_ext = ".nib"; fi_nam = $"{fname}"; }
            Draw_Disk(disk);

            while (r > 80 && track < tracks)
            {
                if (NDG.Track_Length[track] > min_t_len)
                {
                    v2 = false;
                    if (Out_view.Checked)
                    {
                        t_data = new byte[NDG.Track_Length[track]];
                        Array.Copy(NDG.Track_Data[track], 0, t_data, 0, t_data.Length);
                    }
                    if (Src_view.Checked)
                    {
                        if (NDS.cbm[track] == 1 && (NDS.D_End[track] - NDS.D_Start[track]) >> 3 >= min_t_len)
                        {
                            t_data = new byte[(NDS.D_End[track] - NDS.D_Start[track]) >> 3];
                            Array.Copy(NDS.Track_Data[track], NDS.D_Start[track] >> 3, t_data, 0, (NDS.D_End[track] - NDS.D_Start[track]) >> 3);
                        }
                        else
                        {
                            t_data = new byte[NDS.Track_Data[track].Length];
                            Array.Copy(NDS.Track_Data[track], 0, t_data, 0, NDS.Track_Data[track].Length);
                        }
                        if ((NDS.cbm[track] > 1 && NDS.cbm[track] < 5) && NDS.D_End[track] - NDS.D_Start[track] > min_t_len)
                        {
                            t_data = new byte[NDS.D_End[track] - NDS.D_Start[track]];
                            Array.Copy(NDS.Track_Data[track], NDS.D_Start[track] >> 3, t_data, 0, (NDS.D_End[track] - NDS.D_Start[track]));
                        }
                    }
                    len = t_data.Length;
                    if (len > min_t_len)
                    {
                        de = Get_Density(len);
                        for (i = 0; i < t_data.Length; i++)
                        {
                            if (vm_reverse) // && NDS.cbm[track] > 1)
                            {
                                var sub = 255;
                                if (NDS.cbm[track] == 1) col = Color.FromArgb(t_data[i], (int)(t_data[i] / 3), t_data[i]);
                                else
                                {
                                    if (t_data[i] == 0) sub = 0; if (t_data[i] == 255) sub = 255 + 255;
                                    col = Color.FromArgb(30, sub - t_data[i], 30);
                                    if (NDS.cbm[track] == 4) col = Color.FromArgb((int)(t_data[i] / 1.5f), (int)(t_data[i] / 1.5f), t_data[i]);
                                }
                            }
                            else col = Color.FromArgb(30, t_data[i], 30);
                            if (Cap_margins.Checked)
                            {
                                Color bak = col;
                                col = Color.FromArgb(t_data[i] / 2, t_data[i] / 2, t_data[i] / 2);
                                if (!vm_reverse)
                                {
                                    if (i <= density[de]) col = Color.FromArgb(30, t_data[i], 30);
                                    if (i > density[de] && i < density[de] + 5) col = Color.FromArgb(t_data[i], t_data[i], 30);
                                }
                                else
                                {
                                    if (i <= density[de]) col = bak;
                                    if (i > density[de] && i < density[de] + 5) col = Color.FromArgb(t_data[i], t_data[i], 30);
                                }
                            }
                            if (NDS.cbm[track] == 2 && t_data[i] == NDS.v2info[track][0]) v2 = true;
                            if (v2 && t_data[i] == NDS.v2info[track][1]) v2 = false;
                            if (Show_sec.Checked && ((NDS.cbm[track] == 3 && t_data[i] == 0x49) || v2)) col = Color.FromArgb(30, 30, 255);
                            Draw_Arc(disk, x, y, r + j, i, col);
                        }
                        this.Invoke(new Action(() =>
                        {
                            try
                            {
                                Disk_Image.Image = Resize_Image(disk.Bitmap, panPic.Width, panPic.Height - 16, false);
                                Disk_Image_Large.Image = disk.Bitmap;
                                Disk_Image_Large.Refresh();
                                Disk_Image.Refresh();
                            }
                            catch { }
                        }));
                    }
                }
                r -= (t_width * skp); // 5;
                track += 1;
            }
            Invoke(new Action(() =>
            {
                panPic2.Size = new Size(width, height);
                Disk_Image_Large.Image = Resize_Image(disk.Bitmap, disk.Width, disk.Height, false);
                Disk_Image.Image = Resize_Image(disk.Bitmap, panPic.Width, panPic.Height - 16, false);
                interp = false;
                Save_Image.Visible = true;
                Img_zoom.Enabled = true;
                Disk_Image_Large.Enabled = true;
                Img_style.Enabled = true;
                Reset_img_pos.Visible = true;
                disk.Dispose();
            }));
            GC.Collect();

            void Draw_Arc(FastBitmap d, int cx, int cy, int radius, int startAngle, Color color)
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
                    for (j = 0; j < (int)(t_width + m); j++) d.SetPixel((int)(cx + (radius + j) * c), (int)(cy + (radius + j) * s), color);
                }
            }

            void Draw_Disk(FastBitmap d)
            {
                Brush b = new SolidBrush(Write_face);
                Pen p = new Pen(Color.Black, 2);
                using (var g = Graphics.FromImage(d.Bitmap))
                {
                    RectangleF rect = new RectangleF(0, 0, width, height);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 0)), rect);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    // Draw Disk surface
                    g.FillEllipse(b, 3.5f, 6, d.Width - 10, d.Height - 10);
                    g.DrawEllipse(p, 3.5f, 6, d.Width - 10, d.Height - 10);
                    // Draw inner non-writable surface of disk
                    b = new SolidBrush(Inner_face);
                    g.FillEllipse(b, (float)(275 * m), (float)(275 * m), (float)(450 * m), (float)(450 * m));
                    // Draw inner ring of disk (also used for image name text)
                    g.FillEllipse(new SolidBrush(Color.FromArgb(7,7,7)), (float)(350 * m), (float)(350 * m), (float)(300 * m), (float)(300 * m));
                    // Draw remaing disk surface until center hole
                    g.FillEllipse(b, (float)(367.5 * m), (float)(367.5 * m), (float)(265 * m), (float)(265 * m));
                    b = new SolidBrush(Color.FromArgb(0, 0, 0));
                    // Draw index hole
                    g.FillEllipse(b, (float)(380 * m), (float)(380 * m), (float)(240 * m), (float)(240 * m));
                    g.DrawEllipse(p, (float)(380 * m), (float)(380 * m), (float)(240 * m), (float)(240 * m));
                    g.FillEllipse(b, (float)(672.5 * m), (float)(487.5 * m), (float)(20 * m), (float)(20 * m));
                    g.DrawEllipse(p, (float)(672.5 * m), (float)(487.5 * m), (float)(20 * m), (float)(20 * m));
                }
                // Print File name on the disk image
                Brush bsh = new SolidBrush(Color.White);
                Font fnt = new Font("Arial", (float)(11.6 * m), FontStyle.Regular);
                DrawCurvedText(Graphics.FromImage(disk.Bitmap), $"{fi_nam}{fi_ext}", new Point((int)(500 * m), (int)(500 * m)), (float)(128.34 * m), 0f, fnt, bsh, false);
                // Print rotation indicator on the disk image
                bsh = new SolidBrush(Color.Yellow);
                fnt = new Font("Arial", (float)(16 * m), FontStyle.Regular);
                DrawCurvedText(Graphics.FromImage(disk.Bitmap), $"\u2192 noitatoR", new Point((int)(513 * m), (int)(503 * m)), (float)(181.67 * m), 1.45f, fnt, bsh, true);

                if (vm_reverse)
                {
                    Add_Text(disk.Bitmap, "CBM", Color.FromArgb(0, 40, 40, 40), cbm_brush, new Font("Ariel", 11 * m), (int)(1 * m), (int)(1 * m), (int)(60 * m), (int)(17 * m));
                    Add_Text(disk.Bitmap, "Loader", Color.FromArgb(0, 40, 40, 40), ldr_brush, new Font("Ariel", 11 * m), (int)(1 * m), (int)(18 * m), (int)(60 * m), (int)(17 * m));
                    Add_Text(disk.Bitmap, "V-Max!", Color.FromArgb(0, 40, 40, 40), vmx_brush, new Font("Ariel", 11 * m), (int)(1 * m), (int)(35 * m), (int)(60 * m), (int)(17 * m));
                }
            }
        }

        void Add_Text(Image temp, string text, Color c,Brush brsh, Font fnt, int x1, int y1, int x2, int y2)
        {
            Graphics g = Graphics.FromImage(temp);
            Brush b = new SolidBrush(c); // (Color.FromArgb(40, 40, 40));
            RectangleF rectf = new RectangleF(x1, y1, x2, y2);
            RectangleF rectg = new RectangleF(x1, y1, x2, y2);
            g.FillRectangle(b, rectg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.Bicubic;
            g.PixelOffsetMode = PixelOffsetMode.Default;
            g.DrawString($"{text}", fnt, brsh, rectf);
        }

        private void DrawCurvedText(Graphics g, string text, Point center, float distFromCenterToBase, float radiansToTextCenter, Font font, Brush brush, bool rev)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var circleCircumference = (float)(Math.PI * 2 * distFromCenterToBase);
            var characterWidths = GetCharacterWidths(g, text, font).ToArray();
            var characterHeight = g.MeasureString(text, font).Height;
            var textLength = characterWidths.Sum();
            float fractionOfCircumference = textLength / circleCircumference;
            float currentCharacterRadians = radiansToTextCenter - (float)(Math.PI * fractionOfCircumference);
            if (rev) currentCharacterRadians = radiansToTextCenter + (float)(Math.PI * fractionOfCircumference);
            for (int characterIndex = 0; characterIndex < text.Length; characterIndex++)
            {
                char @char = text[characterIndex];
                float x = (float)(distFromCenterToBase * Math.Sin(currentCharacterRadians));
                float y = -(float)(distFromCenterToBase * Math.Cos(currentCharacterRadians));
                using (GraphicsPath characterPath = new GraphicsPath())
                {
                    characterPath.AddString(@char.ToString(), font.FontFamily, (int)font.Style, font.Size, Point.Empty, StringFormat.GenericTypographic);
                    var pathBounds = characterPath.GetBounds();
                    var transform = new Matrix();
                    transform.Translate(center.X + x, center.Y + y);
                    var rotationAngleDegrees = currentCharacterRadians * 180F / (float)Math.PI; // - 180F;
                    if (rev) rotationAngleDegrees = currentCharacterRadians * 180F / (float)Math.PI - 180F;
                    transform.Rotate(rotationAngleDegrees);
                    transform.Translate(-pathBounds.Width / 2F, -characterHeight);
                    characterPath.Transform(transform);
                    g.FillPath(brush, characterPath);
                }

                if (characterIndex != text.Length - 1)
                {
                    var distanceToNextChar = (characterWidths[characterIndex] + characterWidths[characterIndex + 1]) / 2F;
                    float charFractionOfCircumference = distanceToNextChar / circleCircumference;
                    currentCharacterRadians += charFractionOfCircumference * (float)(2F * Math.PI);
                }
            }
        }

        private IEnumerable<float> GetCharacterWidths(Graphics graphics, string text, Font font)
        {
            var spaceLength = graphics.MeasureString(" ", font, Point.Empty, StringFormat.GenericDefault).Width;
            return text.Select(c => c == ' ' ? spaceLength : graphics.MeasureString(c.ToString(), font, Point.Empty, StringFormat.GenericTypographic).Width);
        }

        private void Save_Image_Click(object sender, EventArgs e)
        {
            string Style = $"({styles[Img_style.SelectedIndex].Replace(" ", "_").ToLower()})"; //.ToString();
            Save_Dialog.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp";
            Save_Dialog.Title = "Save Image File";
            Save_Dialog.FileName = $"{fname}{fnappend}{Style}.jpg";
            Save_Dialog.ShowDialog();
            string fs = Save_Dialog.FileName;
            if (Img_style.SelectedIndex == 0) Save_Flat(Save_Dialog.FilterIndex);
            else Save_Circular(Save_Dialog.FilterIndex);

            void Save_Flat(int ft)
            {
                if (Img_zoom.Checked)
                {
                    if (ft == 1) Disk_Image_Large.Image.Save(fs, ImageFormat.Jpeg);
                    if (ft == 2) Disk_Image_Large.Image.Save(fs, ImageFormat.Bmp);
                }
                else
                {
                    Image flat = Resize_Image(Disk_Image_Large.Image, 1920, 1080, false);
                    Add_Text(flat, def_text, Color.FromArgb(0, 0, 0), Brushes.White, new Font("Ariel", 11),
                        20, Disk_Image_Large.Height - 20, 600, Disk_Image_Large.Height);
                    if (ft == 1) flat.Save(fs, ImageFormat.Jpeg);
                    if (ft == 2) flat.Save(fs, ImageFormat.Bmp);
                }
            }

            void Save_Circular(int ft)
            {
                if (ft == 2) Disk_Image_Large.Image.Save(fs, ImageFormat.Bmp);
                if (ft == 1) Disk_Image_Large.Image.Save(fs, ImageFormat.Jpeg);
            }
        }

        private void Disk_Image_Large_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Dragging = label3.Visible = true;
                xPos = e.X;
                yPos = e.Y;
            }
        }

        private void Disk_Image_Large_MouseMove(object sender, MouseEventArgs e)
        {
            if (Dragging && sender is Control c)
            {
                c.Top = e.Y + c.Top - yPos;
                c.Left = e.X + c.Left - xPos;
                var x = c.Left; var y = c.Top;
                x = -x; y = -y;
                label3.Text = $"x:({x}) y:({y})";
            }
        }

        private void Disk_Image_Large_MouseUp(object sender, MouseEventArgs e)
        {
            Dragging = label3.Visible = false;
        }

        private void Reposition_ImageButton(object sender, EventArgs e)
        {
            Disk_Image_Large.Top = 1;
            Disk_Image_Large.Left = 1;
            Reset_img_pos.Checked = false;
        }
    }
}