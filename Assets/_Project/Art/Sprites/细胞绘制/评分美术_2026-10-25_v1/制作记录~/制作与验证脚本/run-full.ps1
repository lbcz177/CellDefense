param([string]$ScoreModes='EditMode,PlayMode',[string]$ScoreRun='full-v1',[string]$ScoreRenderer='-force-d3d11',[string]$ScoreFilter='')
$ErrorActionPreference='Stop'
$scoreCheckRoot='E:/unity/CellDefense'
$scoreCheckOut=Join-Path $scoreCheckRoot 'Logs/ScoreArt20261025'
$scoreCheckProject=Join-Path $scoreCheckRoot 'Logs/CodexTradingTests'
$scoreCheckChannel=[regex]::Match((Get-Content "$env:LOCALAPPDATA/Unity/Editor/Editor.log" -Raw),'Successfully connected to: "(LicenseClient-[^"]+)"').Groups[1].Value
if(!$scoreCheckChannel){throw 'No existing Unity licensing channel'}
$env:CELLDEFENSE_SCORE_CAPTURE_DIR=Join-Path $scoreCheckRoot ('Assets/_Project/Art/Sprites/细胞绘制/评分美术_2026-10-25_v1/验证_'+$ScoreRun+'~')
foreach($scoreCheckMode in $ScoreModes.Split(',')) {
 $scoreCheckName=$scoreCheckMode.ToLowerInvariant()+'-'+$ScoreRun
 $scoreCheckXml=Join-Path $scoreCheckOut ($scoreCheckName+'.xml')
 if(Test-Path $scoreCheckXml){throw 'Preserve previous verification files'}
 $scoreCheckArgs=@('-batchmode','-licensingIpc',$scoreCheckChannel,'-disableManagedDebugger','-disableAudio','-buildTarget','Android','-projectPath',$scoreCheckProject,'-runTests','-testPlatform',$scoreCheckMode,'-testResults',$scoreCheckXml,'-logFile',(Join-Path $scoreCheckOut ($scoreCheckName+'.log')))
 $scoreCheckArgs+=if($scoreCheckMode -eq 'EditMode'){'-nographics'}else{$ScoreRenderer}
 if($ScoreFilter){$scoreCheckArgs+=@('-testFilter',$ScoreFilter)}
 $scoreCheckProcess=Start-Process -FilePath 'E:/UnityEditors/2022.3.53f1c1/Editor/Unity.exe' -ArgumentList $scoreCheckArgs -WindowStyle Hidden -PassThru
 $scoreCheckProcess.Id | Set-Content (Join-Path $scoreCheckOut 'pid.txt')
 $scoreCheckProcess.WaitForExit()
 if(!(Test-Path $scoreCheckXml)){throw 'No test results; inspect startup/compilation log'}
 [xml]$scoreCheckResult=Get-Content $scoreCheckXml -Raw -Encoding utf8
 $scoreCheckResult.'test-run' | Select-Object result,total,passed,failed | ConvertTo-Json -Compress
 if($scoreCheckResult.'test-run'.result -ne 'Passed'){exit 1}
}
