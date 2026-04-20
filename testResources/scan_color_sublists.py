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

start=0x6FB08
length=192
with open(BIN,'rb') as f:
    f.seek(start)
    data=f.read(length)

# print rows
for i in range(0, len(data), 16):
    row=data[i:i+16]
    addr=start+i
    print(f'0x{addr:08X}: ' + ' '.join(f'{b:02X}' for b in row))

print('\nResolved 4-byte words (little-endian):')
for i in range(0, len(data), 4):
    addr=start+i
    w=data[i:i+4]
    hexw=' '.join(f'{b:02X}' for b in w)
    va=int.from_bytes(w,'little')
    key=f'{va:08X}'
    s=map_addr.get(key,'?')
    print(f'0x{addr:08X} | {hexw} | {s}')

# Additionally, try to detect contiguous sequences of pointers (possible sublists)
print('\nDetect contiguous pointer runs (>2 words) in the region:')
for i in range(0, len(data)-8, 4):
    run_start = i
    run = []
    for j in range(i, len(data), 4):
        w=data[j:j+4]
        if len(w)<4: break
        va = int.from_bytes(w,'little')
        # Heuristic: valid pointer into VA range 0x02060000..0x02074000
        if 0x02060000 <= va <= 0x02074000:
            run.append((j,va))
        else:
            break
    if len(run) >= 3:
        print(f'Possible pointer run at offset 0x{start+run_start:08X}, length {len(run)}')
        for off,va in run:
            k=f'{va:08X}'
            print(f'  0x{start+off:08X}: -> {k} = {map_addr.get(k,"?")}')

print('\nDone')
