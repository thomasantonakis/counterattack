# Shared pause ownership in the prepared online board

## Task checklist

- [x] Resolve pause requester from the authenticated connection's reserved team.
- [x] Allow either team to request pause; only its requester can resume.
- [x] Serialize simultaneous requests with a separate control revision.
- [x] Keep bounded duplicate receipts across reconnect; retired peers cannot resume.
- [x] Block old gameplay confirmations and roll requests across pause/resume.
- [x] Hold the authority's scaled time and match pause flag; keep unscaled EOS
  polling and guest rendering alive. Dispose restores the owned time scale.
- [x] Author requester/passive pause views outside the local statistics display.
- [x] Add dedicated EditMode/PlayMode cases and execute only this feature.
- [x] Export updated Windows application and run its packaged local pause check.
- [x] Rebuild Mac and run its local check, then the paired pause proof.

This is the prepared-board shared pause slice. It does not install online
controls into Room, implement substitutions/settings subflows, detect real
disconnects, freeze an active resolution on connection loss, or provide crash
recovery. Paused ownership survives an authenticated connection rebind in the
focused fixture; that is distinct from transport reconnect and resynchronization.

The server's OnlineSharedPause owns a distinct control sequence/revision lane
with the same match ID and authenticated participant mapping as gameplay.
Control requests have a fixed shared-pause decision and only pause.request or
pause.resume actions. They cannot supply a team, method name or time scale.
Receipts are capped at 64 per participant; expired retries cannot pause again.
First accepted request wins; opponent resume cannot steal ownership. The owner
remains the same team/account across a replacement authenticated connection.

The hold uses the existing match pause blocker and Time.timeScale=0 on the
authority. It restores the captured scale rather than assuming 1. EOS polling,
timeouts, frame yields and guest presentation use unscaled time. The guest's
pause view does not change its local gameplay time or activate managers.

## Validation

Live Editor compilation is clean, zero script errors/warnings.
EditMode: **11 passed, 0 failed, 0 skipped**. Native XML:
`TestResults/GameplayRegression/Runs/20261111T220310248Z/results.xml`.
PlayMode: **13 passed, 0 failed, 0 skipped**. Native XML:
`TestResults/GameplayRegression/Runs/20261111T220358201Z/results.xml`.

Updated Windows export reported Succeeded, with the known missing vswhere.exe
diagnostic (one error entry, seven warnings). Packaged local pause/gameplay
check **passed**:
`TestResults/GameplayRegression/Runs/20261111T220654045Z-Gameplay-local/result.json`.
Its gameplay-evidence.json records sharedPauseRevision=2, rollSequence=1 and
publicationSequence=8. Build report and Player.log are retained beside it.
This is a local packaging check; the v2 paired network result remains pending.

## Completed paired pause proof

The family Mac export reported Succeeded with zero errors and six warnings.
Its user-supplied local pause report passed at
`TestResults/GameplayRegression/Runs/20261111T221020Z-gameplay-local-68554/result.json`.
The separate-network paired run then passed on both standalone applications:

- Windows host, directly inspected with launcher exit 0:
  `TestResults/GameplayRegression/Runs/20261111T221104422Z-Gameplay-host/result.json`.
- Mac guest, completed report supplied by the user, retained on that Mac:
  `TestResults/GameplayRegression/Runs/20261111T221131Z-gameplay-guest-68731/result.json`.

Both evidence files report sharedPauseRevision=2, rollSequence=1,
publicationSequence=9 and identical digest
`BD30B1171DCA79D53321D313092B72836719DB5A44DA84190D787773C969CED3`.
The guest reports guestRulesInactive=true. The host passed its rejected
opponent-resume, duplicate-pause and stale-confirmation checks before final
digest acknowledgement. The helper removed the remote handoff; directory
inspection confirmed ActiveEOSGameplayLobby.json was absent. The guest Mac
remained on the hotspot per the test setup. A macOS login-keychain prompt for
the EOS Connect access token appeared during this run; it still completed.

This completes the prepared-board paired requester-owned pause proof, not the
remaining interactive Room/subflow or actual transport-disconnect work above.

The PlayMode fixture uses the existing owned board and cleanup pattern, real
pass/roll entry points, both attacking directions and both requester/host-team
assignments. It checks the real moving ball stays stationary and uncompleted
during pause, and resumes the same pass once. Pending interception requests
cannot draw a die while paused or replay a stale roll after resume. Framework
gating suppresses the legacy runner; fixture cleanup restores random/time,
singleton/event/scene state. No live saves are mutable test fixtures.

## Wire/build integration

Wire version is now ground-wire-v2; v1 is rejected before dispatch. Handoff
kind is ground-gameplay-v2; both applications must be rebuilt. Commands stay:

```sh
bash Documentation/Setup/BuildEOSStandaloneMac.sh gameplay
bash Documentation/Setup/RunEOSStandaloneMac.sh login gameplay
bash Documentation/Setup/RunEOSStandaloneMac.sh local gameplay
```

The updated automated paired proof adds a Home-guest pause at the selected
target, a duplicate pause retry, a rejected host resume and a stale confirm.
The guest sees its authored requester view for at least one unscaled second,
then alone resumes before submitting a fresh confirmation. Final gameplay
evidence must include sharedPauseRevision=2 as well as one roll and matching
digests. The local packaged role performs the same owner/opponent pause checks.

Broader affected checks for the user, not run automatically: category
OnlineGroundPassNetwork (EditMode OnlineGroundPassWireEditorTests under
`Assets/Editor/FeatureTests/OnlineGroundPassNetwork/`, PlayMode
OnlineGroundPassNetworkPlayModeTests under
`Assets/Tests/PlayMode/Features/OnlineGroundPassNetwork/`) for the v2 codec,
additive snapshot fields and authored board; and category OnlineGroundPass
under `Assets/Tests/PlayMode/Features/OnlineGroundPass/` for the existing
gateway's blocked/stale behavior. No full suites are warranted or claimed.
