# Pure request-window validation; no network or process side effects.
function Test-OnlineVerificationHostRequestCurrent {
 param($Request, [long]$NowUnix)
 if (-not $Request -or $Request.kind -ne 'online-verification-request-v1' -or $Request.status -ne 'requested') { return $false }
 [long]$created = 0
 if (-not [long]::TryParse([string]$Request.createdUnix, [ref]$created)) { return $false }
 [long]$expires = $created + 600 # Compatibility with the original ten-minute guest wait.
 if ($Request.PSObject.Properties.Name -contains 'expiresUnix') {
  if (-not [long]::TryParse([string]$Request.expiresUnix, [ref]$expires)) { return $false }
 }
 return ($created -le $NowUnix + 30 -and $expires -gt $created -and $expires -le $created + 7200 -and $NowUnix -lt $expires)
}

# Put the JSON body in a UTF-8 file, rather than a Windows process argument.
# A complete Room report routinely exceeds CreateProcess's command-line limit.
function Publish-OnlineVerificationHostResult {
 param([string]$Id, $Value, [string]$Api, [string]$QueuePath,
       [string]$QueueBranch, [string]$ArtifactRoot, $Previous)
 if ($Id -notmatch '^[A-Za-z0-9_-]+$') { throw 'Invalid verification request ID' }
 $endpoint="$Api/contents/$QueuePath/$Id-host.json"
 $json=$Value|ConvertTo-Json -Depth 40
 $payload=@{
  branch=$QueueBranch
  message="Record Windows verification $Id"
  content=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json+"`n"))
 }
 if ($Previous) { $payload.sha=$Previous.sha }
 $uploadPath=Join-Path $ArtifactRoot ($Id+'-host-upload.json')
 try {
  [IO.File]::WriteAllText($uploadPath, ($payload|ConvertTo-Json -Depth 4), (New-Object Text.UTF8Encoding($false)))
  & gh api -X PUT $endpoint --input $uploadPath | Out-Null
  if ($LASTEXITCODE -ne 0) { throw 'Could not publish Windows verification result' }
 } finally {
  if (Test-Path -LiteralPath $uploadPath) { Remove-Item -LiteralPath $uploadPath }
 }
}
