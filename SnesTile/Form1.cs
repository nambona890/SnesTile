using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;

namespace SnesTile
{
    public partial class Form1 : Form
    {
        private byte[] tileBuffer = new byte[0x10000];
        private byte[,] tileBMP = new byte[128,128];
        private Color[] palette = new Color[256];
        
        public Form1()
        {
            InitializeComponent();
        }

        private void graphicsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadGFXDialog.Filter = "Raw graphics file (*.bin)|*.bin|All files (*.*)|*.*";
            loadGFXDialog.FilterIndex = 2;
            loadGFXDialog.Title = "Load Graphics";
            if (loadGFXDialog.ShowDialog() == DialogResult.OK)
            {
                Stream tempFile = loadGFXDialog.OpenFile();
                byte[] tempBuffer = new byte[1024 * 128];
                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while((read = tempFile.Read(tempBuffer,0,tempBuffer.Length))>0)
                    {
                        ms.Write(tempBuffer, 0, read);
                    }
                    tileBuffer = ms.ToArray();
                }
                tileBMP = BPL2BMP(tileBuffer);
                snesCanvas.Invalidate();
            }
        }

        private void paletteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadGFXDialog.Filter = "Raw palette file (*.mw3)|*.mw3|.PAL file (*.pal)|*.pal|All files (*.*)|*.*";
            loadGFXDialog.FilterIndex = 3;
            loadGFXDialog.Title = "Load Palette";
            if (loadGFXDialog.ShowDialog() == DialogResult.OK)
            {
                Stream tempFile = loadGFXDialog.OpenFile();
                byte[] tempBuffer = new byte[1024];
                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while ((read = tempFile.Read(tempBuffer, 0, tempBuffer.Length)) > 0)
                    {
                        ms.Write(tempBuffer, 0, read);
                    }
                    tempBuffer = ms.ToArray();
                }
                if(Path.GetExtension(loadGFXDialog.FileName).Contains(".pal"))
                {
                    LoadPal(tempBuffer);
                }
                else
                {
                    LoadMW3(tempBuffer);
                }
                palDisplay.Invalidate();
                snesCanvas.Invalidate();
            }
        }

        private void LoadPal(byte[] buffer)
        {
            palette = new Color[256];
            for (int i = 0, j = 0; i < 256; i++)
            {
                byte red = buffer[j++];
                byte green = buffer[j++];
                byte blue = buffer[j++];
                palette[i] = Color.FromArgb(red, green, blue);
            }
        }

        private void LoadMW3(byte[] buffer)
        {
            palette = new Color[256];
            for (int i = 0, j = 0; i < 256; i++)
            {
                ushort tmpColor = Convert.ToUInt16((buffer[j++]) | buffer[j++] << 8);
                byte red = Convert.ToByte((tmpColor & 0x1F) << 3);
                byte green = Convert.ToByte((tmpColor & 0x3E0) >> 2);
                byte blue = Convert.ToByte((tmpColor & 0x7C00) >> 7);
                palette[i] = Color.FromArgb(red, green, blue);
            }
        }

        private byte[,] BPL2BMP(byte[] buffer)
        {
            byte[,] indexBMP = new byte[128,128];
            for(int i=0,j=0;i<Math.Min(buffer.Length,0x2000);)
            {
                for(int k=0;k<8;k++)
                {
                    byte[] row = new byte[8];
                    byte bp1 = buffer[i];
                    byte bp2 = buffer[i+1];
                    byte bp3 = buffer[i+16];
                    byte bp4 = buffer[i+17];

                    row[7] = Convert.ToByte((bp1 & 1) | ((bp2 & 1) << 1) | ((bp3 & 1) << 2) | ((bp4 & 1) << 3));
                    row[6] = Convert.ToByte(((bp1 & 2) >> 1) | ((bp2 & 2)) | ((bp3 & 2) << 1) | ((bp4 & 2) << 2));
                    row[5] = Convert.ToByte(((bp1 & 4) >> 2) | ((bp2 & 4) >> 1) | ((bp3 & 4)) | ((bp4 & 4) << 1));
                    row[4] = Convert.ToByte(((bp1 & 8) >> 3) | ((bp2 & 8) >> 2) | ((bp3 & 8) >> 1) | ((bp4 & 8)));
                    row[3] = Convert.ToByte(((bp1 & 16) >> 4) | ((bp2 & 16) >> 3) | ((bp3 & 16) >> 2) | ((bp4 & 16) >> 1));
                    row[2] = Convert.ToByte(((bp1 & 32) >> 5) | ((bp2 & 32) >> 4) | ((bp3 & 32) >> 3) | ((bp4 & 32) >> 2));
                    row[1] = Convert.ToByte(((bp1 & 64) >> 6) | ((bp2 & 64) >> 5) | ((bp3 & 64) >> 4) | ((bp4 & 64) >> 3));
                    row[0] = Convert.ToByte(((bp1 & 128) >> 7) | ((bp2 & 128) >> 6) | ((bp3 & 128) >> 5) | ((bp4 & 128) >> 4));
                    i += 2;
                    int curY = (GetBPLY(i) * 8) + k;
                    for (int l = 0; l < 8; l++)
                    {
                        int curX = (GetBPLX(i) * 8) + l;
                        indexBMP[curX,curY] = row[l];
                    }
                }
                i += 16;
            }

            return indexBMP;
        }

        private int GetBPLY(int loc)
        {
            return loc >> 9;
        }

        private int GetBPLX(int loc)
        {
            return (loc >> 5) & 0xf;
        }

        private Bitmap BMPIndex2RGB(byte[,] indexBMP,Color[] palette)
        {
            Bitmap bmp = new Bitmap(128, 128);
            for(int i=0;i<128;i++)
            {
                for(int j=0;j<128;j++)
                {
                    bmp.SetPixel(i, j, palette[indexBMP[i, j]]);
                }
            }
            return bmp;
        }

        private void snesCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (tileBMP.Length > 0 && palette.Length > 0)
            {

                Bitmap canvas = BMPIndex2RGB(tileBMP, palette);
                e.Graphics.FillRectangle(Brushes.Black, 0, 0, 384, 384);
                e.Graphics.DrawImage(canvas, 0, 0, 384, 384);
            }
        }

        private void palDisplay_Paint(object sender, PaintEventArgs e)
        {
            for(int i=0;i<16;i++)
            {
                for(int j=0;j<16;j++)
                {
                    Brush b = new SolidBrush(palette[(i * 16) + j]);
                    e.Graphics.FillRectangle(b, j * 16, i * 16, 16, 16);
                }
            }
        }
    }
}
