from pathlib import Path
BIN=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
if not BIN.exists():
    print('missing bin'); raise SystemExit(1)
with open(BIN,'rb') as f:
    f.seek(0x42560)
    b=f.read(4)
    if len(b)<4:
        print('short read'); raise SystemExit(1)
    va=int.from_bytes(b,'little')
    print(f'VA at 0x42560 = 0x{va:08X}')
    file_off=va-0x02000000
    print(f'file offset = 0x{file_off:08X}')
    f.seek(file_off)
    data=f.read(24)
    for i in range(0,24,4):
        w=data[i:i+4]
        val=int.from_bytes(w,'little')
        print(f'0x{file_off+i:08X}: {w.hex().upper()} -> {val} (0x{val:08X})')
