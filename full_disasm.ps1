# Full ARM32 disassembly of a range in the Pet Hospitals ARM9 binary.
# Usage: adjust $start and $end (arm9 offsets), then run with:
#   powershell.exe -File full_disasm.ps1
#
# Note: reads bytes individually (not as uint32) to avoid PowerShell sign issues.

$romPath = 'C:\Users\cass_\OneDrive\Desktop\DeSmuME\ROMs\Let' + [char]39 + 's Play Pet Hospitals.nds'
$rom = [System.IO.File]::ReadAllBytes($romPath)
$arm9Base = 0x4000

function Decode-ARM32 {
    param($off, $b0, $b1, $b2, $b3)
    $note = ''
    $cond = ($b3 -shr 4) -band 0xF
    $condStr = @('EQ','NE','CS','CC','MI','PL','VS','VC','HI','LS','GE','LT','GT','LE','','')[$cond]
    if ($cond -eq 14) { $condStr = '' }

    # STMDB/STMFD (push)
    if ($b3 -eq 0xE9 -and $b2 -eq 0x2D) {
        $note = "PUSH/STMDB"
    }
    # LDMIA/LDMFD (pop)
    elseif ($b3 -eq 0xE8 -and $b2 -eq 0xBD) {
        $note = "POP/LDMIA"
    }
    # BL
    elseif ($b3 -eq 0xEB) {
        $rawOff = $b0 + ($b1 -shl 8) + ($b2 -shl 16)
        if ($rawOff -ge 0x800000) { $rawOff = $rawOff - 0x1000000 }
        $target = $off + 8 + $rawOff * 4 + 0x02000000
        $note = "BL 0x$($target.ToString('X8'))"
    }
    # B (unconditional)
    elseif ($b3 -eq 0xEA) {
        $rawOff = $b0 + ($b1 -shl 8) + ($b2 -shl 16)
        if ($rawOff -ge 0x800000) { $rawOff = $rawOff - 0x1000000 }
        $target = $off + 8 + $rawOff * 4 + 0x02000000
        $note = "B 0x$($target.ToString('X8'))"
    }
    # Bcc (conditional branch)
    elseif (($b3 -band 0x0E) -eq 0x0A -and $cond -ne 14 -and $cond -ne 15) {
        $rawOff = $b0 + ($b1 -shl 8) + ($b2 -shl 16)
        if ($rawOff -ge 0x800000) { $rawOff = $rawOff - 0x1000000 }
        $target = $off + 8 + $rawOff * 4 + 0x02000000
        $note = "B$condStr 0x$($target.ToString('X8'))"
    }
    # ADD rd, rn, #imm8
    elseif ($b3 -eq 0xE2 -and ($b2 -band 0xF0) -eq 0x80) {
        $rd = ($b1 -shr 4) -band 0xF; $rn = $b2 -band 0xF; $imm = $b0
        $note = "ADD r$rd, r$rn, #$imm"
    }
    # SUB rd, rn, #imm8
    elseif ($b3 -eq 0xE2 -and ($b2 -band 0xF0) -eq 0x40) {
        $rd = ($b1 -shr 4) -band 0xF; $rn = $b2 -band 0xF; $imm = $b0
        $note = "SUB r$rd, r$rn, #$imm"
    }
    # MOV rd, #imm
    elseif ($b3 -eq 0xE3 -and ($b2 -band 0xF0) -eq 0xA0) {
        $rd = ($b1 -shr 4) -band 0xF; $imm = $b0
        $note = "MOV r$rd, #0x$($imm.ToString('X'))"
    }
    # MOV rd, rm (with optional shift)
    elseif ($b3 -eq 0xE1 -and $b2 -eq 0xA0) {
        $rd = ($b1 -shr 4) -band 0xF; $rm = $b0 -band 0xF
        $note = "MOV r$rd, r$rm"
    }
    # CMP rn, #imm
    elseif ($b3 -eq 0xE3 -and ($b2 -band 0xF0) -eq 0x50) {
        $rn = $b2 -band 0xF; $imm = $b0
        $note = "CMP r$rn, #$imm"
    }
    # CMP rn, rm
    elseif ($b3 -eq 0xE1 -and ($b2 -band 0xF0) -eq 0x50) {
        $rn = $b2 -band 0xF; $rm = $b0 -band 0xF
        $note = "CMP r$rn, r$rm"
    }
    # STRB
    elseif ($b3 -eq 0xE5 -and ($b2 -band 0xF0) -eq 0xC0) {
        $rd = ($b1 -shr 4) -band 0xF; $rn = $b2 -band 0xF
        $imm = (($b1 -band 0xF) -shl 8) + $b0
        $note = "STRB r$rd,[r$rn,#0x$($imm.ToString('X'))]"
    }
    # LDR
    elseif ($b3 -eq 0xE5 -and ($b2 -band 0xF0) -eq 0x90) {
        $rd = ($b1 -shr 4) -band 0xF; $rn = $b2 -band 0xF
        $imm = (($b1 -band 0xF) -shl 8) + $b0
        $note = "LDR r$rd,[r$rn,#0x$($imm.ToString('X'))]"
    }
    # STR
    elseif ($b3 -eq 0xE5 -and ($b2 -band 0xF0) -eq 0x80) {
        $rd = ($b1 -shr 4) -band 0xF; $rn = $b2 -band 0xF
        $imm = (($b1 -band 0xF) -shl 8) + $b0
        $note = "STR r$rd,[r$rn,#0x$($imm.ToString('X'))]"
    }
    # AND rd, rn, #imm
    elseif ($b3 -eq 0xE2 -and ($b2 -band 0xF0) -eq 0x00) {
        $rd = ($b1 -shr 4) -band 0xF; $rn = $b2 -band 0xF; $imm = $b0
        $note = "AND r$rd, r$rn, #0x$($imm.ToString('X'))"
    }
    # SMULL
    elseif ($b3 -eq 0xE0 -and ($b2 -band 0xF0) -eq 0xC0) {
        $note = "SMULL"
    }

    return $note
}

# ---- adjust range here ----
$start = 0x42000
$end   = 0x422F8
# ---------------------------

Write-Host "=== ARM32 disasm arm9:0x$($start.ToString('X5'))–0x$($end.ToString('X5')) ==="
for ($off = $start; $off -lt $end; $off += 4) {
    $r = $arm9Base + $off
    $b0 = $rom[$r]; $b1 = $rom[$r+1]; $b2 = $rom[$r+2]; $b3 = $rom[$r+3]
    $note = Decode-ARM32 $off $b0 $b1 $b2 $b3
    Write-Host ("  arm9:0x{0}  {1:X2}{2:X2}{3:X2}{4:X2}  {5}" -f $off.ToString("X5"), $b3, $b2, $b1, $b0, $note)
}
