$ErrorActionPreference = 'Stop'
$assetDir = 'E:/unity/CellDefense/Assets/_Project/Art/Sprites/细胞绘制/帧动画/中性粒细胞_强化攻击_v2'
$oldDir = 'E:/unity/CellDefense/Assets/_Project/Art/Sprites/细胞绘制/帧动画/六塔与普通敌人_v1/01_neutrophil_attack'
$sheetTemplate = Get-Content -LiteralPath (Join-Path $oldDir 'animation_sheet.png.meta') -Raw
$singleTemplate = Get-Content -LiteralPath (Join-Path $oldDir 'frames/animation_01.png.meta') -Raw
function Set-NewTextureMeta([string]$ImagePath, [string]$Template) {
    $metaPath = $ImagePath + '.meta'
    $guid = [guid]::NewGuid().ToString('N')
    if (Test-Path -LiteralPath $metaPath) {
        $existing = Get-Content -LiteralPath $metaPath -Raw
        $match = [regex]::Match($existing, '(?m)^guid: ([a-f0-9]+)')
        if ($match.Success) { $guid = $match.Groups[1].Value }
    }
    $content = [regex]::Replace($Template, '(?m)^guid: [a-f0-9]+', ('guid: ' + $guid))
    $content = $content -replace 'filterMode: \d+', 'filterMode: 0'
    $content = $content -replace 'textureCompression: \d+', 'textureCompression: 0'
    $content = $content -replace 'enableMipMap: \d+', 'enableMipMap: 0'
    $content = $content -replace 'spritePixelsToUnits: [0-9.]+', 'spritePixelsToUnits: 128'
    $content = $content -replace 'spriteGenerateFallbackPhysicsShape: \d+', 'spriteGenerateFallbackPhysicsShape: 0'
    $content = $content -replace 'alignment: \d+', 'alignment: 0'
    $content = $content -replace 'spritePivot: \{[^}]+\}', 'spritePivot: {x: 0.5, y: 0.5}'
    $content = $content -replace 'pivot: \{[^}]+\}', 'pivot: {x: 0.5, y: 0.5}'
    $content = [regex]::Replace($content, 'spriteID: [a-f0-9]+', { param($m) 'spriteID: ' + [guid]::NewGuid().ToString('N') })
    for ($i = 0; $i -lt 8; $i++) {
        $name = 'neutrophil_attack_{0:D2}' -f ($i + 1)
        $content = $content.Replace(('animation_sheet_' + $i), $name)
    }
    [IO.File]::WriteAllText($metaPath, $content, [Text.UTF8Encoding]::new($false))
}
Set-NewTextureMeta (Join-Path $assetDir 'neutrophil_attack_sheet.png') $sheetTemplate
Get-ChildItem -LiteralPath (Join-Path $assetDir 'frames') -Filter '*.png' | ForEach-Object {
    Set-NewTextureMeta $_.FullName $singleTemplate
}
$sheetMeta = Get-Content -LiteralPath (Join-Path $assetDir 'neutrophil_attack_sheet.png.meta') -Raw
if ([regex]::Matches($sheetMeta, '(?m)^      name: neutrophil_attack_').Count -ne 8) { throw 'Expected eight sprite rectangles' }
if ($sheetMeta -notmatch 'spriteMode: 2') { throw 'Expected multiple sprites' }
if ($sheetMeta -match 'filterMode: [1-9]|textureCompression: [1-9]|enableMipMap: [1-9]') { throw 'Pixel import settings mismatch' }
Write-Output 'Prepared 8 sprite rectangles and 9 PNG import settings: Point, no compression, no mipmaps, 128 PPU.'
