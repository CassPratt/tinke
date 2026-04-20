# Reverse Session — EYEINFECTION / EarDrop

Objective
---------
Fix EYEINFECTION so the EarDrop tool is assigned and usable from the in-game treatment UI.

Root cause
----------
The game's tool lookup uses a (ptr, key2) pair to match a runtime condition to an entry in the tool array (lookup implemented in `FUN_02042AD4`). EYEINFECTION requires runtime key2 = `0x17`. In the tested ROM no tool array slot had the matching key2 and treatment bytes: slot 27 had `key2 = 0x16` and `b1=b2=0xFF` (unused). Ear-wax (CottonSwab) used `key2 = 0x15` with a different pointer; that pairing must be preserved.

Investigation path (concise)
----------------------------
- Confirmed the (ptr, key2) lookup mechanism by reviewing Ghidra disassembly and using Lua `memory.registerexec` hooks on `FUN_02042AD4`.
- Located the stack pointer used for the lookup (SP address `0x027E3F64`) by scanning memory while the function wrote `STR r0, [sp]`.
- Observed that when the pointer matched but no key2 matched, the lookup returned no treatment — this isolated the problem to the `key2` field.
- Brute-forced `key2` values `0x00`–`0x1F` by modifying the tool array in-memory and re-running the lookup; detected success when the runtime matched the injected `key2` value.
- Discovered that ear-wax uses a different pointer region (`0x020702CC`) and key2 `0x15` — confirmed this must remain unchanged.
- Determined slot 27 is adjacent to relevant literal pools and holds `key2=0x16, b1=b2=0xFF` (unused). This slot is a safe candidate for retargeting.
- Confirmed that appending bytes at the end of ARM9 does not create usable new runtime slots because the init code uses zero-terminated loops; therefore the fix must modify existing table entries.

Final fix
---------
Apply an EyeDrop/EarDrop bundle which:
- Retargets a condition row to the EarDrop resource and enables it.
- Sets the state-machine returned tool bytes so the caller receives row index `7` as the treatment id.
- Modifies slot 27 in-place so `key2 = 0x17` and `b1=b2=0x07`, making the slot match the runtime lookup for EYEINFECTION.

Concretely (ROM writes applied):
- `0x61EAC`: `CC 17 07 02` → `54 18 07 02` (slots 16/17 literal pool → EarDrop ptr `0x02071854`)
- `0x70630`: `38 D3 06 02` → `FC 16 07 02` (condition row 7 +0x08 → EarDrop ptr)
- `0x70640`: `00` → `01` (enable condition row 7)
- `0x6FD15..0x6FD16`: `FF FF` → `07 07` (state-machine b1=b2 → 0x07)
- `0x6FD1C`: `16` → `17` (slot 27 key2 = 0x17)
- `0x6FD1D..0x6FD1E`: `FF FF` → `07 07` (slot 27 b1=b2=0x07)

The EyeDrop bundle preserves slot 26's `key2 = 0x15` and its literal pool pointer so the ear-wax flow remains functional.

Key Lua techniques used
-----------------------
- `memory.registerexec` to intercept function entry/exit points and to force values on the stack (STR target) so the lookup ran with controlled inputs.
- Direct writes to the game's memory region containing the tool array to brute-force `key2` values and detect a match.
- Registering execution hooks on the match/no-match paths to detect a successful treatment match during brute force.

Known side-effect
-----------------
Treating "Ear with wax" may briefly show the intermediate state "clean wound" during the treatment sequence. This is cosmetic and not functionally harmful.



*Session notes and binary offsets are recorded in the project docs. For full context see `Patches.md`.*
