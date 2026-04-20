import json
from pathlib import Path
js=Path('Plugins/PETHOSPITALS/docs/GhidraDumps/all_strings_deep.json')
binpath=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
if not js.exists():
    print('ERROR: JSON not found', js); raise SystemExit(1)
arr=json.loads(js.read_text())
keys=['blue','lilac']
res=[]
for e in arr:
    s=e.get('string','')
    if any(k in s.lower() for k in keys):
        addr=e.get('address')
        refs=e.get('refs') or e.get('references') or []
        res.append((int(addr,16), addr, s, refs))
res.sort()
print('Matches in all_strings_deep.json:')
if not res:
    print('  (none)')
else:
    for off,addr,s,refs in res:
        print(f"- {addr} | {s}")
        if refs:
            for r in refs:
                print('    ref:', r)
        else:
            print('    refs: (none recorded)')

# binary search
if not binpath.exists():
    print('\nBinary not found:', binpath)
    raise SystemExit(1)
print('\nSearching binary for pointers:')
data=binpath.read_bytes()
pat_blue=bytes([0xB4,0xD3,0x06,0x02])
pat_lilac=bytes([0xCC,0xD3,0x06,0x02])
found_blue=[]
found_lilac=[]
idx=0
while True:
    i=data.find(pat_blue, idx)
    if i==-1: break
    found_blue.append(i)
    idx=i+1
idx=0
while True:
    i=data.find(pat_lilac, idx)
    if i==-1: break
    found_lilac.append(i)
    idx=i+1
print('\nBlue (B4 D3 06 02) occurrences:')
if not found_blue:
    print('  (none)')
else:
    for o in found_blue:
        print(f'  0x{o:X} ({o})')
print('\nLilac (CC D3 06 02) occurrences:')
if not found_lilac:
    print('  (none)')
else:
    for o in found_lilac:
        print(f'  0x{o:X} ({o})')

# proximity
print('\nProximity (within 64 bytes):')
paired=False
for b in found_blue:
    for l in found_lilac:
        if abs(b-l) <= 64:
            print(f'  Blue at 0x{b:X} is within {abs(b-l)} bytes of Lilac at 0x{l:X}')
            paired=True
if not paired:
    print('  No occurrences are within 64 bytes of each other.')
