from pathlib import Path
import sys
if len(sys.argv) > 1:
    p = Path(sys.argv[1])
else:
    p = Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
if not p.exists():
    print('ERROR: file not found:', p)
    raise SystemExit(1)
with open(p,'rb') as f:
    data=f.read()
checks=[(0x61EAC,4,'54 18 07 02','PATCH5 — slots 16/17 → EarDrop ptr'),
        (0x6FD14,1,'17','key2 slot 26 = 0x17'),
        (0x6FD15,2,'07 07','PATCH6 — b1=b2=0x07'),
        (0x61ED0,4,'54 18 07 02','slot 26 literal pool → EarDrop'),
        (0x70630,4,'FC 16 07 02','fila 7 +0x08 → EarDrop resource ptr'),
        (0x70640,1,'01','fila 7 +0x18 = enabled')]
for off,l,exp,desc in checks:
    if off + l > len(data):
        fh = '<EOF>'
    else:
        found=data[off:off+l]
        fh=' '.join(f"{b:02X}" for b in found)
    print(f"Offset 0x{off:X}: expected {exp} | found {fh} -- {desc}")
