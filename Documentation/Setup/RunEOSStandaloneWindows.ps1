param([ValidateSet('login', 'host', 'guest', 'local')][string]$Role = 'host', [switch]$Gameplay, [switch]$Manual, [switch]$Room, [switch]$Continuous, [switch]$Managed, [string]$RunDirectory, [string]$VerificationRequestId)
$ErrorActionPreference = 'Stop'
if ($Room) { $Gameplay = $true }
if ($Continuous) { $Manual = $true }
if ($Manual -and (-not $Gameplay -or $Role -notin @('host','guest'))) { throw 'Manual requires host/guest -Gameplay or -Room.' }
if ($Managed -and ($Manual -or $Role -ne 'host')) { throw 'Managed is only for the automatic verification host.' }
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
Set-Location $repoRoot
$exe = Join-Path $repoRoot 'Build/EOSProof/Windows/CounterattackEOSProof.exe'
if ($Gameplay) { $exe = Join-Path $repoRoot 'Build/OnlineGroundPass/Windows/CounterattackOnlineGroundPass.exe' }
if ($Room) { $exe = Join-Path $repoRoot 'Build/OnlineRoom/Windows/CounterattackOnlineRoom.exe' }
if ($Role -eq 'local' -and -not $Gameplay) { throw 'The local role requires -Gameplay.' }
if (-not (Test-Path -LiteralPath $exe)) { throw 'Build the dedicated Windows proof first; see EOSStandaloneProof.md.' }
$proofName = if ($Gameplay) { 'Gameplay' } else { 'Standalone' }
if ($Room) { $proofName = 'Room' }
$run = Join-Path $repoRoot ('TestResults/GameplayRegression/Runs/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-' + $proofName + '-' + $Role)
if ($RunDirectory) { $run = [IO.Path]::GetFullPath($RunDirectory) }
New-Item -ItemType Directory -Path $run | Out-Null
$resultPath = Join-Path $run 'result.json'
$lobbyPath = Join-Path $run 'lobby-id.txt'
$branch = 'codex/online-foundation-personal-mac'
$uri = 'repos/thomasantonakis/counterattack/contents/Documentation/Setup/ActiveEOSStandaloneLobby.json'
$handoffName = 'ActiveEOSStandaloneLobby.json'
$handoffKind = 'standalone-relay-v1'
if ($Gameplay) {
    $handoffName = 'ActiveEOSGameplayLobby.json'; $handoffKind = 'ground-gameplay-v7'
    $uri = 'repos/thomasantonakis/counterattack/contents/Documentation/Setup/' + $handoffName
}
if ($Manual) { $handoffKind = 'ground-manual-v6' }
if ($Room) {
    $handoffName = 'ActiveEOSRoomLobby.json'; $handoffKind = if ($Manual) { 'room-manual-v5' } else { 'room-gameplay-v5' }
    $uri = 'repos/thomasantonakis/counterattack/contents/Documentation/Setup/' + $handoffName
}
if ($Continuous) { $handoffKind = if ($Room) { 'room-continuous-v1' } else { 'gameplay-continuous-v1' } }
$publishedSha = $null
if ($Role -eq 'guest') {
    $encodedHandoff = & gh api ($uri + '?ref=' + $branch) --jq '.content'
    if ($LASTEXITCODE -ne 0) { throw 'Could not read the reverse-role handoff.' }
    $handoff = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String(($encodedHandoff -join '').Replace("`n",''))) | ConvertFrom-Json
    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    if ($handoff.kind -ne $handoffKind -or $handoff.lobbyId -cnotmatch '^[0-9a-f]{32}$' -or $handoff.createdUnix -gt $now + 30 -or $now - $handoff.createdUnix -ge 900) {
        throw 'Incompatible or expired reverse-role handoff.'
    }
    [IO.File]::WriteAllText($lobbyPath, $handoff.lobbyId)
}
if ($Role -eq 'host') {
    $names = & gh api "repos/thomasantonakis/counterattack/contents/Documentation/Setup?ref=$branch" --jq '.[].name'
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect remote handoff state.' }
    if ($names -contains $handoffName) { throw 'A previous handoff exists; resolve it before hosting.' }
}
Write-Host "Starting standalone $Role. Local artifacts: $run"
# Paths are quoted for Windows native argument parsing; no secrets travel on the command line.
$playerArgs = '-ca-eos-role ' + $Role + ' -ca-eos-output "' + $run + '" -logFile "' + (Join-Path $run 'Player.log') + '" -screen-fullscreen 0 -screen-width 640 -screen-height 360'
if ($Manual) { $playerArgs += ' -ca-online-manual'; $playerArgs = $playerArgs.Replace('-screen-width 640 -screen-height 360', '-screen-width 1280 -screen-height 720') }
if ($Continuous) { $playerArgs += ' -ca-online-continuous' }
if ($Role -eq 'guest') { $playerArgs += ' -ca-eos-lobby-file "' + $lobbyPath + '"' }
$proofWindow = if ($Manual) { 'Normal' } else { 'Hidden' }
$player = Start-Process -FilePath $exe -ArgumentList $playerArgs -WindowStyle $proofWindow -PassThru
try {
    if ($Role -eq 'host') {
        $deadline = [DateTime]::UtcNow.AddSeconds(120)
        while (-not (Test-Path -LiteralPath $lobbyPath) -and -not (Test-Path -LiteralPath $resultPath) -and -not $player.HasExited -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Seconds 1 }
        if (-not (Test-Path -LiteralPath $lobbyPath)) {
            if (Test-Path -LiteralPath $resultPath) { Get-Content -LiteralPath $resultPath | Write-Host }
            throw "Host did not create a lobby. Inspect local Player.log in $run"
        }
        $lobbyId = (Get-Content -Raw -LiteralPath $lobbyPath).Trim()
        if ($lobbyId -cnotmatch '^[0-9a-f]{32}$') { throw 'Unexpected lobby ID format.' }
        $handoffData = @{ kind = $handoffKind; lobbyId = $lobbyId; createdUnix = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() }
        if ($Managed -and $VerificationRequestId) { $handoffData.requestId = $VerificationRequestId }
        $handoff = $handoffData | ConvertTo-Json -Compress
        $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($handoff + "`n"))
        $raw = & gh api -X PUT $uri -f 'message=Share temporary standalone EOS lobby' -f "content=$encoded" -f "branch=$branch"
        if ($LASTEXITCODE -ne 0) { throw 'Could not publish standalone handoff.' }
        $publishedSha = (($raw -join "`n") | ConvertFrom-Json).content.sha
        $guestMode = if ($Room) { 'room' } else { 'gameplay' }
        if ($Manual) { Write-Host "Manual handoff published. On the Mac: bash Documentation/Setup/RunEOSStandaloneMac.sh guest $guestMode manual" }
        elseif ($Gameplay) { Write-Host "Gameplay handoff published. On the Mac: bash Documentation/Setup/RunEOSStandaloneMac.sh guest $guestMode" }
        else { Write-Host 'Standalone handoff published. On the Mac: bash Documentation/Setup/RunEOSStandaloneMac.sh guest' }
    }
    $deadline = [DateTime]::UtcNow.AddSeconds($(if ($Managed) { 300 } elseif ($Continuous) { 7500 } elseif ($Role -eq 'host' -or $Manual) { 1250 } else { 120 }))
    while (-not (Test-Path -LiteralPath $resultPath) -and -not $player.HasExited -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Seconds 1 }
    if (-not (Test-Path -LiteralPath $resultPath)) { throw "No completed report. Inspect $run/Player.log; close only the proof application if it is still running." }
    $report = Get-Content -Raw -LiteralPath $resultPath | ConvertFrom-Json
    $report | ConvertTo-Json | Write-Host
    Write-Host "Standalone result: $resultPath"
    if ($report.status -ne 'passed') { throw 'Standalone probe failed; retain the local report and Player.log.' }
    if ($Gameplay -and $Role -ne 'login') {
        $evidencePath = Join-Path $run 'gameplay-evidence.json'
        if (-not (Test-Path -LiteralPath $evidencePath)) { throw 'No gameplay evidence: verify the correct gameplay build was used.' }
        $evidence = Get-Content -Raw -LiteralPath $evidencePath | ConvertFrom-Json
        if (-not $Manual -and ($evidence.sharedPauseRevision -ne 2 -or $evidence.rollSequence -ne 1)) { throw 'Missing shared-pause evidence; rebuild the updated gameplay application.' }
        if ($Manual -and $evidence.role -ne ($Role + '-manual')) { throw 'Missing manual exchange evidence.' }
        if ($Role -eq 'guest' -and -not $evidence.guestRulesInactive) { throw 'Guest rules were not inert.' }
        Get-Content -LiteralPath $evidencePath | Write-Host
    }
} finally {
    if ($Managed -and $player -and -not $player.HasExited) {
        # The exact standalone process launched by this attempt, never Unity.
        $player.Kill()
        $player.WaitForExit(10000) | Out-Null
    }
    if ($publishedSha) {
        & gh api -X DELETE $uri -f 'message=Remove temporary standalone EOS lobby' -f "sha=$publishedSha" -f "branch=$branch" | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning 'Standalone handoff cleanup failed; resolve it before another host run.' }
        else { Write-Host 'Temporary standalone handoff removed.' }
    }
}
