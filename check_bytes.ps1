# Dump raw bytes at specific arm9 offsets in the Pet Hospitals ROM.
# Usage: adjust $sites list, then: powershell.exe -File check_bytes.ps1

$romPath = 'C:\Users\cass_\OneDrive\Desktop\DeSmuME\ROMs\Let' + [char]39 + 's Play Pet Hospitals.nds'
$rom = [System.IO.File]::ReadAllBytes($romPath)
$arm9Base = 0x4000

$sites = @(
    @{off=0x36224; len=8;  desc="PATCH1 (texture fix)"},
    @{off=0x6DA7E; len=4;  desc="PATCH2 (guineaPig)"},
    @{off=0x402A4; len=4;  desc="PATCH3 breed range (ADD r5, r0, #1 = E2805001)"},
    @{off=0x40328; len=4;  desc="PATCH3 color range (ADD r6, r0, #1 = E2806001)"},
    @{off=0x403B8; len=12; desc="first creator STRB block (species/breed/color)"},
    @{off=0x422E8; len=12; desc="second creator STRB block (species/breed/color)"}
)

foreach ($s in $sites) {
    Write-Host "=== arm9:0x$($s.off.ToString('X5')) — $($s.desc) ==="
    $line = ''
    for ($i = 0; $i -lt $s.len; $i++) {
        $line += ("{0:X2} " -f $rom[$arm9Base + $s.off + $i])
    }
    Write-Host "  $line"
}
