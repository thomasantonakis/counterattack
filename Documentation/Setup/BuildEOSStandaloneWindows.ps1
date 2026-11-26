$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

function Invoke-Pipeline([string[]]$Arguments) {
    $PSNativeCommandArgumentPassing = 'Legacy'
    $output = & unity command @Arguments --project-path $repoRoot --caller codex --skill unity-cli --format json
    $exitCode = $LASTEXITCODE
    $response = $output | Out-String | ConvertFrom-Json
    if ($exitCode -ne 0 -or -not $response.success) {
        throw ($output | Out-String)
    }
    return $response.data.result
}

$editor = Invoke-Pipeline -Arguments @('editor_status')
if ($editor.playMode -ne 'stopped' -or $editor.compiling -or $editor.domainReloadInProgress) {
    throw 'Wait for the open Editor to finish loading and exit Play Mode.'
}
$scenes = Invoke-Pipeline -Arguments @('list_open_scenes')
if ($scenes.scenes | Where-Object { $_.isDirty }) { throw 'Save open scenes before building.' }
$tests = Invoke-Pipeline -Arguments @('test_status')
if ($tests.status -notin @('completed', 'no_tests')) { throw 'Wait for the current test run to finish.' }

$exe = 'Build/EOSProof/Windows/CounterattackEOSProof.exe'
$queued = Invoke-Pipeline -Arguments @(
    'build', '--target', 'StandaloneWindows64', '--outputPath', $exe,
    '--scenes', '[\"Assets/Scenes/EOSStandaloneProof.unity\"]', '--confirm', 'true'
)
if ($queued.status -ne 'queued' -or -not $queued.buildId) { throw 'Editor did not queue the proof build.' }
$deadline = [DateTime]::UtcNow.AddMinutes(20)
do {
    $build = Invoke-Pipeline -Arguments @('build_status')
    if ($build.buildId -ne $queued.buildId) { throw 'Editor returned a different build ID.' }
    if ($build.status -eq 'completed') {
        $run = Join-Path $repoRoot ('TestResults/GameplayRegression/Runs/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-packets-Windows-build')
        New-Item -ItemType Directory -Path $run | Out-Null
        $report = Join-Path $run 'build-report.json'
        $build | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $report
        if ($build.result -ne 'Succeeded' -or -not (Test-Path -LiteralPath $exe)) {
            throw "EOS packet build failed. See $report"
        }
        Write-Host "Windows proof build succeeded. Report at $report"
        return
    }
    Start-Sleep -Seconds 2
} while ([DateTime]::UtcNow -lt $deadline)
throw 'Build polling timed out. Inspect build_status before starting another build.'
