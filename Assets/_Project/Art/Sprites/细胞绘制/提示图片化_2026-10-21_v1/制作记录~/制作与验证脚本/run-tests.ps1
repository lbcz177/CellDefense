$ErrorActionPreference = 'Stop'
$taskRoot = 'E:/unity/CellDefense'
$taskProject = Join-Path $taskRoot 'Logs/CodexTradingTests'
$taskOutput = Join-Path $taskRoot 'Logs/PromptArtwork20261021'
$taskUnity = 'E:/UnityEditors/2022.3.53f1c1/Editor/Unity.exe'
function Run-Tests([string]$platform, [string[]]$graphics) {
    $platform | Set-Content -LiteralPath (Join-Path $taskOutput 'stage.txt')
    $taskArguments = @('-batchmode') + $graphics + @('-buildTarget','Android','-projectPath',$taskProject,
        '-runTests','-testPlatform',$platform,'-testResults',(Join-Path $taskOutput ($platform + '.xml')),
        '-logFile',(Join-Path $taskOutput ($platform + '.log')))
    $taskProcess = Start-Process -FilePath $taskUnity -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
    $taskProcess.Id | Set-Content -LiteralPath (Join-Path $taskOutput 'unity-pid.txt')
    $taskProcess.WaitForExit()
    $taskProcess.Refresh()
    if ($taskProcess.ExitCode -ne 0) { throw "$platform exited with $($taskProcess.ExitCode)" }
    [xml]$taskResult = Get-Content -LiteralPath (Join-Path $taskOutput ($platform + '.xml')) -Raw -Encoding UTF8
    if ($taskResult.'test-run'.result -ne 'Passed') { throw "$platform tests failed" }
}
try {
    Run-Tests 'EditMode' @('-nographics')
    Run-Tests 'PlayMode' @('-force-d3d11','-screen-width','1280','-screen-height','720')
    'Passed' | Set-Content -LiteralPath (Join-Path $taskOutput 'stage.txt')
} catch {
    $_.Exception.ToString() | Set-Content -LiteralPath (Join-Path $taskOutput 'error.txt')
    'Failed' | Set-Content -LiteralPath (Join-Path $taskOutput 'stage.txt')
    exit 1
}

