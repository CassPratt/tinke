// ----------------------------------------------------------------------
// <copyright file="TEX.cs" company="none">
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
using System.IO;
using System.Windows.Forms;
using Ekona;

namespace PETHOSPITALS.Formats
{
    /// <summary>
    /// TEX format handler for Pet Hospitals custom texture format
    /// 
    /// Format structure:
    /// - Bytes 0-3:   Texture type (uint32) - determines bit depth
    ///                Type 1 = 8bpp indexed
    ///                Type 3 = 4bpp indexed
    /// - Bytes 4-7:   Width (uint32)
    /// - Bytes 8-11:  Height (uint32)
    /// - Bytes 12-N:  Pixel data (indexed)
    /// - Last bytes:  Palette data (RGB555 format, 2 bytes per color)
    /// </summary>
    public static class TEX
    {
        public struct TexHeader
        {
            public uint Type;
            public uint Width;
            public uint Height;
            public int BitsPerPixel;
            public int PaletteColors;
        }

        public static TexHeader ReadHeader(BinaryReader br)
        {
            TexHeader header = new TexHeader();
            header.Type = br.ReadUInt32();
            header.Width = br.ReadUInt32();
            header.Height = br.ReadUInt32();

            // Determine bits per pixel based on type
            switch (header.Type)
            {
                case 1:
                    header.BitsPerPixel = 8;
                    header.PaletteColors = 256;
                    break;
                case 3:
                    header.BitsPerPixel = 4;
                    header.PaletteColors = 16;
                    break;
                default:
                    header.BitsPerPixel = 8;
                    header.PaletteColors = 256;
                    break;
            }

            return header;
        }

        public static Bitmap Decode(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                TexHeader header = ReadHeader(br);
                int width = (int)header.Width;
                int height = (int)header.Height;

                // Calculate sizes
                int pixelDataSize;
                if (header.BitsPerPixel == 4)
                    pixelDataSize = (width * height) / 2;
                else
                    pixelDataSize = width * height;

                int paletteSize = header.PaletteColors * 2; // RGB555 = 2 bytes per color
                int headerSize = 12;
                int expectedSize = headerSize + pixelDataSize + paletteSize;
                int actualSize = (int)fs.Length;

                // Read pixel data
                byte[] pixelData = br.ReadBytes(pixelDataSize);

                // Read palette (at end of file)
                // Recalculate palette size based on remaining bytes
                int remainingBytes = actualSize - headerSize - pixelDataSize;
                int actualPaletteColors = remainingBytes / 2;
                
                Color[] palette = new Color[Math.Max(actualPaletteColors, header.PaletteColors)];
                
                for (int i = 0; i < actualPaletteColors && i < palette.Length; i++)
                {
                    ushort color555 = br.ReadUInt16();
                    int r = ((color555 >> 0) & 0x1F) * 255 / 31;
                    int g = ((color555 >> 5) & 0x1F) * 255 / 31;
                    int b = ((color555 >> 10) & 0x1F) * 255 / 31;
                    palette[i] = Color.FromArgb(255, r, g, b);
                }

                // Fill remaining palette with black
                for (int i = actualPaletteColors; i < palette.Length; i++)
                {
                    palette[i] = Color.Black;
                }

                // Create bitmap
                Bitmap bmp = new Bitmap(width, height);

                int pixelIndex = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int colorIndex;

                        if (header.BitsPerPixel == 4)
                        {
                            int byteIndex = pixelIndex / 2;
                            if (byteIndex < pixelData.Length)
                            {
                                if ((pixelIndex % 2) == 0)
                                    colorIndex = pixelData[byteIndex] & 0x0F;
                                else
                                    colorIndex = (pixelData[byteIndex] >> 4) & 0x0F;
                            }
                            else
                            {
                                colorIndex = 0;
                            }
                        }
                        else
                        {
                            if (pixelIndex < pixelData.Length)
                                colorIndex = pixelData[pixelIndex];
                            else
                                colorIndex = 0;
                        }

                        if (colorIndex < palette.Length)
                            bmp.SetPixel(x, y, palette[colorIndex]);
                        else
                            bmp.SetPixel(x, y, Color.Magenta); // Error color

                        pixelIndex++;
                    }
                }

                return bmp;
            }
        }

        public static Control ShowInfo(IPluginHost pluginHost, sFile file)
        {
            try
            {
                // Create a simple picture box to display the texture
                Panel panel = new Panel();
                panel.Dock = DockStyle.Fill;
                panel.AutoScroll = true;

                PictureBox pictureBox = new PictureBox();
                pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
                pictureBox.Image = Decode(file.path);
                pictureBox.Location = new Point(10, 10);

                // Add file info label
                Label infoLabel = new Label();
                infoLabel.AutoSize = true;
                infoLabel.Location = new Point(10, pictureBox.Bottom + 10);

                using (FileStream fs = new FileStream(file.path, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    TexHeader header = ReadHeader(br);
                    infoLabel.Text = string.Format(
                        "Format: TEX (Pet Hospitals Texture)\n" +
                        "Type: {0}\n" +
                        "Dimensions: {1} x {2}\n" +
                        "Bits per pixel: {3}\n" +
                        "Palette colors: {4}\n" +
                        "File size: {5} bytes",
                        header.Type, header.Width, header.Height,
                        header.BitsPerPixel, header.PaletteColors, fs.Length);
                }

                panel.Controls.Add(pictureBox);
                panel.Controls.Add(infoLabel);

                return panel;
            }
            catch (Exception ex)
            {
                Label errorLabel = new Label();
                errorLabel.Text = "Error loading TEX file: " + ex.Message;
                errorLabel.AutoSize = true;
                return errorLabel;
            }
        }
    }
}
