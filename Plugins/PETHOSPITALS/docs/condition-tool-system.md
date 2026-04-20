# Condition & Tool System — Key Findings

## Overview

The game "Let's Play Pet Hospitals" (NDS, code C2HP) has a condition state machine
that assigns treatments to animal conditions. EyeDrop and Scissor tool icons exist
in the game assets but are never triggered during normal gameplay.

---

## Data Structures (all ROM-static, fully readable)

### Tool Resource Array — PTR_DAT_02061e90
- **31 slots × 8 bytes** (stride 8), built at init by 31 calls to FUN_02042a64
- Each slot holds a pointer to a tool definition object (searched by name from the 22-entry tool table at VA 0x02070134)
- Slot mapping (confirmed):

| Slot | Tool            | Slot | Tool              |
|------|-----------------|------|-------------------|
| 0    | ShowerHose      | 16   | **Scissor**       |
| 1    | SoapySponge     | 17   | **Scissor**       |
| 2    | ShowerHose      | 18   | FleaPesticide     |
| 3    | Dryer           | 19   | FleaCleaningBrush |
| 4    | CombingBrush    | 20   | IceBag            |
| 5    | SoapySponge     | 21   | BruiseOintment    |
| 6    | ShowerHose      | 22   | AntibioticsShot   |
| 7    | Dryer           | 23   | Antidote          |
| 8    | Dryer           | 24   | Dewormer          |
| 9    | ShowerHose      | 25   | AsthmaMedicine    |
| 10   | SoapySponge     | 26   | CottonSwab        |
| 11   | Gauze           | 27   | EarDrop           |
| 12   | DisinfectantSpray| 28  | Otoscope          |
| 13   | Bandage         | 29   | Stethoscope       |
| 14   | Bandage         | 30   | ScabiesShampoo    |
| 15   | Bandage         | —    | — |

**Patched state (arm9_EyeDropPatch.bin):**
- Slots 16/17 load **EarDrop** (literal pool 0x61EAC → 0x02071854) instead of Scissor
- Slot 26 loads **EarDrop** (literal pool 0x61ED0 → 0x02071854) instead of CottonSwab
- EYEINFECTION state-machine entry: key2=0x15, b1=0x1B, b2=0x1B → treatable with slot 27 (EarDrop)
- EyeDrop (entry 21 in definition table) is **not** assigned to a slot — EarDrop (slot 27) is used instead

**Original state:**
- Scissor occupies slots 16 and 17 but is never referenced by any condition entry
- EyeDrop exists in the definition table but is never loaded into a slot

### Tool system offset correction

**Hallazgo confirmado (comparando `ARM9.bin` y `arm9_EyeDropPatch.bin`):**

- `0x0206FC40` / arm9 `0x6FC40` is **not** a condition state machine table.
- It is a **tool resource array** with **31 entries × 8 bytes**.
- The init code at `0x02061BE8` / arm9 `0x61BE8` loads tool names from the literal pool near
  `0x61E90` and stores the returned tool objects into `0x0206FC40` with stride 8.
- `0x0206FD10` / arm9 `0x6FD10` falls inside that same array:
  - `0x6FD10 - 0x6FC40 = 0xD0`
  - `0xD0 / 8 = 26`
  - therefore `0x6FD10` = **slot 26** of the tool array, **not** an EYEINFECTION condition entry.

**Discarded previous hypotheses:**

- ❌ `0x6FC40` as “condition state machine table”
- ❌ `0x6FD10` as “EYEINFECTION entry”

Those offsets belong to the **tool system**, not the real condition-assignment logic.

#### What Patch 5 actually changes in this area

Patch 5 reuses the duplicated `Scissor` slots in the tool array so they load `EyeDrop` instead:

- `0x61EAC` literal/pointer changes from `Scissor` to `EyeDrop`
- `0x61CFC` is part of the same init routine and reloads the array base before storing slot 17

This region is therefore related to **tool initialization**, not to selecting the medical condition
assigned to a patient.

### Condition State Machine Table — VA 0x0206fc40
> **Obsolete hypothesis — kept below only as historical analysis.**
> Later binary comparison showed that `0x0206FC40` is a tool array, so this section should not be
> used as authoritative documentation for the condition system.

- **31 entries × 8 bytes**: `[key1(4B)][key2(1B)][b1(1B)][b2(1B)][ext(1B)]`
- `key2` = current condition type (matches the type byte at cblock+4)
- `b1`, `b2` = **interpretation uncertain** — may be next-state type (forming treatment chains) for grooming/wound conditions, or tool slots for disease conditions, or determined by a separate lookup path. User confirms CombingBrush treats MESSYFUR in-game, but key2=24 has b1=22 — so b1/b2 are NOT straightforward tool slot indices for at least some entries.
- `b1 = b2 = 255` = condition has no treatment (terminal / untreatable)
- `ext` = alternate path flag (some key2 values appear twice, once with ext=0 and once with ext=1)
- Pool pointer at VA 0x2042b30 → base address 0x0206fc40 (ROM offset 0x606c40)
- **No entry has b1=16 or b1=17** — confirmed by full table scan

Full table dump (from analysis7):

| # | key2 | b1  | b2  | ext | Notes |
|---|------|-----|-----|-----|-------|
| 0 | 0    | 1   | 1   | 0   | Grooming chain start |
| 1 | 1    | 2   | 2   | 0   | |
| 2 | 2    | 3   | 3   | 0   | |
| 3 | 3    | 26  | 26  | 0   | → CottonSwab step |
| 4 | 26   | 255 | 255 | 0   | Terminal |
| 5 | 0    | 4   | 4   | 1   | Alternate: DIRTY→DIRTYSOAPY branch |
| 6 | 4    | 1   | 1   | 0   | DIRTYSOAPY→type1 |
| 7 | 1    | 0   | 0   | 1   | Alternate back-path |
| 8 | 2    | 5   | 5   | 1   | Alternate drying branch |
| 9 | 5    | 3   | 3   | 0   | |
|10 | 3    | 2   | 2   | 1   | Alternate back-path |
|11 | 6    | 7   | 7   | 0   | Wound chain |
|12 | 7    | 8   | 8   | 0   | |
|13 | 8    | 255 | 255 | 0   | Terminal |
|14 | 6    | 10  | 10  | 1   | Alternate wound branch |
|15 | 7    | 9   | 9   | 0   | Alternate wound step |
|16 | 10   | 6   | 6   | 0   | |
|17 | 9    | 7   | 7   | 0   | |
|18 | 11   | 12  | 12  | 0   | Bruise/swelling chain |
|19 | 12   | 255 | 255 | 0   | Terminal |
|20 | 13   | 14  | 14  | 0   | Bandaged chain |
|21 | 14   | 255 | 255 | 0   | Terminal |
|22 | 16   | 255 | 255 | 0   | Terminal (WORMS in-game) |
|23 | 17   | 255 | 255 | 0   | Terminal (ASTHMA in-game) |
|24 | 18   | 255 | 255 | 0   | Terminal |
|25 | 19   | 255 | 255 | 0   | Terminal |
|26 | 21   | 255 | 255 | 0   | **EYEINFECTION — untreatable** |
|27 | 22   | 255 | 255 | 0   | Terminal |
|28 | 24   | 22  | 21  | 0   | MESSYFUR entry (b1/b2 role unclear — CombingBrush confirmed in-game) |
|29 | 25   | 18  | 19  | 0   | FLEAS? (18=FleaPesticide, 19=FleaCleaningBrush if tool slots) |
|30 | 20   | 255 | 255 | 0   | Terminal |

Missing key2 values: **15** and **23** (no entry in table).

---

## All Conditions — Complete List

26 condition name strings confirmed in ROM (pool VA 0x0206130c–0x0206137c).
Ordered by pool address. State machine key2 mapping is not fully resolved — see table above.

### Grooming States
These form treatment chains (types 0–5, 26 in state machine). Tool is applied step by step.

| Name              | Pool VA    | State machine key2 (inferred) | In-game | Notes |
|-------------------|------------|-------------------------------|---------|-------|
| DIRTY             | 0x0206130c | 0                             | Yes     | Initial dirty state — washed with ShowerHose or SoapySponge |
| MUDDY             | 0x02061310 | 4 or 5 (uncertain)            | Yes     | Extra-dirty state; needs additional soaping step |
| SOAPY             | 0x02061318 | 1                             | Yes     | After initial wash — rinse with ShowerHose |
| DIRTYSOAPY        | 0x02061320 | 4 (inferred from ext=1 chain) | Yes     | Partially washed dirty state |
| DRYSOAP           | 0x02061324 | 2 or 3                        | Yes     | After rinsing — drying step |
| MESSYFUR          | 0x0206137c | 24 (state machine) / 4 (tool) | Yes     | **Treated with CombingBrush (confirmed)**; Scissor was intended but removed |

### Wound States
Treatment chain progresses through cleaning, disinfecting, and bandaging steps.

| Name                | Pool VA    | State machine key2 (inferred) | In-game | Notes |
|---------------------|------------|-------------------------------|---------|-------|
| DIRTYWOUND          | 0x02061328 | 6                             | Yes     | Entry point for wound treatment chain |
| CLEANWOUND          | 0x0206132c | 7                             | Yes     | After initial cleaning |
| DISINFECTEDWOUND    | 0x02061330 | 8                             | Yes     | Terminal — fully treated wound |
| BANDAGEDCLEANWOUND  | 0x02061334 | 10 (ext=1 branch)             | Yes     | Alternate wound path — clean then bandage |
| BANDAGEDDIRTYWOUND  | 0x02061338 | 9 (ext=1 branch)              | Yes     | Alternate wound path — dirty bandaged wound |
| BANDAGED            | 0x0206134c | 13                            | Yes     | Bandage applied — chain continues to INFECTION check |

### Infestation

| Name      | Pool VA    | State machine key2 | In-game | Notes |
|-----------|------------|-------------------|---------|-------|
| FLEAS     | 0x0206133c | 25 (inferred)     | Yes     | Treated with FleaPesticide + FleaCleaningBrush |
| DEADFLEAS | 0x02061340 | (uncertain)       | Yes     | Post-treatment state after flea removal |

### Injury

| Name          | Pool VA    | State machine key2 | In-game | Notes |
|---------------|------------|-------------------|---------|-------|
| SWELLINGBRUISE| 0x02061344 | 11                | Yes     | Chain: 11→12→terminal |
| BRUISE        | 0x02061348 | 12                | Yes     | Terminal after treatment |

### Disease

| Name      | Pool VA    | State machine key2 | In-game | Treatment tool (confirmed/inferred) | Notes |
|-----------|------------|-------------------|---------|--------------------------------------|-------|
| INFECTION | 0x02061350 | 14                | Yes     | AntibioticsShot (slot 22)           | Terminal in state machine |
| POISONING | 0x02061354 | 15 (missing!)     | Yes     | Antidote (slot 23, inferred)        | **Confirmed in-game, but key2=15 absent from state machine table — either key2 mapping is wrong or POISONING uses a separate code path** |
| WORMS     | 0x02061358 | 16                | Yes     | Dewormer (slot 24, inferred)        | Terminal in state machine |
| ASTHMA    | 0x0206135c | 17                | Yes     | AsthmaMedicine (slot 25, inferred)  | Terminal in state machine |
| SCABIES   | 0x02061364 | 18                | Yes     | ScabiesShampoo (slot 30, inferred)  | Terminal in state machine |

### Ear Conditions

| Name        | Pool VA    | State machine key2 | In-game | Notes |
|-------------|------------|-------------------|---------|-------|
| EARWITHWAX  | 0x02061368 | 19                | Yes     | Terminal in state machine; tools Otoscope+EarDrop loaded (slots 27-28) |
| INFLAMEDEAR | 0x0206136c | 20                | Yes     | Terminal in state machine |

### Eye Condition — ✅ Now treatable via patch bundle

| Name | Pool VA | State machine key2 | In-game | Notes |
|------|---------|-------------------|---------|-------|
| EYEINFECTION | 0x02061370 | 21 | No (natural) / Yes (patched) | Original: b1=b2=255 (untreatable). **Patch bundle**: literal pool 0x61EAC → EarDrop (0x02071854), b1=b2=0x1B (slot 27), key2=0x15 in state-machine entry. Condition row 23 in 0x02070564 is unimplemented (no asset strings). Fila 23 +0x18 left at 0x00. |

### "Unknown" — Hidden Condition Placeholder States

These are NOT unidentified conditions — they are part of the **"buy a clue" mechanic**. When an animal has a head or body condition that hasn't been revealed yet, the game displays it as "Unknown." Once the player purchases a clue, the game transitions from UNKNOWNHEAD/UNKNOWNBODY to the actual condition (e.g. INFECTION, WORMS). The b1=b2=255 in the state machine reflects that these states cannot be treated until revealed.

| Name        | Pool VA    | State machine key2 | In-game | Notes |
|-------------|------------|-------------------|---------|-------|
| UNKNOWNHEAD | 0x02061374 | 22                | Yes     | Hidden head condition — b1=b2=255 (untreatable until clue purchased) |
| UNKNOWNBODY | 0x02061378 | 23 (missing!)     | Yes     | Hidden body condition — **key2=23 absent from state machine table** |

---

## Condition Generators

### FUN_0203f034 (generator 1, wraps at 8) — VA 0x0203f034
### FUN_0203f390 (generator 2, wraps at 9) — VA 0x0203f390

Both are called from FUN_0203edac (new patient arrival, VA 0x0203edac).

- `r4` = **condition COUNT** (level-based, not type index):
  - Gen1: level ≤ 5 → 2–4; level 6–11 → 5–7; level > 11 → 8–10
  - Gen2: level ≤ 5 → 2; level 6–11 → 3; level > 11 → 4
- `r7` = initial RNG value derived from LCG state (mask 0x1fff, multiplier 0x41c64e6d)
- TYPE BYTE = r7 directly (passes through float round-trip FUN_020595bc/02059534 which is identity for small ints)
- `r7` wraps mod 8 (gen1) or mod 9 (gen2); step = floor(45 / r4)
- Based on this, generators should produce types **0..8 only**

**Known gap:** User confirms ALL conditions except EYEINFECTION appear in-game — including SCABIES, EARWITHWAX, INFLAMEDEAR, POISONING, and WORMS, all with inferred type bytes above 8. This definitively contradicts the 0..8 range conclusion. Either:
  1. There is a third condition-assignment mechanism beyond the two generators (unanalyzed call at 0x0203efd4 in FUN_0203edac)
  2. The type byte is offset or transformed in a way not yet captured
  3. The generator range analysis itself was wrong (the float round-trip may not be identity in all cases)

**This is the highest-priority open question** for understanding the full condition system.

### Complete condition generation flow (confirmed)

#### 1) RNG selection from table `0x02070564`

- `FUN_02042818` (`0x02042818–0x02042893`) computes an index `uVar1` over a stride-`0x1C` table at `0x02070564`.
- In ASM the return value comes out in `r0` (`mov r0,r2` at the end), but **`r2` is no longer the index**: it is the pointer returned by `FUN_020428a4`.
- Therefore, `FUN_02042818` **does not return the raw index**; it returns a **pointer to a 0x48-byte runtime struct**.
- The selected index is preserved inside that struct at `runtime+0x10` (`puVar2[4] = param_1` inside `FUN_020428a4`).

#### 2) Runtime construction in `FUN_020428a4`

`FUN_020428a4` takes the index and materializes a runtime object using entry `index * 0x1C + 0x02070564`:

- `+0x04` → pointer/resource passed to `FUN_0203d77c`
- `+0x08` → pointer/resource passed to `FUN_0203d718`
- `+0x0C` → main pointer/resource passed to `FUN_0203d6c8`
- `+0x10` → auxiliary parameter for `FUN_0203d6c8`
- `+0x14` → auxiliary parameter for `FUN_0203d6c8`
- `+0x18` → validity flag, also copied to `runtime+0x14`

No **3-byte compact packing** equivalent to the final patient descriptor appears here.

#### 3) Confirmed callers of `FUN_02042818`

- `FUN_0203d074` → `FUN_02042818` → `FUN_0203c524`
- `FUN_02019464` → `FUN_02042818` → `FUN_0203c524`
- `FUN_02018d68` → `FUN_02042818` → `FUN_0203c524`

In all three cases the value returned by `FUN_02042818` is immediately consumed as a **runtime object** by `FUN_0203c524`, which inserts/appends it to a condition object list. No conversion to `sp+4`, `sp+5`, `sp+6` is observed.

#### 4) Confirmed callers of `FUN_020428a4`

- Internal from `FUN_02042818` (`0x0204287C`)
- `FUN_0203cf4c` → reads serialized 1-byte indices and calls `FUN_020428a4(index, ...)` directly
- `FUN_0200e240` → obtains an alternate index (`uVar5 & 0xFF`) and calls `FUN_020428a4(index, ...)` directly

This confirms that `FUN_020428a4` is the common runtime constructor for the `0x02070564`-based system, but **that system is not the same one that generates the 3-byte descriptor written by `FUN_0203f874` during patient creation**.

#### 4.1) Direct read of `arm9.bin` at `0x02070564`

Direct binary read of `Plugins/PETHOSPITALS/testResources/arm9.bin`:

- offset `0x70564`
- VA `0x02070564`
- stride `0x1C`

Findings for the first `64` rows:

- rows `0..26` have a coherent structure
- from row `27` the block no longer resembles a table and transitions to ASCII bytes from an adjacent zone; the useful block appears to end at `26`

Decoded resources from valid rows (`row+0x04/+0x08/+0x0C`):

- `0`  → `dirty`, `partDirty`
- `1`  → `muddy`, `partMuddy`
- `2`  → `soapy`, `partSoapy`
- `3`  → `wet`, `partWet`
- `4`  → `dirty`, `partDirtySoapy`
- `5`  → `drySoap`, `partDrySoap`
- `6`  → `dirtyWound`
- `7`  → `cleanWound`
- `8`  → `disinfectedWound`
- `9`  → `bandagedCleanWound`
- `10` → `wrongWound`
- `11/12` → `fleas`
- `13` → `swellBruise`
- `14` → `bruise`
- `15` → `bandagedWound`
- `16/17` → `partInfection`
- `20` → `scabies`
- `24/25` → `unknown`

No valid row was found using resources directly anchored to:

- `s_SCISSOR_02071584`
- `s_COTTONSWAB_020716e0`
- `s_EYEDROP_020716fc`
- `s_Scissor_020717cc`
- `s_CottonSwab_02071848`

Conclusion from this direct read:

- `0x02070564` does not contain, at least in its first valid rows, any entry
  that directly points to resources named `EyeDrop`, `CottonSwab` or `Scissor`
- the visible resources appear to be internal state/part names
  (`dirty`, `partWet`, `cleanWound`, `partInfection`, etc.) and not tool names
- therefore, **no `EyeDrop` row is yet identifiable with certainty** in this
  table from direct string anchors

#### 5) Real path of the `sp+4..6` descriptor in `FUN_0203edac`

In `FUN_0203edac`, the final descriptor that ends up at `patient+4..6` is built locally on the stack:

- `sp+4` is forced to `0`
- `sp+5` is computed via the sequence `FUN_020595bc` → `FUN_020597f0` → `FUN_02059604` → `FUN_02059534`
- `sp+6` is computed with a similar sequence but with an additional multiplication/transform step
- then `add r2,sp,#0x4` and `FUN_0203f874(...)` copies those three bytes to `patient+4`, `+5`, `+6`

That is: those 3 bytes **do not come from any entry in `0x02070564`**. Their immediate source is a **local stack descriptor** generated at runtime by `FUN_0203edac`.

The same applies to `FUN_0203f034` and `FUN_0203f390`: both create local bytes (`local_2d/local_2c/local_2b` or equivalent) and pass them to `FUN_0203f874`.

#### 6) Conclusion: `0x02070564` offsets vs. final descriptor

- **Descriptor byte 0:** does not correspond to a direct offset in the `0x02070564` entry
- **Descriptor byte 1:** does not correspond to a direct offset in the `0x02070564` entry
- **Descriptor byte 2:** does not correspond to a direct offset in the `0x02070564` entry

The observable offsets in `0x02070564` are resource pointers and flags (`+0x04`, `+0x08`, `+0x0C`, `+0x10`, `+0x14`, `+0x18`), not a compact 3-byte descriptor for `FUN_0203f874`.

#### 7) MESSYFUR / EYEINFECTION

- Deep strings `s_EYEINFECTION_020714a8` and `s_MESSYFUR_020714d0` were located in `GhidraDumps/all_strings_deep.json`.
- With current evidence, **there is no confirmed correspondence** between those symbolic entries and the final `patient+4..6` bytes.
- If those conditions are represented inside the `0x02070564` system, what is observable from this path would be runtime-associated pointers/resources, **not** the three bytes that `FUN_0203f874` writes to the patient.

#### Flow diagram

Path `0x02070564` / runtime:

`FUN_02042818`
↓
`FUN_020428a4`
↓
`(runtime struct, index at +0x10)`
↓
`FUN_0203c524`

Patient creation / final descriptor path:

`FUN_0203edac`
↓
`(local stack bytes at sp+4..6)`
↓
`FUN_0203f874`
↓
`patient +4..6`

Conclusion: **no confirmed bridge between both paths**.

## Confirmed runtime condition indices

Results confirmed in-game using the experimental patch in `FUN_020428A4`
(`0x428A8` → `MOV r6, #index`):

- `22` → inflamed ear
- `23` → `EYEINFECTION` (eye infection)

### Note on the observed internal offset

In earlier analysis `EYEINFECTION` had been recorded as `21`, but runtime
testing confirmed `23` as the stable final index at this point in the flow.

This indicates an observed internal offset of approximately `+2` between
some earlier calculations/labels and the final runtime index used by
`FUN_020428A4` / `runtime+0x10`.

That offset explains the historical discrepancy `21` → `23`.

### Real lookup algorithm in `FUN_02042AD4`

Confirmed ASM (`0x02042AD4`):

```text
02042ad8 ldr   r4, [0x2042b30]   ; table base
02042adc mov   lr, #0            ; iteration index
02042ae0 ldr   r12, [r4,#0x0]    ; key1 (4 bytes)
02042ae4 cmp   r12, r0
02042ae8 ldrbeq r12, [r4,#0x4]   ; key2 (1 byte)
02042aec cmpeq r12, r1
02042af0 bne   0x02042b18
02042af4 ldrb  r12, [r4,#0x5]    ; b1
02042b04 ldrb  r2,  [r4,#0x6]    ; b2
02042b0c ldrb  r2,  [r4,#0x7]    ; ext
02042b18 add   lr, lr, #1
02042b1c cmp   lr, #0x1f
02042b20 add   r4, r4, #0x8
```

This confirms:

- **entry stride = `8` bytes**
- the function scans up to **31 entries** (`0x1F`)
- the match condition is exact:
  - `*(u32 *)(entry + 0x0) == r0`  → `key1`
  - `*(u8  *)(entry + 0x4) == r1`  → `key2`

#### Direct dump of `arm9.bin` for entries `0..15` of `0x0206FC40`

Base used:

- RAM: `0x0206FC40`
- ROM offset: `0x06FC40`
- stride: `8`

Output format: `index | offset | hex 8 bytes`

```text
0  | 0x06FC40 | 00 00 00 00 00 01 01 00
1  | 0x06FC48 | 00 00 00 00 01 02 02 00
2  | 0x06FC50 | 00 00 00 00 02 03 03 00
3  | 0x06FC58 | 00 00 00 00 03 1A 1A 00
4  | 0x06FC60 | 00 00 00 00 1A FF FF 00
5  | 0x06FC68 | 00 00 00 00 00 04 04 01
6  | 0x06FC70 | 00 00 00 00 04 01 01 00
7  | 0x06FC78 | 00 00 00 00 01 00 00 01
8  | 0x06FC80 | 00 00 00 00 02 05 05 01
9  | 0x06FC88 | 00 00 00 00 05 03 03 00
10 | 0x06FC90 | 00 00 00 00 03 02 02 01
11 | 0x06FC98 | 00 00 00 00 06 07 07 00
12 | 0x06FCA0 | 00 00 00 00 07 08 08 00
13 | 0x06FCA8 | 00 00 00 00 08 FF FF 00
14 | 0x06FCB0 | 00 00 00 00 06 0A 0A 01
15 | 0x06FCB8 | 00 00 00 00 07 09 09 00
```

Quick byte breakdown:

- `+0x00..+0x03` = `key1`
- `+0x04` = `key2`
- `+0x05` = `b1`
- `+0x06` = `b2`
- `+0x07` = `ext`

There is no internal transformation of `r1` inside `FUN_02042AD4`.
`r1` is compared **directly** against `key2`.

The same applies to `r0`: no internal transformation. `r0` is compared
**directly** against `key1`.

#### Registers used

- `r0` → searched `key1`
- `r1` → searched `key2`
- `r4` → pointer to current table entry
- `r12` → scratch register for loading/comparing `key1` and `key2`
- `r2` → output pointer for `b1`
- `r3` → output pointer for `b2`
- `[sp+8]` → output pointer for `ext` (5th parameter)
- `lr` → loop counter / iteration index

#### Full observed flow

Relevant caller at `FUN_0200E2B4`:

- `r0 = [r10 + 0xA8]`
- `r1 = [condition_runtime + 0x10]`
- `r2 = &b1`
- `r3 = &b2`
- `[sp+0] = &ext`
- `BL FUN_02042AD4`

On match:

- `b1 = entry[5]`
- `b2 = entry[6]`
- `ext = entry[7]`
- `r0 = 1`

On no match after 31 entries:

- `r0 = 0`

#### EYEINFECTION: `21` vs `23`

- `runtime index` confirmed in-game: `23`
- historical `EYEINFECTION` row in the table: entry `26`
- entry `26` offset: `0x0206FD10`
- `key1` expected by that row: `0`
- historical `key2` in that row: `21`
- `key2` adjusted by EyeDrop patch bundle: `23`
- `b1/b2` bytes: `0x0206FD15` / `0x0206FD16`

This means row `0x0206FD10` only matches `EYEINFECTION` for the exact search pair:

- `r0 = 0`
- `r1 = 21` (original ROM)
- `r1 = 23` (patched ROM)

If `r0 != 0` or `r1` does not match the `key2` actually stored in the row
(`21` in base ROM, `23` in patched ROM), `FUN_02042AD4` does not consider that
row a match and continues iterating until it returns `0`.

Since `FUN_02042AD4` compares `r1` **without any transformation**, the real
lookup algorithm expects the exact value stored in `key2`.

Therefore, with current evidence:

- the real observed runtime index for `EYEINFECTION` is `23`
- `FUN_02042AD4` does not normalize `r1`
- the most direct and stable fix is to adjust the table row to accept `key2 = 23`

That adjustment is applied directly in the historical `EYEINFECTION` row:

- arm9 `0x6FD14`
- original: `0x15` (`21`)
- patched: `0x17` (`23`)

With current evidence, `key1` must also be verified at runtime, because the
`EYEINFECTION` row depends not only on `key2` but on the full pair:

- historical: `(key1=0, key2=21)`
- patched: `(key1=0, key2=23)`

#### Final table structure

- historical base: `0x0206FC40`
- format: `[key1(4B)][key2(1B)][b1(1B)][b2(1B)][ext(1B)]`
- stride: `8` bytes
- entries scanned: `31`

---

## EyeDrop / EYEINFECTION — Resolved ✅

**Original blockers (baseline game):**
1. EyeDrop string exists but is not loaded into the tool resource array.
2. EYEINFECTION is untreatable in the state machine (`b1=b2=255`).
3. EYEINFECTION is not observed in normal play.

**Solution (confirmed working with patch bundle):**
- Literal pool `0x61EAC` → `0x02071854` ("EarDrop") — slots 16/17 now load EarDrop
- Literal pool `0x61ED0` → `0x02071854` ("EarDrop") — slot 26 now loads EarDrop
- State-machine entry at `0x6FD14`: key2=`0x15`, b1=`0x1B`, b2=`0x1B` — EYEINFECTION treatable with slot 27 (EarDrop)
- Slot 26 key2 (`0x6FD14`) patched to `0x17`; revert restores `0x0F`

EYEINFECTION condition row 23 in `0x02070564` remains unimplemented (fields null, `+0x18=0x00`).
The treatment is delivered via the state-machine path, not via the condition table row.

## Why Scissors Are Never Used

1. **No condition references them**: Zero entries in the 31-entry state machine table have b1=16 or b1=17.
2. **MESSYFUR treated with CombingBrush**: MESSYFUR (key2=24) uses CombingBrush (slot 4) in practice; the table entry `b1=22` may correspond to a different path.

Both Scissor slots were repurposed by the EarDrop patch bundle.

---

## Key Function Table

| VA         | arm9 offset | Role |
|------------|-------------|------|
| 0x0203edac | 0x3edac     | New patient arrival — calls both generators |
| 0x0203f034 | 0x3f034     | Generator 1 (wraps at 8) |
| 0x0203f390 | 0x3f390     | Generator 2 (wraps at 9) |
| 0x0203f874 | 0x3f874     | Condition insert: `(patient, block, ptr[type,b1,b2], slot, tick)` |
| 0x02042a64 | 0x42a64     | Load one tool resource by name into array |
| 0x02042ad4 | 0x42ad4     | Search condition state machine table by (key1, key2) |
| 0x020595bc | 0x595bc     | int→float (software FP) — identity for small ints |
| 0x02059534 | 0x59534     | float→int (software FP) — identity for small ints |
| 0x02061BE8 | 0x61be8     | Init function: builds both tables, 31 tool resource loads |

## Key Addresses

| Address (VA)  | arm9 offset | Contents |
|---------------|-------------|----------|
| 0x0206fc40    | 0x6fc40     | **Tool resource array base** (31 entries × 8B) — confirmed by init code at `0x61BE8`; prior “condition state machine” attribution discarded |
| 0x0206fd10    | 0x6fd10     | **Tool array slot 26** (`0x6FD10 = 0x6FC40 + 26×8`) — prior “EYEINFECTION entry” attribution discarded |
| 0x02070134    | 0x70134     | Tool definition table (22 entries × 24B, searched by name) |
| 0x02070564    | 0x70564     | **Condition object table** (26 entries × 0x1C B) — enable/disable flags |
|               |             | Flag byte at `entry_base + 0x18`: `0x01` = enabled, `0x00` = disabled |
|               |             | **Entry order not verified**. Prior hardcoded mappings (e.g. "entry 22 = EYEINFECTION") were contradicted by the experiment preserving only `MESSYFUR`. |
| 0x020734E4    | —           | Global manager pointer (BSS) |
| 0x02061e6c    | 0x61e6c     | Pool: PTR_s_EYEDROP string (init table, unused slot) |
| 0x02061e0c    | 0x61e0c     | Pool: PTR_s_SCISSOR string (init table) |
| 0x02061370    | 0x61370     | Pool: PTR_s_EYEINFECTION string (condition hash table) |

### Condition object table layout (arm9 0x70564, 26 entries × 0x1C B)

Each entry:

```
+0x00  dword  (type ID or vtable ptr)
+0x04  ptr    VA pointer (code reference)
+0x08  dword  (unknown)
+0x0C  ptr    VA pointer to condition name string
+0x10  dword  (unknown)
+0x14  dword  (unknown)
+0x18  byte   enable/disable flag  ← PATCH TARGET
+0x19  byte   (padding/unknown)
+0x1A  byte   (padding/unknown)
+0x1B  byte   (padding/unknown)
```

### Experiment notes (forcing a single condition)

- Setting the flag byte at `entry + 0x18` to `0x00` disables that condition.
- Preserving exactly one entry produces a reliable “only one condition appears” behavior.
- However, the identity/index of `EYEINFECTION` in this table is still unresolved.
  Current builds preserve `MESSYFUR` when attempting to preserve `EYEINFECTION`.

Safety:
- Treat the table as exactly 26 entries. Attempts to write beyond that caused a black screen when adopting an animal.
- Do not rely on a fixed index list until the table is validated by decoding the name-string pointers at `+0x0C`.

---

## Real condition assignment flow (confirmed by ARM9 analysis)

This section is based on the exported Ghidra artifacts in `docs/`:

- `nds_full_listing.txt`
- `functions.json`
- `all_strings_deep.json`

### Confirmed function roles

| Function | VA / arm9 | Role | Called by | Calls |
|----------|-----------|------|-----------|-------|
| `FUN_02042818` | `0x02042818` / `0x42818` | Randomly selects a valid condition-table entry from `0x02070564`, checking the enable flag at `+0x18` and retrying until a valid entry is produced | `0x02019558`, `0x0203d0ec`, `0x02018dd0` | `0x020428a4` |
| `FUN_020428a4` | `0x020428A4` / `0x428A4` | Builds a runtime object from one entry of the `0x02070564` table (index × `0x1C`) | called from `FUN_02042818` and other callers | multiple helper calls |
| `FUN_0203edac` | `0x0203EDAC` / `0x3EDAC` | New-patient creation path; allocates objects, calls generators, builds a 3-byte local condition descriptor on the stack, then inserts it | `0x0200a864` | `0x0203f034`, `0x0203f390`, `0x0203f874`, plus setup helpers |
| `FUN_0203f874` | `0x0203F874` / `0x3F874` | Writes the condition descriptor to the patient block and inserts/merges it into the runtime list | `0x0203efd4`, `0x02041fd8`, `0x0203f4cc`, `0x02042034`, `0x020404b0`, `0x0203f21c`, `0x0203f35c`, `0x0203f60c` | `0x02042484` |

### Confirmed condition table usage (`0x02070564`)

#### Selector / validation loop — `FUN_02042818`

Relevant instructions:

- `0x02042824`: `LDR r9, [0x02042894]` → literal = `0x02070564` (condition table base)
- `0x02042838`: `MOV r7, #0x1C` → stride
- `0x02042840`: `MUL r1, r0, r4`
- `0x0204284C`: `UMULL r0, r3, r1, r6`
- `0x02042868`: `MLA r3, r0, r7, r9` → computes `base + index * 0x1C`
- `0x0204286C`: `LDRB r3, [r3, #0x18]` → reads enable flag
- `0x02042874`: `CMP r3, #0x0`
- `0x0204287C`: `BL 0x020428a4` if enabled
- `0x02042888`: branches back to retry if no valid object was produced

Ghidra decompile summary:

```text
uVar6 = *rng_state * 0x41C64E6D + 0x6073;
uVar1 = ... ;
uVar9 = *(byte *)(0x02070564 + uVar1 * 0x1C + 0x18);
if (uVar9 != 0) {
    puVar7 = FUN_020428a4(uVar1, uVar6, ...);
}
repeat until puVar7 != 0
```

This confirms:

- RNG **is involved** in selecting the condition-table index.
- The table at `0x02070564` is indexed with stride `0x1C`.
- The flag at `+0x18` is used as a **filter**.
- Disabled entries are skipped and the function retries until an enabled entry yields a valid runtime object.

No separate probability table was identified here; the probability appears to come from the LCG math inside `FUN_02042818` rather than from a standalone ROM table.

#### Runtime object builder — `FUN_020428a4`

Relevant instructions:

- `0x020428C4`: `LDR r1, [0x02042950]` → literal = `0x02070564`
- `0x020428C8`: `MOV r0, #0x1C`
- `0x020428CC`: `MLA r4, r6, r0, r1` → `entry = base + index * 0x1C`
- `0x020428D4`: `LDRB r0, [r4, #0x18]`
- `0x020428D8`: `STRB r0, [r5, #0x14]`
- `0x020428DC`: `LDR r6, [r4, #0x0C]`
- `0x02042908`: `LDR r6, [r4, #0x04]`
- `0x02042928`: `LDR r4, [r4, #0x08]`

This function constructs a runtime condition object from the selected table entry and copies several entry fields into the new object, including the enable byte.

### Patient creation path — `FUN_0203edac`

`FUN_0203edac` is the confirmed new-patient creation function.

Relevant instructions around the descriptor write:

- `0x0203ef24`: `BL 0x0203f034`
- `0x0203ef2C`: `BL 0x0203f390`
- `0x0203ef64`: `STRB r0, [sp, #0x4]`
- `0x0203ef88`: `STRB r0, [sp, #0x5]`
- `0x0203efBC`: `STRB r0, [sp, #0x6]`
- `0x0203efCC`: `ADD r2, sp, #0x4`
- `0x0203efd4`: `BL 0x0203f874`

This shows that the patient-creation path prepares a **3-byte local descriptor** at `sp+4..6` and passes it to `FUN_0203f874`.

### Final write to patient — `FUN_0203f874`

Relevant instructions:

- `0x0203f878`: `LDRB r4, [r2, #0x0]`
- `0x0203f884`: `STRB r4, [r5, #0x4]`
- `0x0203f888`: `LDRB r7, [r2, #0x1]`
- `0x0203f894`: `STRB r7, [r5, #0x5]`
- `0x0203f898`: `LDRB r0, [r2, #0x2]`
- `0x0203f8A4`: `STRB r0, [r5, #0x6]`

This is the confirmed point where the condition descriptor bytes are copied into the patient's condition block.

### Structural summary

```text
FUN_02042818
  RNG-based selection of index
  ↓
0x02070564 + index * 0x1C
  read enable flag at +0x18
  ↓
FUN_020428a4
  build runtime condition object from table entry
  ↓
FUN_0203edac
  new-patient path prepares local 3-byte descriptor
  ↓
FUN_0203f874
  writes descriptor bytes to patient block (+4/+5/+6)
```

### Strings checked

Using `all_strings_deep.json`:

- `EYEINFECTION` = `0x020714A8`
- `MESSYFUR` = `0x020714D0`

No direct string references to `EYEINFECTION`, `MESSYFUR`, `condition`, `disease`, or `symptom` were found in the immediate literal pools around `FUN_02042818`, `FUN_0203edac`, or `FUN_0203f874`.

### Current interpretation

- `FUN_02042818` is the best confirmed candidate for the **real condition-entry selector**.
- It uses RNG and filters entries through the `0x02070564` table's enable byte.
- `FUN_0203edac` / `FUN_0203f874` are the confirmed **patient insertion / final write** path.
- A direct call edge from `FUN_0203edac` to `FUN_02042818` was not visible in the current exported call graph, so there may be an intermediate runtime manager or prebuilt object source between selection and patient insertion.

---

## Analysis Scripts

| File | Purpose |
|------|---------|
| `C:/tmp/pet_condition_analysis6.py` | FUN_0203f874 signature, generators, init function disasm |
| `C:/tmp/pet_condition_analysis7.py` | Table data: reads all pool pointers, 31-entry table, tool table, RNG constants |
| `C:/tmp/pet_monitor_conditions.lua` | DeSmuME Lua v6 — runtime condition monitor (relaxed is_patient) |
