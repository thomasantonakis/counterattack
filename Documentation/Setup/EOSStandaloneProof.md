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

## Spectator lobby transport proof

Use the packet proof build on three clients with distinct EOS device identities.
Use Windows for one role and macOS for another. Build and run each client from
the same commit. Start the host first, then give its local `lobby-id.txt` to the
manager. The host writes `watch-lobby-id.txt` only after the manager handshake
and the second lobby's creation. Give that file to the viewer. Transfer the
complete files privately while the host is running. Each file contains an
internal EOS lobby ID and the same Match Code. The host validates that code
and the authenticated sender before granting either role. The game will
perform the lobby lookup internally from the one shareable Match Code.

On macOS, build and start one of the three roles. Substitute the absolute path
to the appropriate file for a client role.

```sh
git fetch origin &&
git switch feature/online-spectators &&
git pull --ff-only origin feature/online-spectators &&
git rev-parse HEAD &&
bash Documentation/Setup/BuildEOSStandaloneMac.sh packets &&
bash Documentation/Setup/RunEOSStandaloneMac.sh watch-host packets
# For the manager or viewer, use one of these in place of the host command.
# bash Documentation/Setup/RunEOSStandaloneMac.sh watch-manager packets automatic /absolute/path/lobby-id.txt
# bash Documentation/Setup/RunEOSStandaloneMac.sh watch-viewer packets automatic /absolute/path/watch-lobby-id.txt
```

On Windows, use Windows PowerShell in the checkout.

```powershell
git fetch origin
if ($LASTEXITCODE -ne 0) { throw 'Fetch failed' }
git switch feature/online-spectators
if ($LASTEXITCODE -ne 0) { throw 'Checkout failed' }
git pull --ff-only origin feature/online-spectators
if ($LASTEXITCODE -ne 0) { throw 'Fast-forward failed' }
git rev-parse HEAD
.\Documentation\Setup\BuildEOSStandaloneWindows.ps1
.\Documentation\Setup\RunEOSStandaloneWindows.ps1 -Role watch-manager -LobbyFile 'C:\path\lobby-id.txt'
# For the host or viewer, use one of these in place of the manager command.
# .\Documentation\Setup\RunEOSStandaloneWindows.ps1 -Role watch-host
# .\Documentation\Setup\RunEOSStandaloneWindows.ps1 -Role watch-viewer -LobbyFile 'C:\path\watch-lobby-id.txt'
```

The Mac app is `Build/EOSProof/Mac/CounterattackEOSProof.app`. The Windows app
is `Build/EOSProof/Windows/CounterattackEOSProof.exe`. Require passing local
`result.json` files from all three roles and removal of both host handoff files.
Repeat with Windows hosting and macOS running a client. This proof covers one
viewer, forced relay routing, the manager packet exchange during the watch
lobby's lifetime, and the exchange after watch lobby teardown.
