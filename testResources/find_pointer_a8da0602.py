from pathlib import Path
p=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
data=p.read_bytes()
pat=bytes([0xA8,0xDA,0x06,0x02])
found=[]
idx=0
while True:
    i=data.find(pat, idx)
    if i==-1: break
    found.append(i)
    idx=i+1
if not found:
    print('No occurrences found')
else:
    for o in found:
        print(f'0x{o:X} ({o})')
    print('Total:', len(found))
