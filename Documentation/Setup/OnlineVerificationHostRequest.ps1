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
