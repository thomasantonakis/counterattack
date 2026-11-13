# Focused online Room proof

This uses `Assets/Scenes/OnlineRoomProof.unity`, an owned copy of Room with
Room's real serialized rule managers, deterministic authored board/roster and
separate replica display objects. Normal offline Room remains the default.
An explicit guest session suppresses authority startup in normal Room; the
owned proof also serializes its authority hierarchy inactive on guest load.
It is a first ground-pass/interception slice, not a complete online match.

Wire is ground-wire-v7 (binary CA07). Roster/catalog incompatibility is rejected before
replica application. Committed snapshots carry decision/owner/legal actions,
token and ball cells, statistics, clock/timing values and holds. Animation
frames are a separate presentation payload, bound to the accepted publication;
they cannot advance guest rules. Both applications must pull and rebuild.
The authority alone advances live clocks and roll deadlines. Clock-only
publications preserve command-decision revisions, so a legal command does not
become stale merely while travelling. Reliable outcomes/holds publish at once;
timed full snapshots are bounded to four per second and presentation to twenty.
The first-action completion snapshot stays immutable during its final digest
acknowledgement; this proof then ends rather than running a second action.
Interrupted EOS connections and send failures disable local submission; a
known graceful peer close is distinguished from a recoverable transport loss.

## Integrated action controls

The authored action dropdown and Execute button now expose real ground and long
pass commands for the current team. Long pass supports preview/confirmation,
accuracy, scatter direction/distance, mandatory defending goalkeeper Pace
move/claim/decline, and landing interceptions. Roll observations and aerial pass
statistics come from the host. Selecting a dropdown option is personal; Execute
submits its exact current semantic command. Ordinary Room remains offline.

Space landings can automatically start movement under the existing rules.
Basic movement now supports player selection, destinations, team-phase completion
and read-only player animation. Final Third player movement/forfeit and return
to ordinary movement are integrated. High passes support preparation, accuracy,
scatter, keeper rush responses and unchallenged ball control. Headers now support
owned nominations, confirmation, automatic/manual rolls, result holds and headed
passes, headed keeper back-pass hands/feet/catch choices and landing interceptions.
Guests receive detached nominees, actual scores and the winner. Header
goal-line choices are wired; headed-shot and loose-ball resolution,
dribbling/interactions, shots and restarts are still being integrated;
this proof is not a complete online match. Manual paired checks stay deferred.

## Mac setup, build and local checks

Keep the Windows host on its development network and the family Mac on its
hotspot. Open the Mac checkout in Unity 6000.5.3f1 manually and save dirty
scenes. Use that Mac's existing ignored local EOS configuration and identity.
Do not copy configuration or credentials between machines.

```sh
cd /Users/vasilisantonakis/Documents/Projects/Personal/counterattack
git pull --ff-only origin codex/online-foundation-personal-mac
bash Documentation/Setup/BuildEOSStandaloneMac.sh room
bash Documentation/Setup/RunEOSStandaloneMac.sh login room
bash Documentation/Setup/RunEOSStandaloneMac.sh local room
```

The application is `Build/OnlineRoom/Mac/CounterattackOnlineRoom.app`.
Local verifies packaged real gameplay, shared pause and injected liveness
recovery; it does not exercise EOS connectivity or two-human interaction.

## Windows host and Mac guest

The Windows export can be reproduced through the open, stopped, clean Editor:

```powershell
$repoRoot = (git rev-parse --show-toplevel).Trim()
unity command build --target StandaloneWindows64 --outputPath Build/OnlineRoom/Windows/CounterattackOnlineRoom.exe --scenes '[\"Assets/Scenes/OnlineRoomProof.unity\"]' --confirm true --project-path $repoRoot --caller codex --skill unity-cli --format json
unity command build_status --project-path $repoRoot --caller codex --skill unity-cli --format json
```

Poll build_status until completed/Succeeded before running the application;
inspect diagnostics separately. Paired/manual sessions are deferred until the
user has time. Do not start a host wait merely because the script is prepared.

After both local reports pass, start Windows from this checkout:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Documentation/Setup/RunEOSStandaloneWindows.ps1 host -Room -Manual
```

After `Manual handoff published`, run on the Mac:

```sh
bash Documentation/Setup/RunEOSStandaloneMac.sh guest room manual
```

The guest owns Home and the host owns Away. On the Mac click Start pass,
enter X=0 and Z=6, then click Preview/confirm once to preview. Either player
may pause; only the requester can resume. Click Preview/confirm again to
commit. When the interception is pending, Windows clicks Defender roll.
The first completed action exchanges and confirms final digests, retains
evidence and ends the proof. The authored controls remain available in edit
mode. Camera and Tab statistics visibility are personal.

For actual reconnect acceptance, interrupt the Mac's network at the preview
or pending-roll boundary for more than five seconds, then restore its network
before the proof deadline. Do not close/relaunch the host. The authority must
hold; the guest must stop submitting. The returning authenticated peer gets a
fresh connection generation and acknowledges the full snapshot before release.
Shared pause must retain its requester. This is live-authority recovery, not
host migration or arbitrary process-crash restoration.

Both `result.json` files must pass. Compare `gameplay-evidence.json` digests,
rollSequence, recoveryGenerations and final-state.json ownership, statistics
and continuation. The guest must record guestRulesInactive=true. Retain both
local run folders under TestResults/GameplayRegression/Runs/. Require removal
of ActiveEOSRoomLobby.json from the remote branch. Do not report a paired pass
from one report, a running response or deterministic fake-transport tests.

## Prepared reverse roles and verification boundary

Reverse-role support is prepared, not executed. After local builds pass on both
machines, start on the Mac:

```sh
bash Documentation/Setup/RunEOSStandaloneMacHost.sh room manual
```

After its handoff is published, start Windows as guest:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Documentation/Setup/RunEOSStandaloneWindows.ps1 guest -Room -Manual
```

The guest still owns Home, regardless of operating system. Require both actual
reports and matching digests before claiming this role reversal passed.

Focused startup and real Room action cases have passed locally; exact native
results are in the feature Verification.md files. Paired manual Room operation,
actual EOS interruption/rejoin and reverse-role execution remain unexecuted.
The controlled six-token fixture is not production roster/catalog integration.
Other shots, movements, restarts, draft, full-match clocks and online settings/
substitutions are outside this proof. No full regression suite is claimed.
