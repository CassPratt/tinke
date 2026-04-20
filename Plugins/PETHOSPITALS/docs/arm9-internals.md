# ARM9 Internals: Memory Constraints, Structures & Instruction Reference

## Project Overview

Game: **Let's Play Pet Hospitals** (Nintendo DS, game code C2HP)
- ROM: `C:\Users\cass_\OneDrive\Desktop\DeSmuME\ROMs\Let's Play Pet Hospitals.nds`
- Repository: `C:\Users\cass_\Downloads\repos\tinke` — branch `feature/pet_hospitals`
- Plugin: `Plugins/PETHOSPITALS/PETHOSPITALS/` — main patcher: `Formats/ARM9Patcher.cs`
- ARM9: load address `0x02000000`, ROM offset `0x4000`, size `0x72E38` (all ARM32)
- Address conversion: `arm9_offset = VA − 0x02000000`; `ROM_offset = arm9_offset + 0x4000`
- **All patches must be in-place (same size) — no ARM9 expansion possible**

---

## BSS Region (DO NOT place code here)
- **BSS: [0x02072A00, 0x020B2A40)** — zeroed by CRT0 at every startup
- Any non-zero bytes appended past arm9:0x72A00 will be zeroed at runtime → code injection impossible
- Autoload list (ITCM/DTCM descriptors) lives at arm9:0x72E20–0x72E37 — must remain non-zero

## CRT0 Literal Pool (inside ARM9 binary)
Located at arm9:0xB9C–0xBAC. These values control BSS initialization:
- arm9:0xB9C = 0x02072E20 (autoload list start VA)
- arm9:0xBA0 = 0x02072E38 (autoload list end VA)
- arm9:0xBA4 = 0x02072A00 (BSS_start, copy 1)
- arm9:0xBA8 = 0x02072A00 (BSS_start, copy 2)
- arm9:0xBAC = 0x020B2A40 (BSS_end)

## No Free Space Below BSS
- Zero region at arm9:0x6FB08 (208 bytes): active game data with 5 code references
- Zero region at arm9:0x6FE8C (680 bytes): active game data with 7 code references
- No safe injection point exists in the code section for new code

## Lesson Learned: Why ARM9 Expansion Fails

Attempting to expand ARM9 and move BSS_start causes a white screen because:

1. **Autoload list corruption**: Autoload list bytes at the OLD location become garbage if BSS_start moves. These bytes must be covered by BSS zeroing (CRT0 zeros [BSS_start, BSS_end)). If autoload list is outside the new BSS range, it stays corrupted.

2. **Code zeroing**: Any non-zero code appended in the BSS region gets zeroed at startup, making injected code unreachable.

3. **Uninitialized globals**: Moving BSS_start always leaves some BSS global variables uninitialized, causing game behavior changes or crashes.

**Conclusion**: All patches must be in-place (same size), no ARM9 expansion.

---

## ARM32 Instruction Encoding Quick Reference

Instructions shown in **little-endian byte format** (as they appear in ROM):

### Arithmetic & Load/Store
| Instruction | LE Bytes | Hex Encoding | Notes |
|-------------|----------|--------------|-------|
| ADD r5, r0, #1 | 01 50 80 E2 | E2805001 | Original breed range |
| ADD r6, r0, #1 | 01 60 80 E2 | E2806001 | Original color range |
| MOV r5, #2 | 02 50 A0 E3 | E3A05002 | PATCH3 breed (50% ANY) |
| MOV r6, #2 | 02 60 A0 E3 | E3A06002 | PATCH3 color (50% ANY) |
| MOV r5, #N | 0N 50 A0 E3 | E3A0500N | Any range N (0–255) |
| MOV r6, #N | 0N 60 A0 E3 | E3A0600N | Any range N (0–255) |
| MOV r0, #0 | 00 00 A0 E3 | E3A00000 | Load zero |
| BX lr | 1E FF 2F E1 | E12FFF1E | Return (PATCH1 uses this) |
| NOP | 00 00 A0 E1 | E1A00000 | MOV r0, r0 |
| STRB rd, [rn, #imm] | — | E5C{rn}{rd}{imm12} | Store byte to memory |

### Control Flow
| Instruction | LE Bytes | Notes |
|-------------|----------|-------|
| BL target | encoded | (target − (PC+8)) / 4, 24-bit signed offset |
| BLX r2 | 32 FF 2F E1 | Branch with link exchange (switch modes) |

### Pattern: Condition Code Suffix
All ARM32 instructions can have condition suffixes appended to the base encoding:
- `E` = AL (always, unconditional)
- `0` = EQ (zero flag set)
- `1` = NE (zero flag clear)
- etc.

The fourth byte (`E2`, `E3`, `E1`) encodes the instruction and condition. For patching, the low 4 bits of byte 3 are usually `E` (always execute).

### Common ARM32 Registers in This Code
| Register | Role |
|----------|------|
| r0 | Function parameter / return value; random range base |
| r1 | Function parameter |
| r2 | Temporary; often holds dereferenced pointer (vtable) |
| r4 | Species (first animal creator) |
| r5 | Breed range (first creator); breed value (second creator) |
| r6 | Color range (first creator); color value (second creator) |
| r8 | New animal object (STRB target) |
| r10 | Request object (UI handler context) |
| r11 | Species (second animal creator) |
| lr (r14) | Return address |
| sp (r13) | Stack pointer |

---

## Animal Object Layout

20 bytes total. Relevant fields:

| Offset | Field | Size | Notes |
|--------|-------|------|-------|
| +0x00 | vtable ptr | 4 B | Points to virtual method table |
| +0x04 | refcount | 4 B | Reference counting (used by manager) |
| +0x0E | flag | 1 B | Unknown flag byte |
| +0x0F | species | 1 B | Signed byte: species index |
| +0x10 | breed | 1 B | Signed byte, **-1 (0xFF) = ANY** |
| +0x11 | color | 1 B | Signed byte, **-1 (0xFF) = ANY** |

- **ANY sentinel**: -1 when stored as signed byte (0xFF as unsigned)
- A request with ANY breed/color matches any animal of that species regardless of specific breed/color

## First Animal Creator — Random Generation (arm9:0x40100)

- **Breed range** (arm9:0x402A4): `ADD r5, r0, #1` → PATCH3 replaces with `MOV r5, #N`
- **Color range** (arm9:0x40328): `ADD r6, r0, #1` → PATCH3 replaces with `MOV r6, #N`
- **LCG multiplier**: `0x41C64E6D` (classic NDS/GBA constant), range mask `0x3FFFF`
- Formula: `if random % range == 0` → value = -1 (ANY); else → specific index
- STRB block (arm9:0x403B8): writes `r4` (species), `r5` (breed), `r6` (color) to `+0x0F/+0x10/+0x11`

## Second Animal Creator — Clone (arm9:0x42218)

- Reads species/breed/color from a **source object** via vtable calls (no RNG)
- STRB block (arm9:0x422E8): writes `r11` (species), `r5` (breed), `r6` (color) to new object
- Inherits ANY naturally from source if source was generated with ANY

## Request Array Mechanics

- **Manager global ptr**: VA `0x020734E4` → `[+0x120]` = `request_array`
- **Slot storage**: `array + 0xF4 + i*4`, where `i = 0..6` (7 slots)
- **Accessor** (0x0204059C): returns `r0 + 0xF4 + r1*4`
- **Registration** (0x02040564): stores `raw_animal_ptr`, manages refcounts

## Key Functions Table

| VA | arm9 offset | Role |
|----|-------------|------|
| 0x02036224 | 0x36224 | Texture replacement check — PATCH1 target |
| 0x020402A4 | 0x402A4 | Breed range setup — PATCH3 target |
| 0x02040328 | 0x40328 | Color range setup — PATCH3 target |
| 0x020403B8 | 0x403B8 | First animal creator STRB block |
| 0x02040564 | 0x40564 | Slot registration (refcounting) |
| 0x0204059C | 0x4059C | Request array slot accessor |
| 0x02042218 | 0x42218 | Second animal creator (clone, start) |
| 0x020422E8 | 0x422E8 | Second animal creator STRB block |
| 0x020734E4 | — | Manager global ptr |
| 0x0201B9F4 | 0x1B9F4 | Request manager UI handler |
| 0x020428A4 | 0x428A4 | Treatment row constructor (`FUN_020428A4`) |
| 0x02042AD4 | 0x42AD4 | State-machine lookup (`FUN_02042AD4`) |
| 0x0200E2B4 | 0xE2B4 | Caller that selects b1/b2 and calls `FUN_020428A4` |
| 0x02042818 | 0x42818 | Treatment selector RNG loop (`FUN_02042818`) |
| 0x02042A64 | 0x42A64 | Load one tool resource by name into slot array |

