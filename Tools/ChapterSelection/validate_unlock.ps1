$ErrorActionPreference='Stop'
Set-Location 'E:/unity/CellDefense'
$unlockPrevious=Get-Content Logs/ChapterSelection20261028/active-run.json -Raw | ConvertFrom-Json
Wait-Process -Id $unlockPrevious.pid -ErrorAction SilentlyContinue
Remove-Item Env:CELLDEFENSE_CHAPTER_TEST_FILTER -ErrorAction SilentlyContinue
$unlockOutput=python Tools/ChapterSelection/start_validation.py
if($LASTEXITCODE -ne 0){throw 'Cannot start final validation'}
$unlockOutput | Set-Content Logs/ChapterSelection20261028/unlock-final-run.json -Encoding utf8
& Tools/ChapterSelection/run_unlock_editmode.ps1
