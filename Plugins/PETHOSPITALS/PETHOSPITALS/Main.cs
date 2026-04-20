// ----------------------------------------------------------------------
// <copyright file="Main.cs" company="none">
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
using System.Text;
using System.Windows.Forms;
using Ekona;
using PETHOSPITALS.Formats;

namespace PETHOSPITALS
{
    /// <summary>
    /// Main plugin class for Let's Play Pet Hospitals (C2HP)
    /// Supports formats: TEX, NFF, SFX, INI, PCX, RAW
    /// </summary>
    public class Main : IGamePlugin
    {
        IPluginHost pluginHost;
        string gameCode;

        public void Initialize(IPluginHost pluginHost, string gameCode)
        {
            this.pluginHost = pluginHost;
            this.gameCode = gameCode;
        }

        public bool IsCompatible()
        {
            // Let's Play Pet Hospitals - Game Code: C2HP
            // Also check for potential regional variants
            if (gameCode == "C2HP" || gameCode == "C2HE" || gameCode == "C2HJ" || gameCode == "C2HK")
                return true;

            return false;
        }

        public Format Get_Format(sFile file, byte[] magic)
        {
            string name = file.name.ToUpper();

            // ARM9.BIN - Main executable (patchable)
            if (name == "ARM9.BIN")
                return Format.System;

            // TEX - Custom texture format
            // Header: 4 bytes (type?), 4 bytes (width), 4 bytes (height), then pixel data
            if (name.EndsWith(".TEX"))
                return Format.Texture;

            // PCX - Standard ZSoft PCX image format (magic: 0A)
            if (name.EndsWith(".PCX"))
            {
                if (magic.Length >= 1 && magic[0] == 0x0A)
                    return Format.FullImage;
            }

            // NFF - Custom font format (header starts with "test\0fnt\0")
            if (name.EndsWith(".NFF"))
                return Format.Font;

            // INI - Standard text configuration files
            if (name.EndsWith(".INI") && !name.EndsWith(".INI.CP"))
                return Format.Text;

            // CP - Compiled/binary INI files
            if (name.EndsWith(".CP"))
                return Format.Script;

            // SFX - Sound effects (raw PCM audio)
            if (name.EndsWith(".SFX"))
                return Format.Sound;

            // RAW - Raw audio files (PCM)
            if (name.EndsWith(".RAW"))
                return Format.Sound;

            // ANI - Animation files (skeleton animations)
            if (name.EndsWith(".ANI"))
                return Format.Animation;

            // Model-related formats
            if (name.EndsWith(".MTL"))
                return Format.Model3D; // Material definitions

            if (name.EndsWith(".PDL"))
                return Format.Model3D; // Polygon data

            if (name.EndsWith(".PRL"))
                return Format.Model3D; // Polygon references

            if (name.EndsWith(".SKL"))
                return Format.Model3D; // Skeleton data

            if (name.EndsWith(".NDL"))
                return Format.Model3D; // Node data

            if (name.EndsWith(".COL"))
                return Format.Model3D; // Collision data

            if (name.EndsWith(".ANL"))
                return Format.Model3D; // Animation list

            if (name.EndsWith(".TRG"))
                return Format.Script; // Trigger data

            return Format.Unknown;
        }

        public void Read(sFile file)
        {
            // Pre-read processing if needed
        }

        public Control Show_Info(sFile file)
        {
            string name = file.name.ToUpper();

            // ARM9.BIN - Show patcher UI
            if (name == "ARM9.BIN")
            {
                return ARM9Patcher.ShowPatcherUI(pluginHost, file);
            }

            // TEX - Show texture viewer
            if (name.EndsWith(".TEX"))
            {
                return TEX.ShowInfo(pluginHost, file);
            }

            // PCX - Show image viewer
            if (name.EndsWith(".PCX"))
            {
                return PCX.ShowInfo(pluginHost, file);
            }

            // NFF - Show font info
            if (name.EndsWith(".NFF"))
            {
                return NFF.ShowInfo(pluginHost, file);
            }

            // INI - Show text viewer
            if (name.EndsWith(".INI") && !name.EndsWith(".INI.CP"))
            {
                return INI.ShowInfo(pluginHost, file);
            }

            // CP - Show compiled INI viewer
            if (name.EndsWith(".CP"))
            {
                return CP.ShowInfo(pluginHost, file);
            }

            // SFX/RAW - Show audio player
            if (name.EndsWith(".SFX") || name.EndsWith(".RAW"))
            {
                return SFX.ShowInfo(pluginHost, file);
            }

            // ANI - Show animation info
            if (name.EndsWith(".ANI"))
            {
                return ANI.ShowInfo(pluginHost, file);
            }

            // MTL - Show material editor (allows changing textures)
            if (name.EndsWith(".MTL"))
            {
                return MTL.ShowInfo(pluginHost, file);
            }

            // 3D Model formats - Show generic model info
            if (name.EndsWith(".PDL") || name.EndsWith(".PRL") ||
                name.EndsWith(".SKL") || name.EndsWith(".NDL") || name.EndsWith(".COL") ||
                name.EndsWith(".ANL") || name.EndsWith(".TRG"))
            {
                return Model3D.ShowInfo(pluginHost, file);
            }

            // Return empty control for formats we recognize but don't have custom viewers for
            return new Control();
        }

        public sFolder Unpack(sFile file)
        {
            // No pack files to unpack in this game
            return new sFolder();
        }

        public string Pack(ref sFolder unpacked, sFile file)
        {
            // No repacking implemented yet
            return null;
        }
    }
}
