# PETHOSPITALS — Features & ARM9 Patches

Game: **Let's Play Pet Hospitals** (Nintendo DS, game code C2HP)
ARM9: ROM offset `0x4000`, load address `0x02000000`, size `0x72E38`
Address formula: `ROM file offset = arm9 offset + 0x4000`

All patches are in-place (no ARM9 size change, no header modifications).

---

## Feature — Language file editing (INI / INI.CP)

The Tinke plugin supports viewing and editing the game's localization files directly
from the ROM file tree.

### File types

| Extension | Description |
|-----------|-------------|
| `.ini` | Plain-text localization source. Human-readable `[section]` / `key=value` format, encoded in CP1252. |
| `.ini.cp` | Binary compiled form of the same data, read at runtime by the game. |

Both types appear as separate entries in the ROM. Selecting either one opens the
**CP viewer** (`CP.cs`) which decodes the binary and displays its content with
syntax highlighting.

### Compile INI button

When viewing an `.ini.cp` file, clicking **Compilar INI**:

1. Locates the matching `.ini` source file — first by searching the open ROM via
   `pluginHost.Search_File(iniName)`, then falling back to the repository `lang/`
   folder, and finally offering a manual file picker.
2. Parses the `.ini` with CP1252 encoding (supports `[section]`, `key=value`,
   ignores `;` and `#` comments).
3. Encodes the result into the binary `.ini.cp` format using `CP.Encode`.
4. Validates the output by round-tripping it through `CP.Decode`.
5. Replaces the ROM entry via `pluginHost.ChangeFile` — no file is written to disk
   until the user saves the ROM with **File > Save ROM**.

### Export as INI button

Exports the decoded content of the currently viewed `.ini.cp` to a UTF-8 `.ini`
file on disk, for reference or external editing.

### Known limitation

Changes to the `.ini` entry in the ROM do **not** automatically recompile the
`.ini.cp`. You must open the `.ini.cp` entry and click **Compilar INI** explicitly
to push changes into the compiled binary that the game reads.

---

## Patch 1 — Texture Replacement Fix

### Problem
The game calls `FUN_02036224` to decide whether a texture can be replaced at runtime.
The function always returns `true` (non-zero), blocking dynamic texture substitution.
This prevents the MTL material editor from loading the correct animal texture.

### Solution
Force the function to always return `false` (zero) by replacing the first two instructions
with `MOV r0, #0` + `BX lr`.

### Bytes
| | arm9 offset | ROM offset | Bytes |
|-|-------------|------------|-------|
| Original | `0x36224` | `0x3A224` | `04 10 90 E5  00 00 51 E3` |
| Patched  | `0x36224` | `0x3A224` | `00 00 A0 E3  1E FF 2F E1` |

### Disassembly
```
; Original
LDR  r1, [r0, #0x4]
CMP  r1, #0

; Patched
MOV  r0, #0      ; E3A00000 — return value = false
BX   lr          ; E12FFF1E — return immediately
```

---

## Patch 2 — GuineaPig Capitalisation Fix

### Problem
The species string table contains `"guineapig"` (all lowercase).
The texture loader is case-sensitive and looks for `"guineaPig"` (capital P).
Guinea pig models load without textures as a result.

### Solution
Change the single byte `'p'` (0x70) to `'P'` (0x50) at the exact position of the
lowercase `p` inside the string.

### Bytes
| | arm9 offset | ROM offset | Byte |
|-|-------------|------------|------|
| Original | `0x6DA7E` | `0x71A7E` | `0x70` ('p') |
| Patched  | `0x6DA7E` | `0x71A7E` | `0x50` ('P') |

---

## Patch 3 — Increase ANY Breed/Color Probability

### Background
When the game generates a new animal for a request slot it calls an LCG random number
generator (multiplier `0x41C64E6D`, classic NDS/GBA constant) and reduces the result
modulo a **range** value:

- If `result % range == 0` → breed (or color) is set to **-1 (ANY)**
- Otherwise → breed (or color) is set to a specific index (0-based)

The range is computed as `breed_count + 1` (or `color_count + 1`), so with e.g. 5
breeds the original probability of ANY is `1/6 ≈ 17%`.

A request with ANY breed/color matches any animal of that species regardless of its
specific breed or color, making it easier for the player to fulfill.

### Problem
With the original range the probability of ANY is low (~17–25% depending on species),
so most requests ask for very specific animals that are hard to find.

### Solution
Replace the dynamic range calculation with a fixed small constant.
With range = 2 the probability becomes exactly 50% for both breed and color.

The range can be tuned — see table below.

| Range (N) | P(ANY) | Instruction |
|-----------|--------|-------------|
| 2 | 50.0% | `MOV r5/r6, #2` ← **current** |
| 3 | 33.3% | `MOV r5/r6, #3` |
| 4 | 25.0% | `MOV r5/r6, #4` |
| 5 | 20.0% | `MOV r5/r6, #5` |
| 6 | 16.7% | `MOV r5/r6, #6` (close to original) |

### Breed range patch
| | arm9 offset | ROM offset | Bytes |
|-|-------------|------------|-------|
| Original | `0x402A4` | `0x442A4` | `01 50 80 E2` |
| Patched  | `0x402A4` | `0x442A4` | `02 50 A0 E3` |

```
; Original
ADD  r5, r0, #1      ; E2805001 — r5 = breed_count + 1

; Patched (range = 2)
MOV  r5, #2          ; E3A05002 — fixed range → 50% ANY breed
```

### Color range patch
| | arm9 offset | ROM offset | Bytes |
|-|-------------|------------|-------|
| Original | `0x40328` | `0x44328` | `01 60 80 E2` |
| Patched  | `0x40328` | `0x44328` | `02 60 A0 E3` |

```
; Original
ADD  r6, r0, #1      ; E2806001 — r6 = color_count + 1

; Patched (range = 2)
MOV  r6, #2          ; E3A06002 — fixed range → 50% ANY color
```

### Changing the probability
To use a different range N (must be 2–255):
- Breed patched bytes: `0N 50 A0 E3`
- Color patched bytes: `0N 60 A0 E3`

Using `apply_patches.ps1`:
1. Run `powershell.exe -File apply_patches.ps1 -Revert` to restore originals
2. Edit the two `patched` byte arrays in the script (change `0x02` to `0x0N`)
3. Run `powershell.exe -File apply_patches.ps1` to re-apply

Using Tinke (ARM9PatcherControl):
- Select the desired probability from the dropdown (range 2–10)
- Click **Actualizar probabilidad ANY** to update Patch 3 only

### Why this function only (not the second creator)
There are two functions that write species/breed/color into animal objects:

| Function | VA | Role |
|----------|----|------|
| First creator | `0x020403B8` | **Randomly generates** breed/color using LCG → patched here |
| Second creator | `0x020422E8` | **Clones** an existing animal via vtable calls → no random, no change needed |

The second creator copies values from a source object — if the source already has
ANY breed/color (set by this patch), the copy will too.

---

---

## Patch 4 — Hard-Mode Grade Thresholds (optional)

### Background
At the end of each week the game calls `FUN_020411c8` to compute a 4-week rolling
average score, then passes it to `FUN_0204102c` which converts it to a grade tier
index (0 = lowest, 17 = highest — **18 tiers total**, exact letter names unknown
but the scale appears to start at F/E and end at SS/SSS class).

Each tier has a pair of CMP instructions in `FUN_0204102c`:
1. **First CMP** (early-exit guard): `CMP r1, #N; BXLE lr` — returns previous grade if
   score ≤ N. This is what we patched.
2. **Second CMP** (cascade guard): `CMP r1, #N; BLE` — used to skip this tier in the
   branch-cascade path. **Not patched.**

The full original 18-tier threshold table (each 100 pts):

| Index | Orig threshold | CMP offset (arm9) |
|-------|---------------|-------------------|
|  0    | ≤ 100         | 0x4502C (base CMP #0) |
|  1    | 100–200       | 0x45038 ← patched |
|  2    | 200–300       | 0x45048 ← patched |
|  3    | 300–400       | 0x4505C ← patched |
|  4    | 400–500       | 0x45070 ← patched |
|  5    | 500–600       | 0x45084 ← patched |
|  6    | 600–700       | 0x45098 ← patched |
|  7    | 700–800       | 0x450AC ← patched |
|  8    | 800–900       | 0x450D4 (unpatched) |
|  9    | 900–1000      | 0x450E8 (unpatched) |
| 10    | 1000–1100     | pool = 0x044C      |
| 11    | 1100–1200     | 0x45118 (unpatched)|
| 12    | 1200–1300     | pool = 0x0514      |
| 13    | 1300–1400     | pool = 0x0578      |
| 14    | 1400–1500     | pool = 0x05DC      |
| 15    | 1500–1600     | 0x45180 (unpatched)|
| 16    | 1600–1700     | pool = 0x06A4      |
| 17    | > 1700        | MOVGT at end       |

### Score formula (FUN_02040f08)
```
score = [+0x17c] × 500      ; completed jobs (high value)
      + [+0x184] × 200      ; partial credit
      + ([+0x178] − [+0x18c]) × 100  ; net animals treated
      + [+0x190] × (−50)    ; penalty events
      + bonus + floor
```

### What the patch changes (first 8 tiers only)
Each patched CMP doubles the threshold for its tier's early-exit guard:

| Sub-patch | arm9 offset | ROM offset | Original | Patched | Threshold |
|-----------|-------------|------------|----------|---------|-----------|
#### Inline CMP sub-patches (tiers 1–8)

| Sub-patch | arm9 offset | ROM offset | Original | Patched | Threshold |
|-----------|-------------|------------|----------|---------|-----------|
| 4a | `0x41038` | `0x45038` | `64 00 51 E3` | `C8 00 51 E3` | 100 → 200 |
| 4b | `0x41048` | `0x45048` | `C8 00 51 E3` | `19 0E 51 E3` | 200 → 400 |
| 4c | `0x4105C` | `0x4505C` | `4B 0F 51 E3` | `96 0F 51 E3` | 300 → 600 |
| 4d | `0x41070` | `0x45070` | `19 0E 51 E3` | `32 0E 51 E3` | 400 → 800 |
| 4e | `0x41084` | `0x45084` | `7D 0F 51 E3` | `FA 0F 51 E3` | 500 → 1000 |
| 4f | `0x41098` | `0x45098` | `96 0F 51 E3` | `4B 0E 51 E3` | 600 → 1200 |
| 4g | `0x410AC` | `0x450AC` | `AF 0F 51 E3` | `58 0E 51 E3` | 700 → 1408 |
| 4h | `0x410C0` | `0x450C0` | `32 0E 51 E3` | `19 0D 51 E3` | 800 → 1600 |

#### Inline CMP + ADD sub-patches (tiers 9–16)

| Sub-patch | arm9 offset | ROM offset | Original | Patched | Threshold |
|-----------|-------------|------------|----------|---------|-----------|
| 4i | `0x410D4` | `0x450D4` | `E1 0F 51 E3` | `71 0E 51 E3` | 900 → 1808 (≈×2) |
| 4j | `0x410E8` | `0x450E8` | `FA 0F 51 E3` | `7D 0E 51 E3` | 1000 → 2000 |
| 4k | `0x41118` | `0x45118` | `4B 0E 51 E3` | `96 0E 51 E3` | 1200 → 2400 |
| 4l | `0x41148` | `0x45148` | `64 20 82 E2` | `C8 20 82 E2` | ADD #100→#200 (tier 14: 1400→2800) |
| 4m | `0x41164` | `0x45164` | `64 20 82 E2` | `C8 20 82 E2` | ADD #100→#200 (tier 15: 1500→3000) |
| 4n | `0x41180` | `0x45180` | `19 0D 51 E3` | `C8 0E 51 E3` | 1600 → 3200 |

#### Literal pool sub-patches (tiers 11, 13–15, 17)

| Sub-patch | arm9 offset | ROM offset | Original | Patched | Value |
|-----------|-------------|------------|----------|---------|-------|
| 4o | `0x411B4` | `0x451B4` | `4C 04 00 00` | `98 08 00 00` | 1100 → 2200 |
| 4p | `0x411B8` | `0x451B8` | `14 05 00 00` | `28 0A 00 00` | 1300 → 2600 |
| 4q | `0x411BC` | `0x451BC` | `78 05 00 00` | `F0 0A 00 00` | 1400 → 2800 |
| 4r | `0x411C0` | `0x451C0` | `DC 05 00 00` | `B8 0B 00 00` | 1500 → 3000 |
| 4s | `0x411C4` | `0x451C4` | `A4 06 00 00` | `48 0D 00 00` | 1700 → 3400 |

### Effective grade map (v2 — all 18 grades reachable)

| Score range | Grade | | Score range | Grade |
|-------------|-------|-|-------------|-------|
| ≤ 200       | 0     | | 2001–2200   | 10    |
| 201–400     | 1     | | 2201–2400   | 11    |
| 401–600     | 2     | | 2401–2600   | 12    |
| 601–800     | 3     | | 2601–2800   | 13    |
| 801–1000    | 4     | | 2801–3000   | 14    |
| 1001–1200   | 5     | | 3001–3200   | 15    |
| 1201–1408   | 6     | | 3201–3400   | 16    |
| 1409–1600   | 7     | | > 3400      | 17    |
| 1601–1808   | 8     | | | |
| 1809–2000   | 9     | | | |

**NDSi note:** DeSmuME plays the patched ROM; NDSi does not boot. Likely cause:
startup self-integrity check (CRC loop) enforced by real hardware, skipped by
DeSmuME. Patch bytes are verified correct.

### ARM32 immediate encoding
`CMP r1, #val` encodes as `[imm8, rotation_nibble, 0x51, 0xE3]` (LE bytes) where
`val = imm8 ror (rotation_nibble × 2)`. Examples:
- `#200` → `0xC8=200, rot=0` → `C8 00 51 E3`
- `#400` → `0x19=25, rot=14 (25 ror 28 = 400)` → `19 0E 51 E3`
- `#1808` → `0x71=113, rot=14 (113 × 16 = 1808)` → `71 0E 51 E3`

This patch is **optional** and independent of Patches 1–3. It can be applied or
reverted separately via the Tinke UI checkbox or via `apply_patches.ps1`.

---

### Patch 4 — Version 2 (v2) notes

This repository contains a v2 variant of Patch 4 that applies an expanded set
of hard-mode grade thresholds for all 18 tiers. Key behaviour of v2:

- Literal pool entries are written as full 32-bit little-endian words (`uint32`)
  and therefore contain the exact target values (example: pool entries at
  `0x411B4..0x411C4` contain `3189, 3514, 3676, 3838, 4000` respectively).
- Inline instruction encodings (e.g., `CMP r1, #imm` or `ADD r2, r2, #imm`) are
  restricted by the ARM imm12 encoding (imm8 + rotate). Some requested target
  thresholds cannot be represented exactly in that encoding. To avoid failing
  the patch, the patcher chooses the nearest ARM‑encodable immediate and writes
  that instruction value in-place (examples observed in the test ROM: target
  `1500` was written as `1504`; `1800` became `1792`).
- The patcher logic that implements this behaviour lives in
  `Plugins/PETHOSPITALS/PETHOSPITALS/Formats/ARM9Patcher.cs` (function
  `BuildPatch4Bytes` with a fallback search for the closest encodable immediate).

Rationale: the fallback strategy is the least-invasive safe option. Pool
entries preserve exact numeric thresholds, while inline instruction immediates
are adjusted to an encodable value so the patched ARM9 remains valid and does
not raise encoding errors when written.

If you require exact immediate values for all inline CMPs, a more invasive
approach is needed (emit `MOVW`/`MOVT` pairs or use literal loads), which
requires additional instruction space and careful validation on real hardware.
Contact the repo maintainers before requesting that change.

## Patch 5 — EyeDrop / EarDrop treatment bundle
Makes the `EYEINFECTION` condition appear in-game by enabling its
condition-table row. EarWax and InflamedEar keep their original
treatments (CottonSwab and EarDrop respectively).

Applied writes (executed in order):

| arm9 offset | Original | Patched | Description |
|-------------|----------|---------|-------------|
| `0x61EAC` | `CC 17 07 02` | `54 18 07 02` | Slots 16/17 literal pool → EarDrop ptr (`0x02071854`) |
| `0x70630` | `38 D3 06 02` | `FC 16 07 02` | Condition row 7 +0x08 → EarDrop resource ptr |
| `0x70640` | `00` | `01` | Condition row 7 enabled |
| `0x6FD14` | `0x15` | `0x15` | Slot 26 key2 — preserved (EarWax/CottonSwab, unchanged) |
| `0x6FD15..16` | `FF FF` | `07 07` | EYEINFECTION state-machine b1=b2=0x07 (row index 7) |
| `0x6FD1C` | `0x16` | `0x17` | Slot 27 key2 = 0x17 (runtime index for EYEINFECTION) |
| `0x6FD1D..1E` | `FF FF` | `07 07` | Slot 27 b1=b2=0x07 |

Key2 confirmed values (runtime, via Lua brute-force):
- EARWITHWAX   = `0x13` (runtime index 19) — original, unmodified
- INFLAMEDEAR  = `0x16` (runtime index 22) — original, unmodified
- EYEINFECTION = `0x17` (runtime index 23) — slot 27 retargeted

### Current behavior
- **EarWax** (EARWITHWAX): original CottonSwab treatment, unchanged.
- **InflamedEar** (INFLAMEDEAR): original EarDrop treatment, unchanged.
- **EyeInfection** (EYEINFECTION): condition now appears in-game
  (row enabled). It is currently **untreatable** — no tool assignment
  was possible without breaking EarWax or InflamedEar.

### Why EyeInfection cannot be made treatable without side-effects

Slot 27 was the original INFLAMEDEAR slot (key2=0x16). Redirecting it
to EYEINFECTION (key2=0x17) leaves INFLAMEDEAR without a match.
Reusing slot 26 (CottonSwab) for INFLAMEDEAR broke EarWax treatment
because the slot no longer pointed to CottonSwab resources. No other
free slot loads EarDrop at runtime. A full fix would require either
adding a new tool array entry (not possible in-place) or loading EarDrop
into an additional slot via the init literal pool, which would
displace another tool.

### Known side-effect
Treating "Ear with wax" (EARWITHWAX) may briefly show "clean wound"
as an intermediate state. Cosmetic only, not functionally harmful.

---

## Patch 6 — Part of Patch 5 bundle

See Patch 5 above. The state-machine bytes at `0x6FD15–0x6FD16` are applied as part
of the same `ApplyEyeDropPatch` call.


## Patch Experiment — Force a single condition index

### Purpose
Force the condition index inside `FUN_020428A4` to a fixed value for testing, bypassing the RNG in `FUN_02042818`.

### Mechanism
Replace `MOV r6, r0` at `0x428A8` with `MOV r6, #FORCE_CONDITION_INDEX`.

Confirmed in-game: index `22` → inflamed ear; index `23` → EYEINFECTION.

| | arm9 offset | ROM offset | Bytes |
|-|-------------|------------|-------|
| Original | `0x428A8` | `0x468A8` | `00 60 A0 E1` |
| Patched (index 23) | `0x428A8` | `0x468A8` | `17 60 A0 E3` |

---

## Patch 7 — Harlequin Blue/Lilac texture swap

Problem: rabbit Harlequin Blue loads Lilac textures and vice versa. The `.pcx` and
`.tex` files in the ROM have their contents swapped — confirmed by visual
inspection in Tinke.

Solution: swap the binary contents of three file pairs directly in the ROM using
the host API. No ARM9 changes are needed.

Files swapped:
- `rabbitHarlequinBlue_hi.pcx` ↔ `rabbitHarlequinLilac_hi.pcx`
- `rabbitHarlequinBlue_lo.tex` ↔ `rabbitHarlequinLilac_lo.tex`
- `rabbit_Harlequin_Blue_icon.tex` ↔ `rabbit_Harlequin_Lilac_icon.tex`

Note: `_lo.tex` and `_icon.tex` pairs had identical content in the tested ROM
build — only the `_hi.pcx` pair contained visually different data. The swap is
applied to all three pairs regardless.

Implementation: `AssetSwapper.ApplyHarlequinSwapInProject(IPluginHost)` — searches
files by exact name using `host.Search_File`, reads bytes with
`host.Get_Bytes(path, offset, size)`, and writes swapped content via
`host.ChangeFile(id, tempFile)`.


## Patch 8 — Duplicate prices

### Objective
Double in-game shop prices (factor ×2) for selected item tables (clothing, furniture, a
small set of improvements). The change is deterministic and reversible.

### Rationale
Make shops more expensive uniformly without changing game logic. The change operates on
in-place 32-bit price fields found in known tables and skips values that look like
pointers or implausible numbers.

### Affected ranges (ROM addresses)
- Clothes table: `0x066704` .. `0x0669F4`, stride 12, price at offset `+0` (per-entry)
- Furniture block: `0x0672B8` .. `0x067D00`, stride 20, price at offset `+4`
- Improvements (explicit): `0x066050` .. `0x066054`, stride 4, price at offset `+0`

These ranges are conservative. Review before applying if you change ROM layout.

### How the patch is applied (high level)
- For each configured entry: read a 32-bit little-endian word from the ROM at the
  table entry's price offset (convert ROM address → file offset according to the
  repository convention: `file_offset = rom_address - 0x4000`).
- Skip the entry if the value is ≤ 0, ≥ 0x01000000 (likely a pointer), or outside a
  plausible price window (current implementation uses 20 .. 1_000_000).
- Otherwise write `price * 2` (32-bit little-endian) back to the same location.

### Revert
Revert performs the inverse: for the same ranges, it replaces any even 32-bit value
that falls into the plausibility window with `value / 2`. This is the inverse
operation and does not rely on backup files.

### Notes
- The Tinke UI exposes Patch 8 as the `Duplicate prices` checkbox and applies it
  together with "Update probability / hard mode / prices". It can also be applied
  via the standard patch flow.
- The patcher avoids following pointers by default; only explicitly listed tables are
  modified.
- If you need to extend or correct ranges, edit the table of ROM ranges in the
  patcher source and test on a copy of `arm9.bin`.

## Patch — Corral slot overlap (not implemented)

### Problem
Animals in the same corral can share the same `moveSlot` position, causing
them to overlap visually. This occurs on initial load and after adoptions.

### Why it cannot be fixed in-place

`FUN_02007950` (VA `0x02007950`) assigns each animal a `moveSlot` index using
an LCG random number generator with no collision check. The correct fix would
assign a deterministic 0-based index per animal from the corral's animal count.

The count field is at `(corral_base + 0x18)[0x10]`, accessible in
`FUN_0203df2c` as `r0[0x10]` (where `r0 = corral_base + 0x18`, `r1 = animal_ptr`).
Writing this value to `animal[0xF8]` before the count increments requires
4 new instructions, but only 3 instruction slots are available at the
insertion point without breaking the function's argument setup for its
subsequent `bl FUN_02030ce4(r0=corral+0x18, r1=animal_ptr)`.

Attempts to use `animal[0x1b0]` (scene pointer) or `animal[0x4c]` from
`FUN_02007950` at tick time were unsuccessful: `animal[0x1b0]+0x28` contains
a pointer, not a count, and `animal[0x4c]` is not reliably set for animals
loaded from save. No adjacent free space (≥16 bytes) was found near candidate
injection points. ARM9 expansion is not possible (BSS zeroing constraint).

### Observed behavior
The overlap is cosmetic: both animals are clickable individually, and movement
animations naturally separate them. It does not affect gameplay or saving.

## Patching Scripts & Tooling

### apply_patches.ps1 (direct ROM patching)
```powershell
powershell.exe -File apply_patches.ps1          # apply all patches
powershell.exe -File apply_patches.ps1 -Revert  # restore originals
```
- Creates `.bak` backup on first run; safe to run multiple times
- No Tinke or NDS header changes needed — all patches are in-place

### ARM9Patcher.cs — Public API
- `IsPatched(byte[])` / `NeedsPatching(byte[])` — state checks
- `ApplyPatch(byte[], int range, bool hardMode)` — apply all patches
- `RevertPatch(byte[])` — restore originals, truncate to `0x72E38`
- `ApplyPatchToROM(IPluginHost, sFile, ...)` — apply via Tinke ChangeFile

### Tinke UI (ARM9PatcherControl)
- Dropdown: ANY probability range 2–10 (range 2 = 50%)
- Checkbox: Enable hard mode (Patch 4)
- **Apply All Patches**: applies patches 1–3 + EyeDrop bundle (Patch 5/6) + Harlequin swap (Patch 7). Hard mode (Patch 4) is handled separately.
- **Revert Patches**: restores originals for patches 1–3 and the EyeDrop bundle.
- **Update probability / hard mode**: updates Patch 3 range and applies or reverts Patch 4 independently.

### PowerShell ROM Reading — CRITICAL
Always read bytes individually. Never use `-shl`/`-bor` on bytes — PowerShell's `-shl` produces negative int32 on bytes with high bit set.

```powershell
# CORRECT
$b0 = $rom[$r]; $b1 = $rom[$r+1]; $b2 = $rom[$r+2]; $b3 = $rom[$r+3]
```

### Tinke Save Warning
Tinke's **File > Save ROM** rebuilds the NDS container in a way that breaks R4 SDHC card compatibility on NDSi. Use `apply_patches.ps1` for card testing.

### Method B — Via Tinke UI
1. Open ROM → select `arm9.bin` → ARM9 Patcher panel appears
2. Click **Apply All Patches** → then **File > Save ROM**


### ini_to_cp.py (alternative INI compiler)

If `INITOOL` is not available, the repository contains a helper script that compiles
plain `.ini` localization files into the `.ini.cp` binary format used by the game.

 - Location: `Plugins/PETHOSPITALS/PETHOSPITALS/tools/ini_to_cp.py`
- Purpose: convert a human-editable `*.ini` (CP1252 encoded) into a `.ini.cp` file
  compatible with the project's tooling and testing flow.

Usage examples

- Run from the `lang` directory (recommended):

  1) Change into the language folder:
     `cd Plugins/PETHOSPITALS/lang`
  2) Compile the Spanish INI to CP:
     `python ../PETHOSPITALS/tools/ini_to_cp.py spanish.ini`

- Or call with full paths from the repo root:

  `python Plugins/PETHOSPITALS/PETHOSPITALS/tools/ini_to_cp.py Plugins/PETHOSPITALS/lang/spanish.ini`

Notes and caveats

- The script reads `.ini` files using CP1252 encoding and writes the `.ini.cp` in
  the project's expected binary layout. It is intended for local testing and
  convenience when `INITOOL` is not available.
- This helper is not a perfect substitute for the original `INITOOL` binary used
  by the project's Makefile; when possible prefer running the official `MakeIni`
  rule (`$(INITOOL)`) to generate identical output.
- After generating `.ini.cp` with the script, verify the produced file using
  `Plugins/PETHOSPITALS/lang/analyze_ini_cp.py` or by loading the file in Tinke.

