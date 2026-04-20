from pathlib import Path
p=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
if not p.exists():
    print('MISSING')
    raise SystemExit(1)

data=p.read_bytes()
pat1=bytes([0x8C,0xFD,0x06,0x02])
pat2=bytes([0x38,0xFD,0x06,0x02])
found1=[i for i in range(len(data)) if data.startswith(pat1,i)]
found2=[i for i in range(len(data)) if data.startswith(pat2,i)]
print('Pattern 0x0206FD8C occurrences (count={}):'.format(len(found1)))
for o in found1:
    print(f'  0x{o:08X} ({o})')
print('\nPattern 0x0206FD38 occurrences (count={}):'.format(len(found2)))
for o in found2:
    print(f'  0x{o:08X} ({o})')
