# Read ~80 bytes around FUN_0204102c start to see the full CMP chain
# FUN_0204102c VA = 0x0204102c, arm9 offset = 0x4102c, ROM offset = 0x4102c + 0x4000 = 0x4502c
$rom = [System.IO.File]::ReadAllBytes('C:\Users\cass_\OneDrive\Desktop\DeSmuME\ROMs\Let''s Play Pet Hospitals.nds')

function Decode-ArmCmp($b0, $b1, $b2, $b3) {
    # ARM32 LE: bytes are [imm8, rotation_nibble | Rd_high, opcode | Rn, condition]
    # For CMP: cond=E3, opcode byte = 0x51, imm encoded in b0,b1
    if ($b3 -ne 0xE3 -or $b2 -ne 0x51) { return $null }
    $rot = ($b1 -band 0x0F)
    $imm = $b0
    $shift = ($rot * 2) % 32
    # ror by shift = shl by (32-shift) for the significant bits
    if ($shift -eq 0) { $val = $imm }
    else { $val = (([int]$imm) -shl (32 - $shift)) -bor (([int]$imm) -shr $shift) }
    $val = $val -band 0xFFFFFFFF
    return $val
}

$start = 0x4502C  # ROM offset of FUN_0204102c start
$end   = 0x451D0  # ~360 bytes to capture the full chain

Write-Host "Bytes from ROM:$($start.ToString('X5')) to ROM:$($end.ToString('X5'))"
Write-Host ""

for ($off = $start; $off -lt $end; $off += 4) {
    $b0=$rom[$off]; $b1=$rom[$off+1]; $b2=$rom[$off+2]; $b3=$rom[$off+3]
    $hex = '{0:X2} {1:X2} {2:X2} {3:X2}' -f $b0,$b1,$b2,$b3
    $arm9off = $off - 0x4000
    $va = 0x02000000 + $arm9off

    # Identify CMP r1, #N
    if ($b3 -eq 0xE3 -and $b2 -eq 0x51) {
        $val = Decode-ArmCmp $b0 $b1 $b2 $b3
        Write-Host ('  0x{0:X5} (VA:{1:X8})  {2}  <- CMP r1, #{3}' -f $off,$va,$hex,$val) -ForegroundColor Yellow
    }
    # Identify MOV r0, #N (return value)
    elseif ($b3 -eq 0xE3 -and $b2 -eq 0xA0) {
        $val = Decode-ArmCmp $b0 $b1 $b2 $b3  # reuse decoder, same encoding
        Write-Host ('  0x{0:X5} (VA:{1:X8})  {2}  <- MOV-like / data' -f $off,$va,$hex)
    }
    # BX lr = E1 2F FF 1E
    elseif ($b3 -eq 0xE1 -and $b2 -eq 0x2F -and $b1 -eq 0xFF -and $b0 -eq 0x1E) {
        Write-Host ('  0x{0:X5} (VA:{1:X8})  {2}  <- BX lr (return)' -f $off,$va,$hex) -ForegroundColor Green
    }
    # Branches: BGE=EA,EB; BLT=BA,BB; BGT=CA,CB; BLE=DA,DB; BNE=1A,1B; BEQ=0A,0B
    elseif ($b3 -in @(0xBA,0xBB,0xCA,0xCB,0xDA,0xDB,0xAA,0xAB,0xCA,0x1A,0x1B,0x0A,0x0B)) {
        $cond = switch($b3) {
            0xBA {'BLT'} 0xBB {'BLT'} 0xCA {'BGT'} 0xCB {'BGT'}
            0xDA {'BLE'} 0xDB {'BLE'} 0xAA {'BGE'} 0xAB {'BGE'}
            0x1A {'BNE'} 0x1B {'BNE'} 0x0A {'BEQ'} 0x0B {'BEQ'}
            default {'B??'}
        }
        Write-Host ('  0x{0:X5} (VA:{1:X8})  {2}  <- {3}' -f $off,$va,$hex,$cond) -ForegroundColor Cyan
    }
    else {
        Write-Host ('  0x{0:X5} (VA:{1:X8})  {2}' -f $off,$va,$hex)
    }
}
