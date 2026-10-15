param([Parameter(Mandatory=$true)][string]$Source,[Parameter(Mandatory=$true)][string]$Name)
$ErrorActionPreference='Stop'
$root='E:\unity\CellDefense\Assets\_Project\Art\Sprites\细胞绘制'
$directory=Join-Path $root '前五关地图_2026-10-15_v1'
$destination=Join-Path $directory ($Name+'.png')
if (Test-Path -LiteralPath $destination) { throw 'Versioned output already exists; preserve it.' }
New-Item -ItemType Directory -Path $directory -Force | Out-Null
Copy-Item -LiteralPath $Source -Destination $destination
$template=Get-Content -LiteralPath (Join-Path $root '试玩关_绕路地图_v2\试玩关_底部双入口_中央障碍.png.meta') -Raw
$template=[regex]::Replace($template,'(?m)^guid: [a-f0-9]+','guid: '+[Guid]::NewGuid().ToString('N'))
$template=[regex]::Replace($template,'spriteID: [a-f0-9]+','spriteID: '+[Guid]::NewGuid().ToString('N'))
Set-Content -LiteralPath ($destination+'.meta') -Value $template -Encoding utf8
if (!(Test-Path -LiteralPath ($directory+'.meta'))) {
  $folderMeta="fileFormatVersion: 2`nguid: $([Guid]::NewGuid().ToString('N'))`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData:`n  assetBundleName:`n  assetBundleVariant:`n"
  Set-Content -LiteralPath ($directory+'.meta') -Value $folderMeta -Encoding utf8
}
if ((Get-FileHash -LiteralPath $Source).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) { throw 'Copy verification failed' }
$bytes=[IO.File]::ReadAllBytes($destination)
$width=[int]$bytes[16]*16777216+[int]$bytes[17]*65536+[int]$bytes[18]*256+[int]$bytes[19]
$height=[int]$bytes[20]*16777216+[int]$bytes[21]*65536+[int]$bytes[22]*256+[int]$bytes[23]
[pscustomobject]@{Path=$destination;Width=$width;Height=$height;Bytes=$bytes.Length;CopyVerified=$true}|ConvertTo-Json
