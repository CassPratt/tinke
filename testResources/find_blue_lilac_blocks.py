#!/usr/bin/env python3
from pathlib import Path
import json

BIN=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
JS=Path('Plugins/PETHOSPITALS/docs/GhidraDumps/all_strings_deep.json')
if not BIN.exists():
    print('ERROR: binary not found', BIN)
    raise SystemExit(1)
if not JS.exists():
    print('ERROR: json not found', JS)
    raise SystemExit(1)

arr=json.loads(JS.read_text())
map_addr={e.get('address').upper():e.get('string') for e in arr if e.get('address')}

data=BIN.read_bytes()
pat_blue=bytes([0xB4,0xD3,0x06,0x02])
pat_lilac=bytes([0xCC,0xD3,0x06,0x02])

found_blocks=[]
for off in range(0, len(data)-32+1):
    block=data[off:off+32]
    if pat_blue in block and pat_lilac in block:
        found_blocks.append(off)

if not found_blocks:
    print('No 32-byte blocks contain both patterns')
    raise SystemExit(0)

for off in found_blocks:
    print(f'Block at 0x{off:X} ({off})')
    block=data[off:off+32]
    # Show raw bytes rows of 16
    for i in range(0,32,16):
        row=block[i:i+16]
        print(f'  0x{off+i:08X}: ' + ' '.join(f'{b:02X}' for b in row))
    print('  Resolved 4-byte words:')
    for i in range(0,32,4):
        w=block[i:i+4]
        hexw=' '.join(f'{b:02X}' for b in w)
        va=int.from_bytes(w,'little')
        key=f'{va:08X}'
        s=map_addr.get(key,'?')
        print(f'    0x{off+i:08X} | {hexw} -> {s} (VA 0x{key})')
    print()

print('Done. Total blocks:', len(found_blocks))
