param([ValidateRange(0,7500)][int]$Seconds=3900)
$ErrorActionPreference='Stop'
$repoRoot=(git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$exe=Join-Path $repoRoot 'Build/OnlineAuth/Windows/CounterattackOnlineAuth.exe'
if(-not (Test-Path -LiteralPath $exe)){throw 'Build the isolated EOSStandaloneProof scene at Build/OnlineAuth/Windows/CounterattackOnlineAuth.exe before running this probe.'}
$run=Join-Path $repoRoot ('TestResults/GameplayRegression/Runs/'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')+'-WindowsAuthSoak')
New-Item -ItemType Directory -Path $run|Out-Null
$arguments='-ca-eos-role login -ca-eos-output "'+$run+'" -logFile "'+(Join-Path $run 'Player.log')+'" -screen-fullscreen 0 -screen-width 640 -screen-height 360'
if($Seconds -gt 0){$arguments+=' -ca-eos-auth-soak-seconds '+$Seconds}
Add-Type -TypeDefinition 'using System.Runtime.InteropServices; public static class AuthSoakPower { [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags); }'
[AuthSoakPower]::SetThreadExecutionState([uint32]2147483649)|Out-Null
$player=Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $repoRoot -WindowStyle Hidden -PassThru
$deadline=[DateTime]::UtcNow.AddSeconds($Seconds+150)
$heartbeat=[DateTime]::MinValue
Write-Output ('Authentication probe PID '+$player.Id+'; seconds='+$Seconds+'; artifacts='+$run)
try {
 while(-not $player.HasExited -and -not (Test-Path -LiteralPath (Join-Path $run 'result.json')) -and [DateTime]::UtcNow -lt $deadline){
  if([DateTime]::UtcNow -ge $heartbeat){Write-Output ('Authentication probe active '+[DateTime]::UtcNow.ToString('o'));$heartbeat=[DateTime]::UtcNow.AddSeconds(45)}
  Start-Sleep -Seconds 1
 }
 if(-not (Test-Path -LiteralPath (Join-Path $run 'result.json'))){throw 'Authentication probe did not retain a result before its deadline'}
 $report=Get-Content -Raw -LiteralPath (Join-Path $run 'result.json')|ConvertFrom-Json
 $report|ConvertTo-Json|Write-Output
 if($report.status -ne 'passed' -or $report.authenticationRefreshes -lt 1 -or ($Seconds -ge 3600 -and $report.authenticationRefreshes -lt 2)){throw 'Authentication renewal probe failed; retain its local evidence'}
} finally {
 [AuthSoakPower]::SetThreadExecutionState([uint32]2147483648)|Out-Null
 if(-not $player.HasExited){if(-not $player.WaitForExit(10000)){$player.Kill();$player.WaitForExit(10000)|Out-Null}}
}
