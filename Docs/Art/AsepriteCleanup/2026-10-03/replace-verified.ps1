$ErrorActionPreference='Stop'
$work=$PSScriptRoot
$plan=Get-Content -LiteralPath (Join-Path $work 'plan.json') -Raw | ConvertFrom-Json
$baseline=Get-Content -LiteralPath (Join-Path $work 'baseline.json') -Raw | ConvertFrom-Json
$pixelChecks=Get-Content -LiteralPath (Join-Path $work 'pixel-validation.json') -Raw | ConvertFrom-Json
$animationChecks=Get-Content -LiteralPath (Join-Path $work 'animation-validation.json') -Raw | ConvertFrom-Json
$gifChecks=Get-Content -LiteralPath (Join-Path $work 'gif-validation.json') -Raw | ConvertFrom-Json
if($pixelChecks.Count -ne 177 -or @($pixelChecks|Where-Object {$_.alphaErrors -ne 0}).Count -ne 0 -or $animationChecks.Count -ne 13 -or $gifChecks.Count -ne 13) {throw 'Validation incomplete'}
if(Test-Path -LiteralPath (Join-Path $work 'replacement-manifest.json')) {throw 'Replacement manifest already exists; inspect before rerunning'}
$hashes=@{}
foreach($entry in $baseline) {$hashes[$entry.relative]=$entry.sha256}
$rels=[Collections.Generic.List[string]]::new()
foreach($job in $plan.jobs) {
 $rels.Add($job.relative)
 if($job.kind -eq 'animation') {
  $rels.Add($job.aseRelative);$rels.Add($job.gifRelative)
  foreach($frame in $job.framesRelative) {$rels.Add($frame)}
 }
}
$rootPrefix=[IO.Path]::GetFullPath($plan.root).TrimEnd('\')+'\'
$stagedPrefix=[IO.Path]::GetFullPath((Join-Path $work 'staged')).TrimEnd('\')+'\'
$prepared=[Collections.Generic.List[object]]::new()
foreach($rel in $rels) {
 $destination=[IO.Path]::GetFullPath((Join-Path $plan.root $rel))
 $staged=[IO.Path]::GetFullPath((Join-Path $work ('staged\'+$rel)))
 if(-not $destination.StartsWith($rootPrefix,[StringComparison]::OrdinalIgnoreCase) -or -not $staged.StartsWith($stagedPrefix,[StringComparison]::OrdinalIgnoreCase)) {throw 'Path escaped intended directory'}
 $backup=Join-Path $work ('backup\'+$rel)
 if(-not (Test-Path -LiteralPath $staged) -or -not (Test-Path -LiteralPath $backup)) {throw ('Missing repaired file or backup: '+$rel)}
 $current=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
 if($current -ne $hashes[$rel] -or (Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash -ne $current) {throw ('Original changed during repair: '+$rel)}
 if((Get-FileHash -LiteralPath ($destination+'.meta') -Algorithm SHA256).Hash -ne $hashes[$rel+'.meta']) {throw ('Unity metadata changed: '+$rel)}
 $prepared.Add([pscustomobject]@{relative=$rel;destination=$destination;staged=$staged;before=$current;after=(Get-FileHash -LiteralPath $staged -Algorithm SHA256).Hash})
}
# Validate new standalone frames against their original files, after sheet-level checks.
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $work 'PixelCheck.cs') -ReferencedAssemblies @('System.Drawing.Common','System.Drawing.Primitives','System.Runtime','System.Runtime.InteropServices','System.ComponentModel.Primitives','System.Private.Windows.GdiPlus','System.Private.Windows.Core')
$frameChecks=@()
foreach($entry in $prepared | Where-Object {$_.relative -match '\\frames\\'}) {
 $v=[ArtCleanupPixelCheck]::Compare($entry.destination,$entry.staged)
 if($v[2] -ne 0) {throw ('Frame transparency changed: '+$entry.relative)}
 $frameChecks+=[pscustomobject]@{relative=$entry.relative;width=$v[0];height=$v[1];alphaErrors=$v[2]}
}
$frameChecks | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $work 'frame-validation.json') -Encoding utf8
$prepared | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $work 'replacement-plan.json') -Encoding utf8
$journal=Join-Path $work 'replacement-journal.jsonl'
foreach($entry in $prepared) {
 $atomic=Join-Path $work 'replacement-swap.tmp'
 Copy-Item -LiteralPath $entry.staged -Destination $atomic
 [IO.File]::Replace($atomic,$entry.destination,[System.Management.Automation.Language.NullString]::Value)
 if((Get-FileHash -LiteralPath $entry.destination -Algorithm SHA256).Hash -ne $entry.after) {throw ('Replaced file verification failed: '+$entry.relative)}
 $entry | ConvertTo-Json -Compress | Add-Content -LiteralPath $journal -Encoding utf8
}
$metaCount=0
foreach($entry in $baseline | Where-Object {$_.relative.EndsWith('.meta')}) {
 if((Get-FileHash -LiteralPath (Join-Path $plan.root $entry.relative) -Algorithm SHA256).Hash -ne $entry.sha256) {throw ('Metadata changed: '+$entry.relative)}
 $metaCount++
}
foreach($entry in $plan.excluded) {
 if((Get-FileHash -LiteralPath (Join-Path $plan.root $entry.relative) -Algorithm SHA256).Hash -ne $entry.sha256) {throw ('Excluded image changed: '+$entry.relative)}
}
$report=[ordered]@{
 completed=(Get-Date -Format o);root=$plan.root;
 replacedFiles=$prepared.Count;pngFiles=@($prepared|Where-Object {$_.relative.EndsWith('.png')}).Count;
 asepriteFiles=@($prepared|Where-Object {$_.relative.EndsWith('.aseprite')}).Count;
 gifFiles=@($prepared|Where-Object {$_.relative.EndsWith('.gif')}).Count;
 standaloneFramesVerified=$frameChecks.Count;metadataFilesUnchanged=$metaCount;excludedImagesUnchanged=$plan.excluded.Count;
 backup=(Join-Path $work 'backup');files=@($prepared)
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $work 'replacement-manifest.json') -Encoding utf8
[pscustomobject]$report | Select-Object completed,replacedFiles,pngFiles,asepriteFiles,gifFiles,standaloneFramesVerified,metadataFilesUnchanged,excludedImagesUnchanged,backup | ConvertTo-Json
