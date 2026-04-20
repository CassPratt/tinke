#!/usr/bin/env python3
from pathlib import Path
import json

BIN=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
JS=Path('Plugins/PETHOSPITALS/docs/GhidraDumps/all_strings_deep.json')
if not BIN.exists() or not JS.exists():
    print('Missing files')
    raise SystemExit(1)
arr=json.loads(JS.read_text())
map_addr={e.get('address').upper():e.get('string') for e in arr if e.get('address')}

start=0x6FDBC
with open(BIN,'rb') as f:
    f.seek(start)
    data=f.read(16)
print(f'Read 16 bytes at 0x{start:08X}: ' + ' '.join(f'{b:02X}' for b in data))
for i in range(0,16,4):
    w=data[i:i+4]
    va=int.from_bytes(w,'little')
    key=f'{va:08X}'
    s=map_addr.get(key,'?')
    print(f'0x{start+i:08X} | {" ".join(f"{b:02X}" for b in w)} | VA=0x{key} -> {s}')
