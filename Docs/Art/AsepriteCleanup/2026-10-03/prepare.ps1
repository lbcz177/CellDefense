$ErrorActionPreference = 'Stop'
$root = 'E:\unity\CellDefense\Assets\_Project\Art\Sprites\细胞绘制'
$work = 'E:\unity\CellDefense\Docs\Art\AsepriteCleanup\2026-10-03'
if (Test-Path -LiteralPath (Join-Path $work 'plan.json')) { throw 'Cleanup plan already exists' }
$null = New-Item -ItemType Directory -Force -Path $work
Add-Type -AssemblyName System.Drawing
$pngs = @(Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.png')
$jobs = @()
$excluded = @()
foreach ($file in $pngs) {
 $rel = $file.FullName.Substring($root.Length+1)
 $reason = $null
 if ($rel -match '试玩关_方块样块|试玩关_完整地图|07_地图与覆盖层|00_已有素材\\第[一二三]关_|11_塔位资源与技能\\(145|146|147|148|154|155)_') { $reason='map or terrain element' }
 elseif ($rel -match '制作记录~|\\source\\|\\旧版\\|概念留档') { $reason='original source or archive' }
 elseif ($rel -match '\\frames\\') { continue }
 if ($reason) { $excluded += [ordered]@{relative=$rel;reason=$reason;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash}; continue }
 $bmp = [Drawing.Bitmap]::new($file.FullName)
 $width=$bmp.Width; $height=$bmp.Height; $bmp.Dispose()
 $kind='unit'; $colors=48; $medianLimit=18
 if ($rel -match '^帧动画\\') {$kind='animation'; $colors=48; $medianLimit=0}
 elseif ($rel -match '08_子弹与特效') {$kind='effect'; $colors=128; $medianLimit=8}
 elseif ($rel -match '图标|状态图标|11_塔位资源与技能') {$kind='icon'; $colors=64; $medianLimit=14}
 elseif ($rel -match '^暂停菜单|^胜负结算|^主界面\\按钮') {$kind='ui'; $colors=96; $medianLimit=10}
 elseif ($rel -match '^主界面|^选关界面') {$kind='screen'; $colors=256; $medianLimit=8}
 $job=[ordered]@{id=('{0:D3}' -f ($jobs.Count+1));relative=$rel;input=$file.FullName;output=(Join-Path $work ('staged\'+$rel));kind=$kind;colors=$colors;medianLimit=$medianLimit;width=$width;height=$height}
 if ($kind -eq 'animation') {
  $dir = $file.DirectoryName
  $ase = @(Get-ChildItem -LiteralPath $dir -File -Filter '*.aseprite')
  $gif = @(Get-ChildItem -LiteralPath $dir -File -Filter '*.gif')
  $frames = @(Get-ChildItem -LiteralPath (Join-Path $dir 'frames') -File -Filter '*.png' | Sort-Object Name)
  $job['aseRelative']=$ase[0].FullName.Substring($root.Length+1)
  $job['gifRelative']=$gif[0].FullName.Substring($root.Length+1)
  $job['framesRelative']=@($frames | ForEach-Object {$_.FullName.Substring($root.Length+1)})
 }
 $jobs += $job
}
$baseline=@()
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {$_.Extension -in '.png','.meta','.aseprite','.gif'}) {
 $rel=$file.FullName.Substring($root.Length+1)
 $baseline += [ordered]@{relative=$rel;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash;bytes=$file.Length}
}
$plan=[ordered]@{root=$root;work=$work;created=(Get-Date -Format o);jobs=$jobs;excluded=$excluded}
$plan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $work 'plan.json') -Encoding utf8
$baseline | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $work 'baseline.json') -Encoding utf8
$backupFiles=@()
foreach ($job in $jobs) {
 $rels=@($job.relative)
 if ($job.kind -eq 'animation') {$rels+=@($job.aseRelative,$job.gifRelative); $rels+=@($job.framesRelative)}
 foreach ($rel in $rels) {
  foreach ($suffix in @('', '.meta')) {
   $src=Join-Path $root ($rel+$suffix)
   if (Test-Path -LiteralPath $src) {
    $dst=Join-Path $work ('backup\'+$rel+$suffix)
    $null=New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst)
    Copy-Item -LiteralPath $src -Destination $dst
    if ((Get-FileHash -LiteralPath $src).Hash -ne (Get-FileHash -LiteralPath $dst).Hash) {throw "Backup mismatch: $rel"}
    $backupFiles += $rel+$suffix
   }
  }
 }
 $null=New-Item -ItemType Directory -Force -Path (Split-Path -Parent $job.output)
}
[ordered]@{Jobs=$jobs.Count;Categories=@($jobs|Group-Object kind|ForEach-Object{[ordered]@{Kind=$_.Name;Count=$_.Count}});Excluded=$excluded.Count;BackupFiles=$backupFiles.Count;Work=$work;SizeGroups=@($jobs|Group-Object width,height|ForEach-Object{[ordered]@{Size=$_.Name;Count=$_.Count}})} | ConvertTo-Json -Depth 5
