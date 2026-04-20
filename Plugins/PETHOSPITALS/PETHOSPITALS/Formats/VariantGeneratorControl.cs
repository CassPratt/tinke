// ----------------------------------------------------------------------
// <copyright file="VariantGeneratorControl.cs" company="none">
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
using System.Windows.Forms;
using Ekona;

namespace PETHOSPITALS.Formats
{
    /// <summary>
    /// UI Control for generating animal model variants
    /// </summary>
    public class VariantGeneratorControl : UserControl
    {
        private IPluginHost pluginHost;
        private string modelFolder;
        private ComboBox cmbModel;
        private ComboBox cmbSuffix;
        private ListBox lstVariants;
        private Button btnGenerate;
        private Button btnSelectOutput;
        private TextBox txtOutputFolder;
        private Label lblStatus;
        private ProgressBar progressBar;
        private RichTextBox txtLog;

        public VariantGeneratorControl(IPluginHost pluginHost, string modelFolder)
        {
            this.pluginHost = pluginHost;
            this.modelFolder = modelFolder;
            InitializeComponent();
            LoadModels();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(650, 450);
            this.Dock = DockStyle.Fill;
            this.AutoScroll = true;

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "Animal Model Variant Generator";
            lblTitle.Font = new Font(this.Font.FontFamily, 14, FontStyle.Bold);
            lblTitle.Location = new Point(10, 10);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Description
            Label lblDesc = new Label();
            lblDesc.Text = "Generates MTL, PDL, PRL, SKL files for each animal texture variant.\n" +
                          "This allows each color variant to have correctly mapped textures.";
            lblDesc.Location = new Point(10, 40);
            lblDesc.Size = new Size(600, 35);
            this.Controls.Add(lblDesc);

            // Model selection
            Label lblModel = new Label();
            lblModel.Text = "Select Model:";
            lblModel.Location = new Point(10, 85);
            lblModel.AutoSize = true;
            this.Controls.Add(lblModel);

            cmbModel = new ComboBox();
            cmbModel.Location = new Point(110, 82);
            cmbModel.Size = new Size(150, 25);
            cmbModel.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbModel.SelectedIndexChanged += CmbModel_SelectedIndexChanged;
            this.Controls.Add(cmbModel);

            // Suffix selection
            Label lblSuffix = new Label();
            lblSuffix.Text = "Quality:";
            lblSuffix.Location = new Point(280, 85);
            lblSuffix.AutoSize = true;
            this.Controls.Add(lblSuffix);

            cmbSuffix = new ComboBox();
            cmbSuffix.Location = new Point(340, 82);
            cmbSuffix.Size = new Size(80, 25);
            cmbSuffix.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSuffix.Items.Add("_hi");
            cmbSuffix.Items.Add("_lo");
            cmbSuffix.SelectedIndex = 0;
            this.Controls.Add(cmbSuffix);

            // Variants list
            Label lblVariants = new Label();
            lblVariants.Text = "Texture Variants to Generate:";
            lblVariants.Location = new Point(10, 115);
            lblVariants.AutoSize = true;
            this.Controls.Add(lblVariants);

            lstVariants = new ListBox();
            lstVariants.Location = new Point(10, 135);
            lstVariants.Size = new Size(250, 120);
            lstVariants.SelectionMode = SelectionMode.None;
            this.Controls.Add(lstVariants);

            // Output folder
            Label lblOutput = new Label();
            lblOutput.Text = "Output Folder:";
            lblOutput.Location = new Point(280, 115);
            lblOutput.AutoSize = true;
            this.Controls.Add(lblOutput);

            txtOutputFolder = new TextBox();
            txtOutputFolder.Location = new Point(280, 135);
            txtOutputFolder.Size = new Size(280, 25);
            txtOutputFolder.ReadOnly = true;
            this.Controls.Add(txtOutputFolder);

            btnSelectOutput = new Button();
            btnSelectOutput.Text = "...";
            btnSelectOutput.Location = new Point(565, 134);
            btnSelectOutput.Size = new Size(30, 25);
            btnSelectOutput.Click += BtnSelectOutput_Click;
            this.Controls.Add(btnSelectOutput);

            // Generate button
            btnGenerate = new Button();
            btnGenerate.Text = "🔧 Generate Variants";
            btnGenerate.Location = new Point(280, 170);
            btnGenerate.Size = new Size(150, 35);
            btnGenerate.BackColor = Color.LightGreen;
            btnGenerate.Font = new Font(this.Font, FontStyle.Bold);
            btnGenerate.Click += BtnGenerate_Click;
            this.Controls.Add(btnGenerate);

            // Progress bar
            progressBar = new ProgressBar();
            progressBar.Location = new Point(280, 215);
            progressBar.Size = new Size(310, 20);
            progressBar.Visible = false;
            this.Controls.Add(progressBar);

            // Status
            lblStatus = new Label();
            lblStatus.Text = "";
            lblStatus.Location = new Point(280, 240);
            lblStatus.Size = new Size(310, 20);
            lblStatus.ForeColor = Color.Blue;
            this.Controls.Add(lblStatus);

            // Log
            Label lblLog = new Label();
            lblLog.Text = "Log:";
            lblLog.Location = new Point(10, 265);
            lblLog.AutoSize = true;
            this.Controls.Add(lblLog);

            txtLog = new RichTextBox();
            txtLog.Location = new Point(10, 285);
            txtLog.Size = new Size(590, 150);
            txtLog.ReadOnly = true;
            txtLog.Font = new Font("Consolas", 8);
            this.Controls.Add(txtLog);
        }

        private void LoadModels()
        {
            cmbModel.Items.Clear();
            foreach (string model in AnimalVariantGenerator.GetSupportedModels())
            {
                cmbModel.Items.Add(model);
            }
            if (cmbModel.Items.Count > 0)
            {
                cmbModel.SelectedIndex = 0;
            }
        }

        private void CmbModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            lstVariants.Items.Clear();
            if (cmbModel.SelectedItem != null)
            {
                string model = cmbModel.SelectedItem.ToString();
                string[] variants = AnimalVariantGenerator.GetTextureVariants(model);
                foreach (string variant in variants)
                {
                    lstVariants.Items.Add(variant);
                }
            }
        }

        private void BtnSelectOutput_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select output folder for generated model variants";
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtOutputFolder.Text = fbd.SelectedPath;
                }
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (cmbModel.SelectedItem == null)
            {
                MessageBox.Show("Please select a model.", "No Model Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtOutputFolder.Text))
            {
                MessageBox.Show("Please select an output folder.", "No Output Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string model = cmbModel.SelectedItem.ToString();
            string suffix = cmbSuffix.SelectedItem.ToString();

            try
            {
                btnGenerate.Enabled = false;
                progressBar.Visible = true;
                progressBar.Value = 0;
                txtLog.Clear();

                Log($"Starting generation for {model}{suffix}...");
                Log($"Model folder: {modelFolder}");
                Log($"Output folder: {txtOutputFolder.Text}");
                Log("");

                List<string> generatedFiles = AnimalVariantGenerator.GenerateVariants(
                    modelFolder,
                    model,
                    suffix,
                    txtOutputFolder.Text
                );

                progressBar.Value = 100;

                Log($"Generated {generatedFiles.Count} files:");
                foreach (string file in generatedFiles)
                {
                    Log($"  ✓ {Path.GetFileName(file)}");
                }

                lblStatus.Text = $"✓ Generated {generatedFiles.Count} files";
                lblStatus.ForeColor = Color.Green;

                MessageBox.Show(
                    $"Successfully generated {generatedFiles.Count} variant files!\n\n" +
                    $"Output folder: {txtOutputFolder.Text}\n\n" +
                    "These files can be added to the ROM to support individual texture variants.",
                    "Generation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                lblStatus.Text = "Error: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show($"Error generating variants:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerate.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private void Log(string message)
        {
            txtLog.AppendText(message + "\n");
            txtLog.ScrollToCaret();
            Application.DoEvents();
        }
    }
}
