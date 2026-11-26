# Authoritative online dice results

- [x] Publish actual authority results without another dice draw.
- [x] Bound and detach history; isolate replacement match data and restored clocks.
- [x] Reject guest dice before randomness or counter mutation.
- [x] Atomically reject foreign, reordered, future, impossible or rewritten observations and sequence rewind.
- [x] Version wire as ground-wire-v4 / binary CA04 and update desktop handoff helpers.
- [ ] Integrate remaining pass/shot/header/result interaction phases.

Compilation separately clean (live Editor reload, zero errors).
Focused EditMode category OnlineRollResults / OnlineRollResultsEditorTests:
**11 passed, 0 failed, 0 skipped**. Native results:
`TestResults/GameplayRegression/Runs/20261113T171640642Z/results.xml`.
Affected focused checks: Room snapshot 6 EditMode (171648317Z), wire 13
EditMode (171656276Z), pause 11 EditMode (171704249Z), interrupted exchange
7 PlayMode (171719094Z). All paths have prefix
`TestResults/GameplayRegression/Runs/20261113T` and suffix `/results.xml`;
all passed, zero failed/skipped. No complete suites ran.

The authored status label displays the latest authoritative actor/context/face.
Replica history is observation only; it cannot invoke rules or randomness.
Both desktop exports must rebuild for v4. Paired/manual checks remain deferred.
This does not make the controlled Room proof a full online match.
