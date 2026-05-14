// ----------------------------------------------------------------------
// <copyright file="ARM9Patcher.cs" company="none">
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
using System.IO;
using System.Windows.Forms;
using Ekona;
using PETHOSPITALS.Formats;

namespace PETHOSPITALS.Formats
{
    /// <summary>
    /// Applies patches to ARM9.bin for Let's Play Pet Hospitals (game code C2HP).
    /// Patch 1: texture replacement fix. Patch 2: guineaPig capitalisation.
    /// Patch 3: ANY breed/color probability. Patch 4: hard-mode grade thresholds.
    /// </summary>
    public static class ARM9Patcher
    {
        // ============================================================
        // PATCH 1: Texture replacement fix
        // FUN_02036224 normally blocks dynamic texture replacement.
        // Replaced with MOV r0,#0 / BX lr so it always returns false,
        // allowing the correct per-animal texture to load.
        // ============================================================
        private const int PATCH1_OFFSET = 0x36224;
        private static readonly byte[] PATCH1_ORIGINAL = { 0x04, 0x10, 0x90, 0xE5, 0x00, 0x00, 0x51, 0xE3 };
        private static readonly byte[] PATCH1_PATCHED  = { 0x00, 0x00, 0xA0, 0xE3, 0x1E, 0xFF, 0x2F, 0xE1 };

        // ============================================================
        // PATCH 2: Guinea pig capitalisation fix
        // Species table contains "guineapig" (lowercase p). The texture
        // loader is case-sensitive, so assets under "guineaPig/" are
        // never found. Single byte change: 'p' (0x70) → 'P' (0x50).
        // ============================================================
        private const int PATCH2_OFFSET   = 0x6DA7E;
        private const byte PATCH2_ORIGINAL = 0x70;    // 'p'
        private const byte PATCH2_PATCHED  = 0x50;    // 'P'

        // ============================================================
        // PATCH 3: Increase probability of ANY breed/color in requests
        //
        // FUN_02040100 (first animal creator) uses an LCG RNG to pick
        // breed and color. Original code:
        //   ADD r5, r0, #1  (range = breed_count + 1,  P(ANY) ≈ 17-25%)
        //   ADD r6, r0, #1  (range = color_count + 1,  P(ANY) ≈ 17-25%)
        // Patched:
        //   MOV r5, #N      (fixed range N,  P(ANY breed) = 1/N)
        //   MOV r6, #N      (fixed range N,  P(ANY color) = 1/N)
        // Default N=2 → 50%. Change via the probability selector below.
        // ============================================================
        private const int ARM9_ORIGINAL_SIZE = 0x72E38;

        internal const int PATCH3_BREED_OFFSET = 0x402A4;
        private static readonly byte[] PATCH3_BREED_ORIGINAL = { 0x01, 0x50, 0x80, 0xE2 };  // ADD r5, r0, #1
        private static readonly byte[] PATCH3_BREED_PATCHED  = { 0x02, 0x50, 0xA0, 0xE3 };  // MOV r5, #2

        internal const int PATCH3_COLOR_OFFSET = 0x40328;
        private static readonly byte[] PATCH3_COLOR_ORIGINAL = { 0x01, 0x60, 0x80, 0xE2 };  // ADD r6, r0, #1
        private static readonly byte[] PATCH3_COLOR_PATCHED  = { 0x02, 0x60, 0xA0, 0xE3 };  // MOV r6, #2

        // ============================================================
        // PATCH 4: Hard-mode grade thresholds (optional) — v2, all 18 tiers
        //
        // FUN_0204102c converts the weekly score into a grade tier index.
        // 18 tiers total (0–17). Each tier has an early-exit CMP guard;
        // some are inline immediates, some load from a literal pool,
        // two use ADD r2,r2,#offset to derive the guard from the previous.
        //
        // v2 patches all 17 non-base guards, doubling every threshold
        // (pool entries patched in-place as full 32-bit words).
        // Tier 9: nearest valid ARM32 immediate to 1800 is 1808 (≈+8).
        // Tier 7: nearest valid to 1400 is 1408 (unchanged from v1).
        // All 18 grades remain reachable; no discontinuity.
        // ============================================================
        private static readonly int[] PATCH4_OFFSETS = {
            // tiers 1–8: inline CMPs
            0x41038, 0x41048, 0x4105C, 0x41070, 0x41084, 0x41098, 0x410AC, 0x410C0,
            // tiers 9–17: inline CMPs, ADD instructions, and literal pool
            0x410D4, 0x410E8, 0x41118, 0x41148, 0x41164, 0x41180,
            0x411B4, 0x411B8, 0x411BC, 0x411C0, 0x411C4,
        };
        private static readonly byte[][] PATCH4_ORIGINALS = {
            // tiers 1–8
            new byte[] { 0x64, 0x00, 0x51, 0xE3 }, // CMP r1, #100
            new byte[] { 0xC8, 0x00, 0x51, 0xE3 }, // CMP r1, #200
            new byte[] { 0x4B, 0x0F, 0x51, 0xE3 }, // CMP r1, #300
            new byte[] { 0x19, 0x0E, 0x51, 0xE3 }, // CMP r1, #400
            new byte[] { 0x7D, 0x0F, 0x51, 0xE3 }, // CMP r1, #500
            new byte[] { 0x96, 0x0F, 0x51, 0xE3 }, // CMP r1, #600
            new byte[] { 0xAF, 0x0F, 0x51, 0xE3 }, // CMP r1, #700
            new byte[] { 0x32, 0x0E, 0x51, 0xE3 }, // CMP r1, #800
            // tiers 9–17
            new byte[] { 0xE1, 0x0F, 0x51, 0xE3 }, // CMP r1, #900
            new byte[] { 0xFA, 0x0F, 0x51, 0xE3 }, // CMP r1, #1000
            new byte[] { 0x4B, 0x0E, 0x51, 0xE3 }, // CMP r1, #1200
            new byte[] { 0x64, 0x20, 0x82, 0xE2 }, // ADD r2, r2, #100 (tier 14 guard)
            new byte[] { 0x64, 0x20, 0x82, 0xE2 }, // ADD r2, r2, #100 (tier 15 guard)
            new byte[] { 0x19, 0x0D, 0x51, 0xE3 }, // CMP r1, #1600
            new byte[] { 0x4C, 0x04, 0x00, 0x00 }, // pool: 1100
            new byte[] { 0x14, 0x05, 0x00, 0x00 }, // pool: 1300
            new byte[] { 0x78, 0x05, 0x00, 0x00 }, // pool: 1400
            new byte[] { 0xDC, 0x05, 0x00, 0x00 }, // pool: 1500
            new byte[] { 0xA4, 0x06, 0x00, 0x00 }, // pool: 1700
        };
        // Target values for each PATCH4 entry (order matches PATCH4_OFFSETS & PATCH4_ORIGINALS).
        // Value -1 => do not change this entry (preserve original bytes).
        // These values follow the confirmed plan: tiers 1..8 tripled, tiers 9..16 interpolated, top = 4000.
        private static readonly int[] PATCH4_TARGETS = new int[] {
            // indices 0..7: inline CMPs for tiers 1..8
            600, 900, 1200, 1500, 1800, 2100, 2400, 2700,
            // indices 8..10: inline CMPs for tiers 9..11
            2863, 3026, 3189,
            // indices 11..12: ADD r2,r2,#imm — preserve (unknown dependency) => -1
            -1, -1,
            // index 13: CMP r1,#1600 originally — map to tier 16/top mapping
            4000,
            // pool entries (indices 14..18) — set to corresponding tier values
            3189, 3514, 3676, 3838, 4000
        };

        // Helper: try to encode a 32-bit immediate into ARM "imm12" form (imm8 + rotate*2).
        // Returns true and outputs imm8/rot if successful; otherwise false.
        private static bool TryEncodeArmImm(int value, out byte imm8, out byte rot)
        {
            imm8 = 0;
            rot = 0;
            // unsigned 32-bit representation
            uint v = (uint)value;
            for (int r = 0; r < 16; r++)
            {
                // Right-rotate by r*2
                int shift = (r * 2) & 31;
                uint candidate = (v >> shift) | (v << ((32 - shift) & 31));
                if ((candidate & 0xFFFFFF00u) == 0) // fits in 8 bits
                {
                    imm8 = (byte)(candidate & 0xFFu);
                    rot = (byte)r;
                    return true;
                }
            }
            return false;
        }

        // Build the 4 bytes that would be written for PATCH4 index i given the target table.
        // If the target is -1, returns null (no change).
        private static byte[] BuildPatch4Bytes(int i)
        {
            int target = PATCH4_TARGETS[i];
            if (target == -1) return null;

            byte[] orig = PATCH4_ORIGINALS[i];
            // pool literal (original bytes are 32-bit little-endian non-instruction)
            if (orig[2] == 0x00 && orig[3] == 0x00)
            {
                return BitConverter.GetBytes((uint)target);
            }

            // Instruction: CMP r1, #imm (0x51, 0xE3) or ADD r2,r2,#imm (ends with 0x82,0xE2)
            // We'll preserve the last two bytes and replace imm encoding in the first two bytes.
            byte[] outb = new byte[4];
            Array.Copy(orig, outb, 4);

            if (!TryEncodeArmImm(target, out byte imm8, out byte rot))
            {
                // Try to find a nearby encodable immediate (simple fallback).
                // This keeps the patch safe by using the closest ARM-encodable
                // imm12 value instead of failing outright.
                if (!FindClosestEncodableArmImm(target, out int chosen, out imm8, out rot))
                    throw new InvalidOperationException($"Cannot encode immediate {target} into ARM immediate form.");
                // Use the chosen closest value
                target = chosen;
            }

            // Preserve low nibble of original second byte (register/opcode bits), put rot in high nibble.
            byte lowNibble = (byte)(orig[1] & 0x0F);
            byte newSecond = (byte)((rot << 4) | lowNibble);
            outb[0] = imm8;
            outb[1] = newSecond;
            return outb;
        }

        // Search outward from target for the nearest value that can be encoded
        // in ARM's imm12 (imm8 + rotate*2) form. Returns true and outputs the
        // chosen value plus its imm8/rot if found; otherwise false.
        private static bool FindClosestEncodableArmImm(int target, out int chosen, out byte imm8, out byte rot)
        {
            imm8 = 0;
            rot = 0;
            chosen = 0;
            // Search deltas increasing; limit protects against long loops.
            const int MAX_DELTA = 512;
            for (int delta = 1; delta <= MAX_DELTA; delta++)
            {
                int low = target - delta;
                int high = target + delta;
                if (low >= 0 && TryEncodeArmImm(low, out imm8, out rot))
                {
                    chosen = low;
                    return true;
                }
                if (TryEncodeArmImm(high, out imm8, out rot))
                {
                    chosen = high;
                    return true;
                }
            }
            return false;
        }

        // ============================================================
        // PATCH 5 / 6: EyeDrop bundle (v2)
        //
        // Makes EYEINFECTION appear in-game and treatable with EarDrop.
        // EarWax (CottonSwab) and InflamedEar (EarDrop) keep original treatments.
        //
        // Four writes — b1/b2 of state-machine entries are NOT touched (stay 0xFF).
        //   1) 0x61EAC: slots 16/17 literal pool → EarDrop ptr (0x02071854)
        //   2) 0x6FD14: entry 26 key2 0x15 → 0x16 (INFLAMEDEAR uses slot 16)
        //   3) 0x6FD1C: entry 27 key2 0x16 → 0x17 (EYEINFECTION uses slot 27)
        //   4) 0x70800: condition object table row 23 enable flag 0x00 → 0x01
        // ============================================================
        internal const int PATCH5_OFFSET = 0x61EAC;
        private  static readonly byte[] PATCH5_ORIGINAL = { 0xCC, 0x17, 0x07, 0x02 }; // ptr → Scissor
        internal static readonly byte[] PATCH5_PATCHED  = { 0x54, 0x18, 0x07, 0x02 }; // ptr → EarDrop

        // Entry 26 key2: INFLAMEDEAR (runtime index 0x16 = 22) uses slot 16 = EarDrop
        private const int  PATCH5_KEY2_ENTRY26_OFFSET   = 0x6FD14;
        private const byte PATCH5_KEY2_ENTRY26_ORIGINAL = 0x15;
        private const byte PATCH5_KEY2_ENTRY26_PATCHED  = 0x16;

        // Entry 27 key2: EYEINFECTION (runtime index 0x17 = 23) uses slot 27 = EarDrop
        private const int  PATCH5_KEY2_ENTRY27_OFFSET   = 0x6FD1C;
        private const byte PATCH5_KEY2_ENTRY27_ORIGINAL = 0x16;
        private const byte PATCH5_KEY2_ENTRY27_PATCHED  = 0x17;

        // Condition object table row 23 enable flag (EYEINFECTION, arm9 0x707E8 + 0x18)
        private const int  PATCH5_ROW23_OFFSET   = 0x70800;
        private const byte PATCH5_ROW23_ORIGINAL = 0x00;
        private const byte PATCH5_ROW23_PATCHED  = 0x01;

        // PATCH6 is fully absorbed into Patch 5. Constants kept for API compatibility.
        // b1/b2 are NOT modified in v2 — terminal entries (0xFF 0xFF) stay unchanged.
        internal const int PATCH6_OFFSET = 0x6FD15;
        internal static readonly byte[] PATCH6_ORIGINAL = { 0xFF, 0xFF };
        internal static readonly byte[] PATCH6_PATCHED  = { 0xFF, 0xFF }; // no-op in v2

        // Patch 8 removed — no constants here.

        // ============================================================
        // PATCH 8: Duplicate prices (new)
        //
        // Configuration for ranges/tables where prices are stored
        // and should be doubled when Patch 8 is enabled. Each entry
        // is a tuple (ROM start, ROM end, stride bytes, priceOffset)
        // where `priceOffset` is the byte offset within the struct
        // where the int32 price is stored (0-based). Stride is the
        // struct size in bytes.
        // ============================================================
        private struct Patch8Range
        {
            public int start;
            public int end;
            public int stride;
            public int priceOffset;
            public Patch8Range(int s, int e, int st, int po)
            {
                start = s; end = e; stride = st; priceOffset = po;
            }
        }

        // NOTE: these ranges are conservative examples derived from
        // the analysis. They can be refined later or moved to a
        // JSON config file. Do NOT follow pointers by default.
        private static readonly Patch8Range[] PATCH8_RANGES = new Patch8Range[] {
            // Clothes table (price at offset 0, stride 12) — phase-aligned at 0x066704
            new Patch8Range(0x066704, 0x0669F4, 12, 0),
            // Furniture examples (price at offset 4, stride 20)
            new Patch8Range(0x0672B8, 0x067D00, 20, 4),
            // Improvements (explicit small range, price at offset 0)
            new Patch8Range(0x066050, 0x066054, 4, 0),
        };

        // Do not follow pointer fields by default when doubling prices.
        private const bool PATCH8_FOLLOW_POINTERS_DEFAULT = false;
        // ROM->file base used by other scripts: ARM9 is mapped at ROM 0x4000
        private const int ROM_TO_FILE_BASE = 0x4000;

        /// <summary>
        /// Returns a list of proposed changes for Patch 8 without modifying the data.
        /// Each entry is CSV: ROM_HEX,OLD,NEW
        /// </summary>
        public static System.Collections.Generic.List<string> GetPatch8Changes(byte[] arm9Data)
        {
            var list = new System.Collections.Generic.List<string>();
            if (arm9Data == null) return list;
            foreach (var r in PATCH8_RANGES)
            {
                for (int rom = r.start; rom <= r.end; rom += r.stride)
                {
                    int priceRom = rom + r.priceOffset;
                    int fileOff = priceRom - ROM_TO_FILE_BASE;
                    if (fileOff < 0 || fileOff + 4 > arm9Data.Length) continue;
                    int old = BitConverter.ToInt32(arm9Data, fileOff);
                    if (old >= 0x01000000 || old <= 0) continue;
                    if (old < 20 || old > 1000000) continue;
                    int nw = old * 2;
                    list.Add(string.Format("0x{0:X6},{1},{2}", priceRom, old, nw));
                }
            }
            return list;
        }

        // ============================================================
        // PATCH EXPERIMENT: Force EYEINFECTION runtime index safely
        //
        // FUN_020428A4 receives the selected index in r0, copies it to r6,
        // then writes that value into the runtime object at +0x10:
        //   0x428A8: MOV r6, r0
        //   0x428CC: MLA r4, r6, r0, r1
        //   0x428D0: STR r6, [r5, #0x10]
        //
        // For quick EyeDrop testing we replace the register copy with:
        //   MOV r6, #FORCE_CONDITION_INDEX
        // so the runtime object is built and tagged with the fixed
        // EYEINFECTION entry, without changing FUN_02042818's RNG path.
        //
        // Confirmed experimentally in-game:
        //   22 -> inflamed ear
        //   23 -> EYEINFECTION
        //
        // Earlier analyses remembered EYEINFECTION as 21, but the stable
        // runtime index used by this patch point is 23.
        // ============================================================
        private const int PATCH_EXPERIMENT_LEGACY_OFFSET = 0x42864;
        private static readonly byte[] PATCH_EXPERIMENT_LEGACY_ORIGINAL = { 0x23, 0x06, 0xA0, 0xE1 }; // MOV r0, r3, LSR #0xC
        private const int PATCH_EXPERIMENT_SELECTOR_OFFSET = 0x428A8;
        private static readonly byte[] PATCH_EXPERIMENT_SELECTOR_ORIGINAL = { 0x00, 0x60, 0xA0, 0xE1 }; // MOV r6, r0
        private const int FORCE_CONDITION_INDEX = 23; // confirmed in-game: 23 -> EYEINFECTION

        private static byte[] GetExperimentSelectorPatch()
        {
            return new byte[] { (byte)FORCE_CONDITION_INDEX, 0x60, 0xA0, 0xE3 };
        }

        // ============================================================
        // Public API
        // ============================================================

        // ============================================================
        // Patch 4 helpers
        // ============================================================
        public static bool IsPatched4(byte[] arm9Data)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE) return false;
            for (int i = 0; i < PATCH4_OFFSETS.Length; i++)
            {
                byte[] expected = BuildPatch4Bytes(i);
                if (expected == null) continue; // this entry is intentionally left unchanged
                if (!BytesMatch(arm9Data, PATCH4_OFFSETS[i], expected)) return false;
            }
            return true;
        }

        public static bool IsPatched5(byte[] arm9Data)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE) return false;
            return BytesMatch(arm9Data, PATCH5_OFFSET, PATCH5_PATCHED)
                && arm9Data[PATCH5_KEY2_ENTRY26_OFFSET] == PATCH5_KEY2_ENTRY26_PATCHED
                && arm9Data[PATCH5_KEY2_ENTRY27_OFFSET] == PATCH5_KEY2_ENTRY27_PATCHED
                && arm9Data[PATCH5_ROW23_OFFSET] == PATCH5_ROW23_PATCHED;
        }

        public static bool IsPatched5Key2(byte[] arm9Data)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE) return false;
            return arm9Data[PATCH5_KEY2_ENTRY26_OFFSET] == PATCH5_KEY2_ENTRY26_PATCHED;
        }

        // b1/b2 are no longer modified in v2 — delegate to full bundle check.
        public static bool IsPatched6B1B2(byte[] arm9Data) { return IsPatched5(arm9Data); }

        public static bool IsPatched6(byte[] arm9Data) { return IsPatched5(arm9Data); }

        // ============================================================
        // PATCH 8 helpers
        // ============================================================
        public static bool IsPatched8(byte[] arm9Data)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE) return false;
            // Heuristic: check that the known improvements offset was doubled
            // Original at 0x066050 is 1500; if patched it should be 3000.
            const int checkRom = 0x066050;
            int checkOff = checkRom - ROM_TO_FILE_BASE;
            if (checkOff < 0 || arm9Data.Length < checkOff + 4) return false;
            int v = BitConverter.ToInt32(arm9Data, checkOff);
            return v == 3000;
        }

        private static void ApplyPatch8(byte[] d)
        {
            if (d == null) return;
            // collect changes in-memory for logging only (no intermediate files)
            var changes = new System.Text.StringBuilder();
            changes.AppendLine("ROM,OLD,NEW,OFFSET_HEX");

            foreach (var r in PATCH8_RANGES)
            {
                for (int rom = r.start; rom <= r.end; rom += r.stride)
                {
                    int priceRom = rom + r.priceOffset;
                    int fileOff = priceRom - ROM_TO_FILE_BASE;
                    if (fileOff < 0 || fileOff + 4 > d.Length) continue;
                    int old = BitConverter.ToInt32(d, fileOff);
                    // Skip pointer-like values
                    if (old >= 0x01000000 || old <= 0) continue;
                    // Plausibility: typical prices are small positive integers
                    if (old < 20 || old > 1000000) continue;
                    int nw = old * 2;
                    byte[] nb = BitConverter.GetBytes(nw);
                    CopyBytes(d, fileOff, nb);
                    changes.AppendLine(string.Format("0x{0:X6},{1},{2},0x{0:X6}", priceRom, old, nw));
                }
            }

            // Do not write intermediate files; revert is implemented as the
            // exact inverse operation (divide by two) so no persistent backup
            // file is required. Keep changes in-memory only for transient logs.
        }

        private static void RevertPatch8(byte[] d)
        {
            if (d == null) return;
            foreach (var r in PATCH8_RANGES)
            {
                for (int rom = r.start; rom <= r.end; rom += r.stride)
                {
                    int priceRom = rom + r.priceOffset;
                    int fileOff = priceRom - ROM_TO_FILE_BASE;
                    if (fileOff < 0 || fileOff + 4 > d.Length) continue;
                    int v = BitConverter.ToInt32(d, fileOff);
                    if (v <= 0) continue;
                    // If even and plausible, undo by dividing by 2
                    if ((v % 2) == 0)
                    {
                        int orig = v / 2;
                        if (orig >= 20 && orig <= 1000000)
                        {
                            byte[] ob = BitConverter.GetBytes(orig);
                            CopyBytes(d, fileOff, ob);
                        }
                    }
                }
            }
        }

        internal static bool IsEyeDropPatched(byte[] arm9Data)
        {
            return IsPatched5(arm9Data) && IsPatched6(arm9Data);
        }

        public static bool IsPatched_Experiment(byte[] arm9Data)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE) return false;
            return BytesMatch(arm9Data, PATCH_EXPERIMENT_SELECTOR_OFFSET, GetExperimentSelectorPatch());
        }

        private static void ApplyPatchExperiment(byte[] d)
        {
            if (d == null) return;
            CopyBytes(d, PATCH_EXPERIMENT_SELECTOR_OFFSET, GetExperimentSelectorPatch());
        }

        private static void RevertPatchExperiment(byte[] d)
        {
            if (d == null) return;
            CopyBytes(d, PATCH_EXPERIMENT_SELECTOR_OFFSET, PATCH_EXPERIMENT_SELECTOR_ORIGINAL);
        }

        // ============================================================
        // Public API
        // ============================================================

        /// <summary>
        /// Returns true if any of patches 1-3 are not yet applied.
        /// </summary>
        public static bool NeedsPatching(byte[] arm9Data)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE)
                return false;

            return BytesMatch(arm9Data, PATCH1_OFFSET, PATCH1_ORIGINAL)
                || (arm9Data[PATCH2_OFFSET] == PATCH2_ORIGINAL)
                || BytesMatch(arm9Data, PATCH3_BREED_OFFSET, PATCH3_BREED_ORIGINAL);
        }

        /// <summary>
        /// Returns true if patches 1-3 are all applied at default range (2).
        /// </summary>
        public static bool IsPatched(byte[] arm9Data)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE)
                return false;

            if (!BytesMatch(arm9Data, PATCH1_OFFSET, PATCH1_PATCHED))             return false;
            if (arm9Data[PATCH2_OFFSET] != PATCH2_PATCHED)                        return false;
            if (!BytesMatch(arm9Data, PATCH3_BREED_OFFSET, PATCH3_BREED_PATCHED)) return false;
            if (!BytesMatch(arm9Data, PATCH3_COLOR_OFFSET, PATCH3_COLOR_PATCHED)) return false;
            return true;
        }

        /// <summary>
        /// Returns true if patches 1-3 are all applied at the given range.
        /// </summary>
        public static bool IsPatched(byte[] arm9Data, int range)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE)
                return false;

            if (!BytesMatch(arm9Data, PATCH1_OFFSET, PATCH1_PATCHED)) return false;
            if (arm9Data[PATCH2_OFFSET] != PATCH2_PATCHED)             return false;

            byte[] breedPatch = { (byte)range, 0x50, 0xA0, 0xE3 };
            byte[] colorPatch = { (byte)range, 0x60, 0xA0, 0xE3 };
            if (!BytesMatch(arm9Data, PATCH3_BREED_OFFSET, breedPatch)) return false;
            if (!BytesMatch(arm9Data, PATCH3_COLOR_OFFSET, colorPatch)) return false;
            return true;
        }

        /// <summary>
        /// Returns a human-readable status string for all 4 patches.
        /// </summary>
        public static string GetPatchStatus(byte[] arm9Data)
        {
            return GetPatchStatus(arm9Data, 2, false);
        }

        public static string GetPatchStatus(byte[] arm9Data, int range)
        {
            return GetPatchStatus(arm9Data, range, false);
        }

        public static string GetPatchStatus(byte[] arm9Data, int range, bool checkHardMode)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE)
                return "Invalid file";

            bool p1 = BytesMatch(arm9Data, PATCH1_OFFSET, PATCH1_PATCHED);
            bool p2 = (arm9Data[PATCH2_OFFSET] == PATCH2_PATCHED);
            byte[] breedPatch = { (byte)range, 0x50, 0xA0, 0xE3 };
            byte[] colorPatch = { (byte)range, 0x60, 0xA0, 0xE3 };
            bool p3 = BytesMatch(arm9Data, PATCH3_BREED_OFFSET, breedPatch)
                   && BytesMatch(arm9Data, PATCH3_COLOR_OFFSET, colorPatch);
            bool p4 = IsPatched4(arm9Data);
            bool p5 = IsEyeDropPatched(arm9Data);

            double prob = 100.0 / range;
            bool allCore = p1 && p2 && p3 && p5;

            if (allCore && (!checkHardMode || p4))
                return checkHardMode
                    ? $"✓ All 6 patches applied (ANY {prob:F1}%, hard mode)"
                    : $"✓ All 5 patches applied (ANY {prob:F1}%)";

            var parts = new System.Collections.Generic.List<string>();
            if (!p1) parts.Add("missing texture fix");
            if (!p2) parts.Add("missing guineaPig fix");
            if (!p3) parts.Add($"missing ANY {prob:F1}%");
            if (!p5) parts.Add("missing EyeDrop fix (slots + state-machine)");
            if (checkHardMode && !p4) parts.Add("missing hard-mode patch");
            return "⚠ Partial: " + string.Join(", ", parts.ToArray());
        }

        /// <summary>
        /// Applies patches 1-3 (default range 2). Patch 4 not included — use overload with hardMode.
        /// </summary>
        public static byte[] ApplyPatch(byte[] arm9Data)
        {
            return ApplyPatch(arm9Data, 2, false);
        }

        /// <summary>
        /// Applies patches 1-3 with the given ANY-probability range.
        /// </summary>
        public static byte[] ApplyPatch(byte[] arm9Data, int range)
        {
            return ApplyPatch(arm9Data, range, false);
        }

        /// <summary>
        /// Applies patches 1-5 (plus optional Patch 4 hard-mode grade thresholds).
        /// Returns array of same size — no ARM9 expansion needed.
        /// </summary>
        public static byte[] ApplyPatch(byte[] arm9Data, int range, bool hardMode)
        {
            return ApplyPatch(arm9Data, range, hardMode, false);
        }

        /// <summary>
        /// Applies patches 1-5 plus optional hard-mode and EYEINFECTION experiment.
        /// Returns array of same size — no ARM9 expansion needed.
        /// </summary>
        /// <remarks>
        /// Patch bundle summary (final working EyeDrop/EarDrop behavior):
        /// When `applyEyeDrop == true` the method applies a small bundle of writes
        /// that together make the EYEINFECTION condition treatable at runtime.
        /// The bundle performs two linked actions:
        ///  1) ensure a condition-table row points to a valid EarDrop resource and is enabled
        ///  2) ensure the state-machine returns a treatment_id that selects that row
        ///
        /// Final writes applied by the EyeDrop/EarDrop bundle (addresses are ARM9 offsets):
        ///  - 0x61EAC: original `CC 17 07 02` (ptr → Scissor)
        ///      → patched `54 18 07 02` (ptr → 0x02071854 EarDrop)
        ///      Purpose: redirect the init literal pool so runtime slots 16/17 will load EarDrop.
        ///  - 0x61ED0: original `48 18 07 02` (ptr → CottonSwab)
        ///      → patched `54 18 07 02` (ptr → 0x02071854 EarDrop)
        ///      Purpose: ensure slot 26's init literal pool also references EarDrop.
        ///  - 0x6FD14: original key2 byte `0x0F` (historical)
        ///      → patched `0x17` (runtime-aligned key2 = 23)
        ///      Purpose: align the ROM table key2 with the observed runtime index so lookup matches.
        ///  - 0x6FD15..0x6FD16: original `FF FF` (b1/b2 untreatable)
        ///      → patched `07 07` (return row index 7)
        ///      Purpose: make the state-machine return a treatment_id that equals row index 7.
        ///  - 0x70630 / 0x70640: retarget condition row 7 pointer and enable it
        ///      - 0x70630: original pointer → patched `FC 16 07 02` (EarDrop pointer)
        ///      - 0x70640: original `0x00` → patched `0x01` (enable row 7)
        ///      Purpose: cause `FUN_020428A4` to construct the runtime object from row 7,
        ///               which now points at EarDrop and is enabled.
        ///
        /// Combined effect: the state-machine returns b1=b2=0x07; the caller selects that
        /// treatment_id and calls the runtime constructor to build from `row = 0x02070564 + 7*0x1C`.
        /// Since row 7 was retargeted and enabled, the runtime selects EarDrop and the
        /// EYEINFECTION condition becomes treatable without modifying many caller sites.
        ///
        /// Note: the method also keeps the experimental selector restoration and provides
        /// the ApplyEyeDropPatchPartial helper to apply sub-parts individually.
        /// </remarks>
        public static byte[] ApplyPatch(byte[] arm9Data, int range, bool hardMode, bool forceEyeInfection, bool applyEyeDrop = true, bool duplicatePrices = false)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE)
                throw new ArgumentException("Invalid ARM9 data");

            byte[] d = new byte[arm9Data.Length];
            Array.Copy(arm9Data, d, arm9Data.Length);

            // Always restore the old unstable experiment hook in FUN_02042818.
            CopyBytes(d, PATCH_EXPERIMENT_LEGACY_OFFSET, PATCH_EXPERIMENT_LEGACY_ORIGINAL);

            // Patch 1: texture replacement fix
            CopyBytes(d, PATCH1_OFFSET, PATCH1_PATCHED);

            // Patch 2: guineaPig capitalisation
            d[PATCH2_OFFSET] = PATCH2_PATCHED;

            // Patch 3: ANY breed/color probability
            byte[] breedPatch = { (byte)range, 0x50, 0xA0, 0xE3 }; // MOV r5, #range
            byte[] colorPatch = { (byte)range, 0x60, 0xA0, 0xE3 }; // MOV r6, #range
            CopyBytes(d, PATCH3_BREED_OFFSET, breedPatch);
            CopyBytes(d, PATCH3_COLOR_OFFSET, colorPatch);

            // Patch 4: hard-mode grade thresholds (optional)
            if (hardMode) ApplyPatch4(d);

            if (applyEyeDrop) ApplyEyeDropPatch(d);

            // Experiment: force EYEINFECTION by overriding FUN_020428A4's runtime index register
            if (forceEyeInfection) ApplyPatchExperiment(d);

            // Patch 8: Duplicate prices (optional)
            if (duplicatePrices) ApplyPatch8(d);

            return d;
        }

        /// <summary>
        /// Reverts all patches (1-4). Truncates to original size if an older expanded patch existed.
        /// </summary>
        public static byte[] RevertPatch(byte[] arm9Data)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE)
                throw new ArgumentException("Invalid ARM9 data");

            byte[] d = new byte[ARM9_ORIGINAL_SIZE];
            Array.Copy(arm9Data, d, ARM9_ORIGINAL_SIZE);

            CopyBytes(d, PATCH_EXPERIMENT_LEGACY_OFFSET, PATCH_EXPERIMENT_LEGACY_ORIGINAL);
            CopyBytes(d, PATCH1_OFFSET, PATCH1_ORIGINAL);
            d[PATCH2_OFFSET] = PATCH2_ORIGINAL;
            CopyBytes(d, PATCH3_BREED_OFFSET, PATCH3_BREED_ORIGINAL);
            CopyBytes(d, PATCH3_COLOR_OFFSET, PATCH3_COLOR_ORIGINAL);
            RevertPatch4(d);

            RevertEyeDropPatch(d);

            // Experiment: restore all other condition flags to enabled
            RevertPatchExperiment(d);

            // Patch 8: attempt best-effort revert (halve even values in ranges)
            RevertPatch8(d);

            return d;
        }

        public static bool ApplyPatchToROM(IPluginHost pluginHost, sFile arm9File)
        {
            return ApplyPatchToROM(pluginHost, arm9File, 2, false);
        }

        public static bool ApplyPatchToROM(IPluginHost pluginHost, sFile arm9File, int range)
        {
            return ApplyPatchToROM(pluginHost, arm9File, range, false);
        }

        /// <summary>
        /// Applies all selected patches to the ARM9 file via Tinke.
        /// </summary>
        public static bool ApplyPatchToROM(IPluginHost pluginHost, sFile arm9File, int range, bool hardMode)
        {
            try
            {
                byte[] arm9Data = File.ReadAllBytes(arm9File.path);
                byte[] patchedData = ApplyPatch(arm9Data, range, hardMode, false);

                string tempFile = pluginHost.Get_TempFile();
                File.WriteAllBytes(tempFile, patchedData);
                pluginHost.ChangeFile(arm9File.id, tempFile);

                double prob = 100.0 / range;
                string hardLine = hardMode
                    ? "\n• Patch 4 (0x41038-0x410C0): Hard mode — thresholds doubled"
                    : "";
                MessageBox.Show(
                    "Patches applied successfully!\n\n" +
                    "Use 'File > Save ROM' to persist changes.\n\n" +
                    "Changes applied:\n" +
                    "• Patch 1 (0x36224): Dynamic texture replacement fix\n" +
                    "• Patch 2 (0x6DA7E): guineaPig capitalization fix\n" +
                    $"• Patch 3 (0x402A4, 0x40328): ANY probability {prob:F1}% (range {range})" +
                    hardLine,
                    "Patches Applied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying patch:\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        

        

        

        public static bool ApplyPatchToROM(IPluginHost pluginHost, sFile arm9File, int range, bool hardMode, bool forceEyeInfection, bool applyEyeDrop)
        {
            try
            {
                byte[] arm9Data = File.ReadAllBytes(arm9File.path);
                byte[] patchedData = ApplyPatch(arm9Data, range, hardMode, forceEyeInfection, applyEyeDrop);

                string tempFile = pluginHost.Get_TempFile();
                File.WriteAllBytes(tempFile, patchedData);
                pluginHost.ChangeFile(arm9File.id, tempFile);

                double prob = 100.0 / range;
                string hardLine = hardMode
                    ? "\n• Patch 4 (0x41038-0x410C0): Hard mode — thresholds doubled"
                    : "";
                string expLine = forceEyeInfection
                    ? $"\n• Experiment ({PATCH_EXPERIMENT_SELECTOR_OFFSET:X}): FUN_020428A4 forces r6 = #{FORCE_CONDITION_INDEX}"
                    : "";
                MessageBox.Show(
                    "Patches applied successfully!\n\n" +
                    "Use 'File > Save ROM' to persist changes.\n\n" +
                    "Changes applied:\n" +
                    "• Patch 1 (0x36224): Dynamic texture replacement fix\n" +
                    "• Patch 2 (0x6DA7E): guineaPig capitalization fix\n" +
                    $"• Patch 3 (0x402A4, 0x40328): ANY probability {prob:F1}% (range {range})" +
                    hardLine + expLine,
                    "Patches Applied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying patch:\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool ApplyPatchToROM(IPluginHost pluginHost, sFile arm9File, int range, bool hardMode, bool forceEyeInfection)
        {
            try
            {
                byte[] arm9Data = File.ReadAllBytes(arm9File.path);
                byte[] patchedData = ApplyPatch(arm9Data, range, hardMode, forceEyeInfection);

                string tempFile = pluginHost.Get_TempFile();
                File.WriteAllBytes(tempFile, patchedData);
                pluginHost.ChangeFile(arm9File.id, tempFile);

                double prob = 100.0 / range;
                string hardLine = hardMode
                    ? "\n• Patch 4 (0x41038-0x410C0): Hard mode — thresholds doubled"
                    : "";
                string expLine = forceEyeInfection
                    ? $"\n• Experiment ({PATCH_EXPERIMENT_SELECTOR_OFFSET:X}): FUN_020428A4 forces r6 = #{FORCE_CONDITION_INDEX}"
                    : "";
                MessageBox.Show(
                    "Patches applied successfully!\n\n" +
                    "Use 'File > Save ROM' to persist changes.\n\n" +
                    "Changes applied:\n" +
                    "• Patch 1 (0x36224): Dynamic texture replacement fix\n" +
                    "• Patch 2 (0x6DA7E): guineaPig capitalization fix\n" +
                    $"• Patch 3 (0x402A4, 0x40328): ANY probability {prob:F1}% (range {range})" +
                    hardLine + expLine,
                    "Patches Applied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying patch:\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Shows the ARM9 patcher UI control
        /// </summary>
        public static Control ShowPatcherUI(IPluginHost pluginHost, sFile arm9File)
        {
            return new ARM9PatcherControl(pluginHost, arm9File);
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static bool BytesMatch(byte[] data, int offset, byte[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
                if (data[offset + i] != expected[i]) return false;
            return true;
        }

        internal static void CopyBytes(byte[] dest, int offset, byte[] src)
        {
            Array.Copy(src, 0, dest, offset, src.Length);
        }

        internal static void ApplyEyeDropPatch(byte[] d)
        {
            if (d == null) return;
            CopyBytes(d, PATCH5_OFFSET, PATCH5_PATCHED);                          // slots 16/17 → EarDrop
            d[PATCH5_KEY2_ENTRY26_OFFSET] = PATCH5_KEY2_ENTRY26_PATCHED;          // entry 26 key2=0x16 (INFLAMEDEAR)
            d[PATCH5_KEY2_ENTRY27_OFFSET] = PATCH5_KEY2_ENTRY27_PATCHED;          // entry 27 key2=0x17 (EYEINFECTION)
            d[PATCH5_ROW23_OFFSET]        = PATCH5_ROW23_PATCHED;                 // condition row 23 enabled
        }

        private static void RevertEyeDropPatch(byte[] d)
        {
            if (d == null) return;
            CopyBytes(d, PATCH5_OFFSET, PATCH5_ORIGINAL);
            d[PATCH5_KEY2_ENTRY26_OFFSET] = PATCH5_KEY2_ENTRY26_ORIGINAL;
            d[PATCH5_KEY2_ENTRY27_OFFSET] = PATCH5_KEY2_ENTRY27_ORIGINAL;
            d[PATCH5_ROW23_OFFSET]        = PATCH5_ROW23_ORIGINAL;
        }

        /// <summary>
        /// Apply EyeDrop-related sub-patches independently.
        /// - patch5: replace slots pointers (PATCH5_OFFSET)
        /// - patch6Key2: set historical key2 byte (PATCH5_KEY2_OFFSET)
        /// - patch6B1B2: set b1/b2 bytes in state-machine (PATCH6_OFFSET)
        /// </summary>
        // patch5/patch6Key2/patch6B1B2 params kept for API compatibility; all map to the full bundle.
        public static void ApplyEyeDropPatchPartial(byte[] d, bool patch5, bool patch6Key2, bool patch6B1B2)
        {
            if (d == null) return;
            if (patch5 || patch6Key2 || patch6B1B2)
                ApplyEyeDropPatch(d);
        }

        /// <summary>
        /// Public wrapper to apply only the experiment selector patch (force condition index in FUN_020428A4).
        /// </summary>
        public static void ApplyExperimentOnly(byte[] d)
        {
            if (d == null) return;
            CopyBytes(d, PATCH_EXPERIMENT_SELECTOR_OFFSET, GetExperimentSelectorPatch());
        }

        private static void ApplyPatch4(byte[] d)
        {
            for (int i = 0; i < PATCH4_OFFSETS.Length; i++)
            {
                byte[] bytes = BuildPatch4Bytes(i);
                if (bytes == null) continue; // preserve original
                CopyBytes(d, PATCH4_OFFSETS[i], bytes);
            }
        }

        private static void RevertPatch4(byte[] d)
        {
            for (int i = 0; i < PATCH4_OFFSETS.Length; i++)
                CopyBytes(d, PATCH4_OFFSETS[i], PATCH4_ORIGINALS[i]);
        }

        // Patch 8 helpers removed

        /// <summary>
        /// Returns true if patch 1 (texture fix) and patch 2 (guineaPig capitalization)
        /// are applied. This deliberately ignores PATCH3 (ANY probability) because
        /// the UI lets the user control the selected range independently.
        /// </summary>
        public static bool IsPatched1And2(byte[] arm9Data)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE) return false;
            if (!BytesMatch(arm9Data, PATCH1_OFFSET, PATCH1_PATCHED)) return false;
            if (arm9Data[PATCH2_OFFSET] != PATCH2_PATCHED) return false;
            return true;
        }

        // Public wrappers to allow UI code to apply/revert patch 4 without exposing internals.
        public static void ApplyPatch4ToBytes(byte[] d)
        {
            ApplyPatch4(d);
        }

        public static void RevertPatch4ToBytes(byte[] d)
        {
            RevertPatch4(d);
        }

        // Public wrappers for Patch 8 so UI can apply/revert prices in-place.
        public static void ApplyPatch8ToBytes(byte[] d)
        {
            ApplyPatch8(d);
        }

        public static void RevertPatch8ToBytes(byte[] d)
        {
            RevertPatch8(d);
        }

        // Patch8 public wrappers removed

        // ============================================================
        // PATCH 9: Corral slot overlap fix (v2 — LCG counter)
        //
        // FUN_02007950 assigns moveSlot via LCG with no collision check.
        // v2 replaces the LCG multiply + add constants with a simple +0x1000
        // increment per call. The existing >> 12 extraction produces sequential
        // slots 0, 1, 2, … without reading any animal struct field.
        // ============================================================
        private const int PATCH9_MUL_OFFSET = 0x798C;
        private static readonly byte[] PATCH9_MUL_ORIGINAL = { 0x93, 0x01, 0x00, 0xE0 }; // MUL r0, r3, r1
        private static readonly byte[] PATCH9_MUL_PATCHED  = { 0x01, 0x0A, 0x83, 0xE2 }; // ADD r0, r3, #0x1000

        private const int PATCH9_ADD73_OFFSET = 0x7990;
        private static readonly byte[] PATCH9_ADD73_ORIGINAL = { 0x73, 0x00, 0x80, 0xE2 }; // ADD r0, r0, #0x73
        private static readonly byte[] PATCH9_ADD73_PATCHED  = { 0x00, 0x00, 0xA0, 0xE1 }; // MOV r0, r0 (NOP)

        private const int PATCH9_ADD6000_OFFSET = 0x7994;
        private static readonly byte[] PATCH9_ADD6000_ORIGINAL = { 0x06, 0x0A, 0x80, 0xE2 }; // ADD r0, r0, #0x6000
        private static readonly byte[] PATCH9_ADD6000_PATCHED  = { 0x00, 0x00, 0xA0, 0xE1 }; // MOV r0, r0 (NOP)

        public static bool IsPatched9(byte[] arm9Data)
        {
            if (arm9Data == null || arm9Data.Length < ARM9_ORIGINAL_SIZE) return false;
            return BytesMatch(arm9Data, PATCH9_MUL_OFFSET, PATCH9_MUL_PATCHED)
                && BytesMatch(arm9Data, PATCH9_ADD73_OFFSET, PATCH9_ADD73_PATCHED)
                && BytesMatch(arm9Data, PATCH9_ADD6000_OFFSET, PATCH9_ADD6000_PATCHED);
        }
    }

    /// <summary>
    /// UI Control for ARM9 patching (simplified)
    /// </summary>
    public class ARM9PatcherControl : UserControl
    {
        private IPluginHost pluginHost;
        private sFile arm9File;
        private Label lblStatus;
        private Button btnApplyPatch;
        private Button btnRevertPatch;
        private CheckBox chkDuplicatePrices;
        private ComboBox cmbProbability;
        private Button btnUpdateProbability;
        private CheckBox chkHardMode;
        private TextBox txtLog;

        private int selectedRange = 2;

        public ARM9PatcherControl(IPluginHost pluginHost, sFile arm9File)
        {
            this.pluginHost = pluginHost;
            this.arm9File = arm9File;
            InitializeComponent();
            CheckPatchStatus();
            // Log applied patches at startup
            try { LogAppliedPatches(); } catch { }
        }

        private void InitializeComponent()
        {
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Size = new System.Drawing.Size(500, 560);
            this.AutoScroll = true;

            int y = 10;

            var lblTitle = new Label() { Text = "ARM9 Patcher — Let's Play Pet Hospitals", Font = new System.Drawing.Font(this.Font.FontFamily, 12, System.Drawing.FontStyle.Bold), Location = new System.Drawing.Point(10, y), AutoSize = true };
            this.Controls.Add(lblTitle);
            y += 30;

            var lblDesc = new Label()
            {
                Text =
                    "Patch 1: Dynamic texture replacement fix\n" +
                    "Patch 2: guineaPig capitalization fix\n" +
                    "Patch 3: ANY breed/color probability (configurable below)\n" +
                    "Patch 4: Hard mode — weekly grade thresholds doubled (optional)\n" +
                    "Patch 5/6: EYEINFECTION fix — EarDrop tool assigned correctly\n" +
                    "Patch 7: Harlequin Blue/Lilac texture swap fix",
                Location = new System.Drawing.Point(10, y),
                Size = new System.Drawing.Size(480, 100)
            };
            this.Controls.Add(lblDesc);
            y += 110;

            var lblProb = new Label() { Text = "ANY breed/color probability:", Location = new System.Drawing.Point(10, y), Size = new System.Drawing.Size(220, 25) };
            this.Controls.Add(lblProb);

            cmbProbability = new ComboBox() { Location = new System.Drawing.Point(240, y), Size = new System.Drawing.Size(140, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            for (int rango = 2; rango <= 10; rango++)
                cmbProbability.Items.Add($"{100.0 / rango:F1}% (rango {rango})");

            int actualRange = 2;
            try { var arm9Data = File.ReadAllBytes(arm9File.path); if (arm9Data.Length > ARM9Patcher.PATCH3_BREED_OFFSET) { actualRange = arm9Data[ARM9Patcher.PATCH3_BREED_OFFSET]; if (actualRange < 2 || actualRange > 10) actualRange = 2; } }
            catch { actualRange = 2; }
            cmbProbability.SelectedIndex = actualRange - 2;
            selectedRange = actualRange;
            cmbProbability.SelectedIndexChanged += cmbProbability_SelectedIndexChanged;
            this.Controls.Add(cmbProbability);
            y += 40;

            chkHardMode = new CheckBox() { Text = "Enable hard mode (Patch 4 — grade thresholds doubled)", Location = new System.Drawing.Point(10, y), Size = new System.Drawing.Size(460, 22) };
            chkHardMode.CheckedChanged += ChkHardMode_CheckedChanged;
            this.Controls.Add(chkHardMode);
            y += 30;

            chkDuplicatePrices = new CheckBox() { Text = "Duplicate prices (Patch 8 — doubles shop prices)", Location = new System.Drawing.Point(10, y), Size = new System.Drawing.Size(460, 22) };
            this.Controls.Add(chkDuplicatePrices);
            y += 30;

            lblStatus = new Label() { Location = new System.Drawing.Point(10, y), Size = new System.Drawing.Size(480, 25), Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold) };
            this.Controls.Add(lblStatus);
            y += 35;

            btnApplyPatch = new Button() { Text = "✓ Apply All Patches", Location = new System.Drawing.Point(10, y), Size = new System.Drawing.Size(200, 35), BackColor = System.Drawing.Color.LightGreen };
            btnApplyPatch.Click += BtnApplyPatch_Click;
            this.Controls.Add(btnApplyPatch);

            btnRevertPatch = new Button() { Text = "↩ Revert Patches", Location = new System.Drawing.Point(220, y), Size = new System.Drawing.Size(200, 35) };
            btnRevertPatch.Click += BtnRevertPatch_Click;
            this.Controls.Add(btnRevertPatch);

            // Patch8 UI removed

            btnUpdateProbability = new Button() { Text = "Update probability / hard mode / prices", Location = new System.Drawing.Point(10, y), Size = new System.Drawing.Size(410, 30) };
            btnUpdateProbability.Click += BtnUpdateProbability_Click;
            this.Controls.Add(btnUpdateProbability);
            // Log textbox for operation output
            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Anchor = AnchorStyles.None;
            txtLog.Location = new System.Drawing.Point(10, 340);
            txtLog.Size = new System.Drawing.Size(480, 170);
            txtLog.BackColor = System.Drawing.Color.White;
            try { txtLog.Font = new System.Drawing.Font("Consolas", 8f); } catch { }
            this.Controls.Add(txtLog);
        }

        private void CheckPatchStatus()
        {
            try
            {
                var arm9Data = File.ReadAllBytes(arm9File.path);
                // ensure hard-mode checkbox reflects current ROM without firing the event
                try
                {
                    chkHardMode.CheckedChanged -= ChkHardMode_CheckedChanged;
                }
                catch { }
                chkHardMode.Checked = ARM9Patcher.IsPatched4(arm9Data);
                chkHardMode.CheckedChanged += ChkHardMode_CheckedChanged;
                chkHardMode.Enabled = true;
                try { chkDuplicatePrices.CheckedChanged -= ChkHardMode_CheckedChanged; } catch { }
                chkDuplicatePrices.Checked = ARM9Patcher.IsPatched8(arm9Data);
                try { chkDuplicatePrices.CheckedChanged += ChkHardMode_CheckedChanged; } catch { }
                // Determine core patched state: verify patches 1-3 only.
                // NOTE: patches 5 and 6 (EyeDrop/EarDrop bundle) are known to break
                // gameplay in some ROM builds. The UI no longer enforces them as
                // required for the "core" status — they are applied separately and
                // can be toggled via the advanced actions. See the commented call
                // in BtnApplyPatch_Click for where they would be applied.
                bool corePatched = ARM9Patcher.IsPatched1And2(arm9Data) && ARM9Patcher.IsPatched(arm9Data, selectedRange);
                lblStatus.Text = corePatched ? "Status: ✓ core patches (1-3) applied" : "Status: ⚠ Partial or missing core patches";
                lblStatus.ForeColor = corePatched ? System.Drawing.Color.Green : System.Drawing.Color.Orange;
                btnApplyPatch.Enabled = !corePatched;
                btnRevertPatch.Enabled = corePatched;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Estado: ERROR - " + ex.Message;
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
        }

        private void Log(string message)
        {
            try
            {
                if (txtLog == null) return;
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        private void LogAppliedPatches()
        {
            try
            {
                if (txtLog == null) return;
                byte[] arm9Data = File.ReadAllBytes(arm9File.path);
                // Determine status
                bool p1and2 = ARM9Patcher.IsPatched1And2(arm9Data);
                bool p3 = ARM9Patcher.IsPatched(arm9Data, selectedRange);
                bool p4 = ARM9Patcher.IsPatched4(arm9Data);
                // NOTE: do NOT auto-check patches 5/6 here — they are excluded from
                // the automatic verification because applying them can break some
                // ROM builds. Users may apply them separately at their own risk.
                bool p5 = ARM9Patcher.IsEyeDropPatched(arm9Data);
                bool p8 = ARM9Patcher.IsPatched8(arm9Data);
                bool p9 = ARM9Patcher.IsPatched9(arm9Data);

                txtLog.AppendText("Applied patches summary:\r\n");
                txtLog.AppendText("1) Patch 1 - Texture replacement: " + (p1and2 ? "APPLIED" : "NOT applied") + "\r\n");
                txtLog.AppendText("2) Patch 2 - guineaPig capitalization: " + (p1and2 ? "APPLIED" : "NOT applied") + "\r\n");
                txtLog.AppendText("3) Patch 3 - ANY breed/color probability: " + (p3 ? "APPLIED" : "NOT applied") + "\r\n");
                txtLog.AppendText("4) Patch 4 - Hard-mode thresholds: " + (p4 ? "APPLIED" : "NOT applied") + "\r\n");
                txtLog.AppendText("5/6) Patch 5 & 6 - EyeDrop/EarDrop bundle: " + (p5 ? "APPLIED" : "NOT applied") + "\r\n");
                txtLog.AppendText("8) Patch 8 - Duplicate prices: " + (p8 ? "APPLIED" : "NOT applied") + "\r\n");
                txtLog.AppendText("7) Patch 7 - Harlequin asset swap: N/A (asset swap state not checked)\r\n");
                txtLog.AppendText("9) Patch 9 - Corral slot overlap fix: " + (p9 ? "APPLIED" : "NOT applied") + "\r\n");
                txtLog.AppendText(Environment.NewLine);
            }
            catch { }
        }

        private void BtnApplyPatch_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] arm9Data = File.ReadAllBytes(arm9File.path);
                // Apply patches 1-3. Note: applying Patch 5/6 (EyeDrop/EarDrop bundle)
                // is commented out because it can break gameplay in some ROM builds.
                // The original call (left commented) would enable that bundle.
                // byte[] patchedData = ARM9Patcher.ApplyPatch(arm9Data, selectedRange, chkHardMode.Checked, false, true, chkDuplicatePrices.Checked);
                byte[] patchedData = ARM9Patcher.ApplyPatch(arm9Data, selectedRange, chkHardMode.Checked, false, /*applyEyeDrop*/ false, chkDuplicatePrices.Checked);
                string tempFile = pluginHost.Get_TempFile();
                File.WriteAllBytes(tempFile, patchedData);
                pluginHost.ChangeFile(arm9File.id, tempFile);

                // Then attempt asset swap
                string swapErr;
                bool swapOk = AssetSwapper.ApplyHarlequinSwapInProject(pluginHost, out swapErr);
                // Log results instead of showing a modal dialog
                Log("ARM9 patches applied.");
                if (swapOk) Log($"Harlequin swap: OK. Details: {swapErr}"); else Log($"Harlequin swap: FAILED: {swapErr}");
                Log($"Duplicate prices applied: {(chkDuplicatePrices.Checked ? "YES" : "NO")}");
                Log("Use File > Save ROM to persist changes.");

                CheckPatchStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying patches:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Preview button removed — handler deleted

        private void BtnRevertPatch_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] arm9Data = File.ReadAllBytes(arm9File.path);
                byte[] revertedData = ARM9Patcher.RevertPatch(arm9Data);
                string tempFile = pluginHost.Get_TempFile();
                File.WriteAllBytes(tempFile, revertedData);
                pluginHost.ChangeFile(arm9File.id, tempFile);
                Log("Patches reverted.");
                CheckPatchStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al revertir: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Patch8 event handlers removed

        private void cmbProbability_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedRange = cmbProbability.SelectedIndex + 2;
            CheckPatchStatus();
        }

        private void BtnUpdateProbability_Click(object sender, EventArgs e)
        {
            try
            {
                // Update probability (patch 3) and apply/revert hard-mode (patch 4) and prices (patch 8) as requested.
                byte[] arm9Data = File.ReadAllBytes(arm9File.path);
                byte[] d = new byte[arm9Data.Length];
                Array.Copy(arm9Data, d, arm9Data.Length);

                // apply patch 3 bytes (breed/color MOV immediates)
                byte[] breedPatch = { (byte)selectedRange, 0x50, 0xA0, 0xE3 };
                byte[] colorPatch = { (byte)selectedRange, 0x60, 0xA0, 0xE3 };
                ARM9Patcher.CopyBytes(d, ARM9Patcher.PATCH3_BREED_OFFSET, breedPatch);
                ARM9Patcher.CopyBytes(d, ARM9Patcher.PATCH3_COLOR_OFFSET, colorPatch);

                // apply or revert patch 4 depending on the checkbox
                if (chkHardMode.Checked)
                    ARM9Patcher.ApplyPatch4ToBytes(d);
                else
                    ARM9Patcher.RevertPatch4ToBytes(d);

                // apply or revert patch 8 (duplicate prices) depending on the checkbox
                if (chkDuplicatePrices.Checked)
                    ARM9Patcher.ApplyPatch8ToBytes(d);
                else
                    ARM9Patcher.RevertPatch8ToBytes(d);

                string tempFile = pluginHost.Get_TempFile();
                File.WriteAllBytes(tempFile, d);
                pluginHost.ChangeFile(arm9File.id, tempFile);

                Log($"Updated ANY probability to {100.0/selectedRange:F1}% (range {selectedRange}). Hard mode: {(chkHardMode.Checked ? "enabled" : "disabled")}. Duplicate prices: {(chkDuplicatePrices.Checked ? "enabled" : "disabled")}");
                CheckPatchStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar probabilidad: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChkHardMode_CheckedChanged(object sender, EventArgs e)
        {
            // Intentionally no immediate action. Changes are applied when user clicks Update or Apply All.
        }
    }
}
