from pathlib import Path
BIN=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
start=0x6FC40
length=256
if not BIN.exists():
    print('MISSING BIN',BIN)
    raise SystemExit(1)
with open(BIN,'rb') as f:
    f.seek(start)
    data=f.read(length)
print(f"Read {len(data)} bytes from 0x{start:X}\n")
for i in range(0,len(data),8):
    idx=i//8
    off=start+i
    e=data[i:i+8]
    hexs=' '.join(f'{b:02X}' for b in e)
    k2=e[4]
    b1=e[5]
    b2=e[6]
    flags=e[7]
    print(f"{idx:02d} | 0x{off:08X} | {hexs} | key2=0x{k2:02X} b1=0x{b1:02X} b2=0x{b2:02X} flags=0x{flags:02X}")
