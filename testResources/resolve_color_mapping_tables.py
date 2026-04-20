#!/usr/bin/env python3
from pathlib import Path
import json

BIN=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
JS=Path('Plugins/PETHOSPITALS/docs/GhidraDumps/all_strings_deep.json')
if not BIN.exists():
    print('ERROR: binary not found', BIN); raise SystemExit(1)
if not JS.exists():
    print('ERROR: json not found', JS); raise SystemExit(1)
arr=json.loads(JS.read_text())
map_addr={e.get('address').upper():e.get('string') for e in arr if e.get('address')}


def dump_and_resolve(start, length=128):
    print(f"\nDump {length} bytes from 0x{start:X}:")
    with open(BIN,'rb') as f:
        f.seek(start)
        data=f.read(length)
    for i in range(0, len(data), 16):
        row=data[i:i+16]
        addr=start+i
        print(f'0x{addr:08X}: ' + ' '.join(f'{b:02X}' for b in row))
    print('\nResolved 4-byte words:')
    for i in range(0, len(data), 4):
        addr=start+i
        w=data[i:i+4]
        hexw=' '.join(f'{b:02X}' for b in w)
        va=int.from_bytes(w,'little')
        key=f'{va:08X}'
        s=map_addr.get(key,'?')
        print(f'0x{addr:08X} | {hexw} | {s}')


if __name__=='__main__':
    dump_and_resolve(0x6FC00,128)
    dump_and_resolve(0x6FC10,128)
