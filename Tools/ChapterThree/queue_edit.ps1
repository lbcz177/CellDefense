param([Parameter(Mandatory=$true)][int]$PlayProcessId)
$ErrorActionPreference='Stop'
if(Get-Process -Id $PlayProcessId -ErrorAction SilentlyContinue){Wait-Process -Id $PlayProcessId}
powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'validate.ps1') -Mode edit
