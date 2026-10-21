$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskCli = Join-Path $PSScriptRoot 'node_modules/.bin/taptap-cli.cmd'
$taskTarget = Get-Content -LiteralPath (Join-Path $taskRoot 'Docs/Publishing/taptap-target.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$taskManifest = Join-Path $taskRoot $taskTarget.candidateManifest
if (-not (Test-Path -LiteralPath $taskCli)) { throw 'Project-local TapTap CLI is not installed.' }
$taskVersionOutput = & $taskCli version
if ($LASTEXITCODE -ne 0) { throw 'TapTap CLI could not start.' }
$taskVersion = ($taskVersionOutput -join [Environment]::NewLine) | ConvertFrom-Json
if (-not $taskVersion.ok -or $taskVersion.data.version -ne '2026.10.19') {
    throw 'CLI version changed; inspect current help and skill contracts before continuing.'
}
$taskCandidate = Get-Content -LiteralPath $taskManifest -Raw -Encoding UTF8 | ConvertFrom-Json
$taskApk = Join-Path $taskRoot $taskCandidate.apk
if (-not (Test-Path -LiteralPath $taskApk)) { throw 'Candidate APK is missing.' }
$taskFile = Get-Item -LiteralPath $taskApk
$taskDigest = (Get-FileHash -LiteralPath $taskApk -Algorithm SHA256).Hash.ToLowerInvariant()
if ($taskDigest -ne $taskCandidate.sha256 -or $taskFile.Length -ne $taskCandidate.bytes) {
    throw 'Candidate APK changed; update the reviewable candidate record before any upload.'
}
[PSCustomObject]@{
    localReady = $true
    cliVersion = $taskVersion.data.version
    candidateVersion = $taskCandidate.versionName
    candidateVersionCode = $taskCandidate.versionCode
    apk = $taskCandidate.apk
    sha256 = $taskDigest
    loginAttemptedByCheck = $false
    remoteWritePerformedByCheck = $false
    fixedVersionDeviceTested = $taskCandidate.fixedVersionDeviceTested
} | ConvertTo-Json
