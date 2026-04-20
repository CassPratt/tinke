// ----------------------------------------------------------------------
// <copyright file="NFF.cs" company="none">
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
using System.Text;
using System.Windows.Forms;
using Ekona;

namespace PETHOSPITALS.Formats
{
    /// <summary>
    /// NFF format handler for Pet Hospitals custom font format
    /// 
    /// Format structure (preliminary):
    /// - Bytes 0-3:   Magic "test" or font name
    /// - Bytes 4-7:   Magic "\0fnt\0" identifier
    /// - Bytes 8+:    Font data (glyph definitions, metrics, bitmap data)
    /// 
    /// Note: This is a basic implementation that displays file information.
    /// Full font rendering requires more reverse engineering.
    /// </summary>
    public static class NFF
    {
        public struct NffHeader
        {
            public string FontName;
            public string Magic;
            public uint GlyphCount;
            public uint Unknown1;
            public uint DataOffset;
        }

        public static NffHeader ReadHeader(BinaryReader br)
        {
            NffHeader header = new NffHeader();
            
            // Read font name (null-terminated string, max 32 bytes)
            byte[] nameBytes = br.ReadBytes(32);
            int nameEnd = Array.IndexOf(nameBytes, (byte)0);
            if (nameEnd < 0) nameEnd = 32;
            header.FontName = Encoding.ASCII.GetString(nameBytes, 0, nameEnd);

            // Read additional header data
            if (br.BaseStream.Position < br.BaseStream.Length - 16)
            {
                header.GlyphCount = br.ReadUInt32();
                header.Unknown1 = br.ReadUInt32();
                header.DataOffset = br.ReadUInt32();
            }

            return header;
        }

        public static Control ShowInfo(IPluginHost pluginHost, sFile file)
        {
            try
            {
                Panel panel = new Panel();
                panel.Dock = DockStyle.Fill;
                panel.AutoScroll = true;

                // Create info display
                TextBox infoBox = new TextBox();
                infoBox.Multiline = true;
                infoBox.ReadOnly = true;
                infoBox.ScrollBars = ScrollBars.Vertical;
                infoBox.Dock = DockStyle.Fill;
                infoBox.Font = new Font("Consolas", 10);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine("  NFF Font File - Pet Hospitals");
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine();

                using (FileStream fs = new FileStream(file.path, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    NffHeader header = ReadHeader(br);

                    sb.AppendLine($"File: {file.name}");
                    sb.AppendLine($"Size: {fs.Length} bytes");
                    sb.AppendLine();
                    sb.AppendLine("Header Information:");
                    sb.AppendLine($"  Font Name: {header.FontName}");
                    sb.AppendLine($"  Glyph Count: {header.GlyphCount}");
                    sb.AppendLine($"  Data Offset: 0x{header.DataOffset:X8}");
                    sb.AppendLine();

                    // Show hex dump of first 128 bytes
                    sb.AppendLine("Hex Dump (first 128 bytes):");
                    sb.AppendLine("───────────────────────────────────────────");
                    
                    fs.Position = 0;
                    byte[] preview = new byte[Math.Min(128, fs.Length)];
                    fs.Read(preview, 0, preview.Length);

                    for (int i = 0; i < preview.Length; i += 16)
                    {
                        sb.Append($"{i:X4}: ");
                        
                        // Hex values
                        for (int j = 0; j < 16 && (i + j) < preview.Length; j++)
                        {
                            sb.Append($"{preview[i + j]:X2} ");
                        }
                        
                        // Padding if needed
                        for (int j = preview.Length - i; j < 16; j++)
                        {
                            sb.Append("   ");
                        }
                        
                        sb.Append(" | ");
                        
                        // ASCII representation
                        for (int j = 0; j < 16 && (i + j) < preview.Length; j++)
                        {
                            byte b = preview[i + j];
                            if (b >= 32 && b < 127)
                                sb.Append((char)b);
                            else
                                sb.Append('.');
                        }
                        
                        sb.AppendLine();
                    }
                }

                sb.AppendLine();
                sb.AppendLine("Note: Full font rendering is not yet implemented.");
                sb.AppendLine("This format requires additional reverse engineering.");

                infoBox.Text = sb.ToString();
                panel.Controls.Add(infoBox);

                return panel;
            }
            catch (Exception ex)
            {
                Label errorLabel = new Label();
                errorLabel.Text = "Error loading NFF file: " + ex.Message;
                errorLabel.AutoSize = true;
                return errorLabel;
            }
        }
    }
}
