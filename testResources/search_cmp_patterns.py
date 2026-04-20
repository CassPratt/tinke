from pathlib import Path
p=Path('Plugins/PETHOSPITALS/testResources/arm9_EyeDropPatch.bin')
if not p.exists():
    print('ERROR: file not found', p)
    raise SystemExit(1)
data=p.read_bytes()
start=0x40000
end=0x70000
patterns={
    b'\x1F\x00\x51\xE3':'1F 00 51 E3',
    b'\x1F\x00\x5E\xE3':'1F 00 5E E3',
    b'\x1F\x00\x50\xE3':'1F 00 50 E3'
}
found_any=False
for pat,desc in patterns.items():
    i=start
    while True:
        idx=data.find(pat,i,end)
        if idx==-1:
            break
        print(f"{desc} at 0x{idx:X} ({idx})")
        found_any=True
        i=idx+1
if not found_any:
    print('No matches found in range')
else:
    # summary
    pass
