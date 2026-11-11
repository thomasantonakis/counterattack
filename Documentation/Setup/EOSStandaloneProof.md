# Standalone EOS packaging and packet proof

This exports only `Assets/Scenes/EOSStandaloneProof.unity`, an authored scene
with one probe component. It does not start Room or ordinary gameplay. Both
peers force and read back `ForceRelays`, verify the exact sender/socket/channel
and payload bytes, and clean up their connection/lobby. Each application exits
with a retained `result.json` and local `Player.log` under
`TestResults/GameplayRegression/Runs/<timestamp>-Standalone-<role>/`.
These are application probe reports, not Unity Test Framework XML results.
Keep logs/configuration local; do not commit the exported applications because
they contain this machine's ignored development EOS configuration.

## Prepare both machines

Keep the family Mac on the hotspot and Windows on home Wi-Fi. Leave both
Editors open in their respective checkouts. Save dirty scenes through Unity
and wait for compilation before building. Do not launch another Editor or
use Unity batchmode. Build only through the live Pipeline `build` command.
The Mac build script refuses an unready Editor, dirty scenes or pending tests.
The standalone run scripts do not require an Editor connection.

The Windows CLI skill currently uses old Pipeline command names; keep using
those names for this pinned package. No Pipeline upgrade is required.

## Windows build and local login

From PowerShell in this checkout, after clean compile/status/scene checks:

```powershell
$repoRoot = (git rev-parse --show-toplevel).Trim()
unity command build --target StandaloneWindows64 --outputPath Build/EOSProof/Windows/CounterattackEOSProof.exe --scenes '[\"Assets/Scenes/EOSStandaloneProof.unity\"]' --confirm true --project-path $repoRoot --caller codex --skill unity-cli --format json
```

Poll `unity command build_status` with the same project/caller/skill metadata.
Require `data.result.status == completed` and `data.result.result == Succeeded`.
Inspect any report errors/warnings separately; queueing is not build success.
The build uses the locally configured scripting backend.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Documentation/Setup/RunEOSStandaloneWindows.ps1 login
```

## Mac build and local login

Pull the committed changes before building, preserving local edits:

```sh
cd /Users/vasilisantonakis/Documents/Projects/Personal/counterattack
git pull --ff-only origin codex/online-foundation-personal-mac
bash Documentation/Setup/BuildEOSStandaloneMac.sh
bash Documentation/Setup/RunEOSStandaloneMac.sh login
```

The `.app` uses the Mac's local EOS configuration and build architecture.
Retain its build result, macOS/CPU details and login report. If it fails, stop
before the paired run and inspect local Player.log. Do not copy another
machine's device identity or credentials.

## Paired run

After **both standalone login reports pass**, start Windows:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Documentation/Setup/RunEOSStandaloneWindows.ps1 host
```

When it prints **Standalone handoff published**, run on the Mac:

```sh
bash Documentation/Setup/RunEOSStandaloneMac.sh guest
```

Keep both terminals open. The host removes the one-use
`ActiveEOSStandaloneLobby.json` remote handoff in its cleanup block. This file
is separate from Editor handoffs. The guest rejects stale/wrong-kind handoffs.
Report both completed JSON paths and results, plus which machine used the
hotspot. A queued packet, lobby creation or one passing peer alone does not
complete the paired proof. Standalone application packaging and connectivity
do not establish gameplay synchronization, recovery, performance or signing.

The host's remote handoff creates temporary Git commits; fast-forward the
checkouts after both applications finish and cleanup succeeds.
