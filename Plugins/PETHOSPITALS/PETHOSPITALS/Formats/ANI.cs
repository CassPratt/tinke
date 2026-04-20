// ----------------------------------------------------------------------
// <copyright file="ANI.cs" company="none">
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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Ekona;

namespace PETHOSPITALS.Formats
{
    /// <summary>
    /// ANI format handler - Skeleton animation format
    /// 
    /// Format structure:
    /// - Bytes 0-31:  Animation name (null-terminated ASCII, padded to 32 bytes)
    /// - Bytes 32-35: Data size or offset (uint32)
    /// - Bytes 36-39: Frame count (uint32)
    /// - Bytes 40+:   Joint data (name + keyframe data)
    /// </summary>
    public static class ANI
    {
        public struct AniHeader
        {
            public string Name;
            public uint DataSize;
            public uint FrameCount;
            public List<string> JointNames;
        }

        public static AniHeader ReadHeader(string filePath)
        {
            AniHeader header = new AniHeader();
            header.JointNames = new List<string>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                // Read animation name (32 bytes, null-terminated)
                byte[] nameBytes = br.ReadBytes(32);
                int nameEnd = Array.IndexOf(nameBytes, (byte)0);
                if (nameEnd < 0) nameEnd = 32;
                header.Name = Encoding.ASCII.GetString(nameBytes, 0, nameEnd);

                // Read data size and frame count
                header.DataSize = br.ReadUInt32();
                header.FrameCount = br.ReadUInt32();

                // Try to find joint names in the remaining data
                // Joint names appear to be 8-byte null-padded strings
                long dataStart = fs.Position;
                
                while (fs.Position < fs.Length - 8)
                {
                    long currentPos = fs.Position;
                    
                    // Try to read a potential joint name
                    byte[] potentialName = br.ReadBytes(8);
                    
                    // Check if it looks like a valid ASCII name starting with "joint" or similar
                    string name = Encoding.ASCII.GetString(potentialName).TrimEnd('\0');
                    
                    if (name.StartsWith("joint") || name.StartsWith("bone") || name.StartsWith("root"))
                    {
                        if (!header.JointNames.Contains(name))
                            header.JointNames.Add(name);
                    }
                    
                    // Move to next potential position
                    // This is a heuristic - actual format may differ
                    fs.Position = currentPos + 1;
                    
                    // Limit search to first portion of file
                    if (fs.Position > dataStart + 500)
                        break;
                }
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

                AniHeader header = ReadHeader(file.path);

                // Create info display
                TextBox infoBox = new TextBox();
                infoBox.Multiline = true;
                infoBox.ReadOnly = true;
                infoBox.ScrollBars = ScrollBars.Vertical;
                infoBox.Dock = DockStyle.Fill;
                infoBox.Font = new Font("Consolas", 10);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════════════");
                sb.AppendLine("  ANI Animation File - Pet Hospitals");
                sb.AppendLine("═══════════════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine(string.Format("File: {0}", file.name));
                sb.AppendLine(string.Format("Size: {0:N0} bytes", new FileInfo(file.path).Length));
                sb.AppendLine();
                sb.AppendLine("Header Information:");
                sb.AppendLine("───────────────────────────────────────────────────");
                sb.AppendLine(string.Format("  Animation Name: {0}", header.Name));
                sb.AppendLine(string.Format("  Frame Count: {0}", header.FrameCount));
                sb.AppendLine(string.Format("  Data Size: {0} bytes", header.DataSize));
                sb.AppendLine();

                if (header.JointNames.Count > 0)
                {
                    sb.AppendLine("Detected Joint Names:");
                    sb.AppendLine("───────────────────────────────────────────────────");
                    foreach (string joint in header.JointNames)
                    {
                        sb.AppendLine(string.Format("  • {0}", joint));
                    }
                    sb.AppendLine();
                }

                // Calculate animation duration estimate (assuming 30 FPS)
                double duration30fps = header.FrameCount / 30.0;
                double duration60fps = header.FrameCount / 60.0;
                
                sb.AppendLine("Estimated Duration:");
                sb.AppendLine("───────────────────────────────────────────────────");
                sb.AppendLine(string.Format("  @ 30 FPS: {0:F2} seconds", duration30fps));
                sb.AppendLine(string.Format("  @ 60 FPS: {0:F2} seconds", duration60fps));
                sb.AppendLine();

                // Hex dump of header
                sb.AppendLine("Header Hex Dump (first 64 bytes):");
                sb.AppendLine("───────────────────────────────────────────────────");
                
                byte[] headerBytes = new byte[64];
                using (FileStream fs = new FileStream(file.path, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(headerBytes, 0, Math.Min(64, (int)fs.Length));
                }

                for (int i = 0; i < headerBytes.Length; i += 16)
                {
                    sb.Append(string.Format("{0:X4}: ", i));
                    
                    for (int j = 0; j < 16 && (i + j) < headerBytes.Length; j++)
                    {
                        sb.Append(string.Format("{0:X2} ", headerBytes[i + j]));
                    }
                    
                    sb.Append(" | ");
                    
                    for (int j = 0; j < 16 && (i + j) < headerBytes.Length; j++)
                    {
                        byte b = headerBytes[i + j];
                        if (b >= 32 && b < 127)
                            sb.Append((char)b);
                        else
                            sb.Append('.');
                    }
                    
                    sb.AppendLine();
                }

                sb.AppendLine();
                sb.AppendLine("Note: Full animation playback requires skeleton (SKL) and model (PDL) data.");

                infoBox.Text = sb.ToString();
                panel.Controls.Add(infoBox);

                return panel;
            }
            catch (Exception ex)
            {
                Label errorLabel = new Label();
                errorLabel.Text = "Error loading ANI file: " + ex.Message;
                errorLabel.AutoSize = true;
                return errorLabel;
            }
        }
    }
}
