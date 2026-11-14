# Counterattack EOS proof from Windows Boot Camp

Use **Windows PowerShell**, not Git Bash, on the personal laptop after you
manually boot into Windows. This Windows installation is a separate machine
environment from macOS: sign in to GitHub, Codex, Unity and EOS locally. Keep
the family Mac on macOS as the guest. The Windows laptop hosts the match probe.

Current branch: `codex/online-foundation-personal-mac`. Read `AGENTS.md`,
`Documentation/Plans/OnlineMultiplayer.md`, `Documentation/Plans/EOSSetup.md`,
and `Assets/Editor/FeatureTests/OnlineEOSConnectivity/Verification.md` after
cloning. Windows native initialization/login, liveness registration, exports
and local gameplay/recovery checks have passed. Current evidence is recorded in
`Assets/Editor/FeatureTests/OnlineShots/Verification.md`; current Mac/Windows
commands are in `Documentation/Setup/OnlineRoomProof.md`. The setup steps below
remain applicable to a fresh Windows installation. Paired/manual Room acceptance
is deferred and full-match internet play is still being integrated.

## 1. Install Windows tools in PowerShell

Open PowerShell in Windows. Do not run these blocks in Git Bash or WSL. If
`winget` is missing, install or update Windows App Installer, or use the official
Windows installers for [Git](https://git-scm.com/download/win) and
[GitHub CLI](https://cli.github.com/).

```powershell
winget install --id Git.Git -e
winget install --id GitHub.cli -e
winget install --id Unity.CLI -e
```

Open a **new PowerShell** window so the new commands are on `PATH`. The Unity
CLI also offers an [official PowerShell installer](https://docs.unity.com/en-us/unity-cli/use-unity-cli):
`irm https://unity.com/install.ps1 | iex` if the `winget` route is unavailable.
Choose one Unity CLI installation route, not both.

Install Codex with the [official Windows installer](https://learn.chatgpt.com/docs/codex/cli.md):

```powershell
powershell -ExecutionPolicy ByPass -c "irm https://chatgpt.com/codex/install.ps1 | iex"
```

Open another PowerShell window and verify:

```powershell
git --version
gh --version
unity --version
codex --version
```

If one command is missing, reopen PowerShell after its installer and check its
`PATH` instructions. Do not translate the scripts into Git Bash syntax.

## 2. Sign in and clone a Windows checkout

Use your GitHub account with access to the private repository. Do not copy
GitHub tokens, Codex auth files, macOS EOS configuration or device identity
storage into Windows.

```powershell
gh auth login
gh auth status
gh auth setup-git
$projectBase = Join-Path $env:USERPROFILE 'Documents\Projects\Personal'
New-Item -ItemType Directory -Force -Path $projectBase | Out-Null
Set-Location $projectBase
```

If `counterattack` is absent, clone it:

```powershell
git clone --branch codex/online-foundation-personal-mac https://github.com/thomasantonakis/counterattack.git
Set-Location .\counterattack
git status --short --branch
git log -1 --oneline
$repoRoot = (git rev-parse --show-toplevel).Trim()
```

If a Windows checkout already exists, inspect `git status --short --branch`
first. Preserve its local edits. Then fetch and fast-forward this branch; stop
if Git refuses because of local work:

```powershell
Set-Location (Join-Path $projectBase 'counterattack')
git fetch origin codex/online-foundation-personal-mac
git switch codex/online-foundation-personal-mac
git pull --ff-only origin codex/online-foundation-personal-mac
$repoRoot = (git rev-parse --show-toplevel).Trim()
```

Use this Windows checkout as the Unity project. Do not point Unity at the
macOS volume's checkout. Git carries the project; Windows rebuilds `Library/`
and keeps its own local EOS identity.

Start Codex **from this repository** and sign in with ChatGPT when prompted:

```powershell
Set-Location $repoRoot
codex
```

This is a new local Codex sign-in. The portable `.codex/config.toml` applies
when Codex starts in the checkout. The repository's `AGENTS.md` records your
Git authorization; sandbox or credential prompts imposed by a tool may still
appear.

## 3. Install and open Unity manually

Install [Unity Hub](https://unity.com/download) for Windows, sign in and
activate your license. Install the exact project Editor **6000.5.3f1** shown in
`ProjectSettings/ProjectVersion.txt`. Add the Windows checkout to Hub and open
it **manually**. Let package restore, imports and compilation finish. The
repository pins the EOS Unity plugin and Unity Pipeline package. Do not launch
Unity from Codex, run batchmode or open ordinary gameplay for this proof.

The EOS probe now selects PlayEveryWare's Windows platform adapter in a Windows
Editor. It has compiled on macOS; its Windows compilation and native loading
must be verified here before claiming cross-platform support.

From PowerShell in the checkout:

```powershell
$repoRoot = (git rev-parse --show-toplevel).Trim()
unity pipeline editors --format json
unity status --project-path $repoRoot --format json
unity command editor_status --project-path $repoRoot --caller codex --skill unity-cli --format json
unity command list_open_scenes --project-path $repoRoot --caller codex --skill unity-cli --format json
```

The connected instance must be `ready` and point at this Windows checkout.
If Pipeline is absent, first wait for imports, inspect compile errors/Safe
Mode, and then use the installed Editor UI to start or restart the Pipeline
server if necessary. Do not reinstall Pipeline merely to force a reconnect.

## 4. Configure EOS locally for Windows

In Unity, open **EOS Plugin → EOS Configuration**. Enter the same development
EOS product, Live sandbox, Release deployment and desktop Client ID/Client
Secret used by the two Macs. Give Product Version a nonempty value, select the
deployment and client for **Windows**, then choose **Save All Changes**. Keep
all values in local, Git-ignored `Assets/StreamingAssets/EOS/` files. Do not
paste them into chat, commit them or copy another machine's EOS device-identity
storage. The plugin already bundles its native SDK.

## 5. Verify only the focused Windows prerequisites

Read `Documentation/Testing/RunningTests.md`. Before each test, ensure Unity
compilation/reload has finished, Play Mode is stopped, no test is pending and
every loaded scene is clean. If a scene is dirty, save through Unity and recheck.
Use the live Unity CLI/Pipeline; do not run `dotnet build`, batchmode, ordinary
gameplay or whole test suites.

```powershell
$repoRoot = (git rev-parse --show-toplevel).Trim()
unity command recompile --project-path $repoRoot --caller codex --skill unity-cli --format json
unity command recompile_status --project-path $repoRoot --caller codex --skill unity-cli --format json
unity command editor_status --project-path $repoRoot --caller codex --skill unity-cli --format json
unity command list_open_scenes --project-path $repoRoot --caller codex --skill unity-cli --format json
unity command test_status --project-path $repoRoot --caller codex --skill unity-cli --format json
```

Poll `recompile_status` until `up_to_date` or `completed` with zero errors.
If the new Windows adapter fails to compile or initialize, ask Codex in this
Windows checkout to diagnose and fix it before the host case.

The one-case reuse fixture is a focused native EOS initialization and login
check. It consumes its marker and does not create a lobby. Run it only after
the Editor is clean:

```powershell
New-Item -ItemType File -Force -Path 'Temp\OnlineEOSReuse.run' | Out-Null
unity command run_tests --mode editor --filter EOSProbeSessionReuseEditorTests.TwoSequentialSessionsLogInWithoutRestartingEditor --filter_type testName --async_tests true --project-path $repoRoot --caller codex --skill unity-cli --format json
unity command test_status --project-path $repoRoot --caller codex --skill unity-cli --format json
```

Poll `test_status` until `completed` and inspect the actual counts and newest
native XML under `TestResults/GameplayRegression/Runs/`. A `running` response
is not a verdict. The reuse fixture passed 1/1 on macOS; Windows must earn its
own result. The helper keeps EOS initialized across focused tests, so a second
test in the same Editor process should not require a Unity restart.

## 6. Run Windows host and family Mac guest

Put the Windows laptop and family Mac on **different networks** for the
cross-platform proof. The family Mac stays on macOS and uses its own local EOS
identity. Both Editors must be ready and clean. Pull the current branch on the
family Mac **before** starting the host, preserving its local edits:

```sh
cd /Users/vasilisantonakis/Documents/Projects/Personal/counterattack
git pull --ff-only origin codex/online-foundation-personal-mac
```

On Windows PowerShell, from the Windows checkout, run the single relay-only
host helper. It checks compilation, scenes and test state, then starts only
`EOSTwoMacP2PEditorTests.HostExchangesPacketsWithForcedRelays`:

```powershell
Set-Location $repoRoot
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Documentation\Setup\RunEOSWindowsHost.ps1 relay
```

Keep that PowerShell window open. When it prints **One-use handoff published**,
run this on the family Mac's **Terminal**:

```sh
cd /Users/vasilisantonakis/Documents/Projects/Personal/counterattack
bash Documentation/Setup/RunEOSGuest.sh relay
```

The family Mac helper runs only
`EOSTwoMacP2PEditorTests.GuestExchangesPacketsWithForcedRelays` and prints its
native XML path. The Windows host helper prints its own completed result and
native XML path, and removes the temporary handoff from the private branch.
Do not paste the lobby ID or credentials into chat. If either helper reports a
failure, preserve the native XML and Editor log for Codex to diagnose; rerun
only the affected focused case after the fix. A clean Windows/macOS packet
exchange does not yet prove standalone builds or a playable online match.

The temporary handoff uses GitHub's repository contents API. It creates and
removes short-lived commits on the private branch, so the Windows checkout may
be behind afterward. When the test and cleanup finish, use
`git pull --ff-only origin codex/online-foundation-personal-mac` to catch up.

## 7. Tell Windows Codex where to continue

Start `codex` inside the Windows checkout and send:

> This is Counterattack in Windows Boot Camp, branch
> `codex/online-foundation-personal-mac`. Read `AGENTS.md`,
> `Documentation/Setup/WindowsBootCamp.md`, `Documentation/Plans/EOSSetup.md`,
> `Documentation/Plans/OnlineMultiplayer.md` and the online EOS connectivity
> verification file. This Windows machine hosts; the family Mac is the macOS
> guest. Verify this checkout's live Unity CLI connection and run only focused
> Windows EOS tests. Do not launch Unity, enter ordinary gameplay, run whole
> suites, copy credentials or claim Windows support before native results pass.

The final result to report is Windows compilation separately from the exact
host and guest passed/failed/skipped counts and both native XML paths. Record
whether the machines were actually on different networks. The next proof after
Editor connectivity is a standalone Windows build and a standalone macOS
guest, followed by gameplay integration.
