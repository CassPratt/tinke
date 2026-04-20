import json
from pathlib import Path
p=Path('Plugins/PETHOSPITALS/docs/GhidraDumps/all_strings_deep.json')
if not p.exists():
    print('MISSING')
    raise SystemExit(1)
arr=json.loads(p.read_text())
res=[]
for e in arr:
    s=e.get('string','')
    if 'harlequin' in s.lower():
        addr=e.get('address')
        res.append((int(addr,16), addr, s, e))
res.sort()
if not res:
    print('No matches')
else:
    for v in res:
        off,vaddr,s,e=v
        # gather references? JSON may have references field; check
        refs=e.get('refs') or e.get('references') or []
        print(f"{vaddr} | {s}")
        if refs:
            for r in refs:
                print('  ref:', r)
        else:
            print('  refs: (none recorded)')
        print()
