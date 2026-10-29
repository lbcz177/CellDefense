$ErrorActionPreference='Stop'
$unlockRoot='E:/unity/CellDefense'
$unlockRun=Get-Content (Join-Path $unlockRoot 'Logs/ChapterSelection20261028/active-run.json') -Raw | ConvertFrom-Json
Wait-Process -Id $unlockRun.pid -ErrorAction SilentlyContinue
$unlockArgs=@('-batchmode','-nographics','-buildTarget','Android','-projectPath',(Join-Path $unlockRoot 'Logs/CodexTradingTests'),'-runTests','-testPlatform','EditMode','-testResults',(Join-Path $unlockRoot 'Logs/ChapterSelection20261028/editmode-sequential.xml'),'-logFile',(Join-Path $unlockRoot 'Logs/ChapterSelection20261028/editmode-sequential.log'))
$unlockProcess=Start-Process -FilePath 'E:/UnityEditors/2022.3.53f1c1/Editor/Unity.exe' -ArgumentList $unlockArgs -WindowStyle Hidden -PassThru
$unlockProcess.WaitForExit()
