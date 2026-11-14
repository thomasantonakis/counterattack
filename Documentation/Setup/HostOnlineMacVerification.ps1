param([ValidateRange(1,24)][int]$Hours = 8)
$ErrorActionPreference='Stop'
$repoRoot=(git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
Set-Location -LiteralPath $repoRoot
$source=(git rev-parse HEAD).Trim()
$trees=@('Assets','Packages','ProjectSettings')|ForEach-Object{(git rev-parse ('HEAD:'+$_)).Trim()}
$sha256=[Security.Cryptography.SHA256]::Create()
try{$fingerprint=([BitConverter]::ToString($sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes(($trees -join "`n"))))).Replace('-','').ToLowerInvariant()}finally{$sha256.Dispose()}
$queueBranch='verification/queue'
$queuePath='Documentation/Testing/VerificationQueue'
$api='repos/thomasantonakis/counterattack'
$artifactRoot=Join-Path $repoRoot ('TestResults/GameplayRegression/Runs/'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')+'-WindowsVerificationHost')
New-Item -ItemType Directory -Path $artifactRoot|Out-Null
$processed=@{}; $downloaded=@{}
$buildFiles=@{
 room=(Join-Path $repoRoot 'Build/OnlineRoom/Windows/CounterattackOnlineRoom_Data/Managed/Assembly-CSharp.dll')
 gameplay=(Join-Path $repoRoot 'Build/OnlineGroundPass/Windows/CounterattackOnlineGroundPass_Data/Managed/Assembly-CSharp.dll')
}
$buildHashes=@{}
foreach($mode in $buildFiles.Keys){
 if(-not (Test-Path -LiteralPath $buildFiles[$mode])){throw "Fresh Windows $mode managed export is required before hosting"}
 $buildHashes[$mode]=(Get-FileHash -LiteralPath $buildFiles[$mode] -Algorithm SHA256).Hash
}
# Temporary thread-owned idle-sleep prevention; no persistent power-plan change.
Add-Type -TypeDefinition 'using System.Runtime.InteropServices; public static class OnlineVerificationPower { [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags); }'
[OnlineVerificationPower]::SetThreadExecutionState([uint32]2147483649)|Out-Null
$deadline=[DateTime]::UtcNow.AddHours($Hours)
$heartbeat=[DateTime]::MinValue

function Read-Github([string]$endpoint) {
 $local:ErrorActionPreference='Continue'
 $raw=& gh api $endpoint 2>$null
 if($LASTEXITCODE -ne 0){return $null}
 return ($raw -join "`n")|ConvertFrom-Json
}
function Decode-Github($file) {
 if(-not $file){return $null}
 return [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String(($file.content -replace '\s','')))|ConvertFrom-Json
}
function Publish-HostResult([string]$id,$value) {
 $endpoint="$api/contents/$queuePath/$id-host.json"
 $old=Read-Github ($endpoint+'?ref='+$queueBranch)
 $json=$value|ConvertTo-Json -Depth 40
 $content=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json+"`n"))
 $arguments=@('api','-X','PUT',$endpoint,'-f',"branch=$queueBranch",'-f',"message=Record Windows verification $id",'-f',"content=$content")
 if($old){$arguments+=@('-f',"sha=$($old.sha)")}
 & gh @arguments | Out-Null
 if($LASTEXITCODE -ne 0){throw 'Could not publish Windows verification result'}
}
function Read-LocalJson([string]$path) {
 if(Test-Path -LiteralPath $path){return Get-Content -Raw -LiteralPath $path|ConvertFrom-Json}
 return $null
}
Write-Output "Verification host watching for Mac requests. Source: $source. Artifacts: $artifactRoot"
while([DateTime]::UtcNow -lt $deadline) {
 try {
  $files=Read-Github ("$api/contents/$queuePath"+'?ref='+$queueBranch)
  foreach($file in $files) {
   if($file.name -notmatch '^([A-Za-z0-9_-]+)\.json$' -or $file.name -match '-host\.json$'){continue}
   $id=[IO.Path]::GetFileNameWithoutExtension($file.name)
   if($processed.ContainsKey($id)){continue}
   $request=Decode-Github (Read-Github ($file.url))
   if(-not $request -or $request.kind -ne 'online-verification-request-v1' -or $request.requestId -ne $id -or $request.status -ne 'requested'){continue}
   $age=[DateTimeOffset]::UtcNow.ToUnixTimeSeconds()-[long]$request.createdUnix
   if($age -lt -30 -or $age -gt 7200){continue}
   $processed[$id]=$true
   $result=@{requestId=$id; sourceCommit=$source; sourceFingerprint=$fingerprint; mode=$request.mode; startedUtc=[DateTime]::UtcNow.ToString('o'); status='running'}
   if($request.sourceFingerprint -ne $fingerprint -or $request.mode -notin @('room','gameplay')) {
    $result.status='rejected';$result.message='Gameplay/assets/package/settings fingerprint mismatch or unsupported mode; do not pair different builds.'
    Publish-HostResult $id $result;continue
   }
   $result.buildAssemblySha256=$buildHashes[$request.mode]
   if((Get-FileHash -LiteralPath $buildFiles[$request.mode] -Algorithm SHA256).Hash -ne $result.buildAssemblySha256){
    $result.status='rejected';$result.message='The exported gameplay assembly changed while the host was pinned to an earlier source.'
    Publish-HostResult $id $result;continue
   }
   $prior=Read-Github ("$api/contents/$queuePath/$id-host.json"+'?ref='+$queueBranch)
   if($prior){continue}
   $run=Join-Path $artifactRoot $id
   Publish-HostResult $id $result
   $arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'RunEOSStandaloneWindows.ps1'),'host','-Managed','-RunDirectory',$run,'-VerificationRequestId',$id)
   if($request.mode -eq 'room'){$arguments+='-Room'}else{$arguments+='-Gameplay'}
   Write-Output "Hosting Mac request $id"
   # Synchronous, serial attempts. The launcher owns and retires its standalone
   # process on failure, and removes only the handoff it published.
   & powershell.exe @arguments *> (Join-Path $artifactRoot ($id+'.log'))
   $exitCode=$LASTEXITCODE
   $result.exitCode=$exitCode
   $result.report=Read-LocalJson (Join-Path $run 'result.json')
   $result.evidence=Read-LocalJson (Join-Path $run 'gameplay-evidence.json')
   $result.finalState=Read-LocalJson (Join-Path $run 'final-state.json')
   # Retry cleanup only for this exact managed request, after its process exited.
   $handoffName=if($request.mode -eq 'room'){'ActiveEOSRoomLobby.json'}else{'ActiveEOSGameplayLobby.json'}
   $handoffEndpoint="$api/contents/Documentation/Setup/$handoffName"
   $handoffFile=Read-Github ($handoffEndpoint+'?ref=codex/online-foundation-personal-mac')
   $handoffValue=Decode-Github $handoffFile
   if($handoffValue -and $handoffValue.requestId -eq $id){
    & gh api -X DELETE $handoffEndpoint -f 'message=Retire owned verification handoff' -f "sha=$($handoffFile.sha)" -f 'branch=codex/online-foundation-personal-mac'|Out-Null
    $result.handoffCleanupRetried=$true
    $result.handoffCleanupSucceeded=($LASTEXITCODE -eq 0)
   }
   $result.completedUtc=[DateTime]::UtcNow.ToString('o')
   $result.status=if($exitCode -eq 0 -and $result.report.status -eq 'passed'){'passed'}else{'failed'}
   $result|ConvertTo-Json -Depth 40|Set-Content -LiteralPath (Join-Path $artifactRoot ($id+'-host.json'))
   Publish-HostResult $id $result
   Write-Output "Mac request $id finished: $($result.status)"
  }
  # Download reports without merging their branch into the gameplay checkout.
  $branches=Read-Github ("$api/git/matching-refs/heads/verification/sons-mac-")
  foreach($branch in $branches) {
   $name=$branch.ref -replace '^refs/heads/',''
   if($name -notmatch '^verification/sons-mac-([A-Za-z0-9_-]+)$'){continue}
   $runId=$Matches[1]
   if($downloaded.ContainsKey($branch.object.sha)){continue}
   $file=Read-Github ("$api/contents/Documentation/Testing/MacReports/$runId.json"+'?ref='+$name)
   if(-not $file){continue}
   $reportPath=Join-Path $artifactRoot ($runId+'-Mac-report.json')
   if(-not $file.content -and $file.sha){$file=Read-Github ("$api/git/blobs/"+$file.sha)}
   if(-not $file.content -or $file.encoding -ne 'base64'){throw 'Mac report content was not available from GitHub'}
   [IO.File]::WriteAllBytes($reportPath,[Convert]::FromBase64String(($file.content -replace '\s','')))
   $downloaded[$branch.object.sha]=$true
   $report=Read-LocalJson $reportPath
   Write-Output "MAC_REPORT_READY $reportPath status=$($report.status)"
  }
 } catch {Write-Output ('Host supervisor: '+$_.Exception.Message)}
 if([DateTime]::UtcNow -ge $heartbeat){Write-Output ('Waiting for Mac. '+[DateTime]::UtcNow.ToString('o'));$heartbeat=[DateTime]::UtcNow.AddMinutes(1)}
 Start-Sleep -Seconds 10
}
Write-Output 'Verification host watchdog finished; no Unity process was restarted.'
[OnlineVerificationPower]::SetThreadExecutionState([uint32]2147483648)|Out-Null
