# Online loose balls

- [x] Reproduce missing tied-header scatter commands.
- [x] Owned typed direction, distance and recovery rolls with automatic deadlines.
- [x] Preserve real presentation/collection/statistics and both directions.
- [x] Holds, stale commands, guest rejection and cancelled/replaced continuations.
- [x] Focused tests, compilation and affected checks.

Keeper moves, shot outcomes and restart boundaries remain separate integration
work. Paired acceptance is deferred.

Compilation separately clean. Category `OnlineLooseBall` /
`OnlineLooseBallPlayModeTests`: **18 distinct PlayMode passed**, zero failed/skipped,
with native XML retained under `TestResults/GameplayRegression/Runs/`:

- `20261113T224750355Z/results.xml`: 14 tied-header cases cover direction,
  distance, space, successful/failed recovery, typed automatic deadlines, guest/
  wrong-team/replayed input rejection, overlapping holds, cancellation and a
  replacement phase that completes independently, all in both directions.
- `20261113T224938514Z/results.xml`: 4 real headed-keeper catch spills continue
  through common-panel direction/distance into open play, manual or automatic,
  in both directions. Host roll contexts and keeper actor attribution are asserted.

Initial missing routing reproduced 12 failures in
`20261113T224156037Z/results.xml`: ordinary actions were incorrectly offered
while mandatory loose-ball rolls were pending. Dedicated loose-ball routing now
takes priority over normal action availability, with scoped typed roll admission.
The first 12 cases passed in `20261113T224358496Z/results.xml`. Two additional
replacement cases then failed in `20261113T224554955Z/results.xml`: the old
distance-result continuation deactivated the replacement phase. Version checks
now retire old presentation, input waits and parent continuations before they
can clear new state. The final 14-case run above passes. Rules, directions,
recovery thresholds and possession decisions remain the real manager's behavior.

Affected `LegacyHeaderResolutionPlayModeTests`: **12 passed**, zero failed/skipped,
in `20261113T225032504Z/results.xml`, preserving offline headed-pass interceptions,
ties, scatter, recovery and continuation. No complete suites ran. Broader user-run
checks if needed: category DecisionTiming in
`Assets/Tests/PlayMode/Features/DecisionTiming/`, `LegacyHeaderResolutionPlayModeTests`
and `Assets/Tests/PlayMode/Features/GoalkeeperPickup/` for other scatter/keeper paths.

Unexecuted online boundaries include general goalkeeper free/box moves, shot
rebounds/goals, out-of-bounds restarts and cancellation during physical flight.
Cancellation/replacement evidence here is at the rolled-result boundary. Existing
Windows exports recorded in OnlineKeeperDistribution/Verification.md precede this
batch and must refresh before paired acceptance. Mac commands remain in
Documentation/Setup/OnlineRoomProof.md.
