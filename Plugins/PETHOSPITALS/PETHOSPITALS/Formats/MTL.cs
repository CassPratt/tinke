// ----------------------------------------------------------------------
// <copyright file="MTL.cs" company="none">
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
    /// MTL format handler - Material definition files
    /// 
    /// Format structure:
    /// - Bytes 0-3:   Material count (uint32)
    /// - Bytes 4-35:  Header data (pointers, flags)
    /// - Bytes 36+:   Material names (null-terminated strings, ~32 bytes each)
    /// 
    /// Each material name references a texture file (PCX for _hi, TEX for _lo)
    /// </summary>
    public static class MTL
    {
        public class MtlFile
        {
            public byte[] RawData;
            public int MaterialCount;
            public List<MaterialEntry> Materials;
            public string FilePath;
            public int FileId;
        }

        public class MaterialEntry
        {
            public int Offset;
            public string Name;
            public int MaxLength;
        }

        public static MtlFile Read(string filePath)
        {
            MtlFile mtl = new MtlFile();
            mtl.FilePath = filePath;
            mtl.RawData = File.ReadAllBytes(filePath);
            mtl.Materials = new List<MaterialEntry>();

            using (MemoryStream ms = new MemoryStream(mtl.RawData))
            using (BinaryReader br = new BinaryReader(ms))
            {
                mtl.MaterialCount = br.ReadInt32();

                // Find material names in the data
                // They start around byte 36 and are null-terminated
                int searchStart = 36;
                int currentPos = searchStart;

                while (currentPos < mtl.RawData.Length - 4)
                {
                    // Check if this looks like a valid ASCII string start
                    if (mtl.RawData[currentPos] >= 'A' && mtl.RawData[currentPos] <= 'z')
                    {
                        // Find the end of the string
                        int stringEnd = currentPos;
                        while (stringEnd < mtl.RawData.Length && mtl.RawData[stringEnd] != 0)
                        {
                            stringEnd++;
                        }

                        int stringLength = stringEnd - currentPos;
                        
                        // Valid material names are typically 10-40 characters
                        if (stringLength >= 5 && stringLength <= 40)
                        {
                            string name = Encoding.ASCII.GetString(mtl.RawData, currentPos, stringLength);
                            
                            // Check if it looks like a texture name (contains _ or camelCase)
                            if (name.Contains("_") || (name.Length > 3 && char.IsLower(name[0])))
                            {
                                MaterialEntry entry = new MaterialEntry();
                                entry.Offset = currentPos;
                                entry.Name = name;
                                entry.MaxLength = 32; // Typical max length for material names
                                mtl.Materials.Add(entry);
                            }
                        }

                        currentPos = stringEnd + 1;
                    }
                    else
                    {
                        currentPos++;
                    }
                }
            }

            return mtl;
        }

        public static byte[] Write(MtlFile mtl)
        {
            byte[] newData = (byte[])mtl.RawData.Clone();

            foreach (var material in mtl.Materials)
            {
                // Clear the old name area
                for (int i = 0; i < material.MaxLength && (material.Offset + i) < newData.Length; i++)
                {
                    newData[material.Offset + i] = 0;
                }

                // Write the new name
                byte[] nameBytes = Encoding.ASCII.GetBytes(material.Name);
                int copyLength = Math.Min(nameBytes.Length, material.MaxLength - 1);
                Array.Copy(nameBytes, 0, newData, material.Offset, copyLength);
            }

            return newData;
        }

        public static Control ShowInfo(IPluginHost pluginHost, sFile file)
        {
            try
            {
                MtlFile mtl = Read(file.path);
                mtl.FileId = file.id;

                return new MTLEditorControl(pluginHost, mtl, file);
            }
            catch (Exception ex)
            {
                Label errorLabel = new Label();
                errorLabel.Text = "Error loading MTL file: " + ex.Message;
                errorLabel.AutoSize = true;
                return errorLabel;
            }
        }
    }

    /// <summary>
    /// User control for editing MTL files
    /// </summary>
    public class MTLEditorControl : UserControl
    {
        private IPluginHost pluginHost;
        private MTL.MtlFile mtlFile;
        private sFile originalFile;
        private ListView materialList;
        private TextBox txtNewTexture;
        private ComboBox cmbAvailableTextures;
        private Button btnApply;
        private Button btnSave;
        private Label lblStatus;
        private List<string> availableTextures;

        public MTLEditorControl(IPluginHost pluginHost, MTL.MtlFile mtl, sFile file)
        {
            this.pluginHost = pluginHost;
            this.mtlFile = mtl;
            this.originalFile = file;
            this.availableTextures = new List<string>();

            InitializeComponent();
            LoadMaterials();
            ScanAvailableTextures();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(600, 400);
            this.Dock = DockStyle.Fill;

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "MTL Material Editor - " + Path.GetFileName(originalFile.name);
            lblTitle.Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold);
            lblTitle.Location = new Point(10, 10);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Info label
            Label lblInfo = new Label();
            lblInfo.Text = string.Format("Materials: {0} | File size: {1} bytes", 
                mtlFile.MaterialCount, mtlFile.RawData.Length);
            lblInfo.Location = new Point(10, 35);
            lblInfo.AutoSize = true;
            this.Controls.Add(lblInfo);

            // Material list
            materialList = new ListView();
            materialList.View = View.Details;
            materialList.FullRowSelect = true;
            materialList.GridLines = true;
            materialList.Location = new Point(10, 60);
            materialList.Size = new Size(400, 150);
            materialList.Columns.Add("Offset", 60);
            materialList.Columns.Add("Current Texture Name", 320);
            materialList.SelectedIndexChanged += MaterialList_SelectedIndexChanged;
            this.Controls.Add(materialList);

            // Available textures dropdown
            Label lblAvailable = new Label();
            lblAvailable.Text = "Available textures:";
            lblAvailable.Location = new Point(10, 220);
            lblAvailable.AutoSize = true;
            this.Controls.Add(lblAvailable);

            cmbAvailableTextures = new ComboBox();
            cmbAvailableTextures.Location = new Point(120, 217);
            cmbAvailableTextures.Size = new Size(290, 25);
            cmbAvailableTextures.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAvailableTextures.SelectedIndexChanged += CmbAvailableTextures_SelectedIndexChanged;
            this.Controls.Add(cmbAvailableTextures);

            // New texture name
            Label lblNew = new Label();
            lblNew.Text = "New texture:";
            lblNew.Location = new Point(10, 250);
            lblNew.AutoSize = true;
            this.Controls.Add(lblNew);

            txtNewTexture = new TextBox();
            txtNewTexture.Location = new Point(120, 247);
            txtNewTexture.Size = new Size(290, 25);
            this.Controls.Add(txtNewTexture);

            // Apply button
            btnApply = new Button();
            btnApply.Text = "Apply to Selected";
            btnApply.Location = new Point(120, 280);
            btnApply.Size = new Size(120, 28);
            btnApply.Click += BtnApply_Click;
            this.Controls.Add(btnApply);

            // Save button
            btnSave = new Button();
            btnSave.Text = "💾 Save to ROM";
            btnSave.Location = new Point(250, 280);
            btnSave.Size = new Size(120, 28);
            btnSave.BackColor = Color.LightGreen;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            // Status label
            lblStatus = new Label();
            lblStatus.Text = "";
            lblStatus.Location = new Point(10, 320);
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.Blue;
            this.Controls.Add(lblStatus);

            // Instructions
            Label lblInstructions = new Label();
            lblInstructions.Text = "Instructions:\n" +
                "1. Select a material from the list\n" +
                "2. Choose a new texture from the dropdown or type manually\n" +
                "3. Click 'Apply to Selected' to change the texture\n" +
                "4. Click 'Save to ROM' to save changes\n" +
                "5. Use File > Save ROM in Tinke to export the modified ROM";
            lblInstructions.Location = new Point(420, 60);
            lblInstructions.Size = new Size(170, 150);
            this.Controls.Add(lblInstructions);
        }

        private void LoadMaterials()
        {
            materialList.Items.Clear();
            foreach (var mat in mtlFile.Materials)
            {
                ListViewItem item = new ListViewItem(string.Format("0x{0:X4}", mat.Offset));
                item.SubItems.Add(mat.Name);
                item.Tag = mat;
                materialList.Items.Add(item);
            }
        }

        // Mapping of model names to texture prefixes for animals
        private static readonly Dictionary<string, string> AnimalModelToTexturePrefix = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "abyssinian", "cat" },
            { "beagle", "dog" },
            { "catexotic", "cat" },
            { "catshorthair", "cat" },
            { "dachsund", "dog" },
            { "dalmatian", "dog" },
            { "ferret", "ferret" },
            { "guineapig", "guineaPig" },
            { "horse", "horse" },
            { "husky", "dog" },
            { "labrador", "dog" },
            { "rabbit", "rabbit" },
            { "schnauzer", "dog" },
            { "siamese", "cat" }
        };

        // Known texture variants for each animal type
        private static readonly Dictionary<string, string[]> AnimalTextureVariants = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "cat", new[] { 
                "catAbyssinianRuddy", "catAbyssinianFawn", "catAbyssinianRed", "catAbyssinianBlue",
                "catExoticBlue", "catExoticChocolate", "catExoticRed", "catExoticWhite",
                "catShorthairBlue", "catShorthairChocolate", "catShorthairRed", "catShorthairWhite",
                "catSiameseBluePoint", "catSiameseChocolatePoint", "catSiameseSealPoint", "catSiameseLilacPoint"
            }},
            { "dog", new[] {
                "dogBeagleLemonWhite", "dogBeagleRedWhite", "dogBeagleTricolor",
                "dogDachsundBlack", "dogDachsundChocolate", "dogDachsundRed",
                "dogDalmatianBlack", "dogDalmatianBrown",
                "dogHuskyBlack", "dogHuskyGrey", "dogHuskyRed",
                "dogLabradorBlack", "dogLabradorChocolate", "dogLabradorYellow",
                "dogSchnauzerBlack", "dogSchnauzerSaltPepper", "dogSchnauzerWhite"
            }},
            { "guineaPig", new[] {
                "guineaPigAgoutiChocolate", "guineaPigAgoutiCinnamon", "guineaPigAgoutiRed", "guineaPigAgoutiSilver",
                "guineaPigDutchChocolate", "guineaPigDutchCinnamon", "guineaPigDutchRed", "guineaPigDutchSilver",
                "guineaPigHimalayanBlack", "guineaPigHimalayanChocolate", "guineaPigHimalayanDarkGrey",
                "guineaPigSelfBeige", "guineaPigSelfChocolate", "guineaPigSelfRed", "guineaPigSelfWhite"
            }},
            { "horse", new[] {
                "horseTrakehnerBay", "horseTrakehnerBlack", "horseTrakehnerChestnut", "horseTrakehnerGrey",
                "horseThoroughbredBay", "horseThoroughbredBlack", "horseThoroughbredChestnut", "horseThoroughbredGrey"
            }},
            { "rabbit", new[] {
                "rabbitHarlequinBlack", "rabbitHarlequinBlue", "rabbitHarlequinChocolate",
                "rabbitHimalayanBlack", "rabbitHimalayanBlue", "rabbitHimalayanChocolate",
                "rabbitNetherlandBlack", "rabbitNetherlandChocolate", "rabbitNetherlandOrange", "rabbitNetherlandWhite"
            }},
            { "ferret", new[] {
                "ferretAlbino", "ferretChampagne", "ferretChocolate", "ferretSable"
            }}
        };

        // Mapping of condo levels to texture suffix
        private static readonly Dictionary<string, string> CondoLevelToTextureSuffix = new Dictionary<string, string>
        {
            { "0", "Basic" },
            { "1", "Extra" },
            { "2", "Super" }
        };

        // Condo texture names for each animal
        private static readonly Dictionary<string, string[]> CondoTextureVariants = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "cat", new[] { "condoCat0", "condoCat1", "condoCat2" }},
            { "dog", new[] { "condoDog0", "condoDog1", "condoDog2" }},
            { "guineapig", new[] { "condoGuineaPig0", "condoGuineaPig1", "condoGuineaPig2" }},
            { "horse", new[] { "condoHorse0", "condoHorse1", "condoHorse2" }},
            { "rabbit", new[] { "condoRabbit0", "condoRabbit1", "condoRabbit2" }},
            { "ferret", new[] { "condoFerret0", "condoFerret1", "condoFerret2" }}
        };

        private void ScanAvailableTextures()
        {
            availableTextures.Clear();
            cmbAvailableTextures.Items.Clear();

            try
            {
                // Get the MTL filename to determine what type of model this is
                string mtlName = Path.GetFileNameWithoutExtension(originalFile.name).ToLower();

                // Determine the type of model and what textures to show
                bool isAnimalModel = false;
                bool isCondoModel = false;
                string animalType = "";
                string suffix = "";

                // Check if it's a condo model (format: condo<animal>_<level>)
                if (mtlName.StartsWith("condo"))
                {
                    isCondoModel = true;
                    // Extract animal: condo<animal>_<level>
                    string afterCondo = mtlName.Substring(5); // Remove "condo"
                    int underscoreIdx = afterCondo.LastIndexOf('_');
                    if (underscoreIdx > 0)
                    {
                        animalType = afterCondo.Substring(0, underscoreIdx);
                    }
                }
                // Check if it's an animal model (format: <breed>_hi or <breed>_lo)
                else if (mtlName.EndsWith("_hi") || mtlName.EndsWith("_lo"))
                {
                    suffix = mtlName.EndsWith("_hi") ? "_hi" : "_lo";
                    string modelBase = mtlName.Substring(0, mtlName.Length - 3); // Remove _hi or _lo

                    // Check if this model base matches any known animal
                    if (AnimalModelToTexturePrefix.ContainsKey(modelBase))
                    {
                        isAnimalModel = true;
                        animalType = AnimalModelToTexturePrefix[modelBase];
                    }
                }

                // Build the list of available textures
                if (isCondoModel && CondoTextureVariants.ContainsKey(animalType))
                {
                    foreach (string tex in CondoTextureVariants[animalType])
                    {
                        availableTextures.Add(tex);
                    }
                }
                else if (isAnimalModel && AnimalTextureVariants.ContainsKey(animalType))
                {
                    foreach (string tex in AnimalTextureVariants[animalType])
                    {
                        availableTextures.Add(tex + suffix);
                    }
                }
                else
                {
                    // For unknown models, show the current material names as reference
                    foreach (var mat in mtlFile.Materials)
                    {
                        if (!mat.Name.ToLower().Contains("shadow") && !mat.Name.ToLower().Contains("occ"))
                        {
                            if (!availableTextures.Contains(mat.Name))
                                availableTextures.Add(mat.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error scanning textures: " + ex.Message);
                lblStatus.Text = "Error: " + ex.Message;
            }

            // Sort and populate dropdown
            availableTextures.Sort();
            foreach (string tex in availableTextures)
            {
                cmbAvailableTextures.Items.Add(tex);
            }

            // Update status
            if (availableTextures.Count > 0)
            {
                lblStatus.Text = $"Found {availableTextures.Count} texture variants";
                lblStatus.ForeColor = Color.Blue;
            }
            else
            {
                cmbAvailableTextures.Items.Add("(Type texture name manually)");
                lblStatus.Text = "Model type not recognized. Type texture name manually.";
                lblStatus.ForeColor = Color.Orange;
            }
        }

        private void MaterialList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (materialList.SelectedItems.Count > 0)
            {
                MTL.MaterialEntry mat = (MTL.MaterialEntry)materialList.SelectedItems[0].Tag;
                txtNewTexture.Text = mat.Name;
            }
        }

        private void CmbAvailableTextures_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbAvailableTextures.SelectedItem != null)
            {
                txtNewTexture.Text = cmbAvailableTextures.SelectedItem.ToString();
            }
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (materialList.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a material first.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNewTexture.Text))
            {
                MessageBox.Show("Please enter a texture name.", "Empty Name", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MTL.MaterialEntry mat = (MTL.MaterialEntry)materialList.SelectedItems[0].Tag;
            string oldName = mat.Name;
            mat.Name = txtNewTexture.Text.Trim();

            // Update list view
            materialList.SelectedItems[0].SubItems[1].Text = mat.Name;

            lblStatus.Text = string.Format("Changed: {0} → {1}", oldName, mat.Name);
            lblStatus.ForeColor = Color.Green;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Generate the modified MTL data
                byte[] newData = MTL.Write(mtlFile);

                // Save to a temp file
                string tempFile = pluginHost.Get_TempFile();
                File.WriteAllBytes(tempFile, newData);

                // Tell Tinke to use this new file
                pluginHost.ChangeFile(originalFile.id, tempFile);

                lblStatus.Text = "✓ Changes saved! Use File > Save ROM to export.";
                lblStatus.ForeColor = Color.Green;

                MessageBox.Show(
                    "Changes saved to ROM!\n\n" +
                    "To export the modified ROM:\n" +
                    "1. Go to File menu\n" +
                    "2. Click 'Save ROM'\n" +
                    "3. Choose a location for the new ROM file",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("Error saving file: " + ex.Message, "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
