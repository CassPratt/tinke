// ----------------------------------------------------------------------
// <copyright file="SFX.cs" company="none">
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
    /// SFX/RAW format handler for Pet Hospitals audio files
    /// 
    /// SFX Format:
    /// - Raw PCM audio data (8-bit signed or unsigned)
    /// - No header, raw samples
    /// - Typical sample rates: 8000, 11025, 22050 Hz
    /// 
    /// RAW Format:
    /// - Raw PCM audio data (likely 8-bit or 16-bit)
    /// - Used for music/longer audio
    /// </summary>
    public static class SFX
    {
        // Common NDS audio sample rates
        private static readonly int[] CommonSampleRates = { 8000, 11025, 16000, 22050, 32000, 44100 };
        
        // Default settings for Pet Hospitals audio
        private const int DefaultSampleRate = 22050;
        private const int DefaultBitsPerSample = 8;
        private const int DefaultChannels = 1;

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
                infoBox.Location = new Point(10, 10);
                infoBox.Size = new Size(400, 200);
                infoBox.Font = new Font("Consolas", 10);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine("  Audio File - Pet Hospitals");
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine();

                using (FileStream fs = new FileStream(file.path, FileMode.Open, FileAccess.Read))
                {
                    long fileSize = fs.Length;
                    string extension = Path.GetExtension(file.name).ToUpper();

                    sb.AppendLine($"File: {file.name}");
                    sb.AppendLine($"Size: {fileSize:N0} bytes");
                    sb.AppendLine($"Type: {(extension == ".SFX" ? "Sound Effect" : "Raw Audio")}");
                    sb.AppendLine();

                    // Calculate duration estimates for different sample rates
                    sb.AppendLine("Estimated Duration (assuming 8-bit mono):");
                    sb.AppendLine("───────────────────────────────────────────");
                    
                    foreach (int sampleRate in CommonSampleRates)
                    {
                        double durationSeconds = (double)fileSize / sampleRate;
                        TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
                        sb.AppendLine($"  @ {sampleRate,5} Hz: {duration:mm\\:ss\\.fff}");
                    }

                    sb.AppendLine();
                    sb.AppendLine("Estimated Duration (assuming 16-bit mono):");
                    sb.AppendLine("───────────────────────────────────────────");
                    
                    foreach (int sampleRate in CommonSampleRates)
                    {
                        double durationSeconds = (double)fileSize / (sampleRate * 2);
                        TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
                        sb.AppendLine($"  @ {sampleRate,5} Hz: {duration:mm\\:ss\\.fff}");
                    }

                    // Show hex dump of first 64 bytes
                    sb.AppendLine();
                    sb.AppendLine("Hex Dump (first 64 bytes):");
                    sb.AppendLine("───────────────────────────────────────────");
                    
                    fs.Position = 0;
                    byte[] preview = new byte[Math.Min(64, fs.Length)];
                    fs.Read(preview, 0, preview.Length);

                    for (int i = 0; i < preview.Length; i += 16)
                    {
                        sb.Append($"{i:X4}: ");
                        
                        for (int j = 0; j < 16 && (i + j) < preview.Length; j++)
                        {
                            sb.Append($"{preview[i + j]:X2} ");
                        }
                        
                        sb.AppendLine();
                    }

                    // Analyze sample values
                    sb.AppendLine();
                    sb.AppendLine("Sample Analysis:");
                    sb.AppendLine("───────────────────────────────────────────");
                    
                    fs.Position = 0;
                    byte[] allBytes = new byte[Math.Min(fileSize, 10000)];
                    fs.Read(allBytes, 0, allBytes.Length);
                    
                    int minVal = 255, maxVal = 0;
                    long sum = 0;
                    foreach (byte b in allBytes)
                    {
                        if (b < minVal) minVal = b;
                        if (b > maxVal) maxVal = b;
                        sum += b;
                    }
                    double avg = (double)sum / allBytes.Length;

                    sb.AppendLine($"  Min sample value: {minVal} (0x{minVal:X2})");
                    sb.AppendLine($"  Max sample value: {maxVal} (0x{maxVal:X2})");
                    sb.AppendLine($"  Average value: {avg:F2}");
                    sb.AppendLine($"  Sample range: {maxVal - minVal}");
                    
                    // Guess format based on values
                    if (avg > 100 && avg < 156)
                        sb.AppendLine("  Format guess: Unsigned 8-bit PCM (centered ~128)");
                    else if (avg < 20 || avg > 235)
                        sb.AppendLine("  Format guess: Signed 8-bit PCM (centered ~0)");
                    else
                        sb.AppendLine("  Format guess: Unknown/compressed");
                }

                sb.AppendLine();
                sb.AppendLine("Note: Audio playback requires WAV export.");
                sb.AppendLine("Use 'Export to WAV' to play the audio.");

                infoBox.Text = sb.ToString();

                // Add export button
                Button exportButton = new Button();
                exportButton.Text = "Export to WAV (22050 Hz, 8-bit)";
                exportButton.Location = new Point(10, 220);
                exportButton.Size = new Size(200, 30);
                exportButton.Click += (sender, e) => ExportToWav(file.path, 22050, 8);

                panel.Controls.Add(infoBox);
                panel.Controls.Add(exportButton);

                return panel;
            }
            catch (Exception ex)
            {
                Label errorLabel = new Label();
                errorLabel.Text = "Error loading audio file: " + ex.Message;
                errorLabel.AutoSize = true;
                return errorLabel;
            }
        }

        public static void ExportToWav(string inputPath, int sampleRate, int bitsPerSample)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "WAV files (*.wav)|*.wav";
                sfd.FileName = Path.GetFileNameWithoutExtension(inputPath) + ".wav";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    byte[] rawData = File.ReadAllBytes(inputPath);
                    
                    using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create))
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        int channels = 1;
                        int bytesPerSample = bitsPerSample / 8;
                        int byteRate = sampleRate * channels * bytesPerSample;
                        short blockAlign = (short)(channels * bytesPerSample);

                        // RIFF header
                        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                        bw.Write(36 + rawData.Length); // File size - 8
                        bw.Write(Encoding.ASCII.GetBytes("WAVE"));

                        // fmt chunk
                        bw.Write(Encoding.ASCII.GetBytes("fmt "));
                        bw.Write(16); // Chunk size
                        bw.Write((short)1); // Audio format (PCM)
                        bw.Write((short)channels);
                        bw.Write(sampleRate);
                        bw.Write(byteRate);
                        bw.Write(blockAlign);
                        bw.Write((short)bitsPerSample);

                        // data chunk
                        bw.Write(Encoding.ASCII.GetBytes("data"));
                        bw.Write(rawData.Length);
                        bw.Write(rawData);
                    }

                    MessageBox.Show($"Exported to: {sfd.FileName}", "Export Complete", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting WAV: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
