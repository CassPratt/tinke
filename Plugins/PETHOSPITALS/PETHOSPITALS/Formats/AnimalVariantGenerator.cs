// ----------------------------------------------------------------------
// <copyright file="AnimalVariantGenerator.cs" company="none">
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
using System.IO;
using System.Text;

namespace PETHOSPITALS.Formats
{
    /// <summary>
    /// Generates model file variants (MTL, PDL, PRL, SKL) for each animal texture variant.
    /// This allows each animal color variant to have its own model files with correct texture references.
    /// </summary>
    public static class AnimalVariantGenerator
    {
        /// <summary>
        /// Mapping from model base name to texture variants.
        /// Key: model name (e.g., "abyssinian")
        /// Value: list of texture names without suffix (e.g., "catAbyssinianRuddy")
        /// </summary>
        public static readonly Dictionary<string, string[]> ModelToTextureVariants = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // Cats
            { "abyssinian", new[] { "catAbyssinianRuddy", "catAbyssinianFawn", "catAbyssinianRed", "catAbyssinianBlue" } },
            { "catexotic", new[] { "catExoticBrown", "catExoticCream", "catExoticRed", "catExoticWhite" } },
            { "catshorthair", new[] { "catShortHairBlue", "catShortHairBrown", "catShortHairRed", "catShortHairSilver" } },
            { "siamese", new[] { "catSiameseBluePoint", "catSiameseChocolatePoint", "catSiameseLilacPoint" } },

            // Dogs
            { "beagle", new[] { "dogBeagleRed", "dogBeagleTricolor", "dogBeagleWhite" } },
            { "dachsund", new[] { "dogDachsundCream", "dogDachsundRed", "dogDachsundStandard" } },
            { "dalmatian", new[] { "dogDalmatianBlack", "dogDalmatianBlue", "dogDalmatianLemon" } },
            { "husky", new[] { "dogHuskyCopper", "dogHuskyGrey", "dogHuskyWhite" } },
            { "labrador", new[] { "dogLabradorBlack", "dogLabradorChocolate", "dogLabradorYellow" } },
            { "schnauzer", new[] { "dogSchnauzerBlack", "dogSchnauzerStandard", "dogSchnauzerWhite" } },

            // Ferrets
            { "ferret", new[] { 
                "ferretRoanBlack", "ferretRoanChocolate", "ferretRoanSilver",
                "ferretSiameseChampagne", "ferretSiameseSable",
                "ferretSolidBlack", "ferretSolidChocolate", "ferretSolidWhite",
                "ferretStandardBlack", "ferretStandardChocolate", "ferretStandardCinnamon"
            } },

            // Guinea Pigs
            { "guineapig", new[] {
                "guineaPigAgoutiChocolate", "guineaPigAgoutiCinnamon", "guineaPigAgoutiRed", "guineaPigAgoutiSilver",
                "guineaPigDutchChocolate", "guineaPigDutchCinnamon", "guineaPigDutchRed", "guineaPigDutchSilver",
                "guineaPigHimalayanBlack", "guineaPigHimalayanChocolate", "guineaPigHimalayanDarkGrey",
                "guineaPigSelfBeige", "guineaPigSelfChocolate", "guineaPigSelfRed", "guineaPigSelfWhite"
            } },

            // Horses
            { "horse", new[] {
                "horseAndalusianCream", "horseAndalusianGrey", "horseAndalusianWhite",
                "horseHanoverianBlack", "horseHanoverianChestnut", "horseHanoverianGrey",
                "horseThroughbredBay", "horseThroughbredBlack", "horseThroughbredChestnut",
                "horseTrakehnerBay", "horseTrakehnerBlack", "horseTrakehnerGrey"
            } },

            // Rabbits
            { "rabbit", new[] {
                "rabbitDutchBlack", "rabbitDutchBlue", "rabbitDutchChocolate", "rabbitDutchTortoise",
                "rabbitEnglishSpotsBlack", "rabbitEnglishSpotsBlue", "rabbitEnglishSpotsChocolate",
                "rabbitHarlequinBlack", "rabbitHarlequinBlue", "rabbitHarlequinChocolate", "rabbitHarlequinLilac",
                "rabbitRexCalifornian", "rabbitRexRed", "rabbitRexSable", "rabbitRexWhite"
            } }
        };

        /// <summary>
        /// Model file extensions that should be copied for each variant
        /// </summary>
        public static readonly string[] ModelExtensions = { ".mtl", ".pdl", ".prl", ".skl" };

        /// <summary>
        /// Generates all variant files for a given model
        /// </summary>
        /// <param name="modelFolder">Path to the model folder</param>
        /// <param name="modelBaseName">Base name of the model (e.g., "guineapig")</param>
        /// <param name="suffix">Quality suffix (_hi or _lo)</param>
        /// <param name="outputFolder">Folder to output generated files</param>
        /// <returns>List of generated file paths</returns>
        public static List<string> GenerateVariants(string modelFolder, string modelBaseName, string suffix, string outputFolder)
        {
            List<string> generatedFiles = new List<string>();
            string modelKey = modelBaseName.ToLower();

            if (!ModelToTextureVariants.ContainsKey(modelKey))
            {
                throw new ArgumentException($"Unknown model: {modelBaseName}. Supported models: {string.Join(", ", ModelToTextureVariants.Keys)}");
            }

            string[] textureVariants = ModelToTextureVariants[modelKey];

            // Read the original MTL to find the texture pattern
            string originalMtlPath = Path.Combine(modelFolder, $"{modelBaseName}{suffix}.mtl");
            if (!File.Exists(originalMtlPath))
            {
                throw new FileNotFoundException($"Original MTL not found: {originalMtlPath}");
            }

            byte[] originalMtlData = File.ReadAllBytes(originalMtlPath);
            string originalTexture = FindMainTexture(originalMtlData, suffix);

            if (string.IsNullOrEmpty(originalTexture))
            {
                throw new Exception($"Could not find texture reference in {originalMtlPath}");
            }

            // Create output folder if needed
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // Generate variant for each texture
            foreach (string textureVariant in textureVariants)
            {
                string newTextureName = textureVariant + suffix;
                string variantName = GetVariantName(modelBaseName, textureVariant, suffix);

                // Generate MTL with new texture
                byte[] newMtlData = ReplaceTextureInMtl(originalMtlData, originalTexture, newTextureName);
                string newMtlPath = Path.Combine(outputFolder, $"{variantName}.mtl");
                File.WriteAllBytes(newMtlPath, newMtlData);
                generatedFiles.Add(newMtlPath);

                // Copy other model files (PDL, PRL, SKL)
                foreach (string ext in ModelExtensions)
                {
                    if (ext == ".mtl") continue; // Already handled

                    string originalFile = Path.Combine(modelFolder, $"{modelBaseName}{suffix}{ext}");
                    if (File.Exists(originalFile))
                    {
                        string newFile = Path.Combine(outputFolder, $"{variantName}{ext}");
                        File.Copy(originalFile, newFile, true);
                        generatedFiles.Add(newFile);
                    }
                }
            }

            return generatedFiles;
        }

        /// <summary>
        /// Gets the variant name for output files
        /// </summary>
        private static string GetVariantName(string modelBase, string textureVariant, string suffix)
        {
            // e.g., "guineapig" + "guineaPigAgoutiChocolate" + "_hi" -> "guineapig_AgoutiChocolate_hi"
            // We extract the breed+color part from the texture name
            
            // Find where the model name ends in the texture name
            string lowerModel = modelBase.ToLower();
            string lowerTexture = textureVariant.ToLower();
            
            int modelEndIdx = lowerTexture.IndexOf(lowerModel);
            if (modelEndIdx >= 0)
            {
                modelEndIdx += lowerModel.Length;
                // Handle special cases like "guineapig" vs "guineaPig"
                if (modelEndIdx < textureVariant.Length)
                {
                    string breedColor = textureVariant.Substring(modelEndIdx);
                    return $"{modelBase}_{breedColor}{suffix}";
                }
            }

            // Fallback: use full texture name
            return $"{modelBase}_{textureVariant}{suffix}";
        }

        /// <summary>
        /// Finds the main texture name in MTL data
        /// </summary>
        private static string FindMainTexture(byte[] mtlData, string suffix)
        {
            string text = Encoding.ASCII.GetString(mtlData);
            
            // Look for texture names with the suffix
            int idx = 0;
            while (idx < text.Length)
            {
                int suffixIdx = text.IndexOf(suffix, idx, StringComparison.OrdinalIgnoreCase);
                if (suffixIdx < 0) break;

                // Find the start of this texture name (scan backwards)
                int nameStart = suffixIdx;
                while (nameStart > 0 && IsValidTextureChar(text[nameStart - 1]))
                {
                    nameStart--;
                }

                string textureName = text.Substring(nameStart, suffixIdx - nameStart + suffix.Length);
                
                // Skip shadow/utility textures
                if (!textureName.ToLower().Contains("shadow") && 
                    !textureName.ToLower().Contains("occ") &&
                    !textureName.ToLower().Contains("tongue") &&
                    textureName.Length > 10)
                {
                    return textureName;
                }

                idx = suffixIdx + 1;
            }

            return null;
        }

        private static bool IsValidTextureChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
        }

        /// <summary>
        /// Replaces texture name in MTL data
        /// </summary>
        private static byte[] ReplaceTextureInMtl(byte[] mtlData, string oldTexture, string newTexture)
        {
            if (oldTexture.Length != newTexture.Length)
            {
                // Pad or truncate new texture to match old length
                if (newTexture.Length < oldTexture.Length)
                {
                    newTexture = newTexture.PadRight(oldTexture.Length, '\0');
                }
                else
                {
                    newTexture = newTexture.Substring(0, oldTexture.Length);
                }
            }

            byte[] result = (byte[])mtlData.Clone();
            byte[] oldBytes = Encoding.ASCII.GetBytes(oldTexture);
            byte[] newBytes = Encoding.ASCII.GetBytes(newTexture);

            // Find and replace all occurrences
            for (int i = 0; i <= result.Length - oldBytes.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < oldBytes.Length; j++)
                {
                    if (result[i + j] != oldBytes[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    Array.Copy(newBytes, 0, result, i, newBytes.Length);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all supported model names
        /// </summary>
        public static string[] GetSupportedModels()
        {
            string[] models = new string[ModelToTextureVariants.Count];
            ModelToTextureVariants.Keys.CopyTo(models, 0);
            return models;
        }

        /// <summary>
        /// Gets texture variants for a model
        /// </summary>
        public static string[] GetTextureVariants(string modelName)
        {
            if (ModelToTextureVariants.ContainsKey(modelName.ToLower()))
            {
                return ModelToTextureVariants[modelName.ToLower()];
            }
            return new string[0];
        }
    }
}
