import json
from pathlib import Path
p=Path('Plugins/PETHOSPITALS/docs/GhidraDumps/globals_full.json')
if not p.exists():
    print('MISSING')
    raise SystemExit(1)
arr=json.loads(p.read_text())
res=[]
for e in arr:
    s=e.get('string') or e.get('name') or ''
    if s and 'harlequin' in s.lower():
        res.append(e)
res.sort(key=lambda x: int(x.get('address','0'),16))
for e in res:
    addr=e.get('address')
    s=e.get('string') or e.get('name') or ''
    refs=e.get('refs') or e.get('references') or []
    print(f"{addr} | {s}")
    if refs:
        for r in refs:
            print('  ref:', r)
    else:
        print('  refs: (none recorded)')
    print()
