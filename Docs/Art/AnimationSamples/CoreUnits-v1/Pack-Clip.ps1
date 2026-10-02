param([Parameter(Mandatory=$true)][string]$JobPath)
$ErrorActionPreference='Stop'
. 'D:\aseprite\automation\Invoke-AsepriteBatch.ps1'
$aseprite='D:\aseprite\Aseprite-v1.3.18.6\aseprite.exe'
$job=Get-Content -LiteralPath $JobPath -Raw | ConvertFrom-Json
$root=$PSScriptRoot
$outputDir=Join-Path $root $job.id
New-Item -ItemType Directory -Path (Join-Path $outputDir 'source'),(Join-Path $outputDir 'frames') -Force | Out-Null
Copy-Item -LiteralPath $job.sourcePath -Destination (Join-Path $outputDir 'source\generated-poses.png')
$job.prompt | Set-Content -LiteralPath (Join-Path $outputDir 'source\prompt.txt') -Encoding utf8
$scaleMultiplier=if ($job.scaleMultiplier) {[string]$job.scaleMultiplier} else {'1'}
$arguments=@('--batch','--script-param',('input='+$job.sourcePath),'--script-param',('output='+$outputDir),'--script-param',('action='+$job.action),'--script-param',('name='+$job.label),'--script-param',('anchor='+$job.anchor),'--script-param',('scaleMultiplier='+$scaleMultiplier),'--script',(Join-Path $root 'build-animation.lua'))
$packed=Invoke-AsepriteProcess -Executable $aseprite -Arguments $arguments
if ($packed.ExitCode -ne 0) {throw ($packed.Stdout+$packed.Stderr)}
$preview=Invoke-AsepriteProcess -Executable $aseprite -Arguments @('--batch',(Join-Path $outputDir 'animation.aseprite'),'--scale','3','--save-as',(Join-Path $outputDir 'preview.gif'))
if ($preview.ExitCode -ne 0) {throw ($preview.Stdout+$preview.Stderr)}
$verified=Invoke-AsepriteProcess -Executable $aseprite -Arguments @('--batch','--script-param',('output='+$outputDir),'--script',(Join-Path $root 'verify-animation.lua'))
if ($verified.ExitCode -ne 0) {throw ($verified.Stdout+$verified.Stderr)}
$report=$verified.Stdout | ConvertFrom-Json
Add-Type -AssemblyName System.Drawing
$gif=[System.Drawing.Image]::FromFile((Join-Path $outputDir 'preview.gif'))
try {
  $count=$gif.GetFrameCount([System.Drawing.Imaging.FrameDimension]::Time)
  $delays=$gif.GetPropertyItem(0x5100).Value
  $durations=@()
  for ($i=0;$i -lt $count;$i++) {$durations += [BitConverter]::ToUInt32($delays,$i*4)*10}
  $loops=[BitConverter]::ToUInt16($gif.GetPropertyItem(0x5101).Value,0)
  if ($count -ne 8 -or ($durations | Where-Object {$_ -ne 100}).Count -gt 0 -or $loops -ne 0) {throw 'Invalid GIF timing/loop'}
  $report | Add-Member -NotePropertyName gif -NotePropertyValue @{frames=$count;durationsMs=$durations;loopCount=$loops;width=$gif.Width;height=$gif.Height}
} finally {$gif.Dispose()}
$distinct=@(Get-ChildItem -LiteralPath (Join-Path $outputDir 'frames') -Filter '*.png' -File | Get-FileHash -Algorithm SHA256 | Select-Object -ExpandProperty Hash -Unique).Count
if ($distinct -ne 8) {throw 'Expected eight distinct frames'}
$report | Add-Member -NotePropertyName uniqueFrames -NotePropertyValue $distinct
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputDir 'validation.json') -Encoding utf8
$summary=[pscustomobject]@{id=$job.id;label=$job.label;status='packed-and-verified';sourcePath=$job.sourcePath;outputDir=$outputDir;frames=8;fps=10}
$summary | ConvertTo-Json -Compress | Add-Content -LiteralPath (Join-Path $root 'generation-log.jsonl') -Encoding utf8
$summary | ConvertTo-Json -Compress
