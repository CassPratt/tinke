// ----------------------------------------------------------------------
// <copyright file="Model3D.cs" company="none">
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
    /// Model3D format handler - Generic viewer for 3D model related formats
    /// 
    /// Supported extensions:
    /// - MTL: Material definitions (texture references)
    /// - PDL: Polygon/mesh data
    /// - PRL: Polygon references/links
    /// - SKL: Skeleton hierarchy
    /// - NDL: Node data
    /// - COL: Collision data
    /// - ANL: Animation list
    /// - TRG: Trigger/event data
    /// </summary>
    public static class Model3D
    {
        public static Control ShowInfo(IPluginHost pluginHost, sFile file)
        {
            try
            {
                Panel panel = new Panel();
                panel.Dock = DockStyle.Fill;
                panel.AutoScroll = true;

                TextBox infoBox = new TextBox();
                infoBox.Multiline = true;
                infoBox.ReadOnly = true;
                infoBox.ScrollBars = ScrollBars.Both;
                infoBox.Dock = DockStyle.Fill;
                infoBox.Font = new Font("Consolas", 10);
                infoBox.WordWrap = false;

                string ext = Path.GetExtension(file.name).ToUpper();
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("═══════════════════════════════════════════════════");
                sb.AppendLine(string.Format("  {0} File - Pet Hospitals 3D Model Data", ext));
                sb.AppendLine("═══════════════════════════════════════════════════");
                sb.AppendLine();

                // File info
                FileInfo fi = new FileInfo(file.path);
                sb.AppendLine(string.Format("File: {0}", file.name));
                sb.AppendLine(string.Format("Size: {0:N0} bytes", fi.Length));
                sb.AppendLine();

                // Format description
                sb.AppendLine("Format Description:");
                sb.AppendLine("───────────────────────────────────────────────────");
                
                switch (ext)
                {
                    case ".MTL":
                        sb.AppendLine("  Material definition file");
                        sb.AppendLine("  Contains texture references and material properties");
                        sb.AppendLine("  Used to define surface appearance of 3D models");
                        break;
                    case ".PDL":
                        sb.AppendLine("  Polygon/mesh data file");
                        sb.AppendLine("  Contains vertex positions, normals, UV coordinates");
                        sb.AppendLine("  Defines the 3D geometry of models");
                        break;
                    case ".PRL":
                        sb.AppendLine("  Polygon reference/link file");
                        sb.AppendLine("  Contains references between polygons and other data");
                        break;
                    case ".SKL":
                        sb.AppendLine("  Skeleton hierarchy file");
                        sb.AppendLine("  Contains bone/joint definitions for animation");
                        sb.AppendLine("  Defines the armature structure of animated models");
                        break;
                    case ".NDL":
                        sb.AppendLine("  Node data file");
                        sb.AppendLine("  Contains scene graph or hierarchy information");
                        break;
                    case ".COL":
                        sb.AppendLine("  Collision data file");
                        sb.AppendLine("  Contains collision geometry for physics/interaction");
                        break;
                    case ".ANL":
                        sb.AppendLine("  Animation list file");
                        sb.AppendLine("  Contains list of animations available for a model");
                        break;
                    case ".TRG":
                        sb.AppendLine("  Trigger/event data file");
                        sb.AppendLine("  Contains game triggers and event definitions");
                        break;
                    default:
                        sb.AppendLine("  Unknown 3D model related format");
                        break;
                }
                sb.AppendLine();

                // Try to find readable strings
                sb.AppendLine("Detected Strings:");
                sb.AppendLine("───────────────────────────────────────────────────");
                
                byte[] data = File.ReadAllBytes(file.path);
                var strings = ExtractStrings(data, 4);
                
                if (strings.Count > 0)
                {
                    int count = 0;
                    foreach (var str in strings)
                    {
                        sb.AppendLine(string.Format("  [{0:X4}] {1}", str.Key, str.Value));
                        count++;
                        if (count >= 30) // Limit to 30 strings
                        {
                            sb.AppendLine(string.Format("  ... and {0} more strings", strings.Count - 30));
                            break;
                        }
                    }
                }
                else
                {
                    sb.AppendLine("  No readable strings found");
                }
                sb.AppendLine();

                // Hex dump
                sb.AppendLine("Hex Dump (first 256 bytes):");
                sb.AppendLine("───────────────────────────────────────────────────");
                
                int dumpSize = Math.Min(256, data.Length);
                for (int i = 0; i < dumpSize; i += 16)
                {
                    sb.Append(string.Format("{0:X4}: ", i));
                    
                    // Hex bytes
                    for (int j = 0; j < 16; j++)
                    {
                        if (i + j < dumpSize)
                            sb.Append(string.Format("{0:X2} ", data[i + j]));
                        else
                            sb.Append("   ");
                    }
                    
                    sb.Append(" | ");
                    
                    // ASCII
                    for (int j = 0; j < 16 && (i + j) < dumpSize; j++)
                    {
                        byte b = data[i + j];
                        if (b >= 32 && b < 127)
                            sb.Append((char)b);
                        else
                            sb.Append('.');
                    }
                    
                    sb.AppendLine();
                }

                infoBox.Text = sb.ToString();
                panel.Controls.Add(infoBox);

                return panel;
            }
            catch (Exception ex)
            {
                Label errorLabel = new Label();
                errorLabel.Text = "Error loading file: " + ex.Message;
                errorLabel.AutoSize = true;
                return errorLabel;
            }
        }

        private static System.Collections.Generic.Dictionary<int, string> ExtractStrings(byte[] data, int minLength)
        {
            var result = new System.Collections.Generic.Dictionary<int, string>();
            StringBuilder current = new StringBuilder();
            int startPos = 0;

            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                
                if (b >= 32 && b < 127)
                {
                    if (current.Length == 0)
                        startPos = i;
                    current.Append((char)b);
                }
                else
                {
                    if (current.Length >= minLength)
                    {
                        result[startPos] = current.ToString();
                    }
                    current.Clear();
                }
            }

            if (current.Length >= minLength)
            {
                result[startPos] = current.ToString();
            }

            return result;
        }
    }
}
