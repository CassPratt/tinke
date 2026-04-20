from pathlib import Path
import sys
p=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
if len(sys.argv)>1:
    p=Path(sys.argv[1])
if not p.exists():
    print('ERROR: file not found', p)
    sys.exit(1)
data=p.read_bytes()
pattern=bytes([0xB4,0x02,0x07,0x02,0x15])
found=[]
start=0
while True:
    idx=data.find(pattern,start)
    if idx==-1:
        break
    found.append(idx)
    start=idx+1
if not found:
    print('No occurrences found')
else:
    for o in found:
        print('0x{0:X} ({0})'.format(o))
    print('Total: {}'.format(len(found)))
