// ----------------------------------------------------------------------
// <copyright file="PCX.cs" company="none">
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by 
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful, 
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details. 
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>. 
//
// </copyright>
// -----------------------------------------------------------------------
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Ekona;

namespace PETHOSPITALS.Formats
{
    /// <summary>
    /// PCX format handler - ZSoft Paintbrush format
    /// Standard PCX format decoder for 8-bit indexed images
    /// </summary>
    public static class PCX
    {
        public struct PcxHeader
        {
            public byte Manufacturer;   // Always 0x0A
            public byte Version;        // 0-5
            public byte Encoding;       // 1 = RLE
            public byte BitsPerPixel;   // Bits per pixel per plane
            public ushort XMin, YMin;
            public ushort XMax, YMax;
            public ushort HDpi, VDpi;
            public byte[] Colormap;     // 16-color palette (48 bytes)
            public byte Reserved;
            public byte NPlanes;        // Number of color planes
            public ushort BytesPerLine; // Bytes per scanline per plane
            public ushort PaletteInfo;  // 1 = color, 2 = grayscale
            public ushort HScreenSize;
            public ushort VScreenSize;
        }

        public static PcxHeader ReadHeader(BinaryReader br)
        {
            PcxHeader header = new PcxHeader();
            header.Manufacturer = br.ReadByte();
            header.Version = br.ReadByte();
            header.Encoding = br.ReadByte();
            header.BitsPerPixel = br.ReadByte();
            header.XMin = br.ReadUInt16();
            header.YMin = br.ReadUInt16();
            header.XMax = br.ReadUInt16();
            header.YMax = br.ReadUInt16();
            header.HDpi = br.ReadUInt16();
            header.VDpi = br.ReadUInt16();
            header.Colormap = br.ReadBytes(48);
            header.Reserved = br.ReadByte();
            header.NPlanes = br.ReadByte();
            header.BytesPerLine = br.ReadUInt16();
            header.PaletteInfo = br.ReadUInt16();
            header.HScreenSize = br.ReadUInt16();
            header.VScreenSize = br.ReadUInt16();
            br.ReadBytes(54); // Filler to complete 128-byte header
            return header;
        }

        public static Bitmap Decode(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                PcxHeader header = ReadHeader(br);

                int width = header.XMax - header.XMin + 1;
                int height = header.YMax - header.YMin + 1;
                int scanlineLength = header.NPlanes * header.BytesPerLine;

                // Read image data (RLE compressed)
                byte[] imageData = new byte[height * scanlineLength];
                int dataIndex = 0;

                while (dataIndex < imageData.Length && fs.Position < fs.Length - 769)
                {
                    byte b = br.ReadByte();
                    if ((b & 0xC0) == 0xC0)
                    {
                        // RLE run
                        int count = b & 0x3F;
                        byte value = br.ReadByte();
                        for (int i = 0; i < count && dataIndex < imageData.Length; i++)
                        {
                            imageData[dataIndex++] = value;
                        }
                    }
                    else
                    {
                        imageData[dataIndex++] = b;
                    }
                }

                // Read 256-color palette (if present at end of file)
                Color[] palette = new Color[256];
                bool hasPalette = false;

                if (fs.Length - fs.Position >= 769)
                {
                    // Check for palette marker
                    fs.Seek(-769, SeekOrigin.End);
                    byte marker = br.ReadByte();
                    if (marker == 0x0C)
                    {
                        hasPalette = true;
                        for (int i = 0; i < 256; i++)
                        {
                            byte r = br.ReadByte();
                            byte g = br.ReadByte();
                            byte b2 = br.ReadByte();
                            palette[i] = Color.FromArgb(r, g, b2);
                        }
                    }
                }

                // If no 256-color palette, use header palette
                if (!hasPalette)
                {
                    for (int i = 0; i < 16 && i < 256; i++)
                    {
                        palette[i] = Color.FromArgb(
                            header.Colormap[i * 3],
                            header.Colormap[i * 3 + 1],
                            header.Colormap[i * 3 + 2]);
                    }
                }

                // Create bitmap
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

                if (header.BitsPerPixel == 8 && header.NPlanes == 1)
                {
                    // 8-bit indexed
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * header.BytesPerLine + x;
                            if (idx < imageData.Length)
                            {
                                byte colorIndex = imageData[idx];
                                bmp.SetPixel(x, y, palette[colorIndex]);
                            }
                        }
                    }
                }
                else if (header.BitsPerPixel == 8 && header.NPlanes == 3)
                {
                    // 24-bit RGB
                    for (int y = 0; y < height; y++)
                    {
                        int lineStart = y * scanlineLength;
                        for (int x = 0; x < width; x++)
                        {
                            byte r = imageData[lineStart + x];
                            byte g = imageData[lineStart + header.BytesPerLine + x];
                            byte b2 = imageData[lineStart + 2 * header.BytesPerLine + x];
                            bmp.SetPixel(x, y, Color.FromArgb(r, g, b2));
                        }
                    }
                }
                else if (header.BitsPerPixel == 1)
                {
                    // 1-bit or multi-plane
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int byteIndex = y * header.BytesPerLine + (x / 8);
                            int bitIndex = 7 - (x % 8);
                            if (byteIndex < imageData.Length)
                            {
                                int colorIndex = 0;
                                for (int plane = 0; plane < header.NPlanes; plane++)
                                {
                                    int planeByteIndex = y * scanlineLength + plane * header.BytesPerLine + (x / 8);
                                    if (planeByteIndex < imageData.Length)
                                    {
                                        int bit = (imageData[planeByteIndex] >> bitIndex) & 1;
                                        colorIndex |= (bit << plane);
                                    }
                                }
                                bmp.SetPixel(x, y, palette[colorIndex]);
                            }
                        }
                    }
                }
                else
                {
                    // Fallback - just fill with gray
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Gray);
                    }
                }

                return bmp;
            }
        }

        public static Control ShowInfo(IPluginHost pluginHost, sFile file)
        {
            try
            {
                Panel panel = new Panel();
                panel.Dock = DockStyle.Fill;
                panel.AutoScroll = true;

                // Decode and display image
                Bitmap bmp = Decode(file.path);

                PictureBox pictureBox = new PictureBox();
                pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
                pictureBox.Image = bmp;
                pictureBox.Location = new Point(10, 50);

                // Info label
                Label infoLabel = new Label();
                infoLabel.AutoSize = true;
                infoLabel.Location = new Point(10, 10);

                using (FileStream fs = new FileStream(file.path, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    PcxHeader header = ReadHeader(br);
                    int width = header.XMax - header.XMin + 1;
                    int height = header.YMax - header.YMin + 1;
                    infoLabel.Text = string.Format(
                        "PCX Image | {0} x {1} | {2} bpp | {3} planes | Version {4}",
                        width, height, header.BitsPerPixel, header.NPlanes, header.Version);
                }

                // Save button
                Button saveButton = new Button();
                saveButton.Text = "Save as PNG";
                saveButton.Location = new Point(panel.Width - 110, 10);
                saveButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                saveButton.Click += (sender, e) =>
                {
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "PNG Image (*.png)|*.png";
                    sfd.FileName = Path.GetFileNameWithoutExtension(file.name) + ".png";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        bmp.Save(sfd.FileName, ImageFormat.Png);
                        MessageBox.Show("Image saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                panel.Controls.Add(infoLabel);
                panel.Controls.Add(saveButton);
                panel.Controls.Add(pictureBox);

                return panel;
            }
            catch (Exception ex)
            {
                Label errorLabel = new Label();
                errorLabel.Text = "Error loading PCX file: " + ex.Message + "\n" + ex.StackTrace;
                errorLabel.AutoSize = true;
                return errorLabel;
            }
        }
    }
}
