param([Parameter(Mandatory=$true)][int]$PlayProcessId)
$ErrorActionPreference='Stop'
if(Get-Process -Id $PlayProcessId -ErrorAction SilentlyContinue){Wait-Process -Id $PlayProcessId}
Remove-Item Env:CELLDEFENSE_CHAPTER_TWO_FILTER -ErrorAction SilentlyContinue
python (Join-Path $PSScriptRoot 'validate.py') edit
