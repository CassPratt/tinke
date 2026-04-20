from pathlib import Path
import json
js=Path('Plugins/PETHOSPITALS/docs/GhidraDumps/all_strings_deep.json')
binf=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
if not js.exists():
    print('JSON missing', js); raise SystemExit(1)
if not binf.exists():
    print('BIN missing', binf); raise SystemExit(1)
arr=json.loads(js.read_text())
addrs=['0206D384','0206D3C4','0206D3F4','0206D464','0206D66C']
print('Lookup strings for addresses:')
for a in addrs:
    found=[e for e in arr if e.get('address','').upper()==a]
    if found:
        for e in found:
            print(f"{a} => {e.get('string')}")
    else:
        print(f"{a} => (not found)")


def dump_bytes(off, length=32):
    with open(binf,'rb') as f:
        f.seek(off)
        data=f.read(length)
    for i in range(0, len(data), 16):
        row=data[i:i+16]
        addr=off+i
        print(f'0x{addr:08X}: ' + ' '.join(f'{b:02X}' for b in row))

print('\n32 bytes @ 0x6FA00:')
dump_bytes(0x6FA00,32)
print('\n32 bytes @ 0x6FB00:')
dump_bytes(0x6FB00,32)
