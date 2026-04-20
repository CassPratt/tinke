#!/usr/bin/env python3
from pathlib import Path
BIN=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
if not BIN.exists():
    print('Missing bin', BIN)
    raise SystemExit(1)

LITERAL = 0x42C10
with open(BIN,'rb') as f:
    f.seek(LITERAL)
    data = f.read(4)
    if len(data)<4:
        print('Cannot read literal at', hex(LITERAL))
        raise SystemExit(1)
    va_table = int.from_bytes(data,'little')
    file_off = va_table - 0x02000000
    print(f'Read literal at file 0x{LITERAL:08X}: {data.hex().upper()} -> VA 0x{va_table:08X} -> file offset 0x{file_off:08X}')
    # read 26 entries * 8 bytes = 208
    f.seek(file_off)
    tbl = f.read(26*8)
    if len(tbl) < 26*8:
        print('Short read of breed index table')
    print('\nEntries (breed_index | entry_file_offset | dword0 | dword1):')
    for i in range(26):
        off = file_off + i*8
        e = tbl[i*8:(i+1)*8]
        if len(e) < 8:
            print(f'{i:02d} | 0x{off:08X} | <incomplete>')
            continue
        d0 = int.from_bytes(e[0:4],'little')
        d1 = int.from_bytes(e[4:8],'little')
        print(f'{i:02d} | 0x{off:08X} | 0x{d0:08X} ({d0}) | 0x{d1:08X} ({d1})')
