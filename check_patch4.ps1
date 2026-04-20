$rom = [System.IO.File]::ReadAllBytes('C:\Users\cass_\OneDrive\Desktop\DeSmuME\ROMs\Let''s Play Pet Hospitals.nds')

$offsets = @(0x45038, 0x45048, 0x4505C, 0x45070, 0x45084, 0x45098, 0x450AC, 0x450C0)
$labels  = @('4a','4b','4c','4d','4e','4f','4g','4h')
$orig = @(
    [byte[]](0x64,0x00,0x51,0xE3),
    [byte[]](0xC8,0x00,0x51,0xE3),
    [byte[]](0x4B,0x0F,0x51,0xE3),
    [byte[]](0x19,0x0E,0x51,0xE3),
    [byte[]](0x7D,0x0F,0x51,0xE3),
    [byte[]](0x96,0x0F,0x51,0xE3),
    [byte[]](0xAF,0x0F,0x51,0xE3),
    [byte[]](0x32,0x0E,0x51,0xE3)
)
$patched = @(
    [byte[]](0xC8,0x00,0x51,0xE3),
    [byte[]](0x19,0x0E,0x51,0xE3),
    [byte[]](0x96,0x0F,0x51,0xE3),
    [byte[]](0x32,0x0E,0x51,0xE3),
    [byte[]](0xFA,0x0F,0x51,0xE3),
    [byte[]](0x4B,0x0E,0x51,0xE3),
    [byte[]](0x58,0x0E,0x51,0xE3),
    [byte[]](0x19,0x0D,0x51,0xE3)
)

for ($i = 0; $i -lt 8; $i++) {
    $off    = $offsets[$i]
    $actual = $rom[$off], $rom[$off+1], $rom[$off+2], $rom[$off+3]
    $isOrig  = ($actual[0] -eq $orig[$i][0])    -and ($actual[1] -eq $orig[$i][1])    -and ($actual[2] -eq $orig[$i][2])    -and ($actual[3] -eq $orig[$i][3])
    $isPatch = ($actual[0] -eq $patched[$i][0]) -and ($actual[1] -eq $patched[$i][1]) -and ($actual[2] -eq $patched[$i][2]) -and ($actual[3] -eq $patched[$i][3])
    $state = if ($isPatch) { 'PATCHED' } elseif ($isOrig) { 'ORIGINAL' } else { 'UNKNOWN' }
    $hex = ($actual | ForEach-Object { '{0:X2}' -f $_ }) -join ' '
    Write-Host ('Patch ' + $labels[$i] + '  ROM:0x' + $off.ToString('X5') + '  bytes: ' + $hex + '  -> ' + $state)
}

# Also check patches 1-3
$p1off = 0x3A224; $p1orig = [byte[]](0x04,0x10,0x90,0xE5,0x00,0x00,0x51,0xE3); $p1pat = [byte[]](0x00,0x00,0xA0,0xE3,0x1E,0xFF,0x2F,0xE1)
$p2off = 0x71A7E; $p2orig = 0x70; $p2pat = 0x50
$p3off = 0x442A4; $p3orig = [byte[]](0x01,0x50,0x80,0xE2); $p3pat = [byte[]](0x02,0x50,0xA0,0xE3)

Write-Host ""
$b = $rom[$p1off..($p1off+7)]
$p1state = if (($b[0] -eq $p1pat[0]) -and ($b[1] -eq $p1pat[1])) {'PATCHED'} elseif (($b[0] -eq $p1orig[0]) -and ($b[1] -eq $p1orig[1])) {'ORIGINAL'} else {'UNKNOWN'}
Write-Host ('Patch 1   ROM:0x' + $p1off.ToString('X5') + '  -> ' + $p1state)

$p2state = if ($rom[$p2off] -eq $p2pat) {'PATCHED'} elseif ($rom[$p2off] -eq $p2orig) {'ORIGINAL'} else {'UNKNOWN'}
Write-Host ('Patch 2   ROM:0x' + $p2off.ToString('X5') + '  -> ' + $p2state)

$b3 = $rom[$p3off], $rom[$p3off+1], $rom[$p3off+2], $rom[$p3off+3]
$p3state = if (($b3[0] -eq $p3pat[0]) -and ($b3[1] -eq $p3pat[1])) {'PATCHED'} elseif (($b3[0] -eq $p3orig[0]) -and ($b3[1] -eq $p3orig[1])) {'ORIGINAL'} else {'UNKNOWN'}
Write-Host ('Patch 3a  ROM:0x' + $p3off.ToString('X5') + '  -> ' + $p3state)
