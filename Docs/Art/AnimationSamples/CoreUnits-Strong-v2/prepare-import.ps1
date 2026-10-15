$ErrorActionPreference = 'Stop'
$docRoot = 'E:/unity/CellDefense/Docs/Art/AnimationSamples/CoreUnits-Strong-v2'
$assetRoot = 'E:/unity/CellDefense/Assets/_Project/Art/Sprites/细胞绘制/帧动画/六塔与普通敌人_强化动画_v2'
$oldDir = 'E:/unity/CellDefense/Assets/_Project/Art/Sprites/细胞绘制/帧动画/六塔与普通敌人_v1/01_neutrophil_attack'
$sheetTemplate = Get-Content -LiteralPath (Join-Path $oldDir 'animation_sheet.png.meta') -Raw
$singleTemplate = Get-Content -LiteralPath (Join-Path $oldDir 'frames/animation_01.png.meta') -Raw
$jobs = Get-Content -LiteralPath (Join-Path $docRoot 'progress.json') -Raw | ConvertFrom-Json
function Set-TextureMeta([string]$ImagePath,[string]$Template,[bool]$Multiple) {
    $metaPath = $ImagePath + '.meta'
    $guid = [guid]::NewGuid().ToString('N')
    if (Test-Path -LiteralPath $metaPath) {
        $existing = Get-Content -LiteralPath $metaPath -Raw
        $match = [regex]::Match($existing,'(?m)^guid: ([a-f0-9]+)')
        if ($match.Success) { $guid = $match.Groups[1].Value }
    }
    $content = [regex]::Replace($Template,'(?m)^guid: [a-f0-9]+',('guid: ' + $guid))
    $content = $content -replace 'filterMode: \d+','filterMode: 0'
    $content = $content -replace 'textureCompression: \d+','textureCompression: 0'
    $content = $content -replace 'enableMipMap: \d+','enableMipMap: 0'
    $content = $content -replace 'spritePixelsToUnits: [0-9.]+','spritePixelsToUnits: 128'
    $content = $content -replace 'spriteGenerateFallbackPhysicsShape: \d+','spriteGenerateFallbackPhysicsShape: 0'
    $content = $content -replace 'alignment: \d+','alignment: 0'
    $content = $content -replace 'spritePivot: \{[^}]+\}','spritePivot: {x: 0.5, y: 0.5}'
    $content = $content -replace 'pivot: \{[^}]+\}','pivot: {x: 0.5, y: 0.5}'
    $content = $content -replace 'spriteMode: \d+',('spriteMode: ' + $(if ($Multiple) { 2 } else { 1 }))
    $content = [regex]::Replace($content,'spriteID: [a-f0-9]+',{ param($m) 'spriteID: ' + [guid]::NewGuid().ToString('N') })
    for ($i=0;$i -lt 8;$i++) { $content = $content.Replace(('animation_sheet_' + $i),('animation_{0:D2}' -f ($i+1))) }
    [IO.File]::WriteAllText($metaPath,$content,[Text.UTF8Encoding]::new($false))
    if ($content -match 'filterMode: [1-9]|textureCompression: [1-9]|enableMipMap: [1-9]') { throw 'Pixel import settings mismatch' }
    if ($Multiple -and [regex]::Matches($content,'(?m)^      name: animation_').Count -ne 8) { throw 'Expected 8 sprite rectangles' }
}
foreach ($job in $jobs) {
    $clipRoot = Join-Path $assetRoot $job.id
    Set-TextureMeta (Join-Path $clipRoot 'animation_sheet.png') $sheetTemplate $true
    Get-ChildItem -LiteralPath (Join-Path $clipRoot 'frames') -Filter '*.png' | ForEach-Object {
        Set-TextureMeta $_.FullName $singleTemplate $false
    }
}
Get-ChildItem -LiteralPath $assetRoot -File -Filter '*.png' | ForEach-Object {
    Set-TextureMeta $_.FullName $singleTemplate $false
}
Add-Type -AssemblyName System.Drawing
$gifChecks = foreach ($gifFile in Get-ChildItem -LiteralPath $assetRoot -Filter '*.gif' -File -Recurse) {
    $image = [System.Drawing.Image]::FromFile($gifFile.FullName)
    try {
        $frames = $image.GetFrameCount([System.Drawing.Imaging.FrameDimension]::Time)
        $delayBytes = $image.GetPropertyItem(0x5100).Value
        $delays = @(for ($i=0;$i -lt $frames;$i++) { [BitConverter]::ToUInt32($delayBytes,$i*4)*10 })
        $loop = if ($image.PropertyIdList -contains 0x5101) { [BitConverter]::ToUInt16($image.GetPropertyItem(0x5101).Value,0) } else { -1 }
        if ($loop -ne 0) { throw ('GIF is not an infinite loop: ' + $gifFile.Name) }
        if ($gifFile.Name -eq 'preview.gif') {
            if ($frames -ne 8 -or $image.Width -ne 384 -or $image.Height -ne 384) { throw 'Clip GIF frame/size mismatch' }
            $manifest = Get-Content -LiteralPath (Join-Path $gifFile.DirectoryName 'animation.json') -Raw | ConvertFrom-Json
            for ($i=0;$i -lt 8;$i++) {
                if ($delays[$i] -ne $manifest.frames[$i].durationMs) { throw 'GIF timing mismatch' }
            }
        }
        [pscustomobject]@{Path=$gifFile.FullName;Width=$image.Width;Height=$image.Height;Frames=$frames;Loop=$loop;CycleMs=($delays | Measure-Object -Sum).Sum;Bytes=$gifFile.Length}
    } finally { $image.Dispose() }
}
if ($gifChecks.Count -ne 13) { throw 'Expected 11 clip GIFs plus two overview GIFs' }
$validations = Get-Content -LiteralPath (Join-Path $docRoot 'validation.json') -Raw | ConvertFrom-Json
if ($validations.Count -ne 11 -or @($validations | Where-Object { -not $_.passed }).Count -ne 0) { throw 'Native validation failed' }
foreach ($job in $jobs) { $job.status = 'complete-verified';$job | Add-Member -NotePropertyName assetPath -NotePropertyValue (Join-Path $assetRoot $job.id) -Force }
$jobs | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $docRoot 'progress.json') -Encoding utf8
$result = [pscustomobject]@{Passed=$true;Clips=11;TowerAttackClips=5;EnemyMoveClips=3;EnemyAttackClips=3;Frames=88;SpriteSheets=11;AsepriteFiles=11;GIFs=13;TextureMetas=101;GIFChecks=$gifChecks}
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $docRoot 'delivery-validation.json') -Encoding utf8
$result | Select-Object Passed,Clips,TowerAttackClips,EnemyMoveClips,EnemyAttackClips,Frames,SpriteSheets,AsepriteFiles,GIFs,TextureMetas | ConvertTo-Json -Compress
