# Apply all Pet Hospitals ARM9 patches directly to the ROM file.
# No Tinke required. No header changes needed (ARM9 size is unchanged).
#
# Usage:
#   powershell.exe -File apply_patches.ps1
#   powershell.exe -File apply_patches.ps1 -Revert   (restore original bytes)
#
# A .bak backup is created automatically if one does not already exist.

param([switch]$Revert)

$romPath = 'C:\Users\cass_\OneDrive\Desktop\DeSmuME\ROMs\Let' + [char]39 + 's Play Pet Hospitals.nds'
$bakPath = $romPath + '.bak'
$arm9Base = 0x4000

# ---- Patch definitions (rom_offset, original_bytes, patched_bytes, description) ----
$patches = @(
    @{
        offset  = $arm9Base + 0x36224
        orig    = [byte[]](0x04,0x10,0x90,0xE5, 0x00,0x00,0x51,0xE3)
        patched = [byte[]](0x00,0x00,0xA0,0xE3, 0x1E,0xFF,0x2F,0xE1)
        desc    = "Patch 1 (0x36224): texture replacement fix"
    },
    @{
        offset  = $arm9Base + 0x6DA7E
        orig    = [byte[]](0x70)   # 'p'
        patched = [byte[]](0x50)   # 'P'
        desc    = "Patch 2 (0x6DA7E): guineaPig capitalisation"
    },
    @{
        offset  = $arm9Base + 0x402A4
        orig    = [byte[]](0x01,0x50,0x80,0xE2)   # ADD r5, r0, #1
        patched = [byte[]](0x02,0x50,0xA0,0xE3)   # MOV r5, #2  (50% ANY breed)
        desc    = "Patch 3a (0x402A4): breed range -> 2 (~50% ANY breed)"
    },
    @{
        offset  = $arm9Base + 0x40328
        orig    = [byte[]](0x01,0x60,0x80,0xE2)   # ADD r6, r0, #1
        patched = [byte[]](0x02,0x60,0xA0,0xE3)   # MOV r6, #2  (50% ANY color)
        desc    = "Patch 3b (0x40328): color range -> 2 (~50% ANY color)"
    },
    # Patch 4 — Hard Mode grade thresholds (all x2, lower-bound CMPs only)
    # Grade boundaries: D<200, C 200-400, B- 400-600, B 600-800, B+ 800-1000, A- 1000-1200, A 1200-1408, A+ >1408 (was: every 100pts)
    @{
        offset  = $arm9Base + 0x41038
        orig    = [byte[]](0x64,0x00,0x51,0xE3)   # CMP r1, #100
        patched = [byte[]](0xC8,0x00,0x51,0xE3)   # CMP r1, #200
        desc    = "Patch 4a (0x41038): grade threshold 100->200"
    },
    @{
        offset  = $arm9Base + 0x41048
        orig    = [byte[]](0xC8,0x00,0x51,0xE3)   # CMP r1, #200
        patched = [byte[]](0x19,0x0E,0x51,0xE3)   # CMP r1, #400
        desc    = "Patch 4b (0x41048): grade threshold 200->400"
    },
    @{
        offset  = $arm9Base + 0x4105C
        orig    = [byte[]](0x4B,0x0F,0x51,0xE3)   # CMP r1, #300
        patched = [byte[]](0x96,0x0F,0x51,0xE3)   # CMP r1, #600
        desc    = "Patch 4c (0x4105C): grade threshold 300->600"
    },
    @{
        offset  = $arm9Base + 0x41070
        orig    = [byte[]](0x19,0x0E,0x51,0xE3)   # CMP r1, #400
        patched = [byte[]](0x32,0x0E,0x51,0xE3)   # CMP r1, #800
        desc    = "Patch 4d (0x41070): grade threshold 400->800"
    },
    @{
        offset  = $arm9Base + 0x41084
        orig    = [byte[]](0x7D,0x0F,0x51,0xE3)   # CMP r1, #500
        patched = [byte[]](0xFA,0x0F,0x51,0xE3)   # CMP r1, #1000
        desc    = "Patch 4e (0x41084): grade threshold 500->1000"
    },
    @{
        offset  = $arm9Base + 0x41098
        orig    = [byte[]](0x96,0x0F,0x51,0xE3)   # CMP r1, #600
        patched = [byte[]](0x4B,0x0E,0x51,0xE3)   # CMP r1, #1200
        desc    = "Patch 4f (0x41098): grade threshold 600->1200"
    },
    @{
        offset  = $arm9Base + 0x410AC
        orig    = [byte[]](0xAF,0x0F,0x51,0xE3)   # CMP r1, #700
        patched = [byte[]](0x58,0x0E,0x51,0xE3)   # CMP r1, #1408
        desc    = "Patch 4g (0x410AC): grade threshold 700->1408"
    },
    @{
        offset  = $arm9Base + 0x410C0
        orig    = [byte[]](0x32,0x0E,0x51,0xE3)   # CMP r1, #800
        patched = [byte[]](0x19,0x0D,0x51,0xE3)   # CMP r1, #1600
        desc    = "Patch 4h (0x410C0): grade threshold 800->1600"
    },
    # Patch 4 v2 — tiers 9-17 (fixes grades 8-15 unreachable bug)
    @{
        offset  = $arm9Base + 0x410D4
        orig    = [byte[]](0xE1,0x0F,0x51,0xE3)   # CMP r1, #900
        patched = [byte[]](0x71,0x0E,0x51,0xE3)   # CMP r1, #1808 (nearest valid to 1800)
        desc    = "Patch 4i (0x410D4): grade threshold 900->1808"
    },
    @{
        offset  = $arm9Base + 0x410E8
        orig    = [byte[]](0xFA,0x0F,0x51,0xE3)   # CMP r1, #1000
        patched = [byte[]](0x7D,0x0E,0x51,0xE3)   # CMP r1, #2000
        desc    = "Patch 4j (0x410E8): grade threshold 1000->2000"
    },
    @{
        offset  = $arm9Base + 0x41118
        orig    = [byte[]](0x4B,0x0E,0x51,0xE3)   # CMP r1, #1200
        patched = [byte[]](0x96,0x0E,0x51,0xE3)   # CMP r1, #2400
        desc    = "Patch 4k (0x41118): grade threshold 1200->2400"
    },
    @{
        offset  = $arm9Base + 0x41148
        orig    = [byte[]](0x64,0x20,0x82,0xE2)   # ADD r2, r2, #100  (tier 14 guard offset)
        patched = [byte[]](0xC8,0x20,0x82,0xE2)   # ADD r2, r2, #200  (1400->2800)
        desc    = "Patch 4l (0x41148): tier14 guard ADD #100->#200"
    },
    @{
        offset  = $arm9Base + 0x41164
        orig    = [byte[]](0x64,0x20,0x82,0xE2)   # ADD r2, r2, #100  (tier 15 guard offset)
        patched = [byte[]](0xC8,0x20,0x82,0xE2)   # ADD r2, r2, #200  (1500->3000)
        desc    = "Patch 4m (0x41164): tier15 guard ADD #100->#200"
    },
    @{
        offset  = $arm9Base + 0x41180
        orig    = [byte[]](0x19,0x0D,0x51,0xE3)   # CMP r1, #1600
        patched = [byte[]](0xC8,0x0E,0x51,0xE3)   # CMP r1, #3200
        desc    = "Patch 4n (0x41180): grade threshold 1600->3200"
    },
    @{
        offset  = $arm9Base + 0x411B4
        orig    = [byte[]](0x4C,0x04,0x00,0x00)   # pool: 1100
        patched = [byte[]](0x98,0x08,0x00,0x00)   # pool: 2200
        desc    = "Patch 4o (0x411B4): pool 1100->2200"
    },
    @{
        offset  = $arm9Base + 0x411B8
        orig    = [byte[]](0x14,0x05,0x00,0x00)   # pool: 1300
        patched = [byte[]](0x28,0x0A,0x00,0x00)   # pool: 2600
        desc    = "Patch 4p (0x411B8): pool 1300->2600"
    },
    @{
        offset  = $arm9Base + 0x411BC
        orig    = [byte[]](0x78,0x05,0x00,0x00)   # pool: 1400
        patched = [byte[]](0xF0,0x0A,0x00,0x00)   # pool: 2800
        desc    = "Patch 4q (0x411BC): pool 1400->2800"
    },
    @{
        offset  = $arm9Base + 0x411C0
        orig    = [byte[]](0xDC,0x05,0x00,0x00)   # pool: 1500
        patched = [byte[]](0xB8,0x0B,0x00,0x00)   # pool: 3000
        desc    = "Patch 4r (0x411C0): pool 1500->3000"
    },
    @{
        offset  = $arm9Base + 0x411C4
        orig    = [byte[]](0xA4,0x06,0x00,0x00)   # pool: 1700
        patched = [byte[]](0x48,0x0D,0x00,0x00)   # pool: 3400
        desc    = "Patch 4s (0x411C4): pool 1700->3400"
    }
    ,@{
        offset  = $arm9Base + 0x61EAC
        orig    = [byte[]](0xCC,0x17,0x07,0x02)   # ptr -> 0x020717CC (Scissor)
        patched = [byte[]](0x04,0xD9,0x06,0x02)   # ptr -> 0x0206D904 (EyeDrop) — slots 16 and 17
        desc    = "Patch 5 (0x61EAC): redirect slots 16/17 from Scissor to EyeDrop"
    }
    ,@{
        offset  = $arm9Base + 0x6FD15
        orig    = [byte[]](0xFF,0xFF)              # b1=255, b2=255 — EYEINFECTION untreatable
        patched = [byte[]](0x10,0x10)              # b1=16,  b2=16  — EyeDrop slot
        desc    = "Patch 6 (0x6FD15): state-machine EYEINFECTION b1/b2 -> slot 16 (EyeDrop)"
    }
)

# ---- Helpers ----
function Bytes-Match($rom, $offset, $expected) {
    for ($i = 0; $i -lt $expected.Length; $i++) {
        if ($rom[$offset + $i] -ne $expected[$i]) { return $false }
    }
    return $true
}

function Write-Bytes($rom, $offset, $bytes) {
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        $rom[$offset + $i] = $bytes[$i]
    }
}

# ---- Load ROM ----
if (-not (Test-Path $romPath)) {
    Write-Host "ERROR: ROM not found at $romPath" -ForegroundColor Red
    exit 1
}
$rom = [System.IO.File]::ReadAllBytes($romPath)

# ---- Backup ----
if (-not (Test-Path $bakPath)) {
    Write-Host "Creating backup: $bakPath"
    [System.IO.File]::WriteAllBytes($bakPath, $rom)
} else {
    Write-Host "Backup already exists: $bakPath"
}

# ---- Apply or revert ----
$dirty = $false
foreach ($p in $patches) {
    $src = if ($Revert) { $p.patched } else { $p.orig    }
    $dst = if ($Revert) { $p.orig    } else { $p.patched }

    if (Bytes-Match $rom $p.offset $dst) {
        Write-Host "  Already $(if($Revert){'reverted'}else{'patched'}): $($p.desc)" -ForegroundColor Cyan
    } elseif (Bytes-Match $rom $p.offset $src) {
        Write-Bytes $rom $p.offset $dst
        Write-Host "  $(if($Revert){'Reverted'}else{'Patched'}): $($p.desc)" -ForegroundColor Green
        $dirty = $true
    } else {
        Write-Host "  SKIP (unexpected bytes): $($p.desc)" -ForegroundColor Yellow
    }
}

if ($dirty) {
    [System.IO.File]::WriteAllBytes($romPath, $rom)
    Write-Host ""
    Write-Host "Saved: $romPath" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "No changes written." -ForegroundColor Gray
}
