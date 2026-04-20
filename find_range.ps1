# Scan a range of the Pet Hospitals ARM9 binary for specific instruction types.
# Filters: ADD/MOV with small immediates, STRB to low offsets, BL calls.
# Useful for finding breed/color range setup instructions near injection sites.
# Usage: adjust $start/$end, then: powershell.exe -File find_range.ps1

$romPath = 'C:\Users\cass_\OneDrive\Desktop\DeSmuME\ROMs\Let' + [char]39 + 's Play Pet Hospitals.nds'
$rom = [System.IO.File]::ReadAllBytes($romPath)
$arm9Base = 0x4000

# ---- adjust range here ----
$start = 0x41000
$end   = 0x424FF
# ---------------------------

Write-Host "=== Scanning arm9:0x$($start.ToString('X5'))–0x$($end.ToString('X5')) for notable instructions ==="

for ($off = $start; $off -lt $end; $off += 4) {
    $r = $arm9Base + $off
    $b0 = $rom[$r]; $b1 = $rom[$r+1]; $b2 = $rom[$r+2]; $b3 = $rom[$r+3]
    $note = ''

    # ADD rd, rn, #imm (small imm only)
    if ($b3 -eq 0xE2 -and ($b2 -band 0xF0) -eq 0x80) {
        $rd = ($b1 -shr 4) -band 0xF; $rn = $b2 -band 0xF; $imm = $b0
        if ($imm -le 10) { $note = "ADD r$rd, r$rn, #$imm" }
    }
    # MOV rd, #imm (small imm only)
    elseif ($b3 -eq 0xE3 -and ($b2 -band 0xF0) -eq 0xA0) {
        $rd = ($b1 -shr 4) -band 0xF; $imm = $b0
        if ($imm -le 10) { $note = "MOV r$rd, #$imm" }
    }
    # STRB to low offset
    elseif ($b3 -eq 0xE5 -and ($b2 -band 0xF0) -eq 0xC0) {
        $rd = ($b1 -shr 4) -band 0xF; $rn = $b2 -band 0xF
        $imm = (($b1 -band 0xF) -shl 8) + $b0
        if ($imm -le 0x20) { $note = "STRB r$rd,[r$rn,#0x$($imm.ToString('X'))]" }
    }
    # BL
    elseif ($b3 -eq 0xEB) {
        $rawOff = $b0 + ($b1 -shl 8) + ($b2 -shl 16)
        if ($rawOff -ge 0x800000) { $rawOff = $rawOff - 0x1000000 }
        $target = $off + 8 + $rawOff * 4 + 0x02000000
        $note = "BL 0x$($target.ToString('X8'))"
    }

    if ($note -ne '') {
        Write-Host ("  arm9:0x{0}  {1:X2}{2:X2}{3:X2}{4:X2}  {5}" -f $off.ToString("X5"), $b3, $b2, $b1, $b0, $note)
    }
}
