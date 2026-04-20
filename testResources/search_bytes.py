from pathlib import Path
import sys

if len(sys.argv) < 2:
    print('Usage: python search_bytes.py <hexpattern> [file]')
    print('Example: python search_bytes.py B4 02 07 02 file.bin')
    raise SystemExit(1)

hexparts = []
fileArg = None
for a in sys.argv[1:]:
    if all(c in '0123456789ABCDEFabcdef' for c in a) and len(a) <= 2:
        hexparts.append(a)
    else:
        fileArg = a

if not hexparts:
    print('No hex bytes provided')
    raise SystemExit(1)

pattern = bytes(int(x,16) for x in hexparts)
if fileArg is None:
    p = Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
else:
    p = Path(fileArg)

if not p.exists():
    print('File not found:', p)
    raise SystemExit(1)

data = p.read_bytes()
found = []
idx = 0
while True:
    i = data.find(pattern, idx)
    if i == -1:
        break
    found.append(i)
    idx = i+1

if not found:
    print('No occurrences found')
else:
    for o in found:
        print(f'0x{o:X} ({o})')
    print('Total:', len(found))
