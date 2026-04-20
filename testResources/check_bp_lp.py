from pathlib import Path
p=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
data=p.read_bytes()
pat_bp=bytes([0x84,0xDA,0x06,0x02])
pat_lp=bytes([0xB0,0xDE,0x06,0x02])
found_bp=[i for i in range(len(data)) if data.startswith(pat_bp,i)]
found_lp=[i for i in range(len(data)) if data.startswith(pat_lp,i)]
print('BluePoint occurrences (count={})'.format(len(found_bp)))
for o in found_bp:
    print(f'  0x{o:X} ({o})')
print('\nLilacPoint occurrences (count={})'.format(len(found_lp)))
for o in found_lp:
    print(f'  0x{o:X} ({o})')
# check co-occurrence in same 32-byte window
pairs=[]
for b in found_bp:
    for l in found_lp:
        if abs(b-l) <= 32:
            pairs.append((b,l))
print('\nPairs within 32 bytes (BP, LP): count', len(pairs))
for b,l in pairs:
    print(f'  BP 0x{b:X} , LP 0x{l:X} , order: {"BP before LP" if b<l else "LP before BP"}, distance {abs(b-l)}')
# Check whether every BP has an LP nearby and viceversa
bp_with_lp = set(b for b,l in pairs)
lp_with_bp = set(l for b,l in pairs)
print('\nEvery BP has an LP within 32 bytes?:', set(found_bp) <= bp_with_lp)
print('Every LP has a BP within 32 bytes?:', set(found_lp) <= lp_with_bp)
# Check ordering consistency when both present in same window
consistent = True
for b,l in pairs:
    if not (b<l):
        consistent=False
        break
print('\nWhen both present in same window, BP always before LP?:', consistent)
