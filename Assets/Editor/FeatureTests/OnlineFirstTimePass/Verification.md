# Online First Time Pass

- [x] Reproduce missing online FTP admission.
- [x] Typed targeting, preparation moves/skip and interceptions.
- [x] Real pass continuation, both directions and ownership.
- [x] Holds, interruption and automatic roll admission.
- [x] Live compilation and focused category OnlineFirstTimePass.
- [x] Record native XML and refresh handoff.

Compilation separately clean. Category `OnlineFirstTimePass`, fixture
`OnlineFirstTimePassPlayModeTests`: **14 passed**, zero failed/skipped, in
`TestResults/GameplayRegression/Runs/20261114T103218502Z/results.xml`.
Initial reproduction: **14 failed**, missing `ftp.start` after a real completed
ground pass, in `20261114T102958152Z/results.xml`.

Coverage: real preceding pass/availability, medium target preview/confirmation,
both-team preparation moves or skips, interception failure/success, actor and
pass statistics, possession/collection, late input, automatic deadlines, held
results and cancellation after the dice draw, both directions. Typed timing
registration uses the same guarded entry point as manual admission. An epoch
guard retires cancelled interception-result continuation before cleared state
is accessed. Offside movement/skip predicates remain the source manager's rules.

Real Room serialized extension `OnlineRoomFirstTimePassPlayModeTests`:
**2 passed**, zero failed/skipped, in `20261114T103724760Z/results.xml`.
This completes a real ground pass followed by FTP in both directions using the
authored Room managers, rejects illegal target/guest calls and admits final
replica statistics. Initial Room-only fixture failures (nested host/guest session
scopes) are retained in `20261114T103600978Z/results.xml`; the corrected fixture
disposes/restores its host scope around the guest assertion. Gameplay rules and
assertions were preserved. Feature has **16 distinct passing cases** across the
14-case and 2-case runs; the entire category was not repeated for the extension.

Affected offline `LegacyFirstTimePassPlayModeTests`: **10 passed**, zero
failed/skipped, in `20261114T103412350Z/results.xml`. No full suites ran.
Broader user-run checks when available: category `DecisionTiming` in
`Assets/Tests/PlayMode/Features/DecisionTiming/` for other decision ownership and
action-budget transitions. Paired/manual remains deferred; the Room proof is
still six tokens and its exchange ends after the first ground action.
