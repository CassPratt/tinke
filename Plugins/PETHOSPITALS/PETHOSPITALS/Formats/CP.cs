// ----------------------------------------------------------------------
// <copyright file="CP.cs" company="none">

// ----------------------------------------------------------------------
// <copyright file="CP.cs" company="none">
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
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ekona;

namespace PETHOSPITALS.Formats
{
    /// <summary>
    /// CP format handler - Compiled/binary INI format
    /// 
    /// Format structure:
    /// - Bytes 0-3:   Section count (uint32)
    /// - For each section:
    ///   - 4 bytes: Section name length
    ///   - N bytes: Section name (ASCII)
    ///   - 4 bytes: Entry count in section
    ///   - For each entry:
    ///     - 4 bytes: Key length
    ///     - N bytes: Key name (ASCII)
    ///     - 4 bytes: Value length
    ///     - N bytes: Value (ASCII)
    /// </summary>
    public static class CP
    {
        public class CpSection
        {
            public string Name;
            public List<KeyValuePair<string, string>> Entries = new List<KeyValuePair<string, string>>();
        }

        public static List<CpSection> Decode(string filePath)
        {
            List<CpSection> sections = new List<CpSection>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                uint sectionCount = br.ReadUInt32();

                for (int s = 0; s < sectionCount; s++)
                {
                    CpSection section = new CpSection();

                    // Read section name
                    int sectionNameLen = br.ReadInt32();
                    section.Name = Encoding.ASCII.GetString(br.ReadBytes(sectionNameLen));

                    // Read entry count
                    int entryCount = br.ReadInt32();

                    for (int e = 0; e < entryCount; e++)
                    {
                        // Read key
                        int keyLen = br.ReadInt32();
                        string key = Encoding.ASCII.GetString(br.ReadBytes(keyLen));

                        // Read value
                        int valueLen = br.ReadInt32();
                        string value = Encoding.ASCII.GetString(br.ReadBytes(valueLen));

                        section.Entries.Add(new KeyValuePair<string, string>(key, value));
                    }

                    sections.Add(section);
                }
            }

            return sections;
        }

        public static string ConvertToIni(List<CpSection> sections)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var section in sections)
            {
                sb.AppendLine(string.Format("[{0}]", section.Name));
                foreach (var entry in section.Entries)
                {
                    sb.AppendLine(string.Format("{0}={1}", entry.Key, entry.Value));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Encode sections to a .ini.cp binary file using the same layout expected by Decode.
        /// Uses CP1252 encoding for strings.
        /// </summary>
        public static void Encode(string outPath, List<CpSection> sections)
        {
            var enc = Encoding.GetEncoding(1252);
            using (var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((uint)sections.Count);
                foreach (var section in sections)
                {
                    var secBytes = enc.GetBytes(section.Name ?? "");
                    bw.Write(secBytes.Length);
                    bw.Write(secBytes);

                    bw.Write(section.Entries.Count);
                    foreach (var kv in section.Entries)
                    {
                        var keyBytes = enc.GetBytes(kv.Key ?? "");
                        var valBytes = enc.GetBytes(kv.Value ?? "");
                        bw.Write(keyBytes.Length);
                        bw.Write(keyBytes);
                        bw.Write(valBytes.Length);
                        bw.Write(valBytes);
                    }
                }
            }
        }

        /// <summary>
        /// Parse a plaintext INI file (CP1252) into list of CpSection.
        /// Basic parser: supports [section] headers, key=value lines, ignores comments starting with ; or #.
        /// Preserves file order.
        /// </summary>
        public static List<CpSection> ParseIniFile(string iniPath)
        {
            var sections = new List<CpSection>();
            CpSection current = null;
            var enc = Encoding.GetEncoding(1252);
            var lines = File.ReadAllLines(iniPath, enc);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.Contains("]"))
                {
                    var end = line.IndexOf(']');
                    var name = line.Substring(1, end - 1).Trim();
                    current = new CpSection { Name = name };
                    sections.Add(current);
                    continue;
                }
                if (line.Contains("="))
                {
                    var idx = line.IndexOf('=');
                    var key = line.Substring(0, idx).Trim();
                    var val = line.Substring(idx + 1).Trim();
                    if (current == null)
                    {
                        // create a default unnamed section
                        current = new CpSection { Name = string.Empty };
                        sections.Add(current);
                    }
                    current.Entries.Add(new KeyValuePair<string, string>(key, val));
                }
            }
            return sections;
        }

        public static Control ShowInfo(IPluginHost pluginHost, sFile file)
        {
            try
            {
                Panel panel = new Panel();
                panel.Dock = DockStyle.Fill;

                // Decode CP file
                List<CpSection> sections = Decode(file.path);
                string iniContent = ConvertToIni(sections);

                // Create rich text box for content
                RichTextBox textBox = new RichTextBox();
                textBox.Dock = DockStyle.Fill;
                textBox.Font = new Font("Consolas", 10);
                textBox.ReadOnly = true;
                textBox.WordWrap = false;
                textBox.Text = iniContent;

                // Apply syntax highlighting
                ApplySyntaxHighlighting(textBox);

                // Info bar at top (flow layout so buttons remain visible when window is narrow)
                FlowLayoutPanel infoPanel = new FlowLayoutPanel();
                infoPanel.Dock = DockStyle.Top;
                infoPanel.Height = 35;
                infoPanel.BackColor = SystemColors.Control;
                infoPanel.FlowDirection = FlowDirection.LeftToRight;
                infoPanel.WrapContents = false;
                infoPanel.AutoSize = false;
                infoPanel.AutoScroll = true;

                Label infoLabel = new Label();
                infoLabel.AutoSize = true;
                infoLabel.Margin = new Padding(10, 10, 0, 0);

                int totalEntries = 0;
                foreach (var section in sections)
                    totalEntries += section.Entries.Count;

                infoLabel.Text = string.Format("CP File (Compiled INI) | {0} sections | {1} entries | {2}",
                    sections.Count, totalEntries, file.name);

                Button exportButton = new Button();
                exportButton.Text = "Export as INI";
                exportButton.Margin = new Padding(10, 5, 0, 0);
                exportButton.Click += (sender, e) =>
                {
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "INI files (*.ini)|*.ini|Text files (*.txt)|*.txt";
                    sfd.FileName = Path.GetFileNameWithoutExtension(file.name).Replace(".ini", "") + "_decoded.ini";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, iniContent, Encoding.UTF8);
                        MessageBox.Show("File exported!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                // Compile INI button: runs ini_to_cp.py to (re)generate .ini.cp from source .ini
                Button compileButton = new Button();
                compileButton.Text = "Compilar INI";
                compileButton.Margin = new Padding(10, 5, 0, 0);
                compileButton.Click += (sender, e) =>
                {
                    // Run compilation in background to avoid UI freeze
                    compileButton.Enabled = false;
                    Task.Run(() =>
                    {
                        // Helper to show UI messages from background thread
                        Action<string, string, MessageBoxIcon> UIShow = (text, caption, icon) =>
                        {
                            if (infoPanel.InvokeRequired)
                                infoPanel.Invoke(new Action(() => MessageBox.Show(text, caption, MessageBoxButtons.OK, icon)));
                            else
                                MessageBox.Show(text, caption, MessageBoxButtons.OK, icon);
                        };

                        try
                        {
                            string cpPath = file.path;
                            string cpName = file.name; // ROM filename (e.g. english.ini.cp), not the temp path
                            string iniName = cpName.EndsWith(".ini.cp", StringComparison.OrdinalIgnoreCase)
                                ? cpName.Substring(0, cpName.Length - ".ini.cp".Length) + ".ini"
                                : Path.ChangeExtension(cpName, ".ini");

                            string iniPath = Path.Combine(Path.GetDirectoryName(cpPath), iniName);

                            // First try to find the .ini file directly via Tinke's file search
                            // (the .ini is a separate ROM entry and will have its own temp path)
                            try
                            {
                                var romSearch = pluginHost.Search_File(iniName);
                                if (romSearch.files != null && romSearch.files.Count > 0 && File.Exists(romSearch.files[0].path))
                                    iniPath = romSearch.files[0].path;
                            }
                            catch { }

                            if (!File.Exists(iniPath))
                            {
                                // Try to locate a matching INI in the same folder by core name
                                string dir = Path.GetDirectoryName(cpPath);
                                string baseNoSuffix = cpName.EndsWith(".ini.cp", StringComparison.OrdinalIgnoreCase)
                                    ? cpName.Substring(0, cpName.Length - ".ini.cp".Length)
                                    : Path.GetFileNameWithoutExtension(cpName);

                                string core = null;
                                try
                                {
                                    var m = System.Text.RegularExpressions.Regex.Match(baseNoSuffix, "([A-Za-z_]+)$");
                                    if (m.Success) core = m.Groups[1].Value.ToLowerInvariant();
                                }
                                catch { core = null; }

                                string found = null;
                                if (!string.IsNullOrEmpty(core) && Directory.Exists(dir))
                                {
                                    foreach (var f in Directory.GetFiles(dir, "*.ini"))
                                    {
                                        var nameOnly = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                                        if (nameOnly == core) { found = f; break; }
                                    }
                                if (found == null)
                                {
                                    foreach (var f in Directory.GetFiles(dir, "*.ini"))
                                    {
                                        var nameOnly = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                                        if (!string.IsNullOrEmpty(core) && nameOnly.Contains(core)) { found = f; break; }
                                    }
                                }
                                }

                                // Fallback to repository lang folder. Try several candidate roots
                                string repoAlt = null;
                                var candidateRoots = new List<string>();
                                try { candidateRoots.Add(AppDomain.CurrentDomain.BaseDirectory); } catch { }
                                try { candidateRoots.Add(Environment.CurrentDirectory); } catch { }
                                try { candidateRoots.Add(Directory.GetCurrentDirectory()); } catch { }

                                // Also walk up parents from base directory to handle running from bin/Debug
                                try
                                {
                                    var parent = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                                    for (int i = 0; i < 6 && parent?.Parent != null; i++)
                                    {
                                        parent = parent.Parent;
                                        if (parent != null) candidateRoots.Add(parent.FullName);
                                    }
                                }
                                catch { }

                                string triedPaths = "";
                                foreach (var root in candidateRoots)
                                {
                                    if (string.IsNullOrEmpty(root)) continue;
                                    var tryPath = Path.Combine(root, "Plugins", "PETHOSPITALS", "lang");
                                    triedPaths += tryPath + "\n";
                                    if (Directory.Exists(tryPath)) { repoAlt = tryPath; break; }
                                }
                                if (found == null && Directory.Exists(repoAlt) && !string.Equals(dir, repoAlt, StringComparison.OrdinalIgnoreCase))
                                {
                                    foreach (var f in Directory.GetFiles(repoAlt, "*.ini"))
                                    {
                                        var nameOnly = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                                        if (!string.IsNullOrEmpty(core) && (nameOnly == core || nameOnly.Contains(core))) { found = f; break; }
                                    }
                                }

                                if (found != null)
                                {
                                    iniPath = found;
                                }
                                else
                                {
                                    // If repo lang folder exists, allow the user to pick one of the available INI files
                                    string chosen = null;
                                    if (Directory.Exists(repoAlt))
                                    {
                                        var cand = Directory.GetFiles(repoAlt, "*.ini");
                                        if (cand.Length > 0)
                                        {
                                            // Show modal selector on UI thread
                                            infoPanel.Invoke(new Action(() =>
                                            {
                                                using (Form dlg = new Form())
                                                {
                                                    dlg.Text = "Seleccionar INI fuente";
                                                    dlg.StartPosition = FormStartPosition.CenterParent;
                                                    dlg.Width = Math.Min(800, infoPanel.FindForm()?.Width ?? 800);
                                                    dlg.Height = Math.Min(600, infoPanel.FindForm()?.Height ?? 600);

                                                    ListBox lb = new ListBox();
                                                    lb.Dock = DockStyle.Top;
                                                    lb.Height = dlg.ClientSize.Height - 60;
                                                    foreach (var f in cand) lb.Items.Add(Path.GetFileName(f));

                                                    Button ok = new Button();
                                                    ok.Text = "Aceptar";
                                                    ok.Dock = DockStyle.Bottom;
                                                    ok.DialogResult = DialogResult.OK;

                                                    Button cancel = new Button();
                                                    cancel.Text = "Cancelar";
                                                    cancel.Dock = DockStyle.Bottom;
                                                    cancel.DialogResult = DialogResult.Cancel;

                                                    dlg.Controls.Add(lb);
                                                    dlg.Controls.Add(ok);
                                                    dlg.Controls.Add(cancel);

                                                    if (dlg.ShowDialog(infoPanel.FindForm()) == DialogResult.OK && lb.SelectedIndex >= 0)
                                                    {
                                                        // Prefer file from Tinke's extracted tree if available
                                                        string selName = (string)lb.SelectedItem;
                                                        try
                                                        {
                                                            var folderRes = pluginHost.Search_File(selName);
                                                            if (folderRes.files != null && folderRes.files.Count > 0)
                                                            {
                                                                chosen = folderRes.files[0].path;
                                                            }
                                                            else
                                                            {
                                                                chosen = Path.Combine(repoAlt, selName);
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            chosen = Path.Combine(repoAlt, selName);
                                                        }
                                                    }
                                                }
                                            }));
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(chosen) && File.Exists(chosen))
                                    {
                                        iniPath = chosen;
                                    }
                                    else
                                    {
                                        string altIni = Path.Combine(repoAlt, iniName);
                                        UIShow($"Source INI not found:\n{iniPath}\n\nTried fallback:\n{altIni}", "Error", MessageBoxIcon.Error);
                                        return;
                                    }
                                }
                            }

                            // Backup existing cp before generating new one
                            string backupPath = cpPath + ".bak";
                            try { if (File.Exists(cpPath)) File.Copy(cpPath, backupPath, true); } catch { }

                            // Parse the selected INI into sections
                            List<CpSection> newSections = null;
                            try
                            {
                                newSections = ParseIniFile(iniPath);
                                if (newSections == null || newSections.Count == 0)
                                {
                                    UIShow("Parsed INI contains no sections/entries.", "Error", MessageBoxIcon.Error);
                                    return;
                                }
                            }
                            catch (Exception exParse)
                            {
                                UIShow("Failed to parse INI: " + exParse.Message, "Error", MessageBoxIcon.Error);
                                return;
                            }

                            // Encode to a temporary .cp file
                            string tempCp = Path.GetTempFileName();
                            try
                            {
                                Encode(tempCp, newSections);
                            }
                            catch (Exception exEnc)
                            {
                                UIShow("Failed to encode .ini.cp: " + exEnc.Message, "Error", MessageBoxIcon.Error);
                                // restore backup
                                try { if (File.Exists(backupPath)) File.Copy(backupPath, cpPath, true); } catch { }
                                return;
                            }

                            // Validate generated CP
                            try
                            {
                                CP.Decode(tempCp);
                            }
                            catch (Exception exDecode)
                            {
                                // restore backup
                                try { if (File.Exists(backupPath)) File.Copy(backupPath, cpPath, true); } catch { }
                                UIShow("Generated .ini.cp failed validation: " + exDecode.Message, "Error", MessageBoxIcon.Error);
                                return;
                            }

                            // Replace file in Tinke via pluginHost using temp file
                            try
                            {
                                string tempFile = pluginHost.Get_TempFile();
                                File.Copy(tempCp, tempFile, true);
                                pluginHost.ChangeFile(file.id, tempFile);
                            }
                            catch (Exception exReplace)
                            {
                                UIShow("Failed to replace file in Tinke: " + exReplace.Message, "Error", MessageBoxIcon.Error);
                                // restore backup
                                try { if (File.Exists(backupPath)) File.Copy(backupPath, cpPath, true); } catch { }
                                return;
                            }

                            UIShow("Compiled and updated .ini.cp successfully.", "Success", MessageBoxIcon.Information);
                        }
                        finally
                        {
                            // Re-enable button on UI thread
                            if (compileButton.InvokeRequired)
                                compileButton.Invoke(new Action(() => compileButton.Enabled = true));
                            else
                                compileButton.Enabled = true;
                        }
                    });
                };

                // Add controls to the flow panel in order (compile before export)
                infoPanel.Controls.Add(infoLabel);
                infoPanel.Controls.Add(compileButton);
                infoPanel.Controls.Add(exportButton);

                // Add controls in the same order as INI viewer: add text box first, then add
                // info panel so the layout reserves the top area and the text renders below.
                panel.Controls.Add(textBox);
                panel.Controls.Add(infoPanel);

                return panel;
            }
            catch (Exception ex)
            {
                Label errorLabel = new Label();
                errorLabel.Text = "Error loading CP file: " + ex.Message + "\n\nStack trace:\n" + ex.StackTrace;
                errorLabel.AutoSize = true;
                return errorLabel;
            }
        }

        private static void ApplySyntaxHighlighting(RichTextBox textBox)
        {
            int selStart = textBox.SelectionStart;
            int selLength = textBox.SelectionLength;

            string[] lines = textBox.Text.Split('\n');
            int charIndex = 0;

            textBox.SelectAll();
            textBox.SelectionColor = Color.Black;

            foreach (string line in lines)
            {
                string trimmedLine = line.TrimStart();

                if (trimmedLine.StartsWith("[") && trimmedLine.Contains("]"))
                {
                    int bracketStart = line.IndexOf('[');
                    int bracketEnd = line.IndexOf(']');
                    if (bracketStart >= 0 && bracketEnd > bracketStart)
                    {
                        textBox.Select(charIndex + bracketStart, bracketEnd - bracketStart + 1);
                        textBox.SelectionColor = Color.Blue;
                        textBox.SelectionFont = new Font(textBox.Font, FontStyle.Bold);
                    }
                }
                else if (trimmedLine.Contains("="))
                {
                    int eqIndex = line.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        textBox.Select(charIndex, eqIndex);
                        textBox.SelectionColor = Color.DarkRed;

                        textBox.Select(charIndex + eqIndex + 1, line.Length - eqIndex - 1);
                        textBox.SelectionColor = Color.DarkMagenta;
                    }
                }

                charIndex += line.Length + 1;
            }

            textBox.Select(selStart, selLength);
        }
    }
}
