# Independent-process ground-pass proof

This is an automated prepared-board gameplay probe, isolated from Room and
live saves. It is not yet an interactive online match. The guest owns Home;
the host owns Away and is the only rules/dice/coroutine authority. Both use
the same admitted semantic commands. The guest never activates its gameplay
root; authored display tokens and ball render host snapshots. Wheel/right-drag
and Tab control only the local camera/statistics display.

The guest submits start, target preview and confirmation for a real ordinary
ground pass, repeats the confirmation, and attempts an unauthorized defensive
roll. The host rejects the wrong-team command, returns the duplicate receipt,
then submits and repeats its own defending-team roll. Exactly one real roll
resolves. Either legal dice outcome is accepted, with both sides requiring
the same final-state digest and one pass/interception attempt. The guest
acknowledges that state before leaving; the host destroys the lobby.

## Prepare the Mac before hosting

Keep Windows on home Wi-Fi and the family Mac on its hotspot. Save dirty
Editor scenes. On the family Mac:

```sh
cd /Users/vasilisantonakis/Documents/Projects/Personal/counterattack
git pull --ff-only origin codex/online-foundation-personal-mac
bash Documentation/Setup/BuildEOSStandaloneMac.sh gameplay
bash Documentation/Setup/RunEOSStandaloneMac.sh login gameplay
bash Documentation/Setup/RunEOSStandaloneMac.sh local gameplay
```

`login` validates native EOS/configuration. `local` checks the packaged real
ground pass/roll and detached replica locally; it does not prove networking.
Both must pass before the paired run. The authored scene is
`Assets/Scenes/OnlineGroundPassProof.unity`; build output is
`Build/OnlineGroundPass/Mac/CounterattackOnlineGroundPass.app`.

## Windows build and local check

From the repository PowerShell, with the Editor ready, compiled, stopped and
all loaded scenes clean, use the live Pipeline command (never batchmode):

```powershell
$repoRoot = (git rev-parse --show-toplevel).Trim()
unity command build --target StandaloneWindows64 --outputPath Build/OnlineGroundPass/Windows/CounterattackOnlineGroundPass.exe --scenes '[\"Assets/Scenes/OnlineGroundPassProof.unity\"]' --confirm true --project-path $repoRoot --caller codex --skill unity-cli --format json
```

Poll `build_status` with the same project/caller/skill metadata, requiring
completed/Succeeded. Inspect build errors/warnings separately.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Documentation/Setup/RunEOSStandaloneWindows.ps1 local -Gameplay
```

## Paired run

After the Mac's local checks pass, start Windows:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Documentation/Setup/RunEOSStandaloneWindows.ps1 host -Gameplay
```

When it prints **Gameplay handoff published**, run on the Mac:

```sh
bash Documentation/Setup/RunEOSStandaloneMac.sh guest gameplay
```

Keep the terminals open. Every run retains `result.json`, `Player.log`, and for
completed gameplay `final-state.json` and `gameplay-evidence.json` under local
`TestResults/GameplayRegression/Runs/`. These are application reports, not
framework test XML. For the paired verdict require both pass reports, matching
digests, guestRulesInactive=true on the guest, rollSequence=1, and successful
removal of `Documentation/Setup/ActiveEOSGameplayLobby.json` from the remote
branch. This one-use handoff is separate from earlier packet probes.

Do not commit or share exported applications/configuration or raw local EOS
logs; builds contain local development credentials. Report the safe result and
evidence JSON, not account/client secrets. Source changes contain none.

Shared pause ownership, actual disconnect freezing/resync, interactive input,
full animation replication, reverse roles and a full online Room match remain
later M1 work. Timing/AI/presentation callbacks are disabled in this prepared
fixture rather than claiming those integration risks are already solved.
