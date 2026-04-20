// ----------------------------------------------------------------------
// <copyright file="INI.cs" company="none">
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
    /// INI format handler - Standard text configuration files
    /// Simple text viewer with syntax highlighting for INI files
    /// </summary>
    public static class INI
    {
        public static Control ShowInfo(IPluginHost pluginHost, sFile file)
        {
            try
            {
                Panel panel = new Panel();
                panel.Dock = DockStyle.Fill;

                // Create rich text box for INI content
                RichTextBox textBox = new RichTextBox();
                textBox.Dock = DockStyle.Fill;
                textBox.Font = new Font("Consolas", 10);
                // Allow user edits; encoding/validation enforced on save
                textBox.ReadOnly = false;
                textBox.WordWrap = false;

                // Read file content
                string content = File.ReadAllText(file.path, Encoding.UTF8);

                // If UTF-8 fails or has issues, try other encodings
                if (string.IsNullOrEmpty(content) || content.Contains("\uFFFD"))
                {
                    content = File.ReadAllText(file.path, Encoding.Default);
                }

                textBox.Text = content;

                // Apply simple syntax highlighting
                ApplySyntaxHighlighting(textBox);

                // Info bar at top
                Panel infoPanel = new Panel();
                infoPanel.Dock = DockStyle.Top;
                infoPanel.Height = 35;
                infoPanel.BackColor = SystemColors.Control;

                Label infoLabel = new Label();
                infoLabel.AutoSize = true;
                infoLabel.Location = new Point(10, 10);
                
                int lineCount = content.Split('\n').Length;
                long fileSize = new FileInfo(file.path).Length;
                infoLabel.Text = string.Format("INI File | {0} lines | {1} bytes | {2}",
                    lineCount, fileSize, file.name);

                // Save changes button (overwrites the INI in-place)
                Button saveButton = new Button();
                saveButton.Text = "Guardar cambios";
                saveButton.Location = new Point(350, 5);
                saveButton.Click += (sender, e) =>
                {
                    try
                    {
                        // Basic INI validation: ensure each non-empty, non-comment, non-section
                        // line contains an '=' separating key and value.
                        var lines = textBox.Text.Replace("\r\n", "\n").Split('\n');
                        foreach (var raw in lines)
                        {
                            var line = raw.Trim();
                            if (string.IsNullOrEmpty(line)) continue;
                            if (line.StartsWith(";") || line.StartsWith("#")) continue;
                            if (line.StartsWith("[") && line.EndsWith("]")) continue;
                            if (!line.Contains("="))
                                throw new Exception("Invalid INI line (missing '='): " + line);
                        }

                        // Save using Windows-1252 encoding to preserve game characters
                        // Use pluginHost.ChangeFile to update the file inside Tinke rather than
                        // overwriting the extracted file on disk, ensuring the change is visible
                        // in the file tree and persisted when saving the ROM.
                        string tempFile = pluginHost.Get_TempFile();
                        File.WriteAllText(tempFile, textBox.Text, Encoding.GetEncoding(1252));
                        pluginHost.ChangeFile(file.id, tempFile);

                        // Re-apply syntax highlighting in case of edits
                        ApplySyntaxHighlighting(textBox);

                        MessageBox.Show("INI guardado correctamente.", "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving INI: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                // Export (Save As) button
                Button exportButton = new Button();
                exportButton.Text = "Exportar como...";
                exportButton.Location = new Point(470, 5);
                exportButton.Click += (sender, e) =>
                {
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "INI files (*.ini)|*.ini|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                    sfd.FileName = file.name;
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, textBox.Text, Encoding.GetEncoding(1252));
                        MessageBox.Show("File saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                infoPanel.Controls.Add(infoLabel);
                infoPanel.Controls.Add(saveButton);
                infoPanel.Controls.Add(exportButton);

                panel.Controls.Add(textBox);
                panel.Controls.Add(infoPanel);

                return panel;
            }
            catch (Exception ex)
            {
                Label errorLabel = new Label();
                errorLabel.Text = "Error loading INI file: " + ex.Message;
                errorLabel.AutoSize = true;
                return errorLabel;
            }
        }

        private static void ApplySyntaxHighlighting(RichTextBox textBox)
        {
            // Store current position
            int selStart = textBox.SelectionStart;
            int selLength = textBox.SelectionLength;

            // Process line by line
            string[] lines = textBox.Text.Split('\n');
            int charIndex = 0;

            textBox.SelectAll();
            textBox.SelectionColor = Color.Black;

            foreach (string line in lines)
            {
                string trimmedLine = line.TrimStart();

                if (trimmedLine.StartsWith("[") && trimmedLine.Contains("]"))
                {
                    // Section headers [section]
                    int bracketStart = line.IndexOf('[');
                    int bracketEnd = line.IndexOf(']');
                    if (bracketStart >= 0 && bracketEnd > bracketStart)
                    {
                        textBox.Select(charIndex + bracketStart, bracketEnd - bracketStart + 1);
                        textBox.SelectionColor = Color.Blue;
                        textBox.SelectionFont = new Font(textBox.Font, FontStyle.Bold);
                    }
                }
                else if (trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                {
                    // Comments
                    textBox.Select(charIndex, line.Length);
                    textBox.SelectionColor = Color.Green;
                }
                else if (trimmedLine.Contains("="))
                {
                    // Key=Value pairs
                    int eqIndex = line.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        // Key
                        textBox.Select(charIndex, eqIndex);
                        textBox.SelectionColor = Color.DarkRed;
                        
                        // Value
                        textBox.Select(charIndex + eqIndex + 1, line.Length - eqIndex - 1);
                        textBox.SelectionColor = Color.DarkMagenta;
                    }
                }

                charIndex += line.Length + 1; // +1 for newline
            }

            // Restore position
            textBox.Select(selStart, selLength);
        }
    }
}
