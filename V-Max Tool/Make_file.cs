using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace V_Max_Tool
{
    public partial class Form1 : Form
    {
        void Make_NIB()
        {
            if (!Directory.Exists($@"{dirname}\Output")) Directory.CreateDirectory($@"{dirname}\Output");
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            write.Write(nib_header);
            for (int i = 0; i < tracks; i++) write.Write(NDA.Track_Data[i]);
            try
            {
                File.WriteAllBytes($@"{dirname}\Output\{fname}{fnappend}{fext}", buffer.ToArray());
            }
            catch (Exception ex)
            {
                nib_error = true;
                nib_err_msg = ex.Message;
            }
            buffer.Close();
            write.Close();
        }

        void Make_G64()
        {
            if (!Directory.Exists($@"{dirname}\Output")) Directory.CreateDirectory($@"{dirname}\Output");
            var buffer = new MemoryStream();
            var write = new BinaryWriter(buffer);
            byte[] head = Encoding.ASCII.GetBytes("GCR-1541");
            byte z = 0;
            short m = Convert.ToInt16(NDG.Track_Length.Max());
            if (m < 7928) m = 7928;
            write.Write(head);
            write.Write(z);
            write.Write((byte)84);
            write.Write(m);
            int offset = 684;
            int th = 0;
            int[] td = new int[84];
            if (tracks > 42) Big(); else Small();
            for (int i = 0; i < 84; i++) write.Write(td[i]);
            for (int i = 0; i < tracks; i++)
            {
                if (NDG.Track_Length[i] > 6000)
                {
                    write.Write((short)NDG.Track_Length[i]);
                    if (NDG.Track_Data[i].Length < m) write.Write(NDG.Track_Data[i]);
                    else
                    {
                        byte[] t = new byte[m];
                        Array.Copy(NDG.Track_Data[i], 0, t, 0, m);
                        write.Write(t);
                    }
                    var o = m - NDG.Track_Length[i];
                    if (o > 0) for (int j = 0; j < o; j++) write.Write((byte)0);
                }
            }
            try
            {
                File.WriteAllBytes($@"{dirname}\Output\{fname}{fnappend}.g64", buffer.ToArray());
            }
            catch (Exception ex)
            {
                g64_error = true;
                g64_err_msg = ex.Message;
            }

            void Big() // 84 track nib file
            {
                for (int i = 0; i < 84; i++)
                {
                    if (i < NDG.Track_Data.Length && NDG.Track_Length[i] > 6000)
                    {
                        write.Write((int)offset + th);
                        th += 2;
                        offset += m;
                        td[i] = 3 - Get_Density(NDG.Track_Data[i].Length);
                    }
                    else write.Write((int)0);
                }
            }

            void Small() // 42 track nib file
            {
                int r = 0;
                for (int i = 0; i < 42; i++)
                {
                    if (i < NDG.Track_Data.Length && NDG.Track_Length[i] > 0)
                    {
                        write.Write((int)offset + th);
                        th += 2;
                        offset += m;
                        td[r] = 3 - Get_Density(NDG.Track_Data[i].Length);
                        r++; td[r] = 0; r++;
                    }
                    else write.Write((int)0);
                    write.Write((int)0);
                }
            }
        }
    }
}