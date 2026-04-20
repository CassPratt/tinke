#!/usr/bin/env python3
from pathlib import Path

BIN = Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
if not BIN.exists():
    print('ERROR: binary not found:', BIN)
    raise SystemExit(1)

# literal pool file offset to read the VA from
LITERAL_OFFSET = 0x42564

with open(BIN, 'rb') as f:
    f.seek(LITERAL_OFFSET)
    data = f.read(4)
    if len(data) < 4:
        print(f'ERROR: cannot read 4 bytes at 0x{LITERAL_OFFSET:08X}')
        raise SystemExit(1)
    va = int.from_bytes(data, 'little')
    print(f'Read dword at file offset 0x{LITERAL_OFFSET:08X}: {data.hex().upper()} -> VA 0x{va:08X}')

    file_off = va - 0x02000000
    print(f'Interpreted table VA -> file offset: 0x{file_off:08X} ({file_off})')

    # read 6 dwords (24 bytes) from the computed file offset
    f.seek(file_off)
    tbl = f.read(24)
    if len(tbl) < 24:
        print(f'ERROR: cannot read 24 bytes at 0x{file_off:08X}')
        raise SystemExit(1)

    print('\n6 values at that file offset:')
    for i in range(0, 24, 4):
        chunk = tbl[i:i+4]
        val = int.from_bytes(chunk, 'little')
        off = file_off + i
        print(f'0x{off:08X}: {chunk.hex().upper()} -> {val} (0x{val:08X})')
