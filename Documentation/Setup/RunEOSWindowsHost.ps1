param(
    [ValidateSet('relay', 'p2p')]
    [string] $Mode = 'relay'
)

$ErrorActionPreference = 'Stop'
$branch = 'codex/online-foundation-personal-mac'
$repoRoot = [IO.Path]::GetFullPath((& git rev-parse --show-toplevel).Trim())
if ($LASTEXITCODE -ne 0) { throw 'Run this script from the Windows Counterattack checkout.' }
Set-Location $repoRoot
if ((& git branch --show-current).Trim() -ne $branch) { throw "Check out $branch first." }
foreach ($tool in @('git', 'gh', 'unity')) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "Missing tool: $tool" }
}

function Invoke-Pipeline {
    param([string] $Name, [string[]] $Extra = @())
    $raw = & unity command $Name @Extra --project-path $repoRoot --caller codex --skill unity-cli --format json
    if ($LASTEXITCODE -ne 0) { throw "Unity $Name failed: $raw" }
    $value = ($raw -join "`n") | ConvertFrom-Json
    if (-not $value.success) { throw "Unity $Name failed: $raw" }
    return $value
}

Write-Host 'Checking the Windows host Editor connection...'
$statusRaw = & unity status --project-path $repoRoot --format json
if ($LASTEXITCODE -ne 0) { throw "Unity status failed: $statusRaw" }
$status = ($statusRaw -join "`n") | ConvertFrom-Json
$ready = @($status.data.instances | Where-Object {
    [IO.Path]::GetFullPath($_.projectPath) -eq $repoRoot -and $_.state -eq 'ready'
}).Count -gt 0
if (-not $status.success -or -not $ready) { throw 'This checkout has no ready Unity Pipeline Editor.' }

Write-Host 'Checking compilation, Play Mode, loaded scenes and pending tests...'
$null = Invoke-Pipeline 'recompile'
$compileReady = $false
for ($attempt = 0; $attempt -lt 60; $attempt++) {
    $compile = Invoke-Pipeline 'recompile_status'
    if ($compile.data.result.failed -or $compile.data.result.compilationFailed) {
        throw "Unity compilation failed: $($compile.data.result.errors | ConvertTo-Json -Compress)"
    }
    if ($compile.data.result.status -in @('up_to_date', 'completed')) {
        $compileReady = $true
        break
    }
    Start-Sleep -Seconds 2
}
if (-not $compileReady) { throw 'Unity compilation did not finish in two minutes.' }
$editor = Invoke-Pipeline 'editor_status'
if ($editor.data.result.playMode -ne 'stopped' -or $editor.data.result.compiling -or
    $editor.data.result.domainReloadInProgress) { throw 'Stop Play Mode and wait for compilation.' }
$scenes = Invoke-Pipeline 'list_open_scenes'
if ($scenes.data.result.count -lt 1 -or @($scenes.data.result.scenes | Where-Object { $_.isDirty }).Count -gt 0) {
    throw 'Save dirty loaded scenes through Unity before testing.'
}
$tests = Invoke-Pipeline 'test_status'
if ($tests.data.result.status -notin @('no_tests', 'completed')) {
    throw 'Resolve the pending test run before starting the Windows host.'
}

$marker = if ($Mode -eq 'relay') { 'Temp/OnlineEOSRelayHost.run' } else { 'Temp/OnlineEOSP2PHost.run' }
$fixture = if ($Mode -eq 'relay') {
    'EOSTwoMacP2PEditorTests.HostExchangesPacketsWithForcedRelays'
} else {
    'EOSTwoMacP2PEditorTests.HostReceivesPacketAndAcknowledgesDistinctGuest'
}
$lobbyFile = Join-Path $repoRoot 'Temp/OnlineEOSP2PHostLobbyId.txt'
$uri = 'repos/thomasantonakis/counterattack/contents/Documentation/Setup/ActiveEOSP2PLobby.json'

New-Item -ItemType Directory -Force -Path (Join-Path $repoRoot 'Temp') | Out-Null
if (Test-Path $lobbyFile) { throw 'An old local host lobby-ID file exists. Resolve it before starting.' }
New-Item -ItemType File -Force -Path (Join-Path $repoRoot $marker) | Out-Null
$runStartedUtc = [DateTime]::UtcNow
$publishedSha = $null
try {
    Write-Host "Starting only $fixture ..."
    $started = Invoke-Pipeline 'run_tests' @('--mode', 'editor', '--filter', $fixture,
        '--filter_type', 'testName', '--async_tests', 'true')
    if ($started.data.result.result -ne 'running') { throw 'The host test did not start asynchronously.' }

    for ($attempt = 0; $attempt -lt 120 -and -not (Test-Path $lobbyFile); $attempt++) {
        Start-Sleep -Seconds 1
    }
    if (-not (Test-Path $lobbyFile)) {
        $early = Invoke-Pipeline 'test_status'
        throw "The host did not create a lobby. Test status: $($early.data.result | ConvertTo-Json -Compress)"
    }
    $lobbyId = (Get-Content -Raw $lobbyFile).Trim()
    if ($lobbyId -cnotmatch '^[0-9a-f]{32}$') { throw 'The host lobby ID has an unexpected format.' }
    $handoff = @{ lobbyId = $lobbyId; createdUnix = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() } |
        ConvertTo-Json -Compress
    $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($handoff + "`n"))
    $publishedRaw = & gh api -X PUT $uri -f 'message=Share temporary Windows EOS host lobby' -f "content=$encoded" -f "branch=$branch"
    if ($LASTEXITCODE -ne 0) { throw "Could not publish the one-use lobby handoff. Check for a stale remote handoff: $publishedRaw" }
    $publishedSha = (($publishedRaw -join "`n") | ConvertFrom-Json).content.sha
    if (-not $publishedSha) { throw 'GitHub did not return the handoff file SHA.' }
    Write-Host 'One-use handoff published. On the family Mac, run: bash Documentation/Setup/RunEOSGuest.sh relay'

    $final = $null
    for ($attempt = 0; $attempt -lt 720; $attempt++) {
        $final = Invoke-Pipeline 'test_status'
        if ($final.data.result.status -ne 'running') { break }
        Start-Sleep -Seconds 2
    }
    Write-Host ($final.data.result | ConvertTo-Json -Depth 8)
    $xml = Get-ChildItem (Join-Path $repoRoot 'TestResults/GameplayRegression/Runs') -Filter results.xml -Recurse |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if ($xml -and $xml.LastWriteTimeUtc -ge $runStartedUtc) {
        Write-Host "Native test XML: $($xml.FullName.Substring($repoRoot.Length + 1))"
    } else {
        Write-Warning 'No new native test XML was found for this run.'
    }
    if ($final.data.result.status -ne 'completed' -or $final.data.result.summary.failed -ne 0 -or
        $final.data.result.summary.passed -ne 1) { throw 'The focused Windows host case did not pass 1/1.' }
} finally {
    if ($publishedSha) {
        $deleteRaw = & gh api -X DELETE $uri -f 'message=Remove temporary Windows EOS host lobby' -f "sha=$publishedSha" -f "branch=$branch"
        if ($LASTEXITCODE -ne 0) { Write-Warning "Remove the stale remote handoff manually: $deleteRaw" }
        else { Write-Host 'One-use remote lobby handoff removed.' }
    }
}
