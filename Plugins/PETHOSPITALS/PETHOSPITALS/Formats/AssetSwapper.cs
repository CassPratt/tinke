using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using Ekona;
namespace PETHOSPITALS.Formats
{
    // Simple helper to swap two files on disk atomically using a temporary file.
    // Intended to be called from the Tinke plugin UI handler (ARM9PatcherControl).
    // Usage: call ApplyHarlequinSwapInProject(pluginHost, out error).
    public static class AssetSwapper
    {
        private static readonly (string nameA, string nameB)[] _swapPairs = new[]
        {
            ("rabbitHarlequinBlue_hi.pcx",        "rabbitHarlequinLilac_hi.pcx"),
            ("rabbitHarlequinBlue_lo.tex",         "rabbitHarlequinLilac_lo.tex"),
            ("rabbit_Harlequin_Blue_icon.tex",     "rabbit_Harlequin_Lilac_icon.tex"),
        };

        // Helpers removed: AssetSwapper only exposes ApplyHarlequinSwapInProject and Sha256Bytes.

        public static bool ApplyHarlequinSwapInProject(IPluginHost host, out string error)
        {
            error = null;
            try
            {
                if (host == null) { error = "plugin host is null"; return false; }

                var reportLines = new List<string>();
                foreach (var pair in _swapPairs)
                {
                    string nameA = pair.nameA;
                    string nameB = pair.nameB;

                    sFolder resultA = host.Search_File(nameA);
                    sFolder resultB = host.Search_File(nameB);

                    bool foundA = resultA.files != null && resultA.files.Count > 0;
                    bool foundB = resultB.files != null && resultB.files.Count > 0;

                    if (!foundA || !foundB)
                    {
                        error = $"File not found in project:\n  {nameA}: {(foundA ? "found" : "NOT FOUND")}\n  {nameB}: {(foundB ? "found" : "NOT FOUND")}";
                        return false;
                    }

                    sFile fileA = resultA.files[0];
                    sFile fileB = resultB.files[0];

                    // Read embedded bytes from the ROM using the file offsets
                    byte[] bytesA = host.Get_Bytes(fileA.path, (int)fileA.offset, (int)fileA.size);
                    byte[] bytesB = host.Get_Bytes(fileB.path, (int)fileB.offset, (int)fileB.size);

                    string shaA_before = Sha256Bytes(bytesA);
                    string shaB_before = Sha256Bytes(bytesB);

                    // Write swapped contents to temporary files and instruct host to replace
                    string tmpA = host.Get_TempFile();
                    string tmpB = host.Get_TempFile();
                    File.WriteAllBytes(tmpA, bytesB);
                    File.WriteAllBytes(tmpB, bytesA);

                    host.ChangeFile(fileA.id, tmpA);
                    host.ChangeFile(fileB.id, tmpB);

                    string shaA_after = Sha256Bytes(bytesB);
                    string shaB_after = Sha256Bytes(bytesA);

                    reportLines.Add($"{nameA} <-> {nameB}\n  {nameA}: {shaA_before} -> {shaA_after}\n  {nameB}: {shaB_before} -> {shaB_after}");
                }

                error = string.Join("\n\n", reportLines);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string Sha256Bytes(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(data);
                return BitConverter.ToString(h, 0, 4).Replace("-", "").ToLowerInvariant() + "...";
            }
        }
    }
}
