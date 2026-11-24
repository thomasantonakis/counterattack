[CmdletBinding()]
param([switch] $Launch)

$ErrorActionPreference = 'Stop'
$root = (git -C $PSScriptRoot rev-parse --show-toplevel)
if ($LASTEXITCODE -ne 0) { throw 'Could not locate the Counterattack checkout.' }
$root = $root.Trim()
$cli = @('--project-path', $root, '--caller', 'codex', '--skill', 'unity-cli', '--format', 'json')

function Invoke-Pipeline {
    param([string] $Command, [string[]] $Arguments = @())
    $raw = & unity command $Command @Arguments @cli
    if ($LASTEXITCODE -ne 0) { throw "Unity ${Command} failed: $raw" }
    $response = ($raw -join "`n") | ConvertFrom-Json
    if (!$response.success) { throw "Unity ${Command} failed: $raw" }
    return $response.data.result
}

function Assert-EditorReady {
    $editor = Invoke-Pipeline 'editor_status'
    if ($editor.playMode -ne 'stopped' -or $editor.compiling -or $editor.domainReloadInProgress) {
        throw 'Stop Play Mode and wait for Unity to finish compiling.'
    }
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
$raw = & unity recompile --project-path $root --format json
if ($LASTEXITCODE -ne 0) { throw "Unity compilation failed: $raw" }
$compile = ($raw -join "`n") | ConvertFrom-Json
if (!$compile.success -or $compile.data.failed -or $compile.data.compilationFailed) {
    throw "Unity compilation failed: $raw"
}
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
    $build = Invoke-Pipeline 'build_status'
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
