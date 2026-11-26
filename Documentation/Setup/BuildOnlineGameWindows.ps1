[CmdletBinding()]
param([switch] $Launch)

$ErrorActionPreference = 'Stop'
$root = (git -C $PSScriptRoot rev-parse --show-toplevel)
if ($LASTEXITCODE -ne 0) { throw 'Could not locate the Counterattack checkout.' }
$root = $root.Trim()
$cli = @('--project-path', $root, '--caller', 'codex', '--skill', 'unity-cli', '--format', 'json')

function Invoke-Pipeline {
    param(
        [string] $Command,
        [string[]] $Arguments = @(),
        [int] $TimeoutSeconds = 30,
        [int] $RetrySeconds = 0
    )
    $deadline = [DateTime]::UtcNow.AddSeconds($RetrySeconds)
    do {
        $raw = & unity command $Command @Arguments @cli --timeout $TimeoutSeconds
        $exitCode = $LASTEXITCODE
        $message = $raw -join "`n"
        if ($exitCode -eq 0) {
            $response = $message | ConvertFrom-Json
            if (!$response.success) { throw "Unity ${Command} failed: $message" }
            return $response.data.result
        }
        if ($message -notmatch 'socket hang up|ECONNRESET|ECONNREFUSED|EDITOR_NOT_RUNNING|EDITOR_UNREACHABLE|EDITOR_TIMEOUT|No running Editor|no.*Pipeline|timed out') {
            throw "Unity ${Command} failed: $message"
        }
        if ([DateTime]::UtcNow -ge $deadline) { break }
        Start-Sleep -Seconds 2
    } while ($true)

    & unity status --project-path $root --format json | Write-Host
    throw "Unity ${Command} did not answer for this checkout. Check the open Editor for a blocking dialog and confirm Pipeline is running. Last response: $message"
}

function Assert-EditorReady {
    $deadline = [DateTime]::UtcNow.AddMinutes(2)
    do {
        $editor = Invoke-Pipeline 'editor_status' -TimeoutSeconds 10 -RetrySeconds 60
        if ($editor.playMode -ne 'stopped') { throw 'Stop Play Mode before building.' }
        if (!$editor.compiling -and !$editor.domainReloadInProgress) { break }
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Unity is still compiling or reloading. Check the open Editor for a blocking dialog.' }
        Start-Sleep -Seconds 2
    } while ($true)
    $loaded = Invoke-Pipeline 'list_open_scenes'
    if ($loaded.scenes | Where-Object { $_.isDirty }) {
        throw 'Save the open scenes in Unity before building.'
    }
    $tests = Invoke-Pipeline 'test_status'
    if ($tests.status -notin @('no_tests', 'completed')) {
        throw 'Wait for the active Unity test run before building.'
    }
}

Assert-EditorReady
Write-Host 'Requesting script compilation through the open Unity Editor...'
$null = Invoke-Pipeline 'recompile' -TimeoutSeconds 120
$deadline = [DateTime]::UtcNow.AddMinutes(5)
do {
    $compile = Invoke-Pipeline 'recompile_status' -TimeoutSeconds 10 -RetrySeconds 60
    if ($compile.failed -or $compile.compilationFailed -or $compile.summary.errors -gt 0) {
        throw "Unity compilation failed: $($compile | ConvertTo-Json -Depth 8 -Compress)"
    }
    if ($compile.status -in @('completed', 'up_to_date')) { break }
    if ([DateTime]::UtcNow -ge $deadline) { throw 'Unity compilation did not finish in five minutes. Check the open Editor and its log.' }
    Start-Sleep -Seconds 2
} while ($true)
Assert-EditorReady

$exe = Join-Path $root 'Build/OnlineGame/Windows/Counterattack.exe'
$scenes = @(
    'SplashScreen', 'MainMenu', 'CreateLoadRoom', 'OnlineModeMenu',
    'CreateNewGameScene', 'Draft', 'FreeDraft', 'Room'
) | ForEach-Object { "Assets/Scenes/$_.unity" }
$sceneJson = ConvertTo-Json -InputObject @($scenes) -Compress
# Windows PowerShell uses legacy native argument passing and strips unescaped quotes.
if ($PSVersionTable.PSVersion -lt [version]'7.3') {
    $sceneJson = $sceneJson.Replace('"', '\"')
} else {
    $PSNativeCommandArgumentPassing = 'Standard'
}

Write-Host 'Building the Windows player through the open Unity Editor...'
$queued = Invoke-Pipeline 'build' @(
    '--target', 'StandaloneWindows64', '--outputPath', $exe,
    '--scenes', $sceneJson, '--confirm', 'true'
)
if ($queued.status -ne 'queued' -or !$queued.buildId) {
    throw 'Unity did not queue the Windows build.'
}

$deadline = [DateTime]::UtcNow.AddMinutes(20)
do {
    Start-Sleep -Seconds 2
    $build = Invoke-Pipeline 'build_status' -TimeoutSeconds 15 -RetrySeconds 60
    if ($build.buildId -ne $queued.buildId) { throw 'Unity reported a different build.' }
} until ($build.status -eq 'completed' -or [DateTime]::UtcNow -ge $deadline)

if ($build.status -ne 'completed' -or $build.result -ne 'Succeeded') {
    $build | ConvertTo-Json -Depth 8 | Write-Host
    throw 'The Windows build did not succeed.'
}
Write-Host "Build succeeded: $exe"
if ($Launch) {
    Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe)
}
